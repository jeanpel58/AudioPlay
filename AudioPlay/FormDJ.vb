Imports System.IO
Imports System.Threading
Imports NAudio.Wave
Imports NAudio.Wave.SampleProviders

Public Class FormDJ
    ' === Variables de lecture pour les deux platines ===
    Private lecteurDeckA As IWavePlayer = Nothing
    Private fichierAudioDeckA As AudioFileReader = Nothing
    Private volumeProviderDeckA As VolumeSampleProvider = Nothing
    Private timeStretchProviderDeckA As TimeStretchSampleProvider = Nothing ' TimeStretch SoundTouch (qualité pro)
    Private meteringProviderDeckA As MeteringSampleProvider = Nothing
    Private phaserProviderDeckA As PhaserSampleProvider = Nothing
    Private reverbProviderDeckA As ReverbSampleProvider = Nothing
    Private echoProviderDeckA As EchoSampleProvider = Nothing

    Private lecteurDeckB As IWavePlayer = Nothing
    Private fichierAudioDeckB As AudioFileReader = Nothing
    Private volumeProviderDeckB As VolumeSampleProvider = Nothing
    Private timeStretchProviderDeckB As TimeStretchSampleProvider = Nothing ' TimeStretch SoundTouch (qualité pro)
    Private meteringProviderDeckB As MeteringSampleProvider = Nothing
    Private phaserProviderDeckB As PhaserSampleProvider = Nothing
    Private reverbProviderDeckB As ReverbSampleProvider = Nothing
    Private echoProviderDeckB As EchoSampleProvider = Nothing

    ' === Variables d'état ===
    Private cheminActuelDeckA As String = ""
    Private cheminActuelDeckB As String = ""
    Private enPauseDeckA As Boolean = False
    Private enPauseDeckB As Boolean = False
    Private lectureEnCoursDeckA As Boolean = False
    Private lectureEnCoursDeckB As Boolean = False

    ' === Position Cue ===
    Private cuePositionDeckA As TimeSpan = TimeSpan.Zero
    Private cuePositionDeckB As TimeSpan = TimeSpan.Zero

    ' === Pitch ===
    Private pitchDeckA As Single = 0.0F ' -8% à +8%
    Private pitchDeckB As Single = 0.0F

    ' === Crossfader ===
    Private crossfaderPosition As Single = 0.5F ' 0.0 (100% A) à 1.0 (100% B)

    ' === Timer pour mise à jour position ===
    Private WithEvents timerPosition As New System.Windows.Forms.Timer()

    ' === BPM ===
    ' Précision Double (3 décimales) comme Virtual DJ / Serato
    Private bpmDeckA As Double = 0.0
    Private bpmDeckB As Double = 0.0
    Private detectionBPMEnCoursDeckA As Boolean = False
    Private detectionBPMEnCoursDeckB As Boolean = False

    ' === BPM CIBLE VERROUILLÉ pour SYNC ===
    ' Quand SYNC est activé, on verrouille le BPM cible exact pour éviter les dérives dues aux arrondis
    Private bpmCibleDeckA As Double = 0.0  ' BPM cible lorsque Deck A est synced vers Deck B
    Private bpmCibleDeckB As Double = 0.0  ' BPM cible lorsque Deck B est synced vers Deck A

    ' === Machine Learning (Essentia) ===
    Private mlResultDeckA As MLAudioAnalyzer.MLAnalysisResult = Nothing
    Private mlResultDeckB As MLAudioAnalyzer.MLAnalysisResult = Nothing
    Private mlInstalle As Boolean = False

    ' === Waveform ===
    Private waveformDeckA As WaveformControl
    Private waveformDeckB As WaveformControl

    ' Timer pour rafraîchir les waveform panels indépendamment (50ms)
    Private WithEvents waveformTimer As New System.Windows.Forms.Timer() With {.Interval = 50}

    ' === HotCues ===
    Private hotcueManagerDeckA As New HotCueManager()
    Private hotcueManagerDeckB As New HotCueManager()
    Private hotcuePanelDeckA As HotCuePanel
    Private hotcuePanelDeckB As HotCuePanel

    ' === Loop ===
    Private loopManagerDeckA As New LoopManager()
    Private loopManagerDeckB As New LoopManager()

    ' === DJ Recording (Multi-format) ===
    Private djRecorder As DJRecorder = Nothing
    Private enregistrementEnCours As Boolean = False
    Private timerEnregistrement As New System.Windows.Forms.Timer()
    Private repertoireEnregistrement As String = ""

    ' === Sampler ===
    Private samplerManager As New SamplerManager()

    ' === Beat Sync Engine (synchronisation continue des beats) ===
    Private beatSyncEngine As BeatSyncEngine = Nothing

    ' === AUTO-CALIBRATION SYNC : Ajustement automatique du ratio basé sur le drift mesuré ===
    Private autoCalibrationActive_DeckA As Boolean = False
    Private autoCalibrationActive_DeckB As Boolean = False
    Private autoCalibTimer As New System.Windows.Forms.Timer() With {.Interval = 3000} ' Mesurer toutes les 3 secondes
    Private lastCalibrationTime As DateTime = DateTime.Now
    Private driftAccumuléDeckA As Double = 0.0
    Private driftAccumuléDeckB As Double = 0.0
    Private calibrationCountDeckA As Integer = 0
    Private calibrationCountDeckB As Integer = 0

    ' === Interactive TrackBar flags ===
    Private isUserDraggingPositionA As Boolean = False
    Private isUserDraggingPositionB As Boolean = False

    ' Flags temporaires pour le drag de waveform : se souvient si la lecture était en cours
    Private dragWasPlayingDeckA As Boolean = False
    Private dragWasPlayingDeckB As Boolean = False
    ' Preview timers used to pause main player after short inactivity during drag
    Private previewTimerDeckA As System.Windows.Forms.Timer = Nothing
    Private previewTimerDeckB As System.Windows.Forms.Timer = Nothing

    ' === Flag de fermeture ===
    Private isClosing As Boolean = False

    ' Hover flags for panels to ensure message filter targets correct panel
    Private isMouseOverPlatineA As Boolean = False
    Private isMouseOverPlatineB As Boolean = False

    Private Sub FormDJ_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialisation de l'interface DJ
        Me.WindowState = FormWindowState.Normal

        ' Initialiser les volumes par défaut (avant chargement)
        TrackBarVolumeDeckA.Value = 75
        TrackBarVolumeDeckB.Value = 75
        TrackBarCrossfader.Value = 50 ' Centre

        ' Initialiser les pitch par défaut
        TrackBarPitchDeckA.Value = 100 ' Centre (0%)
        TrackBarPitchDeckB.Value = 100

        ' Charger les ajustements DJ sauvegardés (écrase les valeurs par défaut si le fichier existe)
        ChargerAjustementsDJ()

        ' Initialiser le timer de position (100ms)
        timerPosition.Interval = 100
        timerPosition.Enabled = True

        ' Rafraîchir la langue
        RefreshLanguage()

        ' Configurer drag & drop des labels de deck
        LabelTrackDeckA.AllowDrop = True
        LabelTrackDeckB.AllowDrop = True
        AddHandler LabelTrackDeckA.DragEnter, AddressOf LabelDeck_DragEnter
        AddHandler LabelTrackDeckA.DragDrop, AddressOf LabelDeckA_DragDrop
        AddHandler LabelTrackDeckB.DragEnter, AddressOf LabelDeck_DragEnter
        AddHandler LabelTrackDeckB.DragDrop, AddressOf LabelDeckB_DragDrop

        ' Configurer drag depuis ListView
        AddHandler ListViewPlaylist.ItemDrag, AddressOf ListViewPlaylist_ItemDrag
        ' Activer la gestion du clavier pour le ListViewPlaylist
        RemoveHandler ListViewPlaylist.KeyDown, AddressOf ListViewPlaylist_KeyDown
        AddHandler ListViewPlaylist.KeyDown, AddressOf ListViewPlaylist_KeyDown

        ' Attacher handlers Click/MouseUp à tous les contrôles existants pour restaurer le focus sur la playlist
        Try
            AttachHandlersToControlsContainer(Me)
        Catch
        End Try

        ' Charger la playlist sauvegardée (en arrière-plan pour réactivité)
        ChargerPlaylistDJ()

        ' Insérer les WaveformControl dans les panels existants
        Try
            waveformDeckA = New WaveformControl()
            ' Faire remplir le WaveformControl au panel
            waveformDeckA.Dock = DockStyle.Fill
            ' Focus management: focus waveform when mouse enters panel or waveform
            AddHandler Panel_Platine_A.MouseEnter, AddressOf Panel_Platine_MouseEnter
            AddHandler Panel_Platine_A.MouseLeave, AddressOf Panel_Platine_MouseLeave
            AddHandler waveformDeckA.MouseEnter, AddressOf Panel_Platine_MouseEnter
            AddHandler waveformDeckA.MouseLeave, AddressOf Panel_Platine_MouseLeave
            ' Initial zoom fixe au démarrage
            waveformDeckA.Zoom = 2.0F
            ' Temporarily disable PositionClicked handler to diagnose waveform disappearance on click
            'AddHandler waveformDeckA.PositionClicked, AddressOf WaveformDeckA_PositionClicked
            AddHandler waveformDeckA.DragStarted, AddressOf WaveformDeckA_DragStarted
            AddHandler waveformDeckA.DragMoved, AddressOf WaveformDeckA_DragMoved
            AddHandler waveformDeckA.DragEnded, AddressOf WaveformDeckA_DragEnded
            ' Désactiver les scrollbars du panel : contrôle rempli par Dock.Fill
            Panel_Platine_A.AutoScroll = False
            Panel_Platine_A.Controls.Add(waveformDeckA)
            ' Positionner et dimensionner correctement le contrôle pour permettre zoom illimité
            Try
                waveformDeckA.Location = New Point(0, 0)
                AddHandler Panel_Platine_A.Resize, Sub(s, ev) waveformDeckA.UpdateLayoutToParent()
                waveformDeckA.UpdateLayoutToParent()
                waveformDeckA.BringToFront()
            Catch
            End Try

            waveformDeckB = New WaveformControl()
            waveformDeckB.Dock = DockStyle.Fill
            AddHandler Panel_Platine_B.MouseEnter, AddressOf Panel_Platine_MouseEnter
            AddHandler Panel_Platine_B.MouseLeave, AddressOf Panel_Platine_MouseLeave
            AddHandler waveformDeckB.MouseEnter, AddressOf Panel_Platine_MouseEnter
            AddHandler waveformDeckB.MouseLeave, AddressOf Panel_Platine_MouseLeave
            waveformDeckB.Zoom = 2.0F
            ' Temporarily disable PositionClicked handler to diagnose waveform disappearance on click
            'AddHandler waveformDeckB.PositionClicked, AddressOf WaveformDeckB_PositionClicked
            AddHandler waveformDeckB.DragStarted, AddressOf WaveformDeckB_DragStarted
            AddHandler waveformDeckB.DragMoved, AddressOf WaveformDeckB_DragMoved
            AddHandler waveformDeckB.DragEnded, AddressOf WaveformDeckB_DragEnded
            Panel_Platine_B.AutoScroll = False
            Panel_Platine_B.Controls.Add(waveformDeckB)
            Try
                waveformDeckB.Location = New Point(0, 0)
                AddHandler Panel_Platine_B.Resize, Sub(s, ev) waveformDeckB.UpdateLayoutToParent()
                waveformDeckB.UpdateLayoutToParent()
                waveformDeckB.BringToFront()
            Catch
            End Try
        Catch
        End Try

        ' Démarrer le timer de rafraîchissement des waveforms
        waveformTimer.Start()

        ' Installer un message filter pour intercepter la roulette souris même si un contrôle n'a pas le focus
        Try
            mwMessageFilterInstance = New MouseWheelMessageFilter(Me)
            Application.AddMessageFilter(mwMessageFilterInstance)
            AddHandler Me.FormClosed, AddressOf FormDJ_FormClosed_RemoveFilter
        Catch
        End Try

        ' Appliquer le thème
        ThemeManager.ApplyThemeToForm(Me)

        ' Localiser l'interface
        RefreshLanguage()

        ' Initialiser le moteur de synchronisation de beats
        beatSyncEngine = New BeatSyncEngine()
        AddHandler beatSyncEngine.TempoDeckAAjuste, AddressOf BeatSync_AjusterTempoDeckA
        AddHandler beatSyncEngine.TempoDeckBAjuste, AddressOf BeatSync_AjusterTempoDeckB
        Debug.WriteLine("BeatSyncEngine initialisé")

        ' Initialiser le timer d'auto-calibration (ajustement automatique du SYNC basé sur drift mesuré)
        AddHandler autoCalibTimer.Tick, AddressOf AutoCalibTimer_Tick
        Debug.WriteLine("Auto-Calibration timer initialisé")

        ' Vérifier si Essentia (ML) est installé
        VerifierInstallationML()

        ' Initialiser les contrôles d'enregistrement
        InitialiserEnregistrement()
        Debug.WriteLine("[INIT] FormDJ_Load terminé")

        ' Initialiser le mode d'affichage waveform (global Deck A + Deck B)
        Try
            If ComboBoxDisplayMode IsNot Nothing AndAlso Not ComboBoxDisplayMode.IsDisposed Then
                RemoveHandler ComboBoxDisplayMode.SelectedIndexChanged, AddressOf ComboBoxDisplayMode_SelectedIndexChanged
                AddHandler ComboBoxDisplayMode.SelectedIndexChanged, AddressOf ComboBoxDisplayMode_SelectedIndexChanged

                If Not ComboBoxDisplayMode.Items.Contains("VirtualDJ") Then
                    ComboBoxDisplayMode.Items.Add("VirtualDJ")
                End If

                ComboBoxDisplayMode.SelectedItem = "VirtualDJ"
                ApplyDisplayModeToAllWaveforms("VirtualDJ")
            End If
        Catch
        End Try

        ' === CONFIGURATION CROSSFADER FOCUS & ROULETTE GLOBALE ===
        ' Donner le focus initial à la playlist
        Try
            If ListViewPlaylist IsNot Nothing AndAlso Not ListViewPlaylist.IsDisposed Then
                ListViewPlaylist.Focus()
            End If
        Catch
        End Try

        ' Note : OnMouseWheel est surchargé pour intercepter globalement la roulette
    End Sub

    Private Sub ComboBoxDisplayMode_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            Dim txt = ComboBoxDisplayMode.SelectedItem?.ToString()
            If String.IsNullOrEmpty(txt) Then Return
            Dim mode = [Enum].Parse(GetType(WaveformControl.DisplayMode), txt)
            ApplyDisplayModeToAll(DirectCast(mode, WaveformControl.DisplayMode))
        Catch
        End Try
    End Sub

    Private Sub ApplyDisplayModeToAll(mode As WaveformControl.DisplayMode)
        Try
            If waveformDeckA IsNot Nothing Then waveformDeckA.DisplayModeSetting = mode
            If waveformDeckB IsNot Nothing Then waveformDeckB.DisplayModeSetting = mode
        Catch
        End Try
    End Sub

    ' Message filter instance used to capture mouse wheel while hovering panels
    Private mwMessageFilterInstance As MouseWheelMessageFilter = Nothing

    Private Sub FormDJ_FormClosed_RemoveFilter(sender As Object, e As FormClosedEventArgs)
        Try
            If mwMessageFilterInstance IsNot Nothing Then
                Application.RemoveMessageFilter(mwMessageFilterInstance)
                mwMessageFilterInstance = Nothing
            End If
        Catch
        End Try
    End Sub

    ' Message filter pour capter WM_MOUSEWHEEL et rediriger vers les platines quand la souris survole sans clic
    Private Class MouseWheelMessageFilter
        Implements IMessageFilter

        Private ReadOnly _owner As FormDJ

        Public Sub New(owner As FormDJ)
            _owner = owner
        End Sub

        Public Function PreFilterMessage(ByRef m As Message) As Boolean Implements IMessageFilter.PreFilterMessage
            Const WM_MOUSEWHEEL As Integer = &H20A
            If m.Msg = WM_MOUSEWHEEL Then
                Try
                    Dim mousePtScreen As Point = Cursor.Position
                    Dim mousePt As Point = _owner.PointToClient(mousePtScreen)
                    System.Diagnostics.Debug.WriteLine($"[MouseWheelFilter] WM_MOUSEWHEEL at {mousePt} ModifierKeys={(Control.ModifierKeys And Keys.Control) = Keys.Control}")
                    If (Control.ModifierKeys And Keys.Control) = Keys.Control Then
                        ' Prefer hovered flags when available to avoid race conditions with Bounds.Contains
                        If (_owner.isMouseOverPlatineA OrElse (_owner.Panel_Platine_A IsNot Nothing AndAlso _owner.Panel_Platine_A.Bounds.Contains(mousePt))) AndAlso _owner.waveformDeckA IsNot Nothing Then
                            ' Extract wheel delta robustly from wParam (high-order word, signed 16-bit)
                            Dim w As Long = m.WParam.ToInt64()
                            Dim deltaRaw As Integer = CInt((w >> 16) And &HFFFF)
                            ' Convert to signed 16-bit
                            If deltaRaw >= &H8000 Then deltaRaw = deltaRaw - &H10000
                            System.Diagnostics.Debug.WriteLine($"[MouseWheelFilter] Over Panel A rawDelta={deltaRaw}")
                            ' Compute steps based on WHEEL_DELTA (120)
                            Dim sign As Integer = Math.Sign(deltaRaw)
                            Dim steps As Integer = If(deltaRaw = 0, 0, Math.Max(1, Math.Abs(deltaRaw) \ 120))
                            If steps = 0 AndAlso deltaRaw <> 0 Then steps = 1
                            If sign <> 0 Then
                                _owner.waveformDeckA.Zoom = Math.Max(0.1F, _owner.waveformDeckA.Zoom + (0.1F * sign * steps))
                                Try
                                    _owner.waveformDeckA.UpdateLayoutToParent()
                                Catch
                                End Try
                            End If
                            Return True
                        ElseIf (_owner.isMouseOverPlatineB OrElse (_owner.Panel_Platine_B IsNot Nothing AndAlso _owner.Panel_Platine_B.Bounds.Contains(mousePt))) AndAlso _owner.waveformDeckB IsNot Nothing Then
                            Dim w As Long = m.WParam.ToInt64()
                            Dim deltaRaw As Integer = CInt((w >> 16) And &HFFFF)
                            If deltaRaw >= &H8000 Then deltaRaw = deltaRaw - &H10000
                            System.Diagnostics.Debug.WriteLine($"[MouseWheelFilter] Over Panel B rawDelta={deltaRaw}")
                            Dim sign As Integer = Math.Sign(deltaRaw)
                            Dim steps As Integer = If(deltaRaw = 0, 0, Math.Max(1, Math.Abs(deltaRaw) \ 120))
                            If steps = 0 AndAlso deltaRaw <> 0 Then steps = 1
                            If sign <> 0 Then
                                _owner.waveformDeckB.Zoom = Math.Max(0.1F, _owner.waveformDeckB.Zoom + (0.1F * sign * steps))
                                Try
                                    _owner.waveformDeckB.UpdateLayoutToParent()
                                Catch
                                End Try
                            End If
                            Return True
                        End If
                    End If
                Catch
                End Try
            End If
            Return False
        End Function
    End Class

    ' Parcourt récursivement les contrôles d'un container et attache les handlers Click/MouseUp
    Private Sub AttachHandlersToControlsContainer(container As Control)
        If container Is Nothing Then Return
        For Each c As Control In container.Controls
            Try
                AddHandler c.Click, AddressOf Control_Click
            Catch
            End Try
            Try
                AddHandler c.MouseUp, AddressOf Control_MouseUp
            Catch
            End Try
            ' Recurse into child containers
            If c.HasChildren Then
                AttachHandlersToControlsContainer(c)
            End If
        Next
    End Sub

    ' Méthodes utilitaires pour la gestion de la playlist DJ

    ''' <summary>
    ''' Intercepter la roulette souris au niveau du Form pour contrôler le crossfader globalement
    ''' </summary>
    Protected Overrides Sub OnMouseWheel(e As MouseEventArgs)
        ' Protection : Ne rien faire si le form est en cours de fermeture
        If isClosing OrElse TrackBarCrossfader Is Nothing OrElse TrackBarCrossfader.IsDisposed Then
            Return
        End If
        ' Si Ctrl appuyé et la souris est sur un des panels platine -> contrôler le Zoom de la waveform
        Try
            Dim mousePt As Point = Me.PointToClient(Cursor.Position)
            Dim handled As Boolean = False
            If (Control.ModifierKeys And Keys.Control) = Keys.Control Then
                If Panel_Platine_A IsNot Nothing AndAlso Panel_Platine_A.Bounds.Contains(mousePt) AndAlso waveformDeckA IsNot Nothing Then
                    If e.Delta > 0 Then
                        waveformDeckA.Zoom = waveformDeckA.Zoom + 0.1F
                    Else
                        waveformDeckA.Zoom = Math.Max(0.1F, waveformDeckA.Zoom - 0.1F)
                    End If
                    waveformDeckA.CenterViewOnCurrentPosition()
                    handled = True
                ElseIf Panel_Platine_B IsNot Nothing AndAlso Panel_Platine_B.Bounds.Contains(mousePt) AndAlso waveformDeckB IsNot Nothing Then
                    If e.Delta > 0 Then
                        waveformDeckB.Zoom = waveformDeckB.Zoom + 0.1F
                    Else
                        waveformDeckB.Zoom = Math.Max(0.1F, waveformDeckB.Zoom - 0.1F)
                    End If
                    waveformDeckB.CenterViewOnCurrentPosition()
                    handled = True
                End If
            End If

            If handled Then
                Return
            End If

            ' Contrôler le crossfader avec la roulette peu importe où se trouve la souris
            Dim nouveauValue As Integer = TrackBarCrossfader.Value

            If e.Delta > 0 Then
                ' Roulette vers le haut : augmenter (vers Deck B)
                nouveauValue += 2
            Else
                ' Roulette vers le bas : diminuer (vers Deck A)
                nouveauValue -= 2
            End If

            ' Limiter entre 0 et 100
            nouveauValue = Math.Max(0, Math.Min(100, nouveauValue))

            ' Appliquer la valeur
            If nouveauValue <> TrackBarCrossfader.Value Then
                TrackBarCrossfader.Value = nouveauValue
                ' L'événement Scroll se déclenchera automatiquement
            End If

        Catch
        End Try

        ' Ne pas appeler la base pour éviter le scroll par défaut
        ' MyBase.OnMouseWheel(e)
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        ' Désactiver les raccourcis si la playlist a le focus pour éviter double traitement
        If ListViewPlaylist IsNot Nothing AndAlso ListViewPlaylist.Focused Then
            Return MyBase.ProcessCmdKey(msg, keyData)
        End If

        ' Gérer raccourcis globaux en mode DJ
        If keyData = Keys.Escape Then
            Return MyBase.ProcessCmdKey(msg, keyData)
        ElseIf keyData = Keys.Space Then
            ' Espace = pause/reprise sur deck actif
            If lectureEnCoursDeckA OrElse enPauseDeckA Then
                ButtonPlayDeckA.PerformClick()
            ElseIf lectureEnCoursDeckB OrElse enPauseDeckB Then
                ButtonPlayDeckB.PerformClick()
            End If
            Return True
        ElseIf keyData = (Keys.Control Or Keys.P) Then
            ButtonPlayDeckA.PerformClick()
            Return True
        ElseIf keyData = (Keys.Control Or Keys.S) Then
            ToggleMuteDJ()
            Return True
        ElseIf keyData = (Keys.Control Or Keys.A) Then
            ' Ctrl+A : charger sélection Deck A
            If ListViewPlaylist.SelectedItems.Count = 1 Then
                ChargerFichierDeckA(ExtraireCheminDepuisItemDJ(ListViewPlaylist.SelectedItems(0)))
            End If
            Return True
        ElseIf keyData = (Keys.Control Or Keys.B) Then
            ' Ctrl+B : charger sélection Deck B
            If ListViewPlaylist.SelectedItems.Count = 1 Then
                ChargerFichierDeckB(ExtraireCheminDepuisItemDJ(ListViewPlaylist.SelectedItems(0)))
            End If
            Return True
        ElseIf keyData = Keys.Delete Then
            SupprimerDeListeDJ()
            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Protected Overrides Sub OnClick(e As EventArgs)
        MyBase.OnClick(e)
        ' Protection : Ne rien faire si le form est en cours de fermeture
        If isClosing OrElse TrackBarCrossfader Is Nothing OrElse TrackBarCrossfader.IsDisposed Then
            Return
        End If

        ' Après chaque clic, redonner le focus à la playlist
        Try
            If ListViewPlaylist IsNot Nothing AndAlso Not ListViewPlaylist.IsDisposed Then
                ListViewPlaylist.Focus()
            End If
        Catch
        End Try
    End Sub

    ' Lorsque la souris entre dans un panel de platine, donner le focus à ce panel.
    Private Sub Panel_Platine_MouseEnter(sender As Object, e As EventArgs)
        Try
            ' If mouse entered the panel itself, focus the waveform control inside so it receives wheel/keyboard input
            If sender Is Panel_Platine_A Then
                If waveformDeckA IsNot Nothing AndAlso Not waveformDeckA.IsDisposed Then waveformDeckA.Focus()
                ' Give focus to the form so it receives MouseWheel even without a click
                TryCast(Me, Form).Activate()
                isMouseOverPlatineA = True
                Return
            ElseIf sender Is Panel_Platine_B Then
                If waveformDeckB IsNot Nothing AndAlso Not waveformDeckB.IsDisposed Then waveformDeckB.Focus()
                TryCast(Me, Form).Activate()
                isMouseOverPlatineB = True
                Return
            End If

            ' If sender is the waveform control, focus it directly
            Dim ctl = TryCast(sender, Control)
            If ctl Is Nothing Then Return
            ctl.Focus()
        Catch
        End Try
    End Sub

    ' Lorsque la souris quitte un panel de platine, redonner le focus à la playlist si la souris n'est plus au-dessus d'aucun des deux.
    Private Sub Panel_Platine_MouseLeave(sender As Object, e As EventArgs)
        Try
            Dim mousePt As Point = Me.PointToClient(Cursor.Position)
            Dim overA As Boolean = (Panel_Platine_A IsNot Nothing AndAlso Panel_Platine_A.Bounds.Contains(mousePt))
            Dim overB As Boolean = (Panel_Platine_B IsNot Nothing AndAlso Panel_Platine_B.Bounds.Contains(mousePt))
            ' Update hovered flags
            If Not overA Then isMouseOverPlatineA = False
            If Not overB Then isMouseOverPlatineB = False
            If Not overA AndAlso Not overB Then
                If ListViewPlaylist IsNot Nothing AndAlso Not ListViewPlaylist.IsDisposed Then
                    ListViewPlaylist.Focus()
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub ListViewPlaylist_KeyDown(sender As Object, e As KeyEventArgs)
        ' Implémentation minimale pour reproduire les raccourcis de Form1
        Try
            ' Ctrl+Flèche haut/bas : déplacer l'item
            If e.Control AndAlso e.KeyCode = Keys.Up Then
                e.Handled = True
                e.SuppressKeyPress = True
                DeplacerSelectionListViewDJ(-1)
                Return
            End If
            If e.Control AndAlso e.KeyCode = Keys.Down Then
                e.Handled = True
                e.SuppressKeyPress = True
                DeplacerSelectionListViewDJ(1)
                Return
            End If

            ' Flèche haut/bas sans Ctrl : changer la sélection
            If Not e.Control AndAlso e.KeyCode = Keys.Up Then
                e.Handled = True
                e.SuppressKeyPress = True
                If ListViewPlaylist.SelectedIndices.Count > 0 Then
                    Dim idx = ListViewPlaylist.SelectedIndices(0)
                    If idx > 0 Then
                        ListViewPlaylist.SelectedItems.Clear()
                        ListViewPlaylist.Items(idx - 1).Selected = True
                        ListViewPlaylist.Items(idx - 1).EnsureVisible()
                    End If
                End If
                Return
            End If
            If Not e.Control AndAlso e.KeyCode = Keys.Down Then
                e.Handled = True
                e.SuppressKeyPress = True
                If ListViewPlaylist.SelectedIndices.Count > 0 Then
                    Dim idx = ListViewPlaylist.SelectedIndices(0)
                    If idx < ListViewPlaylist.Items.Count - 1 Then
                        ListViewPlaylist.SelectedItems.Clear()
                        ListViewPlaylist.Items(idx + 1).Selected = True
                        ListViewPlaylist.Items(idx + 1).EnsureVisible()
                    End If
                End If
                Return
            End If

            ' Supprimer la sélection avec la touche Suppr
            If e.KeyCode = Keys.Delete Then
                e.Handled = True
                e.SuppressKeyPress = True
                SupprimerDeListeDJ()
                Return
            End If

            ' Home / End
            If e.KeyCode = Keys.Home Then
                e.Handled = True
                e.SuppressKeyPress = True
                If ListViewPlaylist.Items.Count > 0 Then
                    ListViewPlaylist.SelectedItems.Clear()
                    ListViewPlaylist.Items(0).Selected = True
                    ListViewPlaylist.Items(0).EnsureVisible()
                End If
                Return
            End If
            If e.KeyCode = Keys.End Then
                e.Handled = True
                e.SuppressKeyPress = True
                If ListViewPlaylist.Items.Count > 0 Then
                    ListViewPlaylist.SelectedItems.Clear()
                    Dim lastIdx As Integer = ListViewPlaylist.Items.Count - 1
                    ListViewPlaylist.Items(lastIdx).Selected = True
                    ListViewPlaylist.Items(lastIdx).EnsureVisible()
                End If
                Return
            End If

            ' Page Up / Page Down
            If e.KeyCode = Keys.PageUp Then
                e.Handled = True
                e.SuppressKeyPress = True
                If ListViewPlaylist.Items.Count > 0 Then
                    Dim visibleCount As Integer = ListViewPlaylist.ClientSize.Height \ ListViewPlaylist.Items(0).Bounds.Height
                    Dim idx As Integer = If(ListViewPlaylist.SelectedIndices.Count > 0, ListViewPlaylist.SelectedIndices(0), 0)
                    Dim newIdx As Integer = Math.Max(0, idx - visibleCount)
                    ListViewPlaylist.SelectedItems.Clear()
                    ListViewPlaylist.Items(newIdx).Selected = True
                    ListViewPlaylist.Items(newIdx).EnsureVisible()
                End If
                Return
            End If
            If e.KeyCode = Keys.PageDown Then
                e.Handled = True
                e.SuppressKeyPress = True
                If ListViewPlaylist.Items.Count > 0 Then
                    Dim visibleCount As Integer = ListViewPlaylist.ClientSize.Height \ ListViewPlaylist.Items(0).Bounds.Height
                    Dim idx As Integer = If(ListViewPlaylist.SelectedIndices.Count > 0, ListViewPlaylist.SelectedIndices(0), 0)
                    Dim newIdx As Integer = Math.Min(ListViewPlaylist.Items.Count - 1, idx + visibleCount)
                    ListViewPlaylist.SelectedItems.Clear()
                    ListViewPlaylist.Items(newIdx).Selected = True
                    ListViewPlaylist.Items(newIdx).EnsureVisible()
                End If
                Return
            End If

            ' Espace : bascule pause/reprise sur la platine en cours, sinon charge dans Deck A
            If e.KeyCode = Keys.Space Then
                e.Handled = True
                e.SuppressKeyPress = True
                ' Priorité Deck A si actif ou en pause
                If lectureEnCoursDeckA OrElse enPauseDeckA Then
                    ButtonPlayDeckA.PerformClick()
                ElseIf lectureEnCoursDeckB OrElse enPauseDeckB Then
                    ButtonPlayDeckB.PerformClick()
                ElseIf ListViewPlaylist.SelectedItems.Count > 0 Then
                    ' Si rien ne joue, charger la sélection dans Deck A
                    ChargerFichierDeckA(ExtraireCheminDepuisItemDJ(ListViewPlaylist.SelectedItems(0)))
                End If
                Return
            End If

            ' Ctrl+P = Pause/Reprise sur Deck A
            If e.Control AndAlso e.KeyCode = Keys.P Then
                e.Handled = True
                e.SuppressKeyPress = True
                ButtonPlayDeckA.PerformClick()
                Return
            End If

            ' Ctrl+A = charger la sélection dans Deck A
            If e.Control AndAlso e.KeyCode = Keys.A Then
                e.Handled = True
                e.SuppressKeyPress = True
                If ListViewPlaylist.SelectedItems.Count = 0 Then
                    MessageBox.Show(LanguageManager.GetString("DJ_Load_NoSelection_DeckA"), LanguageManager.GetString("Info_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                If ListViewPlaylist.SelectedItems.Count > 1 Then
                    MessageBox.Show(LanguageManager.GetString("DJ_Load_MultipleSelection_DeckA"), LanguageManager.GetString("Warning_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                ChargerFichierDeckA(ExtraireCheminDepuisItemDJ(ListViewPlaylist.SelectedItems(0)))
                Return
            End If

            ' Ctrl+B = charger la sélection dans Deck B
            If e.Control AndAlso e.KeyCode = Keys.B Then
                e.Handled = True
                e.SuppressKeyPress = True
                If ListViewPlaylist.SelectedItems.Count = 0 Then
                    MessageBox.Show(LanguageManager.GetString("DJ_Load_NoSelection_DeckB"), LanguageManager.GetString("Info_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Return
                End If
                If ListViewPlaylist.SelectedItems.Count > 1 Then
                    MessageBox.Show(LanguageManager.GetString("DJ_Load_MultipleSelection_DeckB"), LanguageManager.GetString("Warning_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                ChargerFichierDeckB(ExtraireCheminDepuisItemDJ(ListViewPlaylist.SelectedItems(0)))
                Return
            End If

            ' Ctrl+S = Sourdine (Mute) -> mute master si présent
            If e.Control AndAlso e.KeyCode = Keys.S Then
                e.Handled = True
                e.SuppressKeyPress = True
                ToggleMuteDJ()
                Return
            End If

            ' Ctrl+Espace = Arrêter decks
            If e.Control AndAlso e.KeyCode = Keys.Space Then
                e.Handled = True
                e.SuppressKeyPress = True
                ButtonStopAllDecks()
                Return
            End If
        Catch ex As Exception
            Debug.WriteLine("Erreur ListViewPlaylist_KeyDown: " & ex.Message)
        End Try
    End Sub

    ' Basculer le mute au niveau DJ (si Button_Mute présent dans FormDJ) sinon appeler la logique globale
    Private Sub ToggleMuteDJ()
        Try
            If Me.Controls IsNot Nothing Then
                For Each c As Control In Me.Controls
                    If c IsNot Nothing AndAlso c.Name = "Button_Mute" AndAlso TypeOf c Is Button Then
                        DirectCast(c, Button).PerformClick()
                        Return
                    End If
                Next
            End If
        Catch
        End Try

        ' Sinon, appeler la méthode globale si Form1 est ouverte
        Try
            For Each frm As Form In Application.OpenForms
                If TypeOf frm Is Form1 Then
                    Dim f1 As Form1 = DirectCast(frm, Form1)
                    If f1.Button_Mute IsNot Nothing Then
                        f1.Button_Mute.PerformClick()
                    End If
                    Return
                End If
            Next
        Catch
        End Try
    End Sub

    Protected Overrides Sub OnControlAdded(e As ControlEventArgs)
        MyBase.OnControlAdded(e)
        ' Protection : Ne rien faire si le form est en cours de fermeture
        If isClosing Then
            Return
        End If

        ' Attacher le gestionnaire de clic à tous les contrôles ajoutés
        If e.Control IsNot Nothing Then
            AddHandler e.Control.Click, AddressOf Control_Click
            AddHandler e.Control.MouseUp, AddressOf Control_MouseUp
        End If
    End Sub

    Private Sub Control_Click(sender As Object, e As EventArgs)
        ' Protection : Ne rien faire si le form est en cours de fermeture
        If isClosing OrElse ListViewPlaylist Is Nothing OrElse ListViewPlaylist.IsDisposed Then
            Return
        End If

        ' Après chaque clic sur un contrôle, redonner le focus à la playlist
        Try
            Dim ctrl = TryCast(sender, Control)
            If ctrl Is Nothing Then Return
            ' Exclure certains contrôles qui doivent garder le focus (TrackBars, Waveform, etc.)
            If TypeOf ctrl Is TrackBar OrElse ctrl.GetType().Name = "WaveformControl" Then
                Return
            End If
            If ctrl.Name IsNot Nothing Then
                Select Case ctrl.Name
                    Case "TrackBarCrossfader", "TrackBarVolumeDeckA", "TrackBarVolumeDeckB", "TrackBarPitchDeckA", "TrackBarPitchDeckB"
                        Return
                End Select
            End If
            If ctrl IsNot ListViewPlaylist Then
                ListViewPlaylist.Focus()
            End If
        Catch
        End Try
    End Sub

    Private Sub Control_MouseUp(sender As Object, e As MouseEventArgs)
        ' Protection : Ne rien faire si le form est en cours de fermeture
        If isClosing OrElse ListViewPlaylist Is Nothing OrElse ListViewPlaylist.IsDisposed Then
            Return
        End If

        ' Après chaque relâchement de souris, redonner le focus au crossfader
        Try
            If sender IsNot ListViewPlaylist Then
                ListViewPlaylist.Focus()
            End If
        Catch
        End Try
    End Sub

    Protected Overrides Sub OnActivated(e As EventArgs)
        MyBase.OnActivated(e)
        If isClosing Then Return
        Try
            If ListViewPlaylist IsNot Nothing AndAlso Not ListViewPlaylist.IsDisposed Then
                ListViewPlaylist.Focus()
            End If
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Vérifier si Essentia est installé (Machine Learning)
    ''' </summary>
    Private Async Sub VerifierInstallationML()
        Try
            mlInstalle = Await MLAudioAnalyzer.EstInstalle()

            If mlInstalle Then
                Debug.WriteLine("[ML] ✓ Essentia installé - Machine Learning disponible")
            Else
                Debug.WriteLine("[ML] ⚠ Essentia non installé - Analyse ML désactivée")
                Debug.WriteLine("[ML] Pour installer : Exécutez InstallEssentia.bat")
            End If
        Catch ex As Exception
            Debug.WriteLine($"[ML] Erreur vérification Essentia: {ex.Message}")
            mlInstalle = False
        End Try
    End Sub

    Public Sub RefreshLanguage()
        ' Titre de la fenêtre
        Me.Text = LanguageManager.GetString("DJMode_Title")

        ' Label d'avertissement
        Label_Avertissement.Text = LanguageManager.GetString("DJ_Label_InDevelopment")

        ' GroupBox Deck A
        GroupBoxDeckA.Text = LanguageManager.GetString("DJ_DeckATitle")
        If String.IsNullOrEmpty(cheminActuelDeckA) Then
            LabelTrackDeckA.Text = LanguageManager.GetString("DJ_DragTrackHere")
        End If
        ButtonSyncDeckA.Text = LanguageManager.GetString("DJ_Sync_TitleAtoB")

        ' GroupBox Deck B
        GroupBoxDeckB.Text = LanguageManager.GetString("DJ_DeckBTitle")
        If String.IsNullOrEmpty(cheminActuelDeckB) Then
            LabelTrackDeckB.Text = LanguageManager.GetString("DJ_DragTrackHere")
        End If
        ButtonSyncDeckB.Text = LanguageManager.GetString("DJ_Sync_TitleBtoA")

        ' GroupBox Mixeur
        GroupBoxMixeur.Text = LanguageManager.GetString("DJ_MixerTitle")
        ButtonRetourModeSimple.Text = LanguageManager.GetString("DJ_ButtonReturnSimple")
        ButtonParametres.Text = LanguageManager.GetString("DJ_ButtonSettings")
        ButtonQuitter.Text = LanguageManager.GetString("DJ_ButtonQuit")

        ' GroupBox Playlist
        GroupBoxPlaylist.Text = LanguageManager.GetString("DJ_PlaylistTitle")
        ColumnNumDJ.Text = LanguageManager.GetString("DJ_ColumnNumber")
        ColumnChansonDJ.Text = LanguageManager.GetString("DJ_ColumnSong")
        ColumnBPMDJ.Text = LanguageManager.GetString("DJ_ColumnBPM")
        ColumnDureeDJ.Text = LanguageManager.GetString("DJ_ColumnDuration")
        ButtonAjouterPiste.Text = LanguageManager.GetString("DJ_ButtonAddTrack")
        ButtonGererPlaylist.Text = LanguageManager.GetString("DJ_ButtonManagePlaylist")

        ' Effets Deck A
        CheckBoxPhaserDeckA.Text = LanguageManager.GetString("DJ_EffectPhaser")
        CheckBoxReverbDeckA.Text = LanguageManager.GetString("DJ_EffectReverb")
        CheckBoxEchoDeckA.Text = LanguageManager.GetString("DJ_EffectEcho")

        ' Effets Deck B
        CheckBoxPhaserDeckB.Text = LanguageManager.GetString("DJ_EffectPhaser")
        CheckBoxReverbDeckB.Text = LanguageManager.GetString("DJ_EffectReverb")
        CheckBoxEchoDeckB.Text = LanguageManager.GetString("DJ_EffectEcho")

        ' Mettre à jour les labels dynamiques
        UpdateVolumeLabel("A", TrackBarVolumeDeckA.Value)
        UpdateVolumeLabel("B", TrackBarVolumeDeckB.Value)
        UpdatePitchLabel("A", TrackBarPitchDeckA.Value)
        UpdatePitchLabel("B", TrackBarPitchDeckB.Value)
        UpdateCrossfaderLabel(TrackBarCrossfader.Value)
    End Sub

    ' Méthodes helper pour mise à jour des labels
    Private Sub UpdateVolumeLabel(deck As String, value As Integer)
        Dim labelText = String.Format(LanguageManager.GetString("DJ_VolumeLabel"), deck, value)
        If deck = "A" Then
            LabelVolumeDeckA.Text = labelText
        Else
            LabelVolumeDeckB.Text = labelText
        End If
    End Sub

    Private Sub UpdatePitchLabel(deck As String, value As Integer)
        Dim pitchPercent = ((value - 100) / 100.0).ToString("F1")
        Dim labelText = String.Format(LanguageManager.GetString("DJ_PitchLabel"), pitchPercent)
        If deck = "A" Then
            LabelPitchDeckA.Text = labelText
        Else
            LabelPitchDeckB.Text = labelText
        End If
    End Sub

    Private Sub UpdateCrossfaderLabel(value As Integer)
        LabelCrossfader.Text = String.Format(LanguageManager.GetString("DJ_CrossfaderLabel"), value)
    End Sub

    Private Sub ComboBoxDisplayMode_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            Dim modeName As String = "VirtualDJ"
            If ComboBoxDisplayMode IsNot Nothing AndAlso ComboBoxDisplayMode.SelectedItem IsNot Nothing Then
                modeName = ComboBoxDisplayMode.SelectedItem.ToString()
            End If
            ApplyDisplayModeToAllWaveforms(modeName)
        Catch
        End Try
    End Sub

    Private Sub ApplyDisplayModeToAllWaveforms(modeName As String)
        Try
            ApplyDisplayModeToWaveform(waveformDeckA, modeName)
            ApplyDisplayModeToWaveform(waveformDeckB, modeName)
        Catch
        End Try
    End Sub

    Private Sub ApplyDisplayModeToWaveform(target As Control, modeName As String)
        Try
            If target Is Nothing OrElse target.IsDisposed Then Return

            Dim prop = target.GetType().GetProperty("DisplayMode")
            If prop Is Nothing OrElse Not prop.CanWrite Then Return

            If prop.PropertyType IsNot Nothing AndAlso prop.PropertyType.IsEnum Then
                Dim enumValue = [Enum].Parse(prop.PropertyType, modeName, True)
                prop.SetValue(target, enumValue, Nothing)
            Else
                prop.SetValue(target, modeName, Nothing)
            End If

            target.Invalidate()
        Catch
        End Try
    End Sub

    ' === Handlers BeatSync : Ajustement temporaire du tempo (pitch bend) ===
    ' Variables pour stocker les tempo de base
    Private tempoBaseDeckA As Single = 1.0F
    Private tempoBaseDeckB As Single = 1.0F

    Private Sub BeatSync_AjusterTempoDeckA(tempoAjustement As Single)
        Try
            If timeStretchProviderDeckA Is Nothing Then Return

            ' Appliquer le pitch bend (ajustement temporaire)
            ' tempoAjustement est un POURCENTAGE relatif (ex: -0.005 = -0.5%)
            ' tempoBaseDeckA est le RATIO de base (ex: 1.025641 pour 120/117)
            ' Donc on MULTIPLIE : tempoFinal = tempoBase × (1 + ajustement)

            Dim tempoFinal As Single = tempoBaseDeckA * (1.0F + tempoAjustement)

            ' Limiter le tempo final entre 0.9 et 1.1 (±10%)
            tempoFinal = Math.Max(0.9F, Math.Min(1.1F, tempoFinal))

            ' Appliquer le tempo ajusté
            timeStretchProviderDeckA.TempoChange = tempoFinal

            Debug.WriteLine($"BeatSync Deck A: Tempo base={tempoBaseDeckA:F4}, Ajustement={tempoAjustement * 100:F3}%, Final={tempoFinal:F4}")

        Catch ex As Exception
            Debug.WriteLine($"Erreur ajustement tempo Deck A: {ex.Message}")
        End Try
    End Sub


    Private Sub BeatSync_AjusterTempoDeckB(tempoAjustement As Single)
        Try
            If timeStretchProviderDeckB Is Nothing Then Return

            ' Appliquer le pitch bend (ajustement temporaire)
            ' tempoAjustement est un POURCENTAGE relatif (ex: -0.005 = -0.5%)
            ' tempoBaseDeckB est le RATIO de base (ex: 1.025641 pour 120/117)
            ' Donc on MULTIPLIE : tempoFinal = tempoBase × (1 + ajustement)
            Dim tempoFinal As Single = tempoBaseDeckB * (1.0F + tempoAjustement)

            ' Limiter le tempo final entre 0.9 et 1.1 (±10%)
            tempoFinal = Math.Max(0.9F, Math.Min(1.1F, tempoFinal))

            ' Appliquer le tempo ajusté
            timeStretchProviderDeckB.TempoChange = tempoFinal

            Debug.WriteLine($"BeatSync Deck B: Tempo base={tempoBaseDeckB:F4}, Ajustement={tempoAjustement * 100:F3}%, Final={tempoFinal:F4}")

        Catch ex As Exception
            Debug.WriteLine($"Erreur ajustement tempo Deck B: {ex.Message}")
        End Try
    End Sub

    ' === AUTO-CALIBRATION : Ajuster automatiquement le tempo basé sur le drift mesuré ===
    Private Sub AutoCalibTimer_Tick(sender As Object, e As EventArgs)
        Try
            ' Calibrer Deck A si actif
            If autoCalibrationActive_DeckA AndAlso fichierAudioDeckA IsNot Nothing AndAlso fichierAudioDeckB IsNot Nothing Then
                CalibreAutoSyncDeckA()
            End If

            ' Calibrer Deck B si actif
            If autoCalibrationActive_DeckB AndAlso fichierAudioDeckA IsNot Nothing AndAlso fichierAudioDeckB IsNot Nothing Then
                CalibreAutoSyncDeckB()
            End If

        Catch ex As Exception
            Debug.WriteLine($"Erreur auto-calibration: {ex.Message}")
        End Try
    End Sub

    Private Sub CalibreAutoSyncDeckA()
        Try
            ' Créer des grilles de beats temporaires
            Dim bpmEffectifA As Double = bpmCibleDeckA  ' BPM cible verrouillé
            Dim bpmEffectifB As Double = bpmDeckB * (1.0 + pitchDeckB)
            Dim tempBeatGridA As New BeatGrid(bpmEffectifA, fichierAudioDeckA.TotalTime.TotalSeconds)
            Dim tempBeatGridB As New BeatGrid(bpmEffectifB, fichierAudioDeckB.TotalTime.TotalSeconds)

            ' Position actuelle des deux decks
            Dim positionA As Double = fichierAudioDeckA.CurrentTime.TotalSeconds
            Dim positionB As Double = fichierAudioDeckB.CurrentTime.TotalSeconds

            ' Calculer la phase actuelle pour chaque deck (0.0 à 1.0)
            Dim phaseA As Double = tempBeatGridA.CalculerPhase(positionA)
            Dim phaseB As Double = tempBeatGridB.CalculerPhase(positionB)

            ' Calculer le décalage de phase (normaliser entre -0.5 et +0.5)
            Dim phaseDiff As Double = phaseA - phaseB
            If phaseDiff < -0.5 Then phaseDiff += 1.0
            If phaseDiff > 0.5 Then phaseDiff -= 1.0

            ' Convertir en temps (secondes)
            Dim driftSecondes As Double = phaseDiff * tempBeatGridA.BeatDuration

            ' Accumuler le drift
            driftAccumuléDeckA += driftSecondes
            calibrationCountDeckA += 1

            ' Après 3 mesures (9 secondes), ajuster le ratio
            If calibrationCountDeckA >= 3 Then
                Dim driftMoyen As Double = driftAccumuléDeckA / calibrationCountDeckA

                ' Si le drift moyen dépasse 10ms, ajuster le ratio
                If Math.Abs(driftMoyen) > 0.01 Then
                    ' Calculer la correction nécessaire
                    Dim tempsEcoule As Double = 9.0 ' 3 mesures × 3 secondes
                    Dim correctionRatio As Double = driftMoyen / tempsEcoule

                    ' Appliquer la correction
                    tempoBaseDeckA = CSng(tempoBaseDeckA * (1.0 + correctionRatio))

                    ' Limiter entre 0.9 et 1.1
                    tempoBaseDeckA = Math.Max(0.9F, Math.Min(1.1F, tempoBaseDeckA))

                    ' Appliquer le nouveau tempo
                    If timeStretchProviderDeckA IsNot Nothing Then
                        timeStretchProviderDeckA.TempoChange = tempoBaseDeckA
                    End If

                    Debug.WriteLine($"[AUTO-CALIB A] Drift moyen={driftMoyen * 1000:F1}ms, Correction={correctionRatio * 100:F4}%, Nouveau tempo={tempoBaseDeckA:F6}")
                End If

                ' Réinitialiser pour la prochaine mesure
                driftAccumuléDeckA = 0.0
                calibrationCountDeckA = 0
            End If

        Catch ex As Exception
            Debug.WriteLine($"Erreur calibration Deck A: {ex.Message}")
        End Try
    End Sub

    Private Sub CalibreAutoSyncDeckB()
        Try
            ' Créer des grilles de beats temporaires
            Dim bpmEffectifA As Double = bpmDeckA * (1.0 + pitchDeckA)
            Dim bpmEffectifB As Double = bpmCibleDeckB  ' BPM cible verrouillé
            Dim tempBeatGridA As New BeatGrid(bpmEffectifA, fichierAudioDeckA.TotalTime.TotalSeconds)
            Dim tempBeatGridB As New BeatGrid(bpmEffectifB, fichierAudioDeckB.TotalTime.TotalSeconds)

            ' Position actuelle des deux decks
            Dim positionA As Double = fichierAudioDeckA.CurrentTime.TotalSeconds
            Dim positionB As Double = fichierAudioDeckB.CurrentTime.TotalSeconds

            ' Calculer la phase actuelle pour chaque deck (0.0 à 1.0)
            Dim phaseA As Double = tempBeatGridA.CalculerPhase(positionA)
            Dim phaseB As Double = tempBeatGridB.CalculerPhase(positionB)

            ' Calculer le décalage de phase (normaliser entre -0.5 et +0.5)
            Dim phaseDiff As Double = phaseA - phaseB
            If phaseDiff < -0.5 Then phaseDiff += 1.0
            If phaseDiff > 0.5 Then phaseDiff -= 1.0

            ' Convertir en temps (secondes)
            Dim driftSecondes As Double = phaseDiff * tempBeatGridB.BeatDuration

            ' Accumuler le drift
            driftAccumuléDeckB += driftSecondes
            calibrationCountDeckB += 1

            ' Après 3 mesures (9 secondes), ajuster le ratio
            If calibrationCountDeckB >= 3 Then
                Dim driftMoyen As Double = driftAccumuléDeckB / calibrationCountDeckB

                ' Si le drift moyen dépasse 10ms, ajuster le ratio
                If Math.Abs(driftMoyen) > 0.01 Then
                    ' Calculer la correction nécessaire
                    Dim tempsEcoule As Double = 9.0 ' 3 mesures × 3 secondes
                    Dim correctionRatio As Double = driftMoyen / tempsEcoule

                    ' Appliquer la correction
                    tempoBaseDeckB = CSng(tempoBaseDeckB * (1.0 + correctionRatio))

                    ' Limiter entre 0.9 et 1.1
                    tempoBaseDeckB = Math.Max(0.9F, Math.Min(1.1F, tempoBaseDeckB))

                    ' Appliquer le nouveau tempo
                    If timeStretchProviderDeckB IsNot Nothing Then
                        timeStretchProviderDeckB.TempoChange = tempoBaseDeckB
                    End If

                    Debug.WriteLine($"[AUTO-CALIB B] Drift moyen={driftMoyen * 1000:F1}ms, Correction={correctionRatio * 100:F4}%, Nouveau tempo={tempoBaseDeckB:F6}")
                End If

                ' Réinitialiser pour la prochaine mesure
                driftAccumuléDeckB = 0.0
                calibrationCountDeckB = 0
            End If

        Catch ex As Exception
            Debug.WriteLine($"Erreur calibration Deck B: {ex.Message}")
        End Try
    End Sub

    ' === Détection BPM Deck A ===
    Private Async Sub DetecterBPMDeckA()
        If detectionBPMEnCoursDeckA Then Return
        detectionBPMEnCoursDeckA = True

        ' ✅ Afficher "Calcul..." pendant la détection
        LabelBPMDeckA.Text = LanguageManager.GetString("BPM_Status_Calculating")

        Try
            ' ✅ EXÉCUTER LA DÉTECTION COMPLÈTE EN ARRIÈRE-PLAN (thread worker)
            ' Cela empêche de bloquer l'UI pendant l'analyse (qui peut prendre plusieurs secondes)
            Await Task.Run(Async Function()
                               Try
                                   ' Utiliser BPMDetector (Librosa/SoundTouch configurable)
                                   Dim bpm As Double = Await BPMDetector.DetecterBPM(cheminActuelDeckA)

                                   ' Mettre à jour l'UI depuis le thread UI
                                   Me.Invoke(Sub()
                                                 bpmDeckA = CSng(bpm)
                                                 If bpm > 0 Then
                                                     LabelBPMDeckA.Text = String.Format(LanguageManager.GetString("DJ_BPM_Value"), bpm)
                                                 Else
                                                     LabelBPMDeckA.Text = LanguageManager.GetString("DJ_BPM_Unknown")
                                                 End If
                                             End Sub)

                                   If bpm > 0 Then
                                       ' === DÉTECTION DES DOWNBEATS (premiers beats de mesure) ===
                                       Debug.WriteLine($"[DOWNBEAT A] Détection des downbeats pour Deck A...")
                                       Dim downbeatResult As DownbeatDetector.DownbeatResult = Await DownbeatDetector.DetecterDownbeats(cheminActuelDeckA)

                                       If downbeatResult IsNot Nothing Then
                                           Debug.WriteLine($"[DOWNBEAT A] BPM: {downbeatResult.BPM:F3}, Signature: {downbeatResult.TimeSignature}/4")
                                           Debug.WriteLine($"[DOWNBEAT A] Beats: {downbeatResult.Beats.Count}, Downbeats: {downbeatResult.Downbeats.Count}")
                                           Debug.WriteLine($"[DOWNBEAT A] Confiance: {downbeatResult.Confidence:F2}")

                                           ' Stocker les informations de downbeat pour utilisation future dans le SYNC
                                           ' (peut être stocké dans une variable membre si nécessaire)
                                           If downbeatResult.Downbeats.Count > 0 Then
                                               Debug.WriteLine($"[DOWNBEAT A] Premier downbeat à {downbeatResult.Downbeats(0):F3}s")
                                               Debug.WriteLine($"[DOWNBEAT A] Détection réussie ✓ - Prêt pour SYNC phrase-aware")
                                           End If
                                           ' Use the first reliable beat as anchor and generate a strict BPM grid
                                           Try
                                               Dim firstBeat As Double = downbeatResult.Beats(0)
                                               Dim bpmUsed As Double = downbeatResult.BPM
                                               If bpmUsed > 0 Then
                                                   Dim interval As Double = 60.0 / bpmUsed
                                                   Dim totalSec As Double = fichierAudioDeckA.TotalTime.TotalSeconds
                                                   Dim centers As New System.Collections.Generic.List(Of Single)()
                                                   Dim t As Double = firstBeat
                                                   While t < totalSec
                                                       centers.Add(CSng(t / totalSec))
                                                       t += interval
                                                   End While
                                                   ' Ne plus injecter les marqueurs d'onsets dans WaveformControl (évite rectangles rouges couvrants)
                                                   ' If centers.Count > 0 AndAlso waveformDeckA IsNot Nothing AndAlso waveformDeckA.IsHandleCreated Then
                                                   '     Dim arr() As Single = centers.ToArray()
                                                   '     waveformDeckA.BeginInvoke(New MethodInvoker(Sub()
                                                   '                                              waveformDeckA.WaveformColor = Color.DodgerBlue
                                                   '                                              waveformDeckA.SetOnsetMarkers(arr)
                                                   '                                          End Sub))
                                                   ' End If
                                               End If
                                           Catch
                                           End Try
                                       Else
                                           Debug.WriteLine($"[DOWNBEAT A] Détection impossible - Utilisation des beats réguliers")
                                       End If

                                       ' === ANALYSE MACHINE LEARNING (si Essentia installé) ===
                                       If mlInstalle Then
                                           Debug.WriteLine($"[ML A] Analyse Machine Learning Deck A...")
                                           Dim mlResult = Await MLAudioAnalyzer.AnalyserAvecML(cheminActuelDeckA)

                                           If mlResult IsNot Nothing Then
                                               ' Stocker et afficher les résultats ML
                                               Me.Invoke(Sub()
                                                             mlResultDeckA = mlResult
                                                             AfficherResultatsML_DeckA(mlResultDeckA)
                                                             VerifierCompatibiliteHarmonique()
                                                         End Sub)

                                               Debug.WriteLine($"[ML A] ✓ Key: {mlResult.CamelotCode} ({mlResult.Key} {mlResult.Scale})")
                                               Debug.WriteLine($"[ML A] ✓ Genre: {mlResult.Genre}, Danceability: {mlResult.Danceability:F2}")
                                               Debug.WriteLine($"[ML A] ✓ Energy: {mlResult.Energy:F2}, Valence: {mlResult.Valence:F2}")
                                           Else
                                               Debug.WriteLine($"[ML A] ⚠ Analyse ML échouée")
                                           End If
                                       End If
                                   End If

                               Catch ex As Exception
                                   Me.Invoke(Sub()
                                                 LabelBPMDeckA.Text = LanguageManager.GetString("DJ_BPM_Unknown")
                                             End Sub)
                                   Debug.WriteLine($"Erreur détection BPM Deck A: {ex.Message}")
                               End Try
                           End Function)

        Catch ex As Exception
            LabelBPMDeckA.Text = LanguageManager.GetString("DJ_BPM_Unknown")
            Debug.WriteLine($"Erreur détection BPM Deck A (outer): {ex.Message}")
        Finally
            detectionBPMEnCoursDeckA = False
        End Try
    End Sub

    ' === Détection BPM Deck B ===
    Private Async Sub DetecterBPMDeckB()
        If detectionBPMEnCoursDeckB Then Return
        detectionBPMEnCoursDeckB = True

        ' ✅ Afficher "Calcul..." pendant la détection
        LabelBPMDeckB.Text = LanguageManager.GetString("BPM_Status_Calculating")

        Try
            ' ✅ EXÉCUTER LA DÉTECTION COMPLÈTE EN ARRIÈRE-PLAN (thread worker)
            ' Cela empêche de bloquer l'UI pendant l'analyse (qui peut prendre plusieurs secondes)
            Await Task.Run(Async Function()
                               Try
                                   ' Utiliser BPMDetector (Librosa/SoundTouch configurable)
                                   Dim bpm As Double = Await BPMDetector.DetecterBPM(cheminActuelDeckB)

                                   ' Mettre à jour l'UI depuis le thread UI
                                   Me.Invoke(Sub()
                                                 bpmDeckB = CSng(bpm)
                                                 If bpm > 0 Then
                                                     LabelBPMDeckB.Text = String.Format(LanguageManager.GetString("DJ_BPM_Value"), bpm)
                                                 Else
                                                     LabelBPMDeckB.Text = LanguageManager.GetString("DJ_BPM_Unknown")
                                                 End If
                                             End Sub)

                                   If bpm > 0 Then
                                       ' === DÉTECTION DES DOWNBEATS (premiers beats de mesure) ===
                                       Debug.WriteLine($"[DOWNBEAT B] Détection des downbeats pour Deck B...")
                                       Dim downbeatResult As DownbeatDetector.DownbeatResult = Await DownbeatDetector.DetecterDownbeats(cheminActuelDeckB)

                                       If downbeatResult IsNot Nothing Then
                                           Debug.WriteLine($"[DOWNBEAT B] BPM: {downbeatResult.BPM:F3}, Signature: {downbeatResult.TimeSignature}/4")
                                           Debug.WriteLine($"[DOWNBEAT B] Beats: {downbeatResult.Beats.Count}, Downbeats: {downbeatResult.Downbeats.Count}")
                                           Debug.WriteLine($"[DOWNBEAT B] Confiance: {downbeatResult.Confidence:F2}")

                                           ' Stocker les informations de downbeat pour utilisation future dans le SYNC
                                           ' (peut être stocké dans une variable membre si nécessaire)
                                           If downbeatResult.Downbeats.Count > 0 Then
                                               Debug.WriteLine($"[DOWNBEAT B] Premier downbeat à {downbeatResult.Downbeats(0):F3}s")
                                               Debug.WriteLine($"[DOWNBEAT B] Détection réussie ✓ - Prêt pour SYNC phrase-aware")
                                           End If
                                       Else
                                           Debug.WriteLine($"[DOWNBEAT B] Détection impossible - Utilisation des beats réguliers")
                                       End If

                                       ' === ANALYSE MACHINE LEARNING (si Essentia installé) ===
                                       If mlInstalle Then
                                           Debug.WriteLine($"[ML B] Analyse Machine Learning Deck B...")
                                           Dim mlResult = Await MLAudioAnalyzer.AnalyserAvecML(cheminActuelDeckB)

                                           If mlResult IsNot Nothing Then
                                               ' Stocker et afficher les résultats ML
                                               Me.Invoke(Sub()
                                                             mlResultDeckB = mlResult
                                                             AfficherResultatsML_DeckB(mlResultDeckB)
                                                             VerifierCompatibiliteHarmonique()
                                                         End Sub)

                                               Debug.WriteLine($"[ML B] ✓ Key: {mlResult.CamelotCode} ({mlResult.Key} {mlResult.Scale})")
                                               Debug.WriteLine($"[ML B] ✓ Genre: {mlResult.Genre}, Danceability: {mlResult.Danceability:F2}")
                                               Debug.WriteLine($"[ML B] ✓ Energy: {mlResult.Energy:F2}, Valence: {mlResult.Valence:F2}")
                                           Else
                                               Debug.WriteLine($"[ML B] ⚠ Analyse ML échouée")
                                           End If
                                       End If
                                   End If

                               Catch ex As Exception
                                   Me.Invoke(Sub()
                                                 LabelBPMDeckB.Text = LanguageManager.GetString("DJ_BPM_Unknown")
                                             End Sub)
                                   Debug.WriteLine($"Erreur détection BPM Deck B: {ex.Message}")
                               End Try
                           End Function)

        Catch ex As Exception
            LabelBPMDeckB.Text = LanguageManager.GetString("DJ_BPM_Unknown")
            Debug.WriteLine($"Erreur détection BPM Deck B (outer): {ex.Message}")
        Finally
            detectionBPMEnCoursDeckB = False
        End Try
    End Sub

    ' === Détection BPM (analyse basique par enveloppe) ===

    ' === Bouton SYNC Deck A : synchroniser BPM de A vers B (style Virtual DJ / Serato) ===
    Private Sub ButtonSyncDeckA_Click(sender As Object, e As EventArgs) Handles ButtonSyncDeckA.Click
        If bpmDeckA = 0.0 OrElse bpmDeckB = 0.0 Then
            MessageBox.Show(LanguageManager.GetString("DJ_BPM_NotDetected"), LanguageManager.GetString("DJ_Sync_TitleAtoB"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If fichierAudioDeckA Is Nothing OrElse fichierAudioDeckB Is Nothing Then
            MessageBox.Show(LanguageManager.GetString("DJ_Error_NoTrackLoaded"), LanguageManager.GetString("DJ_Sync_TitleAtoB"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Debug.WriteLine("═══════════════════════════════════════════════════")
        Debug.WriteLine($"[SYNC A→B] DÉBUT - Style Virtual DJ / Serato")
        Debug.WriteLine($"[SYNC A→B] BPM Deck A: {bpmDeckA:F3}, BPM Deck B: {bpmDeckB:F3}")

        ' === ÉTAPE 1 : SYNCHRONISER LE TEMPO (BPM) AVEC PRÉCISION MAXIMALE ===
        ' VÉRIFIER si c'est un premier SYNC ou un re-SYNC
        Dim estReSyncDeckA As Boolean = (bpmCibleDeckA > 0.0 AndAlso autoCalibrationActive_DeckA)
        Dim ratio As Double = 1.0  ' Déclarer en dehors du If pour être accessible partout

        If Not estReSyncDeckA Then
            ' === PREMIER SYNC : Calculer et appliquer le ratio ===
            Debug.WriteLine($"[SYNC A→B] Premier SYNC détecté - Calcul du ratio")

            ' CORRECTION CRITIQUE : Verrouiller le BPM cible EXACT pour éviter les dérives dues aux arrondis
            bpmCibleDeckA = bpmDeckB  ' Verrouiller exactement le BPM de référence

            ' Calculer le ratio EXACT pour atteindre ce BPM cible
            ratio = bpmCibleDeckA / bpmDeckA

            ' Stocker le pitch EXACT (pas arrondi par le TrackBar !)
            pitchDeckA = CSng(ratio - 1.0)

            ' Pour l'affichage du TrackBar, on peut arrondir (visuel seulement)
            Dim pitchPourAffichage As Double = (ratio - 1.0) * 100.0
            pitchPourAffichage = Math.Max(-8.0, Math.Min(8.0, pitchPourAffichage))
            TrackBarPitchDeckA.Value = 100 + CInt(pitchPourAffichage)

            ' Mettre à jour le label avec la valeur EXACTE
            LabelPitchDeckA.Text = String.Format(LanguageManager.GetString("DJ_Pitch_Value"), pitchDeckA)

            ' Calculer le BPM ajusté EXACT
            Dim bpmAjuste As Double = bpmDeckA * ratio
            LabelBPMDeckA.Text = String.Format(LanguageManager.GetString("DJ_BPM_Value"), bpmAjuste)

            ' Appliquer le time stretch avec le RATIO EXACT
            If timeStretchProviderDeckA IsNot Nothing Then
                timeStretchProviderDeckA.TempoChange = CSng(ratio)
                tempoBaseDeckA = CSng(ratio)  ' Sauvegarder pour re-SYNC
            End If

            Debug.WriteLine($"[SYNC A→B] ÉTAPE 1: Tempo ajusté avec PRÉCISION MAXIMALE")
            Debug.WriteLine($"[SYNC A→B]   Ratio exact={ratio:F15}")
            Debug.WriteLine($"[SYNC A→B]   Pitch exact={pitchDeckA:F15}")
            Debug.WriteLine($"[SYNC A→B]   BPM cible verrouillé={bpmCibleDeckA:F3}")
            Debug.WriteLine($"[SYNC A→B]   BPM ajusté={bpmAjuste:F3}")
        Else
            ' === RE-SYNC : GARDER le tempo actuel, juste réaligner la position ===
            Debug.WriteLine($"[SYNC A→B] RE-SYNC détecté - Conservation du tempo actuel")
            Debug.WriteLine($"[SYNC A→B]   BPM cible verrouillé (conservé)={bpmCibleDeckA:F3}")
            Debug.WriteLine($"[SYNC A→B]   Tempo actuel (conservé)={tempoBaseDeckA:F6}")
            Debug.WriteLine($"[SYNC A→B]   ⚠️ PAS de recalcul du ratio - on garde la calibration en cours")

            ' NE PAS toucher au tempo, l'auto-calibration a peut-être déjà ajusté !
        End If

        ' === ÉTAPE 2 : BEAT SNAP INSTANTANÉ (comme Virtual DJ / Serato) ===
        ' Créer des grilles de beats temporaires pour l'alignement
        ' CORRECTION CRITIQUE : Utiliser le BPM CIBLE VERROUILLÉ pour Deck A
        Dim bpmEffectifA As Double = bpmCibleDeckA  ' ⭐ Utiliser le BPM cible exact verrouillé
        Dim bpmEffectifB As Double = bpmDeckB * (1.0 + pitchDeckB)
        Dim tempBeatGridA As New BeatGrid(bpmEffectifA, fichierAudioDeckA.TotalTime.TotalSeconds)
        Dim tempBeatGridB As New BeatGrid(bpmEffectifB, fichierAudioDeckB.TotalTime.TotalSeconds)

        ' Position actuelle des deux decks
        Dim positionA As Double = fichierAudioDeckA.CurrentTime.TotalSeconds
        Dim positionB As Double = fichierAudioDeckB.CurrentTime.TotalSeconds

        ' Calculer la phase actuelle pour chaque deck (0.0 à 1.0)
        Dim phaseA As Double = tempBeatGridA.CalculerPhase(positionA)
        Dim phaseB As Double = tempBeatGridB.CalculerPhase(positionB)

        ' === SNAP SIMPLE : Aligner Deck A sur son beat le plus proche MAINTENANT ===
        ' Ne faire le snap que si les BPM effectifs des deux decks sont suffisamment proches
        Dim epsRel As Double = 0.01 ' tolérance relative 1%
        Dim bpmDiffRel As Double = 1.0
        If bpmEffectifA > 0 AndAlso bpmEffectifB > 0 Then
            bpmDiffRel = Math.Abs(bpmEffectifA - bpmEffectifB) / Math.Max(1.0, Math.Max(bpmEffectifA, bpmEffectifB))
        End If
        If bpmDiffRel <= epsRel Then
            ' Trouver le beat le plus proche de Deck A (celui qui est le plus proche de sa position actuelle)
            Dim beatLePlusProcheA As Double = tempBeatGridA.TrouverBeatLePlusProche(positionA)

            ' Calculer à quelle distance on est de ce beat
            Dim distanceAuBeat As Double = Math.Abs(positionA - beatLePlusProcheA)

            Debug.WriteLine($"[SYNC A→B] Position A actuelle : {positionA:F3}s")
            Debug.WriteLine($"[SYNC A→B] Beat le plus proche : {beatLePlusProcheA:F3}s")
            Debug.WriteLine($"[SYNC A→B] Distance au beat : {distanceAuBeat * 1000:F1}ms")

            ' SNAP INSTANTANÉ au beat le plus proche
            Dim anciennePositionA As Double = positionA
            fichierAudioDeckA.CurrentTime = TimeSpan.FromSeconds(beatLePlusProcheA)
            TrackBarPositionDeckA.Value = CInt(beatLePlusProcheA)
            LabelDureeDeckA.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"),
                                                 fichierAudioDeckA.CurrentTime,
                                                 fichierAudioDeckA.TotalTime)

            Dim sautTemps As Double = beatLePlusProcheA - anciennePositionA
            Debug.WriteLine($"[SYNC A→B] ÉTAPE 2: BEAT SNAP ⚡ - Saut de {sautTemps:F3}s")
            Debug.WriteLine($"[SYNC A→B] Deck A: {anciennePositionA:F3}s → {beatLePlusProcheA:F3}s (beat le plus proche)")
        Else
            Debug.WriteLine($"[SYNC A→B] SKIP SNAP : BPM mismatch (A={bpmEffectifA:F3}, B={bpmEffectifB:F3}), diffRel={bpmDiffRel:F3} > {epsRel:F3}")
        End If


        ' === ÉTAPE 3 : TEMPO LOCK - PAS de corrections automatiques ===
        ' Le tempo reste FIXE après SYNC, seul re-clic SYNC réaligne la position
        If beatSyncEngine IsNot Nothing Then
            ' Stocker le tempo de base (ratio exact) pour Deck A
            tempoBaseDeckA = CSng(ratio)

            ' CORRECTION CRITIQUE : Utiliser le BPM CIBLE VERROUILLÉ pour éviter les dérives !
            Dim bpmReel_A As Double = bpmCibleDeckA  ' ⭐ Toujours utiliser le BPM cible verrouillé
            Dim bpmReel_B As Double = bpmDeckB * (1.0 + pitchDeckB)

            ' Vérifier si c'est un premier SYNC ou un re-SYNC
            If beatSyncEngine.SyncActifDeckA Then
                ' Re-SYNC : Mettre à jour seulement les BeatGrids avec le BPM CIBLE
                beatSyncEngine.ResynchoniserBeatGrids(
                    bpmCibleDeckA, fichierAudioDeckA.TotalTime.TotalSeconds,  ' ⭐ BPM cible verrouillé
                    bpmReel_B, fichierAudioDeckB.TotalTime.TotalSeconds
                )
                Debug.WriteLine($"[SYNC A→B] ÉTAPE 3: RE-SYNC - BeatGrids mis à jour avec BPM cible verrouillé (A={bpmCibleDeckA:F3}, B={bpmReel_B:F3}) ✅")
            Else
                ' Premier SYNC : Initialiser complètement
                beatSyncEngine.InitialiserBeatGrids(
                    bpmCibleDeckA, fichierAudioDeckA.TotalTime.TotalSeconds,  ' ⭐ BPM cible verrouillé
                    bpmReel_B, fichierAudioDeckB.TotalTime.TotalSeconds,
                    fichierAudioDeckA, fichierAudioDeckB
                )
                Debug.WriteLine($"[SYNC A→B] ÉTAPE 3: BEAT LOCK initialisé avec BPM cible verrouillé (A={bpmCibleDeckA:F3}, B={bpmReel_B:F3}) ✅")

                ' ⚠️ DÉSACTIVÉ : Pas de corrections automatiques continues !
                ' Le tempo reste FIXE. Re-cliquer SYNC réaligne seulement la position.
                ' beatSyncEngine.SyncActifDeckA = True
                Debug.WriteLine($"[SYNC A→B] ÉTAPE 3: TEMPO LOCK activé (tempo fixe = {tempoBaseDeckA:F6}, pas de corrections auto)")
            End If
        End If

        ' === ÉTAPE 4 : ACTIVER L'AUTO-CALIBRATION (ajustement automatique basé sur drift mesuré) ===
        If Not autoCalibrationActive_DeckA Then
            autoCalibrationActive_DeckA = True
            driftAccumuléDeckA = 0.0
            calibrationCountDeckA = 0
            If Not autoCalibTimer.Enabled Then
                autoCalibTimer.Start()
            End If
            Debug.WriteLine($"[SYNC A→B] ÉTAPE 4: AUTO-CALIBRATION activée (mesure drift toutes les 3s pour ajuster le ratio)")
        End If

        ' Sauvegarder les ajustements
        SauvegarderAjustementsDJ()

        Debug.WriteLine($"[SYNC A→B] FIN - Beats alignés instantanément + auto-calibration active ⚡")
        Debug.WriteLine("═══════════════════════════════════════════════════")
    End Sub

    ' === Bouton SYNC Deck B : synchroniser BPM de B vers A (style Virtual DJ / Serato) ===
    Private Sub ButtonSyncDeckB_Click(sender As Object, e As EventArgs) Handles ButtonSyncDeckB.Click
        If bpmDeckA = 0.0 OrElse bpmDeckB = 0.0 Then
            MessageBox.Show(LanguageManager.GetString("DJ_BPM_NotDetected"), LanguageManager.GetString("DJ_Sync_TitleBtoA"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        If fichierAudioDeckA Is Nothing OrElse fichierAudioDeckB Is Nothing Then
            MessageBox.Show(LanguageManager.GetString("DJ_Error_NoTrackLoaded"), LanguageManager.GetString("DJ_Sync_TitleBtoA"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Debug.WriteLine("═══════════════════════════════════════════════════")
        Debug.WriteLine($"[SYNC B→A] DÉBUT - Style Virtual DJ / Serato")
        Debug.WriteLine($"[SYNC B→A] BPM Deck A: {bpmDeckA:F3}, BPM Deck B: {bpmDeckB:F3}")

        ' === ÉTAPE 1 : SYNCHRONISER LE TEMPO (BPM) AVEC PRÉCISION MAXIMALE ===
        ' VÉRIFIER si c'est un premier SYNC ou un re-SYNC
        Dim estReSyncDeckB As Boolean = (bpmCibleDeckB > 0.0 AndAlso autoCalibrationActive_DeckB)
        Dim ratio As Double = 1.0  ' Déclarer en dehors du If pour être accessible partout

        If Not estReSyncDeckB Then
            ' === PREMIER SYNC : Calculer et appliquer le ratio ===
            Debug.WriteLine($"[SYNC B→A] Premier SYNC détecté - Calcul du ratio")

            ' CORRECTION CRITIQUE : Verrouiller le BPM cible EXACT pour éviter les dérives dues aux arrondis
            bpmCibleDeckB = bpmDeckA  ' Verrouiller exactement le BPM de référence (ex: 120.000)

            ' Calculer le ratio EXACT pour atteindre ce BPM cible
            ratio = bpmCibleDeckB / bpmDeckB  ' Ex: 120.0 / 117.0 = 1.025641025641026

            ' Stocker le pitch EXACT (pas arrondi par le TrackBar !)
            ' IMPORTANT : On ne passe PAS par le TrackBar qui arrondit, on calcule directement
            pitchDeckB = CSng(ratio - 1.0)  ' Ex: 0.025641025641026 (2.5641%)

            ' Pour l'affichage du TrackBar, on peut arrondir (visuel seulement)
            Dim pitchPourAffichage As Double = (ratio - 1.0) * 100.0  ' En pourcentage pour affichage
            pitchPourAffichage = Math.Max(-8.0, Math.Min(8.0, pitchPourAffichage))  ' Limiter à ±8%
            TrackBarPitchDeckB.Value = 100 + CInt(pitchPourAffichage)

            ' Mettre à jour le label avec la valeur EXACTE (pas l'arrondie)
            LabelPitchDeckB.Text = String.Format(LanguageManager.GetString("DJ_Pitch_Value"), pitchDeckB)

            ' Calculer le BPM ajusté EXACT (doit être exactement = bpmCibleDeckB)
            Dim bpmAjuste As Double = bpmDeckB * ratio  ' = bpmCibleDeckB par définition
            LabelBPMDeckB.Text = String.Format(LanguageManager.GetString("DJ_BPM_Value"), bpmAjuste)

            ' Appliquer le time stretch avec le RATIO EXACT (pas via pitchDeckB qui peut avoir perdu de la précision)
            If timeStretchProviderDeckB IsNot Nothing Then
                timeStretchProviderDeckB.TempoChange = CSng(ratio)  ' Utiliser le ratio directement
                tempoBaseDeckB = CSng(ratio)  ' Sauvegarder pour re-SYNC
            End If

            Debug.WriteLine($"[SYNC B→A] ÉTAPE 1: Tempo ajusté avec PRÉCISION MAXIMALE")
            Debug.WriteLine($"[SYNC B→A]   Ratio exact={ratio:F15}")
            Debug.WriteLine($"[SYNC B→A]   Pitch exact={pitchDeckB:F15}")
            Debug.WriteLine($"[SYNC B→A]   BPM cible verrouillé={bpmCibleDeckB:F3}")
            Debug.WriteLine($"[SYNC B→A]   BPM ajusté={bpmAjuste:F3} (doit être = {bpmCibleDeckB:F3})")
            Debug.WriteLine($"[SYNC B→A]   TrackBar affichage={TrackBarPitchDeckB.Value} (arrondi OK pour UI)")
        Else
            ' === RE-SYNC : GARDER le tempo actuel, juste réaligner la position ===
            Debug.WriteLine($"[SYNC B→A] RE-SYNC détecté - Conservation du tempo actuel")
            Debug.WriteLine($"[SYNC B→A]   BPM cible verrouillé (conservé)={bpmCibleDeckB:F3}")
            Debug.WriteLine($"[SYNC B→A]   Tempo actuel (conservé)={tempoBaseDeckB:F6}")
            Debug.WriteLine($"[SYNC B→A]   ⚠️ PAS de recalcul du ratio - on garde la calibration en cours")

            ' NE PAS toucher au tempo, l'auto-calibration a peut-être déjà ajusté !
        End If

        ' === ÉTAPE 2 : BEAT SNAP INSTANTANÉ (comme Virtual DJ / Serato) ===
        ' Créer des grilles de beats temporaires pour l'alignement
        ' CORRECTION CRITIQUE : Utiliser le BPM CIBLE VERROUILLÉ pour Deck B (pas bpmAjuste qui peut dériver)
        Dim bpmEffectifA As Double = bpmDeckA * (1.0 + pitchDeckA)
        Dim bpmEffectifB As Double = bpmCibleDeckB  ' ⭐ Utiliser le BPM cible exact verrouillé
        Dim tempBeatGridA As New BeatGrid(bpmEffectifA, fichierAudioDeckA.TotalTime.TotalSeconds)
        Dim tempBeatGridB As New BeatGrid(bpmEffectifB, fichierAudioDeckB.TotalTime.TotalSeconds)

        Debug.WriteLine($"[SYNC B→A] BPM effectifs utilisés : A={bpmEffectifA:F3}, B={bpmEffectifB:F3}")
        Debug.WriteLine($"[SYNC B→A] BeatDuration : A={tempBeatGridA.BeatDuration:F6}s, B={tempBeatGridB.BeatDuration:F6}s")

        ' Position actuelle des deux decks
        Dim positionA As Double = fichierAudioDeckA.CurrentTime.TotalSeconds
        Dim positionB As Double = fichierAudioDeckB.CurrentTime.TotalSeconds

        Debug.WriteLine($"[SYNC B→A] Positions avant snap : A={positionA:F3}s, B={positionB:F3}s")

        ' Calculer la phase actuelle pour chaque deck (0.0 à 1.0)
        Dim phaseA As Double = tempBeatGridA.CalculerPhase(positionA)
        Dim phaseB As Double = tempBeatGridB.CalculerPhase(positionB)

        Debug.WriteLine($"[SYNC B→A] Phases : A={phaseA:F3}, B={phaseB:F3}")

        ' === SNAP SIMPLE : Aligner Deck B sur son beat le plus proche MAINTENANT ===
        ' Ne faire le snap que si les BPM effectifs des deux decks sont suffisamment proches
        Dim epsRel As Double = 0.01 ' tolérance relative 1%
        Dim bpmDiffRel As Double = 1.0
        If bpmEffectifA > 0 AndAlso bpmEffectifB > 0 Then
            bpmDiffRel = Math.Abs(bpmEffectifA - bpmEffectifB) / Math.Max(1.0, Math.Max(bpmEffectifA, bpmEffectifB))
        End If
        If bpmDiffRel <= epsRel Then
            ' Trouver le beat le plus proche de Deck B (celui qui est le plus proche de sa position actuelle)
            Dim beatLePlusProcheB As Double = tempBeatGridB.TrouverBeatLePlusProche(positionB)

            ' Calculer à quelle distance on est de ce beat
            Dim distanceAuBeat As Double = Math.Abs(positionB - beatLePlusProcheB)

            Debug.WriteLine($"[SYNC B→A] Position B actuelle : {positionB:F3}s")
            Debug.WriteLine($"[SYNC B→A] Beat le plus proche : {beatLePlusProcheB:F3}s")
            Debug.WriteLine($"[SYNC B→A] Distance au beat : {distanceAuBeat * 1000:F1}ms")

            ' SNAP INSTANTANÉ au beat le plus proche
            Dim anciennePositionB As Double = positionB
            fichierAudioDeckB.CurrentTime = TimeSpan.FromSeconds(beatLePlusProcheB)
            TrackBarPositionDeckB.Value = CInt(beatLePlusProcheB)
            LabelDureeDeckB.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"),
                                                 fichierAudioDeckB.CurrentTime,
                                                 fichierAudioDeckB.TotalTime)

            Dim sautTemps As Double = beatLePlusProcheB - anciennePositionB
            Debug.WriteLine($"[SYNC B→A] ÉTAPE 2: BEAT SNAP ⚡ - Saut de {sautTemps:F3}s")
            Debug.WriteLine($"[SYNC B→A] Deck B: {anciennePositionB:F3}s → {beatLePlusProcheB:F3}s (beat le plus proche)")
        Else
            Debug.WriteLine($"[SYNC B→A] SKIP SNAP : BPM mismatch (A={bpmEffectifA:F3}, B={bpmEffectifB:F3}), diffRel={bpmDiffRel:F3} > {epsRel:F3}")
        End If


        ' === ÉTAPE 3 : TEMPO LOCK - PAS de corrections automatiques ===
        ' Le tempo reste FIXE après SYNC, seul re-clic SYNC réaligne la position
        If beatSyncEngine IsNot Nothing Then
            ' Stocker le tempo de base (ratio exact) pour Deck B
            tempoBaseDeckB = CSng(ratio)  ' Utiliser le ratio exact

            ' CORRECTION CRITIQUE : Utiliser le BPM CIBLE VERROUILLÉ pour éviter les dérives !
            Dim bpmReel_A As Double = bpmDeckA * (1.0 + pitchDeckA)
            Dim bpmReel_B As Double = bpmCibleDeckB  ' ⭐ Toujours utiliser le BPM cible verrouillé

            ' Vérifier si c'est un premier SYNC ou un re-SYNC
            If beatSyncEngine.SyncActifDeckB Then
                ' Re-SYNC : Mettre à jour seulement les BeatGrids avec le BPM CIBLE (pas recalculé)
                beatSyncEngine.ResynchoniserBeatGrids(
                    bpmReel_A, fichierAudioDeckA.TotalTime.TotalSeconds,
                    bpmCibleDeckB, fichierAudioDeckB.TotalTime.TotalSeconds  ' ⭐ BPM cible verrouillé
                )
                Debug.WriteLine($"[SYNC B→A] ÉTAPE 3: RE-SYNC - BeatGrids mis à jour avec BPM cible verrouillé (A={bpmReel_A:F3}, B={bpmCibleDeckB:F3}) ✅")
                Debug.WriteLine($"[SYNC B→A]   Le BPM cible reste verrouillé à {bpmCibleDeckB:F3}, pas de dérive possible !")
            Else
                ' Premier SYNC : Initialiser complètement avec le BPM cible verrouillé
                beatSyncEngine.InitialiserBeatGrids(
                    bpmReel_A, fichierAudioDeckA.TotalTime.TotalSeconds,
                    bpmCibleDeckB, fichierAudioDeckB.TotalTime.TotalSeconds,  ' ⭐ BPM cible verrouillé
                    fichierAudioDeckA, fichierAudioDeckB
                )
                Debug.WriteLine($"[SYNC B→A] ÉTAPE 3: BEAT LOCK initialisé avec BPM cible verrouillé (A={bpmReel_A:F3}, B={bpmCibleDeckB:F3}) ✅")
                Debug.WriteLine($"[SYNC B→A]   BPM cible={bpmCibleDeckB:F3} est maintenant verrouillé, ne sera jamais recalculé")

                ' ⚠️ DÉSACTIVÉ : Pas de corrections automatiques continues !
                ' Le tempo reste FIXE. Re-cliquer SYNC réaligne seulement la position.
                ' beatSyncEngine.SyncActifDeckB = True
                Debug.WriteLine($"[SYNC B→A] ÉTAPE 3: TEMPO LOCK activé (tempo fixe = {tempoBaseDeckB:F6}, pas de corrections auto)")
            End If
        End If

        ' === ÉTAPE 4 : ACTIVER L'AUTO-CALIBRATION (ajustement automatique basé sur drift mesuré) ===
        If Not autoCalibrationActive_DeckB Then
            autoCalibrationActive_DeckB = True
            driftAccumuléDeckB = 0.0
            calibrationCountDeckB = 0
            If Not autoCalibTimer.Enabled Then
                autoCalibTimer.Start()
            End If
            Debug.WriteLine($"[SYNC B→A] ÉTAPE 4: AUTO-CALIBRATION activée (mesure drift toutes les 3s pour ajuster le ratio)")
        End If

        ' Sauvegarder les ajustements
        SauvegarderAjustementsDJ()

        Debug.WriteLine($"[SYNC B→A] FIN - Beats alignés instantanément + auto-calibration active ⚡")
        Debug.WriteLine("═══════════════════════════════════════════════════")
    End Sub

    Private Sub ChargerFichierDeckA(cheminFichier As String)
        Try
            ' Arrêter la lecture en cours
            ArreterDeckA()

            cheminActuelDeckA = cheminFichier
            LabelTrackDeckA.Text = Path.GetFileName(cheminFichier)

            ' Purger le cache des waveforms en arrière-plan au chargement d'une nouvelle piste
            Try
                Task.Run(Sub() PurgeOldWaveformCache())
            Catch
            End Try

            ' === RESET PITCH ET TEMPO À 0% ===
            ' Remettre le pitch à 0% quand on charge une nouvelle chanson
            pitchDeckA = 0.0F
            TrackBarPitchDeckA.Value = 100
            LabelPitchDeckA.Text = String.Format(LanguageManager.GetString("DJ_Pitch_Value"), pitchDeckA)
            tempoBaseDeckA = 1.0F

            ' Créer la chaîne audio avec time stretch (SoundTouch), effets et metering
            fichierAudioDeckA = New AudioFileReader(cheminFichier)

            ' Générer la waveform en tâche de fond et l'appliquer au contrôle
            Task.Run(Sub()
                         Try
                             ' Avant de recalculer, tenter de lire depuis le cache
                             Try
                                 Dim cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "waveforms")
                                 Dim info = New FileInfo(cheminFichier)
                                 Dim hash = info.FullName.GetHashCode().ToString("X8") & "_" & info.LastWriteTimeUtc.ToFileTime().ToString()
                                 Dim cacheFile = Path.Combine(cacheDir, hash & "_B.dat")
                                 If File.Exists(cacheFile) Then
                                     Using fs As New FileStream(cacheFile, FileMode.Open, FileAccess.Read)
                                         Dim br As New BinaryReader(fs)
                                         Dim len = br.ReadInt32()
                                         Dim cached(len - 1) As Single
                                         For i As Integer = 0 To len - 1
                                             cached(i) = br.ReadSingle()
                                         Next
                                         If waveformDeckB IsNot Nothing AndAlso waveformDeckB.IsHandleCreated Then
                                             waveformDeckB.Invoke(Sub() waveformDeckB.SetWaveformSamples(cached))
                                         End If
                                     End Using
                                     Return
                                 End If
                             Catch
                             End Try
                             ' Avant de recalculer, tenter de lire depuis le cache
                             Try
                                 Dim cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "waveforms")
                                 Dim info = New FileInfo(cheminFichier)
                                 Dim hash = info.FullName.GetHashCode().ToString("X8") & "_" & info.LastWriteTimeUtc.ToFileTime().ToString()
                                 Dim cacheFile = Path.Combine(cacheDir, hash & "_A.dat")
                                 If File.Exists(cacheFile) Then
                                     Using fs As New FileStream(cacheFile, FileMode.Open, FileAccess.Read)
                                         Dim br As New BinaryReader(fs)
                                         Dim len = br.ReadInt32()
                                         Dim cached(len - 1) As Single
                                         For i As Integer = 0 To len - 1
                                             cached(i) = br.ReadSingle()
                                         Next
                                         If waveformDeckA IsNot Nothing AndAlso waveformDeckA.IsHandleCreated Then
                                             waveformDeckA.Invoke(Sub() waveformDeckA.SetWaveformSamples(cached))
                                         End If
                                     End Using
                                     Return
                                 End If
                             Catch
                             End Try
                             Dim samplesPerPixel As Integer = Math.Max(1, CInt(fichierAudioDeckA.Length / (waveformDeckA.Width * 4)))
                             Dim pixelCount As Integer = Math.Max(1, waveformDeckA.Width)
                             Dim samples(pixelCount - 1) As Single

                             Using reader As New AudioFileReader(cheminFichier)
                                 Dim buffer(samplesPerPixel * reader.WaveFormat.Channels - 1) As Single
                                 For px As Integer = 0 To pixelCount - 1
                                     Dim read = reader.Read(buffer, 0, buffer.Length)
                                     If read <= 0 Then Exit For
                                     Dim maxVal As Single = 0
                                     For i As Integer = 0 To read - 1
                                         maxVal = Math.Max(maxVal, Math.Abs(buffer(i)))
                                     Next
                                     samples(px) = maxVal
                                 Next
                             End Using
                             ' Sauvegarder en cache si demandé
                             Try
                                 Dim cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "waveforms")
                                 If Not Directory.Exists(cacheDir) Then Directory.CreateDirectory(cacheDir)
                                 Dim info = New FileInfo(cheminFichier)
                                 Dim hash = info.FullName.GetHashCode().ToString("X8") & "_" & info.LastWriteTimeUtc.ToFileTime().ToString()
                                 Dim cacheFile = Path.Combine(cacheDir, hash & "_B.dat")
                                 Using fs As New FileStream(cacheFile, FileMode.Create, FileAccess.Write)
                                     Dim bw As New BinaryWriter(fs)
                                     bw.Write(samples.Length)
                                     For i As Integer = 0 To samples.Length - 1
                                         bw.Write(samples(i))
                                     Next
                                     bw.Flush()
                                 End Using
                             Catch
                             End Try
                             ' Sauvegarder en cache si demandé
                             Try
                                 Dim cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "waveforms")
                                 If Not Directory.Exists(cacheDir) Then Directory.CreateDirectory(cacheDir)
                                 Dim info = New FileInfo(cheminFichier)
                                 Dim hash = info.FullName.GetHashCode().ToString("X8") & "_" & info.LastWriteTimeUtc.ToFileTime().ToString()
                                 Dim cacheFile = Path.Combine(cacheDir, hash & "_A.dat")
                                 Using fs As New FileStream(cacheFile, FileMode.Create, FileAccess.Write)
                                     Dim bw As New BinaryWriter(fs)
                                     bw.Write(samples.Length)
                                     For i As Integer = 0 To samples.Length - 1
                                         bw.Write(samples(i))
                                     Next
                                     bw.Flush()
                                 End Using
                             Catch
                             End Try

                             ' Normalisation simple
                             Dim maxAll As Single = 0
                             For i As Integer = 0 To samples.Length - 1
                                 If samples(i) > maxAll Then maxAll = samples(i)
                             Next
                             If maxAll > 0 Then
                                 For i As Integer = 0 To samples.Length - 1
                                     samples(i) = samples(i) / maxAll
                                 Next
                             End If

                             ' Appliquer sur le thread UI
                             If waveformDeckA IsNot Nothing AndAlso waveformDeckA.IsHandleCreated Then
                                 waveformDeckA.Invoke(Sub()
                                                          ' Set waveform and retrieve trim info
                                                          waveformDeckA.SetWaveformSamples(samples)
                                                      End Sub)
                             End If
                             ' Calcul d'onsets sur audio complet (RMS + novelty) pour alignement plus précis sur les beats
                             Dim arrA() As Single = Nothing
                             Try
                                 Using readerAll As New AudioFileReader(cheminFichier)
                                     Dim allSamples As New System.Collections.Generic.List(Of Single)()
                                     Dim readBuffer(8191) As Single
                                     Dim readCount As Integer = 0
                                     Do
                                         readCount = readerAll.Read(readBuffer, 0, readBuffer.Length)
                                         If readCount > 0 Then
                                             For ii As Integer = 0 To readCount - 1
                                                 allSamples.Add(readBuffer(ii))
                                             Next
                                         End If
                                     Loop While readCount > 0

                                     Dim channels As Integer = Math.Max(1, readerAll.WaveFormat.Channels)
                                     Dim totalFrames As Integer = Math.Max(1, allSamples.Count \ channels)
                                     Dim sampleRate As Integer = Math.Max(1, readerAll.WaveFormat.SampleRate)

                                     ' RMS frame-level
                                     Dim frameEnergy(totalFrames - 1) As Double
                                     For f As Integer = 0 To totalFrames - 1
                                         Dim sumSq As Double = 0.0
                                         Dim baseIndex As Integer = f * channels
                                         For c As Integer = 0 To channels - 1
                                             Dim idx As Integer = baseIndex + c
                                             If idx < allSamples.Count Then
                                                 Dim v As Double = allSamples(idx)
                                                 sumSq += v * v
                                             End If
                                         Next
                                         frameEnergy(f) = sumSq / channels
                                     Next

                                     ' Fenêtre d'analyse (~50 ms), hop 50%
                                     Dim windowSeconds As Double = 0.05
                                     Dim windowSize As Integer = Math.Max(1, CInt(sampleRate * windowSeconds))
                                     Dim hopSize As Integer = Math.Max(1, windowSize \ 2)

                                     Dim envelope As New System.Collections.Generic.List(Of Double)()
                                     Dim envelopeStartFrame As New System.Collections.Generic.List(Of Integer)()
                                     Dim pos As Integer = 0
                                     While pos + windowSize <= totalFrames
                                         Dim e As Double = 0.0
                                         For jj As Integer = 0 To windowSize - 1
                                             e += frameEnergy(pos + jj)
                                         Next
                                         envelope.Add(e / windowSize)
                                         envelopeStartFrame.Add(pos)
                                         pos += hopSize
                                     End While

                                     Dim sensitivityA As Single = 1.8F
                                     Try
                                         sensitivityA = ParametresGlobaux.OnsetDetectionSensitivity
                                     Catch
                                     End Try

                                     Dim detectedA As New System.Collections.Generic.List(Of Single)()
                                     If envelope.Count > 2 Then
                                         Dim novelty As New System.Collections.Generic.List(Of Double)()
                                         For i As Integer = 1 To envelope.Count - 1
                                             Dim d As Double = envelope(i) - envelope(i - 1)
                                             novelty.Add(If(d > 0.0, d, 0.0))
                                         Next

                                         Dim meanN As Double = 0.0
                                         For Each nv In novelty
                                             meanN += nv
                                         Next
                                         If novelty.Count > 0 Then
                                             meanN /= novelty.Count
                                         End If

                                         Dim varN As Double = 0.0
                                         For Each nv In novelty
                                             varN += (nv - meanN) * (nv - meanN)
                                         Next
                                         Dim stdN As Double = Math.Sqrt(varN / Math.Max(1, novelty.Count))
                                         Dim threshold As Double = meanN + (CDbl(sensitivityA) * stdN)

                                         For k As Integer = 1 To novelty.Count - 2
                                             If novelty(k) > novelty(k - 1) AndAlso novelty(k) >= novelty(k + 1) AndAlso novelty(k) > threshold Then
                                                 Dim envIdx As Integer = k + 1
                                                 ' Use the envelope window bounds as a tighter beat window
                                                 Dim startFrameLocal As Integer = Math.Max(0, envelopeStartFrame(envIdx))
                                                 Dim endFrameLocal As Integer = Math.Min(totalFrames - 1, envelopeStartFrame(envIdx) + windowSize)
                                                 Dim sNorm As Single = CSng(startFrameLocal) / CSng(totalFrames)
                                                 Dim eNorm As Single = CSng(endFrameLocal) / CSng(totalFrames)
                                                 detectedA.Add(Math.Max(0.0F, Math.Min(1.0F, sNorm)))
                                                 detectedA.Add(Math.Max(0.0F, Math.Min(1.0F, eNorm)))
                                             End If
                                         Next
                                         If detectedA.Count > 0 Then
                                             arrA = detectedA.ToArray()
                                         End If
                                     End If
                                 End Using
                                 ' Appliquer les positions détectées sur la waveform A (lignes blanches)
                                 If arrA IsNot Nothing AndAlso waveformDeckA IsNot Nothing AndAlso waveformDeckA.IsHandleCreated Then
                                     ' Need to convert beat times (normalized over full file) into positions relative to trimmed waveform
                                     waveformDeckA.BeginInvoke(New MethodInvoker(Sub()
                                                                                     waveformDeckA.WaveformColor = Color.DodgerBlue
                                                                                     Try
                                                                                         Dim trimmedStart As Integer = waveformDeckA.LastTrimStartPixel
                                                                                         Dim trimmedLen As Integer = waveformDeckA.LastTrimLengthPixels
                                                                                         If trimmedLen <= 0 Then
                                                                                             ' no trimming; markers are already normalized
                                                                                             waveformDeckA.SetOnsetMarkers(arrA)
                                                                                         Else
                                                                                             ' arrA contains start/end pairs (normalized over full file)
                                                                                             Dim converted As New System.Collections.Generic.List(Of Single)()
                                                                                             For i As Integer = 0 To arrA.Length - 1 Step 2
                                                                                                 Dim sNorm As Single = arrA(i)
                                                                                                 Dim eNorm As Single = arrA(i + 1)
                                                                                                 ' convert to pixel indices on original samples length approximated by trimmedLen
                                                                                                 Dim sPixel As Integer = CInt(sNorm * trimmedLen)
                                                                                                 Dim ePixel As Integer = CInt(eNorm * trimmedLen)
                                                                                                 ' shift relative to trimmed start
                                                                                                 sPixel = Math.Max(0, sPixel - trimmedStart)
                                                                                                 ePixel = Math.Max(0, ePixel - trimmedStart)
                                                                                                 ' normalize to trimmed length
                                                                                                 Dim sRel As Single = sPixel / CSng(Math.Max(1, trimmedLen))
                                                                                                 Dim eRel As Single = ePixel / CSng(Math.Max(1, trimmedLen))
                                                                                                 converted.Add(sRel)
                                                                                                 converted.Add(eRel)
                                                                                             Next
                                                                                             waveformDeckA.SetOnsetMarkers(converted.ToArray())
                                                                                         End If
                                                                                     Catch
                                                                                         waveformDeckA.SetOnsetMarkers(arrA)
                                                                                     End Try
                                                                                 End Sub))
                                 End If
                             Catch
                             End Try
                         Catch
                         End Try
                     End Sub)

            ' Time Stretch provider (SoundTouch, qualité professionnelle)
            timeStretchProviderDeckA = New TimeStretchSampleProvider(fichierAudioDeckA)
            timeStretchProviderDeckA.Enabled = True
            timeStretchProviderDeckA.TempoChange = 1.0F ' Tempo normal (pitch 0%)

            ' Effets (désactivés par défaut)
            phaserProviderDeckA = New PhaserSampleProvider(timeStretchProviderDeckA)
            phaserProviderDeckA.Enabled = False

            reverbProviderDeckA = New ReverbSampleProvider(phaserProviderDeckA)
            reverbProviderDeckA.Enabled = False

            echoProviderDeckA = New EchoSampleProvider(reverbProviderDeckA)
            echoProviderDeckA.Enabled = False

            ' Metering provider (pour VU-meter)
            meteringProviderDeckA = New MeteringSampleProvider(echoProviderDeckA)

            ' Volume provider (appliquer le crossfader dès le chargement)
            volumeProviderDeckA = New VolumeSampleProvider(meteringProviderDeckA)

            ' Calculer le volume en tenant compte du crossfader
            Dim volumeA As Single
            If crossfaderPosition < 0.5F Then
                volumeA = 1.0F
            Else
                volumeA = ((1.0F - crossfaderPosition) * 2.0F) ^ 3
            End If
            volumeProviderDeckA.Volume = (TrackBarVolumeDeckA.Value / 100.0F) * volumeA

            lecteurDeckA = New NAudio.Wave.WaveOutEvent()
            lecteurDeckA.Init(volumeProviderDeckA)

            ' Mettre à jour l'affichage
            LabelDureeDeckA.Text = fichierAudioDeckA.TotalTime.ToString("mm\:ss")
            TrackBarPositionDeckA.Maximum = CInt(fichierAudioDeckA.TotalTime.TotalSeconds)
            TrackBarPositionDeckA.Value = 0

            ' Point Cue par défaut au début
            cuePositionDeckA = TimeSpan.Zero

            ' Détecter BPM
            DetecterBPMDeckA()

            ' === DÉSACTIVER SYNC DECK A ===
            ' Désactiver le sync automatiquement quand on charge une nouvelle chanson
            If beatSyncEngine IsNot Nothing Then
                beatSyncEngine.SyncActifDeckA = False
                Debug.WriteLine("BeatSync Deck A désactivé au chargement de la piste")
            End If

        Catch ex As Exception
            MessageBox.Show(String.Format(LanguageManager.GetString("DJ_Error_LoadingDeck"), "A", ex.Message), LanguageManager.GetString("DJ_Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' === Deck A : Lecture ===
    Private Sub ButtonPlayDeckA_Click(sender As Object, e As EventArgs) Handles ButtonPlayDeckA.Click
        If lecteurDeckA Is Nothing OrElse fichierAudioDeckA Is Nothing Then Return

        If enPauseDeckA Then
            ' Reprendre
            lecteurDeckA.Play()
            enPauseDeckA = False
            lectureEnCoursDeckA = True
            ButtonPlayDeckA.Text = LanguageManager.GetString("DJ_Button_Pause")
        Else
            If lectureEnCoursDeckA Then
                ' Pause
                lecteurDeckA.Pause()
                enPauseDeckA = True
                lectureEnCoursDeckA = False
                ButtonPlayDeckA.Text = LanguageManager.GetString("DJ_Button_Play")
            Else
                ' Démarrer
                lecteurDeckA.Play()
                lectureEnCoursDeckA = True
                ButtonPlayDeckA.Text = LanguageManager.GetString("DJ_Button_Pause")
            End If
        End If
    End Sub

    ' === Deck A : Cue ===
    Private Sub ButtonCueDeckA_Click(sender As Object, e As EventArgs) Handles ButtonCueDeckA.Click
        If fichierAudioDeckA Is Nothing Then Return

        If lectureEnCoursDeckA Then
            ' Si en lecture : définir le point Cue à la position actuelle
            cuePositionDeckA = fichierAudioDeckA.CurrentTime
            MessageBox.Show(String.Format(LanguageManager.GetString("DJ_Cue_Set"), "A", cuePositionDeckA.ToString("mm\:ss")), LanguageManager.GetString("DJ_Cue_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            ' Si arrêté : retourner au point Cue
            fichierAudioDeckA.CurrentTime = cuePositionDeckA
            TrackBarPositionDeckA.Value = CInt(cuePositionDeckA.TotalSeconds)
        End If
    End Sub

    ' === Deck A : Stop ===
    Private Sub ButtonStopDeckA_Click(sender As Object, e As EventArgs) Handles ButtonStopDeckA.Click
        ArreterDeckA()
    End Sub

    Private Sub ArreterDeckA()
        If lecteurDeckA IsNot Nothing Then
            lecteurDeckA.Stop()
            lectureEnCoursDeckA = False
            enPauseDeckA = False
            ButtonPlayDeckA.Text = LanguageManager.GetString("DJ_Button_Play")
        End If
        If fichierAudioDeckA IsNot Nothing Then
            fichierAudioDeckA.CurrentTime = TimeSpan.Zero
            TrackBarPositionDeckA.Value = 0
        End If
    End Sub

    Private Sub ChargerFichierDeckB(cheminFichier As String)
        Try
            ' Arrêter la lecture en cours
            ArreterDeckB()

            cheminActuelDeckB = cheminFichier
            LabelTrackDeckB.Text = Path.GetFileName(cheminFichier)

            ' Purger le cache des waveforms en arrière-plan au chargement d'une nouvelle piste
            Try
                Task.Run(Sub() PurgeOldWaveformCache())
            Catch
            End Try

            ' === RESET PITCH ET TEMPO À 0% ===
            ' Remettre le pitch à 0% quand on charge une nouvelle chanson
            pitchDeckB = 0.0F
            TrackBarPitchDeckB.Value = 100
            LabelPitchDeckB.Text = String.Format(LanguageManager.GetString("DJ_Pitch_Value"), pitchDeckB)
            tempoBaseDeckB = 1.0F

            ' Créer la chaîne audio avec time stretch (SoundTouch), effets et metering
            fichierAudioDeckB = New AudioFileReader(cheminFichier)

            ' Générer la waveform en tâche de fond et l'appliquer au contrôle
            Task.Run(Sub()
                         Try
                             Dim samplesPerPixel As Integer = Math.Max(1, CInt(fichierAudioDeckB.Length / (waveformDeckB.Width * 4)))
                             Dim pixelCount As Integer = Math.Max(1, waveformDeckB.Width)
                             Dim samples(pixelCount - 1) As Single

                             Using reader As New AudioFileReader(cheminFichier)
                                 Dim buffer(samplesPerPixel * reader.WaveFormat.Channels - 1) As Single
                                 For px As Integer = 0 To pixelCount - 1
                                     Dim read = reader.Read(buffer, 0, buffer.Length)
                                     If read <= 0 Then Exit For
                                     Dim maxVal As Single = 0
                                     For i As Integer = 0 To read - 1
                                         maxVal = Math.Max(maxVal, Math.Abs(buffer(i)))
                                     Next
                                     samples(px) = maxVal
                                 Next
                             End Using

                             ' Normalisation simple
                             Dim maxAll As Single = 0
                             For i As Integer = 0 To samples.Length - 1
                                 If samples(i) > maxAll Then maxAll = samples(i)
                             Next
                             If maxAll > 0 Then
                                 For i As Integer = 0 To samples.Length - 1
                                     samples(i) = samples(i) / maxAll
                                 Next
                             End If
                             ' no fallback here for Deck B generation

                             ' Appliquer sur le thread UI
                             If waveformDeckB IsNot Nothing AndAlso waveformDeckB.IsHandleCreated Then
                                 waveformDeckB.Invoke(Sub() waveformDeckB.SetWaveformSamples(samples))
                             End If
                             ' Calcul simple d'onsets (energy-based peak picking) pour Deck B
                             Try
                                 Dim onsetListB As New System.Collections.Generic.List(Of Single)()
                                 Dim windowB As Integer = Math.Max(3, CInt(pixelCount * 0.002))
                                 Dim sensitivityB As Single = 1.8F
                                 Try
                                     sensitivityB = ParametresGlobaux.OnsetDetectionSensitivity
                                 Catch
                                 End Try
                                 For i As Integer = windowB To samples.Length - windowB - 1
                                     Dim energyB As Single = 0
                                     For j As Integer = i - windowB To i + windowB
                                         If j >= 0 AndAlso j < samples.Length Then
                                             energyB += Math.Abs(samples(j))
                                         End If
                                     Next
                                     Dim localAvgB As Single = energyB / (windowB * 2 + 1)
                                     If Math.Abs(samples(i)) > localAvgB * sensitivityB AndAlso Math.Abs(samples(i)) > 0.01F Then
                                         ' produce a tight window around the sample i using sample-level neighborhood
                                         Dim sIdx As Integer = Math.Max(0, i - (windowB \ 2))
                                         Dim eIdx As Integer = Math.Min(samples.Length - 1, i + (windowB \ 2))
                                         onsetListB.Add(sIdx / CSng(samples.Length))
                                         onsetListB.Add(eIdx / CSng(samples.Length))
                                     End If
                                 Next
                                 ' Désactivé: ne plus injecter les marqueurs d'onsets dans WaveformControl (Deck B)
                                 ' Désactivé: ne plus injecter les marqueurs d'onsets dans WaveformControl (Deck B)
                                 ' If onsetListB.Count > 0 AndAlso waveformDeckB IsNot Nothing AndAlso waveformDeckB.IsHandleCreated Then
                                 '     Dim arrB() As Single = onsetListB.ToArray()
                                 '     waveformDeckB.BeginInvoke(New MethodInvoker(Sub()
                                 '                                                  waveformDeckB.WaveformColor = Color.OrangeRed
                                 '                                                  waveformDeckB.SetOnsetMarkers(arrB)
                                 '                                              End Sub))
                                 ' End If
                             Catch
                             End Try
                         Catch
                         End Try
                     End Sub)

            ' Time Stretch provider (SoundTouch, qualité professionnelle)
            timeStretchProviderDeckB = New TimeStretchSampleProvider(fichierAudioDeckB)
            timeStretchProviderDeckB.Enabled = True
            timeStretchProviderDeckB.TempoChange = 1.0F ' Tempo normal (pitch 0%)

            ' Effets (désactivés par défaut)
            phaserProviderDeckB = New PhaserSampleProvider(timeStretchProviderDeckB)
            phaserProviderDeckB.Enabled = False

            reverbProviderDeckB = New ReverbSampleProvider(phaserProviderDeckB)
            reverbProviderDeckB.Enabled = False

            echoProviderDeckB = New EchoSampleProvider(reverbProviderDeckB)
            echoProviderDeckB.Enabled = False

            ' Metering provider (pour VU-meter)
            meteringProviderDeckB = New MeteringSampleProvider(echoProviderDeckB)

            ' Volume provider (appliquer le crossfader dès le chargement)
            volumeProviderDeckB = New VolumeSampleProvider(meteringProviderDeckB)

            ' Calculer le volume en tenant compte du crossfader
            Dim volumeB As Single
            If crossfaderPosition < 0.5F Then
                volumeB = (crossfaderPosition * 2.0F) ^ 3
            Else
                volumeB = 1.0F
            End If
            volumeProviderDeckB.Volume = (TrackBarVolumeDeckB.Value / 100.0F) * volumeB

            lecteurDeckB = New NAudio.Wave.WaveOutEvent()
            lecteurDeckB.Init(volumeProviderDeckB)

            ' Mettre à jour l'affichage
            LabelDureeDeckB.Text = fichierAudioDeckB.TotalTime.ToString("mm\:ss")
            TrackBarPositionDeckB.Maximum = CInt(fichierAudioDeckB.TotalTime.TotalSeconds)
            TrackBarPositionDeckB.Value = 0

            ' Point Cue par défaut au début
            cuePositionDeckB = TimeSpan.Zero

            ' Détecter BPM
            DetecterBPMDeckB()

            ' === DÉSACTIVER SYNC DECK B ===
            ' Désactiver le sync automatiquement quand on charge une nouvelle chanson
            If beatSyncEngine IsNot Nothing Then
                beatSyncEngine.SyncActifDeckB = False
                Debug.WriteLine("BeatSync Deck B désactivé au chargement de la piste")
            End If

        Catch ex As Exception
            MessageBox.Show(String.Format(LanguageManager.GetString("DJ_Error_LoadingDeck"), "B", ex.Message), LanguageManager.GetString("DJ_Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' === Deck B : Lecture ===
    Private Sub ButtonPlayDeckB_Click(sender As Object, e As EventArgs) Handles ButtonPlayDeckB.Click
        If lecteurDeckB Is Nothing OrElse fichierAudioDeckB Is Nothing Then Return

        If enPauseDeckB Then
            ' Reprendre
            lecteurDeckB.Play()
            enPauseDeckB = False
            lectureEnCoursDeckB = True
            ButtonPlayDeckB.Text = LanguageManager.GetString("DJ_Button_Pause")
        Else
            If lectureEnCoursDeckB Then
                ' Pause
                lecteurDeckB.Pause()
                enPauseDeckB = True
                lectureEnCoursDeckB = False
                ButtonPlayDeckB.Text = LanguageManager.GetString("DJ_Button_Play")
            Else
                ' Démarrer
                lecteurDeckB.Play()
                lectureEnCoursDeckB = True
                ButtonPlayDeckB.Text = LanguageManager.GetString("DJ_Button_Pause")
            End If
        End If
    End Sub

    ' === Deck B : Cue ===
    Private Sub ButtonCueDeckB_Click(sender As Object, e As EventArgs) Handles ButtonCueDeckB.Click
        If fichierAudioDeckB Is Nothing Then Return

        If lectureEnCoursDeckB Then
            ' Si en lecture : définir le point Cue à la position actuelle
            cuePositionDeckB = fichierAudioDeckB.CurrentTime
            MessageBox.Show(String.Format(LanguageManager.GetString("DJ_Cue_Set"), "B", cuePositionDeckB.ToString("mm\:ss")), LanguageManager.GetString("DJ_Cue_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            ' Si arrêté : retourner au point Cue
            fichierAudioDeckB.CurrentTime = cuePositionDeckB
            TrackBarPositionDeckB.Value = CInt(cuePositionDeckB.TotalSeconds)
        End If
    End Sub

    ' === Deck B : Stop ===
    Private Sub ButtonStopDeckB_Click(sender As Object, e As EventArgs) Handles ButtonStopDeckB.Click
        ArreterDeckB()
    End Sub

    Private Sub ArreterDeckB()
        If lecteurDeckB IsNot Nothing Then
            lecteurDeckB.Stop()
            lectureEnCoursDeckB = False
            enPauseDeckB = False
            ButtonPlayDeckB.Text = LanguageManager.GetString("DJ_Button_Play")
        End If
        If fichierAudioDeckB IsNot Nothing Then
            fichierAudioDeckB.CurrentTime = TimeSpan.Zero
            TrackBarPositionDeckB.Value = 0
        End If
    End Sub

    ' === Crossfader ===
    Private Sub TrackBarCrossfader_Scroll(sender As Object, e As EventArgs) Handles TrackBarCrossfader.Scroll
        ' Valeur 0-100, convertir en 0.0-1.0
        crossfaderPosition = TrackBarCrossfader.Value / 100.0F

        ' Courbe DJ : coupe agressive au centre
        Dim volumeA As Single
        Dim volumeB As Single

        If crossfaderPosition < 0.5F Then
            ' Côté A (0-50) : A plein volume, B diminue
            volumeA = 1.0F
            volumeB = (crossfaderPosition * 2.0F) ^ 3 ' Courbe cubique pour coupe agressive
        Else
            ' Côté B (50-100) : B plein volume, A diminue
            volumeB = 1.0F
            volumeA = ((1.0F - crossfaderPosition) * 2.0F) ^ 3
        End If

        ' Appliquer les volumes
        If volumeProviderDeckA IsNot Nothing Then
            volumeProviderDeckA.Volume = (TrackBarVolumeDeckA.Value / 100.0F) * volumeA
        End If
        If volumeProviderDeckB IsNot Nothing Then
            volumeProviderDeckB.Volume = (TrackBarVolumeDeckB.Value / 100.0F) * volumeB
        End If

        ' Afficher la position
        LabelCrossfader.Text = String.Format(LanguageManager.GetString("DJ_CrossfaderLabel"), TrackBarCrossfader.Value)

        ' Sauvegarder immédiatement
        SauvegarderAjustementsDJ()
    End Sub

    ' === Volume Deck A ===
    Private Sub TrackBarVolumeDeckA_Scroll(sender As Object, e As EventArgs) Handles TrackBarVolumeDeckA.Scroll
        LabelVolumeDeckA.Text = String.Format(LanguageManager.GetString("DJ_VolumeLabel"), "A", TrackBarVolumeDeckA.Value)
        If volumeProviderDeckA IsNot Nothing Then
            ' Prendre en compte le crossfader
            Dim volumeA As Single
            If crossfaderPosition < 0.5F Then
                volumeA = 1.0F
            Else
                volumeA = ((1.0F - crossfaderPosition) * 2.0F) ^ 3
            End If
            volumeProviderDeckA.Volume = (TrackBarVolumeDeckA.Value / 100.0F) * volumeA
        End If
        ' Sauvegarder immédiatement
        SauvegarderAjustementsDJ()
    End Sub

    ' === Volume Deck B ===
    Private Sub TrackBarVolumeDeckB_Scroll(sender As Object, e As EventArgs) Handles TrackBarVolumeDeckB.Scroll
        LabelVolumeDeckB.Text = String.Format(LanguageManager.GetString("DJ_VolumeLabel"), "B", TrackBarVolumeDeckB.Value)
        If volumeProviderDeckB IsNot Nothing Then
            ' Prendre en compte le crossfader
            Dim volumeB As Single
            If crossfaderPosition < 0.5F Then
                volumeB = (crossfaderPosition * 2.0F) ^ 3
            Else
                volumeB = 1.0F
            End If
            volumeProviderDeckB.Volume = (TrackBarVolumeDeckB.Value / 100.0F) * volumeB
        End If
        ' Sauvegarder immédiatement
        SauvegarderAjustementsDJ()
    End Sub

    ' === Pitch Deck A ===
    Private Sub TrackBarPitchDeckA_Scroll(sender As Object, e As EventArgs) Handles TrackBarPitchDeckA.Scroll
        ' Valeur 92-108 (±8%)
        pitchDeckA = (TrackBarPitchDeckA.Value - 100) / 100.0F
        LabelPitchDeckA.Text = String.Format(LanguageManager.GetString("DJ_Pitch_Value"), pitchDeckA)

        ' Mettre à jour le BPM ajusté en temps réel
        Dim bpmAjuste As Double = 0.0
        If bpmDeckA > 0.0 Then
            bpmAjuste = bpmDeckA * (1.0 + CDbl(pitchDeckA))
            LabelBPMDeckA.Text = String.Format(LanguageManager.GetString("DJ_BPM_Value"), bpmAjuste)
        End If

        ' Appliquer le time stretch (tempo change) en temps réel avec SoundTouch
        If timeStretchProviderDeckA IsNot Nothing Then
            timeStretchProviderDeckA.TempoChange = 1.0F + pitchDeckA
        End If

        ' === MISE À JOUR BEAT SYNC ===
        ' Si le sync est actif, mettre à jour le tempo de base et le BeatGrid
        If beatSyncEngine IsNot Nothing AndAlso beatSyncEngine.SyncActifDeckA Then
            ' Mettre à jour le tempo de base (pour que BeatSync utilise le bon tempo)
            tempoBaseDeckA = 1.0F + pitchDeckA

            ' Mettre à jour SEULEMENT le BeatGrid Deck A avec le nouveau BPM ajusté
            ' (sans réinitialiser l'historique de drift !)
            If bpmAjuste > 0.0 Then
                beatSyncEngine.MettreAJourBeatGridDeckA(bpmAjuste)
                Debug.WriteLine($"BeatSync: Tempo base Deck A mis à jour = {tempoBaseDeckA:F4}, BPM = {bpmAjuste:F3}")
            End If
        End If

        ' Sauvegarder immédiatement
        SauvegarderAjustementsDJ()
    End Sub

    ' === Pitch Deck B ===
    Private Sub TrackBarPitchDeckB_Scroll(sender As Object, e As EventArgs) Handles TrackBarPitchDeckB.Scroll
        ' Valeur 92-108 (±8%)
        pitchDeckB = (TrackBarPitchDeckB.Value - 100) / 100.0F
        LabelPitchDeckB.Text = String.Format(LanguageManager.GetString("DJ_Pitch_Value"), pitchDeckB)

        ' Mettre à jour le BPM ajusté en temps réel
        Dim bpmAjuste As Double = 0.0
        If bpmDeckB > 0.0 Then
            bpmAjuste = bpmDeckB * (1.0 + CDbl(pitchDeckB))
            LabelBPMDeckB.Text = String.Format(LanguageManager.GetString("DJ_BPM_Value"), bpmAjuste)
        End If

        ' Appliquer le time stretch (tempo change) en temps réel avec SoundTouch
        If timeStretchProviderDeckB IsNot Nothing Then
            timeStretchProviderDeckB.TempoChange = 1.0F + pitchDeckB
        End If

        ' === MISE À JOUR BEAT SYNC ===
        ' Si le sync est actif, mettre à jour le tempo de base et le BeatGrid
        If beatSyncEngine IsNot Nothing AndAlso beatSyncEngine.SyncActifDeckB Then
            ' Mettre à jour le tempo de base (pour que BeatSync utilise le bon tempo)
            tempoBaseDeckB = 1.0F + pitchDeckB

            ' Mettre à jour SEULEMENT le BeatGrid Deck B avec le nouveau BPM ajusté
            ' (sans réinitialiser l'historique de drift !)
            If bpmAjuste > 0.0 Then
                beatSyncEngine.MettreAJourBeatGridDeckB(bpmAjuste)
                Debug.WriteLine($"BeatSync: Tempo base Deck B mis à jour = {tempoBaseDeckB:F4}, BPM = {bpmAjuste:F3}")
            End If
        End If

        ' Sauvegarder immédiatement
        SauvegarderAjustementsDJ()
    End Sub

    ' === Bouton Reset Pitch à 0% - Deck A ===
    Private Sub Button_DeckA_BackTo0_Click(sender As Object, e As EventArgs) Handles Button_DeckA_BackTo0.Click
        ' Remettre instantanément le pitch à 0.0% (TrackBar = 100)
        TrackBarPitchDeckA.Value = 100
        pitchDeckA = 0.0F
        LabelPitchDeckA.Text = String.Format(LanguageManager.GetString("DJ_Pitch_Value"), pitchDeckA)

        ' Remettre le BPM à sa valeur originale
        If bpmDeckA > 0.0F Then
            LabelBPMDeckA.Text = String.Format(LanguageManager.GetString("DJ_BPM_Value"), bpmDeckA)
        End If

        ' Appliquer le time stretch à 1.0 (normal)
        If timeStretchProviderDeckA IsNot Nothing Then
            timeStretchProviderDeckA.TempoChange = 1.0F
        End If

        ' === MISE À JOUR BEAT SYNC ===
        ' Si le sync est actif, mettre à jour le tempo de base et le BeatGrid
        If beatSyncEngine IsNot Nothing AndAlso beatSyncEngine.SyncActifDeckA Then
            ' Mettre à jour le tempo de base
            tempoBaseDeckA = 1.0F

            ' Mettre à jour le BeatGrid avec le BPM original
            If bpmDeckA > 0.0 AndAlso fichierAudioDeckA IsNot Nothing Then
                beatSyncEngine.InitialiserBeatGrids(
                    bpmDeckA, fichierAudioDeckA.TotalTime.TotalSeconds,
                    If(bpmDeckB > 0, bpmDeckB * (1.0 + CDbl(pitchDeckB)), bpmDeckB),
                    If(fichierAudioDeckB IsNot Nothing, fichierAudioDeckB.TotalTime.TotalSeconds, 0),
                    fichierAudioDeckA, fichierAudioDeckB
                )
                Debug.WriteLine($"BeatSync: BeatGrid Deck A reset au BPM original {bpmDeckA:F3}")
            End If
        End If

        ' Sauvegarder immédiatement
        SauvegarderAjustementsDJ()
    End Sub

    ' === Bouton Reset Pitch à 0% - Deck B ===
    Private Sub Button_DeckB_BackTo0_Click(sender As Object, e As EventArgs) Handles Button_DeckB_BackTo0.Click
        ' Remettre instantanément le pitch à 0.0% (TrackBar = 100)
        TrackBarPitchDeckB.Value = 100
        pitchDeckB = 0.0F
        LabelPitchDeckB.Text = String.Format(LanguageManager.GetString("DJ_Pitch_Value"), pitchDeckB)

        ' Remettre le BPM à sa valeur originale
        If bpmDeckB > 0.0F Then
            LabelBPMDeckB.Text = String.Format(LanguageManager.GetString("DJ_BPM_Value"), bpmDeckB)
        End If

        ' Appliquer le time stretch à 1.0 (normal)
        If timeStretchProviderDeckB IsNot Nothing Then
            timeStretchProviderDeckB.TempoChange = 1.0F
        End If

        ' === MISE À JOUR BEAT SYNC ===
        ' Si le sync est actif, mettre à jour le tempo de base et le BeatGrid
        If beatSyncEngine IsNot Nothing AndAlso beatSyncEngine.SyncActifDeckB Then
            ' Mettre à jour le tempo de base
            tempoBaseDeckB = 1.0F

            ' Mettre à jour le BeatGrid avec le BPM original
            If bpmDeckB > 0.0 AndAlso fichierAudioDeckB IsNot Nothing Then
                beatSyncEngine.InitialiserBeatGrids(
                    If(bpmDeckA > 0, bpmDeckA * (1.0 + CDbl(pitchDeckA)), bpmDeckA),
                    If(fichierAudioDeckA IsNot Nothing, fichierAudioDeckA.TotalTime.TotalSeconds, 0),
                    bpmDeckB, fichierAudioDeckB.TotalTime.TotalSeconds,
                    fichierAudioDeckA, fichierAudioDeckB
                )
                Debug.WriteLine($"BeatSync: BeatGrid Deck B reset au BPM original {bpmDeckB:F3}")
            End If
        End If

        ' Sauvegarder immédiatement
        SauvegarderAjustementsDJ()
    End Sub

    ' === Position Deck A (scratcher/seek) ===

    Private Sub TrackBarPositionDeckA_MouseDown(sender As Object, e As MouseEventArgs) Handles TrackBarPositionDeckA.MouseDown
        isUserDraggingPositionA = True
    End Sub

    Private Sub TrackBarPositionDeckA_MouseUp(sender As Object, e As MouseEventArgs) Handles TrackBarPositionDeckA.MouseUp
        isUserDraggingPositionA = False
        ' Appliquer la nouvelle position au fichier audio
        If fichierAudioDeckA IsNot Nothing Then
            fichierAudioDeckA.CurrentTime = TimeSpan.FromSeconds(TrackBarPositionDeckA.Value)
        End If
    End Sub

    Private Sub TrackBarPositionDeckA_Scroll(sender As Object, e As EventArgs) Handles TrackBarPositionDeckA.Scroll
        ' Pendant le drag, mettre à jour la position en temps réel (scratcher)
        If isUserDraggingPositionA AndAlso fichierAudioDeckA IsNot Nothing Then
            fichierAudioDeckA.CurrentTime = TimeSpan.FromSeconds(TrackBarPositionDeckA.Value)
            LabelDureeDeckA.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckA.CurrentTime, fichierAudioDeckA.TotalTime)
        End If
    End Sub

    ' === Position Deck B (scratcher/seek) ===

    Private Sub TrackBarPositionDeckB_MouseDown(sender As Object, e As MouseEventArgs) Handles TrackBarPositionDeckB.MouseDown
        isUserDraggingPositionB = True
    End Sub

    Private Sub TrackBarPositionDeckB_MouseUp(sender As Object, e As MouseEventArgs) Handles TrackBarPositionDeckB.MouseUp
        isUserDraggingPositionB = False
        ' Appliquer la nouvelle position au fichier audio
        If fichierAudioDeckB IsNot Nothing Then
            fichierAudioDeckB.CurrentTime = TimeSpan.FromSeconds(TrackBarPositionDeckB.Value)
        End If
    End Sub

    Private Sub TrackBarPositionDeckB_Scroll(sender As Object, e As EventArgs) Handles TrackBarPositionDeckB.Scroll
        ' Pendant le drag, mettre à jour la position en temps réel (scratcher)
        If isUserDraggingPositionB AndAlso fichierAudioDeckB IsNot Nothing Then
            fichierAudioDeckB.CurrentTime = TimeSpan.FromSeconds(TrackBarPositionDeckB.Value)
            LabelDureeDeckB.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckB.CurrentTime, fichierAudioDeckB.TotalTime)
        End If
    End Sub

    Private Sub WaveformDeckA_PositionClicked(position As Single)
        Try
            If fichierAudioDeckA Is Nothing Then Return

            Dim positionNormalisee As Double = Math.Max(0.0R, Math.Min(1.0R, CDbl(position)))
            Dim total As Double = Math.Max(1.0R, fichierAudioDeckA.TotalTime.TotalSeconds)
            fichierAudioDeckA.CurrentTime = TimeSpan.FromSeconds(total * positionNormalisee)

            If TrackBarPositionDeckA.Maximum > 0 Then
                Dim positionSecondes As Integer = CInt(fichierAudioDeckA.CurrentTime.TotalSeconds)
                TrackBarPositionDeckA.Value = Math.Max(TrackBarPositionDeckA.Minimum, Math.Min(TrackBarPositionDeckA.Maximum, positionSecondes))
            End If

            LabelDureeDeckA.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckA.CurrentTime, fichierAudioDeckA.TotalTime)
        Catch
        End Try
    End Sub

    Private Sub WaveformDeckA_DragMoved(position As Single)
        ' Pendant le drag, déplacer la position du fichier audio pour qu'on entende ce qui passe sous la ligne centrale
        Try
            If fichierAudioDeckA Is Nothing Then Return
            Dim positionNormalisee As Double = Math.Max(0.0R, Math.Min(1.0R, CDbl(position)))
            Dim total As Double = Math.Max(1.0R, fichierAudioDeckA.TotalTime.TotalSeconds)
            Dim targetSec As Double = total * positionNormalisee

            ' Si la piste était en lecture au début du drag, utiliser le lecteur principal en mode preview (pas de creation de scrub player)
            If dragWasPlayingDeckA Then
                Try
                    ' Mettre la position du lecteur principal temporairement pour audition (preview)
                    If fichierAudioDeckA IsNot Nothing Then
                        fichierAudioDeckA.CurrentTime = TimeSpan.FromSeconds(targetSec)
                        LabelDureeDeckA.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckA.CurrentTime, fichierAudioDeckA.TotalTime)
                    End If
                    ' Démarrer un timer court qui remettra le lecteur en pause si la souris n'a plus bougé
                    If previewTimerDeckA Is Nothing Then
                        previewTimerDeckA = New System.Windows.Forms.Timer()
                        previewTimerDeckA.Interval = 120
                        AddHandler previewTimerDeckA.Tick, Sub(s2, ev2)
                                                              Try
                                                                  If lecteurDeckA IsNot Nothing Then
                                                                      lecteurDeckA.Pause()
                                                                      enPauseDeckA = True
                                                                      lectureEnCoursDeckA = False
                                                                      ButtonPlayDeckA.Text = LanguageManager.GetString("DJ_Button_Play")
                                                                  End If
                                                              Catch
                                                              End Try
                                                              Try
                                                                  previewTimerDeckA.Stop()
                                                              Catch
                                                              End Try
                                                          End Sub
                    End If
                    ' Relancer/Restart the preview timer on each move
                    Try
                        previewTimerDeckA.Stop()
                        previewTimerDeckA.Start()
                    Catch
                    End Try
                Catch
                End Try
            Else
                ' If not playing, only update the file's current time so playback will start here if play is pressed later
                fichierAudioDeckA.CurrentTime = TimeSpan.FromSeconds(targetSec)
                If TrackBarPositionDeckA.Maximum > 0 Then
                    Dim positionSecondes As Integer = CInt(fichierAudioDeckA.CurrentTime.TotalSeconds)
                    TrackBarPositionDeckA.Value = Math.Max(TrackBarPositionDeckA.Minimum, Math.Min(TrackBarPositionDeckA.Maximum, positionSecondes))
                End If
                LabelDureeDeckA.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckA.CurrentTime, fichierAudioDeckA.TotalTime)
            End If
        Catch
        End Try
    End Sub

    Private Sub WaveformDeckA_DragStarted()
        Try
            ' Indiquer que l'utilisateur commence un drag : le contrôle est alors entièrement pris en charge par la souris.
            dragWasPlayingDeckA = lectureEnCoursDeckA
            isUserDraggingPositionA = True
            ' Si la piste était en lecture, on la laisse tourner pour auditionner pendant le drag en mettant à jour CurrentTime
            ' On n'arrête pas immédiatement le lecteur principal ici pour éviter un artefact; previewTimer gérera la pause sur immobilité
            If lecteurDeckA IsNot Nothing AndAlso lectureEnCoursDeckA Then
                ' nothing here, preview will be managed in DragMoved
            End If
            ' nothing to prepare here for preview; DragMoved will manage preview playback
        Catch
        End Try
    End Sub

    Private Sub WaveformDeckA_DragEnded()
        Try
            ' When drag ends, optionally consider snapping center to nearest beat if BPMs match
            Dim snapped As Boolean = False
            Try
                If fichierAudioDeckA IsNot Nothing AndAlso fichierAudioDeckB IsNot Nothing Then
                    Dim bpmA As Double = If(bpmDeckA > 0, bpmDeckA * (1.0 + pitchDeckA), 0.0)
                    Dim bpmB As Double = If(bpmDeckB > 0, bpmDeckB * (1.0 + pitchDeckB), 0.0)
                    Dim epsRel As Double = 0.01
                    Dim diffRel As Double = 1.0
                    If bpmA > 0 AndAlso bpmB > 0 Then
                        diffRel = Math.Abs(bpmA - bpmB) / Math.Max(1.0, Math.Max(bpmA, bpmB))
                    End If
                    If diffRel <= epsRel Then
                        ' Both files present and BPMs similar -> snap
                        Dim centerPos As Single = 0.5F
                        Dim totalA As Double = Math.Max(1.0, fichierAudioDeckA.TotalTime.TotalSeconds)
                        Dim targetSec As Double = totalA * centerPos
                        If bpmDeckA > 0 Then
                            Dim bpmEffectifA As Double = bpmDeckA * (1.0 + pitchDeckA)
                            Dim gridA As New BeatGrid(bpmEffectifA, totalA)
                            Dim nearestBeat As Double = gridA.TrouverBeatLePlusProche(targetSec)
                            fichierAudioDeckA.CurrentTime = TimeSpan.FromSeconds(nearestBeat)
                            TrackBarPositionDeckA.Value = CInt(Math.Max(TrackBarPositionDeckA.Minimum, Math.Min(TrackBarPositionDeckA.Maximum, CInt(nearestBeat))))
                            LabelDureeDeckA.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckA.CurrentTime, fichierAudioDeckA.TotalTime)
                            snapped = True
                        End If
                    End If
                End If
            Catch
            End Try
            ' Nettoyer les timers de preview et reprendre la lecture principale si nécessaire
            Try
                If previewTimerDeckA IsNot Nothing Then
                    Try
                        previewTimerDeckA.Stop()
                    Catch
                    End Try
                    previewTimerDeckA = Nothing
                End If
            Catch
            End Try
            If dragWasPlayingDeckA AndAlso lecteurDeckA IsNot Nothing Then
                Try
                    lecteurDeckA.Play()
                    lectureEnCoursDeckA = True
                    enPauseDeckA = False
                    ButtonPlayDeckA.Text = LanguageManager.GetString("DJ_Button_Pause")
                Catch
                End Try
            End If
            isUserDraggingPositionA = False
            dragWasPlayingDeckA = False
        Catch
        End Try
    End Sub

    Private Sub WaveformDeckB_PositionClicked(position As Single)
        Try
            If fichierAudioDeckB Is Nothing Then Return

            Dim positionNormalisee As Double = Math.Max(0.0R, Math.Min(1.0R, CDbl(position)))
            Dim total As Double = Math.Max(1.0R, fichierAudioDeckB.TotalTime.TotalSeconds)
            fichierAudioDeckB.CurrentTime = TimeSpan.FromSeconds(total * positionNormalisee)

            If TrackBarPositionDeckB.Maximum > 0 Then
                Dim positionSecondes As Integer = CInt(fichierAudioDeckB.CurrentTime.TotalSeconds)
                TrackBarPositionDeckB.Value = Math.Max(TrackBarPositionDeckB.Minimum, Math.Min(TrackBarPositionDeckB.Maximum, positionSecondes))
            End If

            LabelDureeDeckB.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckB.CurrentTime, fichierAudioDeckB.TotalTime)
        Catch
        End Try
    End Sub

    Private Sub WaveformDeckB_DragMoved(position As Single)
        Try
            If fichierAudioDeckB Is Nothing Then Return
            Dim positionNormalisee As Double = Math.Max(0.0R, Math.Min(1.0R, CDbl(position)))
            Dim total As Double = Math.Max(1.0R, fichierAudioDeckB.TotalTime.TotalSeconds)
            Dim targetSec As Double = total * positionNormalisee

            If dragWasPlayingDeckB Then
                Try
                    If fichierAudioDeckB IsNot Nothing Then
                        fichierAudioDeckB.CurrentTime = TimeSpan.FromSeconds(targetSec)
                        LabelDureeDeckB.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckB.CurrentTime, fichierAudioDeckB.TotalTime)
                    End If
                    If previewTimerDeckB Is Nothing Then
                        previewTimerDeckB = New System.Windows.Forms.Timer()
                        previewTimerDeckB.Interval = 120
                        AddHandler previewTimerDeckB.Tick, Sub(s2, ev2)
                                                              Try
                                                                  If lecteurDeckB IsNot Nothing Then
                                                                      lecteurDeckB.Pause()
                                                                      enPauseDeckB = True
                                                                      lectureEnCoursDeckB = False
                                                                      ButtonPlayDeckB.Text = LanguageManager.GetString("DJ_Button_Play")
                                                                  End If
                                                              Catch
                                                              End Try
                                                              Try
                                                                  previewTimerDeckB.Stop()
                                                              Catch
                                                              End Try
                                                          End Sub
                    End If
                    Try
                        previewTimerDeckB.Stop()
                        previewTimerDeckB.Start()
                    Catch
                    End Try
                Catch
                End Try
            Else
                fichierAudioDeckB.CurrentTime = TimeSpan.FromSeconds(targetSec)
                If TrackBarPositionDeckB.Maximum > 0 Then
                    Dim positionSecondes As Integer = CInt(fichierAudioDeckB.CurrentTime.TotalSeconds)
                    TrackBarPositionDeckB.Value = Math.Max(TrackBarPositionDeckB.Minimum, Math.Min(TrackBarPositionDeckB.Maximum, positionSecondes))
                End If
                LabelDureeDeckB.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckB.CurrentTime, fichierAudioDeckB.TotalTime)
            End If
        Catch
        End Try
    End Sub

    Private Sub WaveformDeckB_DragStarted()
        Try
            dragWasPlayingDeckB = lectureEnCoursDeckB
            isUserDraggingPositionB = True
            ' Si la piste était en lecture, la mettre en pause pour éviter la double lecture et permettre le scrub player
            If lecteurDeckB IsNot Nothing AndAlso lectureEnCoursDeckB Then
                Try
                    lecteurDeckB.Pause()
                Catch
                End Try
                lectureEnCoursDeckB = False
                enPauseDeckB = True
                ButtonPlayDeckB.Text = LanguageManager.GetString("DJ_Button_Play")
            End If
            ' nothing to prepare here for preview; DragMoved will manage preview/pause
        Catch
        End Try
    End Sub

    Private Sub WaveformDeckB_DragEnded()
        Try
            If fichierAudioDeckA Is Nothing OrElse fichierAudioDeckB Is Nothing Then Return
            Dim bpmA As Double = If(bpmDeckA > 0, bpmDeckA * (1.0 + pitchDeckA), 0.0)
            Dim bpmB As Double = If(bpmDeckB > 0, bpmDeckB * (1.0 + pitchDeckB), 0.0)
            Dim epsRel As Double = 0.01
            Dim diffRel As Double = 1.0
            If bpmA > 0 AndAlso bpmB > 0 Then
                diffRel = Math.Abs(bpmA - bpmB) / Math.Max(1.0, Math.Max(bpmA, bpmB))
            End If
            If diffRel > epsRel Then
                Return
            End If

            Dim centerPos As Single = 0.5F
            If fichierAudioDeckB Is Nothing Then Return
            Dim totalB As Double = Math.Max(1.0, fichierAudioDeckB.TotalTime.TotalSeconds)
            Dim targetSec As Double = totalB * centerPos
            If bpmDeckB <= 0 Then Return
            Dim bpmEffectifB As Double = bpmDeckB * (1.0 + pitchDeckB)
            Dim gridB As New BeatGrid(bpmEffectifB, totalB)
            Dim nearestBeatB As Double = gridB.TrouverBeatLePlusProche(targetSec)
            Dim anciennePositionB As Double = fichierAudioDeckB.CurrentTime.TotalSeconds
            fichierAudioDeckB.CurrentTime = TimeSpan.FromSeconds(nearestBeatB)
            TrackBarPositionDeckB.Value = CInt(Math.Max(TrackBarPositionDeckB.Minimum, Math.Min(TrackBarPositionDeckB.Maximum, CInt(nearestBeatB))))
            LabelDureeDeckB.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckB.CurrentTime, fichierAudioDeckB.TotalTime)
            ' Nettoyer le timer de preview et reprendre la lecture principale si nécessaire
            Try
                If previewTimerDeckB IsNot Nothing Then
                    Try
                        previewTimerDeckB.Stop()
                    Catch
                    End Try
                    previewTimerDeckB = Nothing
                End If
            Catch
            End Try
            If dragWasPlayingDeckB AndAlso lecteurDeckB IsNot Nothing Then
                Try
                    lecteurDeckB.Play()
                    lectureEnCoursDeckB = True
                    enPauseDeckB = False
                    ButtonPlayDeckB.Text = LanguageManager.GetString("DJ_Button_Pause")
                Catch
                End Try
            End If
            isUserDraggingPositionB = False
            dragWasPlayingDeckB = False
        Catch
        End Try
    End Sub

    ' === Effets Deck A ===
    Private Sub CheckBoxPhaserDeckA_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxPhaserDeckA.CheckedChanged
        If phaserProviderDeckA IsNot Nothing Then
            phaserProviderDeckA.Enabled = CheckBoxPhaserDeckA.Checked
        End If
    End Sub

    Private Sub CheckBoxReverbDeckA_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxReverbDeckA.CheckedChanged
        If reverbProviderDeckA IsNot Nothing Then
            reverbProviderDeckA.Enabled = CheckBoxReverbDeckA.Checked
        End If
    End Sub

    Private Sub CheckBoxEchoDeckA_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxEchoDeckA.CheckedChanged
        If echoProviderDeckA IsNot Nothing Then
            echoProviderDeckA.Enabled = CheckBoxEchoDeckA.Checked
        End If
    End Sub

    ' === Effets Deck B ===
    Private Sub CheckBoxPhaserDeckB_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxPhaserDeckB.CheckedChanged
        If phaserProviderDeckB IsNot Nothing Then
            phaserProviderDeckB.Enabled = CheckBoxPhaserDeckB.Checked
        End If
    End Sub

    Private Sub CheckBoxReverbDeckB_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxReverbDeckB.CheckedChanged
        If reverbProviderDeckB IsNot Nothing Then
            reverbProviderDeckB.Enabled = CheckBoxReverbDeckB.Checked
        End If
    End Sub

    Private Sub CheckBoxEchoDeckB_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxEchoDeckB.CheckedChanged
        If echoProviderDeckB IsNot Nothing Then
            echoProviderDeckB.Enabled = CheckBoxEchoDeckB.Checked
        End If
    End Sub

    ' ========================================
    ' === PERSISTENCE DES AJUSTEMENTS DJ ===
    ' ========================================
    ' Ces méthodes gèrent la sauvegarde/chargement des ajustements DJ
    ' dans le fichier Son_Ajustement_DJ.txt (séparé de parametres.txt)
    ' Format: VolumeDeckA, VolumeDeckB, Crossfader, PitchDeckA, PitchDeckB
    ' ========================================

    ''' <summary>
    ''' Sauvegarde les ajustements DJ (volumes, crossfader, pitch) dans Son_Ajustement_DJ.txt
    ''' Appelé automatiquement lors des changements de TrackBar
    ''' </summary>
    Private Sub SauvegarderAjustementsDJ()
        Try
            Dim cheminAppData As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay")
            If Not Directory.Exists(cheminAppData) Then
                Directory.CreateDirectory(cheminAppData)
            End If

            Dim cheminFichier As String = Path.Combine(cheminAppData, "Son_Ajustement_DJ.txt")

            ' Format: 5 lignes (valeurs brutes des TrackBar)
            Using writer As New StreamWriter(cheminFichier, False, System.Text.Encoding.UTF8)
                writer.WriteLine($"VolumeDeckA={TrackBarVolumeDeckA.Value}")
                writer.WriteLine($"VolumeDeckB={TrackBarVolumeDeckB.Value}")
                writer.WriteLine($"Crossfader={TrackBarCrossfader.Value}")
                writer.WriteLine($"PitchDeckA={TrackBarPitchDeckA.Value}")
                writer.WriteLine($"PitchDeckB={TrackBarPitchDeckB.Value}")
            End Using
        Catch ex As Exception
            ' Silencieux: ne pas bloquer l'utilisateur si la sauvegarde échoue
            Debug.WriteLine($"Erreur sauvegarde ajustements DJ: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Charge les ajustements DJ depuis Son_Ajustement_DJ.txt
    ''' Appelé au démarrage de FormDJ_Load()
    ''' Si le fichier n'existe pas, utilise les valeurs par défaut (75, 75, 50, 100, 100)
    ''' </summary>
    Private Sub ChargerAjustementsDJ()
        Try
            Dim cheminAppData As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay")
            Dim cheminFichier As String = Path.Combine(cheminAppData, "Son_Ajustement_DJ.txt")

            If File.Exists(cheminFichier) Then
                ' Charger depuis le fichier
                Dim lignes As String() = File.ReadAllLines(cheminFichier, System.Text.Encoding.UTF8)

                For Each ligne As String In lignes
                    If ligne.Contains("="c) Then
                        Dim parts() As String = ligne.Split("="c)
                        If parts.Length = 2 Then
                            Dim cle As String = parts(0).Trim()
                            Dim valeur As String = parts(1).Trim()

                            Select Case cle
                                Case "VolumeDeckA"
                                    Dim vol As Integer
                                    If Integer.TryParse(valeur, vol) AndAlso vol >= 0 AndAlso vol <= 100 Then
                                        TrackBarVolumeDeckA.Value = vol
                                    End If
                                Case "VolumeDeckB"
                                    Dim vol As Integer
                                    If Integer.TryParse(valeur, vol) AndAlso vol >= 0 AndAlso vol <= 100 Then
                                        TrackBarVolumeDeckB.Value = vol
                                    End If
                                Case "Crossfader"
                                    Dim cf As Integer
                                    If Integer.TryParse(valeur, cf) AndAlso cf >= 0 AndAlso cf <= 100 Then
                                        TrackBarCrossfader.Value = cf
                                        crossfaderPosition = cf / 100.0F
                                    End If
                                Case "PitchDeckA"
                                    Dim pitch As Integer
                                    If Integer.TryParse(valeur, pitch) AndAlso pitch >= 92 AndAlso pitch <= 108 Then
                                        TrackBarPitchDeckA.Value = pitch
                                        pitchDeckA = (pitch - 100) / 100.0F
                                    End If
                                Case "PitchDeckB"
                                    Dim pitch As Integer
                                    If Integer.TryParse(valeur, pitch) AndAlso pitch >= 92 AndAlso pitch <= 108 Then
                                        TrackBarPitchDeckB.Value = pitch
                                        pitchDeckB = (pitch - 100) / 100.0F
                                    End If
                            End Select
                        End If
                    End If
                Next
            Else
                ' Fichier absent: créer avec valeurs par défaut
                SauvegarderAjustementsDJ()
            End If
        Catch ex As Exception
            ' Silencieux: en cas d'erreur, garder les valeurs par défaut déjà initialisées
            Debug.WriteLine($"Erreur chargement ajustements DJ: {ex.Message}")
        End Try
    End Sub

    ' === Bouton retour mode simple ===
    Private Sub ButtonRetourModeSimple_Click(sender As Object, e As EventArgs) Handles ButtonRetourModeSimple.Click
        ' Basculer directement en mode simple sans confirmation
        Try
            ' Désactiver le mode DJ et redémarrer
            ParametresGlobaux.ModeMixeurDJ = False

            ' Sauvegarder la configuration
            Dim cheminConfig As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "parametres.txt")
            If File.Exists(cheminConfig) Then
                Dim lignes = File.ReadAllLines(cheminConfig).ToList()
                For i = 0 To lignes.Count - 1
                    If lignes(i).StartsWith("ModeMixeurDJ=") Then
                        lignes(i) = "ModeMixeurDJ=False"
                        Exit For
                    End If
                Next
                File.WriteAllLines(cheminConfig, lignes)
            End If
        Catch
        End Try

        Try
            Application.Restart()
        Catch
        End Try
    End Sub

    ' === Bouton Paramètres ===
    Private Sub ButtonParametres_Click(sender As Object, e As EventArgs) Handles ButtonParametres.Click
        ' Ouvrir la fenêtre de paramètres
        Dim formParametres As New FormParametres()
        formParametres.Owner = Me ' Définir le formulaire DJ comme propriétaire
        Dim result = formParametres.ShowDialog()

        ' Ne rafraîchir que si le formulaire n'a pas été fermé par un changement de mode
        ' (dans ce cas, ce formulaire FormDJ aura été fermé et on ne doit rien faire)
        If Not Me.IsDisposed AndAlso Not Me.Disposing Then
            ' Rafraîchir la langue après fermeture (au cas où l'utilisateur a changé la langue)
            RefreshLanguage()
        End If
    End Sub

    ' === Bouton Quitter ===
    Private Sub ButtonQuitter_Click(sender As Object, e As EventArgs) Handles ButtonQuitter.Click
        ' Demander confirmation
        Dim result = MessageBox.Show(
            LanguageManager.GetString("Confirm_QuitApplication"),
            LanguageManager.GetString("Confirm_Title"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            ' Activer le flag de fermeture
            isClosing = True

            ' Sauvegarder les ajustements DJ avant de quitter
            Try
                ' Debug log: état avant calcul du chemin initial (FormDJ)
                Try
                    Dim debugFileDJ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "debug_repertoires.txt")
                    Directory.CreateDirectory(Path.GetDirectoryName(debugFileDJ))
                    Dim before = $"[{DateTime.Now:O}] FormDJ.BeforeOpen: dernierChoisi_DJ='{ParametresGlobaux.dernierRepertoireAjoutRepertoireChoisi_DJ}' dernierParentSaved_DJ='{ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ}' generalParent='{ParametresGlobaux.dernierRepertoireAjoutRepertoire}'{Environment.NewLine}"
                    File.AppendAllText(debugFileDJ, before)
                Catch
                End Try
                SauvegarderAjustementsDJ()
                SauvegarderPlaylistDJ()
            Catch ex As Exception
                Debug.WriteLine($"[QUIT] Erreur sauvegarde: {ex.Message}")
            End Try

            ' Fermer proprement l'application complète
            Application.Exit()
        End If
    End Sub

    ' === Timer de position (mise à jour TrackBars et labels) ===
    Private Sub timerPosition_Tick(sender As Object, e As EventArgs) Handles timerPosition.Tick
        ' Deck A
        If fichierAudioDeckA IsNot Nothing AndAlso lectureEnCoursDeckA Then
            Try
                ' Ne pas mettre à jour le TrackBar si l'utilisateur le manipule
                If Not isUserDraggingPositionA Then
                    Dim positionSecondes As Integer = CInt(fichierAudioDeckA.CurrentTime.TotalSeconds)
                    If TrackBarPositionDeckA.Maximum > 0 AndAlso positionSecondes <= TrackBarPositionDeckA.Maximum Then
                        TrackBarPositionDeckA.Value = positionSecondes
                    End If
                    ' Mettre à jour le label durée
                    LabelDureeDeckA.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckA.CurrentTime, fichierAudioDeckA.TotalTime)
                End If

                ' Vérifier fin de piste
                If fichierAudioDeckA.CurrentTime >= fichierAudioDeckA.TotalTime Then
                    ArreterDeckA()
                End If
            Catch ex As Exception
                ' Ignorer les erreurs de synchronisation
            End Try
        End If

        ' Mettre à jour VU-meter Deck A
        If meteringProviderDeckA IsNot Nothing Then
            VUMeterDeckA.Level = meteringProviderDeckA.Level
        End If

        ' Deck B
        If fichierAudioDeckB IsNot Nothing AndAlso lectureEnCoursDeckB Then
            Try
                ' Ne pas mettre à jour le TrackBar si l'utilisateur le manipule
                If Not isUserDraggingPositionB Then
                    Dim positionSecondes As Integer = CInt(fichierAudioDeckB.CurrentTime.TotalSeconds)
                    If TrackBarPositionDeckB.Maximum > 0 AndAlso positionSecondes <= TrackBarPositionDeckB.Maximum Then
                        TrackBarPositionDeckB.Value = positionSecondes
                    End If
                    ' Mettre à jour le label durée
                    LabelDureeDeckB.Text = String.Format(LanguageManager.GetString("DJ_Duration_Format"), fichierAudioDeckB.CurrentTime, fichierAudioDeckB.TotalTime)
                End If

                ' Vérifier fin de piste
                If fichierAudioDeckB.CurrentTime >= fichierAudioDeckB.TotalTime Then
                    ArreterDeckB()
                End If
            Catch ex As Exception
                ' Ignorer les erreurs de synchronisation
            End Try
        End If

        ' Mettre à jour VU-meter Deck B
        If meteringProviderDeckB IsNot Nothing Then
            VUMeterDeckB.Level = meteringProviderDeckB.Level
        End If

        ' Mettre à jour la position courante des waveforms si présents
        Try
            If waveformDeckA IsNot Nothing AndAlso fichierAudioDeckA IsNot Nothing Then
                waveformDeckA.CurrentPosition = CSng(fichierAudioDeckA.CurrentTime.TotalSeconds / Math.Max(1.0, fichierAudioDeckA.TotalTime.TotalSeconds))
                ' Ensure waveform content is scrolled so current position sits at center
                waveformDeckA.CenterViewOnCurrentPosition()
            End If
        Catch
        End Try
        Try
            If waveformDeckB IsNot Nothing AndAlso fichierAudioDeckB IsNot Nothing Then
                waveformDeckB.CurrentPosition = CSng(fichierAudioDeckB.CurrentTime.TotalSeconds / Math.Max(1.0, fichierAudioDeckB.TotalTime.TotalSeconds))
                waveformDeckB.CenterViewOnCurrentPosition()
            End If
        Catch
        End Try

        ' Si une piste vient de se terminer, lancer le purge du cache en arrière-plan
        Try
            If fichierAudioDeckA IsNot Nothing AndAlso Not lectureEnCoursDeckA AndAlso Not enPauseDeckA Then
                ' On considère qu'elle est arrêtée et on peut purger le cache
                Task.Run(Sub() PurgeOldWaveformCache())
            End If
        Catch
        End Try
        Try
            If fichierAudioDeckB IsNot Nothing AndAlso Not lectureEnCoursDeckB AndAlso Not enPauseDeckB Then
                Task.Run(Sub() PurgeOldWaveformCache())
            End If
        Catch
        End Try
    End Sub

    Private Sub PurgeOldWaveformCache()
        Try
            Dim cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "waveforms")
            If Not Directory.Exists(cacheDir) Then Return

            ' Purge conditionnée à la taille totale du dossier : supprimer les fichiers les plus anciens
            Dim maxSizeMB As Integer = 200
            Try
                maxSizeMB = Math.Max(1, ParametresGlobaux.WaveformCacheMaxSizeMB)
            Catch
            End Try
            Dim maxBytes As Long = CLng(maxSizeMB) * 1024L * 1024L

            Dim files = Directory.GetFiles(cacheDir, "*.dat")
            If files Is Nothing OrElse files.Length = 0 Then Return

            Dim fileInfos = New System.Collections.Generic.List(Of FileInfo)()
            Dim totalBytes As Long = 0
            For Each f In files
                Try
                    Dim fi = New FileInfo(f)
                    fileInfos.Add(fi)
                    totalBytes += fi.Length
                Catch
                End Try
            Next

            If totalBytes <= maxBytes Then Return

            ' Trier par date de modification (les plus vieux en premier)
            fileInfos.Sort(Function(a, b) a.LastWriteTimeUtc.CompareTo(b.LastWriteTimeUtc))

            For Each fi In fileInfos
                Try
                    Dim len = fi.Length
                    fi.Delete()
                    totalBytes -= len
                    If totalBytes <= maxBytes Then Exit For
                Catch
                    ' Ignorer les fichiers verrouillés et continuer
                End Try
            Next
        Catch
        End Try
    End Sub

    Private Sub waveformTimer_Tick(sender As Object, e As EventArgs) Handles waveformTimer.Tick
        Try
            If waveformDeckA IsNot Nothing AndAlso Not waveformDeckA.IsDisposed Then
                waveformDeckA.Invalidate()
            End If
        Catch
        End Try
        Try
            If waveformDeckB IsNot Nothing AndAlso Not waveformDeckB.IsDisposed Then
                waveformDeckB.Invalidate()
            End If
        Catch
        End Try
    End Sub

    ' === Nettoyage à la fermeture ===
    Private Sub FormDJ_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Indiquer que le form est en cours de fermeture
        isClosing = True

        ' CRITIQUE : Arrêter le BeatSyncEngine AVANT de libérer les ressources audio
        If beatSyncEngine IsNot Nothing Then
            beatSyncEngine.SyncActifDeckA = False
            beatSyncEngine.SyncActifDeckB = False
            beatSyncEngine.Arreter()
            Debug.WriteLine("[CLOSE] BeatSyncEngine arrêté")
        End If

        ' Arrêter le timer
        If timerPosition IsNot Nothing Then
            timerPosition.Stop()
            timerPosition.Dispose()
        End If

        ' Arrêter et libérer les ressources
        ArreterDeckA()
        ArreterDeckB()

        If lecteurDeckA IsNot Nothing Then
            lecteurDeckA.Dispose()
            lecteurDeckA = Nothing
        End If
        If fichierAudioDeckA IsNot Nothing Then
            fichierAudioDeckA.Dispose()
            fichierAudioDeckA = Nothing
        End If

        If lecteurDeckB IsNot Nothing Then
            lecteurDeckB.Dispose()
            lecteurDeckB = Nothing
        End If
        If fichierAudioDeckB IsNot Nothing Then
            fichierAudioDeckB.Dispose()
            fichierAudioDeckB = Nothing
        End If

        ' Arrêter l'enregistrement si actif
        If enregistrementEnCours AndAlso djRecorder IsNot Nothing Then
            Try
                djRecorder.ArreterEnregistrement()
                djRecorder.Dispose()
                djRecorder = Nothing
            Catch ex As Exception
                Debug.WriteLine($"[REC] Erreur arrêt enregistrement: {ex.Message}")
            End Try
        End If

        ' Nettoyer les dossiers temporaires restants dans les répertoires usuels
        Try
            ' Supprimer temp dans le repertoire par défaut et les mémoires utilisateur
            ParametresGlobaux.SupprimerTempRestantsDans(ParametresGlobaux.repertoireParDefaut)
            If Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ) Then
                ParametresGlobaux.SupprimerTempRestantsDans(ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ)
            End If
            If Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoireAjoutRepertoire) Then
                ParametresGlobaux.SupprimerTempRestantsDans(ParametresGlobaux.dernierRepertoireAjoutRepertoire)
            End If
        Catch
        End Try

        ' Arrêter le timer d'enregistrement
        If timerEnregistrement IsNot Nothing Then
            timerEnregistrement.Stop()
            timerEnregistrement.Dispose()
        End If
    End Sub

    ' ========================================
    ' GESTION DE LA PLAYLIST DJ
    ' ========================================

    ' UI status strip for DJ playlist metadata progress
    Private StatusStripPlaylist As StatusStrip = Nothing

    ' Cancellation token source for DJ metadata processing
    Private metadataCancellationTokenSource_DJ As Threading.CancellationTokenSource = Nothing

    Private Function IsMetadataCancellationRequested_DJ() As Boolean
        Try
            Return (metadataCancellationTokenSource_DJ IsNot Nothing) AndAlso metadataCancellationTokenSource_DJ.IsCancellationRequested
        Catch
            Return False
        End Try
    End Function

    ' Counters for DJ metadata progress
    Private metadataTotal_DJ As Integer = 0
    Private metadataDone_DJ As Integer = 0

    Private Sub InitMetadataProgressDJ(totalItems As Integer)
        Try
            metadataTotal_DJ = totalItems
            metadataDone_DJ = 0
            If StatusStripPlaylist Is Nothing Then
                Dim ss As New StatusStrip()
                ss.Name = "StatusStripPlaylist"
                Dim lbl As New ToolStripStatusLabel()
                lbl.Name = "ToolStripStatusLabel_MetadataDJ"
                lbl.Text = ""
                ss.Items.Add(lbl)
                Dim pb As New ToolStripProgressBar()
                pb.Name = "ToolStripProgressBar_MetadataDJ"
                pb.Minimum = 0
                pb.Maximum = Math.Max(1, totalItems)
                pb.Value = 0
                pb.AutoSize = False
                pb.Size = New Size(200, 16)
                ss.Items.Add(pb)
                Dim btn As New ToolStripButton()
                btn.Name = "ToolStripButton_MetadataCancelDJ"
                btn.Text = "Annuler"
                AddHandler btn.Click, Sub()
                                         Try
                                             RequestCancelMetadataProcessingDJ()
                                             btn.Enabled = False
                                         Catch
                                         End Try
                                     End Sub
                ss.Items.Add(btn)
                Me.Controls.Add(ss)
                ss.BringToFront()
                StatusStripPlaylist = ss
            End If

            Dim label = CType(StatusStripPlaylist.Items.OfType(Of ToolStripItem)().FirstOrDefault(Function(it) it.Name = "ToolStripStatusLabel_MetadataDJ"), ToolStripStatusLabel)
            If label Is Nothing Then
                label = New ToolStripStatusLabel("ToolStripStatusLabel_MetadataDJ")
                StatusStripPlaylist.Items.Add(label)
            End If
            label.Text = String.Format("Chargement playlist DJ: 0/{0}", metadataTotal_DJ)
            Try
                Dim pb = CType(StatusStripPlaylist.Items.OfType(Of ToolStripItem)().FirstOrDefault(Function(it) it.Name = "ToolStripProgressBar_MetadataDJ"), ToolStripProgressBar)
                If pb IsNot Nothing Then
                    pb.Maximum = Math.Max(1, metadataTotal_DJ)
                    pb.Value = 0
                End If
            Catch
            End Try
        Catch
        End Try
    End Sub

    Private Sub UpdateMetadataProgressDJ(done As Integer, total As Integer)
        Try
            If StatusStripPlaylist Is Nothing Then Return
            Dim label = CType(StatusStripPlaylist.Items.OfType(Of ToolStripItem)().FirstOrDefault(Function(it) it.Name = "ToolStripStatusLabel_MetadataDJ"), ToolStripStatusLabel)
            If label Is Nothing Then Return
            label.Text = String.Format("Chargement playlist DJ: {0}/{1}", done, total)
            Try
                Dim pb = CType(StatusStripPlaylist.Items.OfType(Of ToolStripItem)().FirstOrDefault(Function(it) it.Name = "ToolStripProgressBar_MetadataDJ"), ToolStripProgressBar)
                If pb IsNot Nothing Then
                    pb.Value = Math.Min(pb.Maximum, done)
                End If
            Catch
            End Try
            Try
                If done >= total OrElse IsMetadataCancellationRequested_DJ() Then
                    If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                        Me.BeginInvoke(Sub()
                                           Try
                                               If StatusStripPlaylist IsNot Nothing Then
                                                   Try
                                                       Me.Controls.Remove(StatusStripPlaylist)
                                                       StatusStripPlaylist.Dispose()
                                                   Catch
                                                   End Try
                                                   StatusStripPlaylist = Nothing
                                               End If
                                           Catch
                                           End Try
                                           metadataTotal_DJ = 0
                                           metadataDone_DJ = 0
                                       End Sub)
                    End If
                End If
            Catch
            End Try
        Catch
        End Try
    End Sub

    Private Sub RequestCancelMetadataProcessingDJ()
        Try
            If metadataCancellationTokenSource_DJ IsNot Nothing Then
                Try
                    metadataCancellationTokenSource_DJ.Cancel()
                Catch
                End Try
            End If
        Catch
        End Try
    End Sub

    Private Sub DemarrerTraitementMetadonneesEnArrierePlanDJ(batchEntries As List(Of Tuple(Of String, String, String)))
        Try
            If batchEntries Is Nothing OrElse batchEntries.Count = 0 Then Return

            Dim maxDegree As Integer = Math.Max(1, Math.Min(Environment.ProcessorCount, 4))
            Dim semaphore As New Threading.SemaphoreSlim(maxDegree)

            Dim total = batchEntries.Count
            For Each entry In batchEntries
                Dim cheminLocal = entry.Item1
                Dim bpmExistantLocal = entry.Item2
                Dim dureeExistanteLocal = entry.Item3
                If IsMetadataCancellationRequested_DJ() Then
                    Exit For
                End If

                Threading.ThreadPool.QueueUserWorkItem(Sub()
                                                           semaphore.Wait()
                                                           Try
                                                               If String.IsNullOrWhiteSpace(cheminLocal) OrElse Not File.Exists(cheminLocal) Then
                                                                   Dim currentDone = Interlocked.Increment(metadataDone_DJ)
                                                                   Try
                                                                       Me.BeginInvoke(Sub() UpdateMetadataProgressDJ(currentDone, metadataTotal_DJ))
                                                                   Catch
                                                                   End Try
                                                                   Return
                                                               End If

                                                               Dim needDuree As Boolean = String.IsNullOrWhiteSpace(dureeExistanteLocal) OrElse dureeExistanteLocal = "--:--"
                                                               Dim needBpm As Boolean = String.IsNullOrWhiteSpace(bpmExistantLocal)
                                                               If Not needDuree AndAlso Not needBpm Then
                                                                   Dim currentDone = Interlocked.Increment(metadataDone_DJ)
                                                                   Try
                                                                       Me.BeginInvoke(Sub() UpdateMetadataProgressDJ(currentDone, metadataTotal_DJ))
                                                                   Catch
                                                                   End Try
                                                                   Return
                                                               End If

                                                               Dim newDuree As String = Nothing
                                                               Dim newBpm As String = Nothing

                                                               If IsMetadataCancellationRequested_DJ() Then Return

                                                               If needDuree Then
                                                                   Try
                                                                       Using reader As New AudioFileReader(cheminLocal)
                                                                           Dim ts = reader.TotalTime
                                                                           newDuree = String.Format("{0:D2}:{1:D2}", CInt(ts.TotalMinutes), ts.Seconds)
                                                                       End Using
                                                                   Catch
                                                                   End Try
                                                               End If

                                                               If needBpm Then
                                                                   Try
                                                                       Dim bpmMetadata = BPMMetadataManager.LireBPMPrecisDepuisMetadonnees(cheminLocal)
                                                                       If bpmMetadata > 0 Then
                                                                           newBpm = bpmMetadata.ToString("F2", Globalization.CultureInfo.InvariantCulture)
                                                                       End If
                                                                   Catch
                                                                   End Try
                                                               End If

                                                               If String.IsNullOrWhiteSpace(newDuree) AndAlso String.IsNullOrWhiteSpace(newBpm) Then
                                                                   Dim currentDone = Interlocked.Increment(metadataDone_DJ)
                                                                   Try
                                                                       Me.BeginInvoke(Sub() UpdateMetadataProgressDJ(currentDone, metadataTotal_DJ))
                                                                   Catch
                                                                   End Try
                                                                   Return
                                                               End If

                                                               If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                                                   Me.BeginInvoke(Sub()
                                                                                      Try
                                                                                          Dim targetItem As ListViewItem = Nothing
                                                                                          For Each lvItem As ListViewItem In ListViewPlaylist.Items
                                                                                              Dim tagChemin As String = ""
                                                                                              If TypeOf lvItem.Tag Is Dictionary(Of String, Object) Then
                                                                                                  Dim existingTag = DirectCast(lvItem.Tag, Dictionary(Of String, Object))
                                                                                                  If existingTag.ContainsKey("Chemin") Then
                                                                                                      tagChemin = existingTag("Chemin")?.ToString()
                                                                                                  End If
                                                                                              ElseIf TypeOf lvItem.Tag Is String Then
                                                                                                  tagChemin = lvItem.Tag.ToString()
                                                                                              End If

                                                                                              If String.Equals(tagChemin, cheminLocal, StringComparison.OrdinalIgnoreCase) Then
                                                                                                  targetItem = lvItem
                                                                                                  Exit For
                                                                                              End If
                                                                                          Next

                                                                                          If targetItem Is Nothing Then Return

                                                                                          If Not String.IsNullOrWhiteSpace(newDuree) Then
                                                                                              targetItem.SubItems(3).Text = newDuree
                                                                                          End If

                                                                                          If Not String.IsNullOrWhiteSpace(newBpm) Then
                                                                                              targetItem.SubItems(2).Text = newBpm

                                                                                              Dim tagDict As Dictionary(Of String, Object)
                                                                                              If TypeOf targetItem.Tag Is Dictionary(Of String, Object) Then
                                                                                                  tagDict = DirectCast(targetItem.Tag, Dictionary(Of String, Object))
                                                                                              Else
                                                                                                  tagDict = New Dictionary(Of String, Object) From {
                                                                                                      {"Chemin", cheminLocal}
                                                                                                  }
                                                                                              End If

                                                                                              Dim bpmValue As Double = 0
                                                                                              If Double.TryParse(newBpm, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, bpmValue) Then
                                                                                                  tagDict("BPM") = bpmValue
                                                                                              End If
                                                                                              targetItem.Tag = tagDict
                                                                                          End If
                                                                                      Catch
                                                                                      End Try
                                                                                  End Sub)
                                                               End If

                                                               ' Update cache asynchronously
                                                               Try
                                                                   Threading.ThreadPool.QueueUserWorkItem(Sub()
                                                                                                              Try
                                                                                                                  If Not String.IsNullOrEmpty(cheminLocal) Then
                                                                                                                      MetadataCache.UpdateCache(cheminLocal, If(newDuree, ""), If(newBpm, ""))
                                                                                                                  End If
                                                                                                              Catch
                                                                                                              End Try
                                                                                                          End Sub)
                                                               Catch
                                                               End Try

                                                               Dim currentDoneFinal = Interlocked.Increment(metadataDone_DJ)
                                                               Try
                                                                   Me.BeginInvoke(Sub() UpdateMetadataProgressDJ(currentDoneFinal, metadataTotal_DJ))
                                                               Catch
                                                               End Try
                                                           Catch
                                                           Finally
                                                               semaphore.Release()
                                                           End Try
                                                       End Sub)
            Next
        Catch
        End Try
    End Sub

    ' === Charger la playlist depuis playlistDJ.txt ===
    Private Sub ChargerPlaylistDJ()
        Try
            Dim t As New Threading.Thread(Sub()
                                              Try
                                                  Dim fichierPlaylist = Path.Combine(
                                                      Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                                      "AudioPlay",
                                                      "playlistDJ.txt")
                                                  If Not File.Exists(fichierPlaylist) Then Return

                                                  Dim lignes = File.ReadAllLines(fichierPlaylist)
                                                  Dim entries As New List(Of Tuple(Of String, String, String))()
                                                  For Each ligne In lignes
                                                      If String.IsNullOrWhiteSpace(ligne) Then Continue For
                                                      Dim parties = ligne.Split("|"c)
                                                      If parties.Length >= 2 Then
                                                          Dim chemin = parties(0)
                                                          Dim nom = parties(1)
                                                          Dim bpm = If(parties.Length >= 3, parties(2), "")
                                                          Dim duree = If(parties.Length >= 4, parties(3), "")
                                                          If File.Exists(chemin) Then
                                                              entries.Add(Tuple.Create(chemin, bpm, duree))
                                                          End If
                                                      End If
                                                  Next

                                                  Dim batchSize As Integer = 100
                                                  Dim firstBatchSize As Integer = Math.Min(20, batchSize)
                                                  Dim index As Integer = 0

                                                  ' Initialize DJ cancellation token and progress UI
                                                  Try
                                                      If metadataCancellationTokenSource_DJ IsNot Nothing Then
                                                          Try
                                                              metadataCancellationTokenSource_DJ.Dispose()
                                                          Catch
                                                          End Try
                                                      End If
                                                      metadataCancellationTokenSource_DJ = New Threading.CancellationTokenSource()
                                                  Catch
                                                  End Try

                                                  If entries.Count > 0 Then
                                                      ' Init progress UI on UI thread
                                                      If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                                          Me.BeginInvoke(Sub()
                                                                             Try
                                                                                 InitMetadataProgressDJ(entries.Count)
                                                                             Catch
                                                                             End Try
                                                                         End Sub)
                                                      End If

                                                      Dim firstBatch = entries.Take(firstBatchSize).ToList()
                                                      If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                                          Me.BeginInvoke(Sub()
                                                                             Try
                                                                                 ListViewPlaylist.BeginUpdate()
                                                                                 For Each entry In firstBatch
                                                                                         AjouterItemLightDJ(entry.Item1, Path.GetFileName(entry.Item1), entry.Item2, entry.Item3)
                                                                                 Next
                                                                             Catch
                                                                             Finally
                                                                                 Try
                                                                                     ListViewPlaylist.EndUpdate()
                                                                                 Catch
                                                                                 End Try
                                                                             End Try

                                                                             Try
                                                                                 MettreAJourNumerotationDJ()
                                                                                 DemarrerTraitementMetadonneesEnArrierePlanDJ(firstBatch)
                                                                             Catch
                                                                             End Try
                                                                         End Sub)
                                                      End If
                                                      index += firstBatchSize
                                                  End If

                                                  While index < entries.Count
                                                      Dim batch As New List(Of Tuple(Of String, String, String))()
                                                      Dim maxIndex As Integer = Math.Min(index + batchSize, entries.Count)
                                                      For i As Integer = index To maxIndex - 1
                                                          batch.Add(entries(i))
                                                      Next

                                                      Try
                                                          If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                                              Me.BeginInvoke(Sub()
                                                                                 Try
                                                                                     ListViewPlaylist.BeginUpdate()
                                                                                     For Each entry In batch
                                                                                         AjouterItemLightDJ(entry.Item1, Path.GetFileName(entry.Item1), entry.Item2, entry.Item3)
                                                                                     Next
                                                                                 Catch
                                                                                 Finally
                                                                                     Try
                                                                                         ListViewPlaylist.EndUpdate()
                                                                                     Catch
                                                                                     End Try
                                                                                 End Try

                                                                             Try
                                                                                     MettreAJourNumerotationDJ()
                                                                                 Catch
                                                                                 End Try
                                                                                 Try
                                                                                     DemarrerTraitementMetadonneesEnArrierePlanDJ(batch)
                                                                                 Catch
                                                                                 End Try
                                                                             End Sub)
                                                          End If
                                                      Catch
                                                      End Try

                                                      Try
                                                          Threading.ThreadPool.QueueUserWorkItem(Sub()
                                                                                                     For Each cacheEntry In batch
                                                                                                         Try
                                                                                                             MetadataCache.GetCached(cacheEntry.Item1)
                                                                                                         Catch
                                                                                                         End Try
                                                                                                     Next
                                                                                                 End Sub)
                                                      Catch
                                                      End Try

                                                      index += batchSize
                                                      Threading.Thread.Sleep(10)
                                                  End While
                                              Catch
                                              End Try
                                          End Sub)
            t.IsBackground = True
            t.Start()
        Catch
        End Try
    End Sub

    ' === Sauvegarder la playlist dans playlistDJ.txt ===
    Private Sub SauvegarderPlaylistDJ()
        Dim fichierPlaylist = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AudioPlay",
            "playlistDJ.txt")
        Try
            ' Créer le répertoire s'il n'existe pas
            Dim dossier As String = Path.GetDirectoryName(fichierPlaylist)
            If Not Directory.Exists(dossier) Then
                Directory.CreateDirectory(dossier)
            End If

            Dim lignes As New List(Of String)
            For Each item As ListViewItem In ListViewPlaylist.Items
                ' Extraire le chemin du Tag
                Dim chemin As String = ""
                If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                    Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                    If tagDict.ContainsKey("Chemin") Then
                        chemin = tagDict("Chemin")?.ToString()
                    End If
                ElseIf TypeOf item.Tag Is String Then
                    chemin = item.Tag.ToString()
                End If

                Dim nom = item.SubItems(1).Text
                Dim bpm = item.SubItems(2).Text
                Dim duree = item.SubItems(3).Text
                lignes.Add($"{chemin}|{nom}|{bpm}|{duree}")
            Next
            File.WriteAllLines(fichierPlaylist, lignes)
        Catch ex As Exception
            ' Ignorer les erreurs de sauvegarde
        End Try
    End Sub

    ' === Ajouter un fichier à la liste DJ ===
    Private Sub AjouterFichierAListeDJ(chemin As String, Optional nom As String = "", Optional bpmStr As String = "", Optional dureeStr As String = "")
        ' Créer un ListViewItem avec 4 colonnes : #, Chanson, BPM, Durée
        Dim item As New ListViewItem("")
        If String.IsNullOrEmpty(nom) Then
            nom = Path.GetFileNameWithoutExtension(chemin)
        End If
        item.SubItems.Add(nom)

        ' Si BPM n'est pas fourni, essayer de le lire depuis les métadonnées
        If String.IsNullOrEmpty(bpmStr) Then
            Try
                Dim bpmMetadata = BPMMetadataManager.LireBPMPrecisDepuisMetadonnees(chemin)
                If bpmMetadata > 0 Then
                    bpmStr = bpmMetadata.ToString("F2")
                End If
            Catch
                ' Ignorer les erreurs de lecture BPM
            End Try
        End If

        item.SubItems.Add(bpmStr)

        If String.IsNullOrEmpty(dureeStr) Then
            Try
                Using reader As New AudioFileReader(chemin)
                    dureeStr = reader.TotalTime.ToString("mm\:ss")
                End Using
            Catch
                dureeStr = "--:--"
            End Try
        End If
        item.SubItems.Add(dureeStr)

        ' Stocker le chemin dans le Tag
        Dim tagDict As New Dictionary(Of String, Object) From {
            {"Chemin", chemin}
        }
        If Not String.IsNullOrEmpty(bpmStr) Then
            Dim bpmValue As Double
            If Double.TryParse(bpmStr, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, bpmValue) Then
                tagDict.Add("BPM", bpmValue)
            End If
        End If
        item.Tag = tagDict

        ListViewPlaylist.Items.Add(item)
    End Sub

    ' Ajoute un item léger dans la ListViewPlaylist sans ouvrir le fichier audio (rapide pour affichage initial)
    Private Sub AjouterItemLightDJ(chemin As String, nomFichier As String, bpm As String, duree As String)
        Try
            Dim newItem As New ListViewItem()
            newItem.Text = "" ' Colonne Num (remplie par MettreAJourNumerotationDJ)
            newItem.SubItems.Add(nomFichier) ' Colonne Chanson
            newItem.SubItems.Add(If(String.IsNullOrWhiteSpace(bpm), "", bpm)) ' Colonne BPM
            newItem.SubItems.Add(If(String.IsNullOrWhiteSpace(duree), "--:--", duree)) ' Colonne Durée

            Dim tagDict As New Dictionary(Of String, Object) From {
                {"Chemin", chemin}
            }

            Dim bpmValue As Double = 0
            If Not String.IsNullOrWhiteSpace(bpm) AndAlso Double.TryParse(bpm, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, bpmValue) Then
                tagDict("BPM") = bpmValue
            End If

            newItem.Tag = tagDict
            ListViewPlaylist.Items.Add(newItem)
        Catch
            ' Ignorer les erreurs d'ajout
        End Try
    End Sub

    ' === Mettre à jour la numérotation de la playlist ===
    Private Sub MettreAJourNumerotationDJ()
        For i = 0 To ListViewPlaylist.Items.Count - 1
            ListViewPlaylist.Items(i).SubItems(0).Text = (i + 1).ToString()
        Next
    End Sub

    ' Déplacer la sélection dans la ListViewPlaylist (déplacement d'un index)
    Private Sub DeplacerSelectionListViewDJ(delta As Integer)
        If ListViewPlaylist.SelectedIndices.Count = 0 Then Return
        Dim idx As Integer = ListViewPlaylist.SelectedIndices(0)
        Dim newIdx As Integer = idx + delta
        If newIdx < 0 OrElse newIdx >= ListViewPlaylist.Items.Count Then Return
        Dim item As ListViewItem = ListViewPlaylist.Items(idx)
        ListViewPlaylist.Items.RemoveAt(idx)
        ListViewPlaylist.Items.Insert(newIdx, item)
        ListViewPlaylist.SelectedItems.Clear()
        ListViewPlaylist.Items(newIdx).Selected = True
        ListViewPlaylist.Items(newIdx).EnsureVisible()
        MettreAJourNumerotationDJ()
        SauvegarderPlaylistDJ()
    End Sub

    ' Supprimer la sélection dans la ListViewPlaylist (avec confirmation optionnelle)
    Private Sub SupprimerDeListeDJ()
        If ListViewPlaylist.SelectedItems.Count = 0 Then Return
        If ParametresGlobaux.ConfirmerEffacementChansons Then
            Dim rep = MessageBox.Show(LanguageManager.GetString("Playlist_DeleteConfirm_Message"),
                                       LanguageManager.GetString("Confirmation_Title"),
                                       MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If rep <> DialogResult.Yes Then Return
        End If
        Dim cheminsASupprimer As New List(Of String)
        For Each item As ListViewItem In ListViewPlaylist.SelectedItems
            Dim chemin = ExtraireCheminDepuisItemDJ(item)
            If Not String.IsNullOrEmpty(chemin) Then cheminsASupprimer.Add(chemin)
        Next
        If cheminsASupprimer.Contains(cheminActuelDeckA) Then ArreterDeckA()
        If cheminsASupprimer.Contains(cheminActuelDeckB) Then ArreterDeckB()
        For Each item As ListViewItem In ListViewPlaylist.SelectedItems.Cast(Of ListViewItem).ToList()
            ListViewPlaylist.Items.Remove(item)
        Next
        MettreAJourNumerotationDJ()
        SauvegarderPlaylistDJ()
    End Sub

    ' Extraire le chemin stocké dans l'item de la playlist DJ
    Private Function ExtraireCheminDepuisItemDJ(item As ListViewItem) As String
        If item Is Nothing Then Return String.Empty
        If item.Tag Is Nothing Then Return String.Empty
        If TypeOf item.Tag Is Dictionary(Of String, Object) Then
            Dim d = DirectCast(item.Tag, Dictionary(Of String, Object))
            If d.ContainsKey("Chemin") Then Return d("Chemin").ToString()
        ElseIf TypeOf item.Tag Is String Then
            Return item.Tag.ToString()
        End If
        Return String.Empty
    End Function

    ' Arrêter tous les decks
    Private Sub ButtonStopAllDecks()
        ArreterDeckA()
        ArreterDeckB()
    End Sub

    ' === Bouton AJOUTER : Menu contextuel avec Fichier/Répertoire ===
    Private Sub ButtonAjouterPiste_Click(sender As Object, e As EventArgs) Handles ButtonAjouterPiste.Click
        Dim menuAjout As New ContextMenuStrip()

        ' Option 1 : Ajout d'un fichier
        Dim menuItemFichier As New ToolStripMenuItem(LanguageManager.GetString("DJ_Menu_AddFile"))
        AddHandler menuItemFichier.Click, Sub() AjouterFichierDJ()

        ' Option 2 : Ajout d'un répertoire
        Dim menuItemRepertoire As New ToolStripMenuItem(LanguageManager.GetString("DJ_Menu_AddFolder"))
        AddHandler menuItemRepertoire.Click, Sub() AjouterRepertoireDJ()

        ' Ajouter les options au menu
        menuAjout.Items.Add(menuItemFichier)
        menuAjout.Items.Add(menuItemRepertoire)

        ' Afficher le menu sous le bouton
        menuAjout.Show(ButtonAjouterPiste, New Point(0, ButtonAjouterPiste.Height))
    End Sub

    ' === Ajouter un fichier ===
    Private Sub AjouterFichierDJ()
        Using ofd As New OpenFileDialog With {
            .Filter = LanguageManager.GetString("DJ_Filter_Audio"),
            .Multiselect = True,
            .Title = LanguageManager.GetString("DJ_Dialog_AddAudioFiles"),
            .RestoreDirectory = True
        }
            ' Déterminer le répertoire initial pour l'ajout de fichiers (strictement DJ)
            Dim initialFileDir As String = Nothing
            If Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoireAjoutFichier_DJ) AndAlso Directory.Exists(ParametresGlobaux.dernierRepertoireAjoutFichier_DJ) Then
                initialFileDir = ParametresGlobaux.dernierRepertoireAjoutFichier_DJ
            ElseIf Not String.IsNullOrEmpty(ParametresGlobaux.repertoireParDefaut) AndAlso Directory.Exists(ParametresGlobaux.repertoireParDefaut) Then
                initialFileDir = ParametresGlobaux.repertoireParDefaut
            End If
            If Not String.IsNullOrEmpty(initialFileDir) Then
                Try
                    ofd.InitialDirectory = initialFileDir
                    ' Forcer CurrentDirectory pour contourner les comportements Windows imprévisibles
                    Environment.CurrentDirectory = initialFileDir
                Catch
                End Try
            End If

            ' Utiliser directement le répertoire initial pour l'OpenFileDialog (éviter d'ouvrir le dossier temporaire lui-même)
            Dim tmpFileFolder As String = Nothing
            If Not String.IsNullOrEmpty(initialFileDir) AndAlso Directory.Exists(initialFileDir) Then
                Try
                    ofd.InitialDirectory = initialFileDir
                    Try
                        Environment.CurrentDirectory = initialFileDir
                    Catch
                    End Try
                    ' Debug log: initialFileDir utilisé pour AjouterFichierDJ
                    Try
                        Dim debugFileDJ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "debug_repertoires.txt")
                        Dim dbg = $"[{DateTime.Now:O}] FormDJ.AddFile.Debug: initialFileDir='{initialFileDir}' tmpFileFolder='(none)'{Environment.NewLine}"
                        File.AppendAllText(debugFileDJ, dbg)
                    Catch
                    End Try
                Catch
                    ofd.InitialDirectory = initialFileDir
                End Try
            End If

            If ofd.ShowDialog() = DialogResult.OK Then
                If ofd.FileNames IsNot Nothing AndAlso ofd.FileNames.Length > 0 Then
                    ' Mémoriser le choix dans la mémoire DJ uniquement (ne pas modifier la mémoire générale)
                    Dim chosen = Path.GetDirectoryName(ofd.FileNames(0))
                    If Not String.IsNullOrEmpty(chosen) Then
                        ParametresGlobaux.dernierRepertoireAjoutFichier_DJ = chosen
                        Try
                            ParametresGlobauxHelpers.EcrireCleParametres("DernierRepertoireAjoutFichier_DJ", chosen)
                        Catch
                        End Try
                    End If
                    ' Log après sauvegarde du choix de fichier (FormDJ)
                    Try
                        Dim debugFileDJ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "debug_repertoires.txt")
                        Dim after = $"[{DateTime.Now:O}] FormDJ.AddFile.AfterSave: chosen='{chosen}' tmpFileFolder='{tmpFileFolder}' ParametresGlobaux.dernierRepertoireAjoutFichier_DJ='{ParametresGlobaux.dernierRepertoireAjoutFichier_DJ}'{Environment.NewLine}"
                        File.AppendAllText(debugFileDJ, after)
                    Catch
                    End Try
                End If

                For Each fichier In ofd.FileNames
                    AjouterFichierAListeDJ(fichier)
                Next
                MettreAJourNumerotationDJ()
                SauvegarderPlaylistDJ()
            End If
        End Using
    End Sub

    ' === Ajouter un répertoire ===
    Private Sub AjouterRepertoireDJ()
        Using fbd As New FolderBrowserDialog With {
            .Description = LanguageManager.GetString("DJ_Dialog_SelectFolder"),
            .ShowNewFolderButton = False
        }
            ' Déterminer le chemin initial à utiliser pour le dialogue DJ
            Dim initialPath As String = Nothing
            Try
                If Not String.IsNullOrEmpty(ParametresGlobaux.avantDernierRepertoireAjoutRepertoire_DJ) AndAlso Directory.Exists(ParametresGlobaux.avantDernierRepertoireAjoutRepertoire_DJ) Then
                    initialPath = ParametresGlobaux.avantDernierRepertoireAjoutRepertoire_DJ
                ElseIf Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ) AndAlso Directory.Exists(ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ) Then
                    ' La valeur _DJ stocke déjà le parent calculé ; l'utiliser directement (éviter de remonter encore)
                    initialPath = ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ
                ElseIf Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoireAjoutRepertoire) AndAlso Directory.Exists(ParametresGlobaux.dernierRepertoireAjoutRepertoire) Then
                    ' La valeur générale stocke déjà le parent ; l'utiliser directement
                    initialPath = ParametresGlobaux.dernierRepertoireAjoutRepertoire
                ElseIf Not String.IsNullOrEmpty(ParametresGlobaux.repertoireParDefaut) AndAlso Directory.Exists(ParametresGlobaux.repertoireParDefaut) Then
                    initialPath = ParametresGlobaux.repertoireParDefaut
                End If
            Catch
            End Try

            ' Préserver le répertoire courant et créer un dossier temporaire dans le parent voulu
            Dim cwd = Environment.CurrentDirectory
            Dim tmpFolder As String = Nothing
            Try
                If Not String.IsNullOrEmpty(initialPath) AndAlso Directory.Exists(initialPath) Then
                    tmpFolder = Path.Combine(initialPath, ".AudioPlayTmp_" & Guid.NewGuid().ToString("N"))
                    Directory.CreateDirectory(tmpFolder)
                    fbd.SelectedPath = tmpFolder
                    Environment.CurrentDirectory = tmpFolder
                    ' Debug log: tmpFolder créé
                    Try
                        Dim debugFileDJ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "debug_repertoires.txt")
                        Dim msgTmp = $"[{DateTime.Now:O}] FormDJ.TmpCreated: tmpFolder='{tmpFolder}' initialPath='{initialPath}'{Environment.NewLine}"
                        File.AppendAllText(debugFileDJ, msgTmp)
                    Catch
                    End Try
                End If
            Catch
                tmpFolder = Nothing
            End Try

            If fbd.ShowDialog() = DialogResult.OK Then
                ' Mémoriser et sauvegarder le répertoire choisi
                Dim chosen = fbd.SelectedPath
                ' Conserver l'avant-dernier DJ afin de restaurer le parent au prochain affichage DJ
                Try
                    ParametresGlobaux.avantDernierRepertoireAjoutRepertoire_DJ = ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ
                Catch
                End Try

                ' Conserver aussi le répertoire choisi (non transformé)
                Try
                    ParametresGlobaux.dernierRepertoireAjoutRepertoireChoisi_DJ = chosen
                Catch
                End Try

                Dim toSave As String = chosen
                Try
                    Dim parent = Directory.GetParent(chosen)
                    If parent IsNot Nothing AndAlso Directory.Exists(parent.FullName) Then
                        toSave = parent.FullName
                    End If
                Catch
                    toSave = chosen
                End Try

                ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ = toSave
                Try
                    ParametresGlobauxHelpers.EcrireCleParametres("DernierRepertoireAjoutRepertoire_DJ", toSave)
                Catch
                End Try
                ' Restaurer CurrentDirectory et supprimer dossier temporaire si créé (utiliser méthode robuste)
                Try
                    ParametresGlobaux.SupprimerDossierTemporaire(tmpFolder)
                Catch
                End Try
                Try
                    Environment.CurrentDirectory = AppDomain.CurrentDomain.BaseDirectory
                Catch
                End Try
                ' Debug log: après sauvegarde (FormDJ)
                Try
                    Dim debugFileDJ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "debug_repertoires.txt")
                    Dim after = $"[{DateTime.Now:O}] FormDJ.AfterSave: chosen='{chosen}' savedAs='{toSave}' dernierParentSaved_DJ='{ParametresGlobaux.dernierRepertoireAjoutRepertoire_DJ}' generalParent='{ParametresGlobaux.dernierRepertoireAjoutRepertoire}' tmpFolder='{tmpFolder}'{Environment.NewLine}"
                    File.AppendAllText(debugFileDJ, after)
                Catch
                End Try
                ' Restaurer le répertoire courant et supprimer le dossier temporaire si créé (utiliser méthode robuste)
                Try
                    ParametresGlobaux.SupprimerDossierTemporaire(tmpFolder)
                Catch
                End Try
                Try
                    Environment.CurrentDirectory = cwd
                Catch
                End Try

                Dim extensions = {".mp3", ".wav", ".flac", ".wma", ".aac", ".ogg"}
                Dim fichiers = Directory.GetFiles(fbd.SelectedPath, "*.*", SearchOption.AllDirectories) _
                    .Where(Function(f) extensions.Contains(Path.GetExtension(f).ToLower()))

                For Each fichier In fichiers
                    AjouterFichierAListeDJ(fichier)
                Next

                MettreAJourNumerotationDJ()
                SauvegarderPlaylistDJ()
            End If
        End Using
    End Sub

    ' === Bouton GÉRER : Menu contextuel avec Ouvrir/Enregistrer/Nouvelle/Effacer/Supprimer ===
    Private Sub ButtonGererPlaylist_Click(sender As Object, e As EventArgs) Handles ButtonGererPlaylist.Click
        Dim menuGerer As New ContextMenuStrip()

        ' Option 1 : Ouvrir une liste
        Dim menuItemOuvrir As New ToolStripMenuItem(LanguageManager.GetString("DJ_Menu_OpenList"))
        AddHandler menuItemOuvrir.Click, Sub() OuvrirPlaylistDJ()

        ' Option 2 : Enregistrer la liste
        Dim menuItemEnregistrer As New ToolStripMenuItem(LanguageManager.GetString("DJ_Menu_SaveList"))
        AddHandler menuItemEnregistrer.Click, Sub() EnregistrerPlaylistDJ()

        ' Option 3 : Nouvelle liste (vide)
        Dim menuItemNouvelle As New ToolStripMenuItem(LanguageManager.GetString("DJ_Menu_NewList"))
        AddHandler menuItemNouvelle.Click, Sub() NouvellePlaylistDJ()

        ' Option 4 : Effacer la liste
        Dim menuItemEffacer As New ToolStripMenuItem(LanguageManager.GetString("DJ_Menu_ClearList"))
        AddHandler menuItemEffacer.Click, Sub() NouvellePlaylistDJ()

        ' Option 5 : Supprimer l'item sélectionné
        Dim menuItemSupprimer As New ToolStripMenuItem(LanguageManager.GetString("DJ_Menu_RemoveSelected"))
        AddHandler menuItemSupprimer.Click, Sub() SupprimerItemSelectionne()

        ' Ajouter les options au menu
        menuGerer.Items.Add(menuItemOuvrir)
        menuGerer.Items.Add(menuItemEnregistrer)
        menuGerer.Items.Add(New ToolStripSeparator())
        menuGerer.Items.Add(menuItemNouvelle)
        menuGerer.Items.Add(menuItemEffacer)
        menuGerer.Items.Add(New ToolStripSeparator())
        menuGerer.Items.Add(menuItemSupprimer)

        ' Afficher le menu sous le bouton
        menuGerer.Show(ButtonGererPlaylist, New Point(0, ButtonGererPlaylist.Height))
    End Sub

    ' === Ouvrir une playlist ===
    Private Sub OuvrirPlaylistDJ()
        Using ofd As New OpenFileDialog With {
            .Filter = LanguageManager.GetString("DJ_Filter_Playlists"),
            .Title = LanguageManager.GetString("DJ_Dialog_OpenPlaylist"),
            .RestoreDirectory = True
        }
            ' Utiliser le dernier répertoire spécifique pour les opérations de playlist
            ' Priorité : utiliser la mémoire DJ si présente, sinon la mémoire générale
            If Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoirePlaylist_DJ) AndAlso Directory.Exists(ParametresGlobaux.dernierRepertoirePlaylist_DJ) Then
                ofd.InitialDirectory = ParametresGlobaux.dernierRepertoirePlaylist_DJ
            ElseIf Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoirePlaylist) AndAlso Directory.Exists(ParametresGlobaux.dernierRepertoirePlaylist) Then
                ofd.InitialDirectory = ParametresGlobaux.dernierRepertoirePlaylist
            ElseIf Not String.IsNullOrEmpty(ParametresGlobaux.repertoireParDefaut) AndAlso Directory.Exists(ParametresGlobaux.repertoireParDefaut) Then
                ofd.InitialDirectory = ParametresGlobaux.repertoireParDefaut
            End If

            If ofd.ShowDialog() = DialogResult.OK Then
                Try
                    ' Mémoriser le répertoire utilisé pour les playlists et sauvegarder les paramètres
                    Dim chosen = Path.GetDirectoryName(ofd.FileName)
                    ParametresGlobaux.dernierRepertoirePlaylist_DJ = chosen
                    Try
                        ParametresGlobauxHelpers.EcrireCleParametres("DernierRepertoirePlaylist_DJ", chosen)
                    Catch
                    End Try

                    Dim lignes = File.ReadAllLines(ofd.FileName)
                    Dim bpmEnAttente As String = ""

                    For Each ligne In lignes
                        ligne = ligne.Trim()

                        If String.IsNullOrEmpty(ligne) Then
                            Continue For
                        End If

                        If ligne.StartsWith("#BPM=") Then
                            bpmEnAttente = ligne.Substring("#BPM=".Length).Trim()
                            Continue For
                        End If

                        If Not ligne.StartsWith("#") Then
                            If File.Exists(ligne) Then
                                AjouterFichierAListeDJ(ligne, "", bpmEnAttente, "")
                                bpmEnAttente = ""
                            End If
                        End If
                    Next

                    MettreAJourNumerotationDJ()
                    SauvegarderPlaylistDJ()
                    MessageBox.Show(LanguageManager.GetString("DJ_Playlist_LoadSuccess"), LanguageManager.GetString("DJ_Success_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show(String.Format(LanguageManager.GetString("DJ_Playlist_LoadError"), ex.Message), LanguageManager.GetString("DJ_Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    ' === Enregistrer une playlist ===
    Private Sub EnregistrerPlaylistDJ()
        Using sfd As New SaveFileDialog With {
            .Filter = LanguageManager.GetString("DJ_Filter_Playlists"),
            .DefaultExt = "m3u",
            .Title = LanguageManager.GetString("DJ_Dialog_SavePlaylist"),
            .RestoreDirectory = True
        }
            ' Utiliser le dernier répertoire spécifique pour les opérations de playlist
            If Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoirePlaylist_DJ) AndAlso Directory.Exists(ParametresGlobaux.dernierRepertoirePlaylist_DJ) Then
                sfd.InitialDirectory = ParametresGlobaux.dernierRepertoirePlaylist_DJ
            ElseIf Not String.IsNullOrEmpty(ParametresGlobaux.dernierRepertoirePlaylist) AndAlso Directory.Exists(ParametresGlobaux.dernierRepertoirePlaylist) Then
                sfd.InitialDirectory = ParametresGlobaux.dernierRepertoirePlaylist
            ElseIf Not String.IsNullOrEmpty(ParametresGlobaux.repertoireParDefaut) AndAlso Directory.Exists(ParametresGlobaux.repertoireParDefaut) Then
                sfd.InitialDirectory = ParametresGlobaux.repertoireParDefaut
            End If

            If sfd.ShowDialog() = DialogResult.OK Then
                Try
                    ' Mémoriser le répertoire utilisé pour les playlists et sauvegarder les paramètres
                    Dim chosen = Path.GetDirectoryName(sfd.FileName)
                    ParametresGlobaux.dernierRepertoirePlaylist_DJ = chosen
                    Try
                        ParametresGlobauxHelpers.EcrireCleParametres("DernierRepertoirePlaylist_DJ", chosen)
                    Catch
                    End Try

                    Dim lignes As New List(Of String)()

                    For Each item As ListViewItem In ListViewPlaylist.Items
                        ' Extraire le chemin du Tag
                        Dim chemin As String = ""
                        If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                            Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                            If tagDict.ContainsKey("Chemin") Then
                                chemin = tagDict("Chemin")?.ToString()
                            End If
                        ElseIf TypeOf item.Tag Is String Then
                            chemin = item.Tag.ToString()
                        End If

                        ' Ajouter le BPM si disponible
                        Dim bpm = item.SubItems(2).Text
                        If Not String.IsNullOrEmpty(bpm) AndAlso bpm <> "--" Then
                            lignes.Add($"#BPM={bpm}")
                        End If

                        lignes.Add(chemin)
                    Next

                    File.WriteAllLines(sfd.FileName, lignes)
                    MessageBox.Show(LanguageManager.GetString("DJ_Playlist_SaveSuccess"), LanguageManager.GetString("DJ_Success_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show(String.Format(LanguageManager.GetString("DJ_Playlist_SaveError"), ex.Message), LanguageManager.GetString("DJ_Error_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Using
    End Sub

    ' === Nouvelle playlist (vide) ===
    Private Sub NouvellePlaylistDJ()
        If ListViewPlaylist.Items.Count > 0 Then
            Dim rep = MessageBox.Show(LanguageManager.GetString("DJ_Playlist_ClearConfirm"), LanguageManager.GetString("DJ_Confirm_Title"), MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If rep = DialogResult.Yes Then
                ListViewPlaylist.Items.Clear()
                SauvegarderPlaylistDJ()
            End If
        End If
    End Sub

    ' === Supprimer l'item sélectionné ===
    Private Sub SupprimerItemSelectionne()
        If ListViewPlaylist.SelectedItems.Count > 0 Then
            ListViewPlaylist.Items.Remove(ListViewPlaylist.SelectedItems(0))
            MettreAJourNumerotationDJ()
            SauvegarderPlaylistDJ()
        Else
            MessageBox.Show("Aucun item sélectionné.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    ' === PLAYLIST DJ - Drag depuis ListView ===
    Private Sub ListViewPlaylist_ItemDrag(sender As Object, e As ItemDragEventArgs)
        If e.Item IsNot Nothing Then
            ListViewPlaylist.DoDragDrop(e.Item, DragDropEffects.Copy)
        End If
    End Sub

    ' === DECK Labels - Autoriser Drop ===
    Private Sub LabelDeck_DragEnter(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(GetType(ListViewItem)) Then
            e.Effect = DragDropEffects.Copy
        End If
    End Sub

    ' === DECK A - Drop sur label ===
    Private Sub LabelDeckA_DragDrop(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(GetType(ListViewItem)) Then
            Dim item As ListViewItem = CType(e.Data.GetData(GetType(ListViewItem)), ListViewItem)
            ' Extraire le chemin du Tag
            Dim chemin As String = ""
            If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                If tagDict.ContainsKey("Chemin") Then
                    chemin = tagDict("Chemin")?.ToString()
                End If
            ElseIf TypeOf item.Tag Is String Then
                chemin = item.Tag.ToString()
            End If

            If Not String.IsNullOrEmpty(chemin) Then
                ChargerPisteDeckA(chemin)
            End If
        End If
    End Sub

    ' === DECK B - Drop sur label ===
    Private Sub LabelDeckB_DragDrop(sender As Object, e As DragEventArgs)
        If e.Data.GetDataPresent(GetType(ListViewItem)) Then
            Dim item As ListViewItem = CType(e.Data.GetData(GetType(ListViewItem)), ListViewItem)
            ' Extraire le chemin du Tag
            Dim chemin As String = ""
            If TypeOf item.Tag Is Dictionary(Of String, Object) Then
                Dim tagDict = DirectCast(item.Tag, Dictionary(Of String, Object))
                If tagDict.ContainsKey("Chemin") Then
                    chemin = tagDict("Chemin")?.ToString()
                End If
            ElseIf TypeOf item.Tag Is String Then
                chemin = item.Tag.ToString()
            End If

            If Not String.IsNullOrEmpty(chemin) Then
                ChargerPisteDeckB(chemin)
            End If
        End If
    End Sub

    ' === Charger une piste sur Deck A ===
    Private Sub ChargerPisteDeckA(chemin As String)
        ' Utiliser la méthode complète de chargement de fichier
        ChargerFichierDeckA(chemin)
    End Sub

    ' === Charger une piste sur Deck B ===
    Private Sub ChargerPisteDeckB(chemin As String)
        ' Utiliser la méthode complète de chargement de fichier
        ChargerFichierDeckB(chemin)
    End Sub

    ' === MÉTHODES D'AFFICHAGE ML ===

    ''' <summary>
    ''' Affiche les résultats ML pour Deck A dans les labels UI
    ''' </summary>
    Private Sub AfficherResultatsML_DeckA(result As MLAudioAnalyzer.MLAnalysisResult)
        If result Is Nothing Then Return

        Try
            ' Afficher Camelot Key (sera mappé sur un label dans FormDJ.Designer.vb)
            Debug.WriteLine($"[UI ML A] Key: {result.CamelotCode} ({result.Key} {result.Scale})")
            ' TODO: LabelKeyDeckA.Text = result.CamelotCode

            ' Afficher Genre
            Debug.WriteLine($"[UI ML A] Genre: {result.Genre}")
            ' TODO: LabelGenreDeckA.Text = result.Genre

            ' Afficher Danceability, Energy, Valence
            Debug.WriteLine($"[UI ML A] Danceability: {result.Danceability:F2}, Energy: {result.Energy:F2}, Valence: {result.Valence:F2}")
            ' TODO: ProgressBarDanceabilityA.Value = CInt(result.Danceability * 100)
            ' TODO: ProgressBarEnergyA.Value = CInt(result.Energy * 100)
            ' TODO: ProgressBarValenceA.Value = CInt(result.Valence * 100)
        Catch ex As Exception
            Debug.WriteLine($"[UI ML A] Erreur affichage ML: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Affiche les résultats ML pour Deck B dans les labels UI
    ''' </summary>
    Private Sub AfficherResultatsML_DeckB(result As MLAudioAnalyzer.MLAnalysisResult)
        If result Is Nothing Then Return

        Try
            ' Afficher Camelot Key
            Debug.WriteLine($"[UI ML B] Key: {result.CamelotCode} ({result.Key} {result.Scale})")
            ' TODO: LabelKeyDeckB.Text = result.CamelotCode

            ' Afficher Genre
            Debug.WriteLine($"[UI ML B] Genre: {result.Genre}")
            ' TODO: LabelGenreDeckB.Text = result.Genre

            ' Afficher Danceability, Energy, Valence
            Debug.WriteLine($"[UI ML B] Danceability: {result.Danceability:F2}, Energy: {result.Energy:F2}, Valence: {result.Valence:F2}")
            ' TODO: ProgressBarDanceabilityB.Value = CInt(result.Danceability * 100)
            ' TODO: ProgressBarEnergyB.Value = CInt(result.Energy * 100)
            ' TODO: ProgressBarValenceB.Value = CInt(result.Valence * 100)
        Catch ex As Exception
            Debug.WriteLine($"[UI ML B] Erreur affichage ML: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Vérifie la compatibilité harmonique entre Deck A et Deck B
    ''' </summary>
    Private Sub VerifierCompatibiliteHarmonique()
        ' Vérifier que les deux decks ont des résultats ML
        If mlResultDeckA Is Nothing OrElse mlResultDeckB Is Nothing Then
            Debug.WriteLine("[COMPATIBILITÉ] ⚠ Résultats ML manquants pour l'un des decks")
            Return
        End If

        Try
            Dim camelotA As String = mlResultDeckA.CamelotCode
            Dim camelotB As String = mlResultDeckB.CamelotCode

            Debug.WriteLine($"[COMPATIBILITÉ] Deck A: {camelotA}, Deck B: {camelotB}")

            ' Vérifier compatibilité harmonique
            Dim compatible As Boolean = MLAudioAnalyzer.SontHarmoniquementCompatibles(camelotA, camelotB)

            If compatible Then
                Debug.WriteLine($"[COMPATIBILITÉ] ✓ COMPATIBLE - Mix harmonique possible!")
                ' TODO: Afficher indicateur vert dans l'UI
                ' LabelCompatibiliteHarmonique.ForeColor = Color.Green
                ' LabelCompatibiliteHarmonique.Text = "✓ Compatible"
            Else
                Debug.WriteLine($"[COMPATIBILITÉ] ⚠ NON COMPATIBLE - Transition difficile")
                ' TODO: Afficher indicateur orange dans l'UI
                ' LabelCompatibiliteHarmonique.ForeColor = Color.Orange
                ' LabelCompatibiliteHarmonique.Text = "⚠ Mix difficile"

                ' Suggérer des clés compatibles
                Dim clesCompatibles As List(Of String) = MLAudioAnalyzer.ObtenirClesCompatibles(camelotA)
                Debug.WriteLine($"[COMPATIBILITÉ] Clés compatibles avec {camelotA}: {String.Join(", ", clesCompatibles)}")
            End If

        Catch ex As Exception
            Debug.WriteLine($"[COMPATIBILITÉ] Erreur vérification: {ex.Message}")
        End Try
    End Sub

    ' ═══════════════════════════════════════════════════════════════════════
    ' === ENREGISTREMENT DJ (Multi-format) ===
    ' ═══════════════════════════════════════════════════════════════════════

    ''' <summary>
    ''' Initialiser les contrôles d'enregistrement
    ''' </summary>
    Private Sub InitialiserEnregistrement()
        ' Remplir le ComboBox des formats
        ComboBoxFormatEnregistrement.Items.Clear()
        For Each formatItem In DJRecorder.ObtenirFormatsDisponibles()
            ComboBoxFormatEnregistrement.Items.Add(formatItem)
        Next
        ComboBoxFormatEnregistrement.SelectedIndex = 1 ' MP3 320 kbps par défaut

        ' Configurer le timer de mise à jour de la durée
        timerEnregistrement.Interval = 500 ' Mise à jour toutes les 500ms
        AddHandler timerEnregistrement.Tick, AddressOf TimerEnregistrement_Tick

        ' Répertoire par défaut
        If String.IsNullOrEmpty(repertoireEnregistrement) Then
            repertoireEnregistrement = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AudioPlay", "Recordings")
        End If

        Debug.WriteLine($"[REC] Répertoire enregistrement: {repertoireEnregistrement}")
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton d'enregistrement (Toggle Start/Stop)
    ''' </summary>
    Private Sub ButtonEnregistrement_Click(sender As Object, e As EventArgs) Handles ButtonEnregistrement.Click
        If Not enregistrementEnCours Then
            ' === DÉMARRER L'ENREGISTREMENT ===
            DemarrerEnregistrementDJ()
        Else
            ' === ARRÊTER L'ENREGISTREMENT ===
            ArreterEnregistrementDJ()
        End If
    End Sub

    ''' <summary>
    ''' Démarrer l'enregistrement DJ
    ''' </summary>
    Private Sub DemarrerEnregistrementDJ()
        Try
            ' Vérifier qu'au moins un deck est en lecture
            If fichierAudioDeckA Is Nothing AndAlso fichierAudioDeckB Is Nothing Then
                MessageBox.Show(
                    "Chargez au moins une piste avant de commencer l'enregistrement.",
                    "Enregistrement impossible",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Return
            End If

            ' Demander à l'utilisateur de choisir le répertoire (première fois ou si Shift appuyé)
            If String.IsNullOrEmpty(repertoireEnregistrement) OrElse
               (Control.ModifierKeys And Keys.Shift) = Keys.Shift Then
                Using folderDialog As New FolderBrowserDialog()
                    folderDialog.Description = "Choisissez le répertoire pour enregistrer votre mix"
                    folderDialog.SelectedPath = repertoireEnregistrement

                    If folderDialog.ShowDialog() = DialogResult.OK Then
                        repertoireEnregistrement = folderDialog.SelectedPath
                        ' Sauvegarder dans les paramètres
                        SauvegarderRepertoireEnregistrement(repertoireEnregistrement)
                    Else
                        Return ' Annulé par l'utilisateur
                    End If
                End Using
            End If

            ' Obtenir le format et la qualité sélectionnés
            Dim formatEnreg As DJRecorder.FormatEnregistrement
            Dim qualiteMP3 As DJRecorder.QualiteMP3
            DJRecorder.ObtenirFormatEtQualite(
                ComboBoxFormatEnregistrement.SelectedIndex,
                formatEnreg,
                qualiteMP3)

            ' Créer le recorder
            djRecorder = New DJRecorder(formatEnreg, qualiteMP3)

            ' Démarrer l'enregistrement (capture loopback automatique)
            If djRecorder.DemarrerEnregistrement(repertoireEnregistrement) Then
                enregistrementEnCours = True

                ' Mettre à jour l'UI
                ButtonEnregistrement.Text = "⬛ STOP"
                ButtonEnregistrement.BackColor = Color.FromArgb(50, 50, 50)
                LabelDureeEnregistrement.Visible = True
                LabelDureeEnregistrement.Text = "00:00"
                ComboBoxFormatEnregistrement.Enabled = False

                ' Démarrer le timer
                timerEnregistrement.Start()

                Debug.WriteLine($"[REC] ✓ Enregistrement démarré: {djRecorder.CheminFichierActuel}")
                MessageBox.Show(
                    $"Enregistrement démarré !{vbCrLf}{vbCrLf}Fichier: {Path.GetFileName(djRecorder.CheminFichierActuel)}",
                    "Enregistrement",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            MessageBox.Show(
                $"Erreur lors du démarrage de l'enregistrement :{vbCrLf}{ex.Message}",
                "Erreur",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
            Debug.WriteLine($"[REC] ✗ Erreur démarrage: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Arrêter l'enregistrement DJ
    ''' </summary>
    Private Sub ArreterEnregistrementDJ()
        Try
            If djRecorder IsNot Nothing Then
                Dim cheminFichier As String = djRecorder.CheminFichierActuel
                Dim duree As TimeSpan = djRecorder.DureeEnregistrement

                ' Arrêter l'enregistrement
                djRecorder.ArreterEnregistrement()
                djRecorder.Dispose()
                djRecorder = Nothing

                enregistrementEnCours = False

                ' Arrêter le timer
                timerEnregistrement.Stop()

                ' Mettre à jour l'UI
                ButtonEnregistrement.Text = "⬤ REC"
                ButtonEnregistrement.BackColor = Color.FromArgb(220, 50, 50)
                LabelDureeEnregistrement.Visible = False
                ComboBoxFormatEnregistrement.Enabled = True

                Debug.WriteLine($"[REC] ✓ Enregistrement terminé: {duree:hh\:mm\:ss}")

                ' Demander si l'utilisateur veut ouvrir le dossier
                Dim result As DialogResult = MessageBox.Show(
                    $"Enregistrement terminé !{vbCrLf}{vbCrLf}" &
                    $"Durée: {duree:hh\:mm\:ss}{vbCrLf}" &
                    $"Fichier: {Path.GetFileName(cheminFichier)}{vbCrLf}{vbCrLf}" &
                    $"Voulez-vous ouvrir le dossier ?",
                    "Enregistrement terminé",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information)

                If result = DialogResult.Yes Then
                    Process.Start("explorer.exe", $"/select,""{cheminFichier}""")
                End If
            End If

        Catch ex As Exception
            MessageBox.Show(
                $"Erreur lors de l'arrêt de l'enregistrement :{vbCrLf}{ex.Message}",
                "Erreur",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
            Debug.WriteLine($"[REC] ✗ Erreur arrêt: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Timer pour mettre à jour la durée d'enregistrement
    ''' </summary>
    Private Sub TimerEnregistrement_Tick(sender As Object, e As EventArgs)
        If djRecorder IsNot Nothing AndAlso enregistrementEnCours Then
            Dim duree As TimeSpan = djRecorder.DureeEnregistrement
            LabelDureeEnregistrement.Text = duree.ToString("hh\:mm\:ss")
        End If
    End Sub

    ''' <summary>
    ''' Sauvegarder le répertoire d'enregistrement dans les paramètres
    ''' </summary>
    Private Sub SauvegarderRepertoireEnregistrement(repertoire As String)
        Try
            Dim cheminParametres As String = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AudioPlay", "Son_Ajustement_DJ.txt")

            ' Lire les paramètres existants
            Dim parametres As New Dictionary(Of String, String)
            If File.Exists(cheminParametres) Then
                For Each ligne In File.ReadAllLines(cheminParametres, System.Text.Encoding.UTF8)
                    If ligne.Contains("="c) Then
                        Dim parts() As String = ligne.Split("="c, 2)
                        If parts.Length = 2 Then
                            parametres(parts(0).Trim()) = parts(1).Trim()
                        End If
                    End If
                Next
            End If

            ' Ajouter/modifier le répertoire
            parametres("RepertoireEnregistrement") = repertoire

            ' Sauvegarder
            Dim dossier As String = Path.GetDirectoryName(cheminParametres)
            If Not Directory.Exists(dossier) Then
                Directory.CreateDirectory(dossier)
            End If

            Using writer As New StreamWriter(cheminParametres, False, System.Text.Encoding.UTF8)
                For Each kvp In parametres
                    writer.WriteLine($"{kvp.Key}={kvp.Value}")
                Next
            End Using

            Debug.WriteLine($"[REC] Répertoire sauvegardé: {repertoire}")
        Catch ex As Exception
            Debug.WriteLine($"[REC] Erreur sauvegarde répertoire: {ex.Message}")
        End Try
    End Sub
End Class
