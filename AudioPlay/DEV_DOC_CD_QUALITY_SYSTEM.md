# 🔧 Documentation Technique - Système de Qualité Dynamique CD

## 📐 Architecture

Le système de qualité dynamique permet d'adapter automatiquement les options de qualité d'extraction selon le format audio sélectionné.

---

## 🏗️ Composants principaux

### 1. **ComboBoxTypeConversion**
- **Type** : `ComboBox` (DropDownList)
- **Valeurs** : `"MP3"`, `"Flac"`, `"Wav"`, `"Wma"`
- **Rôle** : Sélection du format d'extraction
- **Source** : Défini dans `FormCompresser.Designer.vb`

### 2. **ComboBoxQualiteConversion**
- **Type** : `ComboBox` (DropDownList)
- **Valeurs** : Dynamiques, dépendent du format sélectionné
- **Rôle** : Sélection de la qualité/bitrate/compression
- **Source** : Rempli dynamiquement par `ComboBoxTypeConversion_SelectedIndexChanged`

### 3. **ComboBoxTypeConversion_SelectedIndexChanged**
- **Type** : Gestionnaire d'événement
- **Déclenchement** : Lorsque l'utilisateur change le format
- **Actions** :
  1. Lit le format sélectionné
  2. Vide `ComboBoxQualiteConversion`
  3. Remplit avec les options appropriées
  4. Sélectionne la valeur par défaut

---

## 🔄 Flux de données

```
[Utilisateur sélectionne format]
		   ↓
[ComboBoxTypeConversion_SelectedIndexChanged déclenché]
		   ↓
[Lecture du format avec .ToUpper()]
		   ↓
[Select Case sur le format]
		   ↓
[Vidage de ComboBoxQualiteConversion]
		   ↓
[Remplissage avec les nouvelles options]
		   ↓
[Sélection de l'option par défaut]
		   ↓
[RecalculerTailles déclenché]
		   ↓
[Mise à jour du ListView avec nouvelles estimations]
```

---

## 📝 Mappage des options

### MP3
```vb
Case "MP3"
	ComboBoxQualiteConversion.Items.Add("Basse (128 kbps)")
	ComboBoxQualiteConversion.Items.Add("Moyenne (192 kbps)")
	ComboBoxQualiteConversion.Items.Add("Haute (256 kbps)")
	ComboBoxQualiteConversion.Items.Add("Très haute (320 kbps)")
	ComboBoxQualiteConversion.SelectedIndex = 3  ' Très haute par défaut
```

**Interprétation dans `ExtraireMp3`** :
```vb
Select Case qualite
	Case "Très haute (320 kbps)" → bitrate = 320
	Case "Haute (256 kbps)" → bitrate = 256
	Case "Moyenne (192 kbps)" → bitrate = 192
	Case "Basse (128 kbps)" → bitrate = 128
End Select
```

**Calcul de taille** :
```vb
Taille_MB = (bitrate × durée_secondes) / 8 / 1024
```

---

### FLAC
```vb
Case "FLAC"
	ComboBoxQualiteConversion.Items.Add("Niveau 0 (rapide)")
	ComboBoxQualiteConversion.Items.Add("Niveau 5 (équilibré)")
	ComboBoxQualiteConversion.Items.Add("Niveau 8 (meilleur)")
	ComboBoxQualiteConversion.SelectedIndex = 2  ' Niveau 8 par défaut
```

**Interprétation dans `ExtraireFlac`** :
```vb
Select Case qualite
	Case "Niveau 8 (meilleur)" → compressionLevel = 8
	Case "Niveau 5 (équilibré)" → compressionLevel = 5
	Case "Niveau 0 (rapide)" → compressionLevel = 0
End Select
```

**Arguments FFMpeg** :
```bash
ffmpeg -i input.wav -compression_level {compressionLevel} output.flac
```

**Calcul de taille** :
```vb
' Estimation : FLAC = 50-60% du WAV selon le niveau
debitKbps = CInt(1411.2 * 0.5)  ' Pour niveau 8
```

---

