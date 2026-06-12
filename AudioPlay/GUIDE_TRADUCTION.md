# 📋 GUIDE DE TRADUCTION - AudioPlay DJ Mode

## ✅ Fichiers créés pour vous

### 1. `DJ_TRADUCTIONS_TEMPLATE.csv`
- **92 clés DJ** à traduire en Espagnol, Allemand et Italien
- Les clés marquées **"DÉJÀ TRADUIT"** existent déjà, mais **vérifiez-les quand même** car certaines traductions peuvent être incorrectes
- Format CSV avec point-virgule (`;`) comme séparateur
- Peut être ouvert dans Excel, LibreOffice Calc ou tout éditeur de texte

### 2. `FORMPARAMETRES_TRADUCTIONS.csv`
- **12 clés** pour les contrôles de FormParametres non traduits
- Inclut les traductions anglaises comme référence
- Nécessite traduction pour ES, DE, IT

---

## 📊 État actuel des traductions

| Langue | DJ Keys présentes | DJ Keys manquantes | % Complet |
|--------|-------------------|---------------------|-----------|
| 🇫🇷 Français | 92 | 0 | 100% ✅ |
| 🇬🇧 Anglais | 92 | 0 | 100% ✅ |
| 🇪🇸 Espagnol | 16 | **76** | 17% ⚠️ |
| 🇩🇪 Allemand | 67 | **25** | 73% ⚠️ |
| 🇮🇹 Italien | 67 | **25** | 73% ⚠️ |

---

## 🎯 Comment procéder

### Option A : Traduction automatique rapide (Google Translate, DeepL)
1. Ouvrez `DJ_TRADUCTIONS_TEMPLATE.csv` dans Excel
2. Copiez la colonne "Français (Référence)"
3. Utilisez DeepL ou Google Translate pour traduire le bloc complet
4. Collez les résultats dans les colonnes ES/DE/IT
5. **Relisez les traductions** et corrigez les termes techniques (BPM, Loop, Hot Cues, etc.)
6. Sauvegardez le fichier CSV

### Option B : Traduction manuelle (meilleure qualité)
1. Ouvrez `DJ_TRADUCTIONS_TEMPLATE.csv`
2. Traduisez ligne par ligne en suivant les instructions de la colonne "Instructions"
3. Respectez les placeholders `{0}`, `{1}`, les formats et les emojis
4. Sauvegardez régulièrement

### Option C : Mixte (recommandé)
1. Utilisez la traduction automatique pour un premier jet
2. Relisez et corrigez manuellement les entrées importantes
3. Accordez une attention particulière aux :
   - Termes DJ techniques (Cue, Loop, Pitch, BPM, Crossfader)
   - Messages d'erreur et de confirmation
   - Formats de chaîne avec `{0}`, `{1}`, etc.

---

## ⚠️ Règles CRITIQUES à respecter

### 1. **NE JAMAIS MODIFIER** :
- Les **placeholders** : `{0}`, `{1}`, `{0:F1}`, `{0:mm\:ss}`, etc.
- Les **formats spéciaux** : `{0:+0.0%;-0.0%;0.0%}` (pitch)
- Les **emojis** : 🎧, 📋, 🔄, ➕, etc.
- Les **symboles** : ▶, ⏸, ⏹, ⬇, →

### 2. **Termes techniques à conserver** (ou adapter légèrement) :
- BPM (Beats Per Minute) → reste BPM
- Loop → peut rester Loop
- Hot Cue → peut rester Hot Cue
- Pitch → peut rester Pitch
- Sync → peut rester Sync
- Crossfader → peut rester Crossfader ou adapter (fondeur croisé)
- Phaser → reste Phaser
- Reverb → reste Reverb
- Echo → reste Echo

### 3. **Formats CSV** :
- Séparateur : `;` (point-virgule)
- Si une traduction contient un `;`, encadrez-la de guillemets doubles `"`
- Si une traduction contient un `"`, doublez-le : `""`

### 4. **Cohérence** :
- Utilisez le même terme pour "Platine" / "Deck" partout
- Utilisez le même terme pour "Piste" / "Track" partout
- Gardez le même style de ponctuation

---

## 📝 Exemples de traductions

### ✅ BON :
```
DJ_DeckATitle;🎧 PLATINE A;🎧 DECK A;🎧 PLATO A;🎧 DECK A;🎧 PIATTO A
DJ_BPM_Value;BPM: {0:F1};BPM: {0:F1};BPM: {0:F1};BPM: {0:F1};BPM: {0:F1}
DJ_Loop_Active;Loop actif;Active loop;Loop activo;Schleife aktiv;Loop attivo
```

### ❌ MAUVAIS :
```
DJ_DeckATitle;🎧 PLATINE A;DECK A;PLATO A;DECK A;PIATTO A  ← emoji manquant
DJ_BPM_Value;BPM: 120.5;BPM: 120.5;BPM: 120.5;BPM: 120.5;BPM: 120.5  ← placeholder supprimé
DJ_Loop_Active;Loop actif;Active loop;Bucle activo;Schleife aktiv;Ciclo attivo  ← incohérent avec d'autres entrées Loop
```

---

## 🔄 Après avoir complété les traductions

### Envoyez-moi les fichiers CSV complétés et je vais :
1. ✅ Valider le format et les placeholders
2. ✅ Intégrer automatiquement les traductions dans les fichiers `.resx`
3. ✅ Compiler le projet pour vérifier qu'il n'y a pas d'erreurs
4. ✅ Tester que les traductions s'affichent correctement

---

## 💡 Conseils de traduction DJ

### Espagnol (ES)
- Platine → **Plato** ou **Deck**
- Piste → **Pista**
- Charger → **Cargar**
- Liste de lecture → **Lista de reproducción** ou **Playlist**

### Allemand (DE)
- Platine → **Plattenspieler** ou **Deck**
- Piste → **Titel** ou **Track**
- Charger → **Laden**
- Liste de lecture → **Wiedergabeliste** ou **Playlist**

### Italien (IT)
- Platine → **Giradischi** ou **Deck**
- Piste → **Traccia**
- Caricare → **Caricare**
- Lista di riproduzione → **Playlist**

---

## 📞 Besoin d'aide ?

Si vous avez des questions sur :
- Des termes techniques spécifiques
- Des formats à conserver
- Des traductions ambiguës

→ Demandez-moi avant de finaliser !

---

**Temps estimé** :
- Traduction automatique + révision : **1-2 heures**
- Traduction manuelle complète : **3-4 heures**
- Par langue (révision uniquement) : **30-45 minutes**
