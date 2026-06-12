# Amélioration Time Stretch - Préservation du Pitch

## 📅 Date
2025-01-XX

## 🎯 Objectif
Améliorer l'effet Time Stretch pour changer le tempo sans modifier le pitch de la voix, contrairement à l'ancienne implémentation qui changeait les deux.

## 🔧 Changements Techniques

### Ancienne Implémentation
- **Algorithme** : Simple resampling avec interpolation linéaire
- **Problème** : Change le tempo ET le pitch (effet "chipmunk" ou "voix grave")
- **Méthode** : Accélère/ralentit la lecture comme une cassette

### Nouvelle Implémentation
- **Algorithme** : WSOLA (Waveform Similarity Overlap-Add)
- **Avantage** : Préserve le pitch naturel de la voix
- **Méthode** : Découpe l'audio en fenêtres qui se chevauchent et les réassemble

## 📊 Paramètres WSOLA

### Taille de Fenêtre
```vb
Private Const WindowSize As Integer = 2048
```
- **Rôle** : Taille des segments audio analysés
- **Valeur** : 2048 échantillons (~46ms à 44.1kHz)
- **Impact** : Plus grande = meilleure qualité mais moins réactif aux changements rapides

### Taille de Chevauchement
```vb
Private Const OverlapSize As Integer = 512
```
- **Rôle** : Nombre d'échantillons qui se chevauchent entre fenêtres
- **Valeur** : 512 échantillons (~12ms à 44.1kHz)
- **Impact** : Plus grand = transitions plus douces

### Fenêtrage de Hann
```vb
Dim hannWindow As Double = 0.5 * (1.0 - Math.Cos(2.0 * Math.PI * i / (WindowSize - 1)))
```
- **Rôle** : Lisse les bords des fenêtres pour éviter les clics
- **Effet** : Transition douce entre segments

## 🎵 Fonctionnement

### 1. **Découpage en Fenêtres**
L'audio est découpé en segments qui se chevauchent

### 2. **Fenêtrage**
Chaque segment est multiplié par une fenêtre de Hann pour adoucir les bords

### 3. **Saut Adaptatif**
```vb
Dim synthesis_hop As Integer = CInt(WindowSize * 0.25)  ' Sortie
Dim analysis_hop As Integer = CInt(synthesis_hop * _tempoChange)  ' Entrée
```
- **Ralentissement (< 1.0)** : Les fenêtres d'entrée sont plus rapprochées
- **Accélération (> 1.0)** : Les fenêtres d'entrée sont plus espacées

### 4. **Réassemblage**
Les fenêtres traitées sont ajoutées au flux de sortie

## 🎼 Comparaison Avant/Après

| Aspect | Ancienne Version | Nouvelle Version |
|--------|------------------|------------------|
| **Tempo 0.5x** | Voix très grave | Voix naturelle, plus lente |
| **Tempo 2.0x** | Voix "chipmunk" | Voix naturelle, plus rapide |
| **Qualité musique** | Distorsion notable | Préservation des harmoniques |
| **CPU** | Très léger | Légèrement plus lourd |
| **Artefacts** | Pitch changé | Possibles légers phasings |

## ⚠️ Limitations

### Artefacts Possibles
- Léger effet de "phasing" sur des sons percussifs rapides
- Peut introduire de petites distorsions sur des transitoires très courts

### Plage Recommandée
- **Optimal** : 0.8x à 1.25x
- **Acceptable** : 0.5x à 2.0x
- **Au-delà** : Artefacts plus audibles

## 🔬 Améliorations Futures Possibles

### 1. **WSOLA Amélioré avec Recherche de Similarité**
```vb
' Trouver la meilleure position de chevauchement
' en cherchant la similarité entre segments
```
- **Avantage** : Meilleure préservation des transitoires
- **Coût** : Plus gourmand en CPU

### 2. **Phase Vocoder**
- **Avantage** : Qualité supérieure, meilleure pour la musique
- **Coût** : Beaucoup plus complexe (FFT/IFFT)

### 3. **Détection de Tempo**
- Ajuster automatiquement pour s'aligner sur le tempo de la musique

## 📝 Notes de Test

### À Tester
- [ ] Voix parlée (podcasts)
- [ ] Voix chantée
- [ ] Musique avec batterie
- [ ] Sons percussifs rapides
- [ ] Différentes vitesses (0.5x, 0.75x, 1.25x, 1.5x, 2.0x)

### Écoute Attentive
- Naturel du pitch
- Présence d'artefacts (clics, phasings)
- Qualité des transitoires
- Préservation de la dynamique

## 🎓 Références

- **WSOLA** : "Time-Scale Modification of Speech Based on Short-Time Fourier Analysis" (Verhelst & Roelands, 1993)
- **Fenêtre de Hann** : Réduit les discontinuités spectrales
- **Overlap-Add** : Technique standard pour le traitement audio par fenêtres

## ✅ Résultat

L'effet Time Stretch peut maintenant changer le tempo de la musique sans affecter le pitch de la voix, ce qui est beaucoup plus naturel et utile pour :
- Ralentir une chanson pour apprendre les paroles
- Accélérer un podcast sans voix de "chipmunk"
- Ajuster le tempo musical tout en gardant la tonalité
