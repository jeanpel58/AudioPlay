# Format d'affichage des pistes CD

## 📋 Dans le sélecteur de pistes

### Avec métadonnées :
```
01. Paul McCartney - Another Day [03:46]
02. Paul McCartney - Oh Woman, Oh Why [03:58]
03. Wings - Hi, Hi, Hi [03:10]
```

**Format** : `NN. Artiste - Titre [MM:SS]`

- **NN** = Numéro de piste (01, 02, 03...)
- **Artiste** = Artiste spécifique de la piste, ou artiste de l'album si pas spécifié
- **Titre** = Titre de la chanson
- **MM:SS** = Durée

### Sans métadonnées :
```
01. Track 01 [03:46]
02. Track 02 [03:58]
03. Track 03 [03:10]
```

---

## 🎵 Dans la ListView principale (playlist)

### Avec métadonnées :
```
💿 01. Paul McCartney - Another Day         03:46
💿 02. Paul McCartney - Oh Woman, Oh Why    03:58
💿 03. Wings - Hi, Hi, Hi                   03:10
```

**Format** : `💿 NN. Artiste - Titre`

### Sans métadonnées :
```
💿 Piste 01    03:46
💿 Piste 02    03:58
💿 Piste 03    03:10
```

---

## 🎯 Avantages

✅ **Cohérent** : même format partout  
✅ **Lisible** : artiste et titre clairement séparés  
✅ **Numéroté** : facile de retrouver la piste d'origine  
✅ **Durée visible** : entre crochets dans le sélecteur, colonne dans la playlist  

---

## 💾 Stockage des métadonnées

Chaque piste CD dans la playlist conserve :
- `CDTrack.Title` → Titre de la chanson
- `CDTrack.Artist` → Artiste/groupe
- `Tag["CDArtist"]` → Artiste pour l'interface
- `Tag["CDAlbum"]` → Album
- `Tag["CDDrive"]` → Lecteur source (D:, E:...)
- `Tag["CDTrackNumber"]` → Numéro de piste sur le CD

---

## 🔄 Cas particuliers

### Piste avec artiste invité :
Si la base de données indique un artiste différent pour une piste (ex: "featuring"), celui-ci sera affiché au lieu de l'artiste de l'album.

**Exemple** :
```
Album: Paul McCartney - "RAM"
Piste 5: Paul McCartney & Linda McCartney - Ram On

Affichage:
05. Paul McCartney & Linda McCartney - Ram On [02:52]
```

### Compilations / Albums multi-artistes :
Chaque piste affiche son propre artiste.

**Exemple** :
```
Album: "Now That's What I Call Music! 80"
Piste 1: Adele - Hello
Piste 2: Justin Bieber - Sorry

Affichage:
01. Adele - Hello [04:55]
02. Justin Bieber - Sorry [03:20]
```
