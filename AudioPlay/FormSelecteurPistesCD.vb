Imports System.Windows.Forms
Imports System.IO

Partial Public Class FormSelecteurPistesCD

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

    Private pistesCD As List(Of CDAudioManager.CDTrack)
    Private pistesSelectionnees As New List(Of CDAudioManager.CDTrack)
    Private lecteur As String
    Private cdMetadata As CDMetadataProvider.CDInfo

    ' Surveillance du CD
    Private cdMonitorTimer As Timer
    Private dernierDiscId As String = ""

    Public Sub New(lecteurCD As String, pistes As List(Of CDAudioManager.CDTrack))
        ' Appeler InitializeComponent généré par le designer
        InitializeComponent()

        ' Appliquer le thème actuel
        ThemeManager.ApplyThemeToForm(Me)

        Me.lecteur = lecteurCD
        Me.pistesCD = pistes

        ' Calculer l'ID initial du CD
        dernierDiscId = CalculerDiscIdSimple(pistes)

        ' Configurer le formulaire et charger les pistes
        ConfigurerFormulaire()
        ChargerPistes()

        ' Démarrer la surveillance du CD
        InitialiserSurveillanceCD()

        ' Définir GnuDB comme source par défaut
        comboSourceMetadonnees.SelectedIndex = 0

        ' Le chargement automatique se fera dans l'événement Shown
    End Sub

    ''' <summary>
    ''' Événement déclenché quand le formulaire est complètement affiché
    ''' </summary>
    Private Sub FormSelecteurPistesCD_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ' Charger automatiquement maintenant que tous les contrôles sont affichés
        TenterChargerAutomatique()
    End Sub

    Private Sub ConfigurerFormulaire()
        Me.Text = LanguageManager.GetString("CDSelector_FormTitle")
        lblTitre.Text = String.Format(LanguageManager.GetString("CDSelector_TitleWithDrive"), lecteur)
        btnOK.Text = LanguageManager.GetString("CDSelector_ButtonAdd")
        btnAnnuler.Text = LanguageManager.GetString("CDSelector_ButtonCancel")
        ButtonExtraction.Text = LanguageManager.GetString("CDSelector_ButtonExtraction")
        CheckBox_FSelect_SelectDeselect.Text = LanguageManager.GetString("CDSelector_SelectDeselectAll")
    End Sub

    Private Sub ChargerPistes()
        checkedListPistes.Items.Clear()
        For Each piste In pistesCD
            Dim duree = TimeSpan.FromSeconds(piste.Duration.TotalSeconds)
            Dim texte = $"Piste {piste.TrackNumber:D2} [{duree:mm\:ss}]"
            checkedListPistes.Items.Add(texte, True) ' Par défaut, toutes les pistes sont sélectionnées
        Next
    End Sub

    ''' <summary>
    ''' Tente de charger automatiquement les métadonnées (cache puis GnuDB)
    ''' </summary>
    Private Sub TenterChargerAutomatique()
        If lblChargement Is Nothing Then Return

        Try
            ' 1. Vérifier d'abord le cache
            Dim discId = CDMetadataProvider.CalculerDiscID(pistesCD)

            If CDMetadataCache.ExisteDansCache(discId) Then
                lblChargement.Text = LanguageManager.GetString("CDSelector_LoadedFromCache")
                lblChargement.ForeColor = Color.Green
                lblChargement.Visible = True

                Dim cachedInfo = CDMetadataCache.RecupererMetadonnees(discId)
                If cachedInfo IsNot Nothing Then
                    cdMetadata = cachedInfo
                    AppliquerMetadonnees()
                    Return
                End If
            End If

            ' 2. Sinon, charger automatiquement depuis GnuDB
            lblChargement.Text = LanguageManager.GetString("CDSelector_LoadingAutoGnuDB")
            lblChargement.ForeColor = Color.Blue
            lblChargement.Visible = True
            Application.DoEvents()

            ChargerMetadonneesGnuDB()

        Catch ex As Exception
            lblChargement.Text = String.Format(LanguageManager.GetString("CDSelector_ErrorAutoLoad"), ex.Message)
            lblChargement.ForeColor = Color.Red
            lblChargement.Visible = True
        End Try
    End Sub

    ''' <summary>
    ''' Ré-initialise le formulaire après retour de FormCompresser
    ''' Détecte les lecteurs CD et recharge les informations si un CD audio est présent
    ''' </summary>
    Private Sub ReinitialiserFormulaire()
        Try
            ' Vider les contrôles
            checkedListPistes.Items.Clear()
            cdMetadata = Nothing
            pistesCD = Nothing
            lecteur = Nothing
            dernierDiscId = ""

            ' Chercher un lecteur CD contenant un CD audio
            Dim lecteurs = CDAudioManager.DetecterLecteursCDAudio()
            Dim lecteurAvecCD As String = Nothing
            Dim nouvellesPistes As List(Of CDAudioManager.CDTrack) = Nothing

            For Each lecteurCandidat In lecteurs
                If CDAudioManager.EstCDAudioPresent(lecteurCandidat) Then
                    ' Lire les pistes du CD
                    nouvellesPistes = CDAudioManager.LirePistesCD(lecteurCandidat)
                    If nouvellesPistes IsNot Nothing AndAlso nouvellesPistes.Count > 0 Then
                        lecteurAvecCD = lecteurCandidat
                        Exit For
                    End If
                End If
            Next

            ' Si un lecteur avec CD audio est trouvé
            If lecteurAvecCD IsNot Nothing AndAlso nouvellesPistes IsNot Nothing Then
                ' Mettre à jour les variables d'instance
                lecteur = lecteurAvecCD
                pistesCD = nouvellesPistes
                dernierDiscId = CalculerDiscIdSimple(pistesCD)

                ' Recharger l'interface
                lblTitre.Text = String.Format(LanguageManager.GetString("CDSelector_TitleWithDrive"), lecteur)
                ChargerPistes()
                TenterChargerAutomatique()

                ' Réactiver les boutons
                btnChargerMetadonnees.Enabled = True
                ButtonExtraction.Enabled = True
            Else
                ' Aucun CD audio trouvé - réinitialiser complètement
                lblTitre.Text = LanguageManager.GetString("CDSelector_Title")
                lblChargement.Text = "Aucun CD audio détecté dans les lecteurs disponibles."
                lblChargement.ForeColor = Color.Orange
                lblChargement.Visible = True

                ' Désactiver les boutons
                btnChargerMetadonnees.Enabled = False
                ButtonExtraction.Enabled = False

                ' Effacer le cache (optionnel)
                CDMetadataCache.EffacerCache()
            End If

        Catch ex As Exception
            MessageBox.Show($"Erreur lors de la ré-initialisation: {ex.Message}",
                          "Erreur",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Sub ChargerMetadonneesGnuDB()
        Try
            Dim provider = New GnuDBMetadataProvider()
            cdMetadata = Await provider.RechercherCD(pistesCD)

            If cdMetadata IsNot Nothing AndAlso cdMetadata.Tracks IsNot Nothing AndAlso cdMetadata.Tracks.Count > 0 Then
                lblChargement.Text = String.Format(LanguageManager.GetString("CDSelector_AlbumFound"), cdMetadata.Artist, cdMetadata.Album)
                lblChargement.ForeColor = Color.Green
                lblChargement.Visible = True

                AppliquerMetadonnees()

                ' Sauvegarder dans le cache
                Dim discId = CDMetadataProvider.CalculerDiscID(pistesCD)
                CDMetadataCache.SauvegarderMetadonnees(discId, cdMetadata, "GnuDB")
            Else
                lblChargement.Text = LanguageManager.GetString("CDSelector_NoMetadataGnuDB")
                lblChargement.ForeColor = Color.Orange
                lblChargement.Visible = True
            End If

        Catch ex As Exception
            lblChargement.Text = String.Format(LanguageManager.GetString("CDSelector_ErrorGnuDB"), ex.Message)
            lblChargement.ForeColor = Color.Red
            lblChargement.Visible = True
        End Try
    End Sub

    Private Sub AppliquerMetadonnees()
        If cdMetadata Is Nothing OrElse cdMetadata.Tracks Is Nothing Then Return

        checkedListPistes.Items.Clear()

        For i = 0 To Math.Min(pistesCD.Count - 1, cdMetadata.Tracks.Count - 1)
            Dim piste = pistesCD(i)
            Dim trackInfo = cdMetadata.Tracks(i)
            Dim duree = TimeSpan.FromSeconds(piste.Duration.TotalSeconds)

            ' Format: 01. Artiste - Titre [03:46]
            Dim artisteAAfficher As String = If(Not String.IsNullOrWhiteSpace(trackInfo.Artist), trackInfo.Artist, cdMetadata.Artist)
            Dim texte = $"{piste.TrackNumber:D2}. {artisteAAfficher} - {trackInfo.Title} [{duree:mm\:ss}]"

            checkedListPistes.Items.Add(texte, True)

            ' Mettre à jour les informations de la piste
            piste.Title = trackInfo.Title
            piste.Artist = artisteAAfficher
        Next
    End Sub

    ''' <summary>
    ''' Gestionnaire pour le changement de source de métadonnées
    ''' Charge automatiquement quand l'utilisateur change de source
    ''' </summary>
    Private Sub ComboSourceMetadonnees_SelectedIndexChanged(sender As Object, e As EventArgs) Handles comboSourceMetadonnees.SelectedIndexChanged
        ' Ne pas charger automatiquement au démarrage (l'événement Shown le fait déjà)
        If Not Me.Visible Then Return

        Dim sourceSelectionnee = comboSourceMetadonnees.SelectedItem?.ToString()
        If String.IsNullOrEmpty(sourceSelectionnee) Then Return

        ' Charger automatiquement selon la source sélectionnée
        Select Case sourceSelectionnee
            Case LanguageManager.GetString("CDSelector_SourceGnuDB")
                ChargerMetadonneesGnuDB()

            Case LanguageManager.GetString("CDSelector_SourceMusicBrainz")
                ChargerMetadonneesMusicBrainz()

            Case LanguageManager.GetString("CDSelector_SourceDiscogs")
                ChargerMetadonneesDiscogs()

            Case LanguageManager.GetString("CDSelector_SourceManual")
                ' Réinitialiser à l'état de base
                ChargerPistes()
                lblChargement.Text = LanguageManager.GetString("CDSelector_ManualMode")
                lblChargement.ForeColor = Color.Blue
                lblChargement.Visible = True
        End Select
    End Sub

    Private Sub BtnChargerMetadonnees_Click(sender As Object, e As EventArgs) Handles btnChargerMetadonnees.Click
        Dim sourceSelectionnee = comboSourceMetadonnees.SelectedItem?.ToString()

        Select Case sourceSelectionnee
            Case LanguageManager.GetString("CDSelector_SourceGnuDB")
                ChargerMetadonneesGnuDB()

            Case LanguageManager.GetString("CDSelector_SourceMusicBrainz")
                ChargerMetadonneesMusicBrainz()

            Case LanguageManager.GetString("CDSelector_SourceDiscogs")
                ChargerMetadonneesDiscogs()

            Case LanguageManager.GetString("CDSelector_SourceManual")
                lblChargement.Text = LanguageManager.GetString("CDSelector_ManualInstructions")
                lblChargement.ForeColor = Color.Blue
                lblChargement.Visible = True

            Case Else
                MessageBox.Show(LanguageManager.GetString("CDSelector_SourceNotRecognized"), LanguageManager.GetString("CDSelector_ErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Select
    End Sub

    Private Async Sub ChargerMetadonneesMusicBrainz()
        Try
            lblChargement.Text = LanguageManager.GetString("CDSelector_LoadingMusicBrainz")
            lblChargement.ForeColor = Color.Blue
            lblChargement.Visible = True
            Application.DoEvents()

            Dim provider = New CDMetadataProvider()
            cdMetadata = Await provider.ObtenirMetadonnees(pistesCD)

            If cdMetadata IsNot Nothing AndAlso cdMetadata.Tracks IsNot Nothing AndAlso cdMetadata.Tracks.Count > 0 Then
                lblChargement.Text = String.Format(LanguageManager.GetString("CDSelector_AlbumFound"), cdMetadata.Artist, cdMetadata.Album)
                lblChargement.ForeColor = Color.Green
                lblChargement.Visible = True

                AppliquerMetadonnees()

                Dim discId = CDMetadataProvider.CalculerDiscID(pistesCD)
                CDMetadataCache.SauvegarderMetadonnees(discId, cdMetadata, "MusicBrainz")
            Else
                lblChargement.Text = LanguageManager.GetString("CDSelector_NoMetadataMusicBrainz")
                lblChargement.ForeColor = Color.Orange
                lblChargement.Visible = True
            End If

        Catch ex As Exception
            lblChargement.Text = String.Format(LanguageManager.GetString("CDSelector_ErrorMusicBrainz"), ex.Message)
            lblChargement.ForeColor = Color.Red
            lblChargement.Visible = True
        End Try
    End Sub

    Private Async Sub ChargerMetadonneesDiscogs()
        Try
            ' Demander l'artiste et le titre de l'album
            Dim artisteRecherche = InputBox(LanguageManager.GetString("CDSelector_DiscogsArtistPrompt"), LanguageManager.GetString("CDSelector_DiscogsSearchTitle"), "")
            If String.IsNullOrWhiteSpace(artisteRecherche) Then Return

            Dim albumRecherche = InputBox(LanguageManager.GetString("CDSelector_DiscogsAlbumPrompt"), LanguageManager.GetString("CDSelector_DiscogsSearchTitle"), "")
            If String.IsNullOrWhiteSpace(albumRecherche) Then Return

            lblChargement.Text = LanguageManager.GetString("CDSelector_LoadingDiscogs")
            lblChargement.ForeColor = Color.Blue
            lblChargement.Visible = True
            Application.DoEvents()

            Dim provider = New DiscogsMetadataProvider()
            cdMetadata = Await provider.RechercherCD(artisteRecherche, albumRecherche)

            If cdMetadata IsNot Nothing AndAlso cdMetadata.Tracks IsNot Nothing AndAlso cdMetadata.Tracks.Count > 0 Then
                lblChargement.Text = String.Format(LanguageManager.GetString("CDSelector_AlbumFound"), cdMetadata.Artist, cdMetadata.Album)
                lblChargement.ForeColor = Color.Green
                lblChargement.Visible = True

                AppliquerMetadonnees()

                Dim discId = CDMetadataProvider.CalculerDiscID(pistesCD)
                CDMetadataCache.SauvegarderMetadonnees(discId, cdMetadata, "Discogs")
            Else
                lblChargement.Text = LanguageManager.GetString("CDSelector_NoMetadataDiscogs")
                lblChargement.ForeColor = Color.Orange
                lblChargement.Visible = True
            End If

        Catch ex As Exception
            lblChargement.Text = String.Format(LanguageManager.GetString("CDSelector_ErrorDiscogs"), ex.Message)
            lblChargement.ForeColor = Color.Red
            lblChargement.Visible = True
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire pour sélectionner/désélectionner toutes les pistes via la CheckBox
    ''' </summary>
    Private Sub CheckBox_FSelect_SelectDeselect_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_FSelect_SelectDeselect.CheckedChanged
        Try
            ' Éviter les actions si la liste est vide
            If checkedListPistes Is Nothing OrElse checkedListPistes.Items.Count = 0 Then
                Return
            End If

            ' Cocher ou décocher toutes les pistes selon l'état de la CheckBox
            Dim etatCoche As Boolean = CheckBox_FSelect_SelectDeselect.Checked

            For i = 0 To checkedListPistes.Items.Count - 1
                checkedListPistes.SetItemChecked(i, etatCoche)
            Next

            System.Diagnostics.Debug.WriteLine($"[FormSelecteurPistesCD] Toutes les pistes {If(etatCoche, "cochées", "décochées")}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormSelecteurPistesCD] Erreur lors de la sélection/désélection: {ex.Message}")
        End Try
    End Sub

    Private Sub ButtonExtraction_Click(sender As Object, e As EventArgs) Handles ButtonExtraction.Click
        Try
            ' Vérifier qu'on a des pistes et des métadonnées
            If pistesCD Is Nothing OrElse pistesCD.Count = 0 Then
                MessageBox.Show(LanguageManager.GetString("CDSelector_NoPistesForExtraction"),
                              LanguageManager.GetString("CDSelector_ErrorTitle"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
                Return
            End If

            ' Ouvrir le formulaire d'extraction/compression
            Dim formCompresser As New FormCompresser()

            ' Lire la préférence agrandi/rapetissé depuis parametres.txt (lecture robuste) mais n'appliquer
            ' qu'après l'initialisation du formulaire pour éviter qu'une initialisation interne n'écrase l'état.
            Dim agrVal As String = Nothing
            Try
                Dim cfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "parametres.txt")
                If File.Exists(cfgPath) Then
                    For Each raw In File.ReadAllLines(cfgPath)
                        Try
                            Dim line = raw.Trim()
                            If String.IsNullOrEmpty(line) OrElse line.StartsWith("#") Then Continue For
                            Dim idx = line.IndexOf("=")
                            If idx <= 0 Then Continue For
                            Dim key = line.Substring(0, idx).Trim()
                            Dim value = line.Substring(idx + 1).Trim()
                            If String.Equals(key, "FormCompresser_Agrandir", StringComparison.InvariantCultureIgnoreCase) Then
                                agrVal = value
                                Exit For
                            End If
                        Catch
                        End Try
                    Next
                End If
            Catch
            End Try

            ' Passer les informations du CD au formulaire
            formCompresser.InitialiserDonneesCD(lecteur, pistesCD, cdMetadata)

            ' Appliquer l'état agrandi/rapetissé après l'initialisation (si trouvé)
            If agrVal IsNot Nothing Then
                Try
                    formCompresser.ApplyAgrandirStateFromString(agrVal)
                    ' Log pour diagnostic
                    ' State debug trace disabled by default
                    Try
                        If CDAudioAnalyzer.DiagnosticsToDiskEnabled Then
                            Dim trace = Path.Combine(Path.GetTempPath(), "AudioPlay_state_debug.txt")
                            File.AppendAllText(trace, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ButtonExtraction applied agrVal={agrVal}{Environment.NewLine}")
                        End If
                    Catch
                    End Try
                Catch
                End Try
            End If

            ' Afficher FormCompresser en mode non-modal SANS owner pour qu'il soit complètement indépendant
            ' Cela permet à l'utilisateur d'utiliser Form1 librement pendant l'affichage et l'extraction du CD
            formCompresser.Show()

            ' Forcer FormCompresser au premier plan à l'ouverture avec TopMost temporaire
            formCompresser.TopMost = True
            formCompresser.BringToFront()
            formCompresser.Activate()

            ' Désactiver TopMost après un court délai pour permettre le comportement normal
            ' (sauf pendant l'extraction où il sera réactivé pour les messages)
            Task.Run(Async Function()
                         Await Task.Delay(500)
                         formCompresser.Invoke(Sub() formCompresser.TopMost = False)
                     End Function)

            ' Fermer FormSelecteurPistesCD après l'ouverture de FormCompresser
            ' ⚠️ Ne PAS utiliser DialogResult.OK, sinon Form1 ajoutera les pistes à la playlist!
            ' On utilise DialogResult.Ignore pour signaler "extraction uniquement, pas d'ajout"
            Me.DialogResult = DialogResult.Ignore
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Erreur lors de l'ouverture du formulaire d'extraction: {ex.Message}",
                          LanguageManager.GetString("CDSelector_ErrorTitle"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Retourne les métadonnées du CD chargé
    ''' </summary>
    Public Function ObtenirMetadonnees() As CDMetadataProvider.CDInfo
        Return cdMetadata
    End Function

    Public Function ObtenirPistesSelectionnees() As List(Of CDAudioManager.CDTrack)
        pistesSelectionnees.Clear()

        For i = 0 To Math.Min(checkedListPistes.Items.Count - 1, pistesCD.Count - 1)
            If checkedListPistes.GetItemChecked(i) Then
                ' Copier la piste avec ses métadonnées mises à jour
                Dim pisteOriginale = pistesCD(i)
                Dim pisteCopie = New CDAudioManager.CDTrack With {
                    .Drive = pisteOriginale.Drive,
                    .TrackNumber = pisteOriginale.TrackNumber,
                    .StartFrame = pisteOriginale.StartFrame,
                    .EndFrame = pisteOriginale.EndFrame,
                    .Duration = pisteOriginale.Duration,
                    .Title = pisteOriginale.Title,
                    .Artist = pisteOriginale.Artist
                }
                pistesSelectionnees.Add(pisteCopie)
            End If
        Next

        Return pistesSelectionnees
    End Function

    ''' <summary>
    ''' Initialise le timer de surveillance du CD
    ''' </summary>
    Private Sub InitialiserSurveillanceCD()
        cdMonitorTimer = New Timer With {
            .Interval = 2000 ' Vérifier toutes les 2 secondes
        }
        AddHandler cdMonitorTimer.Tick, AddressOf SurveillerCD
        cdMonitorTimer.Start()
    End Sub

    ''' <summary>
    ''' Vérifie si le CD a changé ou a été éjecté
    ''' </summary>
    Private Sub SurveillerCD(sender As Object, e As EventArgs)
        Try
            ' Lire les pistes actuelles du lecteur
            Dim pistesActuelles = CDAudioManager.LirePistesCD(lecteur)

            ' Calculer l'ID du CD actuel
            Dim discIdActuel = CalculerDiscIdSimple(pistesActuelles)

            ' Vérifier si le CD a changé
            If discIdActuel <> dernierDiscId Then
                ' Le CD a changé ou a été éjecté
                dernierDiscId = discIdActuel

                If pistesActuelles.Count = 0 Then
                    ' CD éjecté - vider la liste
                    Me.Invoke(Sub()
                                  pistesCD.Clear()
                                  cdMetadata = Nothing
                                  checkedListPistes.Items.Clear()
                                  lblChargement.Text = LanguageManager.GetString("CDSelector_NoCDDetected")
                                  lblChargement.ForeColor = Color.Orange
                                  lblChargement.Visible = True
                              End Sub)
                Else
                    ' Nouveau CD inséré - recharger
                    Me.Invoke(Sub()
                                  pistesCD = pistesActuelles
                                  cdMetadata = Nothing
                                  ChargerPistes()
                                  lblChargement.Text = LanguageManager.GetString("CDSelector_NewCDDetected")
                                  lblChargement.ForeColor = Color.Blue
                                  lblChargement.Visible = True

                                  ' Tenter de charger automatiquement les métadonnées
                                  TenterChargerAutomatique()
                              End Sub)
                End If
            End If
        Catch ex As Exception
            ' Ignorer les erreurs de surveillance pour ne pas perturber l'utilisateur
            System.Diagnostics.Debug.WriteLine($"[FormSelecteurPistesCD] Erreur surveillance CD: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Calcule un ID simple basé sur le nombre de pistes et la durée totale
    ''' </summary>
    Private Function CalculerDiscIdSimple(pistes As List(Of CDAudioManager.CDTrack)) As String
        If pistes Is Nothing OrElse pistes.Count = 0 Then
            Return "EMPTY"
        End If

        ' ID simple: nombre de pistes + durée totale en secondes
        Dim dureeTotale = pistes.Sum(Function(p) p.Duration.TotalSeconds)
        Return $"{pistes.Count}_{dureeTotale:F0}"
    End Function

    ''' <summary>
    ''' Nettoyer le timer à la fermeture
    ''' </summary>
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If cdMonitorTimer IsNot Nothing Then
            cdMonitorTimer.Stop()
            RemoveHandler cdMonitorTimer.Tick, AddressOf SurveillerCD
            cdMonitorTimer.Dispose()
            cdMonitorTimer = Nothing
        End If
        MyBase.OnFormClosing(e)
    End Sub


End Class
