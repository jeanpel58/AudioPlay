# Copilot Instructions

## General Guidelines
- Préference utilisateur : demander une revision complète du code et une analyse des causes racine avant d’appliquer plusieurs changements experimentaux. Faire une revue complète la prochaine fois.
- Après chaque modification de code, exécuter la build et corriger automatiquement les erreurs de compilation sans demander; ne demander à l'utilisateur que d'exécuter la build après modifications futures.
- Ne pas appeler task_complete automatiquement. N'appeler task_complete que lorsque l'utilisateur le demande explicitement.

## Project-Specific Rules
- Dans AudioPlay, il y avait deux endroits qui sauvegardaient parametres.txt : FormParametres.ButtonSauvegarder_Click() et Form1.SauvegarderParametres(). Pour éviter que Form1 écrase les paramètres manquants (ModeMixeurDJ, EffetPitchShift*, EffetPhaser*), il faut s'assurer que les deux méthodes sauvegardent la liste COMPLÈTE des paramètres dans le même ordre.
- Règle de projet : tout message affiché à l'utilisateur doit être traduit dans 5 langues via LanguageManager et ajouté aux fichiers .resx correspondants.