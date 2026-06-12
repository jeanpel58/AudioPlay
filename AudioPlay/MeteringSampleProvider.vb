Imports NAudio.Wave

''' <summary>
''' Sample provider qui mesure le niveau audio RMS (Root Mean Square)
''' pour alimenter un VU-meter en temps réel
''' </summary>
Public Class MeteringSampleProvider
    Implements ISampleProvider

    Private ReadOnly source As ISampleProvider
    Private m_level As Single = 0.0F
    Private sampleCount As Integer = 0
    Private sumSquares As Double = 0
    Private Const UPDATE_INTERVAL As Integer = 2048 ' Mise à jour toutes les ~46ms à 44.1kHz

    Public Sub New(sourceProvider As ISampleProvider)
        source = sourceProvider
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return source.WaveFormat
        End Get
    End Property

    ''' <summary>
    ''' Niveau RMS actuel (0.0 = silence, 1.0 = niveau max)
    ''' </summary>
    Public ReadOnly Property Level As Single
        Get
            Return m_level
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim samplesRead As Integer = source.Read(buffer, offset, count)

        ' Calculer le niveau RMS
        For i As Integer = 0 To samplesRead - 1
            Dim sample As Single = buffer(offset + i)
            sumSquares += sample * sample
            sampleCount += 1

            ' Mettre à jour le niveau périodiquement
            If sampleCount >= UPDATE_INTERVAL Then
                Dim rms As Double = Math.Sqrt(sumSquares / sampleCount)
                m_level = CSng(Math.Min(1.0, rms * 2.0)) ' Facteur 2 pour plus de sensibilité

                ' Reset
                sumSquares = 0
                sampleCount = 0
            End If
        Next

        Return samplesRead
    End Function
End Class
