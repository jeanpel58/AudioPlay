# ⚡ GUIDE DE DÉMARRAGE RAPIDE - INTÉGRATION 5 MIN

## 🎯 Objectif

Intégrer les traductions de l'enregistrement DJ dans AudioPlay en **moins de 5 minutes**.

---

## ✅ Étape 1 : Ouvrir FormDJ.vb (30 sec)

Ouvrez le fichier `AudioPlay\FormDJ.vb` dans Visual Studio.

---

## ✅ Étape 2 : Modifier RefreshLanguage() (2 min)

Localisez la méthode `RefreshLanguage()` et ajoutez à la fin :

```vb
' === ENREGISTREMENT DJ ===

' Bouton REC/STOP
If EnregistrementEnCours Then
	ButtonEnregistrement.Text = LanguageManager.GetString("DJ_Recording_Button_Stop")
Else
	ButtonEnregistrement.Text = LanguageManager.GetString("DJ_Recording_Button_Start")
End If

' Label Format
LabelEnregistrement.Text = LanguageManager.GetString("DJ_Recording_Label_Format")

' ComboBox formats
ComboBoxFormatEnregistrement.Items.Clear()
ComboBoxFormatEnregistrement.Items.AddRange(New String() {
	LanguageManager.GetString("DJ_Recording_Format_WAV"),
	LanguageManager.GetString("DJ_Recording_Format_MP3_320"),
	LanguageManager.GetString("DJ_Recording_Format_MP3_256"),
	LanguageManager.GetString("DJ_Recording_Format_MP3_192"),
	LanguageManager.GetString("DJ_Recording_Format_MP3_128"),
	LanguageManager.GetString("DJ_Recording_Format_FLAC"),
	LanguageManager.GetString("DJ_Recording_Format_WMA"),
	LanguageManager.GetString("DJ_Recording_Format_AAC")
})

' Restaurer la sélection
If ComboBoxFormatEnregistrement.Items.Count > 0 AndAlso 
   ComboBoxFormatEnregistrement.SelectedIndex = -1 Then
	ComboBoxFormatEnregistrement.SelectedIndex = 1 ' MP3 320 par défaut
End If
```

---

## ✅ Étape 3 : Modifier DemarrerEnregistrementDJ() (1 min)

Localisez la méthode `DemarrerEnregistrementDJ()` et remplacez les messages :

```vb
' Remplacer :
' If LecteurA.AudioFileReader Is Nothing AndAlso LecteurB.AudioFileReader Is Nothing Then
'     MessageBox.Show("Chargez au moins une piste...", "Erreur", ...)
' End If

' Par :
If LecteurA.AudioFileReader Is Nothing AndAlso LecteurB.AudioFileReader Is Nothing Then
	MessageBox.Show(
		LanguageManager.GetString("DJ_Recording_Error_NoTrack"),
		LanguageManager.GetString("DJ_Recording_Error_NoTrack_Title"),
		MessageBoxButtons.OK,
		MessageBoxIcon.Warning
	)
	Return
End If

' Remplacer :
' folderBrowser.Description = "Choisissez le répertoire..."

' Par :
folderBrowser.Description = LanguageManager.GetString("DJ_Recording_SelectFolder")

' Remplacer :
' MessageBox.Show("Enregistrement démarré!...", "Enregistrement", ...)

' Par :
MessageBox.Show(
	String.Format(LanguageManager.GetString("DJ_Recording_Started_Message"), nomFichier),
	LanguageManager.GetString("DJ_Recording_Started_Title"),
	MessageBoxButtons.OK,
	MessageBoxIcon.Information
)
```

---

## ✅ Étape 4 : Modifier ArreterEnregistrementDJ() (1 min)

Localisez la méthode `ArreterEnregistrementDJ()` et remplacez :

```vb
' Remplacer :
' MessageBox.Show("Enregistrement terminé!...", "Enregistrement", ...)

' Par :
Dim result = MessageBox.Show(
	String.Format(LanguageManager.GetString("DJ_Recording_Stopped_Message"), 
				  dureeFormatee, 
				  cheminComplet),
	LanguageManager.GetString("DJ_Recording_Stopped_Title"),
	MessageBoxButtons.YesNo,
	MessageBoxIcon.Information
)

If result = DialogResult.Yes Then
	Process.Start("explorer.exe", Path.GetDirectoryName(cheminComplet))
End If
```

