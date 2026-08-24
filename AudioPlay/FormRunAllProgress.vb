Imports System.Windows.Forms
Imports System.Drawing
Imports System.Diagnostics
Imports System.IO

Public Class FormRunAllProgress
    Inherits Form

    Private ReadOnly progressBar As ProgressBar
    Private ReadOnly statusLabel As Label
    Private ReadOnly logBox As TextBox
    Private ReadOnly btnCancel As Button
    Private ReadOnly btnCopyAll As Button
    Private ReadOnly btnDone As Button
    Private ReadOnly btnOpenFolders As Button
    Private sessionFolderPath As String = Nothing
    Private diagnosticsFolderPath As String = Nothing

    Public Event CancelRequested()

    Public Sub New()
        Me.Text = "Run all progress"
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.StartPosition = FormStartPosition.CenterParent
        Me.ClientSize = New Size(640, 360)
        Me.MaximizeBox = True
        Me.MinimizeBox = True

        progressBar = New ProgressBar() With {
            .Location = New Point(12, 12),
            .Size = New Size(616, 20),
            .Minimum = 0,
            .Maximum = 1,
            .Value = 0,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        }

        statusLabel = New Label() With {
            .Location = New Point(12, 40),
            .Size = New Size(616, 20),
            .Text = String.Empty,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        }

        logBox = New TextBox() With {
            .Location = New Point(12, 64),
            .Size = New Size(616, 240),
            .Multiline = True,
            .ReadOnly = True,
            .ScrollBars = ScrollBars.Vertical,
            .Visible = True,
            .Font = New Font("Consolas", 9.0F, FontStyle.Regular),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        }

        btnCancel = New Button() With {
            .Location = New Point(552, 312),
            .Size = New Size(76, 28),
            .Text = "Cancel",
            .Enabled = True,
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        }

        btnCopyAll = New Button() With {
            .Location = New Point(12, 312),
            .Size = New Size(120, 28),
            .Text = "Copy all text",
            .Enabled = False,
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        }

        btnOpenFolders = New Button() With {
            .Location = New Point(140, 312),
            .Size = New Size(160, 28),
            .Text = "Open session + proposals",
            .Enabled = False,
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        }

        btnDone = New Button() With {
            .Location = New Point(388, 312),
            .Size = New Size(120, 28),
            .Text = "Done",
            .Enabled = False,
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right
        }

        Me.Controls.Add(progressBar)
        Me.Controls.Add(statusLabel)
        Me.Controls.Add(logBox)
        Me.Controls.Add(btnCancel)
        Me.Controls.Add(btnCopyAll)
        Me.Controls.Add(btnOpenFolders)
        Me.Controls.Add(btnDone)

        AddHandler btnCopyAll.Click, AddressOf BtnCopyAll_Click
        AddHandler btnDone.Click, AddressOf BtnDone_Click
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click
        AddHandler btnOpenFolders.Click, AddressOf BtnOpenFolders_Click
    End Sub

    Public Sub SetMaximum(max As Integer)
        Try
            progressBar.Maximum = Math.Max(1, max)
        Catch
        End Try
    End Sub

    Public Sub SetValue(value As Integer)
        Try
            Dim v = Math.Max(progressBar.Minimum, Math.Min(progressBar.Maximum, value))
            progressBar.Value = v
            Try
                CDAudioAnalyzer.DiagnosticWrite($"FormRunAllProgress.SetValue: set to {v}")
            Catch
            End Try
        Catch
        End Try
    End Sub

    Public Sub SetStatus(text As String)
        Try
            statusLabel.Text = text
            ' Do not duplicate status in the log by default; caller can AppendLogLine explicitly
            Try
                CDAudioAnalyzer.DiagnosticWrite($"FormRunAllProgress.SetStatus: {text}")
            Catch
            End Try
        Catch
        End Try
    End Sub

    Public Sub AppendLogLine(line As String)
        Try
            If logBox Is Nothing Then Return
            If Not logBox.Visible Then
                logBox.Visible = True
            End If
            logBox.AppendText(line & Environment.NewLine)
            Try
                CDAudioAnalyzer.DiagnosticWrite($"FormRunAllProgress.AppendLogLine: {line}")
            Catch
            End Try
            ' autoscroll to end
            Try
                logBox.SelectionStart = logBox.Text.Length
                logBox.ScrollToCaret()
            Catch
            End Try
        Catch
        End Try
    End Sub

    Private Sub BtnCopyAll_Click(sender As Object, e As EventArgs)
        Try
            If logBox Is Nothing Then Return
            If String.IsNullOrEmpty(logBox.Text) Then Return
            Try
                Clipboard.SetText(logBox.Text)
            Catch ex As Exception
                ' ignore clipboard errors
            End Try
        Catch
        End Try
    End Sub

    Private Sub BtnDone_Click(sender As Object, e As EventArgs)
        Try
            Me.CloseSafe()
        Catch
        End Try
    End Sub

    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        Try
            RaiseEvent CancelRequested()
            ' Keep Cancel enabled; disable other buttons to prevent interaction while cancellation processes
            Try
                btnCopyAll.Enabled = False
                btnOpenFolders.Enabled = False
                btnDone.Enabled = False
            Catch
            End Try
        Catch
        End Try
    End Sub

    Private Sub BtnOpenFolders_Click(sender As Object, e As EventArgs)
        Try
            Try
                If Not String.IsNullOrEmpty(sessionFolderPath) AndAlso Directory.Exists(sessionFolderPath) Then
                    Process.Start("explorer.exe", sessionFolderPath)
                End If
            Catch
            End Try
            Try
                If Not String.IsNullOrEmpty(diagnosticsFolderPath) AndAlso Directory.Exists(diagnosticsFolderPath) Then
                    Process.Start("explorer.exe", diagnosticsFolderPath)
                End If
            Catch
            End Try
        Catch
        End Try
    End Sub

    Public Sub EnableCompletion(Optional sessionFolder As String = Nothing)
        Try
            ' Only update sessionFolderPath if a non-empty value was provided.
            If Not String.IsNullOrEmpty(sessionFolder) Then
                sessionFolderPath = sessionFolder
            End If
            btnDone.Enabled = True
            btnCopyAll.Enabled = True
            btnOpenFolders.Enabled = True
        Catch
        End Try
    End Sub

    Public Sub SetFolders(sessionFolder As String, diagnosticsFolder As String)
        Try
            sessionFolderPath = sessionFolder
            diagnosticsFolderPath = diagnosticsFolder
            If Not String.IsNullOrEmpty(sessionFolderPath) Or Not String.IsNullOrEmpty(diagnosticsFolderPath) Then
                btnOpenFolders.Enabled = True
            End If
        Catch
        End Try
    End Sub

    Public Sub CloseSafe()
        Try
            If Not Me.IsDisposed Then Me.Close()
        Catch
        End Try
    End Sub
End Class
