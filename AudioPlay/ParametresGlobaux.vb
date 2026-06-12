Public Module ParametresGlobaux
    Public ConfirmerEffacementChansons As Boolean = True

    ' === Effets audio ===
    ' Reverb
    Public EffetReverbActif As Boolean = False
    Public EffetReverbMix As Single = 0.3F ' 0.0 à 1.0

    ' Echo
    Public EffetEchoActif As Boolean = False
    Public EffetEchoMix As Single = 0.3F ' 0.0 à 1.0
    Public EffetEchoDelai As Integer = 300 ' ms (50 à 2000)
    Public EffetEchoFeedback As Single = 0.5F ' 0.0 à 0.9

    ' Time Stretch
    Public EffetTimeStretchActif As Boolean = False
    Public EffetTimeStretchRatio As Single = 1.0F ' 0.5 à 2.0

    ' Pitch Shift
    Public EffetPitchShiftActif As Boolean = False
    Public EffetPitchShiftSemiTones As Single = 0.0F ' -12 à +12 demi-tons

    ' Phaser
    Public EffetPhaserActif As Boolean = False
    Public EffetPhaserRate As Single = 0.5F ' 0.1 à 10.0 Hz - Vitesse modérée classique
    Public EffetPhaserDepth As Single = 0.7F ' 0.0 à 1.0 - Balayage audible mais musical (était 1.0)
    Public EffetPhaserFeedback As Single = 0.3F ' 0.0 à 0.95 - Résonance douce vintage (était 0.5)
    Public EffetPhaserMix As Single = 0.5F ' 0.0 à 1.0 - Équilibre parfait dry/wet (était 1.0)
    Public EffetPhaserStages As Integer = 4 ' 2, 4, 6, 8, 12 - Son vintage classique

    ' Mode Mixeur DJ
    Public ModeMixeurDJ As Boolean = False ' Mode lecteur simple (False) ou mixeur DJ (True)
End Module
