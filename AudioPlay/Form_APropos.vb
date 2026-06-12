Public Class Form_APropos
    Private Sub Form_APropos_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ThemeManager.ApplyThemeToForm(Me)
        Me.Text = LanguageManager.GetString("APropos_Title")
        Label_APropos_Ligne01.Text = LanguageManager.GetString("APropos_Ligne01")
        Label_APropos_Ligne02.Text = LanguageManager.GetString("APropos_Ligne02")
        Label_APropos_Ligne03.Text = LanguageManager.GetString("APropos_Ligne03")
        Label2.Text = LanguageManager.GetString("APropos_DonTitre")
        Label1.Text = LanguageManager.GetString("APropos_DonTexte")
        Button_Paypal.Text = LanguageManager.GetString("APropos_Paypal")
        Button_APropos_OK.Text = LanguageManager.GetString("APropos_OK")
    End Sub

    Private Sub Button_Paypal_Click(sender As Object, e As EventArgs) Handles Button_Paypal.Click
        Dim url As String = "https://www.paypal.com/send?recipient=jeanpel58@gmail.com"
        Try
            Process.Start(New ProcessStartInfo(url) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show(
                LanguageManager.GetString("APropos_Error_BrowserOpen"),
                LanguageManager.GetString("Error_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Button_APropos_OK_Click(sender As Object, e As EventArgs) Handles Button_APropos_OK.Click
        Me.Close()
        If Owner IsNot Nothing AndAlso TypeOf Owner Is Form1 Then
            CType(Owner, Form1).ListView1.Focus()
        End If
    End Sub
End Class
