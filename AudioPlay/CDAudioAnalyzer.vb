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
    ''' Par défaut 0.1% = -60 dB environ (détecte les silences même faibles)
    ''' </summary>
    Public Shared Property SilenceThreshold As Double = 0.001

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

            ' Si la piste précédente a détecté un silence, utiliser le CENTRE du silence pour démarrer cette piste
            If previousAnalysis IsNot Nothing AndAlso previousAnalysis.TransitionAnalyzed AndAlso previousAnalysis.SilenceStartFrame > 0 Then
                ' Utiliser le CENTRE du silence détecté comme point de départ
                Dim silenceCenter As Integer = (previousAnalysis.SilenceStartFrame + previousAnalysis.SilenceEndFrame) \ 2

                ' Le centre du silence peut être AVANT le StartFrame TOC (c'est normal, on ajuste la frontière!)
                result.AdjustedStartFrame = silenceCenter
                result.TrimmedStartFrames = silenceCenter - track.StartFrame  ' Peut être NÉGATIF si on recule!
                result.WasAdjusted = True

                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Début ajusté au CENTRE du silence précédent (frame {silenceCenter})")
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ TOC original: {track.StartFrame}")
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Silence de la piste {previousAnalysis.TrackNumber}: {previousAnalysis.SilenceStartFrame}-{previousAnalysis.SilenceEndFrame}")
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    └─ Ajustement: {If(result.TrimmedStartFrames >= 0, "+", "")}{result.TrimmedStartFrames / 75.0:F2}s")
            ElseIf track.TrackNumber = 1 Then
                ' PISTE 1 : Le TOC est déjà au bon endroit (après le pre-gap standard)
                ' Ne PAS analyser le début, garder le StartFrame original
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🎵 Piste 1 : Début au TOC original (frame {track.StartFrame})")
            Else
                ' Pour les autres pistes sans analyse précédente, analyser le début (pre-gap / silence initial)
                Dim startTrimFrames = AnalyzeTrackStart(track)
                If startTrimFrames > 0 Then
                    result.AdjustedStartFrame = track.StartFrame + startTrimFrames
                    result.TrimmedStartFrames = startTrimFrames
                    result.WasAdjusted = True
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Début ajusté : +{startTrimFrames / 75.0:F2}s ({startTrimFrames} frames)")
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Début OK : pas de silence détecté")
                End If
            End If

            ' Si une piste suivante existe, analyser la TRANSITION
            If nextTrack IsNot Nothing Then
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔍 Analyse de la TRANSITION entre piste {track.TrackNumber} et {nextTrack.TrackNumber}")
                Dim transitionResult = AnalyzeTransition(track, nextTrack)

                If transitionResult.SilenceFound Then
                    result.TransitionAnalyzed = True
                    result.SilenceStartFrame = transitionResult.SilenceStart
                    result.SilenceEndFrame = transitionResult.SilenceEnd

                    ' Ajuster la fin de la piste actuelle au CENTRE du silence
                    result.AdjustedEndFrame = transitionResult.SilenceCenter
                    result.TrimmedEndFrames = track.EndFrame - transitionResult.SilenceCenter
                    result.WasAdjusted = True
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Fin ajustée au CENTRE du silence : -{result.TrimmedEndFrames / 75.0:F2}s ({result.TrimmedEndFrames} frames)")
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🎯 Coupe au frame {transitionResult.SilenceCenter} (centre du silence {transitionResult.SilenceStart}-{transitionResult.SilenceEnd})")
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Pas de silence clair détecté dans la transition")
                End If
            Else
                ' Dernière piste du CD : analyser la fin pour détecter le silence final
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🎵 Dernière piste du CD : analyse de fin")
                Dim endTrimFrames = AnalyzeTrackEnd(track)
                If endTrimFrames > 0 Then
                    result.AdjustedEndFrame = track.EndFrame - endTrimFrames
                    result.TrimmedEndFrames = endTrimFrames
                    result.WasAdjusted = True
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Fin ajustée : -{endTrimFrames / 75.0:F2}s ({endTrimFrames} frames)")
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Fin OK : pas de silence détecté")
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
                        ' FILTRE 1: Durée minimale >= 2 secondes (150 frames)
                        Dim silencesLongs = silencesDetectes.Where(Function(s) (s.endPos - s.start) >= 150).ToList()

                        ' FILTRE 2: Proximité au TOC <= 10 secondes (750 frames)
                        Dim silencesProches = silencesLongs.Where(Function(s) Math.Abs(s.distanceFromTOC) <= 750).ToList()

                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔍 Filtrage: {silencesDetectes.Count} détectés → {silencesLongs.Count} longs (≥2s) → {silencesProches.Count} proches (≤10s du TOC)")

                        Dim meilleurSilence As (start As Integer, endPos As Integer, distanceFromTOC As Integer)

                        ' PRIORITÉ SUPPLÉMENTAIRE : préférer un silence qui CONTIENT une portion APRÈS le TOC
                        Dim silencesAvecPortionApresTOC = silencesProches.Where(Function(s) s.endPos >= tocBoundary).ToList()
                        If silencesAvecPortionApresTOC.Count > 0 Then
                            ' Choisir le plus proche du TOC parmi ceux contenant une portion après le TOC
                            meilleurSilence = silencesAvecPortionApresTOC.OrderBy(Function(s) Math.Abs(s.distanceFromTOC)).First()
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Silence sélectionné contenant une portion APRÈS le TOC (préférence)")
                        ElseIf silencesProches.Count > 0 Then
                            ' Pas de silence contenant une portion après le TOC, choisir le plus proche du TOC parmi les filtrés
                            meilleurSilence = silencesProches.OrderBy(Function(s) Math.Abs(s.distanceFromTOC)).First()
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Silence universel sélectionné (filtré: long ET proche du TOC)")
                        ElseIf silencesLongs.Count > 0 Then
                            ' Pas de silence proche, prendre le plus long disponible
                            meilleurSilence = silencesLongs.OrderByDescending(Function(s) (s.endPos - s.start)).First()
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Silence sélectionné (long mais éloigné du TOC)")
                        Else
                            ' Aucun silence >= 2s, prendre le plus proche du TOC tout court
                            meilleurSilence = silencesDetectes.OrderBy(Function(s) Math.Abs(s.distanceFromTOC)).First()
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Silence court sélectionné (aucun ≥2s trouvé)")
                        End If

                        ' Calculer le CENTRE du silence (point de coupe idéal)
                        Dim silenceCenter As Integer = (meilleurSilence.start + meilleurSilence.endPos) \ 2

                        result.SilenceFound = True
                        result.SilenceStart = meilleurSilence.start
                        result.SilenceEnd = meilleurSilence.endPos
                        result.SilenceCenter = silenceCenter
                        result.SilenceDuration = (meilleurSilence.endPos - meilleurSilence.start) / 75.0

                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Début  : frame {meilleurSilence.start} (TOC {(meilleurSilence.start - tocBoundary) / 75.0:F2}s)")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Centre : frame {silenceCenter} (TOC {(silenceCenter - tocBoundary) / 75.0:F2}s) ⭐")
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
                        Dim silenceEnd As Integer = track.EndFrame
                        silencesDetectes.Add((silenceStart, silenceEnd))
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    🔇 Silence #{silencesDetectes.Count} (fin): frames {silenceStart}-{silenceEnd} ({(silenceEnd - silenceStart) / 75.0:F2}s)")
                    End If

                    ' Choisir le DERNIER silence (celui qui est le plus proche de la fin)
                    If silencesDetectes.Count > 0 Then
                        Dim dernierSilence = silencesDetectes.OrderByDescending(Function(s) s.start).First()
                        Dim silenceDuration As Double = (dernierSilence.endPos - dernierSilence.start) / 75.0

                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ✅ Dernier silence trouvé (le plus proche de la fin) !")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Début  : frame {dernierSilence.start}")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    ├─ Fin    : frame {dernierSilence.endPos}")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    └─ Durée  : {silenceDuration:F2}s")

                        ' Calculer le centre du dernier silence
                        Dim silenceCenter As Integer = (dernierSilence.start + dernierSilence.endPos) \ 2
                        Dim framesToTrim As Integer = track.EndFrame - silenceCenter

                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🎯 Coupe au CENTRE du dernier silence : frame {silenceCenter}")
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer]    └─ Trim : {framesToTrim / 75.0:F2}s ({framesToTrim} frames)")

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
    End Function

End Class
