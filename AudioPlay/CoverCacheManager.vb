Imports System.IO

''' <summary>
''' Gestionnaire de cache pour les pochettes d'albums
''' </summary>
Public Class CoverCacheManager

    Private Const TAILLE_MAX_CACHE As Long = 500 * 1024 * 1024 ' 500 MB
    Private Shared ReadOnly CheminCache As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AudioPlay", "CoverCache")

    ''' <summary>
    ''' Obtient le chemin du dossier de cache
    ''' </summary>
    Public Shared Function ObtenirCheminCache() As String
        If Not Directory.Exists(CheminCache) Then
            Directory.CreateDirectory(CheminCache)
        End If
        Return CheminCache
    End Function

    ''' <summary>
    ''' Obtient le chemin du fichier cache pour une URL d'image
    ''' </summary>
    Public Shared Function ObtenirCheminFichier(url As String) As String
        Dim hash As String
        Using md5 = System.Security.Cryptography.MD5.Create()
            Dim hashBytes = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url))
            hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower()
        End Using

        Return Path.Combine(ObtenirCheminCache(), $"{hash}.jpg")
    End Function

    ''' <summary>
    ''' Calcule la taille totale du cache en octets
    ''' </summary>
    Public Shared Function ObtenirTailleCache() As Long
        Try
            If Not Directory.Exists(CheminCache) Then
                Return 0
            End If

            Dim totalSize As Long = 0
            Dim fichiers = Directory.GetFiles(CheminCache, "*.jpg")

            For Each fichier In fichiers
                Dim fileInfo As New FileInfo(fichier)
                totalSize += fileInfo.Length
            Next

            Return totalSize
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CoverCache] Erreur calcul taille: {ex.Message}")
            Return 0
        End Try
    End Function

    ''' <summary>
    ''' Obtient le nombre de fichiers dans le cache
    ''' </summary>
    Public Shared Function ObtenirNombreFichiers() As Integer
        Try
            If Not Directory.Exists(CheminCache) Then
                Return 0
            End If
            Return Directory.GetFiles(CheminCache, "*.jpg").Length
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CoverCache] Erreur comptage fichiers: {ex.Message}")
            Return 0
        End Try
    End Function

    ''' <summary>
    ''' Formate une taille en octets en format lisible (KB, MB, GB)
    ''' </summary>
    Public Shared Function FormaterTaille(octets As Long) As String
        If octets < 1024 Then
            Return $"{octets} octets"
        ElseIf octets < 1024 * 1024 Then
            Return $"{octets / 1024:F1} KB"
        ElseIf octets < 1024 * 1024 * 1024 Then
            Return $"{octets / (1024 * 1024):F1} MB"
        Else
            Return $"{octets / (1024 * 1024 * 1024):F1} GB"
        End If
    End Function

    ''' <summary>
    ''' Nettoie le cache en supprimant les fichiers les plus anciens jusqu'à atteindre la limite
    ''' </summary>
    Public Shared Sub NettoyerCacheAutomatique()
        Try
            Dim tailleActuelle = ObtenirTailleCache()
            If tailleActuelle <= TAILLE_MAX_CACHE Then
                System.Diagnostics.Debug.WriteLine($"[CoverCache] Taille OK: {FormaterTaille(tailleActuelle)} / {FormaterTaille(TAILLE_MAX_CACHE)}")
                Return
            End If

            System.Diagnostics.Debug.WriteLine($"[CoverCache] Nettoyage nécessaire: {FormaterTaille(tailleActuelle)} > {FormaterTaille(TAILLE_MAX_CACHE)}")

            ' Obtenir tous les fichiers triés par date d'accès (plus ancien en premier)
            Dim fichiers = Directory.GetFiles(CheminCache, "*.jpg") _
                .Select(Function(f) New FileInfo(f)) _
                .OrderBy(Function(f) f.LastAccessTime) _
                .ToList()

            Dim tailleSupprimer As Long = tailleActuelle - TAILLE_MAX_CACHE
            Dim tailleSupprimeTotal As Long = 0
            Dim fichiersSupprimes As Integer = 0

            For Each fichier In fichiers
                If tailleSupprimeTotal >= tailleSupprimer Then
                    Exit For
                End If

                tailleSupprimeTotal += fichier.Length
                fichier.Delete()
                fichiersSupprimes += 1
                System.Diagnostics.Debug.WriteLine($"[CoverCache] Supprimé: {fichier.Name} ({FormaterTaille(fichier.Length)})")
            Next

            System.Diagnostics.Debug.WriteLine($"[CoverCache] Nettoyage terminé: {fichiersSupprimes} fichiers supprimés, {FormaterTaille(tailleSupprimeTotal)} libérés")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CoverCache] Erreur nettoyage automatique: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Supprime tous les fichiers du cache
    ''' </summary>
    Public Shared Function ViderCache() As Boolean
        Try
            If Not Directory.Exists(CheminCache) Then
                Return True
            End If

            Dim fichiers = Directory.GetFiles(CheminCache, "*.jpg")
            For Each fichier In fichiers
                File.Delete(fichier)
            Next

            System.Diagnostics.Debug.WriteLine($"[CoverCache] Cache vidé: {fichiers.Length} fichiers supprimés")
            Return True

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CoverCache] Erreur vidage cache: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Sauvegarde une image dans le cache
    ''' </summary>
    Public Shared Sub SauvegarderImage(url As String, imageBytes As Byte())
        Try
            Dim cheminFichier = ObtenirCheminFichier(url)
            File.WriteAllBytes(cheminFichier, imageBytes)
            System.Diagnostics.Debug.WriteLine($"[CoverCache] Image sauvegardée: {Path.GetFileName(cheminFichier)} ({FormaterTaille(imageBytes.Length)})")

            ' Nettoyer le cache si nécessaire
            NettoyerCacheAutomatique()

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CoverCache] Erreur sauvegarde: {ex.Message}")
        End Try
    End Sub

End Class
