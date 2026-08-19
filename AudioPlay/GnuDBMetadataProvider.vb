Imports System.Net.Http
Imports System.Text
Imports System.Security.Cryptography

''' <summary>
''' Fournisseur de métadonnées CD via GnuDB (successeur de FreeDB)
''' Utilise le protocole CDDB classique
''' Documentation: http://gnudb.org
''' </summary>
Public Class GnuDBMetadataProvider

    Private Shared ReadOnly httpClient As New HttpClient()
    Private Const GNUDB_SERVER As String = "http://gnudb.gnudb.org"
    Private Const GNUDB_CGI_PATH As String = "/~cddb/cddb.cgi"

    Shared Sub New()
        httpClient.Timeout = TimeSpan.FromSeconds(10)
    End Sub

    ''' <summary>
    ''' Calcule le CDDB DiscID selon la spécification CDDB/freedb
    ''' Format: 8 caractères hexadécimaux (32-bit integer)
    ''' Formule: ((checksum % 0xFF) << 24) | (durée_totale << 8) | nombre_pistes
    ''' Différent du DiscID MusicBrainz !
    ''' </summary>
    Public Shared Function CalculerCDDBDiscID(pistes As List(Of CDAudioManager.CDTrack)) As String
        If pistes Is Nothing OrElse pistes.Count = 0 Then
            Return Nothing
        End If

        Try
            ' 1. Calculer le checksum CDDB (somme des chiffres des secondes de début de chaque piste)
            Dim checksum As Integer = 0
            For Each piste In pistes
                Dim seconds = CInt(piste.StartFrame / 75) ' Convertir frames en secondes
                checksum += SommeChiffres(seconds)
            Next

            ' 2. Durée totale du disque en secondes (lead-out position)
            Dim dernierePiste = pistes.Last()
            Dim dureeSecondes = CInt((dernierePiste.StartFrame + dernierePiste.Duration.TotalSeconds * 75) / 75)

            ' 3. Nombre de pistes
            Dim nombrePistes = pistes.Count

            ' 4. Construire le DiscID CDDB selon la formule standard:
            ' discid = ((checksum % 0xFF) << 24) | (durée << 8) | nombre_pistes
            Dim checksumByte As UInteger = CUInt(checksum Mod &HFF)
            Dim dureeMasked As UInteger = CUInt(dureeSecondes And &HFFFF)
            Dim pistesMasked As UInteger = CUInt(nombrePistes And &HFF)

            Dim discIDValue As UInteger = (checksumByte << 24) Or (dureeMasked << 8) Or pistesMasked

            ' 5. Convertir en chaîne hexadécimale 8 caractères
            Dim discID As String = discIDValue.ToString("x8")

            System.Diagnostics.Debug.WriteLine($"[GnuDB] Checksum: {checksum} (0x{checksumByte:x2}), Durée: {dureeSecondes}s, Pistes: {nombrePistes}")
            System.Diagnostics.Debug.WriteLine($"[GnuDB] DiscID calculé: {discID}")
            Return discID

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[GnuDB] Erreur calcul DiscID: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Somme des chiffres d'un nombre (pour le checksum CDDB)
    ''' </summary>
    Private Shared Function SommeChiffres(n As Integer) As Integer
        Dim somme As Integer = 0
        While n > 0
            somme += n Mod 10
            n \= 10
        End While
        Return somme
    End Function

    ''' <summary>
    ''' Recherche un CD sur GnuDB
    ''' </summary>
    Public Shared Async Function RechercherCD(pistes As List(Of CDAudioManager.CDTrack)) As Task(Of CDMetadataProvider.CDInfo)
        Try
            ' Calculer le DiscID CDDB
            Dim discID = CalculerCDDBDiscID(pistes)
            If String.IsNullOrEmpty(discID) Then
                System.Diagnostics.Debug.WriteLine("[GnuDB] DiscID invalide")
                Return Nothing
            End If

            ' Construire les offsets (en frames)
            Dim offsets As New StringBuilder()
            For Each piste In pistes
                offsets.Append($"{piste.StartFrame} ")
            Next

            ' Durée totale en secondes
            Dim dernierePiste = pistes.Last()
            Dim dureeSecondes = CInt((dernierePiste.StartFrame + dernierePiste.Duration.TotalSeconds * 75) / 75)

            ' Construire la requête CDDB query
            Dim query = $"cmd=cddb+query+{discID}+{pistes.Count}+{offsets.ToString().Trim()}+{dureeSecondes}" &
                       $"&hello=audioplay+localhost+AudioPlay+1.0&proto=6"

            Dim url = $"{GNUDB_SERVER}{GNUDB_CGI_PATH}?{query}"
            System.Diagnostics.Debug.WriteLine($"[GnuDB] Requête: {url}")

            Dim response = Await httpClient.GetAsync(url)
            If Not response.IsSuccessStatusCode Then
                System.Diagnostics.Debug.WriteLine($"[GnuDB] Erreur HTTP: {response.StatusCode}")
                Return Nothing
            End If

            Dim result = Await response.Content.ReadAsStringAsync()
            System.Diagnostics.Debug.WriteLine($"[GnuDB] Réponse query: {result}")

            ' Parser la réponse
            Dim lines = result.Split(New String() {vbLf, vbCrLf}, StringSplitOptions.RemoveEmptyEntries)
            If lines.Length = 0 Then
                Return Nothing
            End If

            ' Vérifier le code de statut
            Dim statusLine = lines(0)
            If statusLine.StartsWith("200") Then
                ' Match exact trouvé
                Dim parts = statusLine.Split(" "c)
                If parts.Length >= 3 Then
                    Dim category = parts(1)
                    Dim foundDiscID = parts(2)
                    Return Await RecupererDetailsCD(category, foundDiscID, pistes)
                End If
            ElseIf statusLine.StartsWith("211") OrElse statusLine.StartsWith("210") Then
                ' Multiples matches - prendre le premier
                If lines.Length >= 2 Then
                    Dim firstMatch = lines(1)
                    Dim parts = firstMatch.Split(" "c)
                    If parts.Length >= 2 Then
                        Dim category = parts(0)
                        Dim foundDiscID = parts(1)
                        Return Await RecupererDetailsCD(category, foundDiscID, pistes)
                    End If
                End If
            ElseIf statusLine.StartsWith("202") Then
                System.Diagnostics.Debug.WriteLine("[GnuDB] Aucune correspondance trouvée")
            End If

            Return Nothing

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[GnuDB] Erreur recherche: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Récupère les détails d'un CD depuis GnuDB
    ''' </summary>
    Private Shared Async Function RecupererDetailsCD(category As String, discID As String, pistesOriginales As List(Of CDAudioManager.CDTrack)) As Task(Of CDMetadataProvider.CDInfo)
        Try
            ' Construire la requête CDDB read
            Dim query = $"cmd=cddb+read+{category}+{discID}" &
                       $"&hello=audioplay+localhost+AudioPlay+1.0&proto=6"

            Dim url = $"{GNUDB_SERVER}{GNUDB_CGI_PATH}?{query}"
            System.Diagnostics.Debug.WriteLine($"[GnuDB] Requête détails: {url}")

            Dim response = Await httpClient.GetAsync(url)
            If Not response.IsSuccessStatusCode Then
                Return Nothing
            End If

            Dim result = Await response.Content.ReadAsStringAsync()
            System.Diagnostics.Debug.WriteLine($"[GnuDB] Réponse read: {result.Substring(0, Math.Min(500, result.Length))}...")

            ' Parser la réponse CDDB
            Return ParseCDDBResponse(result, pistesOriginales)

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[GnuDB] Erreur récupération détails: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Parse la réponse CDDB et extrait les métadonnées
    ''' </summary>
    Private Shared Function ParseCDDBResponse(cddbData As String, pistesOriginales As List(Of CDAudioManager.CDTrack)) As CDMetadataProvider.CDInfo
        Try
            Dim lines = cddbData.Split(New String() {vbLf, vbCrLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim cdInfo As New CDMetadataProvider.CDInfo()
            Dim trackTitles As New Dictionary(Of Integer, String)

            For Each line In lines
                ' Extraire l'URL de pochette depuis les commentaires
                If line.StartsWith("# Cover:") Then
                    cdInfo.CoverArtUrl = line.Substring(8).Trim()
                    System.Diagnostics.Debug.WriteLine($"[GnuDB] URL pochette extraite: {cdInfo.CoverArtUrl}")
                    Continue For
                End If

                If line.StartsWith("#") OrElse line.StartsWith("210") OrElse line.Trim() = "." Then
                    Continue For
                End If

                If line.StartsWith("DTITLE=") Then
                    ' Format: "Artiste / Album"
                    Dim title = line.Substring(7).Trim()
                    Dim parts = title.Split(New String() {" / "}, 2, StringSplitOptions.None)
                    If parts.Length = 2 Then
                        cdInfo.Artist = parts(0).Trim()
                        cdInfo.Album = parts(1).Trim()
                    Else
                        cdInfo.Album = title
                    End If

                ElseIf line.StartsWith("DYEAR=") Then
                    Dim yearStr = line.Substring(6).Trim()
                    Dim year As Integer
                    If Integer.TryParse(yearStr, year) Then
                        cdInfo.Year = year
                    End If

                ElseIf line.StartsWith("DGENRE=") Then
                    ' Genre CDDB
                    cdInfo.Genre = line.Substring(7).Trim()

                ElseIf line.StartsWith("TTITLE") Then
                    ' Format: TTITLEn=titre
                    Dim equalPos = line.IndexOf("="c)
                    If equalPos > 0 Then
                        Dim trackNumStr = line.Substring(6, equalPos - 6)
                        Dim trackNum As Integer
                        If Integer.TryParse(trackNumStr, trackNum) Then
                            Dim titre = line.Substring(equalPos + 1).Trim()

                            ' Gérer les titres multi-lignes
                            If trackTitles.ContainsKey(trackNum) Then
                                trackTitles(trackNum) &= titre
                            Else
                                trackTitles.Add(trackNum, titre)
                            End If
                        End If
                    End If
                End If
            Next

            ' Créer les TrackInfo
            For i As Integer = 0 To pistesOriginales.Count - 1
                Dim trackInfo As New CDMetadataProvider.TrackInfo With {
                    .TrackNumber = i + 1,
                    .Duration = pistesOriginales(i).Duration
                }

                If trackTitles.ContainsKey(i) Then
                    Dim fullTitle = trackTitles(i)

                    ' Certains titres CDDB ont le format "Artiste / Titre"
                    If fullTitle.Contains(" / ") Then
                        Dim parts = fullTitle.Split(New String() {" / "}, 2, StringSplitOptions.None)
                        trackInfo.Artist = parts(0).Trim()
                        trackInfo.Title = parts(1).Trim()
                    Else
                        trackInfo.Title = fullTitle
                    End If
                Else
                    Dim trackPrefix = LanguageManager.GetString("CDTrack_Prefix")
                    trackInfo.Title = $"{trackPrefix} {i + 1:D2}"
                End If

                cdInfo.Tracks.Add(trackInfo)
            Next

            If String.IsNullOrEmpty(cdInfo.Artist) AndAlso String.IsNullOrEmpty(cdInfo.Album) Then
                Return Nothing
            End If

            System.Diagnostics.Debug.WriteLine($"[GnuDB] Métadonnées parsées: {cdInfo.Artist} - {cdInfo.Album} ({cdInfo.Tracks.Count} pistes)")
            Return cdInfo

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[GnuDB] Erreur parsing: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Génère le contenu CDDB pour la soumission manuelle
    ''' </summary>
    Public Shared Function GenererContenuCDDB(discID As String, cdInfo As CDMetadataProvider.CDInfo, pistes As List(Of CDAudioManager.CDTrack)) As String
        Dim sb As New System.Text.StringBuilder()

        ' En-tête du fichier CDDB
        sb.AppendLine("# xmcd")
        sb.AppendLine("#")
        sb.AppendLine("# Track frame offsets:")
        For Each piste In pistes
            sb.AppendLine($"# {piste.StartFrame}")
        Next
        sb.AppendLine("#")
        sb.AppendLine($"# Disc length: {CInt(pistes.Sum(Function(p) p.Duration.TotalSeconds))} seconds")
        sb.AppendLine("#")
        sb.AppendLine($"# Submitted via: AudioPlay 1.0")
        sb.AppendLine("#")

        ' Informations du disque
        sb.AppendLine($"DISCID={discID}")
        sb.AppendLine($"DTITLE={cdInfo.Artist} / {cdInfo.Album}")

        If cdInfo.Year > 0 Then
            sb.AppendLine($"DYEAR={cdInfo.Year}")
        End If

        If Not String.IsNullOrWhiteSpace(cdInfo.Genre) Then
            sb.AppendLine($"DGENRE={cdInfo.Genre}")
        End If

        ' Titres des pistes
        For i As Integer = 0 To cdInfo.Tracks.Count - 1
            Dim track = cdInfo.Tracks(i)
            If Not String.IsNullOrWhiteSpace(track.Title) Then
                sb.AppendLine($"TTITLE{i}={track.Title}")
            Else
                sb.AppendLine($"TTITLE{i}=Track {i + 1:D2}")
            End If
        Next

        ' Commentaire optionnel
        sb.AppendLine("EXTD=")
        For i As Integer = 0 To cdInfo.Tracks.Count - 1
            sb.AppendLine($"EXTT{i}=")
        Next
        sb.AppendLine("PLAYORDER=")

        Return sb.ToString()
    End Function

    ''' <summary>
    ''' Soumet les métadonnées d'un CD à GnuDB via submit.cgi (format HTTP POST conforme)
    ''' Documentation: https://gnudb.org/
    ''' </summary>
    Public Shared Async Function SoumettreViaHTTP(discID As String, categorie As String, cdInfo As CDMetadataProvider.CDInfo, pistes As List(Of CDAudioManager.CDTrack), emailUtilisateur As String, Optional modeTest As Boolean = False) As Task(Of String)
        Try
            System.Diagnostics.Debug.WriteLine($"[GnuDB] Soumission HTTP à GnuDB - DiscID: {discID}, Mode: {If(modeTest, "test", "submit")}")

            ' Valider les données obligatoires
            If String.IsNullOrWhiteSpace(cdInfo.Artist) OrElse String.IsNullOrWhiteSpace(cdInfo.Album) Then
                Throw New Exception("Artiste et album sont obligatoires")
            End If

            If cdInfo.Tracks Is Nothing OrElse cdInfo.Tracks.Count = 0 Then
                Throw New Exception("Au moins une piste est requise")
            End If

            ' Valider l'email
            If String.IsNullOrWhiteSpace(emailUtilisateur) OrElse Not emailUtilisateur.Contains("@") Then
                Throw New Exception("Adresse email valide requise")
            End If

            ' Valider la catégorie (freedb categories)
            Dim categoriesValides = {"blues", "classical", "country", "data", "folk", "jazz", "misc", "newage", "reggae", "rock", "soundtrack"}
            If Not categoriesValides.Contains(categorie.ToLower()) Then
                categorie = "rock" ' Par défaut
            End If

            ' Générer le contenu CDDB (Entity-Body)
            Dim contenuCddb = GenererContenuCDDB(discID, cdInfo, pistes)

            ' Encoder en UTF-8 pour calculer la vraie longueur
            Dim contenuBytes = System.Text.Encoding.UTF8.GetBytes(contenuCddb)
            Dim contentLength = contenuBytes.Length

            System.Diagnostics.Debug.WriteLine($"[GnuDB] Contenu CDDB (body seul) length: {contentLength} bytes")

            ' Envoyer via HTTP POST
            Using client As New HttpClient()
                client.Timeout = TimeSpan.FromSeconds(30)

                ' URL de submission GnuDB
                Dim submitUrl = "http://gnudb.gnudb.org/~cddb/submit.cgi"

                ' Créer une requête HTTP POST
                Dim request As New HttpRequestMessage(HttpMethod.Post, submitUrl)

                ' Le body contient SEULEMENT le contenu CDDB (xmcd)
                request.Content = New ByteArrayContent(contenuBytes)
                request.Content.Headers.ContentType = New Headers.MediaTypeHeaderValue("text/plain")
                request.Content.Headers.ContentType.CharSet = "UTF-8"

                ' Les métadonnées de soumission doivent être dans les en-têtes HTTP
                request.Headers.Add("Category", categorie.ToLower())
                request.Headers.Add("Discid", discID)
                request.Headers.Add("User-Email", emailUtilisateur)
                request.Headers.Add("Submit-Mode", If(modeTest, "test", "submit"))
                request.Headers.Add("Charset", "UTF-8")
                request.Headers.Add("X-Cddbd-Note", "Sent by AudioPlay 1.0")

                System.Diagnostics.Debug.WriteLine($"[GnuDB] POST vers: {submitUrl}")
                System.Diagnostics.Debug.WriteLine($"[GnuDB] Headers: Category={categorie}, Discid={discID}, User-Email={emailUtilisateur}, Submit-Mode={If(modeTest, "test", "submit")}")
                System.Diagnostics.Debug.WriteLine($"[GnuDB] Body CDDB: {contentLength} bytes")

                Dim response = Await client.SendAsync(request)
                Dim result = Await response.Content.ReadAsStringAsync()

                System.Diagnostics.Debug.WriteLine($"[GnuDB] Status HTTP: {response.StatusCode}")
                System.Diagnostics.Debug.WriteLine($"[GnuDB] Réponse: {result}")

                ' Analyser la réponse selon la spec GnuDB
                ' 200 OK, submission has been sent.
                ' 500 Missing required header information.
                ' 500 Internal Server Error: [description].
                ' 501 Invalid header information [details].

                If result.StartsWith("200") Then
                    Return "✓ Soumission réussie ! Votre contribution a été envoyée à GnuDB." & vbCrLf & vbCrLf & result
                ElseIf result.StartsWith("500") Then
                    Throw New Exception($"Erreur serveur (500): {result}")
                ElseIf result.StartsWith("501") Then
                    Throw New Exception($"Erreur de format (501): {result}")
                Else
                    ' Autre réponse
                    If response.IsSuccessStatusCode Then
                        Return $"Réponse du serveur:{vbCrLf}{result}"
                    Else
                        Throw New Exception($"Erreur HTTP {response.StatusCode}: {result}")
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[GnuDB] Erreur SoumettreViaHTTP: {ex.Message}")
            Throw
        End Try
    End Function

End Class
