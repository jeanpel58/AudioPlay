Imports System.IO
Imports System.ComponentModel
Imports System.Linq

Partial Public Class FormParametres
    Inherits Form

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

    ' Helper type pour stocker la clé interne du thème et le label affiché
    Private Class ThemeDisplayItem
        Public Property Key As String
        Public Property Label As String
        Public Overrides Function ToString() As String
            Return Label
        End Function
    End Class

    Private cheminConfig As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioPlay",
        "parametres.txt")

    ' États initiaux des cases audio pour détection de changement
    Private EtatInitial_MP3 As Boolean
    Private EtatInitial_FLAC As Boolean
    Private EtatInitial_WMA As Boolean
    Private EtatInitial_WAV As Boolean
    Private EtatInitial_AAC As Boolean

    Private themeEnEdition As ThemeColors = Nothing
    Private themeInitial As ThemeColors = Nothing ' Thème au moment de l'ouverture
    Private themeNomInitial As String = "" ' Nom du thème au moment de l'ouverture

    ' États initiaux des effets audio pour annulation
    Private EtatInitial_ReverbActif As Boolean
    Private EtatInitial_ReverbMix As Single
    Private EtatInitial_EchoActif As Boolean
    Private EtatInitial_EchoMix As Single
    Private EtatInitial_EchoDelai As Integer
    Private EtatInitial_EchoFeedback As Single

    Private EtatInitial_TimeStretchActif As Boolean
    Private EtatInitial_TimeStretchRatio As Single

    Private EtatInitial_PitchShiftActif As Boolean
    Private EtatInitial_PitchShiftSemiTones As Single

    Private EtatInitial_PhaserActif As Boolean
    Private EtatInitial_PhaserRate As Single
    Private EtatInitial_PhaserDepth As Single
    Private EtatInitial_PhaserFeedback As Single
    Private EtatInitial_PhaserMix As Single
    Private EtatInitial_PhaserStages As Integer

    ' État initial du mode DJ pour détection de changement
    Private EtatInitial_ModeMixeurDJ As Boolean

    Private Class ThemeColorOption
        Public Property Key As String
        Public Property Label As String

        Public Overrides Function ToString() As String
            Return Label
        End Function
    End Class

    ' Propriétés pour stocker les paramètres
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property RepertoireParDefaut As String

    ' États des cases à cocher audio
    Private EtatCheckBoxMP3 As Boolean = False
    Private EtatCheckBoxWAV As Boolean = False
    Private EtatCheckBoxFLAC As Boolean = False
    Private EtatCheckBoxAAC As Boolean = False
    Private EtatCheckBoxWMA As Boolean = False


    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property VolumeLecture As Integer

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property AfficherBPM As Boolean

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property LectureEnContinu As Boolean

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property NormalisationVolume As Boolean

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property MethodeBPM As String ' "Auto", "Librosa", "SoundTouch"

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property MetronomeActif As Boolean

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property MetronomeSonActif As Boolean

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property MetronomeLumiereActive As Boolean

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property NombreBeatsMetronome As Integer

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property LangueChoisie As String

    ' Méthodes pour définir et obtenir LectureEnContinu
    Public Sub DefinirLectureEnContinu(valeur As Boolean)
        LectureEnContinu = valeur
    End Sub

    ' Retourne la clé interne du thème sélectionné dans la ComboBox (ou chaîne vide)
    Private Function GetSelectedThemeKey() As String
        If ComboBoxThemes Is Nothing OrElse ComboBoxThemes.SelectedItem Is Nothing Then Return ""
        Dim disp = TryCast(ComboBoxThemes.SelectedItem, ThemeDisplayItem)
        If disp IsNot Nothing Then Return disp.Key
        Return ComboBoxThemes.SelectedItem.ToString()
    End Function

    Private Async Sub ButtonCheckLibrosa_Click(sender As Object, e As EventArgs) Handles ButtonCheckLibrosa.Click
        Try
            ' Determine python exe to use
            Dim pythonExe As String = If(Not String.IsNullOrEmpty(ParametresGlobaux.PythonPath), ParametresGlobaux.PythonPath, PythonManager.CheminPython)

            ' Check librosa via PythonManager if embedded installed
            If PythonManager.EstInstalle() Then
                Dim ok = Await PythonManager.LibrosaEstInstalle()
                If ok Then
                    MessageBox.Show(LanguageManager.GetString("BPM_PythonLibrosa_OK"), LanguageManager.GetString("BPM_PythonCheckTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
            End If

            ' If not installed, offer to install via PythonManager
            Dim resp = MessageBox.Show(LanguageManager.GetString("BPM_PythonInstallPrompt"), LanguageManager.GetString("BPM_PythonInstallTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If resp = DialogResult.Yes Then
                Dim progressForm As New Form()
                progressForm.Text = LanguageManager.GetString("BPM_PythonInstallTitle")
                progressForm.Size = New Size(500, 150)
                progressForm.StartPosition = FormStartPosition.CenterParent
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog
                progressForm.ControlBox = False

                Dim lblProgress As New Label()
                lblProgress.AutoSize = False
                lblProgress.Size = New Size(460, 60)
                lblProgress.Location = New Point(20, 20)
                lblProgress.TextAlign = ContentAlignment.MiddleLeft
                progressForm.Controls.Add(lblProgress)
                progressForm.Show(Me)

                Dim progress = New Progress(Of String)(Sub(msg)
                                                           lblProgress.Text = msg
                                                           Application.DoEvents()
                                                       End Sub)

                Dim success = Await PythonManager.InstallerPythonEmbedded(progress)
                progressForm.Close()
                If success Then
                    MessageBox.Show(LanguageManager.GetString("BPM_PythonInstall_Success"), LanguageManager.GetString("BPM_PythonInstallTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    MessageBox.Show(LanguageManager.GetString("BPM_PythonInstall_Failed"), LanguageManager.GetString("BPM_PythonInstallTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("BPM_PythonCheck_Error"), LanguageManager.GetString("BPM_PythonCheckTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Bouton Appliquer maintenant (applique immédiatement les associations choisies)
    Private Sub ButtonApplyNow_Click(sender As Object, e As EventArgs) Handles ButtonApplyNow.Click
        Try
            ' Lire l'état des cases
            Dim mp3 = (CheckBox_MP3 IsNot Nothing AndAlso CheckBox_MP3.Checked)
            Dim wav = (CheckBox_WAV IsNot Nothing AndAlso CheckBox_WAV.Checked)
            Dim flac = (CheckBox_FLAC IsNot Nothing AndAlso CheckBox_FLAC.Checked)
            Dim aac = (CheckBox_AAC IsNot Nothing AndAlso CheckBox_AAC.Checked)
            Dim wma = (CheckBox_WMA IsNot Nothing AndAlso CheckBox_WMA.Checked)

            ' Appliquer les associations immédiatement
            If mp3 Then SetAudioPlayDefault(".mp3", "AudioPlay.mp3") Else RemoveAudioPlayDefault(".mp3")
            If wav Then SetAudioPlayDefault(".wav", "AudioPlay.wav") Else RemoveAudioPlayDefault(".wav")
            If flac Then SetAudioPlayDefault(".flac", "AudioPlay.flac") Else RemoveAudioPlayDefault(".flac")
            If aac Then SetAudioPlayDefault(".aac", "AudioPlay.aac") Else RemoveAudioPlayDefault(".aac")
            If wma Then SetAudioPlayDefault(".wma", "AudioPlay.wma") Else RemoveAudioPlayDefault(".wma")

            ' Sauvegarder la préférence pour la prochaine exécution
            AudioDefautManager.SauvegarderAudioDefaut(mp3, flac, wma, wav, aac)
            ' Essayer d'ouvrir l'UI d'association avancée pour chaque ProgID appliqué (COM IApplicationAssociationRegistrationUI)
            Dim launchedAny As Boolean = False
            If mp3 Then launchedAny = TryLaunchAdvancedAssociationUI("AudioPlay.mp3") Or launchedAny
            If flac Then launchedAny = TryLaunchAdvancedAssociationUI("AudioPlay.flac") Or launchedAny
            If wma Then launchedAny = TryLaunchAdvancedAssociationUI("AudioPlay.wma") Or launchedAny
            If wav Then launchedAny = TryLaunchAdvancedAssociationUI("AudioPlay.wav") Or launchedAny
            If aac Then launchedAny = TryLaunchAdvancedAssociationUI("AudioPlay.aac") Or launchedAny

            If Not launchedAny Then
                ' Fallback : ouvrir la page Paramètres des applications par défaut
                Try
                    Process.Start(New ProcessStartInfo("ms-settings:defaultapps") With {.UseShellExecute = True})
                Catch
                End Try
            End If

            MessageBox.Show(LanguageManager.GetString("AudioTypes_Applied"), LanguageManager.GetString("Success_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Error_FileAssociation", ex.Message), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Tente d'appeler l'API COM IApplicationAssociationRegistrationUI::LaunchAdvancedAssociationUI
    ''' en utilisant un CreateInstance via ProgID/Reflection. Retourne True si l'appel a été lancé.
    ''' </summary>
    Private Function TryLaunchAdvancedAssociationUI(appRegistryName As String) As Boolean
        Try
            ' Tenter d'obtenir le type via ProgID (si disponible sur le système)
            Dim comType As Type = Nothing
            Try
                comType = Type.GetTypeFromProgID("ApplicationAssociationRegistrationUI")
            Catch
            End Try

            If comType Is Nothing Then
                ' Essayer via CLSID connu (si présent sur le système) - utiliser reflection prudente
                Try
                    ' CLSID may not be registered on all systems; use documented ProgID first
                    comType = Type.GetTypeFromCLSID(New Guid("1968106D-F3B5-44CF-890E-1164BA91A3F0"))
                Catch
                    comType = Nothing
                End Try
            End If

            If comType Is Nothing Then Return False

            Dim comObj = Activator.CreateInstance(comType)
            If comObj Is Nothing Then Return False

            ' Rechercher la méthode LaunchAdvancedAssociationUI
            Dim mi = comType.GetMethod("LaunchAdvancedAssociationUI")
            If mi Is Nothing Then
                ' Parfois la méthode est définie sur l'interface; tenter d'invoquer via late-binding
                mi = comObj.GetType().GetMethod("LaunchAdvancedAssociationUI")
            End If
            If mi Is Nothing Then Return False

            mi.Invoke(comObj, New Object() {appRegistryName})
            Return True
        Catch
            Return False
        End Try
    End Function

    Public Function ObtenirLectureEnContinu() As Boolean
        Return LectureEnContinu
    End Function

    Private Sub FormParametres_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialiser le ComboBox méthode BPM avec une valeur par défaut
        If ComboBoxMethodeBPM IsNot Nothing Then
            ComboBoxMethodeBPM.SelectedIndex = 0 ' Par défaut : Auto
        End If

        ' Initialiser TextBoxPythonPath si défini
        Try
            If TextBoxPythonPath IsNot Nothing Then
                TextBoxPythonPath.Text = ParametresGlobaux.PythonPath
            End If
        Catch
        End Try

        ' Initialiser le ComboBox langue
        If ComboBoxLangue IsNot Nothing Then
            ComboBoxLangue.Items.Clear()
            ComboBoxLangue.Items.Add("Français")
            ComboBoxLangue.Items.Add("English")
            ComboBoxLangue.Items.Add("Español")
            ComboBoxLangue.Items.Add("Deutsch")
            ComboBoxLangue.Items.Add("Italiano")
            ComboBoxLangue.SelectedIndex = 0 ' Par défaut : Français
        End If

        ' Charger les paramètres généraux
        ChargerParametres()
        ' Charger les cases à cocher audio depuis AudioDefaut.txt
        AudioDefautManager.ChargerAudioDefaut(EtatCheckBoxMP3, EtatCheckBoxFLAC, EtatCheckBoxWMA, EtatCheckBoxWAV, EtatCheckBoxAAC)
        ' Synchroniser avec le registre Windows
        SynchroniserCasesAudioAvecRegistre()
        AfficherParametres()

        ' Mémoriser l'état initial des cases audio
        EtatInitial_MP3 = (CheckBox_MP3 IsNot Nothing AndAlso CheckBox_MP3.Checked)
        EtatInitial_FLAC = (CheckBox_FLAC IsNot Nothing AndAlso CheckBox_FLAC.Checked)
        EtatInitial_WMA = (CheckBox_WMA IsNot Nothing AndAlso CheckBox_WMA.Checked)
        EtatInitial_WAV = (CheckBox_WAV IsNot Nothing AndAlso CheckBox_WAV.Checked)
        EtatInitial_AAC = (CheckBox_AAC IsNot Nothing AndAlso CheckBox_AAC.Checked)

        ' Sauvegarder le thème initial pour pouvoir le restaurer en cas d'annulation
        themeNomInitial = ThemeManager.GetCurrentThemeName()
        themeInitial = ClonerTheme(ThemeManager.GetCurrentTheme())
        themeEnEdition = ThemeManager.GetCurrentTheme()
        ThemeManager.ApplyThemeToForm(Me)

        ' Charger la liste des thèmes disponibles
        ChargerListeThemes()

        ' Gérer la protection des thèmes préinstallés au chargement
        If ComboBoxThemes IsNot Nothing AndAlso ComboBoxThemes.SelectedItem IsNot Nothing Then
            Dim _disp = TryCast(ComboBoxThemes.SelectedItem, ThemeDisplayItem)
            Dim _key As String = If(_disp IsNot Nothing, _disp.Key, ComboBoxThemes.SelectedItem.ToString())
            GererProtectionThemesPreinstalles(_key)
        End If

        ' Synchroniser la case "Effacer chansons" avec la variable globale
        If CheckBox_EffacerChansons IsNot Nothing Then
            ParametresGlobaux.ConfirmerEffacementChansons = CheckBox_EffacerChansons.Checked
        End If

        ' === Mémoriser l'état initial des effets audio ===
        EtatInitial_ReverbActif = ParametresGlobaux.EffetReverbActif
        EtatInitial_ReverbMix = ParametresGlobaux.EffetReverbMix
        EtatInitial_EchoActif = ParametresGlobaux.EffetEchoActif
        EtatInitial_EchoMix = ParametresGlobaux.EffetEchoMix
        EtatInitial_EchoDelai = ParametresGlobaux.EffetEchoDelai
        EtatInitial_EchoFeedback = ParametresGlobaux.EffetEchoFeedback

        EtatInitial_TimeStretchActif = ParametresGlobaux.EffetTimeStretchActif
        EtatInitial_TimeStretchRatio = ParametresGlobaux.EffetTimeStretchRatio

        EtatInitial_PitchShiftActif = ParametresGlobaux.EffetPitchShiftActif
        EtatInitial_PitchShiftSemiTones = ParametresGlobaux.EffetPitchShiftSemiTones

        EtatInitial_PhaserActif = ParametresGlobaux.EffetPhaserActif
        EtatInitial_PhaserRate = ParametresGlobaux.EffetPhaserRate
        EtatInitial_PhaserDepth = ParametresGlobaux.EffetPhaserDepth
        EtatInitial_PhaserFeedback = ParametresGlobaux.EffetPhaserFeedback
        EtatInitial_PhaserMix = ParametresGlobaux.EffetPhaserMix
        EtatInitial_PhaserStages = ParametresGlobaux.EffetPhaserStages

        ' Mémoriser l'état initial du mode DJ
        EtatInitial_ModeMixeurDJ = ParametresGlobaux.ModeMixeurDJ
        System.Diagnostics.Debug.WriteLine($"[FormParametres_Load] EtatInitial_ModeMixeurDJ = {EtatInitial_ModeMixeurDJ}")

        ' Charger les valeurs dans l'UI
        ChargerEffetsAudioDansUI()

        RefreshLanguage()
    End Sub

    Private Sub ButtonBrowsePython_Click(sender As Object, e As EventArgs) Handles ButtonBrowsePython.Click
        Try
            Using ofd As New OpenFileDialog()
                ofd.Filter = "python.exe|python.exe|All files|*.*"
                ofd.Title = "Sélectionner l'exécutable Python"
                If Not String.IsNullOrEmpty(TextBoxPythonPath.Text) AndAlso File.Exists(TextBoxPythonPath.Text) Then
                    ofd.FileName = TextBoxPythonPath.Text
                End If
                If ofd.ShowDialog() = DialogResult.OK Then
                    TextBoxPythonPath.Text = ofd.FileName
                End If
            End Using
        Catch
        End Try
    End Sub

    ' Charger la liste des thèmes disponibles
    Private Sub ChargerListeThemes()
        If ComboBoxThemes Is Nothing Then Return
        ComboBoxThemes.Items.Clear()
        Dim themes = ThemeManager.GetAvailableThemes()

        ' Construire des éléments affichés localisés mais conserver la clé interne
        For Each themeName In themes
            Dim displayLabel As String = themeName
            Select Case themeName
                Case "Par défaut"
                    displayLabel = LanguageManager.GetString("Theme_Name_Default")
                Case "Automne"
                    displayLabel = LanguageManager.GetString("Theme_Name_Autumn")
                Case "Océan"
                    displayLabel = LanguageManager.GetString("Theme_Name_Ocean")
                Case "Soleil"
                    displayLabel = LanguageManager.GetString("Theme_Name_Sun")
                Case "Sombre"
                    displayLabel = LanguageManager.GetString("Theme_Name_Dark")
                Case Else
                    ' thèmes utilisateur : utiliser le nom tel quel
                    displayLabel = themeName
            End Select
            ComboBoxThemes.Items.Add(New ThemeDisplayItem With {.Key = themeName, .Label = displayLabel})
        Next

        ' Sélectionner le thème courant (par clé interne)
        Dim currentThemeName = ThemeManager.GetCurrentThemeName()
        Dim selIndex As Integer = -1
        For i As Integer = 0 To ComboBoxThemes.Items.Count - 1
            Dim itm = TryCast(ComboBoxThemes.Items(i), ThemeDisplayItem)
            If itm IsNot Nothing AndAlso String.Equals(itm.Key, currentThemeName, StringComparison.OrdinalIgnoreCase) Then
                selIndex = i
                Exit For
            End If
        Next
        If selIndex >= 0 Then
            ComboBoxThemes.SelectedIndex = selIndex
        ElseIf ComboBoxThemes.Items.Count > 0 Then
            ComboBoxThemes.SelectedIndex = 0
        End If
    End Sub

    ' Méthode publique pour charger les paramètres au démarrage de l'application (sans UI)
    Public Sub ChargerParametresAvantDemarrage()
        ChargerParametres()
    End Sub

    ' Charger les paramètres depuis le fichier
    Private Sub ChargerParametres()
        Try
            ' Créer le répertoire s'il n'existe pas
            Dim dossier As String = Path.GetDirectoryName(cheminConfig)
            If Not Directory.Exists(dossier) Then
                Directory.CreateDirectory(dossier)
            End If

            ' Valeurs par défaut
            RepertoireParDefaut = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            VolumeLecture = 50
            AfficherBPM = True
            LectureEnContinu = True
            NormalisationVolume = True ' Activé par défaut
            MethodeBPM = "Auto" ' Par défaut : Auto (librosa si disponible, sinon SoundTouch)
            MetronomeActif = False ' Désactivé par défaut
            NombreBeatsMetronome = 4 ' 4 beats par défaut
            SupprimerSilenceDebut = False ' Désactivé par défaut
            SupprimerSilenceFin = False ' Désactivé par défaut
            ' NE PAS réinitialiser ici les variables EtatCheckBoxMP3, EtatCheckBoxWAV, EtatCheckBoxFLAC, EtatCheckBoxAAC, EtatCheckBoxWMA

            ' Détecter la langue système ou utiliser français par défaut
            LangueChoisie = LanguageManager.GetCurrentLanguageCode()

            ' Charger depuis le fichier si existe
            If File.Exists(cheminConfig) Then
                Dim lignes() As String = File.ReadAllLines(cheminConfig)
                For Each ligne In lignes
                    If ligne.Contains("=") Then
                        Dim parts() As String = ligne.Split("="c, 2)
                        Dim cle As String = parts(0).Trim()
                        Dim valeur As String = parts(1).Trim()

                        Select Case cle
                            Case "RepertoireParDefaut"
                                RepertoireParDefaut = valeur
                                ParametresGlobaux.repertoireParDefaut = valeur
                            Case "DernierRepertoireAjoutFichier"
                                Try
                                    ParametresGlobaux.dernierRepertoireAjoutFichier = valeur
                                Catch ex As Exception
                                End Try
                            Case "DernierRepertoireAjoutFichier_DJ"
                                Try
                                    ParametresGlobaux.dernierRepertoireAjoutFichier_DJ = valeur
                                Catch ex As Exception
                                End Try
                            Case "DernierRepertoireAjoutRepertoire"
                                Try
                                    ParametresGlobaux.dernierRepertoireAjoutRepertoire = valeur
                                Catch ex As Exception
                                End Try
                            Case "DernierRepertoireAjoutRepertoire_DJ"
                                Try
                                    ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ = valeur
                                Catch ex As Exception
                                End Try
                            Case "AvantDernierRepertoireAjoutRepertoire"
                                Try
                                    ParametresGlobaux.avantDernierRepertoireAjoutRepertoire = valeur
                                Catch ex As Exception
                                End Try
                            Case "AvantDernierRepertoireAjoutRepertoire_DJ"
                                Try
                                    ParametresGlobaux.avantDernierRepertoireAjoutRepertoire_DJ = valeur
                                Catch ex As Exception
                                End Try
                            Case "DernierRepertoirePlaylist"
                                Try
                                    ParametresGlobaux.dernierRepertoirePlaylist = valeur
                                Catch ex As Exception
                                End Try
                            Case "DernierRepertoirePlaylist_DJ"
                                Try
                                    ParametresGlobaux.dernierRepertoirePlaylist_DJ = valeur
                                Catch ex As Exception
                                End Try
                            Case "VolumeLecture", "Volume"
                                Dim v As Integer = 20
                                If Integer.TryParse(valeur, v) Then
                                    If v < 0 Then v = 0
                                    If v > 40 Then v = 40
                                    VolumeLecture = v
                                Else
                                    VolumeLecture = 20
                                End If
                            Case "AfficherBPM"
                                Boolean.TryParse(valeur, AfficherBPM)
                            Case "LectureEnContinu"
                                Boolean.TryParse(valeur, LectureEnContinu)
                            Case "NormalisationVolume"
                                Boolean.TryParse(valeur, NormalisationVolume)
                            Case "MethodeBPM"
                                MethodeBPM = valeur
                            Case "MetronomeActif"
                                Boolean.TryParse(valeur, MetronomeActif)
                            Case "MetronomeSonActif"
                                Boolean.TryParse(valeur, MetronomeSonActif)
                            Case "MetronomeLumiereActive"
                                Boolean.TryParse(valeur, MetronomeLumiereActive)
                            Case "NombreBeatsMetronome"
                                Integer.TryParse(valeur, NombreBeatsMetronome)
                            Case "SupprimerSilenceDebut"
                                Boolean.TryParse(valeur, ParametresGlobaux.SupprimerSilenceDebut)
                            Case "SupprimerSilenceFin"
                                Boolean.TryParse(valeur, ParametresGlobaux.SupprimerSilenceFin)
                            Case "EffacerChansons"
                                If CheckBox_EffacerChansons IsNot Nothing Then
                                    Dim b As Boolean = True
                                    Boolean.TryParse(valeur, b)
                                    CheckBox_EffacerChansons.Checked = b
                                    ParametresGlobaux.ConfirmerEffacementChansons = b
                                End If
                            Case "Langue"
                                LangueChoisie = valeur
                            Case "CheckBox_MP3"
                                Boolean.TryParse(valeur, EtatCheckBoxMP3)
                            Case "CheckBox_WAV"
                                Boolean.TryParse(valeur, EtatCheckBoxWAV)
                            Case "CheckBox_FLAC"
                                Boolean.TryParse(valeur, EtatCheckBoxFLAC)
                            Case "CheckBox_AAC"
                                Boolean.TryParse(valeur, EtatCheckBoxAAC)
                            Case "CheckBox_WMA"
                                Boolean.TryParse(valeur, EtatCheckBoxWMA)
                            Case "EffetReverbActif"
                                Boolean.TryParse(valeur, ParametresGlobaux.EffetReverbActif)
                            Case "EffetReverbMix"
                                Dim mix As Single
                                If Single.TryParse(valeur, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, mix) Then
                                    ParametresGlobaux.EffetReverbMix = mix
                                End If
                            Case "EffetEchoActif"
                                Boolean.TryParse(valeur, ParametresGlobaux.EffetEchoActif)
                            Case "EffetEchoMix"
                                Dim mix As Single
                                If Single.TryParse(valeur, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, mix) Then
                                    ParametresGlobaux.EffetEchoMix = mix
                                End If
                            Case "EffetEchoDelai"
                                Dim delai As Integer
                                If Integer.TryParse(valeur, delai) Then
                                    ' Valider la plage (50-2000 ms)
                                    If delai < 50 Then delai = 50
                                    If delai > 2000 Then delai = 2000
                                    ParametresGlobaux.EffetEchoDelai = delai
                                End If
                            Case "EffetEchoFeedback"
                                Dim fb As Single
                                If Single.TryParse(valeur, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, fb) Then
                                    ParametresGlobaux.EffetEchoFeedback = fb
                                End If
                            Case "EffetTimeStretchActif"
                                Boolean.TryParse(valeur, ParametresGlobaux.EffetTimeStretchActif)
                            Case "EffetTimeStretchRatio"
                                Dim ratio As Single
                                If Single.TryParse(valeur, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, ratio) Then
                                    ParametresGlobaux.EffetTimeStretchRatio = ratio
                                End If
                            Case "EffetPitchShiftActif"
                                Boolean.TryParse(valeur, ParametresGlobaux.EffetPitchShiftActif)
                            Case "EffetPitchShiftSemiTones"
                                Dim semiTones As Single
                                If Single.TryParse(valeur, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, semiTones) Then
                                    ParametresGlobaux.EffetPitchShiftSemiTones = semiTones
                                End If
                            Case "EffetPhaserActif"
                                Boolean.TryParse(valeur, ParametresGlobaux.EffetPhaserActif)
                            Case "EffetPhaserRate"
                                Dim rate As Single
                                If Single.TryParse(valeur, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, rate) Then
                                    ParametresGlobaux.EffetPhaserRate = rate
                                End If
                            Case "EffetPhaserDepth"
                                Dim depth As Single
                                If Single.TryParse(valeur, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, depth) Then
                                    ParametresGlobaux.EffetPhaserDepth = depth
                                End If
                            Case "EffetPhaserFeedback"
                                Dim feedback As Single
                                If Single.TryParse(valeur, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, feedback) Then
                                    ParametresGlobaux.EffetPhaserFeedback = feedback
                                End If
                            Case "EffetPhaserMix"
                                Dim mix As Single
                                If Single.TryParse(valeur, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, mix) Then
                                    ParametresGlobaux.EffetPhaserMix = mix
                                End If
                            Case "EffetPhaserStages"
                                Integer.TryParse(valeur, ParametresGlobaux.EffetPhaserStages)
                            Case "ModeMixeurDJ"
                                Boolean.TryParse(valeur, ParametresGlobaux.ModeMixeurDJ)
                                System.Diagnostics.Debug.WriteLine($"[ChargerParametres] ModeMixeurDJ lu depuis fichier = {ParametresGlobaux.ModeMixeurDJ} (valeur brute: '{valeur}')")
                            Case "RepertoireExtractionCD"
                                ParametresGlobaux.repertoireExtractionCD = valeur
                                System.Diagnostics.Debug.WriteLine($"[ChargerParametres] RepertoireExtractionCD lu depuis fichier = '{valeur}'")
                        End Select
                    End If
                Next
            End If
        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Error_LoadingSettings", ex.Message),
                          LanguageManager.GetString("Error_Title"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Warning)
            ' Forcer l'application directe aux cases à cocher audio
            If CheckBox_MP3 IsNot Nothing Then CheckBox_MP3.Checked = EtatCheckBoxMP3
            If CheckBox_WAV IsNot Nothing Then CheckBox_WAV.Checked = EtatCheckBoxWAV
            If CheckBox_FLAC IsNot Nothing Then CheckBox_FLAC.Checked = EtatCheckBoxFLAC
            If CheckBox_AAC IsNot Nothing Then CheckBox_AAC.Checked = EtatCheckBoxAAC
            If CheckBox_WMA IsNot Nothing Then CheckBox_WMA.Checked = EtatCheckBoxWMA

            System.Diagnostics.Debug.WriteLine($"Lecture config: MP3={EtatCheckBoxMP3}, WAV={EtatCheckBoxWAV}, FLAC={EtatCheckBoxFLAC}, AAC={EtatCheckBoxAAC}, WMA={EtatCheckBoxWMA}")
        End Try
    End Sub

    ' Afficher les paramètres dans les contrôles
    Private Sub AfficherParametres()
        ' TextBoxRepertoire.Text = RepertoireParDefaut
        CheckBoxLectureAuto.Checked = LectureEnContinu
        CheckBoxAfficherBPM.Checked = AfficherBPM
        CheckBoxNormalisationVolume.Checked = NormalisationVolume
        CheckBoxMetronome.Checked = MetronomeActif
        CheckBoxMetronomeSon.Checked = MetronomeSonActif
        CheckBoxMetronomeLumiere.Checked = MetronomeLumiereActive
        TextBoxNombreBeats.Text = NombreBeatsMetronome.ToString()
        CheckBoxModeMixeurDJ.Checked = ParametresGlobaux.ModeMixeurDJ

        ' Vérifier si les contrôles existent avant de les utiliser
        If CheckBoxSupprimerSilenceDebut IsNot Nothing Then
            CheckBoxSupprimerSilenceDebut.Checked = ParametresGlobaux.SupprimerSilenceDebut
        End If

        If CheckBoxSupprimerSilenceFin IsNot Nothing Then
            CheckBoxSupprimerSilenceFin.Checked = ParametresGlobaux.SupprimerSilenceFin
        End If

        ' Appliquer l'état des cases à cocher audio
        If CheckBox_MP3 IsNot Nothing Then CheckBox_MP3.Checked = EtatCheckBoxMP3 : System.Diagnostics.Debug.WriteLine($"UI: CheckBox_MP3.Checked={CheckBox_MP3.Checked}")
        If CheckBox_WAV IsNot Nothing Then CheckBox_WAV.Checked = EtatCheckBoxWAV : System.Diagnostics.Debug.WriteLine($"UI: CheckBox_WAV.Checked={CheckBox_WAV.Checked}")
        If CheckBox_FLAC IsNot Nothing Then CheckBox_FLAC.Checked = EtatCheckBoxFLAC : System.Diagnostics.Debug.WriteLine($"UI: CheckBox_FLAC.Checked={CheckBox_FLAC.Checked}")
        If CheckBox_AAC IsNot Nothing Then CheckBox_AAC.Checked = EtatCheckBoxAAC : System.Diagnostics.Debug.WriteLine($"UI: CheckBox_AAC.Checked={CheckBox_AAC.Checked}")
        If CheckBox_WMA IsNot Nothing Then CheckBox_WMA.Checked = EtatCheckBoxWMA : System.Diagnostics.Debug.WriteLine($"UI: CheckBox_WMA.Checked={CheckBox_WMA.Checked}")

        ' Sélectionner la méthode BPM dans le ComboBox
        If ComboBoxMethodeBPM IsNot Nothing Then
            Select Case MethodeBPM
                Case "Librosa"
                    ComboBoxMethodeBPM.SelectedIndex = 1
                Case "SoundTouch"
                    ComboBoxMethodeBPM.SelectedIndex = 2
                Case Else ' "Auto"
                    ComboBoxMethodeBPM.SelectedIndex = 0
            End Select
        End If

        ' Sélectionner la langue dans le ComboBox
        If ComboBoxLangue IsNot Nothing Then
            Select Case LangueChoisie
                Case "en"
                    ComboBoxLangue.SelectedIndex = 1 ' English
                Case "es"
                    ComboBoxLangue.SelectedIndex = 2 ' Español
                Case "de"
                    ComboBoxLangue.SelectedIndex = 3 ' Deutsch
                Case "it"
                    ComboBoxLangue.SelectedIndex = 4 ' Italiano
                Case Else ' "fr"
                    ComboBoxLangue.SelectedIndex = 0 ' Français
            End Select
        End If

        ' === Effets Audio ===
        If CheckBoxReverbActif IsNot Nothing Then CheckBoxReverbActif.Checked = ParametresGlobaux.EffetReverbActif
        If TrackBarReverbMix IsNot Nothing Then
            TrackBarReverbMix.Value = CInt(ParametresGlobaux.EffetReverbMix * 100)
            If LabelReverbMixValeur IsNot Nothing Then LabelReverbMixValeur.Text = $"{TrackBarReverbMix.Value}%"
        End If

        If CheckBoxEchoActif IsNot Nothing Then CheckBoxEchoActif.Checked = ParametresGlobaux.EffetEchoActif
        If TrackBarEchoMix IsNot Nothing Then
            TrackBarEchoMix.Value = CInt(ParametresGlobaux.EffetEchoMix * 100)
            If LabelEchoMixValeur IsNot Nothing Then LabelEchoMixValeur.Text = $"{TrackBarEchoMix.Value}%"
        End If
        If TrackBarEchoDelai IsNot Nothing Then
            ' Convertir la valeur en millisecondes (50-2000) vers le TrackBar (5-200)
            Dim valeurTrackBar As Integer = ParametresGlobaux.EffetEchoDelai \ 10
            ' Valider la plage (Minimum=5, Maximum=200)
            If valeurTrackBar < TrackBarEchoDelai.Minimum Then valeurTrackBar = TrackBarEchoDelai.Minimum
            If valeurTrackBar > TrackBarEchoDelai.Maximum Then valeurTrackBar = TrackBarEchoDelai.Maximum
            TrackBarEchoDelai.Value = valeurTrackBar
            If LabelEchoDelaiValeur IsNot Nothing Then LabelEchoDelaiValeur.Text = $"{ParametresGlobaux.EffetEchoDelai} ms"
        End If
        If TrackBarEchoFeedback IsNot Nothing Then
            TrackBarEchoFeedback.Value = CInt(ParametresGlobaux.EffetEchoFeedback * 100)
            If LabelEchoFeedbackValeur IsNot Nothing Then LabelEchoFeedbackValeur.Text = $"{TrackBarEchoFeedback.Value}%"
        End If

        If CheckBoxTimeStretchActif IsNot Nothing Then CheckBoxTimeStretchActif.Checked = ParametresGlobaux.EffetTimeStretchActif
        If TrackBarTimeStretch IsNot Nothing Then
            TrackBarTimeStretch.Value = CInt(ParametresGlobaux.EffetTimeStretchRatio * 100)
            If LabelTimeStretchValeur IsNot Nothing Then LabelTimeStretchValeur.Text = $"{(TrackBarTimeStretch.Value / 100.0):F2}x"
        End If
    End Sub

    ' Bouton parcourir pour choisir le répertoire par défaut
    Private Sub ButtonParcourir_Click(sender As Object, e As EventArgs) Handles ButtonParcourir.Click
        Using fbd As New FolderBrowserDialog()
            fbd.Description = "Sélectionner le répertoire par défaut"
            fbd.SelectedPath = RepertoireParDefaut

            If fbd.ShowDialog() = DialogResult.OK Then
                TextBoxRepertoire.Text = fbd.SelectedPath
                RepertoireParDefaut = fbd.SelectedPath
            End If
        End Using
    End Sub

    ' Bouton Sauvegarder
    Private Sub ButtonSauvegarder_Click(sender As Object, e As EventArgs) Handles ButtonSauvegarder.Click
        Try
            ' Récupérer les valeurs des contrôles
            ' RepertoireParDefaut supprimé (plus de TextBoxRepertoire)
            LectureEnContinu = CheckBoxLectureAuto.Checked
            AfficherBPM = CheckBoxAfficherBPM.Checked
            NormalisationVolume = CheckBoxNormalisationVolume.Checked
            MetronomeActif = CheckBoxMetronome.Checked
            MetronomeSonActif = CheckBoxMetronomeSon.Checked
            MetronomeLumiereActive = CheckBoxMetronomeLumiere.Checked

            ' Vérifier si les contrôles existent
            If CheckBoxSupprimerSilenceDebut IsNot Nothing Then
                ParametresGlobaux.SupprimerSilenceDebut = CheckBoxSupprimerSilenceDebut.Checked
            End If

            If CheckBoxSupprimerSilenceFin IsNot Nothing Then
                ParametresGlobaux.SupprimerSilenceFin = CheckBoxSupprimerSilenceFin.Checked
            End If

            ' Valider et récupérer le nombre de beats
            Dim beats As Integer = 4
            If Integer.TryParse(TextBoxNombreBeats.Text, beats) AndAlso beats >= 1 AndAlso beats <= 16 Then
                NombreBeatsMetronome = beats
            Else
                MessageBox.Show(LanguageManager.GetString("Error_InvalidBeatsRange"),
                              LanguageManager.GetString("Error_Title"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
                Return
            End If

            ' Récupérer la méthode BPM sélectionnée
            If ComboBoxMethodeBPM IsNot Nothing Then
                Select Case ComboBoxMethodeBPM.SelectedIndex
                    Case 1
                        MethodeBPM = "Librosa"
                    Case 2
                        MethodeBPM = "SoundTouch"
                    Case Else
                        MethodeBPM = "Auto"
                End Select
            End If

            ' Récupérer la langue sélectionnée
            If ComboBoxLangue IsNot Nothing Then
                Select Case ComboBoxLangue.SelectedIndex
                    Case 1
                        LangueChoisie = "en" ' English
                    Case 2
                        LangueChoisie = "es" ' Español
                    Case 3
                        LangueChoisie = "de" ' Deutsch
                    Case 4
                        LangueChoisie = "it" ' Italiano
                    Case Else
                        LangueChoisie = "fr" ' Français
                End Select
                ' Appliquer le changement de langue
                LanguageManager.ChangeLanguage(LangueChoisie)
            End If

            ' Mettre à jour les états pour la persistance
            EtatCheckBoxMP3 = (CheckBox_MP3 IsNot Nothing AndAlso CheckBox_MP3.Checked)
            EtatCheckBoxWAV = (CheckBox_WAV IsNot Nothing AndAlso CheckBox_WAV.Checked)
            EtatCheckBoxFLAC = (CheckBox_FLAC IsNot Nothing AndAlso CheckBox_FLAC.Checked)
            EtatCheckBoxAAC = (CheckBox_AAC IsNot Nothing AndAlso CheckBox_AAC.Checked)
            EtatCheckBoxWMA = (CheckBox_WMA IsNot Nothing AndAlso CheckBox_WMA.Checked)
            System.Diagnostics.Debug.WriteLine($"Sauvegarde config: MP3={EtatCheckBoxMP3}, WAV={EtatCheckBoxWAV}, FLAC={EtatCheckBoxFLAC}, AAC={EtatCheckBoxAAC}, WMA={EtatCheckBoxWMA}")

            ' Synchroniser la variable globale avec la case à cocher
            If CheckBox_EffacerChansons IsNot Nothing Then
                ParametresGlobaux.ConfirmerEffacementChansons = CheckBox_EffacerChansons.Checked
            End If

            ' Récupérer le mode Mixeur DJ (l'ancien état a été mémorisé dans FormLoad)
            If CheckBoxModeMixeurDJ IsNot Nothing Then
                ParametresGlobaux.ModeMixeurDJ = CheckBoxModeMixeurDJ.Checked
                System.Diagnostics.Debug.WriteLine($"[ButtonSauvegarder AVANT écriture] ModeMixeurDJ = {ParametresGlobaux.ModeMixeurDJ}")
            End If

            ' Récupérer les valeurs des effets audio
            If CheckBoxReverbActif IsNot Nothing Then ParametresGlobaux.EffetReverbActif = CheckBoxReverbActif.Checked
            If TrackBarReverbMix IsNot Nothing Then ParametresGlobaux.EffetReverbMix = TrackBarReverbMix.Value / 100.0F
            If CheckBoxEchoActif IsNot Nothing Then ParametresGlobaux.EffetEchoActif = CheckBoxEchoActif.Checked
            If TrackBarEchoMix IsNot Nothing Then ParametresGlobaux.EffetEchoMix = TrackBarEchoMix.Value / 100.0F
            If TrackBarEchoDelai IsNot Nothing Then ParametresGlobaux.EffetEchoDelai = TrackBarEchoDelai.Value
            If TrackBarEchoFeedback IsNot Nothing Then ParametresGlobaux.EffetEchoFeedback = TrackBarEchoFeedback.Value / 100.0F
            If CheckBoxTimeStretchActif IsNot Nothing Then ParametresGlobaux.EffetTimeStretchActif = CheckBoxTimeStretchActif.Checked
            If TrackBarTimeStretch IsNot Nothing Then ParametresGlobaux.EffetTimeStretchRatio = TrackBarTimeStretch.Value / 100.0F
            If CheckBoxPitchShiftActif IsNot Nothing Then ParametresGlobaux.EffetPitchShiftActif = CheckBoxPitchShiftActif.Checked
            If TrackBarPitchShift IsNot Nothing Then ParametresGlobaux.EffetPitchShiftSemiTones = TrackBarPitchShift.Value / 10.0F
            If CheckBoxPhaserActif IsNot Nothing Then ParametresGlobaux.EffetPhaserActif = CheckBoxPhaserActif.Checked
            If TrackBarPhaserRate IsNot Nothing Then ParametresGlobaux.EffetPhaserRate = TrackBarPhaserRate.Value / 10.0F
            If TrackBarPhaserDepth IsNot Nothing Then ParametresGlobaux.EffetPhaserDepth = TrackBarPhaserDepth.Value / 100.0F
            If TrackBarPhaserFeedback IsNot Nothing Then ParametresGlobaux.EffetPhaserFeedback = TrackBarPhaserFeedback.Value / 100.0F
            If TrackBarPhaserMix IsNot Nothing Then ParametresGlobaux.EffetPhaserMix = TrackBarPhaserMix.Value / 100.0F
            If ComboBoxPhaserStages IsNot Nothing AndAlso ComboBoxPhaserStages.SelectedItem IsNot Nothing Then
                ParametresGlobaux.EffetPhaserStages = Integer.Parse(ComboBoxPhaserStages.SelectedItem.ToString())
            End If

            ' Volume, Basses, Aigues sont maintenant gérés dans Son_Ajustement.txt (fichier séparé)
            ' FormParametres ne touche plus à ces valeurs

            ' Sauvegarder dans le fichier parametres.txt (paramètres applicatifs uniquement)
            Dim lignes As New List(Of String) From {
                "RepertoireParDefaut=" & RepertoireParDefaut,
                "LectureEnContinu=" & LectureEnContinu.ToString(),
                "NormalisationVolume=" & NormalisationVolume.ToString(),
                "MethodeBPM=" & MethodeBPM,
                "MetronomeActif=" & MetronomeActif.ToString(),
                "MetronomeSonActif=" & MetronomeSonActif.ToString(),
                "MetronomeLumiereActive=" & MetronomeLumiereActive.ToString(),
                "NombreBeatsMetronome=" & NombreBeatsMetronome.ToString(),
                "SupprimerSilenceDebut=" & ParametresGlobaux.SupprimerSilenceDebut.ToString(),
                "SupprimerSilenceFin=" & ParametresGlobaux.SupprimerSilenceFin.ToString(),
                "ModeAleatoire=False",
                "EffacerChansons=" & (If(CheckBox_EffacerChansons IsNot Nothing, CheckBox_EffacerChansons.Checked.ToString(), "True")),
                "Langue=" & LangueChoisie,
                "EffetReverbActif=" & ParametresGlobaux.EffetReverbActif.ToString(),
                "EffetReverbMix=" & ParametresGlobaux.EffetReverbMix.ToString(Globalization.CultureInfo.InvariantCulture),
                "EffetEchoActif=" & ParametresGlobaux.EffetEchoActif.ToString(),
                "EffetEchoMix=" & ParametresGlobaux.EffetEchoMix.ToString(Globalization.CultureInfo.InvariantCulture),
                "EffetEchoDelai=" & ParametresGlobaux.EffetEchoDelai.ToString(),
                "EffetEchoFeedback=" & ParametresGlobaux.EffetEchoFeedback.ToString(Globalization.CultureInfo.InvariantCulture),
                "EffetTimeStretchActif=" & ParametresGlobaux.EffetTimeStretchActif.ToString(),
                "EffetTimeStretchRatio=" & ParametresGlobaux.EffetTimeStretchRatio.ToString(Globalization.CultureInfo.InvariantCulture),
                "EffetPitchShiftActif=" & ParametresGlobaux.EffetPitchShiftActif.ToString(),
                "EffetPitchShiftSemiTones=" & ParametresGlobaux.EffetPitchShiftSemiTones.ToString(Globalization.CultureInfo.InvariantCulture),
                "EffetPhaserActif=" & ParametresGlobaux.EffetPhaserActif.ToString(),
                "EffetPhaserRate=" & ParametresGlobaux.EffetPhaserRate.ToString(Globalization.CultureInfo.InvariantCulture),
                "EffetPhaserDepth=" & ParametresGlobaux.EffetPhaserDepth.ToString(Globalization.CultureInfo.InvariantCulture),
                "EffetPhaserFeedback=" & ParametresGlobaux.EffetPhaserFeedback.ToString(Globalization.CultureInfo.InvariantCulture),
                "EffetPhaserMix=" & ParametresGlobaux.EffetPhaserMix.ToString(Globalization.CultureInfo.InvariantCulture),
                "EffetPhaserStages=" & ParametresGlobaux.EffetPhaserStages.ToString(),
                "ModeMixeurDJ=" & ParametresGlobaux.ModeMixeurDJ.ToString()
            }

            File.WriteAllLines(cheminConfig, lignes)

            ' Sauvegarder le chemin Python si fourni
            Try
                If TextBoxPythonPath IsNot Nothing Then
                    ParametresGlobaux.PythonPath = TextBoxPythonPath.Text.Trim()
                    ParametresGlobauxHelpers.EcrireCleParametres("PythonPath", ParametresGlobaux.PythonPath)
                End If
            Catch
            End Try

            ' Sauvegarder la méthode BPM choisie
            Try
                ParametresGlobauxHelpers.EcrireCleParametres("MethodeBPM", MethodeBPM)
            Catch
            End Try

            ' DEBUG : Vérifier si ModeMixeurDJ a été écrit
            System.Diagnostics.Debug.WriteLine($"Sauvegarde : ModeMixeurDJ={ParametresGlobaux.ModeMixeurDJ}")
            Dim lignesDansFichier = File.ReadAllLines(cheminConfig)
            Dim modeMixeurLigne = lignesDansFichier.FirstOrDefault(Function(l) l.StartsWith("ModeMixeurDJ="))
            System.Diagnostics.Debug.WriteLine($"Fichier après écriture : {modeMixeurLigne}")

            ' Sauvegarder les cases à cocher audio dans AudioDefaut.txt
            AudioDefautManager.SauvegarderAudioDefaut(
                CheckBox_MP3 IsNot Nothing AndAlso CheckBox_MP3.Checked,
                CheckBox_FLAC IsNot Nothing AndAlso CheckBox_FLAC.Checked,
                CheckBox_WMA IsNot Nothing AndAlso CheckBox_WMA.Checked,
                CheckBox_WAV IsNot Nothing AndAlso CheckBox_WAV.Checked,
                CheckBox_AAC IsNot Nothing AndAlso CheckBox_AAC.Checked)

            ' Relire et forcer l'application après sauvegarde
            ChargerParametres()
            System.Diagnostics.Debug.WriteLine($"[ButtonSauvegarder APRÈS ChargerParametres] ModeMixeurDJ = {ParametresGlobaux.ModeMixeurDJ}")
            AudioDefautManager.ChargerAudioDefaut(EtatCheckBoxMP3, EtatCheckBoxFLAC, EtatCheckBoxWMA, EtatCheckBoxWAV, EtatCheckBoxAAC)

            ' Association/suppression fichiers audio par défaut UNIQUEMENT si changement
            Dim audioChanged As Boolean = False
            If (CheckBox_MP3 IsNot Nothing AndAlso CheckBox_MP3.Checked <> EtatInitial_MP3) _
                OrElse (CheckBox_FLAC IsNot Nothing AndAlso CheckBox_FLAC.Checked <> EtatInitial_FLAC) _
                OrElse (CheckBox_WMA IsNot Nothing AndAlso CheckBox_WMA.Checked <> EtatInitial_WMA) _
                OrElse (CheckBox_WAV IsNot Nothing AndAlso CheckBox_WAV.Checked <> EtatInitial_WAV) _
                OrElse (CheckBox_AAC IsNot Nothing AndAlso CheckBox_AAC.Checked <> EtatInitial_AAC) Then
                audioChanged = True
            End If
            Dim msgResult As String = ""
            If audioChanged Then
                If CheckBox_MP3 IsNot Nothing Then
                    If CheckBox_MP3.Checked Then
                        SetAudioPlayDefault(".mp3", "AudioPlay.mp3")
                        msgResult &= "MP3, "
                    Else
                        RemoveAudioPlayDefault(".mp3")
                    End If
                End If
                If CheckBox_WAV IsNot Nothing Then
                    If CheckBox_WAV.Checked Then
                        SetAudioPlayDefault(".wav", "AudioPlay.wav")
                        msgResult &= "WAV, "
                    Else
                        RemoveAudioPlayDefault(".wav")
                    End If
                End If
                If CheckBox_FLAC IsNot Nothing Then
                    If CheckBox_FLAC.Checked Then
                        SetAudioPlayDefault(".flac", "AudioPlay.flac")
                        msgResult &= "FLAC, "
                    Else
                        RemoveAudioPlayDefault(".flac")
                    End If
                End If
                If CheckBox_AAC IsNot Nothing Then
                    If CheckBox_AAC.Checked Then
                        SetAudioPlayDefault(".aac", "AudioPlay.aac")
                        msgResult &= "AAC, "
                    Else
                        RemoveAudioPlayDefault(".aac")
                    End If
                End If
                If CheckBox_WMA IsNot Nothing Then
                    If CheckBox_WMA.Checked Then
                        SetAudioPlayDefault(".wma", "AudioPlay.wma")
                        msgResult &= "WMA, "
                    Else
                        RemoveAudioPlayDefault(".wma")
                    End If
                End If

            End If

            ' Appliquer le thème sélectionné dans la ComboBox
            If ComboBoxThemes IsNot Nothing AndAlso ComboBoxThemes.SelectedItem IsNot Nothing Then
                Dim selectedThemeKey As String = GetSelectedThemeKey()
                If Not String.IsNullOrEmpty(selectedThemeKey) Then
                    Dim selectedTheme As ThemeColors = ThemeManager.LoadNamedTheme(selectedThemeKey)
                    ThemeManager.SetCurrentTheme(selectedThemeKey, selectedTheme)
                End If
            End If

            ' Vérifier si le mode DJ a changé en comparant l'état initial avec l'état actuel
            Dim nouveauModeDJ As Boolean = ParametresGlobaux.ModeMixeurDJ
            Dim modeDJChanged As Boolean = (EtatInitial_ModeMixeurDJ <> nouveauModeDJ)
            System.Diagnostics.Debug.WriteLine($"[Détection changement] EtatInitial={EtatInitial_ModeMixeurDJ}, Nouveau={nouveauModeDJ}, Changed={modeDJChanged}")

            ' N'afficher le message de succès que si le mode DJ n'a pas changé
            ' Sinon le basculement de formulaire fera office de confirmation
            If Not modeDJChanged Then
                MessageBox.Show(LanguageManager.GetString("Success_SettingsSaved"),
                              LanguageManager.GetString("Success_Title"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Information)
            End If

            ' Détecter le type de formulaire propriétaire
            Dim estForm1 As Boolean = TypeOf Me.Owner Is Form1
            Dim estFormDJ As Boolean = TypeOf Me.Owner Is FormDJ

            If modeDJChanged Then
                ' Stocker les références avant de fermer
                Dim ownerForm As Form = Me.Owner

                ' Fermer le formulaire de paramètres
                Me.DialogResult = DialogResult.OK
                Me.Close()

                ' Basculer vers le nouveau mode depuis le formulaire propriétaire
                If EtatInitial_ModeMixeurDJ Then
                    ' Du mode DJ vers le mode normal
                    Dim formDJ As FormDJ = TryCast(ownerForm, FormDJ)
                    If formDJ IsNot Nothing Then
                        Dim form1 As New Form1()
                        form1.Show()
                        form1.Activate()
                        Application.DoEvents() ' Forcer le traitement des événements
                        formDJ.Close()
                    End If
                Else
                    ' Du mode normal vers le mode DJ
                    Dim mainForm As Form1 = TryCast(ownerForm, Form1)
                    If mainForm IsNot Nothing Then
                        Dim formDJ As New FormDJ()
                        formDJ.Show()
                        formDJ.Activate()
                        Application.DoEvents() ' Forcer le traitement des événements pour initialiser FormDJ
                        mainForm.Close()
                    Else
                        MessageBox.Show("mainForm est Nothing!", "Erreur")
                    End If
                End If
                Return
            End If

            ' Rafraîchir la langue et le thème dans le formulaire actuel
            Dim mainForm1 As Form1 = TryCast(Me.Owner, Form1)
            If mainForm1 IsNot Nothing Then
                mainForm1.RefreshLanguage()
                ThemeManager.ApplyThemeToForm(mainForm1)
                ' Restaurer l'échelle du TrackBar après l'application du thème
                mainForm1.RestaurerEchelleTrackBar()
            End If

            Dim mainFormDJ As FormDJ = TryCast(Me.Owner, FormDJ)
            If mainFormDJ IsNot Nothing Then
                mainFormDJ.RefreshLanguage()
                ThemeManager.ApplyThemeToForm(mainFormDJ)
            End If

            ' Appliquer le thème à toutes les fenêtres ouvertes
            For Each f As Form In Application.OpenForms
                ThemeManager.ApplyThemeToForm(f)
            Next

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Error_SavingSettings", ex.Message),
                          LanguageManager.GetString("Error_Title"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    ' Bouton Annuler
    Private Sub ButtonAnnuler_Click(sender As Object, e As EventArgs) Handles ButtonAnnuler.Click
        ' === Restaurer l'état initial des effets audio ===
        ParametresGlobaux.EffetReverbActif = EtatInitial_ReverbActif
        ParametresGlobaux.EffetReverbMix = EtatInitial_ReverbMix
        ParametresGlobaux.EffetEchoActif = EtatInitial_EchoActif
        ParametresGlobaux.EffetEchoMix = EtatInitial_EchoMix
        ParametresGlobaux.EffetEchoDelai = EtatInitial_EchoDelai
        ParametresGlobaux.EffetEchoFeedback = EtatInitial_EchoFeedback

        ParametresGlobaux.EffetTimeStretchActif = EtatInitial_TimeStretchActif
        ParametresGlobaux.EffetTimeStretchRatio = EtatInitial_TimeStretchRatio

        ParametresGlobaux.EffetPitchShiftActif = EtatInitial_PitchShiftActif
        ParametresGlobaux.EffetPitchShiftSemiTones = EtatInitial_PitchShiftSemiTones

        ParametresGlobaux.EffetPhaserActif = EtatInitial_PhaserActif
        ParametresGlobaux.EffetPhaserRate = EtatInitial_PhaserRate
        ParametresGlobaux.EffetPhaserDepth = EtatInitial_PhaserDepth
        ParametresGlobaux.EffetPhaserFeedback = EtatInitial_PhaserFeedback
        ParametresGlobaux.EffetPhaserMix = EtatInitial_PhaserMix
        ParametresGlobaux.EffetPhaserStages = EtatInitial_PhaserStages

        ' Appliquer immédiatement les effets restaurés
        AppliquerEffetsEnTempsReel()

        ' Restaurer le thème initial (annuler les changements de thème)
        If themeInitial IsNot Nothing AndAlso Not String.IsNullOrEmpty(themeNomInitial) Then
            ThemeManager.SetCurrentTheme(themeNomInitial, themeInitial)
            ThemeManager.ApplyThemeToForm(Me)

            ' Appliquer aussi au formulaire principal
            Dim mainForm As Form1 = TryCast(Me.Owner, Form1)
            If mainForm IsNot Nothing Then
                ThemeManager.ApplyThemeToForm(mainForm)
                mainForm.Invalidate(True)
            End If
        End If

        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    ' Bouton Réinitialiser
    Private Sub ButtonReinitialiser_Click(sender As Object, e As EventArgs) Handles ButtonReinitialiser.Click
        Dim result = MessageBox.Show(LanguageManager.GetString("Reset_ConfirmMessage"),
                                    LanguageManager.GetString("Reset_ConfirmTitle"),
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            ' Réinitialiser aux valeurs par défaut
            RepertoireParDefaut = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            VolumeLecture = 50
            LectureEnContinu = False
            AfficherBPM = True
            NormalisationVolume = True
            MethodeBPM = "Auto"
            MetronomeActif = False
            NombreBeatsMetronome = 4
            ParametresGlobaux.SupprimerSilenceDebut = True
            ParametresGlobaux.SupprimerSilenceFin = True
            If CheckBoxSupprimerSilenceDebut IsNot Nothing Then CheckBoxSupprimerSilenceDebut.Checked = True
            If CheckBoxSupprimerSilenceFin IsNot Nothing Then CheckBoxSupprimerSilenceFin.Checked = True
            If CheckBox_EffacerChansons IsNot Nothing Then
                CheckBox_EffacerChansons.Checked = True
                ParametresGlobaux.ConfirmerEffacementChansons = True
            End If
            AfficherParametres()
        End If
    End Sub

    ' Bouton Aide Normalisation - Afficher le README
    Private Sub ButtonAideNormalisation_Click(sender As Object, e As EventArgs) Handles ButtonAideNormalisation.Click
        Try
            ' Déterminer la langue actuelle
            Dim langueActuelle As String = LanguageManager.CurrentCulture.TwoLetterISOLanguageName.ToLower()
            Dim suffixeLangue As String = ""

            Select Case langueActuelle
                Case "fr"
                    suffixeLangue = ".fr"
                Case "en"
                    suffixeLangue = ".en"
                Case "es"
                    suffixeLangue = ".es"
                Case "de"
                    suffixeLangue = ".de"
                Case "it"
                    suffixeLangue = ".it"
                Case Else
                    suffixeLangue = ".en" ' Par défaut en anglais
            End Select

            Dim cheminHtml = Path.Combine(Application.StartupPath, $"NORMALISATION_README{suffixeLangue}.html")
            Dim cheminMd = Path.Combine(Application.StartupPath, $"NORMALISATION_README{suffixeLangue}.md")

            ' Préférer le HTML s'il existe
            If File.Exists(cheminHtml) Then
                ' Ouvrir le fichier HTML dans le navigateur par défaut
                Process.Start(New ProcessStartInfo(cheminHtml) With {.UseShellExecute = True})
            ElseIf File.Exists(cheminMd) Then
                ' Lire le contenu du fichier Markdown
                Dim contenu As String = File.ReadAllText(cheminMd)

                ' Créer une fenêtre pour afficher le contenu
                Dim formAide As New Form()
                formAide.Text = LanguageManager.GetString("Help_Normalization_Title")
                formAide.Size = New Size(800, 600)
                formAide.StartPosition = FormStartPosition.CenterParent
                formAide.FormBorderStyle = FormBorderStyle.Sizable
                formAide.MinimizeBox = False
                formAide.MaximizeBox = True

                ' Créer un TextBox pour afficher le contenu
                Dim textBox As New TextBox()
                textBox.Multiline = True
                textBox.ScrollBars = ScrollBars.Both
                textBox.WordWrap = True
                textBox.ReadOnly = True
                textBox.Dock = DockStyle.Fill
                textBox.Font = New Font("Consolas", 10)
                textBox.Text = contenu
                textBox.BackColor = Color.White

                ' Ajouter le TextBox au formulaire
                formAide.Controls.Add(textBox)

                ' Afficher la fenêtre
                formAide.ShowDialog()
            Else
                MessageBox.Show(LanguageManager.GetString("Help_FilesNotFound") & Environment.NewLine &
                              LanguageManager.GetString("Help_ExpectedFiles") & Environment.NewLine &
                              "- " & cheminHtml & Environment.NewLine &
                              "- " & cheminMd,
                              LanguageManager.GetString("Help_FilesNotFoundTitle"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
            End If

        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Help_ErrorOpenFile", ex.Message),
                          LanguageManager.GetString("Error_Title"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    ' Bouton Aide Thèmes - Afficher le guide des thèmes
    Private Sub Button_ThemeCouleur_Aide_Click(sender As Object, e As EventArgs) Handles Button_ThemeCouleur_Aide.Click
        Try
            ' Déterminer la langue actuelle
            Dim langueActuelle As String = LanguageManager.CurrentCulture.TwoLetterISOLanguageName.ToLower()
            Dim suffixeLangue As String = ""

            Select Case langueActuelle
                Case "fr"
                    suffixeLangue = ".fr"
                Case "en"
                    suffixeLangue = ".en"
                Case "es"
                    suffixeLangue = ".es"
                Case "de"
                    suffixeLangue = ".de"
                Case "it"
                    suffixeLangue = ".it"
                Case Else
                    suffixeLangue = ".en" ' Par défaut en anglais
            End Select

            Dim cheminHtml = Path.Combine(Application.StartupPath, $"THEMES_GUIDE_USER{suffixeLangue}.html")
            Dim cheminMd = Path.Combine(Application.StartupPath, $"THEMES_GUIDE_USER{suffixeLangue}.md")

            ' Préférer le HTML s'il existe
            If File.Exists(cheminHtml) Then
                ' Ouvrir le fichier HTML dans le navigateur par défaut
                Process.Start(New ProcessStartInfo(cheminHtml) With {.UseShellExecute = True})
            ElseIf File.Exists(cheminMd) Then
                ' Lire le contenu du fichier Markdown
                Dim contenu As String = File.ReadAllText(cheminMd)

                ' Créer une fenêtre pour afficher le contenu
                Dim formAide As New Form()
                formAide.Text = LanguageManager.GetString("Help_Themes_Title")
                formAide.Size = New Size(900, 700)
                formAide.StartPosition = FormStartPosition.CenterParent
                formAide.FormBorderStyle = FormBorderStyle.Sizable
                formAide.MinimizeBox = False
                formAide.MaximizeBox = True

                ' Créer un TextBox pour afficher le contenu
                Dim textBox As New TextBox()
                textBox.Multiline = True
                textBox.ScrollBars = ScrollBars.Both
                textBox.WordWrap = True
                textBox.ReadOnly = True
                textBox.Dock = DockStyle.Fill
                textBox.Font = New Font("Segoe UI", 10)
                textBox.Text = contenu
                textBox.BackColor = Color.White

                ' Ajouter le TextBox au formulaire
                formAide.Controls.Add(textBox)

                ' Afficher la fenêtre
                formAide.ShowDialog()
            Else
                MessageBox.Show(LanguageManager.GetString("Help_FilesNotFound") & Environment.NewLine &
                              LanguageManager.GetString("Help_ExpectedFiles") & Environment.NewLine &
                              "- " & cheminHtml & Environment.NewLine &
                              "- " & cheminMd,
                              LanguageManager.GetString("Help_FilesNotFoundTitle"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
            End If

        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Help_ErrorOpenFile", ex.Message),
                          LanguageManager.GetString("Error_Title"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Button_Metronome_Aide_Click(sender As Object, e As EventArgs) Handles Button_Metronome_Aide.Click
        Try
            ' Déterminer la langue actuelle
            Dim langueActuelle = LanguageManager.CurrentCulture.TwoLetterISOLanguageName.ToLower
            Dim suffixeLangue = ""

            Select Case langueActuelle
                Case "fr"
                    suffixeLangue = ".fr"
                Case "en"
                    suffixeLangue = ".en"
                Case "es"
                    suffixeLangue = ".es"
                Case "de"
                    suffixeLangue = ".de"
                Case "it"
                    suffixeLangue = ".it"
                Case Else
                    suffixeLangue = ".en" ' Par défaut en anglais
            End Select

            Dim cheminHtml = Path.Combine(Application.StartupPath, $"METRONOME_GUIDE_USER{suffixeLangue}.html")

            ' Ouvrir le fichier HTML dans le navigateur par défaut
            If File.Exists(cheminHtml) Then
                Process.Start(New ProcessStartInfo(cheminHtml) With {.UseShellExecute = True})
            Else
                MessageBox.Show(LanguageManager.GetString("Help_FilesNotFound") & Environment.NewLine &
                              LanguageManager.GetString("Help_ExpectedFiles") & Environment.NewLine &
                              "- " & cheminHtml,
                              LanguageManager.GetString("Help_Metronome_Title"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
            End If

        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Help_ErrorOpenFile", ex.Message),
                          LanguageManager.GetString("Error_Title"),
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub TextBoxRepertoire_TextChanged(sender As Object, e As EventArgs) Handles TextBoxRepertoire.TextChanged

    End Sub

    Private Function ClonerTheme(source As ThemeColors) As ThemeColors
        Return New ThemeColors With {
            .FormBackColor = source.FormBackColor,
            .ControlBackColor = source.ControlBackColor,
            .ControlForeColor = source.ControlForeColor,
            .ButtonBackColor = source.ButtonBackColor,
            .ButtonForeColor = source.ButtonForeColor,
            .ListViewBackColor = source.ListViewBackColor,
            .ListViewForeColor = source.ListViewForeColor,
            .ListViewHeaderBackColor = source.ListViewHeaderBackColor,
            .ListViewHeaderForeColor = source.ListViewHeaderForeColor,
            .ListViewSelectionBackColor = source.ListViewSelectionBackColor,
            .ListViewSelectionForeColor = source.ListViewSelectionForeColor,
            .TextBoxBackColor = source.TextBoxBackColor,
            .TextBoxForeColor = source.TextBoxForeColor,
            .GroupBoxForeColor = source.GroupBoxForeColor,
            .GroupBoxBorderColor = source.GroupBoxBorderColor,
            .TrackBarBackColor = source.TrackBarBackColor
        }
    End Function

    Private Function LireCouleurTheme(theme As ThemeColors, key As String) As Color
        Select Case key
            Case NameOf(ThemeColors.FormBackColor) : Return theme.FormBackColor
            Case NameOf(ThemeColors.ControlBackColor) : Return theme.ControlBackColor
            Case NameOf(ThemeColors.ControlForeColor) : Return theme.ControlForeColor
            Case NameOf(ThemeColors.ButtonBackColor) : Return theme.ButtonBackColor
            Case NameOf(ThemeColors.ButtonForeColor) : Return theme.ButtonForeColor
            Case NameOf(ThemeColors.TextBoxBackColor) : Return theme.TextBoxBackColor
            Case NameOf(ThemeColors.TextBoxForeColor) : Return theme.TextBoxForeColor
            Case NameOf(ThemeColors.ListViewBackColor) : Return theme.ListViewBackColor
            Case NameOf(ThemeColors.ListViewForeColor) : Return theme.ListViewForeColor
            Case NameOf(ThemeColors.ListViewHeaderBackColor) : Return theme.ListViewHeaderBackColor
            Case NameOf(ThemeColors.ListViewHeaderForeColor) : Return theme.ListViewHeaderForeColor
            Case NameOf(ThemeColors.ListViewSelectionBackColor) : Return theme.ListViewSelectionBackColor
            Case NameOf(ThemeColors.ListViewSelectionForeColor) : Return theme.ListViewSelectionForeColor
            Case NameOf(ThemeColors.GroupBoxForeColor) : Return theme.GroupBoxForeColor
            Case NameOf(ThemeColors.GroupBoxBorderColor) : Return theme.GroupBoxBorderColor
            Case NameOf(ThemeColors.TrackBarBackColor) : Return theme.TrackBarBackColor
        End Select

        Return Color.White
    End Function

    Private Sub EcrireCouleurTheme(theme As ThemeColors, key As String, value As Color)
        Select Case key
            Case NameOf(ThemeColors.FormBackColor) : theme.FormBackColor = value
            Case NameOf(ThemeColors.ControlBackColor) : theme.ControlBackColor = value
            Case NameOf(ThemeColors.ControlForeColor) : theme.ControlForeColor = value
            Case NameOf(ThemeColors.ButtonBackColor) : theme.ButtonBackColor = value
            Case NameOf(ThemeColors.ButtonForeColor) : theme.ButtonForeColor = value
            Case NameOf(ThemeColors.TextBoxBackColor) : theme.TextBoxBackColor = value
            Case NameOf(ThemeColors.TextBoxForeColor) : theme.TextBoxForeColor = value
            Case NameOf(ThemeColors.ListViewBackColor) : theme.ListViewBackColor = value
            Case NameOf(ThemeColors.ListViewForeColor) : theme.ListViewForeColor = value
            Case NameOf(ThemeColors.ListViewHeaderBackColor) : theme.ListViewHeaderBackColor = value
            Case NameOf(ThemeColors.ListViewHeaderForeColor) : theme.ListViewHeaderForeColor = value
            Case NameOf(ThemeColors.ListViewSelectionBackColor) : theme.ListViewSelectionBackColor = value
            Case NameOf(ThemeColors.ListViewSelectionForeColor) : theme.ListViewSelectionForeColor = value
            Case NameOf(ThemeColors.GroupBoxForeColor) : theme.GroupBoxForeColor = value
            Case NameOf(ThemeColors.GroupBoxBorderColor) : theme.GroupBoxBorderColor = value
            Case NameOf(ThemeColors.TrackBarBackColor) : theme.TrackBarBackColor = value
        End Select
    End Sub

    Private Sub AppliquerThemeEnApercu(theme As ThemeColors)
        Dim currentThemeName = ThemeManager.GetCurrentThemeName()
        ThemeManager.SetCurrentTheme(currentThemeName, theme)
        ThemeManager.ApplyThemeToForm(Me)
        Dim mainForm As Form1 = TryCast(Me.Owner, Form1)
        If mainForm IsNot Nothing Then
            ThemeManager.ApplyThemeToForm(mainForm)
            mainForm.Invalidate(True)
        End If
    End Sub

    Private Sub ButtonPersonnaliserCouleurs_Click(sender As Object, e As EventArgs) Handles ButtonPersonnaliserCouleurs.Click
        ' Permettre la création d'un nouveau thème à partir de n'importe quel thème
        ' La protection se fait lors de la sauvegarde : impossible d'écraser un thème préinstallé

        If themeEnEdition Is Nothing Then
            themeEnEdition = ThemeManager.GetCurrentTheme()
        End If

        Dim originalTheme = ClonerTheme(themeEnEdition)
        Dim workingTheme = ClonerTheme(themeEnEdition)
        Dim aEteApplique As Boolean = False

        Dim dlg As New Form()
        dlg.Text = LanguageManager.GetString("Theme_Group")
        dlg.Size = New Size(520, 420)
        dlg.StartPosition = FormStartPosition.CenterParent
        dlg.FormBorderStyle = FormBorderStyle.FixedDialog
        dlg.MinimizeBox = False
        dlg.MaximizeBox = False
        ThemeManager.ApplyThemeToForm(dlg)

        Dim listOptions As New ListBox()
        listOptions.Location = New Point(12, 12)
        listOptions.Size = New Size(330, 320)

        Dim options As New List(Of ThemeColorOption) From {
            New ThemeColorOption With {.Key = NameOf(ThemeColors.FormBackColor), .Label = LanguageManager.GetString("Theme_Pick_FormBack")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.ControlBackColor), .Label = LanguageManager.GetString("Theme_Pick_ControlBack")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.ControlForeColor), .Label = LanguageManager.GetString("Theme_Pick_ControlText")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.ButtonBackColor), .Label = LanguageManager.GetString("Theme_Pick_ButtonBack")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.ButtonForeColor), .Label = LanguageManager.GetString("Theme_Pick_ButtonText")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.TextBoxBackColor), .Label = LanguageManager.GetString("Theme_Pick_TextBoxBack")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.TextBoxForeColor), .Label = LanguageManager.GetString("Theme_Pick_TextBoxText")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.ListViewBackColor), .Label = LanguageManager.GetString("Theme_Pick_ListViewBack")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.ListViewForeColor), .Label = LanguageManager.GetString("Theme_Pick_ListViewText")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.ListViewHeaderBackColor), .Label = LanguageManager.GetString("Theme_Pick_ListViewHeaderBack")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.ListViewHeaderForeColor), .Label = LanguageManager.GetString("Theme_Pick_ListViewHeaderText")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.ListViewSelectionBackColor), .Label = LanguageManager.GetString("Theme_Pick_ListViewSelBack")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.ListViewSelectionForeColor), .Label = LanguageManager.GetString("Theme_Pick_ListViewSelText")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.GroupBoxForeColor), .Label = LanguageManager.GetString("Theme_Pick_GroupBoxText")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.GroupBoxBorderColor), .Label = LanguageManager.GetString("Theme_Pick_GroupBoxBorder")},
            New ThemeColorOption With {.Key = NameOf(ThemeColors.TrackBarBackColor), .Label = LanguageManager.GetString("Theme_Pick_TrackBarBack")}
        }

        For Each opt In options
            listOptions.Items.Add(opt)
        Next

        Dim panelPreview As New Panel()
        panelPreview.Location = New Point(360, 40)
        panelPreview.Size = New Size(130, 80)
        panelPreview.BorderStyle = BorderStyle.FixedSingle

        Dim lblPreview As New Label()
        lblPreview.Text = LanguageManager.GetString("Theme_Preview")
        lblPreview.Location = New Point(360, 12)
        lblPreview.AutoSize = True

        Dim btnChoose As New Button()
        btnChoose.Text = LanguageManager.GetString("Theme_Customize")
        btnChoose.Location = New Point(360, 140)
        btnChoose.Size = New Size(130, 50)

        Dim btnApply As New Button()
        btnApply.Text = LanguageManager.GetString("Theme_Apply")
        btnApply.Location = New Point(360, 195)
        btnApply.Size = New Size(130, 30)

        Dim btnOk As New Button()
        btnOk.Text = LanguageManager.GetString("Theme_OK")
        btnOk.Location = New Point(360, 300)
        btnOk.Size = New Size(130, 30)

        Dim btnCancel As New Button()
        btnCancel.Text = LanguageManager.GetString("Button_Cancel")
        btnCancel.Location = New Point(360, 335)
        btnCancel.Size = New Size(130, 30)

        Dim updatePreview As Action = Sub()
                                          If listOptions.SelectedItem Is Nothing Then
                                              panelPreview.BackColor = Me.BackColor
                                              Return
                                          End If

                                          Dim selected = DirectCast(listOptions.SelectedItem, ThemeColorOption)
                                          panelPreview.BackColor = LireCouleurTheme(workingTheme, selected.Key)
                                      End Sub

        AddHandler listOptions.SelectedIndexChanged, Sub()
                                                         updatePreview()
                                                     End Sub

        AddHandler btnChoose.Click, Sub()
                                        If listOptions.SelectedItem Is Nothing Then Return
                                        Dim selected = DirectCast(listOptions.SelectedItem, ThemeColorOption)
                                        Dim currentColor = LireCouleurTheme(workingTheme, selected.Key)

                                        Using colorDlg As New ColorDialog()
                                            colorDlg.FullOpen = True
                                            colorDlg.Color = currentColor

                                            If colorDlg.ShowDialog(dlg) = DialogResult.OK Then
                                                EcrireCouleurTheme(workingTheme, selected.Key, colorDlg.Color)
                                                updatePreview()
                                            End If
                                        End Using
                                    End Sub

        AddHandler btnApply.Click, Sub()
                                       themeEnEdition = ClonerTheme(workingTheme)
                                       AppliquerThemeEnApercu(themeEnEdition)
                                       aEteApplique = True
                                   End Sub

        AddHandler btnOk.Click, Sub()
                                    ' Demander le nom du nouveau thème avec validation
                                    Dim themeName As String = ""
                                    Dim nomValide As Boolean = False
                                    Dim annule As Boolean = False

                                    Do While Not nomValide And Not annule
                                        themeName = InputBox(
                                            LanguageManager.GetString("Theme_SaveDialog_Prompt"),
                                            LanguageManager.GetString("Theme_SaveDialog_Title"),
                                            "")

                                        If String.IsNullOrWhiteSpace(themeName) Then
                                            ' L'utilisateur a annulé (laissé vide ou cliqué Annuler)
                                            annule = True
                                        ElseIf VerifierSiThemePreinstalle(themeName) Then
                                            ' Le nom choisi est un nom de thème préinstallé protégé
                                            MessageBox.Show(
                                                LanguageManager.GetString("Theme_PreinstalledCannotReplace"),
                                                LanguageManager.GetString("Error_Title"),
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Warning)
                                            ' Redemander un nom (boucle continue)
                                        Else
                                            ' Nom valide
                                            nomValide = True
                                        End If
                                    Loop

                                    If nomValide Then
                                        ' Sauvegarder le nouveau thème
                                        ThemeManager.SaveNamedTheme(themeName, workingTheme)

                                        ' Recharger la liste des thèmes pour inclure le nouveau
                                        ' Mais on garde la sélection actuelle (ne pas sélectionner le nouveau thème)
                                        Dim selectionActuelle As String = ""
                                        If ComboBoxThemes IsNot Nothing AndAlso ComboBoxThemes.SelectedItem IsNot Nothing Then
                                            selectionActuelle = ComboBoxThemes.SelectedItem.ToString()
                                        End If

                                        ChargerListeThemes()

                                        ' Restaurer la sélection précédente dans la ComboBox
                                        If Not String.IsNullOrEmpty(selectionActuelle) AndAlso ComboBoxThemes IsNot Nothing Then
                                            For i As Integer = 0 To ComboBoxThemes.Items.Count - 1
                                                If ComboBoxThemes.Items(i).ToString().Equals(selectionActuelle, StringComparison.OrdinalIgnoreCase) Then
                                                    ComboBoxThemes.SelectedIndex = i
                                                    Exit For
                                                End If
                                            Next
                                        End If
                                    End If

                                    ' Dans tous les cas (annulé ou sauvegardé), restaurer l'apparence d'origine
                                    If aEteApplique Then
                                        themeEnEdition = ClonerTheme(originalTheme)
                                        AppliquerThemeEnApercu(themeEnEdition)
                                    End If

                                    dlg.DialogResult = DialogResult.OK
                                    dlg.Close()
                                End Sub

        AddHandler btnCancel.Click, Sub()
                                        ' Restaurer l'apparence d'origine si elle a été modifiée
                                        If aEteApplique Then
                                            themeEnEdition = ClonerTheme(originalTheme)
                                            AppliquerThemeEnApercu(themeEnEdition)
                                        End If
                                        dlg.DialogResult = DialogResult.Cancel
                                        dlg.Close()
                                    End Sub

        dlg.Controls.Add(listOptions)
        dlg.Controls.Add(lblPreview)
        dlg.Controls.Add(panelPreview)
        dlg.Controls.Add(btnChoose)
        dlg.Controls.Add(btnApply)
        dlg.Controls.Add(btnOk)
        dlg.Controls.Add(btnCancel)

        If listOptions.Items.Count > 0 Then
            listOptions.SelectedIndex = 0
        End If

        dlg.ShowDialog(Me)
    End Sub

    Private Sub ButtonResetCouleurs_Click(sender As Object, e As EventArgs) Handles ButtonResetCouleurs.Click
        themeEnEdition = ThemeManager.GetDefaultTheme()
        ThemeManager.ResetThemeToDefault()

        ThemeManager.ApplyThemeToForm(Me)

        Dim mainForm As Form1 = TryCast(Me.Owner, Form1)
        If mainForm IsNot Nothing Then
            ThemeManager.ApplyThemeToForm(mainForm)
            mainForm.Invalidate(True)
        End If

        ' Rafraîchir la liste des thèmes pour sélectionner "Par défaut"
        ChargerListeThemes()
    End Sub

    Private Sub Button_ViewCrashLog_Click(sender As Object, e As EventArgs) Handles Button_ViewCrashLog.Click
        Try
            Dim logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay")
            Dim logFile = Path.Combine(logDir, "crash.log")
            If File.Exists(logFile) Then
                Process.Start(New ProcessStartInfo(logFile) With {.UseShellExecute = True})
            Else
                MessageBox.Show(LanguageManager.GetString("CrashLog_NotFound"), LanguageManager.GetString("CrashLog_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Error_Generic"), LanguageManager.GetString("Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ComboBoxThemes_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxThemes.SelectedIndexChanged
        If ComboBoxThemes.SelectedItem Is Nothing Then Return
        Dim themeName As String = ComboBoxThemes.SelectedItem.ToString()
        ' If item is ThemeDisplayItem, use its Key
        Dim displayItem = TryCast(ComboBoxThemes.SelectedItem, ThemeDisplayItem)
        If displayItem IsNot Nothing Then
            themeName = displayItem.Key
        End If
        Dim theme = ThemeManager.LoadNamedTheme(themeName)

        ' Prévisualiser le thème sans le sauvegarder
        themeEnEdition = theme

        ' Appliquer le thème en prévisualisation (avec la surcharge qui prend un thème en paramètre)
        ThemeManager.ApplyThemeToForm(Me, theme)

        Dim mainForm As Form1 = TryCast(Me.Owner, Form1)
        If mainForm IsNot Nothing Then
            ThemeManager.ApplyThemeToForm(mainForm, theme)
            mainForm.Invalidate(True)
        End If

        ' Gérer la protection des thèmes préinstallés
        GererProtectionThemesPreinstalles(themeName)
    End Sub

    ''' <summary>
    ''' Active ou désactive les contrôles de personnalisation selon si un thème préinstallé est sélectionné
    ''' Les thèmes préinstallés protégés sont : Par défaut, Automne, Océan, Soleil, Sombre
    ''' </summary>
    Private Sub GererProtectionThemesPreinstalles(themeName As String)
        Dim estThemePreinstalle As Boolean = VerifierSiThemePreinstalle(themeName)

        ' Le bouton "Créer un thème de couleurs..." est TOUJOURS actif
        ' Il permet de créer un nouveau thème même si un thème préinstallé est sélectionné
        ' La protection se fait lors de la sauvegarde (impossible d'écraser un thème préinstallé)
        If ButtonPersonnaliserCouleurs IsNot Nothing Then
            ButtonPersonnaliserCouleurs.Enabled = True
        End If

        ' Le bouton "Enregistrer le thème sous" reste actif pour permettre la duplication
        ' des thèmes préinstallés vers un nouveau nom
        ' Pour les thèmes utilisateur, il sert à sauvegarder les modifications

        If ButtonResetCouleurs IsNot Nothing Then
            ButtonResetCouleurs.Enabled = Not estThemePreinstalle
        End If

        ' Le bouton de suppression : impossible de supprimer les thèmes préinstallés
        If ButtonDeleteTheme IsNot Nothing Then
            ButtonDeleteTheme.Enabled = Not estThemePreinstalle
        End If
    End Sub

    ''' <summary>
    ''' Vérifie si un thème fait partie des thèmes préinstallés protégés
    ''' </summary>
    Private Function VerifierSiThemePreinstalle(themeName As String) As Boolean
        Dim themesPreinstalles As String() = {
            "Par défaut",
            "Automne",
            "Océan",
            "Soleil",
            "Sombre"
        }

        For Each theme As String In themesPreinstalles
            If theme.Equals(themeName, StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
        Next

        Return False
    End Function

    Private Sub ButtonSaveTheme_Click(sender As Object, e As EventArgs) Handles ButtonSaveTheme.Click
        ' Ce bouton permet de dupliquer le thème actuellement sélectionné dans la ComboBox
        If ComboBoxThemes Is Nothing OrElse ComboBoxThemes.SelectedItem Is Nothing Then
            Return
        End If

        ' Charger le thème actuellement sélectionné
        Dim currentThemeName As String = ComboBoxThemes.SelectedItem.ToString()
        Dim currentTheme As ThemeColors = ThemeManager.LoadNamedTheme(currentThemeName)

        ' Demander le nom du nouveau thème avec validation
        Dim themeName As String = ""
        Dim nomValide As Boolean = False

        Do While Not nomValide
            themeName = InputBox(
                LanguageManager.GetString("Theme_SaveDialog_Prompt"),
                LanguageManager.GetString("Theme_SaveDialog_Title"),
                currentThemeName & " (copie)")

            If String.IsNullOrWhiteSpace(themeName) Then
                ' L'utilisateur a annulé
                Return
            ElseIf VerifierSiThemePreinstalle(themeName) Then
                ' Le nom choisi est un nom de thème préinstallé protégé
                MessageBox.Show(
                    LanguageManager.GetString("Theme_PreinstalledCannotReplace"),
                    LanguageManager.GetString("Error_Title"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                ' Redemander un nom
            Else
                ' Nom valide
                nomValide = True
            End If
        Loop

        ' Sauvegarder le thème sous le nouveau nom (duplication)
        ThemeManager.SaveNamedTheme(themeName, currentTheme)

        ' Rafraîchir la liste et garder la sélection actuelle
        Dim selectionActuelle As String = currentThemeName ' placeholder: no functional change
        ChargerListeThemes()

        ' Restaurer la sélection précédente (ne pas sélectionner le nouveau thème)
        Dim index = ComboBoxThemes.Items.IndexOf(selectionActuelle)
        If index >= 0 Then
            ComboBoxThemes.SelectedIndex = index
        End If

        ' Informer l'utilisateur
        MessageBox.Show(
            String.Format(LanguageManager.GetString("Theme_DuplicatedSuccess"), themeName),
            LanguageManager.GetString("Success_Title"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information)
    End Sub

    Private Sub ButtonDeleteTheme_Click(sender As Object, e As EventArgs) Handles ButtonDeleteTheme.Click
        If ComboBoxThemes.SelectedItem Is Nothing Then Return

        Dim themeName As String = ComboBoxThemes.SelectedItem.ToString()

        ' Ne pas permettre de supprimer les thèmes préinstallés
        If VerifierSiThemePreinstalle(themeName) Then
            MessageBox.Show(
                LanguageManager.GetString("Theme_PreinstalledCannotDelete"),
                LanguageManager.GetString("Error_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning)
            Return
        End If

        ' Confirmation
        Dim result = MessageBox.Show(
            String.Format(LanguageManager.GetString("Theme_DeleteConfirm"), themeName),
            LanguageManager.GetString("Theme_DeleteConfirm_Title"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            ThemeManager.DeleteTheme(themeName)

            ' Charger le thème par défaut
            Dim defaultTheme = ThemeManager.LoadNamedTheme("Par défaut")
            ThemeManager.SetCurrentTheme("Par défaut", defaultTheme)
            themeEnEdition = defaultTheme

            ' Rafraîchir la liste et l'affichage
            ChargerListeThemes()
            ThemeManager.ApplyThemeToForm(Me)

            Dim mainForm As Form1 = TryCast(Me.Owner, Form1)
            If mainForm IsNot Nothing Then
                ThemeManager.ApplyThemeToForm(mainForm)
                mainForm.Invalidate(True)
            End If
        End If
    End Sub


    ' ========================================
    ' GESTION MULTI-LANGUE
    ' ========================================

    Public Sub RefreshLanguage()
        ' Rafraîchir le titre du formulaire
        Me.Text = LanguageManager.GetString("Params_Title")

        ' Rafraîchir les GroupBox
        ' GroupBoxGeneral.Text = LanguageManager.GetString("Params_GeneralSettings")
        GroupBoxLecture.Text = LanguageManager.GetString("Params_PlaybackSettings")

        ' Repeupler la ComboBoxMethodeBPM pour supporter la localisation dynamique
        If ComboBoxMethodeBPM IsNot Nothing Then
            ComboBoxMethodeBPM.Items.Clear()
            ComboBoxMethodeBPM.Items.Add(LanguageManager.GetString("Params_BPMMethod_Auto"))
            ComboBoxMethodeBPM.Items.Add(LanguageManager.GetString("Params_BPMMethod_Librosa"))
            ComboBoxMethodeBPM.Items.Add(LanguageManager.GetString("Params_BPMMethod_SoundTouch"))
            ' Restaurer la sélection à partir des paramètres si possible
            Try
                Select Case MethodeBPM
                    Case "Auto"
                        ComboBoxMethodeBPM.SelectedIndex = 0
                    Case "Librosa"
                        ComboBoxMethodeBPM.SelectedIndex = 1
                    Case "SoundTouch"
                        ComboBoxMethodeBPM.SelectedIndex = 2
                    Case Else
                        If ComboBoxMethodeBPM.Items.Count > 0 Then ComboBoxMethodeBPM.SelectedIndex = 0
                End Select
            Catch
                If ComboBoxMethodeBPM.Items.Count > 0 Then ComboBoxMethodeBPM.SelectedIndex = 0
            End Try
        End If
        GroupBoxLangue.Text = LanguageManager.GetString("Params_LanguageSettings")

        ' Rafraîchir les labels
        ' LabelRepertoire.Text = LanguageManager.GetString("Params_DefaultFolder")
        LabelMethodeBPM.Text = LanguageManager.GetString("Params_BPMMethod")
        LabelNombreBeats.Text = LanguageManager.GetString("Params_MetronomeBeats")
        LabelLangue.Text = LanguageManager.GetString("Params_Language")
        ' Labels / boutons liés à Python / Librosa
        If LabelLibrosaExist IsNot Nothing Then LabelLibrosaExist.Text = LanguageManager.GetString("LabelLibrosaExist")
        If LabelPythonPath IsNot Nothing Then LabelPythonPath.Text = LanguageManager.GetString("LabelPythonPath")
        If ButtonCheckLibrosa IsNot Nothing Then ButtonCheckLibrosa.Text = LanguageManager.GetString("ButtonCheckLibrosa")
        If ButtonBrowsePython IsNot Nothing Then ButtonBrowsePython.Text = LanguageManager.GetString("ButtonBrowsePython")
        ' Bouton pour afficher le dernier crash (log)
        If Button_ViewCrashLog IsNot Nothing Then Button_ViewCrashLog.Text = LanguageManager.GetString("Button_ViewCrashLog")

        ' Rafraîchir les cases à cocher
        CheckBoxLectureAuto.Text = LanguageManager.GetString("Params_AutoPlay")
        CheckBoxAfficherBPM.Text = LanguageManager.GetString("Params_ShowBPM")
        CheckBoxNormalisationVolume.Text = LanguageManager.GetString("Params_VolumeNormalization")
        CheckBoxMetronome.Text = LanguageManager.GetString("Params_EnableMetronome")
        CheckBoxMetronomeSon.Text = LanguageManager.GetString("Params_MetronomeSound")
        CheckBoxMetronomeLumiere.Text = LanguageManager.GetString("Params_MetronomeLight")
        CheckBoxSupprimerSilenceDebut.Text = LanguageManager.GetString("Params_RemoveSilenceStart")
        CheckBoxSupprimerSilenceFin.Text = LanguageManager.GetString("Params_RemoveSilenceEnd")
        If CheckBox_EffacerChansons IsNot Nothing Then CheckBox_EffacerChansons.Text = LanguageManager.GetString("CheckBox_EffacerChansons")
        If CheckBoxModeMixeurDJ IsNot Nothing Then CheckBoxModeMixeurDJ.Text = LanguageManager.GetString("Params_DJMixerMode")

        ' Rafraîchir les boutons
        '  ButtonParcourir.Text = LanguageManager.GetString("Button_Browse")
        ButtonSauvegarder.Text = LanguageManager.GetString("Button_Save")
        ButtonAnnuler.Text = LanguageManager.GetString("Button_Cancel")
        ButtonReinitialiser.Text = LanguageManager.GetString("Button_Reset")
        If ButtonApplyNow IsNot Nothing Then ButtonApplyNow.Text = LanguageManager.GetString("Button_ApplyNow")
        ButtonAideNormalisation.Text = LanguageManager.GetString("Button_Help")
        Button_Metronome_Aide.Text = LanguageManager.GetString("Button_Help")

        If GroupBoxCouleurs IsNot Nothing Then GroupBoxCouleurs.Text = LanguageManager.GetString("Theme_Group")
        If LabelTheme IsNot Nothing Then LabelTheme.Text = LanguageManager.GetString("Theme_Select")
        If ButtonSaveTheme IsNot Nothing Then ButtonSaveTheme.Text = LanguageManager.GetString("Theme_SaveAs")
        If ButtonDeleteTheme IsNot Nothing Then ButtonDeleteTheme.Text = LanguageManager.GetString("Theme_Delete")
        If ButtonPersonnaliserCouleurs IsNot Nothing Then ButtonPersonnaliserCouleurs.Text = LanguageManager.GetString("Theme_Customize")
        If ButtonResetCouleurs IsNot Nothing Then ButtonResetCouleurs.Text = LanguageManager.GetString("Theme_Reset")
        If Button_ThemeCouleur_Aide IsNot Nothing Then Button_ThemeCouleur_Aide.Text = LanguageManager.GetString("Button_Help")

        ' === Effets Audio ===
        If GroupBoxEffetsAudio IsNot Nothing Then GroupBoxEffetsAudio.Text = LanguageManager.GetString("AudioEffects_GroupTitle")
        If CheckBoxReverbActif IsNot Nothing Then CheckBoxReverbActif.Text = LanguageManager.GetString("AudioEffects_Reverb")
        If LabelReverbMix IsNot Nothing Then LabelReverbMix.Text = LanguageManager.GetString("AudioEffects_ReverbMix")
        If CheckBoxEchoActif IsNot Nothing Then CheckBoxEchoActif.Text = LanguageManager.GetString("AudioEffects_Echo")
        If LabelEchoMix IsNot Nothing Then LabelEchoMix.Text = LanguageManager.GetString("AudioEffects_EchoMix")
        If LabelEchoDelai IsNot Nothing Then LabelEchoDelai.Text = LanguageManager.GetString("AudioEffects_EchoDelay")
        If LabelEchoFeedback IsNot Nothing Then LabelEchoFeedback.Text = LanguageManager.GetString("AudioEffects_EchoFeedback")
        If CheckBoxTimeStretchActif IsNot Nothing Then CheckBoxTimeStretchActif.Text = LanguageManager.GetString("AudioEffects_TimeStretch")
        If LabelTimeStretch IsNot Nothing Then LabelTimeStretch.Text = LanguageManager.GetString("AudioEffects_TimeStretchRatio")
        If ButtonResetEffets IsNot Nothing Then ButtonResetEffets.Text = LanguageManager.GetString("AudioEffects_ResetButton")

        ' === Pitch Shift ===
        If CheckBoxPitchShiftActif IsNot Nothing Then CheckBoxPitchShiftActif.Text = LanguageManager.GetString("AudioEffects_PitchShift")
        If LabelPitchShift IsNot Nothing Then LabelPitchShift.Text = LanguageManager.GetString("AudioEffects_PitchSemitones")

        ' === Phaser ===
        If CheckBoxPhaserActif IsNot Nothing Then CheckBoxPhaserActif.Text = LanguageManager.GetString("AudioEffects_Phaser")
        If LabelPhaserRate IsNot Nothing Then LabelPhaserRate.Text = LanguageManager.GetString("AudioEffects_PhaserRate")
        If LabelPhaserDepth IsNot Nothing Then LabelPhaserDepth.Text = LanguageManager.GetString("AudioEffects_PhaserDepth")
        If LabelPhaserFeedback IsNot Nothing Then LabelPhaserFeedback.Text = LanguageManager.GetString("AudioEffects_PhaserFeedback")
        If LabelPhaserMix IsNot Nothing Then LabelPhaserMix.Text = LanguageManager.GetString("AudioEffects_PhaserMix")
        If LabelPhaserStages IsNot Nothing Then LabelPhaserStages.Text = LanguageManager.GetString("AudioEffects_PhaserStages")

        ' === Types Audio par Défaut ===
        If GroupBox_TypesAudioDefaut IsNot Nothing Then GroupBox_TypesAudioDefaut.Text = LanguageManager.GetString("AudioTypes_GroupTitle")
        If LabelTypesAudioDefaut IsNot Nothing Then LabelTypesAudioDefaut.Text = LanguageManager.GetString("AudioTypes_Label")

        System.Diagnostics.Debug.WriteLine("Langue rafraîchie dans FormParametres")
    End Sub

    ''' <summary>
    ''' Charge les valeurs des effets audio depuis ParametresGlobaux vers l'UI
    ''' </summary>
    Private Sub ChargerEffetsAudioDansUI()
        ' Reverb
        If CheckBoxReverbActif IsNot Nothing Then
            CheckBoxReverbActif.Checked = ParametresGlobaux.EffetReverbActif
        End If
        If TrackBarReverbMix IsNot Nothing Then
            TrackBarReverbMix.Value = CInt(ParametresGlobaux.EffetReverbMix * 100)
            If LabelReverbMixValeur IsNot Nothing Then
                LabelReverbMixValeur.Text = $"{TrackBarReverbMix.Value}%"
            End If
        End If

        ' Echo
        If CheckBoxEchoActif IsNot Nothing Then
            CheckBoxEchoActif.Checked = ParametresGlobaux.EffetEchoActif
        End If
        If TrackBarEchoMix IsNot Nothing Then
            TrackBarEchoMix.Value = CInt(ParametresGlobaux.EffetEchoMix * 100)
            If LabelEchoMixValeur IsNot Nothing Then
                LabelEchoMixValeur.Text = $"{TrackBarEchoMix.Value}%"
            End If
        End If
        If TrackBarEchoDelai IsNot Nothing Then
            Dim valeurTrackBar As Integer = ParametresGlobaux.EffetEchoDelai \ 10
            ' Valider la plage (Minimum=5, Maximum=200)
            If valeurTrackBar < TrackBarEchoDelai.Minimum Then valeurTrackBar = TrackBarEchoDelai.Minimum
            If valeurTrackBar > TrackBarEchoDelai.Maximum Then valeurTrackBar = TrackBarEchoDelai.Maximum
            TrackBarEchoDelai.Value = valeurTrackBar
            If LabelEchoDelaiValeur IsNot Nothing Then
                LabelEchoDelaiValeur.Text = $"{ParametresGlobaux.EffetEchoDelai} ms"
            End If
        End If
        If TrackBarEchoFeedback IsNot Nothing Then
            TrackBarEchoFeedback.Value = CInt(ParametresGlobaux.EffetEchoFeedback * 100)
            If LabelEchoFeedbackValeur IsNot Nothing Then
                LabelEchoFeedbackValeur.Text = $"{TrackBarEchoFeedback.Value}%"
            End If
        End If

        ' Time Stretch
        If CheckBoxTimeStretchActif IsNot Nothing Then
            CheckBoxTimeStretchActif.Checked = ParametresGlobaux.EffetTimeStretchActif
        End If
        If TrackBarTimeStretch IsNot Nothing Then
            TrackBarTimeStretch.Value = CInt(ParametresGlobaux.EffetTimeStretchRatio * 100)
            If LabelTimeStretchValeur IsNot Nothing Then
                LabelTimeStretchValeur.Text = $"{ParametresGlobaux.EffetTimeStretchRatio:F2}x"
            End If
        End If

        ' Pitch Shift
        If CheckBoxPitchShiftActif IsNot Nothing Then
            CheckBoxPitchShiftActif.Checked = ParametresGlobaux.EffetPitchShiftActif
        End If
        If TrackBarPitchShift IsNot Nothing Then
            TrackBarPitchShift.Value = CInt(ParametresGlobaux.EffetPitchShiftSemiTones * 10)
            If LabelPitchShiftValeur IsNot Nothing Then
                LabelPitchShiftValeur.Text = ParametresGlobaux.EffetPitchShiftSemiTones.ToString("F1")
            End If
        End If

        ' Phaser
        If CheckBoxPhaserActif IsNot Nothing Then
            CheckBoxPhaserActif.Checked = ParametresGlobaux.EffetPhaserActif
        End If
        If TrackBarPhaserRate IsNot Nothing Then
            TrackBarPhaserRate.Value = CInt(ParametresGlobaux.EffetPhaserRate * 10)
            If LabelPhaserRateValeur IsNot Nothing Then
                LabelPhaserRateValeur.Text = ParametresGlobaux.EffetPhaserRate.ToString("F1")
            End If
        End If
        If TrackBarPhaserDepth IsNot Nothing Then
            TrackBarPhaserDepth.Value = CInt(ParametresGlobaux.EffetPhaserDepth * 100)
            If LabelPhaserDepthValeur IsNot Nothing Then
                LabelPhaserDepthValeur.Text = $"{TrackBarPhaserDepth.Value}%"
            End If
        End If
        If TrackBarPhaserFeedback IsNot Nothing Then
            TrackBarPhaserFeedback.Value = CInt(ParametresGlobaux.EffetPhaserFeedback * 100)
            If LabelPhaserFeedbackValeur IsNot Nothing Then
                LabelPhaserFeedbackValeur.Text = $"{TrackBarPhaserFeedback.Value}%"
            End If
        End If
        If TrackBarPhaserMix IsNot Nothing Then
            TrackBarPhaserMix.Value = CInt(ParametresGlobaux.EffetPhaserMix * 100)
            If LabelPhaserMixValeur IsNot Nothing Then
                LabelPhaserMixValeur.Text = $"{TrackBarPhaserMix.Value}%"
            End If
        End If
        If ComboBoxPhaserStages IsNot Nothing Then
            Dim stageIndex = ComboBoxPhaserStages.Items.IndexOf(ParametresGlobaux.EffetPhaserStages.ToString())
            If stageIndex >= 0 Then
                ComboBoxPhaserStages.SelectedIndex = stageIndex
            Else
                ComboBoxPhaserStages.SelectedIndex = 1 ' Default "4"
            End If
        End If
    End Sub

    ' Associe l'extension au ProgID AudioPlay dans le registre utilisateur
    Private Sub SetAudioPlayDefault(extension As String, progId As String)
        Try
            ' Enregistrer un ProgID et associer l'extension pour l'utilisateur courant (HKCU)
            Dim exePath = Application.ExecutablePath

            ' 1) Créer/mettre à jour le ProgID (AudioPlay.mp3, etc.)
            Using progKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($"Software\Classes\{progId}")
                progKey.SetValue("", $"AudioPlay file ({extension})")
                Using iconKey = progKey.CreateSubKey("DefaultIcon")
                    iconKey.SetValue("", """" & exePath & """,0")
                End Using
                Using cmdKey = progKey.CreateSubKey("shell\open\command")
                    cmdKey.SetValue("", """" & exePath & """ ""%1""")
                End Using
            End Using

            ' 2) Associer l'extension au ProgID dans HKCU
            Using extKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($"Software\Classes\{extension}")
                extKey.SetValue("", progId)
            End Using

            ' 3) Ajouter le ProgID dans OpenWithProgids pour que l'application apparaisse dans "Ouvrir avec"
            Using ow = Microsoft.Win32.Registry.CurrentUser.CreateSubKey($"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\OpenWithProgids")
                ' Valeur vide de type string
                ow.SetValue(progId, String.Empty, Microsoft.Win32.RegistryValueKind.String)
            End Using

            ' Optionnel: demander à l'explorateur de rafraîchir les associations (silencieux)
            ' On évite d'appeler des API natives ici; Windows peut demander confirmation à l'utilisateur
        Catch ex As Exception
            MessageBox.Show(
                String.Format(LanguageManager.GetString("Error_FileAssociation"), extension, ex.Message),
                LanguageManager.GetString("Error_FileAssociation_Title"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub

    ' Supprime l'association de l'extension AudioPlay dans le registre utilisateur
    Private Sub RemoveAudioPlayDefault(extension As String)
        Try
            ' Retirer la valeur par défaut de l'extension si elle pointe vers un ProgID AudioPlay.*
            Using extKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"Software\Classes\{extension}", True)
                If extKey IsNot Nothing Then
                    Dim current = CStr(If(extKey.GetValue(""), String.Empty))
                    If Not String.IsNullOrEmpty(current) AndAlso current.StartsWith("AudioPlay.", StringComparison.OrdinalIgnoreCase) Then
                        ' Supprimer uniquement la valeur par défaut; laisser la clé si d'autres valeurs existent
                        Try
                            extKey.DeleteValue("", False)
                        Catch
                        End Try
                    End If
                End If
            End Using

            ' Supprimer l'entrée OpenWithProgids
            Try
                Using ow = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\{extension}\OpenWithProgids", True)
                    If ow IsNot Nothing Then
                        Dim valueNames() As String = ow.GetValueNames()
                        For Each nm As String In valueNames
                            If nm.StartsWith("AudioPlay.", StringComparison.OrdinalIgnoreCase) Then
                                Try
                                    ow.DeleteValue(nm, False)
                                Catch
                                End Try
                            End If
                        Next
                    End If
                End Using
            Catch
            End Try

            ' Ne pas supprimer le ProgID globalement par sécurité (peut être utilisé pour plusieurs extensions)
        Catch ex As Exception
            ' Silencieux si la clé n'existe pas
        End Try
    End Sub

    ' Synchronise les cases à cocher audio avec l'état réel du registre Windows
    Private Sub SynchroniserCasesAudioAvecRegistre()
        Try
            ' MP3
            EtatCheckBoxMP3 = VerifierAssociationRegistre(".mp3", "AudioPlay.mp3")
            ' FLAC
            EtatCheckBoxFLAC = VerifierAssociationRegistre(".flac", "AudioPlay.flac")
            ' WMA
            EtatCheckBoxWMA = VerifierAssociationRegistre(".wma", "AudioPlay.wma")
            ' WAV
            EtatCheckBoxWAV = VerifierAssociationRegistre(".wav", "AudioPlay.wav")
            ' AAC
            EtatCheckBoxAAC = VerifierAssociationRegistre(".aac", "AudioPlay.aac")
        Catch
            ' Ignore toute erreur
        End Try
        ' Sauvegarder l'état synchronisé dans AudioDefaut.txt
        AudioDefautManager.SauvegarderAudioDefaut(EtatCheckBoxMP3, EtatCheckBoxFLAC, EtatCheckBoxWMA, EtatCheckBoxWAV, EtatCheckBoxAAC)
    End Sub

    Private Function VerifierAssociationRegistre(extension As String, progIdAttendu As String) As Boolean
        Try
            Using extKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey($"Software\Classes\{extension}")
                If extKey IsNot Nothing Then
                    Dim progId = CStr(If(extKey.GetValue(""), ""))
                    Return progId.Equals(progIdAttendu, StringComparison.OrdinalIgnoreCase)
                End If
            End Using
        Catch
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Compare deux thèmes pour détecter si des modifications ont été effectuées
    ''' </summary>
    Private Function ThemesIdentiques(theme1 As ThemeColors, theme2 As ThemeColors) As Boolean
        If theme1 Is Nothing OrElse theme2 Is Nothing Then Return False

        Return theme1.FormBackColor = theme2.FormBackColor AndAlso
               theme1.ControlBackColor = theme2.ControlBackColor AndAlso
               theme1.ControlForeColor = theme2.ControlForeColor AndAlso
               theme1.ButtonBackColor = theme2.ButtonBackColor AndAlso
               theme1.ButtonForeColor = theme2.ButtonForeColor AndAlso
               theme1.ListViewBackColor = theme2.ListViewBackColor AndAlso
               theme1.ListViewForeColor = theme2.ListViewForeColor AndAlso
               theme1.ListViewHeaderBackColor = theme2.ListViewHeaderBackColor AndAlso
               theme1.ListViewHeaderForeColor = theme2.ListViewHeaderForeColor AndAlso
               theme1.ListViewSelectionBackColor = theme2.ListViewSelectionBackColor AndAlso
               theme1.ListViewSelectionForeColor = theme2.ListViewSelectionForeColor AndAlso
               theme1.TextBoxBackColor = theme2.TextBoxBackColor AndAlso
               theme1.TextBoxForeColor = theme2.TextBoxForeColor AndAlso
               theme1.GroupBoxForeColor = theme2.GroupBoxForeColor AndAlso
               theme1.GroupBoxBorderColor = theme2.GroupBoxBorderColor AndAlso
                               theme1.TrackBarBackColor = theme2.TrackBarBackColor
    End Function

    ' === Gestionnaires d'événements pour les effets audio ===

    Private Sub CheckBoxReverbActif_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxReverbActif.CheckedChanged
        ParametresGlobaux.EffetReverbActif = CheckBoxReverbActif.Checked
        AppliquerEffetsEnTempsReel()
    End Sub

    Private Sub TrackBarReverbMix_Scroll(sender As Object, e As EventArgs) Handles TrackBarReverbMix.Scroll
        If LabelReverbMixValeur IsNot Nothing AndAlso TrackBarReverbMix IsNot Nothing Then
            LabelReverbMixValeur.Text = $"{TrackBarReverbMix.Value}%"
            ParametresGlobaux.EffetReverbMix = TrackBarReverbMix.Value / 100.0F
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub CheckBoxEchoActif_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxEchoActif.CheckedChanged
        ParametresGlobaux.EffetEchoActif = CheckBoxEchoActif.Checked
        AppliquerEffetsEnTempsReel()
    End Sub

    Private Sub TrackBarEchoMix_Scroll(sender As Object, e As EventArgs) Handles TrackBarEchoMix.Scroll
        If LabelEchoMixValeur IsNot Nothing AndAlso TrackBarEchoMix IsNot Nothing Then
            LabelEchoMixValeur.Text = $"{TrackBarEchoMix.Value}%"
            ParametresGlobaux.EffetEchoMix = TrackBarEchoMix.Value / 100.0F
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub TrackBarEchoDelai_Scroll(sender As Object, e As EventArgs) Handles TrackBarEchoDelai.Scroll
        If LabelEchoDelaiValeur IsNot Nothing AndAlso TrackBarEchoDelai IsNot Nothing Then
            ' Convertir la valeur du TrackBar (5-200) en millisecondes réelles (50-2000)
            Dim delaiMs As Integer = TrackBarEchoDelai.Value * 10
            LabelEchoDelaiValeur.Text = $"{delaiMs} ms"
            ParametresGlobaux.EffetEchoDelai = delaiMs
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub TrackBarEchoFeedback_Scroll(sender As Object, e As EventArgs) Handles TrackBarEchoFeedback.Scroll
        If LabelEchoFeedbackValeur IsNot Nothing AndAlso TrackBarEchoFeedback IsNot Nothing Then
            LabelEchoFeedbackValeur.Text = $"{TrackBarEchoFeedback.Value}%"
            ParametresGlobaux.EffetEchoFeedback = TrackBarEchoFeedback.Value / 100.0F
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub CheckBoxTimeStretchActif_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxTimeStretchActif.CheckedChanged
        ParametresGlobaux.EffetTimeStretchActif = CheckBoxTimeStretchActif.Checked
        AppliquerEffetsEnTempsReel()
    End Sub

    Private Sub TrackBarTimeStretch_Scroll(sender As Object, e As EventArgs) Handles TrackBarTimeStretch.Scroll
        If LabelTimeStretchValeur IsNot Nothing AndAlso TrackBarTimeStretch IsNot Nothing Then
            LabelTimeStretchValeur.Text = $"{(TrackBarTimeStretch.Value / 100.0):F2}x"
            ParametresGlobaux.EffetTimeStretchRatio = TrackBarTimeStretch.Value / 100.0F
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    ''' <summary>
    ''' Applique les modifications des effets audio en temps réel sans relancer la chanson
    ''' </summary>
    Private Sub AppliquerEffetsEnTempsReel()
        ' Trouver le Form1 principal et appliquer les effets
        Dim form1 As Form1 = TryCast(Me.Owner, Form1)
        If form1 IsNot Nothing Then
            form1.MettreAJourEffetsAudio()
        End If
    End Sub

    Private Sub ButtonResetEffets_Click(sender As Object, e As EventArgs) Handles ButtonResetEffets.Click
        ' Réinitialiser tous les effets aux valeurs par défaut
        If CheckBoxReverbActif IsNot Nothing Then CheckBoxReverbActif.Checked = False
        If TrackBarReverbMix IsNot Nothing Then
            TrackBarReverbMix.Value = 30
            If LabelReverbMixValeur IsNot Nothing Then LabelReverbMixValeur.Text = "30%"
        End If

        If CheckBoxEchoActif IsNot Nothing Then CheckBoxEchoActif.Checked = False
        If TrackBarEchoMix IsNot Nothing Then
            TrackBarEchoMix.Value = 30
            If LabelEchoMixValeur IsNot Nothing Then LabelEchoMixValeur.Text = "30%"
        End If
        If TrackBarEchoDelai IsNot Nothing Then
            TrackBarEchoDelai.Value = 300
            If LabelEchoDelaiValeur IsNot Nothing Then LabelEchoDelaiValeur.Text = "300 ms"
        End If
        If TrackBarEchoFeedback IsNot Nothing Then
            TrackBarEchoFeedback.Value = 50
            If LabelEchoFeedbackValeur IsNot Nothing Then LabelEchoFeedbackValeur.Text = "50%"
        End If

        If CheckBoxTimeStretchActif IsNot Nothing Then CheckBoxTimeStretchActif.Checked = False
        If TrackBarTimeStretch IsNot Nothing Then
            TrackBarTimeStretch.Value = 100
            If LabelTimeStretchValeur IsNot Nothing Then LabelTimeStretchValeur.Text = "1.00x"
        End If

        If CheckBoxPitchShiftActif IsNot Nothing Then CheckBoxPitchShiftActif.Checked = False
        If TrackBarPitchShift IsNot Nothing Then
            TrackBarPitchShift.Value = 0
            If LabelPitchShiftValeur IsNot Nothing Then LabelPitchShiftValeur.Text = "0.0"
        End If

        If CheckBoxPhaserActif IsNot Nothing Then CheckBoxPhaserActif.Checked = False
        If TrackBarPhaserRate IsNot Nothing Then
            TrackBarPhaserRate.Value = 5
            If LabelPhaserRateValeur IsNot Nothing Then LabelPhaserRateValeur.Text = "0.5"
        End If
        If TrackBarPhaserDepth IsNot Nothing Then
            TrackBarPhaserDepth.Value = 50
            If LabelPhaserDepthValeur IsNot Nothing Then LabelPhaserDepthValeur.Text = "50%"
        End If
        If TrackBarPhaserFeedback IsNot Nothing Then
            TrackBarPhaserFeedback.Value = 30
            If LabelPhaserFeedbackValeur IsNot Nothing Then LabelPhaserFeedbackValeur.Text = "30%"
        End If
        If TrackBarPhaserMix IsNot Nothing Then
            TrackBarPhaserMix.Value = 50
            If LabelPhaserMixValeur IsNot Nothing Then LabelPhaserMixValeur.Text = "50%"
        End If
        If ComboBoxPhaserStages IsNot Nothing Then
            ComboBoxPhaserStages.SelectedIndex = 1 ' "4"
        End If

        ' Appliquer immédiatement
        AppliquerEffetsEnTempsReel()

        MessageBox.Show("Les effets audio ont été réinitialisés.", "AudioPlay", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub ButtonResetTimeStretch_Click(sender As Object, e As EventArgs) Handles ButtonResetTimeStretch.Click
        ' Réinitialiser le Time Stretch à 1.0x
        If TrackBarTimeStretch IsNot Nothing Then
            TrackBarTimeStretch.Value = 100
            If LabelTimeStretchValeur IsNot Nothing Then LabelTimeStretchValeur.Text = "1.00x"
            ParametresGlobaux.EffetTimeStretchRatio = 1.0F
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub LabelEchoDelai_Click(sender As Object, e As EventArgs) Handles LabelEchoDelai.Click

    End Sub

    ' === Pitch Shift ===

    Private Sub CheckBoxPitchShiftActif_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxPitchShiftActif.CheckedChanged
        ParametresGlobaux.EffetPitchShiftActif = CheckBoxPitchShiftActif.Checked
        AppliquerEffetsEnTempsReel()
    End Sub

    Private Sub TrackBarPitchShift_Scroll(sender As Object, e As EventArgs) Handles TrackBarPitchShift.Scroll
        If LabelPitchShiftValeur IsNot Nothing AndAlso TrackBarPitchShift IsNot Nothing Then
            Dim semiTones As Single = TrackBarPitchShift.Value / 10.0F
            LabelPitchShiftValeur.Text = semiTones.ToString("F1")
            ParametresGlobaux.EffetPitchShiftSemiTones = semiTones
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub ButtonResetPitchShift_Click(sender As Object, e As EventArgs) Handles ButtonResetPitchShift.Click
        ' Réinitialiser le Pitch Shift à 0.0
        If TrackBarPitchShift IsNot Nothing Then
            TrackBarPitchShift.Value = 0
            If LabelPitchShiftValeur IsNot Nothing Then LabelPitchShiftValeur.Text = "0.0"
            ParametresGlobaux.EffetPitchShiftSemiTones = 0.0F
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    ' === Phaser ===

    Private Sub CheckBoxPhaserActif_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxPhaserActif.CheckedChanged
        ParametresGlobaux.EffetPhaserActif = CheckBoxPhaserActif.Checked
        AppliquerEffetsEnTempsReel()
    End Sub

    Private Sub TrackBarPhaserRate_Scroll(sender As Object, e As EventArgs) Handles TrackBarPhaserRate.Scroll
        If LabelPhaserRateValeur IsNot Nothing AndAlso TrackBarPhaserRate IsNot Nothing Then
            Dim rate = TrackBarPhaserRate.Value / 10.0F
            LabelPhaserRateValeur.Text = rate.ToString("F1")
            ParametresGlobaux.EffetPhaserRate = rate
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub TrackBarPhaserDepth_Scroll(sender As Object, e As EventArgs) Handles TrackBarPhaserDepth.Scroll
        If LabelPhaserDepthValeur IsNot Nothing AndAlso TrackBarPhaserDepth IsNot Nothing Then
            Dim depth = TrackBarPhaserDepth.Value / 100.0F
            LabelPhaserDepthValeur.Text = (depth * 100.0F).ToString("F0") & "%"
            ParametresGlobaux.EffetPhaserDepth = depth
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub TrackBarPhaserFeedback_Scroll(sender As Object, e As EventArgs) Handles TrackBarPhaserFeedback.Scroll
        If LabelPhaserFeedbackValeur IsNot Nothing AndAlso TrackBarPhaserFeedback IsNot Nothing Then
            Dim feedback = TrackBarPhaserFeedback.Value / 100.0F
            LabelPhaserFeedbackValeur.Text = (feedback * 100.0F).ToString("F0") & "%"
            ParametresGlobaux.EffetPhaserFeedback = feedback
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub TrackBarPhaserMix_Scroll(sender As Object, e As EventArgs) Handles TrackBarPhaserMix.Scroll
        If LabelPhaserMixValeur IsNot Nothing AndAlso TrackBarPhaserMix IsNot Nothing Then
            Dim mix = TrackBarPhaserMix.Value / 100.0F
            LabelPhaserMixValeur.Text = (mix * 100.0F).ToString("F0") & "%"
            ParametresGlobaux.EffetPhaserMix = mix
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub ComboBoxPhaserStages_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxPhaserStages.SelectedIndexChanged
        If ComboBoxPhaserStages.SelectedItem IsNot Nothing Then
            Dim stages = Integer.Parse(ComboBoxPhaserStages.SelectedItem.ToString)
            ParametresGlobaux.EffetPhaserStages = stages
            AppliquerEffetsEnTempsReel()
        End If
    End Sub

    Private Sub ButtonResetPhaser_Click(sender As Object, e As EventArgs) Handles ButtonResetPhaser.Click
        ' Réinitialiser le Phaser aux valeurs par défaut
        If TrackBarPhaserRate IsNot Nothing Then
            TrackBarPhaserRate.Value = 5
            If LabelPhaserRateValeur IsNot Nothing Then LabelPhaserRateValeur.Text = "0.5"
            ParametresGlobaux.EffetPhaserRate = 0.5F
        End If
        If TrackBarPhaserDepth IsNot Nothing Then
            TrackBarPhaserDepth.Value = 50
            If LabelPhaserDepthValeur IsNot Nothing Then LabelPhaserDepthValeur.Text = "50%"
            ParametresGlobaux.EffetPhaserDepth = 0.5F
        End If
        If TrackBarPhaserFeedback IsNot Nothing Then
            TrackBarPhaserFeedback.Value = 30
            If LabelPhaserFeedbackValeur IsNot Nothing Then LabelPhaserFeedbackValeur.Text = "30%"
            ParametresGlobaux.EffetPhaserFeedback = 0.3F
        End If
        If TrackBarPhaserMix IsNot Nothing Then
            TrackBarPhaserMix.Value = 50
            If LabelPhaserMixValeur IsNot Nothing Then LabelPhaserMixValeur.Text = "50%"
            ParametresGlobaux.EffetPhaserMix = 0.5F
        End If
        If ComboBoxPhaserStages IsNot Nothing Then
            ComboBoxPhaserStages.SelectedIndex = 1 ' "4"
            ParametresGlobaux.EffetPhaserStages = 4
        End If
        AppliquerEffetsEnTempsReel()
    End Sub

    Private Sub CheckBoxModeMixeurDJ_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxModeMixeurDJ.CheckedChanged
        ' Mettre à jour immédiatement la variable globale
        ParametresGlobaux.ModeMixeurDJ = CheckBoxModeMixeurDJ.Checked
        System.Diagnostics.Debug.WriteLine($"[CheckBox Changed] ModeMixeurDJ = {ParametresGlobaux.ModeMixeurDJ}")
        ' Le basculement de mode se fait automatiquement lors de la sauvegarde
    End Sub

    ''' <summary>
    ''' Obtient les informations du cache de pochettes
    ''' </summary>
    Public Function ObtenirInfosCache() As String
        Dim tailleCache = CoverCacheManager.ObtenirTailleCache()
        Dim nbFichiers = CoverCacheManager.ObtenirNombreFichiers()
        Return $"{nbFichiers} pochette(s) - {CoverCacheManager.FormaterTaille(tailleCache)}"
    End Function

    ''' <summary>
    ''' Vide le cache de pochettes
    ''' </summary>
    Public Sub ViderCachePochettes()
        Dim result = MessageBox.Show(
            LanguageManager.GetString("Settings_ClearCacheConfirmation", "Voulez-vous vraiment supprimer toutes les pochettes en cache ?"),
            LanguageManager.GetString("Settings_ClearCache", "Vider le cache"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            If CoverCacheManager.ViderCache() Then
                MessageBox.Show(
                    LanguageManager.GetString("Settings_CacheClearedSuccess", "Le cache a été vidé avec succès."),
                    LanguageManager.GetString("Settings_ClearCache", "Vider le cache"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)
            Else
                MessageBox.Show(
                    LanguageManager.GetString("Settings_CacheClearedError", "Erreur lors du vidage du cache."),
                    LanguageManager.GetString("CDSelector_ErrorTitle", "Erreur"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)
            End If
        End If
    End Sub
End Class
