# 🌍 AUDIT COMPLET MULTILANGUE - AudioPlay
## Date: 2026-05-31
## Objectif: Vérifier que TOUS les textes de l'application sont multilingues (FR/EN/ES/DE/IT)

---

## ❌ PROBLÈMES IDENTIFIÉS

### 🔴 CRITIQUE - Form1.vb

#### 1. **MessageBox Association de fichiers** (ligne 551-552)
```vb
MessageBox.Show(
	"AudioPlay n'est pas l'application par défaut pour les types suivants : " & String.Join(", ", nonAssocies) & vbCrLf &
```
**Action:** Créer clé `FileAssociation_NotDefault` dans les 5 langues

#### 2. **MessageBox Installation Python** (ligne 742-743)
```vb
Dim result = MessageBox.Show(
	"Python avec librosa permet un calcul de BPM très précis (95%+)." & vbCrLf & vbCrLf &
```
**Action:** Créer clé `BPM_PythonInstallPrompt` dans les 5 langues

#### 3. **Titre fenêtre Installation Python** (ligne 755)
```vb
progressForm.Text = "Installation de Python + librosa"
```
**Action:** Créer clé `BPM_PythonInstallTitle` dans les 5 langues

#### 4. **MessageBox Boucle active** (ligne 1878)
```vb
"Boucle active", MessageBoxButtons.OK, MessageBoxIcon.Information
```
**Action:** Créer clé `Loop_Active_Title` dans les 5 langues

#### 5. **MessageBox Confirmation suppression** (lignes 2880, 3029)
```vb
Dim rep = MessageBox.Show("Voulez-vous vraiment supprimer la sélection de la liste ?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
```
**Action:** Créer clés `Playlist_DeleteConfirm_Message` et `Confirmation_Title` dans les 5 langues

#### 6. **Menu contextuel ListView** (lignes 3007, 3010, 3013)
```vb
Dim itemCalculerBPM As New ToolStripMenuItem("Calculer le BPM")
Dim itemAfficherMetadonnees As New ToolStripMenuItem("Afficher les métadonnées")
Dim itemSupprimerDeListe As New ToolStripMenuItem("Supprimer de la liste")
```
**Action:** Créer clés `Context_CalculateBPM`, `Context_ShowMetadata`, `Context_RemoveFromList` dans les 5 langues

#### 7. **MessageBox Boucle non définie** (lignes 3781-3782)
```vb
MessageBox.Show("Aucune boucle n'a été définie. Utilisez 'I' pour le début et 'O' pour la fin.",
			  "Boucle non définie", MessageBoxButtons.OK, MessageBoxIcon.Information)
```
**Action:** Créer clés `Loop_NotDefined_Message` et `Loop_NotDefined_Title` dans les 5 langues

#### 8. **Titre de la Form** (ligne 590)
```vb
Me.Text = "AudioPlay v" & Version
```
**Action:** Créer clé `App_Title` dans les 5 langues

---

### 🟡 MOYEN - FormParametres.vb

#### 9. **Description FolderBrowserDialog** (ligne 328)
```vb
fbd.Description = "Sélectionner le répertoire par défaut"
```
**Action:** Créer clé `Folder_SelectDefaultDirectory` dans les 5 langues

---

### 🟡 MOYEN - FormKaraoke.vb

#### 10. **Titre fenêtre Karaoke** (ligne 21)
```vb
Me.Text = "AudioPlay Karaoke"
```
**Action:** Utiliser la clé existante `Karaoke_WindowTitle`

---

### 🟢 MINEURS - Form1.Designer.vb (Textes par défaut du Designer)

Ces textes sont écrasés au runtime par `RefreshLanguage()`, mais il serait plus propre de les vider:

#### 11. **Textes de boutons dans Designer** (lignes 371, 387, 403, 418, 430, 444, 535, 566)
```vb
Button_PauseReprise.Text = "Pause"
Button_CalculBPM.Text = "BPM"
Button_Mute.Text = "Mute"
Button_InfoSelect.Text = "Info"
Button_Ajout.Text = "Ajout"
Button_Playlist.Text = "Playlist"
Button_Parametres.Text = "Param"
Button_Loop_Aide.Text = "Aide"
```
**Action:** Mettre tous les `.Text = ""` dans le Designer (sauf Button_Loop_Aide qui est géré)

