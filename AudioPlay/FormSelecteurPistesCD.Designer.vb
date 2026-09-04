<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormSelecteurPistesCD
    Inherits System.Windows.Forms.Form

    'Form remplace la méthode Dispose pour nettoyer la liste des composants.
    <System.Diagnostics.DebuggerNonUserCode()>
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

    'REMARQUE : la procédure suivante est requise par le Concepteur Windows Form
    'Elle peut être modifiée à l'aide du Concepteur Windows Form.  
    'Ne la modifiez pas à l'aide de l'éditeur de code.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormSelecteurPistesCD))
        lblTitre = New Label()
        lblSourceLabel = New Label()
        comboSourceMetadonnees = New ComboBox()
        btnChargerMetadonnees = New Button()
        lblChargement = New Label()
        checkedListPistes = New CheckedListBox()
        btnOK = New Button()
        btnAnnuler = New Button()
        ButtonExtraction = New Button()
        CheckBox_FSelect_SelectDeselect = New CheckBox()
        SuspendLayout()
        ' 
        ' lblTitre
        ' 
        lblTitre.Font = New Font("Segoe UI", 12F, FontStyle.Bold)
        lblTitre.Location = New Point(20, 20)
        lblTitre.Name = "lblTitre"
        lblTitre.Size = New Size(460, 30)
        lblTitre.TabIndex = 0
        lblTitre.Text = LanguageManager.GetString("FormSelecteurPistesCD_lblTitre_Text")
        lblTitre.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' lblSourceLabel
        ' 
        lblSourceLabel.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblSourceLabel.Location = New Point(20, 55)
        lblSourceLabel.Name = "lblSourceLabel"
        lblSourceLabel.Size = New Size(60, 25)
        lblSourceLabel.TabIndex = 1
        lblSourceLabel.Text = LanguageManager.GetString("FormSelecteurPistesCD_lblSourceLabel_Text")
        lblSourceLabel.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' comboSourceMetadonnees
        ' 
        comboSourceMetadonnees.DropDownStyle = ComboBoxStyle.DropDownList
        comboSourceMetadonnees.Font = New Font("Segoe UI", 9.0F)
        comboSourceMetadonnees.FormattingEnabled = True
        comboSourceMetadonnees.Items.AddRange(New Object() {"GnuDB", "MusicBrainz", "Discogs", "Saisie manuelle"})
        comboSourceMetadonnees.Location = New Point(85, 53)
        comboSourceMetadonnees.Name = "comboSourceMetadonnees"
        comboSourceMetadonnees.Size = New Size(150, 23)
        comboSourceMetadonnees.TabIndex = 2
        ' 
        ' btnChargerMetadonnees
        ' 
        btnChargerMetadonnees.Font = New Font("Segoe UI", 8.0F)
        btnChargerMetadonnees.Location = New Point(245, 52)
        btnChargerMetadonnees.Name = "btnChargerMetadonnees"
        btnChargerMetadonnees.Size = New Size(90, 26)
        btnChargerMetadonnees.TabIndex = 3
        btnChargerMetadonnees.Text = LanguageManager.GetString("FormSelecteurPistesCD_btnChargerMetadonnees_Text")
        btnChargerMetadonnees.UseVisualStyleBackColor = True
        btnChargerMetadonnees.Visible = False
        ' 
        ' lblChargement
        ' 
        lblChargement.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        lblChargement.ForeColor = Color.Gray
        lblChargement.Location = New Point(20, 91)
        lblChargement.Name = "lblChargement"
        lblChargement.Size = New Size(460, 20)
        lblChargement.TabIndex = 6
        lblChargement.Text = LanguageManager.GetString("FormSelecteurPistesCD_lblChargement_Text")
        lblChargement.TextAlign = ContentAlignment.MiddleLeft
        lblChargement.Visible = False
        ' 
        ' checkedListPistes
        ' 
        checkedListPistes.CheckOnClick = True
        checkedListPistes.Font = New Font("Consolas", 9.0F)
        checkedListPistes.FormattingEnabled = True
        checkedListPistes.Location = New Point(20, 136)
        checkedListPistes.Name = "checkedListPistes"
        checkedListPistes.Size = New Size(460, 344)
        checkedListPistes.TabIndex = 7
        ' 
        ' btnOK
        ' 
        btnOK.DialogResult = DialogResult.OK
        btnOK.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        btnOK.Location = New Point(280, 553)
        btnOK.Name = "btnOK"
        btnOK.Size = New Size(100, 35)
        btnOK.TabIndex = 10
        btnOK.Text = LanguageManager.GetString("FormSelecteurPistesCD_btnOK_Text")
        btnOK.UseVisualStyleBackColor = True
        ' 
        ' btnAnnuler
        ' 
        btnAnnuler.DialogResult = DialogResult.Cancel
        btnAnnuler.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        btnAnnuler.Location = New Point(390, 553)
        btnAnnuler.Name = "btnAnnuler"
        btnAnnuler.Size = New Size(90, 35)
        btnAnnuler.TabIndex = 11
        btnAnnuler.Text = LanguageManager.GetString("FormSelecteurPistesCD_btnAnnuler_Text")
        btnAnnuler.UseVisualStyleBackColor = True
        ' 
        ' ButtonExtraction
        ' 
        ButtonExtraction.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        ButtonExtraction.Location = New Point(20, 486)
        ButtonExtraction.Name = "ButtonExtraction"
        ButtonExtraction.Size = New Size(460, 35)
        ButtonExtraction.TabIndex = 12
        ButtonExtraction.Text = LanguageManager.GetString("FormSelecteurPistesCD_ButtonExtraction_Text")
        ButtonExtraction.UseVisualStyleBackColor = True
        ' 
        ' CheckBox_FSelect_SelectDeselect
        ' 
        CheckBox_FSelect_SelectDeselect.AutoSize = True
        CheckBox_FSelect_SelectDeselect.Checked = True
        CheckBox_FSelect_SelectDeselect.CheckState = CheckState.Checked
        CheckBox_FSelect_SelectDeselect.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBox_FSelect_SelectDeselect.Location = New Point(20, 114)
        CheckBox_FSelect_SelectDeselect.Name = "CheckBox_FSelect_SelectDeselect"
        CheckBox_FSelect_SelectDeselect.Size = New Size(288, 19)
        CheckBox_FSelect_SelectDeselect.TabIndex = 105
        CheckBox_FSelect_SelectDeselect.Text = LanguageManager.GetString("FormSelecteurPistesCD_CheckBox_FSelect_SelectDeselect_Text")
        CheckBox_FSelect_SelectDeselect.UseVisualStyleBackColor = True
        ' 
        ' FormSelect
        ' 
        AcceptButton = btnOK
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        CancelButton = btnAnnuler
        ClientSize = New Size(498, 600)
        Controls.Add(CheckBox_FSelect_SelectDeselect)
        Text = LanguageManager.GetString("FormSelecteurPistesCD_Form_Text")
        Controls.Add(ButtonExtraction)
        Controls.Add(btnAnnuler)
        Controls.Add(btnOK)
        Controls.Add(checkedListPistes)
        Controls.Add(lblChargement)
        Controls.Add(btnChargerMetadonnees)
        Controls.Add(comboSourceMetadonnees)
        Controls.Add(lblSourceLabel)
        Controls.Add(lblTitre)
        FormBorderStyle = FormBorderStyle.FixedDialog
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximizeBox = False
        MinimizeBox = False
        Name = "FormSelect"
        StartPosition = FormStartPosition.CenterParent
        ' Text already set via LanguageManager above
        ResumeLayout(False)
        PerformLayout()

    End Sub

    Friend WithEvents lblTitre As Label
    Friend WithEvents lblSourceLabel As Label
    Friend WithEvents comboSourceMetadonnees As ComboBox
    Friend WithEvents btnChargerMetadonnees As Button
    Friend WithEvents lblChargement As Label
    Friend WithEvents checkedListPistes As CheckedListBox
    Friend WithEvents btnOK As Button
    Friend WithEvents btnAnnuler As Button
    Friend WithEvents ButtonExtraction As Button
    Friend WithEvents CheckBox_FSelect_SelectDeselect As CheckBox
End Class
