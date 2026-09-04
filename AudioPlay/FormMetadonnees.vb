Public Class FormMetadonnees
    ' API Windows pour enlever le bouton X
    Private Const SC_CLOSE As Integer = &HF060
    Private Const MF_BYCOMMAND As Integer = &H0

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetSystemMenu(hWnd As IntPtr, bRevert As Boolean) As IntPtr
    End Function

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function RemoveMenu(hMenu As IntPtr, uPosition As UInteger, uFlags As UInteger) As Boolean
    End Function

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Dim hMenu As IntPtr = GetSystemMenu(Me.Handle, False)
        RemoveMenu(hMenu, SC_CLOSE, MF_BYCOMMAND)
    End Sub

    Private cheminFichierAudio As String = ""
    Private formParent As Form1 = Nothing
    Private itemCourant As ListViewItem = Nothing

    ' Fonction pour vérifier si un fichier peut être ouvert en lecture/écriture exclusive
    ' Teste exactement le même accès que TagLib utilisera
    Private Function EstFichierAccessible(cheminFichier As String) As Boolean
        Dim fs As IO.FileStream = Nothing
        Try
            ' Essayer d'ouvrir en lecture/écriture exclusive (comme TagLib le fera)
            fs = New IO.FileStream(cheminFichier, IO.FileMode.Open, IO.FileAccess.ReadWrite, IO.FileShare.None)
            ' Si on arrive ici, le fichier est accessible
            Return True
        Catch ex As IO.IOException
            ' Le fichier est verrouillé par un autre processus
            Return False
        Catch ex As UnauthorizedAccessException
            ' Pas les permissions
            Return False
        Catch ex As Exception
            ' Autre erreur
            Return False
        Finally
            ' S'assurer de fermer le stream si on a réussi à l'ouvrir
            If fs IsNot Nothing Then
                Try
                    fs.Close()
                    fs.Dispose()
                Catch
                    ' Ignorer les erreurs de fermeture
                End Try
            End If
        End Try
    End Function

    Private Sub FormMetadonnees_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ThemeManager.ApplyThemeToForm(Me)
        RefreshLanguage()

        ' Configuration du formulaire au chargement
        If itemCourant IsNot Nothing AndAlso itemCourant.Tag IsNot Nothing Then
            ' Extraire le chemin depuis le Tag (qui peut être un Dictionary ou un String)
            Dim cheminFichier As String = ""

            If TypeOf itemCourant.Tag Is Dictionary(Of String, Object) Then
                ' Le Tag est un Dictionary avec la clé "Chemin"
                Dim tagDict = DirectCast(itemCourant.Tag, Dictionary(Of String, Object))
                If tagDict.ContainsKey("Chemin") Then
                    cheminFichier = tagDict("Chemin")?.ToString()
                End If
            ElseIf TypeOf itemCourant.Tag Is String Then
                ' Le Tag est directement le chemin (ancien format)
                cheminFichier = itemCourant.Tag.ToString()
            End If

            If Not String.IsNullOrEmpty(cheminFichier) Then
                AfficherMetadonnees(cheminFichier, TryCast(Me.Owner, Form1))
            End If
        End If
    End Sub

    ' Méthode pour définir l'item à afficher
    Public Sub DefinirItem(item As ListViewItem)
        itemCourant = item
    End Sub

    Private Sub RefreshLanguage()
        Me.Text = LanguageManager.GetString("Meta_Form_Title")
        GroupBoxTags.Text = LanguageManager.GetString("Meta_Group_Tags")
        LabelTitre.Text = LanguageManager.GetString("Meta_Label_Title")
        LabelArtiste.Text = LanguageManager.GetString("Meta_Label_Artist")
        LabelAlbum.Text = LanguageManager.GetString("Meta_Label_Album")
        LabelAnnee.Text = LanguageManager.GetString("Meta_Label_Year")
        LabelGenre.Text = LanguageManager.GetString("Meta_Label_Genre")
        LabelCommentaire.Text = LanguageManager.GetString("Meta_Label_Comment")
        LabelPiste.Text = LanguageManager.GetString("Meta_Label_Track")
        LabelAlbumArtiste.Text = LanguageManager.GetString("Meta_Label_AlbumArtist")
        LabelBPM.Text = LanguageManager.GetString("Meta_Label_BPM")
        ButtonSauvegarder.Text = LanguageManager.GetString("Meta_Button_Save")
        ButtonFermer.Text = LanguageManager.GetString("Meta_Button_Close")
    End Sub

    ' Gestionnaire du bouton Fermer
    Private Sub ButtonFermer_Click(sender As Object, e As EventArgs) Handles ButtonFermer.Click
        Me.Close()
    End Sub

    ' Gestionnaire du bouton Sauvegarder
    Private Sub ButtonSauvegarder_Click(sender As Object, e As EventArgs) Handles ButtonSauvegarder.Click
        SauvegarderMetadonnees()
    End Sub

    ' Méthode pour afficher les métadonnées
    Public Sub AfficherMetadonnees(cheminFichier As String, Optional parentForm As Form1 = Nothing)
        Try
            ' Mémoriser le formulaire parent
            formParent = parentForm

            ' Vérifier si le fichier existe
            If Not IO.File.Exists(cheminFichier) Then
                MessageBox.Show(LanguageManager.GetString("Meta_Error_FileNotExists"),
                              LanguageManager.GetString("Error_Title"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error)
                Return
            End If

            ' Mémoriser le chemin du fichier
            cheminFichierAudio = cheminFichier

            ' Extraire les métadonnées avec NAudio
            Dim metadonnees As String = ""
            metadonnees &= "═══════════════════════════════════════" & vbCrLf
            metadonnees &= "         " & LanguageManager.GetString("Meta_Info_Section_File") & vbCrLf
            metadonnees &= "═══════════════════════════════════════" & vbCrLf & vbCrLf

            ' Informations de base du fichier
            Dim fichierInfo As New IO.FileInfo(cheminFichier)
            metadonnees &= "📁 " & LanguageManager.GetString("Meta_Info_FileName") & vbCrLf
            metadonnees &= "   " & fichierInfo.Name & vbCrLf & vbCrLf

            metadonnees &= "📂 " & LanguageManager.GetString("Meta_Info_FullPath") & vbCrLf
            metadonnees &= "   " & cheminFichier & vbCrLf & vbCrLf

            metadonnees &= "💾 " & LanguageManager.GetString("Meta_Info_FileSize") & vbCrLf
            metadonnees &= "   " & FormatTailleFichier(fichierInfo.Length) & vbCrLf & vbCrLf

            metadonnees &= "📅 " & LanguageManager.GetString("Meta_Info_ModifiedDate") & vbCrLf
            metadonnees &= "   " & fichierInfo.LastWriteTime.ToString("G", Globalization.CultureInfo.CurrentCulture) & vbCrLf & vbCrLf

            ' Informations audio avec NAudio
            Try
                Using reader As New NAudio.Wave.AudioFileReader(cheminFichier)
                    metadonnees &= "───────────────────────────────────────" & vbCrLf
                    metadonnees &= "         " & LanguageManager.GetString("Meta_Info_Section_Audio") & vbCrLf
                    metadonnees &= "───────────────────────────────────────" & vbCrLf & vbCrLf

                    metadonnees &= "⏱️ " & LanguageManager.GetString("Meta_Info_Duration") & vbCrLf
                    Dim duree As TimeSpan = reader.TotalTime
                    metadonnees &= "   " & String.Format("{0:D2}:{1:D2}:{2:D2}", CInt(duree.TotalHours), duree.Minutes, duree.Seconds) & vbCrLf & vbCrLf

                    metadonnees &= "🔊 " & LanguageManager.GetString("Meta_Info_AudioFormat") & vbCrLf
                    metadonnees &= "   " & reader.WaveFormat.Encoding.ToString() & vbCrLf & vbCrLf

                    metadonnees &= "🎚️ " & LanguageManager.GetString("Meta_Info_SampleRate") & vbCrLf
                    metadonnees &= "   " & reader.WaveFormat.SampleRate.ToString() & " Hz" & vbCrLf & vbCrLf

                    metadonnees &= "🎧 " & LanguageManager.GetString("Meta_Info_Channels") & vbCrLf
                    Dim channelType As String = If(reader.WaveFormat.Channels = 1, LanguageManager.GetString("Meta_Channel_Mono"), If(reader.WaveFormat.Channels = 2, LanguageManager.GetString("Meta_Channel_Stereo"), LanguageManager.GetString("Meta_Channel_Multi")))
                    metadonnees &= "   " & reader.WaveFormat.Channels.ToString() & " (" & channelType & ")" & vbCrLf & vbCrLf

                    metadonnees &= "📊 " & LanguageManager.GetString("Meta_Info_BitsPerSample") & vbCrLf
                    metadonnees &= "   " & reader.WaveFormat.BitsPerSample.ToString() & " bits" & vbCrLf & vbCrLf

                    metadonnees &= "💿 " & LanguageManager.GetString("Meta_Info_BitrateAvg") & vbCrLf
                    Dim bitrate As Long = CLng((fichierInfo.Length * 8) / duree.TotalSeconds / 1000)
                    metadonnees &= "   " & bitrate.ToString() & " kbps" & vbCrLf & vbCrLf

                    metadonnees &= "📏 " & LanguageManager.GetString("Meta_Info_BytesPerSecond") & vbCrLf
                    metadonnees &= "   " & reader.WaveFormat.AverageBytesPerSecond.ToString("N0") & " bytes/s" & vbCrLf & vbCrLf
                End Using
            Catch ex As Exception
                metadonnees &= "───────────────────────────────────────" & vbCrLf
                metadonnees &= "         " & LanguageManager.GetString("Meta_Info_Section_Audio") & vbCrLf
                metadonnees &= "───────────────────────────────────────" & vbCrLf & vbCrLf
                metadonnees &= "⚠️ " & LanguageManager.GetString("Meta_Info_AudioReadFailed") & vbCrLf
                metadonnees &= "   " & LanguageManager.GetString("Meta_Info_AudioPossiblyInUse") & vbCrLf & vbCrLf
            End Try

            ' Lire les tags ID3 avec TagLib# et les afficher dans les informations
            Try
                metadonnees &= "───────────────────────────────────────" & vbCrLf
                metadonnees &= "         " & LanguageManager.GetString("Meta_Info_Section_Tags") & vbCrLf
                metadonnees &= "───────────────────────────────────────" & vbCrLf & vbCrLf & vbCrLf

                ' Vérifier si le fichier est en cours de lecture par le parent
                Dim estEnLectureParParent As Boolean = False
                If parentForm IsNot Nothing Then
                    estEnLectureParParent = parentForm.EstEnLecture() AndAlso
                                           parentForm.ObtenirCheminFichierEnCours() = cheminFichier
                End If

                Dim tagFile As TagLib.File = Nothing

                If estEnLectureParParent Then
                    ' Utiliser une abstraction avec partage en lecture pour éviter les conflits
                    Dim abstractionLecture As New FileAbstractionLectureSeule(cheminFichier)
                    tagFile = TagLib.File.Create(abstractionLecture)
                Else
                    ' Le fichier n'est pas en lecture, ouvrir normalement
                    tagFile = TagLib.File.Create(cheminFichier)
                End If

                If Not String.IsNullOrWhiteSpace(tagFile.Tag.Title) Then
                    metadonnees &= "🎵 " & LanguageManager.GetString("Meta_Info_Tag_Title") & " " & tagFile.Tag.Title & vbCrLf
                End If

                If tagFile.Tag.Performers IsNot Nothing AndAlso tagFile.Tag.Performers.Length > 0 Then
                    metadonnees &= "👤 " & LanguageManager.GetString("Meta_Info_Tag_Artist") & " " & String.Join(", ", tagFile.Tag.Performers) & vbCrLf
                End If

                If Not String.IsNullOrWhiteSpace(tagFile.Tag.Album) Then
                    metadonnees &= "💿 " & LanguageManager.GetString("Meta_Info_Tag_Album") & " " & tagFile.Tag.Album & vbCrLf
                End If

                If tagFile.Tag.Year > 0 Then
                    metadonnees &= "📅 " & LanguageManager.GetString("Meta_Info_Tag_Year") & " " & tagFile.Tag.Year.ToString() & vbCrLf
                End If

                If tagFile.Tag.Genres IsNot Nothing AndAlso tagFile.Tag.Genres.Length > 0 Then
                    metadonnees &= "🎸 " & LanguageManager.GetString("Meta_Info_Tag_Genre") & " " & String.Join(", ", tagFile.Tag.Genres) & vbCrLf
                End If

                If tagFile.Tag.Track > 0 Then
                    metadonnees &= "🔢 " & LanguageManager.GetString("Meta_Info_Tag_Track") & " " & tagFile.Tag.Track.ToString()
                    If tagFile.Tag.TrackCount > 0 Then
                        metadonnees &= " / " & tagFile.Tag.TrackCount.ToString()
                    End If
                    metadonnees &= vbCrLf
                End If

                If tagFile.Tag.AlbumArtists IsNot Nothing AndAlso tagFile.Tag.AlbumArtists.Length > 0 Then
                    metadonnees &= "🎤 " & LanguageManager.GetString("Meta_Info_Tag_AlbumArtist") & " " & String.Join(", ", tagFile.Tag.AlbumArtists) & vbCrLf
                End If

                If Not String.IsNullOrWhiteSpace(tagFile.Tag.Comment) Then
                    metadonnees &= "💬 " & LanguageManager.GetString("Meta_Info_Tag_Comment") & " " & tagFile.Tag.Comment & vbCrLf
                End If

                If tagFile.Tag.BeatsPerMinute > 0 Then
                    metadonnees &= "🎼 " & LanguageManager.GetString("Meta_Info_Tag_BPM") & " " & tagFile.Tag.BeatsPerMinute.ToString() & vbCrLf
                End If

                If tagFile.Tag.Composers IsNot Nothing AndAlso tagFile.Tag.Composers.Length > 0 Then
                    metadonnees &= "✍️ " & LanguageManager.GetString("Meta_Info_Tag_Composer") & " " & String.Join(", ", tagFile.Tag.Composers) & vbCrLf
                End If

                ' Informations sur les tags présents
                metadonnees &= vbCrLf & "📋 " & LanguageManager.GetString("Meta_Info_TagTypesDetected") & vbCrLf
                Dim tagTypes As String = tagFile.TagTypes.ToString()
                metadonnees &= "   " & tagTypes & vbCrLf

                ' Si aucun tag n'est présent
                If String.IsNullOrWhiteSpace(tagFile.Tag.Title) AndAlso
                   (tagFile.Tag.Performers Is Nothing OrElse tagFile.Tag.Performers.Length = 0) AndAlso
                   String.IsNullOrWhiteSpace(tagFile.Tag.Album) Then
                    metadonnees &= vbCrLf & "⚠️ " & LanguageManager.GetString("Meta_Info_NoMetadataFound") & vbCrLf
                    metadonnees &= "   " & LanguageManager.GetString("Meta_Info_AddMetadataBelow") & vbCrLf
                End If

                tagFile.Dispose()

            Catch ex As Exception
                metadonnees &= "⚠️ " & LanguageManager.GetString("Meta_Info_TagsReadFailed") & vbCrLf
                metadonnees &= "   " & ex.Message & vbCrLf
            End Try

            ' Afficher dans la TextBox en lecture seule
            TextBoxInfo.Text = metadonnees

            ' Charger les tags ID3 dans les champs éditables avec TagLib
            ChargerTagsID3()

        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Meta_Error_ReadingMetadata", ex.Message),
                          LanguageManager.GetString("Error_Title"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    ' Charger les tags ID3 avec TagLib#
    Private Sub ChargerTagsID3()
        Try
            If String.IsNullOrEmpty(cheminFichierAudio) OrElse Not IO.File.Exists(cheminFichierAudio) Then
                Return
            End If

            ' Vérifier si le fichier est en cours de lecture
            Dim estEnLectureParParent As Boolean = False
            If formParent IsNot Nothing Then
                estEnLectureParParent = formParent.EstEnLecture() AndAlso
                                       formParent.ObtenirCheminFichierEnCours() = cheminFichierAudio
            End If

            Dim file As TagLib.File = Nothing

            If estEnLectureParParent Then
                ' Lire le fichier avec TagLib en utilisant l'abstraction avec partage
                Dim abstractionLecture As New FileAbstractionLectureSeule(cheminFichierAudio)
                file = TagLib.File.Create(abstractionLecture)
            Else
                ' Le fichier n'est pas en lecture, ouvrir normalement
                file = TagLib.File.Create(cheminFichierAudio)
            End If

            ' Remplir les champs éditables
            TextBoxTitre.Text = If(file.Tag.Title, "")
            TextBoxArtiste.Text = If(file.Tag.FirstPerformer, "")
            TextBoxAlbum.Text = If(file.Tag.Album, "")
            TextBoxAnnee.Text = If(file.Tag.Year > 0, file.Tag.Year.ToString(), "")
            TextBoxGenre.Text = If(file.Tag.FirstGenre, "")
            TextBoxCommentaire.Text = If(file.Tag.Comment, "")
            TextBoxPiste.Text = If(file.Tag.Track > 0, file.Tag.Track.ToString(), "")
            TextBoxAlbumArtiste.Text = If(file.Tag.FirstAlbumArtist, "")
            ' Lire le BPM précis (champ personnalisé si présent)
            Dim bpmPrecis As Double = BPMMetadataManager.LireBPMPrecisDepuisMetadonnees(cheminFichierAudio)
            If bpmPrecis > 0 Then
                TextBoxBPM.Text = bpmPrecis.ToString("F2", Globalization.CultureInfo.InvariantCulture)
            ElseIf file.Tag.BeatsPerMinute > 0 Then
                TextBoxBPM.Text = file.Tag.BeatsPerMinute.ToString()
            Else
                TextBoxBPM.Text = ""
            End If

            ' Libérer les ressources
            file.Dispose()

        Catch ex As Exception
            ' Si erreur, laisser les champs vides
            TextBoxTitre.Text = ""
            TextBoxArtiste.Text = ""
            TextBoxAlbum.Text = ""
            TextBoxAnnee.Text = ""
            TextBoxGenre.Text = ""
            TextBoxCommentaire.Text = ""
            TextBoxPiste.Text = ""
            TextBoxAlbumArtiste.Text = ""
            TextBoxBPM.Text = ""
        End Try
    End Sub

    ' Sauvegarder les métadonnées modifiées
    Private Sub SauvegarderMetadonnees()
        Try
            If String.IsNullOrEmpty(cheminFichierAudio) OrElse Not IO.File.Exists(cheminFichierAudio) Then
                MessageBox.Show(LanguageManager.GetString("Meta_Error_NoFileLoaded"),
                              LanguageManager.GetString("Error_Title"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error)
                Return
            End If

            ' FORCER LA LIBÉRATION DE TOUS LES STREAMS TAGLIB EXISTANTS
            ' Ceci est crucial car FileAbstractionLectureSeule peut avoir des streams ouverts
            For i As Integer = 1 To 10
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, True)
                GC.WaitForPendingFinalizers()
                Application.DoEvents()
                System.Threading.Thread.Sleep(100)
            Next

            ' TESTER si le fichier est vraiment accessible en écriture
            If Not EstFichierAccessible(cheminFichierAudio) Then
                MessageBox.Show(
                    LanguageManager.GetString("Meta_Error_FileLocked"),
                    LanguageManager.GetString("Meta_Title_FileLocked"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Return
            End If

            ' Vérifier si le fichier est en cours de lecture par le parent
            Dim etaitEnLecture As Boolean = False
            Dim fichierEnCoursParent As String = ""

            If formParent IsNot Nothing Then
                etaitEnLecture = formParent.EstEnLecture()
                If etaitEnLecture Then
                    fichierEnCoursParent = formParent.ObtenirCheminFichierEnCours()
                End If
            End If

            ' Vérifier si ce fichier spécifique est en cours de lecture
            Dim ceFichierEnLecture As Boolean = (etaitEnLecture AndAlso
                                                 fichierEnCoursParent.Equals(cheminFichierAudio, StringComparison.OrdinalIgnoreCase))

            ' Si CE fichier est en cours de lecture, demander d'arrêter
            If ceFichierEnLecture Then
                Dim resultat = MessageBox.Show(
                    LanguageManager.GetString("Meta_Info_FileCurrentlyPlaying"),
                    LanguageManager.GetString("Meta_Title_FileCurrentlyPlaying"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information)

                If resultat = DialogResult.Yes Then
                    formParent.ArreterLecturePublic()
                    MessageBox.Show(
                        LanguageManager.GetString("Meta_Info_PlaybackStopped"),
                        LanguageManager.GetString("Meta_Title_PlaybackStopped"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)
                End If
                Return
            End If

            ' Tentatives de sauvegarde (fichier n'est plus en cours de lecture)
            Dim tentatives As Integer = 0
            Dim maxTentatives As Integer = 3
            Dim reussi As Boolean = False
            Dim derniereErreur As Exception = Nothing

            While tentatives < maxTentatives AndAlso Not reussi
                Try
                    ' Vérifier que le fichier existe
                    If Not IO.File.Exists(cheminFichierAudio) Then
                        Throw New IO.FileNotFoundException($"Le fichier n'existe pas : {cheminFichierAudio}")
                    End If

                    ' Ouvrir le fichier avec TagLib
                    Dim file As TagLib.File = Nothing
                    Try
                        file = TagLib.File.Create(cheminFichierAudio)
                    Catch ex As Exception
                        Throw New Exception($"Impossible d'ouvrir le fichier avec TagLib : {ex.Message}", ex)
                    End Try

                    ' Mettre à jour les tags
                    file.Tag.Title = TextBoxTitre.Text
                    file.Tag.Performers = If(String.IsNullOrWhiteSpace(TextBoxArtiste.Text), New String() {}, New String() {TextBoxArtiste.Text})
                    file.Tag.Album = TextBoxAlbum.Text
                    file.Tag.Genres = If(String.IsNullOrWhiteSpace(TextBoxGenre.Text), New String() {}, New String() {TextBoxGenre.Text})
                    file.Tag.Comment = TextBoxCommentaire.Text
                    file.Tag.AlbumArtists = If(String.IsNullOrWhiteSpace(TextBoxAlbumArtiste.Text), New String() {}, New String() {TextBoxAlbumArtiste.Text})

                    ' Année
                    Dim annee As UInteger = 0
                    If UInteger.TryParse(TextBoxAnnee.Text, annee) Then
                        file.Tag.Year = annee
                    Else
                        file.Tag.Year = 0
                    End If

                    ' Numéro de piste
                    Dim piste As UInteger = 0
                    If UInteger.TryParse(TextBoxPiste.Text, piste) Then
                        file.Tag.Track = piste
                    Else
                        file.Tag.Track = 0
                    End If

                    ' BPM
                    Dim bpm As UInteger = 0
                    If UInteger.TryParse(TextBoxBPM.Text, bpm) Then
                        file.Tag.BeatsPerMinute = bpm
                    Else
                        file.Tag.BeatsPerMinute = 0
                    End If

                    ' Debug : vérifier l'état avant sauvegarde
                    Debug.WriteLine($"Tentative de sauvegarde sur : {cheminFichierAudio}")
                    Debug.WriteLine($"Fichier existe : {IO.File.Exists(cheminFichierAudio)}")

                    ' Sauvegarder les modifications
                    Try
                        file.Save()
                    Catch saveEx As Exception
                        ' Capturer et ré-lancer avec plus de détails
                        Dim erreurDetaille As String = $"Erreur Save() TagLib:{vbCrLf}" &
                                                       $"Type: {saveEx.GetType().FullName}{vbCrLf}" &
                                                       $"Message: {saveEx.Message}{vbCrLf}" &
                                                       $"Fichier: {cheminFichierAudio}"
                        If saveEx.InnerException IsNot Nothing Then
                            erreurDetaille &= $"{vbCrLf}Inner: {saveEx.InnerException.Message}"
                        End If
                        Throw New Exception(erreurDetaille, saveEx)
                    End Try

                    ' Libérer les ressources
                    file.Dispose()

                    reussi = True

                Catch ex As Exception
                    ' Erreur d'accès au fichier, on réessaie
                    derniereErreur = ex
                    tentatives += 1

                    ' Log de l'erreur pour diagnostic
                    Debug.WriteLine($"Tentative {tentatives} échouée : {ex.GetType().Name} - {ex.Message}")

                    If tentatives < maxTentatives Then
                        ' Attendre de plus en plus longtemps à chaque tentative
                        System.Threading.Thread.Sleep(300 * tentatives)
                        ' Forcer à nouveau le GC
                        GC.Collect()
                        GC.WaitForPendingFinalizers()
                    End If
                End Try
            End While

            ' Vérifier si la sauvegarde a réussi
            If Not reussi Then
                Throw New Exception($"Impossible d'accéder au fichier après {maxTentatives} tentatives. Le fichier est peut-être verrouillé par une autre application.", derniereErreur)
            End If

            MessageBox.Show(LanguageManager.GetString("Meta_Success_Saved"),
                          LanguageManager.GetString("Success_Title"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Information)

            ' Mettre à jour le BPM dans le ListView si l'item existe
            If itemCourant IsNot Nothing AndAlso itemCourant.SubItems.Count > 2 Then
                Dim bpmValue As UInteger = 0
                If UInteger.TryParse(TextBoxBPM.Text, bpmValue) AndAlso bpmValue > 0 Then
                    itemCourant.SubItems(2).Text = bpmValue.ToString()
                Else
                    itemCourant.SubItems(2).Text = ""
                End If
            End If

            ' Fermer le formulaire
            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            ' Message d'erreur détaillé pour le débogage
            Dim messageErreur As String = LanguageManager.GetString("Meta_Error_SavingHeader") & vbCrLf & vbCrLf
            messageErreur &= LanguageManager.GetString("Meta_Error_Type", ex.GetType().Name) & vbCrLf
            messageErreur &= LanguageManager.GetString("Meta_Error_Message", ex.Message) & vbCrLf & vbCrLf

            If ex.InnerException IsNot Nothing Then
                messageErreur &= LanguageManager.GetString("Meta_Error_Details", ex.InnerException.Message) & vbCrLf & vbCrLf
            End If

            messageErreur &= LanguageManager.GetString("Meta_Error_CheckReadonly")

            MessageBox.Show(messageErreur,
                          LanguageManager.GetString("Meta_Title_SaveError"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    ' Fonction utilitaire pour formater la taille du fichier
    Private Function FormatTailleFichier(taille As Long) As String
        Dim suffixes() As String = {
            LanguageManager.GetString("Meta_Unit_Bytes"),
            LanguageManager.GetString("Meta_Unit_KB"),
            LanguageManager.GetString("Meta_Unit_MB"),
            LanguageManager.GetString("Meta_Unit_GB"),
            LanguageManager.GetString("Meta_Unit_TB")
        }
        Dim index As Integer = 0
        Dim tailleDouble As Double = taille

        While tailleDouble >= 1024 AndAlso index < suffixes.Length - 1
            tailleDouble /= 1024
            index += 1
        End While

        Return String.Format("{0:0.##} {1}", tailleDouble, suffixes(index))
    End Function
End Class

' Classe d'abstraction TagLib pour lecture seule avec partage de fichier
' Permet de lire les métadonnées même si le fichier est ouvert par NAudio
Public Class FileAbstractionLectureSeule
    Implements TagLib.File.IFileAbstraction

    Private ReadOnly _path As String

    Public Sub New(path As String)
        _path = path
    End Sub

    Public ReadOnly Property Name As String Implements TagLib.File.IFileAbstraction.Name
        Get
            Return _path
        End Get
    End Property

    Public ReadOnly Property ReadStream As IO.Stream Implements TagLib.File.IFileAbstraction.ReadStream
        Get
            ' Ouvrir en lecture avec partage complet (ReadWrite)
            ' Cela permet de lire même si NAudio a le fichier ouvert
            Return New IO.FileStream(_path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
        End Get
    End Property

    Public ReadOnly Property WriteStream As IO.Stream Implements TagLib.File.IFileAbstraction.WriteStream
        Get
            ' Pour la lecture seule, on ne devrait jamais écrire
            ' Mais l'interface l'exige, donc on retourne Nothing
            Throw New NotSupportedException("Cette abstraction est en lecture seule.")
        End Get
    End Property

    Public Sub CloseStream(stream As IO.Stream) Implements TagLib.File.IFileAbstraction.CloseStream
        If stream IsNot Nothing Then
            stream.Close()
            stream.Dispose()
        End If
    End Sub
End Class
