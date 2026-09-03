Imports System.IO
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports System.Runtime.InteropServices
Imports NAudio.Wave

''' <summary>
''' Gestionnaire pour la détection et la lecture de CD audio
''' </summary>
Public Class CDAudioManager

    ' ========================================
    ' API Windows pour accès direct au CD-ROM
    ' ========================================
    Private Const GENERIC_READ As UInteger = &H80000000UI
    Private Const FILE_SHARE_READ As UInteger = &H1UI
    Private Const FILE_SHARE_WRITE As UInteger = &H2UI
    Private Const OPEN_EXISTING As UInteger = 3
    Private Const IOCTL_CDROM_READ_TOC As UInteger = &H24000UI
    Private Const IOCTL_CDROM_READ_Q_CHANNEL As UInteger = &H2402CUI
    Private Const IOCTL_CDROM_RAW_READ As UInteger = &H2403EUI
    Private Shared ReadOnly INVALID_HANDLE_VALUE As New IntPtr(-1)

    ' Constantes pour la lecture CD
    Private Const CD_SECTOR_SIZE As Integer = 2352 ' Taille d'un secteur CD audio brut
    Private Const CD_FRAMES_PER_SECOND As Integer = 75 ' 75 frames par seconde
    ' Cache en mémoire pour la méthode de lecture par lecteur (clé = lettre de lecteur, ex: "D:")
    ' Valeurs: 0 = LBA sector (frame), 1 = original (frame*2048, TrackMode=2), 2 = fallback (frame*2048, TrackMode=1)
    Private Shared readerModeCache As New Dictionary(Of String, Integer)()
    ' Logging verbeux pour la lecture CD (hex-dumps et messages par secteur) - désactivé par défaut
    Private Shared VerboseCDReadLogging As Boolean = False
    ' Allow configuration of logging frequency to reduce log size
    Private Shared ReadOnly CDReadSummaryInterval As Integer = 100

    ' Fichier persistant pour mémoriser la méthode de lecture par lecteur
    Private Shared ReadOnly readerModeCacheFile As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "reader_mode.json")

    ' Charger le cache persisté au démarrage de la classe
    Shared Sub New()
        LoadReaderModeCache()
    End Sub

    ''' <summary>
    ''' Tentative d'extraction d'une piste via un ripper externe freac (freaccmd) si présent.
    ''' Recherche freaccmd.exe dans le dossier de l'application puis dans PATH.
    ''' Retourne True si un fichier WAV valide a été produit.
    ''' </summary>
    Public Shared Function RipTrackWithFreac(track As CDTrack, outputWavPath As String, Optional timeoutMs As Integer = 5 * 60 * 1000, Optional outputFolder As String = Nothing, Optional cancellationCheck As Func(Of Boolean) = Nothing, Optional progressCallback As Action(Of Integer) = Nothing) As Boolean
        Try
            If track Is Nothing Then Return False
            If String.IsNullOrWhiteSpace(outputWavPath) Then Return False
            Dim freacDisabled As Boolean = True
            If freacDisabled Then
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"FREAC_DISABLED: external ripper disabled. Track={track.TrackNumber}")
                Catch
                End Try
                Return False
            End If

            ' Chercher binaire dans le répertoire de l'application
            Dim appDir As String = AppDomain.CurrentDomain.BaseDirectory
            Dim candidates As New List(Of String)()
            ' Chercher les binaires freaccmd*.exe (fre:ac) dans le répertoire de l'application
            Try
                For Each f In Directory.GetFiles(appDir, "freaccmd*.exe")
                    candidates.Add(f)
                Next
                ' intentionally only search for freaccmd
            Catch
            End Try

            ' Rechercher dans PATH avec pattern
            Dim pathEnv = Environment.GetEnvironmentVariable("PATH")
            If Not String.IsNullOrEmpty(pathEnv) Then
                For Each p In pathEnv.Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
                    Try
                        For Each f In Directory.GetFiles(p.Trim(), "freaccmd*.exe")
                            candidates.Add(f)
                        Next
                    Catch
                    End Try
                Next
            End If

            Dim exePath As String = Nothing
            For Each c In candidates
                Try
                    If File.Exists(c) Then
                        exePath = c
                        Exit For
                    End If
                Catch
                End Try
            Next

            ' Écrire un marqueur de détection du ripper dans le dossier d'extraction si fourni, sinon dans le dossier de l'application
            Try
                Dim detectDir As String = If(String.IsNullOrEmpty(outputFolder), appDir, outputFolder)
                Dim detectPath = Path.Combine(detectDir, "freac.detect.txt")
                If String.IsNullOrEmpty(exePath) Then
                    File.WriteAllText(detectPath, "NOT FOUND")
                Else
                    File.WriteAllText(detectPath, exePath)
                End If
            Catch
            End Try

            If String.IsNullOrEmpty(exePath) Then
                ' Pas trouvé
                Return False
            End If

            ' Construire arguments - utilisation conservative : device = lettre de lecteur (ex: "D:") et track number
            ' Note: l'argument exact dépend de la version du binaire; cette implémentation utilise un template commun.
            Dim driveLetter As String = If(String.IsNullOrEmpty(track.Drive), "", track.Drive)
            Dim trackNum = track.TrackNumber
            ' Template d'arguments par défaut : freaccmd compatible fallback (will be overridden for freaccmd)
            Dim args As String = $"-D ""{driveLetter}"" -t {trackNum} -O wav -o ""{outputWavPath}"""

            ' Si l'exécutable détecté est freaccmd, préparer des arguments de rip WAV
            Dim exeName As String = Path.GetFileName(exePath).ToLowerInvariant()
            If exeName.Contains("freaccmd") Then
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"FREACCMD_DETECTED: exe={exePath} - will invoke for ripping")
                Catch
                End Try

                Try
                    Dim outDir = Path.GetDirectoryName(outputWavPath)
                    Dim outFileName = Path.GetFileName(outputWavPath)
                    args = $"--drive=""{driveLetter}"" --track={trackNum} --encoder=sndfile-wave -d ""{outDir}"" -o ""{outFileName}"""
                    ' If caller requested progress updates, do not pass --quiet so freaccmd can emit progress
                    If progressCallback Is Nothing Then
                        args &= " --quiet"
                    End If
                    CDAudioAnalyzer.DiagnosticWrite($"FREACCMD_RUN: exe={exePath} args={args}")
                Catch
                End Try
            End If

            Try
                CDAudioAnalyzer.DiagnosticWrite($"FREAC_RUN: exe={exePath} args={args}")
            Catch
            End Try

            Dim psi As New ProcessStartInfo(exePath, args) With {
                .CreateNoWindow = True,
                .UseShellExecute = False,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True
            }
            ' Définir le répertoire de travail si un dossier de sortie est fourni pour forcer freaccmd à écrire là
            Try
                If Not String.IsNullOrEmpty(outputFolder) Then
                    psi.WorkingDirectory = outputFolder
                Else
                    psi.WorkingDirectory = Path.GetDirectoryName(outputWavPath)
                End If
            Catch
            End Try

            Using p As Process = Process.Start(psi)
                If p Is Nothing Then Return False
                ' Lire la sortie en tâche de fond pour éviter blocage
                Dim stdOut As String = String.Empty
                Dim stdErr As String = String.Empty
                Dim stdOutBuilder As New System.Text.StringBuilder()
                Dim stdErrBuilder As New System.Text.StringBuilder()
                Dim outputSync As New Object()
                ' Accept various progress formats (e.g. "23%", "23 / 100", or standalone numbers)
                Dim progressRegex As New Regex("\b(100|[1-9]?\d)\b", RegexOptions.Compiled)
                Dim lastProgress As Integer = -1
                Dim sw As System.Diagnostics.Stopwatch = System.Diagnostics.Stopwatch.StartNew()

                Dim reportProgressFromLine As Action(Of String) =
                    Sub(line As String)
                        Try
                            If String.IsNullOrWhiteSpace(line) Then Exit Sub
                            Dim m = progressRegex.Match(line)
                            If Not m.Success Then Exit Sub
                            Dim pct As Integer
                            If Not Integer.TryParse(m.Groups(1).Value, pct) Then Exit Sub
                            pct = Math.Max(0, Math.Min(100, pct))
                            If pct > lastProgress Then
                                lastProgress = pct
                                If progressCallback IsNot Nothing Then
                                    Try
                                        ' Trace progress callback from stdout
                                        Try
                                            Dim tracePath = Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                                            System.IO.File.AppendAllText(tracePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Track={trackNum} source=stdout pct={pct}{Environment.NewLine}")
                                        Catch
                                        End Try
                                        progressCallback(pct)
                                    Catch
                                    End Try
                                End If
                                Try
                                    CDAudioAnalyzer.DiagnosticWrite($"FREAC_PROGRESS: Track={trackNum} pct={pct}")
                                Catch
                                End Try
                            End If
                        Catch
                        End Try
                    End Sub

                Try
                    If progressCallback IsNot Nothing Then
                        ' Trace initial 0% callback
                        Try
                            Dim tracePath = Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                            System.IO.File.AppendAllText(tracePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Track={trackNum} source=init pct=0{Environment.NewLine}")
                        Catch
                        End Try
                        progressCallback(0)
                        lastProgress = 0
                    End If
                Catch
                End Try
                Try
                    AddHandler p.OutputDataReceived,
                        Sub(sender, e)
                            Try
                                If e.Data Is Nothing Then Exit Sub
                                SyncLock outputSync
                                    stdOutBuilder.AppendLine(e.Data)
                                End SyncLock
                                reportProgressFromLine(e.Data)
                            Catch
                            End Try
                        End Sub

                    AddHandler p.ErrorDataReceived,
                        Sub(sender, e)
                            Try
                                If e.Data Is Nothing Then Exit Sub
                                SyncLock outputSync
                                    stdErrBuilder.AppendLine(e.Data)
                                End SyncLock
                                reportProgressFromLine(e.Data)
                            Catch
                            End Try
                        End Sub

                    p.BeginOutputReadLine()
                    p.BeginErrorReadLine()

                    ' If caller provided a progressCallback, additionally poll the output WAV file
                    ' size to produce smoother progress updates when fre:ac emits few stdout lines.
                    Dim cts As System.Threading.CancellationTokenSource = Nothing
                    Dim pollTask As Task = Nothing
                    If progressCallback IsNot Nothing Then
                        Try
                            cts = New System.Threading.CancellationTokenSource()
                            Dim token = cts.Token
                            pollTask = Task.Run(Sub()
                                                    Try
                                                        ' Expected bytes per second for CD audio: 44100 Hz * 2 channels * 2 bytes = 176400
                                                        Dim bytesPerSec As Double = 176400.0
                                                        Dim expectedSize As Double = 0.0
                                                        ' Robust expected size calculation: prefer track.Duration, fallback to frame range if available
                                                        Try
                                                            If track IsNot Nothing AndAlso track.Duration.TotalSeconds > 0 Then
                                                                expectedSize = track.Duration.TotalSeconds * bytesPerSec
                                                            ElseIf track IsNot Nothing AndAlso track.EndFrame > track.StartFrame Then
                                                                Dim durSec As Double = (track.EndFrame - track.StartFrame) / CD_FRAMES_PER_SECOND
                                                                If durSec > 0 Then expectedSize = durSec * bytesPerSec
                                                            End If
                                                        Catch
                                                        End Try

                                                        While Not token.IsCancellationRequested AndAlso Not p.HasExited
                                                            Try
                                                                ' Check external cancellation predicate if provided
                                                                If cancellationCheck IsNot Nothing Then
                                                                    Try
                                                                        If cancellationCheck() Then
                                                                            Try
                                                                                Dim tracePath = Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                                                                                System.IO.File.AppendAllText(tracePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Track={trackNum} source=poll CANCEL{Environment.NewLine}")
                                                                            Catch
                                                                            End Try
                                                                            Try
                                                                                CDAudioAnalyzer.DiagnosticWrite($"FREAC_POLL_CANCEL_REQUEST: Track={trackNum}")
                                                                            Catch
                                                                            End Try
                                                                            Try
                                                                                If Not p.HasExited Then p.Kill()
                                                                            Catch
                                                                            End Try
                                                                            Exit While
                                                                        End If
                                                                    Catch
                                                                    End Try
                                                                End If
                                                                If Not String.IsNullOrWhiteSpace(outputWavPath) AndAlso File.Exists(outputWavPath) Then
                                                                    Dim len = New FileInfo(outputWavPath).Length
                                                                    Try
                                                                        Dim headerDeclaredData As Long? = TryReadWavDataChunkSize(outputWavPath)
                                                                        If headerDeclaredData.HasValue AndAlso headerDeclaredData.Value > 0 Then
                                                                            ' Use declared data chunk size + small header overhead as expected total file size
                                                                            expectedSize = headerDeclaredData.Value + 128
                                                                        End If
                                                                    Catch
                                                                    End Try

                                                                    If expectedSize > 0 Then
                                                                        Dim pctD = (len / expectedSize) * 100.0
                                                                        ' Cap polling progress to avoid sudden jump to 99% before finalization
                                                                        Dim maxPollPercent As Integer = 95
                                                                        Dim pctI As Integer = CInt(Math.Max(0, Math.Min(maxPollPercent, Math.Floor(pctD))))
                                                                        ' Ensure we show minimal progress once file starts growing to avoid staying at 0%
                                                                        If pctI = 0 AndAlso len > 0 Then
                                                                            pctI = 1
                                                                        End If
                                                                        If pctI > lastProgress Then
                                                                            lastProgress = pctI
                                                                            Try
                                                                                ' Trace progress callback from polling
                                                                                Try
                                                                                    Dim tracePath = Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                                                                                    System.IO.File.AppendAllText(tracePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Track={trackNum} source=poll pct={pctI} size={len} expected={CInt(expectedSize)}{Environment.NewLine}")
                                                                                Catch
                                                                                End Try
                                                                                progressCallback(pctI)
                                                                            Catch
                                                                            End Try
                                                                            Try
                                                                                CDAudioAnalyzer.DiagnosticWrite($"FREAC_POLL_PROGRESS: Track={trackNum} pct={pctI} size={len} expected={CInt(expectedSize)}")
                                                                            Catch
                                                                            End Try
                                                                        End If
                                                                    Else
                                                                        ' No expected size: provide a gentle estimated progress based on elapsed time to avoid stuck 0%
                                                                        Try
                                                                            Dim elapsedPct As Integer = CInt(Math.Min(95.0, (sw.Elapsed.TotalMilliseconds / Math.Max(1, timeoutMs)) * 95.0))
                                                                            If elapsedPct > lastProgress Then
                                                                                lastProgress = elapsedPct
                                                                                progressCallback(elapsedPct)
                                                                            End If
                                                                        Catch
                                                                        End Try
                                                                    End If
                                                                End If
                                                            Catch
                                                            End Try
                                                            System.Threading.Thread.Sleep(250)
                                                        End While
                                                        ' Cancel polling task if running
                                                        Try
                                                            If cts IsNot Nothing Then
                                                                cts.Cancel()
                                                                If pollTask IsNot Nothing Then pollTask.Wait(500)
                                                            End If
                                                        Catch
                                                        End Try
                                                    Catch
                                                    End Try
                                                End Sub, token)
                        Catch
                        End Try
                    End If

                    Dim timedOut As Boolean = True
                    While sw.ElapsedMilliseconds < timeoutMs
                        ' Regularly check for external cancellation request while waiting for process exit
                        If cancellationCheck IsNot Nothing Then
                            Try
                                If cancellationCheck() Then
                                    Try
                                        Dim tracePath = Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                                        System.IO.File.AppendAllText(tracePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Track={trackNum} WAIT_LOOP CANCEL{Environment.NewLine}")
                                    Catch
                                    End Try
                                    Try
                                        CDAudioAnalyzer.DiagnosticWrite($"FREAC_WAIT_CANCEL_REQUEST: Track={trackNum}")
                                    Catch
                                    End Try
                                    Try
                                        If Not p.HasExited Then p.Kill()
                                    Catch
                                    End Try
                                    Exit While
                                End If
                            Catch
                            End Try
                        End If
                        If p.WaitForExit(250) Then
                            timedOut = False
                            Exit While
                        End If

                        Try
                            If progressCallback IsNot Nothing Then
                                Dim estimated As Integer = CInt(Math.Min(95.0, (sw.Elapsed.TotalMilliseconds / Math.Max(1, timeoutMs)) * 95.0))
                                If estimated > lastProgress Then
                                    lastProgress = estimated
                                    progressCallback(estimated)
                                End If
                            End If
                        Catch
                        End Try
                    End While

                    If timedOut Then
                        Try
                            p.Kill()
                        Catch
                        End Try
                        SyncLock outputSync
                            stdOut = stdOutBuilder.ToString()
                            stdErr = stdErrBuilder.ToString()
                        End SyncLock
                        CDAudioAnalyzer.DiagnosticWrite($"FREAC_TIMEOUT: Track={trackNum}")
                        ' Ecrire le log d'exécution même en cas de timeout
                        Try
                            Dim logDir As String = If(String.IsNullOrEmpty(outputFolder), Path.GetDirectoryName(outputWavPath), outputFolder)
                            Dim runLog = Path.Combine(logDir, $"freac_run_{trackNum}.log")
                            File.WriteAllText(runLog, $"TIMEOUT{Environment.NewLine}stdout:{stdOut}{Environment.NewLine}stderr:{stdErr}")
                        Catch
                        End Try
                        Return False
                    End If

                    p.WaitForExit(1000)
                    SyncLock outputSync
                        stdOut = stdOutBuilder.ToString()
                        stdErr = stdErrBuilder.ToString()
                    End SyncLock

                    Try
                        If progressCallback IsNot Nothing AndAlso lastProgress < 99 Then
                            progressCallback(99)
                            lastProgress = 99
                        End If
                    Catch
                    End Try

                    ' Après l'exécution, écrire un log détaillé (commande, cwd, exitcode, stdout, stderr, taille fichier si présent)
                    Try
                        Dim exitCode As Integer = -999
                        Try
                            exitCode = p.ExitCode
                        Catch
                        End Try
                        Dim logDir As String = If(String.IsNullOrEmpty(outputFolder), Path.GetDirectoryName(outputWavPath), outputFolder)
                        Dim runLog = Path.Combine(logDir, $"freac_run_{trackNum}.log")
                        Dim producedSize As String = "(not found)"
                        Try
                            If File.Exists(outputWavPath) Then
                                producedSize = New FileInfo(outputWavPath).Length.ToString()
                            End If
                        Catch
                        End Try
                        Dim info As String = $"TIMESTAMP:{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}{Environment.NewLine}COMMAND:{exePath} {args}{Environment.NewLine}WORKING_DIR:{psi.WorkingDirectory}{Environment.NewLine}EXITCODE:{exitCode}{Environment.NewLine}producedSize:{producedSize}{Environment.NewLine}stdout:{stdOut}{Environment.NewLine}stderr:{stdErr}{Environment.NewLine}"
                        Try
                            If Not Directory.Exists(logDir) Then Directory.CreateDirectory(logDir)
                            File.WriteAllText(runLog, info)
                        Catch
                        End Try
                    Catch
                    End Try
                Catch exWait As Exception
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"FREAC_ERROR_WAIT: {exWait.Message}")
                    Catch
                    End Try
                End Try

                ' Vérifier la création du fichier
                Try
                    Dim markerPath = outputWavPath & ".ripper.txt"

                    ' Si le ripper a écrit le WAV dans le répertoire de l'exécutable par erreur,
                    ' essayer de le déplacer vers outputWavPath (évite double extraction).
                    If Not File.Exists(outputWavPath) AndAlso Not String.IsNullOrEmpty(outputFolder) Then
                        Try
                            Dim exeDir As String = Path.GetDirectoryName(exePath)
                            Dim fileName As String = Path.GetFileName(outputWavPath)
                            Dim localCandidates As New List(Of String) From {
                                Path.Combine(exeDir, fileName),
                                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
                                Path.Combine(Environment.CurrentDirectory, fileName)
                            }

                            For Each cand In localCandidates
                                Try
                                    If File.Exists(cand) Then
                                        ' Déplacer vers le dossier d'album (créer le dossier si nécessaire)
                                        Dim destDir = Path.GetDirectoryName(outputWavPath)
                                        If Not Directory.Exists(destDir) Then Directory.CreateDirectory(destDir)
                                        File.Move(cand, outputWavPath)
                                        CDAudioAnalyzer.DiagnosticWrite($"FREAC_MOVED_OUTPUT: from={cand} to={outputWavPath}")
                                        Exit For
                                    End If
                                Catch exMove As Exception
                                    Try
                                        CDAudioAnalyzer.DiagnosticWrite($"FREAC_MOVE_FAILED: src={cand} dst={outputWavPath} err={exMove.Message}")
                                    Catch
                                    End Try
                                End Try
                            Next
                        Catch
                        End Try
                    End If

                    If File.Exists(outputWavPath) Then
                        Dim fi = New FileInfo(outputWavPath)
                        If fi.Length > 1024 Then
                            Try
                                File.WriteAllText(markerPath, $"FREAC_OK: exe={exePath} size={fi.Length}")
                                ' Sauvegarder une copie de l'original produit par freac pour traçabilité
                                ' Historique original supprimé: ne plus créer de copie .freac.orig pour éviter fichiers en double
                            Catch
                            End Try
                            CDAudioAnalyzer.DiagnosticWrite($"FREAC_OK: Track={trackNum} WAV={outputWavPath} size={fi.Length}")
                            ' Ecrire log d'exécution détaillé
                            Try
                                Dim logDir As String = If(String.IsNullOrEmpty(outputFolder), Path.GetDirectoryName(outputWavPath), outputFolder)
                                Dim runLog = Path.Combine(logDir, $"freac_run_{trackNum}.log")
                                File.WriteAllText(runLog, $"EXITCODE:{p.ExitCode}{Environment.NewLine}stdout:{stdOut}{Environment.NewLine}stderr:{stdErr}")
                            Catch
                            End Try
                            Return True
                        Else
                            Try
                                File.WriteAllText(markerPath, $"FREAC_SMALL_OUTPUT: exe={exePath} size={fi.Length}")
                            Catch
                            End Try
                            CDAudioAnalyzer.DiagnosticWrite($"FREAC_SMALL_OUTPUT: Track={trackNum} WAV={outputWavPath} size={fi.Length}")
                            Try
                                Dim logDir As String = If(String.IsNullOrEmpty(outputFolder), Path.GetDirectoryName(outputWavPath), outputFolder)
                                Dim runLog = Path.Combine(logDir, $"freac_run_{trackNum}.log")
                                File.WriteAllText(runLog, $"EXITCODE:{p.ExitCode}{Environment.NewLine}stdout:{stdOut}{Environment.NewLine}stderr:{stdErr}")
                            Catch
                            End Try
                            Return False
                        End If
                    Else
                        Try
                            File.WriteAllText(markerPath, $"FREAC_NO_OUTPUT: exe={exePath}")
                        Catch
                        End Try
                        CDAudioAnalyzer.DiagnosticWrite($"FREAC_NO_OUTPUT: Track={trackNum} expected={outputWavPath}")
                        Try
                            Dim logDir As String = If(String.IsNullOrEmpty(outputFolder), Path.GetDirectoryName(outputWavPath), outputFolder)
                            Dim runLog = Path.Combine(logDir, $"freac_run_{trackNum}.log")
                            File.WriteAllText(runLog, $"EXITCODE:{p.ExitCode}{Environment.NewLine}stdout:{stdOut}{Environment.NewLine}stderr:{stdErr}")
                        Catch
                        End Try
                        Return False
                    End If
                Catch exCheck As Exception
                    Try
                        File.WriteAllText(outputWavPath & ".ripper.txt", $"FREAC_CHECK_ERROR: {exCheck.Message}")
                    Catch
                    End Try
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"FREAC_CHECK_ERROR: {exCheck.Message}")
                    Catch
                    End Try
                    Return False
                End Try
            End Using
        Catch ex As Exception
            Try
                CDAudioAnalyzer.DiagnosticWrite($"FREAC_EXCEPTION: {ex.Message}")
            Catch
            End Try
            Return False
        End Try
    End Function

    ' Try to read WAV data chunk size (in bytes) from the file header in a robust way.
    Private Shared Function TryReadWavDataChunkSize(wavPath As String) As Long?
        Try
            Using fs As New FileStream(wavPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Using br As New BinaryReader(fs)
                    ' Validate RIFF/WAVE
                    fs.Seek(0, SeekOrigin.Begin)
                    Dim riff = System.Text.Encoding.ASCII.GetString(br.ReadBytes(4))
                    If riff <> "RIFF" Then Return Nothing
                    fs.Seek(8, SeekOrigin.Begin)
                    Dim wave = System.Text.Encoding.ASCII.GetString(br.ReadBytes(4))
                    If wave <> "WAVE" Then Return Nothing

                    ' Walk chunks until 'data' found or EOF
                    fs.Seek(12, SeekOrigin.Begin)
                    While fs.Position + 8 <= fs.Length
                        Dim chunkId = System.Text.Encoding.ASCII.GetString(br.ReadBytes(4))
                        Dim chunkSize = br.ReadUInt32()
                        If chunkId = "data" Then
                            Return CLng(chunkSize)
                        Else
                            ' Skip to next chunk (chunkSize may be odd, account for pad byte)
                            Dim skip = CLng(chunkSize)
                            fs.Seek(skip, SeekOrigin.Current)
                            If (skip And 1) = 1 Then fs.Seek(1, SeekOrigin.Current)
                        End If
                    End While
                End Using
            End Using
        Catch
        End Try
        Return Nothing
    End Function

    Private Shared Sub LoadReaderModeCache()
        Try
            Dim dir = Path.GetDirectoryName(readerModeCacheFile)
            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            If File.Exists(readerModeCacheFile) Then
                Dim json = File.ReadAllText(readerModeCacheFile)
                If Not String.IsNullOrWhiteSpace(json) Then
                    Dim data = JsonSerializer.Deserialize(Of Dictionary(Of String, Integer))(json)
                    If data IsNot Nothing Then
                        SyncLock readerModeCache
                            readerModeCache = New Dictionary(Of String, Integer)(data, StringComparer.OrdinalIgnoreCase)
                        End SyncLock
                    End If
                End If
            End If
        Catch ex As Exception
            ' Ne pas interrompre l'exécution si le chargement échoue
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] LoadReaderModeCache failed: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub SaveReaderModeCache()
        Try
            Dim dir = Path.GetDirectoryName(readerModeCacheFile)
            If Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            Dim options As New JsonSerializerOptions(JsonSerializerDefaults.Web) With {
                .WriteIndented = True
            }
            Dim snapshot As Dictionary(Of String, Integer)
            SyncLock readerModeCache
                snapshot = New Dictionary(Of String, Integer)(readerModeCache, StringComparer.OrdinalIgnoreCase)
            End SyncLock

            Dim json = JsonSerializer.Serialize(snapshot, options)
            File.WriteAllText(readerModeCacheFile, json)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] SaveReaderModeCache failed: {ex.Message}")
        End Try
    End Sub

    <DllImport("kernel32.dll", SetLastError:=True, CharSet:=CharSet.Auto)>
    Private Shared Function CreateFile(lpFileName As String, dwDesiredAccess As UInteger,
                                       dwShareMode As UInteger, lpSecurityAttributes As IntPtr,
                                       dwCreationDisposition As UInteger, dwFlagsAndAttributes As UInteger,
                                       hTemplateFile As IntPtr) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function CloseHandle(hObject As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function GetLastError() As Integer
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function DeviceIoControl(hDevice As IntPtr, dwIoControlCode As UInteger,
                                            lpInBuffer As IntPtr, nInBufferSize As UInteger,
                                            <Out> ByRef lpOutBuffer As CDROM_TOC, nOutBufferSize As UInteger,
                                            <Out> ByRef lpBytesReturned As UInteger,
                                            lpOverlapped As IntPtr) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function DeviceIoControl(hDevice As IntPtr, dwIoControlCode As UInteger,
                                            ByRef lpInBuffer As RAW_READ_INFO, nInBufferSize As UInteger,
                                            lpOutBuffer As IntPtr, nOutBufferSize As UInteger,
                                            <Out> ByRef lpBytesReturned As UInteger,
                                            lpOverlapped As IntPtr) As Boolean
    End Function

    ' Structure pour la lecture brute des secteurs CD
    <StructLayout(LayoutKind.Sequential)>
    Private Structure RAW_READ_INFO
        Public DiskOffset As Long ' Position LBA (Logical Block Address) - numéro de secteur absolu sur le CD
        Public SectorCount As UInteger ' Nombre de secteurs à lire
        Public TrackMode As Integer ' 2 = CDDA (audio brut, 2352 bytes/secteur), 1 = Mode1 (données 2048), 0 = tous modes
    End Structure

    ' Structure pour la table des matières du CD
    <StructLayout(LayoutKind.Sequential)>
    Private Structure TRACK_DATA
        Public Reserved As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=1)>
        Public Control As Byte()
        Public TrackNumber As Byte
        Public Reserved1 As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=4)>
        Public Address As Byte()
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure CDROM_TOC
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=2)>
        Public Length As Byte()
        Public FirstTrack As Byte
        Public LastTrack As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=100)>
        Public TrackData As TRACK_DATA()
    End Structure

    ' Structure pour représenter une piste de CD audio
    Public Class CDTrack
        Public Property Drive As String ' Ex: "D:"
        Public Property TrackNumber As Integer ' 1, 2, 3...
        Public Property Duration As TimeSpan
        Public Property Title As String ' "Piste 01", "Piste 02"...
        Public Property Artist As String ' Artiste/groupe de la piste
        Public Property StartFrame As Integer ' Offset de départ en frames (secteurs)
        Public Property EndFrame As Integer ' Offset de fin en frames

        ''' <summary>
        ''' Génère le chemin virtuel unique pour cette piste
        ''' Format: CDDA://D:/Track01
        ''' </summary>
        Public ReadOnly Property VirtualPath As String
            Get
                Return $"CDDA://{Drive}/Track{TrackNumber:D2}"
            End Get
        End Property
    End Class

    ''' <summary>
    ''' Détecte tous les lecteurs CD/DVD disponibles
    ''' </summary>
    Public Shared Function DetecterLecteursCDAudio() As List(Of String)
        Dim lecteurs As New List(Of String)
        Try
            For Each drive As DriveInfo In DriveInfo.GetDrives()
                If drive.DriveType = DriveType.CDRom Then
                    lecteurs.Add(drive.Name.TrimEnd("\"c))
                End If
            Next
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur détection lecteurs: {ex.Message}")
        End Try
        Return lecteurs
    End Function

    ''' <summary>
    ''' Vérifie si un CD audio est présent dans le lecteur spécifié
    ''' </summary>
    Public Shared Function EstCDAudioPresent(driveLetter As String) As Boolean
        Try
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Vérification présence CD dans {driveLetter}")

            ' Normaliser le nom du lecteur
            Dim drive As String = driveLetter.TrimEnd("\"c, ":"c).ToUpper()

            ' Méthode 1: Essayer via DriveInfo (rapide mais peut échouer)
            Try
                Dim driveInfo As New DriveInfo(drive & ":\")
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] DriveType={driveInfo.DriveType}, IsReady={driveInfo.IsReady}")

                If driveInfo.DriveType = DriveType.CDRom AndAlso driveInfo.IsReady Then
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Lecteur {drive} prêt selon DriveInfo")
                End If
            Catch driveEx As Exception
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] DriveInfo échoué: {driveEx.Message}")
            End Try

            ' Méthode 2: Essayer de lire directement la TOC (plus fiable)
            Dim tracks = LirePistesCD(drive & ":")
            Dim hasCD = tracks.Count > 0

            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Résultat final: {If(hasCD, "CD présent", "Pas de CD")} ({tracks.Count} pistes)")
            Return hasCD

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur vérification CD: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] StackTrace: {ex.StackTrace}")
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Lit toutes les pistes d'un CD audio en utilisant l'API Windows native
    ''' </summary>
    Public Shared Function LirePistesCD(driveLetter As String) As List(Of CDTrack)
        Dim pistes As New List(Of CDTrack)
        Try
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Lecture du lecteur {driveLetter} via DeviceIoControl")

            ' Normaliser le nom du lecteur
            Dim drive As String = driveLetter.TrimEnd("\"c, ":"c).ToUpper()
            Dim devicePath As String = $"\\.\{drive}:"

            ' Ouvrir le lecteur CD avec partage READ et WRITE pour éviter ERROR_SHARING_VIOLATION (erreur 32)
            Dim hDevice As IntPtr = CreateFile(devicePath, GENERIC_READ,
                                               FILE_SHARE_READ Or FILE_SHARE_WRITE,
                                               IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero)

            If hDevice = INVALID_HANDLE_VALUE Then
                Dim err = Marshal.GetLastWin32Error()
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Impossible d'ouvrir {devicePath}, erreur: {err}")

                ' Afficher un message d'aide selon l'erreur
                Select Case err
                    Case 32 ' ERROR_SHARING_VIOLATION
                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Le lecteur est utilisé par un autre programme (Explorer, Nero, EAC, etc.)")
                    Case 21 ' ERROR_NOT_READY
                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Aucun CD dans le lecteur ou lecteur pas prêt")
                    Case 5 ' ERROR_ACCESS_DENIED
                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Accès refusé - droits administrateur nécessaires?")
                End Select

                Return pistes
            End If

            Try
                ' Lire la table des matières (TOC) du CD
                Dim toc As New CDROM_TOC()
                Dim bytesReturned As UInteger = 0
                Dim success As Boolean = DeviceIoControl(hDevice, IOCTL_CDROM_READ_TOC, IntPtr.Zero, 0,
                                                         toc, CUInt(Marshal.SizeOf(toc)), bytesReturned, IntPtr.Zero)

                If Not success Then
                    Dim err = Marshal.GetLastWin32Error()
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] DeviceIoControl failed, erreur: {err}")
                    Return pistes
                End If

                Dim firstTrack As Integer = toc.FirstTrack
                Dim lastTrack As Integer = toc.LastTrack
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] TOC lu: {lastTrack - firstTrack + 1} pistes (#{firstTrack} à #{lastTrack})")

                ' Trouver l'entrée Lead-Out (TrackNumber = 0xAA = 170)
                Dim leadOutIndex As Integer = -1
                For j As Integer = 0 To 99
                    If toc.TrackData(j).TrackNumber = &HAA Then
                        leadOutIndex = j
                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Lead-Out trouvé à l'index {j}")
                        Exit For
                    End If
                Next

                ' Créer une entrée pour chaque piste
                For i As Integer = firstTrack To lastTrack
                    Dim trackIndex = i - firstTrack
                    Dim trackData = toc.TrackData(trackIndex)

                    ' Pour la dernière piste, utiliser le Lead-Out
                    ' Pour les autres, utiliser la piste suivante
                    Dim nextTrackData As TRACK_DATA
                    If i < lastTrack Then
                        ' Utiliser l'entrée suivante si elle semble valide, sinon rechercher un remplacement
                        Dim candidate As TRACK_DATA = toc.TrackData(trackIndex + 1)
                        If candidate.TrackNumber <> 0 OrElse HasNonZeroAddress(candidate) Then
                            nextTrackData = candidate
                        Else
                            ' Si l'entrée suivante est vide ou invalide, essayer de trouver un lead-out ou
                            ' la prochaine entrée non vide dans la table TOC
                            Dim found As Boolean = False
                            If leadOutIndex >= 0 Then
                                nextTrackData = toc.TrackData(leadOutIndex)
                                found = True
                            Else
                                For k As Integer = trackIndex + 1 To toc.TrackData.Length - 1
                                    If toc.TrackData(k).TrackNumber <> 0 OrElse HasNonZeroAddress(toc.TrackData(k)) Then
                                        nextTrackData = toc.TrackData(k)
                                        found = True
                                        Exit For
                                    End If
                                Next
                            End If

                            If Not found Then
                                ' Dernier recours: utiliser le candidat même si invalide (protection contre exceptions)
                                nextTrackData = candidate
                                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Warning: TOC entry suivante invalide pour la piste {i}, utilisation du candidat index {trackIndex + 1}")
                            End If
                        End If
                    Else
                        ' Dernière piste: préférer le lead-out, sinon rechercher une entrée valide
                        If leadOutIndex >= 0 Then
                            nextTrackData = toc.TrackData(leadOutIndex)
                        Else
                            Dim found As Boolean = False
                            For k As Integer = trackIndex + 1 To toc.TrackData.Length - 1
                                If toc.TrackData(k).TrackNumber <> 0 OrElse HasNonZeroAddress(toc.TrackData(k)) Then
                                    nextTrackData = toc.TrackData(k)
                                    found = True
                                    Exit For
                                End If
                            Next

                            If Not found Then
                                ' Aucun lead-out ni entrée suivante valide: utiliser la même piste comme fallback
                                nextTrackData = toc.TrackData(trackIndex)
                                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Warning: Lead-Out introuvable pour la dernière piste {i}, utilisation d'un fallback")
                            End If
                        End If
                    End If

                    ' Calculer la durée en frames (75 frames = 1 seconde)
                    ' Use SafeAddressByte to avoid null/address warnings and ensure safe reads
                    Dim startMinute As Integer = SafeAddressByte(trackData, 1)
                    Dim startSecond As Integer = SafeAddressByte(trackData, 2)
                    Dim startFrameByte As Integer = SafeAddressByte(trackData, 3)

                    Dim endMinute As Integer = SafeAddressByte(nextTrackData, 1)
                    Dim endSecond As Integer = SafeAddressByte(nextTrackData, 2)
                    Dim endFrameByte As Integer = SafeAddressByte(nextTrackData, 3)

                    Dim startFrame As Integer = MSFToFrames(CByte(startMinute), CByte(startSecond), CByte(startFrameByte))
                    Dim endFrame As Integer = MSFToFrames(CByte(endMinute), CByte(endSecond), CByte(endFrameByte))
                    ' Protection: si l'adresse de fin n'est pas valide (<= début), estimer une durée par défaut
                    If endFrame <= startFrame Then
                        Dim defaultSeconds As Integer = 180 ' 3 minutes par défaut
                        Dim estimatedEnd As Integer = startFrame + (CD_FRAMES_PER_SECOND * defaultSeconds)
                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Warning: endFrame ({endFrame}) <= startFrame ({startFrame}) pour la piste {i}. Estimation endFrame -> {estimatedEnd}")
                        endFrame = estimatedEnd
                    End If
                    Dim durationSeconds As Double = (endFrame - startFrame) / 75.0

                    ' Diagnostic détaillé pour toutes les pistes
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Piste {i}: MSF={startMinute:D2}:{startSecond:D2}:{startFrameByte:D2} → frames {startFrame} à {endFrame}, durée={durationSeconds:F2}s")

                    ' Vérifier que la durée est raisonnable (éviter les valeurs aberrantes)
                    If durationSeconds > 0 AndAlso durationSeconds < 6000 Then ' Max 100 minutes par piste
                        Dim trackPrefix = LanguageManager.GetString("CDTrack_Prefix")
                        pistes.Add(New CDTrack With {
                            .Drive = drive & ":",
                            .TrackNumber = i,
                            .Duration = TimeSpan.FromSeconds(durationSeconds),
                            .Title = $"{trackPrefix} {i:D2}",
                            .StartFrame = startFrame,
                            .EndFrame = endFrame
                        })

                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Piste {i}: {TimeSpan.FromSeconds(durationSeconds):mm\:ss} (frames {startFrame}-{endFrame})")
                    Else
                        ' Fallback: ne pas ignorer la piste, estimer une durée par défaut
                        Dim fallbackSeconds As Integer = 180 ' 3 minutes par défaut
                        Dim fallbackEndFrame As Integer = startFrame + (CD_FRAMES_PER_SECOND * fallbackSeconds)
                        Dim trackPrefix = LanguageManager.GetString("CDTrack_Prefix")

                        pistes.Add(New CDTrack With {
                            .Drive = drive & ":",
                            .TrackNumber = i,
                            .Duration = TimeSpan.FromSeconds(fallbackSeconds),
                            .Title = $"{trackPrefix} {i:D2}",
                            .StartFrame = startFrame,
                            .EndFrame = fallbackEndFrame
                        })

                        System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Piste {i}: durée invalide ({durationSeconds:F2}s), fallback appliqué à {fallbackSeconds}s (frames {startFrame}-{fallbackEndFrame})")
                    End If
                Next

            Finally
                CloseHandle(hDevice)
            End Try

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur lecture pistes CD: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] StackTrace: {ex.StackTrace}")
        End Try

        Return pistes
    End Function

    ' Convertir MSF (Minute/Second/Frame) en frames totaux
    ' Note: Les adresses MSF des CD incluent un offset de 150 frames (2 secondes)
    ' pour la numérotation Red Book, mais pour la lecture réelle il faut utiliser
    ' l'adresse absolue telle quelle
    Private Shared Function MSFToFrames(minute As Byte, second As Byte, frame As Byte) As Integer
        Return (minute * 60 + second) * 75 + frame
    End Function

    ''' <summary>
    ''' Lit de manière sûre un octet d'adresse MSF depuis TRACK_DATA.Address.
    ''' Retourne 0 si Address est Nothing ou si l'index est invalide.
    ''' </summary>
    Private Shared Function SafeAddressByte(td As TRACK_DATA, idx As Integer) As Byte
        Try
            If td.Address Is Nothing Then Return 0
            If idx < 0 OrElse idx >= td.Address.Length Then Return 0
            Return td.Address(idx)
        Catch
        End Try
        Return 0
    End Function

    ' Vérifie si une TRACK_DATA contient une adresse MSF non nulle
    Private Shared Function HasNonZeroAddress(td As TRACK_DATA) As Boolean
        Try
            If td.Address Is Nothing Then Return False
            For i As Integer = 0 To td.Address.Length - 1
                If td.Address(i) <> 0 Then
                    Return True
                End If
            Next
        Catch
            ' En cas d'erreur, considérer comme invalide
        End Try
        Return False
    End Function

    ''' <summary>
    ''' Vérifie si un chemin est une piste de CD virtuelle
    ''' </summary>
    Public Shared Function EstCheminCDAudio(chemin As String) As Boolean
        Return Not String.IsNullOrEmpty(chemin) AndAlso chemin.StartsWith("CDDA://", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Parse un chemin virtuel CDDA:// et retourne les informations
    ''' </summary>
    Public Shared Function ParseCheminCDAudio(chemin As String) As CDTrack
        Try
            ' Format: CDDA://D:/Track01
            If Not EstCheminCDAudio(chemin) Then Return Nothing

            Dim parts = chemin.Substring(7).Split("/"c) ' Enlever "CDDA://"
            If parts.Length < 2 Then Return Nothing

            Dim drive = parts(0) ' "D:"
            Dim trackPart = parts(1) ' "Track01"

            ' Extraire le numéro de piste
            Dim trackNumStr = trackPart.Replace("Track", "")
            Dim trackNum As Integer
            If Not Integer.TryParse(trackNumStr, trackNum) Then Return Nothing

            ' Relire les pistes pour obtenir la durée
            Dim pistes = LirePistesCD(drive)
            Dim pisteCorrespondante = pistes.FirstOrDefault(Function(p) p.TrackNumber = trackNum)

            If pisteCorrespondante IsNot Nothing Then
                Return pisteCorrespondante
            Else
                ' Si on ne trouve pas la piste dans la TOC, retourner quand même avec durée 0
                Return New CDTrack With {
                    .Drive = drive,
                    .TrackNumber = trackNum,
                    .Title = trackPart,
                    .Duration = TimeSpan.Zero
                }
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur parsing chemin CD: {ex.Message}")
            Return Nothing
        End Try
    End Function

    ''' <summary>
    ''' Crée un WaveStream pour lire une piste de CD audio via NAudio
    ''' </summary>
    Public Shared Function CreerLecteurCDAudio(track As CDTrack) As WaveStream
        Try
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Création lecteur pour {track.Drive} piste {track.TrackNumber}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Frames stockés: {track.StartFrame} à {track.EndFrame} (durée: {track.Duration:mm\:ss})")
            ' Écrire dans le log de diagnostic pour prouver les frames passées au lecteur
            Try
                CDAudioAnalyzer.DiagnosticWrite($"CREER_LECTEUR: Track={track.TrackNumber} Start={track.StartFrame} End={track.EndFrame} Duration={track.Duration.TotalSeconds:F2}s")
            Catch
            End Try

            ' Créer un lecteur CD avec les informations de la piste, en passant les frames précalculés
            Dim cdReader As New CDReader(track.Drive, track.TrackNumber, track.Duration, track.StartFrame, track.EndFrame)
            Return cdReader

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur création lecteur CD: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] StackTrace: {ex.StackTrace}")
            Return Nothing
        End Try
    End Function

