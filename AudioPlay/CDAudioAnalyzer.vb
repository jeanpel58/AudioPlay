Imports System.Runtime.InteropServices
Imports NAudio.Wave

'''
''' <summary>
''' Analyseur audio pour détecter les véritables limites des pistes CD
''' en analysant l'énergie audio autour des positions TOC
''' NOUVELLE APPROCHE : Analyse centrée sur la TRANSITION entre deux pistes
''' </summary>
Public Class CDAudioAnalyzer

    '''
    ''' <summary>
    ''' Seuil de détection de silence (en % de l'amplitude maximale)
    ''' Valeur plus basse = on exige un niveau plus faible pour être considéré comme silence.
    ''' Réduit les faux positifs sur les fade-outs faibles. Par défaut ~0.03% (~-70 dB)
    ''' </summary>
    ' Restore previous, less sensitive default to avoid over-trimming after lowering it caused regressions.
    Public Shared Property SilenceThreshold As Double = 0.001

    '''
    ''' <summary>
    ''' Activer l'analyse appairée des transitions (true par défaut).
    ''' Si false, on conserve le comportement historique (analyse piste par piste).
    ''' </summary>
    Public Shared Property UsePairwiseAnalysis As Boolean = True

    '''
    ''' <summary>
    ''' Durée de la zone d'analyse AVANT la transition TOC (en secondes)
    ''' Par défaut 20 secondes avant la frontière entre deux pistes
    ''' </summary>
    Public Shared Property TransitionWindowBeforeSeconds As Double = 20.0

    '''
    ''' <summary>
    ''' Durée de la zone d'analyse APRÈS la transition TOC (en secondes)
    ''' Par défaut 20 secondes après la frontière entre deux pistes
    ''' </summary>
    Public Shared Property TransitionWindowAfterSeconds As Double = 20.0

    '''
    ''' <summary>
    ''' Durée minimale de silence requise pour valider une transition (en secondes)
    ''' Valeur plus élevée = sélection plus stricte (réduit les faux positifs de fade-out)
    ''' </summary>
    Public Shared Property MinTransitionSilenceSeconds As Double = 2.5

    '''
    ''' <summary>
    ''' Fenêtre de proximité maximale autour de la frontière TOC (en secondes)
    ''' Valeur plus faible = silence accepté uniquement s'il est proche de la frontière
    ''' </summary>
    Public Shared Property TransitionProximityWindowSeconds As Double = 8.0

    '''
    ''' <summary>
    ''' Marge de sécurité pour ne pas couper trop près du silence détecté (en frames)
    ''' Environ 0.1 seconde = 7-8 frames
    ''' </summary>
    Public Shared Property SafetyMarginFrames As Integer = 8

    '''
    ''' <summary>
    ''' Structure pour stocker les résultats d'analyse d'une piste
    ''' </summary>
    Public Class TrackAnalysis
        Public Property TrackNumber As Integer
        Public Property OriginalStartFrame As Integer
        Public Property OriginalEndFrame As Integer
        Public Property AdjustedStartFrame As Integer
        Public Property AdjustedEndFrame As Integer
        Public Property TrimmedStartFrames As Integer
        Public Property TrimmedEndFrames As Integer
        Public Property WasAdjusted As Boolean
        Public Property AnalysisMessage As String

        ' Nouvelles propriétés pour l'analyse de transition
        Public Property SilenceStartFrame As Integer = -1  ' Position où commence le silence entre deux pistes
        Public Property SilenceEndFrame As Integer = -1    ' Position où se termine le silence
        Public Property TransitionAnalyzed As Boolean = False  ' Indique si une analyse de transition a été faite

        Public Overrides Function ToString() As String
            If WasAdjusted Then
                Return $"Piste {TrackNumber}: Début +{TrimmedStartFrames / 75.0:F2}s, Fin -{TrimmedEndFrames / 75.0:F2}s"
            Else
                Return $"Piste {TrackNumber}: OK (pas d'ajustement)"
            End If
        End Function
    End Class

    '''
    ''' <summary>
    ''' Analyse une piste CD pour détecter les véritables limites musicales
    ''' NOUVELLE APPROCHE : Analyse la transition entre la piste actuelle et la suivante
    ''' en cherchant le silence dans une fenêtre de 40 secondes (20s avant + 20s après la frontière TOC)
    ''' </summary>
    ''' <param name="track">La piste à analyser</param>
    ''' <param name="nextTrack">La piste suivante (pour analyser la zone de transition)</param>
    ''' <param name="previousAnalysis">L'analyse de la piste précédente (pour utiliser SilenceEndFrame si disponible)</param>
    Public Shared Function AnalyzeTrack(track As CDAudioManager.CDTrack, Optional nextTrack As CDAudioManager.CDTrack = Nothing, Optional previousAnalysis As TrackAnalysis = Nothing) As TrackAnalysis
        Dim result As New TrackAnalysis With {
            .TrackNumber = track.TrackNumber,
            .OriginalStartFrame = track.StartFrame,
            .OriginalEndFrame = track.EndFrame,
            .AdjustedStartFrame = track.StartFrame,
            .AdjustedEndFrame = track.EndFrame,
            .WasAdjusted = False,
            .TransitionAnalyzed = False
        }

        Try
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ═══════════════════════════════════════════")
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🎵 ANALYSE PISTE {track.TrackNumber}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ═══════════════════════════════════════════")

            ' Toujours analyser le début de la piste de manière indépendante
            Dim startTrimFrames = AnalyzeTrackStart(track)
            If startTrimFrames > 0 Then
                result.AdjustedStartFrame = track.StartFrame + startTrimFrames
                result.TrimmedStartFrames = startTrimFrames
                result.WasAdjusted = True
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Début ajusté : +{startTrimFrames / 75.0:F2}s ({startTrimFrames} frames)")
            Else
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Début OK : pas de silence détecté")
            End If

            ' Si une piste suivante existe, analyser la TRANSITION
            If nextTrack IsNot Nothing Then
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔍 Analyse de la TRANSITION entre piste {track.TrackNumber} et {nextTrack.TrackNumber}")
                Dim transitionResult = AnalyzeTransition(track, nextTrack)

                If transitionResult.SilenceFound Then
                    result.TransitionAnalyzed = True
                    result.SilenceStartFrame = transitionResult.SilenceStart
                    result.SilenceEndFrame = transitionResult.SilenceEnd

                    ' Ajuster la fin de la piste actuelle au début du silence + marge de sécurité (stratégie conservatrice)
                    Dim cutFrame As Integer = Math.Min(track.EndFrame, transitionResult.SilenceStart + SafetyMarginFrames)
                    result.AdjustedEndFrame = cutFrame
                    result.TrimmedEndFrames = track.EndFrame - cutFrame
                    result.WasAdjusted = True
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Fin ajustée (conservatrice): -{result.TrimmedEndFrames / 75.0:F2}s ({result.TrimmedEndFrames} frames)")
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🎯 Coupe au frame {cutFrame} (silence {transitionResult.SilenceStart}-{transitionResult.SilenceEnd}, marge {SafetyMarginFrames} frames)")
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Pas de silence clair détecté dans la transition")
                End If
            Else
                ' Dernière piste du CD : analyser la fin pour détecter un silence final valide
                ' et ne couper que si un silence clair et suffisamment long est trouvé.
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔍 Dernière piste du CD : analyse de fin activée pour détecter silence final")

                Dim endTrimFrames = AnalyzeTrackEnd(track)
                If endTrimFrames > 0 Then
                    ' Calculer la nouvelle frame de fin
                    Dim newEndFrame As Integer = Math.Max(track.StartFrame, track.EndFrame - endTrimFrames)
                    result.AdjustedEndFrame = newEndFrame
                    result.TrimmedEndFrames = track.EndFrame - newEndFrame
                    result.WasAdjusted = True
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Dernière piste : fin ajustée conservativement: -{result.TrimmedEndFrames / 75.0:F2}s ({result.TrimmedEndFrames} frames)")
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ℹ️ Dernière piste : aucun silence final valide détecté, fin TOC conservée")
                End If
            End If

            ' Générer le message d'analyse
            If result.WasAdjusted Then
                result.AnalysisMessage = $"Piste {track.TrackNumber}: Début +{result.TrimmedStartFrames / 75.0:F2}s, Fin -{result.TrimmedEndFrames / 75.0:F2}s"
                If result.TransitionAnalyzed Then
                    result.AnalysisMessage &= $" (silence frames {result.SilenceStartFrame}-{result.SilenceEndFrame})"
                End If
            Else
                result.AnalysisMessage = $"Piste {track.TrackNumber}: OK (pas d'ajustement)"
            End If

            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 📊 {result.AnalysisMessage}")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ❌ Erreur analyse piste {track.TrackNumber}: {ex.Message}")
            result.AnalysisMessage = $"Piste {track.TrackNumber}: Erreur, positions TOC utilisées"
        End Try

        Return result
    End Function

    '''
    ''' <summary>
    ''' Résultat de l'analyse de transition entre deux pistes
    ''' </summary>
    Private Class TransitionAnalysisResult
        Public Property SilenceFound As Boolean
        Public Property SilenceStart As Integer  ' Position absolue du début du silence (frame CD)
        Public Property SilenceEnd As Integer    ' Position absolue de la fin du silence (frame CD)
        Public Property SilenceCenter As Integer ' Position du CENTRE du silence (point de coupe idéal)
        Public Property SilenceDuration As Double  ' Durée du silence en secondes
    End Class

    '''
    ''' <summary>
    ''' NOUVELLE FONCTION : Analyse la transition entre deux pistes
    ''' Lit 20 secondes avant et 20 secondes après la frontière TOC pour détecter le silence
    ''' </summary>
    Private Shared Function AnalyzeTransition(currentTrack As CDAudioManager.CDTrack, nextTrack As CDAudioManager.CDTrack) As TransitionAnalysisResult
        Dim result As New TransitionAnalysisResult With {
            .SilenceFound = False,
            .SilenceStart = -1,
            .SilenceEnd = -1,
            .SilenceDuration = 0
        }

        Try
            ' Position de la frontière TOC (EndFrame de la piste actuelle = StartFrame de la piste suivante)
            Dim tocBoundary As Integer = currentTrack.EndFrame

            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔬 === ANALYSE DE TRANSITION ===")
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 📍 Frontière TOC : frame {tocBoundary}")

            ' Calculer la fenêtre d'analyse (20s avant + 20s après)
            Dim framesBeforeSeconds As Integer = CInt(TransitionWindowBeforeSeconds * 75)
            Dim framesAfterSeconds As Integer = CInt(TransitionWindowAfterSeconds * 75)

            Dim analyzeStartFrame As Integer = Math.Max(currentTrack.StartFrame, tocBoundary - framesBeforeSeconds)
            Dim analyzeEndFrame As Integer = Math.Min(nextTrack.EndFrame - 1, tocBoundary + framesAfterSeconds)
            Dim framesToAnalyze As Integer = analyzeEndFrame - analyzeStartFrame + 1

            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 📏 Fenêtre d'analyse : {framesToAnalyze} frames ({framesToAnalyze / 75.0:F2}s)")
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Début : frame {analyzeStartFrame} ({(tocBoundary - analyzeStartFrame) / 75.0:F2}s avant TOC)")
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    └─ Fin   : frame {analyzeEndFrame} ({(analyzeEndFrame - tocBoundary) / 75.0:F2}s après TOC)")

            If framesToAnalyze <= 0 Then
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Fenêtre invalide, abandon")
                Return result
            End If

            ' Lire l'audio de la fenêtre de transition
            Using reader As New CDAudioManager.CDReader(currentTrack.Drive, currentTrack.TrackNumber, currentTrack.Duration, analyzeStartFrame, analyzeEndFrame)
                Dim bytesToRead As Integer = framesToAnalyze * 2352
                Dim buffer(bytesToRead - 1) As Byte
                Dim bytesRead As Integer = reader.Read(buffer, 0, bytesToRead)

                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 📥 Lu {bytesRead} bytes ({bytesRead / 2352} frames)")

                If bytesRead > 0 Then
                    ' Analyser par tranches de 50ms pour détecter TOUS les silences
                    Dim samplesPerSlice As Integer = CInt(0.05 * 44100 * 2 * 2) ' 50ms
                    Dim minConsecutiveSlices As Integer = 4  ' Au moins 200ms de silence

                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔍 Recherche de silence (seuil: {SilenceThreshold:F4})")

                    ' Liste pour stocker TOUS les blocs de silence trouvés
                    Dim silencesDetectes As New List(Of (start As Integer, endPos As Integer, distanceFromTOC As Double))

                    Dim consecutiveSilentSlices As Integer = 0
                    Dim silenceStartSliceOffset As Integer = -1

                    ' Scanner toute la fenêtre pour trouver TOUS les silences
                    For offset As Integer = 0 To bytesRead - samplesPerSlice Step samplesPerSlice
                        Dim rms As Double = CalculateRMS(buffer, offset, samplesPerSlice)

                        If rms < SilenceThreshold Then
                            ' Silence détecté
                            If silenceStartSliceOffset = -1 Then
                                silenceStartSliceOffset = offset
                                consecutiveSilentSlices = 1
                            Else
                                consecutiveSilentSlices += 1
                            End If
                        Else
                            ' Signal audio détecté
                            If consecutiveSilentSlices >= minConsecutiveSlices Then
                                ' Bloc de silence significatif trouvé - le stocker
                                Dim silenceStart As Integer = analyzeStartFrame + (silenceStartSliceOffset \ 2352)
                                Dim silenceEnd As Integer = analyzeStartFrame + (offset \ 2352)
                                Dim silenceCenter As Integer = (silenceStart + silenceEnd) \ 2
                                Dim distanceFromTOC As Double = Math.Abs(silenceCenter - tocBoundary)

                                silencesDetectes.Add((silenceStart, silenceEnd, distanceFromTOC))
                                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    🔇 Silence #{silencesDetectes.Count}: frames {silenceStart}-{silenceEnd} (distance TOC: {distanceFromTOC / 75.0:F2}s)")
                            End If

                            ' Réinitialiser la recherche
                            silenceStartSliceOffset = -1
                            consecutiveSilentSlices = 0
                        End If
                    Next

                    ' Vérifier si on termine avec un silence
                    If consecutiveSilentSlices >= minConsecutiveSlices AndAlso silenceStartSliceOffset >= 0 Then
                        Dim silenceStart As Integer = analyzeStartFrame + (silenceStartSliceOffset \ 2352)
                        Dim silenceEnd As Integer = analyzeEndFrame
                        Dim silenceCenter As Integer = (silenceStart + silenceEnd) \ 2
                        Dim distanceFromTOC As Double = Math.Abs(silenceCenter - tocBoundary)

                        silencesDetectes.Add((silenceStart, silenceEnd, distanceFromTOC))
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    🔇 Silence #{silencesDetectes.Count} (fin): frames {silenceStart}-{silenceEnd} (distance TOC: {distanceFromTOC / 75.0:F2}s)")
                    End If

                    ' Choisir le silence LE PLUS PROCHE DU TOC avec filtres universels
                    If silencesDetectes.Count > 0 Then
                        ' FILTRE 1: Durée minimale configurable
                        Dim minSilenceFrames As Integer = CInt(MinTransitionSilenceSeconds * 75)
                        Dim silencesLongs = silencesDetectes.Where(Function(s) (s.endPos - s.start) >= minSilenceFrames).ToList()

                        ' FILTRE 2: Proximité au TOC configurable
                        Dim proximityFrames As Integer = CInt(TransitionProximityWindowSeconds * 75)
                        Dim silencesProches = silencesLongs.Where(Function(s) Math.Abs(s.distanceFromTOC) <= proximityFrames).ToList()

                        Dim rejectedByDuration As Integer = silencesDetectes.Count - silencesLongs.Count
                        Dim rejectedByProximity As Integer = silencesLongs.Count - silencesProches.Count

                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔍 Filtrage: {silencesDetectes.Count} détectés → {silencesLongs.Count} longs (≥{MinTransitionSilenceSeconds:F1}s) → {silencesProches.Count} proches (≤{TransitionProximityWindowSeconds:F1}s du TOC)")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    └─ Rejets: durée={rejectedByDuration}, proximité={rejectedByProximity}")

                        ' Refuser tout trim si aucun silence suffisamment long et proche
                        If silencesProches.Count = 0 Then
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Aucun silence valide (long et proche du TOC) -> aucun trim de transition")
                            Return result
                        End If

                        Dim meilleurSilence As (start As Integer, endPos As Integer, distanceFromTOC As Double)

                        ' PRIORITÉ SUPPLÉMENTAIRE : préférer un silence qui CONTIENT une portion APRÈS le TOC
                        Dim silencesAvecPortionApresTOC = silencesProches.Where(Function(s) s.endPos >= tocBoundary).ToList()
                        If silencesAvecPortionApresTOC.Count > 0 Then
                            ' Choisir le plus proche du TOC parmi ceux contenant une portion après le TOC
                            meilleurSilence = silencesAvecPortionApresTOC.OrderBy(Function(s) Math.Abs(s.distanceFromTOC)).First()
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Silence sélectionné contenant une portion APRÈS le TOC (préférence)")
                        Else
                            ' Choisir le plus proche du TOC parmi les silences valides
                            meilleurSilence = silencesProches.OrderBy(Function(s) Math.Abs(s.distanceFromTOC)).First()
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Silence universel sélectionné (filtré: long ET proche du TOC)")
                        End If

                        ' Coupe prudente : début du silence + marge de sécurité
                        Dim cutFrame As Integer = Math.Min(meilleurSilence.endPos, meilleurSilence.start + SafetyMarginFrames)

                        result.SilenceFound = True
                        result.SilenceStart = meilleurSilence.start
                        result.SilenceEnd = meilleurSilence.endPos
                        result.SilenceCenter = cutFrame
                        result.SilenceDuration = (meilleurSilence.endPos - meilleurSilence.start) / 75.0

                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Début  : frame {meilleurSilence.start} (TOC {(meilleurSilence.start - tocBoundary) / 75.0:F2}s)")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Coupe  : frame {cutFrame} (TOC {(cutFrame - tocBoundary) / 75.0:F2}s) ⭐")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Fin    : frame {meilleurSilence.endPos} (TOC {(meilleurSilence.endPos - tocBoundary) / 75.0:F2}s)")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    └─ Durée  : {result.SilenceDuration:F2}s")
                    Else
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Aucun silence significatif détecté dans la transition")
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ❌ Erreur analyse transition: {ex.Message}")
        End Try

        Return result
    End Function

    '''
    ''' <summary>
    ''' Analyse le début d'une piste pour détecter et couper tout silence/pre-gap initial
    ''' </summary>
    Private Shared Function AnalyzeTrackStart(track As CDAudioManager.CDTrack) As Integer
        Try
            ' Utiliser la fenêtre "avant" pour analyser le début
            Dim analysisFrames As Integer = CInt(TransitionWindowBeforeSeconds * 75)
            Dim framesToRead As Integer = Math.Min(analysisFrames, track.EndFrame - track.StartFrame)

            Using tempReader As New CDAudioManager.CDReader(track.Drive, track.TrackNumber, track.Duration, track.StartFrame, track.EndFrame)
                Dim bytesToRead As Integer = framesToRead * 2352
                Dim buffer(bytesToRead - 1) As Byte
                Dim bytesRead As Integer = tempReader.Read(buffer, 0, bytesToRead)

                If bytesRead > 0 Then
                    ' Utiliser des tranches de 50ms pour plus de précision
                    Dim samplesPerSlice As Integer = CInt(0.05 * 44100 * 2 * 2)
                    Dim silentFramesCount As Integer = 0
                    Dim consecutiveSilentSlices As Integer = 0
                    Dim minConsecutiveSlices As Integer = 4 ' Au moins 200ms de silence

                    For offset As Integer = 0 To bytesRead - samplesPerSlice Step samplesPerSlice
                        Dim rms As Double = CalculateRMS(buffer, offset, samplesPerSlice)

                        If rms < SilenceThreshold Then
                            consecutiveSilentSlices += 1
                            silentFramesCount += CInt(0.05 * 75)
                        Else
                            ' On a trouvé du signal audio
                            If consecutiveSilentSlices >= minConsecutiveSlices Then
                                ' Appliquer une marge de sécurité pour ne pas couper trop près
                                silentFramesCount = Math.Max(0, silentFramesCount - SafetyMarginFrames)
                                Exit For
                            Else
                                ' Pas assez de silence consécutif, réinitialiser
                                silentFramesCount = 0
                                consecutiveSilentSlices = 0
                            End If
                        End If
                    Next

                    ' Si on n'a trouvé que du silence, appliquer quand même une marge
                    If consecutiveSilentSlices > 0 AndAlso silentFramesCount > SafetyMarginFrames Then
                        silentFramesCount = Math.Max(0, silentFramesCount - SafetyMarginFrames)
                    End If

                    Return silentFramesCount
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Erreur analyse début: {ex.Message}")
        End Try

        Return 0
    End Function

    '''
    ''' <summary>
    ''' Analyse la fin d'une piste (dernière piste du CD) pour détecter et couper tout silence final
    ''' NOUVELLE VERSION : Scanne toute la piste pour trouver le DERNIER vrai silence avant la fin
    ''' </summary>
    Private Shared Function AnalyzeTrackEnd(track As CDAudioManager.CDTrack) As Integer
        Try
            ' Lire TOUTE la piste pour trouver tous les silences
            Dim framesToRead As Integer = track.EndFrame - track.StartFrame
            Dim readStartFrame As Integer = track.StartFrame

            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔍 Analyse de fin de piste : {framesToRead} frames ({framesToRead / 75.0:F2}s)")
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ De frame {readStartFrame} à {track.EndFrame}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    └─ Seuil silence : {SilenceThreshold:F4}")

            Using tempReader As New CDAudioManager.CDReader(track.Drive, track.TrackNumber, track.Duration, readStartFrame, track.EndFrame)
                Dim bytesToRead As Integer = framesToRead * 2352
                Dim buffer(bytesToRead - 1) As Byte
                Dim bytesRead As Integer = tempReader.Read(buffer, 0, bytesToRead)

                If bytesRead > 0 Then
                    ' Utiliser des tranches de 50ms pour plus de précision
                    Dim samplesPerSlice As Integer = CInt(0.05 * 44100 * 2 * 2)
                    Dim minConsecutiveSlices As Integer = 4 ' Au moins 200ms de silence

                    ' Liste pour stocker TOUS les silences trouvés
                    Dim silencesDetectes As New List(Of (start As Integer, endPos As Integer))

                    Dim consecutiveSilentSlices As Integer = 0
                    Dim silenceStartSliceOffset As Integer = -1

                    ' Scanner toute la piste pour trouver TOUS les silences
                    For offset As Integer = 0 To bytesRead - samplesPerSlice Step samplesPerSlice
                        Dim rms As Double = CalculateRMS(buffer, offset, samplesPerSlice)

                        If rms < SilenceThreshold Then
                            ' Silence détecté
                            If silenceStartSliceOffset = -1 Then
                                silenceStartSliceOffset = offset
                                consecutiveSilentSlices = 1
                            Else
                                consecutiveSilentSlices += 1
                            End If
                        Else
                            ' Signal audio détecté
                            If consecutiveSilentSlices >= minConsecutiveSlices Then
                                ' Bloc de silence significatif trouvé - le stocker
                                Dim silenceStart As Integer = readStartFrame + (silenceStartSliceOffset \ 2352)
                                Dim silenceEnd As Integer = readStartFrame + (offset \ 2352)
                                silencesDetectes.Add((silenceStart, silenceEnd))
                                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    🔇 Silence #{silencesDetectes.Count}: frames {silenceStart}-{silenceEnd} ({(silenceEnd - silenceStart) / 75.0:F2}s)")
                            End If

                            ' Réinitialiser la recherche
                            silenceStartSliceOffset = -1
                            consecutiveSilentSlices = 0
                        End If
                    Next

                    ' Vérifier si on termine avec un silence
                    If consecutiveSilentSlices >= minConsecutiveSlices AndAlso silenceStartSliceOffset >= 0 Then
                        Dim silenceStart As Integer = readStartFrame + (silenceStartSliceOffset \ 2352)
                        Dim silenceEnd As Integer = Math.Min(track.EndFrame, readStartFrame + (bytesRead \ 2352))
                        silencesDetectes.Add((silenceStart, silenceEnd))
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    🔇 Silence #{silencesDetectes.Count} (fin de buffer): frames {silenceStart}-{silenceEnd} ({(silenceEnd - silenceStart) / 75.0:F2}s)")
                    End If

                    ' Choisir le silence le plus adapté pour la fin de piste
                    If silencesDetectes.Count > 0 Then
                        Dim tailPreferenceFrames As Integer = CInt(10 * 75) ' Préférer un silence dans les 10 dernières secondes
                        Dim minimalMusicBeforeCutFrames As Integer = CInt(1 * 75) ' Garder au moins 1s avant le centre de coupe
                        Dim minSilenceFrames As Integer = CInt(1.5 * 75) ' Exiger au moins 1.5s de silence

                        Dim silencesTries = silencesDetectes.OrderByDescending(Function(s) s.start).ToList()
                        Dim candidats = silencesTries.Where(Function(s) s.endPos >= track.EndFrame - tailPreferenceFrames AndAlso (s.endPos - s.start) >= minSilenceFrames).ToList()

                        If candidats.Count > 0 Then
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Priorité queue: {candidats.Count} silence(s) éligible(s) dans les 10 dernières secondes")
                        Else
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Aucun silence de queue éligible (>= {minSilenceFrames / 75.0:F2}s); fin TOC conservée")
                            Return 0
                        End If

                        ' Choisir le silence éligible le plus proche de la fin TOC
                        Dim meilleurSilence = candidats.OrderBy(Function(s) Math.Abs(track.EndFrame - ((s.start + s.endPos) \ 2))).First()
                        Dim silenceStartFrame As Integer = meilleurSilence.start
                        Dim silenceEndFrame As Integer = meilleurSilence.endPos
                        Dim silenceDuration As Double = (silenceEndFrame - silenceStartFrame) / 75.0
                        Dim cutFrame As Integer = silenceStartFrame + SafetyMarginFrames

                        If (cutFrame - track.StartFrame) < minimalMusicBeforeCutFrames Then
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Point de coupe trop proche du début (moins de 1s), fin TOC conservée")
                            Return 0
                        End If

                        Dim framesToTrim As Integer = track.EndFrame - cutFrame

                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Silence de fin sélectionné")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Début  : frame {silenceStartFrame}")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Fin    : frame {silenceEndFrame}")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    └─ Durée  : {silenceDuration:F2}s")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🎯 Coupe PRUDENTE au début du silence + marge ({SafetyMarginFrames} frames) : frame {cutFrame}")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    └─ Trim : {framesToTrim / 75.0:F2}s ({framesToTrim} frames)")

                        ' Protection : éviter des trims absurdes (silence trouvé près du début de la piste
                        ' qui conduiraient à une fin <= début). Si le trim dépasse 90% de la longueur de la piste,
                        ' il s'agit probablement d'un silence en tête (transition précédente) et non de la queue.
                        Dim totalFrames As Integer = track.EndFrame - track.StartFrame
                        If totalFrames > 0 AndAlso framesToTrim >= CInt(totalFrames * 0.9) Then
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Trim rejeté: framesToTrim ({framesToTrim}) >= 90% de la piste ({totalFrames}) -> conserver fin TOC")
                            Return 0
                        End If

                        Return framesToTrim
                    Else
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Aucun silence détecté, fin de piste OK")
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Erreur analyse fin: {ex.Message}")
        End Try

        Return 0
    End Function

    '''
    ''' <summary>
    ''' Calcule le RMS (Root Mean Square) pour mesurer l'énergie audio
    ''' </summary>
    Private Shared Function CalculateRMS(buffer() As Byte, offset As Integer, length As Integer) As Double
        Dim sumOfSquares As Double = 0
        Dim sampleCount As Integer = 0

        For i As Integer = offset To Math.Min(offset + length - 2, buffer.Length - 2) Step 2
            Dim sample As Short = BitConverter.ToInt16(buffer, i)
            Dim normalizedSample As Double = sample / 32768.0
            sumOfSquares += normalizedSample * normalizedSample
            sampleCount += 1
        Next

        If sampleCount > 0 Then
            Return Math.Sqrt(sumOfSquares / sampleCount)
        Else
            Return 0
        End If
    End Function

    '''
    ''' <summary>
    ''' Analyse toutes les pistes sélectionnées et retourne les résultats
    ''' </summary>
    Public Shared Function AnalyzeSelectedTracks(tracks As List(Of CDAudioManager.CDTrack), selectedIndices As List(Of Integer)) As List(Of TrackAnalysis)
        Dim results As New List(Of TrackAnalysis)

        If Not UsePairwiseAnalysis Then
            ' Comportement historique : analyser piste par piste
            For Each index In selectedIndices
                If index >= 0 AndAlso index < tracks.Count Then
                    Dim track = tracks(index)
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Analyse piste {track.TrackNumber}...")
                    Dim analysis = AnalyzeTrack(track)
                    results.Add(analysis)
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] {analysis.AnalysisMessage}")
                End If
            Next

            Return results
        End If

        ' Nouvelle approche appairée : analyser chaque piste en tenant compte de la piste suivante
        For Each index In selectedIndices
            If index >= 0 AndAlso index < tracks.Count Then
                Dim track = tracks(index)

                Dim nextTrack As CDAudioManager.CDTrack = Nothing
                If index + 1 < tracks.Count Then
                    nextTrack = tracks(index + 1)
                End If

                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Analyse piste {track.TrackNumber} (pairwise)...")
                Dim analysis = AnalyzeTrack(track, nextTrack, Nothing)
                results.Add(analysis)
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] {analysis.AnalysisMessage}")
            End If
        Next

        ' Réconciliation paire par paire : éviter tout chevauchement entre la fin d'une piste et le début de la suivante
        For i As Integer = 0 To results.Count - 2
            Dim cur = results(i)
            Dim nxt = results(i + 1)

            ' Calculer valeurs par défaut si non initialisées
            If nxt.AdjustedStartFrame <= 0 Then
                nxt.AdjustedStartFrame = nxt.OriginalStartFrame
            End If

            If cur.AdjustedEndFrame >= nxt.AdjustedStartFrame Then
                Dim correctedStart As Integer = cur.AdjustedEndFrame + 1

                ' Vérifier l'inversion possible (start >= end)
                If correctedStart >= nxt.AdjustedEndFrame Then
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Réconciliation impossible sans inversion entre piste {cur.TrackNumber} et {nxt.TrackNumber} - conservation des positions TOC pour la suivante")
                    ' Reprendre la position TOC pour la piste suivante pour éviter d'écraser la durée
                    nxt.AdjustedStartFrame = nxt.OriginalStartFrame
                    nxt.TrimmedStartFrames = 0
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔧 Réconciliation: déplacement du début de la piste {nxt.TrackNumber} à {correctedStart} pour éviter chevauchement avec piste {cur.TrackNumber}")
                    nxt.AdjustedStartFrame = correctedStart
                    nxt.TrimmedStartFrames = nxt.AdjustedStartFrame - nxt.OriginalStartFrame
                    nxt.WasAdjusted = True
                End If

                ' Mettre à jour le message d'analyse pour la piste suivante
                If nxt.WasAdjusted Then
                    nxt.AnalysisMessage = $"Piste {nxt.TrackNumber}: Début +{nxt.TrimmedStartFrames / 75.0:F2}s, Fin -{nxt.TrimmedEndFrames / 75.0:F2}s"
                Else
                    nxt.AnalysisMessage = $"Piste {nxt.TrackNumber}: OK (pas d'ajustement)"
                End If
            End If
        Next

        Return results
    End Function

End Class
