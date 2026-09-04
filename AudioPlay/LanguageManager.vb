Imports System.Globalization
Imports System.Resources
Imports System.Threading

''' <summary>
''' Gestionnaire de langue pour l'application AudioPlay.
''' Permet de changer la langue de l'interface utilisateur dynamiquement.
''' </summary>
Public Class LanguageManager
    Private Shared _resourceManager As ResourceManager
    Private Shared _currentCulture As CultureInfo
    Public Shared Event LanguageChanged(ByVal newCulture As CultureInfo)

    ''' <summary>
    ''' Initialise le gestionnaire de ressources
    ''' </summary>
    Shared Sub New()
        ' Initialiser le ResourceManager avec le fichier Resources
        _resourceManager = New ResourceManager("AudioPlay.Resources", GetType(LanguageManager).Assembly)
        _currentCulture = Thread.CurrentThread.CurrentUICulture
    End Sub

    ''' <summary>
    ''' Obtient ou définit la culture actuelle de l'application
    ''' </summary>
    Public Shared Property CurrentCulture As CultureInfo
        Get
            Return _currentCulture
        End Get
        Set(value As CultureInfo)
            If value IsNot Nothing Then
                _currentCulture = value
                Thread.CurrentThread.CurrentUICulture = value
                Thread.CurrentThread.CurrentCulture = value
            End If
        End Set
    End Property

    ''' <summary>
    ''' Obtient une chaîne de ressource traduite selon la culture actuelle
    ''' </summary>
    ''' <param name="key">Clé de la ressource</param>
    ''' <returns>Chaîne traduite, ou la clé elle-même si non trouvée</returns>
    Public Shared Function GetString(key As String) As String
        Try
            Dim value = _resourceManager.GetString(key, _currentCulture)
            If String.IsNullOrEmpty(value) Then
                ' Fallback : essayer la ressource française
                value = _resourceManager.GetString(key, New CultureInfo("fr"))
            End If
            If String.IsNullOrEmpty(value) Then
                ' Fallbacks codés en dur pour l'aide pré-roll
                If key = "FormCompresser_PreRoll_Help_Title" Then
                    Return "Aide : Ajustement Position Début"
                End If
                If key = "FormCompresser_PreRoll_Help_Body" Then
                    Return "Ce réglage permet de reculer le point de départ de la première piste d'un nombre de secondes spécifié (pré-roll) pour éviter que la première note soit coupée trop tôt. Valeurs autorisées : 0.5s à 4.0s."
                End If
                System.Diagnostics.Debug.WriteLine($"Ressource non trouvée : {key}")
                Return "[RESX introuvable] " & key
            End If
            Return value
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération de la ressource {key} : {ex.Message}")
            Return key
        End Try
    End Function

    ''' <summary>
    ''' Obtient une chaîne de ressource formatée avec des paramètres
    ''' </summary>
    ''' <param name="key">Clé de la ressource</param>
    ''' <param name="args">Arguments pour le formatage</param>
    ''' <returns>Chaîne traduite et formatée</returns>
    Public Shared Function GetString(key As String, ParamArray args As Object()) As String
        Try
            Dim format = GetString(key)
            Return String.Format(format, args)
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lors du formatage de la ressource {key} : {ex.Message}")
            Return key
        End Try
    End Function

    ''' <summary>
    ''' Change la langue de l'application
    ''' </summary>
    ''' <param name="cultureCode">Code de culture (ex: "fr", "en", "fr-FR", "en-US")</param>
    Public Shared Sub ChangeLanguage(cultureCode As String)
        Try
            Dim newCulture As New CultureInfo(cultureCode)
            CurrentCulture = newCulture
            System.Diagnostics.Debug.WriteLine($"Langue changée vers : {newCulture.DisplayName}")
            Try
                RaiseEvent LanguageChanged(newCulture)
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Erreur lors du déclenchement de LanguageChanged: {ex.Message}")
            End Try
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lors du changement de langue vers {cultureCode} : {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Obtient le code de culture actuel (ex: "fr", "en")
    ''' </summary>
    Public Shared Function GetCurrentLanguageCode() As String
        Return _currentCulture.TwoLetterISOLanguageName
    End Function

    ''' <summary>
    ''' Obtient le nom d'affichage de la langue actuelle
    ''' </summary>
    Public Shared Function GetCurrentLanguageName() As String
        Return _currentCulture.DisplayName
    End Function

    ''' <summary>
    ''' Obtient la liste des langues disponibles
    ''' </summary>
    Public Shared Function GetAvailableLanguages() As Dictionary(Of String, String)
        ' Retourner les langues supportées (code -> nom)
        Return New Dictionary(Of String, String) From {
            {"fr", "Français"},
            {"en", "English"},
            {"es", "Español"},
            {"de", "Deutsch"},
            {"it", "Italiano"}
        }
    End Function

    ''' <summary>
    ''' Vérifie si une langue est disponible
    ''' </summary>
    Public Shared Function IsLanguageAvailable(cultureCode As String) As Boolean
        Return GetAvailableLanguages().ContainsKey(cultureCode)
    End Function

    ''' <summary>
    ''' Détecte et applique la langue système si elle est disponible
    ''' </summary>
    ''' <returns>Le code de langue détecté ou "fr" par défaut</returns>
    Public Shared Function DetectSystemLanguage() As String
        Try
            ' Obtenir la langue du système Windows
            Dim systemCulture = CultureInfo.InstalledUICulture
            Dim languageCode = systemCulture.TwoLetterISOLanguageName.ToLower()

            System.Diagnostics.Debug.WriteLine($"Langue système détectée : {languageCode} ({systemCulture.DisplayName})")

            ' Vérifier si la langue est disponible dans l'application
            If IsLanguageAvailable(languageCode) Then
                System.Diagnostics.Debug.WriteLine($"Langue {languageCode} disponible, application en cours...")
                ChangeLanguage(languageCode)
                Return languageCode
            Else
                System.Diagnostics.Debug.WriteLine($"Langue {languageCode} non disponible, utilisation du français par défaut")
                ChangeLanguage("fr")
                Return "fr"
            End If
        Catch ex As Exception
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la détection de la langue système : {ex.Message}")
            ChangeLanguage("fr")
            Return "fr"
        End Try
    End Function
End Class
