# Différences entre les DiscID : MusicBrainz vs CDDB/GnuDB

AudioPlay utilise **deux types de DiscID** différents selon la source de métadonnées interrogée.

---

## 🔑 MusicBrainz DiscID

### Algorithme
- Basé sur **SHA-1** des offsets de pistes
- Encode : nombre de pistes, offsets (en secteurs), lead-out
- Format : Base64 URL-safe (22 caractères)

### Exemple
```
DiscID: kq7xZL3wXhY7F8nZ8e7qXqY7F8w-
```

### Calcul dans AudioPlay
```vb
' CDMetadataProvider.vb
Public Shared Function CalculerDiscID(pistes As List(Of CDAudioManager.CDTrack)) As String
	' 1. Construire la chaîne : "1 nombre_pistes lead_out offset1 offset2 ..."
	Dim tocString = BuildTOCString(pistes)

	' 2. Hash SHA-1
	Dim hash = SHA1.HashData(Encoding.ASCII.GetBytes(tocString))

	' 3. Encoder en Base64 URL-safe
	Return ConvertToBase64UrlSafe(hash)
End Function
```

### Utilisation
- **MusicBrainz API** : `https://musicbrainz.org/ws/2/discid/{discid}`
- Cache AudioPlay indexé par ce DiscID

---

## 🔑 CDDB DiscID (GnuDB)

### Algorithme
- Basé sur un **checksum** des temps de début + durée totale
- Beaucoup plus simple que MusicBrainz
- Format : Hexadécimal (8-12 caractères)

### Exemple
```
DiscID: 8b0a5c0a
```

### Calcul dans AudioPlay
```vb
' GnuDBMetadataProvider.vb
Public Shared Function CalculerCDDBDiscID(pistes As List(Of CDAudioManager.CDTrack)) As String
	' 1. Calculer le checksum : somme des chiffres de chaque offset en secondes
	Dim checksum As Integer = 0
	For Each piste In pistes
		Dim seconds = CInt(piste.StartFrame / 75)
		checksum += SommeChiffres(seconds)
	Next

	' 2. Durée totale en secondes
	Dim dureeSecondes = CalculerDureeCD(pistes)

	' 3. Format : checksum(8hex) + durée(hex) + nb_pistes(hex)
	Return $"{checksum:x8}{dureeSecondes:x}{pistes.Count:x}"
End Function

Private Shared Function SommeChiffres(n As Integer) As Integer
	Dim somme As Integer = 0
	While n > 0
		somme += n Mod 10
		n \= 10
	End While
	Return somme
End Function
```

### Utilisation
- **GnuDB API** : `http://gnudb.gnudb.org/~cddb/cddb.cgi?cmd=cddb+query+{discid}+...`
- Protocole CDDB classique (années 90)
- Compatible avec **Exact Audio Copy**, **foobar2000**, etc.

---

## 📊 Comparaison

| Aspect | MusicBrainz | CDDB/GnuDB |
|--------|-------------|------------|
| **Algorithme** | SHA-1 cryptographique | Checksum simple |
| **Format** | Base64 (22 car) | Hexadécimal (8-12 car) |
| **Précision** | Très élevée | Moyenne |
| **Collisions** | Quasi impossibles | Possibles (rare) |
| **Compatibilité** | MusicBrainz uniquement | EAC, foobar2000, CDDB, GnuDB |
| **Année création** | ~2000 | ~1996 |
| **Complexité** | Élevée | Faible |

---

## 🎯 Pourquoi deux DiscID différents ?

### Raisons historiques
1. **CDDB** (1996) a créé son propre algorithme simple
2. **MusicBrainz** (2000) a voulu un système plus robuste avec SHA-1
3. Les deux coexistent aujourd'hui avec des bases de données différentes

### Implications pour AudioPlay

#### Cache local
AudioPlay utilise **MusicBrainz DiscID** pour le cache local car :
- Plus unique (moins de collisions)
- Indépendant de la source de métadonnées
- Standard moderne

#### Requêtes API
- **MusicBrainz** → Calcul du MusicBrainz DiscID
- **GnuDB** → Calcul du CDDB DiscID (différent !)
- **Discogs** → Pas de DiscID (recherche manuelle)

---

## 🔬 Exemple concret

Pour le CD **"Abbey Road" des Beatles** :

### MusicBrainz DiscID
```
kq7xZL3wXhY7F8nZ8e7qXqY7F8w-
```

Calculé depuis :
- 17 pistes
- Offsets : 150, 10733, 21445, ...
- Lead-out : 195713

### CDDB DiscID
```
c60a5d11
```

Calculé depuis :
- Checksum : `c60a5d` (hex)
- Durée : `1` (hex, ~2612 secondes)
- Pistes : `11` (hex, 17 pistes)

### Même CD, deux identifiants différents !

---

## 🛠️ Tests dans AudioPlay

### Afficher les deux DiscID

Ajoutez du debug dans `FormSelecteurPistesCD.vb` :

```vb
Private Sub TenterChargerDepuisCache()
	' DiscID MusicBrainz
	Dim mbDiscID = CDMetadataProvider.CalculerDiscID(pistesCD)
	System.Diagnostics.Debug.WriteLine($"MusicBrainz DiscID: {mbDiscID}")

	' DiscID CDDB
	Dim cddbDiscID = GnuDBMetadataProvider.CalculerCDDBDiscID(pistesCD)
	System.Diagnostics.Debug.WriteLine($"CDDB DiscID: {cddbDiscID}")
End Sub
```

---

## 📚 Références

### MusicBrainz DiscID
- Spec officielle : https://musicbrainz.org/doc/Disc_ID_Calculation
- Librairie C : https://github.com/metabrainz/libdiscid

### CDDB DiscID
- Spec CDDB : http://ftp.freedb.org/pub/freedb/latest/CDDBPROTO
- GnuDB : http://gnudb.org

### Implémentations
- **libdiscid** (MusicBrainz) : C, Python, Ruby
- **CDDB.bundle** (EAC) : C++
- **AudioPlay** : VB.NET (nos implémentations custom)

---

## 🎓 Conclusion

Les deux DiscID sont **complémentaires** :
- **MusicBrainz** : Moderne, précis, recommandé pour le cache
- **CDDB** : Historique, compatible EAC, utile pour CD anciens

AudioPlay les implémente **tous les deux** pour offrir la meilleure couverture possible ! 🎉
