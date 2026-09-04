# Guide des sources de métadonnées CD

AudioPlay propose **4 sources** pour récupérer les informations (artiste, album, titres) des CD audio, avec un **système de cache automatique** pour ne pas avoir à recharger les métadonnées à chaque session.

## 🔄 Cache automatique

**Nouveauté** : Les métadonnées sont maintenant **sauvegardées automatiquement** dans un cache local :
- 📁 Fichier : `%APPDATA%\AudioPlay\cd_metadata_cache.json`
- 🔑 Indexation par **DiscID** (identifiant unique calculé depuis la table des matières du CD)
- ⚡ Chargement instantané lors des prochaines sessions
- 🗑️ Option "Effacer cache" pour forcer un rechargement

### Fonctionnement
1. **Première insertion** : Vous choisissez une source et chargez les métadonnées
2. **Sauvegarde automatique** : Les informations sont mises en cache
3. **Sessions suivantes** : Les métadonnées sont **chargées automatiquement** depuis le cache
4. **Mise à jour** : Utilisez "Effacer cache" puis rechargez depuis une source différente

### 💬 Messages persistants
**Nouveau** : Les messages de chargement (succès/erreur) **restent affichés en permanence** pour faciliter le débogage.

**Codes de couleur** :
- 🟢 **Vert** : Métadonnées chargées avec succès
- 🔴 **Rouge** : Erreur ou CD non trouvé
- 🟠 **Orange** : Cache effacé
- 🔵 **Bleu** : Recherche en cours...
- ⚪ **Gris** : Opération annulée

👉 **Conseil** : Si une source échoue (message rouge), essayez une autre source avec le ComboBox !

---

## 1. MusicBrainz (Recommandé)
- **Avantages** : Base de données open-source, gratuite, très complète, bien maintenue
- **Inconvénients** : Certains CD ne sont pas référencés (retourne "404 NotFound")
- **Utilisation** : 
  - Sélectionner "MusicBrainz" dans la liste déroulante
  - Cliquer sur "Charger"
  - Les métadonnées sont récupérées automatiquement via le DiscID MusicBrainz

## 2. GnuDB (CDDB) ✨ NOUVEAU
- **Avantages** : 
  - Successeur officiel de FreeDB (fermé en 2020)
  - Protocole CDDB classique (utilisé par Exact Audio Copy)
  - Gratuit, aucune clé API nécessaire
  - Base de données communautaire importante
- **Inconvénients** : Moins moderne que MusicBrainz
- **Utilisation** :
  - Sélectionner "GnuDB" dans la liste déroulante
  - Cliquer sur "Charger"
  - Les métadonnées sont récupérées automatiquement via le CDDB DiscID

### À propos de GnuDB
GnuDB est le remplaçant direct de FreeDB qui a fermé en mars 2020. Il utilise le même protocole CDDB et contient la plupart des entrées de l'ancienne base FreeDB. C'est la même source utilisée par **Exact Audio Copy (EAC)** depuis 2020.

## 3. Discogs
- **Avantages** : Base de données gigantesque, excellente couverture
- **Inconvénients** : Nécessite une clé API (à configurer dans `DiscogsMetadataProvider.vb`)
- **Utilisation** :
  - Sélectionner "Discogs" dans la liste déroulante
  - Cliquer sur "Charger"
  - Entrer l'artiste et l'album dans le formulaire
  - AudioPlay recherche et affiche les résultats

### Configuration de la clé API Discogs
1. Créer un compte sur https://www.discogs.com
2. Aller dans Settings → Developers
3. Créer une nouvelle application
4. Copier la clé et le secret
5. Les coller dans `DiscogsMetadataProvider.vb` :
   ```vb
   Private Const DISCOGS_API_KEY As String = "votre_clé_ici"
   Private Const DISCOGS_API_SECRET As String = "votre_secret_ici"
   ```

