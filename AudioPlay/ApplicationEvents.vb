Imports Microsoft.VisualBasic.ApplicationServices
Imports System.Windows.Forms
Imports System.IO

Namespace My
    ' The following events are available for MyApplication:
    ' Startup: Raised when the application starts, before the startup form is created.
    ' Shutdown: Raised after all application forms are closed. This event is not raised if the application terminates abnormally.
    ' UnhandledException: Raised if the application encounters an unhandled exception.
    ' StartupNextInstance: Raised when launching a single-instance application and the application is already active.
    ' NetworkAvailabilityChanged: Raised when the network connection is connected or disconnected.

    ' **NEW** ApplyApplicationDefaults: Raised when the application queries default values to be set for the application.

    ' Example:
    ' Private Sub MyApplication_ApplyApplicationDefaults(sender As Object, e As ApplyApplicationDefaultsEventArgs) Handles Me.ApplyApplicationDefaults
    '
    '   ' Setting the application-wide default Font:
    '   e.Font = New Font(FontFamily.GenericSansSerif, 12, FontStyle.Regular)
    '
    '   ' Setting the HighDpiMode for the Application:
    '   e.HighDpiMode = HighDpiMode.PerMonitorV2
    '
    '   ' If a splash dialog is used, this sets the minimum display time:
    '   e.MinimumSplashScreenDisplayTime = 4000
    ' End Sub

    Partial Friend Class MyApplication

        Private Sub MyApplication_Startup(sender As Object, e As Microsoft.VisualBasic.ApplicationServices.StartupEventArgs) Handles Me.Startup
            ' Handlers globaux pour capturer les exceptions non gérées
            Try
                AddHandler System.Windows.Forms.Application.ThreadException, AddressOf OnThreadException
            Catch
                ' Ignorer si non disponible
            End Try

            Try
                AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf OnDomainUnhandledException
            Catch
                ' Ignorer
            End Try
        End Sub

        Private Sub OnThreadException(sender As Object, e As System.Threading.ThreadExceptionEventArgs)
            Try
                Dim logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay")
                If Not Directory.Exists(logDir) Then Directory.CreateDirectory(logDir)
                Dim logFile = Path.Combine(logDir, "crash.log")
                File.AppendAllText(logFile, $"[{DateTime.Now}] ThreadException: {e.Exception.ToString()}{Environment.NewLine}")
            Catch
            End Try

            Try
                MessageBox.Show(AudioPlay.LanguageManager.GetString("UnhandledException_Message", e.Exception.Message, e.Exception.Source, e.Exception.StackTrace),
                                AudioPlay.LanguageManager.GetString("UnhandledException_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            Catch
                MessageBox.Show("An unhandled error occurred. See crash.log in %APPDATA%\\AudioPlay.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
        Private Sub OnDomainUnhandledException(sender As Object, e As System.UnhandledExceptionEventArgs)
            Try
                Dim exObj = TryCast(e.ExceptionObject, Exception)
                Dim logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AudioPlay")
                If Not Directory.Exists(logDir) Then Directory.CreateDirectory(logDir)
                Dim logFile = Path.Combine(logDir, "crash.log")
                If exObj IsNot Nothing Then
                    File.AppendAllText(logFile, $"[{DateTime.Now}] UnhandledException (Domain): {exObj.ToString()}{Environment.NewLine}")
                Else
                    File.AppendAllText(logFile, $"[{DateTime.Now}] UnhandledException (Domain): {e.ExceptionObject.ToString()}{Environment.NewLine}")
                End If
            Catch
            End Try
            ' Ne pas afficher de MessageBox ici car l'application peut être en train de se terminer
        End Sub

        Private Sub MyApplication_StartupNextInstance(sender As Object, e As StartupNextInstanceEventArgs) Handles Me.StartupNextInstance
            ' Ajoute chaque fichier passé en argument à la liste de l'instance principale
            For Each f As Form In System.Windows.Forms.Application.OpenForms
                If TypeOf f Is Form1 Then
                    Dim mainForm = DirectCast(f, Form1)
                    ' Restaurer et activer la fenêtre principale
                    If mainForm.WindowState = FormWindowState.Minimized Then
                        mainForm.WindowState = FormWindowState.Normal
                    End If
                    mainForm.Activate()
                    ' Ajouter les fichiers à la fin de la liste (dans l'ordre)
                    For i As Integer = e.CommandLine.Count - 1 To 0 Step -1
                        Dim arg = e.CommandLine(i)
                        If File.Exists(arg) Then
                            mainForm.AjouterFichierAListe(arg)
                        End If
                    Next
                    Exit For
                End If
            Next
        End Sub

        Private Sub MyApplication_UnhandledException(sender As Object, e As UnhandledExceptionEventArgs) Handles Me.UnhandledException
            Dim errorMessage As String = AudioPlay.LanguageManager.GetString("UnhandledException_Message",
                                                                   e.Exception.Message,
                                                                   e.Exception.Source,
                                                                   e.Exception.StackTrace)

            If e.Exception.InnerException IsNot Nothing Then
                errorMessage &= AudioPlay.LanguageManager.GetString("UnhandledException_Inner",
                                                         e.Exception.InnerException.Message,
                                                         e.Exception.InnerException.StackTrace)
            End If

            MessageBox.Show(errorMessage, AudioPlay.LanguageManager.GetString("UnhandledException_Title"), MessageBoxButtons.OK, MessageBoxIcon.Error)
            e.ExitApplication = False
        End Sub

    End Class
End Namespace
