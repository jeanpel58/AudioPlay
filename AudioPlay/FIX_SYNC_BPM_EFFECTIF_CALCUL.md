# FIX SYNC - CALCUL BPM EFFECTIF AVEC PITCH

## PROBLÈME IDENTIFIÉ

**Symptôme** : Lorsque la platine A joue à 119.750 BPM et la platine B à 117.550 BPM, cliquer Sync B devrait aligner B sur A. Mais un décalage commence, et **recliquer Sync B empire le décalage** au lieu de le corriger !

**Cause racine** : Mauvais calcul du BPM effectif lors de la création des grilles temporaires pour le beat snap.

### Exemple concret

```
Platine A : BPM original = 119.750, Pitch = +2% → BPM effectif = 122.145
Platine B : BPM original = 117.550, Pitch = 0%   → BPM effectif = 117.550
```

**Code AVANT (ligne 579-580 pour Deck B)** :
```vb
Dim tempBeatGridA As New BeatGrid(bpmDeckA, ...)  ' 119.750 ← FAUX !
Dim tempBeatGridB As New BeatGrid(bpmAjuste, ...) ' 119.750 ← correct
```

Le problème : `bpmDeckA` est le **BPM d'origine détecté**, pas le **BPM actuel avec pitch appliqué** !

Si Deck A a un pitch de +2%, son BPM réel est **122.145**, pas 119.750.

**Conséquence** : Le calcul de phase de Deck A était faux, donc le "snap" alignait Deck B sur un mauvais beat. À chaque re-clic, l'erreur s'accumulait.

---

## SOLUTION APPLIQUÉE

**Code APRÈS** :
```vb
' CORRECTION CRITIQUE : Utiliser les BPM EFFECTIFS ACTUELS avec pitch appliqué !
Dim bpmEffectifA As Double = bpmDeckA * (1.0 + pitchDeckA)
Dim bpmEffectifB As Double = bpmAjuste ' Déjà ajusté avec le nouveau pitch
Dim tempBeatGridA As New BeatGrid(bpmEffectifA, fichierAudioDeckA.TotalTime.TotalSeconds)
Dim tempBeatGridB As New BeatGrid(bpmEffectifB, fichierAudioDeckB.TotalTime.TotalSeconds)
```

Maintenant, les grilles temporaires utilisent les **BPM réels actuels** des deux platines, garantissant un calcul de phase correct.

---

## FICHIERS MODIFIÉS

### `FormDJ.vb`

**ButtonSyncDeckB_Click (lignes ~575-583)** :
- Ajout de `bpmEffectifA = bpmDeckA * (1.0 + pitchDeckA)`
- Utilisation de `bpmEffectifA` au lieu de `bpmDeckA`

**ButtonSyncDeckA_Click (lignes ~453-461)** :
- Ajout de `bpmEffectifB = bpmDeckB * (1.0 + pitchDeckB)`
- Utilisation de `bpmEffectifB` au lieu de `bpmDeckB`

---

## VALIDATION

✅ **Compilation réussie**  
✅ **Correction symétrique appliquée aux deux boutons Sync**  
✅ **Les re-clics Sync devraient maintenant réaligner correctement au lieu d'empirer le décalage**

---

## TEST RECOMMANDÉ

1. Charger une piste à 119.750 BPM sur Deck A
2. Charger une piste à 117.550 BPM sur Deck B
3. Ajuster le pitch de Deck A à +2% (BPM effectif = ~122.145)
4. Lancer Deck A
5. Cliquer Sync B → Deck B devrait s'aligner sur le BPM effectif de A (122.145)
6. Lancer Deck B → Les beats devraient rester synchronisés
7. Si un décalage apparaît, recliquer Sync B → Le décalage devrait SE CORRIGER, pas empirer !

---

**Date** : 2025-01-24  
**Correction appliquée par** : GitHub Copilot  
**Référence** : Issue user "un décalage commence, alors je reclique le Sync de la platine B mais le décalage des beats s'accentue"
