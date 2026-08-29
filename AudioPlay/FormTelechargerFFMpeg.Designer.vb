<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormTelechargerFFMpeg
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
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

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormTelechargerFFMpeg))
        LabelTitre = New Label()
        LabelMessage = New Label()
        ProgressBarTelechargemEnt = New CustomProgressBar()
        LabelProgression = New Label()
        ButtonAnnuler = New Button()
        PictureBoxIcon = New PictureBox()
        CType(PictureBoxIcon, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' LabelTitre
        ' 
        LabelTitre.AutoSize = True
        LabelTitre.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        LabelTitre.Location = New Point(80, 20)
        LabelTitre.Name = "LabelTitre"
        LabelTitre.Size = New Size(220, 21)
        LabelTitre.TabIndex = 0
        LabelTitre.Text = "Téléchargement de FFMpeg"
        ' 
        ' LabelMessage
        ' 
        LabelMessage.Location = New Point(80, 50)
        LabelMessage.Name = "LabelMessage"
        LabelMessage.Size = New Size(420, 60)
        LabelMessage.TabIndex = 1
        LabelMessage.Text = "FFMpeg est nécessaire pour extraire en format FLAC et WMA." & vbCrLf & vbCrLf & "AudioPlay va télécharger FFMpeg automatiquement." & vbCrLf & "Taille: ~120 MB"
        ' 
        ' ProgressBarTelechargemEnt
        ' 
        ProgressBarTelechargemEnt.Location = New Point(80, 120)
        ProgressBarTelechargemEnt.Name = "ProgressBarTelechargemEnt"
        ProgressBarTelechargemEnt.Size = New Size(420, 30)
        ProgressBarTelechargemEnt.TabIndex = 2
        ' 
        ' LabelProgression
        ' 
        LabelProgression.Location = New Point(80, 160)
        LabelProgression.Name = "LabelProgression"
        LabelProgression.Size = New Size(420, 20)
        LabelProgression.TabIndex = 3
        LabelProgression.Text = "Préparation du téléchargement..."
        LabelProgression.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' ButtonAnnuler
        ' 
        ButtonAnnuler.Location = New Point(220, 195)
        ButtonAnnuler.Name = "ButtonAnnuler"
        ButtonAnnuler.Size = New Size(100, 30)
        ButtonAnnuler.TabIndex = 4
        ButtonAnnuler.Text = "Annuler"
        ButtonAnnuler.UseVisualStyleBackColor = True
        ' 
        ' PictureBoxIcon
        ' 
        PictureBoxIcon.Location = New Point(20, 20)
        PictureBoxIcon.Name = "PictureBoxIcon"
        PictureBoxIcon.Size = New Size(48, 48)
        PictureBoxIcon.SizeMode = PictureBoxSizeMode.StretchImage
        PictureBoxIcon.TabIndex = 5
        PictureBoxIcon.TabStop = False
        ' 
        ' FormTelechargerFFMpeg
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(534, 241)
        Controls.Add(PictureBoxIcon)
        Controls.Add(ButtonAnnuler)
        Controls.Add(LabelProgression)
        Controls.Add(ProgressBarTelechargemEnt)
        Controls.Add(LabelMessage)
        Controls.Add(LabelTitre)
        FormBorderStyle = FormBorderStyle.FixedDialog
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximizeBox = False
        MinimizeBox = False
        Name = "FormTelechargerFFMpeg"
        StartPosition = FormStartPosition.CenterParent
        Text = "Téléchargement FFMpeg pour AudioPlay"
        CType(PictureBoxIcon, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()

    End Sub

    Friend WithEvents LabelTitre As Label
    Friend WithEvents LabelMessage As Label
    Friend WithEvents ProgressBarTelechargemEnt As CustomProgressBar
    Friend WithEvents LabelProgression As Label
    Friend WithEvents ButtonAnnuler As Button
    Friend WithEvents PictureBoxIcon As PictureBox
End Class
