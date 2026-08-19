<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormEditTracks
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormEditTracks))
        Label_PisteText = New Label()
        Label_PisteNumber = New Label()
        Label_TitreText = New Label()
        Label_ArtisteText = New Label()
        TextBoxTitre = New TextBox()
        TextBoxArtiste = New TextBox()
        Button_PisteSuivante = New Button()
        Button_PistePrecedente = New Button()
        ButtonQuitter = New Button()
        Button_EffaceTitre = New Button()
        Button_EffaceArtiste = New Button()
        SuspendLayout()
        ' 
        ' Label_PisteText
        ' 
        Label_PisteText.FlatStyle = FlatStyle.Flat
        Label_PisteText.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label_PisteText.Location = New Point(12, 9)
        Label_PisteText.Name = "Label_PisteText"
        Label_PisteText.Size = New Size(69, 23)
        Label_PisteText.TabIndex = 2
        Label_PisteText.Text = "Piste #"
        Label_PisteText.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label_PisteNumber
        ' 
        Label_PisteNumber.FlatStyle = FlatStyle.Flat
        Label_PisteNumber.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label_PisteNumber.Location = New Point(87, 9)
        Label_PisteNumber.Name = "Label_PisteNumber"
        Label_PisteNumber.Size = New Size(22, 23)
        Label_PisteNumber.TabIndex = 3
        Label_PisteNumber.Text = "0"
        Label_PisteNumber.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label_TitreText
        ' 
        Label_TitreText.FlatStyle = FlatStyle.Flat
        Label_TitreText.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label_TitreText.Location = New Point(12, 43)
        Label_TitreText.Name = "Label_TitreText"
        Label_TitreText.Size = New Size(69, 23)
        Label_TitreText.TabIndex = 4
        Label_TitreText.Text = "Titre :"
        Label_TitreText.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' Label_ArtisteText
        ' 
        Label_ArtisteText.FlatStyle = FlatStyle.Flat
        Label_ArtisteText.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label_ArtisteText.Location = New Point(12, 76)
        Label_ArtisteText.Name = "Label_ArtisteText"
        Label_ArtisteText.Size = New Size(69, 23)
        Label_ArtisteText.TabIndex = 5
        Label_ArtisteText.Text = "Artiste :"
        Label_ArtisteText.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' TextBoxTitre
        ' 
        TextBoxTitre.BorderStyle = BorderStyle.None
        TextBoxTitre.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        TextBoxTitre.Location = New Point(87, 48)
        TextBoxTitre.Name = "TextBoxTitre"
        TextBoxTitre.Size = New Size(458, 18)
        TextBoxTitre.TabIndex = 6
        ' 
        ' TextBoxArtiste
        ' 
        TextBoxArtiste.BorderStyle = BorderStyle.None
        TextBoxArtiste.Font = New Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        TextBoxArtiste.Location = New Point(87, 78)
        TextBoxArtiste.Name = "TextBoxArtiste"
        TextBoxArtiste.Size = New Size(458, 18)
        TextBoxArtiste.TabIndex = 7
        ' 
        ' Button_PisteSuivante
        ' 
        Button_PisteSuivante.FlatAppearance.MouseDownBackColor = Color.Red
        Button_PisteSuivante.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_PisteSuivante.FlatStyle = FlatStyle.Flat
        Button_PisteSuivante.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_PisteSuivante.Location = New Point(325, 114)
        Button_PisteSuivante.Name = "Button_PisteSuivante"
        Button_PisteSuivante.Size = New Size(117, 25)
        Button_PisteSuivante.TabIndex = 33
        Button_PisteSuivante.Text = "Suivante..."
        Button_PisteSuivante.UseVisualStyleBackColor = True
        ' 
        ' Button_PistePrecedente
        ' 
        Button_PistePrecedente.FlatAppearance.MouseDownBackColor = Color.Red
        Button_PistePrecedente.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_PistePrecedente.FlatStyle = FlatStyle.Flat
        Button_PistePrecedente.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_PistePrecedente.Location = New Point(189, 114)
        Button_PistePrecedente.Name = "Button_PistePrecedente"
        Button_PistePrecedente.Size = New Size(117, 25)
        Button_PistePrecedente.TabIndex = 34
        Button_PistePrecedente.Text = "Précédente..."
        Button_PistePrecedente.UseVisualStyleBackColor = True
        ' 
        ' ButtonQuitter
        ' 
        ButtonQuitter.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonQuitter.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonQuitter.FlatStyle = FlatStyle.Flat
        ButtonQuitter.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonQuitter.Location = New Point(467, 164)
        ButtonQuitter.Name = "ButtonQuitter"
        ButtonQuitter.Size = New Size(110, 35)
        ButtonQuitter.TabIndex = 35
        ButtonQuitter.Text = "Quitter"
        ButtonQuitter.UseVisualStyleBackColor = True
        ' 
        ' Button_EffaceTitre
        ' 
        Button_EffaceTitre.FlatAppearance.MouseDownBackColor = Color.Red
        Button_EffaceTitre.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_EffaceTitre.FlatStyle = FlatStyle.Flat
        Button_EffaceTitre.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_EffaceTitre.Location = New Point(551, 45)
        Button_EffaceTitre.Name = "Button_EffaceTitre"
        Button_EffaceTitre.Size = New Size(33, 25)
        Button_EffaceTitre.TabIndex = 36
        Button_EffaceTitre.Text = "X"
        Button_EffaceTitre.UseVisualStyleBackColor = True
        ' 
        ' Button_EffaceArtiste
        ' 
        Button_EffaceArtiste.FlatAppearance.MouseDownBackColor = Color.Red
        Button_EffaceArtiste.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_EffaceArtiste.FlatStyle = FlatStyle.Flat
        Button_EffaceArtiste.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_EffaceArtiste.Location = New Point(551, 75)
        Button_EffaceArtiste.Name = "Button_EffaceArtiste"
        Button_EffaceArtiste.Size = New Size(33, 25)
        Button_EffaceArtiste.TabIndex = 37
        Button_EffaceArtiste.Text = "X"
        Button_EffaceArtiste.UseVisualStyleBackColor = True
        ' 
        ' FormEditTracks
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(589, 211)
        Controls.Add(Button_EffaceArtiste)
        Controls.Add(Button_EffaceTitre)
        Controls.Add(ButtonQuitter)
        Controls.Add(Button_PistePrecedente)
        Controls.Add(Button_PisteSuivante)
        Controls.Add(TextBoxArtiste)
        Controls.Add(TextBoxTitre)
        Controls.Add(Label_ArtisteText)
        Controls.Add(Label_TitreText)
        Controls.Add(Label_PisteNumber)
        Controls.Add(Label_PisteText)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "FormEditTracks"
        StartPosition = FormStartPosition.Manual
        Text = "Édition des pistes..."
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents Label_PisteText As Label
    Friend WithEvents Label_PisteNumber As Label
    Friend WithEvents Label_TitreText As Label
    Friend WithEvents Label_ArtisteText As Label
    Friend WithEvents TextBoxTitre As TextBox
    Friend WithEvents TextBoxArtiste As TextBox
    Friend WithEvents Button_PisteSuivante As Button
    Friend WithEvents Button_PistePrecedente As Button
    Friend WithEvents ButtonQuitter As Button
    Friend WithEvents Button_EffaceTitre As Button
    Friend WithEvents Button_EffaceArtiste As Button
End Class
