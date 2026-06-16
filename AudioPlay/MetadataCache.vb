Imports System.IO
Imports System.Text.Json
Imports System.Threading

Public Module MetadataCache
    Private cache As Dictionary(Of String, CachedEntry) = Nothing
    Private cachePath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "metadata_cache.json")
    Private cacheLock As New Object()
    ' Debounced save to limit IO
    Private saveTimer As Timer = Nothing
    Private saveDelayMs As Integer = 2000
    Private pendingSave As Boolean = False
    Private saveLock As New Object()

    Public Class CachedEntry
        Public Property Duration As String
        Public Property BPM As String
        Public Property LastWriteUtc As DateTime
        Public Property FileLength As Long
    End Class

    Public Sub LoadCache()
        SyncLock cacheLock
            Try
                If cache IsNot Nothing Then Return
                cache = New Dictionary(Of String, CachedEntry)()
                If File.Exists(cachePath) Then
                    Dim txt = File.ReadAllText(cachePath)
                    If Not String.IsNullOrWhiteSpace(txt) Then
                        Dim opts = New JsonSerializerOptions() With {.PropertyNameCaseInsensitive = True}
                        Dim loaded = JsonSerializer.Deserialize(Of Dictionary(Of String, CachedEntry))(txt, opts)
                        If loaded IsNot Nothing Then cache = loaded
                    End If
                End If
            Catch
                cache = New Dictionary(Of String, CachedEntry)()
            End Try
        End SyncLock
    End Sub

    Public Sub SaveCache()
        SyncLock cacheLock
            Try
                Dim dir = Path.GetDirectoryName(cachePath)
                If Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)
                Dim opts = New JsonSerializerOptions() With {.WriteIndented = True}
                Dim txt = JsonSerializer.Serialize(cache, opts)
                File.WriteAllText(cachePath, txt)
            Catch
            End Try
        End SyncLock
    End Sub

    Private Sub SaveCacheDebounced()
        SyncLock saveLock
            pendingSave = True
            If saveTimer Is Nothing Then
                saveTimer = New Timer(AddressOf SaveTimerCallback, Nothing, saveDelayMs, Timeout.Infinite)
            Else
                saveTimer.Change(saveDelayMs, Timeout.Infinite)
            End If
        End SyncLock
    End Sub

    Private Sub SaveTimerCallback(state As Object)
        Try
            SyncLock saveLock
                pendingSave = False
            End SyncLock
            SaveCache()
        Catch
        End Try
    End Sub

    Public Function GetCached(chemin As String) As CachedEntry
        Try
            LoadCache()
            If String.IsNullOrEmpty(chemin) Then Return Nothing
            If Not File.Exists(chemin) Then Return Nothing
            Dim fi = New FileInfo(chemin)
            SyncLock cacheLock
                If cache IsNot Nothing AndAlso cache.ContainsKey(chemin) Then
                    Dim e = cache(chemin)
                    If e IsNot Nothing Then
                        ' vérifier si le fichier a changé
                        If e.FileLength = fi.Length AndAlso e.LastWriteUtc = fi.LastWriteTimeUtc Then
                            Return e
                        End If
                    End If
                End If
            End SyncLock
        Catch
        End Try
        Return Nothing
    End Function

    Public Sub UpdateCache(chemin As String, duree As String, bpm As String)
        Try
            LoadCache()
            If String.IsNullOrEmpty(chemin) Then Return
            If Not File.Exists(chemin) Then Return
            Dim fi = New FileInfo(chemin)
            Dim e As New CachedEntry With {
                .Duration = duree,
                .BPM = bpm,
                .LastWriteUtc = fi.LastWriteTimeUtc,
                .FileLength = fi.Length
            }
            SyncLock cacheLock
                cache(chemin) = e
            End SyncLock
            ' Sauvegarder de façon debounced pour réduire les écritures
            Try
                SaveCacheDebounced()
            Catch
            End Try
        Catch
        End Try
    End Sub
End Module
