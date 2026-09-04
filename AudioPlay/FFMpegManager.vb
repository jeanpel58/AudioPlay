Imports System.IO
Imports System.Net.Http
Imports System.IO.Compression
Imports System.Linq

''' <summary>
''' Gestionnaire pour le téléchargement et l'installation automatique de FFMpeg
''' </summary>
Public Class FFMpegManager

    Private Shared ReadOnly httpClient As New HttpClient()
    Private Const FFMPEG_DOWNLOAD_URL As String = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
    Private Const FFMPEG_FALLBACK_URL As String = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip"

    Shared Sub New()
        httpClient.Timeout = TimeSpan.FromMinutes(10) ' Timeout de 10 minutes pour les gros téléchargements
    End Sub

    ''' <summary>
    ''' Vérifie si FFMpeg est installé
    ''' </summary>
    Public Shared Function EstInstalle() As Boolean
        Dim cheminFFMpeg = ObtenirCheminFFMpeg()
        Return Not String.IsNullOrEmpty(cheminFFMpeg) AndAlso File.Exists(cheminFFMpeg)
    End Function

    ''' <summary>
    ''' Obtient le chemin de ffmpeg.exe s'il existe
    ''' </summary>
    Public Shared Function ObtenirCheminFFMpeg() As String
        ' Chemins possibles pour ffmpeg.exe
        Dim cheminsPossibles As String() = {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "ffmpeg.exe"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "ffmpeg", "bin", "ffmpeg.exe")
        }

        For Each chemin In cheminsPossibles
            If File.Exists(chemin) Then
                Return chemin
            End If
        Next

        ' Tester si ffmpeg est dans le PATH système
        Try
            Dim processInfo As New ProcessStartInfo With {
                .FileName = "ffmpeg.exe",
                .Arguments = "-version",
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardOutput = True
            }

            Dim process As Process = Process.Start(processInfo)
            process.WaitForExit(1000)
            If process.HasExited AndAlso process.ExitCode = 0 Then
                process.Dispose()
                Return "ffmpeg.exe" ' Dans le PATH
            End If
            process.Dispose()
        Catch
            ' FFMpeg n'est pas dans le PATH
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Télécharge et installe FFMpeg automatiquement
    ''' </summary>
    Public Shared Async Function TelechargerEtInstaller(progressCallback As Action(Of Integer, String)) As Task(Of Boolean)
        Try
            ' Créer le dossier Tools si nécessaire
            Dim dossierTools = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools")
            If Not Directory.Exists(dossierTools) Then
                Directory.CreateDirectory(dossierTools)
            End If

            ' Chemin de destination
            Dim cheminFFMpegFinal = Path.Combine(dossierTools, "ffmpeg.exe")

            ' Télécharger FFMpeg
            progressCallback?.Invoke(0, "Connexion au serveur de téléchargement...")

            Dim cheminArchive = Path.Combine(Path.GetTempPath(), $"ffmpeg_{Guid.NewGuid()}.zip")

            Try
                ' Tenter le téléchargement depuis la source principale
                Dim telechargeAvecSucces = Await TelechargerFichier(FFMPEG_DOWNLOAD_URL, cheminArchive, progressCallback)

                If Not telechargeAvecSucces Then
                    ' Essayer la source alternative
                    progressCallback?.Invoke(0, "Tentative avec une source alternative...")
                    telechargeAvecSucces = Await TelechargerFichier(FFMPEG_FALLBACK_URL, cheminArchive, progressCallback)
                End If

                If Not telechargeAvecSucces Then
                    Throw New Exception("Impossible de télécharger FFMpeg depuis les sources disponibles.")
                End If

                ' Extraire ffmpeg.exe de l'archive
                progressCallback?.Invoke(90, "Extraction de ffmpeg.exe...")

                Using archive As ZipArchive = ZipFile.OpenRead(cheminArchive)
                    ' Chercher ffmpeg.exe dans l'archive
                    Dim entryFFMpeg = archive.Entries.FirstOrDefault(Function(e) e.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))

                    If entryFFMpeg IsNot Nothing Then
                        ' Extraire directement dans Tools/
                        entryFFMpeg.ExtractToFile(cheminFFMpegFinal, True)
                    Else
                        ' Chercher dans les sous-dossiers (bin/)
                        entryFFMpeg = archive.Entries.FirstOrDefault(Function(e) e.FullName.Contains("bin") AndAlso e.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))

                        If entryFFMpeg IsNot Nothing Then
                            entryFFMpeg.ExtractToFile(cheminFFMpegFinal, True)
                        Else
                            Throw New Exception("ffmpeg.exe introuvable dans l'archive téléchargée.")
                        End If
                    End If
                End Using

                progressCallback?.Invoke(100, "Installation terminée !")
                Return True

            Finally
                ' Nettoyer le fichier temporaire
                If File.Exists(cheminArchive) Then
                    Try
                        File.Delete(cheminArchive)
                    Catch
                        ' Ignorer les erreurs de suppression
                    End Try
                End If
            End Try

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FFMpegManager] Erreur installation: {ex.Message}")
            progressCallback?.Invoke(-1, $"Erreur: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Télécharge un fichier avec suivi de progression
    ''' </summary>
    Private Shared Async Function TelechargerFichier(url As String, cheminDestination As String,
                                                      progressCallback As Action(Of Integer, String)) As Task(Of Boolean)
        Try
            Using response = Await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                If Not response.IsSuccessStatusCode Then
                    Return False
                End If

                Dim totalBytes = response.Content.Headers.ContentLength

                Using contentStream = Await response.Content.ReadAsStreamAsync(),
                      fileStream As New FileStream(cheminDestination, FileMode.Create, FileAccess.Write, FileShare.None, 8192, True)

                    Dim buffer(8191) As Byte
                    Dim bytesRead As Long = 0
                    Dim bytesLus As Integer

                    Do
                        bytesLus = Await contentStream.ReadAsync(buffer, 0, buffer.Length)
                        If bytesLus > 0 Then
                            Await fileStream.WriteAsync(buffer, 0, bytesLus)
                            bytesRead += bytesLus

                            ' Calculer la progression
                            If totalBytes.HasValue Then
                                Dim pourcentage = CInt((bytesRead * 100) / totalBytes.Value)
                                Dim tailleTelechargee = FormatTaille(bytesRead)
                                Dim tailleTotal = FormatTaille(totalBytes.Value)
                                progressCallback?.Invoke(Math.Min(pourcentage, 89),
                                                        $"Téléchargement en cours: {tailleTelechargee} / {tailleTotal}")
                            Else
                                progressCallback?.Invoke(50, $"Téléchargement en cours: {FormatTaille(bytesRead)}")
                            End If
                        End If
                    Loop While bytesLus > 0
                End Using
            End Using

            Return True

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FFMpegManager] Erreur téléchargement depuis {url}: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Formate une taille en octets en unité lisible (MB, GB)
    ''' </summary>
    Private Shared Function FormatTaille(bytes As Long) As String
        If bytes >= 1073741824 Then ' GB
            Return $"{bytes / 1073741824.0:F2} GB"
        ElseIf bytes >= 1048576 Then ' MB
            Return $"{bytes / 1048576.0:F2} MB"
        ElseIf bytes >= 1024 Then ' KB
            Return $"{bytes / 1024.0:F2} KB"
        Else
            Return $"{bytes} octets"
        End If
    End Function

    ''' <summary>
    ''' Obtient la taille approximative du téléchargement
    ''' </summary>
    Public Shared Function ObtenirTailleTelechargemEnt() As String
        Return "~120 MB" ' Taille approximative de l'archive FFMpeg essentials
    End Function

End Class