## 4. Saisie manuelle
- **Avantages** : Toujours disponible, permet de corriger/personnaliser
- **Utilisation** :
  - Sélectionner "Saisie manuelle"
  - Cliquer sur "Charger"
  - Remplir le formulaire avec :
	- Artiste (album)
	- Titre de l'album
	- Titres individuels des pistes
	- Artiste par piste (optionnel)
  - Les données sont **sauvegardées en cache** comme les autres sources

---

## 📋 Recommandations d'utilisation

### Workflow optimal
1. **Insérer le CD** → AudioPlay détecte le lecteur
2. **Ouvrir le sélecteur** → Menu "Ajout" → Lecteur CD
3. **Premier chargement** :
   - Si le cache existe déjà : métadonnées affichées automatiquement en bleu
   - Sinon : choisir une source et cliquer "Charger"
4. **Sessions futures** : métadonnées chargées instantanément depuis le cache

### Par type de CD
- **CD commercial standard** : MusicBrainz ou GnuDB (tous deux gratuits, pas de clé API)
- **CD rare/régional** : Essayer GnuDB puis Discogs
- **CD ancien (années 90-2000)** : GnuDB a souvent de meilleures données historiques
- **CD maison/bootleg** : Utiliser la saisie manuelle
- **Erreur 404 sur MusicBrainz** : Essayer GnuDB puis Discogs ou saisie manuelle

### Ordre de préférence recommandé
1. **MusicBrainz** - Le plus moderne et complet
2. **GnuDB** - Bonne alternative, surtout pour CD anciens
3. **Discogs** - Si les deux premiers échouent (nécessite clé API)
4. **Saisie manuelle** - En dernier recours ou pour personnaliser

### Gestion du cache

### Workflow optimal
1. **Insérer le CD** → AudioPlay détecte le lecteur
2. **Ouvrir le sélecteur** → Menu "Ajout" → Lecteur CD
3. **Premier chargement** :
   - Si le cache existe déjà : métadonnées affichées automatiquement en bleu
   - Sinon : choisir une source et cliquer "Charger"
4. **Sessions futures** : métadonnées chargées instantanément depuis le cache

### Par type de CD
- **CD populaire** : MusicBrainz suffit généralement
- **CD rare/régional** : Essayer Discogs avec recherche manuelle
- **CD maison/bootleg** : Utiliser la saisie manuelle
- **Erreur 404 sur MusicBrainz** : Passer à Discogs ou saisie manuelle

### Gestion du cache
- **Voir l'info du cache** : Le label affiche "Cache: [Artiste] - [Album]" en bleu
- **Corriger des métadonnées** : 
  1. Cliquer sur "Effacer cache"
  2. Choisir une autre source
  3. Recharger
- **Réinitialiser tout** : Supprimer `%APPDATA%\AudioPlay\cd_metadata_cache.json`

---

## 🗂️ Structure du cache

Le fichier `cd_metadata_cache.json` contient :
```json
[
  {
	"DiscID": "abc123def456...",
	"Artist": "Paul McCartney",
	"Album": "Never Stop Doing What You Love",
	"Year": 2023,
	"Source": "Discogs",
	"DateAdded": "2026-01-15T10:30:00",
	"Tracks": [
	  {
		"TrackNumber": 1,
		"Title": "My Valentine",
		"Artist": "",
		"DurationTicks": 2340000000
	  },
	  ...
	]
  }
]
```

### Informations stockées
- **DiscID** : Identifiant unique du CD (SHA-1 des offsets TOC)
- **Source** : `MusicBrainz`, `Discogs`, ou `Manual`
- **DateAdded** : Date d'ajout au cache
- **Tracks** : Liste complète des pistes avec titres et durées

---

## 🔧 Développement futur

### Prochaines améliorations possibles
- ✅ Cache local (implémenté)
- 🔄 Synchronisation cloud (Google Drive, OneDrive)
- 🎨 Téléchargement automatique des pochettes d'album
- 📝 Édition inline des métadonnées dans la liste
- 🔍 Recherche fuzzy pour Discogs (correction orthographique)
- 🌐 Autres fournisseurs : CDDB, Gracenote, Last.fm
