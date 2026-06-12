# Correction du Reset des TrackBars (Volume, Basses, Aigues)

## Date
2025-01-XX

## Problème identifié

### Symptôme
Les ajustements du volume, des basses et des aigues ne sont pas conservés de façon fiable. De temps à autre, les valeurs se réinitialisent aux valeurs par défaut.

### Analyse

Le problème avait **deux causes principales** :

#### 1. Événements Scroll déclenchés pendant l'initialisation

**Cause** : Les handlers `TrackBar_Volume_Scroll`, `TrackBar_Basses_Scroll` et `TrackBar_Aigues_Scroll` étaient déjà attachés lors de l'initialisation du formulaire. Quand les TrackBars étaient initialisés avec leurs valeurs par défaut (lignes 674-703 de `Form1_Load`), les événements `Scroll` se déclenchaient et sauvegardaient ces valeurs par défaut **avant** que les vraies valeurs ne soient chargées depuis le fichier de paramètres.

**Séquence problématique** :
```vb
' Form1_Load - ordre d'exécution :
1. TrackBar_Volume.Value = 25          ' ← Déclenche Scroll, sauvegarde 25 (mauvais!)
2. TrackBar_Basses.Value = 0           ' ← Déclenche Scroll, sauvegarde 0 (mauvais!)
3. TrackBar_Aigues.Value = 0           ' ← Déclenche Scroll, sauvegarde 0 (mauvais!)
4. ChargerParametres()                  ' ← Charge 0.7 / 10 / 9 depuis le fichier
5. TrackBar_Volume.Value = 35          ' ← Applique 0.7 (bon!)
6. TrackBar_Basses.Value = 10          ' ← Applique 10 (bon!)
7. TrackBar_Aigues.Value = 9           ' ← Applique 9 (bon!)
```

Résultat : Le fichier `parametres.txt` était écrasé avec les mauvaises valeurs aux étapes 1-3, avant même que les bonnes valeurs ne soient chargées.

#### 2. Absence de synchronisation après FormParametres

**Cause** : Après la fermeture du formulaire de paramètres (`FormParametres`), la méthode `ChargerParametres()` était appelée pour recharger les paramètres depuis le fichier, mais les TrackBars **n'étaient pas mis à jour** avec ces valeurs rechargées.

**Code problématique** :
```vb
' Button_Parametres_Click (ancien code)
Private Sub Button_Parametres_Click(...)
	Dim dlg As New FormParametres()
	dlg.ShowDialog(Me)
	ChargerParametres()                ' ← Recharge les variables dernierVolume, etc.
	' ... mais ne met PAS à jour les TrackBars !
End Sub
```

Résultat : Les variables `dernierVolume`, `dernieresBasses`, `dernieresAigues` étaient rechargées correctement, mais les TrackBars gardaient leurs anciennes valeurs visuelles, créant une désynchronisation entre l'UI et l'état interne.

## Corrections apportées

### 1. Variable `initialisationEnCours` pour bloquer les événements Scroll

**Fichier** : `AudioPlay/Form1.vb` (ligne ~113)

```vb
' Ajout d'une variable de contrôle
Private initialisationEnCours As Boolean = False
```

**Activation au début de Form1_Load** (ligne ~610) :
```vb
Private Async Sub Form1_Load(...) Handles MyBase.Load
	' Indiquer que l'initialisation est en cours
	initialisationEnCours = True

	' ... initialisation des TrackBars ...
```

**Désactivation après application des paramètres** (ligne ~747) :
```vb
	' Appliquer les valeurs chargées aux TrackBars
	TrackBar_Volume.Value = ...
	TrackBar_Basses.Value = ...
	TrackBar_Aigues.Value = ...

	' Fin de l'initialisation : autoriser les événements Scroll à sauvegarder
	initialisationEnCours = False
```

**Modification des handlers Scroll** :
```vb
Private Sub TrackBar_Volume_Scroll(...) Handles TrackBar_Volume.Scroll
	' Ignorer les événements pendant l'initialisation
	If initialisationEnCours Then Return
	' ... reste du code ...
End Sub

Private Sub TrackBar_Basses_Scroll(...) Handles TrackBar_Basses.Scroll
	' Ignorer les événements pendant l'initialisation
	If initialisationEnCours Then Return
	' ... reste du code ...
End Sub

Private Sub TrackBar_Aigues_Scroll(...) Handles TrackBar_Aigues.Scroll
	' Ignorer les événements pendant l'initialisation
	If initialisationEnCours Then Return
	' ... reste du code ...
End Sub
```

### 2. Nouvelle méthode `AppliquerParametresAuxControles()`

**Fichier** : `AudioPlay/Form1.vb` (après ligne ~3270)

Cette méthode applique les valeurs des variables internes (`dernierVolume`, `dernieresBasses`, `dernieresAigues`) aux TrackBars de l'UI, tout en bloquant temporairement les événements Scroll pour éviter les sauvegardes inutiles.

