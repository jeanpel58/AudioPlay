Imports System.Collections.Generic
Imports System.Globalization
Imports System.IO

''' <summary>
''' Gère la lecture et l'écriture des BPM dans les métadonnées des fichiers audio
''' </summary>
Public Class BPMMetadataManager

    Private Const BPMFloatKey As String = "BPM_FLOAT"

    Private Shared Function GetBPMCachePath() As String
        Return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "bpm_precis.txt")
    End Function

    Private Shared Function LoadBPMCache() As Dictionary(Of String, Double)
        Dim result As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
        Try
            Dim cachePath = GetBPMCachePath()
            If Not File.Exists(cachePath) Then Return result

            For Each line In File.ReadAllLines(cachePath)
                If String.IsNullOrWhiteSpace(line) Then Continue For
                Dim parts = line.Split("|"c)
                If parts.Length < 2 Then Continue For

                Dim bpmValue As Double
                If Double.TryParse(parts(1), NumberStyles.Float, CultureInfo.InvariantCulture, bpmValue) AndAlso bpmValue > 0 Then
                    result(parts(0)) = bpmValue
                End If
            Next
        Catch
            ' Ignorer les erreurs de cache
        End Try

        Return result
    End Function

    Private Shared Sub SaveBPMCache(cache As Dictionary(Of String, Double))
        Try
            Dim cachePath = GetBPMCachePath()
            Dim folder = Path.GetDirectoryName(cachePath)
            If Not Directory.Exists(folder) Then Directory.CreateDirectory(folder)

            Dim lines As New List(Of String)
            For Each kvp In cache
                lines.Add($"{kvp.Key}|{kvp.Value.ToString("F2", CultureInfo.InvariantCulture)}")
            Next

            File.WriteAllLines(cachePath, lines)
        Catch
            ' Ignorer les erreurs de cache
        End Try
    End Sub

    Private Shared Sub SavePreciseBPMToCache(filePath As String, bpm As Double)
        If String.IsNullOrWhiteSpace(filePath) OrElse bpm <= 0 Then Return
        Dim cache = LoadBPMCache()
        cache(filePath) = bpm
        SaveBPMCache(cache)
    End Sub

    Private Shared Function ReadPreciseBPMFromCache(filePath As String) As Double
        If String.IsNullOrWhiteSpace(filePath) Then Return 0
        Dim cache = LoadBPMCache()
        If cache.ContainsKey(filePath) Then Return cache(filePath)
        Return 0
    End Function

    ''' <summary>
    ''' Lit le BPM précis (avec décimales si disponible) depuis les métadonnées d'un fichier audio.
    ''' </summary>
    Public Shared Function LireBPMPrecisDepuisMetadonnees(cheminFichier As String) As Double
        Try
            If Not File.Exists(cheminFichier) Then
                Return 0
            End If

            Using fichier = TagLib.File.Create(cheminFichier)
                Dim bpmPrecis = LireBPMFloatPersonnalise(fichier)
                If bpmPrecis > 0 Then
                    SavePreciseBPMToCache(cheminFichier, bpmPrecis)
                    Return bpmPrecis
                End If

                Dim bpmCache = ReadPreciseBPMFromCache(cheminFichier)
                If bpmCache > 0 Then
                    Return bpmCache
                End If

                If fichier.Tag.BeatsPerMinute > 0 Then
                    Return CDbl(fichier.Tag.BeatsPerMinute)
                End If

                Return 0
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lecture BPM précis métadonnées {cheminFichier}: {ex.Message}")
            Return 0
        End Try
    End Function

    ''' <summary>
    ''' Lit le BPM (historique entier) depuis les métadonnées d'un fichier audio.
    ''' </summary>
    Public Shared Function LireBPMDepuisMetadonnees(cheminFichier As String) As Integer
        Return CInt(Math.Round(LireBPMPrecisDepuisMetadonnees(cheminFichier)))
    End Function

    Private Shared Function LireBPMFloatPersonnalise(fichier As TagLib.File) As Double
        Try
            If TypeOf fichier Is TagLib.Mpeg.File Then
                Dim id3v2Tag = TryCast(DirectCast(fichier, TagLib.Mpeg.File).GetTag(TagLib.TagTypes.Id3v2, False), TagLib.Id3v2.Tag)
                If id3v2Tag IsNot Nothing Then
                    Dim frame = TagLib.Id3v2.UserTextInformationFrame.Get(id3v2Tag, BPMFloatKey, False)
                    If frame IsNot Nothing AndAlso frame.Text IsNot Nothing AndAlso frame.Text.Length > 0 Then
                        Dim bpmFloat As Double
                        If Double.TryParse(frame.Text(0), NumberStyles.Float, CultureInfo.InvariantCulture, bpmFloat) Then
                            Return bpmFloat
                        End If
                    End If
                End If
            End If

            Dim xiphTag = TryCast(fichier.GetTag(TagLib.TagTypes.Xiph, False), TagLib.Ogg.XiphComment)
            If xiphTag IsNot Nothing Then
                Dim value = xiphTag.GetFirstField(BPMFloatKey)
                Dim bpmFloat As Double
                If Not String.IsNullOrWhiteSpace(value) AndAlso Double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, bpmFloat) Then
                    Return bpmFloat
                End If
            End If

            Return 0
        Catch
            Return 0
        End Try
    End Function

    Private Shared Sub EcrireBPMFloatPersonnalise(fichier As TagLib.File, bpm As Double)
        Dim bpmTexte As String = bpm.ToString("F2", CultureInfo.InvariantCulture)

        If TypeOf fichier Is TagLib.Mpeg.File Then
            Dim id3v2Tag = TryCast(DirectCast(fichier, TagLib.Mpeg.File).GetTag(TagLib.TagTypes.Id3v2, True), TagLib.Id3v2.Tag)
            If id3v2Tag IsNot Nothing Then
                Dim frame = TagLib.Id3v2.UserTextInformationFrame.Get(id3v2Tag, BPMFloatKey, True)
                frame.Text = New String() {bpmTexte}
            End If
        End If

        Dim xiphTag = TryCast(fichier.GetTag(TagLib.TagTypes.Xiph, True), TagLib.Ogg.XiphComment)
        If xiphTag IsNot Nothing Then
            xiphTag.SetField(BPMFloatKey, New String() {bpmTexte})
        End If
    End Sub

    ''' <summary>
    ''' Écrit le BPM précis dans les métadonnées d'un fichier audio.
    ''' </summary>
    Public Shared Function EcrireBPMDansMetadonnees(cheminFichier As String, bpm As Double, ByRef erreurMessage As String) As Boolean
        Try
            If Not File.Exists(cheminFichier) Then
                erreurMessage = "Le fichier n'existe pas."
                Return False
            End If

            If bpm <= 0 Then
                erreurMessage = "Le BPM doit être supérieur à 0."
                Return False
            End If

            Using fichier = TagLib.File.Create(cheminFichier)
                ' BPM standard pour compatibilité (entier)
                fichier.Tag.BeatsPerMinute = CUInt(Math.Round(bpm))

                ' BPM précis personnalisé quand possible
                EcrireBPMFloatPersonnalise(fichier, bpm)

                fichier.Save()
                SavePreciseBPMToCache(cheminFichier, bpm)
                erreurMessage = ""
                Return True
            End Using

        Catch ex As UnauthorizedAccessException
            erreurMessage = "Accès refusé. Le fichier est peut-être en lecture seule ou utilisé par une autre application."
            System.Diagnostics.Debug.WriteLine($"Erreur accès fichier {cheminFichier}: {ex.Message}")
            Return False

        Catch ex As IOException
            erreurMessage = "Le fichier est utilisé par un autre processus."
            System.Diagnostics.Debug.WriteLine($"Erreur IO fichier {cheminFichier}: {ex.Message}")
            Return False

        Catch ex As Exception
            erreurMessage = $"Erreur : {ex.Message}"
            System.Diagnostics.Debug.WriteLine($"Erreur écriture BPM métadonnées {cheminFichier}: {ex.Message}")
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Vérifie si un fichier a déjà un BPM dans ses métadonnées
    ''' </summary>
    Public Shared Function FichierPossedeBPM(cheminFichier As String) As Boolean
        Return LireBPMPrecisDepuisMetadonnees(cheminFichier) > 0
    End Function

    ''' <summary>
    ''' Tente d'écrire le BPM avec gestion intelligente du fichier en cours de lecture
    ''' </summary>
    Public Shared Function EcrireBPMAvecGestionLecture(cheminFichier As String, bpm As Double, form1 As Form1, ByRef erreurMessage As String) As Boolean
        Try
            Dim fichierEnLecture As Boolean = (form1.ObtenirCheminFichierEnCours() = cheminFichier AndAlso form1.EstEnLecture())

            If fichierEnLecture Then
                Dim positionActuelle = form1.ObtenirPositionLecture()
                Dim etaitEnPause = form1.ObtenirEtatPause()

                form1.ArreterLecturePublic()
                System.Threading.Thread.Sleep(500)

                Dim success = EcrireBPMDansMetadonnees(cheminFichier, bpm, erreurMessage)

                If success Then
                    form1.JouerFichierAPosition(cheminFichier, positionActuelle)

                    If etaitEnPause Then
                        System.Threading.Thread.Sleep(200)
                        form1.BasculerPauseReprisePublic()
                    End If
                End If

                Return success
            Else
                Return EcrireBPMDansMetadonnees(cheminFichier, bpm, erreurMessage)
            End If

        Catch ex As Exception
            erreurMessage = $"Erreur lors de la sauvegarde : {ex.Message}"
            System.Diagnostics.Debug.WriteLine($"Erreur EcrireBPMAvecGestionLecture : {ex.Message}")
            Return False
        End Try
    End Function

End Class
