# Raccourcis Clavier - AudioPlay

## Raccourcis de lecture

| Touche | Action | Description |
|--------|--------|-------------|
| **Espace** | Lecture/Pause | Si rien ne joue → démarre la chanson sélectionnée<br>Si en lecture → met en pause<br>Si en pause → reprend la lecture |
| **Ctrl+Espace** | Arrêter TOUT | **Arrête complètement :**<br>- Calcul BPM en cours (affiche un message de confirmation)<br>- Lecture audio (réinitialise la position à 0:00)<br>- Karaoke (ferme la fenêtre CDG)<br>- Timer de progression<br>- Libère toutes les ressources audio |
| **Ctrl+P** | Pause/Reprise | Alterne entre pause et lecture |
| **Ctrl+S** | Sourdine | Active/désactive le mode muet |
| **Ctrl+A** | Aléatoire | Active/désactive le mode lecture aléatoire |
| **I** | Marquer début boucle | Capture la position actuelle comme début de la boucle (pendant la lecture) |
| **O** | Marquer fin boucle | Capture la position actuelle comme fin de la boucle (pendant la lecture) |

## Navigation dans la playlist

| Touche | Action | Description |
|--------|--------|-------------|
| **↑** | Haut | Sélectionne la chanson précédente |
| **↓** | Bas | Sélectionne la chanson suivante |
| **Ctrl+↑** | Déplacer haut | Déplace la chanson sélectionnée vers le haut dans la liste |
| **Ctrl+↓** | Déplacer bas | Déplace la chanson sélectionnée vers le bas dans la liste |
| **Home** | Début | Sélectionne la première chanson |
| **End** | Fin | Sélectionne la dernière chanson |
| **Suppr** | Supprimer | Retire la chanson sélectionnée de la playlist |

## Logique de la barre d'espace

### ✅ Nouveau comportement (après correction)

```
État actuel          │ Action Espace       │ Résultat
─────────────────────┼────────────────────┼─────────────────────
Aucune lecture       │ Démarre             │ Joue la chanson sélectionnée
En lecture           │ Met en pause        │ Pause à la position actuelle
En pause             │ Reprend             │ Continue depuis la position de pause
```

### ❌ Ancien comportement (avant correction)

```
État actuel          │ Action Espace       │ Résultat
─────────────────────┼────────────────────┼─────────────────────
Aucune lecture       │ Démarre             │ Joue la chanson sélectionnée
En lecture           │ Arrête              │ ❌ Arrête complètement (retour au début)
En pause             │ Démarre             │ ❌ Recommence du début
```

## Différence Espace vs Ctrl+Espace

| Raccourci | Comportement | Cas d'usage |
|-----------|--------------|-------------|
| **Espace** | **Pause/Reprise** - La chanson continue où elle était | Pause rapide (répondre au téléphone, etc.) |
| **Ctrl+Espace** | **Arrêt complet** - Tout s'arrête : lecture, BPM, karaoke, position à 0:00 | Arrêter complètement ou annuler un calcul BPM |

### Comportement détaillé de Ctrl+Espace

**Priorité 1 : Annuler le calcul BPM**
- Si un calcul BPM est en cours :
  - ✅ Annule immédiatement le calcul
  - ✅ Affiche un message de confirmation
  - ✅ **Ne touche PAS à la lecture** (elle peut continuer)
  - ✅ Restaure le focus sur la playlist

**Priorité 2 : Arrêter la lecture**
- Si aucun calcul BPM n'est en cours :
  - ✅ Arrête le lecteur audio
  - ✅ Arrête le karaoke (si actif)
  - ✅ Arrête le timer de progression
  - ✅ Réinitialise la position à 0:00
  - ✅ Libère toutes les ressources (mémoire, fichiers)
  - ✅ Remet tous les boutons à l'état initial (gris/rouge)

## Exemple d'utilisation

### Scénario 1 : Pause temporaire pendant une chanson

1. Une chanson joue à 1:23
2. Appuyer sur **Espace** → La chanson se met en pause à 1:23
3. Appuyer à nouveau sur **Espace** → La chanson reprend à 1:23 ✅

### Scénario 2 : Arrêt complet de la lecture

1. Une chanson joue à 1:23
2. Appuyer sur **Ctrl+Espace** → La chanson s'arrête et revient à 0:00
3. Appuyer sur **Espace** → La chanson recommence à 0:00

