# Correction du Crash Time Stretch - SoundTouch P/Invoke

## 🐛 Problème Initial
AudioPlay plantait lors de l'utilisation de l'effet Time Stretch avec l'implémentation native SoundTouch.

## 🔍 Cause Identifiée
La DLL native `SoundTouch.dll` se trouvait dans :
```
bin\Debug\net8.0-windows\runtimes\win-x64\native\SoundTouch.dll
```

Mais **P/Invoke cherche la DLL dans le répertoire de sortie principal** :
```
bin\Debug\net8.0-windows\SoundTouch.dll
```

## ✅ Solutions Implémentées

### 1️⃣ Chargement Manuel de la DLL (SoundTouchInterop.vb)
Ajout d'un constructeur statique qui charge la DLL depuis le bon chemin :

```vb
Shared Sub New()
	Try
		Dim architecture As String = If(Is64Bit, "win-x64", "win-x86")
		Dim basePath As String = AppDomain.CurrentDomain.BaseDirectory
		Dim dllPath As String = Path.Combine(basePath, "runtimes", architecture, "native", DllName)

		If Not File.Exists(dllPath) Then
			dllPath = Path.Combine(basePath, DllName)
		End If

		If File.Exists(dllPath) Then
			Dim handle As IntPtr = LoadLibrary(dllPath)
			' ...
		End If
	Catch ex As Exception
		' Log mais ne crash pas
	End Try
End Sub
```

### 2️⃣ Protection contre les Handles Null
Toutes les méthodes de `SoundTouchInterop` vérifient maintenant si le handle est valide :

```vb
Public Sub SetTempo(tempo As Single)
	If handle = IntPtr.Zero Then Return
	soundtouch_setTempo(handle, tempo)
End Sub
```

### 3️⃣ Gestion d'Erreur Robuste (TimeStretchSampleProvider.vb)
L'initialisation capture désormais tous les types d'exception :

```vb
Try
	soundTouch = New SoundTouchInterop()

	If soundTouch Is Nothing Then
		' Log et continue en mode bypass
		Return
	End If

	' Configuration...

Catch ex As DllNotFoundException
	System.Diagnostics.Debug.WriteLine($"SoundTouch DLL non trouvée: {ex.Message}")
	soundTouch = Nothing
	isInitialized = True ' Éviter les tentatives répétées
Catch ex As Exception
	System.Diagnostics.Debug.WriteLine($"Erreur: {ex.Message}")
	soundTouch = Nothing
	isInitialized = True
End Try
```

**Mode Bypass Automatique** : Si SoundTouch ne peut pas être initialisé, l'effet est simplement désactivé :

```vb
If soundTouch Is Nothing Then
	' Passer le signal tel quel
	Return sourceProvider.Read(buffer, offset, count)
End If
```

### 4️⃣ Copie Automatique Post-Build (AudioPlay.vbproj)
Ajout d'une tâche MSBuild qui copie automatiquement la DLL à la racine après chaque compilation :

```xml
<Target Name="CopySoundTouchDll" AfterTargets="Build">
  <PropertyGroup>
	<SoundTouchArchitecture>win-x64</SoundTouchArchitecture>
	<SoundTouchSourcePath>$(OutputPath)runtimes\$(SoundTouchArchitecture)\native\SoundTouch.dll</SoundTouchSourcePath>
	<SoundTouchDestPath>$(OutputPath)SoundTouch.dll</SoundTouchDestPath>
  </PropertyGroup>
  <Copy SourceFiles="$(SoundTouchSourcePath)" DestinationFiles="$(SoundTouchDestPath)" />
</Target>
```

Maintenant, **à chaque build**, la DLL est automatiquement copiée au bon endroit !

### 5️⃣ Script PowerShell Manuel (copy-soundtouch.ps1)
Un script de secours pour copier manuellement si nécessaire :

```powershell
.\AudioPlay\copy-soundtouch.ps1
```

## 📊 Résumé des Fichiers Modifiés

| Fichier | Modification |
|---------|-------------|
| `SoundTouchInterop.vb` | ✅ Chargement manuel DLL + protection handles |
| `TimeStretchSampleProvider.vb` | ✅ Gestion erreur robuste + fallback bypass |
| `AudioPlay.vbproj` | ✅ Target MSBuild pour copie automatique |
| `copy-soundtouch.ps1` | ✅ Script manuel de copie (NEW) |

## 🎯 Résultat

L'application **ne crashe plus** même si :
- ❌ La DLL native n'est pas trouvée
- ❌ Le chargement P/Invoke échoue
- ❌ La création d'instance SoundTouch échoue

Dans tous ces cas → **Mode Bypass** : l'audio passe sans modification.

## ✅ Build Réussie
```
Génération réussie
SoundTouch.dll copié vers: bin\Debug\net8.0-windows\SoundTouch.dll
```

## 🧪 Test Recommandé

1. **Lancer AudioPlay**
2. **Ouvrir un fichier audio**
3. **Activer Time Stretch** dans Paramètres
4. **Changer le slider**
5. **Vérifier** :
   - ✅ Pas de crash
   - ✅ Logs dans Debug Output (dans Visual Studio)
   - ✅ Si SoundTouch charge : effet appliqué
   - ✅ Si SoundTouch échoue : audio joue normalement sans effet

## 📝 Logs de Débogage

Ouvrir la fenêtre **Output** dans Visual Studio pour voir :

```
SoundTouch chargé depuis: G:\...\bin\Debug\net8.0-windows\SoundTouch.dll
SoundTouch: instance créée avec handle 12345678
SoundTouch natif initialisé avec succès
```

Ou si échec :

```
SoundTouch DLL non trouvée: ...
Time Stretch désactivé - fallback en mode bypass
```

## 🚀 Prochaines Étapes

Si l'effet fonctionne maintenant :
- ✅ Tester différentes valeurs de tempo (0.5x, 1.5x, 2.0x)
- ✅ Comparer la qualité avec Audacity
- ✅ Vérifier la stabilité sur de longues pistes

Si l'effet ne fonctionne toujours pas :
- 🔍 Vérifier les logs dans la fenêtre Output
- 🔍 Vérifier que `SoundTouch.dll` est bien dans `bin\Debug\net8.0-windows\`
- 🔍 Essayer de copier manuellement : `.\AudioPlay\copy-soundtouch.ps1`

---

## 💡 Pourquoi Cette Solution ?

**Architecture P/Invoke .NET** :
- Les DLL natives doivent être dans le même répertoire que l'exécutable
- NuGet place les DLL dans `runtimes\{arch}\native\` pour la portabilité
- Mais P/Invoke ne cherche pas automatiquement dans ces sous-répertoires
- → Il faut soit copier la DLL, soit charger manuellement avec `LoadLibrary`

Nous avons fait **les deux** pour maximum de robustesse ! 🛡️
