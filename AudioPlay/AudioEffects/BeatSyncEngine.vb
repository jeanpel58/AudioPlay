Imports System.Threading
Imports NAudio.Wave

''' <summary>
''' Moteur de synchronisation de beats pour maintenir l'alignement sur la durée
''' Corrige automatiquement le drift entre deux decks DJ
''' </summary>
Public Class BeatSyncEngine
    ' Grilles de beats pour chaque deck
    Private beatGridDeckA As BeatGrid
    Private beatGridDeckB As BeatGrid

    ' Références aux lecteurs audio
    Private fichierAudioDeckA As AudioFileReader
    Private fichierAudioDeckB As AudioFileReader

    ' Timer pour la synchronisation continue
    Private syncTimer As Timer
    Private syncInterval As Integer = 100 ' 100ms entre chaque vérification (2x plus rapide que 200ms)

    ' === PARAMÈTRES DE BEAT QUANTIZE AVANCÉ (style Virtual DJ / Serato) ===

    ' Tolérance de drift avant correction (en secondes)
    ' VALEURS PLUS TOLÉRANTES pour éviter les corrections trop agressives qui créent du drift
    Private driftTolerance As Double = 0.030 ' 30ms - seuil pour déclencher une correction (était 15ms)
    Private driftDeadZone As Double = 0.015 ' 15ms - zone morte où aucune correction n'est appliquée (était 8ms)
    Private driftMinimal As Double = 0.008 ' 8ms - drift minimal détectable, filtrage du bruit (était 3ms)

    ' Paramètres de lissage temporel
    Private Const HISTORIQUE_TAILLE As Integer = 5 ' Garder 5 mesures de drift (était 10, trop lent)
    Private driftHistoriqueDeckA As New Queue(Of Double)(HISTORIQUE_TAILLE)
    Private driftHistoriqueDeckB As New Queue(Of Double)(HISTORIQUE_TAILLE)

    ' Tempo smoothing : garder la dernière correction pour transition douce
    Private dernierTempoAjustementDeckA As Single = 0.0F
    Private dernierTempoAjustementDeckB As Single = 0.0F
    Private Const TEMPO_SMOOTH_FACTOR As Single = 0.2F ' 20% du nouveau, 80% de l'ancien (réduit pour éviter sur-correction)

    ' Compteur de cycles de correction pour correction progressive
    Private cyclesCorrectionDeckA As Integer = 0
    Private cyclesCorrectionDeckB As Integer = 0
    Private Const CYCLES_AVANT_CORRECTION_COMPLETE As Integer = 5 ' 5 cycles = 0.5 seconde (augmenté pour plus de stabilité)

    ' État de synchronisation
    Private _syncActifDeckA As Boolean = False
    Private _syncActifDeckB As Boolean = False

    ' Callbacks pour ajuster le tempo (pitch bend) au lieu de sauter la position
    Public Event TempoDeckAAjuste(tempoAjustement As Single)
    Public Event TempoDeckBAjuste(tempoAjustement As Single)

    ' Statistiques de drift
    Public Property DriftDeckA As Double = 0
    Public Property DriftDeckB As Double = 0
    Public Property DriftMoyenDeckA As Double = 0 ' Drift lissé
    Public Property DriftMoyenDeckB As Double = 0 ' Drift lissé
    Public Property CorrectionsAppliqueesDeckA As Integer = 0
    Public Property CorrectionsAppliqueesDeckB As Integer = 0

    ''' <summary>
    ''' Activer/désactiver le sync continu pour Deck A
    ''' </summary>
    Public Property SyncActifDeckA As Boolean
        Get
            Return _syncActifDeckA
        End Get
        Set(value As Boolean)
            _syncActifDeckA = value
            Debug.WriteLine($"BeatSync Deck A: {If(value, "ACTIF", "INACTIF")}")
            VerifierEtatTimer()
        End Set
    End Property

    ''' <summary>
    ''' Activer/désactiver le sync continu pour Deck B
    ''' </summary>
    Public Property SyncActifDeckB As Boolean
        Get
            Return _syncActifDeckB
        End Get
        Set(value As Boolean)
            _syncActifDeckB = value
            Debug.WriteLine($"BeatSync Deck B: {If(value, "ACTIF", "INACTIF")}")
            VerifierEtatTimer()
        End Set
    End Property

    ''' <summary>
    ''' Constructeur
    ''' </summary>
    Public Sub New()
        ' Timer sera créé à la demande
    End Sub

    ''' <summary>
    ''' Initialiser les grilles de beats pour les deux decks
    ''' </summary>
    Public Sub InitialiserBeatGrids(bpmA As Double, dureeA As Double, bpmB As Double, dureeB As Double,
                                     audioA As AudioFileReader, audioB As AudioFileReader)
        ' Créer les grilles de beats
        beatGridDeckA = New BeatGrid(bpmA, dureeA)
        beatGridDeckB = New BeatGrid(bpmB, dureeB)

        ' Stocker les références audio
        fichierAudioDeckA = audioA
        fichierAudioDeckB = audioB

        Debug.WriteLine("BeatSync: Grilles de beats initialisées")
        Debug.WriteLine(beatGridDeckA.ObtenirDiagnostic())
        Debug.WriteLine(beatGridDeckB.ObtenirDiagnostic())
    End Sub

    ''' <summary>
    ''' Réinitialiser les BeatGrids sans effacer l'historique de correction
    ''' Utilisé lors des re-clics SYNC pour préserver la continuité des corrections
    ''' </summary>
    Public Sub ResynchoniserBeatGrids(bpmA As Double, dureeA As Double, bpmB As Double, dureeB As Double)
        ' CORRECTION CRITIQUE : Ne PAS utiliser AjusterPourBPM() qui déforme les positions !
        ' À la place, RECRÉER les grilles avec les bons BPM effectifs
        ' L'historique de correction (driftHistoriqueDeckA/B) est préservé car ce sont des variables séparées

        beatGridDeckA = New BeatGrid(bpmA, dureeA)
        beatGridDeckB = New BeatGrid(bpmB, dureeB)

        Debug.WriteLine($"BeatSync: Grilles RECRÉÉES avec BPM effectifs - A={bpmA:F3}, B={bpmB:F3} (historique préservé)")
    End Sub

    ''' <summary>
    ''' Mettre à jour la grille de beats d'un deck après changement de BPM
    ''' </summary>
    ''' <param name="nouveauBPM">Le nouveau BPM absolu (BPM de base * (1 + pitch))</param>
    Public Sub MettreAJourBeatGridDeckA(nouveauBPM As Double)
        If beatGridDeckA IsNot Nothing AndAlso nouveauBPM > 0 Then
            beatGridDeckA.AjusterPourBPM(nouveauBPM)
            Debug.WriteLine($"BeatSync: BeatGrid Deck A mis à jour avec BPM {nouveauBPM:F3}")
        End If
    End Sub

    ''' <summary>
    ''' Mettre à jour la grille de beats d'un deck après changement de BPM
    ''' </summary>
    ''' <param name="nouveauBPM">Le nouveau BPM absolu (BPM de base * (1 + pitch))</param>
    Public Sub MettreAJourBeatGridDeckB(nouveauBPM As Double)
        If beatGridDeckB IsNot Nothing AndAlso nouveauBPM > 0 Then
            beatGridDeckB.AjusterPourBPM(nouveauBPM)
            Debug.WriteLine($"BeatSync: BeatGrid Deck B mis à jour avec BPM {nouveauBPM:F3}")
        End If
    End Sub

    ''' <summary>
    ''' Vérifier et démarrer/arrêter le timer selon l'état des syncs
    ''' </summary>
    Private Sub VerifierEtatTimer()
        If _syncActifDeckA OrElse _syncActifDeckB Then
            ' Au moins un deck est en sync, démarrer le timer
            If syncTimer Is Nothing Then
                syncTimer = New Timer(AddressOf VerifierSync, Nothing, syncInterval, syncInterval)
                Debug.WriteLine("BeatSync Timer: DÉMARRÉ")
            End If
        Else
            ' Aucun deck en sync, arrêter le timer
            If syncTimer IsNot Nothing Then
                syncTimer.Dispose()
                syncTimer = Nothing
                Debug.WriteLine("BeatSync Timer: ARRÊTÉ")
            End If
        End If
    End Sub

    ''' <summary>
    ''' Callback du timer : vérifie et corrige le drift
    ''' </summary>
    Private Sub VerifierSync(state As Object)
        Try
            ' Vérifier Deck A
            If _syncActifDeckA AndAlso fichierAudioDeckA IsNot Nothing AndAlso
               beatGridDeckA IsNot Nothing AndAlso beatGridDeckB IsNot Nothing Then
                VerifierEtCorrigerDeckA()
            End If

            ' Vérifier Deck B
            If _syncActifDeckB AndAlso fichierAudioDeckB IsNot Nothing AndAlso
               beatGridDeckA IsNot Nothing AndAlso beatGridDeckB IsNot Nothing Then
                VerifierEtCorrigerDeckB()
            End If

        Catch ex As Exception
            Debug.WriteLine($"BeatSync erreur: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Vérifier et corriger le drift du Deck A par rapport au Deck B
    ''' Utilise un système de Beat Quantize avancé avec lissage temporel (comme Serato)
    ''' </summary>
    Private Sub VerifierEtCorrigerDeckA()
        Try
            ' Protection : Vérifier que les fichiers audio sont toujours valides
            If fichierAudioDeckA Is Nothing OrElse fichierAudioDeckB Is Nothing Then
                Return
            End If

            ' Position actuelle Deck A
            Dim positionA As Double = fichierAudioDeckA.CurrentTime.TotalSeconds

            ' Phase actuelle Deck A
            Dim phaseA As Double = beatGridDeckA.CalculerPhase(positionA)

            ' Position actuelle Deck B (référence)
            Dim positionB As Double = fichierAudioDeckB.CurrentTime.TotalSeconds

            ' Phase actuelle Deck B
            Dim phaseB As Double = beatGridDeckB.CalculerPhase(positionB)

            ' Calculer le décalage de phase (normaliser entre -0.5 et +0.5)
            Dim phaseDiff As Double = phaseB - phaseA
            If phaseDiff < -0.5 Then phaseDiff += 1.0
            If phaseDiff > 0.5 Then phaseDiff -= 1.0

            ' Convertir en temps (secondes)
            Dim driftSecondes As Double = phaseDiff * beatGridDeckA.BeatDuration

            ' Stocker le drift brut
            DriftDeckA = driftSecondes

            ' === ÉTAPE 1 : FILTRAGE DU BRUIT (ignorer micro-variations < 5ms) ===
            If Math.Abs(driftSecondes) < driftMinimal Then
                driftSecondes = 0.0
            End If

            ' === ÉTAPE 2 : HISTORIQUE ET LISSAGE TEMPOREL ===
            ' Ajouter à l'historique
            driftHistoriqueDeckA.Enqueue(driftSecondes)
            If driftHistoriqueDeckA.Count > HISTORIQUE_TAILLE Then
                driftHistoriqueDeckA.Dequeue()
            End If

            ' Calculer le drift moyen lissé (médiane pour ignorer les outliers)
            Dim driftLisse As Double = CalculerMediane(driftHistoriqueDeckA)
            DriftMoyenDeckA = driftLisse

            ' === ÉTAPE 3 : ZONE MORTE (±10ms) ===
            If Math.Abs(driftLisse) < driftDeadZone Then
                ' Drift dans la zone morte, aucune correction nécessaire
                ' Revenir progressivement au tempo de base si on était en correction
                If Math.Abs(dernierTempoAjustementDeckA) > 0.001F Then
                    Dim tempoRetour As Single = dernierTempoAjustementDeckA * (1.0F - TEMPO_SMOOTH_FACTOR)
                    RaiseEvent TempoDeckAAjuste(tempoRetour)
                    dernierTempoAjustementDeckA = tempoRetour

                    If Math.Abs(tempoRetour) < 0.001F Then
                        dernierTempoAjustementDeckA = 0.0F
                        cyclesCorrectionDeckA = 0
                        Debug.WriteLine("BeatSync A→B: Drift rattrapé, tempo normal restauré ✅")
                    End If
                End If
                Return
            End If

            ' === ÉTAPE 4 : CORRECTION PROGRESSIVE ===
            If Math.Abs(driftLisse) > driftTolerance Then
                ' Incrémenter le compteur de cycles
                cyclesCorrectionDeckA += 1

                ' Calculer l'ajustement de tempo nécessaire
                ' Pour rattraper X ms en Y secondes, on ajuste le tempo de (X/Y) %
                Dim dureeCorrection As Double = 1.5 ' 1.5 secondes pour rattraper (était 3s, maintenant plus rapide comme Virtual DJ)
                Dim tempoAjustementCible As Single = CSng(driftLisse / dureeCorrection)

                ' Limiter l'ajustement à ±2% (était ±1.5%, maintenant plus agressif pour corrections rapides)
                tempoAjustementCible = Math.Max(-0.02F, Math.Min(0.02F, tempoAjustementCible))

                ' === ÉTAPE 5 : CORRECTION PROGRESSIVE (rampe douce sur 3 cycles = 0.3 seconde) ===
                Dim facteurProgression As Single = Math.Min(1.0F, cyclesCorrectionDeckA / CSng(CYCLES_AVANT_CORRECTION_COMPLETE))
                tempoAjustementCible *= facteurProgression

                ' === ÉTAPE 6 : LISSAGE TEMPOREL (transition douce entre les corrections) ===
                Dim tempoAjustement As Single = dernierTempoAjustementDeckA * (1.0F - TEMPO_SMOOTH_FACTOR) +
                                                 tempoAjustementCible * TEMPO_SMOOTH_FACTOR

                ' Mémoriser pour le prochain cycle
                dernierTempoAjustementDeckA = tempoAjustement

                ' Déclencher l'événement d'ajustement de tempo
                RaiseEvent TempoDeckAAjuste(tempoAjustement)
                CorrectionsAppliqueesDeckA += 1

                Debug.WriteLine($"BeatSync A→B: Drift brut={DriftDeckA * 1000:F1}ms, Lissé={driftLisse * 1000:F1}ms, " &
                               $"Pitch bend={tempoAjustement * 100:F3}% (progression {facteurProgression * 100:F0}%), " &
                               $"Cycle {cyclesCorrectionDeckA}, Total corrections: {CorrectionsAppliqueesDeckA}")
            Else
                ' Drift dans la zone de tolérance (10-20ms), maintenir la correction actuelle
                ' Pas de nouvelle correction, juste continuer avec le tempo actuel
            End If

        Catch ex As Exception
            Debug.WriteLine($"BeatSync Deck A erreur: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Vérifier et corriger le drift du Deck B par rapport au Deck A
    ''' Utilise un système de Beat Quantize avancé avec lissage temporel (comme Serato)
    ''' </summary>
    Private Sub VerifierEtCorrigerDeckB()
        Try
            ' Protection : Vérifier que les fichiers audio sont toujours valides
            If fichierAudioDeckA Is Nothing OrElse fichierAudioDeckB Is Nothing Then
                Return
            End If

            ' Position actuelle Deck B
            Dim positionB As Double = fichierAudioDeckB.CurrentTime.TotalSeconds

            ' Phase actuelle Deck B
            Dim phaseB As Double = beatGridDeckB.CalculerPhase(positionB)

            ' Position actuelle Deck A (référence)
            Dim positionA As Double = fichierAudioDeckA.CurrentTime.TotalSeconds

            ' Phase actuelle Deck A
            Dim phaseA As Double = beatGridDeckA.CalculerPhase(positionA)

            ' Calculer le décalage de phase (normaliser entre -0.5 et +0.5)
            Dim phaseDiff As Double = phaseA - phaseB
            If phaseDiff < -0.5 Then phaseDiff += 1.0
            If phaseDiff > 0.5 Then phaseDiff -= 1.0

            ' Convertir en temps (secondes)
            Dim driftSecondes As Double = phaseDiff * beatGridDeckB.BeatDuration

            ' Stocker le drift brut
            DriftDeckB = driftSecondes

            ' === ÉTAPE 1 : FILTRAGE DU BRUIT (ignorer micro-variations < 5ms) ===
            If Math.Abs(driftSecondes) < driftMinimal Then
                driftSecondes = 0.0
            End If

            ' === ÉTAPE 2 : HISTORIQUE ET LISSAGE TEMPOREL ===
            ' Ajouter à l'historique
            driftHistoriqueDeckB.Enqueue(driftSecondes)
            If driftHistoriqueDeckB.Count > HISTORIQUE_TAILLE Then
                driftHistoriqueDeckB.Dequeue()
            End If

            ' Calculer le drift moyen lissé (médiane pour ignorer les outliers)
            Dim driftLisse As Double = CalculerMediane(driftHistoriqueDeckB)
            DriftMoyenDeckB = driftLisse

            ' === ÉTAPE 3 : ZONE MORTE (±10ms) ===
            If Math.Abs(driftLisse) < driftDeadZone Then
                ' Drift dans la zone morte, aucune correction nécessaire
                ' Revenir progressivement au tempo de base si on était en correction
                If Math.Abs(dernierTempoAjustementDeckB) > 0.001F Then
                    Dim tempoRetour As Single = dernierTempoAjustementDeckB * (1.0F - TEMPO_SMOOTH_FACTOR)
                    RaiseEvent TempoDeckBAjuste(tempoRetour)
                    dernierTempoAjustementDeckB = tempoRetour

                    If Math.Abs(tempoRetour) < 0.001F Then
                        dernierTempoAjustementDeckB = 0.0F
                        cyclesCorrectionDeckB = 0
                        Debug.WriteLine("BeatSync B→A: Drift rattrapé, tempo normal restauré ✅")
                    End If
                End If
                Return
            End If

            ' === ÉTAPE 4 : CORRECTION PROGRESSIVE ===
            If Math.Abs(driftLisse) > driftTolerance Then
                ' Incrémenter le compteur de cycles
                cyclesCorrectionDeckB += 1

                ' Calculer l'ajustement de tempo nécessaire
                ' Pour rattraper X ms en Y secondes, on ajuste le tempo de (X/Y) %
                Dim dureeCorrection As Double = 1.5 ' 1.5 secondes pour rattraper (était 3s, maintenant plus rapide comme Virtual DJ)
                Dim tempoAjustementCible As Single = CSng(driftLisse / dureeCorrection)

                ' Limiter l'ajustement à ±2% (était ±1.5%, maintenant plus agressif pour corrections rapides)
                tempoAjustementCible = Math.Max(-0.02F, Math.Min(0.02F, tempoAjustementCible))

                ' === ÉTAPE 5 : CORRECTION PROGRESSIVE (rampe douce sur 3 cycles = 0.3 seconde) ===
                Dim facteurProgression As Single = Math.Min(1.0F, cyclesCorrectionDeckB / CSng(CYCLES_AVANT_CORRECTION_COMPLETE))
                tempoAjustementCible *= facteurProgression

                ' === ÉTAPE 6 : LISSAGE TEMPOREL (transition douce entre les corrections) ===
                Dim tempoAjustement As Single = dernierTempoAjustementDeckB * (1.0F - TEMPO_SMOOTH_FACTOR) +
                                                 tempoAjustementCible * TEMPO_SMOOTH_FACTOR

                ' Mémoriser pour le prochain cycle
                dernierTempoAjustementDeckB = tempoAjustement

                ' Déclencher l'événement d'ajustement de tempo
                RaiseEvent TempoDeckBAjuste(tempoAjustement)
                CorrectionsAppliqueesDeckB += 1

                Debug.WriteLine($"BeatSync B→A: Drift brut={DriftDeckB * 1000:F1}ms, Lissé={driftLisse * 1000:F1}ms, " &
                               $"Pitch bend={tempoAjustement * 100:F3}% (progression {facteurProgression * 100:F0}%), " &
                               $"Cycle {cyclesCorrectionDeckB}, Total corrections: {CorrectionsAppliqueesDeckB}")
            Else
                ' Drift dans la zone de tolérance (10-20ms), maintenir la correction actuelle
                ' Pas de nouvelle correction, juste continuer avec le tempo actuel
            End If

        Catch ex As Exception
            Debug.WriteLine($"BeatSync Deck B erreur: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Arrêter le moteur de synchronisation
    ''' </summary>
    Public Sub Arreter()
        _syncActifDeckA = False
        _syncActifDeckB = False
        VerifierEtatTimer()
    End Sub

    ''' <summary>
    ''' Calculer la médiane d'une liste de valeurs (pour lisser les outliers)
    ''' </summary>
    Private Function CalculerMediane(valeurs As Queue(Of Double)) As Double
        If valeurs.Count = 0 Then Return 0.0

        ' Copier les valeurs et trier
        Dim liste As New List(Of Double)(valeurs)
        liste.Sort()

        ' Calculer la médiane
        Dim milieu As Integer = liste.Count \ 2
        If liste.Count Mod 2 = 0 Then
            ' Nombre pair : moyenne des deux valeurs du milieu
            Return (liste(milieu - 1) + liste(milieu)) / 2.0
        Else
            ' Nombre impair : valeur du milieu
            Return liste(milieu)
        End If
    End Function

    ''' <summary>
    ''' Nettoyer les ressources
    ''' </summary>
    Public Sub Dispose()
        Arreter()
        beatGridDeckA = Nothing
        beatGridDeckB = Nothing
        fichierAudioDeckA = Nothing
        fichierAudioDeckB = Nothing
        driftHistoriqueDeckA.Clear()
        driftHistoriqueDeckB.Clear()
    End Sub

    ''' <summary>
    ''' Obtenir les statistiques de synchronisation
    ''' </summary>
    Public Function ObtenirStatistiques() As String
        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine("=== BeatSync Statistiques ===")
        sb.AppendLine($"Deck A Sync: {If(_syncActifDeckA, "ACTIF", "INACTIF")}, Drift brut: {DriftDeckA * 1000:F1}ms, Drift lissé: {DriftMoyenDeckA * 1000:F1}ms, Corrections: {CorrectionsAppliqueesDeckA}")
        sb.AppendLine($"Deck B Sync: {If(_syncActifDeckB, "ACTIF", "INACTIF")}, Drift brut: {DriftDeckB * 1000:F1}ms, Drift lissé: {DriftMoyenDeckB * 1000:F1}ms, Corrections: {CorrectionsAppliqueesDeckB}")
        Return sb.ToString()
    End Function
End Class
