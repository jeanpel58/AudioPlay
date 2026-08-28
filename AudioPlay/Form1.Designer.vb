<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Form1))
        GroupBox1 = New GroupBox()
        Button_AudioPlay_Aide = New Button()
        Button_APropos = New Button()
        TrackBar_Aigues = New TrackBar()
        Label3 = New Label()
        TrackBar_Basses = New TrackBar()
        Label1 = New Label()
        TrackBar_Volume = New TrackBar()
        LabelVolume = New Label()
        Label2 = New Label()
        Label_DureeRestante = New Label()
        Label_SampleRate = New Label()
        LabelSampleRateTitre = New Label()
        Label_Bitrate = New Label()
        LabelBitrateTitre = New Label()
        TextBox_Display = New TextBox()
        GroupBox2 = New GroupBox()
        Button_Aleatoire = New Button()
        Button_Power = New Button()
        Button_Suivant = New Button()
        Button_Precedent = New Button()
        Button_Arreter = New Button()
        Button_Jouer = New Button()
        Button_PauseReprise = New Button()
        Button_CalculBPM = New Button()
        Button_Mute = New Button()
        Button_InfoSelect = New Button()
        Button_Ajout = New Button()
        Button_Playlist = New Button()
        GroupBox3 = New GroupBox()
        Button_ClearRecherche = New Button()
        TextBox_Recherche = New TextBox()
        ComboBox_TypeRecherche = New ComboBox()
        ListView1 = New ListView()
        Num = New ColumnHeader()
        Chansons = New ColumnHeader()
        BPM = New ColumnHeader()
        Durée = New ColumnHeader()
        GroupBox4 = New GroupBox()
        Button_Loop_Aide = New Button()
        Button_Loop = New Button()
        Button_Parametres = New Button()
        GroupBox_Avancement = New GroupBox()
        TrackBar_Avancement = New TrackBar()
        ButtonModeDJ = New Button()
        GroupBox1.SuspendLayout()
        CType(TrackBar_Aigues, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBar_Basses, ComponentModel.ISupportInitialize).BeginInit()
        CType(TrackBar_Volume, ComponentModel.ISupportInitialize).BeginInit()
        GroupBox2.SuspendLayout()
        GroupBox3.SuspendLayout()
        GroupBox4.SuspendLayout()
        GroupBox_Avancement.SuspendLayout()
        CType(TrackBar_Avancement, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' GroupBox1
        ' 
        GroupBox1.Controls.Add(Button_AudioPlay_Aide)
        GroupBox1.Controls.Add(Button_APropos)
        GroupBox1.Controls.Add(TrackBar_Aigues)
        GroupBox1.Controls.Add(Label3)
        GroupBox1.Controls.Add(TrackBar_Basses)
        GroupBox1.Controls.Add(Label1)
        GroupBox1.Controls.Add(TrackBar_Volume)
        GroupBox1.Controls.Add(LabelVolume)
        GroupBox1.Controls.Add(Label2)
        GroupBox1.Controls.Add(Label_DureeRestante)
        GroupBox1.Controls.Add(Label_SampleRate)
        GroupBox1.Controls.Add(LabelSampleRateTitre)
        GroupBox1.Controls.Add(Label_Bitrate)
        GroupBox1.Controls.Add(LabelBitrateTitre)
        GroupBox1.Controls.Add(TextBox_Display)
        GroupBox1.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        GroupBox1.Location = New Point(12, 6)
        GroupBox1.Name = "GroupBox1"
        GroupBox1.Size = New Size(534, 117)
        GroupBox1.TabIndex = 0
        GroupBox1.TabStop = False
        ' 
        ' Button_AudioPlay_Aide
        ' 
        Button_AudioPlay_Aide.BackgroundImageLayout = ImageLayout.Stretch
        Button_AudioPlay_Aide.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_AudioPlay_Aide.FlatAppearance.BorderSize = 0
        Button_AudioPlay_Aide.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_AudioPlay_Aide.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_AudioPlay_Aide.FlatStyle = FlatStyle.Flat
        Button_AudioPlay_Aide.Font = New Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_AudioPlay_Aide.Location = New Point(407, 14)
        Button_AudioPlay_Aide.Name = "Button_AudioPlay_Aide"
        Button_AudioPlay_Aide.Size = New Size(30, 30)
        Button_AudioPlay_Aide.TabIndex = 12
        Button_AudioPlay_Aide.Text = "?"
        Button_AudioPlay_Aide.UseVisualStyleBackColor = False
        ' 
        ' Button_APropos
        ' 
        Button_APropos.BackColor = Color.Transparent
        Button_APropos.BackgroundImageLayout = ImageLayout.Stretch
        Button_APropos.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_APropos.FlatAppearance.BorderSize = 0
        Button_APropos.FlatAppearance.MouseDownBackColor = Color.Red
        Button_APropos.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_APropos.FlatStyle = FlatStyle.Flat
        Button_APropos.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Button_APropos.Location = New Point(443, 14)
        Button_APropos.Name = "Button_APropos"
        Button_APropos.Size = New Size(80, 30)
        Button_APropos.TabIndex = 11
        Button_APropos.Text = "À propos..."
        Button_APropos.UseVisualStyleBackColor = False
        ' 
        ' TrackBar_Aigues
        ' 
        TrackBar_Aigues.AutoSize = False
        TrackBar_Aigues.Location = New Point(415, 89)
        TrackBar_Aigues.Name = "TrackBar_Aigues"
        TrackBar_Aigues.Size = New Size(94, 19)
        TrackBar_Aigues.TabIndex = 8
        ' 
        ' Label3
        ' 
        Label3.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Label3.Location = New Point(415, 71)
        Label3.Name = "Label3"
        Label3.Size = New Size(94, 17)
        Label3.TabIndex = 7
        Label3.Text = "Aigues"
        Label3.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' TrackBar_Basses
        ' 
        TrackBar_Basses.AutoSize = False
        TrackBar_Basses.Location = New Point(302, 88)
        TrackBar_Basses.Name = "TrackBar_Basses"
        TrackBar_Basses.Size = New Size(94, 19)
        TrackBar_Basses.TabIndex = 6
        ' 
        ' Label1
        ' 
        Label1.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Label1.Location = New Point(302, 71)
        Label1.Name = "Label1"
        Label1.Size = New Size(94, 17)
        Label1.TabIndex = 5
        Label1.Text = "Basses"
        Label1.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' TrackBar_Volume
        ' 
        TrackBar_Volume.AutoSize = False
        TrackBar_Volume.Location = New Point(138, 88)
        TrackBar_Volume.Maximum = 100
        TrackBar_Volume.Name = "TrackBar_Volume"
        TrackBar_Volume.Size = New Size(150, 19)
        TrackBar_Volume.TabIndex = 4
        TrackBar_Volume.TickFrequency = 10
        TrackBar_Volume.Value = 50
        ' 
        ' LabelVolume
        ' 
        LabelVolume.Font = New Font("Microsoft Sans Serif", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        LabelVolume.Location = New Point(138, 71)
        LabelVolume.Name = "LabelVolume"
        LabelVolume.Size = New Size(150, 17)
        LabelVolume.TabIndex = 3
        LabelVolume.Text = "Volume"
        LabelVolume.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Label2
        ' 
        Label2.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Label2.Location = New Point(18, 71)
        Label2.Name = "Label2"
        Label2.Size = New Size(95, 17)
        Label2.TabIndex = 2
        Label2.Text = "Durée restante "
        Label2.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Label_DureeRestante
        ' 
        Label_DureeRestante.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Label_DureeRestante.Location = New Point(18, 88)
        Label_DureeRestante.Name = "Label_DureeRestante"
        Label_DureeRestante.Size = New Size(95, 16)
        Label_DureeRestante.TabIndex = 1
        Label_DureeRestante.Text = "00:00"
        Label_DureeRestante.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' Label_SampleRate
        ' 
        Label_SampleRate.FlatStyle = FlatStyle.Flat
        Label_SampleRate.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold)
        Label_SampleRate.ImageAlign = ContentAlignment.MiddleLeft
        Label_SampleRate.Location = New Point(276, 21)
        Label_SampleRate.Name = "Label_SampleRate"
        Label_SampleRate.Size = New Size(80, 16)
        Label_SampleRate.TabIndex = 8
        Label_SampleRate.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' LabelSampleRateTitre
        ' 
        LabelSampleRateTitre.FlatStyle = FlatStyle.Flat
        LabelSampleRateTitre.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold)
        LabelSampleRateTitre.Location = New Point(151, 22)
        LabelSampleRateTitre.Name = "LabelSampleRateTitre"
        LabelSampleRateTitre.Size = New Size(119, 15)
        LabelSampleRateTitre.TabIndex = 7
        LabelSampleRateTitre.Text = "Échantillonnage :"
        LabelSampleRateTitre.TextAlign = ContentAlignment.MiddleRight
        ' 
        ' Label_Bitrate
        ' 
        Label_Bitrate.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold)
        Label_Bitrate.Location = New Point(65, 21)
        Label_Bitrate.Name = "Label_Bitrate"
        Label_Bitrate.Size = New Size(80, 16)
        Label_Bitrate.TabIndex = 10
        Label_Bitrate.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' LabelBitrateTitre
        ' 
        LabelBitrateTitre.FlatStyle = FlatStyle.Flat
        LabelBitrateTitre.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold)
        LabelBitrateTitre.Location = New Point(7, 22)
        LabelBitrateTitre.Name = "LabelBitrateTitre"
        LabelBitrateTitre.Size = New Size(56, 15)
        LabelBitrateTitre.TabIndex = 9
        LabelBitrateTitre.Text = "Bitrate :"
        LabelBitrateTitre.TextAlign = ContentAlignment.MiddleRight
        ' 
        ' TextBox_Display
        ' 
        TextBox_Display.BackColor = Color.LightCyan
        TextBox_Display.BorderStyle = BorderStyle.None
        TextBox_Display.Font = New Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0)
        TextBox_Display.Location = New Point(7, 50)
        TextBox_Display.Name = "TextBox_Display"
        TextBox_Display.ReadOnly = True
        TextBox_Display.Size = New Size(516, 18)
        TextBox_Display.TabIndex = 0
        TextBox_Display.WordWrap = False
        ' 
        ' GroupBox2
        ' 
        GroupBox2.Controls.Add(Button_Aleatoire)
        GroupBox2.Controls.Add(Button_Power)
        GroupBox2.Controls.Add(Button_Suivant)
        GroupBox2.Controls.Add(Button_Precedent)
        GroupBox2.Controls.Add(Button_Arreter)
        GroupBox2.Controls.Add(Button_Jouer)
        GroupBox2.Controls.Add(Button_PauseReprise)
        GroupBox2.Controls.Add(Button_CalculBPM)
        GroupBox2.Controls.Add(Button_Mute)
        GroupBox2.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        GroupBox2.Location = New Point(13, 129)
        GroupBox2.Name = "GroupBox2"
        GroupBox2.Size = New Size(533, 54)
        GroupBox2.TabIndex = 1
        GroupBox2.TabStop = False
        ' 
        ' Button_Aleatoire
        ' 
        Button_Aleatoire.BackColor = Color.Transparent
        Button_Aleatoire.BackgroundImageLayout = ImageLayout.Stretch
        Button_Aleatoire.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Aleatoire.FlatAppearance.BorderSize = 0
        Button_Aleatoire.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_Aleatoire.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_Aleatoire.FlatStyle = FlatStyle.Flat
        Button_Aleatoire.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Aleatoire.Location = New Point(285, 8)
        Button_Aleatoire.Name = "Button_Aleatoire"
        Button_Aleatoire.Size = New Size(40, 40)
        Button_Aleatoire.TabIndex = 0
        Button_Aleatoire.Text = "Random"
        Button_Aleatoire.UseVisualStyleBackColor = False
        ' 
        ' Button_Power
        ' 
        Button_Power.BackColor = Color.Transparent
        Button_Power.BackgroundImageLayout = ImageLayout.Stretch
        Button_Power.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Power.FlatAppearance.BorderSize = 0
        Button_Power.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_Power.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_Power.FlatStyle = FlatStyle.Flat
        Button_Power.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Power.Location = New Point(487, 8)
        Button_Power.Name = "Button_Power"
        Button_Power.Size = New Size(40, 40)
        Button_Power.TabIndex = 1
        Button_Power.Text = "Power"
        Button_Power.UseVisualStyleBackColor = False
        ' 
        ' Button_Suivant
        ' 
        Button_Suivant.BackColor = Color.Transparent
        Button_Suivant.BackgroundImageLayout = ImageLayout.Stretch
        Button_Suivant.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Suivant.FlatAppearance.BorderSize = 0
        Button_Suivant.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_Suivant.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_Suivant.FlatStyle = FlatStyle.Flat
        Button_Suivant.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Suivant.Location = New Point(55, 8)
        Button_Suivant.Name = "Button_Suivant"
        Button_Suivant.Size = New Size(40, 40)
        Button_Suivant.TabIndex = 2
        Button_Suivant.Text = "Suivant"
        Button_Suivant.UseVisualStyleBackColor = False
        ' 
        ' Button_Precedent
        ' 
        Button_Precedent.BackColor = Color.Transparent
        Button_Precedent.BackgroundImageLayout = ImageLayout.Stretch
        Button_Precedent.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Precedent.FlatAppearance.BorderSize = 0
        Button_Precedent.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_Precedent.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_Precedent.FlatStyle = FlatStyle.Flat
        Button_Precedent.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Precedent.Location = New Point(9, 8)
        Button_Precedent.Name = "Button_Precedent"
        Button_Precedent.Size = New Size(40, 40)
        Button_Precedent.TabIndex = 3
        Button_Precedent.Text = "Précédent"
        Button_Precedent.UseVisualStyleBackColor = False
        ' 
        ' Button_Arreter
        ' 
        Button_Arreter.BackColor = Color.Transparent
        Button_Arreter.BackgroundImageLayout = ImageLayout.Stretch
        Button_Arreter.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Arreter.FlatAppearance.BorderSize = 0
        Button_Arreter.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_Arreter.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_Arreter.FlatStyle = FlatStyle.Flat
        Button_Arreter.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Arreter.Location = New Point(239, 8)
        Button_Arreter.Name = "Button_Arreter"
        Button_Arreter.Size = New Size(40, 40)
        Button_Arreter.TabIndex = 4
        Button_Arreter.Text = "Arrêter"
        Button_Arreter.UseVisualStyleBackColor = False
        ' 
        ' Button_Jouer
        ' 
        Button_Jouer.BackColor = Color.Transparent
        Button_Jouer.BackgroundImageLayout = ImageLayout.Stretch
        Button_Jouer.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Jouer.FlatAppearance.BorderSize = 0
        Button_Jouer.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_Jouer.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_Jouer.FlatStyle = FlatStyle.Flat
        Button_Jouer.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Jouer.Location = New Point(101, 8)
        Button_Jouer.Name = "Button_Jouer"
        Button_Jouer.Size = New Size(40, 40)
        Button_Jouer.TabIndex = 5
        Button_Jouer.Text = "Jouer"
        Button_Jouer.UseVisualStyleBackColor = False
        ' 
        ' Button_PauseReprise
        ' 
        Button_PauseReprise.BackColor = Color.Transparent
        Button_PauseReprise.BackgroundImageLayout = ImageLayout.Stretch
        Button_PauseReprise.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_PauseReprise.FlatAppearance.BorderSize = 0
        Button_PauseReprise.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_PauseReprise.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_PauseReprise.FlatStyle = FlatStyle.Flat
        Button_PauseReprise.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_PauseReprise.Location = New Point(147, 8)
        Button_PauseReprise.Name = "Button_PauseReprise"
        Button_PauseReprise.Size = New Size(40, 40)
        Button_PauseReprise.TabIndex = 6
        Button_PauseReprise.Text = "Pause"
        Button_PauseReprise.UseVisualStyleBackColor = False
        ' 
        ' Button_CalculBPM
        ' 
        Button_CalculBPM.BackColor = Color.Transparent
        Button_CalculBPM.BackgroundImageLayout = ImageLayout.Stretch
        Button_CalculBPM.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_CalculBPM.FlatAppearance.BorderSize = 0
        Button_CalculBPM.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_CalculBPM.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_CalculBPM.FlatStyle = FlatStyle.Flat
        Button_CalculBPM.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_CalculBPM.Location = New Point(331, 8)
        Button_CalculBPM.Name = "Button_CalculBPM"
        Button_CalculBPM.Size = New Size(40, 40)
        Button_CalculBPM.TabIndex = 7
        Button_CalculBPM.Text = "BPM"
        Button_CalculBPM.UseVisualStyleBackColor = False
        ' 
        ' Button_Mute
        ' 
        Button_Mute.BackColor = Color.Transparent
        Button_Mute.BackgroundImageLayout = ImageLayout.Stretch
        Button_Mute.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Mute.FlatAppearance.BorderSize = 0
        Button_Mute.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_Mute.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_Mute.FlatStyle = FlatStyle.Flat
        Button_Mute.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Mute.Location = New Point(193, 8)
        Button_Mute.Name = "Button_Mute"
        Button_Mute.Size = New Size(40, 40)
        Button_Mute.TabIndex = 8
        Button_Mute.Text = "Mute"
        Button_Mute.UseVisualStyleBackColor = False
        ' 
        ' Button_InfoSelect
        ' 
        Button_InfoSelect.BackColor = Color.Transparent
        Button_InfoSelect.BackgroundImageLayout = ImageLayout.Stretch
        Button_InfoSelect.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_InfoSelect.FlatAppearance.BorderSize = 0
        Button_InfoSelect.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_InfoSelect.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_InfoSelect.FlatStyle = FlatStyle.Flat
        Button_InfoSelect.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_InfoSelect.Location = New Point(84, 8)
        Button_InfoSelect.Name = "Button_InfoSelect"
        Button_InfoSelect.Size = New Size(35, 35)
        Button_InfoSelect.TabIndex = 2
        Button_InfoSelect.Text = "Info"
        Button_InfoSelect.UseVisualStyleBackColor = False
        ' 
        ' Button_Ajout
        ' 
        Button_Ajout.BackColor = Color.Transparent
        Button_Ajout.BackgroundImageLayout = ImageLayout.Stretch
        Button_Ajout.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Ajout.FlatAppearance.BorderSize = 0
        Button_Ajout.FlatStyle = FlatStyle.Flat
        Button_Ajout.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Ajout.Location = New Point(48, 8)
        Button_Ajout.Name = "Button_Ajout"
        Button_Ajout.Size = New Size(35, 35)
        Button_Ajout.TabIndex = 3
        Button_Ajout.Text = "Ajout"
        Button_Ajout.UseVisualStyleBackColor = False
        ' 
        ' Button_Playlist
        ' 
        Button_Playlist.BackColor = Color.Transparent
        Button_Playlist.BackgroundImageLayout = ImageLayout.Stretch
        Button_Playlist.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Playlist.FlatAppearance.BorderSize = 0
        Button_Playlist.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_Playlist.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_Playlist.FlatStyle = FlatStyle.Flat
        Button_Playlist.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Playlist.Location = New Point(120, 8)
        Button_Playlist.Name = "Button_Playlist"
        Button_Playlist.Size = New Size(35, 35)
        Button_Playlist.TabIndex = 1
        Button_Playlist.Text = "Playlist"
        Button_Playlist.UseVisualStyleBackColor = False
        ' 
        ' GroupBox3
        ' 
        GroupBox3.Controls.Add(Button_ClearRecherche)
        GroupBox3.Controls.Add(TextBox_Recherche)
        GroupBox3.Controls.Add(ComboBox_TypeRecherche)
        GroupBox3.Controls.Add(ListView1)
        GroupBox3.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        GroupBox3.Location = New Point(13, 189)
        GroupBox3.Name = "GroupBox3"
        GroupBox3.Size = New Size(534, 467)
        GroupBox3.TabIndex = 2
        GroupBox3.TabStop = False
        ' 
        ' Button_ClearRecherche
        ' 
        Button_ClearRecherche.BackColor = Color.Transparent
        Button_ClearRecherche.BackgroundImageLayout = ImageLayout.Stretch
        Button_ClearRecherche.Cursor = Cursors.Hand
        Button_ClearRecherche.FlatAppearance.BorderColor = Color.Black
        Button_ClearRecherche.FlatAppearance.BorderSize = 2
        Button_ClearRecherche.FlatAppearance.MouseDownBackColor = Color.Red
        Button_ClearRecherche.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_ClearRecherche.FlatStyle = FlatStyle.Flat
        Button_ClearRecherche.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Button_ClearRecherche.Location = New Point(500, 15)
        Button_ClearRecherche.Name = "Button_ClearRecherche"
        Button_ClearRecherche.Size = New Size(25, 25)
        Button_ClearRecherche.TabIndex = 4
        Button_ClearRecherche.Text = "✕"
        Button_ClearRecherche.UseVisualStyleBackColor = False
        ' 
        ' TextBox_Recherche
        ' 
        TextBox_Recherche.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        TextBox_Recherche.Location = New Point(149, 18)
        TextBox_Recherche.Name = "TextBox_Recherche"
        TextBox_Recherche.PlaceholderText = "Rechercher..."
        TextBox_Recherche.Size = New Size(349, 22)
        TextBox_Recherche.TabIndex = 3
        ' 
        ' ComboBox_TypeRecherche
        ' 
        ComboBox_TypeRecherche.DropDownStyle = ComboBoxStyle.DropDownList
        ComboBox_TypeRecherche.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        ComboBox_TypeRecherche.FormattingEnabled = True
        ComboBox_TypeRecherche.Location = New Point(8, 18)
        ComboBox_TypeRecherche.Name = "ComboBox_TypeRecherche"
        ComboBox_TypeRecherche.Size = New Size(135, 21)
        ComboBox_TypeRecherche.TabIndex = 2
        ' 
        ' ListView1
        ' 
        ListView1.BackColor = Color.LightCyan
        ListView1.BorderStyle = BorderStyle.None
        ListView1.Columns.AddRange(New ColumnHeader() {Num, Chansons, BPM, Durée})
        ListView1.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        ListView1.FullRowSelect = True
        ListView1.GridLines = True
        ListView1.Location = New Point(8, 47)
        ListView1.Name = "ListView1"
        ListView1.OwnerDraw = True
        ListView1.Size = New Size(517, 281)
        ListView1.TabIndex = 1
        ListView1.UseCompatibleStateImageBehavior = False
        ListView1.View = View.Details
        ' 
        ' Num
        ' 
        Num.Text = "#"
        Num.TextAlign = HorizontalAlignment.Center
        Num.Width = 40
        ' 
        ' Chansons
        ' 
        Chansons.Text = "Chansons"
        Chansons.TextAlign = HorizontalAlignment.Center
        Chansons.Width = 320
        ' 
        ' BPM
        ' 
        BPM.Text = "BPM"
        BPM.TextAlign = HorizontalAlignment.Center
        ' 
        ' Durée
        ' 
        Durée.Text = "Durée"
        Durée.TextAlign = HorizontalAlignment.Center
        Durée.Width = 80
        ' 
        ' GroupBox4
        ' 
        GroupBox4.Controls.Add(ButtonModeDJ)
        GroupBox4.Controls.Add(Button_Loop_Aide)
        GroupBox4.Controls.Add(Button_Loop)
        GroupBox4.Controls.Add(Button_Parametres)
        GroupBox4.Controls.Add(Button_Playlist)
        GroupBox4.Controls.Add(Button_InfoSelect)
        GroupBox4.Controls.Add(Button_Ajout)
        GroupBox4.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        GroupBox4.Location = New Point(14, 720)
        GroupBox4.Name = "GroupBox4"
        GroupBox4.Size = New Size(533, 50)
        GroupBox4.TabIndex = 3
        GroupBox4.TabStop = False
        ' 
        ' ButtonModeDJ
        ' 
        ButtonModeDJ.BackColor = Color.Transparent
        ButtonModeDJ.BackgroundImageLayout = ImageLayout.Stretch
        ButtonModeDJ.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        ButtonModeDJ.FlatAppearance.BorderSize = 0
        ButtonModeDJ.FlatAppearance.MouseDownBackColor = Color.Transparent
        ButtonModeDJ.FlatAppearance.MouseOverBackColor = Color.Transparent
        ButtonModeDJ.FlatStyle = FlatStyle.Flat
        ButtonModeDJ.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        ButtonModeDJ.Location = New Point(12, 8)
        ButtonModeDJ.Name = "ButtonModeDJ"
        ButtonModeDJ.Size = New Size(35, 35)
        ButtonModeDJ.TabIndex = 6
        ButtonModeDJ.Text = "DJ Mixer"
        ButtonModeDJ.UseVisualStyleBackColor = False
        ' 
        ' Button_Loop_Aide
        ' 
        Button_Loop_Aide.BackColor = Color.Transparent
        Button_Loop_Aide.BackgroundImageLayout = ImageLayout.Stretch
        Button_Loop_Aide.FlatAppearance.MouseDownBackColor = Color.Red
        Button_Loop_Aide.FlatAppearance.MouseOverBackColor = Color.Lime
        Button_Loop_Aide.FlatStyle = FlatStyle.Flat
        Button_Loop_Aide.Font = New Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point, 0)
        Button_Loop_Aide.Location = New Point(301, 15)
        Button_Loop_Aide.Name = "Button_Loop_Aide"
        Button_Loop_Aide.Size = New Size(58, 25)
        Button_Loop_Aide.TabIndex = 5
        Button_Loop_Aide.Text = "Aide"
        Button_Loop_Aide.UseVisualStyleBackColor = False
        ' 
        ' Button_Loop
        ' 
        Button_Loop.BackColor = Color.Transparent
        Button_Loop.BackgroundImageLayout = ImageLayout.Stretch
        Button_Loop.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Loop.FlatAppearance.BorderSize = 0
        Button_Loop.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_Loop.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_Loop.FlatStyle = FlatStyle.Flat
        Button_Loop.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Loop.Location = New Point(260, 8)
        Button_Loop.Name = "Button_Loop"
        Button_Loop.Size = New Size(35, 35)
        Button_Loop.TabIndex = 4
        Button_Loop.Text = "Loop"
        Button_Loop.UseVisualStyleBackColor = False
        ' 
        ' Button_Parametres
        ' 
        Button_Parametres.BackColor = Color.Transparent
        Button_Parametres.BackgroundImageLayout = ImageLayout.Stretch
        Button_Parametres.FlatAppearance.BorderColor = Color.FromArgb(224, 224, 224)
        Button_Parametres.FlatAppearance.BorderSize = 0
        Button_Parametres.FlatAppearance.MouseDownBackColor = Color.Transparent
        Button_Parametres.FlatAppearance.MouseOverBackColor = Color.Transparent
        Button_Parametres.FlatStyle = FlatStyle.Flat
        Button_Parametres.Font = New Font("Segoe UI", 6.75F, FontStyle.Regular, GraphicsUnit.Point, 0)
        Button_Parametres.Location = New Point(492, 8)
        Button_Parametres.Name = "Button_Parametres"
        Button_Parametres.Size = New Size(35, 35)
        Button_Parametres.TabIndex = 0
        Button_Parametres.Text = "Param"
        Button_Parametres.UseVisualStyleBackColor = False
        ' 
        ' GroupBox_Avancement
        ' 
        GroupBox_Avancement.Controls.Add(TrackBar_Avancement)
        GroupBox_Avancement.Font = New Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0)
        GroupBox_Avancement.Location = New Point(14, 662)
        GroupBox_Avancement.Name = "GroupBox_Avancement"
        GroupBox_Avancement.Size = New Size(533, 52)
        GroupBox_Avancement.TabIndex = 4
        GroupBox_Avancement.TabStop = False
        ' 
        ' TrackBar_Avancement
        ' 
        TrackBar_Avancement.AutoSize = False
        TrackBar_Avancement.Location = New Point(9, 22)
        TrackBar_Avancement.Name = "TrackBar_Avancement"
        TrackBar_Avancement.RightToLeft = RightToLeft.No
        TrackBar_Avancement.Size = New Size(501, 21)
        TrackBar_Avancement.TabIndex = 0
        TrackBar_Avancement.TickStyle = TickStyle.None
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.LightBlue
        BackgroundImageLayout = ImageLayout.Stretch
        ClientSize = New Size(557, 786)
        Controls.Add(GroupBox_Avancement)
        Controls.Add(GroupBox4)
        Controls.Add(GroupBox3)
        Controls.Add(GroupBox2)
        Controls.Add(GroupBox1)
        FormBorderStyle = FormBorderStyle.FixedSingle
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MaximizeBox = False
        Name = "Form1"
        StartPosition = FormStartPosition.CenterScreen
        Text = "AudioPlay "
        GroupBox1.ResumeLayout(False)
        GroupBox1.PerformLayout()
        CType(TrackBar_Aigues, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBar_Basses, ComponentModel.ISupportInitialize).EndInit()
        CType(TrackBar_Volume, ComponentModel.ISupportInitialize).EndInit()
        GroupBox2.ResumeLayout(False)
        GroupBox3.ResumeLayout(False)
        GroupBox3.PerformLayout()
        GroupBox4.ResumeLayout(False)
        GroupBox_Avancement.ResumeLayout(False)
        CType(TrackBar_Avancement, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents TextBox_Display As TextBox
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents Label_DureeRestante As Label
    Friend WithEvents Label_SampleRate As Label
    Friend WithEvents LabelSampleRateTitre As Label
    Friend WithEvents Label_Bitrate As Label
    Friend WithEvents LabelBitrateTitre As Label
    Friend WithEvents LabelVolume As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Button_Arreter As Button
    Friend WithEvents Button_Jouer As Button
    Friend WithEvents Button_PauseReprise As Button
    Friend WithEvents Button_CalculBPM As Button
    Friend WithEvents Button_Ajout As Button
    Friend WithEvents Button_Playlist As Button
    Friend WithEvents ListView1 As ListView
    Friend WithEvents TextBox_Recherche As TextBox
    Friend WithEvents ComboBox_TypeRecherche As ComboBox
    Friend WithEvents Button_ClearRecherche As Button

    Friend WithEvents Button_Aleatoire As Button
    Friend WithEvents Button_InfoSelect As Button
    Friend WithEvents Button_Parametres As Button
    Friend WithEvents Button_Mute As Button
    Friend WithEvents Chansons As ColumnHeader
    Friend WithEvents BPM As ColumnHeader
    Friend WithEvents Durée As ColumnHeader
    Friend WithEvents Num As ColumnHeader
    Friend WithEvents GroupBox_Avancement As GroupBox
    Friend WithEvents TrackBar_Avancement As TrackBar
    Friend WithEvents TrackBar_Volume As TrackBar
    Friend WithEvents TrackBar_Aigues As TrackBar
    Friend WithEvents Label3 As Label
    Friend WithEvents TrackBar_Basses As TrackBar
    Friend WithEvents Label1 As Label
    Friend WithEvents Button_APropos As Button
    Friend WithEvents Button_Suivant As Button
    Friend WithEvents Button_Precedent As Button
    Friend WithEvents Button_Power As Button
    Friend WithEvents Button_Loop As Button
    Friend WithEvents Button_Loop_Aide As Button
    Friend WithEvents Button_AudioPlay_Aide As Button
    Friend WithEvents ButtonModeDJ As Button

End Class


