# 🎚️ Architecture Son_Ajustement_DJ.txt

## 📋 Vue d'ensemble

**Fichier dédié** pour la persistance des ajustements DJ (volumes platines, crossfader, pitch).

Similaire à `Son_Ajustement.txt` (mode simple), mais pour le **mode DJ**.

---

## 📁 Structure des fichiers

```
%AppData%\AudioPlay\
├── parametres.txt              → Paramètres applicatifs
├── Son_Ajustement.txt          → Volume/Basses/Aigues (mode simple)
└── Son_Ajustement_DJ.txt       → Ajustements DJ (mode DJ) ← NOUVEAU
	├── VolumeDeckA=75
	├── VolumeDeckB=75
	├── Crossfader=50
	├── PitchDeckA=100
	└── PitchDeckB=100
```

---

## 🎯 Objectif

**Problème** : Les ajustements DJ (volumes, crossfader, pitch) étaient **réinitialisés à chaque lancement**.

**Solution** : Fichier dédié `Son_Ajustement_DJ.txt` pour **persister ces valeurs**.

---

## 🔧 Implémentation

### 📝 FormDJ.vb - Nouvelles méthodes

#### 1️⃣ `SauvegarderAjustementsDJ()`

```vb
Private Sub SauvegarderAjustementsDJ()
	' Sauvegarde dans %AppData%\AudioPlay\Son_Ajustement_DJ.txt
	' Format: 5 lignes (valeurs brutes des TrackBar)
	' - VolumeDeckA=75
	' - VolumeDeckB=75
	' - Crossfader=50
	' - PitchDeckA=100
	' - PitchDeckB=100
End Sub
```

**Appelé depuis** :
- `TrackBarVolumeDeckA_Scroll()`
- `TrackBarVolumeDeckB_Scroll()`
- `TrackBarCrossfader_Scroll()`
- `TrackBarPitchDeckA_Scroll()`
- `TrackBarPitchDeckB_Scroll()`

**Comportement** :
- ✅ Sauvegarde **immédiate** à chaque changement de TrackBar
- ✅ Silencieux en cas d'erreur (ne bloque pas l'utilisateur)
- ✅ Crée le répertoire `%AppData%\AudioPlay\` si absent

---

#### 2️⃣ `ChargerAjustementsDJ()`

```vb
Private Sub ChargerAjustementsDJ()
	' Charge depuis %AppData%\AudioPlay\Son_Ajustement_DJ.txt
	' Si fichier absent : crée avec valeurs par défaut
	' Validation stricte des valeurs (ranges)
End Sub
```

**Appelé depuis** :
- `FormDJ_Load()` (après initialisation des valeurs par défaut)

**Comportement** :
- ✅ Si fichier **existe** : charge les valeurs
- ✅ Si fichier **absent** : crée avec valeurs par défaut (75, 75, 50, 100, 100)
- ✅ **Validation stricte** des ranges :
  - `VolumeDeckA` : 0-100
  - `VolumeDeckB` : 0-100
  - `Crossfader` : 0-100
  - `PitchDeckA` : 92-108 (±8%)
  - `PitchDeckB` : 92-108 (±8%)
- ✅ Silencieux en cas d'erreur (garde les valeurs par défaut)

---

### 🔄 Flux de démarrage

```
FormDJ_Load()
	↓
1. Initialiser valeurs par défaut
	TrackBarVolumeDeckA.Value = 75
	TrackBarVolumeDeckB.Value = 75
	TrackBarCrossfader.Value = 50
	TrackBarPitchDeckA.Value = 100
	TrackBarPitchDeckB.Value = 100
	↓
2. ChargerAjustementsDJ()
	↓
	Fichier existe ?
	├─ OUI → Charger valeurs (écrase les défauts)
	└─ NON → SauvegarderAjustementsDJ() (créer avec défauts)
	↓
3. RefreshLanguage() (mettre à jour labels)
```

---

### 🎚️ Flux de sauvegarde

```
Utilisateur bouge TrackBar
	↓
TrackBar_Scroll()
	↓
1. Mettre à jour label/valeur interne
2. Appliquer au provider audio (temps réel)
3. SauvegarderAjustementsDJ() ← Immédiat !
	↓
