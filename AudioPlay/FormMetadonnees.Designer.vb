<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMetadonnees
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormMetadonnees))
        TextBoxInfo = New TextBox()
        ButtonFermer = New Button()
        GroupBoxTags = New GroupBox()
        TextBoxBPM = New TextBox()
        LabelBPM = New Label()
        TextBoxAlbumArtiste = New TextBox()
        LabelAlbumArtiste = New Label()
        TextBoxPiste = New TextBox()
        LabelPiste = New Label()
        TextBoxCommentaire = New TextBox()
        LabelCommentaire = New Label()
        TextBoxGenre = New TextBox()
        LabelGenre = New Label()
        TextBoxAnnee = New TextBox()
        LabelAnnee = New Label()
        TextBoxAlbum = New TextBox()
        LabelAlbum = New Label()
        TextBoxArtiste = New TextBox()
        LabelArtiste = New Label()
        TextBoxTitre = New TextBox()
        LabelTitre = New Label()
        ButtonSauvegarder = New Button()
        GroupBoxTags.SuspendLayout()
        SuspendLayout()
        ' 
        ' TextBoxInfo
        ' 
        TextBoxInfo.Font = New Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        TextBoxInfo.Location = New Point(12, 12)
        TextBoxInfo.Multiline = True
        TextBoxInfo.Name = "TextBoxInfo"
        TextBoxInfo.ReadOnly = True
        TextBoxInfo.ScrollBars = ScrollBars.Vertical
        TextBoxInfo.Size = New Size(660, 300)
        TextBoxInfo.TabIndex = 0
        ' 
        ' ButtonFermer
        ' 
        ButtonFermer.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
        ButtonFermer.Location = New Point(597, 644)
        ButtonFermer.Name = "ButtonFermer"
        ButtonFermer.Size = New Size(75, 30)
        ButtonFermer.TabIndex = 12
        ButtonFermer.Text = LanguageManager.GetString("FormMetadonnees_ButtonFermer_Text")
        ButtonFermer.UseVisualStyleBackColor = True
        ' 
        ' GroupBoxTags
        ' 
        GroupBoxTags.Controls.Add(TextBoxBPM)
        GroupBoxTags.Controls.Add(LabelBPM)
        GroupBoxTags.Controls.Add(TextBoxAlbumArtiste)
        GroupBoxTags.Controls.Add(LabelAlbumArtiste)
        GroupBoxTags.Controls.Add(TextBoxPiste)
        GroupBoxTags.Controls.Add(LabelPiste)
        GroupBoxTags.Controls.Add(TextBoxCommentaire)
        GroupBoxTags.Controls.Add(LabelCommentaire)
        GroupBoxTags.Controls.Add(TextBoxGenre)
        GroupBoxTags.Controls.Add(LabelGenre)
        GroupBoxTags.Controls.Add(TextBoxAnnee)
        GroupBoxTags.Controls.Add(LabelAnnee)
        GroupBoxTags.Controls.Add(TextBoxAlbum)
        GroupBoxTags.Controls.Add(LabelAlbum)
        GroupBoxTags.Controls.Add(TextBoxArtiste)
        GroupBoxTags.Controls.Add(LabelArtiste)
        GroupBoxTags.Controls.Add(TextBoxTitre)
        GroupBoxTags.Controls.Add(LabelTitre)
        GroupBoxTags.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        GroupBoxTags.Location = New Point(12, 318)
        GroupBoxTags.Name = "GroupBoxTags"
        GroupBoxTags.Size = New Size(660, 320)
        GroupBoxTags.TabIndex = 1
        GroupBoxTags.TabStop = False
        GroupBoxTags.Text = LanguageManager.GetString("FormMetadonnees_GroupBoxTags_Text")
        ' 
        ' TextBoxBPM
        ' 
        TextBoxBPM.Location = New Point(125, 286)
        TextBoxBPM.Name = "TextBoxBPM"
        TextBoxBPM.Size = New Size(100, 23)
        TextBoxBPM.TabIndex = 17
        ' 
        ' LabelBPM
        ' 
        LabelBPM.AutoSize = True
        LabelBPM.Font = New Font("Segoe UI", 9.0F)
        LabelBPM.Location = New Point(15, 289)
        LabelBPM.Name = "LabelBPM"
        LabelBPM.Size = New Size(38, 15)
        LabelBPM.TabIndex = 16
        LabelBPM.Text = LanguageManager.GetString("FormMetadonnees_LabelBPM_Text")
        ' 
        ' TextBoxAlbumArtiste
        ' 
        TextBoxAlbumArtiste.Location = New Point(125, 257)
        TextBoxAlbumArtiste.Name = "TextBoxAlbumArtiste"
        TextBoxAlbumArtiste.Size = New Size(520, 23)
        TextBoxAlbumArtiste.TabIndex = 15
        ' 
        ' LabelAlbumArtiste
        ' 
        LabelAlbumArtiste.AutoSize = True
        LabelAlbumArtiste.Font = New Font("Segoe UI", 9.0F)
        LabelAlbumArtiste.Location = New Point(15, 260)
        LabelAlbumArtiste.Name = "LabelAlbumArtiste"
        LabelAlbumArtiste.Size = New Size(86, 15)
        LabelAlbumArtiste.TabIndex = 14
        LabelAlbumArtiste.Text = LanguageManager.GetString("FormMetadonnees_LabelAlbumArtiste_Text")
        ' 
        ' TextBoxPiste
        ' 
        TextBoxPiste.Location = New Point(125, 228)
        TextBoxPiste.Name = "TextBoxPiste"
        TextBoxPiste.Size = New Size(100, 23)
        TextBoxPiste.TabIndex = 13
        ' 
        ' LabelPiste
        ' 
        LabelPiste.AutoSize = True
        LabelPiste.Font = New Font("Segoe UI", 9.0F)
        LabelPiste.Location = New Point(15, 231)
        LabelPiste.Name = "LabelPiste"
        LabelPiste.Size = New Size(71, 15)
        LabelPiste.TabIndex = 12
        LabelPiste.Text = LanguageManager.GetString("FormMetadonnees_LabelPiste_Text")
        ' 
        ' TextBoxCommentaire
        ' 
        TextBoxCommentaire.Location = New Point(125, 170)
        TextBoxCommentaire.Multiline = True
        TextBoxCommentaire.Name = "TextBoxCommentaire"
        TextBoxCommentaire.Size = New Size(520, 52)
        TextBoxCommentaire.TabIndex = 11
        ' 
        ' LabelCommentaire
        ' 
        LabelCommentaire.AutoSize = True
        LabelCommentaire.Font = New Font("Segoe UI", 9.0F)
        LabelCommentaire.Location = New Point(15, 173)
        LabelCommentaire.Name = "LabelCommentaire"
        LabelCommentaire.Size = New Size(86, 15)
        LabelCommentaire.TabIndex = 10
        LabelCommentaire.Text = LanguageManager.GetString("FormMetadonnees_LabelCommentaire_Text")
        ' 
        ' TextBoxGenre
        ' 
        TextBoxGenre.Location = New Point(125, 141)
        TextBoxGenre.Name = "TextBoxGenre"
        TextBoxGenre.Size = New Size(250, 23)
        TextBoxGenre.TabIndex = 9
        ' 
        ' LabelGenre
        ' 
        LabelGenre.AutoSize = True
        LabelGenre.Font = New Font("Segoe UI", 9.0F)
        LabelGenre.Location = New Point(15, 144)
        LabelGenre.Name = "LabelGenre"
        LabelGenre.Size = New Size(44, 15)
        LabelGenre.TabIndex = 8
        LabelGenre.Text = LanguageManager.GetString("FormMetadonnees_LabelGenre_Text")
        ' 
        ' TextBoxAnnee
        ' 
        TextBoxAnnee.Location = New Point(125, 112)
        TextBoxAnnee.Name = "TextBoxAnnee"
        TextBoxAnnee.Size = New Size(100, 23)
        TextBoxAnnee.TabIndex = 7
        ' 
        ' LabelAnnee
        ' 
        LabelAnnee.AutoSize = True
        LabelAnnee.Font = New Font("Segoe UI", 9.0F)
        LabelAnnee.Location = New Point(15, 115)
        LabelAnnee.Name = "LabelAnnee"
        LabelAnnee.Size = New Size(47, 15)
        LabelAnnee.TabIndex = 6
        LabelAnnee.Text = LanguageManager.GetString("FormMetadonnees_LabelAnnee_Text")
        ' 
        ' TextBoxAlbum
        ' 
        TextBoxAlbum.Location = New Point(125, 83)
        TextBoxAlbum.Name = "TextBoxAlbum"
        TextBoxAlbum.Size = New Size(520, 23)
        TextBoxAlbum.TabIndex = 5
        ' 
        ' LabelAlbum
        ' 
        LabelAlbum.AutoSize = True
        LabelAlbum.Font = New Font("Segoe UI", 9.0F)
        LabelAlbum.Location = New Point(15, 86)
        LabelAlbum.Name = "LabelAlbum"
        LabelAlbum.Size = New Size(49, 15)
        LabelAlbum.TabIndex = 4
        LabelAlbum.Text = LanguageManager.GetString("FormMetadonnees_LabelAlbum_Text")
        ' 
        ' TextBoxArtiste
        ' 
        TextBoxArtiste.Location = New Point(125, 54)
        TextBoxArtiste.Name = "TextBoxArtiste"
        TextBoxArtiste.Size = New Size(520, 23)
        TextBoxArtiste.TabIndex = 3
        ' 
        ' LabelArtiste
        ' 
        LabelArtiste.AutoSize = True
        LabelArtiste.Font = New Font("Segoe UI", 9.0F)
        LabelArtiste.Location = New Point(15, 57)
        LabelArtiste.Name = "LabelArtiste"
        LabelArtiste.Size = New Size(47, 15)
        LabelArtiste.TabIndex = 2
        LabelArtiste.Text = LanguageManager.GetString("FormMetadonnees_LabelArtiste_Text")
        ' 
        ' TextBoxTitre
        ' 
        TextBoxTitre.Location = New Point(125, 25)
        TextBoxTitre.Name = "TextBoxTitre"
        TextBoxTitre.Size = New Size(520, 23)
        TextBoxTitre.TabIndex = 1
        TextBoxTitre.TabStop = False
        ' 
        ' LabelTitre
        ' 
        LabelTitre.AutoSize = True
        LabelTitre.Font = New Font("Segoe UI", 9.0F)
        LabelTitre.Location = New Point(15, 28)
        LabelTitre.Name = "LabelTitre"
        LabelTitre.Size = New Size(37, 15)
        LabelTitre.TabIndex = 0
        LabelTitre.Text = LanguageManager.GetString("FormMetadonnees_LabelTitre_Text")
        ' 
        ' ButtonSauvegarder
        ' 
        ButtonSauvegarder.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        ButtonSauvegarder.Location = New Point(479, 644)
        ButtonSauvegarder.Name = "ButtonSauvegarder"
        ButtonSauvegarder.Size = New Size(112, 30)
        ButtonSauvegarder.TabIndex = 11
        ButtonSauvegarder.Text = LanguageManager.GetString("FormMetadonnees_ButtonSauvegarder_Text")
        ButtonSauvegarder.UseVisualStyleBackColor = True
        ' 
        ' FormMetadonnees
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(684, 686)
        Controls.Add(ButtonSauvegarder)
        Controls.Add(GroupBoxTags)
        Controls.Add(ButtonFermer)
        Controls.Add(TextBoxInfo)
        FormBorderStyle = FormBorderStyle.FixedSingle
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximizeBox = False
        MinimizeBox = False
        Name = "FormMetadonnees"
        StartPosition = FormStartPosition.CenterParent
        Text = LanguageManager.GetString("FormMetadonnees_Form_Text")
        GroupBoxTags.ResumeLayout(False)
        GroupBoxTags.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents TextBoxInfo As TextBox
    Friend WithEvents ButtonFermer As Button
    Friend WithEvents GroupBoxTags As GroupBox
    Friend WithEvents LabelTitre As Label
    Friend WithEvents TextBoxTitre As TextBox
    Friend WithEvents TextBoxArtiste As TextBox
    Friend WithEvents LabelArtiste As Label
    Friend WithEvents TextBoxAlbum As TextBox
    Friend WithEvents LabelAlbum As Label
    Friend WithEvents TextBoxAnnee As TextBox
    Friend WithEvents LabelAnnee As Label
    Friend WithEvents TextBoxGenre As TextBox
    Friend WithEvents LabelGenre As Label
    Friend WithEvents TextBoxCommentaire As TextBox
    Friend WithEvents LabelCommentaire As Label
    Friend WithEvents ButtonSauvegarder As Button
    Friend WithEvents TextBoxPiste As TextBox
    Friend WithEvents LabelPiste As Label
    Friend WithEvents TextBoxAlbumArtiste As TextBox
    Friend WithEvents LabelAlbumArtiste As Label
    Friend WithEvents TextBoxBPM As TextBox
    Friend WithEvents LabelBPM As Label

End Class
