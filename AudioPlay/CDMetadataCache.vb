Imports System.IO
Imports System.Text.Json

''' <summary>
''' Gestionnaire de cache local pour les métadonnées de CD audio
''' Sauvegarde et restaure les métadonnées indexées par DiscID
''' </summary>
Public Class CDMetadataCache

    Private Shared ReadOnly CacheFilePath As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioPlay",
        "cd_metadata_cache.json"
    )

    ''' <summary>
    ''' Structure pour la sérialisation JSON
    ''' </summary>
    Private Class CacheEntry
        Public Property DiscID As String
        Public Property Artist As String
        Public Property Album As String
        Public Property Year As Integer
        Public Property Genre As String
        Public Property CoverArtUrl As String ' URL de la pochette
        Public Property Source As String ' "MusicBrainz", "Discogs", "Manual"
        Public Property DateAdded As DateTime
        Public Property Tracks As List(Of TrackEntry)

        Public Sub New()
            Tracks = New List(Of TrackEntry)
        End Sub
    End Class

    Private Class TrackEntry
        Public Property TrackNumber As Integer
        Public Property Title As String
        Public Property Artist As String
        Public Property DurationTicks As Long
    End Class

    ''' <summary>
    ''' Sauvegarde les métadonnées d'un CD dans le cache local
    ''' </summary>
    Public Shared Sub SauvegarderMetadonnees(discID As String, cdInfo As CDMetadataProvider.CDInfo, source As String)
        Try
            ' S'assurer que le dossier existe
            Dim directoryPath = Path.GetDirectoryName(CacheFilePath)
            If Not System.IO.Directory.Exists(directoryPath) Then
                System.IO.Directory.CreateDirectory(directoryPath)
            End If

            ' Charger le cache existant
            Dim cache = ChargerCache()

            ' Supprimer l'ancienne entrée si elle existe
            cache.RemoveAll(Function(e) e.DiscID = discID)

            ' Créer la nouvelle entrée
            Dim entry As New CacheEntry With {
                .DiscID = discID,
                .Artist = cdInfo.Artist,
                .Album = cdInfo.Album,
                .Year = cdInfo.Year,
                .Genre = cdInfo.Genre,
                .CoverArtUrl = cdInfo.CoverArtUrl,
                .Source = source,
                .DateAdded = DateTime.Now
            }

            ' Copier les pistes
            For Each track In cdInfo.Tracks
                entry.Tracks.Add(New TrackEntry With {
                    .TrackNumber = track.TrackNumber,
                    .Title = track.Title,
                    .Artist = track.Artist,
                    .DurationTicks = track.Duration.Ticks
                })
            Next

            ' Ajouter au cache
            cache.Add(entry)

            ' Sauvegarder
            Dim options As New JsonSerializerOptions With {
                .WriteIndented = True
            }
            Dim json = JsonSerializer.Serialize(cache, options)
            File.WriteAllText(CacheFilePath, json)

            System.Diagnostics.Debug.WriteLine($"[CDMetadataCache] Métadonnées sauvegardées pour DiscID: {discID}")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDMetadataCache] Erreur sauvegarde: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Récupère les métadonnées d'un CD depuis le cache local
    ''' </summary>
    Public Shared Function RecupererMetadonnees(discID As String) As CDMetadataProvider.CDInfo
        Try
            If Not File.Exists(CacheFilePath) Then
                Return Nothing
            End If

            Dim cache = ChargerCache()
            Dim entry = cache.FirstOrDefault(Function(e) e.DiscID = discID)

            If entry Is Nothing Then
                System.Diagnostics.Debug.WriteLine($"[CDMetadataCache] Aucune entrée trouvée pour DiscID: {discID}")
                Return Nothing
            End If

            ' Reconstruire CDInfo
            Dim cdInfo As New CDMetadataProvider.CDInfo With {
                .Artist = entry.Artist,
                .Album = entry.Album,
                .Year = entry.Year,
                .Genre = entry.Genre,
                .CoverArtUrl = entry.CoverArtUrl
            }

            For Each trackEntry In entry.Tracks
                cdInfo.Tracks.Add(New CDMetadataProvider.TrackInfo With {
                    .TrackNumber = trackEntry.TrackNumber,
                    .Title = trackEntry.Title,
                    .Artist = trackEntry.Artist,
                    .Duration = New TimeSpan(trackEntry.DurationTicks)
                })
            Next

            System.Diagnostics.Debug.WriteLine($"[CDMetadataCache] Métadonnées récupérées depuis cache ({entry.Source}): {cdInfo.Artist} - {cdInfo.Album}")
            Return cdInfo

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDMetadataCache] Erreur récupération: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Vérifie si des métadonnées existent dans le cache pour un DiscID
    ''' </summary>
    Public Shared Function ExisteDansCache(discID As String) As Boolean
        Try
            If Not File.Exists(CacheFilePath) Then
                Return False
            End If

            Dim cache = ChargerCache()
            Return cache.Any(Function(e) e.DiscID = discID)

        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Obtient des informations sur une entrée du cache
    ''' </summary>
    Public Shared Function ObtenirInfoCache(discID As String) As String
        Try
            If Not File.Exists(CacheFilePath) Then
                Return Nothing
            End If

            Dim cache = ChargerCache()
            Dim entry = cache.FirstOrDefault(Function(e) e.DiscID = discID)

            If entry Is Nothing Then
                Return Nothing
            End If

            Return $"{entry.Artist} - {entry.Album} (Source: {entry.Source}, {entry.DateAdded:dd/MM/yyyy})"

        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Supprime une entrée du cache
    ''' </summary>
    Public Shared Sub SupprimerDuCache(discID As String)
        Try
            If Not File.Exists(CacheFilePath) Then
                Return
            End If

            Dim cache = ChargerCache()
            cache.RemoveAll(Function(e) e.DiscID = discID)

            Dim options As New JsonSerializerOptions With {
                .WriteIndented = True
            }
            Dim json = JsonSerializer.Serialize(cache, options)
            File.WriteAllText(CacheFilePath, json)

            System.Diagnostics.Debug.WriteLine($"[CDMetadataCache] Entrée supprimée pour DiscID: {discID}")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDMetadataCache] Erreur suppression: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Efface tout le cache
    ''' </summary>
    Public Shared Sub EffacerCache()
        Try
            If File.Exists(CacheFilePath) Then
                File.Delete(CacheFilePath)
                System.Diagnostics.Debug.WriteLine("[CDMetadataCache] Cache effacé")
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDMetadataCache] Erreur effacement: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Retourne le nombre d'entrées dans le cache
    ''' </summary>
    Public Shared Function ObtenirNombreEntrees() As Integer
        Try
            If Not File.Exists(CacheFilePath) Then
                Return 0
            End If

            Dim cache = ChargerCache()
            Return cache.Count

        Catch ex As Exception
            Return 0
        End Try
    End Function

    ''' <summary>
    ''' Charge le cache depuis le fichier JSON
    ''' </summary>
    Private Shared Function ChargerCache() As List(Of CacheEntry)
        Try
            If Not File.Exists(CacheFilePath) Then
                Return New List(Of CacheEntry)()
            End If

            Dim json = File.ReadAllText(CacheFilePath)
            Return JsonSerializer.Deserialize(Of List(Of CacheEntry))(json)

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDMetadataCache] Erreur chargement cache: {ex.Message}")
            Return New List(Of CacheEntry)()
        End Try
    End Function

End Class
