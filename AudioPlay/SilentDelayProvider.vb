Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders

''' <summary>
''' Générateur de silence pour une durée spécifique (utilisé pour le délai du métronome sans son)
''' </summary>
Public Class SilentDelayProvider
    Implements ISampleProvider

    Private ReadOnly sampleRate As Integer
    Private position As Long = 0
    Private ReadOnly totalSamples As Long
    Private _estTermine As Boolean = False

    ''' <summary>
    ''' Crée un provider de silence basé sur le BPM et le nombre de beats
    ''' </summary>
    Public Sub New(sampleRate As Integer, bpm As Double, nombreBeats As Integer)
        Me.sampleRate = sampleRate

        ' Calculer la durée totale en secondes
        Dim secondesParBeat As Double = 60.0 / bpm
        Dim dureeTotale As Double = secondesParBeat * nombreBeats

        ' Calculer le nombre total de samples nécessaires
        Me.totalSamples = CLng(dureeTotale * sampleRate)
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1)
        End Get
    End Property

    Public ReadOnly Property EstTermine As Boolean
        Get
            Return _estTermine
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim samplesEcrits As Integer = 0

        For i As Integer = 0 To count - 1
            If position >= totalSamples Then
                ' Le délai silencieux est terminé
                _estTermine = True
                Return samplesEcrits
            End If

            ' Écrire du silence (0.0)
            buffer(offset + i) = 0.0F
            position += 1
            samplesEcrits += 1
        Next

        Return samplesEcrits
    End Function
End Class

''' <summary>
''' Séquenceur qui joue d'abord un délai silencieux puis l'audio principal
''' Utilisé quand seule la lumière LED du métronome est active (pas de son)
''' </summary>
Public Class SilentDelayAudioSequencer
    Implements ISampleProvider

    Private silentDelay As SilentDelayProvider
    Private audioProvider As ISampleProvider
    Private phaseDelay As Boolean = True
    Private ReadOnly format As WaveFormat

    Public Sub New(silentDelay As SilentDelayProvider, audioProvider As ISampleProvider)
        Me.silentDelay = silentDelay
        Me.audioProvider = audioProvider

        ' Utiliser le format de l'audio (peut être stéréo)
        Me.format = audioProvider.WaveFormat
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return format
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        If phaseDelay Then
            ' Phase de délai silencieux
            Dim channelCount = format.Channels

            If channelCount = 1 Then
                ' Audio mono : lecture directe du silence
                Dim lu = silentDelay.Read(buffer, offset, count)

                If silentDelay.EstTermine Then
                    phaseDelay = False
                    System.Diagnostics.Debug.WriteLine("Délai silencieux terminé, passage à l'audio principal")

                    ' Si le délai n'a pas rempli tout le buffer, continuer avec l'audio
                    If lu < count Then
                        Dim luAudio = audioProvider.Read(buffer, offset + lu, count - lu)
                        Return lu + luAudio
                    End If
                End If

                Return lu
            Else
                ' Audio stéréo : dupliquer le silence sur chaque canal
                Dim samplesMonoBuffer(count \ channelCount - 1) As Single
                Dim luMono = silentDelay.Read(samplesMonoBuffer, 0, samplesMonoBuffer.Length)

                ' Dupliquer chaque sample mono (0.0) sur tous les canaux
                For i As Integer = 0 To luMono - 1
                    For ch As Integer = 0 To channelCount - 1
                        buffer(offset + i * channelCount + ch) = samplesMonoBuffer(i)
                    Next
                Next

                Dim luTotal = luMono * channelCount

                If silentDelay.EstTermine Then
                    phaseDelay = False
                    System.Diagnostics.Debug.WriteLine("Délai silencieux terminé, passage à l'audio principal")

                    ' Si le délai n'a pas rempli tout le buffer, continuer avec l'audio
                    If luTotal < count Then
                        Dim luAudio = audioProvider.Read(buffer, offset + luTotal, count - luTotal)
                        Return luTotal + luAudio
                    End If
                End If

                Return luTotal
            End If
        Else
            ' Lire depuis l'audio principal
            Return audioProvider.Read(buffer, offset, count)
        End If
    End Function
End Class

