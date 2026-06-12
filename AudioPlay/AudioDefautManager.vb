' Sauvegarde et lecture des cases à cocher audio dans un fichier séparé AudioDefaut.txt
Imports System.IO

Module AudioDefautManager
    Private ReadOnly cheminAudioDefaut As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "AudioDefaut.txt")

    Public Sub SauvegarderAudioDefaut(mp3 As Boolean, flac As Boolean, wma As Boolean, wav As Boolean, aac As Boolean)
        Dim lignes As New List(Of String) From {
            "CheckBox_MP3=" & mp3.ToString(),
            "CheckBox_FLAC=" & flac.ToString(),
            "CheckBox_WMA=" & wma.ToString(),
            "CheckBox_WAV=" & wav.ToString(),
            "CheckBox_AAC=" & aac.ToString()
        }
        File.WriteAllLines(cheminAudioDefaut, lignes)
    End Sub

    Public Sub ChargerAudioDefaut(ByRef mp3 As Boolean, ByRef flac As Boolean, ByRef wma As Boolean, ByRef wav As Boolean, ByRef aac As Boolean)
        If Not File.Exists(cheminAudioDefaut) Then Return
        Dim lignes = File.ReadAllLines(cheminAudioDefaut)
        For Each ligne In lignes
            If ligne.Contains("=") Then
                Dim parts = ligne.Split("="c, 2)
                Dim cle = parts(0).Trim()
                Dim valeur = parts(1).Trim()
                Select Case cle
                    Case "CheckBox_MP3" : Boolean.TryParse(valeur, mp3)
                    Case "CheckBox_FLAC" : Boolean.TryParse(valeur, flac)
                    Case "CheckBox_WMA" : Boolean.TryParse(valeur, wma)
                    Case "CheckBox_WAV" : Boolean.TryParse(valeur, wav)
                    Case "CheckBox_AAC" : Boolean.TryParse(valeur, aac)
                End Select
            End If
        Next
    End Sub
End Module
