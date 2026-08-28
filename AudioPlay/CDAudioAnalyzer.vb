Imports System.Runtime.InteropServices
Imports NAudio.Wave
Imports System.Text
Imports System.Threading
Imports System.IO

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

    ''' <summary>
    ''' Durée minimale de silence soutenu requise (en secondes) pour valider une coupe
    ''' Permet d'éviter de couper sur de très brefs creux ou sur des transitoires
    ''' </summary>
    Public Shared Property MinSustainedSilenceSeconds As Double = 0.5

    ''' <summary>
    ''' Limite maximale de trim appliqué en dernier recours (en secondes)
    ''' Empêche de supprimer trop de contenu lorsqu'aucun silence clair n'est trouvé
    ''' </summary>
    Public Shared Property MaxTrimSeconds As Double = 4.0

    ''' <summary>
    ''' Limite maximale de trim autorisé au début d'une piste (en secondes)
    ''' Evite que des débuts soient avancés de plusieurs secondes par erreur
    ''' </summary>
    Public Shared Property MaxStartTrimSeconds As Double = 8.0

    ''' <summary>
    ''' Diagnostics détaillés activés en permanence pour la session.
    ''' Cette valeur est en lecture seule afin d'empêcher toute désactivation
    ''' accidentelle depuis d'autres parties du code.
    ''' </summary>
    Public Shared ReadOnly UseDetailedDiagnostics As Boolean = True

    ''' <summary>
    ''' Chemin du fichier de log pour les diagnostics. Par défaut %TEMP%\AudioPlay_AnalysisLog.txt
    ''' </summary>
    Public Shared ReadOnly Property DiagnosticsLogPath As String
        Get
            Try
                Return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_AnalysisLog.txt")
            Catch
                Return "AudioPlay_AnalysisLog.txt"
            End Try
        End Get
    End Property

    ' Alternate path used when writing to %TEMP% fails (fallback to app folder)
    Private Shared alternateDiagnosticsLogPath As String = Nothing

    ' DEBUG: Force the relaxed secondary pass for all analyses (temporary)
    Private Shared ReadOnly ForceSecondaryPass As Boolean = True
    ' DEBUG: Force saving snippets for all tracks
    Private Shared ReadOnly ForceSaveSnippetsForAllTracks As Boolean = True
    ' Configuration flags (readable from parametres.txt in next iteration)
    Public Shared Property EnableAggressiveSecondaryPass As Boolean = True
    Public Shared Property EnableSnippetCapture As Boolean = True
    ''' <summary>
    ''' Force using silence CENTER as cut points (start/end) when a reliable silence is detected.
    ''' This mode is conservative: it still enforces caps and avoids inversions.
    ''' </summary>
    Public Shared Property ForceCenterCuts As Boolean = True

    ''' <summary>
    ''' Initialise le fichier de log de diagnostic pour une nouvelle session.
    ''' Écris un en-tête SESSION START; en cas d'échec sur %TEMP%, bascule vers un fichier dans le répertoire de l'application.
    ''' </summary>
    ' Session state to avoid overwriting the diagnostics log except when explicitly requested
    Private Shared diagnosticsSessionActive As Boolean = False

    ''' <summary>
    ''' Initialise le fichier de log de diagnostic pour une nouvelle session.
    ''' Par défaut, n'écrase pas le fichier existant si une session est déjà active.
    ''' Si forceReset = True, le fichier est réinitialisé (utilisé par ButtonExtraire).
    ''' </summary>
    Public Shared Sub InitializeDiagnosticsLog(Optional sessionMessage As String = "", Optional forceReset As Boolean = False)
        Dim header As String = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] SESSION START{If(String.IsNullOrEmpty(sessionMessage), "", " - " & sessionMessage)}{Environment.NewLine}"
        If Not forceReset AndAlso diagnosticsSessionActive Then
            ' Do not reset the log; append a continuation marker instead and return
            Try
                Dim cont As String = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] SESSION CONTINUED{If(String.IsNullOrEmpty(sessionMessage), "", " - " & sessionMessage)}{Environment.NewLine}"
                SyncLock GetType(CDAudioAnalyzer)
                    Try
                        System.IO.File.AppendAllText(DiagnosticsLogPath, cont, Encoding.UTF8)
                    Catch
                        If Not String.IsNullOrEmpty(alternateDiagnosticsLogPath) Then
                            Try
                                System.IO.File.AppendAllText(alternateDiagnosticsLogPath, cont, Encoding.UTF8)
                            Catch
                            End Try
                        End If
                    End Try
                End SyncLock
            Catch
            End Try
            Return
        End If
        Try
            Dim ver As String = "unknown"
            Try
                Dim asm = System.Reflection.Assembly.GetEntryAssembly()
                If asm Is Nothing Then asm = System.Reflection.Assembly.GetExecutingAssembly()
                If asm IsNot Nothing Then
                    Try
                        Dim asmName = asm.GetName()
                        If asmName IsNot Nothing AndAlso asmName.Version IsNot Nothing Then
                            ver = asmName.Version.ToString()
                        Else
                            ver = "unknown"
                        End If
                    Catch
                        ver = "unknown"
                    End Try
                End If


            Catch
            End Try
            header &= $"BUILD INFO: Version={ver} | InitTime={DateTime.Now:yyyy-MM-dd HH:mm:ss}" & Environment.NewLine
        Catch
        End Try
        Try
            SyncLock GetType(CDAudioAnalyzer)
                File.WriteAllText(DiagnosticsLogPath, header, Encoding.UTF8)
            End SyncLock
            ' clear any previous alternate path
            alternateDiagnosticsLogPath = Nothing
            diagnosticsSessionActive = True
            Return
        Catch ex As Exception
            Try
                Dim alt As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioPlay_AnalysisLog.txt")
                SyncLock GetType(CDAudioAnalyzer)
                    File.WriteAllText(alt, header, Encoding.UTF8)
                End SyncLock
                alternateDiagnosticsLogPath = alt
                diagnosticsSessionActive = True
            Catch ex2 As Exception
                Try
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] InitializeDiagnosticsLog failed: {ex.Message} | alt: {ex2.Message}")
                Catch
                End Try
            End Try
        End Try
    End Sub

    ''' <summary>
    ''' Retourne une étiquette lisible pour la piste (Artiste - Titre) ou un fallback Track n
    ''' </summary>
    Private Shared Function GetTrackLabel(track As CDAudioManager.CDTrack) As String
        Try
            If track Is Nothing Then Return "Track unknown"
            Dim artist = If(String.IsNullOrWhiteSpace(track.Artist), String.Empty, track.Artist.Trim())
            Dim title = If(String.IsNullOrWhiteSpace(track.Title), String.Empty, track.Title.Trim())

            If Not String.IsNullOrEmpty(artist) AndAlso Not String.IsNullOrEmpty(title) Then
                Return $"{artist} - {title} (Track {track.TrackNumber})"
            ElseIf Not String.IsNullOrEmpty(title) Then
                Return $"{title} (Track {track.TrackNumber})"
            Else
                Return $"Track {track.TrackNumber}"
            End If
        Catch
            Return $"Track {If(track IsNot Nothing, track.TrackNumber, -1)}"
        End Try
    End Function

    ' Helper to perform robust reads with retries when reading large chunks from CDReader
    Private Shared Function ReadWithRetries(reader As CDAudioManager.CDReader, buffer() As Byte, offset As Integer, count As Integer, Optional maxAttempts As Integer = 3, Optional delayMs As Integer = 200) As Integer
        If reader Is Nothing Then Return 0

        Dim totalRead As Integer = 0
        ' Read in small blocks aligned to CD sectors to improve resilience
        Dim preferredBlockSize As Integer = 16 * 1024 ' 16 KB
        Dim sectorSize As Integer = 2352

        While totalRead < count
            Dim remaining As Integer = count - totalRead
            ' Align block to sector multiples
            Dim framesForBlock As Integer = Math.Max(1, Math.Min(remaining \ sectorSize, Math.Max(1, preferredBlockSize \ sectorSize)))
            Dim blockBytes As Integer = framesForBlock * sectorSize
            If blockBytes <= 0 Then blockBytes = Math.Min(remaining, preferredBlockSize)

            Dim attempt As Integer = 0
            Dim blockRead As Integer = 0
            Dim backoff As Integer = delayMs

            While attempt < maxAttempts AndAlso blockRead < blockBytes
                Try
                    Dim readNow As Integer = reader.Read(buffer, offset + totalRead + blockRead, blockBytes - blockRead)
                    If readNow > 0 Then
                        blockRead += readNow
                        DiagnosticWrite($"ReadWithRetries: reader read {readNow} bytes (block progress {blockRead}/{blockBytes})")
                        If blockRead >= blockBytes Then Exit While
                    Else
                        attempt += 1
                        DiagnosticWrite($"ReadWithRetries: no progress on attempt {attempt}/{maxAttempts} for block ({blockRead}/{blockBytes})")
                        If attempt < maxAttempts Then Thread.Sleep(backoff)
                        backoff = Math.Min(2000, backoff * 2)
                    End If
                Catch ex As Exception
                    attempt += 1
                    DiagnosticWrite($"ReadWithRetries: exception on attempt {attempt}/{maxAttempts}: {ex.Message}")
                    If attempt < maxAttempts Then Thread.Sleep(backoff)
                    backoff = Math.Min(2000, backoff * 2)
                End Try
            End While

            If blockRead <= 0 Then
                ' Failed to read this block after retries -> give up
                DiagnosticWrite($"ReadWithRetries: failed to read block of {blockBytes} bytes after {maxAttempts} attempts. TotalRead={totalRead}")
                Exit While
            End If

            totalRead += blockRead
        End While

        Return totalRead
    End Function

    ''' <summary>
    ''' Sauvegarde un court extrait WAV autour de la frontière TOC pour inspection manuelle.
    ''' Utilisé uniquement à des fins de diagnostic lorsque les passes d'analyse échouent.
    ''' </summary>
    Private Shared Sub SaveTransitionSnippet(currentTrack As CDAudioManager.CDTrack, nextTrack As CDAudioManager.CDTrack, secondsBefore As Integer, secondsAfter As Integer)
        Try
            Dim toc As Integer = currentTrack.EndFrame
            Dim framesBefore As Integer = secondsBefore * 75
            Dim framesAfter As Integer = secondsAfter * 75

            Dim startFrame As Integer = Math.Max(currentTrack.StartFrame, toc - framesBefore)
            Dim endFrame As Integer
            If nextTrack IsNot Nothing Then
                endFrame = Math.Min(nextTrack.EndFrame - 1, toc + framesAfter)
            Else
                endFrame = toc + framesAfter
            End If

            If endFrame <= startFrame Then Return

            DiagnosticWrite($"SaveTransitionSnippet: entering for track {currentTrack.TrackNumber} startFrame={startFrame} endFrame={endFrame}")
            Using reader As New CDAudioManager.CDReader(currentTrack.Drive, currentTrack.TrackNumber, currentTrack.Duration, startFrame, endFrame)
                Dim bytesToRead As Integer = (endFrame - startFrame + 1) * 2352
                Dim buffer(bytesToRead - 1) As Byte
                ' Use retry-capable read path for robustness on occasional CD read hiccups (context anchor).
                Dim bytesRead As Integer = ReadWithRetries(reader, buffer, 0, bytesToRead, 5, 200)
                If bytesRead <= 0 Then
                    DiagnosticWrite($"SaveTransitionSnippet: no bytes read for track {currentTrack.TrackNumber}")
                    Return
                End If

                Dim fileName As String = Path.Combine(Path.GetTempPath(), $"AudioPlay_Snippet_Track{currentTrack.TrackNumber}_{DateTime.Now:yyyyMMddHHmmss}.wav")
                Try
                    Dim wf = New WaveFormat(44100, 16, 2)
                    Using w As New WaveFileWriter(fileName, wf)
                        w.Write(buffer, 0, bytesRead)
                    End Using
                    DiagnosticWrite($"Saved transition snippet for track {currentTrack.TrackNumber} -> {fileName}")
                Catch exW As Exception
                    DiagnosticWrite($"Failed writing snippet WAV for track {currentTrack.TrackNumber}: {exW.Message}")
                End Try
            End Using
        Catch ex As Exception
            DiagnosticWrite($"SaveTransitionSnippet error: {ex.Message}")
        End Try
    End Sub


    ''' <summary>
    ''' Écrit une ligne de diagnostic dans le fichier si UseDetailedDiagnostics = True
    ''' Usage sûr : nève pas d'exception en cas d'erreur d'I/O
    ''' </summary>
    Public Shared Sub DiagnosticWrite(message As String)
        If Not UseDetailedDiagnostics Then Return
        Try
            Dim line As String = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}{Environment.NewLine}"
            SyncLock GetType(CDAudioAnalyzer)
                ' Écrire dans le fichier principal (ou fallback) en essayant les deux emplacements
                Try
                    System.IO.File.AppendAllText(DiagnosticsLogPath, line, System.Text.Encoding.UTF8)
                Catch ex As Exception
                    If Not String.IsNullOrEmpty(alternateDiagnosticsLogPath) Then
                        Try
                            System.IO.File.AppendAllText(alternateDiagnosticsLogPath, line, System.Text.Encoding.UTF8)
                        Catch
                            ' suppression silencieuse si écriture impossible
                        End Try
                    Else
                        ' essayer de créer le fichier initiaux si possible
                        Try
                            Dim alt As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioPlay_AnalysisLog.txt")
                            System.IO.File.AppendAllText(alt, line, System.Text.Encoding.UTF8)
                            alternateDiagnosticsLogPath = alt
                        Catch
                        End Try
                    End If
                End Try
            End SyncLock
        Catch ex As Exception
            Try
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] DiagnosticWrite failed: {ex.Message}")
            Catch
            End Try
        End Try
    End Sub

    '''
    ''' <summary>
    ''' Durée minimale de silence de secours (fallback) en secondes lorsque
    ''' aucun silence >= MinTransitionSilenceSeconds n'est trouvé.
    ''' Utilisé pour gérer les fade-outs courts/modérés.
    ''' </summary>
    Public Shared Property FallbackShortSilenceSeconds As Double = 0.6

    '''
    ''' <summary>
    ''' Seuil conservateur : ne pas couper la fin d'une piste si le silence détecté
    ''' se termine trop en amont du TOC (en secondes). Permet d'éviter de tronquer
    ''' les longs fade-outs qui contiennent des passages faibles avant la frontière.
    ''' </summary>
    Public Shared Property ConservativeTrimPreTOCSeconds As Double = 1.5


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
        Public Property SilenceCenterFrame As Integer = -1 ' Centre du silence détecté
        Public Property TransitionAnalyzed As Boolean = False  ' Indique si une analyse de transition a été faite
        Public Property PreferAdjustNextStart As Boolean = False ' Si true, préférer ajuster le début de la piste suivante

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
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ===========================================")
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ANALYSE PISTE {track.TrackNumber}")
            ' Write a human-friendly TRACK header with Artist/Title when available
            Try
                Dim label = GetTrackLabel(track)
                DiagnosticWrite($"TRACK: {label}")
            Catch
            End Try
            DiagnosticWrite($"ANALYSE PISTE {track.TrackNumber}: original {track.StartFrame}-{track.EndFrame}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ===========================================")

            ' Toujours analyser le début de la piste de manière indépendante
            Dim startTrimFrames = AnalyzeTrackStart(track)
            If startTrimFrames > 0 Then
                ' Apply cap for start trims to avoid large accidental shifts
                Dim maxStartTrimFrames As Integer = CInt(MaxStartTrimSeconds * 75)
                If startTrimFrames > maxStartTrimFrames Then
                    DiagnosticWrite($"Piste {track.TrackNumber} start trim {startTrimFrames / 75.0:F2}s exceeds MaxStartTrimSeconds {MaxStartTrimSeconds:F2}s -> capping to {maxStartTrimFrames} frames")
                    startTrimFrames = maxStartTrimFrames
                End If
                result.AdjustedStartFrame = track.StartFrame + startTrimFrames
                result.TrimmedStartFrames = startTrimFrames
                result.WasAdjusted = True
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Debut ajuste : +{startTrimFrames / 75.0:F2}s ({startTrimFrames} frames)")
                DiagnosticWrite($"Piste {track.TrackNumber} start trim: +{startTrimFrames / 75.0:F2}s ({startTrimFrames} frames) => {result.AdjustedStartFrame}")
            Else
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Debut OK : pas de silence detecte")
                DiagnosticWrite($"Piste {track.TrackNumber} start: no trim => {result.AdjustedStartFrame}")
            End If

            ' Si une piste suivante existe, analyser la TRANSITION
            If nextTrack IsNot Nothing Then
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Analyse de la TRANSITION entre piste {track.TrackNumber} et {nextTrack.TrackNumber}")
                Dim transitionResult = AnalyzeTransition(track, nextTrack)

                If transitionResult.SilenceFound Then
                    result.TransitionAnalyzed = True
                    result.SilenceStartFrame = transitionResult.SilenceStart
                    result.SilenceEndFrame = transitionResult.SilenceEnd
                    result.SilenceCenterFrame = transitionResult.SilenceCenter
                    DiagnosticWrite($"Piste {track.TrackNumber} transition silence: {transitionResult.SilenceStart}-{transitionResult.SilenceEnd} dur={transitionResult.SilenceDuration:F2}s center={transitionResult.SilenceCenter}")

                    ' Déterminer si le silence contient une portion APRÈS le TOC -> préférer ajuster la piste suivante
                    ' (utiliser la fin du silence plutôt que son centre pour mieux capter les silences qui débordent)
                    If transitionResult.SilenceEnd > track.EndFrame Then
                        result.PreferAdjustNextStart = True
                    End If

                    ' If the chosen silence has a center, use the center as the canonical cut point for both
                    ' start and end adjustments (we want to cut at the CENTER of the silence).
                    Dim canonicalCutFrame As Integer = transitionResult.SilenceCenter
                    If canonicalCutFrame <= 0 Then
                        canonicalCutFrame = transitionResult.SilenceStart + SafetyMarginFrames
                    End If

                    ' If we prefer to adjust the next track start (silence extends after TOC), mark and skip end cut
                    If result.PreferAdjustNextStart Then
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] INFO: Silence with portion AFTER TOC -> prefer adjusting next start; canonicalCut={canonicalCutFrame}")
                        DiagnosticWrite($"Piste {track.TrackNumber} prefer adjust next start: silence extends after TOC -> canonicalCut={canonicalCutFrame}")
                        ' If ForceCenterCuts is enabled, apply the center cut by adjusting the next track start later in reconciliation
                        If ForceCenterCuts Then
                            DiagnosticWrite($"ForceCenterCuts enabled: will prefer center-based start adjustment for next track (canonicalCut={canonicalCutFrame})")
                        End If
                    Else
                    ' Apply center-based end cut automatically when silence found.
                        Dim conservativeEndLimitFrame As Integer = track.EndFrame - CInt(ConservativeTrimPreTOCSeconds * 75)
                        Dim desiredCutFrame As Integer = Math.Min(track.EndFrame, canonicalCutFrame)

                        ' Calculate tentative trimmed frames and cap by a dynamic limit:
                        ' Use the smaller of MaxTrimSeconds and half of the detected silence duration
                        Dim maxTrimFrames As Integer = CInt(MaxTrimSeconds * 75)
                        Dim halfSilenceFrames As Integer = CInt(Math.Floor(transitionResult.SilenceDuration * 75.0 / 2.0))
                        Dim capFrames As Integer = Math.Min(maxTrimFrames, Math.Max(0, halfSilenceFrames))
                        Dim tentativeTrimmed As Integer = track.EndFrame - desiredCutFrame
                        If tentativeTrimmed > capFrames Then
                            Dim cappedCutFrame = track.EndFrame - capFrames
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] INFO: center cut {desiredCutFrame} would trim {tentativeTrimmed} frames which exceeds dynamic cap {capFrames} frames -> capping to {cappedCutFrame}")
                            DiagnosticWrite($"Piste {track.TrackNumber} end cut capped by dynamic cap: originalCenter={desiredCutFrame} cappedCutFrame={cappedCutFrame} originalTrimFrames={tentativeTrimmed} capFrames={capFrames} (halfSilenceFrames={halfSilenceFrames}, maxTrimFrames={maxTrimFrames})")
                            desiredCutFrame = cappedCutFrame
                            tentativeTrimmed = track.EndFrame - desiredCutFrame
                        End If

                        ' Log if center is significantly before conservative limit (informational)
                        If canonicalCutFrame < conservativeEndLimitFrame Then
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] INFO: Chosen center {canonicalCutFrame} is before conservative limit {conservativeEndLimitFrame} -> applying center-based cut (auto-apply)")
                            DiagnosticWrite($"Piste {track.TrackNumber} applying center before conservative limit: center={canonicalCutFrame} limit={conservativeEndLimitFrame}")
                        End If

                        ' Apply end cut at the chosen (or capped) frame
                        Dim cutFrame As Integer = desiredCutFrame
                        result.AdjustedEndFrame = cutFrame
                        result.TrimmedEndFrames = track.EndFrame - cutFrame
                        result.WasAdjusted = True
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Fin ajustee (center-based): -{result.TrimmedEndFrames / 75.0:F2}s ({result.TrimmedEndFrames} frames) to {cutFrame}")
                        DiagnosticWrite($"Piste {track.TrackNumber} end cut: cutFrame={cutFrame} trimmed={result.TrimmedEndFrames / 75.0:F2}s reason=centered_transition_silence start={transitionResult.SilenceStart} end={transitionResult.SilenceEnd} center={canonicalCutFrame}")
                    End If
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] WARNING: Pas de silence clair detecte dans la transition")
                    DiagnosticWrite($"Piste {track.TrackNumber} transition: no clear silence detected")
                End If
            Else
                ' Dernière piste du CD : analyser la fin pour détecter un silence final valide
                ' et ne couper que si un silence clair et suffisamment long est trouvé.
                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Derniere piste du CD : analyse de fin activee pour detecter silence final")

                Dim endTrimFrames = AnalyzeTrackEnd(track)
                If endTrimFrames <> 0 Then
                    ' Calculer la nouvelle frame de fin (endTrimFrames signé : +trim, -extension)
                    Dim newEndFrame As Integer = Math.Max(track.StartFrame, track.EndFrame - endTrimFrames)
                    result.AdjustedEndFrame = newEndFrame
                    result.TrimmedEndFrames = track.EndFrame - newEndFrame
                    result.WasAdjusted = True
                    If result.TrimmedEndFrames >= 0 Then
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Derniere piste : fin ajustee conservativement: -{result.TrimmedEndFrames / 75.0:F2}s ({result.TrimmedEndFrames} frames)")
                    Else
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Derniere piste : fin etendue: +{(-result.TrimmedEndFrames) / 75.0:F2}s ({-result.TrimmedEndFrames} frames)")
                    End If
                    DiagnosticWrite($"Piste {track.TrackNumber} last-track end delta: requested={endTrimFrames / 75.0:F2}s applied={result.TrimmedEndFrames / 75.0:F2}s")
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] INFO: Derniere piste : aucun silence final valide detecte, fin TOC conservee")
                    DiagnosticWrite($"Piste {track.TrackNumber} last-track: no valid final silence detected -> keep TOC end")
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

            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] {result.AnalysisMessage}")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Erreur analyse piste {track.TrackNumber}: {ex.Message}")
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
        ' Overlap detection
        Public Property OverlapDetected As Boolean = False
        Public Property OverlapDurationSeconds As Double = 0.0
        Public Property OverlapCorrelation As Double = 0.0
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
            Dim tocBoundary As Integer = currentTrack.EndFrame

            Dim framesBefore As Integer = CInt(TransitionWindowBeforeSeconds * 75)
            Dim framesAfter As Integer = CInt(TransitionWindowAfterSeconds * 75)

            Dim analyzeStartFrame As Integer = Math.Max(currentTrack.StartFrame, tocBoundary - framesBefore)
            Dim analyzeEndFrame As Integer = Math.Min(nextTrack.EndFrame - 1, tocBoundary + framesAfter)
            Dim framesToAnalyze As Integer = analyzeEndFrame - analyzeStartFrame + 1

            If framesToAnalyze <= 0 Then Return result

            Using reader As New CDAudioManager.CDReader(currentTrack.Drive, currentTrack.TrackNumber, currentTrack.Duration, analyzeStartFrame, analyzeEndFrame)
                Dim bytesToRead As Integer = framesToAnalyze * 2352
                Dim buffer(bytesToRead - 1) As Byte
                Dim bytesRead As Integer = reader.Read(buffer, 0, bytesToRead)

                If bytesRead <= 0 Then Return result

                ' Parameters for slice-level analysis
                Dim sliceBytes As Integer = CInt(0.05 * 44100 * 2 * 2) ' 50ms in bytes
                Dim slicesNeeded As Integer = CInt(Math.Ceiling(MinSustainedSilenceSeconds / 0.05))
                Dim minConsecutiveSlices As Integer = Math.Max(4, slicesNeeded)

                Dim silencesDetectes As New List(Of (start As Integer, endPos As Integer, distanceFromTOC As Double))
                Dim consecutiveSilentSlices As Integer = 0
                Dim silenceStartOffset As Integer = -1

                For off As Integer = 0 To Math.Max(0, bytesRead - sliceBytes) Step sliceBytes
                    Dim rms As Double = CalculateRMS(buffer, off, sliceBytes)
                    If rms < SilenceThreshold Then
                        If silenceStartOffset = -1 Then
                            silenceStartOffset = off
                            consecutiveSilentSlices = 1
                        Else
                            consecutiveSilentSlices += 1
                        End If
                    Else
                        If consecutiveSilentSlices >= minConsecutiveSlices AndAlso silenceStartOffset >= 0 Then
                            Dim silenceStart As Integer = analyzeStartFrame + (silenceStartOffset \ 2352)
                            Dim silenceEnd As Integer = analyzeStartFrame + (off \ 2352)
                            Dim silenceCenter As Integer = (silenceStart + silenceEnd) \ 2
                            Dim distanceFromTOC As Double = Math.Abs(silenceCenter - tocBoundary)
                            silencesDetectes.Add((silenceStart, silenceEnd, distanceFromTOC))
                        End If
                        silenceStartOffset = -1
                        consecutiveSilentSlices = 0
                    End If
                Next



                ' If we finished inside a silence, close it
                If consecutiveSilentSlices >= minConsecutiveSlices AndAlso silenceStartOffset >= 0 Then
                    Dim silenceStart As Integer = analyzeStartFrame + (silenceStartOffset \ 2352)
                    Dim silenceEnd As Integer = analyzeEndFrame
                    Dim silenceCenter As Integer = (silenceStart + silenceEnd) \ 2
                    Dim distanceFromTOC As Double = Math.Abs(silenceCenter - tocBoundary)
                    silencesDetectes.Add((silenceStart, silenceEnd, distanceFromTOC))
                End If

                ' Overlap detection (non-fatal): compare RMS vectors before/after TOC
                Try
                    Dim overlapWindowSeconds As Integer = Math.Min(10, CInt(TransitionWindowBeforeSeconds))
                    Dim overlapFrames As Integer = overlapWindowSeconds * 75

                    Dim lastStart As Integer = Math.Max(analyzeStartFrame, tocBoundary - overlapFrames)
                    Dim lastEnd As Integer = Math.Min(tocBoundary - 1, analyzeEndFrame)
                    Dim firstStart As Integer = Math.Max(tocBoundary, analyzeStartFrame)
                    Dim firstEnd As Integer = Math.Min(analyzeEndFrame, tocBoundary + overlapFrames - 1)

                    If lastEnd >= lastStart AndAlso firstEnd >= firstStart Then
                        Dim lastBytes((lastEnd - lastStart + 1) * 2352 - 1) As Byte
                        Dim firstBytes((firstEnd - firstStart + 1) * 2352 - 1) As Byte

                        Using rLast As New CDAudioManager.CDReader(currentTrack.Drive, currentTrack.TrackNumber, currentTrack.Duration, lastStart, lastEnd)
                            rLast.Read(lastBytes, 0, lastBytes.Length)
                        End Using
                        Using rFirst As New CDAudioManager.CDReader(currentTrack.Drive, nextTrack.TrackNumber, nextTrack.Duration, firstStart, firstEnd)
                            rFirst.Read(firstBytes, 0, firstBytes.Length)
                        End Using

                        Dim lastSlices As New List(Of Double)
                        For off As Integer = 0 To Math.Max(0, lastBytes.Length - sliceBytes) Step sliceBytes
                            lastSlices.Add(CalculateRMS(lastBytes, off, sliceBytes))
                        Next
                        Dim firstSlices As New List(Of Double)
                        For off As Integer = 0 To Math.Max(0, firstBytes.Length - sliceBytes) Step sliceBytes
                            firstSlices.Add(CalculateRMS(firstBytes, off, sliceBytes))
                        Next

                        Dim maxLast As Double = If(lastSlices.Count > 0, lastSlices.Max(), 0.0)
                        Dim maxFirst As Double = If(firstSlices.Count > 0, firstSlices.Max(), 0.0)

                        If lastSlices.Count > 2 AndAlso firstSlices.Count > 2 AndAlso maxLast > 0.000001 AndAlso maxFirst > 0.000001 Then
                            Dim lastNorm = lastSlices.Select(Function(v) v / maxLast).ToArray()
                            Dim firstNorm = firstSlices.Select(Function(v) v / maxFirst).ToArray()
                            Dim minLen As Integer = Math.Min(lastNorm.Length, firstNorm.Length)
                            If minLen > 2 Then
                                Dim corrSum As Double = 0
                                For i As Integer = 0 To minLen - 1
                                    corrSum += lastNorm(lastNorm.Length - minLen + i) * firstNorm(i)
                                Next
                                Dim corr As Double = corrSum / minLen
                                If corr > 0.65 Then
                                    result.OverlapDetected = True
                                    result.OverlapCorrelation = corr
                                    result.OverlapDurationSeconds = minLen * 0.05
                                End If
                            End If
                        End If
                    End If
                Catch exOverlap As Exception
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Erreur overlap detection: {exOverlap.Message}")
                End Try

                ' Select best silence if any using a balanced scoring of before/after candidates
                If silencesDetectes.Count > 0 Then
                    Dim candidates As New List(Of (start As Integer, endPos As Integer, durationFrames As Integer, distance As Double, proportionAfter As Double, score As Double))
                    For Each s In silencesDetectes
                        Dim durFrames As Integer = s.endPos - s.start
                        If durFrames <= 0 Then Continue For
                        Dim durSec As Double = durFrames / 75.0
                        Dim proportionAfter As Double = 0.0
                        If s.endPos > tocBoundary Then
                            Dim afterFrames As Integer = s.endPos - Math.Max(s.start, tocBoundary)
                            proportionAfter = If(durFrames > 0, afterFrames / CDbl(durFrames), 0.0)
                        End If
                        ' Score: favor longer silences, penalize distance from TOC, slightly favor after-TOC proportion
                        Dim score As Double = durSec * 3.0 - (s.distanceFromTOC / 75.0) * 0.5 + proportionAfter * 2.0
                        candidates.Add((s.start, s.endPos, durFrames, s.distanceFromTOC, proportionAfter, score))
                    Next

                    Dim minSilenceFrames As Integer = CInt(MinTransitionSilenceSeconds * 75)
                    Dim proximityFrames As Integer = CInt(TransitionProximityWindowSeconds * 75)

                    ' Filter sensible candidates by minimal duration and reasonable proximity
                    Dim filtered = candidates.Where(Function(c) c.durationFrames >= minSilenceFrames AndAlso Math.Abs(c.distance) <= proximityFrames).ToList()
                    If filtered.Count = 0 Then
                        ' Relax criteria using fallback short silence threshold and wider proximity
                        Dim fallbackMinFrames As Integer = CInt(FallbackShortSilenceSeconds * 75)
                        filtered = candidates.Where(Function(c) c.durationFrames >= fallbackMinFrames).OrderBy(Function(c) Math.Abs(c.distance)).ToList()
                        If filtered.Count = 0 Then
                            Return result
                        End If
                    End If

                    ' Choose best by score
                    Dim best = filtered.OrderByDescending(Function(c) c.score).First()
                    Dim centerFrame As Integer = (best.start + best.endPos) \ 2

                    result.SilenceFound = True
                    result.SilenceStart = best.start
                    result.SilenceEnd = best.endPos
                    result.SilenceCenter = centerFrame
                    result.SilenceDuration = best.durationFrames / 75.0
                    ' Also expose center to track-level analysis for reconciliation
                    ' We'll copy this center into the TrackAnalysis later
                    ' Also expose center to track-level analysis for reconciliation
                    result.SilenceCenter = centerFrame

                    DiagnosticWrite($"AnalyzeTransition: chosen silence {result.SilenceStart}-{result.SilenceEnd} dur={result.SilenceDuration:F2}s propAfter={best.proportionAfter:F2} score={best.score:F2} for track {currentTrack.TrackNumber}")
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Erreur analyse transition: {ex.Message}")
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
    ''' VERSION CONSERVATRICE : Scanne uniquement les 60 dernières secondes
    ''' et exige un silence continu >= 2s proche de la fin (3s)
    ''' </summary>
    Private Shared Function AnalyzeTrackEnd(track As CDAudioManager.CDTrack) As Integer
        ' Conservative scan for final silence around the TOC for the last track only.
        ' Returns a signed number of frames:
        '  - positive: number of frames to trim from the end (cut earlier)
        '  - negative: number of frames to extend beyond TOC (if we detect audio after TOC and need to include it)
        Try
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Analyse finale (last track) piste {track.TrackNumber} - scan avant/apres TOC")
            DiagnosticWrite($"Piste {track.TrackNumber} last-track: scanning around TOC for final silence (before/after)")

            ' Lire une fenêtre pour la dernière piste : utiliser 10s avant et 10s après le TOC
            Dim beforeSeconds As Integer = 10
            Dim afterSeconds As Integer = 10
            Dim toc As Integer = track.EndFrame

            Dim analyzeStart As Integer = Math.Max(track.StartFrame, toc - CInt(beforeSeconds * 75))
            Dim analyzeEnd As Integer = toc + CInt(afterSeconds * 75)

            ' Clamp analyzeEnd to a reasonable max (e.g., track.EndFrame + afterSeconds) to avoid reading unrelated tracks
            analyzeEnd = Math.Min(analyzeEnd, track.EndFrame + CInt(afterSeconds * 75))

            Dim framesToAnalyze As Integer = analyzeEnd - analyzeStart + 1
            If framesToAnalyze <= 0 Then
                DiagnosticWrite($"Piste {track.TrackNumber} last-track: no frames to analyze")
                Return 0
            End If

            Using reader As New CDAudioManager.CDReader(track.Drive, track.TrackNumber, track.Duration, analyzeStart, analyzeEnd)
                Dim bytesToRead As Integer = framesToAnalyze * 2352
                Dim buffer(bytesToRead - 1) As Byte
                Dim bytesRead As Integer = reader.Read(buffer, 0, bytesToRead)
                If bytesRead <= 0 Then
                    DiagnosticWrite($"Piste {track.TrackNumber} last-track: read 0 bytes")
                    Return 0
                End If

                ' Analyse similaire à AnalyzeTransition: détecter silences
                Dim samplesPerSlice As Integer = CInt(0.05 * 44100 * 2 * 2) ' 50ms
                Dim slicesNeeded As Integer = CInt(Math.Ceiling(MinSustainedSilenceSeconds / 0.05))
                Dim minConsecutiveSlices As Integer = Math.Max(4, slicesNeeded)

                Dim consecutiveSilentSlices As Integer = 0
                Dim silenceStartSliceOffset As Integer = -1
                Dim detectedSilences As New List(Of (start As Integer, [end] As Integer))

                For offset As Integer = 0 To bytesRead - samplesPerSlice Step samplesPerSlice
                    Dim rms As Double = CalculateRMS(buffer, offset, samplesPerSlice)
                    If rms < SilenceThreshold Then
                        If silenceStartSliceOffset = -1 Then
                            silenceStartSliceOffset = offset
                            consecutiveSilentSlices = 1
                        Else
                            consecutiveSilentSlices += 1
                        End If
                    Else
                        If consecutiveSilentSlices >= minConsecutiveSlices AndAlso silenceStartSliceOffset >= 0 Then
                            Dim sStart As Integer = analyzeStart + (silenceStartSliceOffset \ 2352)
                            Dim sEnd As Integer = analyzeStart + (offset \ 2352)
                            detectedSilences.Add((sStart, sEnd))
                        End If
                        silenceStartSliceOffset = -1
                        consecutiveSilentSlices = 0
                    End If
                Next

                ' Si on termine sur du silence
                If consecutiveSilentSlices >= minConsecutiveSlices AndAlso silenceStartSliceOffset >= 0 Then
                    Dim sStart As Integer = analyzeStart + (silenceStartSliceOffset \ 2352)
                    Dim sEnd As Integer = analyzeEnd
                    detectedSilences.Add((sStart, sEnd))
                End If

                ' Chercher un silence qui est APRES le TOC (on veut couper/étendre au CENTRE du silence)
                Dim silencesAfter = detectedSilences.Where(Function(s) s.[end] >= toc).OrderBy(Function(s) Math.Abs(((s.start + s.[end]) \ 2) - toc)).ToList()
                If silencesAfter.Count > 0 Then
                    Dim silenceAfterTOC = silencesAfter(0)
                    ' Couper/étendre au centre du silence
                    Dim centerFrame As Integer = ((silenceAfterTOC.start + silenceAfterTOC.[end]) \ 2)
                    Dim cutFrame As Integer = centerFrame
                    Dim trimFrames As Integer = track.EndFrame - cutFrame
                    DiagnosticWrite($"Piste {track.TrackNumber} last-track: (short window) detected silence after TOC {silenceAfterTOC.start}-{silenceAfterTOC.[end]} -> center {centerFrame}, trim {trimFrames} frames")
                    Return trimFrames
                End If

                ' --- Si rien trouvé dans la fenêtre courte (10s), tenter une passe étendue (lead-out) de +30s ---
                Dim extendedAfterSeconds As Integer = 30
                Dim extendedAnalyzeEnd As Integer = toc + CInt(extendedAfterSeconds * 75)
                ' limiter à une borne raisonnable (on peut dépasser EndFrame si lecteur/TOC le permet)
                Dim extFramesToAnalyze As Integer = extendedAnalyzeEnd - analyzeStart + 1
                If extFramesToAnalyze > framesToAnalyze Then
                    Try
                        Using readerExt As New CDAudioManager.CDReader(track.Drive, track.TrackNumber, track.Duration, analyzeStart, extendedAnalyzeEnd)
                            Dim bytesToReadExt As Integer = extFramesToAnalyze * 2352
                            Dim bufferExt(bytesToReadExt - 1) As Byte
                            Dim bytesReadExt As Integer = readerExt.Read(bufferExt, 0, bytesToReadExt)
                            If bytesReadExt > 0 Then
                                ' analyser la fenêtre étendue de la même manière
                                Dim detectedSilencesExt As New List(Of (start As Integer, [end] As Integer))
                                Dim consecutiveSilentSlicesExt As Integer = 0
                                Dim silenceStartSliceOffsetExt As Integer = -1
                                For offset As Integer = 0 To bytesReadExt - samplesPerSlice Step samplesPerSlice
                                    Dim rmsExt As Double = CalculateRMS(bufferExt, offset, samplesPerSlice)
                                    If rmsExt < SilenceThreshold Then
                                        If silenceStartSliceOffsetExt = -1 Then
                                            silenceStartSliceOffsetExt = offset
                                            consecutiveSilentSlicesExt = 1
                                        Else
                                            consecutiveSilentSlicesExt += 1
                                        End If
                                    Else
                                        If consecutiveSilentSlicesExt >= minConsecutiveSlices AndAlso silenceStartSliceOffsetExt >= 0 Then
                                            Dim sStartExt As Integer = analyzeStart + (silenceStartSliceOffsetExt \ 2352)
                                            Dim sEndExt As Integer = analyzeStart + (offset \ 2352)
                                            detectedSilencesExt.Add((sStartExt, sEndExt))
                                        End If
                                        silenceStartSliceOffsetExt = -1
                                        consecutiveSilentSlicesExt = 0
                                    End If
                                Next

                                If consecutiveSilentSlicesExt >= minConsecutiveSlices AndAlso silenceStartSliceOffsetExt >= 0 Then
                                    Dim sStartExt As Integer = analyzeStart + (silenceStartSliceOffsetExt \ 2352)
                                    Dim sEndExt As Integer = extendedAnalyzeEnd
                                    detectedSilencesExt.Add((sStartExt, sEndExt))
                                End If

                                Dim silencesAfterExt = detectedSilencesExt.Where(Function(s) s.[end] >= toc).OrderBy(Function(s) Math.Abs(((s.start + s.[end]) \ 2) - toc)).ToList()
                                If silencesAfterExt.Count > 0 Then
                                    Dim sel = silencesAfterExt(0)
                                    Dim centerFrameExt As Integer = ((sel.start + sel.[end]) \ 2)
                                    Dim trimFramesExt As Integer = track.EndFrame - centerFrameExt
                                    DiagnosticWrite($"Piste {track.TrackNumber} last-track: (extended window) detected silence after TOC {sel.start}-{sel.[end]} -> center {centerFrameExt}, trim {trimFramesExt} frames")
                                    Return trimFramesExt
                                End If
                            End If
                        End Using
                    Catch ex As Exception
                        DiagnosticWrite($"Piste {track.TrackNumber} last-track: extended read exception: {ex.Message}")
                    End Try
                End If

                ' Si aucun silence après TOC, rechercher un silence proche du TOC (avant) et couper si suffisamment proche
                Dim proximityFrames As Integer = CInt(TransitionProximityWindowSeconds * 75)
                ' Pour la dernière piste, exiger que si le silence est AVANT le TOC, il se termine proche du TOC
                Dim maxPreTOCDistance As Integer = CInt(ConservativeTrimPreTOCSeconds * 75)
                Dim candidates = detectedSilences _
                    .Where(Function(s)
                               ' garder si centre du silence est dans la fenêtre de proximité
                               Dim centerDist = Math.Abs(((s.start + s.[end]) \ 2) - toc)
                               If centerDist <= proximityFrames Then
                                   ' si le silence est après le TOC, OK
                                   If s.[end] >= toc Then
                                       Return True
                                   End If
                                   ' si le silence est avant le TOC, n'accepter que s'il se termine suffisamment proche du TOC
                                   Dim distanceEndToTOC = toc - s.[end]
                                   Return distanceEndToTOC <= maxPreTOCDistance
                               End If
                               Return False
                           End Function) _
                    .OrderBy(Function(s) Math.Abs(((s.start + s.[end]) \ 2) - toc)).ToList()
                If candidates.Count > 0 Then
                    Dim candidate = candidates(0)
                    ' Couper au centre du silence détecté
                    Dim centerFrame As Integer = ((candidate.start + candidate.[end]) \ 2)
                    Dim cutFrame As Integer = Math.Min(track.EndFrame, centerFrame)
                    Dim trimFrames As Integer = track.EndFrame - cutFrame
                    If trimFrames > 0 Then
                        DiagnosticWrite($"Piste {track.TrackNumber} last-track: detected nearby silence {candidate.start}-{candidate.[end]} -> center {centerFrame}, trim {trimFrames} frames")
                        Return trimFrames
                    End If
                End If

                ' Aucun silence clair détecté autour du TOC
                DiagnosticWrite($"Piste {track.TrackNumber} last-track: no clear silence found around TOC")
                Return 0
            End Using
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Erreur analyse fin piste: {ex.Message}")
            DiagnosticWrite($"Piste {track.TrackNumber} last-track: exception during end analysis: {ex.Message}")
            Return 0
        End Try
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

            ' === Passe secondaire (relaxed) uniquement pour les pistes non ajustées ===
            Try
                Dim adjustedCount As Integer = 0
                Dim nonAdjustedCount As Integer = 0
                For Each r In results
                    If r.WasAdjusted Then
                        adjustedCount += 1
                    Else
                        nonAdjustedCount += 1
                    End If
                Next

                DiagnosticWrite($"Secondary pass decision: adjustedCount={adjustedCount}, nonAdjustedCount={nonAdjustedCount}")

                Dim needSecondary As Boolean = (nonAdjustedCount > 0) Or ForceSecondaryPass

                If needSecondary Then
                    DiagnosticWrite("Secondary pass: starting relaxed re-analysis for non-adjusted tracks")

                    ' Sauvegarder valeurs originales
                    Dim origSilenceThreshold = SilenceThreshold
                    Dim origMinSustained = MinSustainedSilenceSeconds
                    Dim origBefore = TransitionWindowBeforeSeconds
                    Dim origAfter = TransitionWindowAfterSeconds

                    Try
                        SyncLock GetType(CDAudioAnalyzer)
                            ' More aggressive relaxed parameters for secondary pass
                            SilenceThreshold = Math.Min(0.1, origSilenceThreshold * 5.0)
                            MinSustainedSilenceSeconds = 0.1
                            TransitionWindowBeforeSeconds = origBefore + 20.0
                            TransitionWindowAfterSeconds = origAfter + 20.0
                        End SyncLock

                        ' Ré-analyser chaque piste non ajustée
                        For idx As Integer = 0 To results.Count - 1
                            Dim res = results(idx)
                            If res.WasAdjusted Then Continue For

                            Dim trackIndex = selectedIndices(idx)
                            If trackIndex < 0 OrElse trackIndex >= tracks.Count Then Continue For

                            Dim track = tracks(trackIndex)
                            Dim nextTrack As CDAudioManager.CDTrack = Nothing
                            If trackIndex + 1 < tracks.Count Then nextTrack = tracks(trackIndex + 1)

                            DiagnosticWrite($"Secondary pass: re-analyzing Track {track.TrackNumber} with relaxed params (threshold={SilenceThreshold:F6}, minSustained={MinSustainedSilenceSeconds:F2}, windowBefore={TransitionWindowBeforeSeconds:F1}, windowAfter={TransitionWindowAfterSeconds:F1})")
                            Dim relaxedAnalysis = AnalyzeTrack(track, nextTrack, Nothing)

                            If relaxedAnalysis.WasAdjusted Then
                                DiagnosticWrite($"Secondary pass: Track {track.TrackNumber} produced adjustment: StartTrim={relaxedAnalysis.TrimmedStartFrames} EndTrim={relaxedAnalysis.TrimmedEndFrames} (applying)")
                                ' Appliquer l'ajustement retourné
                                results(idx) = relaxedAnalysis
                            Else
                                DiagnosticWrite($"Secondary pass: Track {track.TrackNumber} no adjustment found with relaxed params")
                                ' Sauvegarder un extrait autour du TOC pour diagnostic (±10s)
                                Try
                                    Dim nextTrackLocal As CDAudioManager.CDTrack = Nothing
                                    If trackIndex + 1 < tracks.Count Then nextTrackLocal = tracks(trackIndex + 1)
                                    SaveTransitionSnippet(track, nextTrackLocal, 10, 10)
                                Catch exSnippet As Exception
                                    DiagnosticWrite($"Secondary pass: failed to save snippet for Track {track.TrackNumber}: {exSnippet.Message}")
                                End Try
                            End If
                        Next

                        ' Re-run reconciliation to ensure no overlaps after applying secondary adjustments
                        For i As Integer = 0 To results.Count - 2
                            Dim cur = results(i)
                            Dim nxt = results(i + 1)

                            If nxt.AdjustedStartFrame <= 0 Then
                                nxt.AdjustedStartFrame = nxt.OriginalStartFrame
                            End If

                            If cur.TransitionAnalyzed AndAlso cur.PreferAdjustNextStart AndAlso cur.SilenceEndFrame > cur.OriginalEndFrame Then
                                ' Use the center of the detected silence as the canonical cut for next start
                                Dim canonicalNextStart As Integer = cur.SilenceCenterFrame
                                If canonicalNextStart <= 0 Then canonicalNextStart = cur.SilenceEndFrame
                                Dim proposedStart As Integer = canonicalNextStart + SafetyMarginFrames
                                Dim maxStartTrimFrames As Integer = CInt(MaxStartTrimSeconds * 75)
                                Dim maxAllowedStart As Integer = nxt.OriginalStartFrame + maxStartTrimFrames

                                If proposedStart > maxAllowedStart Then
                                    DiagnosticWrite($"Réconciliation (secondary): proposedStart {proposedStart} exceeds max allowed start {maxAllowedStart} -> capping to {maxAllowedStart}")
                                    proposedStart = maxAllowedStart
                                End If

                                If proposedStart >= nxt.AdjustedEndFrame Then
                                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] WARNING: Reconciliation impossible sans inversion (secondary) entre piste {cur.TrackNumber} et {nxt.TrackNumber}")
                                ElseIf proposedStart > nxt.AdjustedStartFrame Then
                                    nxt.AdjustedStartFrame = proposedStart
                                    nxt.TrimmedStartFrames = nxt.AdjustedStartFrame - nxt.OriginalStartFrame
                                    nxt.WasAdjusted = True
                                End If

                            ElseIf cur.AdjustedEndFrame >= nxt.AdjustedStartFrame Then
                                Dim correctedStart As Integer = cur.AdjustedEndFrame + 1
                                If correctedStart >= nxt.AdjustedEndFrame Then
                                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] WARNING: Reconciliation impossible sans inversion (secondary) entre piste {cur.TrackNumber} et {nxt.TrackNumber}")
                                    nxt.AdjustedStartFrame = nxt.OriginalStartFrame
                                    nxt.TrimmedStartFrames = 0
                                Else
                                    nxt.AdjustedStartFrame = correctedStart
                                    nxt.TrimmedStartFrames = nxt.AdjustedStartFrame - nxt.OriginalStartFrame
                                    nxt.WasAdjusted = True
                                End If
                            End If

                            If nxt.WasAdjusted Then
                                nxt.AnalysisMessage = $"Piste {nxt.TrackNumber}: Début +{nxt.TrimmedStartFrames / 75.0:F2}s, Fin -{nxt.TrimmedEndFrames / 75.0:F2}s"
                            Else
                                nxt.AnalysisMessage = $"Piste {nxt.TrackNumber}: OK (pas d'ajustement)"
                            End If
                        Next

                    Finally
                        ' Restaurer les paramètres originaux
                        Try
                            SyncLock GetType(CDAudioAnalyzer)
                                SilenceThreshold = origSilenceThreshold
                                MinSustainedSilenceSeconds = origMinSustained
                                TransitionWindowBeforeSeconds = origBefore
                                TransitionWindowAfterSeconds = origAfter
                            End SyncLock
                        Catch
                        End Try
                    End Try
                    DiagnosticWrite("Secondary pass: finished")
                End If
            Catch exSecondary As Exception
                DiagnosticWrite($"Secondary pass failed: {exSecondary.Message}")
            End Try

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

            ' Si la transition précédente a détecté un silence contenant une portion APRÈS le TOC,
            ' préférer ajuster le début de la piste suivante pour sauter cette portion silencieuse.
            If cur.TransitionAnalyzed AndAlso cur.PreferAdjustNextStart AndAlso cur.SilenceEndFrame > cur.OriginalEndFrame Then
                ' Use the center of the detected silence as canonical start for the next track
                Dim canonicalNextStart As Integer = cur.SilenceCenterFrame
                If canonicalNextStart <= 0 Then canonicalNextStart = cur.SilenceEndFrame
                Dim proposedStart As Integer = canonicalNextStart + SafetyMarginFrames

                ' Apply a cap to prevent advancing the next track start by too much
                Dim maxStartTrimFrames As Integer = CInt(MaxStartTrimSeconds * 75)
                Dim maxAllowedStart As Integer = nxt.OriginalStartFrame + maxStartTrimFrames

                If proposedStart > maxAllowedStart Then
                    DiagnosticWrite($"Réconciliation: proposedStart {proposedStart} exceeds max allowed start {maxAllowedStart} -> capping to {maxAllowedStart}")
                    proposedStart = maxAllowedStart
                End If

                If proposedStart >= nxt.AdjustedEndFrame Then
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] WARNING: Reconciliation impossible sans inversion (fallback) entre piste {cur.TrackNumber} et {nxt.TrackNumber} - conservation des positions TOC pour la suivante")
                ElseIf proposedStart > nxt.AdjustedStartFrame Then
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Reconciliation preferentielle: deplacement du debut de la piste {nxt.TrackNumber} a {proposedStart} (canonical={canonicalNextStart}, silence APRES TOC de la piste {cur.TrackNumber})")
                    nxt.AdjustedStartFrame = proposedStart
                    nxt.TrimmedStartFrames = nxt.AdjustedStartFrame - nxt.OriginalStartFrame
                    nxt.WasAdjusted = True
                End If

            ElseIf cur.AdjustedEndFrame >= nxt.AdjustedStartFrame Then
                ' Overlap detected. Prefer to avoid truncating the start of the next track
                ' by moving the end of the current track earlier when possible.
                Dim desiredCurEnd As Integer = nxt.AdjustedStartFrame - 1

                ' Verify we do not invert the current track (start >= end)
                If desiredCurEnd <= cur.AdjustedStartFrame Then
                    ' Cannot shorten current track safely; fallback to preserving TOC start for next
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] WARNING: Reconciliation impossible sans inversion entre piste {cur.TrackNumber} et {nxt.TrackNumber} - conservation des positions TOC pour la suivante")
                    nxt.AdjustedStartFrame = nxt.OriginalStartFrame
                    nxt.TrimmedStartFrames = 0
                Else
                    ' Shrink current track's end to avoid cutting into next track's start
                    Dim oldEnd As Integer = cur.AdjustedEndFrame
                    cur.AdjustedEndFrame = desiredCurEnd
                    cur.TrimmedEndFrames = cur.OriginalEndFrame - cur.AdjustedEndFrame
                    cur.WasAdjusted = True
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Reconciliation: moved end of track {cur.TrackNumber} earlier to {cur.AdjustedEndFrame} to avoid truncating start of {nxt.TrackNumber}")
                    DiagnosticWrite($"Reconciliation: adjusted end of track {cur.TrackNumber} from {oldEnd} to {cur.AdjustedEndFrame} to avoid truncating next track start")
                End If
            End If

            ' Mettre à jour le message d'analyse pour la piste suivante
            If nxt.WasAdjusted Then
                nxt.AnalysisMessage = $"Piste {nxt.TrackNumber}: Début +{nxt.TrimmedStartFrames / 75.0:F2}s, Fin -{nxt.TrimmedEndFrames / 75.0:F2}s"
            Else
                nxt.AnalysisMessage = $"Piste {nxt.TrackNumber}: OK (pas d'ajustement)"
            End If
        Next

        ' After pairwise reconciliation, optionally save diagnostic snippets for any tracks
        ' that remained non-adjusted so the user can inspect the TOC neighborhood.
        If ForceSaveSnippetsForAllTracks Or ForceSecondaryPass Then
            Try
                DiagnosticWrite("Pairwise: saving diagnostic snippets for non-adjusted tracks (post-reconciliation)")
                For i As Integer = 0 To results.Count - 1
                    Dim res = results(i)
                    If res.WasAdjusted Then Continue For

                    Dim trackIndex As Integer = -1
                    If i >= 0 AndAlso i < selectedIndices.Count Then trackIndex = selectedIndices(i)
                    If trackIndex < 0 OrElse trackIndex >= tracks.Count Then Continue For

                    Dim track As CDAudioManager.CDTrack = tracks(trackIndex)
                    Dim nextTrack As CDAudioManager.CDTrack = Nothing
                    If trackIndex + 1 < tracks.Count Then nextTrack = tracks(trackIndex + 1)

                    Try
                        SaveTransitionSnippet(track, nextTrack, 10, 10)
                        DiagnosticWrite($"Pairwise: saved snippet for Track {track.TrackNumber}")
                    Catch exSnippet As Exception
                        DiagnosticWrite($"Pairwise: failed to save snippet for Track {track.TrackNumber}: {exSnippet.Message}")
                    End Try
                Next
            Catch exForce As Exception
                DiagnosticWrite($"Pairwise: forced snippet pass failed: {exForce.Message}")
            End Try
        End If

        Return results
    End Function

End Class
