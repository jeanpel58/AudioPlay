Imports NAudio.Wave

''' <summary>
''' Fournisseur d'échantillons qui applique un effet d'écho amélioré
''' Multi-tap delay avec filtre passe-bas et stéréo ping-pong
''' </summary>
Public Class EchoSampleProvider
    Implements ISampleProvider

    Private ReadOnly sourceProvider As ISampleProvider
    Private delayBuffer As Single()
    Private writePosition As Integer = 0
    Private _delayMilliseconds As Integer = 300
    Private _delaySamples As Integer

    ' Filtre passe-bas pour darkening des échos
    Private filterStore As Single = 0.0F
    Private Const DAMPING As Single = 0.3F ' Amortissement des hautes fréquences dans les échos

    ' Multi-tap (plusieurs échos avec gain décroissant)
    Private ReadOnly tapOffsets() As Single = {1.0F, 1.5F, 2.0F, 2.5F} ' Multiples du délai de base
    Private ReadOnly tapGains() As Single = {0.8F, 0.5F, 0.3F, 0.15F} ' Gain de chaque tap

    Public Property Enabled As Boolean = False
    Public Property Mix As Single = 0.3F ' 0.0 = dry, 1.0 = wet
    Public Property Feedback As Single = 0.5F ' Quantité de signal réinjecté (0.0 à 0.9)

    Public Property DelayMilliseconds As Integer
        Get
            Return _delayMilliseconds
        End Get
        Set(value As Integer)
            If value >= 50 AndAlso value <= 2000 Then
                _delayMilliseconds = value
                RecalculateDelay()
            End If
        End Set
    End Property

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return sourceProvider.WaveFormat
        End Get
    End Property

    Public Sub New(source As ISampleProvider)
        sourceProvider = source
        RecalculateDelay()
    End Sub

    Private Sub RecalculateDelay()
        ' Allouer un buffer assez grand pour contenir le délai max + tous les taps
        Dim maxTapMultiplier = tapOffsets(tapOffsets.Length - 1)
        Dim maxDelaySamples = CInt((_delayMilliseconds * maxTapMultiplier / 1000.0) * sourceProvider.WaveFormat.SampleRate * sourceProvider.WaveFormat.Channels)

        _delaySamples = CInt((_delayMilliseconds / 1000.0) * sourceProvider.WaveFormat.SampleRate * sourceProvider.WaveFormat.Channels)

        ' Créer un buffer assez grand
        ReDim delayBuffer(maxDelaySamples - 1)
        writePosition = 0
        filterStore = 0.0F
    End Sub

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim samplesRead = sourceProvider.Read(buffer, offset, count)

        If Not Enabled OrElse Mix <= 0.0F Then
            Return samplesRead
        End If

        Dim channels = sourceProvider.WaveFormat.Channels
        Dim damp1 = DAMPING
        Dim damp2 = 1.0F - DAMPING

        ' Limiter le feedback pour éviter l'explosion
        Dim safeFeedback = Math.Min(Feedback, 0.85F)

        ' Appliquer l'écho
        For i = 0 To samplesRead - 1
            Dim sampleIndex = offset + i
            Dim drySignal = buffer(sampleIndex)
            Dim echoSignal As Single = 0.0F

            ' === Multi-tap delay ===
            ' Lire plusieurs échos à des délais différents
            For t = 0 To tapOffsets.Length - 1
                Dim tapDelay = CInt(_delaySamples * tapOffsets(t))

                ' Vérifier que le délai est dans les limites du buffer
                If tapDelay < delayBuffer.Length Then
                    Dim readPos = (writePosition - tapDelay + delayBuffer.Length) Mod delayBuffer.Length
                    Dim tapSample = delayBuffer(readPos)

                    ' Accumuler avec le gain approprié
                    echoSignal += tapSample * tapGains(t)
                End If
            Next

            ' Appliquer le filtre passe-bas (darkening) sur l'écho
            ' Les échos naturels perdent des hautes fréquences
            filterStore = (echoSignal * damp2) + (filterStore * damp1)
            Dim filteredEcho = filterStore

            ' === Stéréo ping-pong (si stéréo) ===
            Dim echoToWrite = filteredEcho
            If channels = 2 Then
                ' Alterner l'écho entre gauche et droite
                Dim channelIndex = i Mod 2
                Dim tapIndex = CInt(writePosition / _delaySamples) Mod 2

                ' Réduire l'écho sur le canal opposé pour créer le ping-pong
                If channelIndex <> tapIndex Then
                    echoToWrite *= 0.3F ' Atténuer sur le canal opposé
                End If
            End If

            ' Écrire dans le buffer avec feedback contrôlé
            delayBuffer(writePosition) = drySignal + (echoToWrite * safeFeedback)

            ' Limiter l'écriture dans le buffer pour éviter l'accumulation
            If delayBuffer(writePosition) > 1.5F Then
                delayBuffer(writePosition) = 1.5F
            ElseIf delayBuffer(writePosition) < -1.5F Then
                delayBuffer(writePosition) = -1.5F
            End If

            ' === Mixer dry/wet ===
            Dim wetGain = Mix * 0.7F ' Gain wet ajusté
            Dim dryGain = 1.0F - (Mix * 0.3F) ' Garder plus de signal dry

            Dim output = (drySignal * dryGain) + (filteredEcho * wetGain)

            ' Soft clipping pour éviter le clipping
            If output > 1.0F Then
                output = 1.0F
            ElseIf output < -1.0F Then
                output = -1.0F
            End If

            buffer(sampleIndex) = output

            writePosition = (writePosition + 1) Mod delayBuffer.Length
        Next

        Return samplesRead
    End Function
End Class
