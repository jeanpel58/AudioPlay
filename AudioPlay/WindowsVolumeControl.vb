Imports System.Runtime.InteropServices

''' <summary>
''' Classe pour contrôler le volume système de Windows
''' </summary>
Public Class WindowsVolumeControl

    ' Constantes pour le contrôle du volume
    Private Const APPCOMMAND_VOLUME_MUTE As Integer = &H80000
    Private Const APPCOMMAND_VOLUME_UP As Integer = &HA0000
    Private Const APPCOMMAND_VOLUME_DOWN As Integer = &H90000
    Private Const WM_APPCOMMAND As Integer = &H319

    ' API Windows pour envoyer des commandes
    <DllImport("user32.dll")>
    Private Shared Function SendMessageW(hWnd As IntPtr, Msg As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function

    ' Interface COM pour contrôler le volume de manière plus précise
    <ComImport>
    <Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")>
    Private Class MMDeviceEnumerator
    End Class

    <Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDeviceEnumerator
        Function NotImpl1() As Integer
        Function GetDefaultAudioEndpoint(dataFlow As EDataFlow, role As ERole, <Out> ByRef ppDevice As IMMDevice) As Integer
    End Interface

    <Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IMMDevice
        Function Activate(ByRef iid As Guid, dwClsCtx As Integer, pActivationParams As IntPtr, <Out> ByRef ppInterface As IAudioEndpointVolume) As Integer
    End Interface

    <Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface IAudioEndpointVolume
        Function NotImpl1() As Integer
        Function NotImpl2() As Integer
        Function GetChannelCount(<Out> ByRef pnChannelCount As Integer) As Integer
        Function SetMasterVolumeLevel(fLevelDB As Single, pguidEventContext As Guid) As Integer
        Function SetMasterVolumeLevelScalar(fLevel As Single, pguidEventContext As Guid) As Integer
        Function GetMasterVolumeLevel(<Out> ByRef pfLevelDB As Single) As Integer
        Function GetMasterVolumeLevelScalar(<Out> ByRef pfLevel As Single) As Integer
        Function SetChannelVolumeLevel(nChannel As Integer, fLevelDB As Single, pguidEventContext As Guid) As Integer
        Function SetChannelVolumeLevelScalar(nChannel As Integer, fLevel As Single, pguidEventContext As Guid) As Integer
        Function GetChannelVolumeLevel(nChannel As Integer, <Out> ByRef pfLevelDB As Single) As Integer
        Function GetChannelVolumeLevelScalar(nChannel As Integer, <Out> ByRef pfLevel As Single) As Integer
        Function SetMute(bMute As Boolean, pguidEventContext As Guid) As Integer
        Function GetMute(<Out> ByRef pbMute As Boolean) As Integer
    End Interface

    Private Enum EDataFlow
        eRender
        eCapture
        eAll
    End Enum

    Private Enum ERole
        eConsole
        eMultimedia
        eCommunications
    End Enum

    Private Shared endpointVolume As IAudioEndpointVolume = Nothing

    ''' <summary>
    ''' Initialise l'interface de contrôle du volume
    ''' </summary>
    Private Shared Sub InitializeVolumeControl()
        If endpointVolume IsNot Nothing Then Return

        Try
            Dim deviceEnumerator As IMMDeviceEnumerator = CType(New MMDeviceEnumerator(), IMMDeviceEnumerator)
            Dim device As IMMDevice = Nothing
            deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, device)

            Dim iidIAudioEndpointVolume As Guid = GetType(IAudioEndpointVolume).GUID
            device.Activate(iidIAudioEndpointVolume, 0, IntPtr.Zero, endpointVolume)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lors de l'initialisation du contrôle de volume: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Définit le volume système de Windows (0.0 à 1.0)
    ''' </summary>
    Public Shared Sub SetVolume(volume As Single)
        Try
            InitializeVolumeControl()
            If endpointVolume IsNot Nothing Then
                ' Limiter entre 0.0 et 1.0
                volume = Math.Max(0.0F, Math.Min(1.0F, volume))
                endpointVolume.SetMasterVolumeLevelScalar(volume, Guid.Empty)
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lors du réglage du volume: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Obtient le volume système actuel (0.0 à 1.0)
    ''' </summary>
    Public Shared Function GetVolume() As Single
        Try
            InitializeVolumeControl()
            If endpointVolume IsNot Nothing Then
                Dim volume As Single = 0
                endpointVolume.GetMasterVolumeLevelScalar(volume)
                Return volume
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la lecture du volume: {ex.Message}")
        End Try
        Return 0.5F ' Valeur par défaut
    End Function

    ''' <summary>
    ''' Active ou désactive le mute du système
    ''' </summary>
    Public Shared Sub SetMute(mute As Boolean)
        Try
            InitializeVolumeControl()
            If endpointVolume IsNot Nothing Then
                endpointVolume.SetMute(mute, Guid.Empty)
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lors du réglage du mute: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Vérifie si le système est en mute
    ''' </summary>
    Public Shared Function IsMuted() As Boolean
        Try
            InitializeVolumeControl()
            If endpointVolume IsNot Nothing Then
                Dim muted As Boolean = False
                endpointVolume.GetMute(muted)
                Return muted
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la lecture du mute: {ex.Message}")
        End Try
        Return False
    End Function

End Class