```vb
Private Sub AppliquerParametresAuxControles()
	' Appliquer les valeurs chargées aux TrackBars
	' Cette méthode est appelée après ChargerParametres() pour synchroniser l'UI

	' Protéger contre les événements Scroll pendant la mise à jour
	initialisationEnCours = True

	Try
		If TrackBar_Volume IsNot Nothing Then
			Dim v = Math.Max(TrackBar_Volume.Minimum, Math.Min(TrackBar_Volume.Maximum, CInt(dernierVolume * TrackBar_Volume.Maximum)))
			TrackBar_Volume.Value = v
		End If

		If TrackBar_Basses IsNot Nothing Then
			Dim b = Math.Max(TrackBar_Basses.Minimum, Math.Min(TrackBar_Basses.Maximum, CInt(dernieresBasses)))
			TrackBar_Basses.Value = b
		End If

		If TrackBar_Aigues IsNot Nothing Then
			Dim a = Math.Max(TrackBar_Aigues.Minimum, Math.Min(TrackBar_Aigues.Maximum, CInt(dernieresAigues)))
			TrackBar_Aigues.Value = a
		End If
	Finally
		' Réactiver les événements Scroll
		initialisationEnCours = False
	End Try
End Sub
```

**Raison** : 
- Encapsule la logique de synchronisation UI dans une méthode réutilisable
- Utilise `Try/Finally` pour garantir que `initialisationEnCours` est toujours remis à `False`
- Applique les limites Min/Max pour éviter les exceptions

### 3. Appel de `AppliquerParametresAuxControles()` après rechargement

**Fichier** : `AudioPlay/Form1.vb` (ligne ~37)

```vb
Private Sub Button_Parametres_Click(...) Handles Button_Parametres.Click
	Dim dlg As New FormParametres()
	dlg.ShowDialog(Me)
	' Recharger les paramètres après la fermeture de la fenêtre
	ChargerParametres()
	' Appliquer les paramètres rechargés aux contrôles UI
	AppliquerParametresAuxControles()  ' ← NOUVEAU : synchroniser les TrackBars
	' Mettre à jour la couleur des marqueurs de boucle au cas où le thème a changé
	MettreAJourCouleurMarqueursLoop()
	ListView1.Focus()
End Sub
```

**Raison** : Garantit que les TrackBars affichent toujours les valeurs chargées depuis le fichier, même après modification dans FormParametres.

## Résultat

### Avant le correctif
- Les TrackBars se réinitialisaient aléatoirement
- Les valeurs par défaut (Volume=25, Basses=0, Aigues=0) écrasaient les paramètres sauvegardés
- Après ouverture du formulaire de paramètres, les TrackBars ne reflétaient pas les valeurs rechargées

### Après le correctif
- Les événements Scroll ne se déclenchent **jamais** pendant l'initialisation
- Les valeurs sont chargées depuis `parametres.txt` puis appliquées aux TrackBars de façon atomique
- Après fermeture de FormParametres, les TrackBars sont synchronisés avec les paramètres rechargés
- Les valeurs restent persistantes entre les sessions

## Vérification

Pour tester le correctif :

1. Ajuster Volume, Basses, Aigues à des valeurs spécifiques
2. Fermer et rouvrir AudioPlay → Les valeurs doivent être restaurées
3. Ouvrir FormParametres, modifier d'autres paramètres, fermer → Les TrackBars doivent rester inchangés
4. Ouvrir FormParametres, modifier les paramètres audio ailleurs (hypothétiquement), fermer → Les TrackBars doivent refléter les nouvelles valeurs

Vérifier le contenu de `%APPDATA%\AudioPlay\parametres.txt` :
```
Volume=0,7      ← Doit correspondre au TrackBar (35/50 = 0.7)
Basses=10       ← Doit correspondre au TrackBar
Aigues=9        ← Doit correspondre au TrackBar
```

## Fichiers modifiés

- `AudioPlay/Form1.vb`
  - Ligne ~113 : Ajout de `initialisationEnCours`
  - Ligne ~610 : Activation de `initialisationEnCours` au début de `Form1_Load`
  - Ligne ~747 : Désactivation de `initialisationEnCours` après initialisation
  - Ligne ~2491 : Ajout de guard dans `TrackBar_Volume_Scroll`
  - Ligne ~2527 : Ajout de guard dans `TrackBar_Basses_Scroll`
  - Ligne ~2541 : Ajout de guard dans `TrackBar_Aigues_Scroll`
  - Ligne ~3270 : Ajout de `AppliquerParametresAuxControles()`
  - Ligne ~37 : Appel de `AppliquerParametresAuxControles()` dans `Button_Parametres_Click`

## Notes techniques

- La variable `initialisationEnCours` agit comme un **mutex UI** pour empêcher les effets de bord pendant l'initialisation
- Le pattern `Try/Finally` dans `AppliquerParametresAuxControles()` garantit la cohérence même en cas d'exception
- Cette approche est préférable à `RemoveHandler`/`AddHandler` car elle est plus simple et plus robuste
- Le même pattern pourrait être appliqué à d'autres contrôles si nécessaire (ComboBox, CheckBox, etc.)
