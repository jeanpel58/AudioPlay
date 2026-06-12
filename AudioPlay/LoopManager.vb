''' <summary>
''' Gestionnaire de boucles audio pour un deck
''' </summary>
Public Class LoopManager
    Private m_loopStart As TimeSpan = TimeSpan.Zero
    Private m_loopEnd As TimeSpan = TimeSpan.Zero
    Private m_isLoopActive As Boolean = False
    Private m_isLoopSet As Boolean = False

    ''' <summary>
    ''' Début de la boucle
    ''' </summary>
    Public Property LoopStart As TimeSpan
        Get
            Return m_loopStart
        End Get
        Set(value As TimeSpan)
            m_loopStart = value
            If m_loopStart > m_loopEnd Then
                m_loopEnd = m_loopStart
            End If
        End Set
    End Property

    ''' <summary>
    ''' Fin de la boucle
    ''' </summary>
    Public Property LoopEnd As TimeSpan
        Get
            Return m_loopEnd
        End Get
        Set(value As TimeSpan)
            m_loopEnd = value
            If m_loopEnd < m_loopStart Then
                m_loopStart = m_loopEnd
            End If
        End Set
    End Property

    ''' <summary>
    ''' Indique si la boucle est active
    ''' </summary>
    Public Property IsLoopActive As Boolean
        Get
            Return m_isLoopActive
        End Get
        Set(value As Boolean)
            m_isLoopActive = value
        End Set
    End Property

    ''' <summary>
    ''' Indique si une boucle est définie
    ''' </summary>
    Public ReadOnly Property IsLoopSet As Boolean
        Get
            Return m_isLoopSet
        End Get
    End Property

    ''' <summary>
    ''' Définit une boucle automatique (2, 4, 8, 16 beats)
    ''' </summary>
    Public Sub SetAutoLoop(currentPosition As TimeSpan, bpm As Single, beats As Integer)
        If bpm <= 0 Then Return

        m_loopStart = currentPosition
        Dim beatDuration As Double = 60.0 / bpm ' Durée d'un beat en secondes
        Dim loopDuration As Double = beatDuration * beats
        m_loopEnd = currentPosition.Add(TimeSpan.FromSeconds(loopDuration))
        m_isLoopSet = True
        m_isLoopActive = True
    End Sub

    ''' <summary>
    ''' Définit le début de la boucle manuellement
    ''' </summary>
    Public Sub SetLoopIn(position As TimeSpan)
        m_loopStart = position
        If Not m_isLoopSet Then
            m_loopEnd = m_loopStart.Add(TimeSpan.FromSeconds(4)) ' 4 secondes par défaut
        End If
        m_isLoopSet = True
    End Sub

    ''' <summary>
    ''' Définit la fin de la boucle manuellement
    ''' </summary>
    Public Sub SetLoopOut(position As TimeSpan)
        If position > m_loopStart Then
            m_loopEnd = position
            m_isLoopSet = True
        End If
    End Sub

    ''' <summary>
    ''' Active/désactive la boucle
    ''' </summary>
    Public Sub ToggleLoop()
        If m_isLoopSet Then
            m_isLoopActive = Not m_isLoopActive
        End If
    End Sub

    ''' <summary>
    ''' Vérifie si la position actuelle doit revenir au début de la boucle
    ''' </summary>
    Public Function ShouldLoop(currentPosition As TimeSpan, ByRef newPosition As TimeSpan) As Boolean
        If m_isLoopActive AndAlso m_isLoopSet Then
            If currentPosition >= m_loopEnd Then
                newPosition = m_loopStart
                Return True
            End If
        End If
        Return False
    End Function

    ''' <summary>
    ''' Efface la boucle actuelle
    ''' </summary>
    Public Sub ClearLoop()
        m_loopStart = TimeSpan.Zero
        m_loopEnd = TimeSpan.Zero
        m_isLoopActive = False
        m_isLoopSet = False
    End Sub

    Public Overrides Function ToString() As String
        If m_isLoopSet Then
            Return $"Loop: {m_loopStart:mm\:ss} - {m_loopEnd:mm\:ss} ({If(m_isLoopActive, "Active", "Inactive")})"
        Else
            Return "Aucune boucle définie"
        End If
    End Function
End Class
