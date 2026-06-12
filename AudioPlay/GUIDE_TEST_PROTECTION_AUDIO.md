# 🧪 GUIDE DE TEST UTILISATEUR - Protection Volume/Basses/Aigues

## 🎯 Objectif du test
Vérifier que les valeurs de **Volume, Basses et Aigues** ajustées par l'utilisateur **ne changent JAMAIS**, même après sauvegarde d'autres paramètres.

---

## ✅ TEST #1 : Changement de langue

### Étapes
1. **Ajuster les paramètres audio** :
   - Volume : **70%** (TrackBar à mi-chemin)
   - Basses : **+10** (déplacer vers la droite)
   - Aigues : **+8** (déplacer vers la droite)

2. **Noter les positions des TrackBars** (prendre une capture d'écran si besoin)

3. **Ouvrir les Paramètres** :
   - Cliquer sur le bouton **Paramètres** (icône d'engrenage)

4. **Changer la langue** :
   - Dans l'onglet **Général**
   - Sélectionner une autre langue (ex: Français → English)

5. **Sauvegarder** :
   - Cliquer sur le bouton **Sauvegarder**

6. **Vérifier les valeurs** :
   - ✅ Volume doit être à **70%** (même position)
   - ✅ Basses doit être à **+10** (même position)
   - ✅ Aigues doit être à **+8** (même position)

### Résultat attendu
✅ **Les trois TrackBars doivent être EXACTEMENT à la même position qu'avant.**

---

## ✅ TEST #2 : Changement de thème

### Étapes
1. **Ajuster les paramètres audio** :
   - Volume : **30%**
   - Basses : **-5** (déplacer vers la gauche)
   - Aigues : **+12**

2. **Ouvrir les Paramètres**

3. **Changer le thème** :
   - Onglet **Thème**
   - Sélectionner un autre thème (ex: Bleu → Vert)

4. **Sauvegarder**

5. **Vérifier** :
   - ✅ Volume = **30%**
   - ✅ Basses = **-5**
   - ✅ Aigues = **+12**

### Résultat attendu
✅ **Les valeurs audio doivent être identiques.**

---

## ✅ TEST #3 : Activation du métronome

### Étapes
1. **Ajuster les paramètres audio** :
   - Volume : **85%**
   - Basses : **+15**
   - Aigues : **-3**

2. **Ouvrir les Paramètres**

3. **Activer le métronome** :
   - Onglet **Métronome**
   - Cocher **"Métronome activé"**
   - Changer le nombre de beats (ex: 4 → 3)

4. **Sauvegarder**

5. **Vérifier** :
   - ✅ Volume = **85%**
   - ✅ Basses = **+15**
   - ✅ Aigues = **-3**

### Résultat attendu
✅ **Les valeurs audio doivent être identiques.**

---

## ✅ TEST #4 : Activation d'effets audio

### Étapes
1. **Ajuster les paramètres audio** :
   - Volume : **50%**
   - Basses : **0**
   - Aigues : **0**

2. **Ouvrir les Paramètres**

3. **Activer des effets** :
   - Onglet **Effets Audio**
   - Activer **Reverb** et ajuster le mix
   - Activer **Echo** et ajuster le délai

4. **Sauvegarder**

5. **Vérifier** :
   - ✅ Volume = **50%**
   - ✅ Basses = **0**
   - ✅ Aigues = **0**

### Résultat attendu
✅ **Les valeurs audio doivent être identiques.**

---

## ✅ TEST #5 : Bascule mode DJ

### Étapes
1. **Ajuster les paramètres audio** :
   - Volume : **60%**
   - Basses : **+7**
   - Aigues : **+5**

2. **Ouvrir les Paramètres**

3. **Activer le mode DJ** :
   - Onglet **Général**
   - Cocher **"Mode Mixeur DJ (2 platines avec crossfader et contrôles DJ)"**

4. **Sauvegarder**

5. **AudioPlay redémarre en mode DJ**

6. **Basculer en mode simple** :
   - Cliquer sur **Paramètres** (dans FormDJ)
   - Décocher **"Mode Mixeur DJ"**
   - Sauvegarder

7. **AudioPlay redémarre en mode simple**

8. **Vérifier** :
   - ✅ Volume = **60%**
   - ✅ Basses = **+7**
   - ✅ Aigues = **+5**

### Résultat attendu
✅ **Les valeurs audio doivent être identiques après le retour en mode simple.**

---

## ✅ TEST #6 : Redémarrage d'AudioPlay

### Étapes
1. **Ajuster les paramètres audio** :
   - Volume : **75%**
   - Basses : **+18**
   - Aigues : **-12**

2. **Quitter AudioPlay** (fermer la fenêtre)

3. **Relancer AudioPlay**

4. **Vérifier immédiatement** :
   - ✅ Volume = **75%**
   - ✅ Basses = **+18**
   - ✅ Aigues = **-12**

### Résultat attendu
✅ **Les valeurs doivent être chargées correctement au démarrage.**

---

## ✅ TEST #7 : Changement de méthode BPM

### Étapes
1. **Ajuster les paramètres audio** :
   - Volume : **40%**
   - Basses : **-10**
   - Aigues : **+6**

2. **Ouvrir les Paramètres**

3. **Changer la méthode BPM** :
   - Onglet **Général**
   - Sélectionner une autre méthode de détection BPM

4. **Sauvegarder**

5. **Vérifier** :
   - ✅ Volume = **40%**
   - ✅ Basses = **-10**
   - ✅ Aigues = **+6**

### Résultat attendu
✅ **Les valeurs audio doivent être identiques.**

---

## ✅ TEST #8 : Activation normalisation volume

### Étapes
1. **Ajuster les paramètres audio** :
   - Volume : **90%**
   - Basses : **+20**
   - Aigues : **+20**

2. **Ouvrir les Paramètres**

3. **Désactiver la normalisation volume** :
   - Onglet **Général**
   - Décocher **"Normalisation volume activée"**

4. **Sauvegarder**

5. **Vérifier** :
   - ✅ Volume = **90%**
   - ✅ Basses = **+20**
   - ✅ Aigues = **+20**

### Résultat attendu
✅ **Les valeurs audio doivent être identiques.**

---

## ✅ TEST #9 : Suppression silence début/fin

### Étapes
1. **Ajuster les paramètres audio** :
   - Volume : **55%**
   - Basses : **-8**
   - Aigues : **+14**

2. **Ouvrir les Paramètres**

3. **Activer suppression silence** :
   - Onglet **Général**
   - Cocher **"Supprimer silence début"**
   - Cocher **"Supprimer silence fin"**

4. **Sauvegarder**

5. **Vérifier** :
   - ✅ Volume = **55%**
   - ✅ Basses = **-8**
   - ✅ Aigues = **+14**

### Résultat attendu
✅ **Les valeurs audio doivent être identiques.**

---

## ✅ TEST #10 : Modification multiple de paramètres

### Étapes
1. **Ajuster les paramètres audio** :
   - Volume : **65%**
   - Basses : **+11**
   - Aigues : **-7**

2. **Ouvrir les Paramètres**

3. **Modifier PLUSIEURS paramètres en même temps** :
   - Changer la langue
   - Changer le thème
   - Activer le métronome
   - Activer Reverb
   - Activer Echo
   - Changer la méthode BPM

4. **Sauvegarder**

5. **Vérifier** :
   - ✅ Volume = **65%**
   - ✅ Basses = **+11**
   - ✅ Aigues = **-7**

### Résultat attendu
✅ **Même avec de multiples changements, les valeurs audio doivent être identiques.**

---

## 🔍 Comment vérifier en mode Debug

Si vous exécutez AudioPlay en mode **Debug** dans Visual Studio :

1. **Ouvrir la Console de sortie** :
   - Menu **Affichage** → **Sortie**
   - Ou **Ctrl+Alt+O**

2. **Effectuer un test** (ex: changer la langue)

3. **Chercher les logs suivants** :

```
[FormParametres] ✅ PROTECTION: Valeurs récupérées depuis Form1: Volume=0.700, Basses=10.0, Aigues=8.0
[Form1] ✅ PROTECTION: Valeurs audio restaurées après ChargerParametres: Volume=0.700, Basses=10.0, Aigues=8.0
```

4. **Interprétation** :
   - ✅ Si ces logs apparaissent : **Protection active**
   - ❌ Si ces logs n'apparaissent pas : **Problème de configuration**

---

## 🐛 Que faire en cas d'échec d'un test ?

### Symptôme : Les valeurs reviennent à zéro
1. Vérifier que vous avez bien compilé la dernière version du code
2. Vérifier dans la Console de sortie (mode Debug) les logs de protection
3. Vérifier le contenu de `%AppData%\AudioPlay\parametres.txt`

### Symptôme : Les valeurs changent de manière aléatoire
1. Vérifier que `initialisationEnCours` est bien utilisé dans `AppliquerParametresAuxControles()`
2. Vérifier que `FormParametres.ShowDialog(Me)` est bien appelé avec `Me = Form1`

### Symptôme : Les valeurs ne se sauvegardent pas au démarrage
1. Vérifier que `ChargerParametres()` est bien appelé dans `Form1_Load()`
2. Vérifier que `parametres.txt` existe dans `%AppData%\AudioPlay\`

---

## 📊 Tableau récapitulatif des tests

| Test | Paramètre modifié | Volume | Basses | Aigues | Statut |
|------|-------------------|--------|--------|--------|--------|
| #1   | Langue            | 70%    | +10    | +8     | ☐ |
| #2   | Thème             | 30%    | -5     | +12    | ☐ |
| #3   | Métronome         | 85%    | +15    | -3     | ☐ |
| #4   | Effets audio      | 50%    | 0      | 0      | ☐ |
| #5   | Mode DJ           | 60%    | +7     | +5     | ☐ |
| #6   | Redémarrage       | 75%    | +18    | -12    | ☐ |
| #7   | Méthode BPM       | 40%    | -10    | +6     | ☐ |
| #8   | Normalisation     | 90%    | +20    | +20    | ☐ |
| #9   | Silence début/fin | 55%    | -8     | +14    | ☐ |
| #10  | Multi-changements | 65%    | +11    | -7     | ☐ |

**Cochez (✓) chaque test réussi. Si tous les tests sont cochés, la protection est fonctionnelle à 100% !**

---

## ✅ Validation finale

Si **tous les tests** sont réussis, vous pouvez considérer que :

✅ La protection Volume/Basses/Aigues est **ultra-robuste**
✅ Les valeurs ajustées par l'utilisateur sont **garanties de ne jamais changer**
✅ Le système est **prêt pour la production**

---

**Bon test ! 🎉**