### Scénario 3 : Annuler un calcul BPM en cours

1. Un calcul BPM est en cours (icône ou barre de progression visible)
2. Appuyer sur **Ctrl+Espace** → Le calcul BPM s'annule immédiatement
3. Un message s'affiche : "Calcul BPM annulé" (ou équivalent localisé)
4. La lecture de la chanson **continue** si elle était en cours ✅

### Scénario 4 : Arrêter tout (lecture + BPM)

1. Une chanson joue ET un calcul BPM est en cours
2. Appuyer sur **Ctrl+Espace** → Le calcul BPM s'arrête
3. Appuyer à nouveau sur **Ctrl+Espace** → La lecture s'arrête complètement

## Implémentation technique

### Code de Button_Arreter_Click (Form1.vb, ligne ~1248-1262)

```vb
Private Sub Button_Arreter_Click(sender As Object, e As EventArgs) Handles Button_Arreter.Click
	' PRIORITÉ 1 : Si un calcul BPM est en cours, l'annuler
	If calculBPMEnCours Then
		If bpmCancellationTokenSource IsNot Nothing Then
			bpmCancellationTokenSource.Cancel()
			MessageBox.Show(LanguageManager.GetString("BPM_Cancelled"), 
						  LanguageManager.GetString("Cancellation_Title"), 
						  MessageBoxButtons.OK, MessageBoxIcon.Information)
		End If
		ListView1.Focus()
		Return  ' ← Sort sans toucher à la lecture
	End If

	' PRIORITÉ 2 : Sinon, arrêter la lecture audio normale
	ArreterLecture()
	ListView1.Focus()
End Sub
```

### Fonction ArreterLecture() (Form1.vb, ligne ~1001-1080)

Cette fonction fait un nettoyage complet :

1. ✅ Arrête le timer de progression
2. ✅ Arrête le karaoke (si FormKaraoke est ouvert)
3. ✅ Retire le gestionnaire d'événements PlaybackStopped
4. ✅ Arrête le lecteur audio (WaveOutEvent)
5. ✅ Libère volumeProvider et equalizerProvider
6. ✅ Dispose le lecteur
7. ✅ Ferme et dispose le fichierAudio
8. ✅ Force le garbage collector
9. ✅ Réinitialise les variables d'état (`lectureEnCours`, `enPause`, `cheminActuel`)
10. ✅ Remet tous les boutons à l'état initial (couleurs gris/rouge)

### Gestion du clavier

**Deux endroits gèrent Ctrl+Espace :**

1. **ProcessCmdKey** (ligne ~2535-2537) - Niveau formulaire
```vb
ElseIf keyData = (Keys.Control Or Keys.Space) Then
	Button_Arreter_Click(Button_Arreter, EventArgs.Empty)
	Return True
```

2. **ListView1_KeyDown** (ligne ~2703-2707) - Niveau ListView
```vb
If e.Control AndAlso e.KeyCode = Keys.Space Then
	e.Handled = True
	e.SuppressKeyPress = True
	Button_Arreter_Click(Button_Arreter, EventArgs.Empty)
End If
```

Les deux appellent directement `Button_Arreter_Click`, garantissant un comportement identique.

## Variables d'état

- `lectureEnCours` : `True` si une chanson est chargée et joue/en pause
- `enPause` : `True` si la lecture est en pause
- `Button_PauseReprise.PerformClick()` : Déclenche le bouton Pause/Reprise

## Tests effectués

✅ **Test 1** : Espace pendant lecture → Pause correcte  
✅ **Test 2** : Espace pendant pause → Reprise correcte  
✅ **Test 3** : Espace sans lecture → Démarre la chanson  
✅ **Test 4** : Ctrl+Espace → Arrête complètement  

## Avantages du nouveau comportement

1. **Plus intuitif** : Conforme aux lecteurs multimédia standards (VLC, Windows Media Player, YouTube, etc.)
2. **Workflow naturel** : Espace = pause rapide, Ctrl+Espace = arrêt complet
3. **Pas de perte de position** : On peut mettre en pause et reprendre sans recommencer
4. **Cohérent avec le bouton** : La barre d'espace fait exactement la même chose que le bouton Pause/Reprise

## Date de modification

2024-12-XX - Correction du comportement de la barre d'espace pour pause/reprise au lieu d'arrêt/redémarrage
