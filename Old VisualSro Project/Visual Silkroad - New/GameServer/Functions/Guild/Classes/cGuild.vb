﻿Namespace Functions
    Public Class cGuild
        Public GuildID As UInteger
        Public Name As String
        Public Points As UInteger
        Public Level As Byte

        Public NoticeTitle As String
        Public Notice As String

        Public Member As New List(Of GuildMember)

        Structure GuildMember
            Public CharacterID As UInteger
            Public GuildID As Long
            Public DonantedGP As UInteger
            Public GrantName As String
            Public Rights As GuildRights
        End Structure

        Structure GuildRights
            Public Master As Boolean
            Public Invite As Boolean
            Public Kick As Boolean
            Public Notice As Boolean
            Public Union As Boolean
            Public Storage As Boolean
        End Structure
    End Class
End Namespace