<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormInputEmail
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormInputEmail))
        LabelPrompt = New Label()
        TextBoxEmail = New TextBox()
        ButtonOK = New Button()
        ButtonCancel = New Button()
        SuspendLayout()
        ' 
        ' LabelPrompt
        ' 
        LabelPrompt.Location = New Point(15, 15)
        LabelPrompt.Name = "LabelPrompt"
        LabelPrompt.Size = New Size(360, 65)
        LabelPrompt.TabIndex = 0
        LabelPrompt.Text = LanguageManager.GetString("FormInputEmail_LabelPrompt_Text")
        ' 
        ' TextBoxEmail
        ' 
        TextBoxEmail.Location = New Point(15, 83)
        TextBoxEmail.Name = "TextBoxEmail"
        TextBoxEmail.Size = New Size(360, 23)
        TextBoxEmail.TabIndex = 1
        ' 
        ' ButtonOK
        ' 
        ButtonOK.Location = New Point(210, 126)
        ButtonOK.Name = "ButtonOK"
        ButtonOK.Size = New Size(80, 30)
        ButtonOK.TabIndex = 2
        ButtonOK.Text = LanguageManager.GetString("FormInputEmail_ButtonOK_Text")
        ButtonOK.UseVisualStyleBackColor = True
        ' 
        ' ButtonCancel
        ' 
        ButtonCancel.Location = New Point(295, 126)
        ButtonCancel.Name = "ButtonCancel"
        ButtonCancel.Size = New Size(80, 30)
        ButtonCancel.TabIndex = 3
        ButtonCancel.Text = LanguageManager.GetString("FormInputEmail_ButtonCancel_Text")
        ButtonCancel.UseVisualStyleBackColor = True
        ' 
        ' FormInputEmail
        ' 
        AcceptButton = ButtonOK
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        CancelButton = ButtonCancel
        ClientSize = New Size(394, 168)
        Controls.Add(ButtonCancel)
        Controls.Add(ButtonOK)
        Controls.Add(TextBoxEmail)
        Controls.Add(LabelPrompt)
        FormBorderStyle = FormBorderStyle.FixedDialog
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximizeBox = False
        MinimizeBox = False
        Name = "FormInputEmail"
        StartPosition = FormStartPosition.CenterParent
        Text = LanguageManager.GetString("FormInputEmail_Form_Text")
        ResumeLayout(False)
        PerformLayout()

    End Sub

    Friend WithEvents LabelPrompt As Label
    Friend WithEvents TextBoxEmail As TextBox
    Friend WithEvents ButtonOK As Button
    Friend WithEvents ButtonCancel As Button
End Class
