Imports NAudio.Wave
Imports NAudio.Lame
Imports System.IO

''' <summary>
''' Module d'enregistrement de session DJ avec support multi-format
''' Formats supportés : WAV, MP3, FLAC, WMA, AAC
''' Utilise WasapiLoopbackCapture pour capturer la sortie audio système
''' </summary>
Public Class DJRecorder
    Implements IDisposable

    ' === ÉNUMÉRATION DES FORMATS ===
    Public Enum FormatEnregistrement
        WAV = 0
        MP3 = 1
        FLAC = 2
        WMA = 3
        AAC = 4
    End Enum

    ' === QUALITÉS D'ENCODAGE ===
    Public Enum QualiteMP3
        Kbps128 = 128
        Kbps192 = 192
        Kbps256 = 256
        Kbps320 = 320
    End Enum

    ' === PROPRIÉTÉS ===
    Private _format As FormatEnregistrement
    Private _qualiteMP3 As QualiteMP3
    Private _waveFormat As WaveFormat
    Private _cheminFichier As String
    Private _enregistrementActif As Boolean = False

    ' === CAPTURE LOOPBACK (capture sortie système) ===
    Private _captureLoopback As WasapiLoopbackCapture
    Private _waveFileWriter As WaveFileWriter
    Private _mp3Writer As LameMP3FileWriter

    ' === ÉVÉNEMENTS ===
    Public Event EnregistrementDemarre(cheminFichier As String)
    Public Event EnregistrementArrete(cheminFichier As String, duree As TimeSpan)
    Public Event Erreur(message As String)

    ' === STATISTIQUES ===
    Private _heureDebut As DateTime
    Private _octetsEcrits As Long

    ''' <summary>
    ''' Constructeur du recorder
    ''' </summary>
    Public Sub New(format As FormatEnregistrement, qualiteMP3 As QualiteMP3)
        _format = format
        _qualiteMP3 = qualiteMP3
    End Sub

    ''' <summary>
    ''' Obtient si l'enregistrement est actif
    ''' </summary>
    Public ReadOnly Property EstEnregistrement As Boolean
        Get
            Return _enregistrementActif
        End Get
    End Property

    ''' <summary>
    ''' Obtient le chemin du fichier en cours d'enregistrement
    ''' </summary>
    Public ReadOnly Property CheminFichierActuel As String
        Get
            Return _cheminFichier
        End Get
    End Property

    ''' <summary>
    ''' Obtient la durée d'enregistrement actuelle
    ''' </summary>
    Public ReadOnly Property DureeEnregistrement As TimeSpan
        Get
            If _enregistrementActif Then
                Return DateTime.Now - _heureDebut
            End If
            Return TimeSpan.Zero
        End Get
    End Property

    ''' <summary>
    ''' Démarrer l'enregistrement (capture loopback de la sortie système)
    ''' </summary>
    Public Function DemarrerEnregistrement(repertoireDestination As String, Optional waveFormat As WaveFormat = Nothing) As Boolean
        If _enregistrementActif Then
            RaiseEvent Erreur("Enregistrement déjà en cours")
            Return False
        End If

        Try
            ' Créer le nom de fichier avec timestamp
            Dim timestamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")
            Dim extension As String = ObtenirExtension(_format)
            Dim nomFichier As String = $"DJ_Mix_{timestamp}.{extension}"
            _cheminFichier = Path.Combine(repertoireDestination, nomFichier)

            ' Vérifier que le répertoire existe
            If Not Directory.Exists(repertoireDestination) Then
                Directory.CreateDirectory(repertoireDestination)
            End If

            _heureDebut = DateTime.Now
            _octetsEcrits = 0

            ' Initialiser la capture loopback (capture la sortie audio système)
            _captureLoopback = New WasapiLoopbackCapture()
            _waveFormat = _captureLoopback.WaveFormat

            ' Créer le writer selon le format
            Select Case _format
                Case FormatEnregistrement.WAV
                    _waveFileWriter = New WaveFileWriter(_cheminFichier, _waveFormat)

                Case FormatEnregistrement.MP3
                    ' NAudio.Lame pour encodage MP3
                    _mp3Writer = New LameMP3FileWriter(_cheminFichier, _waveFormat, CInt(_qualiteMP3))

                Case FormatEnregistrement.FLAC
                    ' FLAC via WaveFileWriter puis conversion (ou utiliser FlacWriter si disponible)
                    ' Pour l'instant, WAV en attendant implémentation FLAC
                    _cheminFichier = _cheminFichier.Replace(".flac", ".wav")
                    _waveFileWriter = New WaveFileWriter(_cheminFichier, _waveFormat)
                    Debug.WriteLine("[RECORDER] FLAC non implémenté - Enregistrement WAV")

                Case FormatEnregistrement.WMA, FormatEnregistrement.AAC
                    ' WMA/AAC via MediaFoundationEncoder (nécessite Windows)
                    ' Pour l'instant, WAV en attendant implémentation
                    _cheminFichier = _cheminFichier.Replace(".wma", ".wav").Replace(".aac", ".wav")
                    _waveFileWriter = New WaveFileWriter(_cheminFichier, _waveFormat)
                    Debug.WriteLine($"[RECORDER] {_format} non implémenté - Enregistrement WAV")

                Case Else
                    _waveFileWriter = New WaveFileWriter(_cheminFichier, _waveFormat)
            End Select

            ' Attacher le gestionnaire d'événements pour capturer l'audio
            AddHandler _captureLoopback.DataAvailable, AddressOf OnDataAvailable

            ' Démarrer la capture
            _captureLoopback.StartRecording()

            _enregistrementActif = True
            RaiseEvent EnregistrementDemarre(_cheminFichier)
            Debug.WriteLine($"[RECORDER] ✓ Enregistrement démarré : {_cheminFichier}")
            Debug.WriteLine($"[RECORDER] Format capture: {_waveFormat.SampleRate}Hz, {_waveFormat.Channels} canaux, {_waveFormat.BitsPerSample} bits")
            Return True

        Catch ex As Exception
            RaiseEvent Erreur($"Erreur démarrage enregistrement : {ex.Message}")
            Debug.WriteLine($"[RECORDER] ✗ Erreur : {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Gestionnaire de capture audio (callback loopback)
    ''' </summary>
    Private Sub OnDataAvailable(sender As Object, e As WaveInEventArgs)
        If Not _enregistrementActif Then Return

        Try
            If _waveFileWriter IsNot Nothing Then
                _waveFileWriter.Write(e.Buffer, 0, e.BytesRecorded)
                _octetsEcrits += e.BytesRecorded
            ElseIf _mp3Writer IsNot Nothing Then
                _mp3Writer.Write(e.Buffer, 0, e.BytesRecorded)
                _octetsEcrits += e.BytesRecorded
            End If
        Catch ex As Exception
            RaiseEvent Erreur($"Erreur écriture audio : {ex.Message}")
            Debug.WriteLine($"[RECORDER] ✗ Erreur écriture : {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Arrêter l'enregistrement
    ''' </summary>
    Public Function ArreterEnregistrement() As Boolean
        If Not _enregistrementActif Then
            Return False
        End If

        Try
            Dim duree As TimeSpan = DateTime.Now - _heureDebut

            ' Arrêter la capture
            If _captureLoopback IsNot Nothing Then
                _captureLoopback.StopRecording()
                RemoveHandler _captureLoopback.DataAvailable, AddressOf OnDataAvailable
                _captureLoopback.Dispose()
                _captureLoopback = Nothing
            End If

            ' Fermer les writers
            If _waveFileWriter IsNot Nothing Then
                _waveFileWriter.Flush()
                _waveFileWriter.Dispose()
                _waveFileWriter = Nothing
            End If

            If _mp3Writer IsNot Nothing Then
                _mp3Writer.Flush()
                _mp3Writer.Dispose()
                _mp3Writer = Nothing
            End If

            _enregistrementActif = False

            Dim tailleMo As Double = _octetsEcrits / (1024.0 * 1024.0)
            Debug.WriteLine($"[RECORDER] ✓ Enregistrement terminé : {duree:hh\:mm\:ss} - {tailleMo:F2} MB")
            Debug.WriteLine($"[RECORDER] Fichier sauvegardé : {_cheminFichier}")

            RaiseEvent EnregistrementArrete(_cheminFichier, duree)
            Return True

        Catch ex As Exception
            RaiseEvent Erreur($"Erreur arrêt enregistrement : {ex.Message}")
            Debug.WriteLine($"[RECORDER] ✗ Erreur arrêt : {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Obtenir l'extension de fichier selon le format
    ''' </summary>
    Private Function ObtenirExtension(format As FormatEnregistrement) As String
        Select Case format
            Case FormatEnregistrement.WAV
                Return "wav"
            Case FormatEnregistrement.MP3
                Return "mp3"
            Case FormatEnregistrement.FLAC
                Return "flac"
            Case FormatEnregistrement.WMA
                Return "wma"
            Case FormatEnregistrement.AAC
                Return "aac"
            Case Else
                Return "wav"
        End Select
    End Function

    ''' <summary>
    ''' Obtenir les formats disponibles pour ComboBox
    ''' </summary>
    Public Shared Function ObtenirFormatsDisponibles() As List(Of String)
        Return New List(Of String) From {
            "WAV (Lossless)",
            "MP3 (320 kbps)",
            "MP3 (256 kbps)",
            "MP3 (192 kbps)",
            "MP3 (128 kbps)",
            "FLAC (Lossless) - Prochainement",
            "WMA - Prochainement",
            "AAC - Prochainement"
        }
    End Function

    ''' <summary>
    ''' Convertir l'index ComboBox en format + qualité
    ''' </summary>
    Public Shared Sub ObtenirFormatEtQualite(indexCombo As Integer, ByRef format As FormatEnregistrement, ByRef qualiteMP3 As QualiteMP3)
        Select Case indexCombo
            Case 0 ' WAV
                format = FormatEnregistrement.WAV
                qualiteMP3 = QualiteMP3.Kbps320
            Case 1 ' MP3 320
                format = FormatEnregistrement.MP3
                qualiteMP3 = QualiteMP3.Kbps320
            Case 2 ' MP3 256
                format = FormatEnregistrement.MP3
                qualiteMP3 = QualiteMP3.Kbps256
            Case 3 ' MP3 192
                format = FormatEnregistrement.MP3
                qualiteMP3 = QualiteMP3.Kbps192
            Case 4 ' MP3 128
                format = FormatEnregistrement.MP3
                qualiteMP3 = QualiteMP3.Kbps128
            Case 5 ' FLAC (futur)
                format = FormatEnregistrement.FLAC
                qualiteMP3 = QualiteMP3.Kbps320
            Case 6 ' WMA (futur)
                format = FormatEnregistrement.WMA
                qualiteMP3 = QualiteMP3.Kbps320
            Case 7 ' AAC (futur)
                format = FormatEnregistrement.AAC
                qualiteMP3 = QualiteMP3.Kbps320
            Case Else
                format = FormatEnregistrement.WAV
                qualiteMP3 = QualiteMP3.Kbps320
        End Select
    End Sub

    ''' <summary>
    ''' Nettoyer les ressources
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        If _enregistrementActif Then
            ArreterEnregistrement()
        End If

        If _waveFileWriter IsNot Nothing Then
            _waveFileWriter.Dispose()
            _waveFileWriter = Nothing
        End If

        If _mp3Writer IsNot Nothing Then
            _mp3Writer.Dispose()
            _mp3Writer = Nothing
        End If
    End Sub
End Class
