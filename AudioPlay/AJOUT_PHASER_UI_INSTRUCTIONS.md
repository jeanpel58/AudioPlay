# Ajout des contrôles Phaser dans FormParametres.Designer.vb

## Modifications à faire

### 1. Agrandir GroupBoxEffetsAudio
Ligne ~578:
```vb
' AVANT
GroupBoxEffetsAudio.Size = New Size(460, 570)

' APRÈS  
GroupBoxEffetsAudio.Size = New Size(460, 750)  ' +180 pour Phaser
```

### 2. Ajouter les contrôles dans GroupBoxEffetsAudio.Controls.Add
Après la ligne 574 (`ButtonResetPitchShift`), ajouter:
```vb
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
```

### 3. Initialiser les contrôles Phaser
Après ButtonResetPitchShift (ligne ~858), ajouter:

```vb
' 
' CheckBoxPhaserActif
' 
CheckBoxPhaserActif.AutoSize = True
CheckBoxPhaserActif.Font = New Font("Segoe UI", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
CheckBoxPhaserActif.Location = New Point(6, 490)
CheckBoxPhaserActif.Name = "CheckBoxPhaserActif"
CheckBoxPhaserActif.Size = New Size(65, 19)
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
LabelPhaserRate.Size = New Size(73, 15)
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
TrackBarPhaserRate.Size = New Size(300, 35)
TrackBarPhaserRate.TabIndex = 32
TrackBarPhaserRate.TickFrequency = 10
TrackBarPhaserRate.Value = 5
' 
' LabelPhaserRateValeur
' 
LabelPhaserRateValeur.AutoSize = True
LabelPhaserRateValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
LabelPhaserRateValeur.Location = New Point(312, 538)
LabelPhaserRateValeur.Name = "LabelPhaserRateValeur"
LabelPhaserRateValeur.Size = New Size(25, 15)
LabelPhaserRateValeur.TabIndex = 33
LabelPhaserRateValeur.Text = "0.5"
' 
' LabelPhaserDepth
' 
LabelPhaserDepth.AutoSize = True
LabelPhaserDepth.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
LabelPhaserDepth.Location = New Point(6, 568)
LabelPhaserDepth.Name = "LabelPhaserDepth"
LabelPhaserDepth.Size = New Size(72, 15)
LabelPhaserDepth.TabIndex = 34
LabelPhaserDepth.Text = "Profondeur:"
' 
' TrackBarPhaserDepth
' 
TrackBarPhaserDepth.AutoSize = False
TrackBarPhaserDepth.Location = New Point(6, 586)
TrackBarPhaserDepth.Maximum = 100
TrackBarPhaserDepth.Name = "TrackBarPhaserDepth"
TrackBarPhaserDepth.Size = New Size(300, 35)
TrackBarPhaserDepth.TabIndex = 35
TrackBarPhaserDepth.TickFrequency = 10
TrackBarPhaserDepth.Value = 50
' 
' LabelPhaserDepthValeur
' 
LabelPhaserDepthValeur.AutoSize = True
LabelPhaserDepthValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
LabelPhaserDepthValeur.Location = New Point(312, 594)
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
LabelPhaserFeedback.Size = New Size(61, 15)
LabelPhaserFeedback.TabIndex = 37
LabelPhaserFeedback.Text = "Feedback:"
' 
' TrackBarPhaserFeedback
' 
TrackBarPhaserFeedback.AutoSize = False
TrackBarPhaserFeedback.Location = New Point(6, 642)
TrackBarPhaserFeedback.Maximum = 95
TrackBarPhaserFeedback.Name = "TrackBarPhaserFeedback"
TrackBarPhaserFeedback.Size = New Size(300, 35)
TrackBarPhaserFeedback.TabIndex = 38
TrackBarPhaserFeedback.TickFrequency = 10
TrackBarPhaserFeedback.Value = 30
' 
' LabelPhaserFeedbackValeur
' 
LabelPhaserFeedbackValeur.AutoSize = True
LabelPhaserFeedbackValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
LabelPhaserFeedbackValeur.Location = New Point(312, 650)
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
LabelPhaserMix.Size = New Size(32, 15)
LabelPhaserMix.TabIndex = 40
LabelPhaserMix.Text = "Mix :"
' 
' TrackBarPhaserMix
' 
TrackBarPhaserMix.AutoSize = False
TrackBarPhaserMix.Location = New Point(6, 698)
TrackBarPhaserMix.Maximum = 100
TrackBarPhaserMix.Name = "TrackBarPhaserMix"
TrackBarPhaserMix.Size = New Size(300, 35)
TrackBarPhaserMix.TabIndex = 41
TrackBarPhaserMix.TickFrequency = 10
TrackBarPhaserMix.Value = 50
' 
' LabelPhaserMixValeur
' 
LabelPhaserMixValeur.AutoSize = True
LabelPhaserMixValeur.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
LabelPhaserMixValeur.Location = New Point(312, 706)
LabelPhaserMixValeur.Name = "LabelPhaserMixValeur"
LabelPhaserMixValeur.Size = New Size(29, 15)
LabelPhaserMixValeur.TabIndex = 42
LabelPhaserMixValeur.Text = "50%"
' 
' LabelPhaserStages
' 
LabelPhaserStages.AutoSize = True
LabelPhaserStages.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
LabelPhaserStages.Location = New Point(350, 512)
LabelPhaserStages.Name = "LabelPhaserStages"
LabelPhaserStages.Size = New Size(45, 15)
LabelPhaserStages.TabIndex = 43
LabelPhaserStages.Text = "Stages:"
' 
' ComboBoxPhaserStages
' 
ComboBoxPhaserStages.DropDownStyle = ComboBoxStyle.DropDownList
ComboBoxPhaserStages.Font = New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
ComboBoxPhaserStages.FormattingEnabled = True
ComboBoxPhaserStages.Items.AddRange(New Object() {"2", "4", "6", "8", "12"})
ComboBoxPhaserStages.Location = New Point(350, 530)
ComboBoxPhaserStages.Name = "ComboBoxPhaserStages"
ComboBoxPhaserStages.Size = New Size(80, 23)
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
ButtonResetPhaser.Location = New Point(350, 698)
ButtonResetPhaser.Name = "ButtonResetPhaser"
ButtonResetPhaser.Size = New Size(80, 32)
ButtonResetPhaser.TabIndex = 45
ButtonResetPhaser.Text = "Reset"
ButtonResetPhaser.UseVisualStyleBackColor = False
```

### 4. Ajouter BeginInit/EndInit pour les nouveaux TrackBars
Après ligne ~99 (BeginInit pour TrackBarPitchShift), ajouter:
```vb
CType(TrackBarPhaserRate, ComponentModel.ISupportInitialize).BeginInit()
CType(TrackBarPhaserDepth, ComponentModel.ISupportInitialize).BeginInit()
CType(TrackBarPhaserFeedback, ComponentModel.ISupportInitialize).BeginInit()
CType(TrackBarPhaserMix, ComponentModel.ISupportInitialize).BeginInit()
```

Après ligne ~894 (EndInit pour TrackBarPitchShift), ajouter:
```vb
CType(TrackBarPhaserRate, ComponentModel.ISupportInitialize).EndInit()
CType(TrackBarPhaserDepth, ComponentModel.ISupportInitialize).EndInit()
CType(TrackBarPhaserFeedback, ComponentModel.ISupportInitialize).EndInit()
CType(TrackBarPhaserMix, ComponentModel.ISupportInitialize).EndInit()
```

---

Ces modifications ajouteront tous les contrôles UI nécessaires pour le Phaser avec un layout propre et cohérent avec les autres effets.
