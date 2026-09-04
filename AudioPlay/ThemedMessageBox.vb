Imports System.Windows.Forms

Public NotInheritable Class ThemedMessageBox
    Private Sub New()
    End Sub

    Public Shared Function Show(owner As IWin32Window, text As String, title As String, buttons As MessageBoxButtons, icon As MessageBoxIcon) As DialogResult
        Try
            ' Use a simple themed form with one or two buttons depending on buttons param
            Using dlg As New FormConfirmCancel(text, title)
                ' Localize button text if keys present
                dlg.LocalizeButtons()
                ' Apply the global theme via ThemeManager if available
                Try
                    ThemeManager.ApplyThemeToForm(dlg)
                Catch
                    Try
                        dlg.ApplyTheme(DirectCast(owner, Form))
                    Catch
                    End Try
                End Try

                ' Adjust buttons for OK-only scenarios
                Select Case buttons
                    Case MessageBoxButtons.OK
                        dlg.Controls.Clear()
                        Dim lbl = New Label() With {.AutoSize = False, .Text = text, .Size = New Drawing.Size(360, 80), .Location = New Drawing.Point(12, 12)}
                        Dim btn = New Button() With {.Text = LanguageManager.GetString("OK"), .DialogResult = DialogResult.OK, .Size = New Drawing.Size(80, 28), .Location = New Drawing.Point(292, 100)}
                        dlg.Controls.Add(lbl)
                        dlg.Controls.Add(btn)
                        dlg.AcceptButton = btn
                        ' Ensure topmost and safe owner
                        dlg.TopMost = True
                        Dim realOwner As Form = Nothing
                        Try
                            If owner IsNot Nothing AndAlso TypeOf owner Is Form Then realOwner = DirectCast(owner, Form)
                        Catch
                        End Try
                        If realOwner Is Nothing Then
                            Try
                                realOwner = Application.OpenForms.Cast(Of Form)().FirstOrDefault()
                            Catch
                            End Try
                        End If
                        If realOwner IsNot Nothing Then
                            Return dlg.ShowDialog(realOwner)
                        Else
                            Return dlg.ShowDialog()
                        End If
                    Case MessageBoxButtons.YesNo
                        dlg.TopMost = True
                        Dim realOwner2 As Form = Nothing
                        Try
                            If owner IsNot Nothing AndAlso TypeOf owner Is Form Then realOwner2 = DirectCast(owner, Form)
                        Catch
                        End Try
                        If realOwner2 Is Nothing Then
                            Try
                                realOwner2 = Application.OpenForms.Cast(Of Form)().FirstOrDefault()
                            Catch
                            End Try
                        End If
                        If realOwner2 IsNot Nothing Then
                            Return dlg.ShowDialog(realOwner2)
                        Else
                            Return dlg.ShowDialog()
                        End If
                    Case Else
                        ' Fallback to system MessageBox for other combinations
                        Try
                            Return System.Windows.Forms.MessageBox.Show(owner, text, title, buttons, icon, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly)
                        Catch
                            Return System.Windows.Forms.MessageBox.Show(owner, text, title, buttons, icon)
                        End Try
                End Select
            End Using
        Catch
            ' fallback
            Try
                Return System.Windows.Forms.MessageBox.Show(owner, text, title, buttons, icon, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly)
            Catch
                Return System.Windows.Forms.MessageBox.Show(owner, text, title, buttons, icon)
            End Try
        End Try
    End Function

    Public Shared Function Show(text As String, title As String, buttons As MessageBoxButtons, icon As MessageBoxIcon) As DialogResult
        Return Show(Nothing, text, title, buttons, icon)
    End Function

    Public Shared Function Show(text As String, title As String) As DialogResult
        Return Show(Nothing, text, title, MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Function

    Public Shared Function Show(text As String) As DialogResult
        Return Show(Nothing, text, String.Empty, MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Function
End Class
