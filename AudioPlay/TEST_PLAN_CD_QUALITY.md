# ✅ Test Plan - Système de Qualité Dynamique CD

## 🎯 Objectif
Valider que les options de qualité changent correctement selon le format d'extraction sélectionné.

---

## 📋 Tests à effectuer

### Test 1 : Initialisation par défaut
**Étapes** :
1. Ouvrir AudioPlay
2. Insérer un CD audio
3. Ouvrir "Fichier" → "Lecteur CD Audio"
4. Cliquer sur le bouton "Extraire"

**Résultat attendu** :
- ✅ `ComboBoxTypeConversion` affiche **"MP3"** par défaut
- ✅ `ComboBoxQualiteConversion` affiche **"Très haute (320 kbps)"** par défaut
- ✅ Les autres options MP3 sont visibles : Basse, Moyenne, Haute

---

### Test 2 : Changement vers FLAC
**Étapes** :
1. Dans le formulaire d'extraction, sélectionner **"Flac"** dans `ComboBoxTypeConversion`

**Résultat attendu** :
- ✅ `ComboBoxQualiteConversion` se vide et affiche maintenant :
  - Niveau 0 (rapide)
  - Niveau 5 (équilibré)
  - **Niveau 8 (meilleur)** ← sélectionné par défaut
- ✅ Les anciennes options MP3 ont disparu

---

### Test 3 : Changement vers WAV
**Étapes** :
1. Dans le formulaire d'extraction, sélectionner **"Wav"** dans `ComboBoxTypeConversion`

**Résultat attendu** :
- ✅ `ComboBoxQualiteConversion` se vide et affiche maintenant :
  - PCM 16-bit 44.1 kHz
  - **PCM 24-bit 96 kHz** ← sélectionné par défaut
  - PCM 32-bit 192 kHz
- ✅ Les options FLAC ont disparu

---

### Test 4 : Changement vers WMA
**Étapes** :
1. Dans le formulaire d'extraction, sélectionner **"Wma"** dans `ComboBoxTypeConversion`

**Résultat attendu** :
- ✅ `ComboBoxQualiteConversion` se vide et affiche maintenant :
  - 128 kbps
  - 192 kbps
  - **256 kbps** ← sélectionné par défaut
- ✅ Les options WAV ont disparu

---

### Test 5 : Retour vers MP3
**Étapes** :
1. Dans le formulaire d'extraction, resélectionner **"MP3"** dans `ComboBoxTypeConversion`

**Résultat attendu** :
- ✅ `ComboBoxQualiteConversion` se vide et réaffiche les options MP3 :
  - Basse (128 kbps)
  - Moyenne (192 kbps)
  - Haute (256 kbps)
  - **Très haute (320 kbps)** ← sélectionné par défaut
- ✅ Le système revient correctement à l'état initial

---

### Test 6 : Calcul des tailles
**Étapes** :
1. Sélectionner **MP3** → **Très haute (320 kbps)**
2. Observer la colonne "Taille compressée" dans le `ListViewCompress`
3. Changer vers **FLAC** → **Niveau 8 (meilleur)**
4. Observer la colonne "Taille compressée"
5. Changer vers **WAV** → **PCM 24-bit 96 kHz**
6. Observer la colonne "Taille compressée"

**Résultat attendu** :
- ✅ Les tailles se recalculent automatiquement à chaque changement
- ✅ MP3 320 kbps : ~2.4 MB/min
- ✅ FLAC Niveau 8 : ~5 MB/min (environ 50% du WAV)
- ✅ WAV 24-bit 96 kHz : ~34 MB/min (beaucoup plus gros)

---

### Test 7 : Extraction MP3 Très haute qualité
**Étapes** :
1. Sélectionner **MP3** et **Très haute (320 kbps)**
2. Cocher une ou plusieurs pistes
3. Cliquer sur **"Extraire"**
4. Attendre la fin de l'extraction
5. Vérifier les propriétés du fichier MP3 généré

**Résultat attendu** :
- ✅ Extraction réussie sans erreur
- ✅ Le fichier MP3 a un bitrate de **320 kbps** (vérifier avec un outil comme VLC ou MediaInfo)
- ✅ Les métadonnées sont correctement écrites

---

### Test 8 : Extraction FLAC Niveau 8
**Étapes** :
1. Sélectionner **FLAC** et **Niveau 8 (meilleur)**
2. Cocher une piste
3. Cliquer sur **"Extraire"**
4. Si FFMpeg n'est pas installé, accepter le téléchargement automatique
5. Attendre la fin de l'extraction
6. Vérifier les propriétés du fichier FLAC généré

**Résultat attendu** :
- ✅ Si FFMpeg absent : fenêtre de téléchargement s'affiche
- ✅ Téléchargement et installation automatiques réussis
- ✅ Extraction réussie sans erreur
- ✅ Le fichier FLAC est compressé avec niveau 8 (taille ~50% du WAV)
- ✅ La qualité est lossless (vérifier avec un outil)

---

### Test 9 : Extraction WAV 24-bit 96 kHz
**Étapes** :
1. Sélectionner **WAV** et **PCM 24-bit 96 kHz**
2. Cocher une piste courte (2-3 minutes)
3. Cliquer sur **"Extraire"**
4. Attendre la fin de l'extraction
5. Vérifier les propriétés du fichier WAV généré

