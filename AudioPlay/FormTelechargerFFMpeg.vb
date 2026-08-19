Imports System.Windows.Forms

''' <summary>
''' Formulaire de téléchargement automatique de FFMpeg
''' </summary>
Public Class FormTelechargerFFMpeg

    ' API Windows pour enlever le bouton X
    Private Const SC_CLOSE As Integer = &HF060
    Private Const MF_BYCOMMAND As Integer = &H0

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetSystemMenu(hWnd As IntPtr, bRevert As Boolean) As IntPtr
    End Function

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function RemoveMenu(hMenu As IntPtr, uPosition As UInteger, uFlags As UInteger) As Boolean
    End Function

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Dim hMenu As IntPtr = GetSystemMenu(Me.Handle, False)
        RemoveMenu(hMenu, SC_CLOSE, MF_BYCOMMAND)
    End Sub

    Private annulationDemandee As Boolean = False
    Public Property TelechargemEntReussi As Boolean = False

    Private Sub FormTelechargerFFMpeg_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Charger l'icône de téléchargement
        Try
            PictureBoxIcon.Image = SystemIcons.Information.ToBitmap()
        Catch
            ' Ignorer si l'icône ne peut pas être chargée
        End Try

        ' Appliquer le thème si disponible
        Try
            ThemeManager.ApplyThemeToForm(Me)
        Catch
            ' Ignorer si le thème ne peut pas être appliqué
        End Try

        ' Démarrer le téléchargement automatiquement
        DemarrerTelechargemEnt()
    End Sub

    Private Async Sub DemarrerTelechargemEnt()
        Try
            ButtonAnnuler.Enabled = True
            annulationDemandee = False

            ' Lancer le téléchargement avec callback de progression
            TelechargemEntReussi = Await FFMpegManager.TelechargerEtInstaller(
                Sub(pourcentage As Integer, message As String)
                    ' Mise à jour de l'interface (thread-safe)
                    If Me.InvokeRequired Then
                        Me.Invoke(Sub()
                                      MettreAJourProgression(pourcentage, message)
                                  End Sub)
                    Else
                        MettreAJourProgression(pourcentage, message)
                    End If
                End Sub)

            ' Téléchargement terminé
            If TelechargemEntReussi Then
                LabelProgression.Text = "Installation terminée avec succès !"
                ProgressBarTelechargemEnt.Value = 100
                ButtonAnnuler.Text = "Fermer"

                ' Attendre 1 seconde puis fermer automatiquement
                Await Task.Delay(1000)
                If Not annulationDemandee Then
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                End If
            Else
                If Not annulationDemandee Then
                    MessageBox.Show("Le téléchargement de FFMpeg a échoué." & vbCrLf & vbCrLf &
                                  "Vous pouvez installer FFMpeg manuellement en suivant les instructions dans le fichier:" & vbCrLf &
                                  "Tools\README_FFMPEG.txt",
                                  "Erreur de téléchargement",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Warning)
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                End If
            End If

        Catch ex As Exception
            If Not annulationDemandee Then
                MessageBox.Show($"Erreur lors du téléchargement: {ex.Message}",
                              "Erreur",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error)
                Me.DialogResult = DialogResult.Cancel
                Me.Close()
            End If
        End Try
    End Sub

    Private Sub MettreAJourProgression(pourcentage As Integer, message As String)
        If pourcentage >= 0 AndAlso pourcentage <= 100 Then
            ProgressBarTelechargemEnt.Value = pourcentage
        End If
        LabelProgression.Text = message
        Application.DoEvents()
    End Sub

    Private Sub ButtonAnnuler_Click(sender As Object, e As EventArgs) Handles ButtonAnnuler.Click
        If ButtonAnnuler.Text = "Fermer" Then
            Me.DialogResult = DialogResult.OK
            Me.Close()
        Else
            annulationDemandee = True
            Me.DialogResult = DialogResult.Cancel
            Me.Close()
        End If
    End Sub

    Private Sub FormTelechargerFFMpeg_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not TelechargemEntReussi AndAlso Not annulationDemandee Then
            If MessageBox.Show("Le téléchargement est en cours. Voulez-vous vraiment annuler ?",
                             "Confirmer l'annulation",
                             MessageBoxButtons.YesNo,
                             MessageBoxIcon.Question) = DialogResult.No Then
                e.Cancel = True
            Else
                annulationDemandee = True
            End If
        End If
    End Sub

End Class
