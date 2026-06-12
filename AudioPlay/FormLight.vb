Imports System.Drawing
Imports System.Windows.Forms
Imports System.Timers

Public Class FormLight
    Private isShowing As Boolean = False
    Private ledRougeImage As Image
    Private ledNoireImage As Image
    Private resetTimer As System.Timers.Timer

    Public Sub New()
        InitializeComponent()

        ' Activer le double buffering
        Me.SetStyle(ControlStyles.OptimizedDoubleBuffer Or
                    ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.UserPaint, True)
        Me.UpdateStyles()

        ' Configuration de la fenêtre
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Size = New Size(200, 200)

        ' Précharger les images
        ledNoireImage = AudioPlay.Resources.AudioPlay_Led_Noir
        ledRougeImage = AudioPlay.Resources.AudioPlay_Led_Rouge

        Me.BackgroundImage = ledNoireImage
        Me.BackgroundImageLayout = ImageLayout.Stretch
        Me.TopMost = True
        Me.ShowInTaskbar = False

        ' Timer haute précision (System.Timers au lieu de Windows.Forms)
        resetTimer = New System.Timers.Timer(50) ' 50ms
        resetTimer.AutoReset = False
        AddHandler resetTimer.Elapsed, AddressOf ResetTimer_Elapsed
    End Sub

    Public Sub ShowLight()
        If Not isShowing Then
            Me.Show()
            isShowing = True
        End If
    End Sub

    Public Sub HideLight()
        If isShowing Then
            resetTimer.Stop()
            Me.Hide()
            isShowing = False
        End If
    End Sub

    Public Sub FlashBeat()
        ' Changement immédiat - cette méthode DOIT être appelée sur le thread UI
        Me.BackgroundImage = ledRougeImage
        Me.Update()

        ' Démarrer timer haute précision pour revenir au noir
        resetTimer.Stop()
        resetTimer.Start()
    End Sub

    Private Sub ResetTimer_Elapsed(sender As Object, e As System.Timers.ElapsedEventArgs)
        ' Ce callback arrive sur un thread du pool, on doit marshaller vers UI
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub()
                              If Not Me.IsDisposed Then
                                  Me.BackgroundImage = ledNoireImage
                                  Me.Update()
                              End If
                          End Sub)
            Else
                Me.BackgroundImage = ledNoireImage
                Me.Update()
            End If
        Catch
            ' Ignorer si la fenêtre est fermée
        End Try
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        resetTimer?.Stop()
        resetTimer?.Dispose()
        ledRougeImage = Nothing
        ledNoireImage = Nothing
        MyBase.OnFormClosing(e)
    End Sub
End Class
