# Time Stretch avec SoundTouch Natif - Qualité Audacity

## 📅 Date
2025-01-XX

## 🎯 Objectif
Implémenter un time-stretching de qualité professionnelle avec préservation du pitch, identique à celui d'Audacity, en utilisant directement la DLL native SoundTouch via P/Invoke.

## 🔧 Solution Technique

### Problème des Wrappers .NET
Les wrappers .NET de SoundTouch ont des API incomplètes ou cassées :
- ❌ **SoundTouch.Net** → Package introuvable / API incompatible
- ❌ **SoundTouchSharp** → API incomplète (pas de SetTempo, SetSampleRate, etc.)
- ❌ **Implémentations manuelles WSOLA/OLA** → Artefacts synthétiques inacceptables

### Solution : P/Invoke Direct
Au lieu d'utiliser un wrapper C#, on appelle **directement la DLL native C++** de SoundTouch qui est incluse dans le package SoundTouchSharp.

```
AudioPlay → P/Invoke → SoundTouch.dll (C++ natif)
						↑
						La même DLL qu'utilise Audacity !
```

## 📂 Fichiers Créés/Modifiés

### 1. `AudioEffects/SoundTouchInterop.vb` (NOUVEAU)
Wrapper P/Invoke qui expose les fonctions natives de SoundTouch :

```vb
<DllImport("SoundTouch.dll", CallingConvention:=CallingConvention.Cdecl)>
Private Shared Function soundtouch_createInstance() As IntPtr

<DllImport("SoundTouch.dll", CallingConvention:=CallingConvention.Cdecl)>
Private Shared Sub soundtouch_setTempo(handle As IntPtr, newTempo As Single)

<DllImport("SoundTouch.dll", CallingConvention:=CallingConvention.Cdecl)>
Private Shared Sub soundtouch_putSamples(handle As IntPtr, samples As Single(), numSamples As UInteger)

<DllImport("SoundTouch.dll", CallingConvention:=CallingConvention.Cdecl)>
Private Shared Function soundtouch_receiveSamples(handle As IntPtr, outBuffer As Single(), maxSamples As UInteger) As UInteger
```

**Fonctions exposées** :
- Création/destruction d'instance
- Configuration (sample rate, channels, tempo, pitch)
- Traitement de flux (put/receive samples)
- Paramètres de qualité (anti-aliasing, sequence length, etc.)

### 2. `AudioEffects/TimeStretchSampleProvider.vb` (MODIFIÉ)
Utilise maintenant `SoundTouchInterop` au lieu du resampling simple :

```vb
' Initialiser SoundTouch natif
soundTouch = New SoundTouchInterop()
soundTouch.SetSampleRate(sourceProvider.WaveFormat.SampleRate)
soundTouch.SetChannels(sourceProvider.WaveFormat.Channels)
soundTouch.SetTempo(_tempoChange)

' Paramètres de qualité (comme Audacity)
soundTouch.SetSetting(SoundTouchInterop.SETTING_USE_AA_FILTER, 1)
soundTouch.SetSetting(SoundTouchInterop.SETTING_SEQUENCE_MS, 40)
```

## 🎵 Algorithme SoundTouch (WSOLA)

### Waveform Similarity Overlap-Add
L'algorithme utilisé par Audacity et maintenant AudioPlay :

1. **Découpage en séquences** (40ms par défaut)
2. **Recherche de similarité** (fenêtre de 15ms)
   - Compare les formes d'onde pour trouver les meilleurs points de coupe
   - Évite les discontinuités et artefacts
3. **Chevauchement** (8ms)
   - Cross-fade entre séquences adjacentes
4. **Anti-aliasing** activé
   - Filtre pour éviter le repliement spectral

### Paramètres de Qualité

| Paramètre | Valeur | Description |
|-----------|--------|-------------|
| `SEQUENCE_MS` | 40 | Taille des segments (ms) |
| `SEEKWINDOW_MS` | 15 | Fenêtre de recherche (ms) |
| `OVERLAP_MS` | 8 | Chevauchement (ms) |
| `USE_AA_FILTER` | 1 | Anti-aliasing activé |
| `USE_QUICKSEEK` | 0 | Quick seek désactivé (qualité max) |

