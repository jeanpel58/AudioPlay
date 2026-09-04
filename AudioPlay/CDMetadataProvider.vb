Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json

''' <summary>
''' Fournisseur de métadonnées CD via MusicBrainz
''' </summary>
Public Class CDMetadataProvider

    Private Shared ReadOnly httpClient As New HttpClient()

    Shared Sub New()
        ' MusicBrainz demande un User-Agent personnalisé
        httpClient.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0 (https://github.com/jeanpel58/AudioPlay)")
    End Sub

    ''' <summary>
    ''' Informations sur un CD
    ''' </summary>
    Public Class CDInfo
        Public Property Artist As String
        Public Property Album As String
        Public Property Year As Integer
        Public Property Genre As String
        Public Property Tracks As New List(Of TrackInfo)
        Public Property CoverArtUrl As String ' URL de la pochette de l'album
    End Class

    ''' <summary>
    ''' Informations sur une piste
    ''' </summary>
    Public Class TrackInfo
        Public Property TrackNumber As Integer
        Public Property Title As String
        Public Property Artist As String ' Artiste spécifique si différent de l'album
        Public Property Duration As TimeSpan
    End Class

    ''' <summary>
    ''' Calcule le DiscID MusicBrainz à partir de la TOC
    ''' Spécification: https://musicbrainz.org/doc/Disc_ID_Calculation
    ''' </summary>
    Public Shared Function CalculerDiscID(pistes As List(Of CDAudioManager.CDTrack)) As String
        If pistes Is Nothing OrElse pistes.Count = 0 Then
            Return Nothing
        End If

        Try
            Dim firstTrack As Integer = pistes(0).TrackNumber
            Dim lastTrack As Integer = pistes(pistes.Count - 1).TrackNumber
            Dim leadOutOffset As Integer = pistes(pistes.Count - 1).EndFrame

            ' Construire la chaîne pour le SHA-1 selon la spec MusicBrainz
            ' Format: "{first} {last} {leadout} {offset1} {offset2} ... {offset99}"
            ' où les offsets sont paddés à 99 entrées avec des zéros
            Dim sb As New StringBuilder()
            sb.AppendFormat("{0:X2}", firstTrack)
            sb.AppendFormat("{0:X2}", lastTrack)
            sb.AppendFormat("{0:X8}", leadOutOffset)

            ' Ajouter les offsets de chaque piste
            For i As Integer = 0 To 98 ' 99 pistes max
                If i < pistes.Count Then
                    sb.AppendFormat("{0:X8}", pistes(i).StartFrame)
                Else
                    sb.Append("00000000")
                End If
            Next

            System.Diagnostics.Debug.WriteLine($"[CDMetadataProvider] Chaîne DiscID: {sb.ToString().Substring(0, Math.Min(100, sb.ToString().Length))}")

            ' Calculer le SHA-1 et encoder en base64 URL-safe
            Using sha1 As SHA1 = SHA1.Create()
                Dim hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(sb.ToString()))
                Dim discId = Convert.ToBase64String(hash).Replace("+", ".").Replace("/", "_").Replace("=", "-")
                Return discId
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDMetadataProvider] Erreur calcul DiscID: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Récupère les métadonnées du CD depuis MusicBrainz
    ''' </summary>
    Public Shared Async Function ObtenirMetadonnees(pistes As List(Of CDAudioManager.CDTrack)) As Task(Of CDInfo)
        Try
            Dim discId = CalculerDiscID(pistes)
            If String.IsNullOrEmpty(discId) Then
                Return Nothing
            End If

            System.Diagnostics.Debug.WriteLine($"[CDMetadataProvider] DiscID calculé: {discId}")

            ' Construire l'URL MusicBrainz
            ' API: https://musicbrainz.org/ws/2/discid/{discid}?fmt=json&inc=artist-credits+recordings
            Dim url As String = $"https://musicbrainz.org/ws/2/discid/{discId}?fmt=json&inc=artist-credits+recordings"

            System.Diagnostics.Debug.WriteLine($"[CDMetadataProvider] Requête MusicBrainz: {url}")

            ' Faire la requête HTTP
            Dim response = Await httpClient.GetAsync(url)

            System.Diagnostics.Debug.WriteLine($"[CDMetadataProvider] Status HTTP: {response.StatusCode}")

            If Not response.IsSuccessStatusCode Then
                Dim errorContent = Await response.Content.ReadAsStringAsync()
                System.Diagnostics.Debug.WriteLine($"[CDMetadataProvider] Erreur HTTP {response.StatusCode}: {errorContent}")

                ' Lancer une exception pour propager l'erreur au lieu de retourner Nothing
                Throw New HttpRequestException($"MusicBrainz HTTP {CInt(response.StatusCode)}: {response.ReasonPhrase}")
            End If

            Dim json = Await response.Content.ReadAsStringAsync()
            System.Diagnostics.Debug.WriteLine($"[CDMetadataProvider] Réponse reçue: {json.Substring(0, Math.Min(200, json.Length))}...")

            ' Parser le JSON (simpliste, devrait utiliser un vrai parser JSON)
            Dim cdInfo = ParseMusicBrainzResponse(json, pistes)
            Return cdInfo

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDMetadataProvider] Erreur: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Parse la réponse JSON de MusicBrainz (version simplifiée)
    ''' </summary>
    Private Shared Function ParseMusicBrainzResponse(json As String, pistesOriginales As List(Of CDAudioManager.CDTrack)) As CDInfo
        Try
            ' Utiliser System.Text.Json pour parser
            Dim doc = JsonDocument.Parse(json)
            Dim root = doc.RootElement

            ' Vérifier s'il y a des releases
            If Not root.TryGetProperty("releases", Nothing) Then
                Return Nothing
            End If

            Dim releases = root.GetProperty("releases")
            If releases.GetArrayLength() = 0 Then
                Return Nothing
            End If

            ' Prendre le premier release (le plus pertinent)
            Dim release = releases(0)

            Dim cdInfo As New CDInfo()

            ' Extraire le Release ID pour la pochette
            Dim releaseId As String = Nothing
            If release.TryGetProperty("id", Nothing) Then
                releaseId = release.GetProperty("id").GetString()
                ' Construire l'URL de la pochette depuis Cover Art Archive
                If Not String.IsNullOrEmpty(releaseId) Then
                    cdInfo.CoverArtUrl = $"https://coverartarchive.org/release/{releaseId}/front-500"
                End If
            End If

            ' Extraire l'artiste
            If release.TryGetProperty("artist-credit", Nothing) Then
                Dim artistCredit = release.GetProperty("artist-credit")
                If artistCredit.GetArrayLength() > 0 Then
                    Dim artist = artistCredit(0)
                    If artist.TryGetProperty("name", Nothing) Then
                        cdInfo.Artist = artist.GetProperty("name").GetString()
                    End If
                End If
            End If

            ' Extraire l'album
            If release.TryGetProperty("title", Nothing) Then
                cdInfo.Album = release.GetProperty("title").GetString()
            End If

            ' Extraire l'année
            If release.TryGetProperty("date", Nothing) Then
                Dim dateStr = release.GetProperty("date").GetString()
                If Not String.IsNullOrEmpty(dateStr) AndAlso dateStr.Length >= 4 Then
                    Integer.TryParse(dateStr.Substring(0, 4), cdInfo.Year)
                End If
            End If

            ' Extraire le genre (si disponible dans release-group)
            If release.TryGetProperty("release-group", Nothing) Then
                Dim releaseGroup = release.GetProperty("release-group")
                If releaseGroup.TryGetProperty("primary-type", Nothing) Then
                    Dim primaryType = releaseGroup.GetProperty("primary-type").GetString()
                    ' Mapper certains types MusicBrainz vers des genres standards
                    Select Case primaryType
                        Case "Album"
                            cdInfo.Genre = "Pop" ' Par défaut pour les albums
                        Case "Single"
                            cdInfo.Genre = "Pop"
                        Case "EP"
                            cdInfo.Genre = "Pop"
                        Case Else
                            cdInfo.Genre = primaryType
                    End Select
                End If
            End If

            ' Extraire les pistes
            If release.TryGetProperty("media", Nothing) Then
                Dim media = release.GetProperty("media")
                If media.GetArrayLength() > 0 Then
                    Dim medium = media(0)
                    If medium.TryGetProperty("tracks", Nothing) Then
                        Dim tracks = medium.GetProperty("tracks")

                        For i As Integer = 0 To Math.Min(tracks.GetArrayLength() - 1, pistesOriginales.Count - 1)
                            Dim track = tracks(i)
                            Dim trackInfo As New TrackInfo With {
                                .TrackNumber = i + 1,
                                .Duration = pistesOriginales(i).Duration
                            }

                            ' Titre de la piste
                            If track.TryGetProperty("title", Nothing) Then
                                trackInfo.Title = track.GetProperty("title").GetString()
                            End If

                            ' Artiste spécifique si différent
                            If track.TryGetProperty("artist-credit", Nothing) Then
                                Dim trackArtistCredit = track.GetProperty("artist-credit")
                                If trackArtistCredit.GetArrayLength() > 0 Then
                                    Dim trackArtist = trackArtistCredit(0)
                                    If trackArtist.TryGetProperty("name", Nothing) Then
                                        trackInfo.Artist = trackArtist.GetProperty("name").GetString()
                                    End If
                                End If
                            End If

                            cdInfo.Tracks.Add(trackInfo)
                        Next
                    End If
                End If
            End If

            System.Diagnostics.Debug.WriteLine($"[CDMetadataProvider] Métadonnées trouvées: {cdInfo.Artist} - {cdInfo.Album} ({cdInfo.Tracks.Count} pistes)")
            Return cdInfo

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDMetadataProvider] Erreur parsing JSON: {ex.Message}")
            Return Nothing
        End Try
    End Function
End Class