#### 12. **En-têtes de colonnes ListView** (lignes 473, 479, 485, 490)
```vb
Num.Text = "#"
Chansons.Text = "Chansons"
BPM.Text = "BPM"
Durée.Text = "Durée"
```
**Action:** Créer clés `ListView_Column_Num`, `ListView_Column_Songs`, `ListView_Column_BPM`, `ListView_Column_Duration` dans les 5 langues, puis les appliquer dans `RefreshLanguage()`

---

## ✅ BIEN LOCALISÉS (À CONSERVER)

- ✅ FormMetadonnees.vb - Tous les MessageBox utilisent LanguageManager
- ✅ FormParametres.vb - La plupart des MessageBox utilisent LanguageManager
- ✅ Form_APropos.vb - Utilise LanguageManager
- ✅ Button_Loop_Aide - Le texte est traduit via `Button_Help`
- ✅ Aides HTML - Tous les fichiers HTML existent en 5 langues

---

## 📋 CLÉS À AJOUTER AUX RESSOURCES

### Nouvelles clés nécessaires:

1. `App_Title` = "AudioPlay v{0}"
2. `FileAssociation_NotDefault` = "AudioPlay n'est pas l'application par défaut pour les types suivants : {0}\n\nVoulez-vous configurer les associations maintenant ?"
3. `BPM_PythonInstallPrompt` = "Python avec librosa permet un calcul de BPM très précis (95%+).\n\nVoulez-vous installer Python et librosa maintenant ?\n\nNote: L'installation prendra quelques minutes."
4. `BPM_PythonInstallTitle` = "Installation de Python + librosa"
5. `Loop_Active_Title` = "Boucle active"
6. `Loop_NotDefined_Message` = "Aucune boucle n'a été définie. Utilisez 'I' pour le début et 'O' pour la fin."
7. `Loop_NotDefined_Title` = "Boucle non définie"
8. `Playlist_DeleteConfirm_Message` = "Voulez-vous vraiment supprimer la sélection de la liste ?"
9. `Confirmation_Title` = "Confirmation"
10. `Context_CalculateBPM` = "Calculer le BPM"
11. `Context_ShowMetadata` = "Afficher les métadonnées"
12. `Context_RemoveFromList` = "Supprimer de la liste"
13. `Folder_SelectDefaultDirectory` = "Sélectionner le répertoire par défaut"
14. `ListView_Column_Num` = "#"
15. `ListView_Column_Songs` = "Chansons"
16. `ListView_Column_BPM` = "BPM"
17. `ListView_Column_Duration` = "Durée"

---

## 🎯 PLAN D'ACTION

### Phase 1: Ajouter les clés manquantes aux fichiers de ressources
1. ✅ Resources.resx (Français) - **TERMINÉ**
2. ✅ Resources.en.resx (Anglais) - **TERMINÉ**
3. ✅ Resources.es.resx (Espagnol) - **TERMINÉ**
4. ✅ Resources.de.resx (Allemand) - **TERMINÉ**
5. ✅ Resources.it.resx (Italien) - **TERMINÉ**

### Phase 2: Modifier le code VB
1. ✅ Form1.vb - **TERMINÉ** - Tous les textes en dur remplacés par LanguageManager.GetString()
2. ✅ FormParametres.vb - **TERMINÉ** - Clé FolderBrowserDialog ajoutée
3. ✅ FormKaraoke.vb - **TERMINÉ** - Titre de fenêtre utilise la clé existante
4. ⚠️ Form1.Designer.vb - Textes par défaut conservés (écrasés au runtime, pas critique)

### Phase 3: Ajouter RefreshLanguage pour les colonnes ListView et menu contextuel
1. ✅ Colonnes ListView déjà gérées dans RefreshLanguage()
2. ✅ Menu contextuel recréé à chaque changement de langue - **AJOUTÉ**

### Phase 4: Tests
1. 🔄 À tester dans chaque langue
2. 🔄 Vérifier tous les MessageBox
3. 🔄 Vérifier tous les titres de fenêtres
4. 🔄 Vérifier les menus contextuels

---

## 📝 NOTES IMPORTANTES

- **LanguageManager.GetString()** supporte les paramètres de format: `GetString("Key", param1, param2)`
- **RefreshLanguage()** doit être appelée à chaque changement de langue
- Les textes du **Designer** sont écrasés au runtime mais devraient être vides pour plus de clarté
- Les **colonnes ListView** doivent être mises à jour dans `RefreshLanguage()`

---

## 🌐 TRADUCTIONS SUGGÉRÉES

