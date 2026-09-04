<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form_APropos
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form_APropos))
        Label_APropos_Ligne01 = New Label()
        Label_APropos_Ligne02 = New Label()
        Label_APropos_Ligne03 = New Label()
        Button_Paypal = New Button()
        Button_APropos_OK = New Button()
        Label1 = New Label()
        Label2 = New Label()
        SuspendLayout()
        ' 
        ' Label_APropos_Ligne01
        ' 
        Label_APropos_Ligne01.FlatStyle = FlatStyle.Flat
        Label_APropos_Ligne01.Font = New Font("Segoe UI", 12F, FontStyle.Bold Or FontStyle.Underline, GraphicsUnit.Point, CByte(0))
        Label_APropos_Ligne01.Location = New Point(35, 30)
        Label_APropos_Ligne01.Name = "Label_APropos_Ligne01"
        Label_APropos_Ligne01.Size = New Size(719, 23)
        Label_APropos_Ligne01.TabIndex = 0
        Label_APropos_Ligne01.Text = LanguageManager.GetString("Form_APropos_Label_APropos_Ligne01_Text")
        Label_APropos_Ligne01.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Label_APropos_Ligne02
        ' 
        Label_APropos_Ligne02.FlatStyle = FlatStyle.Flat
        Label_APropos_Ligne02.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label_APropos_Ligne02.Location = New Point(35, 78)
        Label_APropos_Ligne02.Name = "Label_APropos_Ligne02"
        Label_APropos_Ligne02.Size = New Size(719, 49)
        Label_APropos_Ligne02.TabIndex = 1
        Label_APropos_Ligne02.Text = LanguageManager.GetString("Form_APropos_Label_APropos_Ligne02_Text")
        ' 
        ' Label_APropos_Ligne03
        ' 
        Label_APropos_Ligne03.FlatStyle = FlatStyle.Flat
        Label_APropos_Ligne03.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label_APropos_Ligne03.Location = New Point(35, 219)
        Label_APropos_Ligne03.Name = "Label_APropos_Ligne03"
        Label_APropos_Ligne03.Size = New Size(719, 15)
        Label_APropos_Ligne03.TabIndex = 2
        Label_APropos_Ligne03.Text = LanguageManager.GetString("Form_APropos_Label_APropos_Ligne03_Text")
        ' 
        ' Button_Paypal
        ' 
        Button_Paypal.Location = New Point(490, 215)
        Button_Paypal.Name = "Button_Paypal"
        Button_Paypal.Size = New Size(75, 23)
        Button_Paypal.TabIndex = 3
        Button_Paypal.Text = LanguageManager.GetString("Form_APropos_Button_Paypal_Text")
        Button_Paypal.UseVisualStyleBackColor = True
        ' 
        ' Button_APropos_OK
        ' 
        Button_APropos_OK.Location = New Point(713, 268)
        Button_APropos_OK.Name = "Button_APropos_OK"
        Button_APropos_OK.Size = New Size(75, 23)
        Button_APropos_OK.TabIndex = 4
        Button_APropos_OK.Text = LanguageManager.GetString("Form_APropos_Button_APropos_OK_Text")
        Button_APropos_OK.UseVisualStyleBackColor = True
        ' 
        ' Label1
        ' 
        Label1.FlatStyle = FlatStyle.Flat
        Label1.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label1.Location = New Point(35, 176)
        Label1.Name = "Label1"
        Label1.Size = New Size(719, 35)
        Label1.TabIndex = 5
        Label1.Text = LanguageManager.GetString("Form_APropos_Label1_Text")
        ' 
        ' Label2
        ' 
        Label2.FlatStyle = FlatStyle.Flat
        Label2.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold Or FontStyle.Underline, GraphicsUnit.Point, CByte(0))
        Label2.Location = New Point(35, 143)
        Label2.Name = "Label2"
        Label2.Size = New Size(719, 24)
        Label2.TabIndex = 6
        Label2.Text = LanguageManager.GetString("Form_APropos_Label2_Text")
        Label2.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Form_APropos
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 302)
        Controls.Add(Label2)
        Controls.Add(Label1)
        Controls.Add(Button_APropos_OK)
        Controls.Add(Button_Paypal)
        Controls.Add(Label_APropos_Ligne03)
        Controls.Add(Label_APropos_Ligne02)
        Controls.Add(Label_APropos_Ligne01)
        FormBorderStyle = FormBorderStyle.FixedSingle
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "Form_APropos"
        StartPosition = FormStartPosition.CenterScreen
        Text = LanguageManager.GetString("Form_APropos_Form_Text")
        ResumeLayout(False)
    End Sub

    Friend WithEvents Label_APropos_Ligne01 As Label
    Friend WithEvents Label_APropos_Ligne02 As Label
    Friend WithEvents Label_APropos_Ligne03 As Label
    Friend WithEvents Button_Paypal As Button
    Friend WithEvents Button_APropos_OK As Button
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
End Class
