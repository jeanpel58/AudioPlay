Imports NAudio.Wave
Imports System.IO

''' <summary>
''' Pad de sampler individuel
''' </summary>
Public Class SamplePad
    Public Property Index As Integer
    Public Property FilePath As String
    Public Property Name As String
    Public Property Color As Drawing.Color
    Private reader As AudioFileReader
    Private outputDevice As IWavePlayer
    Private volumeProvider As NAudio.Wave.SampleProviders.VolumeSampleProvider

    Public Sub New(index As Integer)
        Me.Index = index
        Me.Name = $"Pad {index}"
        Me.Color = Drawing.Color.FromArgb(50, 150, 200)
    End Sub

    ''' <summary>
    ''' Charge un sample audio
    ''' </summary>
    Public Function LoadSample(filePath As String) As Boolean
        Try
            UnloadSample()

            If Not File.Exists(filePath) Then
                Return False
            End If

            Me.FilePath = filePath
            Me.Name = Path.GetFileNameWithoutExtension(filePath)

            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Déclenche la lecture du sample
    ''' </summary>
    Public Sub Trigger()
        If String.IsNullOrEmpty(FilePath) OrElse Not File.Exists(FilePath) Then
            Return
        End If

        Try
            ' Arrêter la lecture précédente si active
            StopPlayback()

            ' Créer un nouveau reader et démarrer la lecture
            reader = New AudioFileReader(FilePath)
            volumeProvider = New NAudio.Wave.SampleProviders.VolumeSampleProvider(reader) With {
                .Volume = 1.0F
            }

            outputDevice = New WaveOutEvent()
            outputDevice.Init(volumeProvider)
            outputDevice.Play()

        Catch ex As Exception
            ' Erreur silencieuse
        End Try
    End Sub

    ''' <summary>
    ''' Arrête la lecture du sample
    ''' </summary>
    Public Sub StopPlayback()
        Try
            If outputDevice IsNot Nothing Then
                outputDevice.Stop()
                outputDevice.Dispose()
                outputDevice = Nothing
            End If

            If reader IsNot Nothing Then
                reader.Dispose()
                reader = Nothing
            End If
        Catch ex As Exception
            ' Erreur silencieuse
        End Try
    End Sub

    ''' <summary>
    ''' Décharge le sample
    ''' </summary>
    Public Sub UnloadSample()
        StopPlayback()
        FilePath = ""
        Name = $"Pad {Index}"
    End Sub

    ''' <summary>
    ''' Indique si un sample est chargé
    ''' </summary>
    Public ReadOnly Property IsLoaded As Boolean
        Get
            Return Not String.IsNullOrEmpty(FilePath) AndAlso File.Exists(FilePath)
        End Get
    End Property

    ''' <summary>
    ''' Indique si le sample est en cours de lecture
    ''' </summary>
    Public ReadOnly Property IsPlaying As Boolean
        Get
            Return outputDevice IsNot Nothing AndAlso outputDevice.PlaybackState = PlaybackState.Playing
        End Get
    End Property
End Class

''' <summary>
''' Gestionnaire de sampler avec 8 pads
''' </summary>
Public Class SamplerManager
    Private pads As New Dictionary(Of Integer, SamplePad)()
    Private Const MAX_PADS As Integer = 8

    Public Sub New()
        ' Initialiser les 8 pads
        For i As Integer = 1 To MAX_PADS
            pads.Add(i, New SamplePad(i))
        Next
    End Sub

    ''' <summary>
    ''' Obtient un pad par son index
    ''' </summary>
    Public Function GetPad(index As Integer) As SamplePad
        If pads.ContainsKey(index) Then
            Return pads(index)
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' Charge un sample sur un pad
    ''' </summary>
    Public Function LoadSampleOnPad(padIndex As Integer, filePath As String) As Boolean
        Dim pad As SamplePad = GetPad(padIndex)
        If pad IsNot Nothing Then
            Return pad.LoadSample(filePath)
        End If
        Return False
    End Function

    ''' <summary>
    ''' Déclenche un pad
    ''' </summary>
    Public Sub TriggerPad(padIndex As Integer)
        Dim pad As SamplePad = GetPad(padIndex)
        If pad IsNot Nothing Then
            pad.Trigger()
        End If
    End Sub

    ''' <summary>
    ''' Arrête tous les pads
    ''' </summary>
    Public Sub StopAllPads()
        For Each pad In pads.Values
            pad.StopPlayback()
        Next
    End Sub

    ''' <summary>
    ''' Efface tous les pads
    ''' </summary>
    Public Sub ClearAllPads()
        For Each pad In pads.Values
            pad.UnloadSample()
        Next
    End Sub

    ''' <summary>
    ''' Retourne tous les pads
    ''' </summary>
    Public Function GetAllPads() As List(Of SamplePad)
        Return pads.Values.ToList()
    End Function
End Class
