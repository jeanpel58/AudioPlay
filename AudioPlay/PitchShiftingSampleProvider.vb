Imports NAudio.Wave

''' <summary>
''' Sample provider simple qui ajuste le pitch en changeant la vitesse de lecture
''' (pitch shifting basique - change tempo ET pitch ensemble, comme sur une vraie platine vinyle DJ)
''' </summary>
Public Class PitchShiftingSampleProvider
    Implements ISampleProvider

    Private ReadOnly sourceProvider As ISampleProvider
    Private m_pitchFactor As Single = 1.0F
    Private position As Double = 0
    Private sourceBuffer() As Single

    Public Sub New(source As ISampleProvider)
        sourceProvider = source
        sourceBuffer = New Single(source.WaveFormat.SampleRate * source.WaveFormat.Channels - 1) {}
    End Sub

    ''' <summary>
    ''' Facteur de pitch : 1.0 = normal, >1.0 = plus rapide/aigu, <1.0 = plus lent/grave
    ''' </summary>
    Public Property PitchFactor As Single
        Get
            Return m_pitchFactor
        End Get
        Set(value As Single)
            ' Limiter entre 0.5 et 1.5 pour éviter les artefacts extrêmes
            m_pitchFactor = Math.Max(0.5F, Math.Min(1.5F, value))
        End Set
    End Property

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return sourceProvider.WaveFormat
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        If Math.Abs(m_pitchFactor - 1.0F) < 0.001F Then
            ' Pas de pitch shift, lecture directe
            Return sourceProvider.Read(buffer, offset, count)
        End If

        Dim samplesRead As Integer = 0
        Dim channels As Integer = WaveFormat.Channels

        While samplesRead < count
            ' Position source (avec interpolation)
            Dim sourceIndex As Integer = CInt(Math.Floor(position))
            Dim sourceFraction As Single = CSng(position - sourceIndex)

            ' Lire suffisamment de samples de la source
            If sourceIndex >= sourceBuffer.Length - channels Then
                Dim samplesFromSource As Integer = sourceProvider.Read(sourceBuffer, 0, sourceBuffer.Length)
                If samplesFromSource = 0 Then Exit While
                sourceIndex = 0
                position = 0
            End If

            ' Interpolation linéaire pour chaque canal
            For ch As Integer = 0 To channels - 1
                Dim idx As Integer = sourceIndex + ch
                Dim sample1 As Single = If(idx < sourceBuffer.Length, sourceBuffer(idx), 0)
                Dim sample2 As Single = If(idx + channels < sourceBuffer.Length, sourceBuffer(idx + channels), sample1)
                buffer(offset + samplesRead) = sample1 + (sample2 - sample1) * sourceFraction
                samplesRead += 1
            Next

            ' Avancer la position selon le pitch factor
            position += channels * m_pitchFactor
        End While

        Return samplesRead
    End Function
End Class
