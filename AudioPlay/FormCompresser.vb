Imports System.IO
Imports System.Management
Imports System.Runtime.InteropServices
Imports System.Diagnostics
Imports System.Globalization

Public Class FormCompresser

    ' Detected silence centers (seconds) populated after pre-analysis when detectOnlySilence mode runs
    Private detectedSilenceCenters As New List(Of Double)()

    ' Convenience map of named silence variables: "silence1" -> seconds, "silence2" -> seconds, ...
    Private _silenceVariables As New Dictionary(Of String, Double)()

    ''' <summary>
    ''' Read-only access to named silence variables (keys like "silence1", "silence2", ...)
    ''' </summary>
    Public ReadOnly Property SilenceVariables As IReadOnlyDictionary(Of String, Double)
        Get
            Return _silenceVariables
        End Get
    End Property

    ''' <summary>
    ''' Read-only access to the last detected silence centers in seconds.
    ''' </summary>
    Public ReadOnly Property SilenceCentersSeconds As List(Of Double)
        Get
            Return detectedSilenceCenters
        End Get
    End Property

    ' Flag utilisé pendant le chargement des paramètres pour éviter d'écrire
    ' les mêmes valeurs au fichier parametres.txt lors de l'initialisation
    Private loadingParams As Boolean = False

    ' Numeric control value for pre-roll in seconds (stored as double seconds)
    Private Function GetPreRollSeconds() As Double
        Try
            Dim v As Double = 1.75
            If NumericWindowPreRoll IsNot Nothing Then
                If Me IsNot Nothing AndAlso Me.IsHandleCreated AndAlso Me.InvokeRequired Then
                    Me.Invoke(Sub()
                                  v = Convert.ToDouble(NumericWindowPreRoll.Value)
                              End Sub)
                Else
                    v = Convert.ToDouble(NumericWindowPreRoll.Value)
                End If
                Return Math.Max(0.5, Math.Min(4.0, v))
            End If
        Catch
        End Try

        ' Charger la valeur sauvegardée de PreRoll depuis parametres.txt si présente
        Try
            Dim cfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "parametres.txt")
            If File.Exists(cfgPath) AndAlso NumericWindowPreRoll IsNot Nothing Then
                For Each line In File.ReadAllLines(cfgPath)
                    If line.StartsWith("Analyzer_PreRollSeconds=") Then
                        Try
                            Dim raw = line.Substring("Analyzer_PreRollSeconds=".Length).Trim()
                            Dim d As Double
                            If Double.TryParse(raw, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, d) Then
                                Dim clamped = Math.Max(0.5, Math.Min(4.0, d))
                                If Me.InvokeRequired Then
                                    Me.Invoke(Sub() NumericWindowPreRoll.Value = CDec(clamped))
                                Else
                                    NumericWindowPreRoll.Value = CDec(clamped)
                                End If
                            End If
                        Catch
                        End Try
                        Exit For
                    End If
                Next
            End If
        Catch
        End Try
        Return 1.75
    End Function

    ' API Windows pour enlever le bouton X
    Private Const SC_CLOSE As Integer = &HF060
    Private Const MF_BYCOMMAND As Integer = &H0

    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetSystemMenu(hWnd As IntPtr, bRevert As Boolean) As IntPtr
    End Function

    ' Writes a segment of sourceWav to destWav between startSec and endSec (seconds relative to source start)
    Private Sub WriteWavSegment(sourceWav As String, destWav As String, startSec As Double, endSec As Double)
        Try
            Try
                CDAudioAnalyzer.DiagnosticWrite($"WRITE_WAV_SEGMENT: src={sourceWav} dst={destWav} start={startSec:F3} end={endSec:F3}")
            Catch
            End Try

            If endSec <= startSec Then
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"WRITE_WAV_SEGMENT_EMPTY: src={sourceWav} dst={destWav} start={startSec:F3} end={endSec:F3}")
                Catch
                End Try
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"WRITE_WAV_SEGMENT_EMPTY: dst={destWav} start={startSec:F3} end={endSec:F3} Time={DateTime.UtcNow:o}")
                Catch
                End Try
                Return
            End If

            ' Ensure destination directory exists
            Try
                Dim d = Path.GetDirectoryName(destWav)
                If Not String.IsNullOrEmpty(d) AndAlso Not Directory.Exists(d) Then Directory.CreateDirectory(d)
            Catch
            End Try

            Using reader As New NAudio.Wave.AudioFileReader(sourceWav)
                Dim sampleRate = reader.WaveFormat.SampleRate
                Dim blockAlign = reader.WaveFormat.BlockAlign
                Dim startPos As Long = CLng(Math.Round(startSec * sampleRate)) * blockAlign
                Dim endPos As Long = CLng(Math.Round(endSec * sampleRate)) * blockAlign
                startPos = Math.Max(0, Math.Min(startPos, reader.Length))
                endPos = Math.Max(0, Math.Min(endPos, reader.Length))
                If endPos <= startPos Then
                    ' Nothing to write
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"WRITE_WAV_SEGMENT_NOTHING: dst={destWav} start={startSec:F3} end={endSec:F3} Time={DateTime.UtcNow:o}")
                    Catch
                    End Try
                    Return
                End If

                reader.Position = startPos
                Using writer As New NAudio.Wave.WaveFileWriter(destWav, reader.WaveFormat)
                    Dim buf(8191) As Byte
                    Dim remaining As Long = endPos - startPos
                    While remaining > 0
                        Dim toRead As Integer = CInt(Math.Min(remaining, buf.Length))
                        Dim read = reader.Read(buf, 0, toRead)
                        If read <= 0 Then Exit While
                        writer.Write(buf, 0, read)
                        remaining -= read
                    End While
                End Using
            End Using

            ' Success marker
            Try
                CDAudioAnalyzer.DiagnosticWrite($"WRITE_WAV_SEGMENT_DONE: dst={destWav} src={sourceWav} start={startSec:F3} end={endSec:F3} Time={DateTime.UtcNow:o}")
            Catch
            End Try
        Catch ex As Exception
            Try
                CDAudioAnalyzer.DiagnosticWrite($"WRITE_WAV_SEGMENT_ERROR: src={sourceWav} dst={destWav} Err={ex.Message}")
            Catch
            End Try
            Try
                CDAudioAnalyzer.DiagnosticWrite($"WRITE_WAV_SEGMENT_ERROR: dst={destWav} Err={ex.ToString()}")
            Catch
            End Try
        End Try
    End Sub

    ''' <summary>
    ''' Wait until a file stops growing (size stable) or timeout. Returns True when file appears stable and exists.
    ''' </summary>
    Private Function WaitForFileReady(path As String, Optional timeoutMs As Integer = 10000) As Boolean
        Try
            If String.IsNullOrWhiteSpace(path) Then Return False
            Dim sw As New Stopwatch()
            sw.Start()
            If Not File.Exists(path) Then
                ' wait a short time for file to appear
                Do While sw.ElapsedMilliseconds < timeoutMs AndAlso Not File.Exists(path)
                    Threading.Thread.Sleep(150)
                Loop
                If Not File.Exists(path) Then Return False
            End If

            Dim lastSize As Long = -1
            Dim stableCount As Integer = 0
            Do While sw.ElapsedMilliseconds < timeoutMs
                Try
                    Dim fi As New FileInfo(path)
                    Dim size = fi.Length
                    If size = lastSize Then
                        stableCount += 1
                        If stableCount >= 3 Then
                            Return True
                        End If
                    Else
                        stableCount = 0
                        lastSize = size
                    End If
                Catch
                    ' ignore transient IO errors
                End Try
                Threading.Thread.Sleep(200)
            Loop
            Return False
        Catch
            Return False
        End Try
    End Function

    ' Return a file-system safe filename part by removing invalid chars and trimming
    Private Function SanitizeFileNamePart(s As String) As String
        If String.IsNullOrEmpty(s) Then Return String.Empty
        Dim invalid = IO.Path.GetInvalidFileNameChars()
        Dim sb As New System.Text.StringBuilder()
        For Each ch In s
            If Array.IndexOf(invalid, ch) >= 0 Then
                sb.Append("-")
            Else
                sb.Append(ch)
            End If
        Next
        Dim result = sb.ToString().Trim()
        ' Collapse multiple spaces
        result = System.Text.RegularExpressions.Regex.Replace(result, "\s+", " ")
        Return result
    End Function

    ' Create a final trimmed WAV from a source WAV using silence center variables (silence1, silence2, ...)
    ' Returns the path to the final WAV (in destDir). Throws on failure.
    Private Function CreateFinalWavFromSource(sourceWav As String, destDir As String, artist As String, title As String, trackNumber As Integer, wavStartFrame As Integer, defaultDurationSec As Double) As String
        ' compute start/end seconds relative to source WAV start
        Dim startSecUsed As Double = 0.0
        Dim endSecUsed As Double = defaultDurationSec
        Dim convOkLocal As Boolean = False
        Dim stderrLocal As String = String.Empty
        Try
            Dim wavStartAbs As Double = wavStartFrame / 75.0
            If _silenceVariables IsNot Nothing AndAlso _silenceVariables.Count > 0 Then
                If trackNumber = 1 Then
                    startSecUsed = 0.0
                    If _silenceVariables.ContainsKey("silence1") Then
                        Dim absEnd = _silenceVariables("silence1")
                        endSecUsed = Math.Max(0.0, Math.Min(defaultDurationSec, absEnd - wavStartAbs))
                    End If
                Else
                    Dim keyStart = $"silence{trackNumber - 1}"
                    Dim keyEnd = $"silence{trackNumber}"
                    Dim absStart = wavStartAbs
                    Dim absEnd = wavStartAbs + defaultDurationSec
                    If _silenceVariables.ContainsKey(keyStart) Then absStart = _silenceVariables(keyStart)
                    If _silenceVariables.ContainsKey(keyEnd) Then absEnd = _silenceVariables(keyEnd)
                    startSecUsed = Math.Max(0.0, Math.Min(defaultDurationSec, absStart - wavStartAbs))
                    endSecUsed = Math.Max(0.0, Math.Min(defaultDurationSec, absEnd - wavStartAbs))
                End If
            ElseIf detectedSilenceCenters IsNot Nothing AndAlso detectedSilenceCenters.Count > 0 Then
                Dim cdTrackStartAbs = wavStartFrame / 75.0
                Dim absStart As Double = cdTrackStartAbs
                Dim absEnd As Double = cdTrackStartAbs + defaultDurationSec
                If trackNumber > 1 Then
                    Dim idx = trackNumber - 2
                    If idx >= 0 AndAlso idx < detectedSilenceCenters.Count Then absStart = detectedSilenceCenters(idx)
                End If
                Dim idxEnd = trackNumber - 1
                If idxEnd >= 0 AndAlso idxEnd < detectedSilenceCenters.Count Then absEnd = detectedSilenceCenters(idxEnd)
                startSecUsed = Math.Max(0.0, absStart - cdTrackStartAbs)
                endSecUsed = Math.Min(defaultDurationSec, Math.Max(0.0, absEnd - cdTrackStartAbs))
            Else
                startSecUsed = 0.0
                endSecUsed = defaultDurationSec
            End If
        Catch ex As Exception
            Try
                CDAudioAnalyzer.DiagnosticWrite($"CREATE_FINAL_WAV_ERROR: Track={trackNumber} - {ex.Message}")
            Catch
            End Try
            startSecUsed = 0.0
            endSecUsed = defaultDurationSec
        End Try

        ' prepare final filename including track number prefix NN - Artist - Title
        Dim artistSafe = SanitizeFileNamePart(artist)
        Dim titleSafe = SanitizeFileNamePart(title)
        Dim nameBody As String = If(String.IsNullOrWhiteSpace(artistSafe), titleSafe, artistSafe & " - " & titleSafe)
        If String.IsNullOrWhiteSpace(nameBody) Then nameBody = Path.GetFileNameWithoutExtension(sourceWav)
        Dim numeroFormate As String = trackNumber.ToString("D2")
        Dim baseName As String = $"{numeroFormate} - {nameBody}"
        baseName = NettoyerNomFichier(baseName)
        Dim finalWavPath = Path.Combine(destDir, baseName & ".wav")
        Try
            Directory.CreateDirectory(destDir)
        Catch
        End Try

        ' Write trimmed segment
        WriteWavSegment(sourceWav, finalWavPath, startSecUsed, endSecUsed)
        Try
            CDAudioAnalyzer.DiagnosticWrite($"CREATE_FINAL_WAV: Track={trackNumber} src={sourceWav} dst={finalWavPath} start={startSecUsed:F3} end={endSecUsed:F3}")
        Catch
        End Try

        Return finalWavPath
    End Function

    ' Determine start and end seconds for a given track number using detectedSilenceCenters.
    ' Returns a tuple (startSec, endSec). If no center data available, returns the defaults provided.
    ' wavStartFrame: optional absolute start frame of the WAV being used for extraction (if different from CD TOC)
    Private Function GetTrackCutSeconds(trackNumber As Integer, defaultStartSec As Double, defaultEndSec As Double, Optional wavStartFrame As Nullable(Of Integer) = Nothing) As (Double, Double)
        Try
            If pistesCD Is Nothing Then
                Return (defaultStartSec, defaultEndSec)
            End If

            Dim cdTrack = pistesCD.FirstOrDefault(Function(ti) ti.TrackNumber = trackNumber)
            If cdTrack Is Nothing Then
                Return (defaultStartSec, defaultEndSec)
            End If

            ' Determine the absolute start (seconds) of the WAV being used for extraction.
            ' If wavStartFrame is provided, use it (this corresponds to pisteAExtraire.StartFrame when the rip begins at an adjusted frame).
            Dim trackStartAbs As Double
            Dim trackEndAbs As Double
            If wavStartFrame.HasValue Then
                trackStartAbs = wavStartFrame.Value / 75.0
                trackEndAbs = trackStartAbs + defaultEndSec
            Else
                trackStartAbs = cdTrack.StartFrame / 75.0
                trackEndAbs = cdTrack.EndFrame / 75.0
            End If

            ' detectedSilenceCenters index: 0 -> silence between track1 and track2 (silence1)
            Dim absStart As Double = trackStartAbs
            Dim absEnd As Double = trackEndAbs

            ' For start: use previous silence centre if available
            If trackNumber > 1 Then
                Dim idx = trackNumber - 2 ' silence before this track is silence{trackNumber-1} at index trackNumber-2
                If idx >= 0 AndAlso idx < detectedSilenceCenters.Count Then
                    absStart = detectedSilenceCenters(idx)
                End If
            End If

            ' For end: use next silence centre if available
            Dim idxEnd = trackNumber - 1
            If idxEnd >= 0 AndAlso idxEnd < detectedSilenceCenters.Count Then
                absEnd = detectedSilenceCenters(idxEnd)
            End If

            ' Convert absolute CD seconds to seconds relative to the WAV (track start)
            Dim relStart As Double = Math.Max(0.0, absStart - trackStartAbs)
            Dim relEnd As Double = Math.Min(defaultEndSec, Math.Max(0.0, absEnd - trackStartAbs))
            Return (relStart, relEnd)
        Catch
            Return (defaultStartSec, defaultEndSec)
        End Try
    End Function



    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function RemoveMenu(hMenu As IntPtr, uPosition As UInteger, uFlags As UInteger) As Boolean
    End Function

    Protected Overrides Sub OnLoad(e As EventArgs)
        MyBase.OnLoad(e)
        Dim hMenu As IntPtr = GetSystemMenu(Me.Handle, False)
        RemoveMenu(hMenu, SC_CLOSE, MF_BYCOMMAND)
        ' Subscribe to language change events so menus and tooltips update dynamically
        Try
            AddHandler LanguageManager.LanguageChanged, AddressOf Me.LanguageManager_LanguageChanged
            ' Apply current language to picture box menu items/tooltips
            ApplyLanguageToPictureBoxMenu()
            ' Charger la préférence PostTraitementAuto (par défaut: activé)
            Try
                Dim cfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "parametres.txt")
                Dim val As String = Nothing
                If File.Exists(cfgPath) Then
                    For Each line In File.ReadAllLines(cfgPath)
                        If line.StartsWith("PostTraitementAuto=", StringComparison.InvariantCultureIgnoreCase) Then
                            val = line.Substring("PostTraitementAuto=".Length).Trim()
                            Exit For
                        End If
                    Next
                End If
                If CheckBoxPostTraitementAuto IsNot Nothing Then
                    If val Is Nothing Then
                        ' Option A: par défaut cochée
                        CheckBoxPostTraitementAuto.Checked = True
                        ParametresGlobauxHelpers.EcrireCleParametres("PostTraitementAuto", "1")
                    Else
                        CheckBoxPostTraitementAuto.Checked = (val = "1")
                    End If
                    If ToolTipPictureBox IsNot Nothing Then ToolTipPictureBox.SetToolTip(CheckBoxPostTraitementAuto, LanguageManager.GetString("FormCompresser_PostProcessing_AutoTip"))
                End If
                ' Charger/Synchroniser l'état de CheckBox_Affiche_CentreSilence depuis parametres.txt (par défaut: décoché)
                Try
                    If CheckBox_Affiche_CentreSilence IsNot Nothing Then
                        Dim cfgPathCS = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "parametres.txt")
                        Dim valCS As String = Nothing
                        If File.Exists(cfgPathCS) Then
                            For Each line In File.ReadAllLines(cfgPathCS)
                                If line.StartsWith("Affiche_CentreSilence=", StringComparison.InvariantCultureIgnoreCase) Then
                                    valCS = line.Substring("Affiche_CentreSilence=".Length).Trim()
                                    Exit For
                                End If
                            Next
                        End If
                        If valCS Is Nothing Then
                            ' Par défaut décoché
                            CheckBox_Affiche_CentreSilence.Checked = False
                            ParametresGlobauxHelpers.EcrireCleParametres("Affiche_CentreSilence", "0")
                        Else
                            CheckBox_Affiche_CentreSilence.Checked = (valCS = "1")
                        End If
                    End If
                Catch
                End Try
                ' Restaurer l'état agrandi/rapetissé si présent dans les paramètres
                ' Appliquer l'état agrandi/rapetissé depuis parametres.txt au démarrage de l'application
                Try
                    Dim appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay")
                    Dim cfgPathStartup = Path.Combine(appDataPath, "parametres.txt")
                    If File.Exists(cfgPathStartup) Then
                        For Each raw In File.ReadAllLines(cfgPathStartup)
                            Try
                                Dim line = raw.Trim()
                                If String.IsNullOrEmpty(line) OrElse line.StartsWith("#") Then Continue For
                                Dim idx = line.IndexOf("=")
                                If idx <= 0 Then Continue For
                                Dim key = line.Substring(0, idx).Trim()
                                Dim value = line.Substring(idx + 1).Trim()
                                If String.Equals(key, "FormCompresser_Agrandir", StringComparison.InvariantCultureIgnoreCase) Then
                                    Try
                                        ApplyAgrandirStateFromString(value)
                                    Catch
                                    End Try
                                    Exit For
                                End If
                            Catch
                            End Try
                        Next
                    End If
                    ' Ne pas utiliser FormatFormCompresser.txt — aucune suppression nécessaire ici
                Catch
                End Try
            Catch
            End Try
        Catch
        End Try
        ' Recharger les paramètres spécifiques du formulaire à chaque affichage
        ' pour garantir l'application des préférences persistées.
        Try
            ChargerParametresFormCompresser()
        Catch
        End Try
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        ' Recharger les paramètres à chaque affichage pour garantir
        ' que les préférences externes sont appliquées quand la fenêtre redevient visible
        Try
            ChargerParametresFormCompresser()
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Charge les paramètres persistés propres à FormCompresser depuis parametres.txt
    ''' et applique les valeurs à l'UI si disponibles.
    ''' </summary>
    Private Sub ChargerParametresFormCompresser()
        Try
            Dim cfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "parametres.txt")
            If Not File.Exists(cfgPath) Then Return

            loadingParams = True
            For Each raw In File.ReadAllLines(cfgPath)
                Try
                    Dim line = raw.Trim()
                    If String.IsNullOrEmpty(line) OrElse line.StartsWith("#") Then Continue For

                    If line.StartsWith("Affiche_CentreSilence=", StringComparison.InvariantCultureIgnoreCase) Then
                        Dim val = line.Substring("Affiche_CentreSilence=".Length).Trim()
                        If CheckBox_Affiche_CentreSilence IsNot Nothing Then
                            Dim checkedVal As Boolean = (val = "1")
                            If Me.InvokeRequired Then
                                Me.Invoke(Sub() CheckBox_Affiche_CentreSilence.Checked = checkedVal)
                            Else
                                CheckBox_Affiche_CentreSilence.Checked = checkedVal
                            End If
                        End If
                    ElseIf line.StartsWith("Analyzer_PreRollSeconds=", StringComparison.InvariantCultureIgnoreCase) Then
                        Dim rawVal = line.Substring("Analyzer_PreRollSeconds=".Length).Trim()
                        Dim d As Double
                        If Double.TryParse(rawVal, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, d) Then
                            Dim clamped = Math.Max(0.5, Math.Min(4.0, d))
                            If NumericWindowPreRoll IsNot Nothing Then
                                If Me.InvokeRequired Then
                                    Me.Invoke(Sub() NumericWindowPreRoll.Value = CDec(clamped))
                                Else
                                    NumericWindowPreRoll.Value = CDec(clamped)
                                End If
                            End If
                        End If
                    End If
                Catch
                End Try
            Next
            loadingParams = False
        Catch
        End Try
    End Sub

    Private Sub SafeSetLabelPisteEnCours(text As String, Optional visible As Boolean = True)
        Try
            If Me Is Nothing OrElse Me.IsDisposed Then Return
            If Not Me.IsHandleCreated Then
                ' UI handle not ready: skip updating label to avoid CreateHandle-related exceptions
                Return
            End If
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() SafeSetLabelPisteEnCours(text, visible))
                Return
            End If
            If LabelPisteEnCours Is Nothing OrElse LabelPisteEnCours.IsDisposed Then Return
            ' Set text before visibility to avoid a short empty display when toggling
            LabelPisteEnCours.Text = text
            LabelPisteEnCours.Visible = visible
        Catch
        End Try
    End Sub

    Private Sub SafeSetProgressBarMarquee(enabled As Boolean)
        Try
            If Me Is Nothing OrElse Me.IsDisposed Then Return
            If Not Me.IsHandleCreated Then
                ' UI handle not ready: skip progress updates
                Return
            End If
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() SafeSetProgressBarMarquee(enabled))
                Return
            End If
            If ProgressBarPisteActuelle Is Nothing OrElse ProgressBarPisteActuelle.IsDisposed Then Return
            If enabled Then
                ProgressBarPisteActuelle.Style = ProgressBarStyle.Marquee
                ProgressBarPisteActuelle.MarqueeAnimationSpeed = 30
                ProgressBarPisteActuelle.Visible = True
            Else
                ProgressBarPisteActuelle.Style = ProgressBarStyle.Blocks
                ProgressBarPisteActuelle.MarqueeAnimationSpeed = 0
                ' Stop and reset any ongoing animation smoothing to avoid sudden jumps
                Try
                    SyncLock progressAnimationLock
                        If progressAnimationTimer IsNot Nothing AndAlso progressAnimationTimer.Enabled Then
                            progressAnimationTimer.Stop()
                        End If
                        progressAnimationTarget = 0
                    End SyncLock
                Catch
                End Try
                Try
                    ProgressBarPisteActuelle.Value = 0
                Catch
                End Try
                ProgressBarPisteActuelle.Visible = False
            End If
        Catch
        End Try
    End Sub

    ' Validate a WAV file by attempting to open and check its length/duration
    Private Function IsValidWavFile(path As String) As Boolean
        Try
            If String.IsNullOrEmpty(path) OrElse Not File.Exists(path) Then Return False
            Using r As New NAudio.Wave.WaveFileReader(path)
                If r.Length <= 100 Then Return False
                If r.TotalTime.TotalSeconds <= 0 Then Return False
            End Using
            Return True
        Catch
            Return False
        End Try
    End Function

    ' NOTE: Application automatique via Shown removed. State is applied only when ButtonExtraction is used.

    Private Function MakeSafeFileName(input As String) As String
        If String.IsNullOrEmpty(input) Then Return ""
        Dim invalid = Path.GetInvalidFileNameChars()
        Dim sb As New System.Text.StringBuilder()
        For Each ch As Char In input
            If invalid.Contains(ch) Then
                sb.Append("_")
            Else
                sb.Append(ch)
            End If
        Next
        Return sb.ToString()
    End Function

    ' Get a normalized base name for a track file: strip common suffixes like _fixed, _fixed_debug, _orig
    Private Function BaseTrackName(inputPath As String) As String
        If String.IsNullOrEmpty(inputPath) Then Return String.Empty
        Dim name = System.IO.Path.GetFileNameWithoutExtension(inputPath)
        If String.IsNullOrEmpty(name) Then Return String.Empty
        Dim suffixes = New String() {"_fixed_debug", "_fixed", "_debug", "_orig", ".orig"}
        For Each s In suffixes
            If name.EndsWith(s, StringComparison.OrdinalIgnoreCase) Then
                name = name.Substring(0, name.Length - s.Length)
                Exit For
            End If
        Next
        Return name
    End Function

    ' Appliquer l'état agrandi/rapetissé depuis une valeur texte (ex: "1" ou "0")
    Public Sub ApplyAgrandirStateFromString(val As String)
        Try
            If String.IsNullOrEmpty(val) Then Return
            If val = "1" Then
                Agrandir = True
                Me.Size = New Size(633, 968)
                Me.StartPosition = FormStartPosition.Manual
                Dim screenCenterX = (Screen.PrimaryScreen.WorkingArea.Width - Me.Width)
                Dim screenCenterY = (Screen.PrimaryScreen.WorkingArea.Height - Me.Height)
                Me.Location = New Point(screenCenterX \ 2, screenCenterY \ 2)
                Button_Agrandir.Visible = False
                Button_rapetisser.Visible = True
                Try
                    ButtonExtraire.Location = New Point(373, 864)
                    ButtonQuitter.Location = New Point(495, 864)
                    ButtonAnnuler.Location = New Point(373, 864)
                Catch
                End Try
            Else
                Agrandir = False
                Me.Size = New Size(633, 825)
                Me.StartPosition = FormStartPosition.Manual
                Dim screenCenterX2 = (Screen.PrimaryScreen.WorkingArea.Width - Me.Width)
                Dim screenCenterY2 = (Screen.PrimaryScreen.WorkingArea.Height - Me.Height)
                Me.Location = New Point(screenCenterX2 \ 2, screenCenterY2 \ 2)
                Button_Agrandir.Visible = True
                Button_rapetisser.Visible = False
                Try
                    ButtonExtraire.Location = New Point(373, 719)
                    ButtonQuitter.Location = New Point(495, 719)
                    ButtonAnnuler.Location = New Point(373, 719)
                Catch
                End Try
            End If


        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Find WAV file in album directory by track number prefix (e.g. "01 - ")
    ''' </summary>
    Private Function FindWavByTrackNumber(albumDir As String, trackNumber As Integer) As String
        Try
            If String.IsNullOrEmpty(albumDir) OrElse Not Directory.Exists(albumDir) Then Return Nothing
            Dim prefix = trackNumber.ToString("D2") & " - "
            For Each f In Directory.GetFiles(albumDir, "*.wav")
                Dim name = Path.GetFileName(f)
                If name.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase) Then
                    Return f
                End If
            Next
            ' fallback: try prefix without padding
            prefix = trackNumber.ToString() & " - "
            For Each f In Directory.GetFiles(albumDir, "*.wav")
                Dim name = Path.GetFileName(f)
                If name.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase) Then
                    Return f
                End If
            Next
        Catch
        End Try
        Return Nothing
    End Function

    ''' <summary>
    ''' Apply conservative pairwise correction between two WAV files if needed.
    ''' Creates *_fixed.wav files next to originals when modification is performed.
    ''' </summary>
    Private Sub ApplyPairwiseCorrectionIfNeeded(prevWav As String, currWav As String, albumDir As String)
        Try
            If Not File.Exists(prevWav) OrElse Not File.Exists(currWav) Then Return

            Dim deltaMsNullable As Double? = Nothing
            Try
                deltaMsNullable = EstimateDeltaMs(prevWav, currWav, 2000, 10)
            Catch
                deltaMsNullable = Nothing
            End Try

            If Not deltaMsNullable.HasValue Then Return

            Dim deltaMs = deltaMsNullable.Value
            Dim deltaSec = deltaMs / 1000.0

            Dim logPath = Path.Combine(albumDir, "silence_corrections_log.txt")
            Dim logSb As New System.Text.StringBuilder()
            logSb.AppendLine($"Pairwise correction: prev={Path.GetFileName(prevWav)}, curr={Path.GetFileName(currWav)}, deltaMs={deltaMs:F2}")

            ' Debug: save original copies for inspection if debug flag enabled
            Try
                If debugForceCreateFixed Then
                    Try
                        Dim cpPrev = Path.Combine(albumDir, Path.GetFileNameWithoutExtension(prevWav) & ".orig.wav")
                        If Not File.Exists(cpPrev) Then File.Copy(prevWav, cpPrev)
                    Catch
                    End Try
                    Try
                        Dim cpCurr = Path.Combine(albumDir, Path.GetFileNameWithoutExtension(currWav) & ".orig.wav")
                        If Not File.Exists(cpCurr) Then File.Copy(currWav, cpCurr)
                    Catch
                    End Try
                End If
            Catch
            End Try

            Using rPrev As New NAudio.Wave.WaveFileReader(prevWav)
                Using rNext As New NAudio.Wave.WaveFileReader(currWav)
                    Dim bytesPerSec = rPrev.WaveFormat.AverageBytesPerSecond
                    Dim blockAlignPrev = Math.Max(1, rPrev.WaveFormat.BlockAlign)
                    Dim blockAlignNext = Math.Max(1, rNext.WaveFormat.BlockAlign)
                    If bytesPerSec <= 0 Then
                        logSb.AppendLine("Pairwise correction skipped: bytesPerSec <= 0")
                        Try
                            File.AppendAllText(logPath, logSb.ToString())
                        Catch
                        End Try
                        Return
                    End If

                    If deltaSec < 0 Then
                        ' Overlap: remove half from end of prev and half from start of curr
                        Dim overlap = -deltaSec
                        ' Try to determine a better cut point inside the overlap region by searching for the silence center
                        Dim bytesCutPrev As Long = 0
                        Dim bytesCutNext As Long = 0
                        Dim foundCut As Boolean = False
                        Try
                            foundCut = FindSilenceCut(prevWav, currWav, 3000, 10, blockAlignPrev, blockAlignNext, bytesCutPrev, bytesCutNext)
                        Catch
                            foundCut = False
                        End Try
                        If Not foundCut Then
                            ' fallback: split overlap evenly
                            Dim bytesHalf = CLng(Math.Round((overlap / 2.0) * bytesPerSec))
                            bytesCutPrev = (bytesHalf \ blockAlignPrev) * blockAlignPrev
                            bytesCutNext = (bytesHalf \ blockAlignNext) * blockAlignNext
                        End If

                        Dim outPrev = Path.Combine(albumDir, BaseTrackName(prevWav) & "_fixed.wav")
                        Dim outNext = Path.Combine(albumDir, BaseTrackName(currWav) & "_fixed.wav")

                        Try
                            Dim keepBytes = Math.Max(0L, rPrev.Length - bytesCutPrev)
                            keepBytes = (keepBytes \ blockAlignPrev) * blockAlignPrev
                            If keepBytes <= 0L Then
                                logSb.AppendLine($"Trim skipped for {Path.GetFileName(prevWav)}: would remove all audio ({bytesCutPrev} bytes)")
                            Else
                                Using wPrev As New NAudio.Wave.WaveFileWriter(outPrev, rPrev.WaveFormat)
                                    rPrev.Position = 0
                                    Dim buf(8191) As Byte
                                    Dim alignedBufPrev As Integer = buf.Length - (buf.Length Mod blockAlignPrev)
                                    If alignedBufPrev <= 0 Then alignedBufPrev = blockAlignPrev
                                    Dim written As Long = 0
                                    While written < keepBytes
                                        Dim toRead = CInt(Math.Min(alignedBufPrev, keepBytes - written))
                                        toRead -= toRead Mod blockAlignPrev
                                        If toRead <= 0 Then Exit While
                                        Dim read = rPrev.Read(buf, 0, toRead)
                                        If read <= 0 Then Exit While
                                        wPrev.Write(buf, 0, read)
                                        written += read
                                    End While
                                End Using
                                If IsValidWavFile(outPrev) Then
                                    logSb.AppendLine($"Trimmed end of {Path.GetFileName(prevWav)} by {bytesCutPrev} bytes -> {Path.GetFileName(outPrev)}")
                                Else
                                    Try
                                        File.Delete(outPrev)
                                    Catch
                                    End Try
                                    logSb.AppendLine($"Trim result invalid for {Path.GetFileName(prevWav)} and was deleted: {Path.GetFileName(outPrev)}")
                                End If
                            End If
                        Catch exPrev As Exception
                            logSb.AppendLine($"Failed trimming prev {Path.GetFileName(prevWav)}: {exPrev.Message}")
                        End Try

                        Try
                            Dim skip = Math.Min(rNext.Length, bytesCutNext)
                            skip = (skip \ blockAlignNext) * blockAlignNext
                            Dim remainingBytes = Math.Max(0L, rNext.Length - skip)
                            remainingBytes = (remainingBytes \ blockAlignNext) * blockAlignNext
                            If remainingBytes <= 0L Then
                                logSb.AppendLine($"Trim skipped for {Path.GetFileName(currWav)}: would remove all audio ({bytesCutNext} bytes)")
                            Else
                                ' no-op refresh: keep outNext writer block anchored
                                Using wNext As New NAudio.Wave.WaveFileWriter(outNext, rNext.WaveFormat)
                                    rNext.Position = skip
                                    Dim buf(8191) As Byte
                                    Dim alignedBufNext As Integer = buf.Length - (buf.Length Mod blockAlignNext)
                                    If alignedBufNext <= 0 Then alignedBufNext = blockAlignNext
                                    Dim remaining As Long = remainingBytes
                                    While remaining > 0
                                        Dim toRead = CInt(Math.Min(alignedBufNext, remaining))
                                        toRead -= toRead Mod blockAlignNext
                                        If toRead <= 0 Then Exit While
                                        Dim read = rNext.Read(buf, 0, toRead)
                                        If read <= 0 Then Exit While
                                        wNext.Write(buf, 0, read)
                                        remaining -= read
                                    End While
                                End Using
                                If IsValidWavFile(outNext) Then
                                    logSb.AppendLine($"Trimmed start of {Path.GetFileName(currWav)} by {bytesCutNext} bytes -> {Path.GetFileName(outNext)}")
                                Else
                                    Try
                                        File.Delete(outNext)
                                    Catch
                                    End Try
                                    logSb.AppendLine($"Trim result invalid for {Path.GetFileName(currWav)} and was deleted: {Path.GetFileName(outNext)}")
                                End If
                            End If
                        Catch exNext As Exception
                            logSb.AppendLine($"Failed trimming next {Path.GetFileName(currWav)}: {exNext.Message}")
                        End Try
                    ElseIf deltaSec > 0 Then
                        ' Gap: insert silence at end of prev
                        Try
                            Dim bytesToInsert = CLng(Math.Round(deltaSec * bytesPerSec))
                            bytesToInsert = (bytesToInsert \ blockAlignPrev) * blockAlignPrev
                            Dim outPrevGap = Path.Combine(albumDir, BaseTrackName(prevWav) & "_fixed.wav")
                            Using wPrev As New NAudio.Wave.WaveFileWriter(outPrevGap, rPrev.WaveFormat)
                                rPrev.Position = 0
                                Dim buf(8191) As Byte
                                Dim alignedBufPrev As Integer = buf.Length - (buf.Length Mod blockAlignPrev)
                                If alignedBufPrev <= 0 Then alignedBufPrev = blockAlignPrev
                                While True
                                    Dim read = rPrev.Read(buf, 0, alignedBufPrev)
                                    If read <= 0 Then Exit While
                                    wPrev.Write(buf, 0, read)
                                End While
                                Dim silenceBuf(8191) As Byte
                                Dim remaining As Long = bytesToInsert
                                While remaining > 0
                                    Dim chunk = CInt(Math.Min(remaining, silenceBuf.Length))
                                    chunk -= chunk Mod blockAlignPrev
                                    If chunk <= 0 Then Exit While
                                    wPrev.Write(silenceBuf, 0, chunk)
                                    remaining -= chunk
                                End While
                            End Using

                            If IsValidWavFile(outPrevGap) Then
                                logSb.AppendLine($"Inserted {bytesToInsert} bytes of silence at end of {Path.GetFileName(prevWav)} -> {Path.GetFileName(outPrevGap)}")
                            Else
                                Try
                                    File.Delete(outPrevGap)
                                Catch
                                End Try
                                logSb.AppendLine($"Insert-silence result invalid for {Path.GetFileName(prevWav)} and was deleted: {Path.GetFileName(outPrevGap)}")
                            End If
                        Catch exIns As Exception
                            logSb.AppendLine($"Failed inserting silence for {Path.GetFileName(prevWav)}: {exIns.Message}")
                        End Try
                    Else
                        logSb.AppendLine("Pairwise correction skipped: delta is zero")
                    End If
                End Using
            End Using

            Try
                File.AppendAllText(logPath, logSb.ToString())
            Catch
            End Try
            ' create marker to signal pairwise corrections were performed for this album
            Try
                Dim marker = Path.Combine(albumDir, ".pairwise_done")
                File.WriteAllText(marker, DateTime.UtcNow.ToString("o"))
            Catch
            End Try
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Post-traitement conservateur : utilise silence_report.txt pour découper/insérer silence
    ''' Crée des fichiers *_fixed.wav en conservant les originaux.
    ''' Silent on failure to avoid destabiliser l'UI.
    ''' </summary>
    Private Sub ApplyPostProcessingCorrections(albumDir As String)
        Try
            If String.IsNullOrEmpty(albumDir) OrElse Not Directory.Exists(albumDir) Then Return

            Dim reportPath = Path.Combine(albumDir, "silence_report.txt")
            If Not File.Exists(reportPath) Then
                ' Try to find any silence_report*.txt (e.g. pairwise temporary reports)
                Try
                    Dim candidates = Directory.GetFiles(albumDir, "silence_report*.txt")
                    If candidates IsNot Nothing AndAlso candidates.Length > 0 Then
                        ' pick the newest candidate
                        reportPath = candidates.OrderByDescending(Function(f) File.GetLastWriteTimeUtc(f)).First()
                    Else
                        Return
                    End If
                Catch
                    Return
                End Try
            End If

            Dim lines = File.ReadAllLines(reportPath)
            Dim trackInfos As New List(Of (FileName As String, Leading As Double, Trailing As Double, Duration As Double))

            For Each l In lines
                Try
                    If l.Contains(", duration=") AndAlso l.Contains("leading=") AndAlso l.Contains("trailing=") Then
                        ' format: filename, duration=xx.xx s, sr=..., ch=..., leading=0.00s, trailing=0.00s
                        Dim parts = l.Split(","c)
                        Dim fname = parts(0).Trim()
                        Dim leading = 0.0
                        Dim trailing = 0.0
                        Dim duration = 0.0
                        For Each p In parts
                            Dim t = p.Trim()
                            If t.StartsWith("leading=", StringComparison.InvariantCultureIgnoreCase) Then
                                Dim s = t.Substring("leading=".Length).Trim().TrimEnd("s"c)
                                Double.TryParse(s, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, leading)
                            ElseIf t.StartsWith("trailing=", StringComparison.InvariantCultureIgnoreCase) Then
                                Dim s = t.Substring("trailing=".Length).Trim().TrimEnd("s"c)
                                Double.TryParse(s, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, trailing)
                            ElseIf t.StartsWith("duration=", StringComparison.InvariantCultureIgnoreCase) Then
                                Dim s2 = t.Substring("duration=".Length).Trim()
                                ' duration may be like 461.13s or 461.13s
                                s2 = s2.TrimEnd("s"c)
                                Double.TryParse(s2, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, duration)
                            End If
                        Next
                        trackInfos.Add((fname, leading, trailing, duration))
                    End If
                Catch
                End Try
            Next

            If trackInfos.Count < 2 Then Return

            ' If debug mode requested, write initial report of found tracks
            Try
                If debugForceCreateFixed Then
                    Dim dbgReport = Path.Combine(albumDir, "postproc_debug_tracks.txt")
                    Using sw As New StreamWriter(dbgReport, False)
                        sw.WriteLine($"PostProcessing debug report for {albumDir} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
                        For Each t In trackInfos
                            sw.WriteLine($"{t.FileName}, leading={t.Leading}, trailing={t.Trailing}")
                        Next
                    End Using
                End If
            Catch
            End Try

            Dim logSb As New System.Text.StringBuilder()
            logSb.AppendLine($"Post-processing corrections for: {albumDir}")

            ' Implement centre->centre trimming per track using leading/trailing from analyzer
            Try
                ' Replacement: more robust centre->centre pass with local fallback FindSilenceCut
                ' If global silence centers are already populated, per-track extraction
                ' has already been trimmed centre->centre: skip *_fixed.wav post-processing.
                If detectedSilenceCenters IsNot Nothing AndAlso detectedSilenceCenters.Count > 0 Then
                    Try
                        CDAudioAnalyzer.DiagnosticWrite("SKIP_POSTPROC_FIXED: detected silence centers present - skipping creation of *_fixed.wav post-processing pass.")
                    Catch
                    End Try
                    Return
                End If
                For idx = 0 To trackInfos.Count - 1
                    Dim t = trackInfos(idx)
                    Try
                        Dim fname = t.FileName
                        Dim currPath = Path.Combine(albumDir, fname)
                        If Not File.Exists(currPath) Then Continue For

                        Dim leadingSeconds As Double = t.Leading
                        Dim trailingSeconds As Double = t.Trailing

                        ' If the external analyzer returned no silence info, attempt a local aggressive search
                        If leadingSeconds <= 0.0001 AndAlso trailingSeconds <= 0.0001 Then
                            Try
                                Dim prevIdx = idx - 1
                                If prevIdx >= 0 Then
                                    Dim prevPath = Path.Combine(albumDir, trackInfos(prevIdx).FileName)
                                    If File.Exists(prevPath) Then
                                        ' First, attempt quick local edge detection on both files
                                        Dim detectedTrailing = DetectEdgeSilenceSeconds(prevPath, True, 12000, 20)
                                        Dim detectedLeading = DetectEdgeSilenceSeconds(currPath, False, 12000, 20)
                                        If detectedTrailing > 0 OrElse detectedLeading > 0 Then
                                            trailingSeconds = Math.Max(trailingSeconds, detectedTrailing)
                                            leadingSeconds = Math.Max(leadingSeconds, detectedLeading)
                                            logSb.AppendLine($"Edge-detect for {fname}: leading={leadingSeconds:F2}s trailing={trailingSeconds:F2}s (detected)")
                                        Else
                                            ' If edge detect failed, fall back to full FindSilenceCut
                                            Dim blockAlignPrev As Integer = 0
                                            Dim blockAlignCurr As Integer = 0
                                            Using rPrev As New NAudio.Wave.WaveFileReader(prevPath)
                                                blockAlignPrev = rPrev.WaveFormat.BlockAlign
                                            End Using
                                            Using rCurr As New NAudio.Wave.WaveFileReader(currPath)
                                                blockAlignCurr = rCurr.WaveFormat.BlockAlign
                                            End Using

                                            Dim bytesCutPrev As Long = 0
                                            Dim bytesCutCurr As Long = 0
                                            ' Aggressive fallback: 8000 ms window, 10 ms hop
                                            Dim found = FindSilenceCut(prevPath, currPath, 8000, 10, blockAlignPrev, blockAlignCurr, bytesCutPrev, bytesCutCurr)
                                            If found Then
                                                Using rPrev As New NAudio.Wave.WaveFileReader(prevPath)
                                                    Using rCurr As New NAudio.Wave.WaveFileReader(currPath)
                                                        Dim secondsPrev = bytesCutPrev / (rPrev.WaveFormat.SampleRate * rPrev.WaveFormat.BlockAlign)
                                                        Dim secondsCurr = bytesCutCurr / (rCurr.WaveFormat.SampleRate * rCurr.WaveFormat.BlockAlign)
                                                        trailingSeconds = Math.Max(trailingSeconds, secondsPrev)
                                                        leadingSeconds = Math.Max(leadingSeconds, secondsCurr)
                                                    End Using
                                                End Using
                                                logSb.AppendLine($"Fallback FindSilenceCut for {fname}: leading={leadingSeconds:F2}s trailing={trailingSeconds:F2}s")
                                            Else
                                                logSb.AppendLine($"Fallback FindSilenceCut NOT found for {fname}")
                                            End If
                                        End If
                                    End If
                                End If
                            Catch exFallback As Exception
                                logSb.AppendLine($"Fallback search failed for {fname}: {exFallback.Message}")
                            End Try
                        End If

                        ' Compute centre->centre byte positions and write trimmed _Fixed.wav
                        Using readerCurr As New NAudio.Wave.WaveFileReader(currPath)
                            Dim sampleRate = readerCurr.WaveFormat.SampleRate
                            Dim blockAlign = readerCurr.WaveFormat.BlockAlign
                            Dim totalBytes = readerCurr.Length

                            ' If we have detected global silence centers (absolute seconds on CD), prefer centre->centre trimming
                            Dim keepStartBytes As Long
                            Dim keepEndBytes As Long
                            Dim usedCentreToCentre As Boolean = False
                            Try
                                If detectedSilenceCenters IsNot Nothing AndAlso detectedSilenceCenters.Count > 0 AndAlso pistesCD IsNot Nothing Then
                                    ' Try to map filename to CD track number (expecting "NN - ..." naming)
                                    Dim baseName = Path.GetFileNameWithoutExtension(currPath)
                                    Dim parts = baseName.Split(New String() {" - "}, StringSplitOptions.None)
                                    Dim trackNum As Integer = -1
                                    If parts.Length > 0 Then Integer.TryParse(parts(0), trackNum)
                                    If trackNum > 0 Then
                                        Dim cdTrack = pistesCD.FirstOrDefault(Function(trackItem) trackItem.TrackNumber = trackNum)
                                        If cdTrack IsNot Nothing Then
                                            Dim trackStartSec As Double = cdTrack.StartFrame / 75.0
                                            Dim trackEndSec As Double = cdTrack.EndFrame / 75.0

                                            ' Determine absolute CD seconds for start and end cut
                                            Dim absStartSec As Double = 0
                                            Dim absEndSec As Double = 0
                                            If trackNum = 1 Then
                                                absStartSec = trackStartSec ' start of CD/track
                                            ElseIf trackNum - 2 >= 0 AndAlso trackNum - 2 < detectedSilenceCenters.Count Then
                                                absStartSec = detectedSilenceCenters(trackNum - 2)
                                            Else
                                                absStartSec = trackStartSec
                                            End If

                                            If trackNum - 1 >= 0 AndAlso trackNum - 1 < detectedSilenceCenters.Count Then
                                                absEndSec = detectedSilenceCenters(trackNum - 1)
                                            Else
                                                absEndSec = trackEndSec
                                            End If

                                            ' Convert to seconds relative to the current WAV file
                                            Dim relStartSec = Math.Max(0.0, absStartSec - trackStartSec)
                                            Dim relEndSec = Math.Max(0.0, absEndSec - trackStartSec)

                                            keepStartBytes = CLng(Math.Round(relStartSec * sampleRate)) * blockAlign
                                            keepEndBytes = CLng(Math.Round(relEndSec * sampleRate)) * blockAlign
                                            usedCentreToCentre = True
                                        Else
                                            usedCentreToCentre = False
                                        End If
                                    End If
                                End If
                            Catch
                                usedCentreToCentre = False
                            End Try

                            If Not usedCentreToCentre Then
                                Dim keepStartSecondsRel As Double = leadingSeconds
                                Dim keepEndSecondsRel As Double = readerCurr.TotalTime.TotalSeconds - trailingSeconds
                                keepStartBytes = CLng(Math.Round(keepStartSecondsRel * sampleRate)) * blockAlign
                                keepEndBytes = CLng(Math.Round(keepEndSecondsRel * sampleRate)) * blockAlign
                            End If

                            keepStartBytes = Math.Max(0L, Math.Min(totalBytes, keepStartBytes))
                            keepEndBytes = Math.Max(0L, Math.Min(totalBytes, keepEndBytes))

                            If keepEndBytes <= keepStartBytes Then
                                logSb.AppendLine($"Invalid trim range for {fname}: start={keepStartBytes} end={keepEndBytes}")
                                Continue For
                            End If

                            Dim outName = MakeSafeFileName(BaseTrackName(currPath) & "_Fixed.wav")
                            Dim outPath = Path.Combine(albumDir, outName)

                            Try
                                Using w As New NAudio.Wave.WaveFileWriter(outPath, readerCurr.WaveFormat)
                                    readerCurr.Position = keepStartBytes
                                    Dim buf(8191) As Byte
                                    Dim remaining = keepEndBytes - keepStartBytes
                                    While remaining > 0
                                        Dim toRead = CInt(Math.Min(remaining, buf.Length))
                                        toRead -= toRead Mod blockAlign
                                        If toRead <= 0 Then Exit While
                                        Dim read = readerCurr.Read(buf, 0, toRead)
                                        If read <= 0 Then Exit While
                                        w.Write(buf, 0, read)
                                        remaining -= read
                                    End While
                                End Using

                                If IsValidWavFile(outPath) Then
                                    logSb.AppendLine($"Created fixed: {Path.GetFileName(currPath)} -> {Path.GetFileName(outPath)} (lead={leadingSeconds:F2}s trail={trailingSeconds:F2}s)")
                                Else
                                    Try
                                        File.Delete(outPath)
                                    Catch
                                    End Try
                                    logSb.AppendLine($"Fixed file invalid and deleted: {outPath}")
                                End If
                            Catch exCreate As Exception
                                logSb.AppendLine($"Failed creating fixed for {fname}: {exCreate.Message}")
                            End Try
                        End Using

                    Catch exLoop As Exception
                        logSb.AppendLine($"Post-process loop error for {t.FileName}: {exLoop.Message}")
                    End Try
                Next
            Catch
            End Try

            ' After centre->centre pass we stop further pairwise corrections to avoid duplicate work
            Try
                File.AppendAllText(Path.Combine(albumDir, "silence_corrections_log.txt"), logSb.ToString())
            Catch
            End Try

            Return

            For i = 0 To trackInfos.Count - 2
                Try
                    Dim a = trackInfos(i)
                    Dim b = trackInfos(i + 1)
                    Dim prevPath = Path.Combine(albumDir, a.FileName)
                    Dim nextPath = Path.Combine(albumDir, b.FileName)
                    If Not File.Exists(prevPath) OrElse Not File.Exists(nextPath) Then Continue For

                    ' Estimate precise delta (ms) via coarse envelope cross-correlation (frame-based)
                    Dim deltaMsNullable As Double? = Nothing
                    Try
                        deltaMsNullable = EstimateDeltaMs(prevPath, nextPath, 2000, 10) ' search ±2000ms with 10ms frames
                    Catch
                        deltaMsNullable = Nothing
                    End Try

                    Dim delta As Double
                    If deltaMsNullable.HasValue Then
                        delta = deltaMsNullable.Value / 1000.0 ' seconds
                    Else
                        delta = a.Trailing + b.Leading ' fallback to analyzer delta (seconds)
                    End If

                    ' ignore tiny differences (<5ms) unless debug forces fixed file creation
                    If Math.Abs(delta) < 0.005 Then
                        If Not debugForceCreateFixed Then Continue For
                    End If
                    If Not File.Exists(prevPath) OrElse Not File.Exists(nextPath) Then Continue For

                    ' Use bytes based on wave format
                    Using rPrev As New NAudio.Wave.WaveFileReader(prevPath)
                        Using rNext As New NAudio.Wave.WaveFileReader(nextPath)
                            Dim bytesPerSec = rPrev.WaveFormat.AverageBytesPerSecond
                            Dim blockAlignPrev = Math.Max(1, rPrev.WaveFormat.BlockAlign)
                            Dim blockAlignNext = Math.Max(1, rNext.WaveFormat.BlockAlign)
                            If bytesPerSec <= 0 Then Continue For

                            If delta < 0 Then
                                ' Overlap: remove half from end of prev and half from start of next (conservative)
                                Dim overlap = -delta
                                ' compute precise sample-based cutting using wave format block align
                                Dim bytesCutPrev = CLng(Math.Round((overlap / 2.0) * bytesPerSec))
                                Dim bytesCutNext = bytesCutPrev

                                Dim outPrev = Path.Combine(albumDir, BaseTrackName(prevPath) & "_fixed.wav")
                                Dim outNext = Path.Combine(albumDir, BaseTrackName(nextPath) & "_fixed.wav")

                                ' Trim end of prev
                                Try
                                    Dim keepBytes = Math.Max(0L, rPrev.Length - bytesCutPrev)
                                    keepBytes = (keepBytes \ blockAlignPrev) * blockAlignPrev
                                    If keepBytes <= 0L Then
                                        logSb.AppendLine($"Trim skipped for {a.FileName}: would remove all audio ({bytesCutPrev} bytes)")
                                    Else
                                        Using wPrev As New NAudio.Wave.WaveFileWriter(outPrev, rPrev.WaveFormat)
                                            rPrev.Position = 0
                                            Dim buf(8191) As Byte
                                            Dim alignedBufPrev As Integer = buf.Length - (buf.Length Mod blockAlignPrev)
                                            If alignedBufPrev <= 0 Then alignedBufPrev = blockAlignPrev
                                            Dim written As Long = 0
                                            While written < keepBytes
                                                Dim toRead = CInt(Math.Min(alignedBufPrev, keepBytes - written))
                                                toRead -= toRead Mod blockAlignPrev
                                                If toRead <= 0 Then Exit While
                                                Dim read = rPrev.Read(buf, 0, toRead)
                                                If read <= 0 Then Exit While
                                                wPrev.Write(buf, 0, read)
                                                written += read
                                            End While
                                        End Using
                                        Try
                                            Dim fiPrev = New FileInfo(outPrev)
                                            If fiPrev.Exists AndAlso fiPrev.Length <= 100 Then
                                                fiPrev.Delete()
                                                logSb.AppendLine($"Trim result for {a.FileName} was too small and was deleted ({outPrev})")
                                            Else
                                                logSb.AppendLine($"Trimmed end of {a.FileName} by {bytesCutPrev} bytes -> {Path.GetFileName(outPrev)}")
                                            End If
                                        Catch
                                        End Try
                                    End If
                                Catch exPrev As Exception
                                    logSb.AppendLine($"Failed trimming {a.FileName}: {exPrev.Message}")
                                End Try

                                ' Trim start of next
                                Try
                                    Dim skip = Math.Min(rNext.Length, bytesCutNext)
                                    skip = (skip \ blockAlignNext) * blockAlignNext
                                    Dim remainingBytes = Math.Max(0L, rNext.Length - skip)
                                    remainingBytes = (remainingBytes \ blockAlignNext) * blockAlignNext
                                    If remainingBytes <= 0L Then
                                        logSb.AppendLine($"Trim skipped for {b.FileName}: would remove all audio ({bytesCutNext} bytes)")
                                    Else
                                        Using wNext As New NAudio.Wave.WaveFileWriter(outNext, rNext.WaveFormat)
                                            rNext.Position = skip
                                            Dim buf(8191) As Byte
                                            Dim alignedBufNext As Integer = buf.Length - (buf.Length Mod blockAlignNext)
                                            If alignedBufNext <= 0 Then alignedBufNext = blockAlignNext
                                            Dim remaining As Long = remainingBytes
                                            While remaining > 0
                                                Dim toRead = CInt(Math.Min(alignedBufNext, remaining))
                                                toRead -= toRead Mod blockAlignNext
                                                If toRead <= 0 Then Exit While
                                                Dim read = rNext.Read(buf, 0, toRead)
                                                If read <= 0 Then Exit While
                                                wNext.Write(buf, 0, read)
                                                remaining -= read
                                            End While
                                        End Using
                                        Try
                                            Dim fiNext = New FileInfo(outNext)
                                            If fiNext.Exists AndAlso fiNext.Length <= 100 Then
                                                fiNext.Delete()
                                                logSb.AppendLine($"Trim result for {b.FileName} was too small and was deleted ({outNext})")
                                            Else
                                                logSb.AppendLine($"Trimmed start of {b.FileName} by {bytesCutNext} bytes -> {Path.GetFileName(outNext)}")
                                            End If
                                        Catch
                                        End Try
                                    End If
                                Catch exNext As Exception
                                    logSb.AppendLine($"Failed trimming {b.FileName}: {exNext.Message}")
                                End Try

                            ElseIf delta > 0 Then
                                ' Gap: insert silence at end of prev (conservative)
                                Dim gap = delta
                                Dim bytesToInsert = CLng(Math.Round(gap * rPrev.WaveFormat.AverageBytesPerSecond))
                                Dim outPrevGap = Path.Combine(albumDir, Path.GetFileNameWithoutExtension(prevPath) & "_fixed.wav")
                                Try
                                    Using wPrev As New NAudio.Wave.WaveFileWriter(outPrevGap, rPrev.WaveFormat)
                                        rPrev.Position = 0
                                        Dim buf(8191) As Byte
                                        Dim alignedBufPrev As Integer = buf.Length - (buf.Length Mod blockAlignPrev)
                                        If alignedBufPrev <= 0 Then alignedBufPrev = blockAlignPrev
                                        While True
                                            Dim read = rPrev.Read(buf, 0, alignedBufPrev)
                                            If read <= 0 Then Exit While
                                            wPrev.Write(buf, 0, read)
                                        End While
                                        ' write silence bytes
                                        Dim silenceBuf(8191) As Byte
                                        Dim remaining As Long = bytesToInsert
                                        While remaining > 0
                                            Dim chunk = CInt(Math.Min(remaining, silenceBuf.Length))
                                            chunk -= chunk Mod blockAlignPrev
                                            If chunk <= 0 Then Exit While
                                            wPrev.Write(silenceBuf, 0, chunk)
                                            remaining -= chunk
                                        End While
                                    End Using
                                    logSb.AppendLine($"Inserted {bytesToInsert} bytes of silence at end of {a.FileName} -> {Path.GetFileName(outPrevGap)}")
                                Catch exIns As Exception
                                    logSb.AppendLine($"Failed inserting silence for {a.FileName}: {exIns.Message}")
                                End Try
                            End If
                        End Using
                    End Using
                Catch ex As Exception
                    Try
                        ' log and continue
                    Catch
                    End Try
                End Try
            Next

            Try
                Dim outLog = Path.Combine(albumDir, "silence_corrections_log.txt")
                File.AppendAllText(outLog, logSb.ToString())
            Catch
            End Try

            ' Save debug fixed copies if debug mode enabled
            Try
                If debugForceCreateFixed Then
                    For Each f In Directory.GetFiles(albumDir, "*_fixed.wav")
                        Try
                            Dim dbg = Path.Combine(albumDir, BaseTrackName(f) & "_fixed_debug.wav")
                            If Not File.Exists(dbg) Then File.Copy(f, dbg)
                        Catch
                        End Try
                    Next
                End If
            Catch
            End Try

        Catch
            ' ne pas interrompre l'application si le post-traitement échoue
        End Try
    End Sub

    ''' <summary>
    ''' Estimate inter-track delta in milliseconds using simple frame RMS envelope cross-correlation.
    ''' Searches for best alignment within +/- searchWindowMs using frameSizeMs hopMs.
    ''' Returns positive ms if gap (prev end before next start), negative if overlap.
    ''' </summary>
    Private Function EstimateDeltaMs(prevPath As String, nextPath As String, searchWindowMs As Integer, hopMs As Integer) As Double
        Try
            Dim prevFrames = BuildEnvelope(prevPath, True, searchWindowMs, hopMs)
            Dim nextFrames = BuildEnvelope(nextPath, False, searchWindowMs, hopMs)

            If prevFrames Is Nothing OrElse nextFrames Is Nothing Then Return 0.0
            If prevFrames.Count = 0 OrElse nextFrames.Count = 0 Then Return 0.0

            ' cross-correlation to find best offset
            Dim bestCorr As Double = Double.MinValue
            Dim bestOffsetFrames As Integer = 0
            Dim maxLag = Math.Min(prevFrames.Count - 1, nextFrames.Count - 1)
            For lag = -maxLag To maxLag
                Dim corr As Double = 0
                Dim count As Integer = 0
                For i = 0 To prevFrames.Count - 1
                    Dim j = i + lag
                    If j < 0 OrElse j >= nextFrames.Count Then Continue For
                    corr += prevFrames(i) * nextFrames(j)
                    count += 1
                Next
                If count > 0 Then
                    corr /= count
                    If corr > bestCorr Then
                        bestCorr = corr
                        bestOffsetFrames = lag
                    End If
                End If
            Next

            ' bestOffsetFrames > 0 means prev frame i aligns with later next frame -> gap
            Dim deltaFrames = bestOffsetFrames
            Dim deltaMs = deltaFrames * hopMs
            Return deltaMs
        Catch
            Return 0.0
        End Try
    End Function

    Private Function BuildEnvelope(path As String, isTail As Boolean, searchWindowMs As Integer, hopMs As Integer) As List(Of Double)
        Dim env As New List(Of Double)
        Try
            Using reader As New NAudio.Wave.AudioFileReader(path)
                Dim sr = Math.Max(1, reader.WaveFormat.SampleRate)
                Dim channels = Math.Max(1, reader.WaveFormat.Channels)
                Dim hopSamples = CInt(Math.Max(1, (sr * hopMs) / 1000))
                Dim windowFrames = CInt(Math.Ceiling(searchWindowMs / CDbl(Math.Max(1, hopMs))))
                Dim bytesToRead As Long = CLng(windowFrames) * CLng(hopSamples) * CLng(reader.WaveFormat.BlockAlign)

                If isTail Then
                    reader.Position = Math.Max(0L, reader.Length - bytesToRead)
                Else
                    reader.Position = 0L
                End If

                Dim sampleProvider As NAudio.Wave.ISampleProvider = DirectCast(reader, NAudio.Wave.ISampleProvider)
                Dim sampleBuffer(hopSamples * channels - 1) As Single

                For f = 0 To windowFrames - 1
                    Dim read = sampleProvider.Read(sampleBuffer, 0, sampleBuffer.Length)
                    If read <= 0 Then Exit For

                    Dim framesRead = read \ channels
                    If framesRead <= 0 Then Exit For

                    ' Mono RMS envelope
                    Dim sumSquares As Double = 0.0
                    For i = 0 To framesRead - 1
                        Dim mono As Double = 0.0
                        For ch = 0 To channels - 1
                            Dim idx = i * channels + ch
                            If idx < read Then mono += sampleBuffer(idx)
                        Next
                        mono /= channels
                        sumSquares += mono * mono
                    Next

                    env.Add(Math.Sqrt(sumSquares / framesRead))
                Next
            End Using
        Catch
        End Try
        Return env
    End Function

    ''' <summary>
    ''' Try to find a sensible cut point inside overlap region by scanning for minimum energy (silence center).
    ''' Returns True and sets bytesCutPrev and bytesCutNext when found.
    ''' </summary>
    Private Function FindSilenceCut(prevPath As String, nextPath As String, searchWindowMs As Integer, hopMs As Integer, blockAlignPrev As Integer, blockAlignNext As Integer, ByRef bytesCutPrev As Long, ByRef bytesCutNext As Long) As Boolean
        Try
            bytesCutPrev = 0
            bytesCutNext = 0
            Dim prevEnv = BuildEnvelope(prevPath, True, searchWindowMs, hopMs)
            Dim nextEnv = BuildEnvelope(nextPath, False, searchWindowMs, hopMs)
            If prevEnv Is Nothing OrElse nextEnv Is Nothing Then Return False
            If prevEnv.Count = 0 OrElse nextEnv.Count = 0 Then Return False

            ' Find min-energy position near center of concatenation (end of prev + start of next)
            Dim bestPrevIdx = -1
            Dim bestNextIdx = -1
            Dim bestScore = Double.MaxValue
            For i = 0 To prevEnv.Count - 1
                For j = 0 To nextEnv.Count - 1
                    Dim score = prevEnv(prevEnv.Count - 1 - i) + nextEnv(j)
                    If score < bestScore Then
                        bestScore = score
                        bestPrevIdx = i
                        bestNextIdx = j
                    End If
                Next
            Next

            If bestPrevIdx >= 0 AndAlso bestNextIdx >= 0 Then
                Using rPrev As New NAudio.Wave.WaveFileReader(prevPath)
                    Using rNext As New NAudio.Wave.WaveFileReader(nextPath)
                        Dim hopSamplesPrev = CInt(Math.Max(1, (rPrev.WaveFormat.SampleRate * hopMs) / 1000))
                        Dim hopSamplesNext = CInt(Math.Max(1, (rNext.WaveFormat.SampleRate * hopMs) / 1000))

                        ' convert indices to byte offsets
                        bytesCutPrev = CLng(bestPrevIdx) * CLng(hopSamplesPrev) * CLng(rPrev.WaveFormat.BlockAlign)
                        bytesCutNext = CLng(bestNextIdx) * CLng(hopSamplesNext) * CLng(rNext.WaveFormat.BlockAlign)
                        ' align to block align
                        bytesCutPrev = (bytesCutPrev \ blockAlignPrev) * blockAlignPrev
                        bytesCutNext = (bytesCutNext \ blockAlignNext) * blockAlignNext
                        Return True
                    End Using
                End Using
            End If
        Catch
        End Try
        Return False
    End Function

    ' Detect contiguous low-energy frames at the start or end of a file.
    ' Returns detected silence duration in seconds (0 if none found).
    Private Function DetectEdgeSilenceSeconds(path As String, isTail As Boolean, searchWindowMs As Integer, frameMs As Integer) As Double
        Try
            Using reader As New NAudio.Wave.AudioFileReader(path)
                Dim sr = Math.Max(1, reader.WaveFormat.SampleRate)
                Dim channels = Math.Max(1, reader.WaveFormat.Channels)
                Dim frameSamples = CInt(Math.Max(1, (sr * frameMs) / 1000))
                Dim totalFrames = CInt(Math.Ceiling(searchWindowMs / Math.Max(1, frameMs)))

                Dim bytesToRead As Long = CLng(totalFrames) * CLng(frameSamples) * CLng(reader.WaveFormat.BlockAlign)
                If isTail Then
                    reader.Position = Math.Max(0L, reader.Length - bytesToRead)
                Else
                    reader.Position = 0L
                End If

                Dim sampleProvider As NAudio.Wave.ISampleProvider = DirectCast(reader, NAudio.Wave.ISampleProvider)
                Dim sampleBuffer(frameSamples * channels - 1) As Single
                Dim env As New List(Of Double)

                For f = 0 To totalFrames - 1
                    Dim read = sampleProvider.Read(sampleBuffer, 0, sampleBuffer.Length)
                    If read <= 0 Then Exit For
                    Dim framesRead = read \ channels
                    If framesRead <= 0 Then Exit For

                    Dim sumSquares As Double = 0.0
                    For i = 0 To framesRead - 1
                        Dim mono As Double = 0.0
                        For ch = 0 To channels - 1
                            Dim idx = i * channels + ch
                            If idx < read Then mono += sampleBuffer(idx)
                        Next
                        mono /= channels
                        sumSquares += mono * mono
                    Next

                    env.Add(Math.Sqrt(sumSquares / framesRead))
                Next

                If env.Count = 0 Then Return 0.0

                ' compute median RMS to adapt threshold
                Dim sorted = env.OrderBy(Function(x) x).ToList()
                Dim median As Double = sorted((sorted.Count \ 2))
                Dim threshold As Double = Math.Max(0.0005, median * 0.5)

                ' count consecutive low-energy frames from edge
                Dim consec As Integer = 0
                If isTail Then
                    For i = env.Count - 1 To 0 Step -1
                        If env(i) <= threshold Then
                            consec += 1
                        Else
                            Exit For
                        End If
                    Next
                Else
                    For i = 0 To env.Count - 1
                        If env(i) <= threshold Then
                            consec += 1
                        Else
                            Exit For
                        End If
                    Next
                End If

                Dim silenceMs = consec * frameMs
                ' require at least 500 ms of silence to consider it valid
                If silenceMs < 500 Then Return 0.0
                Return silenceMs / 1000.0
            End Using
        Catch
        End Try
        Return 0.0
    End Function

    ' Apply current language strings to picture box menu and tooltip
    Private Sub ApplyLanguageToPictureBoxMenu()
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() ApplyLanguageToPictureBoxMenu())
                Return
            End If

            If tsmiSearchCover IsNot Nothing Then
                tsmiSearchCover.Text = LanguageManager.GetString("Menu_SearchCover")
                ' Assign small icon at runtime to avoid designer issues
                Try
                    Dim bmpSearch As System.Drawing.Bitmap = CType(Global.AudioPlay.Resources.ResourceManager.GetObject("AudioPlay_Aide_Gris", Global.System.Globalization.CultureInfo.CurrentUICulture), System.Drawing.Bitmap)
                    If bmpSearch IsNot Nothing Then tsmiSearchCover.Image = bmpSearch
                Catch
                End Try
            End If
            If tsmiAddCoverFromFile IsNot Nothing Then
                tsmiAddCoverFromFile.Text = LanguageManager.GetString("Menu_AddCoverFromFile")
                Try
                    Dim bmpAdd As System.Drawing.Bitmap = CType(Global.AudioPlay.Resources.ResourceManager.GetObject("AudioPlay_Ajout__Noir", Global.System.Globalization.CultureInfo.CurrentUICulture), System.Drawing.Bitmap)
                    If bmpAdd IsNot Nothing Then tsmiAddCoverFromFile.Image = bmpAdd
                Catch
                End Try
            End If
            If tsmiSizeMenu IsNot Nothing Then
                tsmiSizeMenu.Text = LanguageManager.GetString("Menu_Size")
            End If
            If tsmiSizeNormal IsNot Nothing Then
                tsmiSizeNormal.Text = LanguageManager.GetString("Menu_Size_Normal")
                Try
                    Dim bmpNormal As System.Drawing.Bitmap = CType(Global.AudioPlay.Resources.ResourceManager.GetObject("AudioPlay_Vide__Carre", Global.System.Globalization.CultureInfo.CurrentUICulture), System.Drawing.Bitmap)
                    If bmpNormal IsNot Nothing Then tsmiSizeNormal.Image = bmpNormal
                Catch
                End Try
            End If
            If tsmiSizeStretch IsNot Nothing Then
                tsmiSizeStretch.Text = LanguageManager.GetString("Menu_Size_Stretch")
                Try
                    Dim bmpStretch As System.Drawing.Bitmap = CType(Global.AudioPlay.Resources.ResourceManager.GetObject("AudioPlay_X_Carre_Noir", Global.System.Globalization.CultureInfo.CurrentUICulture), System.Drawing.Bitmap)
                    If bmpStretch IsNot Nothing Then tsmiSizeStretch.Image = bmpStretch
                Catch
                End Try
            End If
            If tsmiSizeZoom IsNot Nothing Then
                tsmiSizeZoom.Text = LanguageManager.GetString("Menu_Size_Zoom")
                Try
                    Dim bmpZoom As System.Drawing.Bitmap = CType(Global.AudioPlay.Resources.ResourceManager.GetObject("AudioPlay_Vide_Rond", Global.System.Globalization.CultureInfo.CurrentUICulture), System.Drawing.Bitmap)
                    If bmpZoom IsNot Nothing Then tsmiSizeZoom.Image = bmpZoom
                Catch
                End Try
            End If
            If ToolTipPictureBox IsNot Nothing AndAlso PictureBoxPochette IsNot Nothing Then
                ToolTipPictureBox.SetToolTip(PictureBoxPochette, LanguageManager.GetString("Tooltip_PictureBox_Prompt"))
            End If
            ' Localize new controls
            Try
                If CheckBoxPostTraitementAuto IsNot Nothing Then
                    CheckBoxPostTraitementAuto.Text = LanguageManager.GetString("FormCompresser_PostProcessing_AutoLabel")
                    If ToolTipPictureBox IsNot Nothing Then ToolTipPictureBox.SetToolTip(CheckBoxPostTraitementAuto, LanguageManager.GetString("FormCompresser_PostProcessing_AutoTip"))
                End If
            Catch
            End Try
            ' ButtonCorrigerAlbum removed: corrections are now automatic via CheckBoxPostTraitementAuto
        Catch
        End Try
    End Sub

    Private Sub LanguageManager_LanguageChanged(newCulture As Globalization.CultureInfo)
        Try
            ApplyLanguageToPictureBoxMenu()
        Catch
        End Try
    End Sub

    ' ButtonCorrigerAlbum removed: manual correction is obsolete.
    ' Automatic post-processing is handled by the extraction workflow when CheckBoxPostTraitementAuto is enabled.

    Private Sub CheckBoxPostTraitementAuto_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxPostTraitementAuto.CheckedChanged
        ' store preference if ParametresGlobaux available
        Try
            If CheckBoxPostTraitementAuto.Checked Then
                ParametresGlobauxHelpers.EcrireCleParametres("PostTraitementAuto", "1")
            Else
                ParametresGlobauxHelpers.EcrireCleParametres("PostTraitementAuto", "0")
            End If
        Catch
        End Try
    End Sub

    Private Sub CheckBox_Affiche_CentreSilence_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_Affiche_CentreSilence.CheckedChanged
        Try
            If CheckBox_Affiche_CentreSilence.Checked Then
                ParametresGlobauxHelpers.EcrireCleParametres("Affiche_CentreSilence", "1")
            Else
                ParametresGlobauxHelpers.EcrireCleParametres("Affiche_CentreSilence", "0")
            End If
        Catch
        End Try
    End Sub

    Private Sub SafeSetGlobalLabel(text As String)
        Try
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() SafeSetGlobalLabel(text))
                Return
            End If
            LabelProgressionGlobale.Visible = True
            LabelProgressionGlobale.Text = text
        Catch
        End Try
    End Sub

    Private Sub SafeSetProgressBarToMax()
        Try
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() SafeSetProgressBarToMax())
                Return
            End If
            ProgressBarPisteActuelle.Value = ProgressBarPisteActuelle.Maximum
        Catch
        End Try
    End Sub

    Private Function SafeGetPictureBoxBitmap() As Image
        Try
            If Me.InvokeRequired Then
                Return CType(Me.BeginInvoke(Function()
                                                Return SafeGetPictureBoxBitmap()
                                            End Function), Image)
            End If

            If PictureBoxPochette.Image Is Nothing Then Return Nothing
            Return New Bitmap(PictureBoxPochette.Image)
        Catch
            Return Nothing
        End Try
    End Function

    Private Sub SafeInitGlobalProgressBar(maxValue As Integer)
        Try
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() SafeInitGlobalProgressBar(maxValue))
                Return
            End If
            ProgressBarGlobale.Visible = True
            ProgressBarGlobale.Minimum = 0
            ProgressBarGlobale.Maximum = Math.Max(1, maxValue)
            ProgressBarGlobale.Value = 0
        Catch
        End Try
    End Sub

    Private Sub SafeUpdateGlobalProgressBar(value As Integer)
        Try
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() SafeUpdateGlobalProgressBar(value))
                Return
            End If
            Dim v = Math.Min(ProgressBarGlobale.Maximum, Math.Max(ProgressBarGlobale.Minimum, value))
            If ProgressBarGlobale.Value <> v Then
                ProgressBarGlobale.Value = v
            End If
        Catch
        End Try
    End Sub

    ' Helpers UI thread safe
    Private Sub SafeSetPictureBoxImage(img As Image)
        Try
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() SafeSetPictureBoxImage(img))
                Return
            End If

            ' Dispose old image safely
            Try
                If PictureBoxPochette.Image IsNot Nothing Then
                    Dim oldImg = PictureBoxPochette.Image
                    PictureBoxPochette.Image = Nothing
                    oldImg.Dispose()
                End If
            Catch
            End Try

            ' Conserver l'assignation simple de l'image dans ce helper
            If img IsNot Nothing Then
                PictureBoxPochette.Image = New Bitmap(img)
                PictureBoxPochette.SizeMode = PictureBoxSizeMode.Zoom
            Else
                PictureBoxPochette.Image = Nothing
            End If

            MettreAJourInfosPochette()
            MettreAJourBoutonsNavigation()
        Catch
        End Try
    End Sub

    ' Overload pour définir explicitement le PictureBoxSizeMode lors de l'affectation
    Private Sub SafeSetPictureBoxImage(img As Image, mode As System.Windows.Forms.PictureBoxSizeMode)
        Try
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() SafeSetPictureBoxImage(img, mode))
                Return
            End If

            ' Dispose old image safely
            Try
                If PictureBoxPochette.Image IsNot Nothing Then
                    Dim oldImg = PictureBoxPochette.Image
                    PictureBoxPochette.Image = Nothing
                    oldImg.Dispose()
                End If
            Catch
            End Try

            If img IsNot Nothing Then
                PictureBoxPochette.Image = New Bitmap(img)
                PictureBoxPochette.SizeMode = mode
            Else
                PictureBoxPochette.Image = Nothing
            End If

            MettreAJourInfosPochette()
            MettreAJourBoutonsNavigation()
        Catch
        End Try
    End Sub

    Private Sub SafeClearPictureBoxImage()
        SafeSetPictureBoxImage(Nothing)
    End Sub

    Private Sub SafeInitProgressBar(maxValue As Integer)
        Try
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() SafeInitProgressBar(maxValue))
                Return
            End If
            ProgressBarPisteActuelle.Visible = True
            ProgressBarPisteActuelle.Minimum = 0
            ProgressBarPisteActuelle.Maximum = Math.Max(1, maxValue)
            ProgressBarPisteActuelle.Value = 0
        Catch
        End Try
    End Sub

    Private Sub SafeUpdateProgressBar(value As Integer)
        Try
            If Me.InvokeRequired Then
                Me.BeginInvoke(Sub() SafeUpdateProgressBar(value))
                Return
            End If
            Dim v = Math.Min(ProgressBarPisteActuelle.Maximum, Math.Max(ProgressBarPisteActuelle.Minimum, value))
            ' Smooth animation: animate the displayed value towards target to avoid large jumps
            ' Use UI timer-based smoothing for reliable painting on the UI thread
            ' Trace anchor (no-op): keeps this block stable for future instrumentation patches.
            Try
                SyncLock progressAnimationLock
                    progressAnimationTarget = v
                    If progressAnimationTimer Is Nothing Then
                        progressAnimationTimer = New System.Windows.Forms.Timer()
                        progressAnimationTimer.Interval = 80
                        AddHandler progressAnimationTimer.Tick, AddressOf ProgressAnimationTimer_Tick
                    End If
                    If Not progressAnimationTimer.Enabled Then
                        ' Trace that SafeUpdateProgressBar received a target value
                        ' Progress trace to temp file disabled by default to avoid creating clutter in %TEMP%
                        If CDAudioAnalyzer.WriteProgressTraceToDisk Then
                            Try
                                Dim tracePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                                System.IO.File.AppendAllText(tracePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] SafeUpdateProgressBar target={v}{Environment.NewLine}")
                            Catch
                            End Try
                        End If
                        progressAnimationTimer.Start()
                    End If
                End SyncLock
            Catch
                ' Fallback: direct set
                If ProgressBarPisteActuelle.Value <> v Then
                    ProgressBarPisteActuelle.Value = v
                End If
            End Try
        Catch
        End Try
    End Sub

    ' Animation helpers for progress smoothing (timer-based)
    Private progressAnimationTimer As System.Windows.Forms.Timer = Nothing
    ' FileSystemWatcher used to suppress unwanted temp trace files during extraction
    Private progressTraceWatcher As System.IO.FileSystemWatcher = Nothing
    Private progressAnimationLock As New Object()
    Private progressAnimationTarget As Integer = 0

    Private Sub ProgressAnimationTimer_Tick(sender As Object, e As EventArgs)
        Try
            SyncLock progressAnimationLock
                If progressAnimationTimer Is Nothing Then Return
                Dim cur As Integer = ProgressBarPisteActuelle.Value
                Dim tgt As Integer = progressAnimationTarget

                If cur < tgt Then
                    Dim diff = tgt - cur
                    Dim stepSize = Math.Min(8, Math.Max(1, CInt(Math.Ceiling(diff * 0.2))))
                    Dim nextV = Math.Min(ProgressBarPisteActuelle.Maximum, cur + stepSize)
                    ProgressBarPisteActuelle.Value = nextV
                    Return
                ElseIf cur > tgt Then
                    ' Snap down immediately
                    ProgressBarPisteActuelle.Value = tgt
                    ' stop timer if idle
                    If progressAnimationTimer.Enabled Then progressAnimationTimer.Stop()
                    Return
                Else
                    ' Equal - stop timer
                    If progressAnimationTimer.Enabled Then progressAnimationTimer.Stop()
                    Return
                End If
            End SyncLock
        Catch
        End Try
    End Sub

    Private Sub StartSuppressingTempTraceFiles()
        Try
            Dim tempDir = System.IO.Path.GetTempPath()
            Dim p1 = System.IO.Path.Combine(tempDir, "AudioPlay_progress_trace.txt")
            Dim p2 = System.IO.Path.Combine(tempDir, "AudioPlay_state_debug.txt")
            ' Delete existing files if present
            Try
                If File.Exists(p1) Then File.Delete(p1)
            Catch
            End Try
            Try
                If File.Exists(p2) Then File.Delete(p2)
            Catch
            End Try

            ' Create watcher that removes these files if created during extraction
            Try
                If progressTraceWatcher IsNot Nothing Then
                    Try
                        progressTraceWatcher.EnableRaisingEvents = False
                        progressTraceWatcher.Dispose()
                    Catch
                    End Try
                    progressTraceWatcher = Nothing
                End If
                progressTraceWatcher = New FileSystemWatcher(tempDir)
                ' Watch all file names in temp and selectively delete our unwanted traces
                progressTraceWatcher.Filter = "*"
                progressTraceWatcher.NotifyFilter = NotifyFilters.FileName Or NotifyFilters.LastWrite
                AddHandler progressTraceWatcher.Created, Sub(s, e)
                                                             Try
                                                                 If String.Equals(e.Name, "AudioPlay_progress_trace.txt", StringComparison.InvariantCultureIgnoreCase) OrElse String.Equals(e.Name, "AudioPlay_state_debug.txt", StringComparison.InvariantCultureIgnoreCase) Then
                                                                     Try
                                                                         File.Delete(e.FullPath)
                                                                     Catch
                                                                     End Try
                                                                 End If
                                                             Catch
                                                             End Try
                                                         End Sub
                AddHandler progressTraceWatcher.Changed, Sub(s, e)
                                                             Try
                                                                 If String.Equals(e.Name, "AudioPlay_progress_trace.txt", StringComparison.InvariantCultureIgnoreCase) OrElse String.Equals(e.Name, "AudioPlay_state_debug.txt", StringComparison.InvariantCultureIgnoreCase) Then
                                                                     Try
                                                                         File.Delete(e.FullPath)
                                                                     Catch
                                                                     End Try
                                                                 End If
                                                             Catch
                                                             End Try
                                                         End Sub
                progressTraceWatcher.IncludeSubdirectories = False
                progressTraceWatcher.EnableRaisingEvents = True
            Catch
                ' no-op if watcher cannot be created
            End Try
        Catch
        End Try
    End Sub

    Private Sub StopSuppressingTempTraceFiles()
        Try
            If progressTraceWatcher IsNot Nothing Then
                Try
                    progressTraceWatcher.EnableRaisingEvents = False
                    progressTraceWatcher.Dispose()
                Catch
                End Try
                progressTraceWatcher = Nothing
            End If
            ' Attempt to delete any leftover files once more
            Try
                Dim tempDir = System.IO.Path.GetTempPath()
                Dim p1 = System.IO.Path.Combine(tempDir, "AudioPlay_progress_trace.txt")
                Dim p2 = System.IO.Path.Combine(tempDir, "AudioPlay_state_debug.txt")
                If File.Exists(p1) Then File.Delete(p1)
                If File.Exists(p2) Then File.Delete(p2)
            Catch
            End Try
        Catch
        End Try
    End Sub

    ' API Windows pour le contrôle du lecteur CD
    <DllImport("winmm.dll", CharSet:=CharSet.Ansi)>
    Private Shared Function mciSendString(lpszCommand As String, lpszReturnString As String,
                                          cchReturnLength As Integer, hwndCallback As IntPtr) As Integer
    End Function

    ' Classe interne pour représenter un lecteur CD
    Private Class LecteurCDItem
        Public Property Lecteur As String
        Public Property ContientCD As Boolean
        Public Property NomComplet As String ' Ex: "D: PIONEER BD-RW BDR-211M 1.51 Adapter: 1 ID: 0"

        Public Sub New(lecteur As String, contientCD As Boolean, nomComplet As String)
            Me.Lecteur = lecteur
            Me.ContientCD = contientCD
            Me.NomComplet = nomComplet
        End Sub

        Public Overrides Function ToString() As String
            Return NomComplet
        End Function
    End Class

    ' Surveillance des changements de CD
    Private cdMonitorTimer As Timer
    Private derniersEtatsLecteurs As New Dictionary(Of String, Boolean)
    Private ignorerChangementsCD As Boolean = False ' Flag pour ignorer temporairement les changements de CD
    Private discIdActuel As String = Nothing ' DiscID du CD actuellement chargé pour éviter les rechargements inutiles
    ' Indique si le formulaire est agrandi (taille augmentée)
    Private Agrandir As Boolean = False

    ' Clé API Last.fm pour AudioPlay (publique, lecture seule)
    ' Si vous préférez utiliser votre propre clé gratuite, vous pouvez la modifier ici
    ' Obtenez une clé sur : https://www.last.fm/api/account/create
    Private Const LASTFM_API_KEY As String = "43693f61d26bd8a3f270320b0eeecffd"

    ' Données du CD
    Private pistesCD As List(Of CDAudioManager.CDTrack)
    Private metadonneesCD As CDMetadataProvider.CDInfo
    Private lecteurCD As String
    Private chargementInitial As Boolean = False
    Private analysesPistes As New Dictionary(Of Integer, CDAudioAnalyzer.TrackAnalysis) ' Analyses des pistes (clé = index piste)
    Private annulationDemandee As Boolean = False ' Flag pour annuler l'extraction en cours
    Private ctsExtraction As System.Threading.CancellationTokenSource = Nothing
    ' Extraction state remembered for post-processing/encoding phase
    Private lastExtractionFormat As String = "MP3"
    Private lastExtractionQualiteIndex As Integer = 0
    Private lastExtractionAutoEnabled As Boolean = False
    ' Debug: forcer la création de *_fixed.wav même si delta négligeable
    ' Disabled by default to avoid creating debug files in album folders
    Private debugForceCreateFixed As Boolean = False

    ' Cache temporaire de la pochette (sauvegardé uniquement lors de l'extraction)
    Private pochetteTempUrl As String = Nothing
    Private pochetteTempBytes As Byte() = Nothing

    ' Historique de navigation des pochettes
    Private historiquePochettes As New List(Of String) ' Liste des URLs
    Private indexPochetteActuelle As Integer = -1 ' Index dans l'historique (-1 = aucune)
    Private cachePochettesBytes As New Dictionary(Of String, Byte()) ' Cache mémoire des images téléchargées
    Private sourcesPochettes As New Dictionary(Of String, String) ' Source de chaque URL (ex: "Last.fm", "iTunes")

    ' Édition en ligne dans la ListView
    Private editTextBox As TextBox
    Private editingItem As ListViewItem
    Private editingSubItemIndex As Integer
    Private editingItemOriginalCheckedState As Boolean ' État original de la checkbox de l'item en édition

    ''' <summary>
    ''' Définit la hauteur de l'en-tête du ListView
    ''' </summary>
    ''' <summary>
    ''' Initialise les données du CD pour l'extraction
    ''' </summary>
    Public Async Sub InitialiserDonneesCD(lecteur As String, pistes As List(Of CDAudioManager.CDTrack), metadata As CDMetadataProvider.CDInfo)
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] InitialiserDonneesCD() appelé - Lecteur: {lecteur}, Pistes: {pistes?.Count}, Metadata: {If(metadata Is Nothing, "Nothing", "OK")}")
        ' Flag pour éviter le rechargement lors de la sélection initiale
        chargementInitial = True

        ' Ignorer les changements de CD pendant 5 secondes pour éviter les rechargements intempestifs
        ignorerChangementsCD = True
        Dim timerReactivation As New Timer With {.Interval = 5000} ' 5 secondes
        AddHandler timerReactivation.Tick, Sub(s, e)
                                               ignorerChangementsCD = False
                                               timerReactivation.Stop()
                                               timerReactivation.Dispose()
                                               System.Diagnostics.Debug.WriteLine("[FormCompresser] Surveillance CD réactivée")
                                           End Sub
        timerReactivation.Start()
        System.Diagnostics.Debug.WriteLine("[FormCompresser] Surveillance CD suspendue pour 5 secondes")

        lecteurCD = lecteur
        pistesCD = pistes
        metadonneesCD = metadata

        ' Calculer et sauvegarder le DiscID du CD actuel
        If pistes IsNot Nothing AndAlso pistes.Count > 0 Then
            discIdActuel = CDMetadataProvider.CalculerDiscID(pistes)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] DiscID actuel sauvegardé: {discIdActuel}")
        Else
            discIdActuel = Nothing
        End If

        ' Sélectionner le lecteur dans le ComboBox si possible
        SelectionnerLecteur(lecteur)

        Await RemplirInformationsCD()

        ' Réactiver le chargement automatique après l'initialisation
        chargementInitial = False
    End Sub

    ''' <summary>
    ''' Sélectionne le lecteur spécifié dans le ComboBox
    ''' </summary>
    Private Sub SelectionnerLecteur(lecteur As String)
        If String.IsNullOrWhiteSpace(lecteur) OrElse ComboBoxChoixLecteur.Items.Count = 0 Then
            Return
        End If

        ' Normaliser le format du lecteur (enlever le ":" si présent)
        Dim lecteurNormalise As String = lecteur.TrimEnd(":"c).ToUpper()

        ' Chercher l'item correspondant dans le ComboBox
        For i As Integer = 0 To ComboBoxChoixLecteur.Items.Count - 1
            Dim item = TryCast(ComboBoxChoixLecteur.Items(i), LecteurCDItem)
            If item IsNot Nothing Then
                Dim itemLecteur As String = item.Lecteur.TrimEnd(":"c).ToUpper()
                If itemLecteur = lecteurNormalise Then
                    ComboBoxChoixLecteur.SelectedIndex = i
                    Exit For
                End If
            End If
        Next
    End Sub

    ''' <summary>
    ''' Remplit les informations du CD dans le formulaire
    ''' </summary>
    ' Aide HTML pour les contrôles analyzer
    Private Sub OuvrirAideAnalyser(nomBase As String)
        Try
            Dim langueActuelle = LanguageManager.CurrentCulture.TwoLetterISOLanguageName.ToLower()
            Dim suffixeLangue As String = ""

            Select Case langueActuelle
                Case "fr"
                    suffixeLangue = ".fr"
                Case "en"
                    suffixeLangue = ".en"
                Case "es"
                    suffixeLangue = ".es"
                Case "de"
                    suffixeLangue = ".de"
                Case "it"
                    suffixeLangue = ".it"
                Case Else
                    suffixeLangue = ".en"
            End Select

            ' Première tentative : même convention que Form1 (fichier à la racine de l'application)
            Dim nomFichierRoot = Path.Combine(Application.StartupPath, (nomBase.ToUpper() & "_GUIDE_USER" & suffixeLangue & ".html"))

            If File.Exists(nomFichierRoot) Then
                Process.Start(New ProcessStartInfo(nomFichierRoot) With {.UseShellExecute = True})
                Return
            End If

            ' Vérification explicite du répertoire de build local (utile en dev : bin\Debug\net8.0-windows)
            Try
                Dim devOut As String = Path.GetFullPath("G:\\Visual Studio Projects\\Jean\\AudioPlay 2026-08-20\\AudioPlay\\bin\\Debug\\net8.0-windows")
                Dim devFile1 = Path.Combine(devOut, nomBase & suffixeLangue & ".html")
                Dim devFile2 = Path.Combine(devOut, (nomBase.ToUpper() & "_GUIDE_USER" & suffixeLangue & ".html"))
                If File.Exists(devFile1) Then
                    Process.Start(New ProcessStartInfo(devFile1) With {.UseShellExecute = True})
                    Return
                End If
                If File.Exists(devFile2) Then
                    Process.Start(New ProcessStartInfo(devFile2) With {.UseShellExecute = True})
                    Return
                End If
            Catch
                ' Ignorer les erreurs d'accès au chemin de développement
            End Try

            ' Deuxième tentative : nom simple à la racine (ex: WindowBefore.fr.html)
            Dim nomFichierSimpleRoot = Path.Combine(Application.StartupPath, nomBase & suffixeLangue & ".html")
            If File.Exists(nomFichierSimpleRoot) Then
                Process.Start(New ProcessStartInfo(nomFichierSimpleRoot) With {.UseShellExecute = True})
                Return
            End If

            ' Troisième tentative : structure help/FormCompresser (fichiers ajoutés dans le projet)
            Dim nomFichier = Path.Combine(Application.StartupPath, "help", "FormCompresser", nomBase & suffixeLangue & ".html")
            If File.Exists(nomFichier) Then
                Process.Start(New ProcessStartInfo(nomFichier) With {.UseShellExecute = True})
                Return
            End If

            ' Quatrième tentative : convention alternative (nomBase_GUIDE_USER) dans help/FormCompresser
            Dim nomFichierAlt = Path.Combine(Application.StartupPath, "help", "FormCompresser", (nomBase.ToUpper() & "_GUIDE_USER" & suffixeLangue & ".html"))
            If File.Exists(nomFichierAlt) Then
                Process.Start(New ProcessStartInfo(nomFichierAlt) With {.UseShellExecute = True})
                Return
            End If

            ' Troisième tentative : rechercher le fichier dans les répertoires parents (utile en mode debug où les fichiers peuvent être à la racine du projet)
            Try
                Dim found As String = Nothing
                Dim start As String = Application.StartupPath
                For depth As Integer = 0 To 4
                    Dim root As String = Path.GetFullPath(Path.Combine(start, String.Concat(Enumerable.Repeat(".." & Path.DirectorySeparatorChar, depth))))
                    If Directory.Exists(root) Then
                        Dim pattern As String = nomBase & "*" & suffixeLangue & ".html"
                        Dim files = Directory.GetFiles(root, pattern, SearchOption.AllDirectories)
                        If files IsNot Nothing AndAlso files.Length > 0 Then
                            found = files(0)
                            Exit For
                        End If
                    End If
                Next

                If Not String.IsNullOrEmpty(found) Then
                    Process.Start(New ProcessStartInfo(found) With {.UseShellExecute = True})
                    Return
                End If
            Catch exSearch As Exception
                ' Ignorer les erreurs de recherche et tomber sur le message d'erreur standard
            End Try

            Dim msg As String = LanguageManager.GetString("Help_FilesNotFound") & Environment.NewLine &
                                LanguageManager.GetString("Help_ExpectedFiles") & Environment.NewLine &
                                "- " & nomFichier

            ' Titre: utiliser Help_Title si disponible, sinon Help_FilesNotFoundTitle, sinon texte par défaut
            Dim titre As String = LanguageManager.GetString("Help_Title")
            If String.IsNullOrWhiteSpace(titre) OrElse titre.StartsWith("[RESX introuvable", StringComparison.OrdinalIgnoreCase) Then
                titre = LanguageManager.GetString("Help_FilesNotFoundTitle")
            End If
            If String.IsNullOrWhiteSpace(titre) OrElse titre.StartsWith("[RESX introuvable", StringComparison.OrdinalIgnoreCase) Then
                titre = "Aide"
            End If

            MessageBox.Show(msg, titre, MessageBoxButtons.OK, MessageBoxIcon.Warning)

        Catch ex As Exception
            MessageBox.Show(LanguageManager.GetString("Help_ErrorOpenFile", ex.Message),
                            LanguageManager.GetString("Error_Title"),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Button_Aide_WindowBefore_Click(sender As Object, e As EventArgs) Handles Button_Aide_WindowBefore.Click
        OuvrirAideAnalyser("WindowBefore")
    End Sub

    Private Sub Button_Aide_WindowAfter_Click(sender As Object, e As EventArgs) Handles Button_Aide_WindowAfter.Click
        OuvrirAideAnalyser("WindowAfter")
    End Sub

    Private Sub Button_Aide_MinSilence_Click(sender As Object, e As EventArgs) Handles Button_Aide_MinSilence.Click
        OuvrirAideAnalyser("MinSilence")
    End Sub

    Private Sub Button_Aide_MaxStartTrim_Click(sender As Object, e As EventArgs) Handles Button_Aide_MaxStartTrim.Click
        OuvrirAideAnalyser("MaxStartTrim")
    End Sub

    Private Sub Button_Agrandir_Click(sender As Object, e As EventArgs) Handles Button_Agrandir.Click
        Try
            If Not Agrandir Then
                ' Taille agrandie demandée
                Me.SuspendLayout()
                Me.Size = New Size(633, 968)
                Me.StartPosition = FormStartPosition.Manual
                Dim screenCenterX = (Screen.PrimaryScreen.WorkingArea.Width - Me.Width)
                Dim screenCenterY = (Screen.PrimaryScreen.WorkingArea.Height - Me.Height)
                Me.Location = New Point(screenCenterX \ 2, screenCenterY \ 2)
                Agrandir = True
                Button_Agrandir.Visible = False
                Button_rapetisser.Visible = True
                ' Déplacer les boutons selon la taille agrandie
                Try
                    ButtonExtraire.Location = New Point(373, 864)
                    ButtonQuitter.Location = New Point(495, 864)
                    ButtonAnnuler.Location = New Point(373, 864)
                Catch
                End Try

                ' Réaffecter explicitement les textes localisés pour s'assurer qu'ils utilisent la culture courante
                Try
                    GroupBoxAnalyzerOptions.Text = LanguageManager.GetString("GroupBoxAnalyzerOptions_Text")
                    LabelWindowBefore.Text = LanguageManager.GetString("LabelWindowBefore_Text")
                    LabelWindowAfter.Text = LanguageManager.GetString("LabelWindowAfter_Text")
                    LabelMinSilence.Text = LanguageManager.GetString("LabelMinSilence_Text")
                    LabelMaxStartTrim.Text = LanguageManager.GetString("LabelMaxStartTrim_Text")
                Catch ex As Exception
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur affectation textes localisés: {ex.Message}")
                End Try
                Me.ResumeLayout()
                ' Sauvegarder l'état agrandi
                Try
                    ParametresGlobauxHelpers.EcrireCleParametres("FormCompresser_Agrandir", "1")
                    SaveFormatFormCompresser("1")
                Catch ex As Exception
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"PARAM_WRITE_DEBUG: Button_Agrandir write exception: {ex.Message}")
                    Catch
                    End Try
                End Try
            End If
        Catch ex As Exception
            Debug.WriteLine($"[FormCompresser] Button_Agrandir_Click erreur: {ex.Message}")
        End Try
    End Sub

    Private Sub Button_rapetisser_Click(sender As Object, e As EventArgs) Handles Button_rapetisser.Click
        Try
            If Agrandir Then
                Me.SuspendLayout()
                Me.Size = New Size(633, 825)
                Me.StartPosition = FormStartPosition.Manual
                Dim screenCenterX = (Screen.PrimaryScreen.WorkingArea.Width - Me.Width)
                Dim screenCenterY = (Screen.PrimaryScreen.WorkingArea.Height - Me.Height)
                Me.Location = New Point(screenCenterX \ 2, screenCenterY \ 2)
                Agrandir = False
                Button_Agrandir.Visible = True
                Button_rapetisser.Visible = False
                ' Repositionner les boutons pour la taille par défaut
                Try
                    ButtonExtraire.Location = New Point(373, 719)
                    ButtonQuitter.Location = New Point(495, 719)
                    ButtonAnnuler.Location = New Point(373, 719)
                Catch
                End Try
                Me.ResumeLayout()
                ' Sauvegarder l'état rapetissé
                Try
                    ParametresGlobauxHelpers.EcrireCleParametres("FormCompresser_Agrandir", "0")
                    SaveFormatFormCompresser("0")
                Catch ex As Exception
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"PARAM_WRITE_DEBUG: Button_rapetisser write exception: {ex.Message}")
                    Catch
                    End Try
                End Try
            End If
        Catch ex As Exception
            Debug.WriteLine($"[FormCompresser] Button_rapetisser_Click erreur: {ex.Message}")
        End Try
    End Sub

    Private Sub FormCompresser_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Try
            ' Ensure persistent state saved on close
            Dim val = If(Agrandir, "1", "0")
            ParametresGlobauxHelpers.EcrireCleParametres("FormCompresser_Agrandir", val)
            Try
                CDAudioAnalyzer.DiagnosticWrite($"PARAM_WRITE_DEBUG: FormClosing wrote FormCompresser_Agrandir={val}")
            Catch
            End Try
        Catch
        End Try
    End Sub

    ' Enregistrer un fichier de format test séparé (FormatFormCompresser.txt) pour débogage/validation
    Private Sub SaveFormatFormCompresser(value As String)
        Try
            Dim appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay")
            If Not Directory.Exists(appData) Then Directory.CreateDirectory(appData)
            Dim f = Path.Combine(appData, "FormatFormCompresser.txt")
            File.WriteAllText(f, value)
        Catch
        End Try
    End Sub

    ''' <param name="chargerPochette">Si True, charge la pochette du CD. Si False, conserve la pochette existante.</param>
    Private Async Function RemplirInformationsCD(Optional chargerPochette As Boolean = True) As Task
        Dim stackTrace = New System.Diagnostics.StackTrace(True)
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ══════════════════════════════════════════════")
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] RemplirInformationsCD() appelé depuis:")
        System.Diagnostics.Debug.WriteLine($"[FormCompresser]   {stackTrace.GetFrame(1)?.GetMethod()?.Name}")
        System.Diagnostics.Debug.WriteLine($"[FormCompresser]   chargerPochette = {chargerPochette}")
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ══════════════════════════════════════════════")

        ' Réinitialiser le cache temporaire de la pochette SEULEMENT si on charge une nouvelle pochette
        If chargerPochette Then
            pochetteTempUrl = Nothing
            pochetteTempBytes = Nothing
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Cache pochette temporaire réinitialisé")
        End If

        ' Vérifier si on a au moins des pistes
        If pistesCD Is Nothing OrElse pistesCD.Count = 0 Then
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ERREUR: pistesCD est Nothing ou vide!")
            ' Vider tous les champs
            TextBoxCDTitre.Text = ""
            TextBoxCDArtiste.Text = ""
            TextBoxAnnee.Text = ""
            TextBoxCommentaire.Text = ""
            ComboBoxGenre.SelectedIndex = -1
            ListViewCompress.Items.Clear()
            SafeClearPictureBoxImage()
            Return
        End If

        System.Diagnostics.Debug.WriteLine($"[FormCompresser] pistesCD OK - {pistesCD.Count} piste(s)")

        ' Si on a des métadonnées, remplir les champs
        If metadonneesCD IsNot Nothing Then
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] metadonneesCD OK - Artist: {metadonneesCD.Artist}, Album: {metadonneesCD.Album}")

            ' Remplir les champs de texte
            TextBoxCDTitre.Text = If(String.IsNullOrWhiteSpace(metadonneesCD.Album), "", metadonneesCD.Album)
            TextBoxCDArtiste.Text = If(String.IsNullOrWhiteSpace(metadonneesCD.Artist), "", metadonneesCD.Artist)
            TextBoxAnnee.Text = If(metadonneesCD.Year > 0, metadonneesCD.Year.ToString(), "")
            TextBoxCommentaire.Text = LanguageManager.GetString("Compressor_DefaultComment") ' CDInfo n'a pas de commentaire par défaut

            ' Sélectionner le genre dans le ComboBox
            If Not String.IsNullOrWhiteSpace(metadonneesCD.Genre) Then
                Dim genreIndex As Integer = ComboBoxGenre.Items.IndexOf(metadonneesCD.Genre)
                If genreIndex >= 0 Then
                    ComboBoxGenre.SelectedIndex = genreIndex
                End If
            End If

            ' Charger la pochette de l'album SEULEMENT si demandé
            If chargerPochette Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] >>> APPEL ChargerPochetteAlbum() - metadonneesCD IsNot Nothing = {metadonneesCD IsNot Nothing}")
                If metadonneesCD IsNot Nothing Then
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] >>> Artist: {metadonneesCD.Artist}, Album: {metadonneesCD.Album}")
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] >>> CoverArtUrl: {If(String.IsNullOrWhiteSpace(metadonneesCD.CoverArtUrl), "VIDE", metadonneesCD.CoverArtUrl)}")
                End If
                Await ChargerPochetteAlbum()
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] >>> ChargerPochetteAlbum() TERMINÉ")
            Else
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ChargerPochetteAlbum() IGNORÉ (même CD)")
            End If
        Else
            ' Pas de métadonnées, vider les champs mais garder des valeurs par défaut
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] metadonneesCD est Nothing, valeurs par défaut")
            TextBoxCDTitre.Text = "CD Audio sans titre"
            TextBoxCDArtiste.Text = "Artiste inconnu"
            TextBoxAnnee.Text = ""
            TextBoxCommentaire.Text = LanguageManager.GetString("Compressor_DefaultComment")
            ComboBoxGenre.SelectedIndex = -1
            SafeClearPictureBoxImage()
        End If

        ' Remplir le ListView avec les pistes (avec ou sans métadonnées)
        RemplirListViewPistes()
    End Function

    ''' <summary>
    ''' Remplit le ListView avec les pistes du CD
    ''' </summary>
    Private Sub RemplirListViewPistes()
        ListViewCompress.Items.Clear()

        ' On affiche les pistes même sans métadonnées (avec valeurs par défaut)
        If pistesCD Is Nothing OrElse pistesCD.Count = 0 Then
            Return
        End If

        ' Obtenir le numéro de départ depuis le TextBox
        Dim premierNumero As Integer = 1
        If Not String.IsNullOrWhiteSpace(TextBoxPremierNumPiste.Text) Then
            Integer.TryParse(TextBoxPremierNumPiste.Text, premierNumero)
            If premierNumero < 1 Then premierNumero = 1
        End If

        Dim positionActuelle As TimeSpan = TimeSpan.Zero
        Dim numeroActuel As Integer = premierNumero

        For Each piste In pistesCD
            Dim item As New ListViewItem()

            ' Colonne 1: Numéro de piste (en utilisant le numéro personnalisé)
            item.Text = numeroActuel.ToString()
            numeroActuel += 1

            ' Colonne 2: Titre
            Dim titre As String = piste.Title
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Piste {piste.TrackNumber}: Titre initial = '{titre}'")
            If metadonneesCD IsNot Nothing AndAlso metadonneesCD.Tracks IsNot Nothing Then
                Dim metaPiste = metadonneesCD.Tracks.FirstOrDefault(Function(p) p.TrackNumber = piste.TrackNumber)
                If metaPiste IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(metaPiste.Title) Then
                    titre = metaPiste.Title
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Piste {piste.TrackNumber}: Titre remplacé par métadonnées = '{titre}'")
                End If
            End If

            ' Si le titre est toujours vide, afficher "Piste XX [mm:ss]" comme dans FormSelecteurPistesCD
            If String.IsNullOrWhiteSpace(titre) Then
                Dim dureeFormat = TimeSpan.FromSeconds(piste.Duration.TotalSeconds)
                titre = $"Piste {piste.TrackNumber:D2} [{dureeFormat:mm\:ss}]"
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Piste {piste.TrackNumber}: Titre par défaut = '{titre}'")
            End If

            item.SubItems.Add(titre)

            ' Colonne 3: Artiste (même que l'album par défaut)
            Dim artiste As String = If(String.IsNullOrWhiteSpace(metadonneesCD?.Artist), piste.Artist, metadonneesCD.Artist)
            If metadonneesCD IsNot Nothing AndAlso metadonneesCD.Tracks IsNot Nothing Then
                Dim metaPiste = metadonneesCD.Tracks.FirstOrDefault(Function(p) p.TrackNumber = piste.TrackNumber)
                If metaPiste IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(metaPiste.Artist) Then
                    artiste = metaPiste.Artist
                End If
            End If

            ' Si l'artiste est toujours vide, afficher "Artiste inconnu"
            If String.IsNullOrWhiteSpace(artiste) Then
                artiste = "Artiste inconnu"
            End If

            item.SubItems.Add(artiste)

            ' Colonne 4: Début (position sur le CD)
            Dim debut As String = String.Format("{0:D2}:{1:D2}", CInt(positionActuelle.TotalMinutes), positionActuelle.Seconds)
            item.SubItems.Add(debut)

            ' Colonne 5: Longueur
            Dim duree As TimeSpan = piste.Duration
            Dim longueur As String = String.Format("{0:D2}:{1:D2}", CInt(duree.TotalMinutes), duree.Seconds)
            item.SubItems.Add(longueur)

            ' Colonne 6: Taille originale (PCM 44.1kHz 16-bit stéréo)
            ' 44100 Hz * 2 canaux * 2 octets/échantillon = 176400 octets/seconde
            Dim tailleOriginale As Double = piste.Duration.TotalSeconds * 176400.0 / (1024.0 * 1024.0) ' En Mo
            item.SubItems.Add(String.Format("{0:F2} Mo", tailleOriginale))

            ' Colonne 7: Taille compressée (estimée)
            Dim tailleCompressee As Double = CalculerTailleCompressee(piste.Duration.TotalSeconds)
            item.SubItems.Add(String.Format("{0:F2} Mo", tailleCompressee))

            ' Cocher la piste par défaut pour l'extraction
            item.Checked = True

            ListViewCompress.Items.Add(item)

            ' Mettre à jour la position pour la prochaine piste
            positionActuelle = positionActuelle.Add(duree)
        Next
    End Sub

    ''' <summary>
    ''' Calcule la taille estimée d'un fichier compressé selon le format et la qualité choisis
    ''' </summary>
    Private Function CalculerTailleCompressee(durationSeconds As Double) As Double
        Dim typeConversion As String = If(ComboBoxTypeConversion.SelectedItem IsNot Nothing, ComboBoxTypeConversion.SelectedItem.ToString(), "MP3")
        Dim qualiteIndex As Integer = ComboBoxQualiteConversion.SelectedIndex

        ' Déterminer le débit en kbps selon le format et l'index de qualité
        Dim debitKbps As Integer = 192 ' Valeur par défaut

        Select Case typeConversion.ToUpper()
            Case "MP3"
                ' Index 0=128, 1=192, 2=256, 3=320
                Select Case qualiteIndex
                    Case 0 ' Basse (128 kbps)
                        debitKbps = 128
                    Case 1 ' Moyenne (192 kbps)
                        debitKbps = 192
                    Case 2 ' Haute (256 kbps)
                        debitKbps = 256
                    Case 3 ' Très haute (320 kbps)
                        debitKbps = 320
                    Case Else ' Fallback
                        debitKbps = 320
                End Select

            Case "FLAC"
                ' FLAC: compression sans perte, environ 50-60% de la taille originale
                ' Index 0=Niveau 0, 1=Niveau 5, 2=Niveau 8
                Select Case qualiteIndex
                    Case 0 ' Niveau 0 (rapide)
                        debitKbps = CInt(1411.2 * 0.6)
                    Case 1 ' Niveau 5 (équilibré)
                        debitKbps = CInt(1411.2 * 0.55)
                    Case 2 ' Niveau 8 (meilleur)
                        debitKbps = CInt(1411.2 * 0.5)
                    Case Else ' Fallback
                        debitKbps = CInt(1411.2 * 0.5)
                End Select

            Case "WMA"
                ' Index 0=128, 1=192, 2=256
                Select Case qualiteIndex
                    Case 0 ' 128 kbps
                        debitKbps = 128
                    Case 1 ' 192 kbps
                        debitKbps = 192
                    Case 2 ' 256 kbps
                        debitKbps = 256
                    Case Else ' Fallback
                        debitKbps = 256
                End Select

            Case "WAV"
                ' WAV: selon la qualité choisie
                ' Index 0=16-bit 44.1kHz, 1=24-bit 96kHz, 2=32-bit 192kHz
                Select Case qualiteIndex
                    Case 0 ' PCM 16-bit 44.1 kHz
                        debitKbps = CInt(1411.2) ' 44.1kHz * 16-bit * 2 canaux
                    Case 1 ' PCM 24-bit 96 kHz
                        debitKbps = 4608 ' Environ 4.6 Mbps
                    Case 2 ' PCM 32-bit 192 kHz
                        debitKbps = 12288 ' Environ 12 Mbps
                    Case Else ' Fallback
                        debitKbps = 4608
                End Select
        End Select

        ' Calcul: (débit en kbps * durée en secondes) / 8 / 1024 = taille en Mo
        Dim tailleMo As Double = (debitKbps * durationSeconds) / 8.0 / 1024.0
        Return tailleMo
    End Function

    ''' <summary>
    ''' Applique les traductions à tous les contrôles du formulaire
    ''' </summary>
    Private Sub AppliquerTraductions()
        ' Titre du formulaire
        Me.Text = LanguageManager.GetString("Compressor_FormTitle")

        ' Labels
        Label_ChoixLecteur.Text = LanguageManager.GetString("Compressor_DriveSelection")
        Label_CDTitre.Text = LanguageManager.GetString("Compressor_CDTitle")
        LabelCDArtiste.Text = LanguageManager.GetString("Compressor_CDArtist")
        LabelAnnee.Text = LanguageManager.GetString("Compressor_Year")
        LabelGenre.Text = LanguageManager.GetString("Compressor_Genre")
        Label3.Text = LanguageManager.GetString("Compressor_CoverArt")
        LabelDimImagText.Text = LanguageManager.GetString("Compressor_ImageDimensions")
        LabelTailleImagText.Text = LanguageManager.GetString("Compressor_ImageSize")
        LabelNumCD.Text = LanguageManager.GetString("Compressor_CDNumber")
        LabelPremierNumPiste.Text = LanguageManager.GetString("Compressor_FirstTrackNumber")
        LabelCommentaire.Text = LanguageManager.GetString("Compressor_Comment")
        LabelTypeConversion.Text = LanguageManager.GetString("Compressor_ConversionType")
        LabelQualiteConversion.Text = LanguageManager.GetString("Compressor_ConversionQuality")
        LabelRepSauvegarde.Text = LanguageManager.GetString("Compressor_SaveDirectory")
        Label1.Text = LanguageManager.GetString("FCompress_Label_ImageDimensions")
        Label2.Text = LanguageManager.GetString("FCompress_Label_ImageSize")
        Label_Im_Site.Text = LanguageManager.GetString("FCompress_Label_ImageSite")
        Label_Normalisation.Text = LanguageManager.GetString("FCompress_Label_Normalization")

        ' Boutons
        ButtonRepSauvegarde.Text = LanguageManager.GetString("Compressor_ButtonBrowse")
        ButtonExtraire.Text = LanguageManager.GetString("Compressor_ButtonExtract")
        ButtonAnnuler.Text = LanguageManager.GetString("Compressor_ButtonCancel")
        ButtonQuitter.Text = LanguageManager.GetString("Compressor_ButtonQuit")
        Button_EditTracks.Text = LanguageManager.GetString("Compressor_ButtonEditTracks")
        ButtonSoumettreGnuDB.Text = LanguageManager.GetString("GnuDB_ButtonSubmit")

        ' CheckBoxes
        CheckBoxEjectCD.Text = LanguageManager.GetString("Compressor_EjectCD")
        CheckBoxVerouillerCD.Text = LanguageManager.GetString("Compressor_LockCD")
        CheckBox_FCompress_SelectDeselect.Text = LanguageManager.GetString("Compressor_SelectDeselectAll")
        Try
            If CheckBox_Affiche_CentreSilence IsNot Nothing Then
                CheckBox_Affiche_CentreSilence.Text = LanguageManager.GetString("FormCompresser_ShowSilenceCenters")
            End If
        Catch
        End Try

        ' ListView colonnes
        ColumnHeaderPiste.Text = LanguageManager.GetString("Compressor_ColumnTrack")
        ColumnHeaderTitre.Text = LanguageManager.GetString("Compressor_ColumnTitle")
        ColumnHeaderArtiste.Text = LanguageManager.GetString("Compressor_ColumnArtist")
        ColumnHeaderDébut.Text = LanguageManager.GetString("Compressor_ColumnStart")
        ColumnHeaderLongueur.Text = LanguageManager.GetString("Compressor_ColumnLength")
        ColumnHeaderTaille.Text = LanguageManager.GetString("Compressor_ColumnSize")
        ColumnHeaderTailleComp.Text = LanguageManager.GetString("Compressor_ColumnCompressedSize")
    End Sub

    ''' <summary>
    ''' Chargement du formulaire
    ''' </summary>
    Private Sub FormCompresser_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Appliquer les traductions
        AppliquerTraductions()
        ' S'abonner aux changements de langue pour rafraîchir automatiquement
        Try
            AddHandler LanguageManager.LanguageChanged, AddressOf OnLanguageChanged
        Catch ex As Exception
        End Try

        ' Configurer le ComboBoxChoixLecteur AVANT d'appliquer le thème
        ' pour éviter que le ThemeManager ne change son DrawMode
        ComboBoxChoixLecteur.DrawMode = DrawMode.OwnerDrawFixed
        AddHandler ComboBoxChoixLecteur.DrawItem, AddressOf ComboBoxChoixLecteur_DrawItem
        AddHandler ComboBoxChoixLecteur.SelectedIndexChanged, AddressOf ComboBoxChoixLecteur_SelectedIndexChanged

        ' Appliquer le thème au formulaire (les autres contrôles seront gérés automatiquement)
        ThemeManager.ApplyThemeToForm(Me)

        ' Initialiser la couleur d'accent pour les CustomProgressBar (par défaut: couleur du thème 'Par défaut')
        Try
            Dim accent As Color = My.Settings.AccentColor
            ProgressBarPisteActuelle.FillColor = accent
            ProgressBarGlobale.FillColor = accent
        Catch
            ' Si échoue, laisser les couleurs par défaut du CustomProgressBar
        End Try

        ' Ajouter les gestionnaires pour le ListView
        AddHandler ListViewCompress.DrawColumnHeader, AddressOf ListViewCompress_DrawColumnHeader
        AddHandler ListViewCompress.DrawItem, AddressOf ListViewCompress_DrawItem
        AddHandler ListViewCompress.DrawSubItem, AddressOf ListViewCompress_DrawSubItem

        ' Ajouter les gestionnaires pour recalculer les tailles ET pour remplir ComboBoxQualiteConversion
        ' IMPORTANT: Ajouter ces gestionnaires AVANT de définir SelectedIndex pour qu'ils se déclenchent
        AddHandler ComboBoxTypeConversion.SelectedIndexChanged, AddressOf ComboBoxTypeConversion_SelectedIndexChanged
        AddHandler ComboBoxTypeConversion.SelectedIndexChanged, AddressOf RecalculerTailles
        AddHandler ComboBoxQualiteConversion.SelectedIndexChanged, AddressOf RecalculerTailles

        ' Définir les valeurs par défaut des ComboBox
        ' Maintenant que le gestionnaire est attaché, cela va déclencher le remplissage de ComboBoxQualiteConversion
        If ComboBoxTypeConversion.Items.Count > 0 Then
            ' Chercher "MP3" dans les items
            Dim mp3Index As Integer = ComboBoxTypeConversion.Items.IndexOf("MP3")
            If mp3Index >= 0 Then
                ComboBoxTypeConversion.SelectedIndex = mp3Index
            Else
                ComboBoxTypeConversion.SelectedIndex = 0 ' Si "MP3" n'est pas trouvé, sélectionner le premier
            End If
        End If

        ' Ajouter le gestionnaire pour le TextBoxPremierNumPiste
        AddHandler TextBoxPremierNumPiste.TextChanged, AddressOf TextBoxPremierNumPiste_TextChanged
        AddHandler TextBoxPremierNumPiste.KeyPress, AddressOf TextBoxPremierNumPiste_KeyPress

        ' Ajouter le gestionnaire pour le bouton de sélection du répertoire
        AddHandler ButtonRepSauvegarde.Click, AddressOf ButtonRepSauvegarde_Click

        ' Ajouter le gestionnaire pour sauvegarder le volume d'extraction quand il change
        AddHandler NumericUpDown_DB.ValueChanged, AddressOf NumericUpDown_DB_ValueChanged

        ' Charger le répertoire de sauvegarde depuis les paramètres
        ChargerRepertoireSauvegarde()

        ' Charger les lecteurs CD
        ChargerLecteursCD()

        ' Démarrer la surveillance des lecteurs
        InitialiserSurveillanceLecteurs()

        ' Vérifier la disponibilité de FFMpeg (en arrière-plan, sans bloquer l'interface)
        Task.Run(Sub() VerifierFFMpegDisponibilite())

        ' NOTE: ChargerPochetteAlbum() est appelé depuis RemplirInformationsCD()
        ' après que metadonneesCD soit initialisé via InitialiserDonneesCD()

        ' Hook up handlers for analyzer UI controls if present
        Try
            AddHandler NumericWindowBefore.ValueChanged, AddressOf AnalyzerControl_ValueChanged
            AddHandler NumericWindowAfter.ValueChanged, AddressOf AnalyzerControl_ValueChanged
            AddHandler NumericMinSilence.ValueChanged, AddressOf AnalyzerControl_ValueChanged
            AddHandler NumericMaxStartTrim.ValueChanged, AddressOf AnalyzerControl_ValueChanged
            ' Hook up pre-roll control and help button
            If NumericWindowPreRoll IsNot Nothing Then
                AddHandler NumericWindowPreRoll.ValueChanged, AddressOf AnalyzerControl_ValueChanged
            End If
            If ButtonHelpPreRoll IsNot Nothing Then
                AddHandler ButtonHelpPreRoll.Click, AddressOf ButtonHelpPreRoll_Click
            End If
            ' Handlers to remember previous values and validate on leave
            AddHandler NumericWindowBefore.Enter, AddressOf AnalyzerControl_Enter
            AddHandler NumericWindowAfter.Enter, AddressOf AnalyzerControl_Enter
            AddHandler NumericMinSilence.Enter, AddressOf AnalyzerControl_Enter
            AddHandler NumericMaxStartTrim.Enter, AddressOf AnalyzerControl_Enter
            AddHandler NumericWindowBefore.Leave, AddressOf AnalyzerControl_Leave
            AddHandler NumericWindowAfter.Leave, AddressOf AnalyzerControl_Leave
            AddHandler NumericMinSilence.Leave, AddressOf AnalyzerControl_Leave
            AddHandler NumericMaxStartTrim.Leave, AddressOf AnalyzerControl_Leave
        Catch
        End Try

        ' S'assurer de la taille par défaut au démarrage (forcé ici pour éviter état persistant)
        Try
            Agrandir = False
            Me.Size = New Size(633, 825)
            Button_Agrandir.Visible = True
            Button_rapetisser.Visible = False
            ' Recentrer sur l'écran de la fenêtre (support multi-écrans)
            Dim wa = Screen.FromControl(Me).WorkingArea
            Me.StartPosition = FormStartPosition.Manual
            Me.Location = New Point(wa.Left + (wa.Width - Me.Width) \ 2, wa.Top + (wa.Height - Me.Height) \ 2)
            ' Positionner les boutons pour la taille par défaut
            Try
                ButtonExtraire.Location = New Point(373, 719)
                ButtonQuitter.Location = New Point(495, 719)
            Catch
            End Try
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Impossible de forcer la taille par défaut: {ex.Message}")
        End Try
    End Sub

    Private Sub OnLanguageChanged(newCulture As Globalization.CultureInfo)
        Try
            If Me.InvokeRequired Then
                Me.BeginInvoke(New Action(Sub() AppliquerTraductions()))
            Else
                AppliquerTraductions()
            End If
        Catch
        End Try
    End Sub

    ' Handler to apply analyzer control changes to CDAudioAnalyzer
    Private Sub AnalyzerControl_ValueChanged(sender As Object, e As EventArgs)
        Try
            Dim ctrl = TryCast(sender, NumericUpDown)
            If ctrl IsNot Nothing Then
                Select Case ctrl.Name
                    Case "NumericWindowBefore"
                        Dim v As Integer = CInt(ctrl.Value)
                        Dim clamped As Integer = Math.Max(5, Math.Min(120, v))
                        If clamped <> v Then ctrl.Value = clamped
                        CDAudioAnalyzer.TransitionWindowBeforeSeconds = clamped
                        ' Persist user change
                        Try
                            If Not loadingParams Then ParametresGlobauxHelpers.EcrireCleParametres("Analyzer_WindowBeforeSeconds", clamped.ToString())
                        Catch
                        End Try

                    Case "NumericWindowAfter"
                        Dim v2 As Integer = CInt(ctrl.Value)
                        Dim clamped2 As Integer = Math.Max(5, Math.Min(120, v2))
                        If clamped2 <> v2 Then ctrl.Value = clamped2
                        CDAudioAnalyzer.TransitionWindowAfterSeconds = clamped2
                        ' Persist user change
                        Try
                            If Not loadingParams Then ParametresGlobauxHelpers.EcrireCleParametres("Analyzer_WindowAfterSeconds", clamped2.ToString())
                        Catch
                        End Try

                    Case "NumericWindowPreRoll"
                        ' Pre-roll value in seconds (0.50 - 4.00). Persist for future sessions.
                        Dim raw As Decimal = ctrl.Value
                        Dim clampedPre As Decimal = Math.Max(CDec(0.5D), Math.Min(CDec(4D), raw))
                        If clampedPre <> raw Then ctrl.Value = clampedPre
                        ' Persist user change with invariant culture for decimal separator
                        Try
                            If Not loadingParams Then ParametresGlobauxHelpers.EcrireCleParametres("Analyzer_PreRollSeconds", clampedPre.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        Catch
                        End Try

                    Case "NumericMinSilence"
                        Dim vd As Double = Convert.ToDouble(ctrl.Value)
                        Dim clampedD As Double = Math.Max(0.05, Math.Min(10.0, vd))
                        If Math.Abs(clampedD - vd) > 0.000001 Then ctrl.Value = CDec(clampedD)
                        CDAudioAnalyzer.MinSustainedSilenceSeconds = clampedD
                        ' Persist user change (use invariant culture)
                        Try
                            If Not loadingParams Then ParametresGlobauxHelpers.EcrireCleParametres("Analyzer_MinSustainedSilenceSeconds", clampedD.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        Catch
                        End Try

                    Case "NumericMaxStartTrim"
                        Dim vd2 As Double = Convert.ToDouble(ctrl.Value)
                        Dim clampedD2 As Double = Math.Max(0.0, Math.Min(10.0, vd2))
                        If Math.Abs(clampedD2 - vd2) > 0.000001 Then ctrl.Value = CDec(clampedD2)
                        CDAudioAnalyzer.MaxStartTrimSeconds = clampedD2
                        ' Persist user change (use invariant culture)
                        Try
                            If Not loadingParams Then ParametresGlobauxHelpers.EcrireCleParametres("Analyzer_MaxStartTrimSeconds", clampedD2.ToString(System.Globalization.CultureInfo.InvariantCulture))
                        Catch
                        End Try

                    Case Else
                        ' nothing
                End Select
            Else
                ' Fallback: apply all values with clamping
                Dim nbefore = Math.Max(5, Math.Min(120, CInt(NumericWindowBefore.Value)))
                Dim nafter = Math.Max(5, Math.Min(120, CInt(NumericWindowAfter.Value)))
                Dim nmin = Math.Max(0.05, Math.Min(10.0, Convert.ToDouble(NumericMinSilence.Value)))
                Dim nmax = Math.Max(0.0, Math.Min(10.0, Convert.ToDouble(NumericMaxStartTrim.Value)))
                CDAudioAnalyzer.TransitionWindowBeforeSeconds = nbefore
                CDAudioAnalyzer.TransitionWindowAfterSeconds = nafter
                CDAudioAnalyzer.MinSustainedSilenceSeconds = nmin
                CDAudioAnalyzer.MaxStartTrimSeconds = nmax
                ' Persist all
                Try
                    ParametresGlobauxHelpers.EcrireCleParametres("Analyzer_WindowBeforeSeconds", nbefore.ToString())
                    ParametresGlobauxHelpers.EcrireCleParametres("Analyzer_WindowAfterSeconds", nafter.ToString())
                    ParametresGlobauxHelpers.EcrireCleParametres("Analyzer_MinSustainedSilenceSeconds", nmin.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    ParametresGlobauxHelpers.EcrireCleParametres("Analyzer_MaxStartTrimSeconds", nmax.ToString(System.Globalization.CultureInfo.InvariantCulture))
                Catch
                End Try
            End If

            CDAudioAnalyzer.DiagnosticWrite($"Analyzer UI updated: before={CDAudioAnalyzer.TransitionWindowBeforeSeconds}, after={CDAudioAnalyzer.TransitionWindowAfterSeconds}, minSilence={CDAudioAnalyzer.MinSustainedSilenceSeconds}, maxStartTrim={CDAudioAnalyzer.MaxStartTrimSeconds}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] AnalyzerControl_ValueChanged error: {ex.Message}")
        End Try
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        Try
            RemoveHandler LanguageManager.LanguageChanged, AddressOf OnLanguageChanged
        Catch
        End Try
        MyBase.OnFormClosed(e)
    End Sub

    ' Store last valid values to revert if user enters out-of-range values
    Private lastValidWindowBefore As Integer = 20
    Private lastValidWindowAfter As Integer = 20
    Private lastValidMinSilence As Decimal = 0.5D
    Private lastValidMaxStartTrim As Decimal = 8D

    Private Sub AnalyzerControl_Enter(sender As Object, e As EventArgs)
        Dim ctrl = TryCast(sender, NumericUpDown)
        If ctrl Is Nothing Then Return
        Select Case ctrl.Name
            Case "NumericWindowBefore"
                lastValidWindowBefore = CInt(ctrl.Value)
            Case "NumericWindowAfter"
                lastValidWindowAfter = CInt(ctrl.Value)
            Case "NumericMinSilence"
                lastValidMinSilence = ctrl.Value
            Case "NumericMaxStartTrim"
                lastValidMaxStartTrim = ctrl.Value
        End Select
    End Sub

    Private Sub AnalyzerControl_Leave(sender As Object, e As EventArgs)
        Dim ctrl = TryCast(sender, NumericUpDown)
        If ctrl Is Nothing Then Return
        Select Case ctrl.Name
            Case "NumericWindowBefore"
                Dim v As Integer = CInt(ctrl.Value)
                If v < 5 OrElse v > 120 Then ctrl.Value = lastValidWindowBefore
            Case "NumericWindowAfter"
                Dim v2 As Integer = CInt(ctrl.Value)
                If v2 < 5 OrElse v2 > 120 Then ctrl.Value = lastValidWindowAfter
            Case "NumericMinSilence"
                Dim d As Double = Convert.ToDouble(ctrl.Value)
                If d < 0.05 OrElse d > 10.0 Then ctrl.Value = lastValidMinSilence
            Case "NumericMaxStartTrim"
                Dim d2 As Double = Convert.ToDouble(ctrl.Value)
                If d2 < 0.0 OrElse d2 > 10.0 Then ctrl.Value = lastValidMaxStartTrim
        End Select
    End Sub

    ''' <summary>
    ''' Vérifie si FFMpeg est disponible et affiche une info discrète si absent
    ''' </summary>
    Private Async Sub VerifierFFMpegDisponibilite()
        Await Task.Delay(500) ' Petit délai pour laisser le formulaire se charger

        If Not FFMpegManager.EstInstalle() Then
            ' Afficher une info discrète (non bloquante)
            If Me.InvokeRequired Then
                Me.Invoke(Sub() AfficherInfoFFMpegAbsent())
            Else
                AfficherInfoFFMpegAbsent()
            End If
        End If
    End Sub

    ' NOTE: FormCompresser_Load already defined earlier; move hookup code into existing load handler.

    Private Sub ButtonHelpPreRoll_Click(sender As Object, e As EventArgs)
        Try
            ' Open the HTML help page for PreRoll using the same helper as other help buttons
            OuvrirAideAnalyser("PreRoll")
        Catch
        End Try
    End Sub

    ''' <summary>
    ''' Affiche une notification discrète que FFMpeg n'est pas installé
    ''' </summary>
    Private Sub AfficherInfoFFMpegAbsent()
        ' On pourrait afficher un label discret en bas du formulaire
        ' Pour l'instant, on ne fait rien - l'utilisateur sera averti lors de l'extraction FLAC/WMA
        System.Diagnostics.Debug.WriteLine("[FormCompresser] FFMpeg n'est pas installé - téléchargement sera proposé lors de l'extraction FLAC/WMA")
    End Sub

    ''' <summary>
    ''' Recalcule les tailles compressées quand le format ou la qualité change
    ''' </summary>
    Private Sub RecalculerTailles(sender As Object, e As EventArgs)
        If pistesCD IsNot Nothing AndAlso pistesCD.Count > 0 Then
            RemplirListViewPistes()
        End If
    End Sub

    ''' <summary>
    ''' Met à jour les options de qualité selon le format d'extraction sélectionné
    ''' </summary>
    Private Sub ComboBoxTypeConversion_SelectedIndexChanged(sender As Object, e As EventArgs)
        If ComboBoxTypeConversion.SelectedItem Is Nothing Then Return

        Dim formatSelectionne As String = ComboBoxTypeConversion.SelectedItem.ToString().ToUpper()

        ' Suspendre la mise à jour de l'interface
        ComboBoxQualiteConversion.BeginUpdate()
        ComboBoxQualiteConversion.Items.Clear()

        Select Case formatSelectionne
            Case "MP3"
                ' Options MP3
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityLow"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityMedium"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityHigh"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityVeryHigh"))
                ' Par défaut : Très haute
                ComboBoxQualiteConversion.SelectedIndex = 3

            Case "FLAC"
                ' Options FLAC
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityFlacLevel0"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityFlacLevel5"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityFlacLevel8"))
                ' Par défaut : Niveau 8
                ComboBoxQualiteConversion.SelectedIndex = 2

            Case "WAV"
                ' Options WAV
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWav16"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWav24"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWav32"))
                ' Par défaut : PCM 24-bit 96 kHz
                ComboBoxQualiteConversion.SelectedIndex = 1

            Case "WMA"
                ' Options WMA
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWma128"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWma192"))
                ComboBoxQualiteConversion.Items.Add(LanguageManager.GetString("Compressor_QualityWma256"))
                ' Par défaut : 256 kbps
                ComboBoxQualiteConversion.SelectedIndex = 2

            Case Else
                ' Fallback si format inconnu (ne devrait pas arriver)
                ComboBoxQualiteConversion.Items.Add("Standard")
                ComboBoxQualiteConversion.SelectedIndex = 0
        End Select

        ' Reprendre la mise à jour de l'interface
        ComboBoxQualiteConversion.EndUpdate()
    End Sub

    ''' <summary>
    ''' Obtient la liste des pistes sélectionnées pour l'extraction
    ''' </summary>
    ''' <summary>
    ''' Retourne les indices des pistes sélectionnées dans le ListView
    ''' </summary>
    Public Function ObtenirIndicesPistesSelectionnees() As List(Of Integer)
        Dim indices As New List(Of Integer)()

        For i As Integer = 0 To ListViewCompress.Items.Count - 1
            If ListViewCompress.Items(i).Checked Then
                indices.Add(i)
            End If
        Next

        Return indices
    End Function

    ''' <summary>
    ''' Retourne les pistes sélectionnées (pour compatibilité)
    ''' </summary>
    Public Function ObtenirPistesSelectionnees() As List(Of CDAudioManager.CDTrack)
        Dim pistesSelectionnees As New List(Of CDAudioManager.CDTrack)()

        If pistesCD Is Nothing OrElse ListViewCompress.Items.Count = 0 Then
            Return pistesSelectionnees
        End If

        ' Parcourir strictement les pistesCD pour éviter de tronquer les pistes au-delà de ListView
        For i As Integer = 0 To pistesCD.Count - 1
            If i < ListViewCompress.Items.Count AndAlso ListViewCompress.Items(i).Checked Then
                pistesSelectionnees.Add(pistesCD(i))
            End If
        Next

        Return pistesSelectionnees
    End Function

    ''' <summary>
    ''' Coche ou décoche toutes les pistes
    ''' </summary>
    Public Sub CocherToutesPistes(cocher As Boolean)
        For Each item As ListViewItem In ListViewCompress.Items
            item.Checked = cocher
        Next
    End Sub

    ''' <summary>
    ''' Charge le répertoire de sauvegarde depuis les paramètres
    ''' </summary>
    Private Sub ChargerRepertoireSauvegarde()
        If Not String.IsNullOrWhiteSpace(ParametresGlobaux.repertoireExtractionCD) AndAlso
           Directory.Exists(ParametresGlobaux.repertoireExtractionCD) Then
            TextBoxRepSauvegarde.Text = ParametresGlobaux.repertoireExtractionCD
        Else
            ' Par défaut : Musique de l'utilisateur
            TextBoxRepSauvegarde.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)
            ParametresGlobaux.repertoireExtractionCD = TextBoxRepSauvegarde.Text
            ' Sauvegarder immédiatement dans le fichier parametres.txt
            ParametresGlobauxHelpers.EcrireCleParametres("RepertoireExtractionCD", TextBoxRepSauvegarde.Text)
        End If

        ' Charger le volume d'extraction depuis les paramètres (1-100, défaut 95)
        NumericUpDown_DB.Value = ParametresGlobaux.volumeExtractionCD
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton de sélection du répertoire de sauvegarde
    ''' </summary>
    Private Sub ButtonRepSauvegarde_Click(sender As Object, e As EventArgs)
        Try
            Using folderDialog As New FolderBrowserDialog()
                folderDialog.Description = "Sélectionnez le répertoire de destination pour l'extraction des pistes CD"
                folderDialog.ShowNewFolderButton = True

                ' Utiliser le répertoire actuel comme point de départ
                If Not String.IsNullOrWhiteSpace(TextBoxRepSauvegarde.Text) AndAlso
                   Directory.Exists(TextBoxRepSauvegarde.Text) Then
                    folderDialog.SelectedPath = TextBoxRepSauvegarde.Text
                End If

                If folderDialog.ShowDialog() = DialogResult.OK Then
                    ' Mettre à jour le TextBox
                    TextBoxRepSauvegarde.Text = folderDialog.SelectedPath

                    ' Sauvegarder dans les paramètres globaux
                    ParametresGlobaux.repertoireExtractionCD = folderDialog.SelectedPath

                    ' Sauvegarder immédiatement dans le fichier parametres.txt
                    ParametresGlobauxHelpers.EcrireCleParametres("RepertoireExtractionCD", folderDialog.SelectedPath)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Erreur lors de la sélection du répertoire: {ex.Message}",
                          "Erreur",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    '''<summary>
    ''' Gestionnaire pour sauvegarder le volume d'extraction quand l'utilisateur le modifie
    '''</summary>
    Private Sub NumericUpDown_DB_ValueChanged(sender As Object, e As EventArgs)
        ' Sauvegarder la nouvelle valeur dans les paramètres globaux
        If Not loadingParams Then
            ParametresGlobaux.volumeExtractionCD = CInt(NumericUpDown_DB.Value)

            ' Sauvegarder immédiatement dans le fichier parametres.txt
            ParametresGlobauxHelpers.EcrireCleParametres("VolumeExtractionCD", ParametresGlobaux.volumeExtractionCD.ToString())

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Volume d'extraction sauvegardé: {ParametresGlobaux.volumeExtractionCD}%")
        End If
    End Sub

    ''' <summary>
    ''' Gestionnaire pour valider et rafraîchir le ListView quand le numéro de piste change
    ''' </summary>
    Private Sub TextBoxPremierNumPiste_TextChanged(sender As Object, e As EventArgs)
        ' Valider et rafraîchir uniquement si la valeur est valide ET qu'un CD est chargé
        If pistesCD Is Nothing OrElse pistesCD.Count = 0 OrElse metadonneesCD Is Nothing Then
            Return
        End If

        Dim numero As Integer = 1
        If Not String.IsNullOrWhiteSpace(TextBoxPremierNumPiste.Text) AndAlso
           Integer.TryParse(TextBoxPremierNumPiste.Text, numero) AndAlso
           numero >= 1 Then
            ' Valeur valide, rafraîchir le ListView
            RemplirListViewPistes()
        End If
    End Sub

    ''' <summary>
    ''' Gestionnaire pour valider la saisie (n'accepter que les chiffres et Entrée)
    ''' </summary>
    Private Sub TextBoxPremierNumPiste_KeyPress(sender As Object, e As KeyPressEventArgs)
        ' Autoriser seulement les chiffres, backspace et Entrée
        If Not Char.IsDigit(e.KeyChar) AndAlso e.KeyChar <> ChrW(Keys.Back) AndAlso e.KeyChar <> ChrW(Keys.Enter) Then
            e.Handled = True
        End If

        ' Si l'utilisateur appuie sur Entrée, valider et rafraîchir
        If e.KeyChar = ChrW(Keys.Enter) Then
            e.Handled = True
            Dim numero As Integer = 1
            If String.IsNullOrWhiteSpace(TextBoxPremierNumPiste.Text) OrElse
               Not Integer.TryParse(TextBoxPremierNumPiste.Text, numero) OrElse
               numero < 1 Then
                ' Valeur invalide, remettre à 1
                TextBoxPremierNumPiste.Text = "1"
            End If
            RemplirListViewPistes()
        End If
    End Sub

    ''' <summary>
    ''' Charge dynamiquement tous les lecteurs CD dans le ComboBox
    ''' </summary>
    Private Sub ChargerLecteursCD()
        Try
            ComboBoxChoixLecteur.Items.Clear()

            ' Détecter tous les lecteurs CD-ROM
            Dim lecteurs = CDAudioManager.DetecterLecteursCDAudio()

            If lecteurs.Count = 0 Then
                ' Aucun lecteur CD trouvé
                Dim itemAucun As New LecteurCDItem("", False, "Aucun lecteur CD détecté")
                ComboBoxChoixLecteur.Items.Add(itemAucun)
                ComboBoxChoixLecteur.SelectedIndex = 0
                ComboBoxChoixLecteur.Enabled = False
                Return
            End If

            ' Ajouter chaque lecteur avec son état et ses informations détaillées
            For Each lecteur In lecteurs
                Dim contientCD = CDAudioManager.EstCDAudioPresent(lecteur)
                Dim infoDetaillees = ObtenirInfoLecteur(lecteur)
                Dim item As New LecteurCDItem(lecteur, contientCD, infoDetaillees)
                ComboBoxChoixLecteur.Items.Add(item)

                ' Sélectionner automatiquement le premier lecteur contenant un CD
                If contientCD AndAlso ComboBoxChoixLecteur.SelectedIndex = -1 Then
                    ComboBoxChoixLecteur.SelectedItem = item
                End If
            Next

            ' Si aucun CD n'a été trouvé, sélectionner le premier lecteur
            If ComboBoxChoixLecteur.SelectedIndex = -1 AndAlso ComboBoxChoixLecteur.Items.Count > 0 Then
                ComboBoxChoixLecteur.SelectedIndex = 0
            End If

        Catch ex As Exception
            MessageBox.Show($"Erreur lors du chargement des lecteurs CD: {ex.Message}",
                          "Erreur",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Dessin personnalisé des items du ComboBox
    ''' Les lecteurs sans CD sont en gris pâle, ceux avec CD en noir
    ''' </summary>
    Private Sub ComboBoxChoixLecteur_DrawItem(sender As Object, e As DrawItemEventArgs)
        If e.Index < 0 Then Return

        Dim theme = ThemeManager.GetCurrentTheme()

        ' Dessiner le fond
        Dim backgroundColor As Color
        If (e.State And DrawItemState.Selected) = DrawItemState.Selected Then
            backgroundColor = SystemColors.Highlight
        Else
            backgroundColor = theme.TextBoxBackColor
        End If

        Using brush As New SolidBrush(backgroundColor)
            e.Graphics.FillRectangle(brush, e.Bounds)
        End Using

        Dim item = TryCast(ComboBoxChoixLecteur.Items(e.Index), LecteurCDItem)
        If item IsNot Nothing Then
            ' Couleur selon l'état du lecteur
            Dim textColor As Color
            If (e.State And DrawItemState.Selected) = DrawItemState.Selected Then
                textColor = If(item.ContientCD, SystemColors.HighlightText, Color.Silver)
            Else
                ' Utiliser les couleurs du thème, mais griser les lecteurs vides
                textColor = If(item.ContientCD, theme.TextBoxForeColor, Color.Gray)
            End If

            Using brush As New SolidBrush(textColor)
                e.Graphics.DrawString(item.ToString(), e.Font, brush, e.Bounds)
            End Using
        End If

        e.DrawFocusRectangle()
    End Sub

    ''' <summary>
    ''' Empêche la sélection d'un lecteur sans CD (silencieusement)
    ''' </summary>
    Private previousIndex As Integer = -1
    Private Sub ComboBoxChoixLecteur_SelectedIndexChanged(sender As Object, e As EventArgs)
        Dim item = TryCast(ComboBoxChoixLecteur.SelectedItem, LecteurCDItem)

        If item IsNot Nothing AndAlso Not item.ContientCD Then
            ' Lecteur sans CD sélectionné - empêcher la sélection silencieusement
            If previousIndex >= 0 AndAlso previousIndex < ComboBoxChoixLecteur.Items.Count Then
                ' Revenir à la sélection précédente
                ComboBoxChoixLecteur.SelectedIndex = previousIndex
            Else
                ' Chercher le premier lecteur avec CD
                For i As Integer = 0 To ComboBoxChoixLecteur.Items.Count - 1
                    Dim testItem = TryCast(ComboBoxChoixLecteur.Items(i), LecteurCDItem)
                    If testItem IsNot Nothing AndAlso testItem.ContientCD Then
                        ComboBoxChoixLecteur.SelectedIndex = i
                        Return
                    End If
                Next
            End If
            ' Ne plus afficher de message - juste empêcher la sélection
        Else
            ' Mémoriser la sélection valide
            previousIndex = ComboBoxChoixLecteur.SelectedIndex

            ' Charger les métadonnées du nouveau CD si un lecteur valide est sélectionné
            ' (sauf lors du chargement initial où les données sont déjà fournies)
            If item IsNot Nothing AndAlso item.ContientCD AndAlso Not chargementInitial Then
                ChargerMetadonneesNouveauCD(item.Lecteur)
            End If
        End If
    End Sub

    ''' <summary>
    ''' Charge les métadonnées d'un nouveau CD depuis GnuDB
    ''' </summary>
    Private Async Sub ChargerMetadonneesNouveauCD(lecteur As String)
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ChargerMetadonneesNouveauCD() appelé pour {lecteur}")

            ' Lire les pistes du CD
            Dim pistes = CDAudioManager.LirePistesCD(lecteur)

            If pistes Is Nothing OrElse pistes.Count = 0 Then
                ' Pas de pistes disponibles
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Aucune piste détectée, réinitialisation")
                pistesCD = Nothing
                metadonneesCD = Nothing
                lecteurCD = lecteur
                discIdActuel = Nothing
                Await RemplirInformationsCD()
                Return
            End If

            ' Calculer le DiscID du nouveau CD
            Dim nouveauDiscId = CDMetadataProvider.CalculerDiscID(pistes)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] DiscID détecté: {nouveauDiscId}, DiscID actuel: {If(discIdActuel, "NULL")}")

            ' IMPORTANT: Ne recharger QUE si le DiscID a changé (CD différent)
            If Not String.IsNullOrWhiteSpace(nouveauDiscId) AndAlso
               Not String.IsNullOrWhiteSpace(discIdActuel) AndAlso
               nouveauDiscId = discIdActuel Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⏩ Même CD détecté (DiscID identique), aucun rechargement nécessaire")
                Return
            End If

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Nouveau CD détecté, chargement des métadonnées...")

            ' Mettre à jour le lecteur courant
            lecteurCD = lecteur
            pistesCD = pistes
            discIdActuel = nouveauDiscId

            ' Charger les métadonnées depuis GnuDB
            Dim provider = New GnuDBMetadataProvider()
            metadonneesCD = Await provider.RechercherCD(pistes)

            ' Si les métadonnées n'ont pas été trouvées, essayer le cache
            If metadonneesCD Is Nothing Then
                Dim discId = CDMetadataProvider.CalculerDiscID(pistes)
                If Not String.IsNullOrWhiteSpace(discId) Then
                    metadonneesCD = CDMetadataCache.RecupererMetadonnees(discId)
                End If
            Else
                ' Sauvegarder dans le cache
                Dim discId = CDMetadataProvider.CalculerDiscID(pistes)
                If Not String.IsNullOrWhiteSpace(discId) Then
                    CDMetadataCache.SauvegarderMetadonnees(discId, metadonneesCD, "GnuDB")
                End If
            End If

            ' Appliquer les métadonnées aux pistes si disponibles
            If metadonneesCD IsNot Nothing AndAlso metadonneesCD.Tracks IsNot Nothing Then
                For i = 0 To Math.Min(pistes.Count - 1, metadonneesCD.Tracks.Count - 1)
                    Dim piste = pistes(i)
                    Dim trackInfo = metadonneesCD.Tracks(i)

                    piste.Title = trackInfo.Title
                    piste.Artist = If(Not String.IsNullOrWhiteSpace(trackInfo.Artist), trackInfo.Artist, metadonneesCD.Artist)
                Next
            End If

            ' Remplir le formulaire avec les données (toujours recharger la pochette)
            Await RemplirInformationsCD(chargerPochette:=True)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ CD chargé, pochette rechargée")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur chargement métadonnées pour {lecteur}: {ex.Message}")
            ' En cas d'erreur, vider les données
            pistesCD = Nothing
            metadonneesCD = Nothing
            discIdActuel = Nothing
        End Try

        ' Remplir les informations même en cas d'erreur (en dehors du Catch)
        If metadonneesCD Is Nothing Then
            Await RemplirInformationsCD()
        End If
    End Sub

    ''' <summary>
    ''' Obtient les informations détaillées d'un lecteur CD
    ''' Format: "D: PIONEER BD-RW BDR-211M 1.51 Adapter: 1 ID: 0"
    ''' </summary>
    Private Function ObtenirInfoLecteur(lecteur As String) As String
        Try
            ' Normaliser la lettre du lecteur (enlever les : et \)
            Dim lettreLecteur = lecteur.TrimEnd("\"c, ":"c).ToUpper()

            ' Utiliser WMI pour obtenir les informations détaillées du lecteur
            Try
                Dim searcher As New ManagementObjectSearcher(
                    "SELECT * FROM Win32_CDROMDrive WHERE Drive = '" & lettreLecteur & ":'")

                For Each drive As ManagementObject In searcher.Get()
                    ' Récupérer les informations du lecteur
                    Dim caption As String = If(drive("Caption")?.ToString(), "CD-ROM")
                    Dim scsiTargetId As Object = drive("SCSITargetId")
                    Dim scsiLogicalUnit As Object = drive("SCSILogicalUnit")

                    ' Convertir en chaîne avec gestion des valeurs null
                    Dim targetId As String = If(scsiTargetId IsNot Nothing, scsiTargetId.ToString(), "?")
                    Dim logicalUnit As String = If(scsiLogicalUnit IsNot Nothing, scsiLogicalUnit.ToString(), "?")

                    ' Formater les informations
                    ' Exemple: "D: PIONEER BD-RW BDR-211M 1.51 Adapter: 1 ID: 0"
                    Return $"{lettreLecteur}: {caption}    Adapter: {logicalUnit} ID: {targetId}"
                Next
            Catch wmiEx As Exception
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] WMI échoué pour {lecteur}: {wmiEx.Message}")
            End Try

            ' Si WMI n'a rien retourné, retourner un format simple
            Return $"{lettreLecteur}: CD-ROM Drive"

        Catch ex As Exception
            ' En cas d'erreur, retourner juste la lettre du lecteur
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur obtention info lecteur {lecteur}: {ex.Message}")
            Return lecteur.TrimEnd("\"c, ":"c).ToUpper() & ": CD-ROM Drive"
        End Try
    End Function

    ''' <summary>
    ''' Bouton pour rafraîchir la liste des lecteurs
    ''' </summary>
    Public Sub RafraichirLecteurs()
        ChargerLecteursCD()
    End Sub

    ''' <summary>
    ''' Obtient le lecteur actuellement sélectionné (ou Nothing si pas de CD)
    ''' </summary>
    Public Function ObtenirLecteurSelectionne() As String
        Dim item = TryCast(ComboBoxChoixLecteur.SelectedItem, LecteurCDItem)
        If item IsNot Nothing AndAlso item.ContientCD Then
            Return item.Lecteur
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' Initialise le timer de surveillance des lecteurs CD
    ''' </summary>
    Private Sub InitialiserSurveillanceLecteurs()
        cdMonitorTimer = New Timer With {
            .Interval = 2000 ' Vérifier toutes les 2 secondes
        }
        AddHandler cdMonitorTimer.Tick, AddressOf SurveillerLecteurs
        cdMonitorTimer.Start()
    End Sub

    ''' <summary>
    ''' Surveille les changements d'état des lecteurs CD
    ''' </summary>
    Private Sub SurveillerLecteurs(sender As Object, e As EventArgs)
        Try
            ' Ignorer si le flag est activé (pendant l'initialisation)
            If ignorerChangementsCD Then
                System.Diagnostics.Debug.WriteLine("[FormCompresser] Surveillance CD ignorée (initialisation en cours)")
                Return
            End If

            ' Détecter tous les lecteurs CD-ROM
            Dim lecteurs = CDAudioManager.DetecterLecteursCDAudio()
            Dim changementDetecte As Boolean = False
            Dim lecteurActuelChange As Boolean = False
            Dim lecteurActuel As String = ObtenirLecteurSelectionne()

            ' Vérifier l'état de chaque lecteur
            Dim nouveauxEtats As New Dictionary(Of String, Boolean)
            For Each lecteur In lecteurs
                Dim contientCD = CDAudioManager.EstCDAudioPresent(lecteur)
                nouveauxEtats(lecteur) = contientCD

                ' Vérifier si l'état a changé
                If Not derniersEtatsLecteurs.ContainsKey(lecteur) OrElse
                   derniersEtatsLecteurs(lecteur) <> contientCD Then
                    changementDetecte = True
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Changement détecté pour {lecteur}: CD={contientCD}")

                    ' Vérifier si c'est le lecteur actuellement sélectionné
                    If lecteurActuel IsNot Nothing AndAlso lecteur.TrimEnd(":"c).ToUpper() = lecteurActuel.TrimEnd(":"c).ToUpper() Then
                        lecteurActuelChange = True

                        ' Si le CD a été ÉJECTÉ du lecteur actuel, réinitialiser discIdActuel
                        If Not contientCD Then
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ CD éjecté du lecteur {lecteur}, réinitialisation de discIdActuel")
                            discIdActuel = Nothing
                            pistesCD = Nothing
                            metadonneesCD = Nothing
                            ' Clear any previously detected silence information so a reinserted CD starts fresh
                            Try
                                detectedSilenceCenters.Clear()
                            Catch
                            End Try
                            Try
                                _silenceVariables.Clear()
                            Catch
                            End Try
                            Try
                                analysesPistes.Clear()
                            Catch
                            End Try
                            System.Diagnostics.Debug.WriteLine("[FormCompresser] Silence caches et analyses réinitialisés après éjection du CD")
                        End If
                    End If
                End If
            Next

            ' Si un changement est détecté, rafraîchir le ComboBox
            If changementDetecte Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] *** CHANGEMENT DÉTECTÉ ***")
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]   lecteurActuelChange = {lecteurActuelChange}")
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]   lecteurActuel = {If(lecteurActuel, "NULL")}")

                Me.Invoke(Sub()
                              Dim lecteurSelectionne = ObtenirLecteurSelectionne()
                              System.Diagnostics.Debug.WriteLine($"[FormCompresser]   lecteurSelectionne (avant ChargerLecteursCD) = {If(lecteurSelectionne, "NULL")}")

                              ChargerLecteursCD()

                              ' Si c'est le lecteur actuellement sélectionné qui a changé
                              If lecteurActuelChange Then
                                  System.Diagnostics.Debug.WriteLine($"[FormCompresser]   Lecteur actuel a changé, recherche du lecteur...")

                                  ' Rechercher le lecteur avec un CD
                                  For i As Integer = 0 To ComboBoxChoixLecteur.Items.Count - 1
                                      Dim item = TryCast(ComboBoxChoixLecteur.Items(i), LecteurCDItem)
                                      If item IsNot Nothing Then
                                          System.Diagnostics.Debug.WriteLine($"[FormCompresser]     Item {i}: Lecteur={item.Lecteur}, ContientCD={item.ContientCD}")

                                          ' Comparer avec le lecteur qui a changé
                                          If lecteurActuel IsNot Nothing AndAlso
                                             item.Lecteur.TrimEnd(":"c).ToUpper() = lecteurActuel.TrimEnd(":"c).ToUpper() AndAlso
                                             item.ContientCD Then
                                              ' Forcer le rechargement même si c'est le même index
                                              System.Diagnostics.Debug.WriteLine($"[FormCompresser]   ✓ Rechargement du lecteur {item.Lecteur}")
                                              ChargerMetadonneesNouveauCD(item.Lecteur)
                                              ComboBoxChoixLecteur.SelectedIndex = i
                                              Exit For
                                          End If
                                      End If
                                  Next
                              ElseIf lecteurSelectionne IsNot Nothing Then
                                  System.Diagnostics.Debug.WriteLine($"[FormCompresser]   Autre lecteur a changé, tentative de resélection...")

                                  ' Essayer de resélectionner le même lecteur s'il contient toujours un CD
                                  For i As Integer = 0 To ComboBoxChoixLecteur.Items.Count - 1
                                      Dim item = TryCast(ComboBoxChoixLecteur.Items(i), LecteurCDItem)
                                      If item IsNot Nothing AndAlso item.Lecteur = lecteurSelectionne AndAlso item.ContientCD Then
                                          ComboBoxChoixLecteur.SelectedIndex = i
                                          Exit For
                                      End If
                                  Next
                              Else
                                  System.Diagnostics.Debug.WriteLine($"[FormCompresser]   Aucun lecteur actuellement sélectionné, tentative de sélection automatique...")

                                  ' Aucun lecteur sélectionné, essayer de sélectionner le premier lecteur avec un CD
                                  For i As Integer = 0 To ComboBoxChoixLecteur.Items.Count - 1
                                      Dim item = TryCast(ComboBoxChoixLecteur.Items(i), LecteurCDItem)
                                      If item IsNot Nothing AndAlso item.ContientCD Then
                                          System.Diagnostics.Debug.WriteLine($"[FormCompresser]   ✓ Sélection automatique du lecteur {item.Lecteur}")
                                          ChargerMetadonneesNouveauCD(item.Lecteur)
                                          ComboBoxChoixLecteur.SelectedIndex = i
                                          Exit For
                                      End If
                                  Next
                              End If
                          End Sub)
            End If

            ' Mettre à jour les derniers états
            derniersEtatsLecteurs = nouveauxEtats

        Catch ex As Exception
            ' Ignorer les erreurs de surveillance pour ne pas perturber l'utilisateur
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur surveillance lecteurs: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Nettoyer le timer à la fermeture
    ''' </summary>
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If cdMonitorTimer IsNot Nothing Then
            cdMonitorTimer.Stop()
            RemoveHandler cdMonitorTimer.Tick, AddressOf SurveillerLecteurs
            cdMonitorTimer.Dispose()
            cdMonitorTimer = Nothing
        End If
        MyBase.OnFormClosing(e)
    End Sub

    ''' <summary>
    ''' Verrouille le tiroir du lecteur CD pour empêcher l'éjection manuelle
    ''' </summary>
    Private Sub VerrouillerCD(lecteur As String)
        Try
            Dim lettre As String = lecteur.TrimEnd(":"c).ToUpper()
            mciSendString($"open {lettre}: type cdaudio alias cd{lettre}", Nothing, 0, IntPtr.Zero)
            mciSendString($"set cd{lettre} door locked", Nothing, 0, IntPtr.Zero)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Lecteur {lettre}: verrouillé")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur verrouillage CD: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Déverrouille le tiroir du lecteur CD
    ''' </summary>
    Private Sub DeverrouillerCD(lecteur As String)
        Try
            Dim lettre As String = lecteur.TrimEnd(":"c).ToUpper()
            mciSendString($"set cd{lettre} door unlocked", Nothing, 0, IntPtr.Zero)
            mciSendString($"close cd{lettre}", Nothing, 0, IntPtr.Zero)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Lecteur {lettre}: déverrouillé")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur déverrouillage CD: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Éjecte le CD du lecteur
    ''' </summary>
    Private Sub EjecterCD(lecteur As String)
        Try
            Dim lettre As String = lecteur.TrimEnd(":"c).ToUpper()
            mciSendString($"open {lettre}: type cdaudio alias cd{lettre}", Nothing, 0, IntPtr.Zero)
            mciSendString($"set cd{lettre} door open", Nothing, 0, IntPtr.Zero)
            mciSendString($"close cd{lettre}", Nothing, 0, IntPtr.Zero)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Lecteur {lettre}: éjecté")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur éjection CD: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Réinitialise les champs de métadonnées du CD
    ''' </summary>
    Private Sub ReinitialiserMetadonneesCD()
        Try
            If TextBoxCDTitre IsNot Nothing Then
                TextBoxCDTitre.Text = ""
            End If
            If TextBoxCDArtiste IsNot Nothing Then
                TextBoxCDArtiste.Text = ""
            End If
            If TextBoxAnnee IsNot Nothing Then
                TextBoxAnnee.Text = ""
            End If
            If ComboBoxGenre IsNot Nothing Then
                ComboBoxGenre.SelectedIndex = -1  ' Aucune sélection
            End If
            If PictureBoxPochette IsNot Nothing Then
                SafeClearPictureBoxImage()
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⚠️ IMAGE EFFACÉE dans ReinitialiserMetadonneesCD()")
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Stack: {New System.Diagnostics.StackTrace(True).GetFrame(1)?.GetMethod()?.Name}")
            End If
            If Label_DimImage IsNot Nothing Then
                Label_DimImage.Text = ""
            End If
            If LabelTailleImage IsNot Nothing Then
                LabelTailleImage.Text = ""
            End If

            ' Réinitialiser l'historique des pochettes
            historiquePochettes.Clear()
            indexPochetteActuelle = -1

            If ListViewCompress IsNot Nothing Then
                ListViewCompress.Items.Clear()  ' Effacer la liste des pistes
            End If
            If CheckBox_FCompress_SelectDeselect IsNot Nothing Then
                CheckBox_FCompress_SelectDeselect.Checked = True  ' Recocher par défaut
            End If

            ' Vider le cache d'analyse des pistes pour forcer une nouvelle analyse
            analysesPistes.Clear()

            ' IMPORTANT: Vider aussi pistesCD, metadonneesCD ET discIdActuel pour éviter un rechargement après éjection
            ' Ceci permet de traiter la réinsertion du même CD comme un nouveau CD
            pistesCD = Nothing
            metadonneesCD = Nothing
            discIdActuel = Nothing

            System.Diagnostics.Debug.WriteLine("[FormCompresser] Métadonnées CD, pistesCD, metadonneesCD, discIdActuel, liste des pistes, checkbox et cache d'analyse réinitialisés")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur réinitialisation métadonnées: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton Extraire
    ''' </summary>
    Private Async Sub ButtonExtraire_Click(sender As Object, e As EventArgs) Handles ButtonExtraire.Click
        Try
            ' Vérifier qu'un lecteur est sélectionné
            If String.IsNullOrWhiteSpace(lecteurCD) Then
                MessageBox.Show(LanguageManager.GetString("Compresser_NoCDDrive_Message"),
                              LanguageManager.GetString("Compresser_NoCDDrive_Title"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
                Return
            End If

            ' Vérifier qu'il y a des pistes à extraire
            Dim indicesPistes = ObtenirIndicesPistesSelectionnees()
            If indicesPistes.Count = 0 Then
                MessageBox.Show(LanguageManager.GetString("Compresser_NoTrackSelected_Message"),
                              LanguageManager.GetString("Compresser_NoTrackSelected_Title"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
                Return
            End If

            ' Vérifier qu'un répertoire de destination est défini
            If String.IsNullOrWhiteSpace(TextBoxRepSauvegarde.Text) OrElse
               Not Directory.Exists(TextBoxRepSauvegarde.Text) Then
                MessageBox.Show(LanguageManager.GetString("Compresser_InvalidDestination_Message"),
                              LanguageManager.GetString("Compresser_InvalidDestination_Title"),
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Warning)
                Return
            End If

            ' Verrouiller le CD si l'option est activée
            If CheckBoxVerouillerCD.Checked Then
                VerrouillerCD(lecteurCD)
            End If

            ' Désactiver les contrôles pendant l'extraction
            ButtonExtraire.Enabled = False
            ButtonExtraire.Visible = False
            ButtonAnnuler.Visible = True
            ButtonAnnuler.Enabled = True
            ButtonQuitter.Enabled = False
            annulationDemandee = False ' Réinitialiser le flag
            ' Initialiser un CancellationTokenSource pour gérer l'annulation coordonnée
            Try
                If ctsExtraction IsNot Nothing Then
                    Try
                        ctsExtraction.Cancel()
                        ctsExtraction.Dispose()
                    Catch
                    End Try
                End If
                ctsExtraction = New System.Threading.CancellationTokenSource()
                ' Provide CDAudioAnalyzer with a cancellation predicate for immediate abort
                Try
                    CDAudioAnalyzer.ExternalCancellationCheck = Function()
                                                                    Return annulationDemandee OrElse (ctsExtraction IsNot Nothing AndAlso ctsExtraction.IsCancellationRequested)
                                                                End Function
                Catch
                End Try
            Catch
            End Try

            ' Reset progress trace file for fresh session (only when enabled)
            Try
                ' Start suppressing unwanted temp trace files during extraction
                StartSuppressingTempTraceFiles()
            Catch
            End Try

            ' Désactiver TopMost pour permettre à l'utilisateur de basculer vers Form1 s'il le souhaite
            ' FormCompresser reste visible mais n'est plus forcé au premier plan
            Me.TopMost = False
            System.Diagnostics.Debug.WriteLine("[FormCompresser] TopMost désactivé - Form1 utilisable pendant l'extraction")

            ' Initialise le log de diagnostic pour cette session d'extraction
            Try
                ' Force reset the diagnostics log at the start of a user-initiated extraction session
                CDAudioAnalyzer.InitializeDiagnosticsLog($"Extraction started by user - Drive={lecteurCD}", True)
                ' SANITY TEST: entrée de log + fichier témoin discret pour valider le chemin exécuté
                Try
                    CDAudioAnalyzer.DiagnosticWrite("SANITY_TEST: ButtonExtraire invoked - writing sanity file")
                    Dim sanityPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AudioPlay_Sanity.txt")
                    File.WriteAllText(sanityPath, $"SANITY {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}")
                Catch
                End Try
            Catch exInitLog As Exception
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] InitializeDiagnosticsLog failed: {exInitLog.Message}")
            End Try

            ' Ensure watcher is active for the duration of the extraction
            Try
                StartSuppressingTempTraceFiles()
            Catch
            End Try

            ' ═══ NOUVELLE STRATÉGIE : ANALYSE EN BATCH AVANT EXTRACTION (pairwise) ═══
            ' Pré-analyser toutes les pistes sélectionnées afin d'appliquer la logique
            ' d'analyse appairée et de réconciliation avant l'étape d'extraction.
            ' Charger les paramètres de l'analyseur depuis parametres.txt si présents
            Try
                Dim cfgPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay", "parametres.txt")
                If File.Exists(cfgPath) Then
                    For Each line In File.ReadAllLines(cfgPath)
                        If Not line.Contains("=") Then Continue For
                        Dim parts = line.Split("="c, 2)
                        Dim key = parts(0).Trim()
                        Dim val = parts(1).Trim()
                        Select Case key
                            Case "Analyzer_WindowBeforeSeconds"
                                Dim v As Integer
                                If Integer.TryParse(val, v) Then CDAudioAnalyzer.TransitionWindowBeforeSeconds = Math.Max(5, Math.Min(120, v))
                            Case "Analyzer_WindowAfterSeconds"
                                Dim v2 As Integer
                                If Integer.TryParse(val, v2) Then CDAudioAnalyzer.TransitionWindowAfterSeconds = Math.Max(5, Math.Min(120, v2))
                            Case "Analyzer_SilenceThreshold"
                                Dim d As Double
                                If Double.TryParse(val, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, d) Then CDAudioAnalyzer.SilenceThreshold = d
                            Case "Analyzer_MinSustainedSilenceSeconds"
                                Dim d2 As Double
                                If Double.TryParse(val, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, d2) Then CDAudioAnalyzer.MinSustainedSilenceSeconds = Math.Max(0.05, d2)
                            Case "Analyzer_MaxStartTrimSeconds"
                                Dim d3 As Double
                                If Double.TryParse(val, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, d3) Then CDAudioAnalyzer.MaxStartTrimSeconds = Math.Max(0, Math.Min(10, d3))
                        End Select
                    Next
                End If
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur lecture paramètres analyseur: {ex.Message}")
            End Try
            analysesPistes.Clear()
            ' Analyse complète du CD : détecter les silences entre toutes les pistes
            ' ...
            ' quel que soit le sous-ensemble de pistes choisi pour l'extraction.
            Dim analysisIndices As New List(Of Integer)()
            Try
                If pistesCD IsNot Nothing Then
                    For j As Integer = 0 To pistesCD.Count - 1
                        analysisIndices.Add(j)
                    Next
                Else
                    ' Fallback : analyser les indices sélectionnés si la TOC n'est pas disponible
                    For Each idx In indicesPistes
                        analysisIndices.Add(idx)
                    Next
                End If
            Catch
            End Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🔍 Pré-analyse des pistes sélectionnées (pairwise)")

            ' Affichage UI pendant l'analyse en batch
            ' Ne pas initialiser la progression globale ici : elle doit rester cachée
            ' jusqu'au début effectif de l'extraction des pistes.

            LabelPisteEnCours.Visible = True
            LabelPisteEnCours.Text = LanguageManager.GetString("Compressor_AnalysisInProgress")
            ' Initialiser les paramètres de diagnostics (niveau Info, per-frame désactivé par défaut)
            ' Ils peuvent être modifiés via le fichier parametres.txt ou l'UI debug ultérieurement
            ProgressBarPisteActuelle.Visible = True
            SafeInitProgressBar(analysisIndices.Count)
            Application.DoEvents()

            ' Analyser piste par piste (off-UI thread) pour pouvoir mettre à jour la barre de progression
            Dim tempAnalyses As New List(Of CDAudioAnalyzer.TrackAnalysis)
            For i As Integer = 0 To analysisIndices.Count - 1
                Dim cdIndex As Integer = analysisIndices(i)
                If cdIndex >= 0 AndAlso cdIndex < pistesCD.Count Then
                    Dim track = pistesCD(cdIndex)
                    Dim nextTrack As CDAudioManager.CDTrack = Nothing
                    If cdIndex + 1 < pistesCD.Count Then
                        nextTrack = pistesCD(cdIndex + 1)
                    End If

                    ' Exécuter l'analyse lourde hors du thread UI
                    Dim analysis = Await Task.Run(Function() CDAudioAnalyzer.AnalyzeTrack(track, nextTrack, Nothing))
                    tempAnalyses.Add(analysis)

                    ' Mettre à jour la progression visuelle (thread-safe)
                    SafeUpdateProgressBar(tempAnalyses.Count)
                    LabelPisteEnCours.Text = $"Analyse en cours... ({tempAnalyses.Count}/{analysisIndices.Count})"
                    Application.DoEvents()
                End If
            Next

            ' Réconciliation paire par paire (même logique que CDAudioAnalyzer.AnalyzeSelectedTracks)
            For i As Integer = 0 To tempAnalyses.Count - 1
                Dim cdIndex As Integer = analysisIndices(i)
                If cdIndex >= 0 AndAlso cdIndex < pistesCD.Count Then
                    ' initialiser mapping
                    analysesPistes(cdIndex) = tempAnalyses(i)
                End If
            Next

            ' Effectuer la réconciliation pour éviter chevauchements
            For i As Integer = 0 To tempAnalyses.Count - 2
                Dim cur = tempAnalyses(i)
                Dim nxt = tempAnalyses(i + 1)

                ' S'assurer que les valeurs par défaut existent
                If nxt.AdjustedStartFrame <= 0 Then
                    nxt.AdjustedStartFrame = nxt.OriginalStartFrame
                End If

                ' Si l'analyse de transition indique que le silence déborde APRÈS la frontière TOC,
                ' préférer ajuster le début de la piste suivante plutôt que de couper la fin de la courante.
                If cur.TransitionAnalyzed AndAlso cur.PreferAdjustNextStart Then
                    Dim proposedStart As Integer = cur.SilenceEndFrame + CDAudioAnalyzer.SafetyMarginFrames

                    If proposedStart >= nxt.AdjustedEndFrame Then
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Réconciliation impossible sans inversion (fallback) entre piste {cur.TrackNumber} et {nxt.TrackNumber} - conservation des positions TOC pour la suivante")
                    ElseIf proposedStart > nxt.AdjustedStartFrame Then
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔧 Réconciliation préférentielle: déplacement du début de la piste {nxt.TrackNumber} à {proposedStart} (silence APRÈS TOC de la piste {cur.TrackNumber})")
                        nxt.AdjustedStartFrame = proposedStart
                        nxt.TrimmedStartFrames = nxt.AdjustedStartFrame - nxt.OriginalStartFrame
                        nxt.WasAdjusted = True
                    End If

                ElseIf cur.AdjustedEndFrame >= nxt.AdjustedStartFrame Then
                    Dim correctedStart As Integer = cur.AdjustedEndFrame + 1

                    If correctedStart >= nxt.AdjustedEndFrame Then
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] ⚠️ Réconciliation impossible sans inversion (fallback) entre piste {cur.TrackNumber} et {nxt.TrackNumber} - conservation des positions TOC pour la suivante")
                        nxt.AdjustedStartFrame = nxt.OriginalStartFrame
                        nxt.TrimmedStartFrames = 0
                    Else
                        System.Diagnostics.Debug.WriteLine($"[CDAudioAnalyzer] 🔧 Réconciliation: déplacement du début de la piste {nxt.TrackNumber} à {correctedStart} pour éviter chevauchement avec piste {cur.TrackNumber}")
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

                ' Écrire la réconciliation dans le mapping global
                Dim nextCdIndex As Integer = analysisIndices(i + 1)
                If nextCdIndex >= 0 AndAlso nextCdIndex < pistesCD.Count Then
                    analysesPistes(nextCdIndex) = nxt
                End If
                ' Mettre à jour l'actuelle aussi
                Dim curCdIndex As Integer = analysisIndices(i)
                If curCdIndex >= 0 AndAlso curCdIndex < pistesCD.Count Then
                    analysesPistes(curCdIndex) = cur
                End If
            Next

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✅ Pré-analyse terminée: {analysesPistes.Count} piste(s) analysée(s)")

            ' TEMP DEV MODE: only detect if silences exist between tracks and stop the extraction
            Try
                ' TEMP DEV MODE: when true, only detect silences and stop the extraction (development aid)
                ' NOTE: affichage forcé par demande utilisateur — ne plus supprimer ce message automatiquement
                Dim detectOnlySilence As Boolean = True
                If detectOnlySilence Then
                    Dim anyFound As Boolean = False
                    For Each a In tempAnalyses
                        Try
                            If a.TransitionAnalyzed Then
                                anyFound = True
                                Exit For
                            End If
                            If a.WasAdjusted Then
                                anyFound = True
                                Exit For
                            End If
                        Catch
                        End Try
                    Next

                    ' Remove temporary result file write used during development

                    If anyFound Then
                        ' Build list of centre positions for between-track silences
                        Dim centers As New List(Of String)()
                        ' Keep detectedSilenceCenters populated for trimming; do not show temporary messages
                        Try
                            If tempAnalyses IsNot Nothing AndAlso tempAnalyses.Count > 1 Then
                                For idx2 As Integer = 0 To tempAnalyses.Count - 2
                                    Try
                                        Dim a2 = tempAnalyses(idx2)
                                        If a2.TransitionAnalyzed Then
                                            Dim centerFrames = a2.SilenceCenterFrame
                                            If centerFrames > 0 Then
                                                Dim centerSec = centerFrames / 75.0
                                                Dim ts = TimeSpan.FromSeconds(centerSec)
                                                centers.Add(ts.ToString("hh\:mm\:ss\.fff"))
                                                Try
                                                    detectedSilenceCenters.Add(centerSec)
                                                Catch
                                                End Try
                                                ' Also populate named silence variables: silence1, silence2, ...
                                                Try
                                                    Dim namedIndex = detectedSilenceCenters.Count ' 1-based index
                                                    Dim key = $"silence{namedIndex}"
                                                    If Not _silenceVariables.ContainsKey(key) Then
                                                        _silenceVariables(key) = centerSec
                                                    Else
                                                        _silenceVariables(key) = centerSec
                                                    End If
                                                Catch
                                                End Try
                                            Else
                                                centers.Add("unknown")
                                            End If
                                        End If
                                    Catch
                                    End Try
                                Next
                            End If
                        Catch
                        End Try

                        Dim positionsText As String = String.Empty
                        Try
                            Dim sbPos As New System.Text.StringBuilder()
                            If detectedSilenceCenters IsNot Nothing AndAlso detectedSilenceCenters.Count > 0 Then
                                For i As Integer = 0 To detectedSilenceCenters.Count - 1
                                    Dim sec As Double = detectedSilenceCenters(i)
                                    Dim ts = TimeSpan.FromSeconds(sec)
                                    sbPos.AppendLine($"Silence {i + 1}: {ts:hh\:mm\:ss\.fff} ({sec:F3} s)")
                                Next
                            ElseIf centers IsNot Nothing AndAlso centers.Count > 0 Then
                                For i As Integer = 0 To centers.Count - 1
                                    sbPos.AppendLine($"Silence {i + 1}: {centers(i)}")
                                Next
                            End If
                            positionsText = sbPos.ToString().Trim()
                        Catch
                        End Try

                        Try
                            CDAudioAnalyzer.DiagnosticWrite($"DETECT_ONLY_SILENCE_INFO: Silences found between tracks. Positions={positionsText}")
                        Catch
                        End Try

                        ' Afficher une boite de dialogue de développement indiquant les silences trouvés
                        Try
                            Dim countFound As Integer = If(detectedSilenceCenters IsNot Nothing AndAlso detectedSilenceCenters.Count > 0, detectedSilenceCenters.Count, centers.Count)
                            Dim header As String = "Silences entre les pistes trouvés." & Environment.NewLine & Environment.NewLine & $"Nombre: {countFound}" & Environment.NewLine & Environment.NewLine
                            Dim fullMsg As String = header & positionsText
                            ' Show the results dialog only if user requested it via CheckBox_Affiche_CentreSilence
                            Try
                                If CheckBox_Affiche_CentreSilence IsNot Nothing AndAlso CheckBox_Affiche_CentreSilence.Checked Then
                                    ' Use the larger resizable form so long lists are visible
                                    ' Show the results dialog; when closed (OK) continue with the full extraction
                                    Using dlg As New FormSilenceResults(fullMsg, LanguageManager.GetString("Detection_Silences_Title"))
                                        dlg.ShowDialog(Me)
                                    End Using
                                    ' Ensure we proceed with extraction after the user acknowledged
                                    Try
                                        CDAudioAnalyzer.DiagnosticWrite("DETECT_ONLY_SILENCE: User acknowledged silence list - continuing with extraction")
                                    Catch
                                    End Try
                                Else
                                    ' User opted out of UI for silence centres; log that we skipped showing the dialog
                                    Try
                                        CDAudioAnalyzer.DiagnosticWrite("DETECT_ONLY_SILENCE: UI suppressed by CheckBox_Affiche_CentreSilence; continuing with extraction")
                                    Catch
                                    End Try
                                End If
                            Catch
                            End Try
                            ' Disable the detect-only flag so we don't interrupt again during this run
                            detectOnlySilence = False
                        Catch
                        End Try
                        ' Continue with extraction after showing results
                    Else
                        Try
                            CDAudioAnalyzer.DiagnosticWrite("DETECT_ONLY_SILENCE_INFO: No between-track silence found.")
                        Catch
                        End Try

                        ' Afficher une boite de dialogue de développement indiquant qu'aucun silence n'a été trouvé
                        Try
                            Dim msgNo As String = "Silences entre les pistes non trouvés."
                            MessageBox.Show(Me, msgNo, LanguageManager.GetString("Detection_Silences_Title"), MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Catch
                        End Try
                        ' Continue with extraction even if no silences were found
                    End If
                    ' No interactive dialogs or forced closing in production flow.
                    ' Continue extraction even if detectOnlySilence is enabled.
                End If
            Catch
            End Try

            ' Restaurer l'affichage global de progression pour l'étape d'extraction
            LabelProgressionGlobale.Visible = True
            ProgressBarGlobale.Visible = True

            ' IMPORTANT: Mettre à jour l'URL de la pochette dans les métadonnées avec l'image actuellement affichée
            ' (qui peut être différente de l'URL initiale si l'utilisateur a navigué avec Prec/Suiv)
            If metadonneesCD IsNot Nothing AndAlso pochetteTempUrl IsNot Nothing Then
                metadonneesCD.CoverArtUrl = pochetteTempUrl
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] CoverArtUrl mis à jour: {pochetteTempUrl}")
            End If

            ' IMPORTANT: Sauvegarder les métadonnées (incluant l'URL de la pochette) dans le cache
            ' Ceci n'est fait qu'après le clic sur ButtonExtraire, pas lors de l'affichage initial
            If metadonneesCD IsNot Nothing AndAlso Not String.IsNullOrWhiteSpace(metadonneesCD.CoverArtUrl) Then
                Dim discId = CDMetadataProvider.CalculerDiscID(pistesCD)
                If Not String.IsNullOrWhiteSpace(discId) Then
                    CDMetadataCache.SauvegarderMetadonnees(discId, metadonneesCD, "GnuDB+MusicBrainz")
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Métadonnées avec URL pochette sauvegardées dans le cache")
                End If
            End If

            ' IMPORTANT: Sauvegarder l'image de la pochette dans le cache d'images
            ' Ceci n'est fait qu'après le clic sur ButtonExtraire, pas lors du téléchargement initial
            If pochetteTempUrl IsNot Nothing AndAlso pochetteTempBytes IsNot Nothing Then
                Try
                    CoverCacheManager.SauvegarderImage(pochetteTempUrl, pochetteTempBytes)
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Image pochette ({pochetteTempBytes.Length} bytes) sauvegardée dans le cache")

                    ' NOUVEAU: Sauvegarder aussi dans le répertoire d'extraction avec nom formaté
                    SauvegarderPochetteDansRepertoire(pochetteTempBytes)

                    ' Vider le cache mémoire temporaire maintenant que l'image est sauvegardée sur disque
                    cachePochettesBytes.Clear()
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Cache mémoire des images vidé après sauvegarde")

                    ' Nettoyer l'historique de navigation : ne garder que la pochette actuellement affichée
                    If historiquePochettes.Count > 0 AndAlso indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count Then
                        Dim pochetteChoisie = historiquePochettes(indexPochetteActuelle)
                        historiquePochettes.Clear()
                        historiquePochettes.Add(pochetteChoisie)
                        indexPochetteActuelle = 0
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Historique nettoyé - seule la pochette choisie conservée: {pochetteChoisie}")
                    Else
                        ' Sinon, vider complètement l'historique
                        historiquePochettes.Clear()
                        indexPochetteActuelle = -1
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Historique complètement vidé")
                    End If

                    ' Mettre à jour les boutons de navigation
                    MettreAJourBoutonsNavigation()

                Catch ex As Exception
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur sauvegarde image cache: {ex.Message}")
                End Try
            End If

            ' Maintenant afficher les barres de progression globale pour l'extraction
            SafeInitGlobalProgressBar(indicesPistes.Count)
            ' Afficher la progression globale à 0/N au démarrage (avant la première piste)
            SafeUpdateGlobalProgressBar(0)
            SafeSetGlobalLabel(String.Format(LanguageManager.GetString("Compressor_GlobalProgress"), 0, indicesPistes.Count))

            ' Afficher aussi la progression individuelle par piste
            LabelPisteEnCours.Visible = True
            ' Initialiser la barre de progression de piste via helper thread-safe
            ' Utiliser 100 comme échelle par défaut pour la progression par piste (percent)
            SafeInitProgressBar(100)
            SafeUpdateProgressBar(0)

            Try
                ' Extraire les pistes sélectionnées
                Dim pistesReussies As Integer = 0
                Dim pistesEchouees As Integer = 0
                Dim pisteNumero As Integer = 0

                ' Préparer un snapshot des pistes à extraire (pour éviter accès UI depuis le thread d'arrière-plan)
                Dim snapshot As New List(Of (Index As Integer, Titre As String, Artiste As String))
                For Each idx In indicesPistes
                    Try
                        Dim item = ListViewCompress.Items(idx)
                        Dim titre As String = If(item.SubItems.Count > 1, item.SubItems(1).Text, $"Piste {item.Text}")
                        Dim artiste As String = If(item.SubItems.Count > 2, item.SubItems(2).Text, TextBoxCDArtiste.Text)
                        snapshot.Add((idx, titre, artiste))
                    Catch
                        snapshot.Add((idx, $"Piste {idx + 1}", TextBoxCDArtiste.Text))
                    End Try
                Next

                ' Exécuter la boucle d'extraction dans un Task de fond pour libérer le thread UI
                ' Instrumentation: log start/iter/exception/finish for background extraction
                ' Compteurs locaux déclarés ici pour être accessibles après la Task (fermature)
                Dim localPistesReussies As Integer = 0
                Dim localPistesEchouees As Integer = 0
                Dim localPisteNumero As Integer = 0

                Await Task.Run(Async Function()
                                   Try
                                       Dim tracePathStart = Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                                       System.IO.File.AppendAllText(tracePathStart, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] BACKGROUND_EXTRACTION_STARTED{Environment.NewLine}")
                                   Catch
                                   End Try
                                   Try

                                       For Each entry In snapshot
                                           If CDAudioAnalyzer.WriteProgressTraceToDisk Then
                                               Try
                                                   Dim traceIter = Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                                                   System.IO.File.AppendAllText(traceIter, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] BACKGROUND_EXTRACT_ITER {entry.Index}{Environment.NewLine}")
                                               Catch
                                               End Try
                                           End If
                                           localPisteNumero += 1

                                           ' Vérifier annulation
                                           If annulationDemandee OrElse (ctsExtraction IsNot Nothing AndAlso ctsExtraction.IsCancellationRequested) Then
                                               System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⚠️ Extraction annulée après {localPistesReussies} piste(s) (background loop)")
                                               Exit For
                                           End If

                                           Try
                                               ' Mettre à jour l'affichage de la piste en cours (thread-safe via BeginInvoke dans helpers ou direct)
                                               ' Use thread-safe helper to set the label for the current track
                                               Try
                                                   SafeSetLabelPisteEnCours($"{entry.Artiste} - {entry.Titre}", True)
                                               Catch
                                               End Try

                                               ' Réinitialiser la progression de la piste (0..100)
                                               SafeInitProgressBar(100)
                                               SafeUpdateProgressBar(0)

                                               ' Appeler l'extraction (ExtrairePiste est asynchrone)
                                               Await ExtrairePiste(entry.Index)
                                               localPistesReussies += 1

                                               ' Marquer la piste comme complétée (utiliser update lissé)
                                               SafeUpdateProgressBar(100)
                                           Catch ex As Exception
                                               localPistesEchouees += 1
                                               System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur extraction piste {entry.Index + 1} (background): {ex.Message}")
                                               If CDAudioAnalyzer.WriteProgressTraceToDisk Then
                                                   Try
                                                       Dim traceEx = Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                                                       System.IO.File.AppendAllText(traceEx, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] BACKGROUND_EXTRACT_EXCEPTION {entry.Index} {ex.Message}{Environment.NewLine}")
                                                   Catch
                                                   End Try
                                               End If
                                           End Try

                                           ' Mettre à jour la progression globale
                                           SafeUpdateGlobalProgressBar(localPisteNumero)
                                           SafeSetGlobalLabel(String.Format(LanguageManager.GetString("Compressor_GlobalProgress"), localPisteNumero, snapshot.Count))
                                       Next
                                       If CDAudioAnalyzer.WriteProgressTraceToDisk Then
                                           Try
                                               Dim traceFinish = Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                                               System.IO.File.AppendAllText(traceFinish, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] BACKGROUND_EXTRACTION_FINISHED{Environment.NewLine}")
                                           Catch
                                           End Try
                                       End If
                                   Catch exOuter As Exception
                                       If CDAudioAnalyzer.WriteProgressTraceToDisk Then
                                           Try
                                               Dim traceOuter = Path.Combine(System.IO.Path.GetTempPath(), "AudioPlay_progress_trace.txt")
                                               System.IO.File.AppendAllText(traceOuter, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] BACKGROUND_EXTRACTION_FATAL {exOuter.Message}{Environment.NewLine}")
                                           Catch
                                           End Try
                                       End If
                                   End Try
                               End Function)

                ' Task.Run ci-dessus est déjà attendu en ligne.

                ' Récupérer les compteurs de la task
                pistesReussies = localPistesReussies
                pistesEchouees = localPistesEchouees

                ' Masquer les barres de progression
                LabelPisteEnCours.Visible = False
                ProgressBarPisteActuelle.Visible = False
                LabelProgressionGlobale.Visible = False
                ProgressBarGlobale.Visible = False

                ' Lancer en tâche de fond un post-traitement interne des WAV produits - non bloquant
                Try
                    ' Capturer le chemin saisi et la préférence auto sur le thread UI
                    Dim uiBasePath As String = TextBoxRepSauvegarde.Text
                    Dim doAutoPost As Boolean = True
                    Try
                        If CheckBoxPostTraitementAuto IsNot Nothing Then
                            doAutoPost = CheckBoxPostTraitementAuto.Checked
                        End If
                    Catch
                        doAutoPost = True
                    End Try

                    ' Capturer ici toutes les valeurs UI qui seront utilisées par le post-traitement
                    ' afin d'éviter tout accès inter-threads depuis le Task.Run ci-dessous.
                    Dim post_LastExtractionQualiteIndex As Integer = lastExtractionQualiteIndex
                    Dim post_LastExtractionFormat As String = lastExtractionFormat
                    Dim post_TextBoxCDArtiste As String = TextBoxCDArtiste.Text
                    Dim post_TextBoxCDTitre As String = TextBoxCDTitre.Text
                    Dim post_TextBoxAnnee As String = TextBoxAnnee.Text
                    Dim post_ComboBoxGenre As String = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                    Dim post_TextBoxCommentaire As String = TextBoxCommentaire.Text
                    ' Only capture the picture if we will perform conversions (not for WAV-only)
                    Dim post_Pochette As Image = Nothing
                    If Not String.Equals(post_LastExtractionFormat, "WAV", StringComparison.InvariantCultureIgnoreCase) Then
                        post_Pochette = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)
                    End If

                    Task.Run(Async Function() As Task
                                 Try
                                     If Not doAutoPost Then Return
                                     ' If last extraction format was WAV-only, skip automatic post-processing conversions
                                     If String.Equals(post_LastExtractionFormat, "WAV", StringComparison.InvariantCultureIgnoreCase) Then
                                         Try
                                             If post_Pochette IsNot Nothing Then post_Pochette.Dispose()
                                         Catch
                                         End Try
                                         Return
                                     End If
                                     If String.IsNullOrEmpty(uiBasePath) OrElse Not Directory.Exists(uiBasePath) Then Return

                                     ' Trouver le sous-répertoire d'album le plus récent
                                     Dim albumDirForAnalysis As String = Nothing
                                     Dim bestTime As DateTime = DateTime.MinValue
                                     For Each d In Directory.EnumerateDirectories(uiBasePath)
                                         Try
                                             Dim t = Directory.GetLastWriteTimeUtc(d)
                                             If t > bestTime Then
                                                 bestTime = t
                                                 albumDirForAnalysis = d
                                             End If
                                         Catch
                                         End Try
                                     Next

                                     If String.IsNullOrEmpty(albumDirForAnalysis) OrElse Not Directory.Exists(albumDirForAnalysis) Then Return
                                     If Directory.GetFiles(albumDirForAnalysis, "*.wav").Length = 0 Then Return

                                     ' Rechercher l'outil WavAnalyzer dans l'arborescence Tools
                                     Try
                                         Dim startDir = AppDomain.CurrentDomain.BaseDirectory
                                         Dim foundDll As String = Nothing
                                         For i As Integer = 0 To 8
                                             If String.IsNullOrEmpty(startDir) Then Exit For
                                             Dim candidate = Path.GetFullPath(Path.Combine(startDir, "Tools", "WavAnalyzer", "bin", "Debug", "net8.0", "WavAnalyzerTool.dll"))
                                             If File.Exists(candidate) Then
                                                 foundDll = candidate
                                                 Exit For
                                             End If
                                             startDir = Path.GetDirectoryName(startDir)
                                         Next

                                         If Not String.IsNullOrEmpty(foundDll) Then
                                             Try
                                                 ' Write analyzer output to a temp file to avoid creating files in album unless diagnostics enabled
                                                 Dim outReport = Path.Combine(Path.GetTempPath(), $"silence_report_{Guid.NewGuid()}.txt")
                                                 Dim psi As New ProcessStartInfo("dotnet") With {
                                                     .CreateNoWindow = True,
                                                     .UseShellExecute = False,
                                                     .RedirectStandardOutput = True,
                                                     .RedirectStandardError = True
                                                 }
                                                 ' Utiliser ArgumentList pour éviter les problèmes d'échappement
                                                 psi.ArgumentList.Add(foundDll)
                                                 psi.ArgumentList.Add(albumDirForAnalysis)
                                                 psi.ArgumentList.Add("--out")
                                                 psi.ArgumentList.Add(outReport)

                                                 Using p As Process = Process.Start(psi)
                                                     If p IsNot Nothing Then
                                                         Try
                                                             ' Attendre avec timeout (120s)
                                                             If Not p.WaitForExit(120 * 1000) Then
                                                                 Try
                                                                     p.Kill(True)
                                                                 Catch
                                                                 End Try
                                                             End If

                                                             Dim stdout = String.Empty
                                                             Dim stderr = String.Empty
                                                             Try
                                                                 stdout = p.StandardOutput.ReadToEnd()
                                                                 stderr = p.StandardError.ReadToEnd()
                                                             Catch
                                                             End Try

                                                             Try
                                                                 ' Analyzer trace to temp is disabled by default; write only when diagnostics enabled
                                                                 If CDAudioAnalyzer.DiagnosticsToDiskEnabled Then
                                                                     Dim trace = Path.Combine(Path.GetTempPath(), "AudioPlay_wav_analyzer_trace.txt")
                                                                     File.AppendAllText(trace, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WavAnalyzer exit={p.ExitCode} outReport={outReport}{Environment.NewLine}")
                                                                     If Not String.IsNullOrEmpty(stdout) Then File.AppendAllText(trace, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] STDOUT: {stdout.Replace(vbCrLf, " ")}{Environment.NewLine}")
                                                                     If Not String.IsNullOrEmpty(stderr) Then File.AppendAllText(trace, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] STDERR: {stderr.Replace(vbCrLf, " ")}{Environment.NewLine}")
                                                                 End If
                                                             Catch
                                                             End Try
                                                         Finally
                                                             Try
                                                                 If Not p.HasExited Then
                                                                     p.Kill(True)
                                                                 End If
                                                             Catch
                                                             End Try
                                                         End Try
                                                     End If
                                                 End Using
                                             Catch exRun As Exception
                                                 Try
                                                     If CDAudioAnalyzer.DiagnosticsToDiskEnabled Then
                                                         Dim trace = Path.Combine(Path.GetTempPath(), "AudioPlay_wav_analyzer_trace.txt")
                                                         If Not String.IsNullOrWhiteSpace(trace) Then
                                                             File.AppendAllText(trace, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed running WavAnalyzer: {exRun.Message}{Environment.NewLine}")
                                                         End If
                                                     End If
                                                 Catch
                                                 End Try
                                             End Try
                                         End If
                                     Catch
                                     End Try

                                     ' Si un rapport a été généré ailleurs, le déplacer dans le dossier d'album (fallback)
                                     Try
                                         Dim searchRoots As New List(Of String) From {uiBasePath, Path.Combine(uiBasePath, "À Traiter"), Path.Combine(uiBasePath, "MP3"), Path.GetTempPath()}
                                         Dim bestReport As String = Nothing
                                         Dim bestReportTime As DateTime = DateTime.MinValue
                                         For Each root In searchRoots
                                             Try
                                                 If String.IsNullOrEmpty(root) OrElse Not Directory.Exists(root) Then Continue For
                                                 For Each f In Directory.GetFiles(root, "silence_report*.txt")
                                                     Try
                                                         Dim t = File.GetLastWriteTimeUtc(f)
                                                         If t > bestReportTime Then
                                                             bestReportTime = t
                                                             bestReport = f
                                                         End If
                                                     Catch
                                                     End Try
                                                 Next
                                             Catch
                                             End Try
                                         Next

                                         If Not String.IsNullOrEmpty(bestReport) AndAlso CDAudioAnalyzer.DiagnosticsToDiskEnabled Then
                                             Try
                                                 Dim dest = Path.Combine(albumDirForAnalysis, "silence_report.txt")
                                                 If Not String.Equals(Path.GetFullPath(bestReport), Path.GetFullPath(dest), StringComparison.InvariantCultureIgnoreCase) Then
                                                     Try
                                                         File.Move(bestReport, dest)
                                                     Catch
                                                         Try
                                                             File.Copy(bestReport, dest, True)
                                                             File.Delete(bestReport)
                                                         Catch
                                                         End Try
                                                     End Try
                                                 End If
                                             Catch
                                             End Try
                                         End If
                                     Catch
                                     End Try

                                     ' Appliquer le post-traitement interne (synchronisé pour garantir fichiers corrigés avant encodage)
                                     Try
                                         Dim canWriteDiagStart As Boolean = CDAudioAnalyzer.DiagnosticsToDiskEnabled
                                         If canWriteDiagStart Then
                                             File.AppendAllText(Path.Combine(albumDirForAnalysis, "AudioPlay_wav_analyzer_trace.txt"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ApplyPostProcessingCorrections START for {albumDirForAnalysis}{Environment.NewLine}")
                                         End If
                                     Catch
                                     End Try
                                     Try
                                         ApplyPostProcessingCorrections(albumDirForAnalysis)
                                     Catch exCorr As Exception
                                         Try
                                             If CDAudioAnalyzer.DiagnosticsToDiskEnabled Then
                                                 Dim trace2 = Path.Combine(Path.GetTempPath(), "AudioPlay_wav_analyzer_trace.txt")
                                                 If CDAudioAnalyzer.DiagnosticsToDiskEnabled Then
                                                     File.AppendAllText(trace2, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Post-processing failed for album={albumDirForAnalysis} - {exCorr.Message}{Environment.NewLine}")
                                                 End If
                                             End If
                                         Catch
                                         End Try
                                     End Try
                                     Try
                                         If CDAudioAnalyzer.DiagnosticsToDiskEnabled Then
                                             File.AppendAllText(Path.Combine(albumDirForAnalysis, "AudioPlay_wav_analyzer_trace.txt"), $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ApplyPostProcessingCorrections END for {albumDirForAnalysis}{Environment.NewLine}")
                                         End If
                                     Catch
                                     End Try

                                     ' If diagnostics to disk are disabled, remove any analyzer/report/debug files created in the album
                                     Try
                                         If Not CDAudioAnalyzer.DiagnosticsToDiskEnabled Then
                                             Try
                                                 ' Remove any silence_report*.txt first
                                                 For Each f In Directory.GetFiles(albumDirForAnalysis, "silence_report*.txt")
                                                     Try
                                                         File.Delete(f)
                                                     Catch
                                                     End Try
                                                 Next

                                                 ' Remove known artifact files
                                                 Dim toDelete As String() = {Path.Combine(albumDirForAnalysis, "AudioPlay_wav_analyzer_trace.txt"), Path.Combine(albumDirForAnalysis, "postproc_debug_tracks.txt"), Path.Combine(albumDirForAnalysis, "silence_report.txt")}
                                                 For Each f In toDelete
                                                     Try
                                                         If File.Exists(f) Then File.Delete(f)
                                                     Catch
                                                     End Try
                                                 Next
                                             Catch
                                             End Try
                                         End If
                                     Catch
                                     End Try

                                     ' No debug-fixed promotion: debug artifacts ("*_fixed_debug.wav") are not used in production flow.

                                     ' Après post-traitement, lancer une passe de conversion déterministe pour tous les WAV présents
                                     Try
                                         For Each wav In Directory.GetFiles(albumDirForAnalysis, "*.wav")
                                             Try
                                                 Dim baseName = Path.GetFileNameWithoutExtension(wav)
                                                 ' Ignorer fichiers déjà _fixed ou temporaire non destiné à conversion
                                                 If baseName.EndsWith("_fixed", StringComparison.InvariantCultureIgnoreCase) Then Continue For
                                                 If baseName.StartsWith("audioplay_temp_", StringComparison.InvariantCultureIgnoreCase) Then Continue For
                                                 ' Extraire le numéro de piste si présent dans le nom "NN - ..."
                                                 Dim trackNum As Integer = -1
                                                 Dim parts() As String = {}
                                                 Try
                                                     ' Essayer d'extraire NN - ... format
                                                     parts = baseName.Split(New String() {" - "}, StringSplitOptions.None)
                                                     If parts.Length > 0 Then Integer.TryParse(parts(0), trackNum)
                                                 Catch
                                                 End Try
                                                 ' Convertir systématiquement les WAV produits (sans dépendre du pending marker)
                                                 ' Choisir le fichier source: préférer _fixed.wav si présent et valide
                                                 Dim fixedCandidate = Path.Combine(albumDirForAnalysis, baseName & "_fixed.wav")
                                                 Dim sourceForEncoding = wav
                                                 Try
                                                     If File.Exists(fixedCandidate) AndAlso IsValidWavFile(fixedCandidate) Then
                                                         sourceForEncoding = fixedCandidate
                                                     End If
                                                 Catch
                                                 End Try

                                                 ' Construire chemin de sortie MP3 selon le nom de fichier attendu
                                                 Dim finalMp3 = Path.Combine(albumDirForAnalysis, baseName & ".mp3")
                                                 Try
                                                     ' Use captured UI values instead of reading controls on the background thread
                                                     Dim convOk = Await ConvertWavToMp3(sourceForEncoding, finalMp3, post_LastExtractionQualiteIndex, baseName, post_TextBoxCDArtiste, parts(0), post_TextBoxCDTitre, post_TextBoxCDArtiste, post_TextBoxAnnee, post_ComboBoxGenre, post_TextBoxCommentaire, post_Pochette)
                                                     If convOk Then
                                                         Try
                                                             If File.Exists(wav) Then File.Delete(wav)
                                                         Catch
                                                         End Try
                                                     Else
                                                         Try
                                                             CDAudioAnalyzer.DiagnosticWrite($"BATCH_CONV_FAIL: album={albumDirForAnalysis} track={trackNum} wav={wav}")
                                                         Catch
                                                         End Try
                                                     End If
                                                 Catch exConvAll As Exception
                                                     Try
                                                         CDAudioAnalyzer.DiagnosticWrite($"BATCH_CONVERSION_ERROR: album={albumDirForAnalysis} wav={wav} Err={exConvAll.Message}")
                                                     Catch
                                                     End Try
                                                 End Try
                                             Catch
                                             End Try
                                         Next
                                     Catch
                                     End Try
                                 Catch exOuter As Exception
                                     Try
                                         If CDAudioAnalyzer.DiagnosticsToDiskEnabled Then
                                             Dim trace = Path.Combine(Path.GetTempPath(), "AudioPlay_wav_analyzer_trace.txt")
                                             File.AppendAllText(trace, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Background analysis error: {exOuter.Message}{Environment.NewLine}")
                                         End If
                                     Catch
                                     End Try
                                 End Try
                             End Function)
                Catch
                End Try

                ' Afficher le résultat
                Dim message As String
                Dim titreMessage As String
                Dim icone As MessageBoxIcon

                If annulationDemandee Then
                    ' Extraction annulée par l'utilisateur
                    message = String.Format(LanguageManager.GetString("Compressor_ExtractionCancelledMessage"), pistesReussies)
                    titreMessage = LanguageManager.GetString("Compressor_ExtractionCancelledTitle")
                    icone = MessageBoxIcon.Warning
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Extraction annulée: {pistesReussies} pistes extraites")
                Else
                    ' Extraction complète
                    message = String.Format(LanguageManager.GetString("Compressor_ExtractionSuccessMessage"), pistesReussies)
                    If pistesEchouees > 0 Then
                        message &= vbCrLf & String.Format(LanguageManager.GetString("Compressor_ExtractionFailedMessage"), pistesEchouees)
                    End If
                    titreMessage = LanguageManager.GetString("Compressor_ExtractionCompletedTitle")
                    icone = If(pistesEchouees = 0, MessageBoxIcon.Information, MessageBoxIcon.Warning)
                End If

                ' Remettre FormCompresser au premier plan pour afficher le message de fin
                Me.TopMost = True
                Me.BringToFront()
                System.Diagnostics.Debug.WriteLine("[FormCompresser] FormCompresser ramené au premier plan pour message de fin")

                MessageBox.Show(Me, message, titreMessage, MessageBoxButtons.OK, icone)

                ' Garder TopMost activé après le message pour que FormCompresser reste au-dessus
                ' L'utilisateur pourra toujours cliquer sur Form1 pour la ramener au premier plan si désiré
                System.Diagnostics.Debug.WriteLine("[FormCompresser] TopMost maintenu après message de fin")

            Finally
                ' Masquer les barres de progression (au cas où)
                LabelPisteEnCours.Visible = False
                ProgressBarPisteActuelle.Visible = False
                LabelProgressionGlobale.Visible = False
                ProgressBarGlobale.Visible = False

                ' Déverrouiller le CD si l'option était activée
                If CheckBoxVerouillerCD.Checked Then
                    DeverrouillerCD(lecteurCD)
                End If

                ' Éjecter le CD si l'option est activée
                If CheckBoxEjectCD.Checked Then
                    EjecterCD(lecteurCD)
                    ' Réinitialiser les métadonnées après éjection
                    ReinitialiserMetadonneesCD()
                End If

                ' Réactiver les contrôles
                ButtonExtraire.Visible = True
                ButtonExtraire.Enabled = True
                ButtonAnnuler.Visible = False
                ButtonAnnuler.Enabled = False
                ButtonQuitter.Enabled = True
                annulationDemandee = False ' Réinitialiser le flag
            End Try

        Catch ex As Exception
            ' Remettre FormCompresser au premier plan en cas d'erreur
            Me.TopMost = True
            Me.BringToFront()

            MessageBox.Show(Me, $"Erreur lors de l'extraction : {ex.Message}",
                          "Erreur",
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Error)

            ' Garder TopMost activé après le message d'erreur
            System.Diagnostics.Debug.WriteLine("[FormCompresser] TopMost maintenu après message d'erreur")
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire du clic sur le bouton Annuler
    ''' </summary>
    Private Sub ButtonAnnuler_Click(sender As Object, e As EventArgs) Handles ButtonAnnuler.Click
        ' Vérifier si une extraction est en cours
        If Not annulationDemandee Then
            ' Use localized Yes/No button text from resources via MessageBoxOptions
            Dim msg = LanguageManager.GetString("Compressor_CancelConfirmMessage")
            Dim title = LanguageManager.GetString("Compressor_CancelConfirmTitle")
            ' Use a custom localized dialog so button labels come from resources and theme can be applied
            Try
                Using dlg As New FormConfirmCancel(msg, title)
                    dlg.LocalizeButtons()
                    ' Apply the application's current theme to the dialog so it matches AudioPlay
                    Try
                        ThemeManager.ApplyThemeToForm(dlg)
                    Catch
                        ' fallback to copying basic colors from parent
                        Try
                            dlg.ApplyTheme(Me)
                        Catch
                        End Try
                    End Try
                    Dim dr = dlg.ShowDialog(Me)
                    Dim resultat = dr
                    If resultat = DialogResult.Yes Then
                        annulationDemandee = True
                        Try
                            If ctsExtraction IsNot Nothing Then
                                ctsExtraction.Cancel()
                            End If
                        Catch
                        End Try
                        LabelPisteEnCours.Text = LanguageManager.GetString("Compressor_CancelInProgress")
                        ButtonAnnuler.Enabled = False
                        System.Diagnostics.Debug.WriteLine("[FormCompresser] ⚠️ Annulation demandée par l'utilisateur (confirm dialog)")
                    End If
                End Using
            Catch
                ' fallback to MessageBox
                Dim resultat = MessageBox.Show(msg, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If resultat = DialogResult.Yes Then
                    annulationDemandee = True
                    Try
                        If ctsExtraction IsNot Nothing Then
                            ctsExtraction.Cancel()
                        End If
                    Catch
                    End Try
                    LabelPisteEnCours.Text = LanguageManager.GetString("Compressor_CancelInProgress")
                    ButtonAnnuler.Enabled = False
                    System.Diagnostics.Debug.WriteLine("[FormCompresser] ⚠️ Annulation demandée par l'utilisateur (fallback)")
                End If
            End Try

            ' legacy anchor removed
        End If
    End Sub

    ''' <summary>
    ''' Extrait une piste CD vers un fichier audio avec métadonnées
    ''' </summary>
    Private Async Function ExtrairePiste(pisteIndex As Integer) As Task
        If pistesCD Is Nothing OrElse pisteIndex < 0 OrElse pisteIndex >= pistesCD.Count Then
            Throw New ArgumentException("Index de piste invalide")
        End If

        Dim piste = pistesCD(pisteIndex)

        ' Lire en sécurité les informations affichées dans le ListView (UI thread)
        Dim numeroFichier As String = ""
        Dim titre As String = ""
        Dim artiste As String = TextBoxCDArtiste.Text
        Dim nextNumeroFichier As String = Nothing
        Dim nextTitre As String = Nothing
        Dim nextArtiste As String = Nothing
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub()
                              If ListViewCompress IsNot Nothing AndAlso pisteIndex >= 0 AndAlso pisteIndex < ListViewCompress.Items.Count Then
                                  Dim itm = ListViewCompress.Items(pisteIndex)
                                  numeroFichier = itm.Text
                                  If itm.SubItems.Count > 1 Then titre = itm.SubItems(1).Text Else titre = $"Piste {numeroFichier}"
                                  If itm.SubItems.Count > 2 Then artiste = itm.SubItems(2).Text Else artiste = TextBoxCDArtiste.Text
                              Else
                                  numeroFichier = (piste.TrackNumber).ToString()
                                  titre = $"Piste {numeroFichier}"
                                  artiste = TextBoxCDArtiste.Text
                              End If

                              If ListViewCompress IsNot Nothing AndAlso (pisteIndex + 1) < ListViewCompress.Items.Count Then
                                  Dim nitm = ListViewCompress.Items(pisteIndex + 1)
                                  nextNumeroFichier = nitm.Text
                                  If nitm.SubItems.Count > 1 Then nextTitre = nitm.SubItems(1).Text Else nextTitre = $"Piste {nextNumeroFichier}"
                                  If nitm.SubItems.Count > 2 Then nextArtiste = nitm.SubItems(2).Text Else nextArtiste = TextBoxCDArtiste.Text
                              End If
                          End Sub)
            Else
                If ListViewCompress IsNot Nothing AndAlso pisteIndex >= 0 AndAlso pisteIndex < ListViewCompress.Items.Count Then
                    Dim itm = ListViewCompress.Items(pisteIndex)
                    numeroFichier = itm.Text
                    If itm.SubItems.Count > 1 Then titre = itm.SubItems(1).Text Else titre = $"Piste {numeroFichier}"
                    If itm.SubItems.Count > 2 Then artiste = itm.SubItems(2).Text Else artiste = TextBoxCDArtiste.Text
                Else
                    numeroFichier = (piste.TrackNumber).ToString()
                    titre = $"Piste {numeroFichier}"
                    artiste = TextBoxCDArtiste.Text
                End If

                If ListViewCompress IsNot Nothing AndAlso (pisteIndex + 1) < ListViewCompress.Items.Count Then
                    Dim nitm = ListViewCompress.Items(pisteIndex + 1)
                    nextNumeroFichier = nitm.Text
                    If nitm.SubItems.Count > 1 Then nextTitre = nitm.SubItems(1).Text Else nextTitre = $"Piste {nextNumeroFichier}"
                    If nitm.SubItems.Count > 2 Then nextArtiste = nitm.SubItems(2).Text Else nextArtiste = TextBoxCDArtiste.Text
                End If
            End If
        Catch
            ' En cas d'erreur d'accès UI, fallback aux données de piste
            numeroFichier = (piste.TrackNumber).ToString()
            titre = $"Piste {numeroFichier}"
            artiste = TextBoxCDArtiste.Text
        End Try

        ' Chronométrage pour diagnostic des latences avant extraction
        Dim swTotal As New Stopwatch()
        Dim swPreAnalysis As New Stopwatch()
        Dim swCreateReader As New Stopwatch()
        Dim swRip As New Stopwatch()
        swTotal.Start()

        ' Capturer les valeurs UI dépendantes en toute sécurité pour éviter les accès inter-threads
        Dim formatLocal As String = "MP3"
        Dim qualiteIndexLocal As Integer = 0
        Dim repSauvegardeLocal As String = TextBoxRepSauvegarde.Text
        Dim anneeLocal As String = TextBoxAnnee.Text.Trim()
        Dim artisteAlbumLocal As String = TextBoxCDArtiste.Text.Trim()
        Dim nomAlbumLocal As String = TextBoxCDTitre.Text.Trim()
        Dim genreLocal As String = ""
        Dim commentaireLocal As String = ""
        Dim pochetteLocal As Image = Nothing
        Dim numeroDisqueLocal As String = TextBoxNumCD.Text
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub()
                              formatLocal = If(ComboBoxTypeConversion.SelectedItem?.ToString(), "MP3").ToUpper()
                              qualiteIndexLocal = If(ComboBoxQualiteConversion.SelectedIndex < 0, 0, ComboBoxQualiteConversion.SelectedIndex)
                              repSauvegardeLocal = TextBoxRepSauvegarde.Text
                              anneeLocal = TextBoxAnnee.Text.Trim()
                              artisteAlbumLocal = TextBoxCDArtiste.Text.Trim()
                              nomAlbumLocal = TextBoxCDTitre.Text.Trim()
                              genreLocal = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                              commentaireLocal = TextBoxCommentaire.Text
                              pochetteLocal = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)
                              numeroDisqueLocal = TextBoxNumCD.Text
                              ' remember for possible post-processing phase
                              lastExtractionFormat = formatLocal
                              lastExtractionQualiteIndex = qualiteIndexLocal
                              lastExtractionAutoEnabled = If(CheckBoxPostTraitementAuto IsNot Nothing, CheckBoxPostTraitementAuto.Checked, False)
                          End Sub)
            Else
                formatLocal = If(ComboBoxTypeConversion.SelectedItem?.ToString(), "MP3").ToUpper()
                qualiteIndexLocal = If(ComboBoxQualiteConversion.SelectedIndex < 0, 0, ComboBoxQualiteConversion.SelectedIndex)
                repSauvegardeLocal = TextBoxRepSauvegarde.Text
                anneeLocal = TextBoxAnnee.Text.Trim()
                artisteAlbumLocal = TextBoxCDArtiste.Text.Trim()
                nomAlbumLocal = TextBoxCDTitre.Text.Trim()
                genreLocal = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                commentaireLocal = TextBoxCommentaire.Text
                pochetteLocal = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)
                numeroDisqueLocal = TextBoxNumCD.Text
                ' remember for possible post-processing phase
                lastExtractionFormat = formatLocal
                lastExtractionQualiteIndex = qualiteIndexLocal
                lastExtractionAutoEnabled = If(CheckBoxPostTraitementAuto IsNot Nothing, CheckBoxPostTraitementAuto.Checked, False)
            End If
        Catch
            ' ignore UI read errors and use defaults
        End Try

        ' Les variables numeroFichier, titre, artiste ont été lues en toute sécurité depuis le thread UI plus haut

        ' Créer le répertoire de l'album: "(Année) Artiste - Album" en utilisant les valeurs capturées
        Dim annee As String = If(String.IsNullOrEmpty(anneeLocal), String.Empty, anneeLocal)
        Dim artisteAlbum As String = If(String.IsNullOrEmpty(artisteAlbumLocal), String.Empty, artisteAlbumLocal)
        Dim nomAlbum As String = If(String.IsNullOrEmpty(nomAlbumLocal), String.Empty, nomAlbumLocal)

        Dim nomRepertoireAlbum As String = String.Empty
        If Not String.IsNullOrEmpty(annee) Then
            nomRepertoireAlbum = $"({annee}) "
        End If
        nomRepertoireAlbum &= $"{artisteAlbum} - {nomAlbum}"
        nomRepertoireAlbum = NettoyerNomFichier(nomRepertoireAlbum)

        Dim cheminRepertoireAlbum As String = Path.Combine(If(String.IsNullOrEmpty(repSauvegardeLocal), TextBoxRepSauvegarde.Text, repSauvegardeLocal), nomRepertoireAlbum)

        ' Créer le répertoire s'il n'existe pas
        If Not Directory.Exists(cheminRepertoireAlbum) Then
            Directory.CreateDirectory(cheminRepertoireAlbum)
        End If

        ' Construire le nom de fichier: "NN - Artiste - Titre"
        ' Formater le numéro avec zéro initial (01, 02, ..., 10, 11, ...)
        Dim numeroFormate As String = Integer.Parse(numeroFichier).ToString("D2")
        Dim nomFichier As String = NettoyerNomFichier($"{numeroFormate} - {artiste} - {titre}")

        ' Obtenir le format, l'extension et la qualité choisie à partir des valeurs capturées
        Dim format As String = If(String.IsNullOrEmpty(formatLocal), "MP3", formatLocal).ToUpper()
        Dim extension As String = "." & format.ToLower()
        Dim qualiteIndex As Integer = qualiteIndexLocal

        Dim cheminComplet As String = Path.Combine(cheminRepertoireAlbum, nomFichier & extension)

        ' ═══ ANALYSER LA PISTE INDIVIDUELLEMENT JUSTE AVANT EXTRACTION ═══
        Dim pisteAExtraire As CDAudioManager.CDTrack = piste

        ' Appliquer immédiatement un pré-roll défensif pour la première piste
        Try
            If piste.TrackNumber = 1 Then
                Dim firstTrackPreRollSeconds As Double = GetPreRollSeconds()
                Dim preRollFrames As Integer = CInt(Math.Round(firstTrackPreRollSeconds * 75.0))
                Dim forcedStart As Integer = Math.Max(0, piste.StartFrame - preRollFrames)
                If pisteAExtraire.StartFrame <> forcedStart Then
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"PRE_ROLL_INIT: Track=1 originalTOC={piste.StartFrame} oldStart={pisteAExtraire.StartFrame} forcedStart={forcedStart} preRoll={firstTrackPreRollSeconds}s")
                    Catch
                    End Try
                    pisteAExtraire = New CDAudioManager.CDTrack With {
                        .Drive = piste.Drive,
                        .TrackNumber = piste.TrackNumber,
                        .Title = piste.Title,
                        .Artist = piste.Artist,
                        .StartFrame = forcedStart,
                        .EndFrame = piste.EndFrame,
                        .Duration = TimeSpan.FromSeconds((piste.EndFrame - forcedStart) / 75.0)
                    }
                Else
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"PRE_ROLL_INIT_NOOP: Track=1 start already {pisteAExtraire.StartFrame}")
                    Catch
                    End Try
                End If
            End If
        Catch
        End Try

        ' Démarrer le chrono de la pré-analyse (pour mesurer latence avant rip)
        swPreAnalysis.Start()
        If ParametresGlobaux.ModeTOCPrecis Then
            ' Mode TOC Précis : utiliser les positions TOC exactes sans modification
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] 📍 Extraction piste {numeroFichier} avec positions TOC EXACTES: {piste.StartFrame}-{piste.EndFrame}")
            ' Toutefois, pour la première piste, appliquer un petit pré-roll en reculant le début
            If piste.TrackNumber = 1 Then
                Dim firstTrackPreRollSeconds As Double = GetPreRollSeconds() ' 1.5-2s recommandé
                Dim preRollFrames As Integer = CInt(Math.Round(firstTrackPreRollSeconds * 75.0))
                Dim forcedStart As Integer = Math.Max(0, piste.StartFrame - preRollFrames)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] MODE_TOC_PRECIS: applying pre-roll for track 1, forcedStart={forcedStart} preRoll={firstTrackPreRollSeconds}s")
                pisteAExtraire = New CDAudioManager.CDTrack With {
                    .Drive = piste.Drive,
                    .TrackNumber = piste.TrackNumber,
                    .Title = piste.Title,
                    .Artist = piste.Artist,
                    .StartFrame = forcedStart,
                    .EndFrame = piste.EndFrame,
                    .Duration = TimeSpan.FromSeconds((piste.EndFrame - forcedStart) / 75.0)
                }
            End If
        Else
            ' Mode Normal : effectuer une ré-analyse finale pour chaque piste juste avant l''extraction
            Dim preAnalyse As CDAudioAnalyzer.TrackAnalysis = Nothing
            If analysesPistes.ContainsKey(pisteIndex) Then
                preAnalyse = analysesPistes(pisteIndex)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🔍 Pré-analyse disponible pour la piste {numeroFichier}")
            Else
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🔍 Pas de pré-analyse en cache pour la piste {numeroFichier}")
            End If

            ' Construire référence à la piste suivante et à l'analyse précédente (si disponible)
            Dim pisteSuivante As CDAudioManager.CDTrack = Nothing
            Dim previousAnalysis As CDAudioAnalyzer.TrackAnalysis = Nothing
            If pisteIndex + 1 < pistesCD.Count Then
                pisteSuivante = pistesCD(pisteIndex + 1)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    └─ Ré-analyse de la transition avec piste {pisteSuivante.TrackNumber}")
            End If
            If analysesPistes.ContainsKey(pisteIndex - 1) Then
                previousAnalysis = analysesPistes(pisteIndex - 1)
            End If

            ' Ré-analyse finale, toujours exécutée pour s'assurer des positions justes
            Dim finalAnalyse As CDAudioAnalyzer.TrackAnalysis = Nothing
            Try
                finalAnalyse = CDAudioAnalyzer.AnalyzeTrack(piste, pisteSuivante, previousAnalysis)
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur ré-analyse piste {numeroFichier}: {ex.Message}")
                finalAnalyse = preAnalyse
            End Try

            ' Mettre à jour le cache d'analyses
            If finalAnalyse IsNot Nothing Then
                analysesPistes(pisteIndex) = finalAnalyse
            End If

            ' Log avant/après pour diagnostic
            Try
                Dim beforeStart As Integer = If(preAnalyse IsNot Nothing, preAnalyse.AdjustedStartFrame, piste.StartFrame)
                Dim beforeEnd As Integer = If(preAnalyse IsNot Nothing, preAnalyse.AdjustedEndFrame, piste.EndFrame)
                CDAudioAnalyzer.DiagnosticWrite($"FINAL_REANALYSE_BEFORE: Track={piste.TrackNumber} Start={beforeStart} End={beforeEnd} FromPreAnalysis={(preAnalyse IsNot Nothing)}")
                If finalAnalyse IsNot Nothing Then
                    CDAudioAnalyzer.DiagnosticWrite($"FINAL_REANALYSE_AFTER: Track={piste.TrackNumber} Start={finalAnalyse.AdjustedStartFrame} End={finalAnalyse.AdjustedEndFrame} WasAdjusted={finalAnalyse.WasAdjusted} SilenceCenter={finalAnalyse.SilenceCenterFrame} PreferAdjustNextStart={finalAnalyse.PreferAdjustNextStart}")
                End If
            Catch
            End Try

            ' Appliquer le résultat final de l'analyse si un ajustement est proposé, sinon conserver les valeurs précédentes/TOC
            Dim appliedAnalyse As CDAudioAnalyzer.TrackAnalysis = finalAnalyse
            If appliedAnalyse Is Nothing OrElse Not appliedAnalyse.WasAdjusted Then
                appliedAnalyse = preAnalyse
            End If

            If appliedAnalyse IsNot Nothing AndAlso appliedAnalyse.WasAdjusted Then
                ' Créer une nouvelle piste avec les positions ajustées issues de la ré-analyse finale
                pisteAExtraire = New CDAudioManager.CDTrack With {
                    .Drive = piste.Drive,
                    .TrackNumber = piste.TrackNumber,
                    .Title = piste.Title,
                    .Artist = piste.Artist,
                    .StartFrame = appliedAnalyse.AdjustedStartFrame,
                    .EndFrame = appliedAnalyse.AdjustedEndFrame,
                    .Duration = TimeSpan.FromSeconds((appliedAnalyse.AdjustedEndFrame - appliedAnalyse.AdjustedStartFrame) / 75.0)
                }
                ' Safety override: never trim the start of the first track here (defensive, in case analysis produced a start trim)
                Try
                    If piste.TrackNumber = 1 Then
                        ' Déplacer le début de la première piste vers l'arrière d'une petite marge (pré-roll)
                        Dim firstTrackPreRollSeconds As Double = GetPreRollSeconds() ' valeur par défaut : 1.75s (~1.5-2s demandé)
                        Dim preRollFrames As Integer = CInt(Math.Round(firstTrackPreRollSeconds * 75.0))
                        Dim forcedStart As Integer = Math.Max(0, piste.StartFrame - preRollFrames)
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] OVERRIDE: forcing start of track 1 to TOC minus preRoll ({forcedStart}) - preRoll={firstTrackPreRollSeconds}s")
                        Try
                            appliedAnalyse.AdjustedStartFrame = forcedStart
                            ' Indiquer qu'aucune coupure de début n'est appliquée (on recule avant TOC)
                            appliedAnalyse.TrimmedStartFrames = 0
                            appliedAnalyse.WasAdjusted = appliedAnalyse.WasAdjusted AndAlso appliedAnalyse.TrimmedEndFrames > 0
                        Catch
                        End Try
                        ' Recreate pisteAExtraire with forced start
                        pisteAExtraire.StartFrame = appliedAnalyse.AdjustedStartFrame
                        pisteAExtraire.EndFrame = appliedAnalyse.AdjustedEndFrame
                        pisteAExtraire.Duration = TimeSpan.FromSeconds((appliedAnalyse.AdjustedEndFrame - appliedAnalyse.AdjustedStartFrame) / 75.0)
                    End If
                Catch
                End Try
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✅ Extraction avec positions AJUSTÉES (appliquées): {appliedAnalyse.AdjustedStartFrame}-{appliedAnalyse.AdjustedEndFrame}")
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    └─ Début: +{appliedAnalyse.TrimmedStartFrames / 75.0:F2}s ({appliedAnalyse.TrimmedStartFrames} frames)")
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    └─ Fin: -{appliedAnalyse.TrimmedEndFrames / 75.0:F2}s ({appliedAnalyse.TrimmedEndFrames} frames)")
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"FINAL_APPLY: Track={piste.TrackNumber} Original={piste.StartFrame}-{piste.EndFrame} Adjusted={appliedAnalyse.AdjustedStartFrame}-{appliedAnalyse.AdjustedEndFrame} TrimStartFrames={appliedAnalyse.TrimmedStartFrames} TrimEndFrames={appliedAnalyse.TrimmedEndFrames} SilenceCenter={appliedAnalyse.SilenceCenterFrame} PreferAdjustNextStart={appliedAnalyse.PreferAdjustNextStart} Message={appliedAnalyse.AnalysisMessage}")
                Catch
                End Try
            Else
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ℹ️ Extraction avec positions TOC (pas d'ajustement final)")
            End If

            ' Si des variables de silence existent, forcer les frames absolues centre->centre pour l'extraction
            Try
                If _silenceVariables IsNot Nothing AndAlso _silenceVariables.Count > 0 Then
                    Dim tn As Integer = piste.TrackNumber
                    Dim startFrameAbs As Integer = pisteAExtraire.StartFrame
                    Dim endFrameAbs As Integer = pisteAExtraire.EndFrame

                    If tn = 1 Then
                        If _silenceVariables.ContainsKey("silence1") Then
                            endFrameAbs = CInt(Math.Round(_silenceVariables("silence1") * 75.0))
                        End If
                    Else
                        Dim keyStart = $"silence{tn - 1}"
                        Dim keyEnd = $"silence{tn}"
                        If _silenceVariables.ContainsKey(keyStart) Then
                            startFrameAbs = CInt(Math.Round(_silenceVariables(keyStart) * 75.0))
                        End If
                        If _silenceVariables.ContainsKey(keyEnd) Then
                            endFrameAbs = CInt(Math.Round(_silenceVariables(keyEnd) * 75.0))
                        End If
                    End If

                    If startFrameAbs < 0 Then startFrameAbs = 0
                    If endFrameAbs <= startFrameAbs Then
                        endFrameAbs = pisteAExtraire.EndFrame
                    End If

                    pisteAExtraire = New CDAudioManager.CDTrack With {
                        .Drive = piste.Drive,
                        .TrackNumber = piste.TrackNumber,
                        .Title = piste.Title,
                        .Artist = piste.Artist,
                        .StartFrame = startFrameAbs,
                        .EndFrame = endFrameAbs,
                        .Duration = TimeSpan.FromSeconds((endFrameAbs - startFrameAbs) / 75.0)
                    }

                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"CREATE_READER_USING_SILENCE_VARIABLES: Track={tn} StartFrame={startFrameAbs} EndFrame={endFrameAbs} SilenceVars={_silenceVariables.Count}")
                    Catch
                    End Try
                End If
            Catch exSil As Exception
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"CREATE_READER_USING_SILENCE_VARIABLES_ERROR: Track={piste.TrackNumber} - {exSil.Message}")
                Catch
                End Try
            End Try
        End If

        ' Fin de la pré-analyse
        Try
            swPreAnalysis.Stop()
            CDAudioAnalyzer.DiagnosticWrite($"TIMING_PREANALYSIS: Track={piste.TrackNumber} ms={swPreAnalysis.ElapsedMilliseconds}")
        Catch
        End Try

        ' Créer le WaveStream pour lire le CD
        swCreateReader.Start()
        ' Essayer d'abord avec les frames issues des silences, autrement retomber sur le TOC
        Dim cdReaderRef As NAudio.Wave.WaveStream = Nothing
        Try
            ' TEMP DEBUG + appliquer un pré-roll pour la première piste avant création du lecteur
            Try
                Dim firstTrackPreRollSeconds As Double = GetPreRollSeconds()
                Dim preRollFrames As Integer = CInt(Math.Round(firstTrackPreRollSeconds * 75.0))
                Dim forcedStartDisplay As Integer = pisteAExtraire.StartFrame
                If piste.TrackNumber = 1 Then
                    Dim forcedStart As Integer = Math.Max(0, piste.StartFrame - preRollFrames)
                    forcedStartDisplay = forcedStart
                    If pisteAExtraire.StartFrame <> forcedStart Then
                        Try
                            CDAudioAnalyzer.DiagnosticWrite($"APPLY_PRE_ROLL: Track=1 originalTOC={piste.StartFrame} oldAppliedStart={pisteAExtraire.StartFrame} forcedStart={forcedStart} preRoll={firstTrackPreRollSeconds}s")
                        Catch
                        End Try
                        pisteAExtraire = New CDAudioManager.CDTrack With {
                            .Drive = piste.Drive,
                            .TrackNumber = piste.TrackNumber,
                            .Title = piste.Title,
                            .Artist = piste.Artist,
                            .StartFrame = forcedStart,
                            .EndFrame = pisteAExtraire.EndFrame,
                            .Duration = TimeSpan.FromSeconds((pisteAExtraire.EndFrame - forcedStart) / 75.0)
                        }
                    Else
                        Try
                            CDAudioAnalyzer.DiagnosticWrite($"APPLY_PRE_ROLL_NOOP: Track=1 start already {pisteAExtraire.StartFrame}")
                        Catch
                        End Try
                    End If
                End If

                Dim dbgMsg As String = $"DEBUG_PRE_CREATE_READER: Track={piste.TrackNumber} TOCStart={piste.StartFrame} ({piste.StartFrame / 75.0:F3}s) AppliedStart={pisteAExtraire.StartFrame} ({pisteAExtraire.StartFrame / 75.0:F3}s) ForcedStart={forcedStartDisplay} ({forcedStartDisplay / 75.0:F3}s) AppliedEnd={pisteAExtraire.EndFrame} ({pisteAExtraire.EndFrame / 75.0:F3}s) ModeTOCPrecis={ParametresGlobaux.ModeTOCPrecis}"
                Try
                    CDAudioAnalyzer.DiagnosticWrite(dbgMsg)
                Catch
                End Try
            Catch
            End Try

            cdReaderRef = CDAudioManager.CreerLecteurCDAudio(pisteAExtraire)
        Catch exCreate1 As Exception
            Try
                CDAudioAnalyzer.DiagnosticWrite($"CREATE_READER_EXCEPTION_SILENCES: Track={piste.TrackNumber} Err={exCreate1.Message}")
            Catch
            End Try
        End Try

        If cdReaderRef Is Nothing Then
            Try
                CDAudioAnalyzer.DiagnosticWrite($"CREATE_READER_NULL_SILENCES: Track={piste.TrackNumber} - attempting TOC fallback")
            Catch
            End Try
            Try
                cdReaderRef = CDAudioManager.CreerLecteurCDAudio(piste)
                If cdReaderRef IsNot Nothing Then
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"CREATE_READER_FALLBACK_TOC_OK: Track={piste.TrackNumber}")
                    Catch
                    End Try
                End If
            Catch exCreate2 As Exception
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"CREATE_READER_EXCEPTION_TOC: Track={piste.TrackNumber} Err={exCreate2.Message}")
                Catch
                End Try
            End Try
        End If

        Using cdReader = cdReaderRef
            If cdReader Is Nothing Then
                ' Échec de création du lecteur interne pour cette piste — créer un marqueur et passer à la suivante
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"CREATE_READER_FAIL_MARKER: Track={piste.TrackNumber} File={cheminRepertoireAlbum} Time={DateTime.UtcNow:o}")
                Catch
                End Try
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"CREATE_READER_FAIL: Track={piste.TrackNumber} - skipping extraction for this track")
                Catch
                End Try
                ' Ne pas tenter l'extraction interne pour cette piste
            Else
                swCreateReader.Stop()
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"TIMING_CREATE_READER: Track={piste.TrackNumber} ms={swCreateReader.ElapsedMilliseconds}")
                Catch
                End Try

            End If

            ' Si on force l'utilisation uniquement du ripper externe, exécuter ici
            If ParametresGlobaux.ForceOnlyExternalRipper Then
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER: external-only flow for Track={piste.TrackNumber} format={format}")
                Catch
                End Try

                ' Traiter les formats principaux: MP3 (WAV-first), WAV, FLAC, WMA
                Dim nomWavTemp As String
                Dim desiredWav As String = Path.Combine(cheminRepertoireAlbum, nomFichier & ".wav")
                If Not String.IsNullOrEmpty(cheminRepertoireAlbum) AndAlso Not File.Exists(desiredWav) Then
                    nomWavTemp = desiredWav
                Else
                    nomWavTemp = Path.Combine(cheminRepertoireAlbum, $"audioplay_temp_{Guid.NewGuid()}.wav")
                End If

                Try
                    Select Case format
                        Case "MP3"
                            ' Demander à freac de produire un WAV temporaire
                            ' Utiliser l'extracteur interne pour créer le WAV temporaire
                            Dim usedExt As Boolean = False
                            Try
                                Dim trackObj = pisteAExtraire
                                SafeInitProgressBar(100)
                                SafeUpdateProgressBar(0)
                                Await ExtraireWav(cdReader, nomWavTemp, titre, artiste, numeroFichier, pisteAExtraire.Duration.TotalSeconds)
                                usedExt = True
                            Catch exW As Exception
                                Try
                                    CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_FAIL_INTERNAL: internal rip failed Track={piste.TrackNumber} Err={exW.Message}")
                                Catch
                                End Try
                            End Try

                            If Not usedExt Then
                                Try
                                    CDAudioAnalyzer.DiagnosticWrite($"INTERNAL_RIP_FAIL_MARKER: Track={piste.TrackNumber} File={cheminRepertoireAlbum} Time={DateTime.UtcNow:o}")
                                Catch
                                End Try
                                Return
                            End If

                            ' Vérifier et resampler si nécessaire
                            Try
                                If File.Exists(nomWavTemp) Then
                                    ' Sauvegarder le WAV original produit par freac avant toute conversion
                                    ' Ne plus sauvegarder la copie .freac.orig pour éviter duplication
                                    ' Respecter l'option de test: SkipEnsureWavQuality
                                    If Not ParametresGlobaux.SkipEnsureWavQuality Then
                                        EnsureWavQuality(nomWavTemp, qualiteIndex)
                                    Else
                                        Try
                                            CDAudioAnalyzer.DiagnosticWrite($"SKIP_ENSURE_WAV_QUALITY: Skipped resample for {nomWavTemp}")
                                        Catch
                                        End Try
                                    End If
                                End If
                            Catch
                            End Try

                            ' Conserver le WAV sous un nom stable dans l'album avant encodage MP3
                            Try
                                If File.Exists(nomWavTemp) Then
                                    Dim stableName = $"{piste.TrackNumber:D2} - {MakeSafeFileName(artiste)} - {MakeSafeFileName(titre)}.wav"
                                    Dim stablePath = Path.Combine(cheminRepertoireAlbum, stableName)
                                    If Not String.Equals(Path.GetFullPath(nomWavTemp), Path.GetFullPath(stablePath), StringComparison.InvariantCultureIgnoreCase) Then
                                        If File.Exists(stablePath) Then
                                            stablePath = Path.Combine(cheminRepertoireAlbum, Path.GetFileNameWithoutExtension(stableName) & "_" & Guid.NewGuid().ToString("N") & ".wav")
                                        End If
                                        File.Move(nomWavTemp, stablePath)
                                        nomWavTemp = stablePath
                                    End If
                                End If
                            Catch
                            End Try

                            ' Convertir en MP3
                            ' Pairwise post-traitement: si le WAV précédent existe, tenter correction entre prev et current
                            Try
                                Dim prevIndex = Math.Max(0, pisteIndex - 1)
                                Dim prevTrack = pistesCD(prevIndex)
                                Dim prevWav = FindWavByTrackNumber(cheminRepertoireAlbum, prevTrack.TrackNumber)
                                If Not String.IsNullOrEmpty(prevWav) AndAlso File.Exists(prevWav) AndAlso File.Exists(nomWavTemp) Then
                                    ' Apply conservative pair correction for prev and current
                                    Try
                                        ApplyPairwiseCorrectionIfNeeded(prevWav, nomWavTemp, cheminRepertoireAlbum)
                                    Catch
                                    End Try
                                End If
                            Catch
                            End Try

                            ' Choisir fichier source pour encodage (préférer *_fixed.wav s'il existe)
                            Dim sourceForEncoding As String = nomWavTemp
                            Dim fixedCandidate = Path.Combine(cheminRepertoireAlbum, Path.GetFileNameWithoutExtension(nomWavTemp) & "_fixed.wav")
                            Try
                                If File.Exists(fixedCandidate) Then
                                    If IsValidWavFile(fixedCandidate) Then
                                        sourceForEncoding = fixedCandidate
                                    Else
                                        ' Invalid fixed file (likely header-only) -> delete and fall back to original
                                        Try
                                            File.Delete(fixedCandidate)
                                        Catch
                                        End Try
                                    End If
                                End If
                            Catch
                            End Try

                            ' Déférer la conversion: conserver le WAV pour un post-traitement complet
                            Try
                                CDAudioAnalyzer.DiagnosticWrite($"PENDING_CONVERSION_MARKER: Track={piste.TrackNumber} WAV={sourceForEncoding} Time={DateTime.UtcNow:o}")
                            Catch
                            End Try
                            Try
                                CDAudioAnalyzer.DiagnosticWrite($"DEFERRED_CONVERSION: Track={piste.TrackNumber} WAV={sourceForEncoding} Final={cheminComplet}")
                            Catch
                            End Try

                            ' Instrumentation additionnelle pour conversion MP3: marqueurs et logs dans le dossier d'album
                            Try
                                CDAudioAnalyzer.DiagnosticWrite($"CONV_ARGS_MARKER: Track={piste.TrackNumber} Format=MP3 Final={cheminComplet} Source={sourceForEncoding} BitrateIndex={qualiteIndex} Time={DateTime.UtcNow:o}")
                            Catch
                            End Try

                        Case "WAV"
                            ' WAV output: follow WAV-first semantics but apply center->center trimming
                            Dim nomWavTempLocal As String = Path.Combine(cheminRepertoireAlbum, $"audioplay_temp_{Guid.NewGuid()}.wav")
                            ' For WAV final output path, prefer using final filename early so WAV is kept with useful name if conversion fails
                            Dim desiredFinalWav = Path.Combine(cheminRepertoireAlbum, nomFichier & ".wav")
                            If Not File.Exists(desiredFinalWav) Then
                                nomWavTempLocal = desiredFinalWav
                            End If
                            Try
                                ' Create temporary WAV from CDReader
                                Try
                                    SafeInitProgressBar(100)
                                    SafeUpdateProgressBar(0)
                                    Await ExtraireWav(CDAudioManager.CreerLecteurCDAudio(pisteAExtraire), nomWavTempLocal, titre, artiste, numeroFichier, pisteAExtraire.Duration.TotalSeconds)
                                Catch exRip As Exception
                                    CDAudioAnalyzer.DiagnosticWrite($"INTERNAL_WAV_RIP_FAIL: Track={piste.TrackNumber} Err={exRip.Message}")
                                    Throw
                                End Try

                                ' Verify temp WAV
                                If Not File.Exists(nomWavTempLocal) OrElse Not IsValidWavFile(nomWavTempLocal) Then
                                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_ERROR: Track={piste.TrackNumber} WAV not created: {nomWavTempLocal}")
                                Else
                                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_CREATED: Track={piste.TrackNumber} WAV={nomWavTempLocal}")

                                    ' Determine cut seconds relative to WAV using detected centers
                                    Dim defaultStart = 0.0
                                    Dim defaultEnd = pisteAExtraire.Duration.TotalSeconds
                                    Dim cut = GetTrackCutSeconds(piste.TrackNumber, defaultStart, defaultEnd, pisteAExtraire.StartFrame)
                                    Dim startSecUsed = cut.Item1
                                    Dim endSecUsed = cut.Item2

                                    ' Write trimmed WAV directly to final path
                                    Try
                                        WriteWavSegment(nomWavTempLocal, cheminComplet, startSecUsed, endSecUsed)
                                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_FINAL_CREATED: Track={piste.TrackNumber} FinalWAV={cheminComplet} start={startSecUsed:F3} end={endSecUsed:F3}")

                                        ' Ensure quality/resample if needed
                                        If Not ParametresGlobaux.SkipEnsureWavQuality Then
                                            Try
                                                EnsureWavQuality(cheminComplet, qualiteIndex)
                                            Catch exQ As Exception
                                                CDAudioAnalyzer.DiagnosticWrite($"ENSURE_WAV_QUALITY_ERROR: {cheminComplet} - {exQ.Message}")
                                            End Try
                                        End If

                                        ' Write metadata using previously captured UI values to avoid cross-thread access
                                        Try
                                            EcrireMetadonnees(cheminComplet, titre, artiste, numeroFichier, nomAlbumLocal, artisteAlbumLocal, anneeLocal, genreLocal, commentaireLocal, pochetteLocal, numeroDisqueLocal)
                                        Catch exMeta As Exception
                                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_META_ERROR: Track={piste.TrackNumber} - {exMeta.Message}")
                                        End Try

                                        ' Remove temp wav
                                        Try
                                            If File.Exists(nomWavTempLocal) Then File.Delete(nomWavTempLocal)
                                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETED: Track={piste.TrackNumber} WAV={nomWavTempLocal}")
                                        Catch exDel As Exception
                                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETE_ERROR: Track={piste.TrackNumber} WAV={nomWavTempLocal} - {exDel.Message}")
                                        End Try
                                    Catch exTrim As Exception
                                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_TRIM_FAIL: Track={piste.TrackNumber} - {exTrim.Message}")
                                    End Try
                                End If
                            Catch
                                ' propagate or continue; errors already logged
                            End Try

                        Case "FLAC", "WMA"
                            ' Demander un WAV, puis utiliser ffmpeg pour encoder en FLAC/WMA
                            Dim usedExtFmt As Boolean = False
                            Try
                                Dim trackObj = pisteAExtraire
                                ' Initialiser la barre de progression à 0-100 pour le rip interne
                                SafeInitProgressBar(100)
                                SafeUpdateProgressBar(0)
                                Await ExtraireWav(cdReader, nomWavTemp, titre, artiste, numeroFichier, pisteAExtraire.Duration.TotalSeconds)
                                usedExtFmt = True
                            Catch
                            End Try

                            If Not usedExtFmt Then
                                CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_FAIL: freac did not produce WAV for Track={piste.TrackNumber}")
                                ' Do not write external_failed marker file in album folder
                                Return
                            End If

                            ' Convertir via ffmpeg
                            Dim ffmpegPath As String = Await TrouverFFMpeg()
                            If String.IsNullOrEmpty(ffmpegPath) Then
                                CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_FAIL: ffmpeg unavailable for Track={piste.TrackNumber}")
                                Return
                            End If

                            Dim convOk As Boolean = False
                            Dim ffmpegErrMsg As String = String.Empty
                            Try
                                If format = "FLAC" Then
                                    Dim compressionLevel As Integer = 8
                                    Select Case qualiteIndex
                                        Case 0 : compressionLevel = 0
                                        Case 1 : compressionLevel = 5
                                        Case 2 : compressionLevel = 8
                                    End Select
                                    Dim args = $"-i ""{nomWavTemp}'' -compression_level {compressionLevel} -y ""{cheminComplet}"""
                                    ' Correct quotes - build safely
                                    args = $"-i ""{nomWavTemp}"" -compression_level {compressionLevel} -y ""{cheminComplet}"""
                                    Dim pi As New ProcessStartInfo(ffmpegPath, args) With {
                                        .CreateNoWindow = True,
                                        .UseShellExecute = False,
                                        .RedirectStandardOutput = True,
                                        .RedirectStandardError = True
                                    }
                                    Dim p As Process = Process.Start(pi)
                                    ' Wait for exit but abort early if cancellation requested
                                    Do While Not p.HasExited
                                        If annulationDemandee OrElse (ctsExtraction IsNot Nothing AndAlso ctsExtraction.IsCancellationRequested) Then
                                            Try
                                                p.Kill(True)
                                            Catch
                                            End Try
                                            Exit Do
                                        End If
                                        Threading.Thread.Sleep(100)
                                    Loop
                                    Dim exitC As Integer = p.ExitCode
                                    Dim stderr As String = String.Empty
                                    Try
                                        stderr = p.StandardError.ReadToEnd()
                                    Catch
                                    End Try
                                    If exitC = 0 Then
                                        convOk = True
                                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_CONVERSION_SUCCESS: Track={piste.TrackNumber} Format=FLAC Out={cheminComplet}")
                                    Else
                                        ffmpegErrMsg = stderr
                                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_CONVERSION_FAIL: Track={piste.TrackNumber} Format=FLAC Exit={exitC} Err={ffmpegErrMsg}")
                                    End If
                                    ' No-op: keep diagnostic behavior; left for consistency (patch placeholder).
                                ElseIf format = "WMA" Then
                                    Dim bitrate As Integer = 256
                                    Select Case qualiteIndex
                                        Case 0 : bitrate = 128
                                        Case 1 : bitrate = 192
                                        Case 2 : bitrate = 256
                                    End Select
                                    Dim args = $"-i ""{nomWavTemp}"" -c:a wmav2 -b:a {bitrate}k -y ""{cheminComplet}"""
                                    Dim pi As New ProcessStartInfo(ffmpegPath, args) With {
                                        .CreateNoWindow = True,
                                        .UseShellExecute = False,
                                        .RedirectStandardOutput = True,
                                        .RedirectStandardError = True
                                    }
                                    Dim p As Process = Process.Start(pi)
                                    ' Wait for exit but abort early if cancellation requested
                                    Do While Not p.HasExited
                                        If annulationDemandee OrElse (ctsExtraction IsNot Nothing AndAlso ctsExtraction.IsCancellationRequested) Then
                                            Try
                                                p.Kill(True)
                                            Catch
                                            End Try
                                            Exit Do
                                        End If
                                        Threading.Thread.Sleep(100)
                                    Loop
                                    Dim exitC2 As Integer = p.ExitCode
                                    Dim stderr2 As String = String.Empty
                                    Try
                                        stderr2 = p.StandardError.ReadToEnd()
                                    Catch
                                    End Try
                                    If exitC2 = 0 Then
                                        convOk = True
                                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_CONVERSION_SUCCESS: Track={piste.TrackNumber} Format=WMA Out={cheminComplet}")
                                    Else
                                        ffmpegErrMsg = stderr2
                                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_CONVERSION_FAIL: Track={piste.TrackNumber} Format=WMA Exit={exitC2} Err={ffmpegErrMsg}")
                                    End If
                                End If
                            Catch exConv As Exception
                                CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_FAIL: conversion error Track={piste.TrackNumber} - {exConv.Message}")
                            Finally
                                Try
                                    If convOk Then
                                        If File.Exists(nomWavTemp) Then
                                            File.Delete(nomWavTemp)
                                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETED: Track={piste.TrackNumber} WAV={nomWavTemp}")
                                        End If
                                    Else
                                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_PRESERVED_AFTER_CONV_FAIL: Track={piste.TrackNumber} WAV={nomWavTemp}")
                                    End If
                                Catch exDelCon As Exception
                                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETE_ERROR: Track={piste.TrackNumber} WAV={nomWavTemp} - {exDelCon.Message}")
                                End Try
                            End Try

                        Case Else
                            CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_UNSUPPORTED_FORMAT: {format} for Track={piste.TrackNumber}")
                            CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_UNSUPPORTED_FORMAT_MARKER: {format} for Track={piste.TrackNumber} File={cheminRepertoireAlbum} Time={DateTime.UtcNow:o}")
                    End Select
                Catch exForce As Exception
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_EXCEPTION: Track={piste.TrackNumber} - {exForce.Message}")
                    Catch
                    End Try
                End Try

                ' Fin du flux externe — retourner pour éviter l'extraction interne
                Return
            End If

            ' Extraire selon le format choisi
            Select Case format
                Case "MP3"
                    ' Pipeline WAV-first: extraire d'abord en WAV, puis convertir en MP3
                    ' Create WAV temporary file in album directory for easier inspection and to avoid Temp folder AV interference
                    Dim nomWavTemp As String = $"audioplay_temp_{Guid.NewGuid()}.wav"
                    ' Prefer meaningful WAV filename for intermediate WAV used for MP3 conversion
                    Dim desiredMp3Wav = Path.Combine(cheminRepertoireAlbum, nomFichier & ".wav")
                    If Not File.Exists(desiredMp3Wav) Then
                        nomWavTemp = nomFichier & ".wav"
                    End If
                    Try
                        If Not String.IsNullOrEmpty(cheminRepertoireAlbum) Then
                            Directory.CreateDirectory(cheminRepertoireAlbum)
                        End If
                    Catch
                    End Try
                    ' Use album folder for intermediate WAV so the temporary WAV resides next to final outputs
                    Dim cheminWavTemp As String = Path.Combine(cheminRepertoireAlbum, nomWavTemp)
                    Try
                        Try
                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_BEGIN: Track={piste.TrackNumber} WAVTemp={cheminWavTemp} FinalFile={cheminComplet} StartFrame={pisteAExtraire.StartFrame} EndFrame={pisteAExtraire.EndFrame}")
                        Catch
                        End Try

                        ' Extraire en WAV temporaire
                        ' If freac (freaccmd) is available, prefer it for the raw rip to improve reliability
                        ' No-op behavior intentionally kept for clarity.
                        ' No-op behavior intentionally kept for clarity (formatting only).
                        ' External ripper (freac) disabled: use internal extractor for WAV temporary
                        If ParametresGlobaux.ForceOnlyExternalRipper Then
                            ' When forced to use only external ripper, skip internal extraction and log
                            Try
                                CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER: Skipping internal rip for Track={piste.TrackNumber}")
                            Catch
                            End Try
                        Else
                            Try
                                swRip.Start()
                                Await ExtraireWav(cdReader, cheminWavTemp, titre, artiste, numeroFichier, pisteAExtraire.Duration.TotalSeconds)
                                swRip.Stop()
                                Try
                                    CDAudioAnalyzer.DiagnosticWrite($"TIMING_RIP_LOCAL: Track={piste.TrackNumber} ms={swRip.ElapsedMilliseconds}")
                                Catch
                                End Try
                            Catch exIE As Exception
                                Try
                                    CDAudioAnalyzer.DiagnosticWrite($"INTERNAL_RIP_FAIL: Track={piste.TrackNumber} Err={exIE.Message}")
                                Catch
                                End Try
                                Throw
                            End Try
                        End If

                        ' Vérifier l'existence du WAV
                        If Not File.Exists(cheminWavTemp) Then
                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_ERROR: Track={piste.TrackNumber} WAV not created: {cheminWavTemp}")
                        Else
                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_CREATED: Track={piste.TrackNumber} WAV={cheminWavTemp}")

                            ' Convertir WAV -> MP3 selon la qualité choisie
                            ' Determine per-track cut seconds using detected silence centres
                            Dim defaultStart = 0.0
                            Dim defaultEnd = pisteAExtraire.Duration.TotalSeconds
                            Dim startSecUsed As Double = defaultStart
                            Dim endSecUsed As Double = defaultEnd

                            Try
                                ' Prefer explicit named silence variables if present (silence1, silence2, ...)
                                Dim trackNum As Integer = piste.TrackNumber
                                Dim wavStartAbs As Double = pisteAExtraire.StartFrame / 75.0
                                Dim wavEndAbs As Double = wavStartAbs + defaultEnd

                                If _silenceVariables IsNot Nothing AndAlso _silenceVariables.Count > 0 Then
                                    ' Track 1: start = wavStartAbs, end = silence1 if exists
                                    If trackNum = 1 Then
                                        startSecUsed = 0.0
                                        If _silenceVariables.ContainsKey("silence1") Then
                                            Dim absEndSec = _silenceVariables("silence1")
                                            endSecUsed = Math.Max(0.0, Math.Min(defaultEnd, absEndSec - wavStartAbs))
                                        Else
                                            endSecUsed = defaultEnd
                                        End If
                                    Else
                                        Dim keyStart = $"silence{trackNum - 1}"
                                        Dim keyEnd = $"silence{trackNum}"
                                        Dim absStartSec As Double = wavStartAbs
                                        Dim absEndSec As Double = wavEndAbs
                                        If _silenceVariables.ContainsKey(keyStart) Then
                                            absStartSec = _silenceVariables(keyStart)
                                        End If
                                        If _silenceVariables.ContainsKey(keyEnd) Then
                                            absEndSec = _silenceVariables(keyEnd)
                                        End If
                                        startSecUsed = Math.Max(0.0, Math.Min(defaultEnd, absStartSec - wavStartAbs))
                                        endSecUsed = Math.Max(0.0, Math.Min(defaultEnd, absEndSec - wavStartAbs))
                                    End If
                                ElseIf detectedSilenceCenters IsNot Nothing AndAlso detectedSilenceCenters.Count > 0 Then
                                    ' Fallback to detectedSilenceCenters list logic (center-to-center)
                                    Dim cdTrackStartAbs = pisteAExtraire.StartFrame / 75.0
                                    Dim absStart As Double = cdTrackStartAbs
                                    Dim absEnd As Double = cdTrackStartAbs + defaultEnd
                                    If piste.TrackNumber > 1 Then
                                        Dim idx = piste.TrackNumber - 2
                                        If idx >= 0 AndAlso idx < detectedSilenceCenters.Count Then
                                            absStart = detectedSilenceCenters(idx)
                                        End If
                                    End If
                                    Dim idxEnd = piste.TrackNumber - 1
                                    If idxEnd >= 0 AndAlso idxEnd < detectedSilenceCenters.Count Then
                                        absEnd = detectedSilenceCenters(idxEnd)
                                    End If
                                    startSecUsed = Math.Max(0.0, absStart - cdTrackStartAbs)
                                    endSecUsed = Math.Min(defaultEnd, Math.Max(0.0, absEnd - cdTrackStartAbs))
                                Else
                                    ' No silence data: fall back to full track
                                    startSecUsed = defaultStart
                                    endSecUsed = defaultEnd
                                End If

                                CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_USE_CUT: Track={piste.TrackNumber} startSec={startSecUsed:F3} endSec={endSecUsed:F3} centersCount={If(detectedSilenceCenters IsNot Nothing, detectedSilenceCenters.Count, 0)} silenceVarsCount={If(_silenceVariables IsNot Nothing, _silenceVariables.Count, 0)}")
                            Catch
                            End Try

                            ' Create a trimmed WAV for this track (centre->centre) and mark pending conversion
                            Dim convOk2 As Boolean = False
                            Try
                                Dim srcForTrim As String = cheminWavTemp
                                ' If a *_fixed.wav exists and is valid, prefer it as source for trimming only if no global centers exist
                                Try
                                    Dim fixedCandidate2 = Path.Combine(cheminRepertoireAlbum, Path.GetFileNameWithoutExtension(cheminWavTemp) & "_fixed.wav")
                                    If File.Exists(fixedCandidate2) AndAlso IsValidWavFile(fixedCandidate2) AndAlso (detectedSilenceCenters Is Nothing OrElse detectedSilenceCenters.Count = 0) Then
                                        srcForTrim = fixedCandidate2
                                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_SOURCE_FIXED_USED: Track={piste.TrackNumber} Source={srcForTrim}")
                                    End If
                                Catch
                                End Try

                                ' Always create final WAV trimmed by silence centers from the available source WAV
                                Dim finalWavPath As String = CreateFinalWavFromSource(srcForTrim, cheminRepertoireAlbum, artisteAlbumLocal, titre, piste.TrackNumber, pisteAExtraire.StartFrame, pisteAExtraire.Duration.TotalSeconds)

                                ' Immediately convert the WAV segment to the chosen format (per-track flow)
                                Try
                                    If String.Equals(format, "WAV", StringComparison.InvariantCultureIgnoreCase) Then
                                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_FINAL_NO_CONV: Track={piste.TrackNumber} WAV={finalWavPath}")
                                        convOk2 = True
                                    Else
                                        Dim finalOutPath As String = Path.Combine(cheminRepertoireAlbum, Path.GetFileNameWithoutExtension(finalWavPath) & ".mp3")
                                        ' UI: indicate conversion started for this trimmed WAV
                                        SafeSetLabelPisteEnCours(LanguageManager.GetString("Compressor_Converting"), True)
                                        SafeSetProgressBarMarquee(True)
                                        ' Use captured UI values for conversion to avoid cross-thread UI access
                                        convOk2 = Await ConvertWavToMp3(finalWavPath, finalOutPath, lastExtractionQualiteIndex, Path.GetFileNameWithoutExtension(finalWavPath), artisteAlbumLocal, piste.TrackNumber.ToString(), nomAlbumLocal, artisteAlbumLocal, anneeLocal, genreLocal, commentaireLocal, pochetteLocal)
                                        ' UI: restore after conversion
                                        SafeSetProgressBarMarquee(False)
                                        SafeSetLabelPisteEnCours("", False)
                                        If convOk2 Then
                                            Try
                                                If File.Exists(finalWavPath) Then File.Delete(finalWavPath)
                                                CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_SEGMENT_DELETED_AFTER_CONV: Track={piste.TrackNumber} WAV={finalWavPath}")
                                            Catch
                                            End Try
                                        Else
                                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_CONV_FAILED: Track={piste.TrackNumber} WAV={finalWavPath}")
                                        End If
                                    End If
                                Catch exCreate As Exception
                                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_SEGMENT_FATAL: Track={piste.TrackNumber} - {exCreate.Message}")
                                Finally
                                    ' Remove the temp rip WAV only if conversion (or no-conversion for WAV) succeeded and temp is not the final file
                                    ' No-op: formatting consistency anchor.
                                    Try
                                        If convOk2 Then
                                            If File.Exists(cheminWavTemp) AndAlso Not String.Equals(Path.GetFullPath(cheminWavTemp), Path.GetFullPath(finalWavPath), StringComparison.InvariantCultureIgnoreCase) Then
                                                File.Delete(cheminWavTemp)
                                            End If
                                        Else
                                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_PRESERVED_AFTER_CONV_FAIL: Track={piste.TrackNumber} WAV={cheminWavTemp}")
                                        End If
                                    Catch
                                    End Try
                                End Try
                            Catch exCreate As Exception
                                CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_SEGMENT_FATAL: Track={piste.TrackNumber} - {exCreate.Message}")
                            End Try
                            ' Use the actual per-track conversion result (convOk2) to decide whether to remove temp rip WAV
                            Dim conversionOk As Boolean = convOk2

                            ' Supprimer le WAV temporaire uniquement si la conversion a réussi
                            If conversionOk Then
                                Try
                                    If File.Exists(cheminWavTemp) Then
                                        File.Delete(cheminWavTemp)
                                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETED: Track={piste.TrackNumber} WAV={cheminWavTemp}")
                                    End If
                                Catch exDel As Exception
                                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETE_ERROR: Track={piste.TrackNumber} WAV={cheminWavTemp} - {exDel.Message}")
                                End Try
                            Else
                                Try
                                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_PRESERVED: Track={piste.TrackNumber} WAV={cheminWavTemp}")
                                Catch
                                End Try
                            End If
                        End If
                    Catch ex As Exception
                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_MP3_FROM_WAV_ERROR: Track={piste.TrackNumber} - {ex.Message}")
                    End Try
                Case "WAV"
                    ' Log the extraction begin info to diagnostics
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_BEGIN: Track={piste.TrackNumber} File={cheminComplet} StartFrame={pisteAExtraire.StartFrame} EndFrame={pisteAExtraire.EndFrame}")
                    Catch
                    End Try
                    ' Prefer external ripper when available
                    Dim usedExternalRipperWav As Boolean = False
                    Try
                        Dim trackObj = pisteAExtraire
                        SafeInitProgressBar(100)
                        SafeUpdateProgressBar(0)
                        ' Utiliser l'extracteur interne pour le WAV final
                        Await ExtraireWav(cdReader, cheminComplet, titre, artiste, numeroFichier, pisteAExtraire.Duration.TotalSeconds)
                        usedExternalRipperWav = True
                    Catch
                    End Try

                    If Not usedExternalRipperWav Then
                        If ParametresGlobaux.ForceOnlyExternalRipper Then
                            Try
                                CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER: Skipping internal rip (WAV) for Track={piste.TrackNumber}")
                            Catch
                            End Try
                        Else
                            Await ExtraireWav(cdReader, cheminComplet, titre, artiste, numeroFichier, pisteAExtraire.Duration.TotalSeconds)
                        End If
                    Else
                        ' If external rip produced the WAV, ensure it matches requested quality
                        Try
                            If File.Exists(cheminComplet) Then
                                If Not ParametresGlobaux.SkipEnsureWavQuality Then
                                    EnsureWavQuality(cheminComplet, qualiteIndex)
                                Else
                                    Try
                                        CDAudioAnalyzer.DiagnosticWrite($"SKIP_ENSURE_WAV_QUALITY: Skipped resample for final file {cheminComplet}")
                                    Catch
                                    End Try
                                End If
                            End If
                        Catch
                        End Try
                    End If
                Case "FLAC"
                    Await ExtraireFlac(cdReader, cheminComplet, titre, artiste, numeroFichier)
                Case "WMA"
                    Await ExtraireWma(cdReader, cheminComplet, titre, artiste, numeroFichier)
                Case Else
                    ' Par défaut, WAV
                    Dim usedExternalRipperDef As Boolean = False
                    Try
                        Dim trackObj = pisteAExtraire
                        SafeInitProgressBar(100)
                        SafeUpdateProgressBar(0)
                        Await ExtraireWav(cdReader, cheminComplet, titre, artiste, numeroFichier, pisteAExtraire.Duration.TotalSeconds)
                        usedExternalRipperDef = True
                    Catch
                    End Try

                    If Not usedExternalRipperDef Then
                        If ParametresGlobaux.ForceOnlyExternalRipper Then
                            Try
                                CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER: Skipping internal rip (default) for Track={piste.TrackNumber}")
                            Catch
                            End Try
                        Else
                            Await ExtraireWav(cdReader, cheminComplet, titre, artiste, numeroFichier, pisteAExtraire.Duration.TotalSeconds)
                        End If
                    Else
                        ' Ensure quality for external rip in default branch as well
                        Try
                            If File.Exists(cheminComplet) Then
                                EnsureWavQuality(cheminComplet, qualiteIndex)
                            End If
                        Catch
                        End Try
                    End If
            End Select
        End Using

        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Piste {numeroFichier} extraite: {cheminComplet}")

        ' Fin totale de l'extraction - journaliser les timings
        Try
            swTotal.Stop()
            CDAudioAnalyzer.DiagnosticWrite($"TIMING_TOTAL: Track={piste.TrackNumber} ms={swTotal.ElapsedMilliseconds}")
        Catch
        End Try

        ' Analyse automatique de frontière si la piste suivante existe et le fichier suivant est déjà présent
        Try
            If pisteIndex + 1 < pistesCD.Count AndAlso Not String.IsNullOrEmpty(nextNumeroFichier) Then
                Try
                    Dim useNextArtiste As String = If(String.IsNullOrEmpty(nextArtiste), TextBoxCDArtiste.Text, nextArtiste)
                    Dim useNextTitre As String = If(String.IsNullOrEmpty(nextTitre), $"Piste {nextNumeroFichier}", nextTitre)
                    Dim nextNumeroFormate As String = Integer.Parse(nextNumeroFichier).ToString("D2")
                    Dim nextNomFichier As String = NettoyerNomFichier($"{nextNumeroFormate} - {useNextArtiste} - {useNextTitre}")
                    Dim nextExtension As String = Path.GetExtension(cheminComplet)
                    Dim nextCheminComplet As String = Path.Combine(Path.GetDirectoryName(cheminComplet), nextNomFichier & nextExtension)

                    If File.Exists(nextCheminComplet) AndAlso File.Exists(cheminComplet) Then
                        Dim boundaryResult = CompareFilesBoundary(cheminComplet, nextCheminComplet)
                        ' No-op anchor after CompareFilesBoundary return-type update; kept for clarity.
                        CDAudioAnalyzer.DiagnosticWrite(String.Format("BOUNDARY_CHECK: Track={0} Next={1} SameBoundaryDetected={2} MatchBytes={3} A_head={4} A_tail={5} B_head={6} B_tail={7}", piste.TrackNumber, pistesCD(pisteIndex + 1).TrackNumber, boundaryResult.Found, boundaryResult.MatchLength, boundaryResult.AHeadPath, boundaryResult.ATailPath, boundaryResult.BHeadPath, boundaryResult.BTailPath))

                        ' Option A: si un chevauchement significatif est détecté, tronquer automatiquement la fin du fichier courant
                        Try
                            If boundaryResult.Found Then
                                Dim matchBytes As Integer = CInt(boundaryResult.MatchLength)
                                ' Seuil minimal pour action automatique (0.25s)
                                Dim minOverlapBytes As Integer = CInt(0.25 * 44100 * 2 * 2)
                                If matchBytes >= minOverlapBytes Then
                                    Dim ext As String = Path.GetExtension(cheminComplet).ToLowerInvariant()
                                    If ext = ".wav" Then
                                        TrimWavEndByBytes(cheminComplet, matchBytes)
                                        CDAudioAnalyzer.DiagnosticWrite($"BOUNDARY_AUTO_TRIM: Track={piste.TrackNumber} TrimmedBytes={matchBytes} File={cheminComplet}")
                                    Else
                                        CDAudioAnalyzer.DiagnosticWrite($"BOUNDARY_DETECTED_BUT_NONWAV: Track={piste.TrackNumber} MatchBytes={matchBytes} FinalFile={cheminComplet}")
                                    End If
                                End If
                            End If
                        Catch exAutoTrim As Exception
                            CDAudioAnalyzer.DiagnosticWrite($"BOUNDARY_AUTO_TRIM_ERROR: Track={piste.TrackNumber} - {exAutoTrim.Message}")
                        End Try
                    End If
                Catch exLocal As Exception
                    CDAudioAnalyzer.DiagnosticWrite($"BOUNDARY_CHECK_LOCAL_ERROR: Track={piste.TrackNumber} - {exLocal.Message}")
                End Try
            End If
        Catch exBoundary As Exception
            CDAudioAnalyzer.DiagnosticWrite($"BOUNDARY_CHECK_ERROR: Track={piste.TrackNumber} - {exBoundary.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Nettoie un nom de fichier en supprimant les caractères invalides
    ''' </summary>
    Private Function NettoyerNomFichier(nom As String) As String
        Dim invalides = Path.GetInvalidFileNameChars()
        For Each c In invalides
            nom = nom.Replace(c, "_"c)
        Next
        Return nom.Trim()
    End Function

    ''' <summary>
    ''' Copie un WaveStream avec rapport de progression
    ''' </summary>
    Private Sub CopierAvecProgression(source As NAudio.Wave.WaveStream, destination As System.IO.Stream)
        Const bufferSize As Integer = 32768 ' 32 KB
        Dim buffer(bufferSize - 1) As Byte
        Dim totalLength As Long = source.Length
        Dim totalRead As Long = 0
        Dim lastProgressUpdate As Integer = 0

        ' Initialiser la barre de progression de la piste via helper thread-safe
        SafeInitProgressBar(100)

        Dim bytesRead As Integer
        Do
            bytesRead = source.Read(buffer, 0, bufferSize)
            If bytesRead = 0 Then Exit Do

            ' Si annulation demandée, arrêter proprement la copie
            If annulationDemandee OrElse (ctsExtraction IsNot Nothing AndAlso ctsExtraction.IsCancellationRequested) Then
                Exit Do
            End If

            destination.Write(buffer, 0, bytesRead)
            totalRead += bytesRead

            ' Calculer le pourcentage
            Dim progressPercent As Integer = CInt((totalRead * 100) / totalLength)

            ' Mettre à jour la barre de progression (seulement si le pourcentage a changé)
            If progressPercent <> lastProgressUpdate AndAlso progressPercent <= 100 Then
                lastProgressUpdate = progressPercent
                ' Mettre à jour via helper
                Try
                    SafeUpdateProgressBar(progressPercent)
                Catch
                    ' Ignorer
                End Try
            End If
        Loop
    End Sub

    ''' <summary>
    ''' Crée un WaveStream avec ajustement de volume basé sur NumericUpDown_DB (1-100, défaut 95)
    ''' </summary>
    Private Function AppliquerAjustementVolume(source As NAudio.Wave.WaveStream, Optional volumePercentLocal As Nullable(Of Decimal) = Nothing) As NAudio.Wave.WaveStream
        Try
            ' Récupérer la valeur du volume (1-100, défaut 95)
            Dim volumePercent As Decimal = 95D ' Valeur par défaut sécuritaire

            If volumePercentLocal.HasValue Then
                volumePercent = volumePercentLocal.Value
            Else
                If Me.InvokeRequired Then
                    Me.Invoke(Sub() volumePercent = NumericUpDown_DB.Value)
                Else
                    volumePercent = NumericUpDown_DB.Value
                End If
            End If

            ' Si le volume est à 95% ou plus, pas besoin d'ajustement (tolérance pour éviter les problèmes)
            If volumePercent >= 95D Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Volume à {volumePercent}%, pas d'ajustement nécessaire")
                Return source
            End If

            ' Convertir en multiplicateur (95 = 0.95, 100 = 1.0, 50 = 0.5, etc.)
            Dim volumeMultiplier As Single = CSng(volumePercent / 100D)

            ' Créer un WaveChannel32 pour ajuster le volume
            ' IMPORTANT: WaveChannel32 préserve Position et Length correctement
            Dim volumeStream As New NAudio.Wave.WaveChannel32(source) With {
                .Volume = volumeMultiplier,
                .PadWithZeroes = False
            }

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Ajustement volume: {volumePercent}% (multiplicateur: {volumeMultiplier})")

            ' Retourner comme WaveStream
            Return volumeStream

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur ajustement volume: {ex.Message}")
            ' En cas d'erreur, retourner la source originale sans modification
            Return source
        End Try
    End Function

    ''' <summary>
    ''' Extrait une piste en MP3
    ''' </summary>
    Private Async Function ExtraireMp3(source As NAudio.Wave.WaveStream, piste As CDAudioManager.CDTrack, cheminFichier As String,
                                       titre As String, artiste As String, numeroPiste As String) As Task
        ' Si ForceOnlyExternalRipper est activé, empêcher toute extraction interne
        Try
            If ParametresGlobaux.ForceOnlyExternalRipper Then
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_HARD_SKIP: Internal MP3 rip skipped for {cheminFichier}")
                Catch
                End Try
                ' Marker skipped: do not write marker file to album folder to avoid clutter; keep diagnostic log
                CDAudioAnalyzer.DiagnosticWrite($"INTERNAL_SKIP_MARKER: SKIPPED {DateTime.UtcNow:o} ForceOnlyExternalRipper=true File={cheminFichier}")
                Return
            End If
        Catch
        End Try

        ' Capturer toutes les valeurs UI AVANT Task.Run pour éviter les erreurs inter-threads
        Dim qualiteIndex As Integer = 0
        Dim album As String = ""
        Dim artisteAlbum As String = ""
        Dim annee As String = ""
        Dim genre As String = ""
        Dim commentaire As String = ""
        Dim numeroDisque As String = ""
        Dim pochette As Image = Nothing
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub()
                              qualiteIndex = If(ComboBoxQualiteConversion.SelectedIndex < 0, 0, ComboBoxQualiteConversion.SelectedIndex)
                              album = TextBoxCDTitre.Text
                              artisteAlbum = TextBoxCDArtiste.Text
                              annee = TextBoxAnnee.Text
                              genre = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                              commentaire = TextBoxCommentaire.Text
                              numeroDisque = TextBoxNumCD.Text
                              pochette = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)
                          End Sub)
            Else
                qualiteIndex = If(ComboBoxQualiteConversion.SelectedIndex < 0, 0, ComboBoxQualiteConversion.SelectedIndex)
                album = TextBoxCDTitre.Text
                artisteAlbum = TextBoxCDArtiste.Text
                annee = TextBoxAnnee.Text
                genre = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                commentaire = TextBoxCommentaire.Text
                numeroDisque = TextBoxNumCD.Text
                pochette = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)
            End If
        Catch
        End Try
        ' Capturer la valeur de normalisation (NumericUpDown_DB) pour éviter l'accès inter-thread plus tard
        Dim volumePercentCaptured As Decimal = NumericUpDown_DB.Value

        ' Index 0=128, 1=192, 2=256, 3=320
        Dim bitrate As Integer = 320 ' Par défaut

        Select Case qualiteIndex
            Case 0 ' Basse (128 kbps)
                bitrate = 128
            Case 1 ' Moyenne (192 kbps)
                bitrate = 192
            Case 2 ' Haute (256 kbps)
                bitrate = 256
            Case 3 ' Très haute (320 kbps)
                bitrate = 320
            Case Else ' Fallback
                bitrate = 320
        End Select

        ' Appliquer l'ajustement de volume en utilisant la valeur capturée
        Dim sourceAvecVolume = AppliquerAjustementVolume(source, volumePercentCaptured)

        ' Base filename to use for temporary WAV when we prefer meaningful name
        Dim nomFichierBase As String = Path.GetFileNameWithoutExtension(cheminFichier)

        Try
            ' WAV-first MP3 conversion: write a WAV in the album folder, then convert via existing ConvertWavToMp3
            Await Task.Run(Async Function()
                               Try
                                   Dim albumFolder As String = Path.GetDirectoryName(cheminFichier)
                                   If Not String.IsNullOrEmpty(albumFolder) Then
                                       Directory.CreateDirectory(albumFolder)
                                   End If

                                   Dim tmpWavName As String = $"audioplay_temp_{Guid.NewGuid()}.wav"
                                   ' Prefer final filename for temporary WAV so it's meaningful if left after failed conversion
                                   Dim finalWavCandidate As String = Path.Combine(albumFolder, nomFichierBase & ".wav")
                                   If Not String.IsNullOrEmpty(albumFolder) AndAlso Not File.Exists(finalWavCandidate) Then
                                       tmpWavName = nomFichierBase & ".wav"
                                   End If
                                   Dim tmpWav As String = Path.Combine(If(String.IsNullOrEmpty(albumFolder), Path.GetTempPath(), albumFolder), tmpWavName)

                                   ' Write WAV to disk from sourceAvecVolume
                                   Try
                                       sourceAvecVolume.Position = 0
                                       Using wavWriter As New NAudio.Wave.WaveFileWriter(tmpWav, sourceAvecVolume.WaveFormat)
                                           Const bufSize As Integer = 32768
                                           Dim buffer(bufSize - 1) As Byte
                                           Dim read As Integer
                                           Do
                                               read = sourceAvecVolume.Read(buffer, 0, bufSize)
                                               If read <= 0 Then Exit Do
                                               wavWriter.Write(buffer, 0, read)
                                           Loop
                                       End Using
                                       CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_CREATED_FOR_MP3: Track={piste.TrackNumber} WAV={tmpWav}")
                                   Catch exW As Exception
                                       CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_MP3_WAV_WRITE_FAIL: Track={piste.TrackNumber} Err={exW.Message}")
                                       Exit Function
                                   End Try

                                   ' Ensure WAV is fully written and stable before conversion
                                   If Not WaitForFileReady(tmpWav, 15000) Then
                                       CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_MP3_WAV_NOT_READY: Track={piste.TrackNumber} WAV={tmpWav}")
                                       Exit Function
                                   End If

                                   ' UI: indicate conversion started
                                   SafeSetLabelPisteEnCours(LanguageManager.GetString("Compressor_Converting"), True)
                                   SafeSetProgressBarMarquee(True)

                                   ' Use ConvertWavToMp3 to encode and write metadata
                                   Dim convOk As Boolean = False
                                   Try
                                       convOk = Await ConvertWavToMp3(tmpWav, cheminFichier, qualiteIndex, titre, artiste, numeroPiste, album, artisteAlbum, annee, genre, commentaire, pochette)
                                   Catch exC As Exception
                                       CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_MP3_CONV_EXCEPTION: Track={piste.TrackNumber} Err={exC.Message}")
                                       Try
                                           File.WriteAllText(Path.Combine(Path.GetDirectoryName(tmpWav), $"track_{piste.TrackNumber}.conv_failed.txt"), exC.ToString())
                                       Catch
                                       End Try
                                       convOk = False
                                   End Try

                                   ' UI: restore labels/progress after conversion
                                   SafeSetProgressBarMarquee(False)
                                   SafeSetLabelPisteEnCours("", False)

                                   If convOk Then
                                       Try
                                           If File.Exists(tmpWav) Then File.Delete(tmpWav)
                                           CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETED_AFTER_MP3: Track={piste.TrackNumber} WAV={tmpWav}")
                                       Catch exDel As Exception
                                           CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETE_ERROR: Track={piste.TrackNumber} WAV={tmpWav} - {exDel.Message}")
                                       End Try
                                   Else
                                       CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_CONV_FAILED_PRESERVE_WAV: Track={piste.TrackNumber} WAV={tmpWav}")
                                   End If
                               Catch exOuter As Exception
                                   CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_MP3_TASK_ERROR: Track={piste.TrackNumber} - {exOuter.Message}")
                               End Try
                           End Function)
        Finally
            ' Disposer le stream de volume si c'est un wrapper différent de la source
            If sourceAvecVolume IsNot source Then
                sourceAvecVolume?.Dispose()
            End If

            ' Libérer la copie de l'image
            pochette?.Dispose()
        End Try
    End Function

    ''' <summary>
    ''' Extrait une piste en WAV avec métadonnées
    ''' </summary>
    Private Async Function ExtraireWav(source As NAudio.Wave.WaveStream, cheminFichier As String,
                                       titre As String, artiste As String, numeroPiste As String,
                                      Optional expectedSeconds As Double = -1.0) As Task
        ' Si l'option ForceOnlyExternalRipper est activée, empêcher explicitement
        ' toute extraction interne même si une autre partie du code l'appelle par erreur.
        Try
            If ParametresGlobaux.ForceOnlyExternalRipper Then
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_HARD_SKIP: Internal WAV rip skipped for {cheminFichier}")
                Catch
                End Try
                ' Écrire un marqueur fichier pour diagnostic à côté du chemin cible
                Try
                    Dim marker As String = cheminFichier & ".internal_skip.txt"
                    File.WriteAllText(marker, $"SKIPPED {DateTime.UtcNow:o} ForceOnlyExternalRipper=true")
                Catch
                End Try
                Return
            End If
        Catch
        End Try

        ' Capturer toutes les valeurs UI AVANT Task.Run (thread-safe)
        Dim qualiteIndex As Integer = 0
        Dim album As String = String.Empty
        Dim artisteAlbum As String = String.Empty
        Dim annee As String = String.Empty
        Dim genre As String = String.Empty
        Dim commentaire As String = String.Empty
        Dim pochette As Image = Nothing
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub()
                              qualiteIndex = If(ComboBoxQualiteConversion.SelectedIndex < 0, 0, ComboBoxQualiteConversion.SelectedIndex)
                              album = TextBoxCDTitre.Text
                              artisteAlbum = TextBoxCDArtiste.Text
                              annee = TextBoxAnnee.Text
                              genre = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                              commentaire = TextBoxCommentaire.Text
                              If PictureBoxPochette.Image IsNot Nothing Then
                                  pochette = New Bitmap(PictureBoxPochette.Image)
                              End If
                          End Sub)
            Else
                qualiteIndex = If(ComboBoxQualiteConversion.SelectedIndex < 0, 0, ComboBoxQualiteConversion.SelectedIndex)
                album = TextBoxCDTitre.Text
                artisteAlbum = TextBoxCDArtiste.Text
                annee = TextBoxAnnee.Text
                genre = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                commentaire = TextBoxCommentaire.Text
                If PictureBoxPochette.Image IsNot Nothing Then
                    pochette = New Bitmap(PictureBoxPochette.Image)
                End If
            End If
        Catch
        End Try

        ' Déterminer le format de sortie selon l'index de qualité
        ' Index 0=16-bit 44.1kHz, 1=24-bit 96kHz, 2=32-bit 192kHz
        Dim sampleRate As Integer = 96000 ' Par défaut
        Dim bitDepth As Integer = 24

        Select Case qualiteIndex
            Case 0 ' PCM 16-bit 44.1 kHz
                sampleRate = 44100
                bitDepth = 16
            Case 1 ' PCM 24-bit 96 kHz
                sampleRate = 96000
                bitDepth = 24
            Case 2 ' PCM 32-bit 192 kHz
                sampleRate = 192000
                bitDepth = 32
            Case Else ' Fallback
                sampleRate = 96000
                bitDepth = 24
        End Select

        ' Appliquer l'ajustement de volume
        Dim sourceAvecVolume = AppliquerAjustementVolume(source)

        ' Defensive checks: ensure source is valid and target directory exists
        Try
            If source Is Nothing Then
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_ENTRY_FAIL: source stream is Nothing for File={cheminFichier}")
                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_NO_SOURCE: File={cheminFichier} Time={DateTime.UtcNow:o}")
                Catch
                End Try
                Return
            End If
        Catch
        End Try
        Try
            Dim dir = Path.GetDirectoryName(cheminFichier)
            If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then Directory.CreateDirectory(dir)
        Catch
        End Try

        Try
            ' Écrire un marqueur indiquant que l'extraction interne commence (diagnostic)
            Try
                CDAudioAnalyzer.DiagnosticWrite($"INTERNAL_WAV_RIP_BEGIN: File={cheminFichier}")
            Catch
            End Try

            ' Write an initial entry marker with stream properties to help diagnose encoding problems
            Try
                Dim entryInfo = $"ENTRY: File={cheminFichier} SourceSampleRate={source.WaveFormat.SampleRate} Bits={source.WaveFormat.BitsPerSample} Channels={source.WaveFormat.Channels} LengthBytes={source.Length}"
                CDAudioAnalyzer.DiagnosticWrite(entryInfo)
            Catch
            End Try

            ' Lancer le travail d'extraction dans un Task et capturer toute exception au niveau appelant
            Try
                CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_TASK_START: File={cheminFichier}")
                ' Task started (diagnostic trace only)
                CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_TASK_MARKER: TASK_STARTED File={cheminFichier} Time={DateTime.UtcNow:o}")
                Await Task.Run(Sub()
                                   ' Encapsuler tout le bloc de lecture/écriture dans un try/catch pour logger les erreurs
                                   Try
                                       CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_TASK_RUNNING: File={cheminFichier}")
                                       ' Si le format source est déjà le format cible (CD audio standard), pas de conversion
                                       If sourceAvecVolume.WaveFormat.SampleRate = sampleRate AndAlso sourceAvecVolume.WaveFormat.BitsPerSample = bitDepth Then
                                           Using writer As New NAudio.Wave.WaveFileWriter(cheminFichier, sourceAvecVolume.WaveFormat)
                                               CopierAvecProgression(sourceAvecVolume, writer)
                                           End Using
                                       Else
                                           ' Créer le format cible
                                           Dim targetFormat As New NAudio.Wave.WaveFormat(sampleRate, bitDepth, sourceAvecVolume.WaveFormat.Channels)

                                           ' Utiliser un resampler pour convertir
                                           Using resampler As New NAudio.Wave.MediaFoundationResampler(sourceAvecVolume, targetFormat)
                                               resampler.ResamplerQuality = 60 ' Haute qualité
                                               Using writer As New NAudio.Wave.WaveFileWriter(cheminFichier, targetFormat)
                                                   ' Copier manuellement depuis le resampler
                                                   Const bufferSize As Integer = 32768
                                                   Dim buffer(bufferSize - 1) As Byte
                                                   Dim bytesRead As Integer

                                                   ' Estimer la longueur totale (approximation basée sur le ratio des sample rates)
                                                   Dim estimatedLength As Long = CLng(source.Length * (sampleRate / source.WaveFormat.SampleRate))
                                                   Dim totalRead As Long = 0
                                                   Dim lastProgressUpdate As Integer = 0

                                                   ' Initialiser la barre de progression de piste via helper thread-safe
                                                   SafeInitProgressBar(100)
                                                   SafeUpdateProgressBar(0)

                                                   Do
                                                       bytesRead = resampler.Read(buffer, 0, bufferSize)
                                                       If bytesRead = 0 Then Exit Do

                                                       writer.Write(buffer, 0, bytesRead)
                                                       totalRead += bytesRead

                                                       ' Calculer le pourcentage
                                                       Dim progressPercent As Integer = CInt(Math.Min((totalRead * 100) / estimatedLength, 100))

                                                       ' Mettre à jour la barre de progression
                                                       If progressPercent <> lastProgressUpdate AndAlso progressPercent <= 100 Then
                                                           lastProgressUpdate = progressPercent
                                                           Try
                                                               ' Utiliser le helper thread-safe pour mettre à jour la progression
                                                               SafeUpdateProgressBar(progressPercent)
                                                           Catch
                                                               ' Ignorer les erreurs d'invocation
                                                           End Try
                                                       End If
                                                   Loop
                                               End Using
                                           End Using
                                       End If
                                   Catch exTaskRun As Exception
                                       Try
                                           CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_WRITE_ERROR: {cheminFichier} - {exTaskRun.Message}{Environment.NewLine}{exTaskRun.ToString()}")
                                       Catch
                                       End Try
                                       Throw
                                   End Try

                                   ' Écrire les métadonnées avec TagLib (supporté pour WAV via RIFF INFO)
                                   ' Avant d'écrire les métadonnées, s'assurer que le WAV a la bonne longueur
                                   Try
                                       Dim waveFormatUsed As NAudio.Wave.WaveFormat = Nothing
                                       ' Déterminer le format effectivement utilisé dans le fichier WAV
                                       If sourceAvecVolume.WaveFormat.SampleRate = sampleRate AndAlso sourceAvecVolume.WaveFormat.BitsPerSample = bitDepth Then
                                           waveFormatUsed = sourceAvecVolume.WaveFormat
                                       Else
                                           waveFormatUsed = New NAudio.Wave.WaveFormat(sampleRate, bitDepth, sourceAvecVolume.WaveFormat.Channels)
                                       End If

                                       If expectedSeconds > 0 AndAlso waveFormatUsed IsNot Nothing Then
                                           ' Calculer le nombre d'octets de données audio attendu pour la durée analysée
                                           Dim expectedDataBytes As Long = CLng(Math.Round(expectedSeconds * waveFormatUsed.SampleRate * waveFormatUsed.Channels * (waveFormatUsed.BitsPerSample / 8.0)))
                                           TruncateWavDataChunk(cheminFichier, expectedDataBytes)
                                       Else
                                           ' Fallback: utiliser la taille réelle disponible
                                           TruncateWavDataChunk(cheminFichier, -1)
                                       End If
                                   Catch exTrunc As Exception
                                       CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_TRUNCATE_ERROR: {cheminFichier} - {exTrunc.Message}")
                                   End Try

                                   ' Sauvegarder des snippets pour diagnostic
                                   Try
                                       SaveWavSnippetsIfRequested(cheminFichier)
                                   Catch
                                   End Try

                                   EcrireMetadonnees(cheminFichier, titre, artiste, numeroPiste, album, artisteAlbum, annee, genre, commentaire, pochette, TextBoxNumCD.Text)
                               End Sub)
            Catch exTaskOuter As Exception
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_TASK_FAILED: File={cheminFichier} - {exTaskOuter.Message}{Environment.NewLine}{exTaskOuter.ToString()}")
                Catch
                End Try
                Throw
            End Try
        Finally
            ' Disposer le stream de volume si c'est un wrapper différent de la source
            If sourceAvecVolume IsNot source Then
                sourceAvecVolume?.Dispose()
            End If

            ' Libérer la copie de l'image
            pochette?.Dispose()
        End Try
    End Function

    ''' <summary>
    ''' Tronque le chunk DATA d'un fichier WAV pour qu'il corresponde à expectedDataBytes.
    ''' expectedDataBytes représente la taille des données audio (pas l'ensemble du fichier).
    ''' Cette fonction ouvre le fichier WAV, vérifie le header RIFF/WAVE et ajuste la taille du chunk "data"
    ''' en modifiant les champs de taille dans l'en-tête si le fichier contient plus d'octets que prévu.
    ''' </summary>
    Private Sub TruncateWavDataChunk(wavPath As String, expectedDataBytes As Long)
        Try
            If String.IsNullOrWhiteSpace(wavPath) Then Return
            If Not File.Exists(wavPath) Then Return
            If expectedDataBytes <= 0 Then Return

            Using fs As New FileStream(wavPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
                Dim br As New BinaryReader(fs, System.Text.Encoding.ASCII, leaveOpen:=True)
                Dim bw As New BinaryWriter(fs, System.Text.Encoding.ASCII, leaveOpen:=True)

                ' Lire RIFF header
                fs.Seek(0, SeekOrigin.Begin)
                Dim riff = br.ReadBytes(4)
                If System.Text.Encoding.ASCII.GetString(riff) <> "RIFF" Then Return

                ' Taille RIFF file size (4 bytes)
                Dim riffSize As UInt32 = br.ReadUInt32()

                Dim wave = br.ReadBytes(4)
                If System.Text.Encoding.ASCII.GetString(wave) <> "WAVE" Then Return

                ' Parcourir les chunks jusqu'à trouver 'data' et récupérer fmt si présent
                Dim foundData As Boolean = False
                Dim dataChunkOffset As Long = -1
                Dim dataChunkSize As UInt32 = 0
                Dim channels As Integer = -1
                Dim sampleRate As Integer = -1
                Dim bitsPerSample As Integer = -1

                While fs.Position < fs.Length - 8
                    Dim chunkIdBytes = br.ReadBytes(4)
                    If chunkIdBytes.Length < 4 Then Exit While
                    Dim chunkId = System.Text.Encoding.ASCII.GetString(chunkIdBytes)
                    Dim chunkSize As UInt32 = 0
                    Try
                        chunkSize = br.ReadUInt32()
                    Catch ex As EndOfStreamException
                        Exit While
                    End Try

                    If chunkId = "fmt " Then
                        ' Lire le chunk fmt pour obtenir format audio
                        Dim fmtStart As Long = fs.Position
                        Try
                            Dim audioFormat As UInt16 = br.ReadUInt16()
                            channels = br.ReadUInt16()
                            sampleRate = br.ReadInt32()
                            Dim byteRate As UInt32 = br.ReadUInt32()
                            Dim blockAlign As UInt16 = br.ReadUInt16()
                            bitsPerSample = br.ReadUInt16()
                        Catch
                            ' Si lecture fmt échoue, on ignore et continue
                        End Try
                        ' Avancer à la fin du chunk (gestion de padding si nécessaire)
                        Dim consumed As Long = fs.Position - fmtStart
                        Dim remaining As Long = CLng(chunkSize) - consumed
                        If remaining > 0 Then fs.Seek(remaining, SeekOrigin.Current)
                    ElseIf chunkId = "data" Then
                        foundData = True
                        dataChunkOffset = fs.Position
                        dataChunkSize = chunkSize
                        Exit While
                    Else
                        ' Saute le chunk (possiblement avec padding pour alignement pair)
                        fs.Seek(chunkSize, SeekOrigin.Current)
                    End If
                    ' Si chunkSize est impair, il y a un octet de padding
                    If (chunkSize And 1) = 1 Then
                        fs.Seek(1, SeekOrigin.Current)
                    End If
                End While

                If Not foundData Then
                    ' Pas de chunk data détecté proprement
                End If

                ' Préparer les valeurs pour le log
                Dim actualDataAvailable As Long = fs.Length - dataChunkOffset

                Dim needRebuild As Boolean = False
                If channels <= 0 OrElse sampleRate <= 0 OrElse bitsPerSample <= 0 Then
                    needRebuild = True
                End If
                If dataChunkSize > fs.Length Then
                    needRebuild = True
                End If
                If dataChunkOffset < 44 Then
                    ' Offset anormalement petit -> header corrompu
                    needRebuild = True
                End If

                If needRebuild Then
                    ' Fermer le flux courant avant de reconstruire
                    bw.Flush()
                    br.Close()
                    bw.Close()
                    fs.Close()

                    Try
                        ' Déterminer paramètres audio valides ou par défaut
                        Dim outChannels As Integer = If(channels > 0, channels, 2)
                        Dim outSampleRate As Integer = If(sampleRate > 0, sampleRate, 44100)
                        Dim outBitsPerSample As Integer = If(bitsPerSample > 0, bitsPerSample, 16)

                        Dim audioStart As Long = If(foundData AndAlso dataChunkOffset >= 0, dataChunkOffset, 44L)
                        If audioStart < 0 Then audioStart = 44L

                        ' Combien d'octets d'audio sont disponibles
                        Dim available As Long = 0
                        Using inFs As New FileStream(wavPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                            available = Math.Max(0, inFs.Length - audioStart)
                        End Using

                        ' Lors de la reconstruction du header, copier TOUS les octets audio réellement disponibles
                        Dim dataLen As Long = available
                        If dataLen < 0 Then dataLen = 0

                        ' Construire un nouveau fichier WAV temporaire dans le même dossier
                        Dim dir As String = Path.GetDirectoryName(wavPath)
                        Dim tempPath As String = Path.Combine(dir, Path.GetFileNameWithoutExtension(wavPath) & "_fixed" & Path.GetExtension(wavPath))
                        If File.Exists(tempPath) Then File.Delete(tempPath)

                        Using inFs As New FileStream(wavPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                            Using outFs As New FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None)
                                Dim outBw As New BinaryWriter(outFs, System.Text.Encoding.ASCII, leaveOpen:=True)

                                ' Écrire header RIFF
                                outBw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"))
                                Dim newRiffSizeLocal As UInt32 = CUInt(36 + dataLen)
                                outBw.Write(newRiffSizeLocal)
                                outBw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"))

                                ' fmt chunk (PCM)
                                outBw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "))
                                outBw.Write(CUInt(16)) ' PCM fmt chunk size
                                outBw.Write(CUShort(1)) ' audio format = PCM
                                outBw.Write(CUShort(outChannels))
                                outBw.Write(CInt(outSampleRate))
                                Dim byteRate As UInt32 = CUInt(outSampleRate * outChannels * (outBitsPerSample \ 8))
                                outBw.Write(byteRate)
                                Dim blockAlign As UInt16 = CUShort(outChannels * (outBitsPerSample \ 8))
                                outBw.Write(blockAlign)
                                outBw.Write(CUShort(outBitsPerSample))

                                ' data chunk
                                outBw.Write(System.Text.Encoding.ASCII.GetBytes("data"))
                                outBw.Write(CUInt(dataLen))

                                ' Copier les données audio depuis l'offset audioStart
                                inFs.Seek(audioStart, SeekOrigin.Begin)
                                Dim bufferSize As Integer = 65536
                                Dim buffer(bufferSize - 1) As Byte
                                Dim remaining As Long = dataLen
                                While remaining > 0
                                    Dim toRead As Integer = CInt(Math.Min(bufferSize, remaining))
                                    Dim rd As Integer = inFs.Read(buffer, 0, toRead)
                                    If rd <= 0 Then Exit While
                                    outFs.Write(buffer, 0, rd)
                                    remaining -= rd
                                End While

                                outBw.Flush()
                                outFs.Flush()
                            End Using
                        End Using

                        ' Remplacer le fichier original par le fixe
                        Try
                            File.Delete(wavPath)
                        Catch
                        End Try
                        File.Move(tempPath, wavPath)

                        CDAudioAnalyzer.DiagnosticWrite($"TRUNCATE_REBUILD_HEADER: rebuilt header for {wavPath}, channels={outChannels}, sampleRate={outSampleRate}, bits={outBitsPerSample}, dataLen={dataLen}")
                    Catch exRebuild As Exception
                        CDAudioAnalyzer.DiagnosticWrite($"TRUNCATE_REBUILD_ERROR: {wavPath} - {exRebuild.Message}")
                    End Try

                    Return
                End If

                ' Si le chunk data dans l'en-tête est manifestement incorrect (> taille fichier), on l'ignore
                If dataChunkSize > fs.Length Then
                    ' Remplacer dataChunkSize par la taille réelle disponible
                    dataChunkSize = CUInt(actualDataAvailable)
                End If

                ' Si le chunk data est plus grand que expectedDataBytes, on tronque
                If dataChunkSize > expectedDataBytes Then
                    Dim newDataSize As UInt32 = CUInt(expectedDataBytes)

                    ' Écrire la nouvelle taille du chunk 'data' (position = dataChunkOffset - 4)
                    fs.Seek(dataChunkOffset - 4, SeekOrigin.Begin)
                    bw.Write(newDataSize)

                    ' Calculer et écrire la nouvelle taille RIFF (file size - 8)
                    Dim newRiffSize As UInt32 = CUInt((dataChunkOffset + newDataSize) - 8)
                    fs.Seek(4, SeekOrigin.Begin)
                    bw.Write(newRiffSize)

                    ' Truncate the file to cut extra data
                    fs.SetLength(dataChunkOffset + newDataSize)
                End If

                ' Snippet extraction removed: no temporary tail snippets are written

                bw.Flush()

                ' Logging détaillé pour diagnostic
                Try
                    Dim logLine As String = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] TruncateWavDataChunk: file={wavPath}, fileLength={fs.Length}, dataChunkSize={dataChunkSize}, expectedDataBytes={expectedDataBytes}, dataChunkOffset={dataChunkOffset}, channels={channels}, sampleRate={sampleRate}, bitsPerSample={bitsPerSample}{Environment.NewLine}"
                    Try
                        System.IO.File.AppendAllText(CDAudioAnalyzer.DiagnosticsLogPath, logLine, System.Text.Encoding.UTF8)
                    Catch
                        ' Ignorer les erreurs de log pour ne pas casser l'extraction
                    End Try
                Catch
                End Try
            End Using
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] TruncateWavDataChunk error: {ex.Message}")
            Throw
        End Try
    End Sub

    ' Diagnostic helper: save small WAV snippets (start and end) during extraction if enabled
    Private Sub SaveWavSnippetsIfRequested(wavPath As String)
        Try
            If String.IsNullOrWhiteSpace(wavPath) OrElse Not File.Exists(wavPath) Then Return
            ' Snippet saving disabled: this function is now a no-op to avoid generating temporary files
            Return

            Using fs As New FileStream(wavPath, FileMode.Open, FileAccess.Read, FileShare.Read)
                Dim headerSize As Integer = 128
                Dim startRead As Integer = CInt(Math.Min(headerSize, fs.Length))
                fs.Seek(0, SeekOrigin.Begin)
                Dim startBuf(startRead - 1) As Byte
                fs.Read(startBuf, 0, startRead)
                Dim startPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(wavPath) & "_head128.bin")
                File.WriteAllBytes(startPath, startBuf)

                Dim tailSize As Integer = 128
                Dim tailRead As Integer = CInt(Math.Min(tailSize, fs.Length))
                fs.Seek(Math.Max(0, fs.Length - tailRead), SeekOrigin.Begin)
                Dim tailBuf(tailRead - 1) As Byte
                fs.Read(tailBuf, 0, tailRead)
                ' Only save binary tail snippet when snippet capture/logging is enabled
                Try
                    If CDAudioAnalyzer.EnableSnippetLogging OrElse CDAudioAnalyzer.EnableSnippetCapture OrElse CDAudioAnalyzer.ForceSaveSnippetsForAllTracks Then
                        Dim tailPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(wavPath) & "_tail128.bin")
                        File.WriteAllBytes(tailPath, tailBuf)
                        Try
                            System.IO.File.AppendAllText(CDAudioAnalyzer.DiagnosticsLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] SaveWavSnippets: head={startPath} tail={tailPath}{Environment.NewLine}", System.Text.Encoding.UTF8)
                        Catch
                        End Try
                    End If
                Catch
                End Try
            End Using
        Catch ex As Exception
            ' Ignore errors
        End Try
    End Sub

    Private Function CompareFilesBoundary(pathA As String, pathB As String) As (Found As Boolean, MatchLength As Integer, AHeadPath As String, ATailPath As String, BHeadPath As String, BTailPath As String)
        Try
            Dim snippetSeconds As Double = 5.0
            ' Temporary no-op anchor for follow-up boundary processing change.
            Dim aHeadPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(pathA) & "_head5s.wav")
            Dim aTailPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(pathA) & "_tail5s.wav")
            Dim bHeadPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(pathB) & "_head5s.wav")
            Dim bTailPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(pathB) & "_tail5s.wav")

            ' Extract raw bytes for 5s using simple file copy (assumes WAV PCM header present)
            Try
                If CDAudioAnalyzer.EnableSnippetLogging OrElse CDAudioAnalyzer.EnableSnippetCapture OrElse CDAudioAnalyzer.ForceSaveSnippetsForAllTracks Then
                    Using fsA As New FileStream(pathA, FileMode.Open, FileAccess.Read)
                        Dim tailLen As Integer = CInt(Math.Min(fsA.Length, 5 * 44100 * 2 * 2)) ' 5s * 44100 * channels(2) * bytesPerSample(2)
                        fsA.Seek(Math.Max(0, fsA.Length - tailLen), SeekOrigin.Begin)
                        Dim buf(tailLen - 1) As Byte
                        fsA.Read(buf, 0, tailLen)
                        File.WriteAllBytes(aTailPath, buf)
                    End Using
                End If
            Catch
            End Try

            Try
                If CDAudioAnalyzer.EnableSnippetLogging OrElse CDAudioAnalyzer.EnableSnippetCapture OrElse CDAudioAnalyzer.ForceSaveSnippetsForAllTracks Then
                    Using fsB As New FileStream(pathB, FileMode.Open, FileAccess.Read)
                        Dim headLen As Integer = CInt(Math.Min(fsB.Length, 5 * 44100 * 2 * 2))
                        fsB.Seek(0, SeekOrigin.Begin)
                        Dim buf2(headLen - 1) As Byte
                        fsB.Read(buf2, 0, headLen)
                        File.WriteAllBytes(bHeadPath, buf2)
                    End Using
                End If
            Catch
            End Try

            ' Compare bytes prefix/suffix
            Dim matchLen As Integer = 0
            Try
                Dim aBytes = File.ReadAllBytes(aTailPath)
                Dim bBytes = File.ReadAllBytes(bHeadPath)
                Dim compareLen = Math.Min(aBytes.Length, bBytes.Length)
                For i As Integer = 0 To compareLen - 1
                    If aBytes(i) = bBytes(i) Then
                        matchLen += 1
                    Else
                        Exit For
                    End If
                Next
            Catch
            End Try

            Return (matchLen > 0, matchLen, aHeadPath, aTailPath, bHeadPath, bTailPath)
        Catch ex As Exception
            Return (False, 0, "", "", "", "")
        End Try
    End Function

    ' Tronque la fin d'un fichier WAV de trimBytes octets en ajustant les en-têtes RIFF/WAVE
    Private Sub TrimWavEndByBytes(wavPath As String, trimBytes As Integer)
        Try
            If String.IsNullOrWhiteSpace(wavPath) Then Return
            If Not File.Exists(wavPath) Then Return
            If trimBytes <= 0 Then Return

            Using fs As New FileStream(wavPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
                Dim br As New BinaryReader(fs, System.Text.Encoding.ASCII, leaveOpen:=True)
                Dim bw As New BinaryWriter(fs, System.Text.Encoding.ASCII, leaveOpen:=True)

                fs.Seek(0, SeekOrigin.Begin)
                Dim riff = br.ReadBytes(4)
                If System.Text.Encoding.ASCII.GetString(riff) <> "RIFF" Then Return
                br.ReadUInt32() ' riff size
                Dim wave = br.ReadBytes(4)
                If System.Text.Encoding.ASCII.GetString(wave) <> "WAVE" Then Return

                Dim dataChunkOffset As Long = -1
                Dim dataChunkSize As UInt32 = 0

                While fs.Position < fs.Length - 8
                    Dim chunkIdBytes = br.ReadBytes(4)
                    If chunkIdBytes.Length < 4 Then Exit While
                    Dim chunkId = System.Text.Encoding.ASCII.GetString(chunkIdBytes)
                    Dim chunkSize As UInt32 = br.ReadUInt32()

                    If chunkId = "data" Then
                        dataChunkOffset = fs.Position
                        dataChunkSize = chunkSize
                        Exit While
                    End If

                    fs.Seek(CLng(chunkSize), SeekOrigin.Current)
                    If (chunkSize And 1UI) = 1UI Then
                        fs.Seek(1, SeekOrigin.Current)
                    End If
                End While

                If dataChunkOffset < 0 Then Return

                Dim actualDataAvailable As Long = Math.Max(0, fs.Length - dataChunkOffset)
                If dataChunkSize > actualDataAvailable Then
                    dataChunkSize = CUInt(actualDataAvailable)
                End If

                Dim bytesToTrim As Long = Math.Min(CLng(trimBytes), CLng(dataChunkSize))
                If bytesToTrim <= 0 Then Return

                Dim newDataSize As UInt32 = CUInt(Math.Max(0, CLng(dataChunkSize) - bytesToTrim))

                fs.Seek(dataChunkOffset - 4, SeekOrigin.Begin)
                bw.Write(newDataSize)

                Dim newRiffSize As UInt32 = CUInt((dataChunkOffset + CLng(newDataSize)) - 8)
                fs.Seek(4, SeekOrigin.Begin)
                bw.Write(newRiffSize)

                fs.SetLength(dataChunkOffset + CLng(newDataSize))
                bw.Flush()
            End Using
        Catch ex As Exception
            CDAudioAnalyzer.DiagnosticWrite($"TrimWavEndByBytes error: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Extrait une piste en FLAC avec compression sans perte via FFMpeg
    ''' </summary>
    Private Async Function ExtraireFlac(source As NAudio.Wave.WaveStream, cheminFichier As String,
                                        titre As String, artiste As String, numeroPiste As String) As Task
        ' Si ForceOnlyExternalRipper est activé, empêcher toute extraction interne
        Try
            If ParametresGlobaux.ForceOnlyExternalRipper Then
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_HARD_SKIP: Internal FLAC rip skipped for {cheminFichier}")
                Catch
                End Try
                ' Do not write .internal_skip.txt marker file to album folder; log instead
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"INTERNAL_SKIP_MARKER: SKIPPED {DateTime.UtcNow:o} ForceOnlyExternalRipper=true File={cheminFichier}")
                Catch
                End Try
                Return
            End If
        Catch
        End Try

        ' Capturer toutes les valeurs UI AVANT les opérations asynchrones de façon thread-safe
        Dim qualiteIndex As Integer = 0
        Dim album As String = ""
        Dim artisteAlbum As String = ""
        Dim annee As String = ""
        Dim genre As String = ""
        Dim commentaire As String = ""
        Dim numeroDisque As String = ""
        Dim pochette As Image = Nothing
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub()
                              qualiteIndex = If(ComboBoxQualiteConversion.SelectedIndex < 0, 0, ComboBoxQualiteConversion.SelectedIndex)
                              album = TextBoxCDTitre.Text
                              artisteAlbum = TextBoxCDArtiste.Text
                              annee = TextBoxAnnee.Text
                              genre = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                              commentaire = TextBoxCommentaire.Text
                              numeroDisque = TextBoxNumCD.Text
                              pochette = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)
                          End Sub)
            Else
                qualiteIndex = If(ComboBoxQualiteConversion.SelectedIndex < 0, 0, ComboBoxQualiteConversion.SelectedIndex)
                album = TextBoxCDTitre.Text
                artisteAlbum = TextBoxCDArtiste.Text
                annee = TextBoxAnnee.Text
                genre = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                commentaire = TextBoxCommentaire.Text
                numeroDisque = TextBoxNumCD.Text
                pochette = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)
            End If
        Catch
        End Try

        ' Déterminer le niveau de compression FLAC
        ' Index 0=Niveau 0, 1=Niveau 5, 2=Niveau 8
        Dim compressionLevel As Integer = 8 ' Par défaut
        Select Case qualiteIndex
            Case 0 ' Niveau 0 (rapide)
                compressionLevel = 0
            Case 1 ' Niveau 5 (équilibré)
                compressionLevel = 5
            Case 2 ' Niveau 8 (meilleur)
                compressionLevel = 8
            Case Else ' Fallback
                compressionLevel = 8
        End Select

        ' Créer un fichier WAV temporaire dans le répertoire de l'album (préféré au dossier Temp)
        Dim albumDir As String = Path.GetDirectoryName(cheminFichier)
        If Not String.IsNullOrEmpty(albumDir) Then
            Try
                Directory.CreateDirectory(albumDir)
            Catch
            End Try
        Else
            ' Fallback minimal: use the target file's parent if available, else Temp (should be rare)
            albumDir = Path.GetDirectoryName(cheminFichier)
            If String.IsNullOrEmpty(albumDir) Then albumDir = Path.GetTempPath()
        End If
        ' Prefer a meaningful WAV filename for FLAC intermediate instead of a random temp name
        Dim cheminWavTemp As String = Nothing
        Try
            Dim desiredBaseName As String = Nothing
            Try
                Dim tn As Integer = -1
                If Integer.TryParse(numeroPiste, tn) Then
                    desiredBaseName = $"{tn:D2} - {MakeSafeFileName(artiste)} - {MakeSafeFileName(titre)}"
                Else
                    desiredBaseName = $"{MakeSafeFileName(numeroPiste)} - {MakeSafeFileName(artiste)} - {MakeSafeFileName(titre)}"
                End If
            Catch
                desiredBaseName = $"audioplay_temp_{Guid.NewGuid().ToString("N")}"
            End Try

            Dim desiredWavPath = Path.Combine(albumDir, desiredBaseName & ".wav")
            cheminWavTemp = desiredWavPath
            Try
                If File.Exists(cheminWavTemp) Then
                    cheminWavTemp = Path.Combine(albumDir, Path.GetFileNameWithoutExtension(desiredBaseName) & "_" & Guid.NewGuid().ToString("N") & ".wav")
                End If
            Catch
                cheminWavTemp = Path.Combine(albumDir, $"audioplay_temp_{Guid.NewGuid()}.wav")
            End Try
        Catch
            cheminWavTemp = Path.Combine(albumDir, $"audioplay_temp_{Guid.NewGuid()}.wav")
        End Try

        ' Capturer la valeur du volume de façon thread-safe
        Dim volumePercent As Decimal = 95D
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() volumePercent = NumericUpDown_DB.Value)
            Else
                volumePercent = NumericUpDown_DB.Value
            End If
        Catch
        End Try
        Dim volumeMultiplier As Single = CSng(volumePercent / 100D)
        Dim convOkLocal As Boolean = False
        Dim stderrLocal As String = String.Empty

        Try
            ' Étape 1: Extraire en WAV temporaire avec progression
            Await Task.Run(Sub()
                               Using writer As New NAudio.Wave.WaveFileWriter(cheminWavTemp, source.WaveFormat)
                                   CopierAvecProgression(source, writer)
                               End Using
                           End Sub)

            ' Si une annulation a été demandée pendant la génération du WAV, supprimer le fichier WAV temporaire
            If annulationDemandee OrElse (ctsExtraction IsNot Nothing AndAlso ctsExtraction.IsCancellationRequested) Then
                Try
                    If File.Exists(cheminWavTemp) Then
                        File.Delete(cheminWavTemp)
                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETED_AFTER_CANCEL: Track={numeroPiste} WAV={cheminWavTemp}")
                    End If
                Catch exDel As Exception
                    Try
                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETE_ERROR_AFTER_CANCEL: {cheminWavTemp} - {exDel.Message}")
                    Catch
                    End Try
                End Try
                Return
            End If

            ' Étape 2: Encoder WAV -> FLAC avec FFMpeg et ajustement de volume
            Dim ffmpegPath As String = Await TrouverFFMpeg()
            If Not String.IsNullOrEmpty(ffmpegPath) Then
                ' UI: indicate conversion started for this track
                SafeSetLabelPisteEnCours(LanguageManager.GetString("Compressor_Converting"), True)
                SafeSetProgressBarMarquee(True)
                Await Task.Run(Sub()
                                   Dim processInfo As New ProcessStartInfo With {
                                       .FileName = ffmpegPath,
                                       .Arguments = $"-i ""{cheminWavTemp}"" -filter:a volume={volumeMultiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} -compression_level {compressionLevel} -y ""{cheminFichier}""",
                                       .UseShellExecute = False,
                                       .CreateNoWindow = True,
                                       .RedirectStandardOutput = True,
                                       .RedirectStandardError = True
                                   }

                                   Dim process As Process = Process.Start(processInfo)
                                   ' Wait for ffmpeg exit but abort early if cancellation requested
                                   Do While Not process.HasExited
                                       If annulationDemandee OrElse (ctsExtraction IsNot Nothing AndAlso ctsExtraction.IsCancellationRequested) Then
                                           Try
                                               process.Kill(True)
                                           Catch
                                           End Try
                                           Exit Do
                                       End If
                                       Threading.Thread.Sleep(100)
                                   Loop
                                   Try
                                       stderrLocal = process.StandardError.ReadToEnd()
                                   Catch
                                   End Try
                                   If process.ExitCode = 0 Then
                                       convOkLocal = True
                                   Else
                                       convOkLocal = False
                                   End If

                                   process.Dispose()
                               End Sub)

                If convOkLocal Then
                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_CONVERSION_SUCCESS: Track={numeroPiste} Format=FLAC Out={cheminFichier}")
                Else
                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_CONVERSION_FAIL: Track={numeroPiste} Format=FLAC Err={stderrLocal}")
                End If

                ' UI: restore after conversion
                SafeSetProgressBarMarquee(False)
                SafeSetLabelPisteEnCours("", False)
                ' Cleanup only on success
                If convOkLocal Then
                    Try
                        If File.Exists(cheminWavTemp) Then
                            File.Delete(cheminWavTemp)
                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETED: Track={numeroPiste} WAV={cheminWavTemp}")
                        End If
                    Catch exDel As Exception
                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_FLAC_CLEANUP_ERROR: {cheminWavTemp} - {exDel.Message}")
                    End Try
                Else
                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_PRESERVED_AFTER_CONV_FAIL: Track={numeroPiste} WAV={cheminWavTemp}")
                End If
            Else
                ' L'utilisateur a refusé le téléchargement ou il a échoué
                Throw New Exception("FFMpeg n'est pas disponible. L'extraction FLAC ne peut pas continuer.")
            End If

            ' Écrire les métadonnées avec TagLib
            EcrireMetadonnees(cheminFichier, titre, artiste, numeroPiste,
                            album, artisteAlbum, annee, genre, commentaire, pochette, numeroDisque)

        Finally
            ' Libérer la copie de l'image
            pochette?.Dispose()

            ' Nettoyer le fichier temporaire uniquement si la conversion a réussi
            Try
                If convOkLocal Then
                    If File.Exists(cheminWavTemp) Then
                        File.Delete(cheminWavTemp)
                    End If
                Else
                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_PRESERVED_AFTER_CONV_FAIL: Track={numeroPiste} WAV={cheminWavTemp}")
                End If
            Catch exDel As Exception
                CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_FLAC_CLEANUP_ERROR: {cheminWavTemp} - {exDel.Message}")
            End Try
        End Try
    End Function

    ''' <summary>
    ''' Cherche ffmpeg.exe dans les emplacements standards
    ''' Si introuvable, propose de le télécharger automatiquement
    ''' </summary>
    Private Async Function TrouverFFMpeg() As Task(Of String)
        ' Vérifier si FFMpeg est déjà installé
        Dim cheminFFMpeg = FFMpegManager.ObtenirCheminFFMpeg()
        If Not String.IsNullOrEmpty(cheminFFMpeg) Then
            Return cheminFFMpeg
        End If

        ' FFMpeg n'est pas installé, proposer de le télécharger
        Dim resultat = MessageBox.Show(
            "FFMpeg est nécessaire pour extraire en format FLAC et WMA." & vbCrLf & vbCrLf &
            "Voulez-vous télécharger et installer FFMpeg automatiquement ?" & vbCrLf &
            "(Taille: ~120 MB, téléchargement unique)",
            "FFMpeg requis",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question)

        If resultat = DialogResult.Yes Then
            ' Afficher le formulaire de téléchargement
            Using formTelechargemEnt As New FormTelechargerFFMpeg()
                If formTelechargemEnt.ShowDialog(Me) = DialogResult.OK Then
                    ' Téléchargement réussi, retourner le chemin
                    Return FFMpegManager.ObtenirCheminFFMpeg()
                End If
            End Using
        End If

        Return Nothing
    End Function

    ''' <summary>
    ''' Extrait une piste en WMA (Windows Media Audio) via FFMpeg
    ''' </summary>
    Private Async Function ExtraireWma(source As NAudio.Wave.WaveStream, cheminFichier As String,
                                       titre As String, artiste As String, numeroPiste As String) As Task
        ' Si ForceOnlyExternalRipper est activé, empêcher toute extraction interne
        Try
            If ParametresGlobaux.ForceOnlyExternalRipper Then
                Try
                    CDAudioAnalyzer.DiagnosticWrite($"FORCE_ONLY_EXTERNAL_RIPPER_HARD_SKIP: Internal WMA rip skipped for {cheminFichier}")
                Catch
                End Try
                Try
                    ' Do not write .internal_skip.txt marker file to album folder; log instead
                    CDAudioAnalyzer.DiagnosticWrite($"INTERNAL_SKIP_MARKER: SKIPPED {DateTime.UtcNow:o} ForceOnlyExternalRipper=true File={cheminFichier}")
                Catch
                End Try
                Return
            End If
        Catch
        End Try

        ' Capturer toutes les valeurs UI AVANT Task.Run
        Dim qualiteIndex As Integer = 0
        Dim album As String = ""
        Dim artisteAlbum As String = ""
        Dim annee As String = ""
        Dim genre As String = ""
        Dim commentaire As String = ""
        Dim pochette As Image = Nothing
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub()
                              qualiteIndex = If(ComboBoxQualiteConversion.SelectedIndex < 0, 0, ComboBoxQualiteConversion.SelectedIndex)
                              album = TextBoxCDTitre.Text
                              artisteAlbum = TextBoxCDArtiste.Text
                              annee = TextBoxAnnee.Text
                              genre = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                              commentaire = TextBoxCommentaire.Text
                              pochette = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)
                          End Sub)
            Else
                qualiteIndex = If(ComboBoxQualiteConversion.SelectedIndex < 0, 0, ComboBoxQualiteConversion.SelectedIndex)
                album = TextBoxCDTitre.Text
                artisteAlbum = TextBoxCDArtiste.Text
                annee = TextBoxAnnee.Text
                genre = If(ComboBoxGenre.SelectedItem?.ToString(), "")
                commentaire = TextBoxCommentaire.Text
                pochette = If(PictureBoxPochette.Image IsNot Nothing, New Bitmap(PictureBoxPochette.Image), Nothing)
            End If
        Catch
        End Try

        ' Déterminer le bitrate WMA
        ' Index 0=128, 1=192, 2=256
        Dim bitrate As Integer = 256 ' Par défaut
        Select Case qualiteIndex
            Case 0 ' 128 kbps
                bitrate = 128
            Case 1 ' 192 kbps
                bitrate = 192
            Case 2 ' 256 kbps
                bitrate = 256
            Case Else ' Fallback
                bitrate = 256
        End Select

        ' Créer un fichier WAV temporaire
        Dim albumDir As String = Path.GetDirectoryName(cheminFichier)
        If String.IsNullOrEmpty(albumDir) Then albumDir = Path.GetTempPath()

        ' Prefer a meaningful WAV filename for WMA intermediate instead of a random temp name
        Dim cheminWavTemp As String = Nothing
        Try
            Dim desiredBaseName As String = Nothing
            Try
                Dim tn As Integer = -1
                If Integer.TryParse(numeroPiste, tn) Then
                    desiredBaseName = $"{tn:D2} - {MakeSafeFileName(artiste)} - {MakeSafeFileName(titre)}"
                Else
                    desiredBaseName = $"{MakeSafeFileName(numeroPiste)} - {MakeSafeFileName(artiste)} - {MakeSafeFileName(titre)}"
                End If
            Catch
                desiredBaseName = $"audioplay_temp_{Guid.NewGuid().ToString("N")}"
            End Try

            Dim desiredWavPath = Path.Combine(albumDir, desiredBaseName & ".wav")
            cheminWavTemp = desiredWavPath
            Try
                If File.Exists(cheminWavTemp) Then
                    cheminWavTemp = Path.Combine(albumDir, Path.GetFileNameWithoutExtension(desiredBaseName) & "_" & Guid.NewGuid().ToString("N") & ".wav")
                End If
            Catch
                cheminWavTemp = Path.Combine(albumDir, $"audioplay_temp_{Guid.NewGuid()}.wav")
            End Try
        Catch
            cheminWavTemp = Path.Combine(albumDir, $"audioplay_temp_{Guid.NewGuid()}.wav")
        End Try
        Dim convOkLocalWma As Boolean = False
        Dim stderrLocalWma As String = String.Empty

        ' Capturer la valeur du volume
        Dim volumePercent As Decimal = 95D
        Try
            If Me.InvokeRequired Then
                Me.Invoke(Sub() volumePercent = NumericUpDown_DB.Value)
            Else
                volumePercent = NumericUpDown_DB.Value
            End If
        Catch
        End Try
        Dim volumeMultiplier As Single = CSng(volumePercent / 100D)

        Try
            ' Étape 1: Extraire en WAV temporaire avec progression
            Await Task.Run(Sub()
                               Using writer As New NAudio.Wave.WaveFileWriter(cheminWavTemp, source.WaveFormat)
                                   CopierAvecProgression(source, writer)
                               End Using
                           End Sub)

            ' Étape 2: Encoder WAV -> WMA avec FFMpeg et ajustement de volume
            Dim ffmpegPath As String = Await TrouverFFMpeg()
            If Not String.IsNullOrEmpty(ffmpegPath) Then
                ' UI: indicate conversion started for this track
                SafeSetLabelPisteEnCours(LanguageManager.GetString("Compressor_Converting"), True)
                SafeSetProgressBarMarquee(True)
                Await Task.Run(Sub()
                                   Dim processInfo As New ProcessStartInfo With {
                                       .FileName = ffmpegPath,
                                       .Arguments = $"-i ""{cheminWavTemp}"" -filter:a volume={volumeMultiplier.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} -codec:a wmav2 -b:a {bitrate}k -y ""{cheminFichier}""",
                                       .UseShellExecute = False,
                                       .CreateNoWindow = True,
                                       .RedirectStandardOutput = True,
                                       .RedirectStandardError = True
                                   }

                                   Dim process As Process = Process.Start(processInfo)
                                   ' Wait for ffmpeg exit but abort early if cancellation requested
                                   Do While Not process.HasExited
                                       If annulationDemandee OrElse (ctsExtraction IsNot Nothing AndAlso ctsExtraction.IsCancellationRequested) Then
                                           Try
                                               process.Kill(True)
                                           Catch
                                           End Try
                                           Exit Do
                                       End If
                                       Threading.Thread.Sleep(100)
                                   Loop
                                   Try
                                       stderrLocalWma = process.StandardError.ReadToEnd()
                                   Catch
                                   End Try
                                   If process.ExitCode = 0 Then
                                       convOkLocalWma = True
                                   Else
                                       convOkLocalWma = False
                                   End If

                                   process.Dispose()
                               End Sub)

                If convOkLocalWma Then
                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_CONVERSION_SUCCESS: Track={numeroPiste} Format=WMA Out={cheminFichier}")
                Else
                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_CONVERSION_FAIL: Track={numeroPiste} Format=WMA Err={stderrLocalWma}")
                End If

                ' UI: restore after conversion
                SafeSetProgressBarMarquee(False)
                SafeSetLabelPisteEnCours("", False)

                If convOkLocalWma Then
                    Try
                        If File.Exists(cheminWavTemp) Then
                            File.Delete(cheminWavTemp)
                            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_DELETED: Track={numeroPiste} WAV={cheminWavTemp}")
                        End If
                    Catch exDel As Exception
                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WMA_CLEANUP_ERROR: {cheminWavTemp} - {exDel.Message}")
                    End Try
                Else
                    CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_PRESERVED_AFTER_CONV_FAIL: Track={numeroPiste} WAV={cheminWavTemp}")
                End If
            Else
                ' L'utilisateur a refusé le téléchargement ou il a échoué
                Throw New Exception("FFMpeg n'est pas disponible. L'extraction WMA ne peut pas continuer.")
            End If

            ' Écrire les métadonnées avec TagLib
            EcrireMetadonnees(cheminFichier, titre, artiste, numeroPiste,
                            album, artisteAlbum, annee, genre, commentaire, pochette, TextBoxNumCD.Text)

        Finally
            ' Libérer la copie de l'image
            pochette?.Dispose()

            ' Nettoyer le fichier temporaire uniquement si conversion réussie
            If convOkLocalWma Then
                If File.Exists(cheminWavTemp) Then
                    Try
                        File.Delete(cheminWavTemp)
                    Catch exDel As Exception
                        CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WMA_CLEANUP_ERROR: {cheminWavTemp} - {exDel.Message}")
                    End Try
                End If
            Else
                CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_PRESERVED_AFTER_CONV_FAIL: Track={numeroPiste} WAV={cheminWavTemp}")
            End If
        End Try
    End Function

    ''' <summary>
    ''' Écrit les métadonnées dans un fichier audio (MP3, WAV, FLAC, WMA)
    ''' </summary>
    Private Sub EcrireMetadonnees(cheminFichier As String, titre As String, artiste As String, numeroPiste As String,
                                   album As String, artisteAlbum As String, annee As String,
                                   genre As String, commentaire As String, pochette As Image, numeroDisque As String)
        Try
            Using fichier = TagLib.File.Create(cheminFichier)
                ' Métadonnées de base
                fichier.Tag.Title = titre
                fichier.Tag.Performers = {artiste}
                fichier.Tag.AlbumArtists = {artisteAlbum}
                fichier.Tag.Album = album

                ' Numéro de piste
                Dim trackNum As UInteger
                If UInteger.TryParse(numeroPiste, trackNum) Then
                    fichier.Tag.Track = trackNum
                End If

                ' Numéro de disque
                Dim discNum As UInteger
                If Not String.IsNullOrWhiteSpace(numeroDisque) AndAlso UInteger.TryParse(numeroDisque, discNum) Then
                    fichier.Tag.Disc = discNum
                End If

                ' Année
                Dim anneeNum As UInteger
                If Not String.IsNullOrWhiteSpace(annee) AndAlso
                   UInteger.TryParse(annee, anneeNum) Then
                    fichier.Tag.Year = anneeNum
                End If

                ' Genre
                If Not String.IsNullOrWhiteSpace(genre) Then
                    fichier.Tag.Genres = {genre}
                End If

                ' Commentaire
                If Not String.IsNullOrWhiteSpace(commentaire) Then
                    fichier.Tag.Comment = commentaire
                End If

                ' Pochette (si disponible)
                If pochette IsNot Nothing Then
                    Try
                        Using ms As New MemoryStream()
                            pochette.Save(ms, Imaging.ImageFormat.Jpeg)
                            Dim imageData As Byte() = ms.ToArray()

                            Dim picture As New TagLib.Picture(New TagLib.ByteVector(imageData))
                            picture.Type = TagLib.PictureType.FrontCover
                            picture.MimeType = "image/jpeg"
                            picture.Description = "Cover"

                            fichier.Tag.Pictures = {picture}
                        End Using
                    Catch ex As Exception
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur ajout pochette: {ex.Message}")
                    End Try
                End If

                fichier.Save()
            End Using
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur écriture métadonnées: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Dessine les en-têtes de colonnes du ListView avec les couleurs du thème
    ''' </summary>
    Private Sub ListViewCompress_DrawColumnHeader(sender As Object, e As DrawListViewColumnHeaderEventArgs)
        ' Récupérer le thème actuel
        Dim theme = ThemeManager.GetCurrentTheme()

        ' Dessiner le fond de l'en-tête avec la couleur spécifique
        Using brush As New SolidBrush(theme.ListViewHeaderBackColor)
            e.Graphics.FillRectangle(brush, e.Bounds)
        End Using

        ' Dessiner la bordure
        e.Graphics.DrawRectangle(SystemPens.ControlDark, New Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1))

        ' Dessiner le texte centré avec support multi-ligne
        Dim sf As New StringFormat()
        sf.Alignment = StringAlignment.Center
        sf.LineAlignment = StringAlignment.Center
        sf.Trimming = StringTrimming.Word
        sf.FormatFlags = StringFormatFlags.LineLimit

        Using textBrush As New SolidBrush(theme.ListViewHeaderForeColor)
            e.Graphics.DrawString(e.Header.Text, e.Font, textBrush, e.Bounds, sf)
        End Using
    End Sub

    ''' <summary>
    ''' Dessine les items du ListView
    ''' </summary>
    Private Sub ListViewCompress_DrawItem(sender As Object, e As DrawListViewItemEventArgs)
        ' Dessiner la checkbox si nécessaire
        e.DrawDefault = False

        ' Dessiner le fond de la ligne entière si sélectionnée
        If e.Item.Selected Then
            Dim theme = ThemeManager.GetCurrentTheme()
            Using brush As New SolidBrush(theme.ListViewSelectionBackColor)
                e.Graphics.FillRectangle(brush, e.Bounds)
            End Using
        End If
    End Sub

    ''' <summary>
    ''' Dessine les sous-items (colonnes) du ListView avec les couleurs du thème
    ''' </summary>
    Private Sub ListViewCompress_DrawSubItem(sender As Object, e As DrawListViewSubItemEventArgs)
        ' Déterminer si cet item est sélectionné
        Dim estSelectionne As Boolean = e.Item.Selected
        Dim theme = ThemeManager.GetCurrentTheme()

        ' Couleur de fond
        Dim couleurFond As Color
        If estSelectionne Then
            couleurFond = theme.ListViewSelectionBackColor
        Else
            couleurFond = e.Item.BackColor
        End If

        ' Couleur de texte
        Dim couleurTexte As Color
        If estSelectionne Then
            couleurTexte = theme.ListViewSelectionForeColor
        Else
            couleurTexte = e.Item.ForeColor
        End If

        ' Dessiner le fond
        Using brush As New SolidBrush(couleurFond)
            e.Graphics.FillRectangle(brush, e.Bounds)
        End Using

        ' Gérer la checkbox pour la première colonne (Piste)
        If e.ColumnIndex = 0 Then
            ' Dessiner la checkbox
            Dim checkBoxSize As Integer = 13
            Dim checkBoxX As Integer = e.Bounds.X + 2
            Dim checkBoxY As Integer = e.Bounds.Y + (e.Bounds.Height - checkBoxSize) \ 2
            Dim checkBoxBounds As New Rectangle(checkBoxX, checkBoxY, checkBoxSize, checkBoxSize)

            Dim checkState As System.Windows.Forms.VisualStyles.CheckBoxState
            If e.Item.Checked Then
                checkState = System.Windows.Forms.VisualStyles.CheckBoxState.CheckedNormal
            Else
                checkState = System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal
            End If

            CheckBoxRenderer.DrawCheckBox(e.Graphics, checkBoxBounds.Location, checkState)

            ' Dessiner le texte à droite de la checkbox
            Dim textBounds As New Rectangle(checkBoxX + checkBoxSize + 4, e.Bounds.Y, e.Bounds.Width - checkBoxSize - 6, e.Bounds.Height)
            Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.VerticalCenter
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font, textBounds, couleurTexte, flags)
        Else
            ' Dessiner le texte normalement pour les autres colonnes
            Dim flags As TextFormatFlags = TextFormatFlags.Left Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis

            ' Centrer certaines colonnes (Début, Longueur, Taille, TailleComp)
            If e.ColumnIndex = 3 OrElse e.ColumnIndex = 4 OrElse e.ColumnIndex = 5 OrElse e.ColumnIndex = 6 Then
                flags = TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter
            End If

            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, e.Item.Font, e.Bounds, couleurTexte, flags)
        End If

        ' Dessiner les lignes de grille
        Using pen As New Pen(Color.LightGray)
            e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom)
            e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1)
        End Using
    End Sub

    ''' <summary>
    ''' Charge et affiche la pochette de l'album dans PictureBoxPochette
    ''' </summary>
    Private Async Function ChargerPochetteAlbum() As Task
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ChargerPochetteAlbum() appelé")

        ' Nettoyer l'image existante (via helper thread-safe)
        If PictureBoxPochette.Image IsNot Nothing Then
            SafeClearPictureBoxImage()
            Label_DimImage.Text = ""
            LabelTailleImage.Text = ""
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⚠️ IMAGE EFFACÉE dans ChargerPochetteAlbum()")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Stack: {New System.Diagnostics.StackTrace(True).GetFrame(1)?.GetMethod()?.Name}")
        End If

        ' IMPORTANT: Réinitialiser l'historique et l'index pour repartir à zéro
        historiquePochettes.Clear()
        indexPochetteActuelle = -1
        pochetteTempUrl = Nothing
        pochetteTempBytes = Nothing
        cachePochettesBytes.Clear() ' Vider le cache mémoire des images
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Historique réinitialisé")

        ' Vérifier si les métadonnées sont disponibles
        If metadonneesCD Is Nothing Then
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] metadonneesCD est Nothing - abandon")
            Return
        End If

        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Métadonnées disponibles: {metadonneesCD.Artist} - {metadonneesCD.Album}")
        System.Diagnostics.Debug.WriteLine($"[FormCompresser] CoverArtUrl: {If(String.IsNullOrWhiteSpace(metadonneesCD.CoverArtUrl), "VIDE", metadonneesCD.CoverArtUrl)}")

        Try
            ' ═══════════════════════════════════════════════════════════════════════════
            ' NOUVELLE STRATÉGIE : RECHERCHE MULTI-SOURCE PARALLÈLE
            ' ═══════════════════════════════════════════════════════════════════════════

            Dim toutesLesUrls As New List(Of String)()

            ' 1️⃣ PRIORITÉ 1 : URL de MusicBrainz (Cover Art Archive) si déjà disponible
            If Not String.IsNullOrWhiteSpace(metadonneesCD.CoverArtUrl) Then
                ' Vérifier d'abord le cache local d'images
                Dim cheminCache = CoverCacheManager.ObtenirCheminFichier(metadonneesCD.CoverArtUrl)
                If File.Exists(cheminCache) Then
                    Try
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Chargement depuis cache local: {cheminCache}")

                        ' Lire les bytes du fichier cache
                        pochetteTempBytes = File.ReadAllBytes(cheminCache)
                        pochetteTempUrl = metadonneesCD.CoverArtUrl

                        ' Ajouter au cache mémoire pour navigation rapide
                        cachePochettesBytes(metadonneesCD.CoverArtUrl) = pochetteTempBytes

                        ' Afficher l'image
                        Using ms As New MemoryStream(pochetteTempBytes)
                            Using tmpImg = Image.FromStream(ms)
                                SafeSetPictureBoxImage(tmpImg)
                            End Using
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✅ IMAGE AFFICHÉE depuis cache local")
                        End Using

                        ' Ajouter à l'historique
                        AjouterAHistoriquePochettes(metadonneesCD.CoverArtUrl)

                        MettreAJourInfosPochette()
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Pochette affichée depuis cache local")

                        ' Ajouter cette URL à la liste pour ne pas la chercher à nouveau
                        toutesLesUrls.Add(metadonneesCD.CoverArtUrl)
                        sourcesPochettes(metadonneesCD.CoverArtUrl) = "Cover Art Archive"

                        ' Mettre à jour l'état des boutons
                        MettreAJourBoutonsNavigation()
                    Catch ex As Exception
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur lecture cache: {ex.Message}")
                    End Try
                Else
                    ' Essayer de télécharger depuis Cover Art Archive
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Téléchargement depuis Cover Art Archive: {metadonneesCD.CoverArtUrl}")
                    Try
                        Await TelechargerEtAfficherPochette(metadonneesCD.CoverArtUrl, ajouterHistorique:=True)
                        toutesLesUrls.Add(metadonneesCD.CoverArtUrl)
                        sourcesPochettes(metadonneesCD.CoverArtUrl) = "Cover Art Archive"
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Cover Art Archive OK")
                    Catch ex As Exception
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Cover Art Archive échoué ({ex.Message}), fallback vers autres sources...")
                    End Try
                End If
            End If

            ' 2️⃣ RECHERCHE PARALLÈLE SUR TOUTES LES SOURCES
            ' Chercher simultanément sur iTunes, Last.fm et MusicBrainz textuel
            If Not String.IsNullOrWhiteSpace(metadonneesCD.Artist) AndAlso Not String.IsNullOrWhiteSpace(metadonneesCD.Album) Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🔍 Recherche parallèle multi-source pour: {metadonneesCD.Artist} - {metadonneesCD.Album}")

                ' Lancer toutes les recherches en parallèle sur TOUTES les sources (8 au total)
                Dim tacheItunes = RechercherPochetteiTunes(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheLastFm = RechercherPochetteLastFm(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheMusicBrainz = RechercherPochetteMusicBrainz(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheDeezer = RechercherPochetteDeezer(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheDiscogs = RechercherPochetteDiscogs(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheTheAudioDB = RechercherPochetteTheAudioDB(metadonneesCD.Artist, metadonneesCD.Album)
                Dim tacheFanartTV = RechercherPochetteFanartTV(metadonneesCD.Artist, metadonneesCD.Album)

                ' Attendre que toutes les recherches se terminent
                Await Task.WhenAll(tacheItunes, tacheLastFm, tacheMusicBrainz, tacheDeezer, tacheDiscogs, tacheTheAudioDB, tacheFanartTV)

                ' Collecter les résultats (ordre de priorité pour affichage)
                Dim urlItunes = Await tacheItunes
                Dim urlLastFm = Await tacheLastFm
                Dim urlDeezer = Await tacheDeezer
                Dim urlDiscogs = Await tacheDiscogs
                Dim urlTheAudioDB = Await tacheTheAudioDB
                Dim urlFanartTV = Await tacheFanartTV
                Dim urlMusicBrainz = Await tacheMusicBrainz

                ' Ajouter les URLs trouvées (ordre de priorité : iTunes, Last.fm, Deezer, Discogs, TheAudioDB, Fanart.tv, MusicBrainz)
                If Not String.IsNullOrWhiteSpace(urlItunes) AndAlso Not toutesLesUrls.Contains(urlItunes) Then
                    toutesLesUrls.Add(urlItunes)
                    sourcesPochettes(urlItunes) = "iTunes"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ iTunes trouvé: {urlItunes}")
                End If

                If Not String.IsNullOrWhiteSpace(urlLastFm) AndAlso Not toutesLesUrls.Contains(urlLastFm) Then
                    toutesLesUrls.Add(urlLastFm)
                    sourcesPochettes(urlLastFm) = "Last.fm"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Last.fm trouvé: {urlLastFm}")
                End If

                If Not String.IsNullOrWhiteSpace(urlDeezer) AndAlso Not toutesLesUrls.Contains(urlDeezer) Then
                    toutesLesUrls.Add(urlDeezer)
                    sourcesPochettes(urlDeezer) = "Deezer"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Deezer trouvé: {urlDeezer}")
                End If

                If Not String.IsNullOrWhiteSpace(urlDiscogs) AndAlso Not toutesLesUrls.Contains(urlDiscogs) Then
                    toutesLesUrls.Add(urlDiscogs)
                    sourcesPochettes(urlDiscogs) = "Discogs"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Discogs trouvé: {urlDiscogs}")
                End If

                If Not String.IsNullOrWhiteSpace(urlTheAudioDB) AndAlso Not toutesLesUrls.Contains(urlTheAudioDB) Then
                    toutesLesUrls.Add(urlTheAudioDB)
                    sourcesPochettes(urlTheAudioDB) = "TheAudioDB"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ TheAudioDB trouvé: {urlTheAudioDB}")
                End If

                If Not String.IsNullOrWhiteSpace(urlFanartTV) AndAlso Not toutesLesUrls.Contains(urlFanartTV) Then
                    toutesLesUrls.Add(urlFanartTV)
                    sourcesPochettes(urlFanartTV) = "Fanart.tv"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Fanart.tv trouvé (HD): {urlFanartTV}")
                End If

                If Not String.IsNullOrWhiteSpace(urlMusicBrainz) AndAlso Not toutesLesUrls.Contains(urlMusicBrainz) Then
                    toutesLesUrls.Add(urlMusicBrainz)
                    sourcesPochettes(urlMusicBrainz) = "MusicBrainz"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ MusicBrainz trouvé: {urlMusicBrainz}")
                End If

                ' 3️⃣ TÉLÉCHARGER TOUTES LES POCHETTES TROUVÉES
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] 📥 Téléchargement de {toutesLesUrls.Count} pochette(s) trouvée(s)...")

                Dim compteurCharge = historiquePochettes.Count ' Combien déjà chargées (cache local ou Cover Art Archive)

                For Each url In toutesLesUrls
                    ' Vérifier si pas déjà dans l'historique ou le cache
                    If Not historiquePochettes.Contains(url) Then
                        Try
                            ' Télécharger sans afficher (sauf si c'est la toute première)
                            Dim estPremiere = (compteurCharge = 0)
                            If estPremiere Then
                                ' Afficher la première pochette
                                Try
                                    Await TelechargerEtAfficherPochette(url, ajouterHistorique:=True)
                                    compteurCharge += 1
                                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Pochette {compteurCharge}/{toutesLesUrls.Count} affichée")
                                Catch exFirst As Exception
                                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Échec affichage première pochette {url}: {exFirst.Message}")
                                    ' Continuer avec les autres
                                End Try
                            Else
                                ' Pré-télécharger les autres en cache sans afficher
                                Try
                                    ' Use HttpClient instead of obsolete WebClient
                                    Using http As New System.Net.Http.HttpClient()
                                        http.DefaultRequestHeaders.UserAgent.ParseAdd("AudioPlay/1.0")
                                        Dim httpResponse = Await http.GetAsync(url)
                                        If httpResponse.IsSuccessStatusCode Then
                                            Dim imageBytes = Await httpResponse.Content.ReadAsByteArrayAsync()
                                            cachePochettesBytes(url) = imageBytes
                                            historiquePochettes.Add(url)
                                            compteurCharge += 1
                                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Pochette {compteurCharge}/{toutesLesUrls.Count} pré-chargée en cache")
                                        Else
                                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Échec pré-chargement {url}: HTTP {httpResponse.StatusCode}")
                                        End If
                                    End Using
                                Catch ex As Exception
                                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Échec pré-chargement {url}: {ex.Message}")
                                End Try
                            End If
                        Catch ex As Exception
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Échec téléchargement {url}: {ex.Message}")
                        End Try
                    Else
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⊙ Pochette déjà en cache/historique: {url}")
                    End If
                Next

                ' Mettre à jour les boutons de navigation
                MettreAJourBoutonsNavigation()

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🎉 {historiquePochettes.Count} pochette(s) disponible(s) pour navigation")
            End If

            ' Si aucune pochette trouvée
            If historiquePochettes.Count = 0 Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ❌ Aucune pochette trouvée sur aucune source")
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur ChargerPochetteAlbum: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Stack: {ex.StackTrace}")
        End Try
    End Function

    '''
    ''' Recherche l'URL de la pochette sur iTunes Search API
    ''' Ne nécessite pas de clé API, excellente qualité d'images
    ''' </summary>
    Private Async Function RechercherPochetteiTunes(artiste As String, album As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(10)

                ' Construire la requête iTunes Search API
                Dim query = Uri.EscapeDataString($"{artiste} {album}")
                Dim searchUrl = $"https://itunes.apple.com/search?term={query}&entity=album&limit=1"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche iTunes: {searchUrl}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette
                Dim coverUrl = ExtraireUrlPochetteITunesDeJSON(response)

                If Not String.IsNullOrWhiteSpace(coverUrl) Then
                    ' iTunes retourne des images 100x100 par défaut, on peut les agrandir
                    ' Remplacer 100x100bb.jpg par 600x600bb.jpg pour meilleure qualité
                    coverUrl = coverUrl.Replace("100x100bb.jpg", "600x600bb.jpg")
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] iTunes - URL pochette trouvée: {coverUrl}")
                    Return coverUrl
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche iTunes: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Extrait l'URL de la pochette depuis la réponse JSON d'iTunes
    ''' </summary>
    Private Function ExtraireUrlPochetteITunesDeJSON(jsonResponse As String) As String
        Try
            ' Parser manuellement le JSON pour éviter une dépendance externe
            ' Format: {"resultCount":1,"results":[{"artworkUrl100":"https://..."}]}

            Dim artworkKey = """artworkUrl100"":"""
            Dim startIndex = jsonResponse.IndexOf(artworkKey)

            If startIndex >= 0 Then
                startIndex += artworkKey.Length
                Dim endIndex = jsonResponse.IndexOf("""", startIndex)

                If endIndex > startIndex Then
                    Dim url = jsonResponse.Substring(startIndex, endIndex - startIndex)
                    ' Déséchapper les caractères JSON
                    url = url.Replace("\/", "/")
                    Return url
                End If
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur parsing JSON iTunes: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur Last.fm API
    ''' Utilise une clé API publique (lecture seule)
    ''' </summary>
    Private Async Function RechercherPochetteLastFm(artiste As String, album As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(10)

                ' Construire la requête Last.fm API
                Dim artisteEncode = Uri.EscapeDataString(artiste)
                Dim albumEncode = Uri.EscapeDataString(album)
                Dim searchUrl = $"http://ws.audioscrobbler.com/2.0/?method=album.getinfo&api_key={LASTFM_API_KEY}&artist={artisteEncode}&album={albumEncode}&format=json"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche Last.fm: {artiste} - {album}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette
                Dim coverUrl = ExtraireUrlPochetteLastFmDeJSON(response)

                If Not String.IsNullOrWhiteSpace(coverUrl) Then
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Last.fm - URL pochette trouvée: {coverUrl}")
                    Return coverUrl
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche Last.fm: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Extrait l'URL de la pochette depuis la réponse JSON de Last.fm
    ''' </summary>
    Private Function ExtraireUrlPochetteLastFmDeJSON(jsonResponse As String) As String
        Try
            ' Parser manuellement le JSON pour éviter une dépendance externe
            ' Last.fm retourne plusieurs tailles: small, medium, large, extralarge, mega
            ' Format: "image":[{"#text":"url","size":"small"},{"#text":"url","size":"extralarge"}]

            ' Chercher la plus grande taille disponible (extralarge ou mega)
            Dim patterns As String() = {
                """size"":""mega""",
                """size"":""extralarge""",
                """size"":""large"""
            }

            For Each pattern In patterns
                Dim sizeIndex = jsonResponse.IndexOf(pattern)
                If sizeIndex >= 0 Then
                    ' Chercher le "#text" qui précède ce size
                    Dim textKey = """#text"":"""
                    Dim searchStart = Math.Max(0, sizeIndex - 200) ' Chercher dans les 200 caractères précédents
                    Dim textIndex = jsonResponse.LastIndexOf(textKey, sizeIndex)

                    If textIndex >= searchStart Then
                        Dim urlStart = textIndex + textKey.Length
                        Dim urlEnd = jsonResponse.IndexOf("""", urlStart)

                        If urlEnd > urlStart Then
                            Dim url = jsonResponse.Substring(urlStart, urlEnd - urlStart)
                            ' Déséchapper les caractères JSON
                            url = url.Replace("\/", "/")

                            ' Vérifier que l'URL n'est pas vide (Last.fm retourne parfois des URLs vides)
                            If Not String.IsNullOrWhiteSpace(url) AndAlso url.StartsWith("http") Then
                                Return url
                            End If
                        End If
                    End If
                End If
            Next

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur parsing JSON Last.fm: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur MusicBrainz en utilisant l'artiste et l'album
    ''' </summary>
    Private Async Function RechercherPochetteMusicBrainz(artiste As String, album As String) As Task(Of String)
        Try
            ' Activer TLS 1.2 et 1.3 pour résoudre les problèmes SSL
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 Or System.Net.SecurityProtocolType.Tls13

            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(15)
                client.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0 (https://github.com/jeanpel58/AudioPlay)")

                ' Rechercher le release sur MusicBrainz
                Dim query = Uri.EscapeDataString($"{artiste} {album}")
                Dim searchUrl = $"https://musicbrainz.org/ws/2/release/?query={query}&fmt=json&limit=1"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche MusicBrainz: {searchUrl}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire le release ID
                Dim releaseId = ExtraireReleaseIdDeJSON(response)

                If Not String.IsNullOrWhiteSpace(releaseId) Then
                    ' Construire l'URL de la pochette depuis Cover Art Archive
                    Dim coverUrl = $"https://coverartarchive.org/release/{releaseId}/front-500"
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] URL pochette trouvée: {coverUrl}")
                    Return coverUrl
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche pochette: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Extrait le Release ID depuis la réponse JSON de MusicBrainz
    ''' </summary>
    Private Function ExtraireReleaseIdDeJSON(json As String) As String
        Try
            ' Rechercher le pattern "id":"xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
            Dim pattern = """releases"":\s*\[\s*\{\s*""id""\s*:\s*""([a-f0-9\-]+)"""
            Dim match = System.Text.RegularExpressions.Regex.Match(json, pattern)

            If match.Success AndAlso match.Groups.Count > 1 Then
                Return match.Groups(1).Value
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur extraction Release ID: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur Deezer API (pas de clé API requise)
    ''' </summary>
    Private Async Function RechercherPochetteDeezer(artiste As String, album As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(15)
                client.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0")

                ' Construire la requête Deezer API (pas de clé requise)
                Dim artisteEncode = Uri.EscapeDataString(artiste)
                Dim albumEncode = Uri.EscapeDataString(album)
                Dim searchUrl = $"https://api.deezer.com/search/album?q=artist:""{artisteEncode}"" album:""{albumEncode}"""

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche Deezer: {artiste} - {album}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette
                ' Format: {"data":[{"cover_xl":"https://..."}]}
                Dim coverKey = """cover_xl"":"""
                Dim startIndex = response.IndexOf(coverKey)

                If startIndex >= 0 Then
                    startIndex += coverKey.Length
                    Dim endIndex = response.IndexOf("""", startIndex)

                    If endIndex > startIndex Then
                        Dim url = response.Substring(startIndex, endIndex - startIndex)
                        url = url.Replace("\/", "/")

                        If Not String.IsNullOrWhiteSpace(url) AndAlso url.StartsWith("http") Then
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Deezer - URL pochette trouvée: {url}")
                            Return url
                        End If
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche Deezer: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur Discogs API (idéal pour CDs physiques)
    ''' </summary>
    Private Async Function RechercherPochetteDiscogs(artiste As String, album As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(15)
                client.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0 (https://github.com/jeanpel58/AudioPlay)")

                ' Construire la requête Discogs API
                Dim query = Uri.EscapeDataString($"{artiste} {album}")
                Dim searchUrl = $"https://api.discogs.com/database/search?q={query}&type=release&format=CD"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche Discogs: {artiste} - {album}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette
                ' Format: {"results":[{"cover_image":"https://..."}]}
                Dim coverKey = """cover_image"":"""
                Dim startIndex = response.IndexOf(coverKey)

                If startIndex >= 0 Then
                    startIndex += coverKey.Length
                    Dim endIndex = response.IndexOf("""", startIndex)

                    If endIndex > startIndex Then
                        Dim url = response.Substring(startIndex, endIndex - startIndex)
                        url = url.Replace("\/", "/")

                        ' Discogs retourne parfois des URLs spacer.gif, les ignorer
                        If Not String.IsNullOrWhiteSpace(url) AndAlso url.StartsWith("http") AndAlso Not url.Contains("spacer.gif") Then
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Discogs - URL pochette trouvée: {url}")
                            Return url
                        End If
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche Discogs: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur TheAudioDB API (gratuit, pas de rate limiting strict)
    ''' </summary>
    Private Async Function RechercherPochetteTheAudioDB(artiste As String, album As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(15)
                client.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0")

                ' Clé API publique gratuite de TheAudioDB
                Dim apiKey = "523532" ' Clé publique pour tests
                Dim artisteEncode = Uri.EscapeDataString(artiste)
                Dim albumEncode = Uri.EscapeDataString(album)
                Dim searchUrl = $"https://www.theaudiodb.com/api/v1/json/{apiKey}/searchalbum.php?s={artisteEncode}&a={albumEncode}"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche TheAudioDB: {artiste} - {album}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette
                ' Format: {"album":[{"strAlbumThumb":"https://..."}]}
                Dim coverKey = """strAlbumThumb"":"""
                Dim startIndex = response.IndexOf(coverKey)

                If startIndex >= 0 Then
                    startIndex += coverKey.Length
                    Dim endIndex = response.IndexOf("""", startIndex)

                    If endIndex > startIndex Then
                        Dim url = response.Substring(startIndex, endIndex - startIndex)
                        url = url.Replace("\/", "/")

                        If Not String.IsNullOrWhiteSpace(url) AndAlso url.StartsWith("http") Then
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] TheAudioDB - URL pochette trouvée: {url}")
                            Return url
                        End If
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche TheAudioDB: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Recherche l'URL de la pochette sur Fanart.tv API (haute résolution, nécessite MusicBrainz ID)
    ''' </summary>
    Private Async Function RechercherPochetteFanartTV(artiste As String, album As String) As Task(Of String)
        Try
            ' Fanart.tv nécessite un MusicBrainz ID, essayons de le trouver d'abord
            Dim mbid = Await ObtenirMusicBrainzArtistID(artiste)

            If String.IsNullOrWhiteSpace(mbid) Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Fanart.tv - MusicBrainz ID non trouvé pour {artiste}")
                Return Nothing
            End If

            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(10)

                ' Clé API publique gratuite de Fanart.tv
                Dim apiKey = "2c4ecd81b39b48289f8b18798c0949e0" ' Clé publique
                Dim searchUrl = $"http://webservice.fanart.tv/v3/music/{mbid}?api_key={apiKey}"

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche Fanart.tv avec MBID: {mbid}")

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser la réponse JSON pour extraire l'URL de la pochette album
                ' Format: {"albums":{"{album-mbid}":{"albumcover":[{"url":"https://..."}]}}}
                Dim coverKey = """albumcover"":\[\{""url"":"""
                Dim startIndex = response.IndexOf(coverKey)

                If startIndex >= 0 Then
                    startIndex += coverKey.Length
                    Dim endIndex = response.IndexOf("""", startIndex)

                    If endIndex > startIndex Then
                        Dim url = response.Substring(startIndex, endIndex - startIndex)
                        url = url.Replace("\/", "/")

                        If Not String.IsNullOrWhiteSpace(url) AndAlso url.StartsWith("http") Then
                            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Fanart.tv - URL pochette HD trouvée: {url}")
                            Return url
                        End If
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur recherche Fanart.tv: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Obtenir le MusicBrainz Artist ID pour Fanart.tv
    ''' </summary>
    Private Async Function ObtenirMusicBrainzArtistID(artiste As String) As Task(Of String)
        Try
            Using client As New System.Net.Http.HttpClient()
                client.Timeout = TimeSpan.FromSeconds(5)
                client.DefaultRequestHeaders.Add("User-Agent", "AudioPlay/1.0 (https://github.com/jeanpel58/AudioPlay)")

                Dim query = Uri.EscapeDataString(artiste)
                Dim searchUrl = $"https://musicbrainz.org/ws/2/artist/?query=artist:{query}&fmt=json&limit=1"

                Dim response = Await client.GetStringAsync(searchUrl)

                ' Parser pour extraire l'ID
                Dim idKey = """artists"":\[\{""id"":"""
                Dim startIndex = response.IndexOf(idKey)

                If startIndex >= 0 Then
                    startIndex += idKey.Length
                    Dim endIndex = response.IndexOf("""", startIndex)

                    If endIndex > startIndex Then
                        Return response.Substring(startIndex, endIndex - startIndex)
                    End If
                End If
            End Using

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur obtention MBID: {ex.Message}")
        End Try

        Return Nothing
    End Function

    ''' <summary>
    ''' Télécharge et affiche une pochette depuis une URL
    ''' </summary>
    ''' <param name="url">URL de l'image</param>
    ''' <param name="ajouterHistorique">Si True, ajoute l'URL à l'historique (défaut: True)</param>
    Private Async Function TelechargerEtAfficherPochette(url As String, Optional ajouterHistorique As Boolean = True) As Task
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Début téléchargement: {url}")

            Dim imageBytes As Byte()

            ' Vérifier d'abord si l'image est déjà en cache mémoire
            If cachePochettesBytes.ContainsKey(url) Then
                imageBytes = cachePochettesBytes(url)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Image récupérée depuis cache mémoire ({imageBytes.Length} bytes)")
            Else
                ' Use HttpClient instead of WebClient (WebClient is obsolete)
                Using http As New System.Net.Http.HttpClient()
                    http.DefaultRequestHeaders.UserAgent.ParseAdd("AudioPlay/1.0")
                    Try
                        Dim resp = Await http.GetAsync(url)
                        If resp.IsSuccessStatusCode Then
                            imageBytes = Await resp.Content.ReadAsByteArrayAsync()
                        Else
                            Throw New Exception($"HTTP {(CInt(resp.StatusCode))} {resp.ReasonPhrase}")
                        End If
                    Catch exHttp As Exception
                        ' Fallback: try with WebClient for compatibility (synchronous to avoid Await in Catch)
                        Try
                            Using client As New System.Net.WebClient()
                                client.Headers.Add("User-Agent", "AudioPlay/1.0")
                                imageBytes = client.DownloadData(url)
                            End Using
                        Catch ex2 As Exception
                            Throw
                        End Try
                    End Try
                End Using

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] {imageBytes.Length} bytes téléchargés")

                ' Ajouter au cache mémoire
                cachePochettesBytes(url) = imageBytes
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Image ajoutée au cache mémoire")
            End If

            ' Stocker temporairement l'URL et les bytes (seront sauvegardés dans le cache lors du clic sur ButtonExtraire)
            pochetteTempUrl = url
            pochetteTempBytes = imageBytes
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Image stockée temporairement (cache différé)")

            ' Créer l'image depuis les bytes
            Using ms As New System.IO.MemoryStream(imageBytes)
                Using tempImage = Image.FromStream(ms)
                    ' Créer une copie de l'image dans un nouveau Bitmap via thread-safe helper
                    SafeSetPictureBoxImage(tempImage)
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✅ IMAGE AFFICHÉE dans TelechargerEtAfficherPochette: {url}")
                End Using
            End Using

            ' Ajouter l'URL à l'historique seulement si demandé
            If ajouterHistorique Then
                AjouterAHistoriquePochettes(url)
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur téléchargement pochette: {ex.Message}")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Type: {ex.GetType().FullName}")
            If ex.InnerException IsNot Nothing Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Inner: {ex.InnerException.Message}")
            End If
            ' Ne pas relancer l'exception pour permettre au téléchargement des autres pochettes de continuer
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Téléchargement échoué, passage à la pochette suivante")
        End Try
    End Function

    '''
    ''' Sauvegarde la pochette dans le répertoire d'extraction avec le nom formaté "Artiste - Album.ext"
    ''' </summary>
    Private Sub SauvegarderPochetteDansRepertoire(imageBytes As Byte())
        Try
            ' Vérifier que le répertoire d'extraction est valide
            Dim repertoireExtraction = TextBoxRepSauvegarde.Text
            If String.IsNullOrWhiteSpace(repertoireExtraction) OrElse Not Directory.Exists(repertoireExtraction) Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Répertoire d'extraction invalide, pochette non sauvegardée localement")
                Return
            End If

            ' Récupérer l'artiste et l'album
            Dim artiste = TextBoxCDArtiste.Text.Trim()
            Dim album = TextBoxCDTitre.Text.Trim()

            If String.IsNullOrWhiteSpace(artiste) OrElse String.IsNullOrWhiteSpace(album) Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Artiste ou album manquant, pochette non sauvegardée localement")
                Return
            End If

            ' Créer le répertoire de l'album: "(Année) Artiste - Album"
            Dim annee As String = TextBoxAnnee.Text.Trim()
            Dim nomRepertoireAlbum As String = ""
            If Not String.IsNullOrEmpty(annee) Then
                nomRepertoireAlbum = $"({annee}) "
            End If
            nomRepertoireAlbum &= $"{artiste} - {album}"
            nomRepertoireAlbum = NettoyerNomFichier(nomRepertoireAlbum)

            Dim cheminRepertoireAlbum As String = Path.Combine(repertoireExtraction, nomRepertoireAlbum)

            ' Créer le répertoire s'il n'existe pas
            If Not Directory.Exists(cheminRepertoireAlbum) Then
                Directory.CreateDirectory(cheminRepertoireAlbum)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Répertoire album créé: {cheminRepertoireAlbum}")
            End If

            ' Détecter le format de l'image depuis les bytes
            Dim extension = DetecterFormatImage(imageBytes)

            ' Nettoyer le nom de fichier (supprimer caractères invalides)
            Dim nomFichier = $"{artiste} - {album}{extension}"
            nomFichier = NettoyerNomFichier(nomFichier)

            ' Construire le chemin complet dans le sous-répertoire de l'album
            Dim cheminComplet = Path.Combine(cheminRepertoireAlbum, nomFichier)

            ' Sauvegarder l'image
            File.WriteAllBytes(cheminComplet, imageBytes)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Pochette sauvegardée: {cheminComplet}")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur sauvegarde pochette dans répertoire: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Détecte le format d'une image depuis ses bytes (magic numbers)
    ''' </summary>
    Private Function DetecterFormatImage(imageBytes As Byte()) As String
        If imageBytes Is Nothing OrElse imageBytes.Length < 4 Then
            Return ".jpg" ' Par défaut
        End If

        ' PNG: 89 50 4E 47
        If imageBytes.Length >= 4 AndAlso
           imageBytes(0) = &H89 AndAlso imageBytes(1) = &H50 AndAlso
           imageBytes(2) = &H4E AndAlso imageBytes(3) = &H47 Then
            Return ".png"
        End If

        ' JPEG: FF D8 FF
        If imageBytes.Length >= 3 AndAlso
           imageBytes(0) = &HFF AndAlso imageBytes(1) = &HD8 AndAlso imageBytes(2) = &HFF Then
            Return ".jpg"
        End If

        ' GIF: 47 49 46
        If imageBytes.Length >= 3 AndAlso
           imageBytes(0) = &H47 AndAlso imageBytes(1) = &H49 AndAlso imageBytes(2) = &H46 Then
            Return ".gif"
        End If

        ' WebP: 52 49 46 46 ... 57 45 42 50
        If imageBytes.Length >= 12 AndAlso
           imageBytes(0) = &H52 AndAlso imageBytes(1) = &H49 AndAlso
           imageBytes(2) = &H46 AndAlso imageBytes(3) = &H46 AndAlso
           imageBytes(8) = &H57 AndAlso imageBytes(9) = &H45 AndAlso
           imageBytes(10) = &H42 AndAlso imageBytes(11) = &H50 Then
            Return ".webp"
        End If

        ' Par défaut, supposer JPEG
        Return ".jpg"
    End Function

    ''' <summary>
    ''' S'assure qu'un WAV respecte la qualité choisie (sample rate / bit depth)
    ''' </summary>
    Private Sub EnsureWavQuality(wavPath As String, qualiteIndex As Integer)
        Try
            If String.IsNullOrWhiteSpace(wavPath) OrElse Not File.Exists(wavPath) Then Return

            ' Index 0=16-bit 44.1kHz, 1=24-bit 96kHz, 2=32-bit 192kHz
            Dim sampleRate As Integer = 96000
            Dim bitDepth As Integer = 24

            Select Case qualiteIndex
                Case 0
                    sampleRate = 44100
                    bitDepth = 16
                Case 1
                    sampleRate = 96000
                    bitDepth = 24
                Case 2
                    sampleRate = 192000
                    bitDepth = 32
                Case Else
                    sampleRate = 96000
                    bitDepth = 24
            End Select

            Using checkReader As New NAudio.Wave.WaveFileReader(wavPath)
                If checkReader.WaveFormat.SampleRate = sampleRate AndAlso checkReader.WaveFormat.BitsPerSample = bitDepth Then
                    Return
                End If
            End Using

            Dim tempPath As String = wavPath & ".resample.tmp"
            If File.Exists(tempPath) Then
                File.Delete(tempPath)
            End If

            Using reader As New NAudio.Wave.WaveFileReader(wavPath)
                Dim targetFormat As New NAudio.Wave.WaveFormat(sampleRate, bitDepth, reader.WaveFormat.Channels)
                Using resampler As New NAudio.Wave.MediaFoundationResampler(reader, targetFormat)
                    resampler.ResamplerQuality = 60
                    Using writer As New NAudio.Wave.WaveFileWriter(tempPath, targetFormat)
                        Const bufferSize As Integer = 32768
                        Dim buffer(bufferSize - 1) As Byte
                        Dim bytesRead As Integer
                        Do
                            bytesRead = resampler.Read(buffer, 0, bufferSize)
                            If bytesRead <= 0 Then Exit Do
                            writer.Write(buffer, 0, bytesRead)
                        Loop
                    End Using
                End Using
            End Using

            File.Delete(wavPath)
            File.Move(tempPath, wavPath)
            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_RESAMPLED_AFTER_EXTERNAL_RIP: File={wavPath} SR={sampleRate} Bits={bitDepth}")
        Catch ex As Exception
            CDAudioAnalyzer.DiagnosticWrite($"EXTRACTION_WAV_RESAMPLE_ERROR: File={wavPath} - {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Convertit un WAV en MP3 en utilisant LAME via NAudio.Lame
    ''' </summary>
    ' Optional startSec/endSec allow encoding only a portion of the WAV (seconds relative to WAV start)
    Private Async Function ConvertWavToMp3(cheminWav As String, cheminMp3 As String, qualiteIndex As Integer,
                                          titre As String, artiste As String, numeroPiste As String,
                                          album As String, artisteAlbum As String, annee As String,
                                          genre As String, commentaire As String, pochette As Image,
                                          Optional startSec As Nullable(Of Double) = Nothing, Optional endSec As Nullable(Of Double) = Nothing) As Task(Of Boolean)

        Return Await Task.Run(Function() As Boolean
                                  Dim tmpPath As String = cheminMp3 & ".tmp"
                                  Dim result As Boolean = False
                                  Try
                                      Using reader As New NAudio.Wave.AudioFileReader(cheminWav)
                                          Dim bitrate As Integer = 320
                                          Select Case qualiteIndex
                                              Case 0
                                                  bitrate = 128
                                              Case 1
                                                  bitrate = 192
                                              Case 2
                                                  bitrate = 256
                                              Case 3
                                                  bitrate = 320
                                          End Select

                                          ' Resample to 44100 Hz 16-bit PCM for LAME compatibility
                                          Dim targetFormat = New NAudio.Wave.WaveFormat(44100, 16, reader.WaveFormat.Channels)
                                          Try
                                              CDAudioAnalyzer.DiagnosticWrite($"CONVERT_MP3_START: Track={numeroPiste} WAV={cheminWav} TMP={tmpPath} Target={cheminMp3} Bitrate={bitrate} OrigFormat={reader.WaveFormat} ResampleTo={targetFormat}")
                                          Catch
                                          End Try

                                          Using resampler As New NAudio.Wave.MediaFoundationResampler(reader, targetFormat)
                                              resampler.ResamplerQuality = 60
                                              ' Encode to temporary file first using resampled PCM
                                              Using writer As New NAudio.Lame.LameMP3FileWriter(tmpPath, resampler.WaveFormat, bitrate)
                                                  ' If startSec specified, set reader current time before reading
                                                  Try
                                                      If startSec.HasValue Then
                                                          reader.CurrentTime = TimeSpan.FromSeconds(Math.Max(0.0, startSec.Value))
                                                      End If
                                                  Catch
                                                  End Try

                                                  ' Compute bytes to read from resampler (resampled format)
                                                  Dim bytesRemaining As Long = -1
                                                  Try
                                                      If endSec.HasValue Then
                                                          Dim actualStart As Double = If(startSec.HasValue, startSec.Value, 0.0)
                                                          Dim dur As Double = Math.Max(0.0, endSec.Value - actualStart)
                                                          bytesRemaining = CLng(Math.Round(dur * resampler.WaveFormat.SampleRate)) * resampler.WaveFormat.BlockAlign
                                                      End If
                                                  Catch
                                                      bytesRemaining = -1
                                                  End Try

                                                  Const bufSize As Integer = 32768
                                                  Dim buffer(bufSize - 1) As Byte
                                                  Dim read As Integer = 0
                                                  Do
                                                      Dim toRead As Integer = bufSize
                                                      If bytesRemaining >= 0 Then
                                                          If bytesRemaining <= 0 Then Exit Do
                                                          toRead = CInt(Math.Min(CLng(bufSize), bytesRemaining))
                                                      End If
                                                      read = resampler.Read(buffer, 0, toRead)
                                                      If read <= 0 Then Exit Do
                                                      writer.Write(buffer, 0, read)
                                                      If bytesRemaining >= 0 Then bytesRemaining -= read
                                                  Loop
                                              End Using
                                          End Using

                                          ' After writer closed, validate tmp file
                                          Dim fileOk As Boolean = False
                                          Try
                                              If File.Exists(tmpPath) Then
                                                  Dim fi As New FileInfo(tmpPath)
                                                  CDAudioAnalyzer.DiagnosticWrite($"CONVERT_MP3_TMP_CREATED: Track={numeroPiste} TMP={tmpPath} Size={fi.Length}")
                                                  If fi.Length > 0 Then
                                                      fileOk = True
                                                  End If
                                              Else
                                                  CDAudioAnalyzer.DiagnosticWrite($"CONVERT_MP3_TMP_MISSING: Track={numeroPiste} TMP={tmpPath} not found after encode")
                                              End If
                                          Catch exInfo As Exception
                                              CDAudioAnalyzer.DiagnosticWrite($"CONVERT_MP3_TMP_INFO_ERROR: Track={numeroPiste} TMP={tmpPath} - {exInfo.ToString}")
                                          End Try

                                          If fileOk Then
                                              ' Write metadata to the temp file
                                              Try
                                                  EcrireMetadonnees(tmpPath, titre, artiste, numeroPiste, album, artisteAlbum, annee, genre, commentaire, pochette, TextBoxNumCD.Text)
                                              Catch exMeta As Exception
                                                  CDAudioAnalyzer.DiagnosticWrite($"CONVERT_MP3_META_ERROR: Track={numeroPiste} TMP={tmpPath} - {exMeta.ToString}")
                                              End Try

                                              ' Move temp to final atomically (delete existing if present)
                                              Try
                                                  If File.Exists(cheminMp3) Then
                                                      File.Delete(cheminMp3)
                                                  End If
                                                  File.Move(tmpPath, cheminMp3)
                                                  Dim fi2 As New FileInfo(cheminMp3)
                                                  CDAudioAnalyzer.DiagnosticWrite($"CONVERT_MP3_DONE: Track={numeroPiste} MP3={cheminMp3} Size={fi2.Length}")
                                                  result = True
                                              Catch exMove As Exception
                                                  CDAudioAnalyzer.DiagnosticWrite($"CONVERT_MP3_MOVE_ERROR: Track={numeroPiste} TMP={tmpPath} -> {cheminMp3} - {exMove.ToString}")
                                                  ' Attempt cleanup
                                                  Try
                                                      If File.Exists(tmpPath) Then File.Delete(tmpPath)
                                                  Catch
                                                  End Try
                                                  result = False
                                              End Try
                                          Else
                                              ' tmp file invalid -> cleanup
                                              Try
                                                  If File.Exists(tmpPath) Then File.Delete(tmpPath)
                                              Catch
                                              End Try
                                              CDAudioAnalyzer.DiagnosticWrite($"CONVERT_MP3_FAILED_EMPTY: Track={numeroPiste} WAV={cheminWav}")
                                              result = False
                                          End If
                                      End Using
                                  Catch ex As Exception
                                      CDAudioAnalyzer.DiagnosticWrite($"CONVERT_MP3_EXCEPTION: Track={numeroPiste} WAV={cheminWav} - {ex.ToString}")
                                      ' Ensure tmp removed
                                      Try
                                          If File.Exists(tmpPath) Then File.Delete(tmpPath)
                                      Catch
                                      End Try
                                      result = False
                                  End Try

                                  Return result
                              End Function)
    End Function



    ''' <summary>
    ''' Met à jour les labels d'information de la pochette (dimensions et taille)
    ''' </summary>
    Private Sub MettreAJourInfosPochette()
        Try
            If PictureBoxPochette.Image IsNot Nothing Then
                ' Dimensions de l'image
                Dim largeur = PictureBoxPochette.Image.Width
                Dim hauteur = PictureBoxPochette.Image.Height
                Label_DimImage.Text = $"{largeur} x {hauteur}"

                ' Taille de l'image en bytes (approximatif depuis l'image en mémoire)
                If pochetteTempBytes IsNot Nothing Then
                    ' Si on a les bytes bruts, utiliser leur taille réelle
                    Dim tailleMo = pochetteTempBytes.Length / 1024.0 / 1024.0
                    If tailleMo >= 1.0 Then
                        LabelTailleImage.Text = $"{tailleMo:F2} Mo"
                    Else
                        Dim tailleKo = pochetteTempBytes.Length / 1024.0
                        LabelTailleImage.Text = $"{tailleKo:F0} Ko"
                    End If
                Else
                    ' Sinon, estimer depuis l'image
                    Using ms As New MemoryStream()
                        PictureBoxPochette.Image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg)
                        Dim tailleMo = ms.Length / 1024.0 / 1024.0
                        If tailleMo >= 1.0 Then
                            LabelTailleImage.Text = $"{tailleMo:F2} Mo"
                        Else
                            Dim tailleKo = ms.Length / 1024.0
                            LabelTailleImage.Text = $"{tailleKo:F0} Ko"
                        End If
                    End Using
                End If
            Else
                ' Pas d'image, vider les labels
                Label_DimImage.Text = ""
                LabelTailleImage.Text = ""
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur mise à jour infos pochette: {ex.Message}")
            Label_DimImage.Text = ""
            LabelTailleImage.Text = ""
        End Try
    End Sub

    ''' <summary>
    ''' Empêche le changement de checkbox pour l'item en cours d'édition uniquement
    ''' </summary>
    Private Sub ListViewCompress_ItemCheck(sender As Object, e As ItemCheckEventArgs) Handles ListViewCompress.ItemCheck
        ' Bloquer uniquement si on est en train d'éditer cet item spécifique
        If editingItem IsNot Nothing AndAlso e.Index = editingItem.Index Then
            ' Annuler le changement et garder l'état original
            e.NewValue = If(editingItemOriginalCheckedState, CheckState.Checked, CheckState.Unchecked)
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Changement de checkbox bloqué pour l'item en édition (index {e.Index})")
        End If
    End Sub

    ''' <summary>
    ''' Détecte le clic AVANT le changement de checkbox pour protéger les zones éditables
    ''' </summary>
    Private Sub ListViewCompress_MouseDown(sender As Object, e As MouseEventArgs) Handles ListViewCompress.MouseDown
        Try
            ' Si on est déjà en train d'éditer, ne rien faire
            If editTextBox IsNot Nothing Then
                Return
            End If

            Dim hitTest = ListViewCompress.HitTest(e.Location)

            ' Si on clique sur un item et un sous-item
            If hitTest.Item IsNot Nothing AndAlso hitTest.SubItem IsNot Nothing Then
                Dim subItemIndex As Integer = hitTest.Item.SubItems.IndexOf(hitTest.SubItem)

                ' Si c'est une colonne éditable (Titre ou Artiste)
                If subItemIndex = 1 OrElse subItemIndex = 2 Then
                    ' Préparer la protection pour cet item
                    editingItem = hitTest.Item
                    editingItemOriginalCheckedState = hitTest.Item.Checked
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Protection activée pour index {hitTest.Item.Index}, état: {editingItemOriginalCheckedState}")
                Else
                    ' Si on clique ailleurs, désactiver la protection
                    editingItem = Nothing
                End If
            Else
                editingItem = Nothing
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur MouseDown: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire du double-clic sur la ListView pour éditer Titre ou Artiste
    ''' </summary>
    Private Sub ListViewCompress_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles ListViewCompress.MouseDoubleClick
        Try
            Dim hitTest = ListViewCompress.HitTest(e.Location)

            ' Vérifier qu'on a cliqué sur un item et un sous-item
            If hitTest.Item IsNot Nothing AndAlso hitTest.SubItem IsNot Nothing Then
                ' Déterminer l'index du sous-item
                Dim subItemIndex As Integer = hitTest.Item.SubItems.IndexOf(hitTest.SubItem)

                ' Permettre l'édition seulement pour les colonnes Titre (1) et Artiste (2)
                If subItemIndex = 1 OrElse subItemIndex = 2 Then
                    ' editingItem et editingItemOriginalCheckedState ont déjà été configurés par MouseDown
                    DemarrerEdition(hitTest.Item, subItemIndex)
                End If
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur MouseDoubleClick: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Démarre l'édition d'une cellule de la ListView
    ''' </summary>
    Private Sub DemarrerEdition(item As ListViewItem, subItemIndex As Integer)
        Try
            ' Fermer toute édition en cours
            TerminerEdition(True)

            editingItem = item
            editingSubItemIndex = subItemIndex

            ' Obtenir les coordonnées du sous-item
            Dim subItemRect As Rectangle = item.SubItems(subItemIndex).Bounds

            ' Créer le TextBox d'édition
            editTextBox = New TextBox() With {
                .Bounds = subItemRect,
                .Text = item.SubItems(subItemIndex).Text,
                .BorderStyle = BorderStyle.FixedSingle,
                .Font = ListViewCompress.Font,
                .BackColor = Color.LightYellow
            }

            ' Ajouter les gestionnaires d'événements
            AddHandler editTextBox.KeyPress, AddressOf EditTextBox_KeyPress
            AddHandler editTextBox.LostFocus, AddressOf EditTextBox_LostFocus

            ' Ajouter le TextBox à la ListView
            ListViewCompress.Controls.Add(editTextBox)
            editTextBox.Focus()
            editTextBox.SelectAll()

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Édition démarrée - Colonne {subItemIndex}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur DemarrerEdition: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gère les touches dans le TextBox d'édition
    ''' </summary>
    Private Sub EditTextBox_KeyPress(sender As Object, e As KeyPressEventArgs)
        If e.KeyChar = ChrW(Keys.Enter) Then
            e.Handled = True
            TerminerEdition(True) ' Valider
        ElseIf e.KeyChar = ChrW(Keys.Escape) Then
            e.Handled = True
            TerminerEdition(False) ' Annuler
        End If
    End Sub

    ''' <summary>
    ''' Gère la perte de focus du TextBox d'édition
    ''' </summary>
    Private Sub EditTextBox_LostFocus(sender As Object, e As EventArgs)
        TerminerEdition(True) ' Valider par défaut
    End Sub

    ''' <summary>
    ''' Termine l'édition et met à jour la valeur
    ''' </summary>
    Private Sub TerminerEdition(valider As Boolean)
        Try
            If editTextBox IsNot Nothing Then
                ' Retirer les gestionnaires d'événements
                RemoveHandler editTextBox.KeyPress, AddressOf EditTextBox_KeyPress
                RemoveHandler editTextBox.LostFocus, AddressOf EditTextBox_LostFocus

                ' Mettre à jour la valeur si validé
                If valider AndAlso editingItem IsNot Nothing AndAlso editingSubItemIndex >= 0 Then
                    Dim nouvelleValeur As String = editTextBox.Text.Trim()
                    editingItem.SubItems(editingSubItemIndex).Text = nouvelleValeur

                    Dim colonneNom As String = If(editingSubItemIndex = 1, "Titre", "Artiste")
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] {colonneNom} mis à jour: {nouvelleValeur}")
                End If

                ' Supprimer le TextBox
                ListViewCompress.Controls.Remove(editTextBox)
                editTextBox.Dispose()
                editTextBox = Nothing
                editingItem = Nothing
                editingSubItemIndex = -1

                ' Redonner le focus à la ListView
                ListViewCompress.Focus()
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur TerminerEdition: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire pour sélectionner/désélectionner toutes les pistes
    ''' </summary>
    Private Sub CheckBox_FCompress_SelectDeselect_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox_FCompress_SelectDeselect.CheckedChanged
        Try
            ' Éviter les boucles infinies en cas de modification programmatique
            If ListViewCompress Is Nothing OrElse ListViewCompress.Items.Count = 0 Then
                Return
            End If

            ' Désactiver temporairement la protection pour permettre le SelectDeselect
            Dim tempEditingItem = editingItem
            editingItem = Nothing

            ' Cocher ou décocher toutes les pistes selon l'état de la CheckBox
            Dim etatCoche As Boolean = CheckBox_FCompress_SelectDeselect.Checked

            For Each item As ListViewItem In ListViewCompress.Items
                item.Checked = etatCoche
            Next

            ' Restaurer la protection si on était en train d'éditer
            editingItem = tempEditingItem

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Toutes les pistes {If(etatCoche, "cochées", "décochées")}")
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur lors de la sélection/désélection: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton EditTracks - Ouvre FormEditTracks pour éditer les métadonnées
    ''' </summary>
    Private Sub Button_EditTracks_Click(sender As Object, e As EventArgs) Handles Button_EditTracks.Click
        Try
            ' Vérifier qu'il y a des pistes à éditer
            If ListViewCompress Is Nothing OrElse ListViewCompress.Items.Count = 0 Then
                MessageBox.Show(LanguageManager.GetString("FormCompresser_NoTrackToEdit_Message"), LanguageManager.GetString("FormCompresser_NoTrackToEdit_Title"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Créer et afficher le formulaire d'édition
            Using formEdit As New FormEditTracks()
                ' Initialiser avec le ListView
                formEdit.InitialiserAvecListView(ListViewCompress)

                ' Positionner le formulaire juste au-dessus de ListViewCompress et centré horizontalement
                ' Obtenir la position absolue de ListViewCompress à l'écran
                Dim listViewScreenPos = ListViewCompress.PointToScreen(New Point(0, 0))

                ' Calculer la position X centrée horizontalement avec FormCompresser
                Dim centreFormCompresser = Me.Left + (Me.Width \ 2)
                Dim posX = centreFormCompresser - (formEdit.Width \ 2)

                ' Position Y juste au-dessus de ListViewCompress (avec un petit décalage)
                Dim posY = listViewScreenPos.Y - formEdit.Height - 10

                ' S'assurer que le formulaire reste dans les limites de l'écran
                If posX < 0 Then posX = 10
                If posY < 0 Then posY = 10

                ' StartPosition doit être Manual pour pouvoir définir la position
                formEdit.StartPosition = FormStartPosition.Manual
                formEdit.Location = New Point(posX, posY)

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] FormEditTracks positionnée à ({posX}, {posY})")

                ' Afficher en mode modal
                Dim result = formEdit.ShowDialog(Me)

                ' Les modifications sont automatiquement appliquées au ListView
                If result = DialogResult.OK Then
                    System.Diagnostics.Debug.WriteLine("[FormCompresser] Édition des pistes terminée avec succès")
                End If
            End Using
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur Button_EditTracks_Click: {ex.Message}")
            MessageBox.Show($"Erreur lors de l'ouverture de l'éditeur : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton Quitter
    ''' </summary>
    Private Sub ButtonQuitter_Click(sender As Object, e As EventArgs) Handles ButtonQuitter.Click
        ' Fermer FormCompresser et revenir à FormSelecteurPistesCD
        ' (FormSelecteurPistesCD est en attente avec ShowDialog, elle réapparaîtra automatiquement)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    ''' <summary>
    ''' Gestionnaire du bouton Soumettre à GnuDB
    ''' </summary>
    Private Async Sub ButtonSoumettreGnuDB_Click(sender As Object, e As EventArgs) Handles ButtonSoumettreGnuDB.Click
        Try
            System.Diagnostics.Debug.WriteLine("[FormCompresser] Clic sur Soumettre à GnuDB")

            ' Valider les métadonnées avant soumission
            If String.IsNullOrWhiteSpace(TextBoxCDArtiste.Text) OrElse String.IsNullOrWhiteSpace(TextBoxCDTitre.Text) Then
                MessageBox.Show(
                    LanguageManager.GetString("GnuDB_SubmitMissingData"),
                    LanguageManager.GetString("GnuDB_SubmitErrorTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Return
            End If

            ' Vérifier que toutes les pistes ont un titre
            For Each item As ListViewItem In ListViewCompress.Items
                If item.SubItems.Count < 2 OrElse String.IsNullOrWhiteSpace(item.SubItems(1).Text) Then
                    MessageBox.Show(
                        LanguageManager.GetString("GnuDB_SubmitMissingData"),
                        LanguageManager.GetString("GnuDB_SubmitErrorTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning)
                    Return
                End If
            Next

            ' Demander l'adresse email de l'utilisateur (OBLIGATOIRE selon spec GnuDB)
            Dim formEmail As New FormInputEmail(
                LanguageManager.GetString("GnuDB_EmailPrompt"),
                LanguageManager.GetString("GnuDB_EmailTitle"),
                "")

            Dim emailUtilisateur As String = ""
            If formEmail.ShowDialog(Me) = DialogResult.OK Then
                emailUtilisateur = formEmail.Email
            Else
                ' L'utilisateur a annulé, sortir silencieusement
                Return
            End If

            ' Valider l'email seulement si l'utilisateur n'a pas annulé
            If String.IsNullOrWhiteSpace(emailUtilisateur) OrElse Not emailUtilisateur.Contains("@") Then
                MessageBox.Show(
                    LanguageManager.GetString("GnuDB_EmailInvalid"),
                    LanguageManager.GetString("GnuDB_EmailMissing"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning)
                Return
            End If

            ' Demander la catégorie
            Dim categoriesGnuDB = {"blues", "classical", "country", "data", "folk", "jazz", "misc", "newage", "reggae", "rock", "soundtrack"}
            Dim categorieChoisie = "rock" ' Par défaut

            ' Essayer de mapper le genre actuel vers une catégorie GnuDB
            If ComboBoxGenre.SelectedItem IsNot Nothing Then
                Dim genreActuel = ComboBoxGenre.SelectedItem.ToString().ToLower()
                If categoriesGnuDB.Contains(genreActuel) Then
                    categorieChoisie = genreActuel
                ElseIf genreActuel.Contains("class") Then
                    categorieChoisie = "classical"
                ElseIf genreActuel.Contains("countr") Then
                    categorieChoisie = "country"
                ElseIf genreActuel.Contains("folk") Then
                    categorieChoisie = "folk"
                ElseIf genreActuel.Contains("jazz") Then
                    categorieChoisie = "jazz"
                ElseIf genreActuel.Contains("reggae") Then
                    categorieChoisie = "reggae"
                End If
            End If

            ' Demander confirmation et choix test/submit
            Dim modeTest = MessageBox.Show(
                String.Format(LanguageManager.GetString("GnuDB_Category"), categorieChoisie) & vbCrLf & vbCrLf &
                LanguageManager.GetString("GnuDB_ModePrompt"),
                LanguageManager.GetString("GnuDB_ModeTitle"),
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question)

            If modeTest = DialogResult.Cancel Then
                Return
            End If

            Dim estModeTest = (modeTest = DialogResult.Yes)

            ' Préparer les métadonnées
            Dim cdInfo As New CDMetadataProvider.CDInfo()
            cdInfo.Artist = TextBoxCDArtiste.Text.Trim()
            cdInfo.Album = TextBoxCDTitre.Text.Trim()
            If Integer.TryParse(TextBoxAnnee.Text, cdInfo.Year) = False Then
                cdInfo.Year = 0
            End If
            cdInfo.Genre = categorieChoisie
            cdInfo.Tracks = New List(Of CDMetadataProvider.TrackInfo)()

            ' Ajouter les pistes
            For i As Integer = 0 To pistesCD.Count - 1
                Dim trackInfo As New CDMetadataProvider.TrackInfo()
                trackInfo.TrackNumber = pistesCD(i).TrackNumber
                trackInfo.Title = ListViewCompress.Items(i).SubItems(1).Text ' Colonne Titre
                trackInfo.Artist = ListViewCompress.Items(i).SubItems(2).Text ' Colonne Artiste
                trackInfo.Duration = pistesCD(i).Duration
                cdInfo.Tracks.Add(trackInfo)
            Next

            ' Calculer le DiscID CDDB
            Dim discID As String = GnuDBMetadataProvider.CalculerCDDBDiscID(pistesCD)

            If String.IsNullOrWhiteSpace(discID) Then
                MessageBox.Show(
                    LanguageManager.GetString("GnuDB_DiscIDError"),
                    LanguageManager.GetString("GnuDB_Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error)
                Return
            End If

            ' Afficher un curseur d'attente
            Me.Cursor = Cursors.WaitCursor
            Try
                ' Soumettre via HTTP POST (format conforme GnuDB)
                Dim resultat = Await GnuDBMetadataProvider.SoumettreViaHTTP(discID, categorieChoisie, cdInfo, pistesCD, emailUtilisateur, estModeTest)

                ' Afficher le résultat
                MessageBox.Show(
                    resultat,
                    If(estModeTest, LanguageManager.GetString("GnuDB_TestTitle"), LanguageManager.GetString("GnuDB_SubmitSuccessTitle")),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information)

                System.Diagnostics.Debug.WriteLine("[FormCompresser] Soumission GnuDB terminée avec succès")

            Finally
                Me.Cursor = Cursors.Default
            End Try

        Catch ex As Exception
            Me.Cursor = Cursors.Default
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur ButtonSoumettreGnuDB_Click: {ex.Message}")
            MessageBox.Show(
                LanguageManager.GetString("GnuDB_SubmitError") & vbCrLf & vbCrLf & ex.Message,
                LanguageManager.GetString("GnuDB_SubmitErrorTitle"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error)
        End Try
    End Sub


    ' ==================== NAVIGATION POCHETTES ====================

    ''' <summary>
    ''' Bouton Précédent - Revenir à la pochette précédente dans l'historique
    ''' </summary>
    Private Async Sub Button_Image_Prec_Click(sender As Object, e As EventArgs) Handles Button_Image_Prec.Click
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ═══ CLIC PRÉCÉDENT ═══")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Avant: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")

            If indexPochetteActuelle > 0 Then
                indexPochetteActuelle -= 1
                Dim url = historiquePochettes(indexPochetteActuelle)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Navigation ← Précédent: {url} (index {indexPochetteActuelle})")

                ' Ne PAS ajouter à l'historique ni modifier les métadonnées pendant la navigation
                Await TelechargerEtAfficherPochette(url, ajouterHistorique:=False)

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Après téléchargement: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")

                ' NE PAS mettre à jour metadonneesCD.CoverArtUrl ici - seulement au moment de l'extraction
                MettreAJourBoutonsNavigation()
            Else
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Impossible d'aller en arrière (index={indexPochetteActuelle})")
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur Button_Image_Prec: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Bouton Suivant - Chercher une nouvelle pochette alternative
    ''' </summary>
    Private Async Sub Button_Image_Suiv_Click(sender As Object, e As EventArgs) Handles Button_Image_Suiv.Click
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] ═══ CLIC SUIVANT ═══")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Avant: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")

            ' Si on a déjà un historique et qu'on n'est pas à la fin, avancer dans l'historique
            If indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count - 1 Then
                indexPochetteActuelle += 1
                Dim url = historiquePochettes(indexPochetteActuelle)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Navigation → Suivant (historique): {url} (index {indexPochetteActuelle})")

                ' Ne PAS ajouter à l'historique ni modifier les métadonnées pendant la navigation
                Await TelechargerEtAfficherPochette(url, ajouterHistorique:=False)

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Après téléchargement: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")

                ' NE PAS mettre à jour metadonneesCD.CoverArtUrl ici - seulement au moment de l'extraction
                MettreAJourBoutonsNavigation()
                Return
            End If

            ' Sinon, chercher une nouvelle pochette alternative
            If metadonneesCD IsNot Nothing AndAlso
               Not String.IsNullOrWhiteSpace(metadonneesCD.Artist) AndAlso
               Not String.IsNullOrWhiteSpace(metadonneesCD.Album) Then

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche nouvelle pochette alternative...")
                Dim resultat = Await RechercherPochetteSuivante(metadonneesCD.Artist, metadonneesCD.Album)

                If Not String.IsNullOrWhiteSpace(resultat.url) Then
                    ' Ajouter à l'historique (supprimer tout ce qui suit l'index actuel)
                    If indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count - 1 Then
                        historiquePochettes.RemoveRange(indexPochetteActuelle + 1, historiquePochettes.Count - indexPochetteActuelle - 1)
                    End If

                    historiquePochettes.Add(resultat.url)
                    ' Enregistrer la source
                    If Not String.IsNullOrWhiteSpace(resultat.source) Then
                        sourcesPochettes(resultat.url) = resultat.source
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Source enregistrée: {resultat.source} pour {resultat.url}")
                    End If
                    indexPochetteActuelle = historiquePochettes.Count - 1
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] Nouvelle pochette trouvée: {resultat.url}")

                    ' Ne PAS ajouter à l'historique car déjà fait manuellement ci-dessus
                    Await TelechargerEtAfficherPochette(resultat.url, ajouterHistorique:=False)
                    ' NE PAS mettre à jour metadonneesCD.CoverArtUrl ici - seulement au moment de l'extraction
                    MettreAJourBoutonsNavigation()
                Else
                    MessageBox.Show(
                        LanguageManager.GetString("Compressor_NoMoreCovers"),
                        LanguageManager.GetString("Compressor_SearchTitle"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information)

                    ' Remettre le focus sur la PictureBox pour éviter que le bouton reste "enfoncé"
                    SafeSetPictureBoxImage(SafeGetPictureBoxBitmap())

                    ' Forcer le rafraîchissement des boutons
                    Button_Image_Suiv.Refresh()
                    Button_Image_Prec.Refresh()
                End If
            End If

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur Button_Image_Suiv: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Bouton Effacer - Supprimer l'image de la pochette
    ''' </summary>
    Private Sub Button_Image_Erase_Click(sender As Object, e As EventArgs) Handles Button_Image_Erase.Click
        Try
            ' Nettoyer l'image
            If PictureBoxPochette.Image IsNot Nothing Then
                SafeClearPictureBoxImage()
                System.Diagnostics.Debug.WriteLine($"[FormCompresser] ⚠️ IMAGE EFFACÉE par Button_Image_Erase")
            End If

            ' Effacer les infos
            Label_DimImage.Text = ""
            LabelTailleImage.Text = ""

            ' Réinitialiser l'historique et le cache
            historiquePochettes.Clear()
            indexPochetteActuelle = -1
            pochetteTempUrl = Nothing
            pochetteTempBytes = Nothing

            If metadonneesCD IsNot Nothing Then
                metadonneesCD.CoverArtUrl = Nothing
            End If

            MettreAJourBoutonsNavigation()
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Pochette effacée")

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur Button_Image_Erase: {ex.Message}")
        End Try
    End Sub

    ' Handler pour le menu contextuel sur la pochette
    Private Async Sub tsmiSearchCover_Click(sender As Object, e As EventArgs) Handles tsmiSearchCover.Click
        Try
            If String.IsNullOrWhiteSpace(TextBoxCDArtiste.Text) OrElse String.IsNullOrWhiteSpace(TextBoxCDTitre.Text) Then
                MessageBox.Show(LanguageManager.GetString("Compressor_EnterArtistAlbum"), LanguageManager.GetString("Compressor_SearchTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Mettre à jour metadonneesCD minimal
            If metadonneesCD Is Nothing Then metadonneesCD = New CDMetadataProvider.CDInfo()
            metadonneesCD.Artist = TextBoxCDArtiste.Text
            metadonneesCD.Album = TextBoxCDTitre.Text

            ' Lancer la recherche via la logique existante
            Cursor = Cursors.WaitCursor
            tsmiSearchCover.Enabled = False
            tsmiAddCoverFromFile.Enabled = False
            Try
                Await ChargerPochetteAlbum()
            Finally
                Cursor = Cursors.Default
                tsmiSearchCover.Enabled = True
                tsmiAddCoverFromFile.Enabled = True
            End Try
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur tsmiSearchCover_Click: {ex.Message}")
        End Try
    End Sub

    Private Sub tsmiAddCoverFromFile_Click(sender As Object, e As EventArgs) Handles tsmiAddCoverFromFile.Click
        Try
            Using ofd As New OpenFileDialog()
                ofd.Title = LanguageManager.GetString("Compressor_SelectCoverFile")
                ofd.Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif|Tous les fichiers|*.*"
                If ofd.ShowDialog() = DialogResult.OK Then
                    Dim filePath = ofd.FileName
                    Try
                        Dim bytes = System.IO.File.ReadAllBytes(filePath)
                        Using ms As New MemoryStream(bytes)
                            Using img = Image.FromStream(ms)
                                ' Afficher et ajouter à l'historique (par défaut en StretchImage)
                                SafeSetPictureBoxImage(New Bitmap(img), PictureBoxSizeMode.StretchImage)
                                pochetteTempBytes = bytes
                                pochetteTempUrl = "file://" & filePath
                                ' Ajouter à l'historique
                                AjouterAHistoriquePochettes(pochetteTempUrl)
                                sourcesPochettes(pochetteTempUrl) = "Local File"
                                MettreAJourBoutonsNavigation()
                            End Using
                        End Using
                    Catch exImg As Exception
                        MessageBox.Show(LanguageManager.GetString("Compressor_InvalidImage"), LanguageManager.GetString("Compressor_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error)
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur lecture image locale: {exImg.Message}")
                    End Try
                End If
            End Using
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur tsmiAddCoverFromFile_Click: {ex.Message}")
        End Try
    End Sub

    ' Handlers pour le sous-menu Affichage
    Private Sub tsmiSizeNormal_Click(sender As Object, e As EventArgs) Handles tsmiSizeNormal.Click
        Try
            PictureBoxPochette.SizeMode = PictureBoxSizeMode.Normal
        Catch ex As Exception
        End Try
    End Sub

    Private Sub tsmiSizeStretch_Click(sender As Object, e As EventArgs) Handles tsmiSizeStretch.Click
        Try
            PictureBoxPochette.SizeMode = PictureBoxSizeMode.StretchImage
        Catch ex As Exception
        End Try
    End Sub

    Private Sub tsmiSizeZoom_Click(sender As Object, e As EventArgs) Handles tsmiSizeZoom.Click
        Try
            PictureBoxPochette.SizeMode = PictureBoxSizeMode.Zoom
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>
    ''' Met à jour l'état des boutons de navigation
    ''' </summary>
    Private Sub MettreAJourBoutonsNavigation()
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] 🔄 MettreAJourBoutonsNavigation: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}, Image={If(PictureBoxPochette.Image Is Nothing, "NULL", "OK")}")

            If Button_Image_Prec IsNot Nothing Then
                Button_Image_Prec.Enabled = (indexPochetteActuelle > 0)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Button_Image_Prec.Enabled = {Button_Image_Prec.Enabled}")
            End If

            If Button_Image_Suiv IsNot Nothing Then
                ' Toujours actif pour chercher de nouvelles pochettes
                Button_Image_Suiv.Enabled = (metadonneesCD IsNot Nothing)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Button_Image_Suiv.Enabled = {Button_Image_Suiv.Enabled}")
            End If

            If Button_Image_Erase IsNot Nothing Then
                Button_Image_Erase.Enabled = (PictureBoxPochette.Image IsNot Nothing)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Button_Image_Erase.Enabled = {Button_Image_Erase.Enabled}")
            End If

            ' Mettre à jour le label de la source de l'image
            If Label_Image_Site IsNot Nothing Then
                If indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count Then
                    Dim urlActuelle = historiquePochettes(indexPochetteActuelle)
                    If sourcesPochettes.ContainsKey(urlActuelle) Then
                        Label_Image_Site.Text = sourcesPochettes(urlActuelle)
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Label_Image_Site.Text = '{sourcesPochettes(urlActuelle)}'")
                    Else
                        Label_Image_Site.Text = ""
                    End If
                Else
                    Label_Image_Site.Text = ""
                End If
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur MettreAJourBoutonsNavigation: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Recherche une pochette alternative suivante en cascade sur toutes les sources
    ''' </summary>
    Private Async Function RechercherPochetteSuivante(artiste As String, album As String) As Task(Of (url As String, source As String))
        Try
            ' Déterminer quelle source a été utilisée en dernier
            Dim derniereUrl = If(indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count,
                                 historiquePochettes(indexPochetteActuelle),
                                 "")

            ' Déterminer l'ordre des sources à essayer (toutes les 8 sources maintenant)
            Dim ordreRecherche As New List(Of String)

            If derniereUrl.Contains("itunes.apple.com") Then
                ordreRecherche.AddRange({"LastFm", "Deezer", "Discogs", "TheAudioDB", "FanartTV", "MusicBrainz", "iTunes"})
            ElseIf derniereUrl.Contains("last.fm") OrElse derniereUrl.Contains("lastfm") Then
                ordreRecherche.AddRange({"Deezer", "Discogs", "iTunes", "TheAudioDB", "FanartTV", "MusicBrainz", "LastFm"})
            ElseIf derniereUrl.Contains("deezer.com") Then
                ordreRecherche.AddRange({"Discogs", "iTunes", "LastFm", "TheAudioDB", "FanartTV", "MusicBrainz", "Deezer"})
            ElseIf derniereUrl.Contains("discogs.com") Then
                ordreRecherche.AddRange({"TheAudioDB", "FanartTV", "iTunes", "LastFm", "Deezer", "MusicBrainz", "Discogs"})
            ElseIf derniereUrl.Contains("theaudiodb.com") Then
                ordreRecherche.AddRange({"FanartTV", "iTunes", "LastFm", "Deezer", "Discogs", "MusicBrainz", "TheAudioDB"})
            ElseIf derniereUrl.Contains("fanart.tv") Then
                ordreRecherche.AddRange({"iTunes", "LastFm", "Deezer", "Discogs", "TheAudioDB", "MusicBrainz", "FanartTV"})
            ElseIf derniereUrl.Contains("coverartarchive.org") Then
                ordreRecherche.AddRange({"iTunes", "LastFm", "Deezer", "Discogs", "TheAudioDB", "FanartTV", "MusicBrainz"})
            Else
                ' Par défaut, essayer dans l'ordre de priorité
                ordreRecherche.AddRange({"iTunes", "LastFm", "Deezer", "Discogs", "TheAudioDB", "FanartTV", "MusicBrainz"})
            End If

            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Recherche en cascade sur: {String.Join(" → ", ordreRecherche)}")

            ' Essayer chaque source en cascade jusqu'à trouver une nouvelle pochette
            For Each source In ordreRecherche
                Dim nouvelleUrl As String = Nothing

                System.Diagnostics.Debug.WriteLine($"[FormCompresser] Essai source: {source}")

                Select Case source
                    Case "iTunes"
                        nouvelleUrl = Await RechercherPochetteiTunes(artiste, album)
                    Case "LastFm"
                        nouvelleUrl = Await RechercherPochetteLastFm(artiste, album)
                    Case "MusicBrainz"
                        nouvelleUrl = Await RechercherPochetteMusicBrainz(artiste, album)
                    Case "Deezer"
                        nouvelleUrl = Await RechercherPochetteDeezer(artiste, album)
                    Case "Discogs"
                        nouvelleUrl = Await RechercherPochetteDiscogs(artiste, album)
                    Case "TheAudioDB"
                        nouvelleUrl = Await RechercherPochetteTheAudioDB(artiste, album)
                    Case "FanartTV"
                        nouvelleUrl = Await RechercherPochetteFanartTV(artiste, album)
                End Select

                ' Vérifier si on a trouvé une URL et qu'elle n'est pas déjà dans l'historique
                If Not String.IsNullOrWhiteSpace(nouvelleUrl) Then
                    If Not historiquePochettes.Contains(nouvelleUrl) Then
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✓ Nouvelle pochette trouvée sur {source}: {nouvelleUrl}")
                        ' Mapper le nom de source
                        Dim nomSource = source
                        If source = "LastFm" Then nomSource = "Last.fm"
                        If source = "FanartTV" Then nomSource = "Fanart.tv"
                        Return (nouvelleUrl, nomSource)
                    Else
                        System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ URL déjà dans l'historique, continuer...")
                    End If
                Else
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser] ✗ Aucune pochette sur {source}, continuer...")
                End If
            Next

            ' Aucune nouvelle pochette trouvée sur aucune source
            System.Diagnostics.Debug.WriteLine("[FormCompresser] Aucune nouvelle pochette trouvée sur toutes les sources")
            Return (Nothing, Nothing)

        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur RechercherPochetteSuivante: {ex.Message}")
            Return (Nothing, Nothing)
        End Try
    End Function

    ''' <summary>
    ''' Ajouter une URL à l'historique lors du chargement d'une pochette
    ''' </summary>
    Private Sub AjouterAHistoriquePochettes(url As String)
        Try
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] 📝 AjouterAHistoriquePochettes: '{url}'")
            System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Avant: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")

            If String.IsNullOrWhiteSpace(url) Then
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    ⚠️ URL vide, abandon")
                Return
            End If

            ' Si pas encore dans l'historique ou c'est une nouvelle image
            If Not historiquePochettes.Contains(url) Then
                ' Supprimer tout ce qui suit l'index actuel
                If indexPochetteActuelle >= 0 AndAlso indexPochetteActuelle < historiquePochettes.Count - 1 Then
                    Dim nbSupprimes = historiquePochettes.Count - indexPochetteActuelle - 1
                    historiquePochettes.RemoveRange(indexPochetteActuelle + 1, nbSupprimes)
                    System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Supprimé {nbSupprimes} élément(s) après l'index actuel")
                End If

                historiquePochettes.Add(url)
                indexPochetteActuelle = historiquePochettes.Count - 1
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    ✅ Ajouté à l'historique")
            Else
                ' Mettre à jour l'index si l'URL existe déjà
                indexPochetteActuelle = historiquePochettes.IndexOf(url)
                System.Diagnostics.Debug.WriteLine($"[FormCompresser]    ℹ️ URL existante, index mis à jour")
            End If

            System.Diagnostics.Debug.WriteLine($"[FormCompresser]    Après: index={indexPochetteActuelle}, historique.Count={historiquePochettes.Count}")
            MettreAJourBoutonsNavigation()
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"[FormCompresser] Erreur AjouterAHistoriquePochettes: {ex.Message}")
        End Try
    End Sub

End Class
