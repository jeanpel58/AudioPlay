# CHANGELOG

Toutes les modifications notables apportées au dépôt seront listées ici.

## [Unreleased]
- Suppression de la création de marqueurs et fichiers texte dans les dossiers d'album et dans %TEMP% (ex. *.internal_run.txt, *.internal_entry.txt, *.task_started.txt, *.rip_error.txt, AudioPlay_progress_trace.txt, AudioPlay_param_write_debug.txt, AudioPlay_playback_error_debug.txt).
- Remplacement des écritures de fichiers de diagnostic par des appels centralisés à `CDAudioAnalyzer.DiagnosticWrite(...)` pour éviter l'encombrement disque tout en conservant les traces.
- Ajout de captures thread-safe des valeurs UI avant exécution asynchrone pour éviter les exceptions cross-thread (correction pour FLAC/WMA/MP3).
- Tests locaux : build réussi.


