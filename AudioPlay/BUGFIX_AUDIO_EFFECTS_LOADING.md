# BUGFIX : Chargement des effets audio au démarrage

## Problème identifié

**Symptôme** : L'utilisateur sauvegarde les effets audio (ex: Echo activé avec paramètres personnalisés) dans FormParametres, mais au redémarrage d'AudioPlay, les effets ne sont pas réappliqués. Les cases à cocher sont décochées et les paramètres sont réinitialisés aux valeurs par défaut.

**Causes racines** (deux problèmes distincts) :

1. **Chargement manquant** : La méthode `Form1.ChargerParametres()` ne lisait **PAS** les paramètres d'effets audio depuis `parametres.txt` au démarrage.

2. **Écrasement du fichier** : La méthode `Form1.SauvegarderParametres()` (appelée lors des changements de volume/basses/aigus) **écrasait le fichier** sans inclure les effets audio, détruisant les paramètres sauvegardés par `FormParametres`.

---

## Solution implémentée

### Modification de `Form1.ChargerParametres()`

Ajout du chargement des 10 paramètres d'effets audio dans la boucle de lecture du fichier `parametres.txt` :

```vb
' === Effets Audio ===
ElseIf ligne.StartsWith("EffetReverbActif=") Then
	Boolean.TryParse(ligne.Substring("EffetReverbActif=".Length), ParametresGlobaux.EffetReverbActif)
ElseIf ligne.StartsWith("EffetReverbMix=") Then
	Dim mix As Single
	If Single.TryParse(ligne.Substring("EffetReverbMix=".Length), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, mix) Then
		ParametresGlobaux.EffetReverbMix = mix
	End If
ElseIf ligne.StartsWith("EffetEchoActif=") Then
	Boolean.TryParse(ligne.Substring("EffetEchoActif=".Length), ParametresGlobaux.EffetEchoActif)
ElseIf ligne.StartsWith("EffetEchoMix=") Then
	Dim mix As Single
	If Single.TryParse(ligne.Substring("EffetEchoMix=".Length), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, mix) Then
		ParametresGlobaux.EffetEchoMix = mix
	End If
ElseIf ligne.StartsWith("EffetEchoDelai=") Then
	Integer.TryParse(ligne.Substring("EffetEchoDelai=".Length), ParametresGlobaux.EffetEchoDelai)
ElseIf ligne.StartsWith("EffetEchoFeedback=") Then
	Dim fb As Single
	If Single.TryParse(ligne.Substring("EffetEchoFeedback=".Length), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, fb) Then
		ParametresGlobaux.EffetEchoFeedback = fb
	End If
ElseIf ligne.StartsWith("EffetPitchActif=") Then
	Boolean.TryParse(ligne.Substring("EffetPitchActif=".Length), ParametresGlobaux.EffetPitchActif)
ElseIf ligne.StartsWith("EffetPitchSemitones=") Then
	Dim pitch As Single
	If Single.TryParse(ligne.Substring("EffetPitchSemitones=".Length), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, pitch) Then
		ParametresGlobaux.EffetPitchSemitones = pitch
	End If
ElseIf ligne.StartsWith("EffetTimeStretchActif=") Then
	Boolean.TryParse(ligne.Substring("EffetTimeStretchActif=".Length), ParametresGlobaux.EffetTimeStretchActif)
ElseIf ligne.StartsWith("EffetTimeStretchRatio=") Then
	Dim ratio As Single
	If Single.TryParse(ligne.Substring("EffetTimeStretchRatio=".Length), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, ratio) Then
		ParametresGlobaux.EffetTimeStretchRatio = ratio
	End If
```

### Fichiers modifiés

- **AudioPlay/Form1.vb** : 
  - `ChargerParametres()` : Ajout du chargement des effets (lignes ~3329-3373)
  - `SauvegarderParametres()` : Ajout de la sauvegarde des effets (lignes ~3396-3422)

### 2. Modification de `Form1.SauvegarderParametres()`

⚠️ **CRITIQUE** : Ajout des effets audio à la sauvegarde pour éviter l'écrasement !