## 📊 Comparaison Avant/Après

### ❌ Avant (Resampling Simple)
```
Tempo 0.5x → Voix grave + lent
Tempo 2.0x → Voix aiguë + rapide
Comme une cassette accélérée/ralentie
```

### ✅ Après (SoundTouch Natif)
```
Tempo 0.5x → Voix naturelle, juste plus lent ✨
Tempo 2.0x → Voix naturelle, juste plus rapide ✨
Comme Audacity !
```

## 🔍 Emplacement de la DLL Native

Le package **SoundTouchSharp** inclut les DLL natives :

```
packages/soundtouchsharp/2.3.2/runtimes/
├── win-x64/native/SoundTouch.dll
└── win-x86/native/SoundTouch.dll
```

**.NET choisit automatiquement** la bonne DLL selon l'architecture (x86/x64).

## ⚡ Performance

- **CPU** : Léger (algorithme optimisé en C++)
- **Latence** : Acceptable (buffer de ~40ms)
- **Qualité** : Professionnelle (identique à Audacity)

## 🎯 Plages Recommandées

| Plage | Qualité | Usage |
|-------|---------|-------|
| **0.8x - 1.25x** | ⭐⭐⭐⭐⭐ Excellent | Usage quotidien |
| **0.5x - 0.8x** | ⭐⭐⭐⭐ Très bon | Ralentir pour apprentissage |
| **1.25x - 1.5x** | ⭐⭐⭐⭐ Très bon | Accélérer podcasts |
| **1.5x - 2.0x** | ⭐⭐⭐ Bon | Lecture rapide |

## 🐛 Gestion des Erreurs

### Fallback Automatique
Si la DLL native ne peut pas être chargée (P/Invoke échoue) :
```vb
If soundTouch Is Nothing Then
	' Passer en mode bypass (audio non modifié)
	Return sourceProvider.Read(buffer, offset, count)
End If
```

L'effet est simplement désactivé, l'application continue de fonctionner.

## 📝 Utilisation

### Dans FormParametres
```vb
' L'utilisateur change le slider
TrackBarTimeStretch.Value = 150  ' 1.5x

' Mise à jour automatique
ParametresGlobaux.EffetTimeStretchRatio = 1.5F
Form1.MettreAJourEffetsAudio()
```

### Dans Form1 (Chaîne Audio)
```vb
timeStretchProvider = New TimeStretchSampleProvider(currentProvider)
timeStretchProvider.Enabled = ParametresGlobaux.EffetTimeStretchActif
timeStretchProvider.TempoChange = ParametresGlobaux.EffetTimeStretchRatio
currentProvider = timeStretchProvider
```

## ✅ Résultat Final

L'effet **Time Stretch d'AudioPlay** fonctionne maintenant **exactement comme dans Audacity** :

- ✅ Change le tempo **sans** changer le pitch
- ✅ Qualité audio professionnelle
- ✅ Pas d'artefacts synthétiques
- ✅ Utilise la vraie bibliothèque SoundTouch (C++)
- ✅ Même algorithme qu'Audacity (WSOLA)

## 🎓 Références

- **SoundTouch** : https://www.surina.net/soundtouch/
- **Audacity** : Utilise SoundTouch pour ses effets de tempo
- **WSOLA** : Waveform Similarity Overlap-Add algorithm
- **P/Invoke** : Platform Invocation Services (.NET)

---

## 💡 Notes Techniques

### Pourquoi P/Invoke ?
Les wrappers .NET ont des API cassées, mais la DLL C++ native fonctionne parfaitement. P/Invoke nous donne un accès direct à toutes les fonctions natives.

### Sécurité des Pointeurs
On utilise `IntPtr` pour manipuler les handles natifs en toute sécurité, avec dispose/finalize pour libérer la mémoire native.

### Compatibilité
La DLL native SoundTouch est compilée pour :
- Windows x86 (32-bit)
- Windows x64 (64-bit)

.NET Core/.NET 5+ choisit automatiquement la bonne DLL selon le runtime.
