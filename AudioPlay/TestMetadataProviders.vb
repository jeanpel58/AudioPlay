Imports System.Threading.Tasks

''' <summary>
''' Module de test pour diagnostiquer les problèmes de métadonnées CD
''' </summary>
Public Module TestMetadataProviders

    ''' <summary>
    ''' Teste tous les providers avec des logs détaillés
    ''' </summary>
    Public Async Function TesterTousLesProviders(pistes As List(Of CDAudioManager.CDTrack)) As Task(Of String)
        Dim rapport As New Text.StringBuilder()
        rapport.AppendLine("=== TEST DES PROVIDERS DE MÉTADONNÉES ===")
        rapport.AppendLine($"Nombre de pistes: {pistes.Count}")
        rapport.AppendLine()

        ' 1. Test DiscID MusicBrainz
        Try
            rapport.AppendLine("--- MusicBrainz DiscID ---")
            Dim mbDiscID = CDMetadataProvider.CalculerDiscID(pistes)
            rapport.AppendLine($"✓ DiscID calculé: {mbDiscID}")

            ' Afficher les offsets
            rapport.AppendLine("Offsets (frames):")
            For Each piste In pistes
                rapport.AppendLine($"  Piste {piste.TrackNumber}: {piste.StartFrame}")
            Next
            rapport.AppendLine()

        Catch ex As Exception
            rapport.AppendLine($"❌ Erreur calcul DiscID MusicBrainz: {ex.Message}")
            rapport.AppendLine($"   Stack: {ex.StackTrace}")
            rapport.AppendLine()
        End Try

        ' 2. Test DiscID CDDB/GnuDB
        Try
            rapport.AppendLine("--- GnuDB/CDDB DiscID ---")
            Dim cddbDiscID = GnuDBMetadataProvider.CalculerCDDBDiscID(pistes)
            rapport.AppendLine($"✓ DiscID CDDB calculé: {cddbDiscID}")
            rapport.AppendLine()

        Catch ex As Exception
            rapport.AppendLine($"❌ Erreur calcul DiscID CDDB: {ex.Message}")
            rapport.AppendLine($"   Stack: {ex.StackTrace}")
            rapport.AppendLine()
        End Try

        ' 3. Test requête MusicBrainz
        Try
            rapport.AppendLine("--- Requête MusicBrainz ---")
            Dim mbDiscID = CDMetadataProvider.CalculerDiscID(pistes)
            rapport.AppendLine($"URL: https://musicbrainz.org/ws/2/discid/{mbDiscID}?fmt=json&inc=artist-credits+recordings")

            Dim mbResult = Await CDMetadataProvider.ObtenirMetadonnees(pistes)

            If mbResult IsNot Nothing Then
                rapport.AppendLine($"✓ Trouvé: {mbResult.Artist} - {mbResult.Album}")
                rapport.AppendLine($"  Année: {mbResult.Year}")
                rapport.AppendLine($"  Nombre de pistes: {mbResult.Tracks.Count}")
            Else
                rapport.AppendLine("❌ Aucun résultat MusicBrainz")
            End If
            rapport.AppendLine()

        Catch ex As Exception
            rapport.AppendLine($"❌ Erreur requête MusicBrainz: {ex.Message}")
            rapport.AppendLine($"   Stack: {ex.StackTrace}")
            rapport.AppendLine()
        End Try

        ' 4. Test requête GnuDB
        Try
            rapport.AppendLine("--- Requête GnuDB ---")
            Dim cddbDiscID = GnuDBMetadataProvider.CalculerCDDBDiscID(pistes)

            ' Construire l'URL pour affichage
            Dim offsets As New Text.StringBuilder()
            For Each piste In pistes
                offsets.Append($"{piste.StartFrame} ")
            Next
            Dim dernierePiste = pistes.Last()
            Dim dureeSecondes = CInt((dernierePiste.StartFrame + dernierePiste.Duration.TotalSeconds * 75) / 75)

            rapport.AppendLine($"URL: http://gnudb.gnudb.org/~cddb/cddb.cgi?cmd=cddb+query+{cddbDiscID}+{pistes.Count}+{offsets.ToString().Trim()}+{dureeSecondes}&hello=audioplay+localhost+AudioPlay+1.0&proto=6")

            Dim gnudbResult = Await GnuDBMetadataProvider.RechercherCD(pistes)

            If gnudbResult IsNot Nothing Then
                rapport.AppendLine($"✓ Trouvé: {gnudbResult.Artist} - {gnudbResult.Album}")
                rapport.AppendLine($"  Année: {gnudbResult.Year}")
                rapport.AppendLine($"  Nombre de pistes: {gnudbResult.Tracks.Count}")
            Else
                rapport.AppendLine("❌ Aucun résultat GnuDB")
            End If
            rapport.AppendLine()

        Catch ex As Exception
            rapport.AppendLine($"❌ Erreur requête GnuDB: {ex.Message}")
            rapport.AppendLine($"   Type: {ex.GetType().FullName}")
            If ex.InnerException IsNot Nothing Then
                rapport.AppendLine($"   Inner: {ex.InnerException.Message}")
            End If
            rapport.AppendLine($"   Stack: {ex.StackTrace}")
            rapport.AppendLine()
        End Try

        ' 5. Test initialisation Discogs
        Try
            rapport.AppendLine("--- Test Discogs ---")
            ' Juste tester l'initialisation
            Dim testArtist = "The Beatles"
            Dim testAlbum = "Abbey Road"
            rapport.AppendLine($"  Tentative avec: {testArtist} - {testAlbum}")

            Dim discogsResult = Await DiscogsMetadataProvider.RechercherCD(testArtist, testAlbum)

            If discogsResult IsNot Nothing Then
                rapport.AppendLine($"✓ Trouvé: {discogsResult.Artist} - {discogsResult.Album}")
            Else
                rapport.AppendLine("❌ Aucun résultat Discogs (ou clé API manquante)")
            End If
            rapport.AppendLine()

        Catch ex As Exception
            rapport.AppendLine($"❌ Erreur Discogs: {ex.Message}")
            rapport.AppendLine($"   Type: {ex.GetType().FullName}")
            If ex.InnerException IsNot Nothing Then
                rapport.AppendLine($"   Inner: {ex.InnerException.Message}")
            End If
            rapport.AppendLine($"   Stack: {ex.StackTrace}")
            rapport.AppendLine()
        End Try

        rapport.AppendLine("=== FIN DU TEST ===")
        Return rapport.ToString()
    End Function

    ''' <summary>
    ''' Teste un provider spécifique avec logs détaillés
    ''' </summary>
    Public Async Function TesterProvider(nomProvider As String, pistes As List(Of CDAudioManager.CDTrack)) As Task(Of String)
        Dim rapport As New Text.StringBuilder()
        rapport.AppendLine($"=== TEST {nomProvider.ToUpper()} ===")
        rapport.AppendLine($"Nombre de pistes: {pistes.Count}")
        rapport.AppendLine()

        Try
            Select Case nomProvider.ToLower()
                Case "musicbrainz"
                    Dim result = Await CDMetadataProvider.ObtenirMetadonnees(pistes)
                    If result IsNot Nothing Then
                        rapport.AppendLine($"✓ {result.Artist} - {result.Album} ({result.Year})")
                        For Each track In result.Tracks
                            rapport.AppendLine($"  {track.TrackNumber}. {track.Title} - {track.Artist}")
                        Next
                    Else
                        rapport.AppendLine("❌ Aucun résultat")
                    End If

                Case "gnudb"
                    Dim result = Await GnuDBMetadataProvider.RechercherCD(pistes)
                    If result IsNot Nothing Then
                        rapport.AppendLine($"✓ {result.Artist} - {result.Album} ({result.Year})")
                        For Each track In result.Tracks
                            rapport.AppendLine($"  {track.TrackNumber}. {track.Title} - {track.Artist}")
                        Next
                    Else
                        rapport.AppendLine("❌ Aucun résultat")
                    End If

                Case Else
                    rapport.AppendLine($"❌ Provider inconnu: {nomProvider}")
            End Select

        Catch ex As Exception
            rapport.AppendLine($"❌ ERREUR: {ex.Message}")
            rapport.AppendLine($"Type: {ex.GetType().FullName}")
            If ex.InnerException IsNot Nothing Then
                rapport.AppendLine($"Inner: {ex.InnerException.Message}")
            End If
            rapport.AppendLine()
            rapport.AppendLine("Stack trace:")
            rapport.AppendLine(ex.StackTrace)
        End Try

        Return rapport.ToString()
    End Function

End Module
