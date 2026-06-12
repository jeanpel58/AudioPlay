Imports NAudio.Wave
Imports System.Math

''' <summary>
''' Fournisseur d'échantillons qui applique un effet de réverbération amélioré
''' Utilise l'algorithme Freeverb avec filtres comb, allpass, et damping
''' </summary>
Public Class ReverbSampleProvider
    Implements ISampleProvider

    Private ReadOnly sourceProvider As ISampleProvider

    ' Pré-délai (early reflections)
    Private ReadOnly preDelayBuffer As Single()
    Private preDelayLength As Integer
    Private preDelayPos As Integer = 0

    ' Filtres Comb avec damping (8 filtres pour plus de densité)
    Private ReadOnly combBuffers As List(Of Single())
    Private ReadOnly combDelays As List(Of Integer)
    Private ReadOnly combReadPos As List(Of Integer)
    Private ReadOnly combWritePos As List(Of Integer)
    Private ReadOnly combFilterStore As List(Of Single) ' Pour le filtre passe-bas

    ' Filtres Allpass (4 filtres pour meilleure diffusion)
    Private ReadOnly allpassBuffers As List(Of Single())
    Private ReadOnly allpassDelays As List(Of Integer)
    Private ReadOnly allpassReadPos As List(Of Integer)
    Private ReadOnly allpassWritePos As List(Of Integer)

    Private Const ROOM_SIZE As Single = 0.84F      ' Taille de la pièce (feedback)
    Private Const DAMPING As Single = 0.2F          ' Amortissement des hautes fréquences
    Private Const ALLPASS_FEEDBACK As Single = 0.5F ' Feedback allpass
    Private Const STEREO_SPREAD As Integer = 23     ' Écart stéréo
    Private Const PREDELAY_MS As Single = 20.0F     ' Pré-délai en ms

    Public Property Enabled As Boolean = False
    Public Property Mix As Single = 0.3F ' 0.0 = dry, 1.0 = wet

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return sourceProvider.WaveFormat
        End Get
    End Property

    Public Sub New(source As ISampleProvider)
        sourceProvider = source

        ' Initialiser les listes
        combBuffers = New List(Of Single())
        combDelays = New List(Of Integer)
        combReadPos = New List(Of Integer)
        combWritePos = New List(Of Integer)
        combFilterStore = New List(Of Single)

        allpassBuffers = New List(Of Single())
        allpassDelays = New List(Of Integer)
        allpassReadPos = New List(Of Integer)
        allpassWritePos = New List(Of Integer)

        Dim sampleRate = source.WaveFormat.SampleRate
        Dim channels = source.WaveFormat.Channels

        ' === Pré-délai ===
        preDelayLength = CInt((PREDELAY_MS / 1000.0) * sampleRate * channels)
        preDelayBuffer = New Single(preDelayLength - 1) {}

        ' === Filtres Comb (8 filtres) ===
        ' Délais basés sur les valeurs Freeverb classiques (en samples @44.1kHz, adaptés)
        Dim combTunings() = {1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617}

        ' Ajuster pour le sample rate actuel
        For Each tuning In combTunings
            Dim delaySamples = CInt(tuning * (sampleRate / 44100.0) * channels)
            combDelays.Add(delaySamples)
            combBuffers.Add(New Single(delaySamples - 1) {})
            combReadPos.Add(0)
            combWritePos.Add(0)
            combFilterStore.Add(0.0F) ' Pour le filtre damping
        Next

        ' === Filtres Allpass (4 filtres) ===
        Dim allpassTunings() = {556, 441, 341, 225}

        For Each tuning In allpassTunings
            Dim delaySamples = CInt(tuning * (sampleRate / 44100.0) * channels)
            allpassDelays.Add(delaySamples)
            allpassBuffers.Add(New Single(delaySamples - 1) {})
            allpassReadPos.Add(0)
            allpassWritePos.Add(0)
        Next
    End Sub

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim samplesRead = sourceProvider.Read(buffer, offset, count)

        If Not Enabled OrElse Mix <= 0.0F Then
            Return samplesRead
        End If

        ' Facteurs de damping
        Dim damp1 = DAMPING
        Dim damp2 = 1.0F - DAMPING

        ' Traiter chaque échantillon
        For i = 0 To samplesRead - 1
            Dim sampleIndex = offset + i
            Dim inputSample = buffer(sampleIndex)

            ' === Pré-délai ===
            Dim preDelayedSample = preDelayBuffer(preDelayPos)
            preDelayBuffer(preDelayPos) = inputSample
            preDelayPos = (preDelayPos + 1) Mod preDelayLength

            ' Utiliser le signal pré-délayé comme entrée des filtres
            Dim reverbInput = preDelayedSample
            Dim reverbOutput As Single = 0.0F

            ' === Filtres Comb avec damping (parallèles) ===
            For c = 0 To combBuffers.Count - 1
                Dim delayBuffer = combBuffers(c)
                Dim delayLength = combDelays(c)
                Dim rPos = combReadPos(c)
                Dim wPos = combWritePos(c)

                ' Lire la sortie du délai
                Dim delayedSample = delayBuffer(rPos)

                ' Filtre passe-bas (damping) pour simuler l'absorption des hautes fréquences
                combFilterStore(c) = (delayedSample * damp2) + (combFilterStore(c) * damp1)

                ' Écrire l'entrée + feedback dampé dans le buffer
                delayBuffer(wPos) = reverbInput + (combFilterStore(c) * ROOM_SIZE)

                ' Accumuler la sortie
                reverbOutput += delayedSample

                ' Avancer les positions
                combReadPos(c) = (rPos + 1) Mod delayLength
                combWritePos(c) = (wPos + 1) Mod delayLength
            Next

            ' Moyenner la sortie des filtres comb
            reverbOutput = reverbOutput / combBuffers.Count

            ' === Filtres Allpass (en série) ===
            For a = 0 To allpassBuffers.Count - 1
                Dim delayBuffer = allpassBuffers(a)
                Dim delayLength = allpassDelays(a)
                Dim rPos = allpassReadPos(a)
                Dim wPos = allpassWritePos(a)

                ' Lire la sortie du délai
                Dim delayedSample = delayBuffer(rPos)

                ' Calculer la sortie allpass avec la formule classique
                Dim output = -reverbOutput + delayedSample

                ' Écrire dans le buffer
                delayBuffer(wPos) = reverbOutput + (delayedSample * ALLPASS_FEEDBACK)

                ' La sortie devient l'entrée du prochain étage
                reverbOutput = output

                ' Avancer les positions
                allpassReadPos(a) = (rPos + 1) Mod delayLength
                allpassWritePos(a) = (wPos + 1) Mod delayLength
            Next

            ' === Mixer dry/wet avec gain compensé ===
            Dim wetGain = Mix * 0.35F  ' Gain wet ajusté pour éviter la saturation
            Dim dryGain = 1.0F - (Mix * 0.7F) ' Réduire le dry progressivement

            buffer(sampleIndex) = (inputSample * dryGain) + (reverbOutput * wetGain)

            ' Soft clipping pour éviter la distorsion
            If buffer(sampleIndex) > 1.0F Then
                buffer(sampleIndex) = 1.0F
            ElseIf buffer(sampleIndex) < -1.0F Then
                buffer(sampleIndex) = -1.0F
            End If
        Next

        Return samplesRead
    End Function
End Class
