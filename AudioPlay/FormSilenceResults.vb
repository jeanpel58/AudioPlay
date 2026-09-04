Imports System.Windows.Forms
Imports System.Drawing

Public Class FormSilenceResults
    Inherits Form

    Private txt As TextBox
    Private btnOK As Button

    Public Sub New(message As String, Optional title As String = Nothing)
        If String.IsNullOrEmpty(title) Then
            Me.Text = LanguageManager.GetString("Detection_Silences_Title")
        Else
            Me.Text = title
        End If
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.TopMost = True

        txt = New TextBox() With {
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Both,
            .Font = New Font("Consolas", 10),
            .WordWrap = True,
            .Text = message
        }

        btnOK = New Button() With {
            .Text = "OK",
            .DialogResult = DialogResult.OK,
            .Size = New Size(100, 30)
        }
        AddHandler btnOK.Click, Sub() Me.Close()

        Dim pnl As New Panel() With {
            .Height = 48
        }

        pnl.Controls.Add(btnOK)

        ' Estimate a sensible size based on message content so the dialog expands to show as much as possible
        Try
            Dim font = txt.Font
            Dim sampleChar = TextRenderer.MeasureText("W", font)
            Dim approxCharWidth = Math.Max(6, sampleChar.Width)
            Dim lineHeight = TextRenderer.MeasureText("A", font).Height

            Dim lines = message.Split(New String() {Environment.NewLine}, StringSplitOptions.None)
            Dim maxLineLen As Integer = 0
            Dim totalWrappedLines As Integer = 0
            For Each l In lines
                maxLineLen = Math.Max(maxLineLen, If(l Is Nothing, 0, l.Length))
            Next

            Dim cols As Integer = Math.Min(160, Math.Max(40, maxLineLen))
            If cols < 60 AndAlso maxLineLen > cols Then
                cols = Math.Min(160, maxLineLen)
            End If

            For Each l In lines
                Dim len = If(l Is Nothing, 0, l.Length)
                Dim wrapped = Math.Max(1, CInt(Math.Ceiling(len / Math.Max(1, cols))))
                totalWrappedLines += wrapped
            Next

            Dim prefWidth = Math.Min(1200, Math.Max(480, cols * approxCharWidth + 40))
            Dim prefHeight = Math.Min(1000, Math.Max(240, totalWrappedLines * lineHeight + 120))

            Me.ClientSize = New Size(prefWidth, prefHeight)
            txt.Location = New Point(10, 10)
            txt.Size = New Size(Me.ClientSize.Width - 20, Me.ClientSize.Height - 70)

            pnl.Location = New Point(0, Me.ClientSize.Height - pnl.Height)
            pnl.Width = Me.ClientSize.Width
            btnOK.Location = New Point(pnl.ClientSize.Width - btnOK.Width - 10, 10)

            Me.Controls.Add(txt)
            Me.Controls.Add(pnl)
        Catch
            ' Fallback layout
            Me.Size = New Size(700, 450)
            txt.Dock = DockStyle.Fill
            pnl.Dock = DockStyle.Bottom
            Me.Controls.Add(txt)
            Me.Controls.Add(pnl)
            AddHandler Me.Load, Sub(sender, e)
                                    btnOK.Location = New Point(Me.ClientSize.Width - btnOK.Width - 10, 10)
                                End Sub
        End Try
    End Sub

    Public Shadows Function ShowDialog(owner As IWin32Window) As DialogResult
        Return MyBase.ShowDialog(owner)
    End Function
End Class
