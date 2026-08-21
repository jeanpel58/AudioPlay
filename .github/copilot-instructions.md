# Copilot Instructions

## General Guidelines
- Préférence utilisateur : demander une révision complète du code et une analyse des causes racine avant d’appliquer plusieurs changements expérimentaux. Faire une revue complète la prochaine fois.
- Préférence utilisateur : conserver la purge automatique des snippets WAV (rétention par défaut 30 jours).
- Note : les modifications récentes du code ont été appliquées par l'assistant, et non par l'utilisateur.

## Directives de projet
- Dans AudioPlay, il y avait deux endroits qui sauvegardaient parametres.txt : FormParametres.ButtonSauvegarder_Click() et Form1.SauvegarderParametres(). Pour éviter que Form1 écrase les paramètres manquants (ModeMixeurDJ, EffetPitchShift*, EffetPhaser*), il faut s'assurer que les deux méthodes sauvegardent la liste COMPLÈTE des paramètres dans le même ordre.