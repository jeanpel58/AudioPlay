Imports System.IO
Imports System.Management
Imports System.Runtime.InteropServices
Imports System.Diagnostics

Public Class FormCompresser

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

    ' API Windows pour le contrôle du lecteur CD
    <DllImport("winmm.dll", CharSet:=CharSet.Ansi)>
    Private Shared Function mciSendString(lpszCommand As String, lpszReturnString As String,
                                          cchReturnLength As Integer, hwndCallback As IntPtr) As Integer
    End Function

    ' Classe interne pour représenter un lecteur CD
    Private Class LecteurCDItem
        Public Property Lecteur As String
        Public Property ContientCD As Boolean
        Public Property NomComplet As String ' Ex: "D: PIONEER BD-RW BDR-211M 1.51 Adapter: 1 ID: 0"

        Public Sub New(lecteur As String, contientCD As Boolean, nomComplet As String)
            Me.Lecteur = lecteur
            Me.ContientCD = contientCD
            Me.NomComplet = nomComplet
        End Sub

        Public Overrides Function ToString() As String
            Return NomComplet
        End Function
    End Class

    ' Surveillance des changements de CD
    Private cdMonitorTimer As Timer
    Private derniersEtatsLecteurs As New Dictionary(Of String, Boolean)
    Private ignorerChangementsCD As Boolean = False ' Flag pour ignorer temporairement les changements de CD
    Private discIdActuel As String = Nothing ' DiscID du CD actuellement chargé pour éviter les rechargements inutiles

    ' Clé API Last.fm pour AudioPlay (publique, lecture seule)
    ' Si vous préférez utiliser votre propre clé gratuite, vous pouvez la modifier ici
    ' Obtenez une clé sur : https://www.last.fm/api/account/create
    Private Const LASTFM_API_KEY As String = "43693f61d26bd8a3f270320b0eeecffd"

    ' Données du CD
    Private pistesCD As List(Of CDAudioManager.CDTrack)
    Private metadonneesCD As CDMetadataProvider.CDInfo
    Private lecteurCD As String
    Private chargementInitial As Boolean = False
    Private analysesPistes As New Dictionary(Of Integer, CDAudioAnalyzer.TrackAnalysis) ' Analyses des pistes (clé = index piste)
    Private annulationDemandee As Boolean = False ' Flag pour annuler l'extraction en cours

    ' Cache temporaire de la pochette (sauvegardé uniquement lors de l'extraction)
    Private pochetteTempUrl As String = Nothing
    Private pochetteTempBytes As Byte() = Nothing

    ' Historique de navigation des pochettes
    Private historiquePochettes As New List(Of String) ' Liste des URLs
    Private indexPochetteActuelle As Integer = -1 ' Index dans l'historique (-1 = aucune)
    Private cachePochettesBytes As New Dictionary(Of String, Byte()) ' Cache mémoire des images téléchargées
    Private sourcesPochettes As New Dictionary(Of String, String) ' Source de chaque URL (ex: "Last.fm", "iTunes")

    ' Édition en ligne dans la ListView
    Private editTextBox As TextBox
    Private editingItem As ListViewItem
    Private editingSubItemIndex As Integer
    Private editingItemOriginalCheckedState As Boolean ' État original de la checkbox de l'item en édition

    ''' <summary>
    ''' Définit la hauteur de l'en-tête du ListView
    ''' </summary>
    ''' <summary>
    ''' Initialise les données du CD pour l'extraction
    ''' </summary>
    Public Async Sub InitialiserDonneesCD(lecteur As String, pistes As List(Of CDAudioManager.CDTrack), metadata As CDMetadataProvider.CDInfo)
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] InitialiserDonneesCD() appelé - Lecteur: {lecteur}, Pistes: {pistes?.Count}, Metadata: {If(metadata Is Nothing, "Nothing", "OK")}")
        ' Flag pour éviter le rechargement lors de la sélection initiale
        chargementInitial = True

        ' Ignorer les changements de CD pendant 5 secondes pour éviter les rechargements intempestifs
        ignorerChangementsCD = True
        Dim timerReactivation As New Timer With {.Interval = 5000} ' 5 secondes
        AddHandler timerReactivation.Tick, Sub(s, e)
                                               ignorerChangementsCD = False
                                               timerReactivation.Stop()
                                               timerReactivation.Dispose()
                                               System.Diagnostics.Debug.WriteLine("[FormCompresser] Surveillance CD réactivée")
                                           End Sub
        timerReactivation.Start()
        System.Diagnostics.Debug.WriteLine("[FormCompresser] Surveillance CD suspendue pour 5 secondes")

        lecteurCD = lecteur
        pistesCD = pistes
        metadonneesCD = metadata

        ' Calculer et sauvegarder le DiscID du CD actuel
        If pistes IsNot Nothing AndAlso pistes.Count > 0 Then
            discIdActuel = CDMetadataProvider.CalculerDiscID(pistes)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] DiscID actuel sauvegardé: {discIdActuel}")
        Else
            discIdActuel = Nothing
        End If

        ' Sélectionner le lecteur dans le ComboBox si possible
        SelectionnerLecteur(lecteur)

        Await RemplirInformationsCD()

        ' Réactiver le chargement automatique après l'initialisation
        chargementInitial = False
    End Sub

    ''' <summary>
    ''' Sélectionne le lecteur spécifié dans le ComboBox
    ''' </summary>
    Private Sub SelectionnerLecteur(lecteur As String)
        If String.IsNullOrWhiteSpace(lecteur) OrElse ComboBoxChoixLecteur.Items.Count = 0 Then
            Return
        End If

        ' Normaliser le format du lecteur (enlever le ":" si présent)
        Dim lecteurNormalise As String = lecteur.TrimEnd(":"c).ToUpper()

        ' Chercher l'item correspondant dans le ComboBox
        For i As Integer = 0 To ComboBoxChoixLecteur.Items.Count - 1
            Dim item = TryCast(ComboBoxChoixLecteur.Items(i), LecteurCDItem)
            If item IsNot Nothing Then
                Dim itemLecteur As String = item.Lecteur.TrimEnd(":"c).ToUpper()
                If itemLecteur = lecteurNormalise Then
                    ComboBoxChoixLecteur.SelectedIndex = i
                    Exit For
                End If
            End If
        Next
    End Sub

    ''' <summary>
    ''' Remplit les informations du CD dans le formulaire
    ''' </summary>
    ''' <param name="chargerPochette">Si True, charge la pochette du CD. Si False, conserve la pochette existante.</param>
    Private Async Function RemplirInformationsCD(Optional chargerPochette As Boolean = True) As Task
        Dim stackTrace = New System.Diagnostics.StackTrace(True)
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ══════════════════════════════════════════════")
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] RemplirInformationsCD() appelé depuis:")
        System.Diagnostics.Debug.WriteLine($"[FormCompresser]   {stackTrace.GetFrame(1)?.GetMethod()?.Name}")
        System.Diagnostics.Debug.WriteLine($"[FormCompresser]   chargerPochette = {chargerPochette}")
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ══════════════════════════════════════════════")

        ' Réinitialiser le cache temporaire de la pochette SEULEMENT si on charge une nouvelle pochette
        If chargerPochette Then
            pochetteTempUrl = Nothing
            pochetteTempBytes = Nothing
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Cache pochette temporaire réinitialisé")
        End If

        ' Vérifier si on a au moins des pistes
        If pistesCD Is Nothing OrElse pistesCD.Count = 0 Then
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ERREUR: pistesCD est Nothing ou vide!")
            ' Vider tous les champs
            TextBoxCDTitre.Text = ""
            TextBoxCDArtiste.Text = ""
            TextBoxAnnee.Text = ""
            TextBoxCommentaire.Text = ""
            ComboBoxGenre.SelectedIndex = -1
            ListViewCompress.Items.Clear()
            PictureBoxPochette.Image = Nothing
            Return
        End If

        System.Diagnostics.Debug.WriteLine($"[FormCompresser] pistesCD OK - {pistesCD.Count} piste(s)")

        ' Si on a des métadonnées, remplir les champs
        If metadonneesCD IsNot Nothing Then
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] metadonneesCD OK - Artist: {metadonneesCD.Artist}, Album: {metadonneesCD.Album}")

            ' Remplir les champs de texte
            TextBoxCDTitre.Text = If(String.IsNullOrWhiteSpace(metadonneesCD.Album), "", metadonneesCD.Album)
            TextBoxCDArtiste.Text = If(String.IsNullOrWhiteSpace(metadonneesCD.Artist), "", metadonneesCD.Artist)
            TextBoxAnnee.Text = If(metadonneesCD.Year > 0, metadonneesCD.Year.ToString(), "")
            TextBoxCommentaire.Text = LanguageManager.GetString("Compressor_DefaultComment") ' CDInfo n'a pas de commentaire par défaut

            ' Sélectionner le genre dans le ComboBox
            If Not String.IsNullOrWhiteSpace(metadonneesCD.Genre) Then
                Dim genreIndex As Integer = ComboBoxGenre.Items.IndexOf(metadonneesCD.Genre)
                If genreIndex >= 0 Then
                    ComboBoxGenre.SelectedIndex = genreIndex
                End If
            End If

            ' Charger la pochette de l'album SEULEMENT si demandé
            If chargerPochette Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] >>> APPEL ChargerPochetteAlbum() - metadonneesCD IsNot Nothing = {metadonneesCD IsNot Nothing}")
                If metadonneesCD IsNot Nothing Then
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] >>> Artist: {metadonneesCD.Artist}, Album: {metadonneesCD.Album}")
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] >>> CoverArtUrl: {If(String.IsNullOrWhiteSpace(metadonneesCD.CoverArtUrl), "VIDE", metadonneesCD.CoverArtUrl)}")
                End If
                Await ChargerPochetteAlbum()
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] >>> ChargerPochetteAlbum() TERMINÉ")
            Else
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ChargerPochetteAlbum() IGNORÉ (même CD)")
            End If
        Else
            ' Pas de métadonnées, vider les champs mais garder des valeurs par défaut
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] metadonneesCD est Nothing, valeurs par défaut")
            TextBoxCDTitre.Text = "CD Audio sans titre"
            TextBoxCDArtiste.Text = "Artiste inconnu"
            TextBoxAnnee.Text = ""
            TextBoxCommentaire.Text = LanguageManager.GetString("Compressor_DefaultComment")
            ComboBoxGenre.SelectedIndex = -1
            PictureBoxPochette.Image = Nothing
        End If

        ' Remplir le ListView avec les pistes (avec ou sans métadonnées)
        RemplirListViewPistes()
    End Function

    ''' <summary>
    ''' Remplit le ListView avec les pistes du CD
    ''' </summary>
    Private Sub RemplirListViewPistes()
        ListViewCompress.Items.Clear()

        ' On affiche les pistes même sans métadonnées (avec valeurs par défaut)
        If pistesCD Is Nothing OrElse pistesCD.Count = 0 Then
            Return
        End If

        ' Obtenir le numéro de départ depuis le TextBox
        Dim premierNumero As Integer = 1
        If Not String.IsNullOrWhiteSpace(TextBoxPremierNumPiste.Text) Then
            Integer.TryParse(TextBoxPremierNumPiste.Text, premierNumero)
            If premierNumero < 1 Then premierNumero = 1
        End If

        Dim positionActuelle As TimeSpan = TimeSpan.Zero
        Dim numeroActuel As Integer = premierNumero

        For Each piste In pistesCD
            Dim item As New ListViewItem()

            ' Colonne 1: Numéro de piste (en utilisant le numéro personnalisé)
            item.Text = numeroActuel.ToString()
            numeroActuel += 1

            ' Colonne 2: Titre
            Dim titre As String = piste.Title
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Piste {piste.TrackNumber}: Titre initial = '{titre}'")
            If metadonneesCD IsNot Nothing AndAlso metadonneesCD.Tracks IsNot Nothing Then
                Dim metaPiste = metadonneesCD.Tracks.FirstOrDefault(Function(p) p.TrackNumber = piste.TrackNumber)
                If metaPiste IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(metaPiste.Title) Then
                    titre = metaPiste.Title
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Piste {piste.TrackNumber}: Titre remplacé par métadonnées = '{titre}'")
                End If
            End If

            ' Si le titre est toujours vide, afficher "Piste XX [mm:ss]" comme dans FormSelecteurPistesCD
            If String.IsNullOrWhiteSpace(titre) Then
                Dim dureeFormat = TimeSpan.FromSeconds(piste.Duration.TotalSeconds)
                titre = $"Piste {piste.TrackNumber:D2} [{dureeFormat:mm\:ss}]"
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Piste {piste.TrackNumber}: Titre par défaut = '{titre}'")
            End If

            item.SubItems.Add(titre)

            ' Colonne 3: Artiste (même que l'album par défaut)
            Dim artiste As String = If(String.IsNullOrWhiteSpace(metadonneesCD?.Artist), piste.Artist, metadonneesCD.Artist)
            If metadonneesCD IsNot Nothing AndAlso metadonneesCD.Tracks IsNot Nothing Then
                Dim metaPiste = metadonneesCD.Tracks.FirstOrDefault(Function(p) p.TrackNumber = piste.TrackNumber)
                If metaPiste IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(metaPiste.Artist) Then
                    artiste = metaPiste.Artist
                End If
            End If

            ' Si l'artiste est toujours vide, afficher "Artiste inconnu"
            If String.IsNullOrWhiteSpace(artiste) Then
                artiste = "Artiste inconnu"
            End If

            item.SubItems.Add(artiste)

            ' Colonne 4: Début (position sur le CD)
            Dim debut As String = String.Format("{0:D2}:{1:D2}", CInt(positionActuelle.TotalMinutes), positionActuelle.Seconds)
            item.SubItems.Add(debut)

            ' Colonne 5: Longueur
            Dim duree As TimeSpan = piste.Duration
            Dim longueur As String = String.Format("{0:D2}:{1:D2}", CInt(duree.TotalMinutes), duree.Seconds)
            item.SubItems.Add(longueur)

            ' Colonne 6: Taille originale (PCM 44.1kHz 16-bit stéréo)
            ' 44100 Hz * 2 canaux * 2 octets/échantillon = 176400 octets/seconde
            Dim tailleOriginale As Double = piste.Duration.TotalSeconds * 176400.0 / (1024.0 * 1024.0) ' En Mo
            item.SubItems.Add(String.Format("{0:F2} Mo", tailleOriginale))

            ' Colonne 7: Taille compressée (estimée)
            Dim tailleCompressee As Double = CalculerTailleCompressee(piste.Duration.TotalSeconds)
            item.SubItems.Add(String.Format("{0:F2} Mo", tailleCompressee))

            ' Cocher la piste par défaut pour l'extraction
            item.Checked = True

            ListViewCompress.Items.Add(item)

            ' Mettre à jour la position pour la prochaine piste
            positionActuelle = positionActuelle.Add(duree)
        Next
    End Sub

    ''' <summary>
    ''' Calcule la taille estimée d'un fichier compressé selon le format et la qualité choisis
    ''' </summary>
    Private Function CalculerTailleCompressee(durationSeconds As Double) As Double
        Dim typeConversion As String = If(ComboBoxTypeConversion.SelectedItem IsNot Nothing, ComboBoxTypeConversion.SelectedItem.ToString(), "MP3")
        Dim qualiteIndex As Integer = ComboBoxQualiteConversion.SelectedIndex

        ' Déterminer le débit en kbps selon le format et l'index de qualité
        Dim debitKbps As Integer = 192 ' Valeur par défaut

        Select Case typeConversion.ToUpper()
            Case "MP3"
                ' Index 0=128, 1=192, 2=256, 3=320
                Select Case qualiteIndex
                    Case 0 ' Basse (128 kbps)
                        debitKbps = 128
                    Case 1 ' Moyenne (192 kbps)
                        debitKbps = 192
                    Case 2 ' Haute (256 kbps)
                        debitKbps = 256
                    Case 3 ' Très haute (320 kbps)
                        debitKbps = 320
                    Case Else ' Fallback
                        debitKbps = 320
                End Select

            Case "FLAC"
                ' FLAC: compression sans perte, environ 50-60% de la taille originale
                ' Index 0=Niveau 0, 1=Niveau 5, 2=Niveau 8
                Select Case qualiteIndex
                    Case 0 ' Niveau 0 (rapide)
                        debitKbps = CInt(1411.2 * 0.6)
                    Case 1 ' Niveau 5 (équilibré)
                        debitKbps = CInt(1411.2 * 0.55)
                    Case 2 ' Niveau 8 (meilleur)
                        debitKbps = CInt(1411.2 * 0.5)
                    Case Else ' Fallback
                        debitKbps = CInt(1411.2 * 0.5)
                End Select

            Case "WMA"
                ' Index 0=128, 1=192, 2=256
                Select Case qualiteIndex
                    Case 0 ' 128 kbps
                        debitKbps = 128
                    Case 1 ' 192 kbps
                        debitKbps = 192
                    Case 2 ' 256 kbps
                        debitKbps = 256
                    Case Else ' Fallback
                        debitKbps = 256
                End Select

            Case "WAV"
                ' WAV: selon la qualité choisie
                ' Index 0=16-bit 44.1kHz, 1=24-bit 96kHz, 2=32-bit 192kHz
                Select Case qualiteIndex
                    Case 0 ' PCM 16-bit 44.1 kHz
                        debitKbps = CInt(1411.2) ' 44.1kHz * 16-bit * 2 canaux
                    Case 1 ' PCM 24-bit 96 kHz
                        debitKbps = 4608 ' Environ 4.6 Mbps
                    Case 2 ' PCM 32-bit 192 kHz
                        debitKbps = 12288 ' Environ 12 Mbps
                    Case Else ' Fallback
                        debitKbps = 4608
                End Select
        End Select

        ' Calcul: (débit en kbps * durée en secondes) / 8 / 1024 = taille en Mo
        Dim tailleMo As Double = (debitKbps * durationSeconds) / 8.0 / 1024.0
        Return tailleMo
    End Function

    ''' <summary>
    ''' Applique les traductions à tous les contrôles du formulaire
    ''' </summary>
    Private Sub AppliquerTraductions()
        ' Titre du formulaire
        Me.Text = LanguageManager.GetString("Compressor_FormTitle")

        ' Labels
        Label_ChoixLecteur.Text = LanguageManager.GetString("Compressor_DriveSelection")
        Label_CDTitre.Text = LanguageManager.GetString("Compressor_CDTitle")
        LabelCDArtiste.Text = LanguageManager.GetString("Compressor_CDArtist")
        LabelAnnee.Text = LanguageManager.GetString("Compressor_Year")
        LabelGenre.Text = LanguageManager.GetString("Compressor_Genre")
        Label3.Text = LanguageManager.GetString("Compressor_CoverArt")
        LabelDimImagText.Text = LanguageManager.GetString("Compressor_ImageDimensions")
        LabelTailleImagText.Text = LanguageManager.GetString("Compressor_ImageSize")
        LabelNumCD.Text = LanguageManager.GetString("Compressor_CDNumber")
        LabelPremierNumPiste.Text = LanguageManager.GetString("Compressor_FirstTrackNumber")
        LabelCommentaire.Text = LanguageManager.GetString("Compressor_Comment")
        LabelTypeConversion.Text = LanguageManager.GetString("Compressor_ConversionType")
        LabelQualiteConversion.Text = LanguageManager.GetString("Compressor_ConversionQuality")
        LabelRepSauvegarde.Text = LanguageManager.GetString("Compressor_SaveDirectory")
        Label1.Text = LanguageManager.GetString("FCompress_Label_ImageDimensions")
        Label2.Text = LanguageManager.GetString("FCompress_Label_ImageSize")
        Label_Im_Site.Text = LanguageManager.GetString("FCompress_Label_ImageSite")
        Label_Normalisation.Text = LanguageManager.GetString("FCompress_Label_Normalization")

        ' Boutons
        ButtonRepSauvegarde.Text = LanguageManager.GetString("Compressor_ButtonBrowse")
        ButtonExtraire.Text = LanguageManager.GetString("Compressor_ButtonExtract")
        ButtonAnnuler.Text = LanguageManager.GetString("Compressor_ButtonCancel")
        ButtonQuitter.Text = LanguageManager.GetString("Compressor_ButtonQuit")
        Button_EditTracks.Text = LanguageManager.GetString("Compressor_ButtonEditTracks")
        ButtonSoumettreGnuDB.Text = LanguageManager.GetString("GnuDB_ButtonSubmit")

        ' CheckBoxes
        CheckBoxEjectCD.Text = LanguageManager.GetString("Compressor_EjectCD")
        CheckBoxVerouillerCD.Text = LanguageManager.GetString("Compressor_LockCD")
        CheckBox_FCompress_SelectDeselect.Text = LanguageManager.GetString("Compressor_SelectDeselectAll")

        ' ListView colonnes
        ColumnHeaderPiste.Text = LanguageManager.GetString("Compressor_ColumnTrack")
        ColumnHeaderTitre.Text = LanguageManager.GetString("Compressor_ColumnTitle")
        ColumnHeaderArtiste.Text = LanguageManager.GetString("Compressor_ColumnArtist")
        ColumnHeaderDébut.Text = LanguageManager.GetString("Compressor_ColumnStart")
        ColumnHeaderLongueur.Text = LanguageManager.GetString("Compressor_ColumnLength")
        ColumnHeaderTaille.Text = LanguageManager.GetString("Compressor_ColumnSize")
        ColumnHeaderTailleComp.Text = LanguageManager.GetString("Compressor_ColumnCompressedSize")
    End Sub

    ''' <summary>
    ''' Chargement du formulaire
    ''' </summary>
    Private Sub FormCompresser_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Appliquer les traductions
        AppliquerTraductions()

        ' Configurer le ComboBoxChoixLecteur AVANT d'appliquer le thème
        ' pour éviter que le ThemeManager ne change son DrawMode
        ComboBoxChoixLecteur.DrawMode = DrawMode.OwnerDrawFixed
        AddHandler ComboBoxChoixLecteur.DrawItem, AddressOf ComboBoxChoixLecteur_DrawItem
        AddHandler ComboBoxChoixLecteur.SelectedIndexChanged, AddressOf ComboBoxChoixLecteur_SelectedIndexChanged

        ' Appliquer le thème au formulaire (les autres contrôles seront gérés automatiquement)
        ThemeManager.ApplyThemeToForm(Me)

        ' Ajouter les gestionnaires pour le ListView
        AddHandler ListViewCompress.DrawColumnHeader, AddressOf ListViewCompress_DrawColumnHeader
        AddHandler ListViewCompress.DrawItem, AddressOf ListViewCompress_DrawItem
        AddHandler ListViewCompress.DrawSubItem, AddressOf ListViewCompress_DrawSubItem

        ' Ajouter les gestionnaires pour recalculer les tailles ET pour remplir ComboBoxQualiteConversion
        ' IMPORTANT: Ajouter ces gestionnaires AVANT de définir SelectedIndex pour qu'ils se déclenchent
        AddHandler ComboBoxTypeConversion.SelectedIndexChanged, AddressOf ComboBoxTypeConversion_SelectedIndexChanged
        AddHandler ComboBoxTypeConversion.SelectedIndexChanged, AddressOf RecalculerTailles
        AddHandler ComboBoxQualiteConversion.SelectedIndexChanged, AddressOf RecalculerTailles

        ' Définir les valeurs par défaut des ComboBox
        ' Maintenant que le gestionnaire est attaché, cela va déclencher le remplissage de ComboBoxQualiteConversion
        If ComboBoxTypeConversion.Items.Count > 0 Then
            ' Chercher "MP3" dans les items
            Dim mp3Index As Integer = ComboBoxTypeConversion.Items.IndexOf("MP3")
            If mp3Index >= 0 Then
                ComboBoxTypeConversion.SelectedIndex = mp3Index
            Else
                ComboBoxTypeConversion.SelectedIndex = 0 ' Si "MP3" n'est pas trouvé, sélectionner le premier
            End If
        End If

        ' Ajouter le gestionnaire pour le TextBoxPremierNumPiste
        AddHandler TextBoxPremierNumPiste.TextChanged, AddressOf TextBoxPremierNumPiste_TextChanged
        AddHandler TextBoxPremierNumPiste.KeyPress, AddressOf TextBoxPremierNumPiste_KeyPress

        ' Ajouter le gestionnaire pour le bouton de sélection du répertoire
        AddHandler ButtonRepSauvegarde.Click, AddressOf ButtonRepSauvegarde_Click

        ' Ajouter le gestionnaire pour sauvegarder le volume d'extraction quand il change
        AddHandler NumericUpDown_DB.ValueChanged, AddressOf NumericUpDown_DB_ValueChanged

        ' Charger le répertoire de sauvegarde depuis les paramètres
        ChargerRepertoireSauvegarde()

        ' Charger les lecteurs CD
        ChargerLecteursCD()

        ' Démarrer la surveillance des lecteurs
        InitialiserSurveillanceLecteurs()

        ' Vérifier la disponibilité de FFMpeg (en arrière-plan, sans bloquer l'interface)
        Task.Run(Sub() VerifierFFMpegDisponibilite())

        ' NOTE: ChargerPochetteAlbum() est appelé depuis RemplirInformationsCD()
        ' après que metadonneesCD soit initialisé via InitialiserDonneesCD()
    End Sub

    ''' <summary>
    ''' Vérifie si FFMpeg est disponible et affiche une info discrète si absent
    ''' </summary>
    Private Async Sub VerifierFFMpegDisponibilite()
        Await Task.Delay(500) ' Petit délai pour laisser le formulaire se charger

        If Not FFMpegManager.EstInstalle() Then
            ' Afficher une info discrète (non bloquante)
            If Me.InvokeRequired Then
                Me.Invoke(Sub() AfficherInfoFFMpegAbsent())
            Else
                AfficherInfoFFMpegAbsent()
            End If
        End If
    End Sub

    ''' <summary>
    ''' Affiche une notification discrète que FFMpeg n'est pas installé
    ''' </summary>
    Private Sub AfficherInfoFFMpegAbsent()
        ' On pourrait afficher un label discret en bas du formulaire
        ' Pour l'instant, on ne fait rien - l'utilisateur sera averti lors de l'extraction FLAC/WMA
        System.Diagnostics.Debug.WriteLine("[FormCompresser] FFMpeg n'est pas installé - téléchargement sera proposé lors de l'extraction FLAC/WMA")
    End Sub

    ''' <summary>
    ''' Recalcule les tailles compressées quand le format ou la qualité change
    ''' </summary>
    Private Sub RecalculerTailles(sender As Object, e As EventArgs)
        If pistesCD IsNot Nothing AndAlso pistesCD.Count > 0 Then
            RemplirListViewPistes()
        End If
    End Sub

    ''' <summary>
    ''' Met à jour les options de qualité selon le format d'extraction sélectionné
    ''' </summary>
    Private Sub ComboBoxTypeConversion_SelectedIndexChanged(sender As Object, e As EventArgs)
        If ComboBoxTypeConversion.SelectedItem Is Nothing Then Return

        Dim formatSelectionne As String = ComboBoxTypeConversion.SelectedItem.ToString().ToUpper()

        ' Suspendre la mise à jour de l'interface
        ComboBoxQualiteConversion.BeginUpdate()
        ComboBoxQualiteConversion.Items.Clear()

        Select Case formatSelectionne
            Case "MP3"
                ' Options MP3
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityLow"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityMedium"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityHigh"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityVeryHigh"))
                ' Par défaut : Très haute
                ComboBoxQualiteConversion.SelectedIndex = 3

            Case "FLAC"
                ' Options FLAC
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityFlacLevel0"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityFlacLevel5"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityFlacLevel8"))
                ' Par défaut : Niveau 8
                ComboBoxQualiteConversion.SelectedIndex = 2

            Case "WAV"
                ' Options WAV
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWav16"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWav24"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWav32"))
                ' Par défaut : PCM 24-bit 96 kHz
                ComboBoxQualiteConversion.SelectedIndex = 1

            Case "WMA"
                ' Options WMA
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWma128"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWma192"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWma256"))
                ' Par défaut : 256 kbps
                ComboBoxQualiteConversion.SelectedIndex = 2

            Case Else
                ' Fallback si format inconnu (ne devrait pas arriver)
                ComboBoxQualiteConversion.Items.Add("Standard")
                ComboBoxQualiteConversion.SelectedIndex = 0
        End Select

        ' Reprendre la mise à jour de l'interface
        ComboBoxQualiteConversion.EndUpdate()
    End Sub

    ''' <summary>
    ''' Obtient la liste des pistes sélectionnées pour l'extraction
    ''' </summary>
    ''' <summary>
    ''' Retourne les indices des pistes sélectionnées dans le ListView
    ''' </summary>
    Public Function ObtenirIndicesPistesSelectionnees() As List(Of Integer)
        Dim indices As New List(Of Integer)()

        For i As Integer = 0 To ListViewCompress.Items.Count - 1
            If ListViewCompress.Items(i).Checked Then
                indices.Add(i)
            End If
        Next

        Return indices
    End Function

    ''' <summary>
    ''' Retourne les pistes sélectionnées (pour compatibilité)
    ''' </summary>
    Public Function ObtenirPistesSelectionnees() As List(Of CDAudioManager.CDTrack)
        Dim pistesSelectionnees As New List(Of CDAudioManager.CDTrack)()

        If pistesCD Is Nothing OrElse ListViewCompress.Items.Count = 0 Then
            Return pistesSelectionnees
        End If

        ' Parcourir strictement les pistesCD pour éviter de tronquer les pistes au-delà de ListView
        For i As Integer = 0 To pistesCD.Count - 1
            If i < ListViewCompress.Items.Count AndAlso ListViewCompress.Items(i).Checked Then
                pistesSelectionnees.Add(pistesCD(i))
            End If
        Next

        Return pistesSelectionnees
    End Function

    ''' <summary>
    ''' Coche ou décoche toutes les pistes
    ''' </summary>
    Public Sub CocherToutesPistes(cocher As Boolean)
        For Each item As ListViewItem In ListViewCompress.Items
            item.Checked = cocher
        Next
    End Sub

    ''' <summary>
    ''' Charge le répertoire de sauvegarde depuis les paramètres
    ''' </summary>
    Private Sub ChargerRepertoireSauvegarde()
        If Not String.IsNullOrWhiteSpace(ParametresGlobaux.repertoireExtractionCD) AndAlso
           Directory.Exists(ParametresGlobaux.repertoireExtractionCD) Then
            TextBoxRepSauvegarde.Text = ParametresGlobaux.repertoireExtractionCD
        Else
            ' Par défaut : Musique de l'utilisateur
            TextBoxRepSauvegarde.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            ParametresGlobaux.repertoireExtractionCD = TextBoxRepSauvegarde.Text
            ' Sauvegarder immédiatement dans le fichier parametres.txt
            ParametresGlobauxHelpers.EcrireCleParametres("RepertoireExtractionCD", TextBoxRepSauvegarde.Text)
        End If

        ' Charger le volume d'extraction depuis les paramètres (1-100, défaut 95)
        NumericUpDown_DB.Value = ParametresGlobaux.volumeExtractionCD
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton de sélection du répertoire de sauvegarde
    ''' </summary>
    Private Sub ButtonRepSauvegarde_Click(sender As Object, e As EventArgs)
        Try
            Using folderDialog As New FolderBrowserDialog()
                folderDialog.Description = "Sélectionnez le répertoire de destination pour l'extraction des pistes CD"
                folderDialog.ShowNewFolderButton = True

                ' Utiliser le répertoire actuel comme point de départ
                If Not String.IsNullOrWhiteSpace(TextBoxRepSauvegarde.Text) AndAlso
                   Directory.Exists(TextBoxRepSauvegarde.Text) Then
                    folderDialog.SelectedPath = TextBoxRepSauvegarde.Text
                End If

                If folderDialog.ShowDialog() = DialogResult.OK Then
                    ' Mettre à jour le TextBox
                    TextBoxRepSauvegarde.Text = folderDialog.SelectedPath

                    ' Sauvegarder dans les paramètres globaux
                    ParametresGlobaux.repertoireExtractionCD = folderDialog.SelectedPath

                    ' Sauvegarder immédiatement dans le fichier parametres.txt
                    ParametresGlobauxHelpers.EcrireCleParametres("RepertoireExtractionCD", folderDialog.SelectedPath)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Erreur lors de la sélection du répertoire: {ex.Message}",
                          "Erreur",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    '''<summary>
    ''' Gestionnaire pour sauvegarder le volume d'extraction quand l'utilisateur le modifie
    '''</summary>
    Private Sub NumericUpDown_DB_ValueChanged(sender As Object, e As EventArgs)
        ' Sauvegarder la nouvelle valeur dans les paramètres globaux
        ParametresGlobaux.volumeExtractionCD = CInt(NumericUpDown_DB.Value)

        ' Sauvegarder immédiatement dans le fichier parametres.txt
        ParametresGlobauxHelpers.EcrireCleParametres("VolumeExtractionCD", ParametresGlobaux.volumeExtractionCD.ToString())

        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Volume d'extraction sauvegardé: {ParametresGlobaux.volumeExtractionCD}%")
    End Sub

    ''' <summary>
    ''' Gestionnaire pour valider et rafraîchir le ListView quand le numéro de piste change
    ''' </summary>
    Private Sub TextBoxPremierNumPiste_TextChanged(sender As Object, e As EventArgs)
        ' Valider et rafraîchir uniquement si la valeur est valide ET qu'un CD est chargé
        If pistesCD Is Nothing OrElse pistesCD.Count = 0 OrElse metadonneesCD Is Nothing Then
            Return
        End If

        Dim numero As Integer = 1
        If Not String.IsNullOrWhiteSpace(TextBoxPremierNumPiste.Text) AndAlso
           Integer.TryParse(TextBoxPremierNumPiste.Text, numero) AndAlso
           numero >= 1 Then
            ' Valeur valide, rafraîchir le ListView
            RemplirListViewPistes()
        End If
    End Sub

    ''' <summary>
    ''' Gestionnaire pour valider la saisie (n'accepter que les chiffres et Entrée)
    ''' </summary>
    Private Sub TextBoxPremierNumPiste_KeyPress(sender As Object, e As KeyPressEventArgs)
        ' Autoriser seulement les chiffres, backspace et Entrée
        If Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> ChrW(Keys.Back) AndAlso e.KeyChar <> ChrW(Keys.Enter) Then
            e.Handled = True
        End If

        ' Si l'utilisateur appuie sur Entrée, valider et rafraîchir
        If e.KeyChar = ChrW(Keys.Enter) Then
            e.Handled = True
            Dim numero As Integer = 1
            If String.IsNullOrWhiteSpace(TextBoxPremierNumPiste.Text) OrElse
               Not Integer.TryParse(TextBoxPremierNumPiste.Text, numero) OrElse
               numero < 1 Then
                ' Valeur invalide, remettre à 1
                TextBoxPremierNumPiste.Text = "1"
            End If
            RemplirListViewPistes()
        End If
    End Sub

    ''' <summary>
    ''' Charge dynamiquement tous les lecteurs CD dans le ComboBox
    ''' </summary>
    Private Sub ChargerLecteursCD()
        Try
            ComboBoxChoixLecteur.Items.Clear()

            ' Détecter tous les lecteurs CD-ROM
            Dim lecteurs = CDAudioManager.DetecterLecteursCDAudio()

            If lecteurs.Count = 0 Then
                ' Aucun lecteur CD trouvé
                Dim itemAucun As New LecteurCDItem("", False, "Aucun lecteur CD détecté")
                ComboBoxChoixLecteur.Items.Add(itemAucun)
                ComboBoxChoixLecteur.SelectedIndex = 0
                ComboBoxChoixLecteur.Enabled = False
                Return
            End If

            ' Ajouter chaque lecteur avec son état et ses informations détaillées
            For Each lecteur In lecteurs
                Dim contientCD = CDAudioManager.EstCDAudioPresent(lecteur)
                Dim infoDetaillees = ObtenirInfoLecteur(lecteur)
                Dim item As New LecteurCDItem(lecteur, contientCD, infoDetaillees)
                ComboBoxChoixLecteur.Items.Add(item)

                ' Sélectionner automatiquement le premier lecteur contenant un CD
                If contientCD AndAlso ComboBoxChoixLecteur.SelectedIndex = -1 Then
                    ComboBoxChoixLecteur.SelectedItem = item
                End If
            Next

            ' Si aucun CD n'a été trouvé, sélectionner le premier lecteur
            If ComboBoxChoixLecteur.SelectedIndex = -1 AndAlso ComboBoxChoixLecteur.Items.Count > 0 Then
                ComboBoxChoixLecteur.SelectedIndex = 0
            End If

        Catch ex As Exception
            MessageBox.Show($"Erreur lors du chargement des lecteurs CD: {ex.Message}",
                          "Erreur",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Dessin personnalisé des items du ComboBox
    ''' Les lecteurs sans CD sont en gris pâle, ceux avec CD en noir
    ''' </summary>
    Private Sub ComboBoxChoixLecteur_DrawItem(sender As Object, e As DrawItemEventArgs)
        If e.Index < 0 Then Return

        Dim theme = ThemeManager.GetCurrentTheme()

        ' Dessiner le fond
        Dim backgroundColor As Color
        If (e.State And DrawItemState.Selected) = DrawItemState.Selected Then
            backgroundColor = SystemColors.Highlight
        Else
            backgroundColor = theme.TextBoxBackColor
        End If

        Using brush As New SolidBrush(backgroundColor)
            e.Graphics.FillRectangle(brush, e.Bounds)
        End Using

        Dim item = TryCast(ComboBoxChoixLecteur.Items(e.Index), LecteurCDItem)
        If item IsNot Nothing Then
            ' Couleur selon l'état du lecteur
            Dim textColor As Color
            If (e.State And DrawItemState.Selected) = DrawItemState.Selected Then
                textColor = If(item.ContientCD, SystemColors.HighlightText, Color.Silver)
            Else
                ' Utiliser les couleurs du thème, mais griser les lecteurs vides
                textColor = If(item.ContientCD, theme.TextBoxForeColor, Color.Gray)
            End If

            Using brush As New SolidBrush(textColor)
                e.Graphics.DrawString(item.ToString(), e.Font, brush, e.Bounds)
            End Using
        End If

        e.DrawFocusRectangle()
    End Sub

    ''' <summary>
    ''' Empêche la sélection d'un lecteur sans CD (silencieusement)
    ''' </summary>
    Private previousIndex As Integer = -1
    Private Sub ComboBoxChoixLecteur_SelectedIndexChanged(sender As Object, e As EventArgs)
        Dim item = TryCast(ComboBoxChoixLecteur.SelectedItem, LecteurCDItem)

        If item IsNot Nothing AndAlso Not item.ContientCD Then
            ' Lecteur sans CD sélectionné - empêcher la sélection silencieusement
            If previousIndex >= 0 AndAlso previousIndex < ComboBoxChoixLecteur.Items.Count Then
                ' Revenir à la sélection précédente
                ComboBoxChoixLecteur.SelectedIndex = previousIndex
            Else
                ' Chercher le premier lecteur avec CD
                For i As Integer = 0 To ComboBoxChoixLecteur.Items.Count - 1
                    Dim testItem = TryCast(ComboBoxChoixLecteur.Items(i), LecteurCDItem)
                    If testItem IsNot Nothing AndAlso testItem.ContientCD Then
                        ComboBoxChoixLecteur.SelectedIndex = i
                        Return
                    End If
                Next
            End If
            ' Ne plus afficher de message - juste empêcher la sélection
        Else
            ' Mémoriser la sélection valide
            previousIndex = ComboBoxChoixLecteur.SelectedIndex

            ' Charger les métadonnées du nouveau CD si un lecteur valide est sélectionné
            ' (sauf lors du chargement initial où les données sont déjà fournies)
            If item IsNot Nothing AndAlso item.ContientCD AndAlso Not chargementInitial Then
                ChargerMetadonneesNouveauCD(item.Lecteur)
            End If
        End If
    End Sub

    ''' <summary>
    ''' Charge les métadonnées d'un nouveau CD depuis GnuDB
    ''' </summary>
    Private Async Sub ChargerMetadonneesNouveauCD(lecteur As String)
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ChargerMetadonneesNouveauCD() appelé pour {lecteur}")

            ' Lire les pistes du CD
            Dim pistes = CDAudioManager.LirePistesCD(lecteur)

            If pistes Is Nothing OrElse pistes.Count = 0 Then
                ' Pas de pistes disponibles
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Aucune piste détectée, réinitialisation")
                pistesCD = Nothing
                metadonneesCD = Nothing
                lecteurCD = lecteur
                discIdActuel = Nothing
                Await RemplirInformationsCD()
                Return
            End If

            ' Calculer le DiscID du nouveau CD
            Dim nouveauDiscId = CDMetadataProvider.CalculerDiscID(pistes)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] DiscID détecté: {nouveauDiscId}, DiscID actuel: {If(discIdActuel, "NULL")}")

            ' IMPORTANT: Ne recharger QUE si le DiscID a changé (CD différent)
            If Not String.IsNullOrWhiteSpace(nouveauDiscId) AndAlso
               Not String.IsNullOrWhiteSpace(discIdActuel) AndAlso
               nouveauDiscId = discIdActuel Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⏩ Même CD détecté (DiscID identique), aucun rechargement nécessaire")
                Return
            End If

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Nouveau CD détecté, chargement des métadonnées...")

            ' Mettre à jour le lecteur courant
            lecteurCD = lecteur
            pistesCD = pistes
            discIdActuel = nouveauDiscId

            ' Charger les métadonnées depuis GnuDB
            Dim provider = New GnuDBMetadataProvider()
            metadonneesCD = Await provider.RechercherCD(pistes)

            ' Si les métadonnées n'ont pas été trouvées, essayer le cache
            If metadonneesCD Is Nothing Then
                Dim discId = CDMetadataProvider.CalculerDiscID(pistes)
                If Not String.IsNullOrWhiteSpace(discId) Then
                    metadonneesCD = CDMetadataCache.RecupererMetadonnees(discId)
                End If
            Else
                ' Sauvegarder dans le cache
                Dim discId = CDMetadataProvider.CalculerDiscID(pistes)
                If Not String.IsNullOrWhiteSpace(discId) Then
                    CDMetadataCache.SauvegarderMetadonnees(discId, metadonneesCD, "GnuDB")
                End If
            End If

            ' Appliquer les métadonnées aux pistes si disponibles
            If metadonneesCD IsNot Nothing AndAlso metadonneesCD.Tracks IsNot Nothing Then
                For i = 0 To Math.Min(pistes.Count - 1, metadonneesCD.Tracks.Count - 1)
                    Dim piste = pistes(i)
                    Dim trackInfo = metadonneesCD.Tracks(i)

                    piste.Title = trackInfo.Title
                    piste.Artist = If(Not String.IsNullOrWhiteSpace(trackInfo.Artist), trackInfo.Artist, metadonneesCD.Artist)
                Next
            End If

            ' Remplir le formulaire avec les données (toujours recharger la pochette)
            Await RemplirInformationsCD(chargerPochette:=True)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ CD chargé, pochette rechargée")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur chargement métadonnées pour {lecteur}: {ex.Message}")
            ' En cas d'erreur, vider les données
            pistesCD = Nothing
            metadonneesCD = Nothing
            discIdActuel = Nothing
        End Try

        ' Remplir les informations même en cas d'erreur (en dehors du Catch)
        If metadonneesCD Is Nothing Then
            Await RemplirInformationsCD()
        End If
    End Sub

    ''' <summary>
    ''' Obtient les informations détaillées d'un lecteur CD
    ''' Format: "D: PIONEER BD-RW BDR-211M 1.51 Adapter: 1 ID: 0"
    ''' </summary>
    Private Function ObtenirInfoLecteur(lecteur As String) As String
        Try
            ' Normaliser la lettre du lecteur (enlever les : et \)
            Dim lettreLecteur = lecteur.TrimEnd("\"c, ":"c).ToUpper()

            ' Utiliser WMI pour obtenir les informations détaillées du lecteur
            Try
                Dim searcher As New ManagementObjectSearcher(
                    "SELECT * FROM Win32_CDROMDrive WHERE Drive = '" & lettreLecteur & ":'")

                For Each drive As ManagementObject In searcher.Get()
                    ' Récupérer les informations du lecteur
                    Dim caption As String = If(drive("Caption")?.ToString(), "CD-ROM")
                    Dim scsiTargetId As Object = drive("SCSITargetId")
                    Dim scsiLogicalUnit As Object = drive("SCSILogicalUnit")

                    ' Convertir en chaîne avec gestion des valeurs null
                    Dim targetId As String = If(scsiTargetId IsNot Nothing, scsiTargetId.ToString(), "?")
                    Dim logicalUnit As String = If(scsiLogicalUnit IsNot Nothing, scsiLogicalUnit.ToString(), "?")

                    ' Formater les informations
                    ' Exemple: "D: PIONEER BD-RW BDR-211M 1.51 Adapter: 1 ID: 0"
                    Return $"{lettreLecteur}: {caption}    Adapter: {logicalUnit} ID: {targetId}"
                Next
            Catch wmiEx As Exception
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] WMI échoué pour {lecteur}: {wmiEx.Message}")
            End Try

            ' Si WMI n'a rien retourné, retourner un format simple
            Return $"{lettreLecteur}: CD-ROM Drive"

        Catch ex As Exception
            ' En cas d'erreur, retourner juste la lettre du lecteur
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur obtention info lecteur {lecteur}: {ex.Message}")
            Return lecteur.TrimEnd("\"c, ":"c).ToUpper() & ": CD-ROM Drive"
        End Try
    End Function

    ''' <summary>
    ''' Bouton pour rafraîchir la liste des lecteurs
    ''' </summary>
    Public Sub RafraichirLecteurs()
        ChargerLecteursCD()
    End Sub

    ''' <summary>
    ''' Obtient le lecteur actuellement sélectionné (ou Nothing si pas de CD)
    ''' </summary>
    Public Function ObtenirLecteurSelectionne() As String
        Dim item = TryCast(ComboBoxChoixLecteur.SelectedItem, LecteurCDItem)
        If item IsNot Nothing AndAlso item.ContientCD Then
            Return item.Lecteur
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' Initialise le timer de surveillance des lecteurs CD
    ''' </summary>
    Private Sub InitialiserSurveillanceLecteurs()
        cdMonitorTimer = New Timer With {
            .Interval = 2000 ' Vérifier toutes les 2 secondes
        }
        AddHandler cdMonitorTimer.Tick, AddressOf SurveillerLecteurs
        cdMonitorTimer.Start()
    End Sub

    ''' <summary>
    ''' Surveille les changements d'état des lecteurs CD
    ''' </summary>
    Private Sub SurveillerLecteurs(sender As Object, e As EventArgs)
        Try
            ' Ignorer si le flag est activé (pendant l'initialisation)
            If ignorerChangementsCD Then
                System.Diagnostics.Debug.WriteLine("[FormCompresser] Surveillance CD ignorée (initialisation en cours)")
                Return
            End If

            ' Détecter tous les lecteurs CD-ROM
            Dim lecteurs = CDAudioManager.DetecterLecteursCDAudio()
            Dim changementDetecte As Boolean = False
            Dim lecteurActuelChange As Boolean = False
            Dim lecteurActuel As String = ObtenirLecteurSelectionne()

            ' Vérifier l'état de chaque lecteur
            Dim nouveauxEtats As New Dictionary(Of String, Boolean)
            For Each lecteur In lecteurs
                Dim contientCD = CDAudioManager.EstCDAudioPresent(lecteur)
                nouveauxEtats(lecteur) = contientCD

                ' Vérifier si l'état a changé
                If Not derniersEtatsLecteurs.ContainsKey(lecteur) OrElse
                   derniersEtatsLecteurs(lecteur) <> contientCD Then
                    changementDetecte = True
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Changement détecté pour {lecteur}: CD={contientCD}")

                    ' Vérifier si c'est le lecteur actuellement sélectionné
                    If lecteurActuel IsNot Nothing AndAlso lecteur.TrimEnd(":"c).ToUpper() = lecteurActuel.TrimEnd(":"c).ToUpper() Then
                        lecteurActuelChange = True

                        ' Si le CD a été ÉJECTÉ du lecteur actuel, réinitialiser discIdActuel
                        If Not contientCD Then
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ CD éjecté du lecteur {lecteur}, réinitialisation de discIdActuel")
                            discIdActuel = Nothing
                            pistesCD = Nothing
                            metadonneesCD = Nothing
                        End If
                    End If
                End If
            Next

            ' Si un changement est détecté, rafraîchir le ComboBox
            If changementDetecte Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] *** CHANGEMENT DÉTECTÉ ***")
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]   lecteurActuelChange = {lecteurActuelChange}")
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]   lecteurActuel = {If(lecteurActuel, "NULL")}")

                Me.Invoke(Sub()
                              Dim lecteurSelectionne = ObtenirLecteurSelectionne()
                              System.Diagnostics.Debug.WriteLine($"[FormCompresser]   lecteurSelectionne (avant ChargerLecteursCD) = {If(lecteurSelectionne, "NULL")}")

                              ChargerLecteursCD()

                              ' Si c'est le lecteur actuellement sélectionné qui a changé
                              If lecteurActuelChange Then
                                  System.Diagnostics.Debug.WriteLine($"[FormCompresser]   Lecteur actuel a changé, recherche du lecteur...")

                                  ' Rechercher le lecteur avec un CD
                                  For i As Integer = 0 To ComboBoxChoixLecteur.Items.Count - 1
                                      Dim item = TryCast(ComboBoxChoixLecteur.Items(i), LecteurCDItem)
                                      If item IsNot Nothing Then
                                          System.Diagnostics.Debug.WriteLine($"[FormCompresser]     Item {i}: Lecteur={item.Lecteur}, ContientCD={item.ContientCD}")

                                          ' Comparer avec le lecteur qui a changé
                                          If lecteurActuel IsNot Nothing AndAlso
                                             item.Lecteur.TrimEnd(":"c).ToUpper() = lecteurActuel.TrimEnd(":"c).ToUpper() AndAlso
                                             item.ContientCD Then
                                              ' Forcer le rechargement même si c'est le même index
                                              System.Diagnostics.Debug.WriteLine($"[FormCompresser]   ✓ Rechargement du lecteur {item.Lecteur}")
                                              ChargerMetadonneesNouveauCD(item.Lecteur)
                                              ComboBoxChoixLecteur.SelectedIndex = i
                                              Exit For
                                          End If
                                      End If
                                  Next
                              ElseIf lecteurSelectionne IsNot Nothing Then
                                  System.Diagnostics.Debug.WriteLine($"[FormCompresser]   Autre lecteur a changé, tentative de resélection...")

                                  ' Essayer de resélectionner le même lecteur s'il contient toujours un CD
                                  For i As Integer = 0 To ComboBoxChoixLecteur.Items.Count - 1
                                      Dim item = TryCast(ComboBoxChoixLecteur.Items(i), LecteurCDItem)
                                      If item IsNot Nothing AndAlso item.Lecteur = lecteurSelectionne AndAlso item.ContientCD Then
                                          ComboBoxChoixLecteur.SelectedIndex = i
                                          Exit For
                                      End If
                                  Next
                              Else
                                  System.Diagnostics.Debug.WriteLine($"[FormCompresser]   Aucun lecteur actuellement sélectionné, tentative de sélection automatique...")

                                  ' Aucun lecteur sélectionné, essayer de sélectionner le premier lecteur avec un CD
                                  For i As Integer = 0 To ComboBoxChoixLecteur.Items.Count - 1
                                      Dim item = TryCast(ComboBoxChoixLecteur.Items(i), LecteurCDItem)
                                      If item IsNot Nothing AndAlso item.ContientCD Then
                                          System.Diagnostics.Debug.WriteLine($"[FormCompresser]   ✓ Sélection automatique du lecteur {item.Lecteur}")
                                          ChargerMetadonneesNouveauCD(item.Lecteur)
                                          ComboBoxChoixLecteur.SelectedIndex = i
                                          Exit For
                                      End If
                                  Next
                              End If
                          End Sub)
            End If

            ' Mettre à jour les derniers états
            derniersEtatsLecteurs = nouveauxEtats

        Catch ex As Exception
            ' Ignorer les erreurs de surveillance pour ne pas perturber l'utilisateur
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur surveillance lecteurs: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Nettoyer le timer à la fermeture
    ''' </summary>
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If cdMonitorTimer IsNot Nothing Then
            cdMonitorTimer.Stop()
            RemoveHandler cdMonitorTimer.Tick, AddressOf SurveillerLecteurs
            cdMonitorTimer.Dispose()
            cdMonitorTimer = Nothing
        End If
        MyBase.OnFormClosing(e)
    End Sub

    ''' <summary>
    ''' Verrouille le tiroir du lecteur CD pour empêcher l'éjection manuelle
    ''' </summary>
    Private Sub VerrouillerCD(lecteur As String)
        Try
            Dim lettre As String = lecteur.TrimEnd(":"c).ToUpper()
            mciSendString($"open {lettre}: type cdaudio alias cd{lettre}", Nothing, 0, IntPtr.Zero)
            mciSendString($"set cd{lettre} door locked", Nothing, 0, IntPtr.Zero)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Lecteur {lettre}: verrouillé")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur verrouillage CD: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Déverrouille le tiroir du lecteur CD
    ''' </summary>
    Private Sub DeverrouillerCD(lecteur As String)
        Try
            Dim lettre As String = lecteur.TrimEnd(":"c).ToUpper()
            mciSendString($"set cd{lettre} door unlocked", Nothing, 0, IntPtr.Zero)
            mciSendString($"close cd{lettre}", Nothing, 0, IntPtr.Zero)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Lecteur {lettre}: déverrouillé")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur déverrouillage CD: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Éjecte le CD du lecteur
    ''' </summary>
    Private Sub EjecterCD(lecteur As String)
        Try
            Dim lettre As String = lecteur.TrimEnd(":"c).ToUpper()
            mciSendString($"open {lettre}: type cdaudio alias cd{lettre}", Nothing, 0, IntPtr.Zero)
            mciSendString($"set cd{lettre} door open", Nothing, 0, IntPtr.Zero)
            mciSendString($"close cd{lettre}", Nothing, 0, IntPtr.Zero)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Lecteur {lettre}: éjecté")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur éjection CD: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Réinitialise les champs de métadonnées du CD
    ''' </summary>
    Private Sub ReinitialiserMetadonneesCD()
        Try
            If TextBoxCDTitre IsNot Nothing Then
                TextBoxCDTitre.Text = ""
            End If
            If TextBoxCDArtiste IsNot Nothing Then
                TextBoxCDArtiste.Text = ""
            End If
            If TextBoxAnnee IsNot Nothing Then
                TextBoxAnnee.Text = ""
            End If
            If ComboBoxGenre IsNot Nothing Then
                ComboBoxGenre.SelectedIndex = -1  ' Aucune sélection
            End If
            If PictureBoxPochette IsNot Nothing Then
                PictureBoxPochette.Image = Nothing
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⚠️ IMAGE EFFACÉE dans ReinitialiserMetadonneesCD()")
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Stack: {New System.Diagnostics.StackTrace(True).GetFrame(1)?.GetMethod()?.Name}")
            End If
            If Label_DimImage IsNot Nothing Then
                Label_DimImage.Text = ""
            End If
            If LabelTailleImage IsNot Nothing Then
                LabelTailleImage.Text = ""
            End If

            ' Réinitialiser l'historique des pochettes
            historiquePochettes.Clear()
            indexPochetteActuelle = -1

            If ListViewCompress IsNot Nothing Then
                ListViewCompress.Items.Clear()  ' Effacer la liste des pistes
            End If
            If CheckBox_FCompress_SelectDeselect IsNot Nothing Then
                CheckBox_FCompress_SelectDeselect.Checked = True  ' Recocher par défaut
            End If

            ' Vider le cache d'analyse des pistes pour forcer une nouvelle analyse
            analysesPistes.Clear()

            ' IMPORTANT: Vider aussi pistesCD, metadonneesCD ET discIdActuel pour éviter un rechargement après éjection
            ' Ceci permet de traiter la réinsertion du même CD comme un nouveau CD
            pistesCD = Nothing
            metadonneesCD = Nothing
            discIdActuel = Nothing

            System.Diagnostics.Debug.WriteLine("[FormCompresser] Métadonnées CD, pistesCD, metadonneesCD, discIdActuel, liste des pistes, checkbox et cache d'analyse réinitialisés")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur réinitialisation métadonnées: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton Extraire
    ''' </summary>
    Private Async Sub ButtonExtraire_Click(sender As Object, e As EventArgs) Handles ButtonExtraire.Click
        Try
            ' Vérifier qu'un lecteur est sélectionné
            If String.IsNullOrWhiteSpace(lecteurCD) Then
                MessageBox.Show("Veuillez sélectionner un lecteur CD contenant un disque audio.",
                              "Aucun lecteur sélectionné",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
                Return
            End If

            ' Vérifier qu'il y a des pistes à extraire
            Dim indicesPistes = ObtenirIndicesPistesSelectionnees()
            If indicesPistes.Count = 0 Then
                MessageBox.Show("Veuillez sélectionner au moins une piste à extraire.",
                              "Aucune piste sélectionnée",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
                Return
            End If

            ' Vérifier qu'un répertoire de destination est défini
            If String.IsNullOrWhiteSpace(TextBoxRepSauvegarde.Text) OrElse
               Not Directory.Exists(TextBoxRepSauvegarde.Text) Then
                MessageBox.Show("Veuillez sélectionner un répertoire de destination valide.",
                              "Répertoire invalide",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
                Return
            End If

            ' Verrouiller le CD si l'option est activée
            If CheckBoxVerouillerCD.Checked Then
                VerrouillerCD(lecteurCD)
            End If

            ' Désactiver les contrôles pendant l'extraction
            ButtonExtraire.Enabled = False
            ButtonExtraire.Visible = False
            ButtonAnnuler.Visible = True
            ButtonAnnuler.Enabled = True
            ButtonQuitter.Enabled = False
            annulationDemandee = False ' Réinitialiser le flag

            ' Désactiver TopMost pour permettre à l'utilisateur de basculer vers Form1 s'il le souhaite
            ' FormCompresser reste visible mais n'est plus forcé au premier plan
            Me.TopMost = False
            System.Diagnostics.Debug.WriteLine("[FormCompresser] TopMost désactivé - Form1 utilisable pendant l'extraction")

            ' ═══ NOUVELLE STRATÉGIE : PAS D'ANALYSE EN BATCH ═══
            ' L'analyse sera faite individuellement pour chaque piste juste avant son extraction
            ' Ceci permet un réajustement plus précis et évite les problèmes de chansons incomplètes
            analysesPistes.Clear()
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🔍 Mode analyse individuelle : chaque piste sera analysée juste avant extraction")

            ' IMPORTANT: Mettre à jour l'URL de la pochette dans les métadonnées avec l'image actuellement affichée
            ' (qui peut être différente de l'URL initiale si l'utilisateur a navigué avec Prec/Suiv)
            If metadonneesCD IsNot Nothing AndAlso pochetteTempUrl IsNot Nothing Then
                metadonneesCD.CoverArtUrl = pochetteTempUrl
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] CoverArtUrl mis à jour: {pochetteTempUrl}")
            End If

            ' IMPORTANT: Sauvegarder les métadonnées (incluant l'URL de la pochette) dans le cache
            ' Ceci n'est fait qu'après le clic sur ButtonExtraire, pas lors de l'affichage initial
            If metadonneesCD IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(metadonneesCD.CoverArtUrl) Then
                Dim discId = CDMetadataProvider.CalculerDiscID(pistesCD)
                If Not String.IsNullOrWhiteSpace(discId) Then
                    CDMetadataCache.SauvegarderMetadonnees(discId, metadonneesCD, "GnuDB+MusicBrainz")
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Métadonnées avec URL pochette sauvegardées dans le cache")
                End If
            End If

            ' IMPORTANT: Sauvegarder l'image de la pochette dans le cache d'images
            ' Ceci n'est fait qu'après le clic sur ButtonExtraire, pas lors du téléchargement initial
            If pochetteTempUrl IsNot Nothing AndAlso pochetteTempBytes IsNot Nothing Then
                Try
                    CoverCacheManager.SauvegarderImage(pochetteTempUrl, pochetteTempBytes)
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Image pochette ({pochetteTempBytes.Length} bytes) sauvegardée dans le cache")

                    ' NOUVEAU: Sauvegarder aussi dans le répertoire d'extraction avec nom formaté
                    SauvegarderPochetteDansRepertoire(pochetteTempBytes)

                    ' Vider le cache mémoire temporaire maintenant que l'image est sauvegardée sur disque
                    cachePochettesBytes.Clear()
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Cache mémoire des images vidé après sauvegarde")

                    ' Nettoyer l'historique de navigation : ne garder que la pochette actuellement affichée
                    If historiquePochettes.Count > 0 AndAlso indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count Then
                        Dim pochetteChoisie = historiquePochettes(indexPochetteActuelle)
                        historiquePochettes.Clear()
                        historiquePochettes.Add(pochetteChoisie)
                        indexPochetteActuelle = 0
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Historique nettoyé - seule la pochette choisie conservée: {pochetteChoisie}")
                    Else
                        ' Sinon, vider complètement l'historique
                        historiquePochettes.Clear()
                        indexPochetteActuelle = -1
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Historique complètement vidé")
                    End If

                    ' Mettre à jour les boutons de navigation
                    MettreAJourBoutonsNavigation()

                Catch ex As Exception
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur sauvegarde image cache: {ex.Message}")
                End Try
            End If

            ' Maintenant afficher les barres de progression globale pour l'extraction
            LabelProgressionGlobale.Visible = True
            ProgressBarGlobale.Visible = True
            ProgressBarGlobale.Maximum = indicesPistes.Count
            ProgressBarGlobale.Value = 0
            LabelProgressionGlobale.Text = String.Format(LanguageManager.GetString("Compressor_GlobalProgress"), 0, indicesPistes.Count)

            ' Afficher aussi la progression individuelle par piste
            LabelPisteEnCours.Visible = True
            ProgressBarPisteActuelle.Visible = True
            ProgressBarPisteActuelle.Value = 0

            Try
                ' Extraire les pistes sélectionnées
                Dim pistesReussies As Integer = 0
                Dim pistesEchouees As Integer = 0
                Dim pisteNumero As Integer = 0

                For Each index In indicesPistes
                    pisteNumero += 1

                    ' ═══ VÉRIFIER SI L'ANNULATION A ÉTÉ DEMANDÉE ═══
                    If annulationDemandee Then
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⚠️ Extraction annulée après {pistesReussies} piste(s)")
                        Exit For
                    End If

                    Try
                        ' Mettre à jour l'affichage de la piste en cours
                        Dim item = ListViewCompress.Items(index)
                        Dim titre As String = If(item.SubItems.Count > 1, item.SubItems(1).Text, $"Piste {item.Text}")
                        Dim artiste As String = If(item.SubItems.Count > 2, item.SubItems(2).Text, TextBoxCDArtiste.Text)

                        LabelPisteEnCours.Text = $"{artiste} - {titre}"
                        ProgressBarPisteActuelle.Value = 0
                        Application.DoEvents()

                        ' Extraire la piste
                        Await ExtrairePiste(index)
                        pistesReussies += 1

                        ' Marquer la piste comme complétée
                        ProgressBarPisteActuelle.Value = ProgressBarPisteActuelle.Maximum

                    Catch ex As Exception
                        pistesEchouees += 1
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur extraction piste {index + 1}: {ex.Message}")
                    End Try

                    ' Mettre à jour la progression globale
                    ProgressBarGlobale.Value = pisteNumero
                    LabelProgressionGlobale.Text = String.Format(LanguageManager.GetString("Compressor_GlobalProgress"), pisteNumero, indicesPistes.Count)
                    Application.DoEvents()
                Next

                ' Masquer les barres de progression
                LabelPisteEnCours.Visible = False
                ProgressBarPisteActuelle.Visible = False
                LabelProgressionGlobale.Visible = False
                ProgressBarGlobale.Visible = False

                ' Afficher le résultat
                Dim message As String
                Dim titreMessage As String
                Dim icone As MessageBoxIcon

                If annulationDemandee Then
                    ' Extraction annulée par l'utilisateur
                    message = String.Format(LanguageManager.GetString("Compressor_ExtractionCancelledMessage"), pistesReussies)
                    titreMessage = LanguageManager.GetString("Compressor_ExtractionCancelledTitle")
                    icone = MessageBoxIcon.Warning
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Extraction annulée: {pistesReussies} pistes extraites")
                Else
                    ' Extraction complète
                    message = String.Format(LanguageManager.GetString("Compressor_ExtractionSuccessMessage"), pistesReussies)
                    If pistesEchouees > 0 Then
                        message &= vbCrLf & String.Format(LanguageManager.GetString("Compressor_ExtractionFailedMessage"), pistesEchouees)
                    End If
                    titreMessage = LanguageManager.GetString("Compressor_ExtractionCompletedTitle")
                    icone = If(pistesEchouees = 0, MessageBoxIcon.Information, MessageBoxIcon.Warning)
                End If

                ' Remettre FormCompresser au premier plan pour afficher le message de fin
                Me.TopMost = True
                Me.BringToFront()
                System.Diagnostics.Debug.WriteLine("[FormCompresser] FormCompresser ramené au premier plan pour message de fin")

                MessageBox.Show(Me, message, titreMessage, MessageBoxButtons.OK, icone)

                ' Garder TopMost activé après le message pour que FormCompresser reste au-dessus
                ' L'utilisateur pourra toujours cliquer sur Form1 pour la ramener au premier plan si désiré
                System.Diagnostics.Debug.WriteLine("[FormCompresser] TopMost maintenu après message de fin")

            Finally
                ' Masquer les barres de progression (au cas où)
                LabelPisteEnCours.Visible = False
                ProgressBarPisteActuelle.Visible = False
                LabelProgressionGlobale.Visible = False
                ProgressBarGlobale.Visible = False

                ' Déverrouiller le CD si l'option était activée
                If CheckBoxVerouillerCD.Checked Then
                    DeverrouillerCD(lecteurCD)
                End If

                ' Éjecter le CD si l'option est activée
                If CheckBoxEjectCD.Checked Then
                    EjecterCD(lecteurCD)
                    ' Réinitialiser les métadonnées après éjection
                    ReinitialiserMetadonneesCD()
                End If

                ' Réactiver les contrôles
                ButtonExtraire.Visible = True
                ButtonExtraire.Enabled = True
                ButtonAnnuler.Visible = False
                ButtonAnnuler.Enabled = False
                ButtonQuitter.Enabled = True
                annulationDemandee = False ' Réinitialiser le flag
            End Try

        Catch ex As Exception
            ' Remettre FormCompresser au premier plan en cas d'erreur
            Me.TopMost = True
            Me.BringToFront()

            MessageBox.Show(Me, $"Erreur lors de l'extraction : {ex.Message}",
                          "Erreur",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)

            ' Garder TopMost activé après le message d'erreur
            System.Diagnostics.Debug.WriteLine("[FormCompresser] TopMost maintenu après message d'erreur")
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire du clic sur le bouton Annuler
    ''' </summary>
    Private Sub ButtonAnnuler_Click(sender As Object, e As EventArgs) Handles ButtonAnnuler.Click
        ' Vérifier si une extraction est en cours
        If Not annulationDemandee Then
            Dim resultat = MessageBox.Show(LanguageManager.GetString("Compressor_CancelConfirmMessage"),
                                          LanguageManager.GetString("Compressor_CancelConfirmTitle"),
                                          MessageBoxButtons.YesNo,
                                          MessageBoxIcon.Question)

            If resultat = DialogResult.Yes Then
                annulationDemandee = True
                LabelPisteEnCours.Text = LanguageManager.GetString("Compressor_CancelInProgress")
                ButtonAnnuler.Enabled = False
                System.Diagnostics.Debug.WriteLine("[FormCompresser] ⚠️ Annulation demandée par l'utilisateur")
            End If
        End If
    End Sub

    ''' <summary>
    ''' Extrait une piste CD vers un fichier audio avec métadonnées
    ''' </summary>
    Private Async Function ExtrairePiste(pisteIndex As Integer) As Task
        If pistesCD Is Nothing OrElse pisteIndex < 0 OrElse pisteIndex >= pistesCD.Count Then
            Throw New ArgumentException("Index de piste invalide")
        End If

        Dim piste = pistesCD(pisteIndex)
        Dim item = ListViewCompress.Items(pisteIndex)

        ' Obtenir le numéro de piste depuis le ListView (qui peut être différent du numéro original)
        Dim numeroFichier As String = item.Text

        ' Obtenir le titre et l'artiste depuis le ListView
        Dim titre As String = If(item.SubItems.Count > 1, item.SubItems(1).Text, $"Piste {numeroFichier}")
        Dim artiste As String = If(item.SubItems.Count > 2, item.SubItems(2).Text, TextBoxCDArtiste.Text)

        ' Créer le répertoire de l'album: "(Année) Artiste - Album"
        Dim annee As String = TextBoxAnnee.Text.Trim()
        Dim artisteAlbum As String = TextBoxCDArtiste.Text.Trim()
        Dim nomAlbum As String = TextBoxCDTitre.Text.Trim()

        Dim nomRepertoireAlbum As String = ""
        If Not String.IsNullOrEmpty(annee) Then
            nomRepertoireAlbum = $"({annee}) "
        End If
        nomRepertoireAlbum &= $"{artisteAlbum} - {nomAlbum}"
        nomRepertoireAlbum = NettoyerNomFichier(nomRepertoireAlbum)

        Dim cheminRepertoireAlbum As String = Path.Combine(TextBoxRepSauvegarde.Text, nomRepertoireAlbum)

        ' Créer le répertoire s'il n'existe pas
        If Not Directory.Exists(cheminRepertoireAlbum) Then
            Directory.CreateDirectory(cheminRepertoireAlbum)
        End If

        ' Construire le nom de fichier: "NN - Artiste - Titre"
        ' Formater le numéro avec zéro initial (01, 02, ..., 10, 11, ...)
        Dim numeroFormate As String = Integer.Parse(numeroFichier).ToString("D2")
        Dim nomFichier As String = NettoyerNomFichier($"{numeroFormate} - {artiste} - {titre}")

        ' Obtenir le format et l'extension
        Dim format As String = If(ComboBoxTypeConversion.SelectedItem?.ToString(), "MP3").ToUpper()
        Dim extension As String = "." & format.ToLower()

        Dim cheminComplet As String = Path.Combine(cheminRepertoireAlbum, nomFichier & extension)

        ' ═══ ANALYSER LA PISTE INDIVIDUELLEMENT JUSTE AVANT EXTRACTION ═══
        Dim pisteAExtraire As CDAudioManager.CDTrack = piste

        If ParametresGlobaux.ModeTOCPrecis Then
            ' Mode TOC Précis : utiliser les positions TOC exactes sans modification
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] 📍 Extraction piste {numeroFichier} avec positions TOC EXACTES: {piste.StartFrame}-{piste.EndFrame}")
        Else
            ' Mode Normal : TOUJOURS analyser individuellement chaque piste
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🔍 Analyse individuelle de la piste {numeroFichier} avant extraction...")

            ' Trouver la piste suivante pour analyser la ZONE DE TRANSITION
            Dim pisteSuivante As CDAudioManager.CDTrack = Nothing
            If pisteIndex + 1 < pistesCD.Count Then
                pisteSuivante = pistesCD(pisteIndex + 1)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    └─ Analyse de la transition avec piste {pisteSuivante.TrackNumber}")
            End If

            ' ANALYSE INDÉPENDANTE : Toujours analyser le début/fin de la piste en cours
            ' sans utiliser l'analyse précédente afin que l'extraction d'une piste isolée
            ' (ex. extraire uniquement la piste 8) reste correcte.
            Dim analyse = CDAudioAnalyzer.AnalyzeTrack(piste, pisteSuivante, Nothing)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser]    └─ {analyse.AnalysisMessage}")

            ' Stocker l'analyse pour la piste suivante
            analysesPistes(pisteIndex) = analyse

            ' GARDE ANTI-CHEVREMENT : si l'analyse produit des positions ajustées qui chevauchent
            ' la piste suivante (cas où la détection du silence réduit trop la fin),
            ' on pourra corriger au moment d'analyser la piste suivante. Ici on ajoute un log.
            If analyse.WasAdjusted Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⚠️ Vérifier chevauchement potentiel après ajustement: piste {numeroFichier} => {analyse.AdjustedStartFrame}-{analyse.AdjustedEndFrame}")
            End If

            If analyse.WasAdjusted Then
                ' Créer une nouvelle piste avec les positions ajustées
                pisteAExtraire = New CDAudioManager.CDTrack With {
                    .Drive = piste.Drive,
                    .TrackNumber = piste.TrackNumber,
                    .Title = piste.Title,
                    .Artist = piste.Artist,
                    .StartFrame = analyse.AdjustedStartFrame,
                    .EndFrame = analyse.AdjustedEndFrame,
                    .Duration = TimeSpan.FromSeconds((analyse.AdjustedEndFrame - analyse.AdjustedStartFrame) / 75.0)
                }
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✅ Extraction avec positions AJUSTÉES: {analyse.AdjustedStartFrame}-{analyse.AdjustedEndFrame}")
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    └─ Début: +{analyse.TrimmedStartFrames / 75.0:F2}s ({analyse.TrimmedStartFrames} frames)")
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    └─ Fin: -{analyse.TrimmedEndFrames / 75.0:F2}s ({analyse.TrimmedEndFrames} frames)")
            Else
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ℹ️ Extraction avec positions TOC (pas d'ajustement nécessaire)")
            End If
        End If

        ' Créer le WaveStream pour lire le CD
        Using cdReader = CDAudioManager.CreerLecteurCDAudio(pisteAExtraire)
            If cdReader Is Nothing Then
                Throw New Exception($"Impossible de créer le lecteur pour la piste {numeroFichier}")
            End If

            ' Extraire selon le format choisi
            Select Case format
                Case "MP3"
                    Await ExtraireMp3(cdReader, cheminComplet, titre, artiste, numeroFichier)
                Case "WAV"
                    Await ExtraireWav(cdReader, cheminComplet, titre, artiste, numeroFichier)
                Case "FLAC"
                    Await ExtraireFlac(cdReader, cheminComplet, titre, artiste, numeroFichier)
                Case "WMA"
                    Await ExtraireWma(cdReader, cheminComplet, titre, artiste, numeroFichier)
                Case Else
                    ' Par défaut, WAV
                    Await ExtraireWav(cdReader, cheminComplet, titre, artiste, numeroFichier)
            End Select
        End Using

        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Piste {numeroFichier} extraite: {cheminComplet}")
    End Function

    ''' <summary>
    ''' Nettoie un nom de fichier en supprimant les caractères invalides
    ''' </summary>
    Private Function NettoyerNomFichier(nom As String) As String
        Dim invalides = Path.GetInvalidFileNameChars()
        For Each c In invalides
            nom = nom.Replace(c, "_"c)
        Next
        Return nom.Trim()
    End Function

    ''' <summary>
    ''' Copie un WaveStream avec rapport de progression
    ''' </summary>
    Private Sub CopierAvecProgression(source As NAudio.Wave.WaveStream, destination As System.IO.Stream)
        Const bufferSize As Integer = 32768 ' 32 KB
        Dim buffer(bufferSize - 1) As Byte
        Dim totalLength As Long = source.Length
        Dim totalRead As Long = 0
        Dim lastProgressUpdate As Integer = 0

        ' Initialiser la barre de progression de la piste
        If Me.InvokeRequired Then
            Me.Invoke(Sub()
                          ProgressBarPisteActuelle.Maximum = 100
                          ProgressBarPisteActuelle.Value = 0
                      End Sub)
        Else
            ProgressBarPisteActuelle.Maximum = 100
            ProgressBarPisteActuelle.Value = 0
        End If

        Dim bytesRead As Integer
        Do
            bytesRead = source.Read(buffer, 0, bufferSize)
            If bytesRead = 0 Then Exit Do

            destination.Write(buffer, 0, bytesRead)
            totalRead += bytesRead

            ' Calculer le pourcentage
            Dim progressPercent As Integer = CInt((totalRead * 100) / totalLength)

            ' Mettre à jour la barre de progression (seulement si le pourcentage a changé)
            If progressPercent <> lastProgressUpdate AndAlso progressPercent <= 100 Then
                lastProgressUpdate = progressPercent
                Try
                    If Me.InvokeRequired Then
                        Me.Invoke(Sub()
                                      If ProgressBarPisteActuelle.Value < progressPercent Then
                                          ProgressBarPisteActuelle.Value = progressPercent
                                      End If
                                  End Sub)
                    Else
                        If ProgressBarPisteActuelle.Value < progressPercent Then
                            ProgressBarPisteActuelle.Value = progressPercent
                        End If
                    End If
                Catch
                    ' Ignorer les erreurs d'invocation (formulaire peut être fermé)
                End Try
            End If
        Loop
    End Sub

    ''' <summary>
    ''' Crée un WaveStream avec ajustement de volume basé sur NumericUpDown_DB (1-100, défaut 95)
    ''' </summary>
    Private Function AppliquerAjustementVolume(source As NAudio.Wave.WaveStream) As NAudio.Wave.WaveStream
        Try
            ' Récupérer la valeur du volume (1-100, défaut 95)
            Dim volumePercent As Decimal = 95D ' Valeur par défaut sécuritaire

            If Me.InvokeRequired Then
                Me.Invoke(Sub() volumePercent = NumericUpDown_DB.Value)
            Else
                volumePercent = NumericUpDown_DB.Value
            End If

            ' Si le volume est à 95% ou plus, pas besoin d'ajustement (tolérance pour éviter les problèmes)
            If volumePercent >= 95D Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Volume à {volumePercent}%, pas d'ajustement nécessaire")
                Return source
            End If

            ' Convertir en multiplicateur (95 = 0.95, 100 = 1.0, 50 = 0.5, etc.)
            Dim volumeMultiplier As Single = CSng(volumePercent / 100D)

            ' Créer un WaveChannel32 pour ajuster le volume
            ' IMPORTANT: WaveChannel32 préserve Position et Length correctement
            Dim volumeStream As New NAudio.Wave.WaveChannel32(source) With {
                .Volume = volumeMultiplier,
                .PadWithZeroes = False
            }

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Ajustement volume: {volumePercent}% (multiplicateur: {volumeMultiplier})")

            ' Retourner comme WaveStream
            Return volumeStream

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur ajustement volume: {ex.Message}")
            ' En cas d'erreur, retourner la source originale sans modification
            Return source
        End Try
    End Function

    ''' <summary>
    ''' Extrait une piste en MP3
    ''' </summary>
    Private Async Function ExtraireMp3(source As NAudio.Wave.WaveStream, cheminFichier As String,
                                       titre As String, artiste As String, numeroPiste As String) As Task
        ' Capturer toutes les valeurs UI AVANT Task.Run pour éviter les erreurs inter-threads
        Dim qualiteIndex As Integer = ComboBoxQualiteConversion.SelectedIndex
        Dim album As String = TextBoxCDTitre.Text
        Dim artisteAlbum As String = TextBoxCDArtiste.Text
        Dim annee As String = TextBoxAnnee.Text
        Dim genre As String = If(ComboBoxGenre.SelectedItem?.ToString(), "")
        Dim commentaire As String = TextBoxCommentaire.Text
        Dim pochette As Image = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)

        ' Index 0=128, 1=192, 2=256, 3=320
        Dim bitrate As Integer = 320 ' Par défaut

        Select Case qualiteIndex
            Case 0 ' Basse (128 kbps)
                bitrate = 128
            Case 1 ' Moyenne (192 kbps)
                bitrate = 192
            Case 2 ' Haute (256 kbps)
                bitrate = 256
            Case 3 ' Très haute (320 kbps)
                bitrate = 320
            Case Else ' Fallback
                bitrate = 320
        End Select

        ' Appliquer l'ajustement de volume
        Dim sourceAvecVolume = AppliquerAjustementVolume(source)

        Try
            Await Task.Run(Sub()
                               Using writer As New NAudio.Lame.LameMP3FileWriter(cheminFichier, sourceAvecVolume.WaveFormat, bitrate)
                                   ' Copier les données audio avec progression
                                   CopierAvecProgression(sourceAvecVolume, writer)
                               End Using

                               ' Écrire les métadonnées avec TagLib
                               EcrireMetadonnees(cheminFichier, titre, artiste, numeroPiste,
                                               album, artisteAlbum, annee, genre, commentaire, pochette, TextBoxNumCD.Text)
                           End Sub)
        Finally
            ' Disposer le stream de volume si c'est un wrapper différent de la source
            If sourceAvecVolume IsNot source Then
                sourceAvecVolume?.Dispose()
            End If

            ' Libérer la copie de l'image
            pochette?.Dispose()
        End Try
    End Function

    ''' <summary>
    ''' Extrait une piste en WAV avec métadonnées
    ''' </summary>
    Private Async Function ExtraireWav(source As NAudio.Wave.WaveStream, cheminFichier As String,
                                       titre As String, artiste As String, numeroPiste As String) As Task
        ' Capturer toutes les valeurs UI AVANT Task.Run
        Dim qualiteIndex As Integer = ComboBoxQualiteConversion.SelectedIndex
        Dim album As String = TextBoxCDTitre.Text
        Dim artisteAlbum As String = TextBoxCDArtiste.Text
        Dim annee As String = TextBoxAnnee.Text
        Dim genre As String = If(ComboBoxGenre.SelectedItem?.ToString(), "")
        Dim commentaire As String = TextBoxCommentaire.Text
        Dim pochette As Image = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)

        ' Déterminer le format de sortie selon l'index de qualité
        ' Index 0=16-bit 44.1kHz, 1=24-bit 96kHz, 2=32-bit 192kHz
        Dim sampleRate As Integer = 96000 ' Par défaut
        Dim bitDepth As Integer = 24

        Select Case qualiteIndex
            Case 0 ' PCM 16-bit 44.1 kHz
                sampleRate = 44100
                bitDepth = 16
            Case 1 ' PCM 24-bit 96 kHz
                sampleRate = 96000
                bitDepth = 24
            Case 2 ' PCM 32-bit 192 kHz
                sampleRate = 192000
                bitDepth = 32
            Case Else ' Fallback
                sampleRate = 96000
                bitDepth = 24
        End Select

        ' Appliquer l'ajustement de volume
        Dim sourceAvecVolume = AppliquerAjustementVolume(source)

        Try
            Await Task.Run(Sub()
                               ' Si le format source est déjà le format cible (CD audio standard), pas de conversion
                               If sourceAvecVolume.WaveFormat.SampleRate = sampleRate AndAlso sourceAvecVolume.WaveFormat.BitsPerSample = bitDepth Then
                                   Using writer As New NAudio.Wave.WaveFileWriter(cheminFichier, sourceAvecVolume.WaveFormat)
                                       CopierAvecProgression(sourceAvecVolume, writer)
                                   End Using
                               Else
                                   ' Créer le format cible
                                   Dim targetFormat As New NAudio.Wave.WaveFormat(sampleRate, bitDepth, sourceAvecVolume.WaveFormat.Channels)

                                   ' Utiliser un resampler pour convertir
                                   Using resampler As New NAudio.Wave.MediaFoundationResampler(sourceAvecVolume, targetFormat)
                                       resampler.ResamplerQuality = 60 ' Haute qualité
                                       Using writer As New NAudio.Wave.WaveFileWriter(cheminFichier, targetFormat)
                                           ' Copier manuellement depuis le resampler
                                           Const bufferSize As Integer = 32768
                                           Dim buffer(bufferSize - 1) As Byte
                                           Dim bytesRead As Integer

                                           ' Estimer la longueur totale (approximation basée sur le ratio des sample rates)
                                           Dim estimatedLength As Long = CLng(source.Length * (sampleRate / source.WaveFormat.SampleRate))
                                           Dim totalRead As Long = 0
                                           Dim lastProgressUpdate As Integer = 0

                                           If Me.InvokeRequired Then
                                               Me.Invoke(Sub()
                                                             ProgressBarPisteActuelle.Maximum = 100
                                                             ProgressBarPisteActuelle.Value = 0
                                                         End Sub)
                                           Else
                                               ProgressBarPisteActuelle.Maximum = 100
                                               ProgressBarPisteActuelle.Value = 0
                                           End If

                                           Do
                                               bytesRead = resampler.Read(buffer, 0, bufferSize)
                                               If bytesRead = 0 Then Exit Do

                                               writer.Write(buffer, 0, bytesRead)
                                               totalRead += bytesRead

                                               ' Calculer le pourcentage
                                               Dim progressPercent As Integer = CInt(Math.Min((totalRead * 100) / estimatedLength, 100))

                                               ' Mettre à jour la barre de progression
                                               If progressPercent <> lastProgressUpdate AndAlso progressPercent <= 100 Then
                                                   lastProgressUpdate = progressPercent
                                                   Try
                                                       If Me.InvokeRequired Then
                                                           Me.Invoke(Sub()
                                                                         If ProgressBarPisteActuelle.Value < progressPercent Then
                                                                             ProgressBarPisteActuelle.Value = progressPercent
                                                                         End If
                                                                     End Sub)
                                                       Else
                                                           If ProgressBarPisteActuelle.Value < progressPercent Then
                                                               ProgressBarPisteActuelle.Value = progressPercent
                                                           End If
                                                       End If
                                                   Catch
                                                       ' Ignorer les erreurs d'invocation
                                                   End Try
                                               End If
                                           Loop
                                       End Using
                                   End Using
                               End If

                               ' Écrire les métadonnées avec TagLib (supporté pour WAV via RIFF INFO)
                               EcrireMetadonnees(cheminFichier, titre, artiste, numeroPiste,
                                               album, artisteAlbum, annee, genre, commentaire, pochette, TextBoxNumCD.Text)
                           End Sub)
        Finally
            ' Disposer le stream de volume si c'est un wrapper différent de la source
            If sourceAvecVolume IsNot source Then
                sourceAvecVolume?.Dispose()
            End If

            ' Libérer la copie de l'image
            pochette?.Dispose()
        End Try
    End Function

    ''' <summary>
    ''' Extrait une piste en FLAC avec compression sans perte via FFMpeg
    ''' </summary>
    Private Async Function ExtraireFlac(source As NAudio.Wave.WaveStream, cheminFichier As String,
                                        titre As String, artiste As String, numeroPiste As String) As Task
        ' Capturer toutes les valeurs UI AVANT les opérations asynchrones
        Dim qualiteIndex As Integer = ComboBoxQualiteConversion.SelectedIndex
        Dim album As String = TextBoxCDTitre.Text
        Dim artisteAlbum As String = TextBoxCDArtiste.Text
        Dim annee As String = TextBoxAnnee.Text
        Dim genre As String = If(ComboBoxGenre.SelectedItem?.ToString(), "")
        Dim commentaire As String = TextBoxCommentaire.Text
        Dim pochette As Image = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)

        ' Déterminer le niveau de compression FLAC
        ' Index 0=Niveau 0, 1=Niveau 5, 2=Niveau 8
        Dim compressionLevel As Integer = 8 ' Par défaut
        Select Case qualiteIndex
            Case 0 ' Niveau 0 (rapide)
                compressionLevel = 0
            Case 1 ' Niveau 5 (équilibré)
                compressionLevel = 5
            Case 2 ' Niveau 8 (meilleur)
                compressionLevel = 8
            Case Else ' Fallback
                compressionLevel = 8
        End Select

        ' Créer un fichier WAV temporaire
        Dim cheminWavTemp = Path.Combine(Path.GetTempPath(), $"audioplay_temp_{Guid.NewGuid()}.wav")

        ' Capturer la valeur du volume
        Dim volumePercent As Decimal = NumericUpDown_DB.Value
        Dim volumeMultiplier As Single = CSng(volumePercent / 100D)

        Try
            ' Étape 1: Extraire en WAV temporaire avec progression
            Await Task.Run(Sub()
                               Using writer As New NAudio.Wave.WaveFileWriter(cheminWavTemp, source.WaveFormat)
                                   CopierAvecProgression(source, writer)
                               End Using
                           End Sub)

            ' Étape 2: Encoder WAV -> FLAC avec FFMpeg et ajustement de volume
            Dim ffmpegPath As String = Await TrouverFFMpeg()
            If Not String.IsNullOrEmpty(ffmpegPath) Then
                Await Task.Run(Sub()
                                   Dim processInfo As New ProcessStartInfo With {
                                       .FileName = ffmpegPath,
                                       .Arguments = $"-i ""{cheminWavTemp}"" -filter:a volume={volumeMultiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} -compression_level {compressionLevel} -y ""{cheminFichier}""",
                                       .UseShellExecute = False,
                                       .CreateNoWindow = True,
                                       .RedirectStandardOutput = True,
                                       .RedirectStandardError = True
                                   }

                                   Dim process As Process = Process.Start(processInfo)
                                   process.WaitForExit()

                                   If process.ExitCode <> 0 Then
                                       Dim erreur = process.StandardError.ReadToEnd()
                                       Throw New Exception($"Erreur FFMpeg: {erreur}")
                                   End If

                                   process.Dispose()
                               End Sub)
            Else
                ' L'utilisateur a refusé le téléchargement ou il a échoué
                Throw New Exception("FFMpeg n'est pas disponible. L'extraction FLAC ne peut pas continuer.")
            End If

            ' Écrire les métadonnées avec TagLib
            EcrireMetadonnees(cheminFichier, titre, artiste, numeroPiste,
                            album, artisteAlbum, annee, genre, commentaire, pochette, TextBoxNumCD.Text)

        Finally
            ' Libérer la copie de l'image
            pochette?.Dispose()

            ' Nettoyer le fichier temporaire
            If File.Exists(cheminWavTemp) Then
                Try
                    File.Delete(cheminWavTemp)
                Catch
                    ' Ignorer les erreurs de suppression
                End Try
            End If
        End Try
    End Function

    ''' <summary>
    ''' Cherche ffmpeg.exe dans les emplacements standards
    ''' Si introuvable, propose de le télécharger automatiquement
    ''' </summary>
    Private Async Function TrouverFFMpeg() As Task(Of String)
        ' Vérifier si FFMpeg est déjà installé
        Dim cheminFFMpeg = FFMpegManager.ObtenirCheminFFMpeg()
        If Not String.IsNullOrEmpty(cheminFFMpeg) Then
            Return cheminFFMpeg
        End If

        ' FFMpeg n'est pas installé, proposer de le télécharger
        Dim resultat = MessageBox.Show(
            "FFMpeg est nécessaire pour extraire en format FLAC et WMA." & vbCrLf & vbCrLf &
            "Voulez-vous télécharger et installer FFMpeg automatiquement ?" & vbCrLf &
            "(Taille: ~120 MB, téléchargement unique)",
            "FFMpeg requis",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If resultat = DialogResult.Yes Then
            ' Afficher le formulaire de téléchargement
            Using formTelechargemEnt As New FormTelechargerFFMpeg()
                If formTelechargemEnt.ShowDialog(Me) = DialogResult.OK Then
                    ' Téléchargement réussi, retourner le chemin
                    Return FFMpegManager.ObtenirCheminFFMpeg()
                End If
            End Using
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Extrait une piste en WMA (Windows Media Audio) via FFMpeg
    ''' </summary>
    Private Async Function ExtraireWma(source As NAudio.Wave.WaveStream, cheminFichier As String,
                                       titre As String, artiste As String, numeroPiste As String) As Task
        ' Capturer toutes les valeurs UI AVANT Task.Run
        Dim qualiteIndex As Integer = ComboBoxQualiteConversion.SelectedIndex
        Dim album As String = TextBoxCDTitre.Text
        Dim artisteAlbum As String = TextBoxCDArtiste.Text
        Dim annee As String = TextBoxAnnee.Text
        Dim genre As String = If(ComboBoxGenre.SelectedItem?.ToString(), "")
        Dim commentaire As String = TextBoxCommentaire.Text
        Dim pochette As Image = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)

        ' Déterminer le bitrate WMA
        ' Index 0=128, 1=192, 2=256
        Dim bitrate As Integer = 256 ' Par défaut
        Select Case qualiteIndex
            Case 0 ' 128 kbps
                bitrate = 128
            Case 1 ' 192 kbps
                bitrate = 192
            Case 2 ' 256 kbps
                bitrate = 256
            Case Else ' Fallback
                bitrate = 256
        End Select

        ' Créer un fichier WAV temporaire
        Dim cheminWavTemp = Path.Combine(Path.GetTempPath(), $"audioplay_temp_{Guid.NewGuid()}.wav")

        ' Capturer la valeur du volume
        Dim volumePercent As Decimal = NumericUpDown_DB.Value
        Dim volumeMultiplier As Single = CSng(volumePercent / 100D)

        Try
            ' Étape 1: Extraire en WAV temporaire avec progression
            Await Task.Run(Sub()
                               Using writer As New NAudio.Wave.WaveFileWriter(cheminWavTemp, source.WaveFormat)
                                   CopierAvecProgression(source, writer)
                               End Using
                           End Sub)

            ' Étape 2: Encoder WAV -> WMA avec FFMpeg et ajustement de volume
            Dim ffmpegPath As String = Await TrouverFFMpeg()
            If Not String.IsNullOrEmpty(ffmpegPath) Then
                Await Task.Run(Sub()
                                   Dim processInfo As New ProcessStartInfo With {
                                       .FileName = ffmpegPath,
                                       .Arguments = $"-i ""{cheminWavTemp}"" -filter:a volume={volumeMultiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} -codec:a wmav2 -b:a {bitrate}k -y ""{cheminFichier}""",
                                       .UseShellExecute = False,
                                       .CreateNoWindow = True,
                                       .RedirectStandardOutput = True,
                                       .RedirectStandardError = True
                                   }

                                   Dim process As Process = Process.Start(processInfo)
                                   process.WaitForExit()

                                   If process.ExitCode <> 0 Then
                                       Dim erreur = process.StandardError.ReadToEnd()
                                       Throw New Exception($"Erreur FFMpeg: {erreur}")
                                   End If

                                   process.Dispose()
                               End Sub)
            Else
                ' L'utilisateur a refusé le téléchargement ou il a échoué
                Throw New Exception("FFMpeg n'est pas disponible. L'extraction WMA ne peut pas continuer.")
            End If

            ' Écrire les métadonnées avec TagLib
            EcrireMetadonnees(cheminFichier, titre, artiste, numeroPiste,
                            album, artisteAlbum, annee, genre, commentaire, pochette, TextBoxNumCD.Text)

        Finally
            ' Libérer la copie de l'image
            pochette?.Dispose()

            ' Nettoyer le fichier temporaire
            If File.Exists(cheminWavTemp) Then
                Try
                    File.Delete(cheminWavTemp)
                Catch
                    ' Ignorer les erreurs de suppression
                End Try
            End If
        End Try
    End Function

    ''' <summary>
    ''' Écrit les métadonnées dans un fichier audio (MP3, WAV, FLAC, WMA)
    ''' </summary>
    Private Sub EcrireMetadonnees(cheminFichier As String, titre As String, artiste As String, numeroPiste As String,
                                   album As String, artisteAlbum As String, annee As String,
                                   genre As String, commentaire As String, pochette As Image, numeroDisque As String)
        Try
            Using fichier = TagLib.File.Create(cheminFichier)
                ' Métadonnées de base
                fichier.Tag.Title = titre
                fichier.Tag.Performers = {artiste}
                fichier.Tag.AlbumArtists = {artisteAlbum}
                fichier.Tag.Album = album

                ' Numéro de piste
                Dim trackNum As UInteger
                If UInteger.TryParse(numeroPiste, trackNum) Then
                    fichier.Tag.Track = trackNum
                End If

                ' Numéro de disque
                Dim discNum As UInteger
                If Not String.IsNullOrWhiteSpace(numeroDisque) AndAlso UInteger.TryParse(numeroDisque, discNum) Then
                    fichier.Tag.Disc = discNum
                End If

                ' Année
                Dim anneeNum As UInteger
                If Not String.IsNullOrWhiteSpace(annee) AndAlso
                   UInteger.TryParse(annee, anneeNum) Then
                    fichier.Tag.Year = anneeNum
                End If

                ' Genre
                If Not String.IsNullOrWhiteSpace(genre) Then
                    fichier.Tag.Genres = {genre}
                End If

                ' Commentaire
                If Not String.IsNullOrWhiteSpace(commentaire) Then
                    fichier.Tag.Comment = commentaire
                End If

                ' Pochette (si disponible)
                If pochette IsNot Nothing Then
                    Try
                        Using ms As New MemoryStream()
                            pochette.Save(ms, Imaging.ImageFormat.Jpeg)
                            Dim imageData As Byte() = ms.ToArray()

                            Dim picture As New TagLib.Picture(New TagLib.ByteVector(imageData))
                            picture.Type = TagLib.PictureType.FrontCover
                            picture.MimeType = "image/jpeg"
                            picture.Description = "Cover"

                            fichier.Tag.Pictures = {picture}
                        End Using
                    Catch ex As Exception
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur ajout pochette: {ex.Message}")
                    End Try
                End If

                fichier.Save()
            End Using
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur écriture métadonnées: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Dessine les en-têtes de colonnes du ListView avec les couleurs du thème
    ''' </summary>
    Private Sub ListViewCompress_DrawColumnHeader(sender As Object, e As DrawListViewColumnHeaderEventArgs)
        ' Récupérer le thème actuel
        Dim theme = ThemeManager.GetCurrentTheme()

        ' Dessiner le fond de l'en-tête avec la couleur spécifique
        Using brush As New SolidBrush(theme.ListViewHeaderBackColor)
            e.Graphics.FillRectangle(brush, e.Bounds)
        End Using

        ' Dessiner la bordure
        e.Graphics.DrawRectangle(SystemPens.ControlDark, New Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1))

        ' Dessiner le texte centré avec support multi-ligne
        Dim sf As New StringFormat()
        sf.Alignment = StringAlignment.Center
        sf.LineAlignment = StringAlignment.Center
        sf.Trimming = StringTrimming.Word
        sf.FormatFlags = StringFormatFlags.LineLimit

        Using textBrush As New SolidBrush(theme.ListViewHeaderForeColor)
            e.Graphics.DrawString(e.Header.Text, e.Font, textBrush, e.Bounds, sf)
        End Using
    End Sub

    ''' <summary>
    ''' Dessine les items du ListView
    ''' </summary>
    Private Sub ListViewCompress_DrawItem(sender As Object, e As DrawListViewItemEventArgs)
        ' Dessiner la checkbox si nécessaire
        e.DrawDefault = False

        ' Dessiner le fond de la ligne entière si sélectionnée
        If e.Item.Selected Then
            Dim theme = ThemeManager.GetCurrentTheme()
            Using brush As New SolidBrush(theme.ListViewSelectionBackColor)
                e.Graphics.FillRectangle(brush, e.Bounds)
            End Using
        End If
    End Sub

    ''' <summary>
    ''' Dessine les sous-items (colonnes) du ListView avec les couleurs du thème
    ''' </summary>
    Private Sub ListViewCompress_DrawSubItem(sender As Object, e As DrawListViewSubItemEventArgs)
        ' Déterminer si cet item est sélectionné
        Dim estSelectionne As Boolean = e.Item.Selected
        Dim theme = ThemeManager.GetCurrentTheme()

        ' Couleur de fond
        Dim couleurFond As Color
        If estSelectionne Then
            couleurFond = theme.ListViewSelectionBackColor
        Else
            couleurFond = e.Item.BackColor
        End If

        ' Couleur de texte
        Dim couleurTexte As Color
        If estSelectionne Then
            couleurTexte = theme.ListViewSelectionForeColor
        Else
            couleurTexte = e.Item.ForeColor
        End If

        ' Dessiner le fond
        Using brush As New SolidBrush(couleurFond)
            e.Graphics.FillRectangle(brush, e.Bounds)
        End Using

        ' Gérer la checkbox pour la première colonne (Piste)
        If e.ColumnIndex = 0 Then
            ' Dessiner la checkbox
            Dim checkBoxSize As Integer = 13
            Dim checkBoxX As Integer = e.Bounds.X + 2
            Dim checkBoxY As Integer = e.Bounds.Y + (e.Bounds.Height - checkBoxSize) \ 2
            Dim checkBoxBounds As New Rectangle(checkBoxX, checkBoxY, checkBoxSize, checkBoxSize)

            Dim checkState As System.Windows.Forms.VisualStyles.CheckBoxState
            If e.Item.Checked Then
                checkState = System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
            Else
                checkState = System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal
            End If

            CheckBoxRenderer.DrawCheckBox(e.Graphics, checkBoxBounds.Location, checkState)

            ' Dessiner le texte à droite de la checkbox
            Dim textBounds As New Rectangle(checkBoxX + checkBoxSize + 4, e.Bounds.Y, e.Bounds.Width - checkBoxSize - 6, e.Bounds.Height)
            Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.VerticalCenter
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font, textBounds, couleurTexte, flags)
        Else
            ' Dessiner le texte normalement pour les autres colonnes
            Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis

            ' Centrer certaines colonnes (Début, Longueur, Taille, TailleComp)
            If e.ColumnIndex = 3 OrElse e.ColumnIndex = 4 OrElse e.ColumnIndex = 5 OrElse e.ColumnIndex = 6 Then
                flags = TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter
            End If

            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font, e.Bounds, couleurTexte, flags)
        End If

        ' Dessiner les lignes de grille
        Using pen As New Pen(Color.LightGray)
            e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom)
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1)
        End Using
    End Sub

    ''' <summary>
    ''' Charge et affiche la pochette de l'album dans PictureBoxPochette
    ''' </summary>
    Private Async Function ChargerPochetteAlbum() As Task
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ChargerPochetteAlbum() appelé")

        ' Nettoyer l'image existante
        If PictureBoxPochette.Image IsNot Nothing Then
            Dim oldImage = PictureBoxPochette.Image
            PictureBoxPochette.Image = Nothing
            oldImage.Dispose()
            Label_DimImage.Text = ""
            LabelTailleImage.Text = ""
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⚠️ IMAGE EFFACÉE dans ChargerPochetteAlbum()")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Stack: {New System.Diagnostics.StackTrace(True).GetFrame(1)?.GetMethod()?.Name}")
        End If

        ' IMPORTANT: Réinitialiser l'historique et l'index pour repartir à zéro
        historiquePochettes.Clear()
        indexPochetteActuelle = -1
        pochetteTempUrl = Nothing
        pochetteTempBytes = Nothing
        cachePochettesBytes.Clear() ' Vider le cache mémoire des images
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Historique réinitialisé")

        ' Vérifier si les métadonnées sont disponibles
        If metadonneesCD Is Nothing Then
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] metadonneesCD est Nothing - abandon")
            Return
        End If

        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Métadonnées disponibles: {metadonneesCD.Artist} - {metadonneesCD.Album}")
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] CoverArtUrl: {If(String.IsNullOrWhiteSpace(metadonneesCD.CoverArtUrl), "VIDE", metadonneesCD.CoverArtUrl)}")

        Try
            ' ═══════════════════════════════════════════════════════════════════════════
            ' NOUVELLE STRATÉGIE : RECHERCHE MULTI-SOURCE PARALLÈLE
            ' ═══════════════════════════════════════════════════════════════════════════

            Dim toutesLesUrls As New List(Of String)()

            ' 1️⃣ PRIORITÉ 1 : URL de MusicBrainz (Cover Art Archive) si déjà disponible
            If Not String.IsNullOrWhiteSpace(metadonneesCD.CoverArtUrl) Then
                ' Vérifier d'abord le cache local d'images
                Dim cheminCache = CoverCacheManager.ObtenirCheminFichier(metadonneesCD.CoverArtUrl)
                If File.Exists(cheminCache) Then
                    Try
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Chargement depuis cache local: {cheminCache}")

                        ' Lire les bytes du fichier cache
                        pochetteTempBytes = File.ReadAllBytes(cheminCache)
                        pochetteTempUrl = metadonneesCD.CoverArtUrl

                        ' Ajouter au cache mémoire pour navigation rapide
                        cachePochettesBytes(metadonneesCD.CoverArtUrl) = pochetteTempBytes

                        ' Afficher l'image
                        Using ms As New MemoryStream(pochetteTempBytes)
                            PictureBoxPochette.Image = New Bitmap(Image.FromStream(ms))
                            PictureBoxPochette.SizeMode = PictureBoxSizeMode.Zoom
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✅ IMAGE AFFICHÉE depuis cache local")
                        End Using

                        ' Ajouter à l'historique
                        AjouterAHistoriquePochettes(metadonneesCD.CoverArtUrl)

                        MettreAJourInfosPochette()
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Pochette affichée depuis cache local")

                        ' Ajouter cette URL à la liste pour ne pas la chercher à nouveau
                        toutesLesUrls.Add(metadonneesCD.CoverArtUrl)
                        sourcesPochettes(metadonneesCD.CoverArtUrl) = "Cover Art Archive"

                        ' Mettre à jour l'état des boutons
                        MettreAJourBoutonsNavigation()
                    Catch ex As Exception
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur lecture cache: {ex.Message}")
                    End Try
                Else
                    ' Essayer de télécharger depuis Cover Art Archive
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Téléchargement depuis Cover Art Archive: {metadonneesCD.CoverArtUrl}")
                    Try
                        Await TelechargerEtAfficherPochette(metadonneesCD.CoverArtUrl, ajouterHistorique:=True)
                        toutesLesUrls.Add(metadonneesCD.CoverArtUrl)
                        sourcesPochettes(metadonneesCD.CoverArtUrl) = "Cover Art Archive"
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Cover Art Archive OK")
                    Catch ex As Exception
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Cover Art Archive échoué ({ex.Message}), fallback vers autres sources...")
                    End Try
                End If
            End If

            ' 2️⃣ RECHERCHE PARALLÈLE SUR TOUTES LES SOURCES
            ' Chercher simultanément sur iTunes, Last.fm et MusicBrainz textuel
            If Not String.IsNullOrWhiteSpace(metadonneesCD.Artist) AndAlso Not String.IsNullOrWhiteSpace(metadonneesCD.Album) Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🔍 Recherche parallèle multi-source pour: {metadonneesCD.Artist} - {metadonneesCD.Album}")

                ' Lancer toutes les recherches en parallèle sur TOUTES les sources (8 au total)
                Dim tacheItunes = RechercherPochetteiTunes(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheLastFm = RechercherPochetteLastFm(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheMusicBrainz = RechercherPochetteMusicBrainz(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheDeezer = RechercherPochetteDeezer(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheDiscogs = RechercherPochetteDiscogs(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheTheAudioDB = RechercherPochetteTheAudioDB(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheFanartTV = RechercherPochetteFanartTV(metadonneesCD.Artist, metadonneesCD.Album)

                ' Attendre que toutes les recherches se terminent
                Await Task.WhenAll(tacheItunes, tacheLastFm, tacheMusicBrainz, tacheDeezer, tacheDiscogs, tacheTheAudioDB, tacheFanartTV)

                ' Collecter les résultats (ordre de priorité pour affichage)
                Dim urlItunes = Await tacheItunes
                Dim urlLastFm = Await tacheLastFm
                Dim urlDeezer = Await tacheDeezer
                Dim urlDiscogs = Await tacheDiscogs
                Dim urlTheAudioDB = Await tacheTheAudioDB
                Dim urlFanartTV = Await tacheFanartTV
                Dim urlMusicBrainz = Await tacheMusicBrainz

                ' Ajouter les URLs trouvées (ordre de priorité : iTunes, Last.fm, Deezer, Discogs, TheAudioDB, Fanart.tv, MusicBrainz)
                If Not String.IsNullOrWhiteSpace(urlItunes) AndAlso Not toutesLesUrls.Contains(urlItunes) Then
                    toutesLesUrls.Add(urlItunes)
                    sourcesPochettes(urlItunes) = "iTunes"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ iTunes trouvé: {urlItunes}")
                End If

                If Not String.IsNullOrWhiteSpace(urlLastFm) AndAlso Not toutesLesUrls.Contains(urlLastFm) Then
                    toutesLesUrls.Add(urlLastFm)
                    sourcesPochettes(urlLastFm) = "Last.fm"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Last.fm trouvé: {urlLastFm}")
                End If

                If Not String.IsNullOrWhiteSpace(urlDeezer) AndAlso Not toutesLesUrls.Contains(urlDeezer) Then
                    toutesLesUrls.Add(urlDeezer)
                    sourcesPochettes(urlDeezer) = "Deezer"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Deezer trouvé: {urlDeezer}")
                End If

                If Not String.IsNullOrWhiteSpace(urlDiscogs) AndAlso Not toutesLesUrls.Contains(urlDiscogs) Then
                    toutesLesUrls.Add(urlDiscogs)
                    sourcesPochettes(urlDiscogs) = "Discogs"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Discogs trouvé: {urlDiscogs}")
                End If

                If Not String.IsNullOrWhiteSpace(urlTheAudioDB) AndAlso Not toutesLesUrls.Contains(urlTheAudioDB) Then
                    toutesLesUrls.Add(urlTheAudioDB)
                    sourcesPochettes(urlTheAudioDB) = "TheAudioDB"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ TheAudioDB trouvé: {urlTheAudioDB}")
                End If

                If Not String.IsNullOrWhiteSpace(urlFanartTV) AndAlso Not toutesLesUrls.Contains(urlFanartTV) Then
                    toutesLesUrls.Add(urlFanartTV)
                    sourcesPochettes(urlFanartTV) = "Fanart.tv"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Fanart.tv trouvé (HD): {urlFanartTV}")
                End If

                If Not String.IsNullOrWhiteSpace(urlMusicBrainz) AndAlso Not toutesLesUrls.Contains(urlMusicBrainz) Then
                    toutesLesUrls.Add(urlMusicBrainz)
                    sourcesPochettes(urlMusicBrainz) = "MusicBrainz"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ MusicBrainz trouvé: {urlMusicBrainz}")
                End If

                ' 3️⃣ TÉLÉCHARGER TOUTES LES POCHETTES TROUVÉES
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] 📥 Téléchargement de {toutesLesUrls.Count} pochette(s) trouvée(s)...")

                Dim compteurCharge = historiquePochettes.Count ' Combien déjà chargées (cache local ou Cover Art Archive)

                For Each url In toutesLesUrls
                    ' Vérifier si pas déjà dans l'historique ou le cache
                    If Not historiquePochettes.Contains(url) Then
                        Try
                            ' Télécharger sans afficher (sauf si c'est la toute première)
                            Dim estPremiere = (compteurCharge = 0)
                            If estPremiere Then
                                ' Afficher la première pochette
                                Try
                                    Await TelechargerEtAfficherPochette(url, ajouterHistorique:=True)
                                    compteurCharge += 1
                                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Pochette {compteurCharge}/{toutesLesUrls.Count} affichée")
                                Catch exFirst As Exception
                                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Échec affichage première pochette {url}: {exFirst.Message}")
                                    ' Continuer avec les autres
                                End Try
                            Else
                                ' Pré-télécharger les autres en cache sans afficher
                                Try
                                    Using client As New System.Net.WebClient()
                                        client.Headers.Add("User-Agent", "AudioPlay/1.0")

                                        ' Forcer TLS 1.2 et 1.3 pour compatibilité HTTPS
                                        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 Or System.Net.SecurityProtocolType.Tls13

                                        Dim imageBytes = Await client.DownloadDataTaskAsync(url)

                                        ' Ajouter au cache mémoire
                                        cachePochettesBytes(url) = imageBytes

                                        ' Ajouter à l'historique
                                        historiquePochettes.Add(url)
                                        compteurCharge += 1
                                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Pochette {compteurCharge}/{toutesLesUrls.Count} pré-chargée en cache")
                                    End Using
                                Catch ex As Exception
                                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Échec pré-chargement {url}: {ex.Message}")
                                End Try
                            End If
                        Catch ex As Exception
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Échec téléchargement {url}: {ex.Message}")
                        End Try
                    Else
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⊙ Pochette déjà en cache/historique: {url}")
                    End If
                Next

                ' Mettre à jour les boutons de navigation
                MettreAJourBoutonsNavigation()

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🎉 {historiquePochettes.Count} pochette(s) disponible(s) pour navigation")
            End If

            ' Si aucune pochette trouvée
            If historiquePochettes.Count = 0 Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ❌ Aucune pochette trouvée sur aucune source")
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur ChargerPochetteAlbum: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Stack: {ex.StackTrace}")
        End Try
    End Function

    '''
    ''' Recherche l'URL de la pochette sur iTunes Search API
    ''' Ne nécessite pas de clé API, excellente qualité d'images
    ''' </summary>
    Private Async Function RechercherPochetteiTunes(artiste As String, album As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(10)

                ' Construire la requête iTunes Search API
                Dim query = Uri.EscapeDataString($"{artiste} {album}")
                Dim searchUrl = $"https://itunes.apple.com/search?term={query}&entity=album&limit=1"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche iTunes: {searchUrl}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette
                Dim coverUrl = ExtraireUrlPochetteITunesDeJSON(response)

                If Not String.IsNullOrWhiteSpace(coverUrl) Then
                    ' iTunes retourne des images 100x100 par défaut, on peut les agrandir
                    ' Remplacer 100x100bb.jpg par 600x600bb.jpg pour meilleure qualité
                    coverUrl = coverUrl.Replace("100x100bb.jpg", "600x600bb.jpg")
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] iTunes - URL pochette trouvée: {coverUrl}")
                    Return coverUrl
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche iTunes: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Extrait l'URL de la pochette depuis la réponse JSON d'iTunes
    ''' </summary>
    Private Function ExtraireUrlPochetteITunesDeJSON(jsonResponse As String) As String
        Try
            ' Parser manuellement le JSON pour éviter une dépendance externe
            ' Format: {"resultCount":1,"results":[{"artworkUrl100":"https://..."}]}

            Dim artworkKey = """artworkUrl100"":"""
            Dim startIndex = jsonResponse.IndexOf(artworkKey)

            If startIndex >= 0 Then
                startIndex += artworkKey.Length
                Dim endIndex = jsonResponse.IndexOf("""", startIndex)

                If endIndex > startIndex Then
                    Dim url = jsonResponse.Substring(startIndex, endIndex - startIndex)
                    ' Déséchapper les caractères JSON
                    url = url.Replace("\/", "/")
                    Return url
                End If
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur parsing JSON iTunes: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur Last.fm API
    ''' Utilise une clé API publique (lecture seule)
    ''' </summary>
    Private Async Function RechercherPochetteLastFm(artiste As String, album As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(10)

                ' Construire la requête Last.fm API
                Dim artisteEncode = Uri.EscapeDataString(artiste)
                Dim albumEncode = Uri.EscapeDataString(album)
                Dim searchUrl = $"http://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key={LASTFM_API_KEY}&artist={artisteEncode}&album={albumEncode}&format=json"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche Last.fm: {artiste} - {album}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette
                Dim coverUrl = ExtraireUrlPochetteLastFmDeJSON(response)

                If Not String.IsNullOrWhiteSpace(coverUrl) Then
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Last.fm - URL pochette trouvée: {coverUrl}")
                    Return coverUrl
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche Last.fm: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Extrait l'URL de la pochette depuis la réponse JSON de Last.fm
    ''' </summary>
    Private Function ExtraireUrlPochetteLastFmDeJSON(jsonResponse As String) As String
        Try
            ' Parser manuellement le JSON pour éviter une dépendance externe
            ' Last.fm retourne plusieurs tailles: small, medium, large, extralarge, mega
            ' Format: "image":[{"#text":"url","size":"small"},{"#text":"url","size":"extralarge"}]

            ' Chercher la plus grande taille disponible (extralarge ou mega)
            Dim patterns As String() = {
                """size"":""mega""",
                """size"":""extralarge""",
                """size"":""large"""
            }

            For Each pattern In patterns
                Dim sizeIndex = jsonResponse.IndexOf(pattern)
                If sizeIndex >= 0 Then
                    ' Chercher le "#text" qui précède ce size
                    Dim textKey = """#text"":"""
                    Dim searchStart = Math.Max(0, sizeIndex - 200) ' Chercher dans les 200 caractères précédents
                    Dim textIndex = jsonResponse.LastIndexOf(textKey, sizeIndex)

                    If textIndex >= searchStart Then
                        Dim urlStart = textIndex + textKey.Length
                        Dim urlEnd = jsonResponse.IndexOf("""", urlStart)

                        If urlEnd > urlStart Then
                            Dim url = jsonResponse.Substring(urlStart, urlEnd - urlStart)
                            ' Déséchapper les caractères JSON
                            url = url.Replace("\/", "/")

                            ' Vérifier que l'URL n'est pas vide (Last.fm retourne parfois des URLs vides)
                            If Not String.IsNullOrWhiteSpace(url) AndAlso url.StartsWith("http") Then
                                Return url
                            End If
                        End If
                    End If
                End If
            Next

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur parsing JSON Last.fm: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur MusicBrainz en utilisant l'artiste et l'album
    ''' </summary>
    Private Async Function RechercherPochetteMusicBrainz(artiste As String, album As String) As Task(Of String)
        Try
            ' Activer TLS 1.2 et 1.3 pour résoudre les problèmes SSL
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 Or System.Net.SecurityProtocolType.Tls13

            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(15)
                client.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0 (https://github.com/jeanpel58/AudioPlay)")

                ' Rechercher le release sur MusicBrainz
                Dim query = Uri.EscapeDataString($"{artiste} {album}")
                Dim searchUrl = $"https://musicbrainz.org/ws/2/release/?query={query}&fmt=json&limit=1"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche MusicBrainz: {searchUrl}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire le release ID
                Dim releaseId = ExtraireReleaseIdDeJSON(response)

                If Not String.IsNullOrWhiteSpace(releaseId) Then
                    ' Construire l'URL de la pochette depuis Cover Art Archive
                    Dim coverUrl = $"https://coverartarchive.org/release/{releaseId}/front-500"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] URL pochette trouvée: {coverUrl}")
                    Return coverUrl
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche pochette: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Extrait le Release ID depuis la réponse JSON de MusicBrainz
    ''' </summary>
    Private Function ExtraireReleaseIdDeJSON(json As String) As String
        Try
            ' Rechercher le pattern "id":"xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            Dim pattern = """releases"":\s*\[\s*\{\s*""id""\s*:\s*""([a-f0-9\-]+)"""
            Dim match = System.Text.RegularExpressions.Regex.Match(json, pattern)

            If match.Success AndAlso match.Groups.Count > 1 Then
                Return match.Groups(1).Value
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur extraction Release ID: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur Deezer API (pas de clé API requise)
    ''' </summary>
    Private Async Function RechercherPochetteDeezer(artiste As String, album As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(15)
                client.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0")

                ' Construire la requête Deezer API (pas de clé requise)
                Dim artisteEncode = Uri.EscapeDataString(artiste)
                Dim albumEncode = Uri.EscapeDataString(album)
                Dim searchUrl = $"https://api.deezer.com/search/album?q=artist:""{artisteEncode}"" album:""{albumEncode}"""

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche Deezer: {artiste} - {album}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette
                ' Format: {"data":[{"cover_xl":"https://..."}]}
                Dim coverKey = """cover_xl"":"""
                Dim startIndex = response.IndexOf(coverKey)

                If startIndex >= 0 Then
                    startIndex += coverKey.Length
                    Dim endIndex = response.IndexOf("""", startIndex)

                    If endIndex > startIndex Then
                        Dim url = response.Substring(startIndex, endIndex - startIndex)
                        url = url.Replace("\/", "/")

                        If Not String.IsNullOrWhiteSpace(url) AndAlso url.StartsWith("http") Then
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Deezer - URL pochette trouvée: {url}")
                            Return url
                        End If
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche Deezer: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur Discogs API (idéal pour CDs physiques)
    ''' </summary>
    Private Async Function RechercherPochetteDiscogs(artiste As String, album As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(15)
                client.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0 (https://github.com/jeanpel58/AudioPlay)")

                ' Construire la requête Discogs API
                Dim query = Uri.EscapeDataString($"{artiste} {album}")
                Dim searchUrl = $"https://api.discogs.com/database/search?q={query}&type=release&format=CD"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche Discogs: {artiste} - {album}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette
                ' Format: {"results":[{"cover_image":"https://..."}]}
                Dim coverKey = """cover_image"":"""
                Dim startIndex = response.IndexOf(coverKey)

                If startIndex >= 0 Then
                    startIndex += coverKey.Length
                    Dim endIndex = response.IndexOf("""", startIndex)

                    If endIndex > startIndex Then
                        Dim url = response.Substring(startIndex, endIndex - startIndex)
                        url = url.Replace("\/", "/")

                        ' Discogs retourne parfois des URLs spacer.gif, les ignorer
                        If Not String.IsNullOrWhiteSpace(url) AndAlso url.StartsWith("http") AndAlso Not url.Contains("spacer.gif") Then
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Discogs - URL pochette trouvée: {url}")
                            Return url
                        End If
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche Discogs: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur TheAudioDB API (gratuit, pas de rate limiting strict)
    ''' </summary>
    Private Async Function RechercherPochetteTheAudioDB(artiste As String, album As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(15)
                client.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0")

                ' Clé API publique gratuite de TheAudioDB
                Dim apiKey = "523532" ' Clé publique pour tests
                Dim artisteEncode = Uri.EscapeDataString(artiste)
                Dim albumEncode = Uri.EscapeDataString(album)
                Dim searchUrl = $"https://www.theaudiodb.com/api/v1/json/{apiKey}/searchalbum.php?s={artisteEncode}&a={albumEncode}"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche TheAudioDB: {artiste} - {album}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette
                ' Format: {"album":[{"strAlbumThumb":"https://..."}]}
                Dim coverKey = """strAlbumThumb"":"""
                Dim startIndex = response.IndexOf(coverKey)

                If startIndex >= 0 Then
                    startIndex += coverKey.Length
                    Dim endIndex = response.IndexOf("""", startIndex)

                    If endIndex > startIndex Then
                        Dim url = response.Substring(startIndex, endIndex - startIndex)
                        url = url.Replace("\/", "/")

                        If Not String.IsNullOrWhiteSpace(url) AndAlso url.StartsWith("http") Then
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] TheAudioDB - URL pochette trouvée: {url}")
                            Return url
                        End If
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche TheAudioDB: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur Fanart.tv API (haute résolution, nécessite MusicBrainz ID)
    ''' </summary>
    Private Async Function RechercherPochetteFanartTV(artiste As String, album As String) As Task(Of String)
        Try
            ' Fanart.tv nécessite un MusicBrainz ID, essayons de le trouver d'abord
            Dim mbid = Await ObtenirMusicBrainzArtistID(artiste)

            If String.IsNullOrWhiteSpace(mbid) Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Fanart.tv - MusicBrainz ID non trouvé pour {artiste}")
                Return Nothing
            End If

            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(10)

                ' Clé API publique gratuite de Fanart.tv
                Dim apiKey = "2c4ecd81b39b48289f8b18798c0949e0" ' Clé publique
                Dim searchUrl = $"http://webservice.fanart.tv/v3/music/{mbid}?api_key={apiKey}"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche Fanart.tv avec MBID: {mbid}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette album
                ' Format: {"albums":{"{album-mbid}":{"albumcover":[{"url":"https://..."}]}}}
                Dim coverKey = """albumcover"":\[\{""url"":"""
                Dim startIndex = response.IndexOf(coverKey)

                If startIndex >= 0 Then
                    startIndex += coverKey.Length
                    Dim endIndex = response.IndexOf("""", startIndex)

                    If endIndex > startIndex Then
                        Dim url = response.Substring(startIndex, endIndex - startIndex)
                        url = url.Replace("\/", "/")

                        If Not String.IsNullOrWhiteSpace(url) AndAlso url.StartsWith("http") Then
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Fanart.tv - URL pochette HD trouvée: {url}")
                            Return url
                        End If
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche Fanart.tv: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Obtenir le MusicBrainz Artist ID pour Fanart.tv
    ''' </summary>
    Private Async Function ObtenirMusicBrainzArtistID(artiste As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(5)
                client.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0 (https://github.com/jeanpel58/AudioPlay)")

                Dim query = Uri.EscapeDataString(artiste)
                Dim searchUrl = $"https://musicbrainz.org/ws/2/artist/?query=artist:{query}&fmt=json&limit=1"

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser pour extraire l'ID
                Dim idKey = """artists"":\[\{""id"":"""
                Dim startIndex = response.IndexOf(idKey)

                If startIndex >= 0 Then
                    startIndex += idKey.Length
                    Dim endIndex = response.IndexOf("""", startIndex)

                    If endIndex > startIndex Then
                        Return response.Substring(startIndex, endIndex - startIndex)
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur obtention MBID: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Télécharge et affiche une pochette depuis une URL
    ''' </summary>
    ''' <param name="url">URL de l'image</param>
    ''' <param name="ajouterHistorique">Si True, ajoute l'URL à l'historique (défaut: True)</param>
    Private Async Function TelechargerEtAfficherPochette(url As String, Optional ajouterHistorique As Boolean = True) As Task
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Début téléchargement: {url}")

            Dim imageBytes As Byte()

            ' Vérifier d'abord si l'image est déjà en cache mémoire
            If cachePochettesBytes.ContainsKey(url) Then
                imageBytes = cachePochettesBytes(url)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Image récupérée depuis cache mémoire ({imageBytes.Length} bytes)")
            Else
                ' Forcer TLS 1.2 et 1.3
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 Or System.Net.SecurityProtocolType.Tls13

                ' Ignorer les erreurs de certificat SSL
                System.Net.ServicePointManager.ServerCertificateValidationCallback = Function(sender, certificate, chain, sslPolicyErrors) True

                ' Utiliser WebClient pour un téléchargement plus robuste
                Using client As New System.Net.WebClient()
                    client.Headers.Add("User-Agent", "AudioPlay/1.0")
                    imageBytes = Await client.DownloadDataTaskAsync(url)
                End Using

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] {imageBytes.Length} bytes téléchargés")

                ' Ajouter au cache mémoire
                cachePochettesBytes(url) = imageBytes
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Image ajoutée au cache mémoire")
            End If

            ' Stocker temporairement l'URL et les bytes (seront sauvegardés dans le cache lors du clic sur ButtonExtraire)
            pochetteTempUrl = url
            pochetteTempBytes = imageBytes
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Image stockée temporairement (cache différé)")

            ' Créer l'image depuis les bytes
            Using ms As New System.IO.MemoryStream(imageBytes)
                Using tempImage = Image.FromStream(ms)
                    ' Créer une copie de l'image dans un nouveau Bitmap
                    PictureBoxPochette.Image = New Bitmap(tempImage)
                    PictureBoxPochette.SizeMode = PictureBoxSizeMode.Zoom
                    MettreAJourInfosPochette()
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✅ IMAGE AFFICHÉE dans TelechargerEtAfficherPochette: {url}")
                End Using
            End Using

            ' Ajouter l'URL à l'historique seulement si demandé
            If ajouterHistorique Then
                AjouterAHistoriquePochettes(url)
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur téléchargement pochette: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Type: {ex.GetType().FullName}")
            If ex.InnerException IsNot Nothing Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Inner: {ex.InnerException.Message}")
            End If
            ' Ne pas relancer l'exception pour permettre au téléchargement des autres pochettes de continuer
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Téléchargement échoué, passage à la pochette suivante")
        End Try
    End Function

    '''
    ''' Sauvegarde la pochette dans le répertoire d'extraction avec le nom formaté "Artiste - Album.ext"
    ''' </summary>
    Private Sub SauvegarderPochetteDansRepertoire(imageBytes As Byte())
        Try
            ' Vérifier que le répertoire d'extraction est valide
            Dim repertoireExtraction = TextBoxRepSauvegarde.Text
            If String.IsNullOrWhiteSpace(repertoireExtraction) OrElse Not Directory.Exists(repertoireExtraction) Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Répertoire d'extraction invalide, pochette non sauvegardée localement")
                Return
            End If

            ' Récupérer l'artiste et l'album
            Dim artiste = TextBoxCDArtiste.Text.Trim()
            Dim album = TextBoxCDTitre.Text.Trim()

            If String.IsNullOrWhiteSpace(artiste) OrElse String.IsNullOrWhiteSpace(album) Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Artiste ou album manquant, pochette non sauvegardée localement")
                Return
            End If

            ' Créer le répertoire de l'album: "(Année) Artiste - Album"
            Dim annee As String = TextBoxAnnee.Text.Trim()
            Dim nomRepertoireAlbum As String = ""
            If Not String.IsNullOrEmpty(annee) Then
                nomRepertoireAlbum = $"({annee}) "
            End If
            nomRepertoireAlbum &= $"{artiste} - {album}"
            nomRepertoireAlbum = NettoyerNomFichier(nomRepertoireAlbum)

            Dim cheminRepertoireAlbum As String = Path.Combine(repertoireExtraction, nomRepertoireAlbum)

            ' Créer le répertoire s'il n'existe pas
            If Not Directory.Exists(cheminRepertoireAlbum) Then
                Directory.CreateDirectory(cheminRepertoireAlbum)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Répertoire album créé: {cheminRepertoireAlbum}")
            End If

            ' Détecter le format de l'image depuis les bytes
            Dim extension = DetecterFormatImage(imageBytes)

            ' Nettoyer le nom de fichier (supprimer caractères invalides)
            Dim nomFichier = $"{artiste} - {album}{extension}"
            nomFichier = NettoyerNomFichier(nomFichier)

            ' Construire le chemin complet dans le sous-répertoire de l'album
            Dim cheminComplet = Path.Combine(cheminRepertoireAlbum, nomFichier)

            ' Sauvegarder l'image
            File.WriteAllBytes(cheminComplet, imageBytes)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Pochette sauvegardée: {cheminComplet}")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur sauvegarde pochette dans répertoire: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Détecte le format d'une image depuis ses bytes (magic numbers)
    ''' </summary>
    Private Function DetecterFormatImage(imageBytes As Byte()) As String
        If imageBytes Is Nothing OrElse imageBytes.Length < 4 Then
            Return ".jpg" ' Par défaut
        End If

        ' Vérifier les "magic numbers" (signatures de fichier)
        ' PNG: 89 50 4E 47
        If imageBytes.Length >= 4 AndAlso
           imageBytes(0) = &H89 AndAlso imageBytes(1) = &H50 AndAlso
           imageBytes(2) = &H4E AndAlso imageBytes(3) = &H47 Then
            Return ".png"
        End If

        ' JPEG: FF D8 FF
        If imageBytes.Length >= 3 AndAlso
           imageBytes(0) = &HFF AndAlso imageBytes(1) = &HD8 AndAlso imageBytes(2) = &HFF Then
            Return ".jpg"
        End If

        ' GIF: 47 49 46
        If imageBytes.Length >= 3 AndAlso
           imageBytes(0) = &H47 AndAlso imageBytes(1) = &H49 AndAlso imageBytes(2) = &H46 Then
            Return ".gif"
        End If

        ' WebP: 52 49 46 46 ... 57 45 42 50
        If imageBytes.Length >= 12 AndAlso
           imageBytes(0) = &H52 AndAlso imageBytes(1) = &H49 AndAlso
           imageBytes(2) = &H46 AndAlso imageBytes(3) = &H46 AndAlso
           imageBytes(8) = &H57 AndAlso imageBytes(9) = &H45 AndAlso
           imageBytes(10) = &H42 AndAlso imageBytes(11) = &H50 Then
            Return ".webp"
        End If

        ' Par défaut, supposer JPEG
        Return ".jpg"
    End Function

    ''' <summary>
    ''' Met à jour les labels d'information de la pochette (dimensions et taille)
    ''' </summary>
    Private Sub MettreAJourInfosPochette()
        Try
            If PictureBoxPochette.Image IsNot Nothing Then
                ' Dimensions de l'image
                Dim largeur = PictureBoxPochette.Image.Width
                Dim hauteur = PictureBoxPochette.Image.Height
                Label_DimImage.Text = $"{largeur} x {hauteur}"

                ' Taille de l'image en bytes (approximatif depuis l'image en mémoire)
                If pochetteTempBytes IsNot Nothing Then
                    ' Si on a les bytes bruts, utiliser leur taille réelle
                    Dim tailleMo = pochetteTempBytes.Length / 1024.0 / 1024.0
                    If tailleMo >= 1.0 Then
                        LabelTailleImage.Text = $"{tailleMo:F2} Mo"
                    Else
                        Dim tailleKo = pochetteTempBytes.Length / 1024.0
                        LabelTailleImage.Text = $"{tailleKo:F0} Ko"
                    End If
                Else
                    ' Sinon, estimer depuis l'image
                    Using ms As New MemoryStream()
                        PictureBoxPochette.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg)
                        Dim tailleMo = ms.Length / 1024.0 / 1024.0
                        If tailleMo >= 1.0 Then
                            LabelTailleImage.Text = $"{tailleMo:F2} Mo"
                        Else
                            Dim tailleKo = ms.Length / 1024.0
                            LabelTailleImage.Text = $"{tailleKo:F0} Ko"
                        End If
                    End Using
                End If
            Else
                ' Pas d'image, vider les labels
                Label_DimImage.Text = ""
                LabelTailleImage.Text = ""
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur mise à jour infos pochette: {ex.Message}")
            Label_DimImage.Text = ""
            LabelTailleImage.Text = ""
        End Try
    End Sub

    ''' <summary>
    ''' Empêche le changement de checkbox pour l'item en cours d'édition uniquement
    ''' </summary>
    Private Sub ListViewCompress_ItemCheck(sender As Object, e As ItemCheckEventArgs) Handles ListViewCompress.ItemCheck
        ' Bloquer uniquement si on est en train d'éditer cet item spécifique
        If editingItem IsNot Nothing AndAlso e.Index = editingItem.Index Then
            ' Annuler le changement et garder l'état original
            e.NewValue = If(editingItemOriginalCheckedState, CheckState.Checked, CheckState.Unchecked)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Changement de checkbox bloqué pour l'item en édition (index {e.Index})")
        End If
    End Sub

    ''' <summary>
    ''' Détecte le clic AVANT le changement de checkbox pour protéger les zones éditables
    ''' </summary>
    Private Sub ListViewCompress_MouseDown(sender As Object, e As MouseEventArgs) Handles ListViewCompress.MouseDown
        Try
            ' Si on est déjà en train d'éditer, ne rien faire
            If editTextBox IsNot Nothing Then
                Return
            End If

            Dim hitTest = ListViewCompress.HitTest(e.Location)

            ' Si on clique sur un item et un sous-item
            If hitTest.Item IsNot Nothing AndAlso hitTest.SubItem IsNot Nothing Then
                Dim subItemIndex As Integer = hitTest.Item.SubItems.IndexOf(hitTest.SubItem)

                ' Si c'est une colonne éditable (Titre ou Artiste)
                If subItemIndex = 1 OrElse subItemIndex = 2 Then
                    ' Préparer la protection pour cet item
                    editingItem = hitTest.Item
                    editingItemOriginalCheckedState = hitTest.Item.Checked
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Protection activée pour index {hitTest.Item.Index}, état: {editingItemOriginalCheckedState}")
                Else
                    ' Si on clique ailleurs, désactiver la protection
                    editingItem = Nothing
                End If
            Else
                editingItem = Nothing
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur MouseDown: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire du double-clic sur la ListView pour éditer Titre ou Artiste
    ''' </summary>
    Private Sub ListViewCompress_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles ListViewCompress.MouseDoubleClick
        Try
            Dim hitTest = ListViewCompress.HitTest(e.Location)

            ' Vérifier qu'on a cliqué sur un item et un sous-item
            If hitTest.Item IsNot Nothing AndAlso hitTest.SubItem IsNot Nothing Then
                ' Déterminer l'index du sous-item
                Dim subItemIndex As Integer = hitTest.Item.SubItems.IndexOf(hitTest.SubItem)

                ' Permettre l'édition seulement pour les colonnes Titre (1) et Artiste (2)
                If subItemIndex = 1 OrElse subItemIndex = 2 Then
                    ' editingItem et editingItemOriginalCheckedState ont déjà été configurés par MouseDown
                    DemarrerEdition(hitTest.Item, subItemIndex)
                End If
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur MouseDoubleClick: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Démarre l'édition d'une cellule de la ListView
    ''' </summary>
    Private Sub DemarrerEdition(item As ListViewItem, subItemIndex As Integer)
        Try
            ' Fermer toute édition en cours
            TerminerEdition(True)

            editingItem = item
            editingSubItemIndex = subItemIndex

            ' Obtenir les coordonnées du sous-item
            Dim subItemRect As Rectangle = item.SubItems(subItemIndex).Bounds

            ' Créer le TextBox d'édition
            editTextBox = New TextBox() With {
                .Bounds = subItemRect,
                .Text = item.SubItems(subItemIndex).Text,
                .BorderStyle = BorderStyle.FixedSingle,
                .Font = ListViewCompress.Font,
                .BackColor = Color.LightYellow
            }

            ' Ajouter les gestionnaires d'événements
            AddHandler editTextBox.KeyPress, AddressOf EditTextBox_KeyPress
            AddHandler editTextBox.LostFocus, AddressOf EditTextBox_LostFocus

            ' Ajouter le TextBox à la ListView
            ListViewCompress.Controls.Add(editTextBox)
            editTextBox.Focus()
            editTextBox.SelectAll()

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Édition démarrée - Colonne {subItemIndex}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur DemarrerEdition: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gère les touches dans le TextBox d'édition
    ''' </summary>
    Private Sub EditTextBox_KeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Enter) Then
            e.Handled = True
            TerminerEdition(True) ' Valider
        ElseIf e.KeyChar = ChrW(Keys.Escape) Then
            e.Handled = True
            TerminerEdition(False) ' Annuler
        End If
    End Sub

    ''' <summary>
    ''' Gère la perte de focus du TextBox d'édition
    ''' </summary>
    Private Sub EditTextBox_LostFocus(sender As Object, e As EventArgs)
        TerminerEdition(True) ' Valider par défaut
    End Sub

    ''' <summary>
    ''' Termine l'édition et met à jour la valeur
    ''' </summary>
    Private Sub TerminerEdition(valider As Boolean)
        Try
            If editTextBox IsNot Nothing Then
                ' Retirer les gestionnaires d'événements
                RemoveHandler editTextBox.KeyPress, AddressOf EditTextBox_KeyPress
                RemoveHandler editTextBox.LostFocus, AddressOf EditTextBox_LostFocus

                ' Mettre à jour la valeur si validé
                If valider AndAlso editingItem IsNot Nothing AndAlso editingSubItemIndex >= 0 Then
                    Dim nouvelleValeur As String = editTextBox.Text.Trim()
                    editingItem.SubItems(editingSubItemIndex).Text = nouvelleValeur

                    Dim colonneNom As String = If(editingSubItemIndex = 1, "Titre", "Artiste")
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] {colonneNom} mis à jour: {nouvelleValeur}")
                End If

                ' Supprimer le TextBox
                ListViewCompress.Controls.Remove(editTextBox)
                editTextBox.Dispose()
                editTextBox = Nothing
                editingItem = Nothing
                editingSubItemIndex = -1

                ' Redonner le focus à la ListView
                ListViewCompress.Focus()
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur TerminerEdition: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire pour sélectionner/désélectionner toutes les pistes
    ''' </summary>
    Private Sub CheckBox_FCompress_SelectDeselect_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_FCompress_SelectDeselect.CheckedChanged
        Try
            ' Éviter les boucles infinies en cas de modification programmatique
            If ListViewCompress Is Nothing OrElse ListViewCompress.Items.Count = 0 Then
                Return
            End If

            ' Désactiver temporairement la protection pour permettre le SelectDeselect
            Dim tempEditingItem = editingItem
            editingItem = Nothing

            ' Cocher ou décocher toutes les pistes selon l'état de la CheckBox
            Dim etatCoche As Boolean = CheckBox_FCompress_SelectDeselect.Checked

            For Each item As ListViewItem In ListViewCompress.Items
                item.Checked = etatCoche
            Next

            ' Restaurer la protection si on était en train d'éditer
            editingItem = tempEditingItem

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Toutes les pistes {If(etatCoche, "cochées", "décochées")}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur lors de la sélection/désélection: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton EditTracks - Ouvre FormEditTracks pour éditer les métadonnées
    ''' </summary>
    Private Sub Button_EditTracks_Click(sender As Object, e As EventArgs) Handles Button_EditTracks.Click
        Try
            ' Vérifier qu'il y a des pistes à éditer
            If ListViewCompress Is Nothing OrElse ListViewCompress.Items.Count = 0 Then
                MessageBox.Show("Aucune piste à éditer.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Créer et afficher le formulaire d'édition
            Using formEdit As New FormEditTracks()
                ' Initialiser avec le ListView
                formEdit.InitialiserAvecListView(ListViewCompress)

                ' Positionner le formulaire juste au-dessus de ListViewCompress et centré horizontalement
                ' Obtenir la position absolue de ListViewCompress à l'écran
                Dim listViewScreenPos = ListViewCompress.PointToScreen(New Point(0, 0))

                ' Calculer la position X centrée horizontalement avec FormCompresser
                Dim centreFormCompresser = Me.Left + (Me.Width \ 2)
                Dim posX = centreFormCompresser - (formEdit.Width \ 2)

                ' Position Y juste au-dessus de ListViewCompress (avec un petit décalage)
                Dim posY = listViewScreenPos.Y - formEdit.Height - 10

                ' S'assurer que le formulaire reste dans les limites de l'écran
                If posX < 0 Then posX = 10
                If posY < 0 Then posY = 10

                ' StartPosition doit être Manual pour pouvoir définir la position
                formEdit.StartPosition = FormStartPosition.Manual
                formEdit.Location = New Point(posX, posY)

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] FormEditTracks positionnée à ({posX}, {posY})")

                ' Afficher en mode modal
                Dim result = formEdit.ShowDialog(Me)

                ' Les modifications sont automatiquement appliquées au ListView
                If result = DialogResult.OK Then
                    System.Diagnostics.Debug.WriteLine("[FormCompresser] Édition des pistes terminée avec succès")
                End If
            End Using
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur Button_EditTracks_Click: {ex.Message}")
            MessageBox.Show($"Erreur lors de l'ouverture de l'éditeur : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton Quitter
    ''' </summary>
    Private Sub ButtonQuitter_Click(sender As Object, e As EventArgs) Handles ButtonQuitter.Click
        ' Fermer FormCompresser et revenir à FormSelecteurPistesCD
        ' (FormSelecteurPistesCD est en attente avec ShowDialog, elle réapparaîtra automatiquement)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton Soumettre à GnuDB
    ''' </summary>
    Private Async Sub ButtonSoumettreGnuDB_Click(sender As Object, e As EventArgs) Handles ButtonSoumettreGnuDB.Click
        Try
            System.Diagnostics.Debug.WriteLine("[FormCompresser] Clic sur Soumettre à GnuDB")

            ' Valider les métadonnées avant soumission
            If String.IsNullOrWhiteSpace(TextBoxCDArtiste.Text) OrElse String.IsNullOrWhiteSpace(TextBoxCDTitre.Text) Then
                MessageBox.Show(
                    LanguageManager.GetString("GnuDB_SubmitMissingData"),
                    LanguageManager.GetString("GnuDB_SubmitErrorTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Return
            End If

            ' Vérifier que toutes les pistes ont un titre
            For Each item As ListViewItem In ListViewCompress.Items
                If item.SubItems.Count < 2 OrElse String.IsNullOrWhiteSpace(item.SubItems(1).Text) Then
                    MessageBox.Show(
                        LanguageManager.GetString("GnuDB_SubmitMissingData"),
                        LanguageManager.GetString("GnuDB_SubmitErrorTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
                    Return
                End If
            Next

            ' Demander l'adresse email de l'utilisateur (OBLIGATOIRE selon spec GnuDB)
            Dim formEmail As New FormInputEmail(
                LanguageManager.GetString("GnuDB_EmailPrompt"),
                LanguageManager.GetString("GnuDB_EmailTitle"),
                "")

            Dim emailUtilisateur As String = ""
            If formEmail.ShowDialog(Me) = DialogResult.OK Then
                emailUtilisateur = formEmail.Email
            Else
                ' L'utilisateur a annulé, sortir silencieusement
                Return
            End If

            ' Valider l'email seulement si l'utilisateur n'a pas annulé
            If String.IsNullOrWhiteSpace(emailUtilisateur) OrElse Not emailUtilisateur.Contains("@") Then
                MessageBox.Show(
                    LanguageManager.GetString("GnuDB_EmailInvalid"),
                    LanguageManager.GetString("GnuDB_EmailMissing"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Return
            End If

            ' Demander la catégorie
            Dim categoriesGnuDB = {"blues", "classical", "country", "data", "folk", "jazz", "misc", "newage", "reggae", "rock", "soundtrack"}
            Dim categorieChoisie = "rock" ' Par défaut

            ' Essayer de mapper le genre actuel vers une catégorie GnuDB
            If ComboBoxGenre.SelectedItem IsNot Nothing Then
                Dim genreActuel = ComboBoxGenre.SelectedItem.ToString().ToLower()
                If categoriesGnuDB.Contains(genreActuel) Then
                    categorieChoisie = genreActuel
                ElseIf genreActuel.Contains("class") Then
                    categorieChoisie = "classical"
                ElseIf genreActuel.Contains("countr") Then
                    categorieChoisie = "country"
                ElseIf genreActuel.Contains("folk") Then
                    categorieChoisie = "folk"
                ElseIf genreActuel.Contains("jazz") Then
                    categorieChoisie = "jazz"
                ElseIf genreActuel.Contains("reggae") Then
                    categorieChoisie = "reggae"
                End If
            End If

            ' Demander confirmation et choix test/submit
            Dim modeTest = MessageBox.Show(
                String.Format(LanguageManager.GetString("GnuDB_Category"), categorieChoisie) & vbCrLf & vbCrLf &
                LanguageManager.GetString("GnuDB_ModePrompt"),
                LanguageManager.GetString("GnuDB_ModeTitle"),
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question)

            If modeTest = DialogResult.Cancel Then
                Return
            End If

            Dim estModeTest = (modeTest = DialogResult.Yes)

            ' Préparer les métadonnées
            Dim cdInfo As New CDMetadataProvider.CDInfo()
            cdInfo.Artist = TextBoxCDArtiste.Text.Trim()
            cdInfo.Album = TextBoxCDTitre.Text.Trim()
            If Integer.TryParse(TextBoxAnnee.Text, cdInfo.Year) = False Then
                cdInfo.Year = 0
            End If
            cdInfo.Genre = categorieChoisie
            cdInfo.Tracks = New List(Of CDMetadataProvider.TrackInfo)()

            ' Ajouter les pistes
            For i As Integer = 0 To pistesCD.Count - 1
                Dim trackInfo As New CDMetadataProvider.TrackInfo()
                trackInfo.TrackNumber = pistesCD(i).TrackNumber
                trackInfo.Title = ListViewCompress.Items(i).SubItems(1).Text ' Colonne Titre
                trackInfo.Artist = ListViewCompress.Items(i).SubItems(2).Text ' Colonne Artiste
                trackInfo.Duration = pistesCD(i).Duration
                cdInfo.Tracks.Add(trackInfo)
            Next

            ' Calculer le DiscID CDDB
            Dim discID As String = GnuDBMetadataProvider.CalculerCDDBDiscID(pistesCD)

            If String.IsNullOrWhiteSpace(discID) Then
                MessageBox.Show(
                    LanguageManager.GetString("GnuDB_DiscIDError"),
                    LanguageManager.GetString("GnuDB_Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)
                Return
            End If

            ' Afficher un curseur d'attente
            Me.Cursor = Cursors.WaitCursor
            Try
                ' Soumettre via HTTP POST (format conforme GnuDB)
                Dim resultat = Await GnuDBMetadataProvider.SoumettreViaHTTP(discID, categorieChoisie, cdInfo, pistesCD, emailUtilisateur, estModeTest)

                ' Afficher le résultat
                MessageBox.Show(
                    resultat,
                    If(estModeTest, LanguageManager.GetString("GnuDB_TestTitle"), LanguageManager.GetString("GnuDB_SubmitSuccessTitle")),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)

                System.Diagnostics.Debug.WriteLine("[FormCompresser] Soumission GnuDB terminée avec succès")

            Finally
                Me.Cursor = Cursors.Default
            End Try

        Catch ex As Exception
            Me.Cursor = Cursors.Default
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur ButtonSoumettreGnuDB_Click: {ex.Message}")
            MessageBox.Show(
                LanguageManager.GetString("GnuDB_SubmitError") & vbCrLf & vbCrLf & ex.Message,
                LanguageManager.GetString("GnuDB_SubmitErrorTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub


    ' ==================== NAVIGATION POCHETTES ====================

    ''' <summary>
    ''' Bouton Précédent - Revenir à la pochette précédente dans l'historique
    ''' </summary>
    Private Async Sub Button_Image_Prec_Click(sender As Object, e As EventArgs) Handles Button_Image_Prec.Click
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ═══ CLIC PRÉCÉDENT ═══")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Avant: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")

            If indexPochetteActuelle > 0 Then
                indexPochetteActuelle -= 1
                Dim url = historiquePochettes(indexPochetteActuelle)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Navigation ← Précédent: {url} (index {indexPochetteActuelle})")

                ' Ne PAS ajouter à l'historique ni modifier les métadonnées pendant la navigation
                Await TelechargerEtAfficherPochette(url, ajouterHistorique:=False)

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Après téléchargement: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")

                ' NE PAS mettre à jour metadonneesCD.CoverArtUrl ici - seulement au moment de l'extraction
                MettreAJourBoutonsNavigation()
            Else
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Impossible d'aller en arrière (index={indexPochetteActuelle})")
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur Button_Image_Prec: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Bouton Suivant - Chercher une nouvelle pochette alternative
    ''' </summary>
    Private Async Sub Button_Image_Suiv_Click(sender As Object, e As EventArgs) Handles Button_Image_Suiv.Click
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ═══ CLIC SUIVANT ═══")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Avant: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")

            ' Si on a déjà un historique et qu'on n'est pas à la fin, avancer dans l'historique
            If indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count - 1 Then
                indexPochetteActuelle += 1
                Dim url = historiquePochettes(indexPochetteActuelle)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Navigation → Suivant (historique): {url} (index {indexPochetteActuelle})")

                ' Ne PAS ajouter à l'historique ni modifier les métadonnées pendant la navigation
                Await TelechargerEtAfficherPochette(url, ajouterHistorique:=False)

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Après téléchargement: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")

                ' NE PAS mettre à jour metadonneesCD.CoverArtUrl ici - seulement au moment de l'extraction
                MettreAJourBoutonsNavigation()
                Return
            End If

            ' Sinon, chercher une nouvelle pochette alternative
            If metadonneesCD IsNot Nothing AndAlso
               Not String.IsNullOrWhiteSpace(metadonneesCD.Artist) AndAlso
               Not String.IsNullOrWhiteSpace(metadonneesCD.Album) Then

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche nouvelle pochette alternative...")
                Dim resultat = Await RechercherPochetteSuivante(metadonneesCD.Artist, metadonneesCD.Album)

                If Not String.IsNullOrWhiteSpace(resultat.url) Then
                    ' Ajouter à l'historique (supprimer tout ce qui suit l'index actuel)
                    If indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count - 1 Then
                        historiquePochettes.RemoveRange(indexPochetteActuelle + 1, historiquePochettes.Count - indexPochetteActuelle - 1)
                    End If

                    historiquePochettes.Add(resultat.url)
                    ' Enregistrer la source
                    If Not String.IsNullOrWhiteSpace(resultat.source) Then
                        sourcesPochettes(resultat.url) = resultat.source
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Source enregistrée: {resultat.source} pour {resultat.url}")
                    End If
                    indexPochetteActuelle = historiquePochettes.Count - 1
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Nouvelle pochette trouvée: {resultat.url}")

                    ' Ne PAS ajouter à l'historique car déjà fait manuellement ci-dessus
                    Await TelechargerEtAfficherPochette(resultat.url, ajouterHistorique:=False)
                    ' NE PAS mettre à jour metadonneesCD.CoverArtUrl ici - seulement au moment de l'extraction
                    MettreAJourBoutonsNavigation()
                Else
                    MessageBox.Show(
                        LanguageManager.GetString("Compressor_NoMoreCovers"),
                        LanguageManager.GetString("Compressor_SearchTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)

                    ' Remettre le focus sur la PictureBox pour éviter que le bouton reste "enfoncé"
                    PictureBoxPochette.Focus()

                    ' Forcer le rafraîchissement des boutons
                    Button_Image_Suiv.Refresh()
                    Button_Image_Prec.Refresh()
                End If
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur Button_Image_Suiv: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Bouton Effacer - Supprimer l'image de la pochette
    ''' </summary>
    Private Sub Button_Image_Erase_Click(sender As Object, e As EventArgs) Handles Button_Image_Erase.Click
        Try
            ' Nettoyer l'image
            If PictureBoxPochette.Image IsNot Nothing Then
                Dim oldImage = PictureBoxPochette.Image
                PictureBoxPochette.Image = Nothing
                oldImage.Dispose()
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⚠️ IMAGE EFFACÉE par Button_Image_Erase")
            End If

            ' Effacer les infos
            Label_DimImage.Text = ""
            LabelTailleImage.Text = ""

            ' Réinitialiser l'historique et le cache
            historiquePochettes.Clear()
            indexPochetteActuelle = -1
            pochetteTempUrl = Nothing
            pochetteTempBytes = Nothing

            If metadonneesCD IsNot Nothing Then
                metadonneesCD.CoverArtUrl = Nothing
            End If

            MettreAJourBoutonsNavigation()
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Pochette effacée")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur Button_Image_Erase: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Met à jour l'état des boutons de navigation
    ''' </summary>
    Private Sub MettreAJourBoutonsNavigation()
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🔄 MettreAJourBoutonsNavigation: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}, Image={If(PictureBoxPochette.Image Is Nothing, "NULL", "OK")}")

            If Button_Image_Prec IsNot Nothing Then
                Button_Image_Prec.Enabled = (indexPochetteActuelle > 0)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Button_Image_Prec.Enabled = {Button_Image_Prec.Enabled}")
            End If

            If Button_Image_Suiv IsNot Nothing Then
                ' Toujours actif pour chercher de nouvelles pochettes
                Button_Image_Suiv.Enabled = (metadonneesCD IsNot Nothing)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Button_Image_Suiv.Enabled = {Button_Image_Suiv.Enabled}")
            End If

            If Button_Image_Erase IsNot Nothing Then
                Button_Image_Erase.Enabled = (PictureBoxPochette.Image IsNot Nothing)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Button_Image_Erase.Enabled = {Button_Image_Erase.Enabled}")
            End If

            ' Mettre à jour le label de la source de l'image
            If Label_Image_Site IsNot Nothing Then
                If indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count Then
                    Dim urlActuelle = historiquePochettes(indexPochetteActuelle)
                    If sourcesPochettes.ContainsKey(urlActuelle) Then
                        Label_Image_Site.Text = sourcesPochettes(urlActuelle)
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Label_Image_Site.Text = '{sourcesPochettes(urlActuelle)}'")
                    Else
                        Label_Image_Site.Text = ""
                    End If
                Else
                    Label_Image_Site.Text = ""
                End If
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur MettreAJourBoutonsNavigation: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Recherche une pochette alternative suivante en cascade sur toutes les sources
    ''' </summary>
    Private Async Function RechercherPochetteSuivante(artiste As String, album As String) As Task(Of (url As String, source As String))
        Try
            ' Déterminer quelle source a été utilisée en dernier
            Dim derniereUrl = If(indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count,
                                 historiquePochettes(indexPochetteActuelle),
                                 "")

            ' Déterminer l'ordre des sources à essayer (toutes les 8 sources maintenant)
            Dim ordreRecherche As New List(Of String)

            If derniereUrl.Contains("itunes.apple.com") Then
                ordreRecherche.AddRange({"LastFm", "Deezer", "Discogs", "TheAudioDB", "FanartTV", "MusicBrainz", "iTunes"})
            ElseIf derniereUrl.Contains("last.fm") OrElse derniereUrl.Contains("lastfm") Then
                ordreRecherche.AddRange({"Deezer", "Discogs", "iTunes", "TheAudioDB", "FanartTV", "MusicBrainz", "LastFm"})
            ElseIf derniereUrl.Contains("deezer.com") Then
                ordreRecherche.AddRange({"Discogs", "iTunes", "LastFm", "TheAudioDB", "FanartTV", "MusicBrainz", "Deezer"})
            ElseIf derniereUrl.Contains("discogs.com") Then
                ordreRecherche.AddRange({"TheAudioDB", "FanartTV", "iTunes", "LastFm", "Deezer", "MusicBrainz", "Discogs"})
            ElseIf derniereUrl.Contains("theaudiodb.com") Then
                ordreRecherche.AddRange({"FanartTV", "iTunes", "LastFm", "Deezer", "Discogs", "MusicBrainz", "TheAudioDB"})
            ElseIf derniereUrl.Contains("fanart.tv") Then
                ordreRecherche.AddRange({"iTunes", "LastFm", "Deezer", "Discogs", "TheAudioDB", "MusicBrainz", "FanartTV"})
            ElseIf derniereUrl.Contains("coverartarchive.org") Then
                ordreRecherche.AddRange({"iTunes", "LastFm", "Deezer", "Discogs", "TheAudioDB", "FanartTV", "MusicBrainz"})
            Else
                ' Par défaut, essayer dans l'ordre de priorité
                ordreRecherche.AddRange({"iTunes", "LastFm", "Deezer", "Discogs", "TheAudioDB", "FanartTV", "MusicBrainz"})
            End If

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche en cascade sur: {String.Join(" → ", ordreRecherche)}")

            ' Essayer chaque source en cascade jusqu'à trouver une nouvelle pochette
            For Each source In ordreRecherche
                Dim nouvelleUrl As String = Nothing

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Essai source: {source}")

                Select Case source
                    Case "iTunes"
                        nouvelleUrl = Await RechercherPochetteiTunes(artiste, album)
                    Case "LastFm"
                        nouvelleUrl = Await RechercherPochetteLastFm(artiste, album)
                    Case "MusicBrainz"
                        nouvelleUrl = Await RechercherPochetteMusicBrainz(artiste, album)
                    Case "Deezer"
                        nouvelleUrl = Await RechercherPochetteDeezer(artiste, album)
                    Case "Discogs"
                        nouvelleUrl = Await RechercherPochetteDiscogs(artiste, album)
                    Case "TheAudioDB"
                        nouvelleUrl = Await RechercherPochetteTheAudioDB(artiste, album)
                    Case "FanartTV"
                        nouvelleUrl = Await RechercherPochetteFanartTV(artiste, album)
                End Select

                ' Vérifier si on a trouvé une URL et qu'elle n'est pas déjà dans l'historique
                If Not String.IsNullOrWhiteSpace(nouvelleUrl) Then
                    If Not historiquePochettes.Contains(nouvelleUrl) Then
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Nouvelle pochette trouvée sur {source}: {nouvelleUrl}")
                        ' Mapper le nom de source
                        Dim nomSource = source
                        If source = "LastFm" Then nomSource = "Last.fm"
                        If source = "FanartTV" Then nomSource = "Fanart.tv"
                        Return (nouvelleUrl, nomSource)
                    Else
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ URL déjà dans l'historique, continuer...")
                    End If
                Else
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Aucune pochette sur {source}, continuer...")
                End If
            Next

            ' Aucune nouvelle pochette trouvée sur aucune source
            System.Diagnostics.Debug.WriteLine("[FormCompresser] Aucune nouvelle pochette trouvée sur toutes les sources")
            Return (Nothing, Nothing)

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur RechercherPochetteSuivante: {ex.Message}")
            Return (Nothing, Nothing)
        End Try
    End Function

    ''' <summary>
    ''' Ajouter une URL à l'historique lors du chargement d'une pochette
    ''' </summary>
    Private Sub AjouterAHistoriquePochettes(url As String)
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] 📝 AjouterAHistoriquePochettes: '{url}'")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Avant: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")

            If String.IsNullOrWhiteSpace(url) Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    ⚠️ URL vide, abandon")
                Return
            End If

            ' Si pas encore dans l'historique ou c'est une nouvelle image
            If Not historiquePochettes.Contains(url) Then
                ' Supprimer tout ce qui suit l'index actuel
                If indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count - 1 Then
                    Dim nbSupprimes = historiquePochettes.Count - indexPochetteActuelle - 1
                    historiquePochettes.RemoveRange(indexPochetteActuelle + 1, nbSupprimes)
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Supprimé {nbSupprimes} élément(s) après l'index actuel")
                End If

                historiquePochettes.Add(url)
                indexPochetteActuelle = historiquePochettes.Count - 1
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    ✅ Ajouté à l'historique")
            Else
                ' Mettre à jour l'index si l'URL existe déjà
                indexPochetteActuelle = historiquePochettes.IndexOf(url)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    ℹ️ URL existante, index mis à jour")
            End If

            System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Après: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")
            MettreAJourBoutonsNavigation()
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur AjouterAHistoriquePochettes: {ex.Message}")
        End Try
    End Sub

End Class
