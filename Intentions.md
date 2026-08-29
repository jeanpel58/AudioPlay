Intentions et plan de travail — AudioPlay

Contexte
- L'application extrait des pistes audio avec fre:ac (wav puis conversion). Des fichiers temporaires (_head5s/_tail5s) ont été supprimés sauf si la capture de snippets est explicitement activée.
- Le problème prioritaire restant : ProgressBarPisteActuelle n'affiche pas une progression fluide (apparait quasi fixe puis saute à 99%).
- Objectif ultérieur : corriger le chevauchement des pistes en coupant au centre des silences entre pistes (méthode sample‑exacte, sans snippets intrusifs).

Objectifs prioritaires (ordre)
1. Sauvegarder les intentions actuelles (ce fichier) pour garder le plan et les décisions.
2. Corriger le comportement de ProgressBarPisteActuelle et stabiliser les traces de diagnostic.
3. Une fois la progression corrigee et validée, implémenter l'algorithme de découpe au centre des silences (trim sample‑exact).

Etat actuel et contraintes
- Traces temporaires actives : %TEMP%\AudioPlay_progress_trace.txt (utile pour debug). Ne pas supprimer tant que le debug est en cours.
- Flags de snippet : CDAudioAnalyzer.EnableSnippetLogging, CDAudioAnalyzer.EnableSnippetCapture, CDAudioAnalyzer.ForceSaveSnippetsForAllTracks.
- Le flux d'extraction doit rester fre:ac-first; l'extracteur interne reste fallback.

Plan immédiat pour ProgressBarPisteActuelle
- Reproduire une extraction d'essai et collecter la trace existante.
- Localiser le calcul du pourcentage (parsing stdout fre:ac et/ou polling taille fichier dans CDAudioManager.RipTrackWithFreac / CopierAvecProgression).
- Augmenter l'échantillonnage du polling si nécessaire et consigner expectedSize/actualSize à chaque pas.
- S'assurer que SafeUpdateProgressBar est toujours appelé via Invoke/BeginInvoke (thread UI).
- Corriger le calcul expectedDataBytes si incorrect (utiliser métadonnées fiables plutôt que estimation approximative).
- Ajouter mode "simulation" pour afficher coupes/progressions sans écraser fichiers, si utile.

Critères d'acceptation pour ce sprint
- ProgressBarPisteActuelle monte de façon lisse et représentative pendant l'extraction d'une piste (pas de saut brutal à la fin).
- Les traces restent disponibles et lisibles pour diagnostic.

Étapes suivantes après validation
- Développer l'analyse RMS sample‑exacte sur les WAV normaux (fenêtre configurable, seuil, durée minimale de silence).
- Calculer le centre du silence entre pistes et couper précisément sans chevauchement.
- Tests automatisés sur jeux de pistes problèmes.

Validation requise
- Confirmez si je commence maintenant la correction de ProgressBarPisteActuelle et si je dois conserver les traces temporaires pendant le debug (oui/non).

---
Fichier créé automatiquement pour conserver l'état des intentions.
