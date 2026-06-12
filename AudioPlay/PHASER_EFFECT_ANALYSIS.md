# 🌀 Phaser Effect - Analyse et Plan d'Implémentation

## 🎯 Qu'est-ce qu'un Phaser ?

Le **Phaser** est un effet audio qui crée un son "spatial" et "tournant" en :
1. Dupliquant le signal audio
2. Appliquant des filtres **all-pass** avec décalage de phase variable
3. Mélangeant le signal traité avec l'original

**Résultat** : Son caractéristique des années 70-80, utilisé sur guitares, synthés, et voix.

---

## 🎵 Exemples célèbres

- **Van Halen** - "Eruption" (guitare)
- **Pink Floyd** - "Breathe" (synthé)
- **The Police** - "Message in a Bottle" (guitare)
- **Jean-Michel Jarre** - "Oxygène" (synthé)

---

## 🔧 Architecture Technique

### Composants du Phaser

```
Input Signal
	↓
	├─→ [Direct Path] ────────────┐
	│                              ↓
	└─→ [All-Pass Filters] → [LFO] → [Mix] → Output
		 (4 à 12 stages)
```

### Paramètres Typiques

| Paramètre | Description | Plage | Défaut |
|-----------|-------------|-------|--------|
| **Rate (Speed)** | Vitesse de balayage du LFO | 0.1 - 10 Hz | 0.5 Hz |
| **Depth** | Intensité de la modulation | 0 - 100% | 50% |
| **Feedback** | Quantité de signal réinjecté | 0 - 95% | 30% |
| **Stages** | Nombre de filtres all-pass | 2, 4, 6, 8, 12 | 4 |
| **Mix** | Wet/Dry | 0 - 100% | 50% |
| **Center Frequency** | Fréquence centrale | 200 - 5000 Hz | 1000 Hz |

---

## 📋 Plan d'Implémentation

### Option A : **Implémentation VB.NET Managée** (Recommandé)

#### ✅ Avantages
- Cohérent avec vos Reverb/Echo actuels
- Contrôle total sur les paramètres
- Facile à déboguer et modifier
- Pas de dépendance externe

#### Fichiers à créer

1. **PhaserSampleProvider.vb**
```vb
Public Class PhaserSampleProvider
	Implements ISampleProvider

	' Filtres all-pass (4-12 stages)
	Private allPassFilters As List(Of AllPassFilter)

	' LFO (Low Frequency Oscillator) pour modulation
	Private lfoPhase As Single = 0.0F
	Private lfoIncrement As Single

	' Paramètres
	Public Property Enabled As Boolean = False
	Public Property Rate As Single = 0.5F      ' Hz
	Public Property Depth As Single = 0.5F     ' 0.0 - 1.0
	Public Property Feedback As Single = 0.3F  ' 0.0 - 0.95
	Public Property Mix As Single = 0.5F       ' 0.0 - 1.0
	Public Property Stages As Integer = 4      ' 2, 4, 6, 8, 12

	' ...
End Class
```

2. **AllPassFilter.vb** (classe helper)
```vb
Public Class AllPassFilter
	Private buffer As Single = 0.0F
	Private a1 As Single ' Coefficient all-pass

	Public Sub New(centerFreq As Single, sampleRate As Integer)
		' Calculer coefficient basé sur fréquence
	End Sub

	Public Function Process(input As Single) As Single
		' Appliquer filtre all-pass
	End Function
End Class
```

#### Algorithme Principal

```vb
Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer
	Dim samplesRead = sourceProvider.Read(buffer, offset, count)

	If Not Enabled Then Return samplesRead

	For i = offset To offset + samplesRead - 1
		Dim input = buffer(i)

		' 1. LFO (oscillateur sinusoïdal)
		Dim lfo = Math.Sin(lfoPhase * 2.0 * Math.PI)
		lfoPhase += lfoIncrement
		If lfoPhase >= 1.0F Then lfoPhase -= 1.0F

		' 2. Moduler fréquence des filtres all-pass
		Dim modFreq = centerFrequency * (1.0 + Depth * lfo)

		' 3. Appliquer cascade de filtres all-pass
		Dim filtered = input
		For Each filter In allPassFilters
			filter.SetFrequency(modFreq)
			filtered = filter.Process(filtered)
		Next

		' 4. Feedback
		filtered = filtered + (feedbackSample * Feedback)
		feedbackSample = filtered

		' 5. Mix wet/dry
		buffer(i) = input * (1.0F - Mix) + filtered * Mix
	Next

	Return samplesRead
End Function
```

