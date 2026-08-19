Imports System.IO
Imports System.Runtime.InteropServices
Imports NAudio.Wave

''' <summary>
''' Gestionnaire pour la détection et la lecture de CD audio
''' </summary>
Public Class CDAudioManager

    ' ========================================
    ' API Windows pour accès direct au CD-ROM
    ' ========================================
    Private Const GENERIC_READ As UInteger = &H80000000UI
    Private Const FILE_SHARE_READ As UInteger = &H1UI
    Private Const FILE_SHARE_WRITE As UInteger = &H2UI
    Private Const OPEN_EXISTING As UInteger = 3
    Private Const IOCTL_CDROM_READ_TOC As UInteger = &H24000UI
    Private Const IOCTL_CDROM_READ_Q_CHANNEL As UInteger = &H2402CUI
    Private Const IOCTL_CDROM_RAW_READ As UInteger = &H2403EUI
    Private Shared ReadOnly INVALID_HANDLE_VALUE As New IntPtr(-1)

    ' Constantes pour la lecture CD
    Private Const CD_SECTOR_SIZE As Integer = 2352 ' Taille d'un secteur CD audio brut
    Private Const CD_FRAMES_PER_SECOND As Integer = 75 ' 75 frames par seconde

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function CreateFile(lpFileName As String, dwDesiredAccess As UInteger,
                                       dwShareMode As UInteger, lpSecurityAttributes As IntPtr,
                                       dwCreationDisposition As UInteger, dwFlagsAndAttributes As UInteger,
                                       hTemplateFile As IntPtr) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function CloseHandle(hObject As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function GetLastError() As Integer
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function DeviceIoControl(hDevice As IntPtr, dwIoControlCode As UInteger,
                                            lpInBuffer As IntPtr, nInBufferSize As UInteger,
                                            <Out> ByRef lpOutBuffer As CDROM_TOC, nOutBufferSize As UInteger,
                                            <Out> ByRef lpBytesReturned As UInteger,
                                            lpOverlapped As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function DeviceIoControl(hDevice As IntPtr, dwIoControlCode As UInteger,
                                            ByRef lpInBuffer As RAW_READ_INFO, nInBufferSize As UInteger,
                                            lpOutBuffer As IntPtr, nOutBufferSize As UInteger,
                                            <Out> ByRef lpBytesReturned As UInteger,
                                            lpOverlapped As IntPtr) As Boolean
    End Function

    ' Structure pour la lecture brute des secteurs CD
    <StructLayout(LayoutKind.Sequential)>
    Private Structure RAW_READ_INFO
        Public DiskOffset As Long ' Position LBA (Logical Block Address) - numéro de secteur absolu sur le CD
        Public SectorCount As UInteger ' Nombre de secteurs à lire
        Public TrackMode As Integer ' 2 = CDDA (audio brut, 2352 bytes/secteur), 1 = Mode1 (données 2048), 0 = tous modes
    End Structure

    ' Structure pour la table des matières du CD
    <StructLayout(LayoutKind.Sequential)>
    Private Structure TRACK_DATA
        Public Reserved As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=1)>
        Public Control As Byte()
        Public TrackNumber As Byte
        Public Reserved1 As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=4)>
        Public Address As Byte()
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure CDROM_TOC
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=2)>
        Public Length As Byte()
        Public FirstTrack As Byte
        Public LastTrack As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=100)>
        Public TrackData As TRACK_DATA()
    End Structure

    ' Structure pour représenter une piste de CD audio
    Public Class CDTrack
        Public Property Drive As String ' Ex: "D:"
        Public Property TrackNumber As Integer ' 1, 2, 3...
        Public Property Duration As TimeSpan
        Public Property Title As String ' "Piste 01", "Piste 02"...
        Public Property Artist As String ' Artiste/groupe de la piste
        Public Property StartFrame As Integer ' Offset de départ en frames (secteurs)
        Public Property EndFrame As Integer ' Offset de fin en frames

        ''' <summary>
        ''' Génère le chemin virtuel unique pour cette piste
        ''' Format: CDDA://D:/Track01
        ''' </summary>
        Public ReadOnly Property VirtualPath As String
            Get
                Return $"CDDA://{Drive}/Track{TrackNumber:D2}"
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Détecte tous les lecteurs CD/DVD disponibles
    ''' </summary>
    Public Shared Function DetecterLecteursCDAudio() As List(Of String)
        Dim lecteurs As New List(Of String)
        Try
            For Each drive As DriveInfo In DriveInfo.GetDrives()
                If drive.DriveType = DriveType.CDRom Then
                    lecteurs.Add(drive.Name.TrimEnd("\"c))
                End If
            Next
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur détection lecteurs: {ex.Message}")
        End Try
        Return lecteurs
    End Function

    ''' <summary>
    ''' Vérifie si un CD audio est présent dans le lecteur spécifié
    ''' </summary>
    Public Shared Function EstCDAudioPresent(driveLetter As String) As Boolean
        Try
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Vérification présence CD dans {driveLetter}")

            ' Normaliser le nom du lecteur
            Dim drive As String = driveLetter.TrimEnd("\"c, ":"c).ToUpper()

            ' Méthode 1: Essayer via DriveInfo (rapide mais peut échouer)
            Try
                Dim driveInfo As New DriveInfo(drive & ":\")
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] DriveType={driveInfo.DriveType}, IsReady={driveInfo.IsReady}")

                If driveInfo.DriveType = DriveType.CDRom AndAlso driveInfo.IsReady Then
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Lecteur {drive} prêt selon DriveInfo")
                End If
            Catch driveEx As Exception
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] DriveInfo échoué: {driveEx.Message}")
            End Try

            ' Méthode 2: Essayer de lire directement la TOC (plus fiable)
            Dim tracks = LirePistesCD(drive & ":")
            Dim hasCD = tracks.Count > 0

            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Résultat final: {If(hasCD, "CD présent", "Pas de CD")} ({tracks.Count} pistes)")
            Return hasCD

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur vérification CD: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] StackTrace: {ex.StackTrace}")
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Lit toutes les pistes d'un CD audio en utilisant l'API Windows native
    ''' </summary>
    Public Shared Function LirePistesCD(driveLetter As String) As List(Of CDTrack)
        Dim pistes As New List(Of CDTrack)
        Try
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Lecture du lecteur {driveLetter} via DeviceIoControl")

            ' Normaliser le nom du lecteur
            Dim drive As String = driveLetter.TrimEnd("\"c, ":"c).ToUpper()
            Dim devicePath As String = $"\\.\{drive}:"

            ' Ouvrir le lecteur CD avec partage READ et WRITE pour éviter ERROR_SHARING_VIOLATION (erreur 32)
            Dim hDevice As IntPtr = CreateFile(devicePath, GENERIC_READ,
                                               FILE_SHARE_READ Or FILE_SHARE_WRITE,
                                               IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero)

            If hDevice = INVALID_HANDLE_VALUE Then
                Dim err = Marshal.GetLastWin32Error()
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Impossible d'ouvrir {devicePath}, erreur: {err}")

                ' Afficher un message d'aide selon l'erreur
                Select Case err
                    Case 32 ' ERROR_SHARING_VIOLATION
                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Le lecteur est utilisé par un autre programme (Explorer, Nero, EAC, etc.)")
                    Case 21 ' ERROR_NOT_READY
                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Aucun CD dans le lecteur ou lecteur pas prêt")
                    Case 5 ' ERROR_ACCESS_DENIED
                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Accès refusé - droits administrateur nécessaires?")
                End Select

                Return pistes
            End If

            Try
                ' Lire la table des matières (TOC) du CD
                Dim toc As New CDROM_TOC()
                Dim bytesReturned As UInteger = 0
                Dim success As Boolean = DeviceIoControl(hDevice, IOCTL_CDROM_READ_TOC, IntPtr.Zero, 0,
                                                         toc, CUInt(Marshal.SizeOf(toc)), bytesReturned, IntPtr.Zero)

                If Not success Then
                    Dim err = Marshal.GetLastWin32Error()
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] DeviceIoControl failed, erreur: {err}")
                    Return pistes
                End If

                Dim firstTrack As Integer = toc.FirstTrack
                Dim lastTrack As Integer = toc.LastTrack
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] TOC lu: {lastTrack - firstTrack + 1} pistes (#{firstTrack} à #{lastTrack})")

                ' Trouver l'entrée Lead-Out (TrackNumber = 0xAA = 170)
                Dim leadOutIndex As Integer = -1
                For j As Integer = 0 To 99
                    If toc.TrackData(j).TrackNumber = &HAA Then
                        leadOutIndex = j
                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Lead-Out trouvé à l'index {j}")
                        Exit For
                    End If
                Next

                ' Créer une entrée pour chaque piste
                For i As Integer = firstTrack To lastTrack
                    Dim trackIndex = i - firstTrack
                    Dim trackData = toc.TrackData(trackIndex)

                    ' Pour la dernière piste, utiliser le Lead-Out
                    ' Pour les autres, utiliser la piste suivante
                    Dim nextTrackData As TRACK_DATA
                    If i < lastTrack Then
                        ' Utiliser l'entrée suivante si elle semble valide, sinon rechercher un remplacement
                        Dim candidate As TRACK_DATA = toc.TrackData(trackIndex + 1)
                        If candidate.TrackNumber <> 0 OrElse HasNonZeroAddress(candidate) Then
                            nextTrackData = candidate
                        Else
                            ' Si l'entrée suivante est vide ou invalide, essayer de trouver un lead-out ou
                            ' la prochaine entrée non vide dans la table TOC
                            Dim found As Boolean = False
                            If leadOutIndex >= 0 Then
                                nextTrackData = toc.TrackData(leadOutIndex)
                                found = True
                            Else
                                For k As Integer = trackIndex + 1 To toc.TrackData.Length - 1
                                    If toc.TrackData(k).TrackNumber <> 0 OrElse HasNonZeroAddress(toc.TrackData(k)) Then
                                        nextTrackData = toc.TrackData(k)
                                        found = True
                                        Exit For
                                    End If
                                Next
                            End If

                            If Not found Then
                                ' Dernier recours: utiliser le candidat même si invalide (protection contre exceptions)
                                nextTrackData = candidate
                                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Warning: TOC entry suivante invalide pour la piste {i}, utilisation du candidat index {trackIndex + 1}")
                            End If
                        End If
                    Else
                        ' Dernière piste: préférer le lead-out, sinon rechercher une entrée valide
                        If leadOutIndex >= 0 Then
                            nextTrackData = toc.TrackData(leadOutIndex)
                        Else
                            Dim found As Boolean = False
                            For k As Integer = trackIndex + 1 To toc.TrackData.Length - 1
                                If toc.TrackData(k).TrackNumber <> 0 OrElse HasNonZeroAddress(toc.TrackData(k)) Then
                                    nextTrackData = toc.TrackData(k)
                                    found = True
                                    Exit For
                                End If
                            Next

                            If Not found Then
                                ' Aucun lead-out ni entrée suivante valide: utiliser la même piste comme fallback
                                nextTrackData = toc.TrackData(trackIndex)
                                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Warning: Lead-Out introuvable pour la dernière piste {i}, utilisation d'un fallback")
                            End If
                        End If
                    End If

                    ' Calculer la durée en frames (75 frames = 1 seconde)
                    Dim startFrame As Integer = MSFToFrames(trackData.Address(1), trackData.Address(2), trackData.Address(3))
                    Dim endFrame As Integer = MSFToFrames(nextTrackData.Address(1), nextTrackData.Address(2), nextTrackData.Address(3))
                    ' Protection: si l'adresse de fin n'est pas valide (<= début), estimer une durée par défaut
                    If endFrame <= startFrame Then
                        Dim defaultSeconds As Integer = 180 ' 3 minutes par défaut
                        Dim estimatedEnd As Integer = startFrame + (CD_FRAMES_PER_SECOND * defaultSeconds)
                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Warning: endFrame ({endFrame}) <= startFrame ({startFrame}) pour la piste {i}. Estimation endFrame -> {estimatedEnd}")
                        endFrame = estimatedEnd
                    End If
                    Dim durationSeconds As Double = (endFrame - startFrame) / 75.0

                    ' Diagnostic détaillé pour toutes les pistes
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Piste {i}: MSF={trackData.Address(1):D2}:{trackData.Address(2):D2}:{trackData.Address(3):D2} → frames {startFrame} à {endFrame}, durée={durationSeconds:F2}s")

                    ' Vérifier que la durée est raisonnable (éviter les valeurs aberrantes)
                    If durationSeconds > 0 AndAlso durationSeconds < 6000 Then ' Max 100 minutes par piste
                        Dim trackPrefix = LanguageManager.GetString("CDTrack_Prefix")
                        pistes.Add(New CDTrack With {
                            .Drive = drive & ":",
                            .TrackNumber = i,
                            .Duration = TimeSpan.FromSeconds(durationSeconds),
                            .Title = $"{trackPrefix} {i:D2}",
                            .StartFrame = startFrame,
                            .EndFrame = endFrame
                        })

                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Piste {i}: {TimeSpan.FromSeconds(durationSeconds):mm\:ss} (frames {startFrame}-{endFrame})")
                    Else
                        ' Fallback: ne pas ignorer la piste, estimer une durée par défaut
                        Dim fallbackSeconds As Integer = 180 ' 3 minutes par défaut
                        Dim fallbackEndFrame As Integer = startFrame + (CD_FRAMES_PER_SECOND * fallbackSeconds)
                        Dim trackPrefix = LanguageManager.GetString("CDTrack_Prefix")

                        pistes.Add(New CDTrack With {
                            .Drive = drive & ":",
                            .TrackNumber = i,
                            .Duration = TimeSpan.FromSeconds(fallbackSeconds),
                            .Title = $"{trackPrefix} {i:D2}",
                            .StartFrame = startFrame,
                            .EndFrame = fallbackEndFrame
                        })

                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Piste {i}: durée invalide ({durationSeconds:F2}s), fallback appliqué à {fallbackSeconds}s (frames {startFrame}-{fallbackEndFrame})")
                    End If
                Next

            Finally
                CloseHandle(hDevice)
            End Try

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur lecture pistes CD: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] StackTrace: {ex.StackTrace}")
        End Try

        Return pistes
    End Function

    ' Convertir MSF (Minute/Second/Frame) en frames totaux
    ' Note: Les adresses MSF des CD incluent un offset de 150 frames (2 secondes)
    ' pour la numérotation Red Book, mais pour la lecture réelle il faut utiliser
    ' l'adresse absolue telle quelle
    Private Shared Function MSFToFrames(minute As Byte, second As Byte, frame As Byte) As Integer
        Return (minute * 60 + second) * 75 + frame
    End Function

    ' Vérifie si une TRACK_DATA contient une adresse MSF non nulle
    Private Shared Function HasNonZeroAddress(td As TRACK_DATA) As Boolean
        Try
            If td.Address Is Nothing Then Return False
            For i As Integer = 0 To td.Address.Length - 1
                If td.Address(i) <> 0 Then
                    Return True
                End If
            Next
        Catch
            ' En cas d'erreur, considérer comme invalide
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Vérifie si un chemin est une piste de CD virtuelle
    ''' </summary>
    Public Shared Function EstCheminCDAudio(chemin As String) As Boolean
        Return Not String.IsNullOrEmpty(chemin) AndAlso chemin.StartsWith("CDDA://", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Parse un chemin virtuel CDDA:// et retourne les informations
    ''' </summary>
    Public Shared Function ParseCheminCDAudio(chemin As String) As CDTrack
        Try
            ' Format: CDDA://D:/Track01
            If Not EstCheminCDAudio(chemin) Then Return Nothing

            Dim parts = chemin.Substring(7).Split("/"c) ' Enlever "CDDA://"
            If parts.Length < 2 Then Return Nothing

            Dim drive = parts(0) ' "D:"
            Dim trackPart = parts(1) ' "Track01"

            ' Extraire le numéro de piste
            Dim trackNumStr = trackPart.Replace("Track", "")
            Dim trackNum As Integer
            If Not Integer.TryParse(trackNumStr, trackNum) Then Return Nothing

            ' Relire les pistes pour obtenir la durée
            Dim pistes = LirePistesCD(drive)
            Dim pisteCorrespondante = pistes.FirstOrDefault(Function(p) p.TrackNumber = trackNum)

            If pisteCorrespondante IsNot Nothing Then
                Return pisteCorrespondante
            Else
                ' Si on ne trouve pas la piste dans la TOC, retourner quand même avec durée 0
                Return New CDTrack With {
                    .Drive = drive,
                    .TrackNumber = trackNum,
                    .Title = trackPart,
                    .Duration = TimeSpan.Zero
                }
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur parsing chemin CD: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Crée un WaveStream pour lire une piste de CD audio via NAudio
    ''' </summary>
    Public Shared Function CreerLecteurCDAudio(track As CDTrack) As WaveStream
        Try
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Création lecteur pour {track.Drive} piste {track.TrackNumber}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Frames stockés: {track.StartFrame} à {track.EndFrame} (durée: {track.Duration:mm\:ss})")

            ' Créer un lecteur CD avec les informations de la piste, en passant les frames précalculés
            Dim cdReader As New CDReader(track.Drive, track.TrackNumber, track.Duration, track.StartFrame, track.EndFrame)
            Return cdReader

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur création lecteur CD: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] StackTrace: {ex.StackTrace}")
            Return Nothing
        End Try
    End Function

#Region "MCI (Media Control Interface) - API Windows pour CD Audio"

    ' Commandes MCI
    Private Const MCI_OPEN As UInteger = &H803UI
    Private Const MCI_CLOSE As UInteger = &H804UI
    Private Const MCI_STATUS As UInteger = &H814UI
    Private Const MCI_SET As UInteger = &H80DUI

    ' Flags MCI
    Private Const MCI_OPEN_TYPE As UInteger = &H2000UI
    Private Const MCI_OPEN_ELEMENT As UInteger = &H200UI
    Private Const MCI_STATUS_ITEM As UInteger = &H100UI
    Private Const MCI_STATUS_NUMBER_OF_TRACKS As UInteger = &H4UI ' 4 = nombre de pistes
    Private Const MCI_STATUS_LENGTH As UInteger = &H1UI
    Private Const MCI_STATUS_POSITION As UInteger = &H2UI
    Private Const MCI_STATUS_READY As UInteger = &H7UI
    Private Const MCI_STATUS_MEDIA_PRESENT As UInteger = &H5UI ' 5 = média présent
    Private Const MCI_TRACK As UInteger = &H10UI
    Private Const MCI_SET_TIME_FORMAT As UInteger = &H400UI
    Private Const MCI_FORMAT_MILLISECONDS As UInteger = &H0UI
    Private Const MCI_FORMAT_TMSF As UInteger = &HAUI ' Track/Minute/Second/Frame

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode, Pack:=1)>
    Private Structure MCI_OPEN_PARMS
        Public dwCallback As IntPtr
        Public wDeviceID As UInteger
        Public lpstrDeviceType As String
        Public lpstrElementName As String
        Public lpstrAlias As String
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Private Structure MCI_STATUS_PARMS
        Public dwCallback As IntPtr
        Public dwReturn As UInteger
        Public dwItem As UInteger
        Public dwTrack As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Private Structure MCI_SET_PARMS
        Public dwCallback As IntPtr
        Public dwTimeFormat As UInteger
    End Structure

    <DllImport("winmm.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function mciSendCommand(wDeviceID As UInteger, uMessage As UInteger, dwParam1 As UInteger, ByRef dwParam2 As MCI_OPEN_PARMS) As Integer
    End Function

    <DllImport("winmm.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function mciSendCommand(wDeviceID As UInteger, uMessage As UInteger, dwParam1 As UInteger, ByRef dwParam2 As MCI_STATUS_PARMS) As Integer
    End Function

    <DllImport("winmm.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function mciSendCommand(wDeviceID As UInteger, uMessage As UInteger, dwParam1 As UInteger, ByRef dwParam2 As MCI_SET_PARMS) As Integer
    End Function

    <DllImport("winmm.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function mciGetErrorString(dwError As UInteger, lpszErrorText As System.Text.StringBuilder, cchErrorText As UInteger) As Boolean
    End Function

    Private Shared Function OuvrirCDAudio(driveLetter As String) As UInteger
        Try
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Tentative d'ouverture du lecteur: '{driveLetter}'")

            ' Nettoyer le nom du lecteur - MCI accepte juste la lettre ou lettre:
            Dim deviceName As String = driveLetter.TrimEnd("\"c, "/"c).TrimEnd(":"c) & ":"

            Dim openParams As New MCI_OPEN_PARMS With {
                .dwCallback = IntPtr.Zero,
                .wDeviceID = 0,
                .lpstrDeviceType = "cdaudio",
                .lpstrElementName = deviceName,
                .lpstrAlias = Nothing
            }

            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Device name formaté: '{deviceName}'")
            Dim result = mciSendCommand(0, MCI_OPEN, MCI_OPEN_TYPE Or MCI_OPEN_ELEMENT, openParams)

            If result = 0 Then
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] CD ouvert avec succès, deviceID: {openParams.wDeviceID}")
                ' Configurer le format de temps en TMSF pour les CD audio
                ' (Track/Minute/Second/Frame - format natif des CD audio)
                Dim setParams As New MCI_SET_PARMS With {
                    .dwCallback = IntPtr.Zero,
                    .dwTimeFormat = MCI_FORMAT_TMSF
                }
                Dim setResult = mciSendCommand(openParams.wDeviceID, MCI_SET, MCI_SET_TIME_FORMAT, setParams)
                If setResult <> 0 Then
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] mciSendCommand SET TMSF failed with error: {setResult}")
                    Dim errorMsg As New System.Text.StringBuilder(256)
                    mciGetErrorString(CUInt(setResult), errorMsg, CUInt(errorMsg.Capacity))
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] MCI Error SET: {errorMsg.ToString()}")
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Format TMSF défini avec succès")
                End If
                Return openParams.wDeviceID
            Else
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] mciSendCommand OPEN failed with error: {result}")
                ' Obtenir le message d'erreur MCI
                Dim errorMsg As New System.Text.StringBuilder(256)
                mciGetErrorString(CUInt(result), errorMsg, CUInt(errorMsg.Capacity))
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] MCI Error: {errorMsg.ToString()}")
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Exception MCI OPEN: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] StackTrace: {ex.StackTrace}")
        End Try
        Return 0
    End Function

    Private Shared Sub FermerCDAudio(deviceId As UInteger)
        Try
            If deviceId = 0 Then Return
            Dim openParams As New MCI_OPEN_PARMS With {
                .dwCallback = IntPtr.Zero,
                .wDeviceID = 0,
                .lpstrDeviceType = Nothing,
                .lpstrElementName = Nothing,
                .lpstrAlias = Nothing
            }
            mciSendCommand(deviceId, MCI_CLOSE, 0, openParams)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur MCI CLOSE: {ex.Message}")
        End Try
    End Sub

    Private Shared Function ObtenirNombrePistes(deviceId As UInteger) As Integer
        Try
            ' Ne pas vérifier média présent - essayer directement de lire les pistes
            ' Si aucun CD n'est présent, cette commande échouera simplement
            Dim statusParams As New MCI_STATUS_PARMS With {
                .dwCallback = IntPtr.Zero,
                .dwReturn = 0,
                .dwItem = MCI_STATUS_NUMBER_OF_TRACKS,
                .dwTrack = 0
            }
            Dim result = mciSendCommand(deviceId, MCI_STATUS, MCI_STATUS_ITEM, statusParams)
            If result = 0 Then
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Nombre de pistes retourné: {statusParams.dwReturn}")
                Return CInt(statusParams.dwReturn)
            Else
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] mciSendCommand STATUS NUMBER_OF_TRACKS failed with error: {result}")
                ' Obtenir le message d'erreur MCI
                Dim errorMsg As New System.Text.StringBuilder(256)
                mciGetErrorString(CUInt(result), errorMsg, CUInt(errorMsg.Capacity))
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] MCI Error: {errorMsg.ToString()}")

                ' Essayer avec la valeur alternative pour NUMBER_OF_TRACKS
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Essai avec MCI_STATUS_MODE...")
                statusParams.dwItem = &H3UI ' Essayer l'ancienne valeur
                result = mciSendCommand(deviceId, MCI_STATUS, MCI_STATUS_ITEM, statusParams)
                If result = 0 Then
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Mode retourné: {statusParams.dwReturn}")
                Else
                    Dim errMsg2 As New System.Text.StringBuilder(256)
                    mciGetErrorString(CUInt(result), errMsg2, CUInt(errMsg2.Capacity))
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] MCI Error MODE: {errMsg2.ToString()}")
                End If
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur MCI STATUS: {ex.Message}")
        End Try
        Return 0
    End Function

    Private Shared Function ObtenirDureePiste(deviceId As UInteger, trackNumber As Integer) As TimeSpan
        Try
            Dim statusParams As New MCI_STATUS_PARMS With {
                .dwCallback = IntPtr.Zero,
                .dwReturn = 0,
                .dwItem = MCI_STATUS_LENGTH,
                .dwTrack = CUInt(trackNumber)
            }
            Dim result = mciSendCommand(deviceId, MCI_STATUS, MCI_STATUS_ITEM Or MCI_TRACK, statusParams)
            If result = 0 Then
                ' dwReturn contient la durée en millisecondes
                Return TimeSpan.FromMilliseconds(statusParams.dwReturn)
            Else
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] mciSendCommand LENGTH failed with error: {result}")
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur durée piste: {ex.Message}")
        End Try
        Return TimeSpan.Zero
    End Function

#End Region

#Region "CDReader - Lecteur de piste CD via MCI"

    ''' <summary>
    ''' Lecteur de piste CD compatible avec NAudio WaveStream
    ''' </summary>
    Public Class CDReader
        Inherits WaveStream

        Private _drive As String
        Private _trackNumber As Integer
        Private _waveFormat As WaveFormat
        Private _length As Long
        Private _position As Long
        Private _duration As TimeSpan
        Private _startFrame As Integer
        Private _endFrame As Integer
        Private _hDevice As IntPtr
        Private _buffer As Byte()
        Private _bufferPosition As Integer
        Private _bufferDataLength As Integer

        Public Sub New(drive As String, trackNumber As Integer, duration As TimeSpan, startFrame As Integer, endFrame As Integer)
            _drive = drive
            _trackNumber = trackNumber
            _duration = duration
            _startFrame = startFrame
            _endFrame = endFrame
            _waveFormat = New WaveFormat(44100, 16, 2) ' CD Audio standard
            _buffer = New Byte(CD_SECTOR_SIZE * 10 - 1) {} ' Buffer pour 10 secteurs
            _bufferPosition = 0
            _bufferDataLength = 0

            ' Ouvrir le lecteur CD
            ' Format attendu par Windows: \\.\D: (avec les deux-points)
            Dim driveLetter As String = drive.TrimEnd("\"c, ":"c).ToUpper()
            Dim devicePath As String = $"\\.\{driveLetter}:"
            System.Diagnostics.Debug.WriteLine($"[CDReader] Tentative d'ouverture de {devicePath}")

            ' Ouvrir avec FILE_SHARE_READ | FILE_SHARE_WRITE pour éviter ERROR_SHARING_VIOLATION
            _hDevice = CreateFile(devicePath, GENERIC_READ,
                                  FILE_SHARE_READ Or FILE_SHARE_WRITE,
                                  IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero)

            If _hDevice = INVALID_HANDLE_VALUE Then
                Dim lastError = Marshal.GetLastWin32Error()
                System.Diagnostics.Debug.WriteLine($"[CDReader] Erreur CreateFile: code {lastError}")
                Throw New InvalidOperationException($"Impossible d'ouvrir le lecteur CD {drive} (erreur {lastError})")
            End If

            ' Les frames sont déjà calculés et passés en paramètre, pas besoin de relire la TOC
            ' Calculer la longueur en bytes (chaque frame = 2352 bytes bruts)
            Dim totalFrames = _endFrame - _startFrame
            _length = CLng(totalFrames) * 2352L

            ' CORRECTION SPÉCIALE pour piste 1 : Réduire légèrement _length pour compenser
            ' la différence entre -150 et -337 frames et éviter débordement de 3-4 secondes
            If trackNumber = 1 Then
                ' Réduire de 215 frames (187 + 28 de marge) = environ 2.87 secondes
                ' Les 28 frames supplémentaires (≈0.37s) éliminent complètement la note résiduelle
                _length = Math.Max(0, _length - (215L * 2352L))
                System.Diagnostics.Debug.WriteLine($"[CDReader] Piste 1: _length réduit de 215 frames pour éviter débordement")
            End If

            _position = 0

            System.Diagnostics.Debug.WriteLine($"[CDReader] Piste {trackNumber}: frames {_startFrame} à {_endFrame} (total: {totalFrames})")
        End Sub

        Public Overrides ReadOnly Property WaveFormat As WaveFormat
            Get
                Return _waveFormat
            End Get
        End Property

        Public Overrides ReadOnly Property Length As Long
            Get
                Return _length
            End Get
        End Property

        Public Overrides Property Position As Long
            Get
                Return _position
            End Get
            Set(value As Long)
                _position = value
                _bufferPosition = 0
                _bufferDataLength = 0
            End Set
        End Property

        Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
            If _hDevice = INVALID_HANDLE_VALUE Then
                Return 0
            End If

            Dim totalBytesRead As Integer = 0

            While totalBytesRead < count AndAlso _position < _length
                ' Si le buffer interne est vide, lire plus de secteurs du CD
                If _bufferPosition >= _bufferDataLength Then
                    Dim currentFrame = _startFrame + CInt(_position \ 2352)

                    ' ✅ CORRECTION : Calculer les frames restants jusqu'à la fin de la piste
                    Dim framesRestants = _endFrame - currentFrame
                    If framesRestants <= 0 Then
                        ' On a atteint ou dépassé la fin de la piste
                        Exit While
                    End If

                    ' Lire au maximum 10 frames, mais pas plus que ce qui reste
                    Dim framesToRead = Math.Min(10, framesRestants)

                    ' Lire les secteurs bruts du CD
                    ' Pour IOCTL_CDROM_RAW_READ avec TrackMode=2 (CDDA):
                    ' DiskOffset doit être en unités de secteurs logiques de 2048 bytes
                    ' CORRECTION d'offset nécessaire pour la synchronisation CD:
                    ' - Piste 1: Correction de -150 frames (limitée par frame de départ ~152)
                    ' - Autres pistes: Correction de -337 frames (~4.5 secondes)
                    ' MAIS pour la piste 1, on limite aussi framesToRead à la fin pour éviter débordement
                    Dim frameALire As Long
                    If _trackNumber = 1 Then
                        ' Piste 1: -150 frames = environ 2 secondes
                        frameALire = currentFrame - 150
                        If frameALire < 0 Then frameALire = 0
                    Else
                        ' Autres pistes: -337 frames = environ 4.5 secondes
                        frameALire = currentFrame - 337
                        If frameALire < 0 Then frameALire = 0
                    End If

                    Dim rawRead As New RAW_READ_INFO With {
                        .DiskOffset = frameALire * 2048L,
                        .SectorCount = CUInt(framesToRead),
                        .TrackMode = 2 ' CDDA (audio)
                    }

                    If _position = 0 Then
                        System.Diagnostics.Debug.WriteLine($"[CDReader] ⭐ PREMIÈRE LECTURE Piste {_trackNumber} - currentFrame={currentFrame}, frameALire={frameALire} (correction={If(_trackNumber = 1, "-150", "-337")}), DiskOffset={rawRead.DiskOffset}, framesToRead={framesToRead}, framesRestants={framesRestants}, _startFrame={_startFrame}, _endFrame={_endFrame}")
                    End If

                    Dim bytesReturned As UInteger = 0
                    Dim bufferHandle As GCHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned)

                    Try
                        Dim success = DeviceIoControl(_hDevice, IOCTL_CDROM_RAW_READ,
                                                      rawRead, CUInt(Marshal.SizeOf(rawRead)),
                                                      bufferHandle.AddrOfPinnedObject(), CUInt(_buffer.Length),
                                                      bytesReturned, IntPtr.Zero)

                        If Not success OrElse bytesReturned = 0 Then
                            System.Diagnostics.Debug.WriteLine($"[CDReader] Erreur lecture secteur {currentFrame}")
                            Exit While
                        End If

                        _bufferDataLength = CInt(bytesReturned)
                        _bufferPosition = 0
                    Finally
                        bufferHandle.Free()
                    End Try
                End If

                ' Copier du buffer interne vers le buffer de sortie
                Dim bytesToCopy = Math.Min(count - totalBytesRead, _bufferDataLength - _bufferPosition)

                ' ✅ CORRECTION : S'assurer de ne JAMAIS dépasser _length
                Dim bytesRemainingInTrack = CInt(_length - _position)
                If bytesRemainingInTrack <= 0 Then
                    ' On a atteint la fin de la piste
                    Exit While
                End If

                bytesToCopy = Math.Min(bytesToCopy, bytesRemainingInTrack)

                If bytesToCopy <= 0 Then
                    Exit While
                End If

                Array.Copy(_buffer, _bufferPosition, buffer, offset + totalBytesRead, bytesToCopy)
                _bufferPosition += bytesToCopy
                _position += bytesToCopy
                totalBytesRead += bytesToCopy
            End While

            Return totalBytesRead
        End Function

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If _hDevice <> INVALID_HANDLE_VALUE Then
                    CloseHandle(_hDevice)
                    _hDevice = INVALID_HANDLE_VALUE
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub
    End Class

#End Region

End Class
