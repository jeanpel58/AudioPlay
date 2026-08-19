Imports System.Net.Http
Imports System.Text.Json

''' <summary>
''' Fournisseur de métadonnées CD via Discogs API
''' Documentation: https://www.discogs.com/developers
''' </summary>
Public Class DiscogsMetadataProvider

    Private Shared ReadOnly httpClient As New HttpClient()
    Private Const DISCOGS_API_KEY As String = "your_api_key_here" ' À remplacer par une vraie clé
    Private Const DISCOGS_API_SECRET As String = "your_api_secret_here"

    Shared Sub New()
        ' Discogs demande un User-Agent personnalisé (sans '+' devant l'URL)
        httpClient.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0 (https://github.com/jeanpel58/AudioPlay)")
    End Sub

    ''' <summary>
    ''' Recherche un CD sur Discogs par artiste et album
    ''' </summary>
    Public Shared Async Function RechercherCD(artisteRecherche As String, albumRecherche As String) As Task(Of CDMetadataProvider.CDInfo)
        Try
            ' Construire la requête de recherche
            Dim query As String = Uri.EscapeDataString($"{artisteRecherche} {albumRecherche}")
            Dim url As String = $"https://api.discogs.com/database/search?q={query}&type=release&format=CD&per_page=5"

            System.Diagnostics.Debug.WriteLine($"[DiscogsMetadataProvider] Requête: {url}")

            Dim response = Await httpClient.GetAsync(url)

            If Not response.IsSuccessStatusCode Then
                System.Diagnostics.Debug.WriteLine($"[DiscogsMetadataProvider] Erreur HTTP: {response.StatusCode}")
                Return Nothing
            End If

            Dim json = Await response.Content.ReadAsStringAsync()
            System.Diagnostics.Debug.WriteLine($"[DiscogsMetadataProvider] Réponse reçue")

            ' Parser les résultats
            Dim doc = JsonDocument.Parse(json)
            Dim root = doc.RootElement

            If Not root.TryGetProperty("results", Nothing) Then
                Return Nothing
            End If

            Dim results = root.GetProperty("results")
            If results.GetArrayLength() = 0 Then
                System.Diagnostics.Debug.WriteLine($"[DiscogsMetadataProvider] Aucun résultat trouvé")
                Return Nothing
            End If

            ' Prendre le premier résultat (le plus pertinent)
            Dim release = results(0)
            Dim releaseId As String = ""

            If release.TryGetProperty("id", Nothing) Then
                releaseId = release.GetProperty("id").ToString()
            End If

            If String.IsNullOrEmpty(releaseId) Then
                Return Nothing
            End If

            ' Récupérer les détails complets du release
            Return Await ObtenirDetailsRelease(releaseId)

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[DiscogsMetadataProvider] Erreur: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Récupère les détails complets d'un release Discogs
    ''' </summary>
    Private Shared Async Function ObtenirDetailsRelease(releaseId As String) As Task(Of CDMetadataProvider.CDInfo)
        Try
            Dim url As String = $"https://api.discogs.com/releases/{releaseId}"
            System.Diagnostics.Debug.WriteLine($"[DiscogsMetadataProvider] Récupération release: {url}")

            Dim response = Await httpClient.GetAsync(url)

            If Not response.IsSuccessStatusCode Then
                Return Nothing
            End If

            Dim json = Await response.Content.ReadAsStringAsync()
            Dim doc = JsonDocument.Parse(json)
            Dim root = doc.RootElement

            Dim cdInfo As New CDMetadataProvider.CDInfo()

            ' Extraire l'artiste
            If root.TryGetProperty("artists", Nothing) Then
                Dim artists = root.GetProperty("artists")
                If artists.GetArrayLength() > 0 Then
                    Dim artist = artists(0)
                    If artist.TryGetProperty("name", Nothing) Then
                        cdInfo.Artist = artist.GetProperty("name").GetString()
                    End If
                End If
            End If

            ' Extraire le titre de l'album
            If root.TryGetProperty("title", Nothing) Then
                cdInfo.Album = root.GetProperty("title").GetString()
            End If

            ' Extraire l'année
            If root.TryGetProperty("year", Nothing) Then
                Dim yearValue = root.GetProperty("year")
                If yearValue.ValueKind = JsonValueKind.Number Then
                    cdInfo.Year = yearValue.GetInt32()
                End If
            End If

            ' Extraire les pistes (tracklist)
            If root.TryGetProperty("tracklist", Nothing) Then
                Dim tracklist = root.GetProperty("tracklist")

                For i As Integer = 0 To tracklist.GetArrayLength() - 1
                    Dim track = tracklist(i)
                    Dim trackInfo As New CDMetadataProvider.TrackInfo With {
                        .TrackNumber = i + 1
                    }

                    ' Titre de la piste
                    If track.TryGetProperty("title", Nothing) Then
                        trackInfo.Title = track.GetProperty("title").GetString()
                    End If

                    ' Durée (format "MM:SS")
                    If track.TryGetProperty("duration", Nothing) Then
                        Dim durationStr = track.GetProperty("duration").GetString()
                        If Not String.IsNullOrEmpty(durationStr) Then
                            Dim parts = durationStr.Split(":"c)
                            If parts.Length = 2 Then
                                Dim minutes, seconds As Integer
                                If Integer.TryParse(parts(0), minutes) AndAlso Integer.TryParse(parts(1), seconds) Then
                                    trackInfo.Duration = New TimeSpan(0, minutes, seconds)
                                End If
                            End If
                        End If
                    End If

                    ' Artiste spécifique pour la piste
                    If track.TryGetProperty("artists", Nothing) Then
                        Dim trackArtists = track.GetProperty("artists")
                        If trackArtists.GetArrayLength() > 0 Then
                            Dim trackArtist = trackArtists(0)
                            If trackArtist.TryGetProperty("name", Nothing) Then
                                trackInfo.Artist = trackArtist.GetProperty("name").GetString()
                            End If
                        End If
                    End If

                    cdInfo.Tracks.Add(trackInfo)
                Next
            End If

            System.Diagnostics.Debug.WriteLine($"[DiscogsMetadataProvider] Métadonnées trouvées: {cdInfo.Artist} - {cdInfo.Album} ({cdInfo.Tracks.Count} pistes)")
            Return cdInfo

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[DiscogsMetadataProvider] Erreur détails: {ex.Message}")
            Return Nothing
        End Try
    End Function
End Class
