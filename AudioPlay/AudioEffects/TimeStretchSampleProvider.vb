Imports NAudio.Wave

''' <summary>
''' Fournisseur d'échantillons qui applique un changement de tempo avec préservation du pitch
''' Utilise la bibliothèque native SoundTouch (la même qu'Audacity) via P/Invoke
''' </summary>
Public Class TimeStretchSampleProvider
    Implements ISampleProvider

    Private ReadOnly sourceProvider As ISampleProvider
    Private soundTouch As SoundTouchInterop
    Private inputBuffer As Single()
    Private outputBuffer As Single()
    Private tempReceiveBuffer As Single()  ' Buffer temporaire pour ReceiveSamples
    Private outputBufferOffset As Integer = 0
    Private outputBufferCount As Integer = 0
    Private isFinished As Boolean = False
    Private isInitialized As Boolean = False

    Public Property Enabled As Boolean = False
    Private _tempoChange As Single = 1.0F

    Public Property TempoChange As Single
        Get
            Return _tempoChange
        End Get
        Set(value As Single)
            If value >= 0.5F AndAlso value <= 2.0F Then
                _tempoChange = value
                If soundTouch IsNot Nothing Then
                    ' SoundTouch utilise le ratio directement (1.0 = normal)
                    soundTouch.SetTempo(_tempoChange)
                End If
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
        ' Allouer des buffers suffisamment grands pour le traitement audio
        ' 8192 samples = 4096 frames stéréo (environ 93ms à 44.1kHz)
        inputBuffer = New Single(8191) {}
        outputBuffer = New Single(16383) {} ' Buffer de sortie plus grand pour le time-stretching
        tempReceiveBuffer = New Single(16383) {} ' Buffer temporaire pour P/Invoke
    End Sub

    Private Sub InitializeSoundTouch()
        If isInitialized Then Return

        Try
            ' Créer l'instance SoundTouch native
            soundTouch = New SoundTouchInterop()

            ' Vérifier que l'instance a bien été créée
            If soundTouch Is Nothing Then
                isInitialized = True
                Return
            End If

            ' Configurer SoundTouch
            Dim sampleRate As Integer = sourceProvider.WaveFormat.SampleRate
            Dim channels As Integer = sourceProvider.WaveFormat.Channels

            soundTouch.SetSampleRate(sampleRate)
            soundTouch.SetChannels(channels)

            ' Paramètres de qualité (comme Audacity)
            soundTouch.SetSetting(SoundTouchInterop.SETTING_USE_QUICKSEEK, 0)  ' Désactiver quick seek
            soundTouch.SetSetting(SoundTouchInterop.SETTING_USE_AA_FILTER, 1)  ' Activer anti-aliasing
            soundTouch.SetSetting(SoundTouchInterop.SETTING_SEQUENCE_MS, 40)   ' Taille séquence
            soundTouch.SetSetting(SoundTouchInterop.SETTING_SEEKWINDOW_MS, 15) ' Fenêtre recherche
            soundTouch.SetSetting(SoundTouchInterop.SETTING_OVERLAP_MS, 8)     ' Chevauchement

            ' Définir le tempo initial
            soundTouch.SetTempo(_tempoChange)

            isInitialized = True

        Catch ex As DllNotFoundException
            System.Diagnostics.Debug.WriteLine($"SoundTouch DLL non trouvée: {ex.Message}")
            soundTouch = Nothing
            isInitialized = True
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur initialisation SoundTouch: {ex.Message}")
            soundTouch = Nothing
            isInitialized = True ' Marquer comme initialisé pour éviter les tentatives répétées
        End Try
    End Sub

    Public Function Read(buffer() As Single, offset As Integer, count As Integer) As Integer Implements ISampleProvider.Read
        If Not Enabled OrElse Math.Abs(_tempoChange - 1.0F) < 0.01F Then
            ' Passer le signal tel quel si désactivé
            Return sourceProvider.Read(buffer, offset, count)
        End If

        ' Initialiser SoundTouch à la première utilisation
        If Not isInitialized Then
            InitializeSoundTouch()
        End If

        ' Si l'initialisation a échoué, passer en mode bypass
        If soundTouch Is Nothing Then
            System.Diagnostics.Debug.WriteLine("TimeStretch: soundTouch est Nothing, mode bypass")
            Return sourceProvider.Read(buffer, offset, count)
        End If

        Dim samplesWritten As Integer = 0

        Try
            While samplesWritten < count
                ' Si on a des échantillons dans le buffer de sortie, les utiliser
                If outputBufferCount > 0 Then
                    Dim samplesToCopy As Integer = Math.Min(count - samplesWritten, outputBufferCount)

                    ' Vérification des limites
                    If outputBufferOffset + samplesToCopy <= outputBuffer.Length AndAlso
                       offset + samplesWritten + samplesToCopy <= buffer.Length Then

                        ' Copie manuelle pour éviter ArrayTypeMismatchException
                        For i As Integer = 0 To samplesToCopy - 1
                            buffer(offset + samplesWritten + i) = outputBuffer(outputBufferOffset + i)
                        Next

                        samplesWritten += samplesToCopy
                        outputBufferOffset += samplesToCopy
                        outputBufferCount -= samplesToCopy
                    Else
                        ' Dépassement détecté, arrêter pour éviter le crash
                        Exit While
                    End If
                Else
                    ' Besoin de plus de données traitées par SoundTouch
                    If Not ProcessMoreSamples() Then
                        ' Fin du flux
                        Exit While
                    End If
                End If
            End While

        Catch ex As Exception
            ' En cas d'erreur, retourner ce qu'on a déjà écrit
        End Try

        Return samplesWritten
    End Function

    Private Function ProcessMoreSamples() As Boolean
        Try
            ' Lire plus de données depuis la source
            Dim samplesRead As Integer = sourceProvider.Read(inputBuffer, 0, inputBuffer.Length)

            If samplesRead > 0 Then
                ' Envoyer les échantillons à SoundTouch
                ' SoundTouch attend le nombre de frames (samples / channels)
                Dim numFrames As Integer = samplesRead \ WaveFormat.Channels
                soundTouch.PutSamples(inputBuffer, numFrames)
            Else
                ' Fin du flux source
                If Not isFinished Then
                    soundTouch.Flush()
                    isFinished = True
                End If
            End If

            ' Récupérer les échantillons traités
            ' Demander le nombre de frames disponibles, puis convertir en samples
            Dim maxFrames As Integer = (tempReceiveBuffer.Length \ WaveFormat.Channels)
            Dim framesReceived As Integer = soundTouch.ReceiveSamples(tempReceiveBuffer, maxFrames)

            If framesReceived > 0 Then
                ' Copier depuis le buffer temporaire vers le buffer de sortie
                Dim samplesReceived As Integer = framesReceived * WaveFormat.Channels
                Array.Copy(tempReceiveBuffer, 0, outputBuffer, 0, samplesReceived)
                outputBufferOffset = 0
                outputBufferCount = samplesReceived
                Return True
            End If

            ' Pas d'échantillons disponibles pour l'instant
            Return samplesRead > 0

        Catch ex As Exception
            Return False
        End Try
    End Function

    Protected Overrides Sub Finalize()
        If soundTouch IsNot Nothing Then
            soundTouch.Dispose()
            soundTouch = Nothing
        End If
        MyBase.Finalize()
    End Sub
End Class