### WAV
```vb
Case "WAV"
	ComboBoxQualiteConversion.Items.Add("PCM 16-bit 44.1 kHz")
	ComboBoxQualiteConversion.Items.Add("PCM 24-bit 96 kHz")
	ComboBoxQualiteConversion.Items.Add("PCM 32-bit 192 kHz")
	ComboBoxQualiteConversion.SelectedIndex = 1  ' 24-bit 96 kHz par défaut
```

**Interprétation dans `ExtraireWav`** :
```vb
Select Case qualite
	Case "PCM 32-bit 192 kHz" → sampleRate = 192000, bitDepth = 32
	Case "PCM 24-bit 96 kHz" → sampleRate = 96000, bitDepth = 24
	Case "PCM 16-bit 44.1 kHz" → sampleRate = 44100, bitDepth = 16
End Select
```

**Conversion avec resampling** :
```vb
Dim targetFormat As New WaveFormat(sampleRate, bitDepth, channels)
Using resampler As New MediaFoundationResampler(source, targetFormat)
	resampler.ResamplerQuality = 60  ' Haute qualité
	' ... copie vers fichier WAV
End Using
```

**Calcul de taille** :
```vb
debitKbps = (sampleRate × bitDepth × channels) / 1000
Exemple : (96000 × 24 × 2) / 1000 = 4608 kbps
```

---

### WMA
```vb
Case "WMA"
	ComboBoxQualiteConversion.Items.Add("128 kbps")
	ComboBoxQualiteConversion.Items.Add("192 kbps")
	ComboBoxQualiteConversion.Items.Add("256 kbps")
	ComboBoxQualiteConversion.SelectedIndex = 2  ' 256 kbps par défaut
```

**Interprétation dans `ExtraireWma`** :
```vb
Select Case qualite
	Case "256 kbps" → bitrate = 256
	Case "192 kbps" → bitrate = 192
	Case "128 kbps" → bitrate = 128
End Select
```

**Arguments FFMpeg** :
```bash
ffmpeg -i input.wav -codec:a wmav2 -b:a {bitrate}k output.wma
```

**Calcul de taille** :
```vb
Taille_MB = (bitrate × durée_secondes) / 8 / 1024
```

---

## 🔗 Chaîne d'appel

### Lors de la sélection du format
```
FormCompresser_Load()
  ↓
ComboBoxTypeConversion.SelectedIndex = mp3Index
  ↓
[EVENT] ComboBoxTypeConversion_SelectedIndexChanged
  ↓
Remplir ComboBoxQualiteConversion selon format
  ↓
[EVENT] ComboBoxQualiteConversion_SelectedIndexChanged
  ↓
RecalculerTailles()
  ↓
RemplirListViewPistes()
  ↓
CalculerTailleCompressee(durée) pour chaque piste
```

### Lors de l'extraction
```
ButtonExtraire_Click()
  ↓
Validation des données
  ↓
ExtrairePiste(track) pour chaque piste cochée
  ↓
Lecture de ComboBoxTypeConversion → format
Lecture de ComboBoxQualiteConversion → qualite
  ↓
Select Case format
  Case "MP3" → ExtraireMp3(...)
  Case "FLAC" → ExtraireFlac(...)
  Case "WAV" → ExtraireWav(...)
  Case "WMA" → ExtraireWma(...)
  ↓
Extraction avec paramètres capturés
  ↓
EcrireMetadonnees(...)
```

---

## 🛡️ Sécurité et validation

### Capture des valeurs UI avant Task.Run
```vb
' ✅ BON : Capturer AVANT les opérations asynchrones
Dim qualite As String = If(ComboBoxQualiteConversion.SelectedItem?.ToString(), "Défaut")
Await Task.Run(Sub()
	' Utiliser la variable 'qualite' capturée
	Select Case qualite
		' ...
	End Select
End Sub)
```

```vb
' ❌ MAUVAIS : Accès direct dans Task.Run (erreur inter-thread)
Await Task.Run(Sub()
	Dim qualite = ComboBoxQualiteConversion.SelectedItem.ToString()  ' ERREUR !
End Sub)
```

