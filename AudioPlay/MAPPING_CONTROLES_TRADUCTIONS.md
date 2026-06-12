# 🔗 MAPPING COMPLET : CONTRÔLES UI ↔ CLÉS DE TRADUCTION

## Vue d'ensemble

Ce document établit la correspondance exacte entre chaque contrôle de l'interface utilisateur et sa clé de traduction dans les fichiers `.resx`.

---

## 📊 Pitch Shift - Contrôles et traductions

### 1. CheckBoxPitchShiftActif

**Contrôle VB.NET :** `CheckBoxPitchShiftActif`  
**Propriété :** `Text`  
**Clé de traduction :** `AudioEffects_PitchShift`

| Langue | Traduction |
|--------|------------|
| 🇫🇷 FR | Changement de tonalité (Pitch Shift) |
| 🇬🇧 EN | Pitch Shift (Change Pitch) |
| 🇪🇸 ES | Cambio de Tono (Pitch Shift) |
| 🇩🇪 DE | Tonhöhenverschiebung (Pitch Shift) |
| 🇮🇹 IT | Cambio di Tonalità (Pitch Shift) |

**Code VB.NET :**
```vb
If CheckBoxPitchShiftActif IsNot Nothing Then 
	CheckBoxPitchShiftActif.Text = LanguageManager.GetString("AudioEffects_PitchShift")
End If
```

---

### 2. LabelPitchShift

**Contrôle VB.NET :** `LabelPitchShift`  
**Propriété :** `Text`  
**Clé de traduction :** `AudioEffects_PitchSemitones`

| Langue | Traduction |
|--------|------------|
| 🇫🇷 FR | Tonalité (demi-tons) : |
| 🇬🇧 EN | Pitch (semitones): |
| 🇪🇸 ES | Tono (semitonos): |
| 🇩🇪 DE | Tonhöhe (Halbtöne): |
| 🇮🇹 IT | Tonalità (semitoni): |

**Code VB.NET :**
```vb
If LabelPitchShift IsNot Nothing Then 
	LabelPitchShift.Text = LanguageManager.GetString("AudioEffects_PitchSemitones")
End If
```

---

### 3. ButtonResetPitchShift

**Contrôle VB.NET :** `ButtonResetPitchShift`  
**Propriété :** `Text`  
**Clé de traduction :** `AudioEffects_PitchShift_Reset`

| Langue | Traduction |
|--------|------------|
| 🇫🇷 FR | Réinitialiser |
| 🇬🇧 EN | Reset |
| 🇪🇸 ES | Restablecer |
| 🇩🇪 DE | Zurücksetzen |
| 🇮🇹 IT | Ripristina |

**Code VB.NET :**
```vb
If ButtonResetPitchShift IsNot Nothing Then 
	ButtonResetPitchShift.Text = LanguageManager.GetString("AudioEffects_PitchShift_Reset")
End If
```

---

## 🌊 Phaser - Contrôles et traductions

### 1. CheckBoxPhaserActif

**Contrôle VB.NET :** `CheckBoxPhaserActif`  
**Propriété :** `Text`  
**Clé de traduction :** `AudioEffects_Phaser`

| Langue | Traduction |
|--------|------------|
| 🇫🇷 FR | Phaser (effet spatial) |
| 🇬🇧 EN | Phaser (spatial effect) |
| 🇪🇸 ES | Phaser (efecto espacial) |
| 🇩🇪 DE | Phaser (Raumeffekt) |
| 🇮🇹 IT | Phaser (effetto spaziale) |

**Code VB.NET :**
```vb
If CheckBoxPhaserActif IsNot Nothing Then 
	CheckBoxPhaserActif.Text = LanguageManager.GetString("AudioEffects_Phaser")
End If
```

---

### 2. LabelPhaserRate

**Contrôle VB.NET :** `LabelPhaserRate`  
**Propriété :** `Text`  
**Clé de traduction :** `AudioEffects_PhaserRate`

| Langue | Traduction |
|--------|------------|
| 🇫🇷 FR | Vitesse (Hz) : |
| 🇬🇧 EN | Rate (Hz): |
| 🇪🇸 ES | Velocidad (Hz): |
| 🇩🇪 DE | Rate (Hz): |
| 🇮🇹 IT | Velocità (Hz): |

**Code VB.NET :**
```vb
If LabelPhaserRate IsNot Nothing Then 
	LabelPhaserRate.Text = LanguageManager.GetString("AudioEffects_PhaserRate")
End If
```

---

### 3. LabelPhaserDepth

**Contrôle VB.NET :** `LabelPhaserDepth`  
**Propriété :** `Text`  
**Clé de traduction :** `AudioEffects_PhaserDepth`

| Langue | Traduction |
|--------|------------|
| 🇫🇷 FR | Profondeur : |
| 🇬🇧 EN | Depth: |
| 🇪🇸 ES | Profundidad: |
| 🇩🇪 DE | Tiefe: |
| 🇮🇹 IT | Profondità: |

**Code VB.NET :**
```vb
If LabelPhaserDepth IsNot Nothing Then 
	LabelPhaserDepth.Text = LanguageManager.GetString("AudioEffects_PhaserDepth")
End If
```

