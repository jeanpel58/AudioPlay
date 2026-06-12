Imports System.Runtime.InteropServices
Imports System.IO

''' <summary>
''' Interopérabilité P/Invoke avec la bibliothèque native SoundTouch
''' Utilise la même DLL C++ qu'Audacity pour un time-stretching de qualité professionnelle
''' </summary>
Public Class SoundTouchInterop
    ' Déterminer la plateforme (x86 ou x64)
    Private Shared ReadOnly Is64Bit As Boolean = IntPtr.Size = 8

    ' Chemin vers la DLL native SoundTouch
    Private Const DllName As String = "SoundTouch.dll"

    ' Chargement manuel de la DLL native depuis le bon répertoire
    Shared Sub New()
        Try
            ' Déterminer l'architecture (x64 ou x86)
            Dim architecture As String = If(Is64Bit, "win-x64", "win-x86")

            ' Construire le chemin vers la DLL native
            Dim basePath As String = AppDomain.CurrentDomain.BaseDirectory
            Dim dllPath As String = Path.Combine(basePath, "runtimes", architecture, "native", DllName)

            ' Si la DLL n'est pas dans runtimes/, essayer à la racine
            If Not File.Exists(dllPath) Then
                dllPath = Path.Combine(basePath, DllName)
            End If

            ' Charger la DLL manuellement
            If File.Exists(dllPath) Then
                Dim handle As IntPtr = LoadLibrary(dllPath)
                If handle = IntPtr.Zero Then
                    System.Diagnostics.Debug.WriteLine($"Impossible de charger SoundTouch depuis: {dllPath}")
                Else
                    System.Diagnostics.Debug.WriteLine($"SoundTouch chargé depuis: {dllPath}")
                End If
            Else
                System.Diagnostics.Debug.WriteLine($"SoundTouch.dll introuvable dans: {dllPath}")
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur chargement SoundTouch: {ex.Message}")
        End Try
    End Sub

    ' Fonction Windows pour charger une DLL
    <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
    Private Shared Function LoadLibrary(lpFileName As String) As IntPtr
    End Function

    ' === Fonctions natives SoundTouch ===

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function soundtouch_createInstance() As IntPtr
    End Function

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_destroyInstance(handle As IntPtr)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_setRate(handle As IntPtr, newRate As Single)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_setTempo(handle As IntPtr, newTempo As Single)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_setRateChange(handle As IntPtr, rateChange As Single)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_setTempoChange(handle As IntPtr, tempoChange As Single)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_setPitch(handle As IntPtr, newPitch As Single)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_setPitchSemiTones(handle As IntPtr, pitchSemiTones As Single)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_setChannels(handle As IntPtr, numChannels As UInteger)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_setSampleRate(handle As IntPtr, sampleRate As UInteger)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_flush(handle As IntPtr)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_putSamples(handle As IntPtr, samples As Single(), numSamples As UInteger)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function soundtouch_receiveSamples(handle As IntPtr, outBuffer As Single(), maxSamples As UInteger) As UInteger
    End Function

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function soundtouch_numSamples(handle As IntPtr) As UInteger
    End Function

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function soundtouch_numUnprocessedSamples(handle As IntPtr) As UInteger
    End Function

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_clear(handle As IntPtr)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function soundtouch_isEmpty(handle As IntPtr) As Integer
    End Function

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Sub soundtouch_setSetting(handle As IntPtr, settingId As Integer, value As Integer)
    End Sub

    <DllImport(DllName, CallingConvention:=CallingConvention.Cdecl)>
    Private Shared Function soundtouch_getSetting(handle As IntPtr, settingId As Integer) As Integer
    End Function

    ' === Wrapper orienté objet ===

    Private handle As IntPtr = IntPtr.Zero

    Public Sub New()
        Try
            handle = soundtouch_createInstance()
            If handle = IntPtr.Zero Then
                System.Diagnostics.Debug.WriteLine("SoundTouch: createInstance a retourné un handle null")
                Throw New Exception("Impossible de créer l'instance SoundTouch native")
            End If
            System.Diagnostics.Debug.WriteLine($"SoundTouch: instance créée avec handle {handle}")
        Catch ex As DllNotFoundException
            System.Diagnostics.Debug.WriteLine($"SoundTouch: DLL non trouvée - {ex.Message}")
            Throw
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"SoundTouch: erreur création instance - {ex.Message}")
            Throw
        End Try
    End Sub

    Public Function IsValid() As Boolean
        Return handle <> IntPtr.Zero
    End Function

    Public Sub SetSampleRate(sampleRate As Integer)
        If handle = IntPtr.Zero Then Return
        soundtouch_setSampleRate(handle, CUInt(sampleRate))
    End Sub

    Public Sub SetChannels(channels As Integer)
        If handle = IntPtr.Zero Then Return
        soundtouch_setChannels(handle, CUInt(channels))
    End Sub

    ''' <summary>
    ''' Définir le tempo (1.0 = normal, 0.5 = 50% plus lent, 2.0 = 2x plus rapide)
    ''' </summary>
    Public Sub SetTempo(tempo As Single)
        If handle = IntPtr.Zero Then Return
        soundtouch_setTempo(handle, tempo)
    End Sub

    ''' <summary>
    ''' Définir le changement de tempo en pourcentage (-50 = 50% plus lent, +100 = 2x plus rapide)
    ''' </summary>
    Public Sub SetTempoChange(tempoChangePercent As Single)
        If handle = IntPtr.Zero Then Return
        soundtouch_setTempoChange(handle, tempoChangePercent)
    End Sub

    ''' <summary>
    ''' Définir le pitch en demi-tons (0 = normal, +12 = une octave plus haut)
    ''' </summary>
    Public Sub SetPitchSemiTones(pitchSemiTones As Single)
        If handle = IntPtr.Zero Then Return
        soundtouch_setPitchSemiTones(handle, pitchSemiTones)
    End Sub

    ''' <summary>
    ''' Envoyer des échantillons à traiter
    ''' </summary>
    Public Sub PutSamples(samples As Single(), numSamples As Integer)
        If handle = IntPtr.Zero Then Return
        soundtouch_putSamples(handle, samples, CUInt(numSamples))
    End Sub

    ''' <summary>
    ''' Récupérer les échantillons traités
    ''' </summary>
    Public Function ReceiveSamples(outBuffer As Single(), maxSamples As Integer) As Integer
        If handle = IntPtr.Zero Then Return 0
        Return CInt(soundtouch_receiveSamples(handle, outBuffer, CUInt(maxSamples)))
    End Function

    ''' <summary>
    ''' Vider le pipeline (à appeler en fin de traitement)
    ''' </summary>
    Public Sub Flush()
        If handle = IntPtr.Zero Then Return
        soundtouch_flush(handle)
    End Sub

    ''' <summary>
    ''' Nombre d'échantillons disponibles en sortie
    ''' </summary>
    Public Function NumSamples() As Integer
        If handle = IntPtr.Zero Then Return 0
        Return CInt(soundtouch_numSamples(handle))
    End Function

    ''' <summary>
    ''' Effacer les buffers internes
    ''' </summary>
    Public Sub Clear()
        If handle = IntPtr.Zero Then Return
        soundtouch_clear(handle)
    End Sub

    ''' <summary>
    ''' Vérifier si le buffer de sortie est vide
    ''' </summary>
    Public Function IsEmpty() As Boolean
        If handle = IntPtr.Zero Then Return True
        Return soundtouch_isEmpty(handle) <> 0
    End Function

    ''' <summary>
    ''' Définir un paramètre de configuration
    ''' </summary>
    Public Sub SetSetting(settingId As Integer, value As Integer)
        If handle = IntPtr.Zero Then Return
        soundtouch_setSetting(handle, settingId, value)
    End Sub

    ' IDs des paramètres SoundTouch
    Public Const SETTING_USE_AA_FILTER As Integer = 0
    Public Const SETTING_AA_FILTER_LENGTH As Integer = 1
    Public Const SETTING_USE_QUICKSEEK As Integer = 2
    Public Const SETTING_SEQUENCE_MS As Integer = 3
    Public Const SETTING_SEEKWINDOW_MS As Integer = 4
    Public Const SETTING_OVERLAP_MS As Integer = 5

    Protected Overrides Sub Finalize()
        If handle <> IntPtr.Zero Then
            soundtouch_destroyInstance(handle)
            handle = IntPtr.Zero
        End If
        MyBase.Finalize()
    End Sub

    Public Sub Dispose()
        If handle <> IntPtr.Zero Then
            soundtouch_destroyInstance(handle)
            handle = IntPtr.Zero
        End If
        GC.SuppressFinalize(Me)
    End Sub
End Class
