Imports System.Windows.Forms

' Shadow System.Windows.Forms.MessageBox by providing local overloads that forward to ThemedMessageBox.
' Because calls in the code use unqualified MessageBox.Show(...), this class will be preferred by VB resolution.
Public NotInheritable Class MessageBox
    Private Sub New()
    End Sub

    Public Shared Function Show(owner As IWin32Window, text As String, caption As String, buttons As MessageBoxButtons, icon As MessageBoxIcon) As DialogResult
        Try
            ' Ensure themed message boxes appear above other windows
            Return ThemedMessageBox.Show(owner, text, caption, buttons, icon)
        Catch
            Try
                Return System.Windows.Forms.MessageBox.Show(owner, text, caption, buttons, icon, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly)
            Catch
                Return System.Windows.Forms.MessageBox.Show(owner, text, caption, buttons, icon)
            End Try
        End Try
    End Function

    Public Shared Function Show(text As String, caption As String, buttons As MessageBoxButtons, icon As MessageBoxIcon) As DialogResult
        Try
            Return ThemedMessageBox.Show(text, caption, buttons, icon)
        Catch
            Try
                Return System.Windows.Forms.MessageBox.Show(text, caption, buttons, icon, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly)
            Catch
                Return System.Windows.Forms.MessageBox.Show(text, caption, buttons, icon)
            End Try
        End Try
    End Function

    Public Shared Function Show(text As String, caption As String) As DialogResult
        Try
            Return ThemedMessageBox.Show(text, caption)
        Catch
            Try
                Return System.Windows.Forms.MessageBox.Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly)
            Catch
                Return System.Windows.Forms.MessageBox.Show(text, caption)
            End Try
        End Try
    End Function

    Public Shared Function Show(text As String) As DialogResult
        Try
            Return ThemedMessageBox.Show(text)
        Catch
            Try
                Return System.Windows.Forms.MessageBox.Show(text, String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly)
            Catch
                Return System.Windows.Forms.MessageBox.Show(text)
            End Try
        End Try
    End Function

    ' Additional common overloads
    Public Shared Function Show(owner As IWin32Window, text As String) As DialogResult
        Try
            Return ThemedMessageBox.Show(owner, text, String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch
            Return System.Windows.Forms.MessageBox.Show(owner, text)
        End Try
    End Function

    Public Shared Function Show(owner As IWin32Window, text As String, caption As String) As DialogResult
        Try
            Return ThemedMessageBox.Show(owner, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch
            Return System.Windows.Forms.MessageBox.Show(owner, text, caption)
        End Try
    End Function
End Class
