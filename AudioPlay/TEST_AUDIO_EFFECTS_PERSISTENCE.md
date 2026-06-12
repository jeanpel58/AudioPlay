# TEST : Persistance des effets audio

## Objectif
Vérifier que les effets audio sont correctement sauvegardés et restaurés après redémarrage d'AudioPlay.

---

## Test 1 : Sauvegarde et redémarrage simple

### Étapes
1. ✅ Lancer AudioPlay
2. ✅ Ouvrir FormParametres (menu Fichier → Paramètres)
3. ✅ Activer **Echo**
4. ✅ Ajuster **Délai = 500ms**
5. ✅ Ajuster **Feedback = 40%**
6. ✅ Cliquer **Sauvegarder**
7. ✅ Fermer AudioPlay
8. ✅ Vérifier le fichier config :
   ```powershell
   Get-Content "$env:APPDATA\AudioPlay\parametres.txt"
   ```
   **Attendu** : Les lignes suivantes doivent être présentes :
   ```
   EffetEchoActif=True
   EffetEchoMix=0.3
   EffetEchoDelai=500
   EffetEchoFeedback=0.4
   ```

9. ✅ Rouvrir AudioPlay
10. ✅ Lancer une chanson
11. ✅ **Attendu** : Echo actif immédiatement
12. ✅ Ouvrir FormParametres
13. ✅ **Attendu** : 
	- Case Echo cochée ✅
	- Délai = 500ms ✅
	- Feedback = 40% ✅

---

## Test 2 : Ajustement volume après sauvegarde effets

### Objectif
Vérifier que `Form1.SauvegarderParametres()` ne détruit pas les effets.

### Étapes
1. ✅ Lancer AudioPlay
2. ✅ Ouvrir FormParametres
3. ✅ Activer **Reverb** (mix = 50%)
4. ✅ Activer **Echo** (délai = 1000ms, feedback = 30%)
5. ✅ Cliquer **Sauvegarder**
6. ✅ Retour à Form1
7. ✅ Lancer une chanson (effets actifs)
8. ✅ **Ajuster le volume avec le trackbar**
9. ✅ Vérifier le fichier config :
   ```powershell
   Get-Content "$env:APPDATA\AudioPlay\parametres.txt"
   ```
   **Attendu** : Les effets doivent **encore être présents** dans le fichier !
   ```
   Volume=0.25
   EffetReverbActif=True
   EffetReverbMix=0.5
   EffetEchoActif=True
   EffetEchoMix=0.3
   EffetEchoDelai=1000
   EffetEchoFeedback=0.3
   ```

10. ✅ Fermer et rouvrir AudioPlay
11. ✅ Lancer une chanson
12. ✅ **Attendu** : Reverb ET Echo toujours actifs !

---

## Test 3 : Annulation d'effets

### Étapes
1. ✅ Lancer AudioPlay (Echo déjà sauvegardé actif)
2. ✅ Ouvrir FormParametres
3. ✅ **Décocher Echo**
4. ✅ Activer **Pitch Shift** (+2 semitones)
5. ✅ Cliquer **Annuler**
6. ✅ **Attendu** : Echo revient à l'état initial (actif), Pitch Shift désactivé
7. ✅ Ouvrir FormParametres à nouveau
8. ✅ **Attendu** : Echo toujours coché, Pitch Shift décoché

---

## Test 4 : Effets multiples

### Étapes
1. ✅ Lancer AudioPlay
2. ✅ Ouvrir FormParametres
3. ✅ Activer **tous les effets** :
   - Reverb : mix = 40%
   - Echo : délai = 750ms, feedback = 35%
   - Pitch Shift : +3 semitones
   - Time Stretch : 1.2x
