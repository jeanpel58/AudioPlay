' Vérification des associations par défaut dans le registre
Imports Microsoft.Win32

Module AudioAssociationChecker
    Public Function GetNonAssociatedTypes() As List(Of String)
        Dim result As New List(Of String)
        Dim types As New Dictionary(Of String, String) From {
            {".mp3", "AudioPlay.mp3"},
            {".flac", "AudioPlay.flac"},
            {".wma", "AudioPlay.wma"},
            {".wav", "AudioPlay.wav"},
            {".aac", "AudioPlay.aac"}
        }
        For Each kvp In types
            Try
                Using extKey = Registry.CurrentUser.OpenSubKey($"Software\Classes\{kvp.Key}")
                    If extKey Is Nothing OrElse Not String.Equals(CStr(If(extKey.GetValue("") , "")), kvp.Value, StringComparison.OrdinalIgnoreCase) Then
                        result.Add(kvp.Key)
                    End If
                End Using
            Catch
                result.Add(kvp.Key)
            End Try
        Next
        Return result
    End Function
End Module
