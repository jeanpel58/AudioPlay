<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormCompresser
    Inherits System.Windows.Forms.Form

    'Form remplace la méthode Dispose pour nettoyer la liste des composants.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Requise par le Concepteur Windows Form
    Private components As System.ComponentModel.IContainer

    'REMARQUE : la procédure suivante est requise par le Concepteur Windows Form
    'Elle peut être modifiée à l'aide du Concepteur Windows Form.  
    'Ne la modifiez pas à l'aide de l'éditeur de code.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormCompresser))
        ComboBoxChoixLecteur = New ComboBox()
        Label_CDTitre = New Label()
        TextBoxCDTitre = New TextBox()
        TextBoxCDArtiste = New TextBox()
        LabelCDArtiste = New Label()
        Label_ChoixLecteur = New Label()
        TextBoxAnnee = New TextBox()
        LabelAnnee = New Label()
        ComboBoxGenre = New ComboBox()
        LabelGenre = New Label()
        Label3 = New Label()
        PictureBoxPochette = New PictureBox()
        LabelDimImagText = New Label()
        LabelTailleImagText = New Label()
        LabelTailleImage = New Label()
        Label_DimImage = New Label()
        LabelNumCD = New Label()
        LabelPremierNumPiste = New Label()
        TextBoxPremierNumPiste = New TextBox()
        TextBoxNumCD = New TextBox()
        TextBoxCommentaire = New TextBox()
        LabelTypeConversion = New Label()
        ComboBoxTypeConversion = New ComboBox()
        ComboBoxQualiteConversion = New ComboBox()
        LabelQualiteConversion = New Label()
        LabelRepSauvegarde = New Label()
        TextBoxRepSauvegarde = New TextBox()
        ButtonQuitter = New Button()
        ButtonExtraire = New Button()
        ButtonAnnuler = New Button()
        ButtonRepSauvegarde = New Button()
        CheckBoxEjectCD = New CheckBox()
        CheckBoxVerouillerCD = New CheckBox()
        LabelCommentaire = New Label()
        ListViewCompress = New ListView()
        ColumnHeaderPiste = New ColumnHeader()
        ColumnHeaderTitre = New ColumnHeader()
        ColumnHeaderArtiste = New ColumnHeader()
        ColumnHeaderDébut = New ColumnHeader()
        ColumnHeaderLongueur = New ColumnHeader()
        ColumnHeaderTaille = New ColumnHeader()
        ColumnHeaderTailleComp = New ColumnHeader()
        LabelPisteEnCours = New Label()
        ProgressBarPisteActuelle = New ProgressBar()
        ProgressBarGlobale = New ProgressBar()
        LabelProgressionGlobale = New Label()
        CheckBox_FCompress_SelectDeselect = New CheckBox()
        Button_EditTracks = New Button()
        ButtonSoumettreGnuDB = New Button()
        Button_Image_Erase = New Button()
        Button_Image_Suiv = New Button()
        Button_Image_Prec = New Button()
        Label1 = New Label()
        Label2 = New Label()
        Label_Im_Site = New Label()
        Label_Image_Site = New Label()
        NumericUpDown_DB = New NumericUpDown()
        Label4 = New Label()
        Label_Normalisation = New Label()
        CType(PictureBoxPochette, ComponentModel.ISupportInitialize).BeginInit()
        CType(NumericUpDown_DB, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' ComboBoxChoixLecteur
        ' 
        ComboBoxChoixLecteur.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxChoixLecteur.FlatStyle = FlatStyle.Flat
        ComboBoxChoixLecteur.FormattingEnabled = True
        ComboBoxChoixLecteur.Location = New Point(12, 34)
        ComboBoxChoixLecteur.Name = "ComboBoxChoixLecteur"
        ComboBoxChoixLecteur.Size = New Size(373, 23)
        ComboBoxChoixLecteur.TabIndex = 0
        ' 
        ' Label_CDTitre
        ' 
        Label_CDTitre.FlatStyle = FlatStyle.Flat
        Label_CDTitre.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Label_CDTitre.Location = New Point(17, 69)
        Label_CDTitre.Name = "Label_CDTitre"
        Label_CDTitre.Size = New Size(100, 23)
        Label_CDTitre.TabIndex = 1
        Label_CDTitre.Text = "CD Titre"
        Label_CDTitre.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' TextBoxCDTitre
        ' 
        TextBoxCDTitre.BorderStyle = BorderStyle.None
        TextBoxCDTitre.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        TextBoxCDTitre.Location = New Point(123, 73)
        TextBoxCDTitre.Name = "TextBoxCDTitre"
        TextBoxCDTitre.Size = New Size(262, 18)
        TextBoxCDTitre.TabIndex = 2
        ' 
        ' TextBoxCDArtiste
        ' 
        TextBoxCDArtiste.BorderStyle = BorderStyle.None
        TextBoxCDArtiste.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        TextBoxCDArtiste.Location = New Point(123, 107)
        TextBoxCDArtiste.Name = "TextBoxCDArtiste"
        TextBoxCDArtiste.Size = New Size(262, 18)
        TextBoxCDArtiste.TabIndex = 4
        ' 
        ' LabelCDArtiste
        ' 
        LabelCDArtiste.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelCDArtiste.Location = New Point(17, 103)
        LabelCDArtiste.Name = "LabelCDArtiste"
        LabelCDArtiste.Size = New Size(100, 23)
        LabelCDArtiste.TabIndex = 3
        LabelCDArtiste.Text = "CD Artiste"
        LabelCDArtiste.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label_ChoixLecteur
        ' 
        Label_ChoixLecteur.FlatStyle = FlatStyle.Flat
        Label_ChoixLecteur.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Label_ChoixLecteur.Location = New Point(12, 8)
        Label_ChoixLecteur.Name = "Label_ChoixLecteur"
        Label_ChoixLecteur.Size = New Size(373, 23)
        Label_ChoixLecteur.TabIndex = 5
        Label_ChoixLecteur.Text = "Choix de lecteur"
        Label_ChoixLecteur.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' TextBoxAnnee
        ' 
        TextBoxAnnee.BorderStyle = BorderStyle.None
        TextBoxAnnee.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        TextBoxAnnee.Location = New Point(123, 141)
        TextBoxAnnee.Name = "TextBoxAnnee"
        TextBoxAnnee.Size = New Size(59, 18)
        TextBoxAnnee.TabIndex = 7
        ' 
        ' LabelAnnee
        ' 
        LabelAnnee.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelAnnee.Location = New Point(17, 137)
        LabelAnnee.Name = "LabelAnnee"
        LabelAnnee.Size = New Size(100, 23)
        LabelAnnee.TabIndex = 6
        LabelAnnee.Text = "Année"
        LabelAnnee.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' ComboBoxGenre
        ' 
        ComboBoxGenre.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxGenre.FlatStyle = FlatStyle.Flat
        ComboBoxGenre.FormattingEnabled = True
        ComboBoxGenre.Items.AddRange(New Object() {"Acapella", "Acid", "Acid Jazz", "Acid Punk", "Acoustic", "Alt Rock", "Alternative", "Ambient", "Anime", "Autre", "AvantGarde", "Ballad", "Bass", "Beat", "Bebop", "Big Band", "Black Metal", "Bluegrass", "Blues", "Booty Bass", "Brit Pop", "Cabaret", "Celtic", "Chamber Music", "Chanson", "Chorus", "Christian Gangsta Rap", "Christian Rap", "Christian Rock", "Classic Rock", "Classical", "Club", "Club-House", "Comedy", "Compilation", "Contemporary Christian", "Country", "Crooners", "Crossover", "Cult", "Dance", "Dance Hall", "Darkwave", "Death Metal", "Disco", "Dream", "Drum & Bass", "Drum Solo", "Duet", "Easy Listening", "Electronic", "Ethnic", "Eurodance", "Euro-House", "Euro-Techno", "Fast-Fusion", "Folk", "Folk/Rock", "Folklore", "Français", "France", "Freestyle", "Funk", "Fusion", "Game", "Gangsta Rap", "Goa", "Gospel", "Gothic", "Gothic Rock", "Greatest Hits", "Grunge", "Hardcore", "Hard Rock", "Heavy Metal", "Hip-Hop", "House", "Humour", "Indie", "Industrial", "Instrumental", "Instrumental Pop", "Instrumental Rock", "Jazz", "Jazz+Funk", "JPop", "Jungle", "Latin", "Latino", "Lo-Fi", "Meditation", "Meditative", "Merengue", "Metal", "Musical", "National Folk", "Native", "Native American", "Negerpunk", "New Age", "New Wave", "Noise", "Oldies", "Opera", "Other", "Polka", "Polsk Punk", "Pop", "Pop/Funk", "Pop-Folk", "Porn Groove", "Power Ballad", "Pranks", "Primus", "Progressive Rock", "Psychedelic", "Psychedelic Rock", "Punk", "Punk Rock", "Quebec", "Quebecois", "R&B", "Rap", "Rave", "Reggae", "Reaggaeton", "Retro", "Revival", "Rhythmic Soul", "Rock", "Rock & Roll", "Rock Alternative", "Rock Folk", "Rock Heavy", "Rock Metal", "Rock Progressive", "Rock Punk Rock", "Rock Slow", "Rock Soft", "Salsa", "Samba", "Satire", "Showtunes", "Ska", "Slow", "Slow Jam", "Sonata", "Soul", "Sound Clip", "Soundtrack", "Southern Rock", "Space", "Speech", "Swing", "Symphonic Rock", "Symphony", "Synthpop", "Tango", "Techno", "Techno-Industrial", "Terror", "Trash Metal", "Top 10", "Top 20", "Top 30", "Top 40", "Top 50", "Top 60", "Top 100", "Top Hits", "Trailer", "Trance", "Tribal", "Trip-Hop", "Vocal"})
        ComboBoxGenre.Location = New Point(272, 141)
        ComboBoxGenre.Name = "ComboBoxGenre"
        ComboBoxGenre.Size = New Size(113, 23)
        ComboBoxGenre.TabIndex = 8
        ' 
        ' LabelGenre
        ' 
        LabelGenre.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelGenre.Location = New Point(213, 141)
        LabelGenre.Name = "LabelGenre"
        LabelGenre.Size = New Size(53, 23)
        LabelGenre.TabIndex = 9
        LabelGenre.Text = "Genre"
        LabelGenre.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label3
        ' 
        Label3.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Label3.Location = New Point(391, 8)
        Label3.Name = "Label3"
        Label3.Size = New Size(108, 23)
        Label3.TabIndex = 10
        Label3.Text = "CD Pochette"
        Label3.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' PictureBoxPochette
        ' 
        PictureBoxPochette.BorderStyle = BorderStyle.FixedSingle
        PictureBoxPochette.Location = New Point(399, 34)
        PictureBoxPochette.Name = "PictureBoxPochette"
        PictureBoxPochette.Size = New Size(200, 200)
        PictureBoxPochette.TabIndex = 11
        PictureBoxPochette.TabStop = False
        ' 
        ' LabelDimImagText
        ' 
        LabelDimImagText.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelDimImagText.Location = New Point(399, 237)
        LabelDimImagText.Name = "LabelDimImagText"
        LabelDimImagText.Size = New Size(132, 23)
        LabelDimImagText.TabIndex = 12
        LabelDimImagText.Text = "Dimensions image:"
        LabelDimImagText.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' LabelTailleImagText
        ' 
        LabelTailleImagText.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelTailleImagText.Location = New Point(399, 260)
        LabelTailleImagText.Name = "LabelTailleImagText"
        LabelTailleImagText.Size = New Size(132, 23)
        LabelTailleImagText.TabIndex = 13
        LabelTailleImagText.Text = "Taille image:"
        LabelTailleImagText.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' LabelTailleImage
        ' 
        LabelTailleImage.Font = New Font("Segoe UI", 9F)
        LabelTailleImage.Location = New Point(537, 260)
        LabelTailleImage.Name = "LabelTailleImage"
        LabelTailleImage.Size = New Size(47, 23)
        LabelTailleImage.TabIndex = 15
        LabelTailleImage.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label_DimImage
        ' 
        Label_DimImage.Font = New Font("Segoe UI", 9F)
        Label_DimImage.Location = New Point(537, 237)
        Label_DimImage.Name = "Label_DimImage"
        Label_DimImage.Size = New Size(72, 23)
        Label_DimImage.TabIndex = 14
        Label_DimImage.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' LabelNumCD
        ' 
        LabelNumCD.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelNumCD.Location = New Point(399, 354)
        LabelNumCD.Name = "LabelNumCD"
        LabelNumCD.Size = New Size(132, 23)
        LabelNumCD.TabIndex = 17
        LabelNumCD.Text = "Numéro de CD:"
        LabelNumCD.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' LabelPremierNumPiste
        ' 
        LabelPremierNumPiste.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelPremierNumPiste.Location = New Point(399, 331)
        LabelPremierNumPiste.Name = "LabelPremierNumPiste"
        LabelPremierNumPiste.Size = New Size(167, 23)
        LabelPremierNumPiste.TabIndex = 16
        LabelPremierNumPiste.Text = "Premier numéro de piste:"
        LabelPremierNumPiste.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' TextBoxPremierNumPiste
        ' 
        TextBoxPremierNumPiste.BorderStyle = BorderStyle.None
        TextBoxPremierNumPiste.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        TextBoxPremierNumPiste.Location = New Point(572, 333)
        TextBoxPremierNumPiste.Name = "TextBoxPremierNumPiste"
        TextBoxPremierNumPiste.Size = New Size(27, 18)
        TextBoxPremierNumPiste.TabIndex = 18
        TextBoxPremierNumPiste.Text = "1"
        TextBoxPremierNumPiste.TextAlign = HorizontalAlignment.Right
        ' 
        ' TextBoxNumCD
        ' 
        TextBoxNumCD.BorderStyle = BorderStyle.None
        TextBoxNumCD.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        TextBoxNumCD.Location = New Point(572, 356)
        TextBoxNumCD.Name = "TextBoxNumCD"
        TextBoxNumCD.Size = New Size(27, 18)
        TextBoxNumCD.TabIndex = 19
        TextBoxNumCD.Text = "1"
        TextBoxNumCD.TextAlign = HorizontalAlignment.Right
        ' 
        ' TextBoxCommentaire
        ' 
        TextBoxCommentaire.BorderStyle = BorderStyle.None
        TextBoxCommentaire.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        TextBoxCommentaire.Location = New Point(123, 180)
        TextBoxCommentaire.Name = "TextBoxCommentaire"
        TextBoxCommentaire.Size = New Size(262, 18)
        TextBoxCommentaire.TabIndex = 21
        ' 
        ' LabelTypeConversion
        ' 
        LabelTypeConversion.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelTypeConversion.Location = New Point(17, 211)
        LabelTypeConversion.Name = "LabelTypeConversion"
        LabelTypeConversion.Size = New Size(165, 23)
        LabelTypeConversion.TabIndex = 20
        LabelTypeConversion.Text = "Type de conversion:"
        LabelTypeConversion.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' ComboBoxTypeConversion
        ' 
        ComboBoxTypeConversion.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxTypeConversion.FlatStyle = FlatStyle.Flat
        ComboBoxTypeConversion.FormattingEnabled = True
        ComboBoxTypeConversion.Items.AddRange(New Object() {"MP3", "Flac", "Wav", "Wma"})
        ComboBoxTypeConversion.Location = New Point(17, 237)
        ComboBoxTypeConversion.Name = "ComboBoxTypeConversion"
        ComboBoxTypeConversion.Size = New Size(165, 23)
        ComboBoxTypeConversion.TabIndex = 25
        ' 
        ' ComboBoxQualiteConversion
        ' 
        ComboBoxQualiteConversion.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxQualiteConversion.FlatStyle = FlatStyle.Flat
        ComboBoxQualiteConversion.FormattingEnabled = True
        ComboBoxQualiteConversion.Location = New Point(220, 237)
        ComboBoxQualiteConversion.Name = "ComboBoxQualiteConversion"
        ComboBoxQualiteConversion.Size = New Size(165, 23)
        ComboBoxQualiteConversion.TabIndex = 27
        ' 
        ' LabelQualiteConversion
        ' 
        LabelQualiteConversion.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelQualiteConversion.Location = New Point(220, 211)
        LabelQualiteConversion.Name = "LabelQualiteConversion"
        LabelQualiteConversion.Size = New Size(165, 23)
        LabelQualiteConversion.TabIndex = 26
        LabelQualiteConversion.Text = "Qualité de conversion:"
        LabelQualiteConversion.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' LabelRepSauvegarde
        ' 
        LabelRepSauvegarde.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelRepSauvegarde.Location = New Point(17, 277)
        LabelRepSauvegarde.Name = "LabelRepSauvegarde"
        LabelRepSauvegarde.Size = New Size(165, 23)
        LabelRepSauvegarde.TabIndex = 28
        LabelRepSauvegarde.Text = "Répertoire de sauvegarde:"
        LabelRepSauvegarde.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' TextBoxRepSauvegarde
        ' 
        TextBoxRepSauvegarde.BorderStyle = BorderStyle.None
        TextBoxRepSauvegarde.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0)
        TextBoxRepSauvegarde.Location = New Point(17, 303)
        TextBoxRepSauvegarde.Name = "TextBoxRepSauvegarde"
        TextBoxRepSauvegarde.Size = New Size(287, 16)
        TextBoxRepSauvegarde.TabIndex = 29
        ' 
        ' ButtonQuitter
        ' 
        ButtonQuitter.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonQuitter.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonQuitter.FlatStyle = FlatStyle.Flat
        ButtonQuitter.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        ButtonQuitter.Location = New Point(495, 803)
        ButtonQuitter.Name = "ButtonQuitter"
        ButtonQuitter.Size = New Size(110, 35)
        ButtonQuitter.TabIndex = 31
        ButtonQuitter.Text = "Quitter"
        ButtonQuitter.UseVisualStyleBackColor = True
        ' 
        ' ButtonExtraire
        ' 
        ButtonExtraire.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonExtraire.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonExtraire.FlatStyle = FlatStyle.Flat
        ButtonExtraire.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
        ButtonExtraire.Location = New Point(373, 803)
        ButtonExtraire.Name = "ButtonExtraire"
        ButtonExtraire.Size = New Size(110, 35)
        ButtonExtraire.TabIndex = 30
        ButtonExtraire.Text = "Extraire"
        ButtonExtraire.UseVisualStyleBackColor = True
        ' 
        ' ButtonAnnuler
        ' 
        ButtonAnnuler.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonAnnuler.FlatAppearance.MouseOverBackColor = Color.Orange
        ButtonAnnuler.FlatStyle = FlatStyle.Flat
        ButtonAnnuler.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
        ButtonAnnuler.Location = New Point(373, 803)
        ButtonAnnuler.Name = "ButtonAnnuler"
        ButtonAnnuler.Size = New Size(110, 35)
        ButtonAnnuler.TabIndex = 32
        ButtonAnnuler.Text = "Annuler"
        ButtonAnnuler.UseVisualStyleBackColor = True
        ButtonAnnuler.Visible = False
        ' 
        ' ButtonRepSauvegarde
        ' 
        ButtonRepSauvegarde.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonRepSauvegarde.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonRepSauvegarde.FlatStyle = FlatStyle.Flat
        ButtonRepSauvegarde.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        ButtonRepSauvegarde.Location = New Point(310, 299)
        ButtonRepSauvegarde.Name = "ButtonRepSauvegarde"
        ButtonRepSauvegarde.Size = New Size(75, 22)
        ButtonRepSauvegarde.TabIndex = 32
        ButtonRepSauvegarde.Text = "Parcourir"
        ButtonRepSauvegarde.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxEjectCD
        ' 
        CheckBoxEjectCD.AutoSize = True
        CheckBoxEjectCD.Checked = True
        CheckBoxEjectCD.CheckState = CheckState.Checked
        CheckBoxEjectCD.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        CheckBoxEjectCD.Location = New Point(17, 334)
        CheckBoxEjectCD.Name = "CheckBoxEjectCD"
        CheckBoxEjectCD.Size = New Size(145, 19)
        CheckBoxEjectCD.TabIndex = 33
        CheckBoxEjectCD.Text = "Éjecter le CD à la fin..."
        CheckBoxEjectCD.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxVerouillerCD
        ' 
        CheckBoxVerouillerCD.AutoSize = True
        CheckBoxVerouillerCD.Checked = True
        CheckBoxVerouillerCD.CheckState = CheckState.Checked
        CheckBoxVerouillerCD.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        CheckBoxVerouillerCD.Location = New Point(17, 359)
        CheckBoxVerouillerCD.Name = "CheckBoxVerouillerCD"
        CheckBoxVerouillerCD.Size = New Size(319, 19)
        CheckBoxVerouillerCD.TabIndex = 34
        CheckBoxVerouillerCD.Text = "Verrouiller le plateau du lecteur pendant l'extraction"
        CheckBoxVerouillerCD.UseVisualStyleBackColor = True
        ' 
        ' LabelCommentaire
        ' 
        LabelCommentaire.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelCommentaire.Location = New Point(17, 178)
        LabelCommentaire.Name = "LabelCommentaire"
        LabelCommentaire.Size = New Size(100, 23)
        LabelCommentaire.TabIndex = 35
        LabelCommentaire.Text = "Commentaire"
        LabelCommentaire.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' ListViewCompress
        ' 
        ListViewCompress.BackColor = Color.LightCyan
        ListViewCompress.BorderStyle = BorderStyle.None
        ListViewCompress.CheckBoxes = True
        ListViewCompress.Columns.AddRange(New ColumnHeader() {ColumnHeaderPiste, ColumnHeaderTitre, ColumnHeaderArtiste, ColumnHeaderDébut, ColumnHeaderLongueur, ColumnHeaderTaille, ColumnHeaderTailleComp})
        ListViewCompress.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        ListViewCompress.FullRowSelect = True
        ListViewCompress.GridLines = True
        ListViewCompress.Location = New Point(17, 422)
        ListViewCompress.Name = "ListViewCompress"
        ListViewCompress.OwnerDraw = True
        ListViewCompress.Size = New Size(582, 318)
        ListViewCompress.TabIndex = 36
        ListViewCompress.UseCompatibleStateImageBehavior = False
        ListViewCompress.View = View.Details
        ' 
        ' ColumnHeaderPiste
        ' 
        ColumnHeaderPiste.Text = "Piste"
        ' 
        ' ColumnHeaderTitre
        ' 
        ColumnHeaderTitre.Text = "Titre"
        ColumnHeaderTitre.Width = 300
        ' 
        ' ColumnHeaderArtiste
        ' 
        ColumnHeaderArtiste.Text = "Artiste"
        ColumnHeaderArtiste.Width = 200
        ' 
        ' ColumnHeaderDébut
        ' 
        ColumnHeaderDébut.Text = "Début"
        ColumnHeaderDébut.Width = 100
        ' 
        ' ColumnHeaderLongueur
        ' 
        ColumnHeaderLongueur.Text = "Longueur"
        ColumnHeaderLongueur.Width = 100
        ' 
        ' ColumnHeaderTaille
        ' 
        ColumnHeaderTaille.Text = "Taille du fichier"
        ColumnHeaderTaille.Width = 100
        ' 
        ' ColumnHeaderTailleComp
        ' 
        ColumnHeaderTailleComp.Text = "Taille du fichier compressé"
        ColumnHeaderTailleComp.Width = 100
        ' 
        ' LabelPisteEnCours
        ' 
        LabelPisteEnCours.AutoSize = True
        LabelPisteEnCours.Location = New Point(17, 755)
        LabelPisteEnCours.Name = "LabelPisteEnCours"
        LabelPisteEnCours.Size = New Size(0, 15)
        LabelPisteEnCours.TabIndex = 100
        LabelPisteEnCours.Visible = False
        ' 
        ' ProgressBarPisteActuelle
        ' 
        ProgressBarPisteActuelle.Location = New Point(17, 773)
        ProgressBarPisteActuelle.Name = "ProgressBarPisteActuelle"
        ProgressBarPisteActuelle.Size = New Size(340, 20)
        ProgressBarPisteActuelle.TabIndex = 101
        ProgressBarPisteActuelle.Visible = False
        ' 
        ' ProgressBarGlobale
        ' 
        ProgressBarGlobale.Location = New Point(17, 818)
        ProgressBarGlobale.Name = "ProgressBarGlobale"
        ProgressBarGlobale.Size = New Size(340, 20)
        ProgressBarGlobale.TabIndex = 103
        ProgressBarGlobale.Visible = False
        ' 
        ' LabelProgressionGlobale
        ' 
        LabelProgressionGlobale.AutoSize = True
        LabelProgressionGlobale.Location = New Point(17, 800)
        LabelProgressionGlobale.Name = "LabelProgressionGlobale"
        LabelProgressionGlobale.Size = New Size(0, 15)
        LabelProgressionGlobale.TabIndex = 102
        LabelProgressionGlobale.Visible = False
        ' 
        ' CheckBox_FCompress_SelectDeselect
        ' 
        CheckBox_FCompress_SelectDeselect.AutoSize = True
        CheckBox_FCompress_SelectDeselect.Checked = True
        CheckBox_FCompress_SelectDeselect.CheckState = CheckState.Checked
        CheckBox_FCompress_SelectDeselect.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        CheckBox_FCompress_SelectDeselect.Location = New Point(17, 401)
        CheckBox_FCompress_SelectDeselect.Name = "CheckBox_FCompress_SelectDeselect"
        CheckBox_FCompress_SelectDeselect.Size = New Size(288, 19)
        CheckBox_FCompress_SelectDeselect.TabIndex = 104
        CheckBox_FCompress_SelectDeselect.Text = "Sélectionner/Désélectionner toutes les pistes..."
        CheckBox_FCompress_SelectDeselect.UseVisualStyleBackColor = True
        ' 
        ' Button_EditTracks
        ' 
        Button_EditTracks.FlatAppearance.MouseDownBackColor = Color.Red
        Button_EditTracks.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_EditTracks.FlatStyle = FlatStyle.Flat
        Button_EditTracks.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Button_EditTracks.Location = New Point(456, 397)
        Button_EditTracks.Name = "Button_EditTracks"
        Button_EditTracks.Size = New Size(143, 23)
        Button_EditTracks.TabIndex = 105
        Button_EditTracks.Text = "Éditer les pistes..."
        Button_EditTracks.UseVisualStyleBackColor = True
        ' 
        ' ButtonSoumettreGnuDB
        ' 
        ButtonSoumettreGnuDB.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonSoumettreGnuDB.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonSoumettreGnuDB.FlatStyle = FlatStyle.Flat
        ButtonSoumettreGnuDB.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        ButtonSoumettreGnuDB.Location = New Point(435, 745)
        ButtonSoumettreGnuDB.Name = "ButtonSoumettreGnuDB"
        ButtonSoumettreGnuDB.Size = New Size(164, 25)
        ButtonSoumettreGnuDB.TabIndex = 106
        ButtonSoumettreGnuDB.Text = "Soumettre à GnuDB"
        ButtonSoumettreGnuDB.UseVisualStyleBackColor = True
        ' 
        ' Button_Image_Erase
        ' 
        Button_Image_Erase.FlatAppearance.MouseDownBackColor = Color.Red
        Button_Image_Erase.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_Image_Erase.FlatStyle = FlatStyle.Flat
        Button_Image_Erase.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Button_Image_Erase.Location = New Point(572, 3)
        Button_Image_Erase.Name = "Button_Image_Erase"
        Button_Image_Erase.Size = New Size(27, 25)
        Button_Image_Erase.TabIndex = 107
        Button_Image_Erase.Text = "X"
        Button_Image_Erase.UseVisualStyleBackColor = True
        ' 
        ' Button_Image_Suiv
        ' 
        Button_Image_Suiv.FlatAppearance.MouseDownBackColor = Color.Red
        Button_Image_Suiv.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_Image_Suiv.FlatStyle = FlatStyle.Flat
        Button_Image_Suiv.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Button_Image_Suiv.Location = New Point(527, 3)
        Button_Image_Suiv.Name = "Button_Image_Suiv"
        Button_Image_Suiv.Size = New Size(37, 25)
        Button_Image_Suiv.TabIndex = 108
        Button_Image_Suiv.Text = "-->"
        Button_Image_Suiv.UseVisualStyleBackColor = True
        ' 
        ' Button_Image_Prec
        ' 
        Button_Image_Prec.FlatAppearance.MouseDownBackColor = Color.Red
        Button_Image_Prec.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_Image_Prec.FlatStyle = FlatStyle.Flat
        Button_Image_Prec.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Button_Image_Prec.Location = New Point(484, 3)
        Button_Image_Prec.Name = "Button_Image_Prec"
        Button_Image_Prec.Size = New Size(37, 25)
        Button_Image_Prec.TabIndex = 109
        Button_Image_Prec.Text = "<--"
        Button_Image_Prec.UseVisualStyleBackColor = True
        ' 
        ' Label1
        ' 
        Label1.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold)
        Label1.Location = New Point(399, 237)
        Label1.Name = "Label1"
        Label1.Size = New Size(132, 22)
        Label1.TabIndex = 12
        Label1.Text = "Dimensions image:"
        Label1.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label2
        ' 
        Label2.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold)
        Label2.Location = New Point(399, 259)
        Label2.Name = "Label2"
        Label2.Size = New Size(132, 22)
        Label2.TabIndex = 13
        Label2.Text = "Taille image:"
        Label2.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label_Im_Site
        ' 
        Label_Im_Site.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold)
        Label_Im_Site.Location = New Point(399, 283)
        Label_Im_Site.Name = "Label_Im_Site"
        Label_Im_Site.Size = New Size(51, 22)
        Label_Im_Site.TabIndex = 110
        Label_Im_Site.Text = "Site:"
        Label_Im_Site.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label_Image_Site
        ' 
        Label_Image_Site.Font = New Font("Segoe UI", 8.25F)
        Label_Image_Site.Location = New Point(456, 283)
        Label_Image_Site.Name = "Label_Image_Site"
        Label_Image_Site.Size = New Size(143, 22)
        Label_Image_Site.TabIndex = 111
        Label_Image_Site.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' NumericUpDown_DB
        ' 
        NumericUpDown_DB.Location = New Point(330, 397)
        NumericUpDown_DB.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        NumericUpDown_DB.Name = "NumericUpDown_DB"
        NumericUpDown_DB.Size = New Size(55, 23)
        NumericUpDown_DB.TabIndex = 112
        NumericUpDown_DB.Value = New Decimal(New Integer() {95, 0, 0, 0})
        ' 
        ' Label4
        ' 
        Label4.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Label4.Location = New Point(391, 398)
        Label4.Name = "Label4"
        Label4.Size = New Size(51, 22)
        Label4.TabIndex = 113
        Label4.Text = "%"
        Label4.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label_Normalisation
        ' 
        Label_Normalisation.Font = New Font("Segoe UI", 6.75F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Label_Normalisation.Location = New Point(330, 381)
        Label_Normalisation.Name = "Label_Normalisation"
        Label_Normalisation.Size = New Size(97, 14)
        Label_Normalisation.TabIndex = 114
        Label_Normalisation.Text = "Normalization"
        Label_Normalisation.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' FormCompresser
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(617, 850)
        Controls.Add(Label_Normalisation)
        Controls.Add(Label4)
        Controls.Add(NumericUpDown_DB)
        Controls.Add(Label_Image_Site)
        Controls.Add(Label_Im_Site)
        Controls.Add(Button_Image_Prec)
        Controls.Add(Button_Image_Suiv)
        Controls.Add(Button_Image_Erase)
        Controls.Add(ButtonSoumettreGnuDB)
        Controls.Add(Button_EditTracks)
        Controls.Add(CheckBox_FCompress_SelectDeselect)
        Controls.Add(ProgressBarGlobale)
        Controls.Add(LabelProgressionGlobale)
        Controls.Add(ProgressBarPisteActuelle)
        Controls.Add(LabelPisteEnCours)
        Controls.Add(ListViewCompress)
        Controls.Add(LabelCommentaire)
        Controls.Add(CheckBoxVerouillerCD)
        Controls.Add(CheckBoxEjectCD)
        Controls.Add(ButtonRepSauvegarde)
        Controls.Add(ButtonQuitter)
        Controls.Add(ButtonExtraire)
        Controls.Add(ButtonAnnuler)
        Controls.Add(TextBoxRepSauvegarde)
        Controls.Add(LabelRepSauvegarde)
        Controls.Add(ComboBoxQualiteConversion)
        Controls.Add(LabelQualiteConversion)
        Controls.Add(ComboBoxTypeConversion)
        Controls.Add(TextBoxCommentaire)
        Controls.Add(LabelTypeConversion)
        Controls.Add(TextBoxNumCD)
        Controls.Add(TextBoxPremierNumPiste)
        Controls.Add(LabelNumCD)
        Controls.Add(LabelPremierNumPiste)
        Controls.Add(LabelTailleImage)
        Controls.Add(Label_DimImage)
        Controls.Add(Label2)
        Controls.Add(LabelTailleImagText)
        Controls.Add(Label1)
        Controls.Add(LabelDimImagText)
        Controls.Add(PictureBoxPochette)
        Controls.Add(Label3)
        Controls.Add(LabelGenre)
        Controls.Add(ComboBoxGenre)
        Controls.Add(TextBoxAnnee)
        Controls.Add(LabelAnnee)
        Controls.Add(Label_ChoixLecteur)
        Controls.Add(TextBoxCDArtiste)
        Controls.Add(LabelCDArtiste)
        Controls.Add(TextBoxCDTitre)
        Controls.Add(Label_CDTitre)
        Controls.Add(ComboBoxChoixLecteur)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "FormCompresser"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Extraction du CD Audio"
        CType(PictureBoxPochette, ComponentModel.ISupportInitialize).EndInit()
        CType(NumericUpDown_DB, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents ComboBoxChoixLecteur As ComboBox
    Friend WithEvents Label_CDTitre As Label
    Friend WithEvents TextBoxCDTitre As TextBox
    Friend WithEvents TextBoxCDArtiste As TextBox
    Friend WithEvents LabelCDArtiste As Label
    Friend WithEvents Label_ChoixLecteur As Label
    Friend WithEvents TextBoxAnnee As TextBox
    Friend WithEvents LabelAnnee As Label
    Friend WithEvents ComboBoxGenre As ComboBox
    Friend WithEvents LabelGenre As Label
    Friend WithEvents Label3 As Label
    Friend WithEvents PictureBoxPochette As PictureBox
    Friend WithEvents LabelDimImagText As Label
    Friend WithEvents LabelTailleImagText As Label
    Friend WithEvents LabelTailleImage As Label
    Friend WithEvents Label_DimImage As Label
    Friend WithEvents LabelNumCD As Label
    Friend WithEvents LabelPremierNumPiste As Label
    Friend WithEvents TextBoxPremierNumPiste As TextBox
    Friend WithEvents TextBoxNumCD As TextBox
    Friend WithEvents TextBoxCommentaire As TextBox
    Friend WithEvents LabelTypeConversion As Label
    Friend WithEvents ComboBoxTypeConversion As ComboBox
    Friend WithEvents ComboBoxQualiteConversion As ComboBox
    Friend WithEvents LabelQualiteConversion As Label
    Friend WithEvents LabelRepSauvegarde As Label
    Friend WithEvents TextBoxRepSauvegarde As TextBox
    Friend WithEvents ButtonQuitter As Button
    Friend WithEvents ButtonExtraire As Button
    Friend WithEvents ButtonAnnuler As Button
    Friend WithEvents ButtonRepSauvegarde As Button
    Friend WithEvents CheckBoxEjectCD As CheckBox
    Friend WithEvents CheckBoxVerouillerCD As CheckBox
    Friend WithEvents LabelCommentaire As Label
    Friend WithEvents ListViewCompress As ListView
    Friend WithEvents ColumnHeaderPiste As ColumnHeader
    Friend WithEvents ColumnHeaderDébut As ColumnHeader
    Friend WithEvents ColumnHeaderLongueur As ColumnHeader
    Friend WithEvents ColumnHeaderTaille As ColumnHeader
    Friend WithEvents ColumnHeaderTailleComp As ColumnHeader
    Friend WithEvents LabelPisteEnCours As Label
    Friend WithEvents ProgressBarPisteActuelle As ProgressBar
    Friend WithEvents ProgressBarGlobale As ProgressBar
    Friend WithEvents LabelProgressionGlobale As Label
    Friend WithEvents CheckBox_FCompress_SelectDeselect As CheckBox
    Friend WithEvents Button_EditTracks As Button
    Friend WithEvents ButtonSoumettreGnuDB As Button
    Friend WithEvents ColumnHeaderTitre As ColumnHeader
    Friend WithEvents ColumnHeaderArtiste As ColumnHeader
    Friend WithEvents Button_Image_Erase As Button
    Friend WithEvents Button_Image_Suiv As Button
    Friend WithEvents Button_Image_Prec As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Label_Im_Site As Label
    Friend WithEvents Label_Image_Site As Label
    Friend WithEvents NumericUpDown_DB As NumericUpDown
    Friend WithEvents Label4 As Label
    Friend WithEvents Label_Normalisation As Label
End Class