### FileAssociation_NotDefault
- 🇫🇷 FR: "AudioPlay n'est pas l'application par défaut pour les types suivants : {0}\n\nVoulez-vous configurer les associations maintenant ?"
- 🇬🇧 EN: "AudioPlay is not the default application for the following types: {0}\n\nDo you want to configure file associations now?"
- 🇪🇸 ES: "AudioPlay no es la aplicación predeterminada para los siguientes tipos: {0}\n\n¿Desea configurar las asociaciones ahora?"
- 🇩🇪 DE: "AudioPlay ist nicht die Standardanwendung für die folgenden Typen: {0}\n\nMöchten Sie die Dateizuordnungen jetzt konfigurieren?"
- 🇮🇹 IT: "AudioPlay non è l'applicazione predefinita per i seguenti tipi: {0}\n\nVuoi configurare le associazioni ora?"

### BPM_PythonInstallPrompt
- 🇫🇷 FR: "Python avec librosa permet un calcul de BPM très précis (95%+).\n\nVoulez-vous installer Python et librosa maintenant ?\n\nNote: L'installation prendra quelques minutes."
- 🇬🇧 EN: "Python with librosa enables highly accurate BPM calculation (95%+).\n\nDo you want to install Python and librosa now?\n\nNote: Installation will take a few minutes."
- 🇪🇸 ES: "Python con librosa permite un cálculo de BPM muy preciso (95%+).\n\n¿Desea instalar Python y librosa ahora?\n\nNota: La instalación tomará unos minutos."
- 🇩🇪 DE: "Python mit librosa ermöglicht eine sehr präzise BPM-Berechnung (95%+).\n\nMöchten Sie Python und librosa jetzt installieren?\n\nHinweis: Die Installation dauert einige Minuten."
- 🇮🇹 IT: "Python con librosa consente un calcolo BPM molto preciso (95%+).\n\nVuoi installare Python e librosa ora?\n\nNota: L'installazione richiederà alcuni minuti."

### Loop_NotDefined_Message
- 🇫🇷 FR: "Aucune boucle n'a été définie. Utilisez 'I' pour le début et 'O' pour la fin."
- 🇬🇧 EN: "No loop has been defined. Use 'I' for the start and 'O' for the end."
- 🇪🇸 ES: "No se ha definido ningún bucle. Use 'I' para el inicio y 'O' para el final."
- 🇩🇪 DE: "Es wurde keine Schleife definiert. Verwenden Sie 'I' für den Anfang und 'O' für das Ende."
- 🇮🇹 IT: "Nessun loop è stato definito. Usa 'I' per l'inizio e 'O' per la fine."

### Context Menu Items
**Context_CalculateBPM:**
- 🇫🇷 FR: "Calculer le BPM"
- 🇬🇧 EN: "Calculate BPM"
- 🇪🇸 ES: "Calcular BPM"
- 🇩🇪 DE: "BPM berechnen"
- 🇮🇹 IT: "Calcola BPM"

**Context_ShowMetadata:**
- 🇫🇷 FR: "Afficher les métadonnées"
- 🇬🇧 EN: "Show metadata"
- 🇪🇸 ES: "Mostrar metadatos"
- 🇩🇪 DE: "Metadaten anzeigen"
- 🇮🇹 IT: "Mostra metadati"

**Context_RemoveFromList:**
- 🇫🇷 FR: "Supprimer de la liste"
- 🇬🇧 EN: "Remove from list"
- 🇪🇸 ES: "Eliminar de la lista"
- 🇩🇪 DE: "Aus Liste entfernen"
- 🇮🇹 IT: "Rimuovi dalla lista"

### ListView Columns
**ListView_Column_Songs:**
- 🇫🇷 FR: "Chansons"
- 🇬🇧 EN: "Songs"
- 🇪🇸 ES: "Canciones"
- 🇩🇪 DE: "Lieder"
- 🇮🇹 IT: "Canzoni"

**ListView_Column_Duration:**
- 🇫🇷 FR: "Durée"
- 🇬🇧 EN: "Duration"
- 🇪🇸 ES: "Duración"
- 🇩🇪 DE: "Dauer"
- 🇮🇹 IT: "Durata"

---

## ✨ RÉSUMÉ

**Total de textes en dur trouvés:** 17
**Fichiers affectés:** 4 (Form1.vb, FormParametres.vb, FormKaraoke.vb, Form1.Designer.vb)
**Nouvelles clés à créer:** 17
**Langues à traiter:** 5 (FR, EN, ES, DE, IT)

**Prochaine étape:** Commencer Phase 1 - Ajout des clés dans les fichiers de ressources
