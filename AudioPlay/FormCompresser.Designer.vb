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
        components = New ComponentModel.Container()
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
        ContextMenuStripPictureBox = New ContextMenuStrip(components)
        tsmiSearchCover = New ToolStripMenuItem()
        tsmiAddCoverFromFile = New ToolStripMenuItem()
        tsmiSizeMenu = New ToolStripMenuItem()
        tsmiSizeNormal = New ToolStripMenuItem()
        tsmiSizeStretch = New ToolStripMenuItem()
        tsmiSizeZoom = New ToolStripMenuItem()
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
        ProgressBarPisteActuelle = New CustomProgressBar()
        ProgressBarGlobale = New CustomProgressBar()
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
        GroupBoxAnalyzerOptions = New GroupBox()
        Button_Aide_MaxStartTrim = New Button()
        Button_Aide_MinSilence = New Button()
        Button_Aide_WindowAfter = New Button()
        Button_Aide_WindowBefore = New Button()
        LabelWindowBefore = New Label()
        NumericWindowBefore = New NumericUpDown()
        LabelWindowAfter = New Label()
        NumericWindowAfter = New NumericUpDown()
        LabelMinSilence = New Label()
        NumericMinSilence = New NumericUpDown()
        LabelMaxStartTrim = New Label()
        NumericMaxStartTrim = New NumericUpDown()
        Button_Agrandir = New Button()
        Button_rapetisser = New Button()
        ToolTipPictureBox = New ToolTip(components)
        CType(PictureBoxPochette, ComponentModel.ISupportInitialize).BeginInit()
        ContextMenuStripPictureBox.SuspendLayout()
        CType(NumericUpDown_DB, ComponentModel.ISupportInitialize).BeginInit()
        GroupBoxAnalyzerOptions.SuspendLayout()
        CType(NumericWindowBefore, ComponentModel.ISupportInitialize).BeginInit()
        CType(NumericWindowAfter, ComponentModel.ISupportInitialize).BeginInit()
        CType(NumericMinSilence, ComponentModel.ISupportInitialize).BeginInit()
        CType(NumericMaxStartTrim, ComponentModel.ISupportInitialize).BeginInit()
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
        Label_CDTitre.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        TextBoxCDTitre.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        TextBoxCDTitre.Location = New Point(123, 73)
        TextBoxCDTitre.Name = "TextBoxCDTitre"
        TextBoxCDTitre.Size = New Size(262, 18)
        TextBoxCDTitre.TabIndex = 2
        ' 
        ' TextBoxCDArtiste
        ' 
        TextBoxCDArtiste.BorderStyle = BorderStyle.None
        TextBoxCDArtiste.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        TextBoxCDArtiste.Location = New Point(123, 107)
        TextBoxCDArtiste.Name = "TextBoxCDArtiste"
        TextBoxCDArtiste.Size = New Size(262, 18)
        TextBoxCDArtiste.TabIndex = 4
        ' 
        ' LabelCDArtiste
        ' 
        LabelCDArtiste.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        Label_ChoixLecteur.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        TextBoxAnnee.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        TextBoxAnnee.Location = New Point(123, 141)
        TextBoxAnnee.Name = "TextBoxAnnee"
        TextBoxAnnee.Size = New Size(59, 18)
        TextBoxAnnee.TabIndex = 7
        ' 
        ' LabelAnnee
        ' 
        LabelAnnee.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        LabelGenre.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LabelGenre.Location = New Point(213, 141)
        LabelGenre.Name = "LabelGenre"
        LabelGenre.Size = New Size(53, 23)
        LabelGenre.TabIndex = 9
        LabelGenre.Text = "Genre"
        LabelGenre.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label3
        ' 
        Label3.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        PictureBoxPochette.ContextMenuStrip = ContextMenuStripPictureBox
        PictureBoxPochette.Location = New Point(399, 34)
        PictureBoxPochette.Name = "PictureBoxPochette"
        PictureBoxPochette.Size = New Size(200, 200)
        PictureBoxPochette.TabIndex = 11
        PictureBoxPochette.TabStop = False
        ToolTipPictureBox.SetToolTip(PictureBoxPochette, "Clic droit pour options: rechercher / ajouter image")
        ' 
        ' ContextMenuStripPictureBox
        ' 
        ContextMenuStripPictureBox.Items.AddRange(New ToolStripItem() {tsmiSearchCover, tsmiAddCoverFromFile, tsmiSizeMenu})
        ContextMenuStripPictureBox.Name = "ContextMenuStripPictureBox"
        ContextMenuStripPictureBox.Size = New Size(197, 70)
        ' 
        ' tsmiSearchCover
        ' 
        tsmiSearchCover.Name = "tsmiSearchCover"
        tsmiSearchCover.Size = New Size(196, 22)
        tsmiSearchCover.Text = "Rechercher pochette"
        ' 
        ' tsmiAddCoverFromFile
        ' 
        tsmiAddCoverFromFile.Name = "tsmiAddCoverFromFile"
        tsmiAddCoverFromFile.Size = New Size(196, 22)
        tsmiAddCoverFromFile.Text = "Ajouter depuis fichier..."
        ' 
        ' tsmiSizeMenu
        ' 
        tsmiSizeMenu.DropDownItems.AddRange(New ToolStripItem() {tsmiSizeNormal, tsmiSizeStretch, tsmiSizeZoom})
        tsmiSizeMenu.Name = "tsmiSizeMenu"
        tsmiSizeMenu.Size = New Size(196, 22)
        tsmiSizeMenu.Text = "Affichage"
        ' 
        ' tsmiSizeNormal
        ' 
        tsmiSizeNormal.Name = "tsmiSizeNormal"
        tsmiSizeNormal.Size = New Size(154, 22)
        tsmiSizeNormal.Text = "Tel quel"
        ' 
        ' tsmiSizeStretch
        ' 
        tsmiSizeStretch.Name = "tsmiSizeStretch"
        tsmiSizeStretch.Size = New Size(154, 22)
        tsmiSizeStretch.Text = "Étiré"
        ' 
        ' tsmiSizeZoom
        ' 
        tsmiSizeZoom.Name = "tsmiSizeZoom"
        tsmiSizeZoom.Size = New Size(154, 22)
        tsmiSizeZoom.Text = "Ajuster (Zoom)"
        ' 
        ' LabelDimImagText
        ' 
        LabelDimImagText.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LabelDimImagText.Location = New Point(399, 237)
        LabelDimImagText.Name = "LabelDimImagText"
        LabelDimImagText.Size = New Size(132, 23)
        LabelDimImagText.TabIndex = 12
        LabelDimImagText.Text = "Dimensions image:"
        LabelDimImagText.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' LabelTailleImagText
        ' 
        LabelTailleImagText.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        LabelNumCD.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LabelNumCD.Location = New Point(399, 354)
        LabelNumCD.Name = "LabelNumCD"
        LabelNumCD.Size = New Size(132, 23)
        LabelNumCD.TabIndex = 17
        LabelNumCD.Text = "Numéro de CD:"
        LabelNumCD.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' LabelPremierNumPiste
        ' 
        LabelPremierNumPiste.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        TextBoxPremierNumPiste.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
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
        TextBoxNumCD.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
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
        TextBoxCommentaire.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        TextBoxCommentaire.Location = New Point(123, 180)
        TextBoxCommentaire.Name = "TextBoxCommentaire"
        TextBoxCommentaire.Size = New Size(262, 18)
        TextBoxCommentaire.TabIndex = 21
        ' 
        ' LabelTypeConversion
        ' 
        LabelTypeConversion.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        LabelQualiteConversion.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LabelQualiteConversion.Location = New Point(220, 211)
        LabelQualiteConversion.Name = "LabelQualiteConversion"
        LabelQualiteConversion.Size = New Size(165, 23)
        LabelQualiteConversion.TabIndex = 26
        LabelQualiteConversion.Text = "Qualité de conversion:"
        LabelQualiteConversion.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' LabelRepSauvegarde
        ' 
        LabelRepSauvegarde.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        TextBoxRepSauvegarde.Font = New Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
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
        ButtonQuitter.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonQuitter.Location = New Point(495, 719)
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
        ButtonExtraire.Location = New Point(373, 760)
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
        ButtonAnnuler.Location = New Point(373, 719)
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
        ButtonRepSauvegarde.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        CheckBoxEjectCD.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        CheckBoxVerouillerCD.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxVerouillerCD.Location = New Point(17, 359)
        CheckBoxVerouillerCD.Name = "CheckBoxVerouillerCD"
        CheckBoxVerouillerCD.Size = New Size(319, 19)
        CheckBoxVerouillerCD.TabIndex = 34
        CheckBoxVerouillerCD.Text = "Verrouiller le plateau du lecteur pendant l'extraction"
        CheckBoxVerouillerCD.UseVisualStyleBackColor = True
        ' 
        ' LabelCommentaire
        ' 
        LabelCommentaire.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        ListViewCompress.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ListViewCompress.FullRowSelect = True
        ListViewCompress.GridLines = True
        ListViewCompress.Location = New Point(17, 422)
        ListViewCompress.Name = "ListViewCompress"
        ListViewCompress.OwnerDraw = True
        ListViewCompress.Size = New Size(582, 233)
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
        LabelPisteEnCours.Location = New Point(17, 671)
        LabelPisteEnCours.Name = "LabelPisteEnCours"
        LabelPisteEnCours.Size = New Size(0, 15)
        LabelPisteEnCours.TabIndex = 100
        LabelPisteEnCours.Visible = False
        ' 
        ' ProgressBarPisteActuelle
        ' 
        ProgressBarPisteActuelle.FillColor = SystemColors.Highlight
        ProgressBarPisteActuelle.Location = New Point(17, 689)
        ProgressBarPisteActuelle.Name = "ProgressBarPisteActuelle"
        ProgressBarPisteActuelle.Size = New Size(340, 20)
        ProgressBarPisteActuelle.TabIndex = 101
        ProgressBarPisteActuelle.Visible = False
        ' 
        ' ProgressBarGlobale
        ' 
        ProgressBarGlobale.FillColor = SystemColors.Highlight
        ProgressBarGlobale.Location = New Point(17, 734)
        ProgressBarGlobale.Name = "ProgressBarGlobale"
        ProgressBarGlobale.Size = New Size(340, 20)
        ProgressBarGlobale.TabIndex = 103
        ProgressBarGlobale.Visible = False
        ' 
        ' LabelProgressionGlobale
        ' 
        LabelProgressionGlobale.AutoSize = True
        LabelProgressionGlobale.Location = New Point(17, 716)
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
        CheckBox_FCompress_SelectDeselect.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        Button_EditTracks.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        ButtonSoumettreGnuDB.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonSoumettreGnuDB.Location = New Point(435, 661)
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
        Button_Image_Erase.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        Button_Image_Suiv.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        Button_Image_Prec.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
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
        Label4.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label4.Location = New Point(391, 398)
        Label4.Name = "Label4"
        Label4.Size = New Size(51, 22)
        Label4.TabIndex = 113
        Label4.Text = "%"
        Label4.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label_Normalisation
        ' 
        Label_Normalisation.Font = New Font("Segoe UI", 6.75F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label_Normalisation.Location = New Point(330, 381)
        Label_Normalisation.Name = "Label_Normalisation"
        Label_Normalisation.Size = New Size(97, 14)
        Label_Normalisation.TabIndex = 114
        Label_Normalisation.Text = "Normalization"
        Label_Normalisation.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' GroupBoxAnalyzerOptions
        ' 
        GroupBoxAnalyzerOptions.Controls.Add(Button_Aide_MaxStartTrim)
        GroupBoxAnalyzerOptions.Controls.Add(Button_Aide_MinSilence)
        GroupBoxAnalyzerOptions.Controls.Add(Button_Aide_WindowAfter)
        GroupBoxAnalyzerOptions.Controls.Add(Button_Aide_WindowBefore)
        GroupBoxAnalyzerOptions.Controls.Add(LabelWindowBefore)
        GroupBoxAnalyzerOptions.Controls.Add(NumericWindowBefore)
        GroupBoxAnalyzerOptions.Controls.Add(LabelWindowAfter)
        GroupBoxAnalyzerOptions.Controls.Add(NumericWindowAfter)
        GroupBoxAnalyzerOptions.Controls.Add(LabelMinSilence)
        GroupBoxAnalyzerOptions.Controls.Add(NumericMinSilence)
        GroupBoxAnalyzerOptions.Controls.Add(LabelMaxStartTrim)
        GroupBoxAnalyzerOptions.Controls.Add(NumericMaxStartTrim)
        GroupBoxAnalyzerOptions.Location = New Point(17, 785)
        GroupBoxAnalyzerOptions.Name = "GroupBoxAnalyzerOptions"
        GroupBoxAnalyzerOptions.Size = New Size(340, 140)
        GroupBoxAnalyzerOptions.TabIndex = 110
        GroupBoxAnalyzerOptions.TabStop = False
        GroupBoxAnalyzerOptions.Text = "Options d'analyse"
        ' 
        ' Button_Aide_MaxStartTrim
        ' 
        Button_Aide_MaxStartTrim.FlatAppearance.MouseDownBackColor = Color.Red
        Button_Aide_MaxStartTrim.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_Aide_MaxStartTrim.FlatStyle = FlatStyle.Flat
        Button_Aide_MaxStartTrim.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_Aide_MaxStartTrim.Location = New Point(172, 110)
        Button_Aide_MaxStartTrim.Name = "Button_Aide_MaxStartTrim"
        Button_Aide_MaxStartTrim.Size = New Size(22, 24)
        Button_Aide_MaxStartTrim.TabIndex = 117
        Button_Aide_MaxStartTrim.Text = "?"
        Button_Aide_MaxStartTrim.UseVisualStyleBackColor = True
        ' 
        ' Button_Aide_MinSilence
        ' 
        Button_Aide_MinSilence.FlatAppearance.MouseDownBackColor = Color.Red
        Button_Aide_MinSilence.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_Aide_MinSilence.FlatStyle = FlatStyle.Flat
        Button_Aide_MinSilence.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_Aide_MinSilence.Location = New Point(172, 77)
        Button_Aide_MinSilence.Name = "Button_Aide_MinSilence"
        Button_Aide_MinSilence.Size = New Size(22, 28)
        Button_Aide_MinSilence.TabIndex = 117
        Button_Aide_MinSilence.Text = "?"
        Button_Aide_MinSilence.UseVisualStyleBackColor = True
        ' 
        ' Button_Aide_WindowAfter
        ' 
        Button_Aide_WindowAfter.FlatAppearance.MouseDownBackColor = Color.Red
        Button_Aide_WindowAfter.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_Aide_WindowAfter.FlatStyle = FlatStyle.Flat
        Button_Aide_WindowAfter.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_Aide_WindowAfter.Location = New Point(172, 47)
        Button_Aide_WindowAfter.Name = "Button_Aide_WindowAfter"
        Button_Aide_WindowAfter.Size = New Size(22, 28)
        Button_Aide_WindowAfter.TabIndex = 117
        Button_Aide_WindowAfter.Text = "?"
        Button_Aide_WindowAfter.UseVisualStyleBackColor = True
        ' 
        ' Button_Aide_WindowBefore
        ' 
        Button_Aide_WindowBefore.FlatAppearance.MouseDownBackColor = Color.Red
        Button_Aide_WindowBefore.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_Aide_WindowBefore.FlatStyle = FlatStyle.Flat
        Button_Aide_WindowBefore.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_Aide_WindowBefore.Location = New Point(172, 16)
        Button_Aide_WindowBefore.Name = "Button_Aide_WindowBefore"
        Button_Aide_WindowBefore.Size = New Size(22, 26)
        Button_Aide_WindowBefore.TabIndex = 117
        Button_Aide_WindowBefore.Text = "?"
        Button_Aide_WindowBefore.UseVisualStyleBackColor = True
        ' 
        ' LabelWindowBefore
        ' 
        LabelWindowBefore.Location = New Point(12, 22)
        LabelWindowBefore.Name = "LabelWindowBefore"
        LabelWindowBefore.Size = New Size(160, 20)
        LabelWindowBefore.TabIndex = 0
        LabelWindowBefore.Text = "Fenêtre avant TOC (s):"
        ' 
        ' NumericWindowBefore
        ' 
        NumericWindowBefore.Location = New Point(200, 20)
        NumericWindowBefore.Maximum = New Decimal(New Integer() {120, 0, 0, 0})
        NumericWindowBefore.Minimum = New Decimal(New Integer() {5, 0, 0, 0})
        NumericWindowBefore.Name = "NumericWindowBefore"
        NumericWindowBefore.Size = New Size(120, 23)
        NumericWindowBefore.TabIndex = 1
        NumericWindowBefore.Value = New Decimal(New Integer() {20, 0, 0, 0})
        ' 
        ' LabelWindowAfter
        ' 
        LabelWindowAfter.Location = New Point(12, 52)
        LabelWindowAfter.Name = "LabelWindowAfter"
        LabelWindowAfter.Size = New Size(160, 20)
        LabelWindowAfter.TabIndex = 2
        LabelWindowAfter.Text = "Fenêtre après TOC (s):"
        ' 
        ' NumericWindowAfter
        ' 
        NumericWindowAfter.Location = New Point(200, 52)
        NumericWindowAfter.Maximum = New Decimal(New Integer() {120, 0, 0, 0})
        NumericWindowAfter.Minimum = New Decimal(New Integer() {5, 0, 0, 0})
        NumericWindowAfter.Name = "NumericWindowAfter"
        NumericWindowAfter.Size = New Size(120, 23)
        NumericWindowAfter.TabIndex = 3
        NumericWindowAfter.Value = New Decimal(New Integer() {20, 0, 0, 0})
        ' 
        ' LabelMinSilence
        ' 
        LabelMinSilence.Location = New Point(12, 82)
        LabelMinSilence.Name = "LabelMinSilence"
        LabelMinSilence.Size = New Size(160, 20)
        LabelMinSilence.TabIndex = 4
        LabelMinSilence.Text = "Silence minimal soutenu (s):"
        ' 
        ' NumericMinSilence
        ' 
        NumericMinSilence.DecimalPlaces = 2
        NumericMinSilence.Increment = New Decimal(New Integer() {1, 0, 0, 131072})
        NumericMinSilence.Location = New Point(200, 82)
        NumericMinSilence.Minimum = New Decimal(New Integer() {1, 0, 0, 131072})
        NumericMinSilence.Name = "NumericMinSilence"
        NumericMinSilence.Size = New Size(120, 23)
        NumericMinSilence.TabIndex = 5
        NumericMinSilence.Value = New Decimal(New Integer() {50, 0, 0, 131072})
        ' 
        ' LabelMaxStartTrim
        ' 
        LabelMaxStartTrim.Location = New Point(12, 112)
        LabelMaxStartTrim.Name = "LabelMaxStartTrim"
        LabelMaxStartTrim.Size = New Size(160, 20)
        LabelMaxStartTrim.TabIndex = 6
        LabelMaxStartTrim.Text = "Trim début max (s):"
        ' 
        ' NumericMaxStartTrim
        ' 
        NumericMaxStartTrim.DecimalPlaces = 2
        NumericMaxStartTrim.Increment = New Decimal(New Integer() {1, 0, 0, 131072})
        NumericMaxStartTrim.Location = New Point(200, 112)
        NumericMaxStartTrim.Name = "NumericMaxStartTrim"
        NumericMaxStartTrim.Size = New Size(120, 23)
        NumericMaxStartTrim.TabIndex = 7
        NumericMaxStartTrim.Value = New Decimal(New Integer() {8, 0, 0, 0})
        ' 
        ' Button_Agrandir
        ' 
        Button_Agrandir.FlatAppearance.MouseDownBackColor = Color.Red
        Button_Agrandir.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_Agrandir.FlatStyle = FlatStyle.Flat
        Button_Agrandir.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_Agrandir.Location = New Point(593, 760)
        Button_Agrandir.Name = "Button_Agrandir"
        Button_Agrandir.Size = New Size(22, 24)
        Button_Agrandir.TabIndex = 115
        Button_Agrandir.Text = "▼"
        Button_Agrandir.UseVisualStyleBackColor = True
        ' 
        ' Button_rapetisser
        ' 
        Button_rapetisser.FlatAppearance.MouseDownBackColor = Color.Red
        Button_rapetisser.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_rapetisser.FlatStyle = FlatStyle.Flat
        Button_rapetisser.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_rapetisser.Location = New Point(593, 901)
        Button_rapetisser.Name = "Button_rapetisser"
        Button_rapetisser.Size = New Size(22, 24)
        Button_rapetisser.TabIndex = 116
        Button_rapetisser.Text = "▲"
        Button_rapetisser.UseVisualStyleBackColor = True
        ' 
        ' ToolTipPictureBox
        ' 
        ToolTipPictureBox.AutoPopDelay = 5000
        ToolTipPictureBox.InitialDelay = 500
        ToolTipPictureBox.ReshowDelay = 200
        ToolTipPictureBox.ShowAlways = True
        ' 
        ' FormCompresser
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(617, 929)
        Controls.Add(Button_rapetisser)
        Controls.Add(Button_Agrandir)
        Controls.Add(GroupBoxAnalyzerOptions)
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
        ContextMenuStripPictureBox.ResumeLayout(False)
        CType(NumericUpDown_DB, ComponentModel.ISupportInitialize).EndInit()
        GroupBoxAnalyzerOptions.ResumeLayout(False)
        CType(NumericWindowBefore, ComponentModel.ISupportInitialize).EndInit()
        CType(NumericWindowAfter, ComponentModel.ISupportInitialize).EndInit()
        CType(NumericMinSilence, ComponentModel.ISupportInitialize).EndInit()
        CType(NumericMaxStartTrim, ComponentModel.ISupportInitialize).EndInit()
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
    Friend WithEvents ContextMenuStripPictureBox As ContextMenuStrip
    Friend WithEvents tsmiSearchCover As ToolStripMenuItem
    Friend WithEvents tsmiAddCoverFromFile As ToolStripMenuItem
    Friend WithEvents ToolTipPictureBox As ToolTip
    Friend WithEvents tsmiSizeMenu As ToolStripMenuItem
    Friend WithEvents tsmiSizeNormal As ToolStripMenuItem
    Friend WithEvents tsmiSizeStretch As ToolStripMenuItem
    Friend WithEvents tsmiSizeZoom As ToolStripMenuItem
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
    Friend WithEvents ProgressBarPisteActuelle As CustomProgressBar
    Friend WithEvents ProgressBarGlobale As CustomProgressBar
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
    Friend WithEvents GroupBoxAnalyzerOptions As GroupBox
    Friend WithEvents LabelWindowBefore As Label
    Friend WithEvents NumericWindowBefore As NumericUpDown
    Friend WithEvents LabelWindowAfter As Label
    Friend WithEvents NumericWindowAfter As NumericUpDown
    Friend WithEvents LabelMinSilence As Label
    Friend WithEvents NumericMinSilence As NumericUpDown
    Friend WithEvents LabelMaxStartTrim As Label
    Friend WithEvents NumericMaxStartTrim As NumericUpDown
    Friend WithEvents Button_Agrandir As Button
    Friend WithEvents Button_rapetisser As Button
    Friend WithEvents Button_Aide_MaxStartTrim As Button
    Friend WithEvents Button_Aide_MinSilence As Button
    Friend WithEvents Button_Aide_WindowAfter As Button
    Friend WithEvents Button_Aide_WindowBefore As Button
End Class