Son_Ajustement_DJ.txt mis à jour (5 lignes)
```

---

## 📊 Valeurs par défaut

| Paramètre | Valeur par défaut | Range | Description |
|-----------|-------------------|-------|-------------|
| **VolumeDeckA** | 75 | 0-100 | Volume platine A (75%) |
| **VolumeDeckB** | 75 | 0-100 | Volume platine B (75%) |
| **Crossfader** | 50 | 0-100 | Position crossfader (centre) |
| **PitchDeckA** | 100 | 92-108 | Pitch platine A (0% = 100) |
| **PitchDeckB** | 100 | 92-108 | Pitch platine B (0% = 100) |

---

## ✅ Avantages

### 1. **Séparation des préoccupations**
- ✅ `parametres.txt` : Paramètres applicatifs
- ✅ `Son_Ajustement.txt` : Ajustements audio mode simple
- ✅ `Son_Ajustement_DJ.txt` : Ajustements DJ mode DJ

### 2. **Persistance automatique**
- ✅ L'utilisateur retrouve ses réglages **à chaque lancement**
- ✅ Pas besoin de reconfigurer les volumes/pitch

### 3. **Robustesse**
- ✅ Validation stricte des ranges
- ✅ Valeurs par défaut si fichier corrompu
- ✅ Création automatique si fichier absent

### 4. **Performance**
- ✅ Fichier léger (5 lignes)
- ✅ Sauvegarde rapide (< 1ms)
- ✅ Pas d'impact sur la lecture audio

### 5. **Cohérence avec mode simple**
- ✅ Même logique que `Son_Ajustement.txt`
- ✅ Architecture uniforme

---

## 🔍 Détails techniques

### Format du fichier

```
VolumeDeckA=75
VolumeDeckB=75
Crossfader=50
PitchDeckA=100
PitchDeckB=100
```

**Encodage** : UTF-8  
**Ordre** : Non critique (lecture par clé=valeur)  
**Taille** : ~100 octets

---

### Gestion des erreurs

| Erreur | Comportement |
|--------|-------------|
| **Fichier absent** | Créer avec valeurs par défaut |
| **Fichier corrompu** | Ignorer, garder valeurs par défaut |
| **Valeur hors range** | Ignorer, garder valeur par défaut |
| **Erreur I/O** | Silencieux (Debug.WriteLine) |

**Principe** : **Ne jamais bloquer l'utilisateur**. En cas d'erreur, utiliser les valeurs par défaut et continuer.

---

### Validation des ranges

```vb
' VolumeDeckA/B
If Integer.TryParse(valeur, vol) AndAlso vol >= 0 AndAlso vol <= 100 Then
	TrackBarVolumeDeckA.Value = vol
End If

' Crossfader
If Integer.TryParse(valeur, cf) AndAlso cf >= 0 AndAlso cf <= 100 Then
	TrackBarCrossfader.Value = cf
	crossfaderPosition = cf / 100.0F
End If

' PitchDeckA/B
If Integer.TryParse(valeur, pitch) AndAlso pitch >= 92 AndAlso pitch <= 108 Then
	TrackBarPitchDeckA.Value = pitch
	pitchDeckA = (pitch - 100) / 100.0F
End If
```

**Strict** : Toute valeur hors range est **ignorée**.

---

## 🧪 Tests à effectuer

### Test 1 : Premier lancement
1. ✅ Supprimer `%AppData%\AudioPlay\Son_Ajustement_DJ.txt`
2. ✅ Lancer AudioPlay en mode DJ
3. ✅ Vérifier que le fichier est **créé** avec valeurs par défaut
4. ✅ Vérifier contenu : `VolumeDeckA=75`, `VolumeDeckB=75`, etc.

### Test 2 : Persistance
1. ✅ Ajuster VolumeDeckA à 50
2. ✅ Ajuster Crossfader à 70
3. ✅ Ajuster PitchDeckA à 104 (+4%)
4. ✅ Fermer AudioPlay
5. ✅ Rouvrir AudioPlay en mode DJ
6. ✅ Vérifier que les valeurs sont **restaurées**

### Test 3 : Validation stricte
1. ✅ Modifier manuellement le fichier avec valeur invalide : `VolumeDeckA=150`
2. ✅ Lancer AudioPlay en mode DJ
3. ✅ Vérifier que `VolumeDeckA` est **75** (valeur par défaut)

### Test 4 : Fichier corrompu
1. ✅ Créer fichier avec contenu invalide : `asjkdhakjsdh`
2. ✅ Lancer AudioPlay en mode DJ
3. ✅ Vérifier que les valeurs par défaut sont **utilisées**

### Test 5 : Sauvegarde temps réel
1. ✅ Lancer AudioPlay en mode DJ
2. ✅ Bouger TrackBarVolumeDeckA
3. ✅ Ouvrir `Son_Ajustement_DJ.txt` (avec notepad)
4. ✅ Vérifier que la valeur est **mise à jour immédiatement**

---

## 📚 Comparaison avec mode simple

| Aspect | Mode Simple | Mode DJ |
|--------|-------------|---------|
| **Fichier** | `Son_Ajustement.txt` | `Son_Ajustement_DJ.txt` |
| **Paramètres** | Volume, Basses, Aigues | VolumeDeckA/B, Crossfader, Pitch |
| **Nombre de lignes** | 3 | 5 |
| **Logique** | Identique | Identique |
| **Validation** | Stricte | Stricte |
| **Sauvegarde** | Immédiate | Immédiate |

**Cohérence** : ✅ Même architecture, même logique, même robustesse.

---

## 🎊 Résultat

**Avant** :
- ❌ Volumes/pitch réinitialisés à chaque lancement
- ❌ Utilisateur doit reconfigurer à chaque fois

**Après** :
- ✅ Volumes/pitch **persistés** automatiquement
- ✅ L'utilisateur retrouve ses réglages
- ✅ Expérience DJ **professionnelle**

---

## 🔗 Liens

- `Son_Ajustement.txt` : Mode simple (Volume/Basses/Aigues)
- `NOUVELLE_ARCHITECTURE_SON_AJUSTEMENT.md` : Architecture mode simple
- `FormDJ.vb` : Code source mode DJ

---

**Date** : 2025-01-XX  
**Auteur** : GitHub Copilot  
**Inspiration** : Excellente idée de l'utilisateur pour `Son_Ajustement.txt` 🎉

---

**FIN DE LA DOCUMENTATION** 📖