### Valeurs par défaut en fallback
```vb
' Si SelectedItem est Nothing ou valeur inattendue
Dim qualite As String = If(ComboBoxQualiteConversion.SelectedItem?.ToString(), "Défaut")

Select Case qualite
	Case "Valeur attendue"
		' ...
	Case Else
		' Fallback vers qualité par défaut
		bitrate = 320  ' Pour MP3
End Select
```

---

## 📊 Gestion des événements

### Ordre d'initialisation dans FormCompresser_Load

```vb
' 1. Configuration du ComboBoxChoixLecteur (avant ApplyTheme)
ComboBoxChoixLecteur.DrawMode = DrawMode.OwnerDrawFixed
AddHandler ComboBoxChoixLecteur.DrawItem, AddressOf ComboBoxChoixLecteur_DrawItem

' 2. Application du thème
ThemeManager.ApplyThemeToForm(Me)

' 3. Définir MP3 par défaut
ComboBoxTypeConversion.SelectedIndex = mp3Index
' → Ceci déclenche automatiquement ComboBoxTypeConversion_SelectedIndexChanged
' → Qui remplit ComboBoxQualiteConversion avec les options MP3
' → Et sélectionne "Très haute (320 kbps)" par défaut

' 4. Ajouter le handler pour le recalcul des tailles
AddHandler ComboBoxTypeConversion.SelectedIndexChanged, AddressOf RecalculerTailles
AddHandler ComboBoxTypeConversion.SelectedIndexChanged, AddressOf ComboBoxTypeConversion_SelectedIndexChanged
AddHandler ComboBoxQualiteConversion.SelectedIndexChanged, AddressOf RecalculerTailles
```

**⚠️ Important** : Le handler `ComboBoxTypeConversion_SelectedIndexChanged` est attaché **après** la sélection initiale dans le code actuel. Pour une meilleure cohérence, il faudrait l'attacher **avant** de sélectionner l'index.

---

## 🔍 Dépannage

### Problème : ComboBox vide après sélection

**Symptôme** :
```
ComboBoxQualiteConversion.Items.Count = 0
```

**Causes possibles** :
1. Le handler `ComboBoxTypeConversion_SelectedIndexChanged` n'est pas attaché
2. La méthode `BeginUpdate()` est appelée mais pas `EndUpdate()`
3. Une exception silencieuse vide la collection

**Solution** :
```vb
' Vérifier que le handler est attaché
AddHandler ComboBoxTypeConversion.SelectedIndexChanged, _
	AddressOf ComboBoxTypeConversion_SelectedIndexChanged

' Toujours appeler EndUpdate() même en cas d'erreur
Try
	ComboBoxQualiteConversion.BeginUpdate()
	' ... remplissage
Finally
	ComboBoxQualiteConversion.EndUpdate()
End Try
```

---

### Problème : Mauvaise qualité encodée

**Symptôme** :
```
Fichier MP3 encodé en 128 kbps alors que "Très haute (320 kbps)" était sélectionné
```

**Causes possibles** :
1. Le `Select Case` ne reconnaît pas le libellé exact
2. La valeur par défaut du fallback est incorrecte
3. La variable `qualite` n'est pas capturée correctement

**Solution** :
```vb
' Vérifier que les libellés correspondent EXACTEMENT
Select Case qualite
	Case "Très haute (320 kbps)"  ' ← Doit être IDENTIQUE à Items.Add(...)
		bitrate = 320
	Case Else
		' Log pour debug
		Debug.WriteLine($"Qualité non reconnue : '{qualite}'")
		bitrate = 320  ' Fallback sûr
End Select
```

---

### Problème : Exception NullReferenceException

**Symptôme** :
```
System.NullReferenceException: Object reference not set to an instance of an object.
```

**Causes possibles** :
1. `ComboBoxQualiteConversion.SelectedItem` est `Nothing`
2. Accès à une propriété sur un objet `Nothing`

**Solution** :
```vb
' Utiliser l'opérateur null-conditional ?.
Dim qualite As String = If(ComboBoxQualiteConversion.SelectedItem?.ToString(), "Défaut")

' Ou vérifier explicitement
If ComboBoxQualiteConversion.SelectedItem IsNot Nothing Then
	Dim qualite As String = ComboBoxQualiteConversion.SelectedItem.ToString()
Else
	Dim qualite As String = "Très haute (320 kbps)"  ' Défaut
End If
```

