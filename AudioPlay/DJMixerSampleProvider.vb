Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders

''' <summary>
''' Provider qui mixe deux decks avec crossfader et capture pour enregistrement
''' </summary>
Public Class DJMixerSampleProvider
    Implements ISampleProvider

    Private _deckAProvider As ISampleProvider
    Private _deckBProvider As ISampleProvider
    Private _waveFormat As WaveFormat
    Private _crossfaderPosition As Single = 0.5F ' 0.0 = 100% A, 1.0 = 100% B
    Private _onMixedSamples As Action(Of Single(), Integer, Integer)

    ''' <summary>
    ''' Constructeur
    ''' </summary>
    Public Sub New(deckAProvider As ISampleProvider, deckBProvider As ISampleProvider, Optional onMixedSamples As Action(Of Single(), Integer, Integer) = Nothing)
        If deckAProvider Is Nothing AndAlso deckBProvider Is Nothing Then
            Throw New ArgumentException("Au moins un deck doit être fourni")
        End If

        _deckAProvider = deckAProvider
        _deckBProvider = deckBProvider
        _onMixedSamples = onMixedSamples

        ' Utiliser le format du premier deck disponible
        If deckAProvider IsNot Nothing Then
            _waveFormat = deckAProvider.WaveFormat
        Else
            _waveFormat = deckBProvider.WaveFormat
        End If

        ' Vérifier que les formats correspondent si les deux sont disponibles
        If deckAProvider IsNot Nothing AndAlso deckBProvider IsNot Nothing Then
            If deckAProvider.WaveFormat.SampleRate <> deckBProvider.WaveFormat.SampleRate OrElse
               deckAProvider.WaveFormat.Channels <> deckBProvider.WaveFormat.Channels Then
                Throw New ArgumentException("Les formats audio des deux decks doivent correspondre")
            End If
        End If
    End Sub

    ''' <summary>
    ''' Format audio du mixer
    ''' </summary>
    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return _waveFormat
        End Get
    End Property

    ''' <summary>
    ''' Position du crossfader (0.0 = 100% A, 0.5 = 50/50, 1.0 = 100% B)
    ''' </summary>
    Public Property CrossfaderPosition As Single
        Get
            Return _crossfaderPosition
        End Get
        Set(value As Single)
            _crossfaderPosition = Math.Max(0.0F, Math.Min(1.0F, value))
        End Set
    End Property

    ''' <summary>
    ''' Mettre à jour le provider Deck A
    ''' </summary>
    Public Sub UpdateDeckA(provider As ISampleProvider)
        _deckAProvider = provider
    End Sub

    ''' <summary>
    ''' Mettre à jour le provider Deck B
    ''' </summary>
    Public Sub UpdateDeckB(provider As ISampleProvider)
        _deckBProvider = provider
    End Sub

    ''' <summary>
    ''' Lire et mixer les samples des deux decks
    ''' </summary>
    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim samplesRead As Integer = 0

        ' Buffer temporaires pour chaque deck
        Dim bufferA(count - 1) As Single
        Dim bufferB(count - 1) As Single

        ' Lire depuis Deck A
        Dim samplesReadA As Integer = 0
        If _deckAProvider IsNot Nothing Then
            samplesReadA = _deckAProvider.Read(bufferA, 0, count)
        End If

        ' Lire depuis Deck B
        Dim samplesReadB As Integer = 0
        If _deckBProvider IsNot Nothing Then
            samplesReadB = _deckBProvider.Read(bufferB, 0, count)
        End If

        ' Prendre le maximum des deux
        samplesRead = Math.Max(samplesReadA, samplesReadB)

        ' Calculer les volumes selon le crossfader
        ' Courbe linéaire pour l'instant (peut être amélioré avec courbe logarithmique)
        Dim volumeA As Single = 1.0F - _crossfaderPosition
        Dim volumeB As Single = _crossfaderPosition

        ' Mixer les samples
        For i As Integer = 0 To samplesRead - 1
            Dim sampleA As Single = If(i < samplesReadA, bufferA(i) * volumeA, 0.0F)
            Dim sampleB As Single = If(i < samplesReadB, bufferB(i) * volumeB, 0.0F)
            buffer(offset + i) = sampleA + sampleB
        Next

        ' Appeler le callback pour l'enregistrement (si défini)
        If _onMixedSamples IsNot Nothing AndAlso samplesRead > 0 Then
            Try
                _onMixedSamples(buffer, offset, samplesRead)
            Catch ex As Exception
                ' Erreur silencieuse pour ne pas interrompre la lecture
                Debug.WriteLine($"[MIXER] Erreur callback: {ex.Message}")
            End Try
        End If

        Return samplesRead
    End Function
End Class
