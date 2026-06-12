# Options de calcul et recalcul BPM

## Vue d'ensemble

Le menu BPM d'AudioPlay propose maintenant **quatre options distinctes** pour gérer le calcul du BPM de vos fichiers audio. Ces options permettent de contrôler précisément quand calculer et quand recalculer les BPM.

## Indicateur visuel de progression

Pendant le calcul du BPM, la **colonne BPM change de couleur** pour indiquer quel fichier est en cours de traitement :

- 🟠 **Fond orange** + texte "Calcul..." ou "Recalcul..." = Fichier en cours de traitement
- ⚪ **Fond blanc** + valeur BPM = Calcul terminé avec succès
- ⚪ **Fond blanc** + case vide = Calcul échoué

Cet indicateur visuel permet de suivre facilement la progression lors du calcul en masse de plusieurs fichiers.

## Accès au menu BPM

Cliquez sur le bouton **Calcul BPM** dans l'interface principale pour afficher le menu déroulant avec les quatre options.

---

## Options disponibles

### 1. **Calcul du BPM de l'item sélectionné**

**Comportement :**
- Calcule le BPM **uniquement si aucun BPM n'est affiché** dans la colonne BPM de la ListView
- Si un BPM est déjà présent dans la colonne, affiche un message d'information et ne fait rien
- Si le fichier possède un BPM dans ses métadonnées, propose de l'utiliser avant de calculer

**Utilisation :**
1. Sélectionnez un fichier dans la liste
2. Cliquez sur **Calcul BPM** → **Calcul du BPM de l'item sélectionné**
3. Si le fichier n'a pas de BPM affiché, le calcul démarre
4. Le BPM calculé est affiché dans la liste et sauvegardé dans les métadonnées

**Quand l'utiliser :**
- Première analyse d'un nouveau fichier
- Compléter les BPM manquants dans votre liste
- Éviter de recalculer inutilement des BPM existants

---

### 2. **Recalculer le BPM de l'item sélectionné**

**Comportement :**
- **Force le recalcul** du BPM même si un BPM est déjà affiché dans la colonne
- Demande confirmation avant de recalculer
- Écrase l'ancien BPM avec le nouveau
- Sauvegarde automatiquement le nouveau BPM dans les métadonnées

**Utilisation :**
1. Sélectionnez un fichier dans la liste
2. Cliquez sur **Calcul BPM** → **Recalculer le BPM de l'item sélectionné**
3. Confirmez le recalcul dans la boîte de dialogue
4. Le nouveau BPM remplace l'ancien

**Quand l'utiliser :**
- Vous pensez que le BPM affiché est incorrect
- Vous avez changé la méthode de calcul (SoundTouch → Librosa)
- Le fichier audio a été modifié/remixé
- Vous voulez vérifier la précision d'un BPM

---

### 3. **Calcul de tous les items de la liste...**

**Comportement :**
- Parcourt **tous les fichiers** de la liste
- Calcule le BPM **uniquement pour les fichiers sans BPM affiché** dans la colonne
- Ignore automatiquement les fichiers qui ont déjà un BPM dans la colonne
- Utilise les BPM existants dans les métadonnées si disponibles (évite de recalculer)
- Affiche un résumé détaillé à la fin

**Utilisation :**
1. Cliquez sur **Calcul BPM** → **Calcul de tous les items de la liste...**
2. L'application compte combien de fichiers n'ont pas de BPM
3. Choisissez si vous voulez sauvegarder dans les métadonnées :
   - **OUI** : Calcule ET sauvegarde dans les métadonnées
   - **NON** : Calcule sans sauvegarder dans les métadonnées
   - **ANNULER** : Annule l'opération
4. Le calcul démarre automatiquement

**Rapport final :**
- Fichiers traités
- Fichiers ignorés (BPM déjà présent)
- BPM détectés
- BPM existants utilisés (depuis métadonnées)
- Échecs
- Méthode utilisée (librosa ou SoundTouch)
- Statistiques de sauvegarde métadonnées

**Quand l'utiliser :**
- Compléter automatiquement les BPM manquants dans une grande liste
- Première analyse d'une nouvelle bibliothèque musicale
- Éviter de recalculer les BPM existants (gain de temps)

---

### 4. **Recalculer tous les items de la liste...**

**Comportement :**
- **Force le recalcul** du BPM de **TOUS les fichiers** de la liste
- Recalcule même les fichiers qui ont déjà un BPM affiché
- Ignore complètement les BPM existants (dans la colonne et dans les métadonnées)
- Demande confirmation car l'opération peut être longue
- Écrase tous les anciens BPM avec les nouveaux

