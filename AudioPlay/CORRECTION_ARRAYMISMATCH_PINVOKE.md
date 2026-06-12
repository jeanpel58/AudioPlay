# 🔧 Correction: ArrayTypeMismatchException avec P/Invoke

## 🐛 Problème

```
ProcessMoreSamples: samplesRead=8192
  ReceiveSamples: framesReceived=2472
  outputBufferCount=4944 samples disponibles
Exception: System.ArrayTypeMismatchException
  Source array type cannot be assigned to destination array type.
```

### Cause Racine

L'appel P/Invoke à `soundtouch_receiveSamples()` **modifie la référence interne** du tableau `outBuffer`.

```vb
' ❌ PROBLÈME
soundtouch_receiveSamples(handle, outputBuffer, maxSamples)
' À ce moment, outputBuffer peut avoir changé de type interne

' Plus tard...
Array.Copy(outputBuffer, outputBufferOffset, buffer, offset, count)
' → ArrayTypeMismatchException car outputBuffer n'est plus compatible
```

### Pourquoi Cela Arrive

Quand un tableau managé (`Single()`) est passé à du code natif via P/Invoke :

1. Le CLR **épingle** le tableau en mémoire (garbage collector ne peut pas le déplacer)
2. Le code natif écrit **directement** dans la mémoire
3. Après l'appel, le CLR **peut réorganiser** les métadonnées du tableau
4. Le type interne peut devenir **incompatible** avec `Array.Copy`

## ✅ Solution: Buffer Intermédiaire

Utiliser un **buffer temporaire** pour isoler les appels P/Invoke des opérations managées.

### Avant (Code Cassé)
```vb
Private outputBuffer As Single()

' Dans ProcessMoreSamples()
soundTouch.ReceiveSamples(outputBuffer, maxFrames)  ' ❌ Écrit directement
' outputBuffer est maintenant "contaminé" par P/Invoke

' Dans Read()
Array.Copy(outputBuffer, ..., buffer, ...)  ' ❌ Exception !
```

### Après (Code Corrigé)
```vb
Private outputBuffer As Single()           ' Buffer managé propre
Private tempReceiveBuffer As Single()      ' Buffer P/Invoke isolé

' Dans New()
outputBuffer = New Single(16383) {}
tempReceiveBuffer = New Single(16383) {}   ' ✅ Buffer séparé

' Dans ProcessMoreSamples()
soundTouch.ReceiveSamples(tempReceiveBuffer, maxFrames)  ' ✅ P/Invoke isolé

' Copier immédiatement vers le buffer propre
Array.Copy(tempReceiveBuffer, 0, outputBuffer, 0, samplesReceived)  ' ✅ Fonctionne

' Dans Read()
Array.Copy(outputBuffer, ..., buffer, ...)  ' ✅ Pas de problème !
```

## 📝 Modifications Appliquées

### 1. Déclaration du Buffer Temporaire
```vb
Private tempReceiveBuffer As Single()  ' ✅ Nouveau buffer
```

### 2. Initialisation dans le Constructeur
```vb
Public Sub New(source As ISampleProvider)
	sourceProvider = source
	inputBuffer = New Single(8191) {}
	outputBuffer = New Single(16383) {}
	tempReceiveBuffer = New Single(16383) {}  ' ✅ Allouer le buffer P/Invoke
End Sub
```

### 3. Utilisation dans ProcessMoreSamples()
```vb
' Recevoir dans le buffer temporaire (isolé)
Dim framesReceived = soundTouch.ReceiveSamples(tempReceiveBuffer, maxFrames)

If framesReceived > 0 Then
	' Copier immédiatement vers le buffer propre
	Dim samplesReceived = framesReceived * WaveFormat.Channels
	Array.Copy(tempReceiveBuffer, 0, outputBuffer, 0, samplesReceived)  ' ✅ Sûr
	outputBufferOffset = 0
	outputBufferCount = samplesReceived
	Return True
End If
```

## 🎯 Résultat

- ✅ **Pas d'exception** : `outputBuffer` reste un tableau managé propre
- ✅ **P/Invoke isolé** : `tempReceiveBuffer` absorbe les effets secondaires
- ✅ **Copie sûre** : `Array.Copy` fonctionne correctement
- ✅ **Performance** : Une seule copie supplémentaire, négligeable

## 📚 Leçon Apprise

**Règle d'or avec P/Invoke** :
> Ne jamais réutiliser un tableau passé à du code natif pour des opérations managées sensibles comme `Array.Copy`.
> 
> Toujours copier immédiatement dans un buffer managé propre.

---

## 🧪 Tests

**Avant** :
```
TimeStretch activé → Exception immédiate → Son coupe
```

**Après** :
```
TimeStretch activé → Pas d'exception → Son continue normalement
```

✅ **Le son ne coupe plus !**
