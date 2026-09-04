# Diagnostic : Lecteur CD bloqué par erreur 32 (ERROR_SHARING_VIOLATION)

## 🔍 Problème identifié

Vos lecteurs D: et E: retournent **erreur 32** (`ERROR_SHARING_VIOLATION`) :
```
[CDAudioManager] Impossible d'ouvrir \\.\D:, erreur: 32
```

Cela signifie qu'un autre programme a **déjà ouvert les lecteurs** et empêche AudioPlay d'y accéder.

---

## 🛠️ Solutions appliquées

### 1. Partage amélioré
**Modification** : `CreateFile()` utilise maintenant :
```vb
FILE_SHARE_READ Or FILE_SHARE_WRITE
```

Au lieu de seulement `FILE_SHARE_READ`.

Cela permet à AudioPlay de coexister avec d'autres programmes qui lisent le CD.

### 2. Menu synchrone
**Problème** : Le `Async Sub` dans le handler d'événement causait un plantage du menu.

**Solution** : Retour à un `Sub` synchrone classique.

---

## 🔧 Comment tester

### Étape 1 : Fermer les programmes suspects

**Fermez ces programmes s'ils sont ouverts** :
- ❌ **Windows Explorer** avec D: ou E: ouvert
- ❌ **Exact Audio Copy (EAC)**
- ❌ **Nero / ImgBurn / CDBurnerXP**
- ❌ **iTunes** (si configuré pour détecter les CD)
- ❌ **Windows Media Player**
- ❌ **foobar2000** avec composant CDDA
- ❌ **Daemon Tools / Virtual CloneDrive**
- ❌ **Any DVD / AnyBurn**

### Étape 2 : Vérifier dans le Gestionnaire des tâches

1. **Ouvrir le Gestionnaire des tâches** (Ctrl+Shift+Esc)
2. **Onglet "Détails"**
3. **Rechercher ces processus** :
   - `EAC.exe`
   - `Nero.exe` / `NeroBurnRights.exe`
   - `ImgBurn.exe`
   - `DTAgent.exe` (Daemon Tools)
   - `explorer.exe` (parfois il faut tuer et relancer)

### Étape 3 : Éjecter et réinsérer les CD

1. **Éjecter les CD** (bouton physique du lecteur)
2. **Attendre 5 secondes**
3. **Réinsérer les CD**
4. **Attendre que Windows les reconnaisse**

### Étape 4 : Redémarrer AudioPlay

1. **Arrêter AudioPlay** (Shift+F5 dans VS)
2. **Relancer** (F5)
3. **Cliquer sur "Ajout"** → **"CD Audio"**
4. **Vérifier les logs dans Output**

---

## 📋 Logs attendus après correction

### Si tout fonctionne :
```
[Form1] 2 lecteur(s) CD détecté(s)
[Form1] === Vérification lecteur D: ===
[CDAudioManager] Lecture du lecteur D: via DeviceIoControl
[CDAudioManager] TOC lu: 17 pistes (#1 à #17)
[Form1] ✓ Lecteur D:: 17 pistes détectées
```

### Si toujours bloqué :
```
[CDAudioManager] Impossible d'ouvrir \\.\D:, erreur: 32
[CDAudioManager] Le lecteur est utilisé par un autre programme
```

---

## 🧪 Test alternatif : Outil Process Explorer

Si le problème persiste, utilisez **Process Explorer** (Microsoft Sysinternals) :

### Installation
1. Télécharger : https://docs.microsoft.com/sysinternals/downloads/process-explorer
2. Extraire et lancer `procexp.exe` (en **Administrateur**)

### Trouver le coupable
1. **Menu** → **Find** → **Find Handle or DLL...**
2. Chercher : `D:`
3. Regarder quels processus ont ouvert le lecteur
4. **Fermer ces processus**

---

## 🔬 Alternative : Tester avec un script PowerShell

Créez `test_cd_access.ps1` :

```powershell
# Tester l'accès direct aux lecteurs CD
$drives = Get-WmiObject Win32_CDROMDrive | Select-Object Drive

foreach ($drive in $drives) {
	$driveLetter = $drive.Drive
	Write-Host "Test du lecteur $driveLetter :"

	try {
		# Tenter d'ouvrir en lecture
		$stream = [System.IO.File]::Open("$driveLetter\", [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
		$stream.Close()
		Write-Host "  ✓ Accès OK" -ForegroundColor Green
	}
	catch {
		Write-Host "  ✗ Erreur: $($_.Exception.Message)" -ForegroundColor Red
	}
}
```

Lancez dans PowerShell **en Administrateur** :
```powershell
.\test_cd_access.ps1
```

---

## 🎯 Si rien ne fonctionne

### Solution de dernier recours : Droits administrateur

1. **Fermer AudioPlay**
2. Dans Visual Studio : **Projet** → **Propriétés**
3. **Manifest** → **UAC Execution Level** → Choisir `requireAdministrator`
4. **Recompiler et tester**

⚠️ **Attention** : Cela demandera l'élévation UAC à chaque lancement !

---

## 📊 Codes d'erreur Windows

| Code | Nom | Signification | Solution |
|------|-----|---------------|----------|
| **32** | `ERROR_SHARING_VIOLATION` | Fichier/device déjà ouvert | Fermer le programme qui utilise le lecteur |
| **21** | `ERROR_NOT_READY` | Pas de CD dans le lecteur | Insérer un CD |
| **5** | `ERROR_ACCESS_DENIED` | Accès refusé | Lancer en administrateur |
| **2** | `ERROR_FILE_NOT_FOUND` | Lecteur introuvable | Vérifier la lettre du lecteur |

---

## ✅ Checklist de débogage

- [ ] Fermer tous les programmes CD/DVD
- [ ] Vérifier dans le Gestionnaire des tâches
- [ ] Éjecter et réinsérer les CD
- [ ] Redémarrer AudioPlay
- [ ] Vérifier les logs dans Output
- [ ] (Si échec) Utiliser Process Explorer
- [ ] (Si échec) Tester le script PowerShell
- [ ] (Si échec) Lancer AudioPlay en administrateur

---

**Une fois que vous aurez fermé les programmes qui bloquent, AudioPlay devrait détecter les CD correctement !** 🎉