**Utilisation :**
1. Cliquez sur **Calcul BPM** → **Recalculer tous les items de la liste...**
2. Confirmez le recalcul (message d'avertissement)
3. Choisissez si vous voulez sauvegarder dans les métadonnées :
   - **OUI** : Recalcule ET sauvegarde dans les métadonnées
   - **NON** : Recalcule sans sauvegarder dans les métadonnées
   - **ANNULER** : Annule l'opération
4. Le recalcul démarre automatiquement

**Rapport final :**
- Fichiers traités
- BPM détectés
- Échecs
- Méthode utilisée (librosa ou SoundTouch)
- Statistiques de sauvegarde métadonnées

**Quand l'utiliser :**
- Vous avez changé de méthode de calcul BPM (ex: passage de SoundTouch à Librosa)
- Vous voulez vérifier la précision de tous les BPM existants
- Vos fichiers audio ont été modifiés/remixés
- Vous pensez que plusieurs BPM sont incorrects

**⚠️ Attention :**
- Cette opération peut être **très longue** pour de grandes listes
- Tous les BPM existants seront **remplacés**

---

## Comparaison des options

| Option | Calcule si BPM absent | Calcule si BPM présent | Utilise métadonnées | Confirmation requise |
|--------|----------------------|------------------------|---------------------|---------------------|
| **Calcul item sélectionné** | ✅ Oui | ❌ Non | ✅ Oui (propose) | ❌ Non |
| **Recalcul item sélectionné** | ✅ Oui | ✅ Oui | ❌ Non | ✅ Oui |
| **Calcul tous les items** | ✅ Oui | ❌ Non | ✅ Oui (réutilise) | ❌ Non |
| **Recalcul tous les items** | ✅ Oui | ✅ Oui | ❌ Non | ✅ Oui |

---

## Stratégies recommandées

### Première analyse d'une bibliothèque
1. Utilisez **"Calcul de tous les items de la liste..."**
2. Choisissez **OUI** pour sauvegarder dans les métadonnées
3. Les BPM seront automatiquement disponibles la prochaine fois

### Compléter les BPM manquants
1. Ajoutez vos fichiers à la liste
2. Utilisez **"Calcul de tous les items de la liste..."**
3. Seuls les fichiers sans BPM seront traités

### Améliorer la précision avec Librosa
1. Installez Python/Librosa (si pas déjà fait)
2. Changez la méthode de calcul dans **Paramètres** → **Librosa uniquement**
3. Utilisez **"Recalculer tous les items de la liste..."**
4. Tous les BPM seront recalculés avec Librosa

### Vérifier un BPM suspect
1. Sélectionnez le fichier
2. Utilisez **"Recalculer le BPM de l'item sélectionné"**
3. Comparez l'ancien et le nouveau BPM

---

## Gestion des métadonnées

### Sauvegarde automatique
- Les options **"Calcul..."** sauvegardent toujours dans les métadonnées
- Les options **"Recalculer..."** demandent si vous voulez sauvegarder

### Réutilisation des métadonnées
- **"Calcul de tous les items..."** réutilise les BPM existants dans les métadonnées (évite de recalculer)
- **"Recalculer tous les items..."** ignore complètement les métadonnées et recalcule tout

### Avantages de la sauvegarde métadonnées
- ✅ BPM disponible immédiatement au prochain chargement
- ✅ Compatible avec d'autres logiciels audio
- ✅ Portable (voyage avec le fichier)
- ✅ Évite les recalculs inutiles

---

## Performance

### Indicateur visuel en temps réel

Pendant le traitement des fichiers :
- La case BPM du fichier en cours devient **orange** 🟠
- Le texte affiche "Calcul..." ou "Recalcul..."
- Une fois terminé, la case redevient **blanche** ⚪ avec la valeur BPM
- En cas d'erreur, la case redevient blanche et reste vide

Cela permet de :
- ✅ Voir exactement où en est le traitement
- ✅ Savoir quel fichier prend plus de temps
- ✅ Identifier rapidement les échecs (cases vides après traitement)

### Vitesse de calcul

**Un seul fichier :**
- SoundTouch : ~1-2 secondes
- Librosa : ~3-5 secondes

**100 fichiers :**
- Calcul initial : 5-10 minutes
- Réutilisation métadonnées : instantané

**Conseils :**
- Pour de grandes listes, lancez le calcul et laissez tourner
- L'affichage se met à jour tous les 5 fichiers
- La couleur orange vous permet de suivre la progression en temps réel
- Vous pouvez voir quel fichier est en cours de traitement
- Les cases vides après un traitement orange indiquent un échec du calcul

---

## Questions fréquentes

**Q: Quelle option utiliser pour ma première analyse ?**  
R: Utilisez **"Calcul de tous les items de la liste..."** avec sauvegarde métadonnées activée.

**Q: Comment éviter de recalculer des fichiers déjà analysés ?**  
R: Utilisez **"Calcul de tous les items de la liste..."** (pas "Recalculer"). Cette option ignore automatiquement les fichiers avec BPM.

**Q: Comment mettre à jour tous mes BPM avec Librosa ?**  
R: Changez la méthode dans Paramètres, puis utilisez **"Recalculer tous les items de la liste..."**.

**Q: Pourquoi "Calcul de tous les items" dit que tous les items ont déjà un BPM ?**  
R: Tous vos fichiers ont un BPM affiché dans la colonne. Utilisez **"Recalculer tous les items..."** pour forcer le recalcul.

**Q: Est-ce que les métadonnées sont écrasées ?**  
R: Oui, si vous recalculez un fichier qui a déjà un BPM dans ses métadonnées, l'ancien BPM sera remplacé par le nouveau.

---

## Notes techniques

- Les options de **calcul** vérifient la colonne BPM de la ListView pour décider de calculer ou non
- Les options de **recalcul** ignorent complètement la colonne BPM et forcent un nouveau calcul
- La méthode de calcul utilisée (Auto/Librosa/SoundTouch) dépend du paramètre choisi dans **Paramètres**
- Tous les calculs se font sur le fichier audio **complet** (pas seulement un extrait)