---

### Option B : **Librairie Native** (Si qualité studio requise)

#### Bass.NET
- Effet `BASS_FX_BFX_PHASER` intégré
- Qualité professionnelle
- Coût : Gratuit (non-commercial) / $99+ (commercial)

#### OpenAL EFX
- Effet phaser disponible
- Gratuit, open source
- Complexité élevée (P/Invoke)

---

## 🎨 Interface Utilisateur Proposée

### Dans FormParametres.Designer.vb

```vb
' Groupe Phaser (après Pitch Shift)
CheckBoxPhaserActif
LabelPhaserRate : "Vitesse (Hz):"
TrackBarPhaserRate : 1-100 (divisé par 10 = 0.1 - 10.0 Hz)
LabelPhaserRateValeur : "0.5"

LabelPhaserDepth : "Profondeur:"
TrackBarPhaserDepth : 0-100 (%)
LabelPhaserDepthValeur : "50%"

LabelPhaserFeedback : "Feedback:"
TrackBarPhaserFeedback : 0-95 (%)
LabelPhaserFeedbackValeur : "30%"

LabelPhaserMix : "Mix:"
TrackBarPhaserMix : 0-100 (%)
LabelPhaserMixValeur : "50%"

ComboBoxPhaserStages : "2 stages", "4 stages", "6 stages", "8 stages", "12 stages"

ButtonResetPhaser
```

### Position dans GroupBoxEffetsAudio
```
[Reverb]
[Echo]
[Time Stretch]
[Pitch Shift]
[Phaser] ← NOUVEAU
```

### Nouvelle taille GroupBox
- Actuelle : 570
- Nouvelle : **750** (ajouter ~180 pour les 5 contrôles Phaser)

---

## 📊 Ordre de la Chaîne d'Effets

```
Audio Source
  ↓
Equalizer
  ↓
Time Stretch
  ↓
Pitch Shift
  ↓
Phaser ← NOUVEAU (avant reverb/echo pour plus de naturel)
  ↓
Reverb
  ↓
Echo
  ↓
Volume
```

**Pourquoi avant Reverb/Echo ?**
- Le phaser modifie le timbre du signal
- Reverb/Echo ajoutent l'espace
- Ordre logique : Timbre → Espace → Volume

---

## ⚡ Complexité d'Implémentation

### Estimation

| Tâche | Temps | Difficulté |
|-------|-------|------------|
| **AllPassFilter.vb** | 30 min | ⭐⭐ Moyenne |
| **PhaserSampleProvider.vb** | 1h | ⭐⭐⭐ Moyenne+ |
| **UI (Designer)** | 30 min | ⭐ Facile |
| **Handlers (FormParametres)** | 30 min | ⭐ Facile |
| **Intégration (Form1)** | 15 min | ⭐ Facile |
| **Tests/Ajustements** | 30 min | ⭐⭐ Moyenne |
| **Total** | ~3h | ⭐⭐⭐ Moyenne |

**Plus simple que Time Stretch/Pitch Shift** car :
- Pas besoin de SoundTouch
- Pas de gestion de buffers complexes
- Pattern similaire à Reverb/Echo existants

---

## 🎯 Recommandation

### ✅ **Implémentation VB.NET Managée**

**Pourquoi ?**
1. Cohérent avec votre architecture actuelle
2. Reverb et Echo fonctionnent déjà très bien en managed
3. Contrôle total sur les paramètres
4. Pas de dépendance externe
5. Facilité de maintenance

**Difficulté** : Moyenne (similaire à vos effets existants)

---

## 📝 Prochaines Étapes Proposées

Si vous voulez implémenter le Phaser, je peux vous aider à :

1. **Créer AllPassFilter.vb** (filtre all-pass de base)
2. **Créer PhaserSampleProvider.vb** (effet complet)
3. **Ajouter les contrôles UI** (Designer)
4. **Ajouter les handlers** (FormParametres.vb)
5. **Intégrer dans Form1.vb** (chaîne d'effets)
6. **Ajouter persistance** (ParametresGlobaux + save/load)

---

## 🎵 Résultat Attendu

Un effet phaser de qualité qui donnera un son :
- **Spatial** et **tournant** (type synthé années 80)
- **Contrôlable** en temps réel
- **Combinable** avec les autres effets (reverb, echo, etc.)

---

**Voulez-vous que je commence l'implémentation du Phaser ?** 🌀

Je commencerais par créer le filtre all-pass de base, puis le provider complet avec LFO, et enfin l'intégration UI/handlers.