#Region "MCI (Media Control Interface) - API Windows pour CD Audio"

    ' Commandes MCI
    Private Const MCI_OPEN As UInteger = &H803UI
    Private Const MCI_CLOSE As UInteger = &H804UI
    Private Const MCI_STATUS As UInteger = &H814UI
    Private Const MCI_SET As UInteger = &H80DUI

    ' Flags MCI
    Private Const MCI_OPEN_TYPE As UInteger = &H2000UI
    Private Const MCI_OPEN_ELEMENT As UInteger = &H200UI
    Private Const MCI_STATUS_ITEM As UInteger = &H100UI
    Private Const MCI_STATUS_NUMBER_OF_TRACKS As UInteger = &H4UI ' 4 = nombre de pistes
    Private Const MCI_STATUS_LENGTH As UInteger = &H1UI
    Private Const MCI_STATUS_POSITION As UInteger = &H2UI
    Private Const MCI_STATUS_READY As UInteger = &H7UI
    Private Const MCI_STATUS_MEDIA_PRESENT As UInteger = &H5UI ' 5 = média présent
    Private Const MCI_TRACK As UInteger = &H10UI
    Private Const MCI_SET_TIME_FORMAT As UInteger = &H400UI
    Private Const MCI_FORMAT_MILLISECONDS As UInteger = &H0UI
    Private Const MCI_FORMAT_TMSF As UInteger = &HAUI ' Track/Minute/Second/Frame

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode, Pack:=1)>
    Private Structure MCI_OPEN_PARMS
        Public dwCallback As IntPtr
        Public wDeviceID As UInteger
        Public lpstrDeviceType As String
        Public lpstrElementName As String
        Public lpstrAlias As String
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Private Structure MCI_STATUS_PARMS
        Public dwCallback As IntPtr
        Public dwReturn As UInteger
        Public dwItem As UInteger
        Public dwTrack As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Private Structure MCI_SET_PARMS
        Public dwCallback As IntPtr
        Public dwTimeFormat As UInteger
    End Structure

    <DllImport("winmm.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function mciSendCommand(wDeviceID As UInteger, uMessage As UInteger, dwParam1 As UInteger, ByRef dwParam2 As MCI_OPEN_PARMS) As Integer
    End Function

    <DllImport("winmm.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function mciSendCommand(wDeviceID As UInteger, uMessage As UInteger, dwParam1 As UInteger, ByRef dwParam2 As MCI_STATUS_PARMS) As Integer
    End Function

    <DllImport("winmm.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function mciSendCommand(wDeviceID As UInteger, uMessage As UInteger, dwParam1 As UInteger, ByRef dwParam2 As MCI_SET_PARMS) As Integer
    End Function

    <DllImport("winmm.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function mciGetErrorString(dwError As UInteger, lpszErrorText As System.Text.StringBuilder, cchErrorText As UInteger) As Boolean
    End Function

    Private Shared Function OuvrirCDAudio(driveLetter As String) As UInteger
        Try
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Tentative d'ouverture du lecteur: '{driveLetter}'")

            ' Nettoyer le nom du lecteur - MCI accepte juste la lettre ou lettre:
            Dim deviceName As String = driveLetter.TrimEnd("\"c, "/"c).TrimEnd(":"c) & ":"

            Dim openParams As New MCI_OPEN_PARMS With {
                .dwCallback = IntPtr.Zero,
                .wDeviceID = 0,
                .lpstrDeviceType = "cdaudio",
                .lpstrElementName = deviceName,
                .lpstrAlias = Nothing
            }

            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Device name formaté: '{deviceName}'")
            Dim result = mciSendCommand(0, MCI_OPEN, MCI_OPEN_TYPE Or MCI_OPEN_ELEMENT, openParams)

            If result = 0 Then
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] CD ouvert avec succès, deviceID: {openParams.wDeviceID}")
                ' Configurer le format de temps en TMSF pour les CD audio
                ' (Track/Minute/Second/Frame - format natif des CD audio)
                Dim setParams As New MCI_SET_PARMS With {
                    .dwCallback = IntPtr.Zero,
                    .dwTimeFormat = MCI_FORMAT_TMSF
                }
                Dim setResult = mciSendCommand(openParams.wDeviceID, MCI_SET, MCI_SET_TIME_FORMAT, setParams)
                If setResult <> 0 Then
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] mciSendCommand SET TMSF failed with error: {setResult}")
                    Dim errorMsg As New System.Text.StringBuilder(256)
                    mciGetErrorString(CUInt(setResult), errorMsg, CUInt(errorMsg.Capacity))
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] MCI Error SET: {errorMsg.ToString()}")
                Else
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Format TMSF défini avec succès")
                End If
                Return openParams.wDeviceID
            Else
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] mciSendCommand OPEN failed with error: {result}")
                ' Obtenir le message d'erreur MCI
                Dim errorMsg As New System.Text.StringBuilder(256)
                mciGetErrorString(CUInt(result), errorMsg, CUInt(errorMsg.Capacity))
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] MCI Error: {errorMsg.ToString()}")
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Exception MCI OPEN: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] StackTrace: {ex.StackTrace}")
        End Try
        Return 0
    End Function

    Private Shared Sub FermerCDAudio(deviceId As UInteger)
        Try
            If deviceId = 0 Then Return
            Dim openParams As New MCI_OPEN_PARMS With {
                .dwCallback = IntPtr.Zero,
                .wDeviceID = 0,
                .lpstrDeviceType = Nothing,
                .lpstrElementName = Nothing,
                .lpstrAlias = Nothing
            }
            mciSendCommand(deviceId, MCI_CLOSE, 0, openParams)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur MCI CLOSE: {ex.Message}")
        End Try
    End Sub

    Private Shared Function ObtenirNombrePistes(deviceId As UInteger) As Integer
        Try
            ' Ne pas vérifier média présent - essayer directement de lire les pistes
            ' Si aucun CD n'est présent, cette commande échouera simplement
            Dim statusParams As New MCI_STATUS_PARMS With {
                .dwCallback = IntPtr.Zero,
                .dwReturn = 0,
                .dwItem = MCI_STATUS_NUMBER_OF_TRACKS,
                .dwTrack = 0
            }
            Dim result = mciSendCommand(deviceId, MCI_STATUS, MCI_STATUS_ITEM, statusParams)
            If result = 0 Then
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Nombre de pistes retourné: {statusParams.dwReturn}")
                Return CInt(statusParams.dwReturn)
            Else
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] mciSendCommand STATUS NUMBER_OF_TRACKS failed with error: {result}")
                ' Obtenir le message d'erreur MCI
                Dim errorMsg As New System.Text.StringBuilder(256)
                mciGetErrorString(CUInt(result), errorMsg, CUInt(errorMsg.Capacity))
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] MCI Error: {errorMsg.ToString()}")

                ' Essayer avec la valeur alternative pour NUMBER_OF_TRACKS
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Essai avec MCI_STATUS_MODE...")
                statusParams.dwItem = &H3UI ' Essayer l'ancienne valeur
                result = mciSendCommand(deviceId, MCI_STATUS, MCI_STATUS_ITEM, statusParams)
                If result = 0 Then
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Mode retourné: {statusParams.dwReturn}")
                Else
                    Dim errMsg2 As New System.Text.StringBuilder(256)
                    mciGetErrorString(CUInt(result), errMsg2, CUInt(errMsg2.Capacity))
                    System.Diagnostics.Debug.WriteLine($"[CDAudioManager] MCI Error MODE: {errMsg2.ToString()}")
                End If
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur MCI STATUS: {ex.Message}")
        End Try
        Return 0
    End Function

    Private Shared Function ObtenirDureePiste(deviceId As UInteger, trackNumber As Integer) As TimeSpan
        Try
            Dim statusParams As New MCI_STATUS_PARMS With {
                .dwCallback = IntPtr.Zero,
                .dwReturn = 0,
                .dwItem = MCI_STATUS_LENGTH,
                .dwTrack = CUInt(trackNumber)
            }
            Dim result = mciSendCommand(deviceId, MCI_STATUS, MCI_STATUS_ITEM Or MCI_TRACK, statusParams)
            If result = 0 Then
                ' dwReturn contient la durée en millisecondes
                Return TimeSpan.FromMilliseconds(statusParams.dwReturn)
            Else
                System.Diagnostics.Debug.WriteLine($"[CDAudioManager] mciSendCommand LENGTH failed with error: {result}")
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[CDAudioManager] Erreur durée piste: {ex.Message}")
        End Try
        Return TimeSpan.Zero
    End Function

