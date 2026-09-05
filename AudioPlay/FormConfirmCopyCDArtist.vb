Imports System.Windows.Forms

Public Class FormConfirmCopyCDArtist
    Inherits Form

    Private labelMsg As Label
    Private btnYes As Button
    Private btnNo As Button

    Public Sub New(message As String, title As String)
        Me.Text = title
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.ShowInTaskbar = False
        Me.ClientSize = New Size(460, 120)
        ' Apply current theme colors if available (use theme values consistently)
        Try
            Dim theme = ThemeManager.GetCurrentTheme()
            If theme IsNot Nothing Then
                Me.BackColor = theme.FormBackColor
                Me.ForeColor = theme.ControlForeColor
            End If
        Catch
        End Try

        labelMsg = New Label() With {
            .AutoSize = False,
            .Text = message,
            .Location = New Point(12, 12),
            .Size = New Size(Me.ClientSize.Width - 24, 56)
        }

        btnYes = New Button() With {
            .Text = LanguageManager.GetString("Confirm_Yes", "Oui"),
            .DialogResult = DialogResult.Yes,
            .Size = New Size(90, 28),
            .FlatStyle = FlatStyle.Flat
        }
        btnNo = New Button() With {
            .Text = LanguageManager.GetString("Confirm_No", "Non"),
            .DialogResult = DialogResult.No,
            .Size = New Size(90, 28),
            .FlatStyle = FlatStyle.Flat
        }

        ' Position buttons bottom-right
        Dim gap As Integer = 8
        btnNo.Location = New Point(Me.ClientSize.Width - 12 - btnNo.Width, Me.ClientSize.Height - btnNo.Height - 12)
        btnYes.Location = New Point(btnNo.Left - gap - btnYes.Width, btnNo.Top)

        Me.Controls.Add(labelMsg)
        Me.Controls.Add(btnYes)
        Me.Controls.Add(btnNo)

        Me.AcceptButton = btnYes
        Me.CancelButton = btnNo

        ' Ensure label wraps text
        labelMsg.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        btnYes.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        btnNo.Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        ' Apply theme colors to controls if theme available
        Try
            Dim theme2 = ThemeManager.GetCurrentTheme()
            If theme2 IsNot Nothing Then
                ' Label should use text box colors for readability inside form
                labelMsg.BackColor = theme2.TextBoxBackColor
                labelMsg.ForeColor = theme2.TextBoxForeColor

                ' Buttons should follow theme button colors
                btnYes.BackColor = theme2.ButtonBackColor
                btnYes.ForeColor = theme2.ButtonForeColor
                btnNo.BackColor = theme2.ButtonBackColor
                btnNo.ForeColor = theme2.ButtonForeColor

                ' Optional: use AccentColor for Yes hover/back effects if available
                Try
                    btnYes.FlatAppearance.BorderColor = theme2.GroupBoxBorderColor
                    btnNo.FlatAppearance.BorderColor = theme2.GroupBoxBorderColor
                    btnYes.FlatAppearance.MouseOverBackColor = theme2.AccentColor
                    btnNo.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                        Math.Max(0, theme2.AccentColor.R - 30), Math.Max(0, theme2.AccentColor.G - 30), Math.Max(0, theme2.AccentColor.B - 30))
                Catch
                End Try
            End If
        Catch
        End Try
    End Sub
End Class