---

## ✅ Étape 5 : Compiler et tester (30 sec)

1. **Appuyez sur F6** ou **Build → Build Solution**
2. **Lancez AudioPlay** (F5)
3. **Allez en mode DJ**
4. **Changez la langue** (Menu → Options → Langue)
5. **Vérifiez** que le bouton REC et les formats changent de langue

---

## 🎉 C'est terminé !

Votre enregistrement DJ est maintenant **100% localisé** en 5 langues !

### 🧪 Test rapide

1. Français : Bouton "⬤ REC" → ComboBox "MP3 (320 kbps)"
2. English : Button "⬤ REC" → ComboBox "MP3 (320 kbps)"
3. Español : Botón "⬤ REC" → ComboBox "MP3 (320 kbps)"
4. Italiano : Pulsante "⬤ REC" → ComboBox "MP3 (320 kbps)"
5. Deutsch : Schaltfläche "⬤ REC" → ComboBox "MP3 (320 kbps)"

---

## 📚 Aller plus loin

### Ajouter un bouton d'aide (optionnel)

Dans `FormDJ.Designer.vb`, ajoutez :

```vb
' Déclarer
Private ButtonAideEnregistrement As Button

' Dans InitializeComponent()
Me.ButtonAideEnregistrement = New Button()
Me.ButtonAideEnregistrement.Location = New Point(450, 20)
Me.ButtonAideEnregistrement.Size = New Size(30, 30)
Me.ButtonAideEnregistrement.Text = "?"
Me.ButtonAideEnregistrement.BackColor = Color.FromArgb(102, 126, 234)
Me.ButtonAideEnregistrement.ForeColor = Color.White
Me.ButtonAideEnregistrement.FlatStyle = FlatStyle.Flat
Me.GroupBoxMixeur.Controls.Add(Me.ButtonAideEnregistrement)
```

Dans `FormDJ.vb`, ajoutez :

```vb
Private Sub ButtonAideEnregistrement_Click(sender As Object, e As EventArgs) _
	Handles ButtonAideEnregistrement.Click

	Dim langue = LanguageManager.GetCurrentLanguage().ToLower()
	Dim fichier = $"DJ_RECORDING_GUIDE_USER.{langue}.html"
	Dim chemin = Path.Combine(Application.StartupPath, fichier)

	If File.Exists(chemin) Then
		Process.Start(New ProcessStartInfo With {
			.FileName = chemin,
			.UseShellExecute = True
		})
	End If
End Sub
```

---

## ❓ Questions fréquentes

### Les traductions ne s'affichent pas ?

**Solution :** Vérifiez que les fichiers `.resx` sont bien inclus dans le projet et que `Build Action = Embedded Resource`.

### Les accents sont mal affichés ?

**Solution :** Vérifiez que les fichiers `.resx` sont en UTF-8.

### Le guide HTML ne s'ouvre pas ?

**Solution :** Ajoutez les guides HTML au projet avec `Copy to Output Directory = Copy if newer`.

---

## 📊 Checklist finale

- [ ] `RefreshLanguage()` modifié
- [ ] `DemarrerEnregistrementDJ()` modifié
- [ ] `ArreterEnregistrementDJ()` modifié
- [ ] Build réussi (F6)
- [ ] Testé en français
- [ ] Testé en anglais
- [ ] Testé changement de langue en temps réel
- [ ] (Optionnel) Bouton d'aide ajouté
- [ ] (Optionnel) Guides HTML copiés dans Output

---

## 🎊 Félicitations !

Vous avez intégré **105 traductions** en **5 minutes** !

**Les utilisateurs du monde entier peuvent maintenant profiter de l'enregistrement DJ dans leur langue ! 🌍**

---

*Guide créé le : 2 juin 2026*  
*Temps d'intégration : ~5 minutes*  
*Niveau : Débutant*
