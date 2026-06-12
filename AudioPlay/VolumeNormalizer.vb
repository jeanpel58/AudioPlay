Imports NAudio.Wave

' Classe pour analyser et normaliser le volume des fichiers audio
Public Class VolumeNormalizer
    ' Structure pour stocker les informations d'analyse de volume
    Public Class VolumeInfo
        Public Property CheminFichier As String
        Public Property VolumePeak As Single ' Volume maximum trouvé (0.0 à 1.0)
        Public Property VolumeRMS As Single ' Volume moyen (RMS - Root Mean Square)
        Public Property GainSuggere As Single ' Gain à appliquer pour normaliser (multiplicateur)
        Public Property Analysé As Boolean = False
    End Class

    ' Volume cible pour la normalisation (0.0 à 1.0)
    ' 0.8 = 80% du maximum, laisse de la marge pour éviter la saturation
    Private Const VOLUME_CIBLE As Single = 0.8F

    ' Analyser un fichier audio et calculer le gain nécessaire
    Public Shared Function AnalyserFichier(cheminFichier As String) As VolumeInfo
        Dim info As New VolumeInfo With {
            .CheminFichier = cheminFichier,
            .VolumePeak = 0.0F,
            .VolumeRMS = 0.0F,
            .GainSuggere = 1.0F,
            .Analysé = False
        }

        Try
            ' Ouvrir le fichier avec NAudio
            Using reader As New AudioFileReader(cheminFichier)
                ' Lire des échantillons pour analyser le volume
                ' On lit par blocs de 4096 échantillons
                Dim buffer(4095) As Single
                Dim bytesRead As Integer
                Dim sommeCarre As Double = 0
                Dim nombreEchantillons As Long = 0
                Dim maxPeak As Single = 0.0F

                ' Lire tout le fichier par blocs
                ' Pour accélérer, on peut limiter à une portion du fichier
                Dim maxBytesToRead As Long = Math.Min(reader.Length, 1024 * 1024 * 10) ' Max 10 MB
                Dim totalRead As Long = 0

                Do
                    bytesRead = reader.Read(buffer, 0, buffer.Length)
                    If bytesRead = 0 Then Exit Do

                    totalRead += bytesRead

                    ' Analyser ce bloc
                    For i As Integer = 0 To bytesRead - 1
                        Dim echantillon As Single = Math.Abs(buffer(i))

                        ' Mettre à jour le peak
                        If echantillon > maxPeak Then
                            maxPeak = echantillon
                        End If

                        ' Accumuler pour RMS
                        sommeCarre += echantillon * echantillon
                        nombreEchantillons += 1
                    Next

                    ' Arrêter si on a lu assez d'échantillons
                    If totalRead >= maxBytesToRead Then Exit Do
                Loop

                ' Calculer le volume RMS (Root Mean Square)
                If nombreEchantillons > 0 Then
                    info.VolumeRMS = CSng(Math.Sqrt(sommeCarre / nombreEchantillons))
                End If

                info.VolumePeak = maxPeak

                ' Calculer le gain suggéré
                ' On utilise le RMS pour avoir un volume perçu plus cohérent
                If info.VolumeRMS > 0.001F Then ' Éviter division par zéro pour fichiers silencieux
                    info.GainSuggere = VOLUME_CIBLE / info.VolumeRMS

                    ' Limiter le gain pour éviter la saturation
                    ' Si le peak * gain dépasse 1.0, réduire le gain
                    Dim peakAvecGain As Single = info.VolumePeak * info.GainSuggere
                    If peakAvecGain > 0.95F Then
                        info.GainSuggere = 0.95F / info.VolumePeak
                    End If

                    ' Limiter le gain maximum à 3x pour éviter d'amplifier trop le bruit
                    If info.GainSuggere > 3.0F Then
                        info.GainSuggere = 3.0F
                    End If
                Else
                    ' Fichier silencieux ou presque
                    info.GainSuggere = 1.0F
                End If

                info.Analysé = True
            End Using

        Catch ex As Exception
            ' En cas d'erreur, retourner un gain neutre
            System.Diagnostics.Debug.WriteLine($"Erreur analyse volume {cheminFichier}: {ex.Message}")
            info.GainSuggere = 1.0F
            info.Analysé = False
        End Try

        Return info
    End Function

    ' Analyser rapidement un fichier (version allégée, plus rapide)
    Public Shared Function AnalyserFichierRapide(cheminFichier As String) As VolumeInfo
        Dim info As New VolumeInfo With {
            .CheminFichier = cheminFichier,
            .VolumePeak = 0.0F,
            .VolumeRMS = 0.0F,
            .GainSuggere = 1.0F,
            .Analysé = False
        }

        Try
            Using reader As New AudioFileReader(cheminFichier)
                ' Lire seulement les 30 premières secondes pour l'analyse rapide
                Dim buffer(4095) As Single
                Dim bytesRead As Integer
                Dim sommeCarre As Double = 0
                Dim nombreEchantillons As Long = 0
                Dim maxPeak As Single = 0.0F

                ' Limiter à 30 secondes
                Dim maxSamples As Long = CLng(reader.WaveFormat.SampleRate * 30 * reader.WaveFormat.Channels)
                Dim samplesRead As Long = 0

                Do While samplesRead < maxSamples
                    bytesRead = reader.Read(buffer, 0, buffer.Length)
                    If bytesRead = 0 Then Exit Do

                    For i As Integer = 0 To bytesRead - 1
                        Dim echantillon As Single = Math.Abs(buffer(i))
                        If echantillon > maxPeak Then maxPeak = echantillon
                        sommeCarre += echantillon * echantillon
                        nombreEchantillons += 1
                    Next

                    samplesRead += bytesRead
                Loop

                If nombreEchantillons > 0 Then
                    info.VolumeRMS = CSng(Math.Sqrt(sommeCarre / nombreEchantillons))
                End If

                info.VolumePeak = maxPeak

                If info.VolumeRMS > 0.001F Then
                    info.GainSuggere = VOLUME_CIBLE / info.VolumeRMS
                    Dim peakAvecGain As Single = info.VolumePeak * info.GainSuggere
                    If peakAvecGain > 0.95F Then
                        info.GainSuggere = 0.95F / info.VolumePeak
                    End If
                    If info.GainSuggere > 3.0F Then
                        info.GainSuggere = 3.0F
                    End If
                Else
                    info.GainSuggere = 1.0F
                End If

                info.Analysé = True
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur analyse rapide {cheminFichier}: {ex.Message}")
            info.GainSuggere = 1.0F
            info.Analysé = False
        End Try

        Return info
    End Function
End Class
