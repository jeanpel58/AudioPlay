Imports NAudio.Wave
Imports System.IO

''' <summary>
''' Détecteur automatique du point de départ audio (Auto-Cue)
''' </summary>
Public Class AutoCueDetector
    Private Const DEFAULT_THRESHOLD As Single = 0.005F ' Seuil de détection par défaut
    Private Const SAMPLE_WINDOW As Integer = 4096 ' Fenêtre d'échantillonnage

    ''' <summary>
    ''' Détecte le premier point audio significatif dans un fichier
    ''' </summary>
    Public Shared Function DetectCuePoint(audioFilePath As String, Optional threshold As Single = DEFAULT_THRESHOLD) As TimeSpan
        If Not File.Exists(audioFilePath) Then
            Return TimeSpan.Zero
        End If

        Try
            Using reader As New AudioFileReader(audioFilePath)
                Dim buffer(SAMPLE_WINDOW - 1) As Single
                Dim totalSamplesRead As Long = 0

                While True
                    Dim samplesRead As Integer = reader.Read(buffer, 0, buffer.Length)
                    If samplesRead = 0 Then Exit While

                    ' Analyser le buffer pour détecter un signal audio
                    For i As Integer = 0 To samplesRead - 1
                        If Math.Abs(buffer(i)) > threshold Then
                            ' Signal détecté, calculer la position temporelle
                            Dim samplePosition As Long = totalSamplesRead + i
                            Dim seconds As Double = samplePosition / CDbl(reader.WaveFormat.SampleRate * reader.WaveFormat.Channels)

                            ' Retourner légèrement en arrière pour ne pas couper l'attaque
                            seconds = Math.Max(0, seconds - 0.05) ' 50ms avant
                            Return TimeSpan.FromSeconds(seconds)
                        End If
                    Next

                    totalSamplesRead += samplesRead
                End While
            End Using
        Catch ex As Exception
            ' Erreur silencieuse, retourner au début
            Return TimeSpan.Zero
        End Try

        ' Aucun signal trouvé, retourner au début
        Return TimeSpan.Zero
    End Function

    ''' <summary>
    ''' Détecte le dernier point audio significatif (fin de piste)
    ''' </summary>
    Public Shared Function DetectEndPoint(audioFilePath As String, Optional threshold As Single = DEFAULT_THRESHOLD) As TimeSpan
        If Not File.Exists(audioFilePath) Then
            Return TimeSpan.Zero
        End If

        Try
            Using reader As New AudioFileReader(audioFilePath)
                Dim duration As TimeSpan = reader.TotalTime
                Dim buffer(SAMPLE_WINDOW - 1) As Single

                ' Commencer par la fin et reculer
                Dim stepSize As Long = SAMPLE_WINDOW * 10 ' Sauter par blocs
                Dim currentPos As Long = reader.Length - (SAMPLE_WINDOW * reader.WaveFormat.BlockAlign)

                While currentPos > 0
                    reader.Position = currentPos
                    Dim samplesRead As Integer = reader.Read(buffer, 0, buffer.Length)

                    ' Analyser en ordre inverse
                    For i As Integer = samplesRead - 1 To 0 Step -1
                        If Math.Abs(buffer(i)) > threshold Then
                            ' Signal détecté
                            Dim samplePosition As Long = (currentPos / reader.WaveFormat.BlockAlign) + i
                            Dim seconds As Double = samplePosition / CDbl(reader.WaveFormat.SampleRate * reader.WaveFormat.Channels)

                            ' Ajouter une petite marge
                            seconds = Math.Min(duration.TotalSeconds, seconds + 0.1) ' 100ms après
                            Return TimeSpan.FromSeconds(seconds)
                        End If
                    Next

                    currentPos -= stepSize * reader.WaveFormat.BlockAlign
                End While

                ' Si aucun signal trouvé, retourner la durée totale
                Return duration
            End Using
        Catch ex As Exception
            Return TimeSpan.Zero
        End Try
    End Function

    ''' <summary>
    ''' Calcule le seuil optimal basé sur le niveau RMS moyen du fichier
    ''' </summary>
    Public Shared Function CalculateOptimalThreshold(audioFilePath As String) As Single
        Try
            Using reader As New AudioFileReader(audioFilePath)
                Dim buffer(8192 - 1) As Single
                Dim totalRMS As Double = 0
                Dim blockCount As Integer = 0
                Dim maxBlocks As Integer = 50 ' Limiter l'analyse

                While blockCount < maxBlocks
                    Dim samplesRead As Integer = reader.Read(buffer, 0, buffer.Length)
                    If samplesRead = 0 Then Exit While

                    ' Calculer RMS pour ce bloc
                    Dim sumSquares As Double = 0
                    For i As Integer = 0 To samplesRead - 1
                        sumSquares += buffer(i) * buffer(i)
                    Next
                    totalRMS += Math.Sqrt(sumSquares / samplesRead)
                    blockCount += 1
                End While

                If blockCount > 0 Then
                    Dim avgRMS As Single = CSng(totalRMS / blockCount)
                    ' Le seuil = 10% du niveau RMS moyen
                    Return Math.Max(0.001F, avgRMS * 0.1F)
                End If
            End Using
        Catch ex As Exception
            ' Erreur, retourner seuil par défaut
        End Try

        Return DEFAULT_THRESHOLD
    End Function
End Class
