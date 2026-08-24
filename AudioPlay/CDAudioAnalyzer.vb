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
    Public Shared Property MaxTrimSeconds As Double = 2.0

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
    Private Shared ReadOnly ForceSecondaryPass As Boolean = False
    ' DEBUG: Force saving snippets for all tracks
    Private Shared ReadOnly ForceSaveSnippetsForAllTracks As Boolean = False
    ' Configuration flags (readable from parametres.txt in next iteration)
    Public Shared Property EnableAggressiveSecondaryPass As Boolean = False
    Public Shared Property EnableSnippetCapture As Boolean = False

    ''' <summary>
    ''' Initialise le fichier de log de diagnostic pour une nouvelle session.
    ''' Écris un en-tête SESSION START; en cas d'échec sur %TEMP%, bascule vers un fichier dans le répertoire de l'application.
    ''' </summary>
    Public Shared Sub InitializeDiagnosticsLog(Optional sessionMessage As String = "")
        Dim header As String = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] SESSION START{If(String.IsNullOrEmpty(sessionMessage), "", " - " & sessionMessage)}{Environment.NewLine}"
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
            ' Ensure diagnostics session folder exists
            Try
                Dim dir As String = GetDiagnosticsDirectory()
                If String.IsNullOrEmpty(dir) Then dir = Path.GetTempPath()
                Dim sessionFolder As String = Path.Combine(dir, $"Snippets_Session_{DateTime.Now:yyyyMMddHHmmss}")
                Directory.CreateDirectory(sessionFolder)
                ' Save current session folder path in alternateDiagnosticsLogPath for retrieval
                alternateDiagnosticsLogPath = Path.Combine(sessionFolder, Path.GetFileName(alternateDiagnosticsLogPath))
            Catch
            End Try
            ' Write diagnostic params snapshot for this session
            Try
                WriteDiagnosticParams()
            Catch
            End Try
            ' Cleanup old snippets (default retention 30 days)
            Try
                CleanupOldSnippets(30)
            Catch
            End Try
            Return
        Catch ex As Exception
            Try
                Dim alt As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioPlay_AnalysisLog.txt")
                SyncLock GetType(CDAudioAnalyzer)
                    File.WriteAllText(alt, header, Encoding.UTF8)
                End SyncLock
                alternateDiagnosticsLogPath = alt
            Catch ex2 As Exception
                Try
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] InitializeDiagnosticsLog failed: {ex.Message} | alt: {ex2.Message}")
                Catch
                End Try
            End Try
        End Try
    End Sub

    ''' <summary>
    ''' Purge all previous Snippets_Session_* directories and AudioPlay_DiagnosticParams.txt files
    ''' from the diagnostics directory (or %TEMP% fallback). This is used to ensure a clean
    ''' workspace before starting a new extraction session (invoked by UI handlers).
    ''' </summary>
    Public Shared Sub PurgeOldSessionsAndDiagnosticParams()
        Try
            Dim dir As String = GetDiagnosticsDirectory()
            If String.IsNullOrEmpty(dir) Then dir = Path.GetTempPath()
            If Not Directory.Exists(dir) Then Return

            ' Delete Snippets_Session_* directories at top level
            Try
                Dim sessions = Directory.GetDirectories(dir, "Snippets_Session_*", SearchOption.TopDirectoryOnly)
                For Each d In sessions
                    Try
                        Directory.Delete(d, True)
                        DiagnosticWrite($"PurgeOldSessionsAndDiagnosticParams: deleted session dir {d}")
                    Catch exDel As Exception
                        DiagnosticWrite($"PurgeOldSessionsAndDiagnosticParams: failed delete session dir {d}: {exDel.Message}")
                    End Try
                Next
            Catch exSess As Exception
                DiagnosticWrite($"PurgeOldSessionsAndDiagnosticParams: error enumerating session dirs: {exSess.Message}")
            End Try

            ' Delete any AudioPlay_DiagnosticParams.txt files at top level
            Try
                Dim paramsFiles = Directory.GetFiles(dir, "AudioPlay_DiagnosticParams.txt", SearchOption.TopDirectoryOnly)
                For Each f In paramsFiles
                    Try
                        File.Delete(f)
                        DiagnosticWrite($"PurgeOldSessionsAndDiagnosticParams: deleted diagnostic params {f}")
                    Catch exDel As Exception
                        DiagnosticWrite($"PurgeOldSessionsAndDiagnosticParams: failed delete params {f}: {exDel.Message}")
                    End Try
                Next
            Catch exParams As Exception
                DiagnosticWrite($"PurgeOldSessionsAndDiagnosticParams: error enumerating params files: {exParams.Message}")
            End Try
        Catch ex As Exception
            DiagnosticWrite($"PurgeOldSessionsAndDiagnosticParams error: {ex.Message}")
        End Try
    End Sub

    ' Cleanup snippet WAV files older than the provided UTC cutoff time.
    Public Shared Sub CleanupSnippetsOlderThan(cutoffUtc As DateTime)
        Try
            Dim dir As String = GetDiagnosticsDirectory()
            If String.IsNullOrEmpty(dir) Then dir = Path.GetTempPath()
            If Not Directory.Exists(dir) Then Return

            Dim wavs = Directory.GetFiles(dir, "*.wav", SearchOption.AllDirectories)
            For Each f In wavs
                Try
                    Dim info = New FileInfo(f)
                    If info.LastWriteTimeUtc < cutoffUtc.ToUniversalTime() Then
                        Try
                            info.Delete()
                            DiagnosticWrite($"CleanupSnippetsOlderThan: deleted old snippet {f}")
                        Catch exDel As Exception
                            DiagnosticWrite($"CleanupSnippetsOlderThan: failed delete {f}: {exDel.Message}")
                        End Try
                    End If
                Catch
                End Try
            Next
        Catch ex As Exception
            DiagnosticWrite($"CleanupSnippetsOlderThan error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Debug helper: save paired snippets for a given track number (1-based) and produce inspection + proposal.
    ''' Non-destructive. Usable from UI to target a single transition.
    ''' </summary>
    Public Shared Sub Debug_SaveTransitionForTrack(tracks As List(Of CDAudioManager.CDTrack), trackNumber As Integer)
        Try
            If tracks Is Nothing OrElse tracks.Count = 0 Then
                DiagnosticWrite("Debug_SaveTransitionForTrack: tracks list is empty")
                Return
            End If

            Dim track = tracks.FirstOrDefault(Function(t) t.TrackNumber = trackNumber)
            If track Is Nothing Then
                DiagnosticWrite($"Debug_SaveTransitionForTrack: track {trackNumber} not found in list")
                Return
            End If

            Dim nextTrack As CDAudioManager.CDTrack = Nothing
            ' find next by track number +1 if present
            nextTrack = tracks.FirstOrDefault(Function(t) t.TrackNumber = trackNumber + 1)

            DiagnosticWrite($"Debug_SaveTransitionForTrack: saving pair for Track {track.TrackNumber} (start={track.StartFrame} end={track.EndFrame}) next={(If(nextTrack IsNot Nothing, nextTrack.TrackNumber.ToString(), "none"))}")

            Dim s1 As SnippetInfo = Nothing
            Dim s2 As SnippetInfo = Nothing
            Try
                s1 = SaveTransitionSnippetFile(track, nextTrack, 10, 10)
            Catch ex As Exception
                DiagnosticWrite($"Debug_SaveTransitionForTrack: failed save s1: {ex.Message}")
            End Try

            If nextTrack IsNot Nothing Then
                Try
                    s2 = SaveTransitionSnippetFile(nextTrack, Nothing, 0, 20, True)
                Catch ex As Exception
                    DiagnosticWrite($"Debug_SaveTransitionForTrack: failed save s2: {ex.Message}")
                End Try
            End If

            Try
                If s1 IsNot Nothing Then InspectSnippetFile(s1.FilePath)
            Catch
            End Try
            Try
                If s2 IsNot Nothing Then InspectSnippetFile(s2.FilePath)
            Catch
            End Try

            Try
                GenerateTransitionProposal(track, nextTrack, s1, s2)
            Catch ex As Exception
                DiagnosticWrite($"Debug_SaveTransitionForTrack: GenerateTransitionProposal error: {ex.Message}")
            End Try

        Catch ex As Exception
            DiagnosticWrite($"Debug_SaveTransitionForTrack: unexpected error: {ex.Message}")
        End Try
    End Sub

    ' Event raised when a transition proposal file has been generated
    Public Shared Event TransitionProposalGenerated(filePath As String)
    ' Event raised when a snippet WAV file has been saved
    Public Shared Event SnippetSaved(filePath As String)

    ' Represents a saved snippet on disk
    Private Class SnippetInfo
        Public Property TrackNumber As Integer
        Public Property FilePath As String
        Public Property StartFrame As Integer
        Public Property EndFrame As Integer
    End Class

    ' Save a snippet and return info about it (throws on failure)
    ' centerOnStart: when True, center snippet around track START (useful for next-track beginnings)
    Private Shared Function SaveTransitionSnippetFile(currentTrack As CDAudioManager.CDTrack, nextTrack As CDAudioManager.CDTrack, secondsBefore As Integer, secondsAfter As Integer, Optional centerOnStart As Boolean = False) As SnippetInfo
        Dim toc As Integer
        If centerOnStart Then
            toc = currentTrack.StartFrame
        Else
            toc = currentTrack.EndFrame
        End If
        Dim framesBefore As Integer = secondsBefore * 75
        Dim framesAfter As Integer = secondsAfter * 75

        Dim startFrame As Integer = Math.Max(currentTrack.StartFrame, toc - framesBefore)
        Dim endFrame As Integer
        If centerOnStart Then
            ' when centering on start, do not extend past the track end
            endFrame = Math.Min(currentTrack.EndFrame, toc + framesAfter)
        Else
            If nextTrack IsNot Nothing Then
                endFrame = Math.Min(nextTrack.EndFrame - 1, toc + framesAfter)
            Else
                endFrame = toc + framesAfter
            End If
        End If

        If endFrame <= startFrame Then
            Throw New InvalidOperationException("Invalid snippet range")
        End If

        DiagnosticWrite($"SaveTransitionSnippetFile: entering for track {currentTrack.TrackNumber} startFrame={startFrame} endFrame={endFrame}")

        Using reader As New CDAudioManager.CDReader(currentTrack.Drive, currentTrack.TrackNumber, currentTrack.Duration, startFrame, endFrame)
            Dim bytesToRead As Integer = (endFrame - startFrame + 1) * 2352
            Dim buffer(bytesToRead - 1) As Byte
            Dim bytesRead As Integer = ReadWithRetries(reader, buffer, 0, bytesToRead, 5, 200)
            If bytesRead <= 0 Then
                DiagnosticWrite($"SaveTransitionSnippetFile: no bytes read for track {currentTrack.TrackNumber}")
                Throw New IOException("No bytes read from CDReader")
            End If

            Dim dir As String = GetDiagnosticsDirectory()
            If String.IsNullOrEmpty(dir) Then dir = Path.GetTempPath()
            ' use session folder if present
            Dim sessionFolders = Directory.GetDirectories(dir, "Snippets_Session_*", SearchOption.TopDirectoryOnly)
            Dim sessionDir As String = If(sessionFolders.Length > 0, sessionFolders(sessionFolders.Length - 1), dir)
            Try
                Directory.CreateDirectory(sessionDir)
            Catch
            End Try
            ' Write to a temporary .partial file and rename to .wav once complete to avoid incomplete files
            Dim fileName As String = Nothing
            Dim tmpFile As String = Path.Combine(sessionDir, $"AudioPlay_Snippet_Track{currentTrack.TrackNumber}_{DateTime.Now:yyyyMMddHHmmss}.partial")
            Dim finalFile As String = Path.Combine(sessionDir, $"AudioPlay_Snippet_Track{currentTrack.TrackNumber}_{DateTime.Now:yyyyMMddHHmmss}.wav")
            Dim wf = New WaveFormat(44100, 16, 2)
            Try
                Using w As New WaveFileWriter(tmpFile, wf)
                    w.Write(buffer, 0, bytesRead)
                End Using
                ' If we read less than expected, attempt to move to final, but if move fails keep the .partial
                Try
                    If File.Exists(finalFile) Then File.Delete(finalFile)
                Catch
                End Try
                Try
                    File.Move(tmpFile, finalFile)
                    fileName = finalFile
                Catch moveEx As Exception
                    Try
                        DiagnosticWrite($"SaveTransitionSnippetFile: move to final failed: {moveEx.Message}; keeping partial file {tmpFile}")
                    Catch
                    End Try
                    fileName = tmpFile
                End Try
            Catch exWrite As Exception
                Try
                    DiagnosticWrite($"SaveTransitionSnippetFile: failed to write snippet file: {exWrite.Message}")
                Catch
                End Try
                ' If tmp exists, keep it instead of failing the whole run
                Try
                    If File.Exists(tmpFile) Then
                        Try
                            DiagnosticWrite($"SaveTransitionSnippetFile: write failed but partial exists, continuing with {tmpFile}")
                        Catch
                        End Try
                        fileName = tmpFile
                    Else
                        Throw
                    End If
                Catch
                    ' If we cannot recover, rethrow to let caller handle
                    Throw
                End Try
            End Try
            DiagnosticWrite($"Saved transition snippet for track {currentTrack.TrackNumber} -> {fileName}")
            Try
                RaiseEvent SnippetSaved(fileName)
            Catch
            End Try

            Dim si As New SnippetInfo With {.TrackNumber = currentTrack.TrackNumber, .FilePath = fileName, .StartFrame = startFrame, .EndFrame = endFrame}
            Return si
        End Using
    End Function

    ' Compute RMS slices for a buffer. sliceMs is slice duration in milliseconds (e.g., 50).
    Private Shared Function ComputeRMSlices(buffer() As Byte, offset As Integer, length As Integer, sliceMs As Integer) As List(Of Double)
        Dim sampleRate As Integer = 44100
        Dim bytesPerSample As Integer = 2 * 2 ' 16-bit stereo
        Dim sliceBytes As Integer = CInt((sliceMs / 1000.0) * sampleRate * bytesPerSample)
        If sliceBytes <= 0 Then sliceBytes = CInt(0.05 * sampleRate * bytesPerSample)

        Dim rmsList As New List(Of Double)
        For off As Integer = offset To Math.Max(offset, Math.Min(offset + length - sliceBytes, buffer.Length - sliceBytes)) Step sliceBytes
            Dim rms = CalculateRMS(buffer, off, sliceBytes)
            rmsList.Add(rms)
        Next

        Return rmsList
    End Function

    ' Cross-correlate two RMS vectors and return (bestLag, normalizedScore)
    ' bestLag > 0 means snippetB starts after snippetA by bestLag slices
    Private Shared Function CrossCorrelateSlices(a As List(Of Double), b As List(Of Double)) As (bestLag As Integer, score As Double)
        If a Is Nothing OrElse b Is Nothing OrElse a.Count = 0 OrElse b.Count = 0 Then Return (0, 0.0)

        ' Normalize vectors to their max to reduce level influence
        Dim maxA = If(a.Count > 0, a.Max(), 1.0)
        Dim maxB = If(b.Count > 0, b.Max(), 1.0)
        If maxA <= 0 Then maxA = 1.0
        If maxB <= 0 Then maxB = 1.0

        Dim na = a.Select(Function(v) v / maxA).ToArray()
        Dim nb = b.Select(Function(v) v / maxB).ToArray()

        Dim maxLag = Math.Min(na.Length, nb.Length)
        Dim bestLag As Integer = 0
        Dim bestScore As Double = -1

        For lag As Integer = -maxLag To maxLag
            Dim sum As Double = 0
            Dim count As Integer = 0
            For i As Integer = 0 To na.Length - 1
                Dim j As Integer = i - lag
                If j >= 0 AndAlso j < nb.Length Then
                    sum += na(i) * nb(j)
                    count += 1
                End If
            Next
            If count > 0 Then
                Dim sc As Double = sum / count
                If sc > bestScore Then
                    bestScore = sc
                    bestLag = lag
                End If
            End If
        Next

        Return (bestLag, If(bestScore < 0, 0.0, bestScore))
    End Function

    ' Generate a transition proposal from two snippets (non-destructive). Writes a proposal file next to diagnostics.
    Private Shared Sub GenerateTransitionProposal(track As CDAudioManager.CDTrack, nextTrack As CDAudioManager.CDTrack, snippet1 As SnippetInfo, snippet2 As SnippetInfo)
        Try
            If snippet1 Is Nothing Then Return
            ' If snippet2 available, perform simple cross-correlation on mono RMS frames
            Dim proposalText As New StringBuilder()
            proposalText.AppendLine($"Transition proposal for tracks {track.TrackNumber}{If(nextTrack IsNot Nothing, "->" & nextTrack.TrackNumber, "")}")
            proposalText.AppendLine($"Snippet1={snippet1.FilePath}")
            If snippet2 IsNot Nothing Then proposalText.AppendLine($"Snippet2={snippet2.FilePath}")

            ' Compute RMS slices for each snippet (50 ms default)
            Dim sliceMs As Integer = 50
            Try
                Dim data1 = File.ReadAllBytes(snippet1.FilePath)
                ' WAV header skip: assume 44 bytes
                Dim body1 = New Byte(Math.Max(0, data1.Length - 45)) {}
                Array.Copy(data1, 44, body1, 0, Math.Max(0, data1.Length - 44))
                Dim rms1 = ComputeRMSlices(body1, 0, body1.Length, sliceMs)

                Dim bestLag As Integer = 0
                Dim bestScore As Double = 0
                If snippet2 IsNot Nothing AndAlso File.Exists(snippet2.FilePath) Then
                    Dim data2 = File.ReadAllBytes(snippet2.FilePath)
                    Dim body2 = New Byte(Math.Max(0, data2.Length - 45)) {}
                    Array.Copy(data2, 44, body2, 0, Math.Max(0, data2.Length - 44))
                    Dim rms2 = ComputeRMSlices(body2, 0, body2.Length, sliceMs)
                    Dim res = CrossCorrelateSlices(rms1, rms2)
                    bestLag = res.bestLag
                    bestScore = res.score
                    proposalText.AppendLine($"Cross-correlation score={bestScore:F3}, lag(slices)={bestLag}")
                    ' Convert lag to frames: lag * sliceMs * 44.1 frames per ms -> frames per slice = sliceMs*75/1000? We approximate: 75 frames per second -> frames per ms = 0.075
                    Dim framesPerSlice As Double = sliceMs * 0.075
                    Dim suggestedFrame As Integer = CInt((snippet1.StartFrame + snippet1.EndFrame) / 2 + bestLag * framesPerSlice)
                    proposalText.AppendLine($"Suggested cut frame = {suggestedFrame} (derived from cross-correlation)")
                Else
                    Dim centerFrame As Integer = (snippet1.StartFrame + snippet1.EndFrame) \ 2
                    proposalText.AppendLine($"Suggested cut frame = {centerFrame} (center of snippet1)")
                End If
            Catch exData As Exception
                proposalText.AppendLine($"Correlation failed: {exData.Message}")
            End Try

            Dim dir As String = GetDiagnosticsDirectory()
            If String.IsNullOrEmpty(dir) Then dir = Path.GetTempPath()
            Dim outFile = Path.Combine(dir, $"AudioPlay_TransitionProposal_Track{track.TrackNumber}_{DateTime.Now:yyyyMMddHHmmss}.txt")
            File.WriteAllText(outFile, proposalText.ToString(), Encoding.UTF8)
            DiagnosticWrite($"Generated transition proposal: {outFile}")
            Try
                RaiseEvent TransitionProposalGenerated(outFile)
            Catch
            End Try
        Catch ex As Exception
            DiagnosticWrite($"GenerateTransitionProposal error: {ex.Message}")
        End Try
    End Sub

    ' Inspect a WAV snippet: log file info, duration, and a few RMS slices for diagnostics
    Private Shared Sub InspectSnippetFile(filePath As String)
        Try
            If String.IsNullOrEmpty(filePath) OrElse Not File.Exists(filePath) Then
                DiagnosticWrite($"InspectSnippetFile: file not found: {filePath}")
                Return
            End If

            Dim fi As New FileInfo(filePath)
            DiagnosticWrite($"InspectSnippetFile: {filePath} size={fi.Length} bytes lastWrite={fi.LastWriteTimeUtc}")

            Using r As New WaveFileReader(filePath)
                Dim totalSeconds = r.TotalTime.TotalSeconds
                DiagnosticWrite($"InspectSnippetFile: duration={totalSeconds:F3}s waveFormat={r.WaveFormat}")

                ' Read first 2 seconds and compute RMS slices (50ms)
                Dim msToRead As Integer = CInt(Math.Min(2000, totalSeconds * 1000))
                Dim bytesToRead As Integer = CInt((msToRead / 1000.0) * r.WaveFormat.SampleRate * r.WaveFormat.BlockAlign)
                Dim buffer(bytesToRead - 1) As Byte
                Dim read = r.Read(buffer, 0, buffer.Length)
                Dim slices = ComputeRMSlices(buffer, 0, read, 50)
                DiagnosticWrite($"InspectSnippetFile: first {msToRead}ms -> {slices.Count} slices, sample RMS first3={String.Join(",", slices.Take(3).Select(Function(v) v.ToString("F6"))) }")
            End Using
        Catch ex As Exception
            DiagnosticWrite($"InspectSnippetFile error: {ex.Message}")
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

    ' Helper used by FormCompresser to run the same secondary pass logic on analyses produced by the form.
    Public Shared Sub PerformSecondaryPassForForm(tracks As List(Of CDAudioManager.CDTrack), selectedIndices As List(Of Integer), ByRef analyses As List(Of TrackAnalysis))
        If analyses Is Nothing OrElse tracks Is Nothing OrElse selectedIndices Is Nothing Then Return

        Try
            DiagnosticWrite($"PerformSecondaryPassForForm: starting (tracks={tracks.Count}, analyses={analyses.Count})")

            ' Sauvegarder valeurs originales
            Dim origSilenceThreshold = SilenceThreshold
            Dim origMinSustained = MinSustainedSilenceSeconds
            Dim origBefore = TransitionWindowBeforeSeconds
            Dim origAfter = TransitionWindowAfterSeconds

            Try
                SyncLock GetType(CDAudioAnalyzer)
                    SilenceThreshold = Math.Min(0.1, origSilenceThreshold * 5.0)
                    MinSustainedSilenceSeconds = 0.1
                    TransitionWindowBeforeSeconds = origBefore + 20.0
                    TransitionWindowAfterSeconds = origAfter + 20.0
                End SyncLock

                For i As Integer = 0 To analyses.Count - 1
                    Dim res = analyses(i)
                    If res.WasAdjusted Then Continue For

                    Dim trackIndex = selectedIndices(i)
                    If trackIndex < 0 OrElse trackIndex >= tracks.Count Then Continue For

                    Dim track = tracks(trackIndex)
                    Dim nextTrack As CDAudioManager.CDTrack = Nothing
                    If trackIndex + 1 < tracks.Count Then nextTrack = tracks(trackIndex + 1)

                    DiagnosticWrite($"PerformSecondaryPassForForm: re-analyzing Track {track.TrackNumber}")
                    Dim relaxedAnalysis = AnalyzeTrack(track, nextTrack, Nothing)
                    ' Placeholder: keep secondary-pass output as current result for future auto-apply integration.
                    analyses(i) = relaxedAnalysis

                    If relaxedAnalysis.WasAdjusted Then
                        DiagnosticWrite($"PerformSecondaryPassForForm: Track {track.TrackNumber} adjusted StartTrim={relaxedAnalysis.TrimmedStartFrames} EndTrim={relaxedAnalysis.TrimmedEndFrames}")
                    Else
                        DiagnosticWrite($"PerformSecondaryPassForForm: Track {track.TrackNumber} no adjustment found; saving snippet(s) and generating proposal if enabled")
                        If EnableSnippetCapture Or ForceSaveSnippetsForAllTracks Then
                            Try
                                ' Log track and neighbor frames before saving snippets
                                Try
                                    DiagnosticWrite($"PerformSecondaryPassForForm: about to save snippets for Track {track.TrackNumber} start={track.StartFrame} end={track.EndFrame}")
                                    If nextTrack IsNot Nothing Then DiagnosticWrite($"PerformSecondaryPassForForm: nextTrack {nextTrack.TrackNumber} start={nextTrack.StartFrame} end={nextTrack.EndFrame}")
                                Catch
                                End Try

                                ' Save snippet for current track
                                Dim s1 = SaveTransitionSnippetFile(track, nextTrack, 10, 10)
                                ' Also save snippet for next track (to inspect beginning) if available
                                Dim s2 As SnippetInfo = Nothing
                                If nextTrack IsNot Nothing Then
                                    Try
                                        ' Save snippet centered on the START of the next track (beginning)
                                        s2 = SaveTransitionSnippetFile(nextTrack, Nothing, 0, 20, True)
                                    Catch
                                    End Try
                                End If

                                ' Generate a proposal using both snippets when available
                                Try
                                    GenerateTransitionProposal(track, nextTrack, s1, s2)
                                    ' Inspect saved snippet files to log duration and RMS slices for diagnostics
                                    Try
                                        If s1 IsNot Nothing AndAlso Not String.IsNullOrEmpty(s1.FilePath) Then InspectSnippetFile(s1.FilePath)
                                    Catch
                                    End Try
                                    Try
                                        If s2 IsNot Nothing AndAlso Not String.IsNullOrEmpty(s2.FilePath) Then InspectSnippetFile(s2.FilePath)
                                    Catch
                                    End Try
                                Catch exProp As Exception
                                    DiagnosticWrite($"PerformSecondaryPassForForm: proposal generation failed: {exProp.Message}")
                                End Try
                            Catch exSn As Exception
                                DiagnosticWrite($"PerformSecondaryPassForForm: failed to save snippet for Track {track.TrackNumber}: {exSn.Message}")
                            End Try
                        End If
                    End If
                Next

            Finally
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

            DiagnosticWrite("PerformSecondaryPassForForm: finished")
        Catch ex As Exception
            DiagnosticWrite($"PerformSecondaryPassForForm failed: {ex.Message}")
        End Try
    End Sub

    ' Helper to perform robust reads with retries when reading large chunks from CDReader
    Private Shared Function ReadWithRetries(reader As CDAudioManager.CDReader, buffer() As Byte, offset As Integer, count As Integer, Optional maxAttempts As Integer = 5, Optional delayMs As Integer = 200) As Integer
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
            Dim partialAttempts As Integer = 0

            While attempt < maxAttempts AndAlso blockRead < blockBytes
                Try
                    Dim readNow As Integer = reader.Read(buffer, offset + totalRead + blockRead, blockBytes - blockRead)
                    If readNow > 0 Then
                        blockRead += readNow
                        DiagnosticWrite($"ReadWithRetries: reader read {readNow} bytes (block progress {blockRead}/{blockBytes})")
                        If blockRead >= blockBytes Then Exit While
                    Else
                        attempt += 1
                        partialAttempts += 1
                        DiagnosticWrite($"ReadWithRetries: no progress on attempt {attempt}/{maxAttempts} for block ({blockRead}/{blockBytes})")
                        ' If intermittent, try a short seek to re-sync the drive before retrying
                        Try
                            Dim currentPos = reader.Position
                            reader.Seek(Math.Max(0, currentPos - sectorSize), SeekOrigin.Begin)
                        Catch
                        End Try
                        If attempt < maxAttempts Then Thread.Sleep(backoff)
                        backoff = Math.Min(4000, backoff * 2)
                    End If
                Catch ex As Exception
                    attempt += 1
                    partialAttempts += 1
                    DiagnosticWrite($"ReadWithRetries: exception on attempt {attempt}/{maxAttempts}: {ex.Message}")
                    ' Try to reposition slightly before retrying
                    Try
                        Dim currentPos = reader.Position
                        reader.Seek(Math.Max(0, currentPos - sectorSize * 2), SeekOrigin.Begin)
                    Catch
                    End Try
                    If attempt < maxAttempts Then Thread.Sleep(backoff)
                    backoff = Math.Min(4000, backoff * 2)
                End Try
            End While

            If blockRead <= 0 Then
                ' Failed to read this block after retries -> give up
                DiagnosticWrite($"ReadWithRetries: failed to read block of {blockBytes} bytes after {maxAttempts} attempts. TotalRead={totalRead}")
                Exit While
            End If

            ' If we had partial progress but not full block, allow caller to handle partial writes
            If blockRead < blockBytes Then
                DiagnosticWrite($"ReadWithRetries: partial block read ({blockRead}/{blockBytes}) after {partialAttempts} partial attempts; returning partial data")
            End If

            totalRead += blockRead
        End While

        Return totalRead
    End Function

    ' Create a small marker file in %TEMP% to prove SaveTransitionSnippet was invoked (or attempted)
    Private Shared Sub WriteSnippetMarker(trackNumber As Integer, startFrame As Integer, endFrame As Integer, source As String)
        Try
            Dim dir As String = GetDiagnosticsDirectory()
            If String.IsNullOrEmpty(dir) Then dir = Path.GetTempPath()
            Try
                Directory.CreateDirectory(dir)
            Catch
            End Try
            Dim fname As String = Path.Combine(dir, $"AudioPlay_SnippetCalled_{source}_Track{trackNumber}_{DateTime.Now:yyyyMMddHHmmss}.txt")
            Dim content As String = $"Track={trackNumber}, start={startFrame}, end={endFrame}, source={source}, time={DateTime.Now:O}"
            File.WriteAllText(fname, content)
            DiagnosticWrite($"Snippet marker created: {fname}")
        Catch ex As Exception
            Try
                DiagnosticWrite($"Failed to create snippet marker: {ex.Message}")
            Catch
            End Try
        End Try
    End Sub

    Public Shared Function GetDiagnosticsDirectory() As String
        Try
            If Not String.IsNullOrEmpty(alternateDiagnosticsLogPath) Then
                Dim d As String = Path.GetDirectoryName(alternateDiagnosticsLogPath)
                If Not String.IsNullOrEmpty(d) Then Return d
            End If
            Dim main As String = DiagnosticsLogPath
            If Not String.IsNullOrEmpty(main) Then
                Dim dm As String = Path.GetDirectoryName(main)
                If Not String.IsNullOrEmpty(dm) Then Return dm
            End If
        Catch
        End Try
        Return String.Empty
    End Function

    ''' <summary>
    ''' Writes a diagnostics parameters snapshot next to the diagnostics log.
    ''' </summary>
    Public Shared Sub WriteDiagnosticParams()
        Try
            Dim dir As String = GetDiagnosticsDirectory()
            If String.IsNullOrEmpty(dir) Then dir = Path.GetTempPath()
            Try
                Directory.CreateDirectory(dir)
            Catch
            End Try

            Dim sb As New StringBuilder()
            sb.AppendLine($"Diagnostic params snapshot: {DateTime.Now:O}")
            Try
                Dim asm = System.Reflection.Assembly.GetEntryAssembly()
                If asm Is Nothing Then asm = System.Reflection.Assembly.GetExecutingAssembly()
                If asm IsNot Nothing Then sb.AppendLine($"Assembly: {asm.GetName().Name} v{asm.GetName().Version}")
            Catch
            End Try
            sb.AppendLine($"SilenceThreshold={SilenceThreshold}")
            sb.AppendLine($"MinSustainedSilenceSeconds={MinSustainedSilenceSeconds}")
            sb.AppendLine($"MinTransitionSilenceSeconds={MinTransitionSilenceSeconds}")
            sb.AppendLine($"TransitionWindowBeforeSeconds={TransitionWindowBeforeSeconds}")
            sb.AppendLine($"TransitionWindowAfterSeconds={TransitionWindowAfterSeconds}")
            sb.AppendLine($"TransitionProximityWindowSeconds={TransitionProximityWindowSeconds}")
            sb.AppendLine($"SafetyMarginFrames={SafetyMarginFrames}")
            sb.AppendLine($"MaxTrimSeconds={MaxTrimSeconds}")
            sb.AppendLine($"MaxStartTrimSeconds={MaxStartTrimSeconds}")
            sb.AppendLine($"UsePairwiseAnalysis={UsePairwiseAnalysis}")
            sb.AppendLine($"ForceSecondaryPass={ForceSecondaryPass}")
            sb.AppendLine($"ForceSaveSnippetsForAllTracks={ForceSaveSnippetsForAllTracks}")
            sb.AppendLine($"EnableAggressiveSecondaryPass={EnableAggressiveSecondaryPass}")
            sb.AppendLine($"EnableSnippetCapture={EnableSnippetCapture}")
            sb.AppendLine($"UseDetailedDiagnostics={UseDetailedDiagnostics}")

            Dim outFile As String = Path.Combine(dir, "AudioPlay_DiagnosticParams.txt")
            File.WriteAllText(outFile, sb.ToString(), Encoding.UTF8)
            DiagnosticWrite($"Wrote diagnostic params to {outFile}")
        Catch ex As Exception
            Try
                DiagnosticWrite($"WriteDiagnosticParams failed: {ex.Message}")
            Catch
            End Try
        End Try
    End Sub

    ' Cleanup old snippet WAV files older than retentionDays in diagnostics directory and subfolders
    Private Shared Sub CleanupOldSnippets(retentionDays As Integer)
        Try
            Dim dir As String = GetDiagnosticsDirectory()
            If String.IsNullOrEmpty(dir) Then dir = Path.GetTempPath()
            If Not Directory.Exists(dir) Then Return

            Dim cutoff As DateTime = DateTime.UtcNow.AddDays(-retentionDays)
            Dim wavs = Directory.GetFiles(dir, "*.wav", SearchOption.AllDirectories)
            For Each f In wavs
                Try
                    Dim info = New FileInfo(f)
                    If info.LastWriteTimeUtc < cutoff Then
                        Try
                            info.Delete()
                            DiagnosticWrite($"CleanupOldSnippets: deleted old snippet {f}")
                        Catch exDel As Exception
                            DiagnosticWrite($"CleanupOldSnippets: failed delete {f}: {exDel.Message}")
                        End Try
                    End If
                Catch
                End Try
            Next
        Catch ex As Exception
            DiagnosticWrite($"CleanupOldSnippets error: {ex.Message}")
        End Try
    End Sub

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
            Dim info As SnippetInfo = SaveTransitionSnippetFile(currentTrack, nextTrack, secondsBefore, secondsAfter)
            If info Is Nothing Then
                DiagnosticWrite($"SaveTransitionSnippet: no snippet generated for track {currentTrack.TrackNumber}")
            End If
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
        Public Property TransitionAnalyzed As Boolean = False  ' Indique si une analyse de transition a été faite
        Public Property PreferAdjustNextStart As Boolean = False ' Si true, préférer ajuster le début de la piste suivante
        ' Confiance (0.0-1.0) indiquant la qualité de l'ajustement détecté
        Public Property Confidence As Double = 1.0
        ' Indique si l'ajustement est jugé suffisamment sûr pour application automatique
        Public Property AutoApplyApproved As Boolean = False

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
                    DiagnosticWrite($"Piste {track.TrackNumber} transition silence: {transitionResult.SilenceStart}-{transitionResult.SilenceEnd} dur={transitionResult.SilenceDuration:F2}s center={transitionResult.SilenceCenter}")

                    ' Déterminer si le silence contient une portion APRÈS le TOC -> préférer ajuster la piste suivante
                    ' (utiliser la fin du silence plutôt que son centre pour mieux capter les silences qui débordent)
                    If transitionResult.SilenceEnd > track.EndFrame Then
                        result.PreferAdjustNextStart = True
                    End If

                    ' Si on préfère ajuster le début de la piste suivante, ne pas couper la fin de la piste courante
                    If result.PreferAdjustNextStart Then
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] INFO: Silence avec portion APRES TOC -> pas de coupe de la fin de la piste actuelle, preference deplacee au debut de la suivante")
                        DiagnosticWrite($"Piste {track.TrackNumber} prefer adjust next start: silence extends after TOC -> no end cut")
                    Else
                        ' Garde-fou conservateur : éviter de couper si le silence se termine trop en amont du TOC
                        Dim conservativeEndLimitFrame As Integer = track.EndFrame - CInt(ConservativeTrimPreTOCSeconds * 75)
                        If transitionResult.SilenceEnd < conservativeEndLimitFrame Then
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ℹ️ Silence trop en amont du TOC (fin à {(track.EndFrame - transitionResult.SilenceEnd) / 75.0:F2}s avant) -> fin TOC conservée (paramètre ConservativeTrimPreTOCSeconds={ConservativeTrimPreTOCSeconds:F2}s)")
                        Else
                            ' Ajuster la fin de la piste actuelle au début du silence + marge de sécurité (stratégie conservatrice)
                            Dim cutFrame As Integer = Math.Min(track.EndFrame, transitionResult.SilenceStart + SafetyMarginFrames)
                            result.AdjustedEndFrame = cutFrame
                            result.TrimmedEndFrames = track.EndFrame - cutFrame
                            result.WasAdjusted = True
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Fin ajustee (conservatrice): -{result.TrimmedEndFrames / 75.0:F2}s ({result.TrimmedEndFrames} frames)")
                            System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Coupe au frame {cutFrame} (silence {transitionResult.SilenceStart}-{transitionResult.SilenceEnd}, marge {SafetyMarginFrames} frames)")
                            DiagnosticWrite($"Piste {track.TrackNumber} end cut: cutFrame={cutFrame} trimmed={result.TrimmedEndFrames / 75.0:F2}s reason=transition_silence start={transitionResult.SilenceStart} end={transitionResult.SilenceEnd} safetyMargin={SafetyMarginFrames}")
                        End If
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

                ' Select best silence if any
                If silencesDetectes.Count > 0 Then
                    Dim minSilenceFrames As Integer = CInt(MinTransitionSilenceSeconds * 75)
                    Dim proximityFrames As Integer = CInt(TransitionProximityWindowSeconds * 75)

                    Dim silencesLongs = silencesDetectes.Where(Function(s) (s.endPos - s.start) >= minSilenceFrames).ToList()
                    Dim silencesProches = silencesLongs.Where(Function(s) Math.Abs(s.distanceFromTOC) <= proximityFrames).ToList()

                    If silencesProches.Count = 0 Then
                        ' fallback on shorter silences
                        Dim fallbackMinFrames As Integer = CInt(FallbackShortSilenceSeconds * 75)
                        Dim silencesFallback = silencesDetectes.Where(Function(s) (s.endPos - s.start) >= fallbackMinFrames AndAlso Math.Abs(s.distanceFromTOC) <= proximityFrames).ToList()
                        If silencesFallback.Count = 0 Then
                            Return result
                        End If
                        ' prefer those containing portion after TOC
                        Dim fallbackWithAfter = silencesFallback.Where(Function(s) s.endPos >= tocBoundary).ToList()
                        Dim chosen = If(fallbackWithAfter.Count > 0, fallbackWithAfter.OrderBy(Function(s) Math.Abs(s.distanceFromTOC)).First(), silencesFallback.OrderBy(Function(s) Math.Abs(s.distanceFromTOC)).First())
                        Dim cutFrameFallback As Integer = Math.Min(chosen.endPos, chosen.start + SafetyMarginFrames)
                        result.SilenceFound = True
                        result.SilenceStart = chosen.start
                        result.SilenceEnd = chosen.endPos
                        result.SilenceCenter = cutFrameFallback
                        result.SilenceDuration = (chosen.endPos - chosen.start) / 75.0
                        Return result
                    End If

                    ' choose closest silence, prefer those that include after-TOC portion
                    Dim silencesWithAfter = silencesProches.Where(Function(s) s.endPos >= tocBoundary).ToList()
                    Dim best = If(silencesWithAfter.Count > 0, silencesWithAfter.OrderBy(Function(s) Math.Abs(s.distanceFromTOC)).First(), silencesProches.OrderBy(Function(s) Math.Abs(s.distanceFromTOC)).First())
                    Dim cutFrame2 As Integer = Math.Min(best.endPos, best.start + SafetyMarginFrames)

                    result.SilenceFound = True
                    result.SilenceStart = best.start
                    result.SilenceEnd = best.endPos
                    result.SilenceCenter = cutFrame2
                    result.SilenceDuration = (best.endPos - best.start) / 75.0
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

    ' Passe de réconciliation générique pour éviter les chevauchements résiduels entre pistes adjacentes
    Private Shared Sub ReconcileTrackBoundaries(results As List(Of TrackAnalysis))
        If results Is Nothing OrElse results.Count < 2 Then Return

        Try
            For i As Integer = 0 To results.Count - 2
                Dim cur = results(i)
                Dim nxt = results(i + 1)

                If cur Is Nothing OrElse nxt Is Nothing Then Continue For

                If cur.AdjustedStartFrame <= 0 Then cur.AdjustedStartFrame = cur.OriginalStartFrame
                If cur.AdjustedEndFrame <= 0 Then cur.AdjustedEndFrame = cur.OriginalEndFrame
                If nxt.AdjustedStartFrame <= 0 Then nxt.AdjustedStartFrame = nxt.OriginalStartFrame
                If nxt.AdjustedEndFrame <= 0 Then nxt.AdjustedEndFrame = nxt.OriginalEndFrame

                If cur.AdjustedEndFrame >= nxt.AdjustedStartFrame Then
                    Dim correctedStart As Integer = cur.AdjustedEndFrame + 1
                    If correctedStart < nxt.AdjustedEndFrame Then
                        nxt.AdjustedStartFrame = correctedStart
                        nxt.TrimmedStartFrames = nxt.AdjustedStartFrame - nxt.OriginalStartFrame
                        nxt.WasAdjusted = True
                        DiagnosticWrite($"Reconciliation pass: adjusted start of track {nxt.TrackNumber} to {correctedStart} to avoid overlap with track {cur.TrackNumber}")
                    Else
                        Dim correctedEnd As Integer = Math.Max(cur.AdjustedStartFrame, nxt.AdjustedEndFrame - 1)
                        cur.AdjustedEndFrame = correctedEnd
                        cur.TrimmedEndFrames = cur.OriginalEndFrame - cur.AdjustedEndFrame
                        cur.WasAdjusted = True
                        DiagnosticWrite($"Reconciliation pass: adjusted end of track {cur.TrackNumber} to {correctedEnd} (fallback) to avoid overlap with track {nxt.TrackNumber}")
                    End If
                End If
            Next
        Catch ex As Exception
            DiagnosticWrite($"ReconcileTrackBoundaries failed: {ex.Message}")
        End Try
    End Sub

    ' Décision d'auto-application des ajustements selon les paramètres globaux + seuils de confiance/taille
    Private Shared Sub EvaluateAutoApplyDecision(results As List(Of TrackAnalysis))
        If results Is Nothing Then Return

        Dim autoApplyEnabled As Boolean = False
        Try
            autoApplyEnabled = ParametresGlobaux.AutoApplyAnalysis
        Catch
            autoApplyEnabled = False
        End Try

        If Not autoApplyEnabled Then
            For Each r In results
                If r IsNot Nothing Then r.AutoApplyApproved = False
            Next
            Return
        End If

        Dim confidenceThreshold As Double = 0.8
        Dim maxAdjustmentSeconds As Double = MaxTrimSeconds

        Try
            confidenceThreshold = ParametresGlobaux.AnalysisAutoApplyConfidenceThreshold
        Catch
        End Try

        Try
            maxAdjustmentSeconds = ParametresGlobaux.AnalysisAutoApplyMaxSeconds
        Catch
        End Try

        confidenceThreshold = Math.Max(0.0, Math.Min(1.0, confidenceThreshold))
        maxAdjustmentSeconds = Math.Max(0.0, maxAdjustmentSeconds)

        For Each r In results
            If r Is Nothing Then Continue For
            Dim adjustmentFrames As Integer = Math.Abs(r.AdjustedStartFrame - r.OriginalStartFrame) + Math.Abs(r.OriginalEndFrame - r.AdjustedEndFrame)
            Dim adjustmentSeconds As Double = adjustmentFrames / 75.0

            r.AutoApplyApproved = r.WasAdjusted AndAlso r.Confidence >= confidenceThreshold AndAlso adjustmentSeconds <= maxAdjustmentSeconds

            If r.AutoApplyApproved Then
                DiagnosticWrite($"AutoApply approved for track {r.TrackNumber}: confidence={r.Confidence:F2}, adjustment={adjustmentSeconds:F2}s")
            Else
                DiagnosticWrite($"AutoApply skipped for track {r.TrackNumber}: confidence={r.Confidence:F2}/{confidenceThreshold:F2}, adjustment={adjustmentSeconds:F2}s/{maxAdjustmentSeconds:F2}s")
            End If
        Next
    End Sub

    '''
    ''' <summary>
    ''' Analyse toutes les pistes sélectionnées et retourne les résultats
    ''' </summary>
    Public Shared Function AnalyzeSelectedTracks(tracks As List(Of CDAudioManager.CDTrack), selectedIndices As List(Of Integer)) As List(Of TrackAnalysis)
        Dim results As New List(Of TrackAnalysis)

        DiagnosticWrite($"AnalyzeSelectedTracks: start UsePairwiseAnalysis={UsePairwiseAnalysis} selectedIndicesCount={If(selectedIndices IsNot Nothing, selectedIndices.Count, -1)} tracksCount={If(tracks IsNot Nothing, tracks.Count, -1)}")

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
                            ' Placeholder: keep secondary-pass output as current result for future auto-apply integration.
                            results(idx) = relaxedAnalysis

                            If relaxedAnalysis.WasAdjusted Then
                                DiagnosticWrite($"Secondary pass: Track {track.TrackNumber} produced adjustment: StartTrim={relaxedAnalysis.TrimmedStartFrames} EndTrim={relaxedAnalysis.TrimmedEndFrames} (stored)")
                            Else
                                DiagnosticWrite($"Secondary pass: Track {track.TrackNumber} no adjustment found with relaxed params")
                                ' Sauvegarder un extrait autour du TOC pour diagnostic (±10s)
                                Try
                                    Dim nextTrackLocal As CDAudioManager.CDTrack = Nothing
                                    If trackIndex + 1 < tracks.Count Then nextTrackLocal = tracks(trackIndex + 1)
                                    WriteSnippetMarker(track.TrackNumber, Math.Max(track.StartFrame, track.EndFrame - 1), Math.Max(track.StartFrame, track.EndFrame), "secondary_pre")
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
                                Dim proposedStart As Integer = cur.SilenceEndFrame + SafetyMarginFrames
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

            ReconcileTrackBoundaries(results)
            EvaluateAutoApplyDecision(results)
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
                Dim proposedStart As Integer = cur.SilenceEndFrame + SafetyMarginFrames

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
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Reconciliation preferentielle: deplacement du debut de la piste {nxt.TrackNumber} a {proposedStart} (silence APRES TOC de la piste {cur.TrackNumber})")
                    nxt.AdjustedStartFrame = proposedStart
                    nxt.TrimmedStartFrames = nxt.AdjustedStartFrame - nxt.OriginalStartFrame
                    nxt.WasAdjusted = True
                End If

            ElseIf cur.AdjustedEndFrame >= nxt.AdjustedStartFrame Then
                Dim correctedStart As Integer = cur.AdjustedEndFrame + 1

                ' Vérifier l'inversion possible (start >= end)
                If correctedStart >= nxt.AdjustedEndFrame Then
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] WARNING: Reconciliation impossible sans inversion entre piste {cur.TrackNumber} et {nxt.TrackNumber} - conservation des positions TOC pour la suivante")
                    ' Reprendre la position TOC pour la piste suivante pour éviter d'écraser la durée
                    nxt.AdjustedStartFrame = nxt.OriginalStartFrame
                    nxt.TrimmedStartFrames = 0
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] Reconciliation: deplacement du debut de la piste {nxt.TrackNumber} a {correctedStart} pour eviter chevauchement avec piste {cur.TrackNumber}")
                    nxt.AdjustedStartFrame = correctedStart
                    nxt.TrimmedStartFrames = nxt.AdjustedStartFrame - nxt.OriginalStartFrame
                    nxt.WasAdjusted = True
                End If
            End If

            ' Mettre à jour le message d'analyse pour la piste suivante
            If nxt.WasAdjusted Then
                nxt.AnalysisMessage = $"Piste {nxt.TrackNumber}: Début +{nxt.TrimmedStartFrames / 75.0:F2}s, Fin -{nxt.TrimmedEndFrames / 75.0:F2}s"
            Else
                nxt.AnalysisMessage = $"Piste {nxt.TrackNumber}: OK (pas d'ajustement)"
            End If
        Next

        ' --- Secondary relaxed pass for pairwise mode ---
        If (EnableAggressiveSecondaryPass Or ForceSecondaryPass) Then
            Try
                DiagnosticWrite("Pairwise: starting relaxed secondary re-analysis for non-adjusted tracks")

                ' Sauvegarder valeurs originales
                Dim origSilenceThreshold = SilenceThreshold
                Dim origMinSustained = MinSustainedSilenceSeconds
                Dim origBefore = TransitionWindowBeforeSeconds
                Dim origAfter = TransitionWindowAfterSeconds

                Try
                    SyncLock GetType(CDAudioAnalyzer)
                        SilenceThreshold = Math.Min(0.1, origSilenceThreshold * 5.0)
                        MinSustainedSilenceSeconds = 0.1
                        TransitionWindowBeforeSeconds = origBefore + 20.0
                        TransitionWindowAfterSeconds = origAfter + 20.0
                    End SyncLock

                    For i As Integer = 0 To results.Count - 1
                        Dim res = results(i)
                        If res.WasAdjusted Then Continue For

                        Dim trackIndex As Integer = -1
                        If i >= 0 AndAlso i < selectedIndices.Count Then trackIndex = selectedIndices(i)
                        If trackIndex < 0 OrElse trackIndex >= tracks.Count Then Continue For

                        Dim track As CDAudioManager.CDTrack = tracks(trackIndex)
                        Dim nextTrack As CDAudioManager.CDTrack = Nothing
                        If trackIndex + 1 < tracks.Count Then nextTrack = tracks(trackIndex + 1)

                        DiagnosticWrite($"Pairwise secondary: re-analyzing Track {track.TrackNumber} with relaxed params (threshold={SilenceThreshold:F6}, minSustained={MinSustainedSilenceSeconds:F2}, windowBefore={TransitionWindowBeforeSeconds:F1}, windowAfter={TransitionWindowAfterSeconds:F1})")
                        Dim relaxedAnalysis = AnalyzeTrack(track, nextTrack, Nothing)
                        ' Placeholder: keep secondary-pass output as current result for future auto-apply integration.
                        results(i) = relaxedAnalysis

                        If relaxedAnalysis.WasAdjusted Then
                            DiagnosticWrite($"Pairwise secondary: Track {track.TrackNumber} produced adjustment: StartTrim={relaxedAnalysis.TrimmedStartFrames} EndTrim={relaxedAnalysis.TrimmedEndFrames} (stored)")
                        Else
                            DiagnosticWrite($"Pairwise secondary: Track {track.TrackNumber} no adjustment found with relaxed params")
                            If EnableSnippetCapture Or ForceSaveSnippetsForAllTracks Then
                                Try
                                    WriteSnippetMarker(track.TrackNumber, track.StartFrame, track.EndFrame, "pairwise_secondary")
                                    SaveTransitionSnippet(track, nextTrack, 10, 10)
                                Catch exSnippet As Exception
                                    DiagnosticWrite($"Pairwise secondary: failed to save snippet for Track {track.TrackNumber}: {exSnippet.Message}")
                                End Try
                            End If
                        End If
                    Next

                    ' Re-run reconciliation after secondary adjustments
                    For i As Integer = 0 To results.Count - 2
                        Dim cur = results(i)
                        Dim nxt = results(i + 1)

                        If nxt.AdjustedStartFrame <= 0 Then
                            nxt.AdjustedStartFrame = nxt.OriginalStartFrame
                        End If

                        If cur.TransitionAnalyzed AndAlso cur.PreferAdjustNextStart AndAlso cur.SilenceEndFrame > cur.OriginalEndFrame Then
                            Dim proposedStart As Integer = cur.SilenceEndFrame + SafetyMarginFrames
                            Dim maxStartTrimFrames As Integer = CInt(MaxStartTrimSeconds * 75)
                            Dim maxAllowedStart As Integer = nxt.OriginalStartFrame + maxStartTrimFrames

                            If proposedStart > maxAllowedStart Then
                                DiagnosticWrite($"Réconciliation (pairwise secondary): proposedStart {proposedStart} exceeds max allowed start {maxAllowedStart} -> capping to {maxAllowedStart}")
                                proposedStart = maxAllowedStart
                            End If

                            If proposedStart >= nxt.AdjustedEndFrame Then
                                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] WARNING: Reconciliation impossible sans inversion (pairwise secondary) entre piste {cur.TrackNumber} et {nxt.TrackNumber}")
                            ElseIf proposedStart > nxt.AdjustedStartFrame Then
                                nxt.AdjustedStartFrame = proposedStart
                                nxt.TrimmedStartFrames = nxt.AdjustedStartFrame - nxt.OriginalStartFrame
                                nxt.WasAdjusted = True
                            End If

                        ElseIf cur.AdjustedEndFrame >= nxt.AdjustedStartFrame Then
                            Dim correctedStart As Integer = cur.AdjustedEndFrame + 1
                            If correctedStart >= nxt.AdjustedEndFrame Then
                                System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] WARNING: Reconciliation impossible sans inversion (pairwise secondary) entre piste {cur.TrackNumber} et {nxt.TrackNumber}")
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
                    ' Restaurer paramètres originaux
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

                DiagnosticWrite("Pairwise: finished relaxed secondary pass")
            Catch exSecondary As Exception
                DiagnosticWrite($"Pairwise secondary pass failed: {exSecondary.Message}")
            End Try
        End If

        ' After pairwise reconciliation, optionally save diagnostic snippets for any tracks
        ' that remained non-adjusted so the user can inspect the TOC neighborhood.
        If ForceSaveSnippetsForAllTracks Or EnableSnippetCapture Or ForceSecondaryPass Then
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
                        WriteSnippetMarker(track.TrackNumber, track.StartFrame, track.EndFrame, "pairwise_post")
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

        ReconcileTrackBoundaries(results)
        EvaluateAutoApplyDecision(results)
        Return results
    End Function

End Class
