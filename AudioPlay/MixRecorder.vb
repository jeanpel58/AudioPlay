Imports System.IO
Imports NAudio.Wave

''' <summary>
''' Gestionnaire d'enregistrement de mix en temps réel
''' </summary>
Public Class MixRecorder
    Private writer As WaveFileWriter = Nothing
    Private m_isRecording As Boolean = False
    Private recordingFilePath As String = ""
    Private waveFormat As WaveFormat

    ''' <summary>
    ''' Indique si l'enregistrement est en cours
    ''' </summary>
    Public ReadOnly Property IsRecording As Boolean
        Get
            Return m_isRecording
        End Get
    End Property

    ''' <summary>
    ''' Chemin du fichier d'enregistrement actuel
    ''' </summary>
    Public ReadOnly Property FilePath As String
        Get
            Return recordingFilePath
        End Get
    End Property

    ''' <summary>
    ''' Démarre l'enregistrement
    ''' </summary>
    Public Function StartRecording(format As WaveFormat) As String
        If m_isRecording Then
            Throw New InvalidOperationException("Un enregistrement est déjà en cours.")
        End If

        ' Créer un nom de fichier avec timestamp
        Dim timestamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")
        Dim documentsPath As String = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        Dim audioPlayFolder As String = Path.Combine(documentsPath, "AudioPlay", "Recordings")

        ' Créer le dossier si nécessaire
        If Not Directory.Exists(audioPlayFolder) Then
            Directory.CreateDirectory(audioPlayFolder)
        End If

        recordingFilePath = Path.Combine(audioPlayFolder, $"Mix_{timestamp}.wav")
        waveFormat = format

        writer = New WaveFileWriter(recordingFilePath, waveFormat)
        m_isRecording = True

        Return recordingFilePath
    End Function

    ''' <summary>
    ''' Écrit des samples audio dans l'enregistrement
    ''' </summary>
    Public Sub WriteSamples(buffer() As Single, offset As Integer, count As Integer)
        If Not m_isRecording OrElse writer Is Nothing Then Return

        Try
            writer.WriteSamples(buffer, offset, count)
        Catch ex As Exception
            ' Erreur silencieuse pour ne pas interrompre la lecture
        End Try
    End Sub

    ''' <summary>
    ''' Arrête l'enregistrement
    ''' </summary>
    Public Sub StopRecording()
        If Not m_isRecording Then Return

        Try
            If writer IsNot Nothing Then
                writer.Flush()
                writer.Dispose()
                writer = Nothing
            End If
        Catch ex As Exception
            ' Erreur silencieuse
        Finally
            m_isRecording = False
        End Try
    End Sub

    ''' <summary>
    ''' Obtient la durée actuelle de l'enregistrement
    ''' </summary>
    Public Function GetRecordingDuration() As TimeSpan
        If writer IsNot Nothing AndAlso m_isRecording Then
            Try
                Dim totalSamples As Long = CLng(writer.Length / (waveFormat.BitsPerSample / 8))
                Dim seconds As Double = totalSamples / CDbl(waveFormat.SampleRate * waveFormat.Channels)
                Return TimeSpan.FromSeconds(seconds)
            Catch ex As Exception
                Return TimeSpan.Zero
            End Try
        End If
        Return TimeSpan.Zero
    End Function
End Class

''' <summary>
''' Sample provider qui capture l'audio pour l'enregistrement
''' </summary>
Public Class RecordingSampleProvider
    Implements ISampleProvider

    Private ReadOnly source As ISampleProvider
    Private recorder As MixRecorder

    Public Sub New(sourceProvider As ISampleProvider, mixRecorder As MixRecorder)
        source = sourceProvider
        recorder = mixRecorder
    End Sub

    Public ReadOnly Property WaveFormat As WaveFormat Implements ISampleProvider.WaveFormat
        Get
            Return source.WaveFormat
        End Get
    End Property

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        Dim samplesRead As Integer = source.Read(buffer, offset, count)

        ' Si l'enregistrement est actif, capturer les samples
        If recorder.IsRecording AndAlso samplesRead > 0 Then
            recorder.WriteSamples(buffer, offset, samplesRead)
        End If

        Return samplesRead
    End Function
End Class