#End Region

#Region "CDReader - Lecteur de piste CD via MCI"

    ''' <summary>
    ''' Lecteur de piste CD compatible avec NAudio WaveStream
    ''' </summary>
    Public Class CDReader
        Inherits WaveStream

        Private _drive As String
        Private _trackNumber As Integer
        Private _waveFormat As WaveFormat
        Private _length As Long
        Private _position As Long
        Private _duration As TimeSpan
        Private _startFrame As Integer
        Private _endFrame As Integer
        Private _hDevice As IntPtr
        Private _buffer As Byte()
        Private _bufferPosition As Integer
        Private _bufferDataLength As Integer
        Private _readSummaryCounter As Integer = 0
        Private _lastNonZeroCount As Integer = -1

        Public Sub New(drive As String, trackNumber As Integer, duration As TimeSpan, startFrame As Integer, endFrame As Integer)
            _drive = drive
            _trackNumber = trackNumber
            _duration = duration
            _startFrame = startFrame
            _endFrame = endFrame
            _waveFormat = New WaveFormat(44100, 16, 2) ' CD Audio standard
            _buffer = New Byte(CD_SECTOR_SIZE * 10 - 1) {} ' Buffer pour 10 secteurs
            _bufferPosition = 0
            _bufferDataLength = 0

            ' Ouvrir le lecteur CD
            ' Format attendu par Windows: \\.\D: (avec les deux-points)
            Dim driveLetter As String = drive.TrimEnd("\"c, ":"c).ToUpper()
            Dim devicePath As String = $"\\.\{driveLetter}:"
            System.Diagnostics.Debug.WriteLine($"[CDReader] Tentative d'ouverture de {devicePath}")

            ' Ouvrir avec FILE_SHARE_READ | FILE_SHARE_WRITE pour éviter ERROR_SHARING_VIOLATION
            _hDevice = CreateFile(devicePath, GENERIC_READ,
                                  FILE_SHARE_READ Or FILE_SHARE_WRITE,
                                  IntPtr.Zero, OPEN_EXISTING, 0, IntPtr.Zero)

            If _hDevice = INVALID_HANDLE_VALUE Then
                Dim lastError = Marshal.GetLastWin32Error()
                System.Diagnostics.Debug.WriteLine($"[CDReader] Erreur CreateFile: code {lastError}")
                Throw New InvalidOperationException($"Impossible d'ouvrir le lecteur CD {drive} (erreur {lastError})")
            End If

            ' Les frames sont déjà calculés et passés en paramètre, pas besoin de relire la TOC
            ' Calculer la longueur en bytes (chaque frame = 2352 bytes bruts)
            Dim totalFrames = _endFrame - _startFrame
            _length = CLng(totalFrames) * 2352L

            ' NOTE: ne pas appliquer de corrections fixes sur les offsets de lecture ici.
            ' Les frames de début/fin (_startFrame/_endFrame) sont calculées par l'analyse
            ' et doivent être respectées telles quelles lors de la lecture.

            _position = 0

            System.Diagnostics.Debug.WriteLine($"[CDReader] Piste {trackNumber}: frames {_startFrame} à {_endFrame} (total: {totalFrames})")
        End Sub

        Public Overrides ReadOnly Property WaveFormat As WaveFormat
            Get
                Return _waveFormat
            End Get
        End Property

        Public Overrides ReadOnly Property Length As Long
            Get
                Return _length
            End Get
        End Property

        Public Overrides Property Position As Long
            Get
                Return _position
            End Get
            Set(value As Long)
                _position = value
                _bufferPosition = 0
                _bufferDataLength = 0
            End Set
        End Property

        Public Overrides Function Read(buffer() As Byte, offset As Integer, count As Integer) As Integer
            If _hDevice = INVALID_HANDLE_VALUE Then
                Return 0
            End If

            Dim totalBytesRead As Integer = 0

            While totalBytesRead < count AndAlso _position < _length
                ' Si le buffer interne est vide, lire plus de secteurs du CD
                If _bufferPosition >= _bufferDataLength Then
                    Dim currentFrame = _startFrame + CInt(_position \ 2352)

                    ' ✅ CORRECTION : Calculer les frames restants jusqu'à la fin de la piste
                    Dim framesRestants = _endFrame - currentFrame
                    If framesRestants <= 0 Then
                        ' On a atteint ou dépassé la fin de la piste
                        Exit While
                    End If

                    ' Lire au maximum 10 frames, mais pas plus que ce qui reste
                    Dim framesToRead = Math.Min(10, framesRestants)

                    ' Lire les secteurs bruts du CD
                    ' Pour IOCTL_CDROM_RAW_READ avec TrackMode=2 (CDDA):
                    ' DiskOffset doit être en unités de secteurs logiques de 2048 bytes
                    ' CORRECTION d'offset nécessaire pour la synchronisation CD:
                    ' - Piste 1: Correction de -150 frames (limitée par frame de départ ~152)
                    ' - Autres pistes: Correction de -337 frames (~4.5 secondes)
                    ' MAIS pour la piste 1, on limite aussi framesToRead à la fin pour éviter débordement
                    ' Lire exactement à partir de la frame courante (aucune correction fixe)
                    Dim frameALire As Long = currentFrame

                    ' Prepare default raw read (LBA sector index, TrackMode=2)
                    Dim rawRead As New RAW_READ_INFO()
                    ' DiskOffset pour IOCTL_CDROM_RAW_READ doit être exprimé en unités de blocs logiques (2048 bytes)
                    ' Utiliser frameALire * 2048 pour conserver le comportement antérieur qui fonctionnait
                    rawRead.DiskOffset = frameALire * 2048L
                    rawRead.SectorCount = CUInt(framesToRead)
                    rawRead.TrackMode = 2 ' CDDA (audio)

                    ' Check if we have a cached working mode for this drive
                    Dim cacheKey As String = _drive.TrimEnd("\"c, ":"c).ToUpper()
                    Dim cachedMode As Integer = -1
                    Dim hasCache As Boolean = False
                    SyncLock readerModeCache
                        hasCache = readerModeCache.TryGetValue(cacheKey, cachedMode)
                    End SyncLock

                    Dim forcedMode As Integer = -1 ' -1 = none, 0 = LBA, 1 = orig, 2 = fallback
                    If hasCache Then
                        forcedMode = cachedMode
                    End If

                    If _position = 0 Then
                        System.Diagnostics.Debug.WriteLine($"[CDReader] ⭐ PREMIÈRE LECTURE Piste {_trackNumber} - currentFrame={currentFrame}, frameALire={frameALire} (no offset), DiskOffset={rawRead.DiskOffset}, framesToRead={framesToRead}, framesRestants={framesRestants}, _startFrame={_startFrame}, _endFrame={_endFrame}")
                        ' Écrire la première frame lue dans le log de diagnostic
                        Try
                            CDAudioAnalyzer.DiagnosticWrite($"CDREAD_FIRST: Track={_trackNumber} currentFrame={currentFrame} startFrame={_startFrame} endFrame={_endFrame}")
                        Catch
                        End Try
                    End If

                    Dim bytesReturned As UInteger = 0
                    Dim bufferHandle As GCHandle = GCHandle.Alloc(_buffer, GCHandleType.Pinned)

                    Try
                        Dim success As Boolean = False
                        Dim bytesReturnedLocal As UInteger = 0

                        ' If we have a cached/forced mode for this drive, use it
                        If forcedMode = 1 Then
                            ' Original behavior: DiskOffset = frame*2048, TrackMode = 2
                            Dim rawReadOrig As RAW_READ_INFO = rawRead
                            rawReadOrig.DiskOffset = frameALire * 2048L
                            rawReadOrig.TrackMode = 2
                            success = DeviceIoControl(_hDevice, IOCTL_CDROM_RAW_READ, rawReadOrig, CUInt(Marshal.SizeOf(rawReadOrig)), bufferHandle.AddrOfPinnedObject(), CUInt(_buffer.Length), bytesReturnedLocal, IntPtr.Zero)
                        ElseIf forcedMode = 2 Then
                            ' Fallback: DiskOffset = frame*2048, TrackMode = 1
                            Dim rawRead2 As RAW_READ_INFO = rawRead
                            rawRead2.DiskOffset = frameALire * 2048L
                            rawRead2.TrackMode = 1
                            success = DeviceIoControl(_hDevice, IOCTL_CDROM_RAW_READ, rawRead2, CUInt(Marshal.SizeOf(rawRead2)), bufferHandle.AddrOfPinnedObject(), CUInt(_buffer.Length), bytesReturnedLocal, IntPtr.Zero)
                        Else
                            ' Default: DiskOffset = LBA sector index, TrackMode = 2
                            success = DeviceIoControl(_hDevice, IOCTL_CDROM_RAW_READ, rawRead, CUInt(Marshal.SizeOf(rawRead)), bufferHandle.AddrOfPinnedObject(), CUInt(_buffer.Length), bytesReturnedLocal, IntPtr.Zero)
                        End If

                        bytesReturned = bytesReturnedLocal

                        If Not success OrElse bytesReturned = 0 Then
                            System.Diagnostics.Debug.WriteLine($"[CDReader] Erreur lecture secteur {currentFrame}")

                            Exit While
                        End If

                        _bufferDataLength = CInt(bytesReturned)
                        _bufferPosition = 0

                        ' Diagnostic: analyser les premiers octets lus pour détecter données nulles/corrompues
                        Try
                            Dim nz As Integer = 0
                            For i As Integer = 0 To CInt(bytesReturned) - 1
                                If _buffer(i) <> 0 Then nz += 1
                            Next

                            ' Construire un petit hex dump des premiers 64 octets (ou moins) seulement si le logging verbeux est activé
                            If VerboseCDReadLogging Then
                                Dim dumpLen As Integer = Math.Min(CInt(bytesReturned), 64)
                                Dim sb As New System.Text.StringBuilder()
                                For i As Integer = 0 To dumpLen - 1
                                    sb.Append(_buffer(i).ToString("X2"))
                                    If i < dumpLen - 1 Then sb.Append(" ")
                                Next

                                CDAudioAnalyzer.DiagnosticWrite($"CDREAD_RAW: Track={_trackNumber} currentFrame={currentFrame} bytesReturned={bytesReturned} nonZeroBytes={nz} hexFirst{dumpLen}={sb.ToString}")
                            Else
                                ' Minimal logging: limiter la fréquence pour éviter des logs volumineux
                                _readSummaryCounter += 1
                                Dim shouldLog As Boolean = False
                                ' Log si le nombre d'octets non nuls change (anomalie) ou toutes les N lectures
                                If nz <> _lastNonZeroCount Then
                                    shouldLog = True
                                ElseIf (_readSummaryCounter Mod CDReadSummaryInterval) = 0 Then
                                    shouldLog = True
                                End If

                                If shouldLog Then
                                    CDAudioAnalyzer.DiagnosticWrite($"CDREAD_SUMMARY: Track={_trackNumber} currentFrame={currentFrame} bytesReturned={bytesReturned} nonZeroBytes={nz}")
                                End If
                                _lastNonZeroCount = nz
                            End If

                            ' If we read only zeros, attempt a fallback with alternate parameters
                            If nz = 0 Then
                                Try
                                    ' First try: attempt to mimic the original behavior used previously by the app
                                    ' Some systems expected DiskOffset = frame * 2048 and TrackMode = 2
                                    Try
                                        CDAudioAnalyzer.DiagnosticWrite($"CDREAD_ORIG_TRY: Track={_trackNumber} currentFrame={currentFrame} - trying original mode (DiskOffset=frame*2048, TrackMode=2)")

                                        Dim rawReadOrig As RAW_READ_INFO = rawRead
                                        rawReadOrig.DiskOffset = frameALire * 2048L
                                        rawReadOrig.SectorCount = CUInt(framesToRead)
                                        rawReadOrig.TrackMode = 2

                                        Dim bytesReturnedOrig As UInteger = 0
                                        Dim successOrig = DeviceIoControl(_hDevice, IOCTL_CDROM_RAW_READ,
                                                                          rawReadOrig, CUInt(Marshal.SizeOf(rawReadOrig)),
                                                                          bufferHandle.AddrOfPinnedObject(), CUInt(_buffer.Length),
                                                                          bytesReturnedOrig, IntPtr.Zero)

                                        If successOrig AndAlso bytesReturnedOrig > 0 Then
                                            Dim nzOrig As Integer = 0
                                            For i As Integer = 0 To CInt(bytesReturnedOrig) - 1
                                                If _buffer(i) <> 0 Then nzOrig += 1
                                            Next

                                            Dim dumpLenOrig As Integer = Math.Min(CInt(bytesReturnedOrig), 64)
                                            Dim sbOrig As New System.Text.StringBuilder()
                                            For i As Integer = 0 To dumpLenOrig - 1
                                                sbOrig.Append(_buffer(i).ToString("X2"))
                                                If i < dumpLenOrig - 1 Then sbOrig.Append(" ")
                                            Next

                                            If VerboseCDReadLogging Then
                                                CDAudioAnalyzer.DiagnosticWrite($"CDREAD_ORIG_RESULT: Track={_trackNumber} currentFrame={currentFrame} bytesReturned={bytesReturnedOrig} nonZeroBytes={nzOrig} hexFirst{dumpLenOrig}={sbOrig.ToString}")
                                            Else
                                                CDAudioAnalyzer.DiagnosticWrite($"CDREAD_ORIG_RESULT_SUMMARY: Track={_trackNumber} currentFrame={currentFrame} bytesReturned={bytesReturnedOrig} nonZeroBytes={nzOrig}")
                                            End If

                                            If nzOrig > 0 Then
                                                _bufferDataLength = CInt(bytesReturnedOrig)
                                                _bufferPosition = 0
                                                ' Found valid data using original mode; skip other fallbacks
                                                ' Cache this mode for the drive so future reads use it directly
                                                Try
                                                    SyncLock readerModeCache
                                                        readerModeCache(cacheKey) = 1
                                                    End SyncLock
                                                Catch
                                                End Try
                                                Continue While
                                            End If
                                        Else
                                            CDAudioAnalyzer.DiagnosticWrite($"CDREAD_ORIG_ERROR: Track={_trackNumber} currentFrame={currentFrame} successOrig={successOrig} bytesReturnedOrig={bytesReturnedOrig}")
                                        End If
                                    Catch exOrig As Exception
                                        CDAudioAnalyzer.DiagnosticWrite($"CDREAD_ORIG_EXCEPTION: Track={_trackNumber} currentFrame={currentFrame} - {exOrig.Message}")
                                    End Try

                                    ' Second try: fallback previously implemented (2048 blocks, TrackMode=1)
                                    CDAudioAnalyzer.DiagnosticWrite($"CDREAD_FALLBACK: Track={_trackNumber} currentFrame={currentFrame} - attempting alternate read (2048-byte units, TrackMode=1)")

                                    Dim rawRead2 As RAW_READ_INFO = rawRead
                                    rawRead2.DiskOffset = frameALire * 2048L
                                    rawRead2.SectorCount = CUInt(framesToRead)
                                    rawRead2.TrackMode = 1

                                    Dim bytesReturned2 As UInteger = 0
                                    Dim success2 = DeviceIoControl(_hDevice, IOCTL_CDROM_RAW_READ,
                                                                   rawRead2, CUInt(Marshal.SizeOf(rawRead2)),
                                                                   bufferHandle.AddrOfPinnedObject(), CUInt(_buffer.Length),
                                                                   bytesReturned2, IntPtr.Zero)

                                    If success2 AndAlso bytesReturned2 > 0 Then
                                        Dim nz2 As Integer = 0
                                        For i As Integer = 0 To CInt(bytesReturned2) - 1
                                            If _buffer(i) <> 0 Then nz2 += 1
                                        Next

                                        Dim dumpLen2 As Integer = Math.Min(CInt(bytesReturned2), 64)
                                        Dim sb2 As New System.Text.StringBuilder()
                                        For i As Integer = 0 To dumpLen2 - 1
                                            sb2.Append(_buffer(i).ToString("X2"))
                                            If i < dumpLen2 - 1 Then sb2.Append(" ")
                                        Next

                                        If VerboseCDReadLogging Then
                                            CDAudioAnalyzer.DiagnosticWrite($"CDREAD_FALLBACK_RESULT: Track={_trackNumber} currentFrame={currentFrame} bytesReturned={bytesReturned2} nonZeroBytes={nz2} hexFirst{dumpLen2}={sb2.ToString}")
                                        Else
                                            CDAudioAnalyzer.DiagnosticWrite($"CDREAD_FALLBACK_RESULT_SUMMARY: Track={_trackNumber} currentFrame={currentFrame} bytesReturned={bytesReturned2} nonZeroBytes={nz2}")
                                        End If

                                        If nz2 > 0 Then
                                            _bufferDataLength = CInt(bytesReturned2)
                                            _bufferPosition = 0
                                            ' Cache this successful fallback for the drive
                                            Try
                                                SyncLock readerModeCache
                                                    readerModeCache(cacheKey) = 2
                                                End SyncLock
                                            Catch
                                            End Try
                                        Else
                                            CDAudioAnalyzer.DiagnosticWrite($"CDREAD_FALLBACK_FAILED: Track={_trackNumber} currentFrame={currentFrame} - still zero-filled")
                                        End If
                                    Else
                                        CDAudioAnalyzer.DiagnosticWrite($"CDREAD_FALLBACK_ERROR: Track={_trackNumber} currentFrame={currentFrame} success2={success2} bytesReturned2={bytesReturned2}")
                                    End If
                                Catch exFb As Exception
                                    CDAudioAnalyzer.DiagnosticWrite($"CDREAD_FALLBACK_EXCEPTION: Track={_trackNumber} currentFrame={currentFrame} - {exFb.Message}")
                                End Try
                            End If
                        Catch exDiag As Exception
                            System.Diagnostics.Debug.WriteLine($"[CDReader] Diagnostic error: {exDiag.Message}")
                        End Try
                    Finally
                        bufferHandle.Free()
                    End Try
                End If

                ' Copier du buffer interne vers le buffer de sortie
                Dim bytesToCopy = Math.Min(count - totalBytesRead, _bufferDataLength - _bufferPosition)

                ' ✅ CORRECTION : S'assurer de ne JAMAIS dépasser _length
                Dim bytesRemainingInTrack = CInt(_length - _position)
                If bytesRemainingInTrack <= 0 Then
                    ' On a atteint la fin de la piste
                    Exit While
                End If

                bytesToCopy = Math.Min(bytesToCopy, bytesRemainingInTrack)

                If bytesToCopy <= 0 Then
                    Exit While
                End If

                Array.Copy(_buffer, _bufferPosition, buffer, offset + totalBytesRead, bytesToCopy)
                _bufferPosition += bytesToCopy
                _position += bytesToCopy
                totalBytesRead += bytesToCopy
            End While

            Return totalBytesRead
        End Function

        Protected Overrides Sub Dispose(disposing As Boolean)
            If disposing Then
                If _hDevice <> INVALID_HANDLE_VALUE Then
                    CloseHandle(_hDevice)
                    _hDevice = INVALID_HANDLE_VALUE
                End If
            End If
            MyBase.Dispose(disposing)
        End Sub
    End Class

#End Region

End Class
