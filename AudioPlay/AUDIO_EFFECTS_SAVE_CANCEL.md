# Comportement Sauvegarde/Annulation des Effets Audio

## Vue d'ensemble

Les effets audio dans AudioPlay peuvent être modifiés **en temps réel** pendant la lecture. L'utilisateur peut ajuster les paramètres et entendre immédiatement le résultat. Le comportement au moment de fermer FormParametres dépend du bouton utilisé.

**Important** : Les effets sont maintenant **chargés au démarrage** de l'application depuis `parametres.txt` et réappliqués automatiquement.

---

## Chargement au démarrage

### Au lancement d'AudioPlay

Lorsque `Form1` démarre, la méthode `ChargerParametres()` lit le fichier `parametres.txt` et charge **tous les paramètres d'effets** dans `ParametresGlobaux` :

```vb
' Dans Form1.ChargerParametres()
ElseIf ligne.StartsWith("EffetEchoActif=") Then
    Boolean.TryParse(ligne.Substring("EffetEchoActif=".Length), ParametresGlobaux.EffetEchoActif)
ElseIf ligne.StartsWith("EffetEchoMix=") Then
    ' ... chargement de tous les paramètres
```

### Lors de la première lecture

Lorsqu'une chanson est lancée, la chaîne audio est construite et les effets sont appliqués selon l'état de `ParametresGlobaux` :

```vb
' Dans Form1 - construction de la chaîne audio
If ParametresGlobaux.EffetReverbActif Then
    reverbProvider = New ReverbSampleProvider(equalizerProvider)
    reverbProvider.Enabled = True
    reverbProvider.Mix = ParametresGlobaux.EffetReverbMix
    source = reverbProvider
End If
```

**Résultat** : Si l'utilisateur avait sauvegardé Echo activé la dernière fois, Echo sera automatiquement actif au prochain lancement.

---

## Bouton "Sauvegarder"

### Comportement
- ✅ **Tous les effets modifiés restent actifs**
- ✅ **Les paramètres sont enregistrés dans `parametres.txt`**
- ✅ **Form1 conserve l'état actuel des effets**
- ✅ **Les effets seront réappliqués au prochain démarrage**

### Paramètres sauvegardés
```
EffetReverbActif=True/False
EffetReverbMix=0.0-1.0
EffetEchoActif=True/False
EffetEchoMix=0.0-1.0
EffetEchoDelai=50-2000 (ms)
EffetEchoFeedback=0.0-1.0
EffetPitchActif=True/False
EffetPitchSemitones=-12 à +12
EffetTimeStretchActif=True/False
EffetTimeStretchRatio=0.5-2.0
```

---

## Bouton "Annuler"

### Comportement
- 🔄 **Tous les effets sont restaurés à leur état initial**
- 🔄 **L'état initial = état au moment de l'ouverture de FormParametres**
- 🔄 **Form1 revient à l'état pré-édition**
- ❌ **Aucune modification n'est sauvegardée dans `parametres.txt`**

### Restauration automatique
Lors du clic sur "Annuler" :
1. Tous les paramètres d'effets reviennent à leur valeur initiale
2. `AppliquerEffetsEnTempsReel()` est appelée
3. `Form1.MettreAJourEffetsAudio()` met à jour la chaîne audio en direct
4. La musique en cours continue de jouer avec les paramètres d'origine

---

## Édition en temps réel

### Modification pendant la lecture
- Cocher/décocher une case d'effet → effet activé/désactivé immédiatement
- Déplacer un trackbar → paramètre mis à jour immédiatement
- Les changements sont visibles dans `ParametresGlobaux`
- Les changements sont appliqués à la chaîne audio via `Form1.MettreAJourEffetsAudio()`

### Mécanisme technique
```vb
' Dans FormParametres.vb
Private Sub CheckBoxEchoActif_CheckedChanged(...)
	ParametresGlobaux.EffetEchoActif = CheckBoxEchoActif.Checked
	AppliquerEffetsEnTempsReel()
End Sub

Private Sub AppliquerEffetsEnTempsReel()
	Dim form1 As Form1 = TryCast(Me.Owner, Form1)
	If form1 IsNot Nothing Then
		form1.MettreAJourEffetsAudio()
	End If
End Sub
```

