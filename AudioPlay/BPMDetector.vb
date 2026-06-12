Imports NAudio.Wave
Imports SoundTouchSharp

''' <summary>
''' Classe pour détecter le BPM d'un fichier audio
''' Utilise librosa (précis) si disponible, sinon SoundTouch (rapide)
''' </summary>
Public Class BPMDetector
    ' Méthode choisie par l'utilisateur: "Auto", "Librosa", "SoundTouch"
    Public Shared MethodeChoisie As String = "Auto"

    ' Détecter le BPM d'un fichier audio (méthode principale)
    Public Shared Async Function DetecterBPM(cheminFichier As String) As Task(Of Double)
        Select Case MethodeChoisie
            Case "Librosa"
                ' Forcer librosa uniquement
                If PythonManager.EstInstalle() Then
                    Dim bpm = Await PythonManager.DetecterBPMAvecLibrosa(cheminFichier)
                    If bpm > 0 Then
                        Return bpm
                    End If
                End If
                ' Si librosa n'est pas disponible, retourner 0
                Return 0

            Case "SoundTouch"
                ' Forcer SoundTouch uniquement
                Return DetecterBPMAvecSoundTouch(cheminFichier)

            Case Else ' "Auto"
                ' Essayer d'abord avec librosa si disponible
                If PythonManager.EstInstalle() Then
                    Dim bpm = Await PythonManager.DetecterBPMAvecLibrosa(cheminFichier)
                    If bpm > 0 Then
                        Return bpm
                    End If
                End If

                ' Fallback sur SoundTouch
                Return DetecterBPMAvecSoundTouch(cheminFichier)
        End Select
    End Function

    ' Détecter le BPM avec analyse approfondie
    Public Shared Async Function DetecterBPMComplet(cheminFichier As String) As Task(Of Double)
        Select Case MethodeChoisie
            Case "Librosa"
                ' Forcer librosa uniquement
                If PythonManager.EstInstalle() Then
                    Dim bpm = Await PythonManager.DetecterBPMAvecLibrosa(cheminFichier)
                    If bpm > 0 Then
                        Return bpm
                    End If
                End If
                ' Si librosa n'est pas disponible, retourner 0
                Return 0

            Case "SoundTouch"
                ' Forcer SoundTouch uniquement
                Return DetecterBPMCompletAvecSoundTouch(cheminFichier)

            Case Else ' "Auto"
                ' Pour l'analyse complète, utiliser librosa de préférence
                If PythonManager.EstInstalle() Then
                    Dim bpm = Await PythonManager.DetecterBPMAvecLibrosa(cheminFichier)
                    If bpm > 0 Then
                        Return bpm
                    End If
                End If

                ' Fallback sur SoundTouch avec analyse longue
                Return DetecterBPMCompletAvecSoundTouch(cheminFichier)
        End Select
    End Function

    ''' <summary>
    ''' Détection BPM avec SoundTouch (fallback rapide)
    ''' </summary>
    Private Shared Function DetecterBPMAvecSoundTouch(cheminFichier As String) As Double
        Try
            Using reader As New AudioFileReader(cheminFichier)
                ' Analyser le fichier COMPLET (pas de limite de durée)
                Dim dureeAnalyse As TimeSpan = reader.TotalTime

                ' Calculer le nombre d'échantillons à lire (fichier entier)
                Dim nombreEchantillons As Integer = CInt(dureeAnalyse.TotalSeconds * reader.WaveFormat.SampleRate * reader.WaveFormat.Channels)

                ' Limiter à 20 minutes max pour éviter les problèmes de mémoire
                Dim maxEchantillons As Integer = CInt(20 * 60 * reader.WaveFormat.SampleRate * reader.WaveFormat.Channels)
                If nombreEchantillons > maxEchantillons Then
                    nombreEchantillons = maxEchantillons
                End If

                Dim buffer(nombreEchantillons - 1) As Single

                ' Lire les échantillons
                Dim echantillonsLus As Integer = reader.Read(buffer, 0, nombreEchantillons)

                If echantillonsLus = 0 Then
                    Return 0
                End If

                ' Créer l'instance BPMDetect de SoundTouch
                Dim bpmDetect As New BPMDetect(CUInt(reader.WaveFormat.Channels), CUInt(reader.WaveFormat.SampleRate))

                ' Envoyer les échantillons au détecteur de BPM
                bpmDetect.PutSamples(buffer, CUInt(echantillonsLus / reader.WaveFormat.Channels))

                ' Obtenir le BPM détecté
                Dim bpm As Single = bpmDetect.Bpm

                ' Retourner le BPM avec 2 décimales
                If bpm > 0 Then
                    Return Math.Round(bpm, 2)
                Else
                    System.Diagnostics.Debug.WriteLine($"SoundTouch n'a pas pu détecter de BPM pour {cheminFichier} (BPM=0)")
                    Return 0
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur détection BPM SoundTouch {cheminFichier}: {ex.Message}")
            Return 0
        End Try
    End Function

    ''' <summary>
    ''' Détection BPM complète avec SoundTouch
    ''' </summary>
    Private Shared Function DetecterBPMCompletAvecSoundTouch(cheminFichier As String) As Double
        Try
            Using reader As New AudioFileReader(cheminFichier)
                ' Analyser le fichier COMPLET (pas de limite)
                Dim dureeAnalyse As TimeSpan = reader.TotalTime

                ' Calculer le nombre d'échantillons à lire (fichier entier)
                Dim nombreEchantillons As Integer = CInt(dureeAnalyse.TotalSeconds * reader.WaveFormat.SampleRate * reader.WaveFormat.Channels)

                ' Limiter à 20 minutes max pour éviter les problèmes de mémoire
                Dim maxEchantillons As Integer = CInt(20 * 60 * reader.WaveFormat.SampleRate * reader.WaveFormat.Channels)
                If nombreEchantillons > maxEchantillons Then
                    nombreEchantillons = maxEchantillons
                End If

                Dim buffer(nombreEchantillons - 1) As Single

                ' Lire les échantillons
                Dim echantillonsLus As Integer = reader.Read(buffer, 0, nombreEchantillons)

                If echantillonsLus = 0 Then
                    Return 0
                End If

                ' Créer l'instance BPMDetect
                Dim bpmDetect As New BPMDetect(CUInt(reader.WaveFormat.Channels), CUInt(reader.WaveFormat.SampleRate))

                ' Envoyer les échantillons
                bpmDetect.PutSamples(buffer, CUInt(echantillonsLus / reader.WaveFormat.Channels))

                Dim bpm As Single = bpmDetect.Bpm

                If bpm > 0 Then
                    Return Math.Round(bpm, 2)
                Else
                    System.Diagnostics.Debug.WriteLine($"SoundTouch (complet) n'a pas pu détecter de BPM pour {cheminFichier} (BPM=0)")
                    Return 0
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur détection BPM complet SoundTouch {cheminFichier}: {ex.Message}")
            Return 0
        End Try
    End Function
End Class