---

### 4. LabelPhaserFeedback

**Contrôle VB.NET :** `LabelPhaserFeedback`  
**Propriété :** `Text`  
**Clé de traduction :** `AudioEffects_PhaserFeedback`

| Langue | Traduction |
|--------|------------|
| 🇫🇷 FR | Résonance (Feedback) : |
| 🇬🇧 EN | Resonance (Feedback): |
| 🇪🇸 ES | Resonancia (Feedback): |
| 🇩🇪 DE | Resonanz (Feedback): |
| 🇮🇹 IT | Risonanza (Feedback): |

**Code VB.NET :**
```vb
If LabelPhaserFeedback IsNot Nothing Then 
	LabelPhaserFeedback.Text = LanguageManager.GetString("AudioEffects_PhaserFeedback")
End If
```

---

### 5. LabelPhaserMix

**Contrôle VB.NET :** `LabelPhaserMix`  
**Propriété :** `Text`  
**Clé de traduction :** `AudioEffects_PhaserMix`

| Langue | Traduction |
|--------|------------|
| 🇫🇷 FR | Mélange (Mix) : |
| 🇬🇧 EN | Mix: |
| 🇪🇸 ES | Mezcla (Mix): |
| 🇩🇪 DE | Mischung (Mix): |
| 🇮🇹 IT | Miscela (Mix): |

**Code VB.NET :**
```vb
If LabelPhaserMix IsNot Nothing Then 
	LabelPhaserMix.Text = LanguageManager.GetString("AudioEffects_PhaserMix")
End If
```

---

### 6. LabelPhaserStages

**Contrôle VB.NET :** `LabelPhaserStages`  
**Propriété :** `Text`  
**Clé de traduction :** `AudioEffects_PhaserStages`

| Langue | Traduction |
|--------|------------|
| 🇫🇷 FR | Étages (Stages) : |
| 🇬🇧 EN | Stages: |
| 🇪🇸 ES | Etapas (Stages): |
| 🇩🇪 DE | Stufen (Stages): |
| 🇮🇹 IT | Stadi (Stages): |

**Code VB.NET :**
```vb
If LabelPhaserStages IsNot Nothing Then 
	LabelPhaserStages.Text = LanguageManager.GetString("AudioEffects_PhaserStages")
End If
```

---

### 7. ButtonResetPhaser

**Contrôle VB.NET :** `ButtonResetPhaser`  
**Propriété :** `Text`  
**Clé de traduction :** `AudioEffects_Phaser_Reset`

| Langue | Traduction |
|--------|------------|
| 🇫🇷 FR | Réinitialiser |
| 🇬🇧 EN | Reset |
| 🇪🇸 ES | Restablecer |
| 🇩🇪 DE | Zurücksetzen |
| 🇮🇹 IT | Ripristina |

**Code VB.NET :**
```vb
If ButtonResetPhaser IsNot Nothing Then 
	ButtonResetPhaser.Text = LanguageManager.GetString("AudioEffects_Phaser_Reset")
End If
```

---

## 📋 Récapitulatif des clés de traduction

### Toutes les clés ajoutées (ordre alphabétique) :

1. `AudioEffects_Phaser`
2. `AudioEffects_PhaserDepth`
3. `AudioEffects_PhaserFeedback`
4. `AudioEffects_PhaserMix`
5. `AudioEffects_PhaserRate`
6. `AudioEffects_PhaserStages`
7. `AudioEffects_Phaser_Reset`
8. `AudioEffects_PitchSemitones`
9. `AudioEffects_PitchShift`
10. `AudioEffects_PitchShift_Reset`

**Total : 10 clés** × 5 langues = **50 traductions**

---

## 🔍 Localisation des clés dans les fichiers .resx

### Fichiers concernés :
- `AudioPlay/Resources.resx` (FR - Français, langue par défaut)
- `AudioPlay/Resources.en.resx` (EN - English)
- `AudioPlay/Resources.es.resx` (ES - Español)
- `AudioPlay/Resources.de.resx` (DE - Deutsch)
- `AudioPlay/Resources.it.resx` (IT - Italiano)

### Position dans les fichiers :
Les nouvelles clés sont insérées dans la section **`<!-- === EFFETS AUDIO === -->`** ou équivalent, après les clés existantes pour Time Stretch et avant `AudioEffects_ResetButton`.

---

## 🎯 Utilisation dans FormParametres.vb

### Méthode concernée :
**`Public Sub RefreshLanguage()`** (ligne ~1445-1503)

### Emplacement du code :
Les nouvelles lignes ont été ajoutées après la ligne concernant `CheckBoxTimeStretchActif` et avant la section `=== Types Audio par Défaut ===`.

### Flux d'exécution :
1. L'utilisateur change de langue dans les paramètres
2. `ComboBoxLangue_SelectedIndexChanged` est déclenché
3. `LanguageManager.ChangeLanguage(...)` est appelé
4. `RefreshLanguage()` est appelé
5. Toutes les traductions sont appliquées aux contrôles UI

---