Cette méthode est appelée par :
- `SauvegarderVolume()` → déclenchée à chaque changement de volume
- `SauvegarderBasses()` → déclenchée à chaque changement de basses
- `SauvegarderAigues()` → déclenchée à chaque changement d'aigus

**Sans ce correctif** : Chaque ajustement d'égaliseur effaçait les paramètres d'effets sauvegardés !

```vb
Dim lignes As New List(Of String) From {
    ' ... paramètres existants ...
    $"EffetReverbActif={ParametresGlobaux.EffetReverbActif}",
    $"EffetReverbMix={ParametresGlobaux.EffetReverbMix.ToString(InvariantCulture)}",
    $"EffetEchoActif={ParametresGlobaux.EffetEchoActif}",
    $"EffetEchoMix={ParametresGlobaux.EffetEchoMix.ToString(InvariantCulture)}",
    $"EffetEchoDelai={ParametresGlobaux.EffetEchoDelai}",
    $"EffetEchoFeedback={ParametresGlobaux.EffetEchoFeedback.ToString(InvariantCulture)}",
    $"EffetPitchActif={ParametresGlobaux.EffetPitchActif}",
    $"EffetPitchSemitones={ParametresGlobaux.EffetPitchSemitones.ToString(InvariantCulture)}",
    $"EffetTimeStretchActif={ParametresGlobaux.EffetTimeStretchActif}",
    $"EffetTimeStretchRatio={ParametresGlobaux.EffetTimeStretchRatio.ToString(InvariantCulture)}"
}
```

---

## Flux de persistance complet

### 1. Sauvegarde (dans FormParametres)

```vb
' ButtonSauvegarder_Click écrit dans parametres.txt
"EffetReverbActif=" & ParametresGlobaux.EffetReverbActif.ToString()
"EffetReverbMix=" & ParametresGlobaux.EffetReverbMix.ToString(InvariantCulture)
"EffetEchoActif=" & ParametresGlobaux.EffetEchoActif.ToString()
' ... etc pour tous les effets
```

### 2. Fermeture application

Les paramètres sont sauvegardés dans :
```
%AppData%\AudioPlay\parametres.txt
```

⚠️ **Attention** : Si l'utilisateur ajuste le volume/basses/aigus avant de fermer, `Form1.SauvegarderParametres()` est appelée et **doit** inclure les effets !

### 3. Réouverture application

```vb
' Form1.ChargerParametres() lit parametres.txt
' et peuple ParametresGlobaux avec les valeurs sauvegardées
ParametresGlobaux.EffetEchoActif = True
ParametresGlobaux.EffetEchoMix = 0.3
ParametresGlobaux.EffetEchoDelai = 500
ParametresGlobaux.EffetEchoFeedback = 0.4
```

### 4. Première lecture d'une chanson

```vb
' La chaîne audio est construite avec les effets sauvegardés
If ParametresGlobaux.EffetEchoActif Then
	echoProvider = New EchoSampleProvider(source)
	echoProvider.Enabled = True
	echoProvider.Mix = ParametresGlobaux.EffetEchoMix
	echoProvider.DelayMilliseconds = ParametresGlobaux.EffetEchoDelai
	echoProvider.Feedback = ParametresGlobaux.EffetEchoFeedback
	source = echoProvider
End If
```

### 5. Ouverture de FormParametres

```vb
' FormParametres.AfficherParametres() synchronise l'UI avec ParametresGlobaux
CheckBoxEchoActif.Checked = ParametresGlobaux.EffetEchoActif  ' True
TrackBarEchoMix.Value = CInt(ParametresGlobaux.EffetEchoMix * 100)  ' 30
TrackBarEchoDelai.Value = ParametresGlobaux.EffetEchoDelai  ' 500
TrackBarEchoFeedback.Value = CInt(ParametresGlobaux.EffetEchoFeedback * 100)  ' 40
```

**Résultat** : L'utilisateur retrouve ses réglages exactement comme il les avait laissés.

---

## Tests de validation