**Résultat attendu** :
- ✅ Extraction réussie sans erreur
- ✅ Le fichier WAV a les caractéristiques : **24-bit, 96 kHz, stéréo** (vérifier avec VLC ou MediaInfo)
- ✅ La taille du fichier est environ **34 MB par minute**
- ✅ Les métadonnées sont écrites (si supportées)

---

### Test 10 : Extraction WMA 256 kbps
**Étapes** :
1. Sélectionner **WMA** et **256 kbps**
2. Cocher une piste
3. Cliquer sur **"Extraire"**
4. Si FFMpeg n'est pas installé, accepter le téléchargement automatique
5. Attendre la fin de l'extraction
6. Vérifier les propriétés du fichier WMA généré

**Résultat attendu** :
- ✅ Si FFMpeg absent : fenêtre de téléchargement s'affiche (si pas déjà téléchargé pour FLAC)
- ✅ Extraction réussie sans erreur
- ✅ Le fichier WMA a un bitrate de **256 kbps** (vérifier avec VLC ou MediaInfo)
- ✅ Les métadonnées sont correctement écrites

---

### Test 11 : Changement de qualité après sélection
**Étapes** :
1. Sélectionner **MP3** → **Basse (128 kbps)**
2. Cocher des pistes
3. Extraire
4. Vérifier le fichier généré

**Résultat attendu** :
- ✅ Le fichier MP3 est encodé en **128 kbps** (et non 320 kbps)
- ✅ La sélection de qualité est bien prise en compte

---

### Test 12 : Persistence des choix
**Étapes** :
1. Sélectionner **FLAC** → **Niveau 5 (équilibré)**
2. Fermer le formulaire d'extraction (bouton Quitter)
3. Rouvrir le formulaire d'extraction

**Résultat attendu** :
- ✅ Le formulaire revient aux valeurs par défaut : **MP3** et **Très haute (320 kbps)**
- ⚠️ (Optionnel) Si vous voulez que les choix persistent, il faudrait sauvegarder les préférences dans les paramètres

---

## 🐛 Bugs potentiels à surveiller

### ❌ ComboBox vide
**Symptôme** : `ComboBoxQualiteConversion` reste vide après sélection d'un format
**Cause possible** : Le gestionnaire d'événement `ComboBoxTypeConversion_SelectedIndexChanged` ne se déclenche pas
**Solution** : Vérifier que le handler est bien attaché dans `FormCompresser_Load`

### ❌ Mauvaise qualité encodée
**Symptôme** : Le fichier généré n'a pas le bitrate/qualité attendu
**Cause possible** : Le code d'extraction ne lit pas correctement la valeur de `ComboBoxQualiteConversion`
**Solution** : Vérifier les `Select Case` dans `ExtraireMp3`, `ExtraireFlac`, `ExtraireWav`, `ExtraireWma`

### ❌ Crash lors du changement de format
**Symptôme** : Exception lors du changement de format
**Cause possible** : Accès à un contrôle depuis un mauvais thread
**Solution** : Vérifier que les modifications de `ComboBoxQualiteConversion` se font sur le thread UI

### ❌ Tailles estimées incorrectes
**Symptôme** : La colonne "Taille compressée" affiche des valeurs absurdes
**Cause possible** : La fonction `CalculerTailleCompressee` ne reconnaît pas les nouveaux libellés
**Solution** : Vérifier les `Select Case` dans `CalculerTailleCompressee`

---

## ✅ Critères de réussite

Le système est considéré comme validé si :

1. ✅ Tous les 12 tests passent sans erreur
2. ✅ Aucun crash ou exception pendant les changements de format
3. ✅ Les fichiers extraits ont bien les caractéristiques attendues (bitrate, sample rate, bit depth)
4. ✅ Les métadonnées sont correctement écrites dans tous les formats
5. ✅ FFMpeg se télécharge et s'installe automatiquement quand nécessaire
6. ✅ Les tailles estimées sont cohérentes avec les fichiers réels

---

## 📊 Rapport de test

À compléter après exécution :

| Test | Statut | Notes |
|------|--------|-------|
| Test 1 : Initialisation | ⬜ À tester | |
| Test 2 : Changement FLAC | ⬜ À tester | |
| Test 3 : Changement WAV | ⬜ À tester | |
| Test 4 : Changement WMA | ⬜ À tester | |
| Test 5 : Retour MP3 | ⬜ À tester | |
| Test 6 : Calcul tailles | ⬜ À tester | |
| Test 7 : Extraction MP3 | ⬜ À tester | |
| Test 8 : Extraction FLAC | ⬜ À tester | |
| Test 9 : Extraction WAV | ⬜ À tester | |
| Test 10 : Extraction WMA | ⬜ À tester | |
| Test 11 : Changement qualité | ⬜ À tester | |
| Test 12 : Persistence | ⬜ À tester | |

**Légende** :
- ✅ : Réussi
- ❌ : Échec
- ⚠️ : Réussi avec remarques
- ⬜ : Non testé

---

**Date du test** : _______________

**Testeur** : _______________

**Version AudioPlay** : _______________

**Notes additionnelles** :
_______________________________________________________________
_______________________________________________________________
_______________________________________________________________