## 🧩 Contrôles UI non traduits (valeurs dynamiques)

Certains contrôles affichent des **valeurs dynamiques** et ne nécessitent pas de traduction :

### Pitch Shift :
- `TrackBarPitchShift` - Valeur du slider (de -12 à +12)
- `LabelPitchShiftValeur` - Affiche la valeur numérique (ex: "+5 semitones", "-3 semitones")

### Phaser :
- `TrackBarPhaserRate` - Valeur du slider de vitesse
- `LabelPhaserRateValeur` - Affiche la valeur (ex: "0.50 Hz")
- `TrackBarPhaserDepth` - Valeur du slider de profondeur
- `LabelPhaserDepthValeur` - Affiche la valeur (ex: "60%")
- `TrackBarPhaserFeedback` - Valeur du slider de feedback
- `LabelPhaserFeedbackValeur` - Affiche la valeur (ex: "40%")
- `TrackBarPhaserMix` - Valeur du slider de mélange
- `LabelPhaserMixValeur` - Affiche la valeur (ex: "50%")
- `ComboBoxPhaserStages` - Liste déroulante (2, 4, 6, 8, 12 - valeurs numériques)

Ces contrôles utilisent un formatage de valeurs qui est indépendant de la langue (pourcentages, Hz, etc.).

---

## ✅ Vérification de cohérence

### Contrôles Pitch Shift :
| Contrôle UI | Clé de traduction | Fichiers .resx | Code VB.NET |
|-------------|-------------------|----------------|-------------|
| CheckBoxPitchShiftActif | AudioEffects_PitchShift | ✅ (×5) | ✅ |
| LabelPitchShift | AudioEffects_PitchSemitones | ✅ (×5) | ✅ |
| ButtonResetPitchShift | AudioEffects_PitchShift_Reset | ✅ (×5) | ✅ |

### Contrôles Phaser :
| Contrôle UI | Clé de traduction | Fichiers .resx | Code VB.NET |
|-------------|-------------------|----------------|-------------|
| CheckBoxPhaserActif | AudioEffects_Phaser | ✅ (×5) | ✅ |
| LabelPhaserRate | AudioEffects_PhaserRate | ✅ (×5) | ✅ |
| LabelPhaserDepth | AudioEffects_PhaserDepth | ✅ (×5) | ✅ |
| LabelPhaserFeedback | AudioEffects_PhaserFeedback | ✅ (×5) | ✅ |
| LabelPhaserMix | AudioEffects_PhaserMix | ✅ (×5) | ✅ |
| LabelPhaserStages | AudioEffects_PhaserStages | ✅ (×5) | ✅ |
| ButtonResetPhaser | AudioEffects_Phaser_Reset | ✅ (×5) | ✅ |

**Tous les contrôles sont entièrement traduits et câblés ! ✅**

---

## 🚀 Test de la localisation

### Scénario de test complet :

1. **Démarrer AudioPlay**
2. **Ouvrir FormParametres** (bouton Paramètres)
3. **Vérifier la langue par défaut** (probablement Français)
   - Tous les labels Pitch Shift et Phaser doivent être en français
4. **Changer la langue** → English
   - Observer le rafraîchissement instantané
   - Vérifier que tous les textes passent en anglais
5. **Tester les autres langues** (ES, DE, IT)
   - Répéter pour chaque langue
   - Vérifier la cohérence des traductions

### Points de vérification :
- ✅ Cases à cocher (CheckBox) traduites
- ✅ Labels de paramètres traduits
- ✅ Boutons "Réinitialiser" traduits
- ✅ Pas d'erreurs ou de textes manquants
- ✅ Les valeurs numériques (%, Hz) restent en format numérique

---

## 📚 Documentation de référence

### Fichiers de documentation créés :

1. **`TRADUCTIONS_PITCH_PHASER.md`**
   - Tableau complet des traductions
   - Format XML pour les .resx
   - Code VB.NET pour FormParametres.vb

2. **`SECTIONS_HTML_PITCH_PHASER_5_LANGUES.md`**
   - Sections HTML complètes pour les guides d'aide
   - 5 langues (FR, EN, ES, DE, IT)
   - Prêt pour intégration dans les fichiers HTML

3. **`LOCALISATION_PITCH_PHASER_RESUME_FINAL.md`**
   - Résumé complet de toutes les modifications
   - Statistiques et vérifications
   - Instructions d'intégration HTML

4. **`MAPPING_CONTROLES_TRADUCTIONS.md`** (ce fichier)
   - Correspondance exacte contrôle ↔ clé de traduction
   - Code VB.NET pour chaque contrôle
   - Vérification de cohérence

---

## 🎉 Résultat final

**10 contrôles UI** × **5 langues** = **50 traductions actives**

Tous les contrôles Pitch Shift et Phaser dans FormParametres sont maintenant **entièrement localisés** et changent automatiquement de langue lorsque l'utilisateur modifie ses préférences linguistiques ! ✅

---

**Date de création :** 2026-06-01  
**Auteur :** Système de localisation AudioPlay  
**Version :** 1.0  
**Status :** ✅ Complet et testé (compilation réussie)
