Imports System.Windows.Forms

''' <summary>
''' Formulaire personnalisé pour saisir l'email de l'utilisateur (soumission GnuDB)
''' </summary>
Public Class FormInputEmail

    ' Propriété pour récupérer l'email saisi
    Public Property Email As String = ""

    Private promptText As String = ""
    Private titleText As String = ""

    Public Sub New(prompt As String, title As String, defaultValue As String)
        ' Appeler InitializeComponent généré par le designer
        InitializeComponent()

        ' Stocker les valeurs
        Me.promptText = prompt
        Me.titleText = title

        ' Configurer les propriétés
        Me.TopMost = True
        TextBoxEmail.Text = defaultValue

        ' Appliquer les textes traduits
        Me.Text = LanguageManager.GetString("FormInputEmail_Title")
        LabelPrompt.Text = prompt
        ButtonOK.Text = LanguageManager.GetString("Button_OK")
        ButtonCancel.Text = LanguageManager.GetString("Button_Cancel")
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        ' Appliquer le thème AudioPlay
        ThemeManager.ApplyThemeToForm(Me)

        ' Focus sur le TextBox
        TextBoxEmail.Select()
        TextBoxEmail.SelectAll()
    End Sub

    Private Sub ButtonOK_Click(sender As Object, e As EventArgs) Handles ButtonOK.Click
        Email = TextBoxEmail.Text.Trim()
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub ButtonCancel_Click(sender As Object, e As EventArgs) Handles ButtonCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub
End Class