### Test 1 : Sauvegarde et réouverture
1. ✅ Ouvrir FormParametres
2. ✅ Activer Echo, ajuster délai=500ms, feedback=40%
3. ✅ Cliquer "Sauvegarder"
4. ✅ Fermer AudioPlay
5. ✅ Rouvrir AudioPlay
6. ✅ Lancer une chanson → Echo actif avec les bons paramètres
7. ✅ Ouvrir FormParametres → cases cochées, trackbars aux bonnes valeurs

### Test 2 : Modification multiple
1. ✅ Activer Reverb (mix=50%)
2. ✅ Activer Echo (délai=1000ms, feedback=30%)
3. ✅ Sauvegarder
4. ✅ Redémarrer AudioPlay
5. ✅ Les deux effets sont actifs avec leurs paramètres

### Test 3 : Désactivation et sauvegarde
1. ✅ Ouvrir FormParametres (Echo était actif)
2. ✅ Décocher Echo
3. ✅ Sauvegarder
4. ✅ Redémarrer AudioPlay
5. ✅ Echo reste désactivé

---

## Impact sur les fichiers existants

### Rétrocompatibilité

Si un utilisateur a une ancienne version de `parametres.txt` **sans** les clés d'effets audio :
- ✅ Les valeurs par défaut de `ParametresGlobaux` sont utilisées (tous effets désactivés)
- ✅ Aucune erreur n'est levée
- ✅ Dès la première sauvegarde, les clés sont ajoutées au fichier

### Migration automatique

Aucune migration nécessaire. Le système utilise `TryParse` pour tous les paramètres, donc :
- Clé manquante → valeur par défaut conservée
- Clé présente → valeur chargée

---

## Documentation mise à jour

- ✅ `AUDIO_EFFECTS_SAVE_CANCEL.md` : Ajout de la section "Chargement au démarrage"
- ✅ `BUGFIX_AUDIO_EFFECTS_LOADING.md` : Ce document (récapitulatif du correctif)

---

## Comparaison avant/après

### Avant le correctif

```
Utilisateur :
1. Ouvre FormParametres
2. Active Echo, ajuste paramètres
3. Clique "Sauvegarder"
4. Ferme AudioPlay

Au redémarrage :
❌ Echo est désactivé
❌ Paramètres sont réinitialisés
❌ parametres.txt contient les bonnes valeurs mais elles ne sont pas lues
```

### Après le correctif

```
Utilisateur :
1. Ouvre FormParametres
2. Active Echo, ajuste paramètres
3. Clique "Sauvegarder"
4. Ferme AudioPlay

Au redémarrage :
✅ Echo est activé
✅ Paramètres sont restaurés
✅ parametres.txt est lu et appliqué correctement
```

---

## Notes techniques

### Pourquoi deux emplacements de chargement ?

1. **Form1.ChargerParametres()** : Charge depuis `parametres.txt` au démarrage de l'application
2. **FormParametres.ChargerParametres()** : Recharge depuis `parametres.txt` lors de l'ouverture du formulaire de paramètres

Les deux méthodes lisent le même fichier mais ont des responsabilités différentes :
- `Form1` : initialiser l'état global de l'application
- `FormParametres` : initialiser l'UI du formulaire de paramètres

### Synchronisation ParametresGlobaux

`ParametresGlobaux` est un **module global** accessible depuis tous les formulaires. C'est la source de vérité pour tous les paramètres d'effets :

```vb
Module ParametresGlobaux
	Public EffetReverbActif As Boolean = False
	Public EffetReverbMix As Single = 0.3F
	Public EffetEchoActif As Boolean = False
	' ... etc
End Module
```

- Form1 le lit au démarrage
- FormParametres le modifie en temps réel
- Form1.MettreAJourEffetsAudio() l'applique à la chaîne audio

---

## Voir aussi

- `AUDIO_EFFECTS_README.md` : Documentation générale des effets
- `AUDIO_EFFECTS_SAVE_CANCEL.md` : Comportement Save/Cancel détaillé
- `ParametresGlobaux.vb` : Module de variables globales
- `Form1.vb` : Méthode `ChargerParametres()` et `MettreAJourEffetsAudio()`
- `FormParametres.vb` : Méthode `ChargerParametres()` et `AfficherParametres()`
