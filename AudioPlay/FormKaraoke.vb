Imports System.Drawing
Imports System.Windows.Forms

''' <summary>
''' Fenêtre détachée pour l'affichage karaoke CDG
''' </summary>
Public Class FormKaraoke
    Inherits Form
    Private cdgReader As CDGReader
    Private WithEvents timerUpdate As New Timer()
    Private currentCDGPath As String = ""
    Private pictureBox As PictureBox
    Private statusLabel As Label
    Private isPlaying As Boolean = False
    Private currentTimeCallback As Func(Of Double) = Nothing

    Public Sub New()
        MyBase.New()

        ' Configuration de la fenêtre
        Me.Text = LanguageManager.GetString("Karaoke_WindowTitle")
        Me.Size = New Size(600, 450)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.MinimumSize = New Size(400, 300)
        Me.BackColor = Color.Black

        ' PictureBox pour afficher les graphiques CDG
        pictureBox = New PictureBox() With {
            .Dock = DockStyle.Fill,
            .SizeMode = PictureBoxSizeMode.Zoom,
            .BackColor = Color.Black
        }
        Me.Controls.Add(pictureBox)

        ' Label de statut
        statusLabel = New Label() With {
            .Dock = DockStyle.Bottom,
            .Height = 30,
            .TextAlign = ContentAlignment.MiddleCenter,
            .ForeColor = Color.White,
            .BackColor = Color.FromArgb(40, 40, 40),
            .Text = LanguageManager.GetString("Karaoke_NoFile")
        }
        Me.Controls.Add(statusLabel)

        ' Timer pour mise à jour synchronisée
        timerUpdate.Interval = 33 ' ~30 FPS
        AddHandler timerUpdate.Tick, AddressOf TimerUpdate_Tick

        cdgReader = New CDGReader()
    End Sub

    ''' <summary>
    ''' Charge un fichier CDG
    ''' </summary>
    Public Function LoadCDGFile(cdgPath As String) As Boolean
        Try
            If Not IO.File.Exists(cdgPath) Then
                statusLabel.Text = LanguageManager.GetString("Karaoke_FileNotFound")
                Return False
            End If

            If cdgReader.LoadCDGFile(cdgPath) Then
                currentCDGPath = cdgPath
                Dim fileName As String = IO.Path.GetFileName(cdgPath)
                statusLabel.Text = String.Format(LanguageManager.GetString("Karaoke_Loaded"), fileName)
                Return True
            Else
                statusLabel.Text = LanguageManager.GetString("Karaoke_LoadError")
                Return False
            End If
        Catch ex As Exception
            statusLabel.Text = LanguageManager.GetString("Karaoke_LoadError")
            System.Diagnostics.Debug.WriteLine($"Erreur chargement CDG: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Démarre l'affichage karaoke synchronisé
    ''' </summary>
    Public Sub StartPlayback(getTimeCallback As Func(Of Double))
        currentTimeCallback = getTimeCallback
        isPlaying = True
        timerUpdate.Start()
        statusLabel.Text = LanguageManager.GetString("Karaoke_Playing")
    End Sub

    ''' <summary>
    ''' Arrête l'affichage karaoke
    ''' </summary>
    Public Sub StopPlayback()
        isPlaying = False
        timerUpdate.Stop()
        cdgReader.Reset()
        pictureBox.Image = Nothing
        statusLabel.Text = LanguageManager.GetString("Karaoke_Stopped")
    End Sub

    ''' <summary>
    ''' Met en pause l'affichage
    ''' </summary>
    Public Sub PausePlayback()
        isPlaying = False
        timerUpdate.Stop()
        statusLabel.Text = LanguageManager.GetString("Karaoke_Paused")
    End Sub

    ''' <summary>
    ''' Reprend l'affichage
    ''' </summary>
    Public Sub ResumePlayback()
        If currentTimeCallback IsNot Nothing Then
            isPlaying = True
            timerUpdate.Start()
            statusLabel.Text = LanguageManager.GetString("Karaoke_Playing")
        End If
    End Sub

    ''' <summary>
    ''' Timer de mise à jour de l'affichage
    ''' </summary>
    Private Sub TimerUpdate_Tick(sender As Object, e As EventArgs)
        If Not isPlaying OrElse currentTimeCallback Is Nothing Then
            Return
        End If

        Try
            Dim currentTime As Double = currentTimeCallback.Invoke()
            Dim frame As Bitmap = cdgReader.RenderAtTime(currentTime)

            If frame IsNot Nothing Then
                Dim oldImage As Image = pictureBox.Image
                pictureBox.Image = frame
                oldImage?.Dispose()
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur mise à jour karaoke: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Applique le thème actuel à la fenêtre
    ''' </summary>
    Public Sub ApplyCurrentTheme()
        ' Le thème sera appliqué depuis Form1 si nécessaire
        ' Pour l'instant, on garde un style simple
    End Sub

    ''' <summary>
    ''' Nettoyage lors de la fermeture
    ''' </summary>
    Protected Overrides Sub OnClosing(e As System.ComponentModel.CancelEventArgs)
        timerUpdate.Stop()
        pictureBox.Image?.Dispose()
        MyBase.OnClosing(e)
    End Sub

    ''' <summary>
    ''' Rafraîchit les textes selon la langue actuelle
    ''' </summary>
    Public Sub RefreshLanguage()
        Me.Text = LanguageManager.GetString("Karaoke_WindowTitle")
        If String.IsNullOrEmpty(currentCDGPath) Then
            statusLabel.Text = LanguageManager.GetString("Karaoke_NoFile")
        ElseIf isPlaying Then
            statusLabel.Text = LanguageManager.GetString("Karaoke_Playing")
        Else
            Dim fileName As String = IO.Path.GetFileName(currentCDGPath)
            statusLabel.Text = String.Format(LanguageManager.GetString("Karaoke_Loaded"), fileName)
        End If
    End Sub
End Class
