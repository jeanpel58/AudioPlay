<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormDJ
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormDJ))
        GroupBoxDeckA = New GroupBox()
        Button_DeckA_BackTo0 = New Button()
        Label_Avertissement = New Label()
        LabelTrackDeckA = New Label()
        ButtonPlayDeckA = New Button()
        ButtonCueDeckA = New Button()
        ButtonStopDeckA = New Button()
        TrackBarPositionDeckA = New TrackBar()
        LabelDureeDeckA = New Label()
        TrackBarVolumeDeckA = New TrackBar()
        LabelVolumeDeckA = New Label()
        TrackBarPitchDeckA = New TrackBar()
        LabelPitchDeckA = New Label()
        LabelBPMDeckA = New Label()
        VUMeterDeckA = New VUMeterControl()
        CheckBoxPhaserDeckA = New CheckBox()
        CheckBoxReverbDeckA = New CheckBox()
        CheckBoxEchoDeckA = New CheckBox()
        ButtonSyncDeckA = New Button()
        GroupBoxDeckB = New GroupBox()
        Button_DeckB_BackTo0 = New Button()
        LabelTrackDeckB = New Label()
        ButtonPlayDeckB = New Button()
        ButtonCueDeckB = New Button()
        ButtonStopDeckB = New Button()
        TrackBarPositionDeckB = New TrackBar()
        LabelDureeDeckB = New Label()
        TrackBarVolumeDeckB = New TrackBar()
        LabelVolumeDeckB = New Label()
        TrackBarPitchDeckB = New TrackBar()
        LabelPitchDeckB = New Label()
        LabelBPMDeckB = New Label()
        VUMeterDeckB = New VUMeterControl()
        CheckBoxPhaserDeckB = New CheckBox()
        CheckBoxReverbDeckB = New CheckBox()
        CheckBoxEchoDeckB = New CheckBox()
        ButtonSyncDeckB = New Button()
        GroupBoxMixeur = New GroupBox()
        LabelDureeEnregistrement = New Label()
        ComboBoxFormatEnregistrement = New ComboBox()
        LabelEnregistrement = New Label()
        ButtonEnregistrement = New Button()
        TrackBarCrossfader = New TrackBar()
        LabelCrossfader = New Label()
        ButtonRetourModeSimple = New Button()
        ButtonParametres = New Button()
        ButtonQuitter = New Button()
        GroupBoxPlaylist = New GroupBox()
        ListViewPlaylist = New ListView()
        ColumnNumDJ = New ColumnHeader()
        ColumnChansonDJ = New ColumnHeader()
        ColumnBPMDJ = New ColumnHeader()
        ColumnDureeDJ = New ColumnHeader()
        ButtonAjouterPiste = New Button()
        ButtonGererPlaylist = New Button()
        GroupBoxDeckA.SuspendLayout()
        CType(TrackBarPositionDeckA, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarVolumeDeckA, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarPitchDeckA, ComponentModel.ISupportInitialize).BeginInit()
        GroupBoxDeckB.SuspendLayout()
        CType(TrackBarPositionDeckB, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarVolumeDeckB, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBarPitchDeckB, ComponentModel.ISupportInitialize).BeginInit()
        GroupBoxMixeur.SuspendLayout()
        CType(TrackBarCrossfader, ComponentModel.ISupportInitialize).BeginInit()
        GroupBoxPlaylist.SuspendLayout()
        SuspendLayout()
        ' 
        ' GroupBoxDeckA
        ' 
        GroupBoxDeckA.Controls.Add(Button_DeckA_BackTo0)
        GroupBoxDeckA.Controls.Add(Label_Avertissement)
        GroupBoxDeckA.Controls.Add(LabelTrackDeckA)
        GroupBoxDeckA.Controls.Add(ButtonPlayDeckA)
        GroupBoxDeckA.Controls.Add(ButtonCueDeckA)
        GroupBoxDeckA.Controls.Add(ButtonStopDeckA)
        GroupBoxDeckA.Controls.Add(TrackBarPositionDeckA)
        GroupBoxDeckA.Controls.Add(LabelDureeDeckA)
        GroupBoxDeckA.Controls.Add(TrackBarVolumeDeckA)
        GroupBoxDeckA.Controls.Add(LabelVolumeDeckA)
        GroupBoxDeckA.Controls.Add(TrackBarPitchDeckA)
        GroupBoxDeckA.Controls.Add(LabelPitchDeckA)
        GroupBoxDeckA.Controls.Add(LabelBPMDeckA)
        GroupBoxDeckA.Controls.Add(VUMeterDeckA)
        GroupBoxDeckA.Controls.Add(CheckBoxPhaserDeckA)
        GroupBoxDeckA.Controls.Add(CheckBoxReverbDeckA)
        GroupBoxDeckA.Controls.Add(CheckBoxEchoDeckA)
        GroupBoxDeckA.Controls.Add(ButtonSyncDeckA)
        GroupBoxDeckA.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        GroupBoxDeckA.Location = New Point(12, 12)
        GroupBoxDeckA.Name = "GroupBoxDeckA"
        GroupBoxDeckA.Size = New Size(480, 494)
        GroupBoxDeckA.TabIndex = 0
        GroupBoxDeckA.TabStop = False
        GroupBoxDeckA.Text = "🎧 PLATINE A"
        ' 
        ' Button_DeckA_BackTo0
        ' 
        Button_DeckA_BackTo0.BackgroundImageLayout = ImageLayout.Stretch
        Button_DeckA_BackTo0.FlatStyle = FlatStyle.Flat
        Button_DeckA_BackTo0.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_DeckA_BackTo0.ImageAlign = ContentAlignment.TopCenter
        Button_DeckA_BackTo0.Location = New Point(354, 233)
        Button_DeckA_BackTo0.Name = "Button_DeckA_BackTo0"
        Button_DeckA_BackTo0.Size = New Size(23, 26)
        Button_DeckA_BackTo0.TabIndex = 17
        Button_DeckA_BackTo0.Text = "0"
        Button_DeckA_BackTo0.UseVisualStyleBackColor = True
        ' 
        ' Label_Avertissement
        ' 
        Label_Avertissement.BackColor = Color.Yellow
        Label_Avertissement.Font = New Font("Segoe UI", 24.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Label_Avertissement.ForeColor = Color.Red
        Label_Avertissement.Location = New Point(162, -12)
        Label_Avertissement.Name = "Label_Avertissement"
        Label_Avertissement.Size = New Size(365, 47)
        Label_Avertissement.TabIndex = 4
        Label_Avertissement.Text = "En développement...."
        ' 
        ' LabelTrackDeckA
        ' 
        LabelTrackDeckA.BackColor = Color.FromArgb(CByte(240), CByte(240), CByte(240))
        LabelTrackDeckA.BorderStyle = BorderStyle.FixedSingle
        LabelTrackDeckA.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        LabelTrackDeckA.Location = New Point(15, 35)
        LabelTrackDeckA.Name = "LabelTrackDeckA"
        LabelTrackDeckA.Size = New Size(450, 47)
        LabelTrackDeckA.TabIndex = 0
        LabelTrackDeckA.Text = "Glissez une piste ici ⬇"
        LabelTrackDeckA.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' ButtonPlayDeckA
        ' 
        ButtonPlayDeckA.Font = New Font("Segoe UI", 16.0F, FontStyle.Bold)
        ButtonPlayDeckA.Location = New Point(56, 97)
        ButtonPlayDeckA.Name = "ButtonPlayDeckA"
        ButtonPlayDeckA.Size = New Size(85, 50)
        ButtonPlayDeckA.TabIndex = 1
        ButtonPlayDeckA.Text = "▶"
        ButtonPlayDeckA.UseVisualStyleBackColor = True
        ' 
        ' ButtonCueDeckA
        ' 
        ButtonCueDeckA.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        ButtonCueDeckA.Location = New Point(200, 97)
        ButtonCueDeckA.Name = "ButtonCueDeckA"
        ButtonCueDeckA.Size = New Size(85, 50)
        ButtonCueDeckA.TabIndex = 2
        ButtonCueDeckA.Text = "CUE"
        ButtonCueDeckA.UseVisualStyleBackColor = True
        ' 
        ' ButtonStopDeckA
        ' 
        ButtonStopDeckA.Font = New Font("Segoe UI", 16.0F, FontStyle.Bold)
        ButtonStopDeckA.Location = New Point(330, 99)
        ButtonStopDeckA.Name = "ButtonStopDeckA"
        ButtonStopDeckA.Size = New Size(85, 50)
        ButtonStopDeckA.TabIndex = 4
        ButtonStopDeckA.Text = "⏹"
        ButtonStopDeckA.UseVisualStyleBackColor = True
        ' 
        ' TrackBarPositionDeckA
        ' 
        TrackBarPositionDeckA.Location = New Point(15, 169)
        TrackBarPositionDeckA.Maximum = 100
        TrackBarPositionDeckA.Name = "TrackBarPositionDeckA"
        TrackBarPositionDeckA.Size = New Size(450, 45)
        TrackBarPositionDeckA.TabIndex = 5
        ' 
        ' LabelDureeDeckA
        ' 
        LabelDureeDeckA.Font = New Font("Segoe UI", 9.0F)
        LabelDureeDeckA.Location = New Point(162, 220)
        LabelDureeDeckA.Name = "LabelDureeDeckA"
        LabelDureeDeckA.Size = New Size(160, 20)
        LabelDureeDeckA.TabIndex = 6
        LabelDureeDeckA.Text = "00:00 / 00:00"
        LabelDureeDeckA.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' TrackBarVolumeDeckA
        ' 
        TrackBarVolumeDeckA.Location = New Point(15, 265)
        TrackBarVolumeDeckA.Maximum = 100
        TrackBarVolumeDeckA.Name = "TrackBarVolumeDeckA"
        TrackBarVolumeDeckA.Size = New Size(200, 45)
        TrackBarVolumeDeckA.TabIndex = 7
        TrackBarVolumeDeckA.Value = 75
        ' 
        ' LabelVolumeDeckA
        ' 
        LabelVolumeDeckA.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        LabelVolumeDeckA.Location = New Point(15, 313)
        LabelVolumeDeckA.Name = "LabelVolumeDeckA"
        LabelVolumeDeckA.Size = New Size(200, 20)
        LabelVolumeDeckA.TabIndex = 8
        LabelVolumeDeckA.Text = "Vol A: 75%"
        LabelVolumeDeckA.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' TrackBarPitchDeckA
        ' 
        TrackBarPitchDeckA.Location = New Point(265, 265)
        TrackBarPitchDeckA.Maximum = 108
        TrackBarPitchDeckA.Minimum = 92
        TrackBarPitchDeckA.Name = "TrackBarPitchDeckA"
        TrackBarPitchDeckA.Size = New Size(200, 45)
        TrackBarPitchDeckA.TabIndex = 9
        TrackBarPitchDeckA.TickFrequency = 2
        TrackBarPitchDeckA.Value = 100
        ' 
        ' LabelPitchDeckA
        ' 
        LabelPitchDeckA.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        LabelPitchDeckA.Location = New Point(265, 313)
        LabelPitchDeckA.Name = "LabelPitchDeckA"
        LabelPitchDeckA.Size = New Size(200, 20)
        LabelPitchDeckA.TabIndex = 10
        LabelPitchDeckA.Text = "Pitch: 0.0%"
        LabelPitchDeckA.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' LabelBPMDeckA
        ' 
        LabelBPMDeckA.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        LabelBPMDeckA.ForeColor = Color.Green
        LabelBPMDeckA.Location = New Point(70, 345)
        LabelBPMDeckA.Name = "LabelBPMDeckA"
        LabelBPMDeckA.Size = New Size(380, 35)
        LabelBPMDeckA.TabIndex = 11
        LabelBPMDeckA.Text = "BPM: --"
        LabelBPMDeckA.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' VUMeterDeckA
        ' 
        VUMeterDeckA.Level = 0F
        VUMeterDeckA.Location = New Point(15, 336)
        VUMeterDeckA.Name = "VUMeterDeckA"
        VUMeterDeckA.Size = New Size(40, 140)
        VUMeterDeckA.TabIndex = 12
        ' 
        ' CheckBoxPhaserDeckA
        ' 
        CheckBoxPhaserDeckA.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        CheckBoxPhaserDeckA.Location = New Point(70, 395)
        CheckBoxPhaserDeckA.Name = "CheckBoxPhaserDeckA"
        CheckBoxPhaserDeckA.Size = New Size(120, 30)
        CheckBoxPhaserDeckA.TabIndex = 13
        CheckBoxPhaserDeckA.Text = "🌀 PHASER"
        CheckBoxPhaserDeckA.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxReverbDeckA
        ' 
        CheckBoxReverbDeckA.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        CheckBoxReverbDeckA.Location = New Point(200, 395)
        CheckBoxReverbDeckA.Name = "CheckBoxReverbDeckA"
        CheckBoxReverbDeckA.Size = New Size(120, 30)
        CheckBoxReverbDeckA.TabIndex = 14
        CheckBoxReverbDeckA.Text = "🎵 REVERB"
        CheckBoxReverbDeckA.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxEchoDeckA
        ' 
        CheckBoxEchoDeckA.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        CheckBoxEchoDeckA.Location = New Point(330, 395)
        CheckBoxEchoDeckA.Name = "CheckBoxEchoDeckA"
        CheckBoxEchoDeckA.Size = New Size(120, 30)
        CheckBoxEchoDeckA.TabIndex = 15
        CheckBoxEchoDeckA.Text = "📢 ECHO"
        CheckBoxEchoDeckA.UseVisualStyleBackColor = True
        ' 
        ' ButtonSyncDeckA
        ' 
        ButtonSyncDeckA.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        ButtonSyncDeckA.Location = New Point(70, 435)
        ButtonSyncDeckA.Name = "ButtonSyncDeckA"
        ButtonSyncDeckA.Size = New Size(380, 40)
        ButtonSyncDeckA.TabIndex = 16
        ButtonSyncDeckA.Text = "🔄 SYNC → B"
        ButtonSyncDeckA.UseVisualStyleBackColor = True
        ' 
        ' GroupBoxDeckB
        ' 
        GroupBoxDeckB.Controls.Add(Button_DeckB_BackTo0)
        GroupBoxDeckB.Controls.Add(LabelTrackDeckB)
        GroupBoxDeckB.Controls.Add(ButtonPlayDeckB)
        GroupBoxDeckB.Controls.Add(ButtonCueDeckB)
        GroupBoxDeckB.Controls.Add(ButtonStopDeckB)
        GroupBoxDeckB.Controls.Add(TrackBarPositionDeckB)
        GroupBoxDeckB.Controls.Add(LabelDureeDeckB)
        GroupBoxDeckB.Controls.Add(TrackBarVolumeDeckB)
        GroupBoxDeckB.Controls.Add(LabelVolumeDeckB)
        GroupBoxDeckB.Controls.Add(TrackBarPitchDeckB)
        GroupBoxDeckB.Controls.Add(LabelPitchDeckB)
        GroupBoxDeckB.Controls.Add(LabelBPMDeckB)
        GroupBoxDeckB.Controls.Add(VUMeterDeckB)
        GroupBoxDeckB.Controls.Add(CheckBoxPhaserDeckB)
        GroupBoxDeckB.Controls.Add(CheckBoxReverbDeckB)
        GroupBoxDeckB.Controls.Add(CheckBoxEchoDeckB)
        GroupBoxDeckB.Controls.Add(ButtonSyncDeckB)
        GroupBoxDeckB.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        GroupBoxDeckB.Location = New Point(530, 12)
        GroupBoxDeckB.Name = "GroupBoxDeckB"
        GroupBoxDeckB.Size = New Size(480, 494)
        GroupBoxDeckB.TabIndex = 1
        GroupBoxDeckB.TabStop = False
        GroupBoxDeckB.Text = "🎧 PLATINE B"
        ' 
        ' Button_DeckB_BackTo0
        ' 
        Button_DeckB_BackTo0.BackgroundImageLayout = ImageLayout.Stretch
        Button_DeckB_BackTo0.FlatStyle = FlatStyle.Flat
        Button_DeckB_BackTo0.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        Button_DeckB_BackTo0.ImageAlign = ContentAlignment.TopCenter
        Button_DeckB_BackTo0.Location = New Point(354, 233)
        Button_DeckB_BackTo0.Name = "Button_DeckB_BackTo0"
        Button_DeckB_BackTo0.Size = New Size(23, 26)
        Button_DeckB_BackTo0.TabIndex = 18
        Button_DeckB_BackTo0.Text = "0"
        Button_DeckB_BackTo0.UseVisualStyleBackColor = True
        ' 
        ' LabelTrackDeckB
        ' 
        LabelTrackDeckB.BackColor = Color.FromArgb(CByte(240), CByte(240), CByte(240))
        LabelTrackDeckB.BorderStyle = BorderStyle.FixedSingle
        LabelTrackDeckB.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        LabelTrackDeckB.Location = New Point(15, 35)
        LabelTrackDeckB.Name = "LabelTrackDeckB"
        LabelTrackDeckB.Size = New Size(450, 47)
        LabelTrackDeckB.TabIndex = 0
        LabelTrackDeckB.Text = "Glissez une piste ici ⬇"
        LabelTrackDeckB.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' ButtonPlayDeckB
        ' 
        ButtonPlayDeckB.Font = New Font("Segoe UI", 16.0F, FontStyle.Bold)
        ButtonPlayDeckB.Location = New Point(53, 99)
        ButtonPlayDeckB.Name = "ButtonPlayDeckB"
        ButtonPlayDeckB.Size = New Size(85, 50)
        ButtonPlayDeckB.TabIndex = 1
        ButtonPlayDeckB.Text = "▶"
        ButtonPlayDeckB.UseVisualStyleBackColor = True
        ' 
        ' ButtonCueDeckB
        ' 
        ButtonCueDeckB.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        ButtonCueDeckB.Location = New Point(200, 99)
        ButtonCueDeckB.Name = "ButtonCueDeckB"
        ButtonCueDeckB.Size = New Size(85, 50)
        ButtonCueDeckB.TabIndex = 3
        ButtonCueDeckB.Text = "CUE"
        ButtonCueDeckB.UseVisualStyleBackColor = True
        ' 
        ' ButtonStopDeckB
        ' 
        ButtonStopDeckB.Font = New Font("Segoe UI", 16.0F, FontStyle.Bold)
        ButtonStopDeckB.Location = New Point(342, 97)
        ButtonStopDeckB.Name = "ButtonStopDeckB"
        ButtonStopDeckB.Size = New Size(85, 50)
        ButtonStopDeckB.TabIndex = 4
        ButtonStopDeckB.Text = "⏹"
        ButtonStopDeckB.UseVisualStyleBackColor = True
        ' 
        ' TrackBarPositionDeckB
        ' 
        TrackBarPositionDeckB.Location = New Point(15, 169)
        TrackBarPositionDeckB.Maximum = 100
        TrackBarPositionDeckB.Name = "TrackBarPositionDeckB"
        TrackBarPositionDeckB.Size = New Size(450, 45)
        TrackBarPositionDeckB.TabIndex = 5
        ' 
        ' LabelDureeDeckB
        ' 
        LabelDureeDeckB.Font = New Font("Segoe UI", 9.0F)
        LabelDureeDeckB.Location = New Point(162, 220)
        LabelDureeDeckB.Name = "LabelDureeDeckB"
        LabelDureeDeckB.Size = New Size(160, 20)
        LabelDureeDeckB.TabIndex = 6
        LabelDureeDeckB.Text = "00:00 / 00:00"
        LabelDureeDeckB.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' TrackBarVolumeDeckB
        ' 
        TrackBarVolumeDeckB.Location = New Point(15, 265)
        TrackBarVolumeDeckB.Maximum = 100
        TrackBarVolumeDeckB.Name = "TrackBarVolumeDeckB"
        TrackBarVolumeDeckB.Size = New Size(200, 45)
        TrackBarVolumeDeckB.TabIndex = 7
        TrackBarVolumeDeckB.Value = 75
        ' 
        ' LabelVolumeDeckB
        ' 
        LabelVolumeDeckB.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        LabelVolumeDeckB.Location = New Point(15, 313)
        LabelVolumeDeckB.Name = "LabelVolumeDeckB"
        LabelVolumeDeckB.Size = New Size(200, 20)
        LabelVolumeDeckB.TabIndex = 8
        LabelVolumeDeckB.Text = "Vol B: 75%"
        LabelVolumeDeckB.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' TrackBarPitchDeckB
        ' 
        TrackBarPitchDeckB.Location = New Point(265, 265)
        TrackBarPitchDeckB.Maximum = 108
        TrackBarPitchDeckB.Minimum = 92
        TrackBarPitchDeckB.Name = "TrackBarPitchDeckB"
        TrackBarPitchDeckB.Size = New Size(200, 45)
        TrackBarPitchDeckB.TabIndex = 9
        TrackBarPitchDeckB.TickFrequency = 2
        TrackBarPitchDeckB.Value = 100
        ' 
        ' LabelPitchDeckB
        ' 
        LabelPitchDeckB.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        LabelPitchDeckB.Location = New Point(265, 313)
        LabelPitchDeckB.Name = "LabelPitchDeckB"
        LabelPitchDeckB.Size = New Size(200, 20)
        LabelPitchDeckB.TabIndex = 10
        LabelPitchDeckB.Text = "Pitch: 0.0%"
        LabelPitchDeckB.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' LabelBPMDeckB
        ' 
        LabelBPMDeckB.Font = New Font("Segoe UI", 14.0F, FontStyle.Bold)
        LabelBPMDeckB.ForeColor = Color.Blue
        LabelBPMDeckB.Location = New Point(85, 345)
        LabelBPMDeckB.Name = "LabelBPMDeckB"
        LabelBPMDeckB.Size = New Size(380, 35)
        LabelBPMDeckB.TabIndex = 11
        LabelBPMDeckB.Text = "BPM: --"
        LabelBPMDeckB.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' VUMeterDeckB
        ' 
        VUMeterDeckB.Level = 0F
        VUMeterDeckB.Location = New Point(15, 336)
        VUMeterDeckB.Name = "VUMeterDeckB"
        VUMeterDeckB.Size = New Size(40, 140)
        VUMeterDeckB.TabIndex = 12
        ' 
        ' CheckBoxPhaserDeckB
        ' 
        CheckBoxPhaserDeckB.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        CheckBoxPhaserDeckB.Location = New Point(85, 395)
        CheckBoxPhaserDeckB.Name = "CheckBoxPhaserDeckB"
        CheckBoxPhaserDeckB.Size = New Size(120, 30)
        CheckBoxPhaserDeckB.TabIndex = 13
        CheckBoxPhaserDeckB.Text = "🌀 PHASER"
        CheckBoxPhaserDeckB.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxReverbDeckB
        ' 
        CheckBoxReverbDeckB.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        CheckBoxReverbDeckB.Location = New Point(215, 395)
        CheckBoxReverbDeckB.Name = "CheckBoxReverbDeckB"
        CheckBoxReverbDeckB.Size = New Size(120, 30)
        CheckBoxReverbDeckB.TabIndex = 14
        CheckBoxReverbDeckB.Text = "🎵 REVERB"
        CheckBoxReverbDeckB.UseVisualStyleBackColor = True
        ' 
        ' CheckBoxEchoDeckB
        ' 
        CheckBoxEchoDeckB.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        CheckBoxEchoDeckB.Location = New Point(345, 395)
        CheckBoxEchoDeckB.Name = "CheckBoxEchoDeckB"
        CheckBoxEchoDeckB.Size = New Size(120, 30)
        CheckBoxEchoDeckB.TabIndex = 15
        CheckBoxEchoDeckB.Text = "📢 ECHO"
        CheckBoxEchoDeckB.UseVisualStyleBackColor = True
        ' 
        ' ButtonSyncDeckB
        ' 
        ButtonSyncDeckB.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        ButtonSyncDeckB.Location = New Point(85, 435)
        ButtonSyncDeckB.Name = "ButtonSyncDeckB"
        ButtonSyncDeckB.Size = New Size(380, 40)
        ButtonSyncDeckB.TabIndex = 16
        ButtonSyncDeckB.Text = "🔄 SYNC → A"
        ButtonSyncDeckB.UseVisualStyleBackColor = True
        ' 
        ' GroupBoxMixeur
        ' 
        GroupBoxMixeur.Controls.Add(LabelDureeEnregistrement)
        GroupBoxMixeur.Controls.Add(ComboBoxFormatEnregistrement)
        GroupBoxMixeur.Controls.Add(LabelEnregistrement)
        GroupBoxMixeur.Controls.Add(ButtonEnregistrement)
        GroupBoxMixeur.Controls.Add(TrackBarCrossfader)
        GroupBoxMixeur.Controls.Add(LabelCrossfader)
        GroupBoxMixeur.Controls.Add(ButtonRetourModeSimple)
        GroupBoxMixeur.Controls.Add(ButtonParametres)
        GroupBoxMixeur.Controls.Add(ButtonQuitter)
        GroupBoxMixeur.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        GroupBoxMixeur.Location = New Point(12, 512)
        GroupBoxMixeur.Name = "GroupBoxMixeur"
        GroupBoxMixeur.Size = New Size(998, 150)
        GroupBoxMixeur.TabIndex = 2
        GroupBoxMixeur.TabStop = False
        GroupBoxMixeur.Text = "🎚️ MIXEUR"
        ' 
        ' LabelDureeEnregistrement
        ' 
        LabelDureeEnregistrement.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        LabelDureeEnregistrement.ForeColor = Color.Red
        LabelDureeEnregistrement.Location = New Point(15, 88)
        LabelDureeEnregistrement.Name = "LabelDureeEnregistrement"
        LabelDureeEnregistrement.Size = New Size(110, 20)
        LabelDureeEnregistrement.TabIndex = 7
        LabelDureeEnregistrement.TextAlign = ContentAlignment.MiddleCenter
        LabelDureeEnregistrement.Visible = False
        ' 
        ' ComboBoxFormatEnregistrement
        ' 
        ComboBoxFormatEnregistrement.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBoxFormatEnregistrement.Font = New Font("Segoe UI", 9.0F)
        ComboBoxFormatEnregistrement.FormattingEnabled = True
        ComboBoxFormatEnregistrement.Location = New Point(860, 53)
        ComboBoxFormatEnregistrement.Name = "ComboBoxFormatEnregistrement"
        ComboBoxFormatEnregistrement.Size = New Size(130, 23)
        ComboBoxFormatEnregistrement.TabIndex = 6
        ' 
        ' LabelEnregistrement
        ' 
        LabelEnregistrement.Font = New Font("Segoe UI", 8.0F)
        LabelEnregistrement.Location = New Point(860, 30)
        LabelEnregistrement.Name = "LabelEnregistrement"
        LabelEnregistrement.Size = New Size(130, 20)
        LabelEnregistrement.TabIndex = 5
        LabelEnregistrement.Text = "Format:"
        LabelEnregistrement.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' ButtonEnregistrement
        ' 
        ButtonEnregistrement.BackColor = Color.FromArgb(CByte(220), CByte(50), CByte(50))
        ButtonEnregistrement.Font = New Font("Segoe UI", 11.0F, FontStyle.Bold)
        ButtonEnregistrement.ForeColor = Color.White
        ButtonEnregistrement.Location = New Point(15, 30)
        ButtonEnregistrement.Name = "ButtonEnregistrement"
        ButtonEnregistrement.Size = New Size(110, 55)
        ButtonEnregistrement.TabIndex = 4
        ButtonEnregistrement.Text = "⬤ REC"
        ButtonEnregistrement.UseVisualStyleBackColor = False
        ' 
        ' TrackBarCrossfader
        ' 
        TrackBarCrossfader.Location = New Point(150, 40)
        TrackBarCrossfader.Maximum = 100
        TrackBarCrossfader.Name = "TrackBarCrossfader"
        TrackBarCrossfader.Size = New Size(700, 45)
        TrackBarCrossfader.TabIndex = 0
        TrackBarCrossfader.TickFrequency = 10
        TrackBarCrossfader.Value = 50
        ' 
        ' LabelCrossfader
        ' 
        LabelCrossfader.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        LabelCrossfader.Location = New Point(150, 88)
        LabelCrossfader.Name = "LabelCrossfader"
        LabelCrossfader.Size = New Size(700, 25)
        LabelCrossfader.TabIndex = 1
        LabelCrossfader.Text = "Crossfader: 50%"
        LabelCrossfader.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' ButtonRetourModeSimple
        ' 
        ButtonRetourModeSimple.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        ButtonRetourModeSimple.Location = New Point(15, 99)
        ButtonRetourModeSimple.Name = "ButtonRetourModeSimple"
        ButtonRetourModeSimple.Size = New Size(110, 40)
        ButtonRetourModeSimple.TabIndex = 2
        ButtonRetourModeSimple.Text = "◀ Mode Simple"
        ButtonRetourModeSimple.UseVisualStyleBackColor = True
        ' 
        ' ButtonParametres
        ' 
        ButtonParametres.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        ButtonParametres.Location = New Point(765, 99)
        ButtonParametres.Name = "ButtonParametres"
        ButtonParametres.Size = New Size(112, 40)
        ButtonParametres.TabIndex = 3
        ButtonParametres.Text = "⚙️ Paramètres"
        ButtonParametres.UseVisualStyleBackColor = True
        ' 
        ' ButtonQuitter
        ' 
        ButtonQuitter.BackColor = Color.FromArgb(CByte(200), CByte(60), CByte(60))
        ButtonQuitter.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        ButtonQuitter.ForeColor = Color.White
        ButtonQuitter.Location = New Point(883, 99)
        ButtonQuitter.Name = "ButtonQuitter"
        ButtonQuitter.Size = New Size(100, 40)
        ButtonQuitter.TabIndex = 4
        ButtonQuitter.Text = "✖ Quitter"
        ButtonQuitter.UseVisualStyleBackColor = False
        ' 
        ' GroupBoxPlaylist
        ' 
        GroupBoxPlaylist.Controls.Add(ListViewPlaylist)
        GroupBoxPlaylist.Controls.Add(ButtonAjouterPiste)
        GroupBoxPlaylist.Controls.Add(ButtonGererPlaylist)
        GroupBoxPlaylist.Font = New Font("Segoe UI", 12.0F, FontStyle.Bold)
        GroupBoxPlaylist.Location = New Point(1020, 12)
        GroupBoxPlaylist.Name = "GroupBoxPlaylist"
        GroupBoxPlaylist.Size = New Size(534, 650)
        GroupBoxPlaylist.TabIndex = 3
        GroupBoxPlaylist.TabStop = False
        GroupBoxPlaylist.Text = "📋 PLAYLIST DJ"
        ' 
        ' ListViewPlaylist
        ' 
        ListViewPlaylist.AllowDrop = True
        ListViewPlaylist.Columns.AddRange(New ColumnHeader() {ColumnNumDJ, ColumnChansonDJ, ColumnBPMDJ, ColumnDureeDJ})
        ListViewPlaylist.FullRowSelect = True
        ListViewPlaylist.GridLines = True
        ListViewPlaylist.Location = New Point(10, 30)
        ListViewPlaylist.MultiSelect = False
        ListViewPlaylist.Name = "ListViewPlaylist"
        ListViewPlaylist.Size = New Size(517, 555)
        ListViewPlaylist.TabIndex = 0
        ListViewPlaylist.UseCompatibleStateImageBehavior = False
        ListViewPlaylist.View = View.Details
        ' 
        ' ColumnNumDJ
        ' 
        ColumnNumDJ.Text = "#"
        ColumnNumDJ.TextAlign = HorizontalAlignment.Center
        ColumnNumDJ.Width = 40
        ' 
        ' ColumnChansonDJ
        ' 
        ColumnChansonDJ.Text = "Chansons"
        ColumnChansonDJ.TextAlign = HorizontalAlignment.Center
        ColumnChansonDJ.Width = 320
        ' 
        ' ColumnBPMDJ
        ' 
        ColumnBPMDJ.Text = "BPM"
        ColumnBPMDJ.TextAlign = HorizontalAlignment.Center
        ColumnBPMDJ.Width = 65
        ' 
        ' ColumnDureeDJ
        ' 
        ColumnDureeDJ.Text = "Durée"
        ColumnDureeDJ.TextAlign = HorizontalAlignment.Center
        ColumnDureeDJ.Width = 80
        ' 
        ' ButtonAjouterPiste
        ' 
        ButtonAjouterPiste.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        ButtonAjouterPiste.Location = New Point(10, 599)
        ButtonAjouterPiste.Name = "ButtonAjouterPiste"
        ButtonAjouterPiste.Size = New Size(160, 40)
        ButtonAjouterPiste.TabIndex = 1
        ButtonAjouterPiste.Text = "➕ Ajouter"
        ButtonAjouterPiste.UseVisualStyleBackColor = True
        ' 
        ' ButtonGererPlaylist
        ' 
        ButtonGererPlaylist.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold)
        ButtonGererPlaylist.Location = New Point(176, 599)
        ButtonGererPlaylist.Name = "ButtonGererPlaylist"
        ButtonGererPlaylist.Size = New Size(160, 40)
        ButtonGererPlaylist.TabIndex = 2
        ButtonGererPlaylist.Text = "📋 Gérer"
        ButtonGererPlaylist.UseVisualStyleBackColor = True
        ' 
        ' FormDJ
        ' 
        AutoScaleDimensions = New SizeF(7.0F, 15.0F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1564, 676)
        Controls.Add(GroupBoxPlaylist)
        Controls.Add(GroupBoxMixeur)
        Controls.Add(GroupBoxDeckB)
        Controls.Add(GroupBoxDeckA)
        FormBorderStyle = FormBorderStyle.FixedSingle
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MinimumSize = New Size(1580, 715)
        Name = "FormDJ"
        StartPosition = FormStartPosition.CenterScreen
        Text = "AudioPlay - Mode Mixeur DJ"
        GroupBoxDeckA.ResumeLayout(False)
        GroupBoxDeckA.PerformLayout()
        CType(TrackBarPositionDeckA, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarVolumeDeckA, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarPitchDeckA, ComponentModel.ISupportInitialize).EndInit()
        GroupBoxDeckB.ResumeLayout(False)
        GroupBoxDeckB.PerformLayout()
        CType(TrackBarPositionDeckB, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarVolumeDeckB, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBarPitchDeckB, ComponentModel.ISupportInitialize).EndInit()
        GroupBoxMixeur.ResumeLayout(False)
        GroupBoxMixeur.PerformLayout()
        CType(TrackBarCrossfader, ComponentModel.ISupportInitialize).EndInit()
        GroupBoxPlaylist.ResumeLayout(False)
        ResumeLayout(False)
    End Sub

    ' === DECK A ===
    Friend WithEvents GroupBoxDeckA As GroupBox
    Friend WithEvents LabelTrackDeckA As Label
    Friend WithEvents ButtonPlayDeckA As Button
    Friend WithEvents ButtonCueDeckA As Button
    Friend WithEvents ButtonStopDeckA As Button
    Friend WithEvents TrackBarPositionDeckA As TrackBar
    Friend WithEvents LabelDureeDeckA As Label
    Friend WithEvents TrackBarVolumeDeckA As TrackBar
    Friend WithEvents LabelVolumeDeckA As Label
    Friend WithEvents TrackBarPitchDeckA As TrackBar
    Friend WithEvents LabelPitchDeckA As Label
    Friend WithEvents LabelBPMDeckA As Label
    Friend WithEvents VUMeterDeckA As VUMeterControl
    Friend WithEvents CheckBoxPhaserDeckA As CheckBox
    Friend WithEvents CheckBoxReverbDeckA As CheckBox
    Friend WithEvents CheckBoxEchoDeckA As CheckBox
    Friend WithEvents ButtonSyncDeckA As Button

    ' === DECK B ===
    Friend WithEvents GroupBoxDeckB As GroupBox
    Friend WithEvents LabelTrackDeckB As Label
    Friend WithEvents ButtonPlayDeckB As Button
    Friend WithEvents ButtonCueDeckB As Button
    Friend WithEvents ButtonStopDeckB As Button
    Friend WithEvents TrackBarPositionDeckB As TrackBar
    Friend WithEvents LabelDureeDeckB As Label
    Friend WithEvents TrackBarVolumeDeckB As TrackBar
    Friend WithEvents LabelVolumeDeckB As Label
    Friend WithEvents TrackBarPitchDeckB As TrackBar
    Friend WithEvents LabelPitchDeckB As Label
    Friend WithEvents LabelBPMDeckB As Label
    Friend WithEvents VUMeterDeckB As VUMeterControl
    Friend WithEvents CheckBoxPhaserDeckB As CheckBox
    Friend WithEvents CheckBoxReverbDeckB As CheckBox
    Friend WithEvents CheckBoxEchoDeckB As CheckBox
    Friend WithEvents ButtonSyncDeckB As Button

    ' === MIXEUR ===
    Friend WithEvents GroupBoxMixeur As GroupBox
    Friend WithEvents TrackBarCrossfader As TrackBar
    Friend WithEvents LabelCrossfader As Label
    Friend WithEvents ButtonRetourModeSimple As Button
    Friend WithEvents ButtonParametres As Button
    Friend WithEvents ButtonQuitter As Button
    Friend WithEvents ButtonEnregistrement As Button
    Friend WithEvents LabelEnregistrement As Label
    Friend WithEvents ComboBoxFormatEnregistrement As ComboBox
    Friend WithEvents LabelDureeEnregistrement As Label

    ' === PLAYLIST DJ ===
    Friend WithEvents GroupBoxPlaylist As GroupBox
    Friend WithEvents ListViewPlaylist As ListView
    Friend ColumnNumDJ As ColumnHeader
    Friend ColumnChansonDJ As ColumnHeader
    Friend ColumnBPMDJ As ColumnHeader
    Friend ColumnDureeDJ As ColumnHeader
    Friend WithEvents ButtonAjouterPiste As Button
    Friend WithEvents ButtonGererPlaylist As Button
    Friend WithEvents Label_Avertissement As Label
    Friend WithEvents Button_DeckA_BackTo0 As Button
    Friend WithEvents Button_DeckB_BackTo0 As Button
End Class