---

## Cycle de vie des états

### 1. Ouverture de FormParametres
```vb
' FormParametres_Load
EtatInitial_ReverbActif = ParametresGlobaux.EffetReverbActif
EtatInitial_ReverbMix = ParametresGlobaux.EffetReverbMix
' ... tous les autres effets
```

### 2. Édition en direct
L'utilisateur modifie les contrôles → effets appliqués immédiatement → musique change en temps réel

### 3a. Clic sur "Sauvegarder"
```vb
' ButtonSauvegarder_Click
' Les valeurs actuelles de ParametresGlobaux sont écrites dans parametres.txt
File.WriteAllLines(cheminConfig, lignes)
' Les effets restent actifs dans Form1
```

### 3b. Clic sur "Annuler"
```vb
' ButtonAnnuler_Click
ParametresGlobaux.EffetReverbActif = EtatInitial_ReverbActif
ParametresGlobaux.EffetReverbMix = EtatInitial_ReverbMix
' ... tous les autres effets
AppliquerEffetsEnTempsReel()
```

---

## Exemples de scénarios

### Scénario 1 : Expérimentation puis sauvegarde
1. Utilisateur ouvre FormParametres (Echo désactivé)
2. Utilisateur coche Echo, ajuste délai à 500ms, feedback à 40%
3. Utilisateur écoute le résultat en temps réel
4. Utilisateur clique "Sauvegarder"
5. **Résultat** : Echo reste actif avec délai=500ms, feedback=40%, sauvegardé dans config

### Scénario 2 : Expérimentation puis abandon
1. Utilisateur ouvre FormParametres (Reverb actif, mix=30%)
2. Utilisateur change Reverb mix à 80%
3. Utilisateur active Echo avec délai=1000ms
4. Utilisateur n'aime pas le résultat
5. Utilisateur clique "Annuler"
6. **Résultat** : Reverb revient à 30%, Echo est désactivé, rien n'est sauvegardé

### Scénario 3 : Modification multiple avec sauvegarde partielle
1. Utilisateur ouvre FormParametres (Reverb=off, Echo=off)
2. Utilisateur active Echo (entend immédiatement)
3. Utilisateur active Reverb (entend immédiatement)
4. Utilisateur ajuste les paramètres
5. Utilisateur clique "Sauvegarder"
6. **Résultat** : Les deux effets restent actifs avec leurs nouveaux paramètres

---

## Notes techniques

### Variables d'état initial
```vb
Private EtatInitial_ReverbActif As Boolean
Private EtatInitial_ReverbMix As Single
Private EtatInitial_EchoActif As Boolean
Private EtatInitial_EchoMix As Single
Private EtatInitial_EchoDelai As Integer
Private EtatInitial_EchoFeedback As Single
Private EtatInitial_PitchActif As Boolean
Private EtatInitial_PitchSemitones As Single
Private EtatInitial_TimeStretchActif As Boolean
Private EtatInitial_TimeStretchRatio As Single
```

### Synchronisation avec Form1
- `FormParametres` met à jour `ParametresGlobaux` (module global)
- `Form1.MettreAJourEffetsAudio()` lit `ParametresGlobaux` et met à jour les providers actifs
- Pas de redémarrage de la chanson nécessaire

---

## Avantages de cette approche

1. **Expérimentation sans risque** : l'utilisateur peut tester différents réglages
2. **Feedback immédiat** : entendre les changements en temps réel
3. **Contrôle total** : sauvegarder uniquement si satisfait
4. **Cohérence** : même comportement que les thèmes (save/cancel)
5. **Performance** : pas de redémarrage de la lecture nécessaire

---

## Voir aussi

- `AUDIO_EFFECTS_README.md` : documentation générale des effets
- `ParametresGlobaux.vb` : variables globales des effets
- `Form1.vb` : méthode `MettreAJourEffetsAudio()`
- `FormParametres.vb` : gestionnaires d'événements et sauvegarde
