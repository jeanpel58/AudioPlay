Imports NAudio.Wave
Imports System.Math

''' <summary>
''' Fournisseur d'échantillons qui applique un effet Phaser
''' Le Phaser crée un son spatial et tournant en modulant des filtres all-pass
''' avec un oscillateur basse fréquence (LFO)
''' </summary>
Public Class PhaserSampleProvider
    Implements ISampleProvider

    Private ReadOnly sourceProvider As ISampleProvider

    ' Filtres all-pass (cascade)
    Private allPassFilters As List(Of AllPassFilter)
    Private _stages As Integer = 4 ' Nombre de filtres (2, 4, 6, 8, 12)

    ' LFO (Low Frequency Oscillator)
    Private lfoPhase As Single = 0.0F
    Private lfoIncrement As Single

    ' Buffer pour feedback
    Private feedbackSample As Single = 0.0F

    ' Paramètres
    Private _enabled As Boolean = False
    Private _rate As Single = 0.5F        ' Hz (vitesse de modulation)
    Private _depth As Single = 0.7F       ' 0.0 - 1.0 (profondeur) - 70% pour son musical vintage
    Private _feedback As Single = 0.3F    ' 0.0 - 0.95 (réinjection) - 30% pour son doux
    Private _mix As Single = 0.5F         ' 0.0 - 1.0 (wet/dry) - 50% équilibre parfait
    Private _centerFrequency As Single = 1000.0F ' Hz (fréquence centrale)
    Private _sweepWidth As Single = 2000.0F      ' Hz (largeur du balayage)

    Public Property Enabled As Boolean
        Get
            Return _enabled
        End Get
        Set(value As Boolean)
            _enabled = value
            If Not _enabled Then
                ' Réinitialiser quand désactivé
                ResetFilters()
            End If
        End Set
    End Property

    ''' <summary>
    ''' Vitesse de modulation du LFO en Hz (0.1 - 10.0)
    ''' </summary>
    Public Property Rate As Single
        Get
            Return _rate
        End Get
        Set(value As Single)
            _rate = Math.Max(0.01F, Math.Min(10.0F, value))
            UpdateLFOIncrement()
        End Set
    End Property

    ''' <summary>
    ''' Profondeur de modulation (0.0 - 1.0)
    ''' </summary>
    Public Property Depth As Single
        Get
            Return _depth
        End Get
        Set(value As Single)
            _depth = Math.Max(0.0F, Math.Min(1.0F, value))
        End Set
    End Property

    ''' <summary>
    ''' Feedback - quantité de signal réinjecté (0.0 - 0.95)
    ''' </summary>
    Public Property Feedback As Single
        Get
            Return _feedback
        End Get
        Set(value As Single)
            _feedback = Math.Max(0.0F, Math.Min(0.95F, value))
        End Set
    End Property

    ''' <summary>
    ''' Mix wet/dry (0.0 = dry only, 1.0 = wet only)
    ''' </summary>
    Public Property Mix As Single
        Get
            Return _mix
        End Get
        Set(value As Single)
            _mix = Math.Max(0.0F, Math.Min(1.0F, value))
        End Set
    End Property

    ''' <summary>
    ''' Nombre de stages (filtres all-pass en cascade): 2, 4, 6, 8, ou 12
    ''' Plus de stages = effet plus prononcé
    ''' </summary>
    Public Property Stages As Integer
        Get
            Return _stages
        End Get
        Set(value As Integer)
            ' Limiter aux valeurs valides (pairs seulement)
            Dim newStages = Math.Max(2, Math.Min(12, value))
            If newStages Mod 2 <> 0 Then newStages -= 1 ' Forcer pair

            If newStages <> _stages Then
                _stages = newStages
                InitializeFilters()
            End If
        End Set
    End Property

    ''' <summary>
    ''' Fréquence centrale du balayage en Hz (200 - 5000)
    ''' </summary>
    Public Property CenterFrequency As Single
        Get
            Return _centerFrequency
        End Get
        Set(value As Single)
            _centerFrequency = Math.Max(200.0F, Math.Min(5000.0F, value))
        End Set
    End Property

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return sourceProvider.WaveFormat
        End Get
    End Property

    Public Sub New(source As ISampleProvider)
        sourceProvider = source
        InitializeFilters()
        UpdateLFOIncrement()
    End Sub

    Private Sub InitializeFilters()
        allPassFilters = New List(Of AllPassFilter)

        ' Créer les filtres all-pass
        For i = 0 To _stages - 1
            allPassFilters.Add(New AllPassFilter(sourceProvider.WaveFormat.SampleRate, _centerFrequency))
        Next
    End Sub

    Private Sub UpdateLFOIncrement()
        ' Calculer l'incrément de phase du LFO pour la vitesse désirée
        ' lfoIncrement = rate / sampleRate
        lfoIncrement = _rate / sourceProvider.WaveFormat.SampleRate
    End Sub

    Private Sub ResetFilters()
        lfoPhase = 0.0F
        feedbackSample = 0.0F
        If allPassFilters IsNot Nothing Then
            For Each apFilter In allPassFilters
                apFilter.Reset()
            Next
        End If
    End Sub

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim samplesRead = sourceProvider.Read(buffer, offset, count)

        If Not _enabled OrElse _mix <= 0.0F Then
            Return samplesRead
        End If

        ' Traiter chaque échantillon
        For i = offset To offset + samplesRead - 1
            Dim drySignal = buffer(i)

            ' === 1. Générer LFO (oscillateur simple et pur) ===
            Dim lfo = CSng(Math.Sin(lfoPhase * 2.0 * Math.PI))
            lfoPhase += lfoIncrement
            If lfoPhase >= 1.0F Then
                lfoPhase -= 1.0F
            End If

            ' === 2. Calculer fréquence modulée (linéaire simple) ===
            ' Le LFO varie entre -1 et +1
            ' Mapper ça sur la plage de fréquences
            Dim freqOffset = lfo * _depth * _sweepWidth * 0.5F
            Dim modulatedFreq = _centerFrequency + freqOffset

            ' Limiter la plage de fréquences
            modulatedFreq = Math.Max(200.0F, Math.Min(5000.0F, modulatedFreq))

            ' === 3. Phaser classique : Dry + Wet ===
            Dim wetSignal = drySignal

            ' Ajouter feedback (réinjection du signal précédent)
            If Math.Abs(_feedback) > 0.01F Then
                wetSignal = wetSignal + (feedbackSample * _feedback)
            End If

            ' Appliquer la cascade de filtres all-pass
            ' Chaque filtre déphasera le signal
            For Each apFilter In allPassFilters
                apFilter.SetFrequency(modulatedFreq)
                wetSignal = apFilter.Process(wetSignal)
            Next

            ' Sauvegarder pour feedback
            feedbackSample = wetSignal

            ' === 4. Mix CORRECT : Dry + (Wet - Dry) * Mix ===
            ' Cela crée l'addition/soustraction de phase qui fait les notches
            ' Sans augmenter le volume global
            Dim phasedSignal = wetSignal - drySignal ' Différence de phase
            buffer(i) = drySignal + (phasedSignal * _mix)
        Next

        Return samplesRead
    End Function
End Class
