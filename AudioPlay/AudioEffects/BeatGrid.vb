Imports System.Threading.Tasks

''' <summary>
''' Classe pour gérer la grille de beats d'une piste audio
''' Permet la synchronisation continue et le quantize pour un mixage DJ précis
''' Supporte la détection des downbeats (premiers beats de mesure) style Virtual DJ / Serato
''' </summary>
Public Class BeatGrid
    ' Position des beats en secondes
    Public Property Beats As List(Of Double)

    ' Position des downbeats (premiers beats de chaque mesure) en secondes
    Public Property Downbeats As List(Of Double)

    ' Signature rythmique (4 = 4/4, 3 = 3/4, etc.)
    Public Property TimeSignature As Integer

    ' BPM de la piste (BPM actuel ajusté)
    Public Property BPM As Double

    ' BPM de base (avant tout ajustement de tempo) - utilisé pour recalculer correctement
    Public Property BPMBase As Double

    ' Durée d'un beat en secondes
    Public ReadOnly Property BeatDuration As Double
        Get
            If BPM > 0 Then
                Return 60.0 / BPM
            End If
            Return 0
        End Get
    End Property

    ' Nombre total de beats
    Public ReadOnly Property BeatCount As Integer
        Get
            If Beats Is Nothing Then Return 0
            Return Beats.Count
        End Get
    End Property

    ' Nombre de downbeats
    Public ReadOnly Property DownbeatCount As Integer
        Get
            If Downbeats Is Nothing Then Return 0
            Return Downbeats.Count
        End Get
    End Property

    ''' <summary>
    ''' Constructeur vide
    ''' </summary>
    Public Sub New()
        Beats = New List(Of Double)()
        Downbeats = New List(Of Double)()
        TimeSignature = 4 ' Par défaut 4/4
        BPM = 0
    End Sub

    ''' <summary>
    ''' Constructeur avec BPM connu
    ''' Génère une grille de beats régulière basée sur le BPM
    ''' </summary>
    Public Sub New(bpm As Double, dureeTotale As Double)
        Me.BPM = bpm
        Me.BPMBase = bpm ' Stocker le BPM de base pour ajustements futurs
        Beats = New List(Of Double)()
        Downbeats = New List(Of Double)()
        TimeSignature = 4 ' Par défaut 4/4

        If bpm <= 0 OrElse dureeTotale <= 0 Then
            Return
        End If

        ' Générer les beats à intervalles réguliers
        Dim beatDuration As Double = 60.0 / bpm
        Dim currentTime As Double = 0
        Dim beatIndex As Integer = 0

        While currentTime < dureeTotale
            Beats.Add(currentTime)

            ' Marquer les downbeats (tous les N beats selon la signature)
            If beatIndex Mod TimeSignature = 0 Then
                Downbeats.Add(currentTime)
            End If

            currentTime += beatDuration
            beatIndex += 1
        End While

        Debug.WriteLine($"BeatGrid générée : {Beats.Count} beats, {Downbeats.Count} downbeats pour {dureeTotale:F1}s ({bpm:F1} BPM, {TimeSignature}/4)")
    End Sub

    ''' <summary>
    ''' Trouve le beat le plus proche d'une position donnée
    ''' </summary>
    Public Function TrouverBeatLePlusProche(position As Double) As Double
        If Beats Is Nothing OrElse Beats.Count = 0 Then
            Return position
        End If

        ' Recherche binaire du beat le plus proche
        Dim index As Integer = Beats.BinarySearch(position)

        If index >= 0 Then
            ' Position exacte trouvée
            Return Beats(index)
        Else
            ' Pas de correspondance exacte, trouver le plus proche
            index = Not index

            If index = 0 Then
                ' Avant le premier beat
                Return Beats(0)
            ElseIf index >= Beats.Count Then
                ' Après le dernier beat
                Return Beats(Beats.Count - 1)
            Else
                ' Entre deux beats, choisir le plus proche
                Dim beatAvant As Double = Beats(index - 1)
                Dim beatApres As Double = Beats(index)

                If Math.Abs(position - beatAvant) < Math.Abs(position - beatApres) Then
                    Return beatAvant
                Else
                    Return beatApres
                End If
            End If
        End If
    End Function

    ''' <summary>
    ''' Trouve le prochain beat après une position donnée
    ''' </summary>
    Public Function TrouverProchainBeat(position As Double) As Double
        If Beats Is Nothing OrElse Beats.Count = 0 Then
            Return position
        End If

        For Each beat In Beats
            If beat > position Then
                Return beat
            End If
        Next

        ' Si aucun beat trouvé après, retourner le dernier
        Return Beats(Beats.Count - 1)
    End Function

    ''' <summary>
    ''' Trouve le beat précédent avant une position donnée
    ''' </summary>
    Public Function TrouverBeatPrecedent(position As Double) As Double
        If Beats Is Nothing OrElse Beats.Count = 0 Then
            Return position
        End If

        For i = Beats.Count - 1 To 0 Step -1
            If Beats(i) < position Then
                Return Beats(i)
            End If
        Next

        ' Si aucun beat trouvé avant, retourner le premier
        Return Beats(0)
    End Function

    ''' <summary>
    ''' Trouve le downbeat le plus proche d'une position donnée
    ''' </summary>
    Public Function TrouverDownbeatLePlusProche(position As Double) As Double
        If Downbeats Is Nothing OrElse Downbeats.Count = 0 Then
            ' Fallback : utiliser les beats normaux
            Return TrouverBeatLePlusProche(position)
        End If

        ' Recherche binaire du downbeat le plus proche
        Dim index As Integer = Downbeats.BinarySearch(position)

        If index >= 0 Then
            Return Downbeats(index)
        Else
            index = Not index

            If index = 0 Then
                Return Downbeats(0)
            ElseIf index >= Downbeats.Count Then
                Return Downbeats(Downbeats.Count - 1)
            Else
                Dim downbeatAvant As Double = Downbeats(index - 1)
                Dim downbeatApres As Double = Downbeats(index)

                If Math.Abs(position - downbeatAvant) < Math.Abs(position - downbeatApres) Then
                    Return downbeatAvant
                Else
                    Return downbeatApres
                End If
            End If
        End If
    End Function

    ''' <summary>
    ''' Trouve le prochain downbeat après une position donnée
    ''' </summary>
    Public Function TrouverProchainDownbeat(position As Double) As Double
        If Downbeats Is Nothing OrElse Downbeats.Count = 0 Then
            ' Fallback : utiliser les beats normaux
            Return TrouverProchainBeat(position)
        End If

        For Each downbeat In Downbeats
            If downbeat > position Then
                Return downbeat
            End If
        Next

        ' Si aucun downbeat trouvé après, retourner le dernier
        Return Downbeats(Downbeats.Count - 1)
    End Function

    ''' <summary>
    ''' Trouve le downbeat précédent avant une position donnée
    ''' </summary>
    Public Function TrouverDownbeatPrecedent(position As Double) As Double
        If Downbeats Is Nothing OrElse Downbeats.Count = 0 Then
            ' Fallback : utiliser les beats normaux
            Return TrouverBeatPrecedent(position)
        End If

        For i = Downbeats.Count - 1 To 0 Step -1
            If Downbeats(i) < position Then
                Return Downbeats(i)
            End If
        Next

        ' Si aucun downbeat trouvé avant, retourner le premier
        Return Downbeats(0)
    End Function

    ''' <summary>
    ''' Vérifie si une position est sur un downbeat (à ±100ms près)
    ''' </summary>
    Public Function EstSurDownbeat(position As Double, Optional tolerance As Double = 0.1) As Boolean
        If Downbeats Is Nothing OrElse Downbeats.Count = 0 Then
            Return False
        End If

        For Each downbeat In Downbeats
            If Math.Abs(position - downbeat) <= tolerance Then
                Return True
            End If
        Next

        Return False
    End Function

    ''' <summary>
    ''' Calcule le décalage (drift) entre la position actuelle et le downbeat le plus proche
    ''' </summary>
    Public Function CalculerDriftDownbeat(position As Double) As Double
        If Downbeats Is Nothing OrElse Downbeats.Count = 0 Then
            Return CalculerDrift(position)
        End If

        Dim downbeatPlusProche As Double = TrouverDownbeatLePlusProche(position)
        Return position - downbeatPlusProche
    End Function

    ''' <summary>
    ''' Calcule la position de la mesure (phrase) actuelle (0.0 à 1.0)
    ''' 0.0 = début de mesure (downbeat), 1.0 = fin de mesure
    ''' </summary>
    Public Function CalculerPhrasePhase(position As Double) As Double
        If Downbeats Is Nothing OrElse Downbeats.Count < 2 Then
            Return 0
        End If

        ' Trouver le downbeat précédent
        Dim downbeatPrecedent As Double = TrouverDownbeatPrecedent(position)
        Dim downbeatSuivant As Double = TrouverProchainDownbeat(position)

        ' Calculer la phase dans la phrase
        Dim dureeMesure As Double = downbeatSuivant - downbeatPrecedent
        If dureeMesure <= 0 Then
            Return 0
        End If

        Dim tempsDepuisDownbeat As Double = position - downbeatPrecedent
        Dim phase As Double = tempsDepuisDownbeat / dureeMesure

        Return Math.Max(0, Math.Min(1.0, phase))
    End Function

    ''' <summary>
    ''' Calcule le drift entre les beats le plus proche
    ''' Retourne la différence en secondes (positif = en avance, négatif = en retard)
    ''' </summary>
    Public Function CalculerDrift(position As Double) As Double
        If Beats Is Nothing OrElse Beats.Count = 0 Then
            Return 0
        End If

        Dim beatPlusProche As Double = TrouverBeatLePlusProche(position)
        Return position - beatPlusProche
    End Function

    ''' <summary>
    ''' Calcule la phase actuelle dans le cycle de beat (0.0 à 1.0)
    ''' 0.0 = début du beat, 0.5 = milieu, 1.0 = fin du beat
    ''' </summary>
    Public Function CalculerPhase(position As Double) As Double
        If BeatDuration <= 0 Then
            Return 0
        End If

        ' Trouver le beat précédent
        Dim beatPrecedent As Double = TrouverBeatPrecedent(position)

        ' Calculer la phase dans le cycle
        Dim tempsDepuisBeat As Double = position - beatPrecedent
        Dim phase As Double = tempsDepuisBeat / BeatDuration

        ' Normaliser entre 0.0 et 1.0
        Return Math.Max(0, Math.Min(1.0, phase))
    End Function

    ''' <summary>
    ''' Vérifie si la position est proche d'un beat (à ±50ms près)
    ''' </summary>
    Public Function EstProcheDUnBeat(position As Double, Optional tolerance As Double = 0.05) As Boolean
        If Beats Is Nothing OrElse Beats.Count = 0 Then
            Return False
        End If

        Dim drift As Double = Math.Abs(CalculerDrift(position))
        Return drift < tolerance
    End Function

    ''' <summary>
    ''' Quantize : ajuste une position pour qu'elle tombe exactement sur un beat
    ''' </summary>
    Public Function Quantize(position As Double) As Double
        Return TrouverBeatLePlusProche(position)
    End Function

    ''' <summary>
    ''' Ajuste la grille de beats en fonction d'un changement de tempo
    ''' (utilisé quand le pitch change)
    ''' </summary>
    ''' <summary>
    ''' Ajuster la grille de beats pour un changement de tempo
    ''' CORRIGÉ : Utilise maintenant le BPM de base au lieu d'accumuler les multiplications
    ''' </summary>
    ''' <param name="nouveauBPM">Le nouveau BPM absolu (pas un ratio)</param>
    Public Sub AjusterPourBPM(nouveauBPM As Double)
        If Beats Is Nothing OrElse Beats.Count = 0 OrElse nouveauBPM <= 0 Then
            Return
        End If

        ' Calculer le ratio de changement depuis le BPM actuel
        Dim ratio As Double = nouveauBPM / BPM

        ' Mettre à jour le BPM
        BPM = nouveauBPM

        ' Recalculer les positions de beats avec le nouveau ratio
        Dim nouvellesPositions As New List(Of Double)()
        For Each position In Beats
            ' Ajuster la position : si on accélère (ratio > 1), les beats arrivent plus tôt
            nouvellesPositions.Add(position / ratio)
        Next

        ' Remplacer les beats
        Beats = nouvellesPositions

        ' Recalculer les downbeats avec le même ratio
        Dim nouveauxDownbeats As New List(Of Double)()
        For Each position In Downbeats
            nouveauxDownbeats.Add(position / ratio)
        Next
        Downbeats = nouveauxDownbeats

        Debug.WriteLine($"BeatGrid ajustée pour nouveau BPM {nouveauBPM:F1} (ratio {ratio:F3}): {Beats.Count} beats")
    End Sub

    ''' <summary>
    ''' OBSOLÈTE : Utiliser AjusterPourBPM() à la place
    ''' Ajuster la grille de beats pour un changement de tempo (ratio)
    ''' </summary>
    <Obsolete("Utiliser AjusterPourBPM(nouveauBPM As Double) à la place pour éviter les erreurs d'accumulation")>
    Public Sub AjusterPourTempo(tempoChange As Single)
        If Beats Is Nothing OrElse Beats.Count = 0 OrElse tempoChange <= 0 Then
            Return
        End If

        ' Ajuster le BPM (ANCIEN CODE : accumule les erreurs)
        BPM = BPM * tempoChange

        ' Recalculer les positions de beats
        Dim anciennesDurees As New List(Of Double)()
        For i = 1 To Beats.Count - 1
            anciennesDurees.Add(Beats(i) - Beats(i - 1))
        Next

        ' Réinitialiser les beats
        Beats.Clear()
        Dim position As Double = 0
        Beats.Add(position)

        ' Recalculer avec le nouveau tempo
        For Each duree In anciennesDurees
            position += duree / tempoChange
            Beats.Add(position)
        Next

        Debug.WriteLine($"BeatGrid ajustée pour tempo {tempoChange:F2}: {BPM:F1} BPM, {Beats.Count} beats")
    End Sub

    ''' <summary>
    ''' Génère un rapport de diagnostic
    ''' </summary>
    Public Function ObtenirDiagnostic() As String
        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine($"BeatGrid Diagnostic:")
        sb.AppendLine($"  BPM: {BPM:F2}")
        sb.AppendLine($"  Beat Duration: {BeatDuration:F3}s")
        sb.AppendLine($"  Total Beats: {BeatCount}")

        If Beats IsNot Nothing AndAlso Beats.Count > 0 Then
            sb.AppendLine($"  Premier beat: {Beats(0):F3}s")
            sb.AppendLine($"  Dernier beat: {Beats(Beats.Count - 1):F3}s")
        End If

        Return sb.ToString()
    End Function
End Class
