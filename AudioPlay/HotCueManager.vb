Imports System.Drawing

''' <summary>
''' Représente un point de repère (hotcue) avec nom et couleur
''' </summary>
Public Class HotCue
    Public Property Position As TimeSpan
    Public Property Name As String
    Public Property Color As Color
    Public Property Index As Integer ' 1-8

    Public Sub New(index As Integer, position As TimeSpan)
        Me.Index = index
        Me.Position = position
        Me.Name = $"Cue {index}"

        ' Couleurs par défaut selon l'index
        Select Case index
            Case 1
                Me.Color = Color.Red
            Case 2
                Me.Color = Color.Orange
            Case 3
                Me.Color = Color.Yellow
            Case 4
                Me.Color = Color.Lime
            Case 5
                Me.Color = Color.Cyan
            Case 6
                Me.Color = Color.Blue
            Case 7
                Me.Color = Color.Magenta
            Case 8
                Me.Color = Color.Pink
            Case Else
                Me.Color = Color.White
        End Select
    End Sub

    Public Overrides Function ToString() As String
        Return $"{Name} - {Position:mm\:ss\.ff}"
    End Function
End Class

''' <summary>
''' Gestionnaire de hotcues pour un deck
''' </summary>
Public Class HotCueManager
    Private hotcues As New Dictionary(Of Integer, HotCue)()
    Private Const MAX_HOTCUES As Integer = 8

    ''' <summary>
    ''' Définit un hotcue à la position actuelle
    ''' </summary>
    Public Function SetHotCue(index As Integer, position As TimeSpan) As HotCue
        If index < 1 OrElse index > MAX_HOTCUES Then
            Throw New ArgumentOutOfRangeException("index", "L'index doit être entre 1 et 8")
        End If

        Dim hotcue As HotCue
        If hotcues.ContainsKey(index) Then
            hotcue = hotcues(index)
            hotcue.Position = position
        Else
            hotcue = New HotCue(index, position)
            hotcues.Add(index, hotcue)
        End If

        Return hotcue
    End Function

    ''' <summary>
    ''' Récupère un hotcue par son index
    ''' </summary>
    Public Function GetHotCue(index As Integer) As HotCue
        If hotcues.ContainsKey(index) Then
            Return hotcues(index)
        End If
        Return Nothing
    End Function

    ''' <summary>
    ''' Supprime un hotcue
    ''' </summary>
    Public Sub RemoveHotCue(index As Integer)
        If hotcues.ContainsKey(index) Then
            hotcues.Remove(index)
        End If
    End Sub

    ''' <summary>
    ''' Efface tous les hotcues
    ''' </summary>
    Public Sub ClearAll()
        hotcues.Clear()
    End Sub

    ''' <summary>
    ''' Retourne tous les hotcues définis
    ''' </summary>
    Public Function GetAllHotCues() As List(Of HotCue)
        Return hotcues.Values.ToList()
    End Function

    ''' <summary>
    ''' Vérifie si un hotcue existe
    ''' </summary>
    Public Function HasHotCue(index As Integer) As Boolean
        Return hotcues.ContainsKey(index)
    End Function
End Class
