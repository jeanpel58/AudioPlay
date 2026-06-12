Imports System.IO
Imports System.Drawing
Imports System.Globalization

Public Class ThemeColors
    Public Property FormBackColor As Color
    Public Property ControlBackColor As Color
    Public Property ControlForeColor As Color
    Public Property ButtonBackColor As Color
    Public Property ButtonForeColor As Color
    Public Property ListViewBackColor As Color
    Public Property ListViewForeColor As Color
    Public Property ListViewHeaderBackColor As Color
    Public Property ListViewHeaderForeColor As Color
    Public Property ListViewSelectionBackColor As Color
    Public Property ListViewSelectionForeColor As Color
    Public Property TextBoxBackColor As Color
    Public Property TextBoxForeColor As Color
    Public Property GroupBoxForeColor As Color
    Public Property GroupBoxBorderColor As Color
    Public Property TrackBarBackColor As Color
End Class

Public Class ThemeManager
    Private Shared _currentTheme As ThemeColors = Nothing
    Private Shared _currentThemeName As String = "Par défaut"

    Private Shared ReadOnly ThemesFolderPath As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioPlay",
        "Themes")

    Private Shared ReadOnly CurrentThemeFilePath As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioPlay",
        "current_theme.txt")

    ' Ancien chemin pour compatibilité (sera migré)
    Private Shared ReadOnly LegacyThemeFilePath As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "AudioPlay",
        "theme.txt")

    Public Shared Function GetDefaultTheme() As ThemeColors
        Return New ThemeColors With {
            .FormBackColor = Color.LightBlue,
            .ControlBackColor = Color.LightBlue,
            .ControlForeColor = SystemColors.ControlText,
            .ButtonBackColor = Color.LightBlue,
            .ButtonForeColor = SystemColors.ControlText,
            .ListViewBackColor = Color.LightCyan,
            .ListViewForeColor = SystemColors.ControlText,
            .ListViewHeaderBackColor = Color.FromArgb(7, 192, 254),
            .ListViewHeaderForeColor = Color.White,
            .ListViewSelectionBackColor = SystemColors.Highlight,
            .ListViewSelectionForeColor = SystemColors.HighlightText,
            .TextBoxBackColor = Color.LightCyan,
            .TextBoxForeColor = SystemColors.ControlText,
            .GroupBoxForeColor = SystemColors.ControlText,
            .GroupBoxBorderColor = Color.FromArgb(7, 192, 254),
            .TrackBarBackColor = Color.LightCyan
        }
    End Function

    Public Shared Function GetCurrentThemeName() As String
        Return _currentThemeName
    End Function

    Public Shared Function GetAvailableThemes() As List(Of String)
        Dim themes As New List(Of String)()

        Try
            If Not Directory.Exists(ThemesFolderPath) Then
                Directory.CreateDirectory(ThemesFolderPath)
            End If

            ' Copier les thèmes préinstallés lors de la première exécution
            CopyPreinstalledThemes()

            For Each file In Directory.GetFiles(ThemesFolderPath, "*.theme")
                Dim themeName = Path.GetFileNameWithoutExtension(file)
                themes.Add(themeName)
            Next

            ' S'assurer que "Par défaut" est toujours en premier
            If Not themes.Contains("Par défaut") Then
                themes.Insert(0, "Par défaut")
            Else
                themes.Remove("Par défaut")
                themes.Insert(0, "Par défaut")
            End If
        Catch
            ' En cas d'erreur, retourner au moins "Par défaut"
            If Not themes.Contains("Par défaut") Then
                themes.Add("Par défaut")
            End If
        End Try

        Return themes
    End Function

    Private Shared Sub CopyPreinstalledThemes()
        Try
            ' Dossier des thèmes préinstallés dans le répertoire de l'application
            Dim appThemesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Themes")

            If Not Directory.Exists(appThemesPath) Then
                Return
            End If

            ' Copier chaque thème préinstallé s'il n'existe pas déjà
            For Each sourceFile In Directory.GetFiles(appThemesPath, "*.theme")
                Dim fileName = Path.GetFileName(sourceFile)
                Dim destFile = Path.Combine(ThemesFolderPath, fileName)

                ' Ne copier que si le fichier n'existe pas déjà dans le dossier utilisateur
                If Not File.Exists(destFile) Then
                    File.Copy(sourceFile, destFile, False)
                End If
            Next
        Catch
            ' Ignorer les erreurs de copie
        End Try
    End Sub

    Public Shared Sub SaveNamedTheme(themeName As String, theme As ThemeColors)
        Try
            If Not Directory.Exists(ThemesFolderPath) Then
                Directory.CreateDirectory(ThemesFolderPath)
            End If

            Dim themeFilePath = Path.Combine(ThemesFolderPath, $"{themeName}.theme")

            Dim lines As New List(Of String) From {
                $"{NameOf(ThemeColors.FormBackColor)}={ColorToString(theme.FormBackColor)}",
                $"{NameOf(ThemeColors.ControlBackColor)}={ColorToString(theme.ControlBackColor)}",
                $"{NameOf(ThemeColors.ControlForeColor)}={ColorToString(theme.ControlForeColor)}",
                $"{NameOf(ThemeColors.ButtonBackColor)}={ColorToString(theme.ButtonBackColor)}",
                $"{NameOf(ThemeColors.ButtonForeColor)}={ColorToString(theme.ButtonForeColor)}",
                $"{NameOf(ThemeColors.ListViewBackColor)}={ColorToString(theme.ListViewBackColor)}",
                $"{NameOf(ThemeColors.ListViewForeColor)}={ColorToString(theme.ListViewForeColor)}",
                $"{NameOf(ThemeColors.ListViewHeaderBackColor)}={ColorToString(theme.ListViewHeaderBackColor)}",
                $"{NameOf(ThemeColors.ListViewHeaderForeColor)}={ColorToString(theme.ListViewHeaderForeColor)}",
                $"{NameOf(ThemeColors.ListViewSelectionBackColor)}={ColorToString(theme.ListViewSelectionBackColor)}",
                $"{NameOf(ThemeColors.ListViewSelectionForeColor)}={ColorToString(theme.ListViewSelectionForeColor)}",
                $"{NameOf(ThemeColors.TextBoxBackColor)}={ColorToString(theme.TextBoxBackColor)}",
                $"{NameOf(ThemeColors.TextBoxForeColor)}={ColorToString(theme.TextBoxForeColor)}",
                $"{NameOf(ThemeColors.GroupBoxForeColor)}={ColorToString(theme.GroupBoxForeColor)}",
                $"{NameOf(ThemeColors.GroupBoxBorderColor)}={ColorToString(theme.GroupBoxBorderColor)}",
                $"{NameOf(ThemeColors.TrackBarBackColor)}={ColorToString(theme.TrackBarBackColor)}"
            }

            File.WriteAllLines(themeFilePath, lines)
        Catch
            ' Ignorer erreurs de sauvegarde
        End Try
    End Sub

    Public Shared Function LoadNamedTheme(themeName As String) As ThemeColors
        Dim theme = GetDefaultTheme()

        If themeName = "Par défaut" Then
            Return theme
        End If

        Try
            Dim themeFilePath = Path.Combine(ThemesFolderPath, $"{themeName}.theme")

            If Not File.Exists(themeFilePath) Then
                Return theme
            End If

            For Each line In File.ReadAllLines(themeFilePath)
                If String.IsNullOrWhiteSpace(line) OrElse Not line.Contains("=") Then
                    Continue For
                End If

                Dim parts = line.Split("="c, 2)
                Dim key = parts(0).Trim()
                Dim value = parts(1).Trim()
                Dim color = ColorFromString(value)

                Select Case key
                    Case NameOf(ThemeColors.FormBackColor)
                        theme.FormBackColor = color
                    Case NameOf(ThemeColors.ControlBackColor)
                        theme.ControlBackColor = color
                    Case NameOf(ThemeColors.ControlForeColor)
                        theme.ControlForeColor = color
                    Case NameOf(ThemeColors.ButtonBackColor)
                        theme.ButtonBackColor = color
                    Case NameOf(ThemeColors.ButtonForeColor)
                        theme.ButtonForeColor = color
                    Case NameOf(ThemeColors.ListViewBackColor)
                        theme.ListViewBackColor = color
                    Case NameOf(ThemeColors.ListViewForeColor)
                        theme.ListViewForeColor = color
                    Case NameOf(ThemeColors.ListViewHeaderBackColor)
                        theme.ListViewHeaderBackColor = color
                    Case NameOf(ThemeColors.ListViewHeaderForeColor)
                        theme.ListViewHeaderForeColor = color
                    Case NameOf(ThemeColors.ListViewSelectionBackColor)
                        theme.ListViewSelectionBackColor = color
                    Case NameOf(ThemeColors.ListViewSelectionForeColor)
                        theme.ListViewSelectionForeColor = color
                    Case NameOf(ThemeColors.TextBoxBackColor)
                        theme.TextBoxBackColor = color
                    Case NameOf(ThemeColors.TextBoxForeColor)
                        theme.TextBoxForeColor = color
                    Case NameOf(ThemeColors.GroupBoxForeColor)
                        theme.GroupBoxForeColor = color
                    Case NameOf(ThemeColors.GroupBoxBorderColor)
                        theme.GroupBoxBorderColor = color
                    Case NameOf(ThemeColors.TrackBarBackColor)
                        theme.TrackBarBackColor = color
                End Select
            Next
        Catch
            ' Fallback silencieux sur thème par défaut
        End Try

        Return theme
    End Function

    Public Shared Sub SetCurrentTheme(themeName As String, theme As ThemeColors)
        _currentTheme = theme
        _currentThemeName = themeName

        Try
            If Not Directory.Exists(Path.GetDirectoryName(CurrentThemeFilePath)) Then
                Directory.CreateDirectory(Path.GetDirectoryName(CurrentThemeFilePath))
            End If
            File.WriteAllText(CurrentThemeFilePath, themeName)
        Catch
            ' Ignorer erreurs de sauvegarde
        End Try
    End Sub

    Public Shared Sub DeleteTheme(themeName As String)
        If themeName = "Par défaut" Then
            Return ' Ne pas supprimer le thème par défaut
        End If

        Try
            Dim themeFilePath = Path.Combine(ThemesFolderPath, $"{themeName}.theme")
            If File.Exists(themeFilePath) Then
                File.Delete(themeFilePath)
            End If
        Catch
            ' Ignorer erreurs de suppression
        End Try
    End Sub

    Public Shared Function GetCurrentTheme() As ThemeColors
        If _currentTheme Is Nothing Then
            ' Charger le nom du thème actuel
            Dim themeName As String = "Par défaut"
            Try
                If File.Exists(CurrentThemeFilePath) Then
                    themeName = File.ReadAllText(CurrentThemeFilePath).Trim()
                ElseIf File.Exists(LegacyThemeFilePath) Then
                    ' Migration depuis l'ancien système
                    themeName = "Par défaut"
                    SaveNamedTheme("Par défaut", GetDefaultTheme())
                    File.WriteAllText(CurrentThemeFilePath, "Par défaut")
                Else
                    ' Première utilisation
                    SaveNamedTheme("Par défaut", GetDefaultTheme())
                    File.WriteAllText(CurrentThemeFilePath, "Par défaut")
                End If
            Catch
                themeName = "Par défaut"
            End Try

            _currentThemeName = themeName
            _currentTheme = LoadNamedTheme(themeName)
        End If
        Return _currentTheme
    End Function

    Public Shared Sub ResetThemeToDefault()
        Dim defaults = GetDefaultTheme()
        SaveNamedTheme("Par défaut", defaults)
        SetCurrentTheme("Par défaut", defaults)
    End Sub

    Public Shared Sub ApplyThemeToForm(form As Form)
        Dim theme = GetCurrentTheme()
        ApplyThemeToForm(form, theme)
    End Sub

    ' Surcharge pour appliquer un thème spécifique (pour prévisualisation)
    Public Shared Sub ApplyThemeToForm(form As Form, theme As ThemeColors)
        form.BackColor = theme.FormBackColor
        form.ForeColor = theme.ControlForeColor

        For Each ctrl As Control In form.Controls
            ApplyThemeToControl(ctrl, theme)
        Next
    End Sub

    Private Shared Sub ApplyThemeToControl(ctrl As Control, theme As ThemeColors)
        If TypeOf ctrl Is Button Then
            Dim btn As Button = CType(ctrl, Button)
            ' Ne pas changer le fond des boutons avec images (garder transparent)
            ' Boutons concernés : Button_Precedent, Button_Suivant, Button_Jouer, Button_PauseReprise,
            ' Button_Arreter, Button_Mute, Button_CalculBPM, Button_Aleatoire, Button_Power,
            ' Button_Ajout, Button_InfoSelect, Button_Playlist, Button_Parametres, Button_Loop,
            ' Button_AudioPlay_Aide, Button_APropos
            If btn.BackgroundImage Is Nothing Then
                ctrl.BackColor = theme.ButtonBackColor
                ctrl.ForeColor = theme.ButtonForeColor
            Else
                ' Boutons avec image : fond transparent
                ctrl.BackColor = Color.Transparent
                ctrl.ForeColor = theme.ButtonForeColor
            End If

        ElseIf TypeOf ctrl Is TextBox Then
            ctrl.BackColor = theme.TextBoxBackColor
            ctrl.ForeColor = theme.TextBoxForeColor

        ElseIf TypeOf ctrl Is GroupBox Then
            Dim groupBox As GroupBox = CType(ctrl, GroupBox)
            ctrl.BackColor = theme.ControlBackColor
            ctrl.ForeColor = theme.GroupBoxForeColor

            ' Activer le dessin personnalisé pour la bordure
            ' Retirer les anciens gestionnaires s'ils existent
            RemoveHandler groupBox.Paint, AddressOf GroupBox_Paint

            ' Ajouter le gestionnaire de dessin personnalisé
            AddHandler groupBox.Paint, AddressOf GroupBox_Paint

            ' Stocker la couleur de bordure dans le Tag (TOUJOURS mettre à jour)
            groupBox.Tag = theme.GroupBoxBorderColor

            groupBox.Invalidate()

        ElseIf TypeOf ctrl Is ListView Then
            ctrl.BackColor = theme.ListViewBackColor
            ctrl.ForeColor = theme.ListViewForeColor
            ' Forcer le redessin pour les ListView en mode OwnerDraw
            ctrl.Invalidate()

        ElseIf TypeOf ctrl Is TrackBar Then
            ctrl.BackColor = theme.TrackBarBackColor
            ctrl.ForeColor = theme.ControlForeColor

        ElseIf TypeOf ctrl Is CheckBox OrElse TypeOf ctrl Is RadioButton OrElse TypeOf ctrl Is Label Then
            ctrl.BackColor = theme.ControlBackColor
            ctrl.ForeColor = theme.ControlForeColor

        ElseIf TypeOf ctrl Is ComboBox Then
            Dim combo As ComboBox = CType(ctrl, ComboBox)
            ctrl.BackColor = theme.TextBoxBackColor
            ctrl.ForeColor = theme.TextBoxForeColor

            ' Activer le dessin personnalisé pour appliquer les couleurs
            If combo.DrawMode = DrawMode.Normal Then
                combo.DrawMode = DrawMode.OwnerDrawFixed

                ' Retirer les anciens gestionnaires s'ils existent
                RemoveHandler combo.DrawItem, AddressOf ComboBox_DrawItem

                ' Ajouter le gestionnaire de dessin personnalisé
                AddHandler combo.DrawItem, AddressOf ComboBox_DrawItem
            End If

        Else
            ctrl.BackColor = theme.ControlBackColor
            ctrl.ForeColor = theme.ControlForeColor
        End If

        For Each child As Control In ctrl.Controls
            ApplyThemeToControl(child, theme)
        Next
    End Sub

    Private Shared Function ColorToString(color As Color) As String
        ' Sauvegarder au format hexadécimal #RRGGBB
        Return $"#{color.R:X2}{color.G:X2}{color.B:X2}"
    End Function

    Private Shared Function ColorFromString(value As String) As Color
        ' Gérer le format hexadécimal #RRGGBB
        If value.StartsWith("#") AndAlso value.Length = 7 Then
            Try
                Dim r = Convert.ToInt32(value.Substring(1, 2), 16)
                Dim g = Convert.ToInt32(value.Substring(3, 2), 16)
                Dim b = Convert.ToInt32(value.Substring(5, 2), 16)
                Return Color.FromArgb(r, g, b)
            Catch
                ' Si la conversion échoue, continuer
            End Try
        End If

        ' Gérer l'ancien format ARGB (pour compatibilité)
        Dim argb As Integer
        If Integer.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, argb) Then
            Return Color.FromArgb(argb)
        End If

        Return SystemColors.Control
    End Function

    ' Gestionnaire de dessin personnalisé pour les ComboBox
    Private Shared Sub ComboBox_DrawItem(sender As Object, e As DrawItemEventArgs)
        If e.Index < 0 Then Return

        Dim combo As ComboBox = CType(sender, ComboBox)
        Dim theme = GetCurrentTheme()

        ' Dessiner le fond
        e.DrawBackground()

        ' Déterminer les couleurs en fonction de l'état
        Dim backColor As Color
        Dim foreColor As Color

        If (e.State And DrawItemState.Selected) = DrawItemState.Selected Then
            ' Item sélectionné : utiliser les couleurs de sélection
            backColor = theme.ListViewSelectionBackColor
            foreColor = theme.ListViewSelectionForeColor
        Else
            ' Item normal : utiliser les couleurs du TextBox
            backColor = theme.TextBoxBackColor
            foreColor = theme.TextBoxForeColor
        End If

        ' Dessiner le fond coloré
        Using brush As New SolidBrush(backColor)
            e.Graphics.FillRectangle(brush, e.Bounds)
        End Using

        ' Dessiner le texte
        If combo.Items.Count > e.Index Then
            Dim text As String = combo.Items(e.Index).ToString()
            Using textBrush As New SolidBrush(foreColor)
                e.Graphics.DrawString(text, e.Font, textBrush, e.Bounds.X + 2, e.Bounds.Y + 2)
            End Using
        End If

        ' Dessiner le rectangle de focus si nécessaire
        e.DrawFocusRectangle()
    End Sub

    ' Gestionnaire de dessin personnalisé pour les GroupBox
    Private Shared Sub GroupBox_Paint(sender As Object, e As PaintEventArgs)
        Dim groupBox As GroupBox = CType(sender, GroupBox)

        ' Récupérer la couleur de bordure depuis le Tag
        Dim borderColor As Color = If(TypeOf groupBox.Tag Is Color, CType(groupBox.Tag, Color), GetCurrentTheme().GroupBoxBorderColor)

        ' Effacer le fond
        e.Graphics.Clear(groupBox.BackColor)

        ' Position de départ du rectangle
        Dim borderTop As Integer = 0
        Dim textWidth As Integer = 0
        Dim hasText As Boolean = Not String.IsNullOrEmpty(groupBox.Text)

        If hasText Then
            ' Mesurer la taille du texte
            Dim textSize As SizeF = e.Graphics.MeasureString(groupBox.Text, groupBox.Font)
            textWidth = CInt(textSize.Width)
            Dim textHeight As Integer = CInt(textSize.Height)
            borderTop = textHeight \ 2
        End If

        ' Dessiner la bordure
        Using pen As New Pen(borderColor, 1)
            If hasText Then
                ' Si le GroupBox a du texte : dessiner en 2 segments (avant et après le texte)
                ' Ligne du haut - gauche (avant le texte)
                e.Graphics.DrawLine(pen, 0, borderTop, 8, borderTop)
                ' Ligne du haut - droite (après le texte)
                e.Graphics.DrawLine(pen, 8 + textWidth + 4, borderTop, groupBox.Width - 1, borderTop)
            Else
                ' Si pas de texte : dessiner une ligne continue en haut
                e.Graphics.DrawLine(pen, 0, borderTop, groupBox.Width - 1, borderTop)
            End If

            ' Ligne de droite
            e.Graphics.DrawLine(pen, groupBox.Width - 1, borderTop, groupBox.Width - 1, groupBox.Height - 1)

            ' Ligne du bas
            e.Graphics.DrawLine(pen, 0, groupBox.Height - 1, groupBox.Width - 1, groupBox.Height - 1)

            ' Ligne de gauche
            e.Graphics.DrawLine(pen, 0, borderTop, 0, groupBox.Height - 1)
        End Using

        ' Dessiner le texte (seulement s'il existe)
        If hasText Then
            Using textBrush As New SolidBrush(groupBox.ForeColor)
                e.Graphics.DrawString(groupBox.Text, groupBox.Font, textBrush, 10, 0)
            End Using
        End If
    End Sub
End Class
