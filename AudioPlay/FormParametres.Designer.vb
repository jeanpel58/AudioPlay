<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormParametres
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormParametres))
        GroupBox_TypesAudioDefaut = New GroupBox()
        CheckBox_AAC = New CheckBox()
        CheckBox_WAV = New CheckBox()
        CheckBox_WMA = New CheckBox()
        CheckBox_FLAC = New CheckBox()
        CheckBox_MP3 = New CheckBox()
        LabelTypesAudioDefaut = New Label()
        GroupBoxLecture = New GroupBox()
        CheckBoxModeMixeurDJ = New CheckBox()
        Button_Metronome_Aide = New Button()
        CheckBox_EffacerChansons = New CheckBox()
        CheckBoxSupprimerSilenceFin = New CheckBox()
        TextBoxNombreBeats = New TextBox()
        LabelNombreBeats = New Label()
        CheckBoxSupprimerSilenceDebut = New CheckBox()
        CheckBoxMetronome = New CheckBox()
        CheckBoxMetronomeSon = New CheckBox()
        CheckBoxMetronomeLumiere = New CheckBox()
        ButtonAideNormalisation = New Button()
        CheckBoxNormalisationVolume = New CheckBox()
        CheckBoxAfficherBPM = New CheckBox()
        CheckBoxLectureAuto = New CheckBox()
        LabelMethodeBPM = New Label()
        ComboBoxMethodeBPM = New ComboBox()
        ButtonSauvegarder = New Button()
        ButtonAnnuler = New Button()
        ButtonReinitialiser = New Button()
        GroupBoxLangue = New GroupBox()
        ComboBoxLangue = New ComboBox()
        LabelLangue = New Label()
        GroupBoxCouleurs = New GroupBox()
        Button_ThemeCouleur_Aide = New Button()
        LabelTheme = New Label()
        ComboBoxThemes = New ComboBox()
        ButtonSaveTheme = New Button()
        ButtonDeleteTheme = New Button()
        ButtonResetCouleurs = New Button()
        ButtonPersonnaliserCouleurs = New Button()
        GroupBoxEffetsAudio = New GroupBox()
        ButtonResetEffets = New Button()
        CheckBoxReverbActif = New CheckBox()
        LabelReverbMix = New Label()
        TrackBarReverbMix = New TrackBar()
        LabelReverbMixValeur = New Label()
        CheckBoxEchoActif = New CheckBox()
        LabelEchoMix = New Label()
        TrackBarEchoMix = New TrackBar()
        LabelEchoMixValeur = New Label()
        LabelEchoDelai = New Label()
        TrackBarEchoDelai = New TrackBar()
        LabelEchoDelaiValeur = New Label()
        LabelEchoFeedback = New Label()
        TrackBarEchoFeedback = New TrackBar()
        LabelEchoFeedbackValeur = New Label()
        CheckBoxTimeStretchActif = New CheckBox()
        LabelTimeStretch = New Label()
        TrackBarTimeStretch = New TrackBar()
        LabelTimeStretchValeur = New Label()
        ButtonResetTimeStretch = New Button()
        CheckBoxPitchShiftActif = New CheckBox()
        LabelPitchShift = New Label()
        TrackBarPitchShift = New TrackBar()
        LabelPitchShiftValeur = New Label()
        ButtonResetPitchShift = New Button()
        CheckBoxPhaserActif = New CheckBox()
        LabelPhaserRate = New Label()
        TrackBarPhaserRate = New TrackBar()
        LabelPhaserRateValeur = New Label()
        LabelPhaserDepth = New Label()
        TrackBarPhaserDepth = New TrackBar()
        LabelPhaserDepthValeur = New Label()
        LabelPhaserFeedback = New Label()
        TrackBarPhaserFeedback = New TrackBar()
        LabelPhaserFeedbackValeur = New Label()
        LabelPhaserMix = New Label()
        TrackBarPhaserMix = New TrackBar()
        LabelPhaserMixValeur = New Label()
        LabelPhaserStages = New Label()
        ComboBoxPhaserStages = New ComboBox()
        ButtonResetPhaser = New Button()
        GroupBox_TypesAudioDefaut.SuspendLayout()
        GroupBoxLecture.SuspendLayout()
        GroupBoxLangue.SuspendLayout()
        GroupBoxCouleurs.SuspendLayout()
        GroupBoxEffetsAudio.SuspendLayout()
        CType(TrackBarReverbMix, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarEchoMix, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarEchoDelai, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarEchoFeedback, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarTimeStretch, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarPitchShift, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarPhaserRate, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarPhaserDepth, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarPhaserFeedback, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarPhaserMix, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' Keep designer consistency (no-op placeholder)
        ' 
        ' GroupBox_TypesAudioDefaut
        ' 
        GroupBox_TypesAudioDefaut.Controls.Add(CheckBox_AAC)
        GroupBox_TypesAudioDefaut.Controls.Add(CheckBox_WAV)
        GroupBox_TypesAudioDefaut.Controls.Add(CheckBox_WMA)
        GroupBox_TypesAudioDefaut.Controls.Add(CheckBox_FLAC)
        GroupBox_TypesAudioDefaut.Controls.Add(CheckBox_MP3)
        GroupBox_TypesAudioDefaut.Controls.Add(LabelTypesAudioDefaut)
        GroupBox_TypesAudioDefaut.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        GroupBox_TypesAudioDefaut.Location = New Point(12, 96)
        GroupBox_TypesAudioDefaut.Name = "GroupBox_TypesAudioDefaut"
        GroupBox_TypesAudioDefaut.Size = New Size(460, 78)
        GroupBox_TypesAudioDefaut.TabIndex = 0
        GroupBox_TypesAudioDefaut.TabStop = False
        GroupBox_TypesAudioDefaut.Text = "Types Audio par Défaut"
        ' 
        ' CheckBox_AAC
        ' 
        CheckBox_AAC.AutoSize = True
        CheckBox_AAC.Location = New Point(329, 47)
        CheckBox_AAC.Name = "CheckBox_AAC"
        CheckBox_AAC.Size = New Size(52, 19)
        CheckBox_AAC.TabIndex = 5
        CheckBox_AAC.Text = ".AAC"
        CheckBox_AAC.UseVisualStyleBackColor = True
        ' 
        ' CheckBox_WAV
        ' 
        CheckBox_WAV.AutoSize = True
        CheckBox_WAV.Location = New Point(268, 47)
        CheckBox_WAV.Name = "CheckBox_WAV"
        CheckBox_WAV.Size = New Size(56, 19)
        CheckBox_WAV.TabIndex = 4
        CheckBox_WAV.Text = ".WAV"
        CheckBox_WAV.UseVisualStyleBackColor = True
        ' 
        ' CheckBox_WMA
        ' 
        CheckBox_WMA.AutoSize = True
        CheckBox_WMA.Location = New Point(191, 47)
        CheckBox_WMA.Name = "CheckBox_WMA"
        CheckBox_WMA.Size = New Size(60, 19)
        CheckBox_WMA.TabIndex = 3
        CheckBox_WMA.Text = ".WMA"
        CheckBox_WMA.UseVisualStyleBackColor = True
        ' 
        ' CheckBox_FLAC
        ' 
        CheckBox_FLAC.AutoSize = True
        CheckBox_FLAC.Location = New Point(128, 47)
        CheckBox_FLAC.Name = "CheckBox_FLAC"
        CheckBox_FLAC.Size = New Size(50, 19)
        CheckBox_FLAC.TabIndex = 2
        CheckBox_FLAC.Text = ".Flac"
        CheckBox_FLAC.UseVisualStyleBackColor = True
        ' 
        ' CheckBox_MP3
        ' 
        CheckBox_MP3.AutoSize = True
        CheckBox_MP3.Location = New Point(60, 47)
        CheckBox_MP3.Name = "CheckBox_MP3"
        CheckBox_MP3.Size = New Size(54, 19)
        CheckBox_MP3.TabIndex = 1
        CheckBox_MP3.Text = ".MP3"
        CheckBox_MP3.UseVisualStyleBackColor = True
        ' 
        ' LabelTypesAudioDefaut
        ' 
        LabelTypesAudioDefaut.AutoSize = True
        LabelTypesAudioDefaut.Location = New Point(6, 19)
        LabelTypesAudioDefaut.Name = "LabelTypesAudioDefaut"
        LabelTypesAudioDefaut.Size = New Size(197, 15)
        LabelTypesAudioDefaut.TabIndex = 0
        LabelTypesAudioDefaut.Text = "Cocher les types audio par défaut :"
        ' 
        ' GroupBoxLecture
        ' 
        GroupBoxLecture.Controls.Add(CheckBoxModeMixeurDJ)
        GroupBoxLecture.Controls.Add(Button_Metronome_Aide)
        GroupBoxLecture.Controls.Add(CheckBox_EffacerChansons)
        GroupBoxLecture.Controls.Add(CheckBoxSupprimerSilenceFin)
        GroupBoxLecture.Controls.Add(TextBoxNombreBeats)
        GroupBoxLecture.Controls.Add(LabelNombreBeats)
        GroupBoxLecture.Controls.Add(CheckBoxSupprimerSilenceDebut)
        GroupBoxLecture.Controls.Add(CheckBoxMetronome)
        GroupBoxLecture.Controls.Add(CheckBoxMetronomeSon)
        GroupBoxLecture.Controls.Add(CheckBoxMetronomeLumiere)
        GroupBoxLecture.Controls.Add(ButtonAideNormalisation)
        GroupBoxLecture.Controls.Add(CheckBoxNormalisationVolume)
        GroupBoxLecture.Controls.Add(CheckBoxAfficherBPM)
        GroupBoxLecture.Controls.Add(CheckBoxLectureAuto)
        GroupBoxLecture.Controls.Add(LabelMethodeBPM)
        GroupBoxLecture.Controls.Add(ComboBoxMethodeBPM)
        ' Python path textbox
        LabelPythonPath = New Label()
        TextBoxPythonPath = New TextBox()
        ButtonBrowsePython = New Button()
        LabelPythonPath.AutoSize = True
        LabelPythonPath.Location = New Point(15, 320)
        LabelPythonPath.Name = "LabelPythonPath"
        LabelPythonPath.Size = New Size(120, 15)
        LabelPythonPath.TabIndex = 18
        LabelPythonPath.Text = "Chemin Python (optionnel):"

        TextBoxPythonPath.Location = New Point(150, 318)
        TextBoxPythonPath.Name = "TextBoxPythonPath"
        TextBoxPythonPath.Size = New Size(220, 23)
        TextBoxPythonPath.TabIndex = 19

        ButtonBrowsePython.Location = New Point(375, 316)
        ButtonBrowsePython.Name = "ButtonBrowsePython"
        ButtonBrowsePython.Size = New Size(75, 25)
        ButtonBrowsePython.TabIndex = 20
        ButtonBrowsePython.Text = "Parcourir"
        ButtonBrowsePython.UseVisualStyleBackColor = True

        ' Button to verify librosa and offer installation
        ButtonCheckLibrosa = New Button()
        ButtonCheckLibrosa.Location = New Point(375, 348)
        ButtonCheckLibrosa.Name = "ButtonCheckLibrosa"
        ButtonCheckLibrosa.Size = New Size(75, 25)
        ButtonCheckLibrosa.TabIndex = 21
        ButtonCheckLibrosa.Text = "Vérifier"
        ButtonCheckLibrosa.UseVisualStyleBackColor = True

        GroupBoxLecture.Controls.Add(ButtonCheckLibrosa)

        GroupBoxLecture.Controls.Add(LabelPythonPath)
        GroupBoxLecture.Controls.Add(TextBoxPythonPath)
        GroupBoxLecture.Controls.Add(ButtonBrowsePython)
        GroupBoxLecture.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        GroupBoxLecture.Location = New Point(12, 180)
        GroupBoxLecture.Name = "GroupBoxLecture"
        GroupBoxLecture.Size = New Size(460, 366)
        GroupBoxLecture.TabIndex = 1
        GroupBoxLecture.TabStop = False
        GroupBoxLecture.Text = "Paramètres de lecture"
        ' 
        ' Button_Metronome_Aide
        ' 
        Button_Metronome_Aide.FlatAppearance.MouseDownBackColor = Color.Red
        Button_Metronome_Aide.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_Metronome_Aide.FlatStyle = FlatStyle.Flat
        Button_Metronome_Aide.Location = New Point(375, 197)
        Button_Metronome_Aide.Name = "Button_Metronome_Aide"
        Button_Metronome_Aide.Size = New Size(75, 23)
        Button_Metronome_Aide.TabIndex = 16
        Button_Metronome_Aide.Text = "Aide"
        Button_Metronome_Aide.UseVisualStyleBackColor = True
        ' 
        ' CheckBox_EffacerChansons
        ' 
        CheckBox_EffacerChansons.AutoSize = True
        CheckBox_EffacerChansons.Checked = True
        CheckBox_EffacerChansons.CheckState = CheckState.Checked
        CheckBox_EffacerChansons.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBox_EffacerChansons.Location = New Point(14, 287)
        CheckBox_EffacerChansons.Name = "CheckBox_EffacerChansons"
        CheckBox_EffacerChansons.Size = New Size(394, 19)
        CheckBox_EffacerChansons.TabIndex = 15
        CheckBox_EffacerChansons.Text = "Message pour confirmer l'enlèvement d'une sélection dans la liste"
        CheckBox_EffacerChansons.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxSupprimerSilenceFin
        ' 
        CheckBoxSupprimerSilenceFin.AutoSize = True
        CheckBoxSupprimerSilenceFin.Checked = True
        CheckBoxSupprimerSilenceFin.CheckState = CheckState.Checked
        CheckBoxSupprimerSilenceFin.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxSupprimerSilenceFin.Location = New Point(14, 262)
        CheckBoxSupprimerSilenceFin.Name = "CheckBoxSupprimerSilenceFin"
        CheckBoxSupprimerSilenceFin.Size = New Size(268, 19)
        CheckBoxSupprimerSilenceFin.TabIndex = 14
        CheckBoxSupprimerSilenceFin.Text = "Supprimert les silences à la fin des chansons"
        CheckBoxSupprimerSilenceFin.UseVisualStyleBackColor = True
        ' 
        ' TextBoxNombreBeats
        ' 
        TextBoxNombreBeats.Location = New Point(150, 198)
        TextBoxNombreBeats.Name = "TextBoxNombreBeats"
        TextBoxNombreBeats.Size = New Size(61, 23)
        TextBoxNombreBeats.TabIndex = 13
        ' 
        ' LabelNombreBeats
        ' 
        LabelNombreBeats.AutoSize = True
        LabelNombreBeats.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LabelNombreBeats.Location = New Point(15, 201)
        LabelNombreBeats.Name = "LabelNombreBeats"
        LabelNombreBeats.Size = New Size(109, 15)
        LabelNombreBeats.TabIndex = 12
        LabelNombreBeats.Text = "Nombre de beats :"
        ' 
        ' CheckBoxSupprimerSilenceDebut
        ' 
        CheckBoxSupprimerSilenceDebut.AutoSize = True
        CheckBoxSupprimerSilenceDebut.Checked = True
        CheckBoxSupprimerSilenceDebut.CheckState = CheckState.Checked
        CheckBoxSupprimerSilenceDebut.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxSupprimerSilenceDebut.Location = New Point(14, 237)
        CheckBoxSupprimerSilenceDebut.Name = "CheckBoxSupprimerSilenceDebut"
        CheckBoxSupprimerSilenceDebut.Size = New Size(276, 19)
        CheckBoxSupprimerSilenceDebut.TabIndex = 11
        CheckBoxSupprimerSilenceDebut.Text = "Supprimer les silences au début des chansons"
        CheckBoxSupprimerSilenceDebut.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxMetronome
        ' 
        CheckBoxMetronome.AutoSize = True
        CheckBoxMetronome.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxMetronome.Location = New Point(15, 176)
        CheckBoxMetronome.Name = "CheckBoxMetronome"
        CheckBoxMetronome.Size = New Size(275, 19)
        CheckBoxMetronome.TabIndex = 10
        CheckBoxMetronome.Text = "Activer le métronome avant chaque chanson"
        CheckBoxMetronome.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxMetronomeSon
        ' 
        CheckBoxMetronomeSon.AutoSize = True
        CheckBoxMetronomeSon.Checked = True
        CheckBoxMetronomeSon.CheckState = CheckState.Checked
        CheckBoxMetronomeSon.Font = New Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        CheckBoxMetronomeSon.Location = New Point(228, 201)
        CheckBoxMetronomeSon.Name = "CheckBoxMetronomeSon"
        CheckBoxMetronomeSon.Size = New Size(46, 17)
        CheckBoxMetronomeSon.TabIndex = 14
        CheckBoxMetronomeSon.Text = "Son"
        CheckBoxMetronomeSon.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxMetronomeLumiere
        ' 
        CheckBoxMetronomeLumiere.AutoSize = True
        CheckBoxMetronomeLumiere.Checked = True
        CheckBoxMetronomeLumiere.CheckState = CheckState.Checked
        CheckBoxMetronomeLumiere.Font = New Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        CheckBoxMetronomeLumiere.Location = New Point(296, 201)
        CheckBoxMetronomeLumiere.Name = "CheckBoxMetronomeLumiere"
        CheckBoxMetronomeLumiere.Size = New Size(66, 17)
        CheckBoxMetronomeLumiere.TabIndex = 15
        CheckBoxMetronomeLumiere.Text = "Lumière"
        CheckBoxMetronomeLumiere.UseVisualStyleBackColor = True
        ' 
        ' ButtonAideNormalisation
        ' 
        ButtonAideNormalisation.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonAideNormalisation.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonAideNormalisation.FlatStyle = FlatStyle.Flat
        ButtonAideNormalisation.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonAideNormalisation.Location = New Point(193, 96)
        ButtonAideNormalisation.Name = "ButtonAideNormalisation"
        ButtonAideNormalisation.Size = New Size(77, 25)
        ButtonAideNormalisation.TabIndex = 7
        ButtonAideNormalisation.Text = "Aide"
        ButtonAideNormalisation.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxNormalisationVolume
        ' 
        CheckBoxNormalisationVolume.AutoSize = True
        CheckBoxNormalisationVolume.Checked = True
        CheckBoxNormalisationVolume.CheckState = CheckState.Checked
        CheckBoxNormalisationVolume.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxNormalisationVolume.Location = New Point(15, 100)
        CheckBoxNormalisationVolume.Name = "CheckBoxNormalisationVolume"
        CheckBoxNormalisationVolume.Size = New Size(165, 19)
        CheckBoxNormalisationVolume.TabIndex = 6
        CheckBoxNormalisationVolume.Text = "Normalisation du volume"
        CheckBoxNormalisationVolume.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxAfficherBPM
        ' 
        CheckBoxAfficherBPM.AutoSize = True
        CheckBoxAfficherBPM.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxAfficherBPM.Location = New Point(15, 65)
        CheckBoxAfficherBPM.Name = "CheckBoxAfficherBPM"
        CheckBoxAfficherBPM.Size = New Size(239, 19)
        CheckBoxAfficherBPM.TabIndex = 4
        CheckBoxAfficherBPM.Text = "Afficher le BPM dans les métadonnées"
        CheckBoxAfficherBPM.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxLectureAuto
        ' 
        CheckBoxLectureAuto.AutoSize = True
        CheckBoxLectureAuto.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxLectureAuto.Location = New Point(15, 40)
        CheckBoxLectureAuto.Name = "CheckBoxLectureAuto"
        CheckBoxLectureAuto.Size = New Size(255, 19)
        CheckBoxLectureAuto.TabIndex = 3
        CheckBoxLectureAuto.Text = "Lecture automatique du morceau suivant"
        CheckBoxLectureAuto.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxModeMixeurDJ
        ' 
        CheckBoxModeMixeurDJ.AutoSize = True
        CheckBoxModeMixeurDJ.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxModeMixeurDJ.Location = New Point(15, 312)
        CheckBoxModeMixeurDJ.Name = "CheckBoxModeMixeurDJ"
        CheckBoxModeMixeurDJ.Size = New Size(363, 19)
        CheckBoxModeMixeurDJ.TabIndex = 17
        CheckBoxModeMixeurDJ.Text = "Mode Mixeur DJ (2 platines avec crossfader et contrôles DJ)"
        CheckBoxModeMixeurDJ.UseVisualStyleBackColor = True
        ' 
        ' LabelMethodeBPM
        ' 
        LabelMethodeBPM.AutoSize = True
        LabelMethodeBPM.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LabelMethodeBPM.Location = New Point(15, 135)
        LabelMethodeBPM.Name = "LabelMethodeBPM"
        LabelMethodeBPM.Size = New Size(144, 15)
        LabelMethodeBPM.TabIndex = 8
        LabelMethodeBPM.Text = "Méthode de calcul BPM :"
        ' 
        ' ComboBoxMethodeBPM
        ' 
        ComboBoxMethodeBPM.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxMethodeBPM.FlatStyle = FlatStyle.Flat
        ComboBoxMethodeBPM.FormattingEnabled = True
        ComboBoxMethodeBPM.Items.AddRange(New Object() {"Auto (Librosa si disponible, sinon SoundTouch)", "Librosa uniquement (plus précis)", "SoundTouch uniquement (moins précis)"})
        ComboBoxMethodeBPM.Location = New Point(170, 132)
        ComboBoxMethodeBPM.Name = "ComboBoxMethodeBPM"
        ComboBoxMethodeBPM.Size = New Size(280, 23)
        ComboBoxMethodeBPM.TabIndex = 9
        ' 
        ' ButtonSauvegarder
        ' 
        ButtonSauvegarder.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonSauvegarder.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonSauvegarder.FlatStyle = FlatStyle.Flat
        ButtonSauvegarder.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        ButtonSauvegarder.Location = New Point(706, 840)
        ButtonSauvegarder.Name = "ButtonSauvegarder"
        ButtonSauvegarder.Size = New Size(110, 35)
        ButtonSauvegarder.TabIndex = 2
        ButtonSauvegarder.Text = "Sauvegarder"
        ButtonSauvegarder.UseVisualStyleBackColor = True
        ' 
        ' ButtonAnnuler
        ' 
        ButtonAnnuler.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonAnnuler.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonAnnuler.FlatStyle = FlatStyle.Flat
        ButtonAnnuler.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonAnnuler.Location = New Point(828, 840)
        ButtonAnnuler.Name = "ButtonAnnuler"
        ButtonAnnuler.Size = New Size(110, 35)
        ButtonAnnuler.TabIndex = 3
        ButtonAnnuler.Text = "Annuler"
        ButtonAnnuler.UseVisualStyleBackColor = True
        ' 
        ' ButtonReinitialiser
        ' 
        ButtonReinitialiser.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonReinitialiser.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonReinitialiser.FlatStyle = FlatStyle.Flat
        ButtonReinitialiser.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonReinitialiser.Location = New Point(14, 840)
        ButtonReinitialiser.Name = "ButtonReinitialiser"
        ButtonReinitialiser.Size = New Size(110, 35)
        ButtonReinitialiser.TabIndex = 4
        ButtonReinitialiser.Text = "Réinitialiser"
        ButtonReinitialiser.UseVisualStyleBackColor = True
        ' 
        ' GroupBoxLangue
        ' 
        GroupBoxLangue.Controls.Add(ComboBoxLangue)
        GroupBoxLangue.Controls.Add(LabelLangue)
        GroupBoxLangue.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        GroupBoxLangue.Location = New Point(12, 12)
        GroupBoxLangue.Name = "GroupBoxLangue"
        GroupBoxLangue.Size = New Size(460, 78)
        GroupBoxLangue.TabIndex = 5
        GroupBoxLangue.TabStop = False
        GroupBoxLangue.Text = "Paramètres de langue"
        ' 
        ' ComboBoxLangue
        ' 
        ComboBoxLangue.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxLangue.FlatStyle = FlatStyle.Flat
        ComboBoxLangue.FormattingEnabled = True
        ComboBoxLangue.Location = New Point(84, 32)
        ComboBoxLangue.Name = "ComboBoxLangue"
        ComboBoxLangue.Size = New Size(211, 23)
        ComboBoxLangue.TabIndex = 1
        ' 
        ' LabelLangue
        ' 
        LabelLangue.AutoSize = True
        LabelLangue.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LabelLangue.Location = New Point(18, 35)
        LabelLangue.Name = "LabelLangue"
        LabelLangue.Size = New Size(53, 15)
        LabelLangue.TabIndex = 0
        LabelLangue.Text = "Langue :"
        ' 
        ' GroupBoxCouleurs
        ' 
        GroupBoxCouleurs.Controls.Add(Button_ThemeCouleur_Aide)
        GroupBoxCouleurs.Controls.Add(LabelTheme)
        GroupBoxCouleurs.Controls.Add(ComboBoxThemes)
        GroupBoxCouleurs.Controls.Add(ButtonSaveTheme)
        GroupBoxCouleurs.Controls.Add(ButtonDeleteTheme)
        GroupBoxCouleurs.Controls.Add(ButtonResetCouleurs)
        GroupBoxCouleurs.Controls.Add(ButtonPersonnaliserCouleurs)
        GroupBoxCouleurs.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        GroupBoxCouleurs.Location = New Point(12, 552)
        GroupBoxCouleurs.Name = "GroupBoxCouleurs"
        GroupBoxCouleurs.Size = New Size(460, 133)
        GroupBoxCouleurs.TabIndex = 6
        GroupBoxCouleurs.TabStop = False
        GroupBoxCouleurs.Text = "Couleurs de l'interface"
        ' 
        ' Button_ThemeCouleur_Aide
        ' 
        Button_ThemeCouleur_Aide.FlatAppearance.MouseDownBackColor = Color.Red
        Button_ThemeCouleur_Aide.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_ThemeCouleur_Aide.FlatStyle = FlatStyle.Flat
        Button_ThemeCouleur_Aide.Location = New Point(373, 18)
        Button_ThemeCouleur_Aide.Name = "Button_ThemeCouleur_Aide"
        Button_ThemeCouleur_Aide.Size = New Size(75, 24)
        Button_ThemeCouleur_Aide.TabIndex = 6
        Button_ThemeCouleur_Aide.Text = "Aide"
        Button_ThemeCouleur_Aide.UseVisualStyleBackColor = True
        ' 
        ' LabelTheme
        ' 
        LabelTheme.AutoSize = True
        LabelTheme.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LabelTheme.Location = New Point(15, 22)
        LabelTheme.Name = "LabelTheme"
        LabelTheme.Size = New Size(141, 15)
        LabelTheme.TabIndex = 5
        LabelTheme.Text = "Sélectionner un thème :"
        ' 
        ' ComboBoxThemes
        ' 
        ComboBoxThemes.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxThemes.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ComboBoxThemes.FormattingEnabled = True
        ComboBoxThemes.Location = New Point(175, 19)
        ComboBoxThemes.Name = "ComboBoxThemes"
        ComboBoxThemes.Size = New Size(166, 23)
        ComboBoxThemes.TabIndex = 0
        ' 
        ' ButtonSaveTheme
        ' 
        ButtonSaveTheme.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonSaveTheme.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonSaveTheme.FlatStyle = FlatStyle.Flat
        ButtonSaveTheme.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonSaveTheme.Location = New Point(15, 52)
        ButtonSaveTheme.Name = "ButtonSaveTheme"
        ButtonSaveTheme.Size = New Size(207, 24)
        ButtonSaveTheme.TabIndex = 1
        ButtonSaveTheme.Text = "Enregistrer le thème sous..."
        ButtonSaveTheme.UseVisualStyleBackColor = True
        ' 
        ' ButtonDeleteTheme
        ' 
        ButtonDeleteTheme.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonDeleteTheme.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonDeleteTheme.FlatStyle = FlatStyle.Flat
        ButtonDeleteTheme.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonDeleteTheme.Location = New Point(228, 95)
        ButtonDeleteTheme.Name = "ButtonDeleteTheme"
        ButtonDeleteTheme.Size = New Size(220, 23)
        ButtonDeleteTheme.TabIndex = 4
        ButtonDeleteTheme.Text = "Supprimer le thème du menu"
        ButtonDeleteTheme.UseVisualStyleBackColor = True
        ' 
        ' ButtonResetCouleurs
        ' 
        ButtonResetCouleurs.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonResetCouleurs.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonResetCouleurs.FlatStyle = FlatStyle.Flat
        ButtonResetCouleurs.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonResetCouleurs.Location = New Point(15, 95)
        ButtonResetCouleurs.Name = "ButtonResetCouleurs"
        ButtonResetCouleurs.Size = New Size(207, 23)
        ButtonResetCouleurs.TabIndex = 3
        ButtonResetCouleurs.Text = "Couleurs par défaut"
        ButtonResetCouleurs.UseVisualStyleBackColor = True
        ' 
        ' ButtonPersonnaliserCouleurs
        ' 
        ButtonPersonnaliserCouleurs.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonPersonnaliserCouleurs.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonPersonnaliserCouleurs.FlatStyle = FlatStyle.Flat
        ButtonPersonnaliserCouleurs.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonPersonnaliserCouleurs.Location = New Point(228, 52)
        ButtonPersonnaliserCouleurs.Name = "ButtonPersonnaliserCouleurs"
        ButtonPersonnaliserCouleurs.Size = New Size(220, 24)
        ButtonPersonnaliserCouleurs.TabIndex = 2
        ButtonPersonnaliserCouleurs.Text = "Créer un thème de couleurs..."
        ButtonPersonnaliserCouleurs.UseVisualStyleBackColor = True
        ' 
        ' GroupBoxEffetsAudio
        ' 
        GroupBoxEffetsAudio.Controls.Add(ButtonResetEffets)
        GroupBoxEffetsAudio.Controls.Add(CheckBoxReverbActif)
        GroupBoxEffetsAudio.Controls.Add(LabelReverbMix)
        GroupBoxEffetsAudio.Controls.Add(TrackBarReverbMix)
        GroupBoxEffetsAudio.Controls.Add(LabelReverbMixValeur)
        GroupBoxEffetsAudio.Controls.Add(CheckBoxEchoActif)
        GroupBoxEffetsAudio.Controls.Add(LabelEchoMix)
        GroupBoxEffetsAudio.Controls.Add(TrackBarEchoMix)
        GroupBoxEffetsAudio.Controls.Add(LabelEchoMixValeur)
        GroupBoxEffetsAudio.Controls.Add(LabelEchoDelai)
        GroupBoxEffetsAudio.Controls.Add(TrackBarEchoDelai)
        GroupBoxEffetsAudio.Controls.Add(LabelEchoDelaiValeur)
        GroupBoxEffetsAudio.Controls.Add(LabelEchoFeedback)
        GroupBoxEffetsAudio.Controls.Add(TrackBarEchoFeedback)
        GroupBoxEffetsAudio.Controls.Add(LabelEchoFeedbackValeur)
        GroupBoxEffetsAudio.Controls.Add(CheckBoxTimeStretchActif)
        GroupBoxEffetsAudio.Controls.Add(LabelTimeStretch)
        GroupBoxEffetsAudio.Controls.Add(TrackBarTimeStretch)
        GroupBoxEffetsAudio.Controls.Add(LabelTimeStretchValeur)
        GroupBoxEffetsAudio.Controls.Add(ButtonResetTimeStretch)
        GroupBoxEffetsAudio.Controls.Add(CheckBoxPitchShiftActif)
        GroupBoxEffetsAudio.Controls.Add(LabelPitchShift)
        GroupBoxEffetsAudio.Controls.Add(TrackBarPitchShift)
        GroupBoxEffetsAudio.Controls.Add(LabelPitchShiftValeur)
        GroupBoxEffetsAudio.Controls.Add(ButtonResetPitchShift)
        GroupBoxEffetsAudio.Controls.Add(CheckBoxPhaserActif)
        GroupBoxEffetsAudio.Controls.Add(LabelPhaserRate)
        GroupBoxEffetsAudio.Controls.Add(TrackBarPhaserRate)
        GroupBoxEffetsAudio.Controls.Add(LabelPhaserRateValeur)
        GroupBoxEffetsAudio.Controls.Add(LabelPhaserDepth)
        GroupBoxEffetsAudio.Controls.Add(TrackBarPhaserDepth)
        GroupBoxEffetsAudio.Controls.Add(LabelPhaserDepthValeur)
        GroupBoxEffetsAudio.Controls.Add(LabelPhaserFeedback)
        GroupBoxEffetsAudio.Controls.Add(TrackBarPhaserFeedback)
        GroupBoxEffetsAudio.Controls.Add(LabelPhaserFeedbackValeur)
        GroupBoxEffetsAudio.Controls.Add(LabelPhaserMix)
        GroupBoxEffetsAudio.Controls.Add(TrackBarPhaserMix)
        GroupBoxEffetsAudio.Controls.Add(LabelPhaserMixValeur)
        GroupBoxEffetsAudio.Controls.Add(LabelPhaserStages)
        GroupBoxEffetsAudio.Controls.Add(ComboBoxPhaserStages)
        GroupBoxEffetsAudio.Controls.Add(ButtonResetPhaser)
        GroupBoxEffetsAudio.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        GroupBoxEffetsAudio.Location = New Point(478, 12)
        GroupBoxEffetsAudio.Name = "GroupBoxEffetsAudio"
        GroupBoxEffetsAudio.Size = New Size(460, 813)
        GroupBoxEffetsAudio.TabIndex = 7
        GroupBoxEffetsAudio.TabStop = False
        GroupBoxEffetsAudio.Text = "Effets Audio"
        ' 
        ' ButtonResetEffets
        ' 
        ButtonResetEffets.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonResetEffets.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonResetEffets.FlatStyle = FlatStyle.Flat
        ButtonResetEffets.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonResetEffets.Location = New Point(228, 779)
        ButtonResetEffets.Name = "ButtonResetEffets"
        ButtonResetEffets.Size = New Size(220, 24)
        ButtonResetEffets.TabIndex = 22
        ButtonResetEffets.Text = "Réinitialiser les effets"
        ButtonResetEffets.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxReverbActif
        ' 
        CheckBoxReverbActif.AutoSize = True
        CheckBoxReverbActif.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxReverbActif.Location = New Point(6, 22)
        CheckBoxReverbActif.Name = "CheckBoxReverbActif"
        CheckBoxReverbActif.Size = New Size(159, 19)
        CheckBoxReverbActif.TabIndex = 0
        CheckBoxReverbActif.Text = "Réverbération (Reverb)"
        CheckBoxReverbActif.UseVisualStyleBackColor = True
        ' 
        ' LabelReverbMix
        ' 
        LabelReverbMix.AutoSize = True
        LabelReverbMix.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelReverbMix.Location = New Point(6, 41)
        LabelReverbMix.Name = "LabelReverbMix"
        LabelReverbMix.Size = New Size(32, 15)
        LabelReverbMix.TabIndex = 1
        LabelReverbMix.Text = "Mix :"
        ' 
        ' TrackBarReverbMix
        ' 
        TrackBarReverbMix.AutoSize = False
        TrackBarReverbMix.Location = New Point(6, 59)
        TrackBarReverbMix.Maximum = 100
        TrackBarReverbMix.Name = "TrackBarReverbMix"
        TrackBarReverbMix.Size = New Size(390, 35)
        TrackBarReverbMix.TabIndex = 2
        TrackBarReverbMix.TickFrequency = 10
        TrackBarReverbMix.Value = 30
        ' 
        ' LabelReverbMixValeur
        ' 
        LabelReverbMixValeur.AutoSize = True
        LabelReverbMixValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelReverbMixValeur.Location = New Point(402, 61)
        LabelReverbMixValeur.Name = "LabelReverbMixValeur"
        LabelReverbMixValeur.Size = New Size(29, 15)
        LabelReverbMixValeur.TabIndex = 3
        LabelReverbMixValeur.Text = "30%"
        ' 
        ' CheckBoxEchoActif
        ' 
        CheckBoxEchoActif.AutoSize = True
        CheckBoxEchoActif.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxEchoActif.Location = New Point(6, 104)
        CheckBoxEchoActif.Name = "CheckBoxEchoActif"
        CheckBoxEchoActif.Size = New Size(52, 19)
        CheckBoxEchoActif.TabIndex = 4
        CheckBoxEchoActif.Text = "Écho"
        CheckBoxEchoActif.UseVisualStyleBackColor = True
        ' 
        ' LabelEchoMix
        ' 
        LabelEchoMix.AutoSize = True
        LabelEchoMix.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelEchoMix.Location = New Point(6, 127)
        LabelEchoMix.Name = "LabelEchoMix"
        LabelEchoMix.Size = New Size(32, 15)
        LabelEchoMix.TabIndex = 5
        LabelEchoMix.Text = "Mix :"
        ' 
        ' TrackBarEchoMix
        ' 
        TrackBarEchoMix.AutoSize = False
        TrackBarEchoMix.Location = New Point(6, 142)
        TrackBarEchoMix.Maximum = 100
        TrackBarEchoMix.Name = "TrackBarEchoMix"
        TrackBarEchoMix.Size = New Size(390, 35)
        TrackBarEchoMix.TabIndex = 6
        TrackBarEchoMix.TickFrequency = 10
        TrackBarEchoMix.Value = 30
        ' 
        ' LabelEchoMixValeur
        ' 
        LabelEchoMixValeur.AutoSize = True
        LabelEchoMixValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelEchoMixValeur.Location = New Point(402, 148)
        LabelEchoMixValeur.Name = "LabelEchoMixValeur"
        LabelEchoMixValeur.Size = New Size(29, 15)
        LabelEchoMixValeur.TabIndex = 7
        LabelEchoMixValeur.Text = "30%"
        ' 
        ' LabelEchoDelai
        ' 
        LabelEchoDelai.AutoSize = True
        LabelEchoDelai.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelEchoDelai.Location = New Point(6, 180)
        LabelEchoDelai.Name = "LabelEchoDelai"
        LabelEchoDelai.Size = New Size(39, 15)
        LabelEchoDelai.TabIndex = 8
        LabelEchoDelai.Text = "Délai :"
        ' 
        ' TrackBarEchoDelai
        ' 
        TrackBarEchoDelai.AutoSize = False
        TrackBarEchoDelai.LargeChange = 10
        TrackBarEchoDelai.Location = New Point(6, 198)
        TrackBarEchoDelai.Maximum = 200
        TrackBarEchoDelai.Minimum = 5
        TrackBarEchoDelai.Name = "TrackBarEchoDelai"
        TrackBarEchoDelai.Size = New Size(390, 35)
        TrackBarEchoDelai.TabIndex = 9
        TrackBarEchoDelai.TickFrequency = 10
        TrackBarEchoDelai.Value = 30
        ' 
        ' LabelEchoDelaiValeur
        ' 
        LabelEchoDelaiValeur.AutoSize = True
        LabelEchoDelaiValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelEchoDelaiValeur.Location = New Point(402, 202)
        LabelEchoDelaiValeur.Name = "LabelEchoDelaiValeur"
        LabelEchoDelaiValeur.Size = New Size(44, 15)
        LabelEchoDelaiValeur.TabIndex = 10
        LabelEchoDelaiValeur.Text = "300 ms"
        ' 
        ' LabelEchoFeedback
        ' 
        LabelEchoFeedback.AutoSize = True
        LabelEchoFeedback.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelEchoFeedback.Location = New Point(6, 236)
        LabelEchoFeedback.Name = "LabelEchoFeedback"
        LabelEchoFeedback.Size = New Size(63, 15)
        LabelEchoFeedback.TabIndex = 11
        LabelEchoFeedback.Text = "Feedback :"
        ' 
        ' TrackBarEchoFeedback
        ' 
        TrackBarEchoFeedback.AutoSize = False
        TrackBarEchoFeedback.Location = New Point(6, 254)
        TrackBarEchoFeedback.Maximum = 90
        TrackBarEchoFeedback.Name = "TrackBarEchoFeedback"
        TrackBarEchoFeedback.Size = New Size(390, 35)
        TrackBarEchoFeedback.TabIndex = 12
        TrackBarEchoFeedback.TickFrequency = 10
        TrackBarEchoFeedback.Value = 50
        ' 
        ' LabelEchoFeedbackValeur
        ' 
        LabelEchoFeedbackValeur.AutoSize = True
        LabelEchoFeedbackValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelEchoFeedbackValeur.Location = New Point(402, 254)
        LabelEchoFeedbackValeur.Name = "LabelEchoFeedbackValeur"
        LabelEchoFeedbackValeur.Size = New Size(29, 15)
        LabelEchoFeedbackValeur.TabIndex = 13
        LabelEchoFeedbackValeur.Text = "50%"
        ' 
        ' CheckBoxTimeStretchActif
        ' 
        CheckBoxTimeStretchActif.AutoSize = True
        CheckBoxTimeStretchActif.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxTimeStretchActif.Location = New Point(6, 305)
        CheckBoxTimeStretchActif.Name = "CheckBoxTimeStretchActif"
        CheckBoxTimeStretchActif.Size = New Size(195, 19)
        CheckBoxTimeStretchActif.TabIndex = 18
        CheckBoxTimeStretchActif.Text = "Time Stretch (changer tempo)"
        CheckBoxTimeStretchActif.UseVisualStyleBackColor = True
        ' 
        ' LabelTimeStretch
        ' 
        LabelTimeStretch.AutoSize = True
        LabelTimeStretch.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelTimeStretch.Location = New Point(6, 327)
        LabelTimeStretch.Name = "LabelTimeStretch"
        LabelTimeStretch.Size = New Size(50, 15)
        LabelTimeStretch.TabIndex = 19
        LabelTimeStretch.Text = "Tempo :"
        ' 
        ' TrackBarTimeStretch
        ' 
        TrackBarTimeStretch.AutoSize = False
        TrackBarTimeStretch.Location = New Point(6, 345)
        TrackBarTimeStretch.Maximum = 200
        TrackBarTimeStretch.Minimum = 50
        TrackBarTimeStretch.Name = "TrackBarTimeStretch"
        TrackBarTimeStretch.Size = New Size(390, 35)
        TrackBarTimeStretch.TabIndex = 20
        TrackBarTimeStretch.TickFrequency = 10
        TrackBarTimeStretch.Value = 100
        ' 
        ' LabelTimeStretchValeur
        ' 
        LabelTimeStretchValeur.AutoSize = True
        LabelTimeStretchValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelTimeStretchValeur.Location = New Point(404, 338)
        LabelTimeStretchValeur.Name = "LabelTimeStretchValeur"
        LabelTimeStretchValeur.Size = New Size(33, 15)
        LabelTimeStretchValeur.TabIndex = 21
        LabelTimeStretchValeur.Text = "1.00x"
        ' 
        ' ButtonResetTimeStretch
        ' 
        ButtonResetTimeStretch.BackColor = Color.Transparent
        ButtonResetTimeStretch.Cursor = Cursors.Hand
        ButtonResetTimeStretch.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonResetTimeStretch.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonResetTimeStretch.FlatStyle = FlatStyle.Flat
        ButtonResetTimeStretch.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonResetTimeStretch.Location = New Point(402, 359)
        ButtonResetTimeStretch.Name = "ButtonResetTimeStretch"
        ButtonResetTimeStretch.Size = New Size(32, 24)
        ButtonResetTimeStretch.TabIndex = 24
        ButtonResetTimeStretch.Text = "✕"
        ButtonResetTimeStretch.UseVisualStyleBackColor = False
        ' 
        ' CheckBoxPitchShiftActif
        ' 
        CheckBoxPitchShiftActif.AutoSize = True
        CheckBoxPitchShiftActif.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxPitchShiftActif.Location = New Point(6, 401)
        CheckBoxPitchShiftActif.Name = "CheckBoxPitchShiftActif"
        CheckBoxPitchShiftActif.Size = New Size(186, 19)
        CheckBoxPitchShiftActif.TabIndex = 25
        CheckBoxPitchShiftActif.Text = "Pitch Shift (changer tonalité)"
        CheckBoxPitchShiftActif.UseVisualStyleBackColor = True
        ' 
        ' LabelPitchShift
        ' 
        LabelPitchShift.AutoSize = True
        LabelPitchShift.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelPitchShift.Location = New Point(6, 423)
        LabelPitchShift.Name = "LabelPitchShift"
        LabelPitchShift.Size = New Size(103, 15)
        LabelPitchShift.TabIndex = 26
        LabelPitchShift.Text = "Pitch (demi-tons):"
        ' 
        ' TrackBarPitchShift
        ' 
        TrackBarPitchShift.AutoSize = False
        TrackBarPitchShift.Location = New Point(6, 441)
        TrackBarPitchShift.Maximum = 120
        TrackBarPitchShift.Minimum = -120
        TrackBarPitchShift.Name = "TrackBarPitchShift"
        TrackBarPitchShift.Size = New Size(390, 35)
        TrackBarPitchShift.TabIndex = 27
        TrackBarPitchShift.TickFrequency = 10
        ' 
        ' LabelPitchShiftValeur
        ' 
        LabelPitchShiftValeur.AutoSize = True
        LabelPitchShiftValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelPitchShiftValeur.Location = New Point(404, 434)
        LabelPitchShiftValeur.Name = "LabelPitchShiftValeur"
        LabelPitchShiftValeur.Size = New Size(22, 15)
        LabelPitchShiftValeur.TabIndex = 28
        LabelPitchShiftValeur.Text = "0.0"
        ' 
        ' ButtonResetPitchShift
        ' 
        ButtonResetPitchShift.BackColor = Color.Transparent
        ButtonResetPitchShift.Cursor = Cursors.Hand
        ButtonResetPitchShift.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonResetPitchShift.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonResetPitchShift.FlatStyle = FlatStyle.Flat
        ButtonResetPitchShift.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonResetPitchShift.Location = New Point(402, 455)
        ButtonResetPitchShift.Name = "ButtonResetPitchShift"
        ButtonResetPitchShift.Size = New Size(32, 24)
        ButtonResetPitchShift.TabIndex = 29
        ButtonResetPitchShift.Text = "✕"
        ButtonResetPitchShift.UseVisualStyleBackColor = False
        ' 
        ' CheckBoxPhaserActif
        ' 
        CheckBoxPhaserActif.AutoSize = True
        CheckBoxPhaserActif.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        CheckBoxPhaserActif.Location = New Point(6, 490)
        CheckBoxPhaserActif.Name = "CheckBoxPhaserActif"
        CheckBoxPhaserActif.Size = New Size(63, 19)
        CheckBoxPhaserActif.TabIndex = 30
        CheckBoxPhaserActif.Text = "Phaser"
        CheckBoxPhaserActif.UseVisualStyleBackColor = True
        ' 
        ' LabelPhaserRate
        ' 
        LabelPhaserRate.AutoSize = True
        LabelPhaserRate.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelPhaserRate.Location = New Point(6, 512)
        LabelPhaserRate.Name = "LabelPhaserRate"
        LabelPhaserRate.Size = New Size(71, 15)
        LabelPhaserRate.TabIndex = 31
        LabelPhaserRate.Text = "Vitesse (Hz):"
        ' 
        ' TrackBarPhaserRate
        ' 
        TrackBarPhaserRate.AutoSize = False
        TrackBarPhaserRate.Location = New Point(6, 530)
        TrackBarPhaserRate.Maximum = 100
        TrackBarPhaserRate.Minimum = 1
        TrackBarPhaserRate.Name = "TrackBarPhaserRate"
        TrackBarPhaserRate.Size = New Size(390, 35)
        TrackBarPhaserRate.TabIndex = 32
        TrackBarPhaserRate.TickFrequency = 10
        TrackBarPhaserRate.Value = 50
        ' 
        ' LabelPhaserRateValeur
        ' 
        LabelPhaserRateValeur.AutoSize = True
        LabelPhaserRateValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelPhaserRateValeur.Location = New Point(404, 523)
        LabelPhaserRateValeur.Name = "LabelPhaserRateValeur"
        LabelPhaserRateValeur.Size = New Size(22, 15)
        LabelPhaserRateValeur.TabIndex = 33
        LabelPhaserRateValeur.Text = "0.5"
        ' 
        ' LabelPhaserDepth
        ' 
        LabelPhaserDepth.AutoSize = True
        LabelPhaserDepth.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelPhaserDepth.Location = New Point(6, 568)
        LabelPhaserDepth.Name = "LabelPhaserDepth"
        LabelPhaserDepth.Size = New Size(70, 15)
        LabelPhaserDepth.TabIndex = 34
        LabelPhaserDepth.Text = "Profondeur:"
        ' 
        ' TrackBarPhaserDepth
        ' 
        TrackBarPhaserDepth.AutoSize = False
        TrackBarPhaserDepth.Location = New Point(6, 586)
        TrackBarPhaserDepth.Maximum = 100
        TrackBarPhaserDepth.Name = "TrackBarPhaserDepth"
        TrackBarPhaserDepth.Size = New Size(390, 35)
        TrackBarPhaserDepth.TabIndex = 35
        TrackBarPhaserDepth.TickFrequency = 10
        TrackBarPhaserDepth.Value = 50
        ' 
        ' LabelPhaserDepthValeur
        ' 
        LabelPhaserDepthValeur.AutoSize = True
        LabelPhaserDepthValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelPhaserDepthValeur.Location = New Point(404, 579)
        LabelPhaserDepthValeur.Name = "LabelPhaserDepthValeur"
        LabelPhaserDepthValeur.Size = New Size(29, 15)
        LabelPhaserDepthValeur.TabIndex = 36
        LabelPhaserDepthValeur.Text = "50%"
        ' 
        ' LabelPhaserFeedback
        ' 
        LabelPhaserFeedback.AutoSize = True
        LabelPhaserFeedback.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelPhaserFeedback.Location = New Point(6, 624)
        LabelPhaserFeedback.Name = "LabelPhaserFeedback"
        LabelPhaserFeedback.Size = New Size(60, 15)
        LabelPhaserFeedback.TabIndex = 37
        LabelPhaserFeedback.Text = "Feedback:"
        ' 
        ' TrackBarPhaserFeedback
        ' 
        TrackBarPhaserFeedback.AutoSize = False
        TrackBarPhaserFeedback.Location = New Point(6, 642)
        TrackBarPhaserFeedback.Maximum = 95
        TrackBarPhaserFeedback.Name = "TrackBarPhaserFeedback"
        TrackBarPhaserFeedback.Size = New Size(390, 35)
        TrackBarPhaserFeedback.TabIndex = 38
        TrackBarPhaserFeedback.TickFrequency = 10
        TrackBarPhaserFeedback.Value = 30
        ' 
        ' LabelPhaserFeedbackValeur
        ' 
        LabelPhaserFeedbackValeur.AutoSize = True
        LabelPhaserFeedbackValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelPhaserFeedbackValeur.Location = New Point(404, 635)
        LabelPhaserFeedbackValeur.Name = "LabelPhaserFeedbackValeur"
        LabelPhaserFeedbackValeur.Size = New Size(29, 15)
        LabelPhaserFeedbackValeur.TabIndex = 39
        LabelPhaserFeedbackValeur.Text = "30%"
        ' 
        ' LabelPhaserMix
        ' 
        LabelPhaserMix.AutoSize = True
        LabelPhaserMix.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelPhaserMix.Location = New Point(6, 680)
        LabelPhaserMix.Name = "LabelPhaserMix"
        LabelPhaserMix.Size = New Size(29, 15)
        LabelPhaserMix.TabIndex = 40
        LabelPhaserMix.Text = "Mix:"
        ' 
        ' TrackBarPhaserMix
        ' 
        TrackBarPhaserMix.AutoSize = False
        TrackBarPhaserMix.Location = New Point(6, 698)
        TrackBarPhaserMix.Maximum = 100
        TrackBarPhaserMix.Name = "TrackBarPhaserMix"
        TrackBarPhaserMix.Size = New Size(390, 35)
        TrackBarPhaserMix.TabIndex = 41
        TrackBarPhaserMix.TickFrequency = 10
        TrackBarPhaserMix.Value = 50
        ' 
        ' LabelPhaserMixValeur
        ' 
        LabelPhaserMixValeur.AutoSize = True
        LabelPhaserMixValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelPhaserMixValeur.Location = New Point(404, 691)
        LabelPhaserMixValeur.Name = "LabelPhaserMixValeur"
        LabelPhaserMixValeur.Size = New Size(29, 15)
        LabelPhaserMixValeur.TabIndex = 42
        LabelPhaserMixValeur.Text = "50%"
        ' 
        ' LabelPhaserStages
        ' 
        LabelPhaserStages.FlatStyle = FlatStyle.Flat
        LabelPhaserStages.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        LabelPhaserStages.Location = New Point(6, 751)
        LabelPhaserStages.Name = "LabelPhaserStages"
        LabelPhaserStages.Size = New Size(98, 15)
        LabelPhaserStages.TabIndex = 43
        LabelPhaserStages.Text = "Stages:"
        LabelPhaserStages.TextAlign = ContentAlignment.MiddleRight
        ' 
        ' ComboBoxPhaserStages
        ' 
        ComboBoxPhaserStages.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxPhaserStages.FormattingEnabled = True
        ComboBoxPhaserStages.Items.AddRange(New Object() {"2", "4", "6", "8", "10", "12"})
        ComboBoxPhaserStages.Location = New Point(110, 748)
        ComboBoxPhaserStages.Name = "ComboBoxPhaserStages"
        ComboBoxPhaserStages.Size = New Size(121, 23)
        ComboBoxPhaserStages.TabIndex = 44
        ' 
        ' ButtonResetPhaser
        ' 
        ButtonResetPhaser.BackColor = Color.Transparent
        ButtonResetPhaser.Cursor = Cursors.Hand
        ButtonResetPhaser.FlatAppearance.MouseDownBackColor = Color.Red
        ButtonResetPhaser.FlatAppearance.MouseOverBackColor = Color.Lime
        ButtonResetPhaser.FlatStyle = FlatStyle.Flat
        ButtonResetPhaser.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        ButtonResetPhaser.Location = New Point(404, 742)
        ButtonResetPhaser.Name = "ButtonResetPhaser"
        ButtonResetPhaser.Size = New Size(32, 24)
        ButtonResetPhaser.TabIndex = 45
        ButtonResetPhaser.Text = "✕"
        ButtonResetPhaser.UseVisualStyleBackColor = False
        ' 
        ' FormParametres
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(949, 887)
        Controls.Add(GroupBoxEffetsAudio)
        Controls.Add(GroupBoxCouleurs)
        Controls.Add(GroupBoxLangue)
        Controls.Add(ButtonReinitialiser)
        Controls.Add(ButtonAnnuler)
        Controls.Add(ButtonSauvegarder)
        Controls.Add(GroupBoxLecture)
        Controls.Add(GroupBox_TypesAudioDefaut)
        FormBorderStyle = FormBorderStyle.FixedSingle
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximizeBox = False
        MinimizeBox = False
        Name = "FormParametres"
        StartPosition = FormStartPosition.CenterParent
        Text = "Paramètres - AudioPlay"
        GroupBox_TypesAudioDefaut.ResumeLayout(False)
        GroupBox_TypesAudioDefaut.PerformLayout()
        GroupBoxLecture.ResumeLayout(False)
        GroupBoxLecture.PerformLayout()
        GroupBoxLangue.ResumeLayout(False)
        GroupBoxLangue.PerformLayout()
        GroupBoxCouleurs.ResumeLayout(False)
        GroupBoxCouleurs.PerformLayout()
        GroupBoxEffetsAudio.ResumeLayout(False)
        GroupBoxEffetsAudio.PerformLayout()
        CType(TrackBarReverbMix, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarEchoMix, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarEchoDelai, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarEchoFeedback, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarTimeStretch, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarPitchShift, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarPhaserRate, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarPhaserDepth, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarPhaserFeedback, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarPhaserMix, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)

    End Sub

    Friend WithEvents GroupBox_TypesAudioDefaut As GroupBox
    Friend WithEvents ButtonParcourir As Button
    Friend WithEvents TextBoxRepertoire As TextBox
    Friend WithEvents LabelTypesAudioDefaut As Label
    Friend WithEvents GroupBoxLecture As GroupBox
    Friend WithEvents ButtonAideNormalisation As Button
    Friend WithEvents CheckBoxNormalisationVolume As CheckBox
    Friend WithEvents CheckBoxAfficherBPM As CheckBox
    Friend WithEvents CheckBoxLectureAuto As CheckBox
    Friend WithEvents LabelMethodeBPM As Label
    Friend WithEvents ComboBoxMethodeBPM As ComboBox
    Friend WithEvents ButtonSauvegarder As Button
    Friend WithEvents ButtonAnnuler As Button
    Friend WithEvents ButtonReinitialiser As Button
    Friend WithEvents CheckBoxSupprimerSilenceFin As CheckBox
    Friend WithEvents TextBoxNombreBeats As TextBox
    Friend WithEvents LabelNombreBeats As Label
    Friend WithEvents CheckBoxSupprimerSilenceDebut As CheckBox
    Friend WithEvents CheckBoxMetronome As CheckBox
    Friend WithEvents CheckBoxMetronomeSon As CheckBox
    Friend WithEvents CheckBoxMetronomeLumiere As CheckBox
    Friend WithEvents GroupBoxLangue As GroupBox
    Friend WithEvents ComboBoxLangue As ComboBox
    Friend WithEvents LabelLangue As Label
    Friend WithEvents GroupBoxCouleurs As GroupBox
    Friend WithEvents LabelTheme As Label
    Friend WithEvents ComboBoxThemes As ComboBox
    Friend WithEvents ButtonSaveTheme As Button
    Friend WithEvents ButtonDeleteTheme As Button
    Friend WithEvents ButtonResetCouleurs As Button
    Friend WithEvents ButtonPersonnaliserCouleurs As Button
    Friend WithEvents CheckBox_AAC As CheckBox
    Friend WithEvents CheckBox_WAV As CheckBox
    Friend WithEvents CheckBox_WMA As CheckBox
    Friend WithEvents CheckBox_FLAC As CheckBox
    Friend WithEvents CheckBox_MP3 As CheckBox
    Friend WithEvents CheckBox_EffacerChansons As CheckBox
    Friend WithEvents Button_ThemeCouleur_Aide As Button
    Friend WithEvents Button_Metronome_Aide As Button

    ' === Effets Audio ===
    Friend WithEvents GroupBoxEffetsAudio As GroupBox
    Friend WithEvents CheckBoxReverbActif As CheckBox
    Friend WithEvents LabelReverbMix As Label
    Friend WithEvents TrackBarReverbMix As TrackBar
    Friend WithEvents LabelReverbMixValeur As Label
    Friend WithEvents CheckBoxEchoActif As CheckBox
    Friend WithEvents LabelEchoMix As Label
    Friend WithEvents TrackBarEchoMix As TrackBar
    Friend WithEvents LabelEchoMixValeur As Label
    Friend WithEvents LabelEchoDelai As Label
    Friend WithEvents TrackBarEchoDelai As TrackBar
    Friend WithEvents LabelEchoDelaiValeur As Label
    Friend WithEvents LabelEchoFeedback As Label
    Friend WithEvents TrackBarEchoFeedback As TrackBar
    Friend WithEvents LabelEchoFeedbackValeur As Label

    Friend WithEvents CheckBoxTimeStretchActif As CheckBox
    Friend WithEvents LabelTimeStretch As Label
    Friend WithEvents TrackBarTimeStretch As TrackBar
    Friend WithEvents LabelTimeStretchValeur As Label
    Friend WithEvents ButtonResetTimeStretch As Button

    Friend WithEvents CheckBoxPitchShiftActif As CheckBox
    Friend WithEvents LabelPitchShift As Label
    Friend WithEvents TrackBarPitchShift As TrackBar
    Friend WithEvents LabelPitchShiftValeur As Label
    Friend WithEvents ButtonResetPitchShift As Button

    Friend WithEvents CheckBoxPhaserActif As CheckBox
    Friend WithEvents LabelPhaserRate As Label
    Friend WithEvents TrackBarPhaserRate As TrackBar
    Friend WithEvents LabelPhaserRateValeur As Label
    Friend WithEvents LabelPhaserDepth As Label
    Friend WithEvents TrackBarPhaserDepth As TrackBar
    Friend WithEvents LabelPhaserDepthValeur As Label
    Friend WithEvents LabelPhaserFeedback As Label
    Friend WithEvents TrackBarPhaserFeedback As TrackBar
    Friend WithEvents LabelPhaserFeedbackValeur As Label
    Friend WithEvents LabelPhaserMix As Label
    Friend WithEvents TrackBarPhaserMix As TrackBar
    Friend WithEvents LabelPhaserMixValeur As Label
    Friend WithEvents LabelPhaserStages As Label
    Friend WithEvents ComboBoxPhaserStages As ComboBox
    Friend WithEvents ButtonResetPhaser As Button

    Friend WithEvents ButtonResetEffets As Button
    Friend WithEvents CheckBoxModeMixeurDJ As CheckBox
End Class
