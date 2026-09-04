# Chargement automatique des métadonnées CD

## 🚀 Nouveau comportement simplifié

Le sélecteur de pistes CD charge maintenant **automatiquement** les métadonnées sans action de l'utilisateur !

---

## 📋 Ordre de chargement automatique

Lorsque vous ouvrez le sélecteur de pistes CD :

### 1️⃣ **Cache local** (instantané)
- ✅ Si le CD a déjà été chargé précédemment
- 💾 Affiche : `Cache: Artiste - Album`
- 🔵 Couleur : **Bleu**

### 2️⃣ **GnuDB** (automatique si pas de cache)
- 🔍 Recherche automatique sur GnuDB (même base qu'Exact Audio Copy)
- ⏱️ Prend quelques secondes
- 🟢 Si trouvé : `GnuDB : Artiste - Album` en **vert**
- 🔴 Si non trouvé : `GnuDB : CD non trouvé` en **rouge**

---

## 🎛️ ComboBox : Changement de source

Le ComboBox "Source" permet de **recharger** depuis une autre base de données :

| Source | Comportement | Quand l'utiliser |
|--------|-------------|------------------|
| **GnuDB** *(défaut)* | Automatique | Base principale (comme EAC) |
| **MusicBrainz** | Charge en cliquant | Si GnuDB ne trouve pas |
| **Discogs** | Demande artiste/album | Pour rechercher manuellement |
| **Saisie manuelle** | Formulaire d'édition | En dernier recours |

### Comment ça marche :

1. **Ouvrez le sélecteur** → GnuDB se charge automatiquement
2. **Si échec (rouge)** → changez la source dans le ComboBox
3. **Le chargement démarre automatiquement** dès que vous changez de source !

**Plus besoin de cliquer sur "Charger"** → c'est automatique ! 🎉

---

## 🔄 Workflow typique

### Scénario 1 : CD connu
```
1. Insérer le CD
2. Menu Ajout → CD Audio → Lecteur
3. ✅ Métadonnées affichées automatiquement (cache ou GnuDB)
4. Cocher les pistes
5. OK
```

### Scénario 2 : CD inconnu de GnuDB
```
1. Insérer le CD
2. Menu Ajout → CD Audio → Lecteur
3. 🔴 "GnuDB : CD non trouvé"
4. Changer source → "MusicBrainz"
5. ✅ ou 🔴 Résultat automatique
6. Si échec → "Discogs" ou "Saisie manuelle"
```

### Scénario 3 : CD déjà chargé une fois
```
1. Insérer le CD
2. Menu Ajout → CD Audio → Lecteur
3. 💾 "Cache: Paul McCartney - Never Stop Doing What You Love"
4. Cocher les pistes
5. OK
```

---

## 🗑️ Bouton "Effacer cache"

- **Activé** uniquement si des métadonnées sont en cache
- **Utilité** : forcer un rechargement depuis Internet
- **Après effacement** : recharge automatiquement depuis GnuDB

---

## 🔬 Bouton de test (🔬)

Le bouton 🔬 reste disponible pour **diagnostiquer** les problèmes :
- Affiche les DiscID calculés
- Teste toutes les sources
- Affiche les URL appelées
- Montre les erreurs détaillées

---

## 💡 Avantages du nouveau système

✅ **Plus simple** : pas de bouton "Charger" à cliquer  
✅ **Plus rapide** : chargement automatique dès l'ouverture  
✅ **Cache intelligent** : pas de rechargement inutile  
✅ **GnuDB par défaut** : même source qu'Exact Audio Copy  
✅ **Changement facile** : juste changer le ComboBox pour essayer une autre source  

---

## 🎯 Résumé

**Avant** :
1. Ouvrir le sélecteur
2. Choisir une source
3. Cliquer "Charger"
4. Attendre
5. Cocher les pistes

**Maintenant** :
1. Ouvrir le sélecteur → **métadonnées déjà là !**
2. Cocher les pistes

**Gain de temps : 2 clics en moins !** ⚡