---

## 🎯 Améliorations futures

### 1. Persistance des choix utilisateur
Sauvegarder le dernier format/qualité sélectionné dans les paramètres :

```vb
' Sauvegarde
My.Settings.FormatExtraction = ComboBoxTypeConversion.SelectedItem.ToString()
My.Settings.QualiteExtraction = ComboBoxQualiteConversion.SelectedItem.ToString()
My.Settings.Save()

' Restauration au prochain chargement
If Not String.IsNullOrEmpty(My.Settings.FormatExtraction) Then
	Dim index = ComboBoxTypeConversion.Items.IndexOf(My.Settings.FormatExtraction)
	If index >= 0 Then
		ComboBoxTypeConversion.SelectedIndex = index
		' Le handler se charge de remplir la qualité

		' Puis sélectionner la qualité sauvegardée
		If Not String.IsNullOrEmpty(My.Settings.QualiteExtraction) Then
			Dim qualiteIndex = ComboBoxQualiteConversion.Items.IndexOf(My.Settings.QualiteExtraction)
			If qualiteIndex >= 0 Then
				ComboBoxQualiteConversion.SelectedIndex = qualiteIndex
			End If
		End If
	End If
End If
```

---

### 2. Validation des formats
Ajouter une validation pour s'assurer que les valeurs sont cohérentes :

```vb
Private Function ValiderFormatQualite(format As String, qualite As String) As Boolean
	Select Case format.ToUpper()
		Case "MP3"
			Return qualite.Contains("kbps") AndAlso _
				   (qualite.Contains("128") Or qualite.Contains("192") Or _
					qualite.Contains("256") Or qualite.Contains("320"))
		Case "FLAC"
			Return qualite.Contains("Niveau") AndAlso _
				   (qualite.Contains("0") Or qualite.Contains("5") Or qualite.Contains("8"))
		Case "WAV"
			Return qualite.Contains("PCM") AndAlso qualite.Contains("kHz")
		Case "WMA"
			Return qualite.Contains("kbps") AndAlso _
				   (qualite.Contains("128") Or qualite.Contains("192") Or qualite.Contains("256"))
		Case Else
			Return False
	End Select
End Function
```

---

### 3. Prévisualisation de la taille totale
Afficher la taille totale estimée de toutes les pistes sélectionnées :

```vb
Private Sub AfficherTailleTotale()
	Dim tailleTotal As Double = 0

	For i As Integer = 0 To ListViewCompress.Items.Count - 1
		If ListViewCompress.Items(i).Checked Then
			Dim tailleText = ListViewCompress.Items(i).SubItems(4).Text  ' Colonne "Taille compressée"
			' Parser la taille (ex: "12.5 MB")
			Dim tailleMB As Double
			If Double.TryParse(tailleText.Replace(" MB", ""), tailleMB) Then
				tailleTotal += tailleMB
			End If
		End If
	Next

	LabelTailleTotal.Text = $"Taille totale estimée : {tailleTotal:F2} MB"
End Sub
```

---

## 📚 Références

### NAudio
- **WaveFormat** : https://github.com/naudio/NAudio/blob/master/NAudio/Wave/WaveFormats/WaveFormat.cs
- **MediaFoundationResampler** : https://github.com/naudio/NAudio/blob/master/NAudio/Wave/WaveStreams/MediaFoundationResampler.cs
- **LameMP3FileWriter** : https://github.com/Corey-M/NAudio.Lame

### FFMpeg
- **FLAC encoder options** : https://ffmpeg.org/ffmpeg-codecs.html#flac-2
- **WMA encoder options** : https://ffmpeg.org/ffmpeg-codecs.html#wmav2

---

## 📝 Changelog

### Version actuelle (2024-07-11)
- ✅ Implémentation du système de qualité dynamique
- ✅ Support de 4 formats : MP3, FLAC, WAV, WMA
- ✅ Options de qualité spécifiques par format
- ✅ Recalcul automatique des tailles estimées
- ✅ Capture correcte des valeurs UI avant async
- ✅ Fallback sur valeurs par défaut sûres

---

**AudioPlay - Système de Qualité Dynamique CD** © 2024