4. ✅ Cliquer **Sauvegarder**
5. ✅ Fermer AudioPlay
6. ✅ Vérifier le fichier config (tous les effets présents)
7. ✅ Rouvrir AudioPlay
8. ✅ Lancer une chanson
9. ✅ **Attendu** : Tous les effets actifs
10. ✅ Ouvrir FormParametres
11. ✅ **Attendu** : Toutes les cases cochées avec les bons paramètres

---

## Test 5 : Réinitialisation via bouton Reset

### Étapes
1. ✅ Lancer AudioPlay (plusieurs effets actifs)
2. ✅ Ouvrir FormParametres
3. ✅ Cliquer **Reset Effets**
4. ✅ **Attendu** : Toutes les cases décochées, trackbars aux valeurs par défaut
5. ✅ **NE PAS** cliquer Sauvegarder
6. ✅ Cliquer **Annuler**
7. ✅ **Attendu** : Les effets reviennent à l'état initial (actifs)

---

## Test 6 : Édition en temps réel

### Étapes
1. ✅ Lancer AudioPlay
2. ✅ Lancer une chanson
3. ✅ Ouvrir FormParametres (musique continue)
4. ✅ Cocher **Echo**
5. ✅ **Attendu** : Echo s'active immédiatement pendant la lecture
6. ✅ Déplacer trackbar **Délai**
7. ✅ **Attendu** : Le délai change en temps réel
8. ✅ Ajuster **Feedback**
9. ✅ **Attendu** : Le feedback change en temps réel
10. ✅ Cliquer **Sauvegarder**
11. ✅ Fermer AudioPlay
12. ✅ Rouvrir et lancer une chanson
13. ✅ **Attendu** : Echo actif avec les derniers paramètres

---

## Vérification fichier de configuration

### Commande PowerShell
```powershell
$configPath = Join-Path $env:APPDATA "AudioPlay\parametres.txt"
Write-Host "=== Contenu du fichier de configuration ===" -ForegroundColor Cyan
Get-Content $configPath | Where-Object { $_ -like "*Effet*" } | ForEach-Object { Write-Host $_ -ForegroundColor Yellow }
```

### Exemple de sortie attendue (tous effets actifs)
```
EffetReverbActif=True
EffetReverbMix=0.4
EffetEchoActif=True
EffetEchoMix=0.3
EffetEchoDelai=750
EffetEchoFeedback=0.35
EffetPitchActif=True
EffetPitchSemitones=3
EffetTimeStretchActif=True
EffetTimeStretchRatio=1.2
```

---

## Checklist de validation

- [ ] Test 1 réussi : Sauvegarde et redémarrage simple
- [ ] Test 2 réussi : Ajustement volume n'écrase pas les effets
- [ ] Test 3 réussi : Annulation restaure l'état initial
- [ ] Test 4 réussi : Effets multiples tous persistants
- [ ] Test 5 réussi : Reset + Annuler restaure l'état
- [ ] Test 6 réussi : Édition temps réel + sauvegarde

---

## En cas d'échec

### Si les effets ne sont pas dans le fichier après sauvegarde
1. Vérifier que `FormParametres.ButtonSauvegarder_Click` écrit bien les lignes `EffetXXX=`
2. Vérifier le chemin : `%AppData%\AudioPlay\parametres.txt`

### Si les effets disparaissent après ajustement volume
1. Vérifier que `Form1.SauvegarderParametres()` inclut bien les lignes `EffetXXX=`
2. Ce bug était présent avant le correctif

### Si les effets ne se chargent pas au démarrage
1. Vérifier que `Form1.ChargerParametres()` lit bien les clés `EffetXXX=`
2. Ajouter des `Debug.WriteLine` pour tracer le chargement

---

## Résumé des correctifs

✅ **Correctif 1** : `Form1.ChargerParametres()` charge maintenant les effets depuis parametres.txt
✅ **Correctif 2** : `Form1.SauvegarderParametres()` sauvegarde maintenant les effets (évite l'écrasement)
✅ **Correctif 3** : `FormParametres` capture l'état initial pour permettre l'annulation
