﻿Public Class cMonster
    Public Pk2ID As UInt32
    Public UniqueID As UInt32
    Public SpotID As Long

    Public Mob_Type As Byte
    Public Angle As UInt16 = 0
    Public HP_Cur As UInteger
    Public HP_Max As UInteger

    Public Position As Position
    Public Position_FromPos As Position
    Public Position_ToPos As Position
    Public Position_Spawn As Position

    Public WalkStart As New Date
    Public WalkEnd As New Date
    Public Walking As Boolean = False

    Public Death As Boolean = False
    Public DeathRemoveTime As Date

    Public DamageFromPlayer As New List(Of cDamageDone)

    Public IsAttacking As Boolean = False
    Public AttackingId As UInteger
    Public UsingSkillId As UInteger
    Public LastSkillId As UInteger

    Function GetsAttacked() As Boolean
        For i = 0 To Me.DamageFromPlayer.Count - 1
            If Me.DamageFromPlayer(i).Attacking = True Then
                Return True
            End If
        Next
        Return False
    End Function

End Class

''' <summary>
''' Is for the Damage done from a Player to a Monster.
''' </summary>
''' <remarks></remarks>
Public Class cDamageDone
    Public PlayerIndex As Integer
    Public Damage As ULong
    Public Attacking As Boolean = False
End Class
