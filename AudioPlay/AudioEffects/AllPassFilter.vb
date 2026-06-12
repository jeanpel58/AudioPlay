''' <summary>
''' Filtre all-pass de premier ordre pour effet Phaser analogique
''' Basé sur les circuits classiques des phasers vintage (MXR Phase 90, etc.)
''' </summary>
Public Class AllPassFilter
    ' Coefficient et états du filtre
    Private a As Single
    Private zm1 As Single = 0.0F ' État précédent

    Private ReadOnly sampleRate As Integer
    Private currentFrequency As Single

    ''' <summary>
    ''' Crée un nouveau filtre all-pass
    ''' </summary>
    Public Sub New(sampleRate As Integer, Optional frequency As Single = 1000.0F)
        Me.sampleRate = sampleRate
        SetFrequency(frequency)
    End Sub

    ''' <summary>
    ''' Définit la fréquence du filtre avec interpolation douce
    ''' </summary>
    Public Sub SetFrequency(frequency As Single)
        ' Calculer le coefficient all-pass classique
        ' Formule standard: a = (tan(πf/fs) - 1) / (tan(πf/fs) + 1)
        Dim omega = Math.PI * frequency / sampleRate
        Dim tanOmega = Math.Tan(omega)

        ' Limiter tan pour éviter les valeurs extrêmes
        tanOmega = Math.Max(0.001, Math.Min(1000.0, tanOmega))

        a = CSng((tanOmega - 1.0) / (tanOmega + 1.0))

        ' Forcer dans la plage de stabilité
        a = Math.Max(-0.99F, Math.Min(0.99F, a))

        currentFrequency = frequency
    End Sub

    ''' <summary>
    ''' Traite un échantillon - formule all-pass classique
    ''' </summary>
    Public Function Process(input As Single) As Single
        ' Formule all-pass standard (Chamberlin)
        ' y[n] = a * x[n] + x[n-1] - a * y[n-1]
        Dim output = a * input + zm1
        zm1 = input - a * output

        Return output
    End Function

    ''' <summary>
    ''' Réinitialise l'état
    ''' </summary>
    Public Sub Reset()
        zm1 = 0.0F
    End Sub
End Class
