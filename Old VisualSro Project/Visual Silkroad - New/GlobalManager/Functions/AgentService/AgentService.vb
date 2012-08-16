﻿Imports SRFramework

Namespace Agent
    Module AgentService

        Public Sub OnSendUserAuth(ByVal packet As PacketReader, ByVal Index_ As Integer)
            Dim tmp As New _UserAuth
            tmp.UserIndex = packet.DWord
            tmp.GameServerId = packet.Word
            tmp.UserName = packet.String(packet.Word)
            tmp.UserPw = packet.String(packet.Word)
            tmp.SessionId = Id_Gen.GetSessionId
            tmp.ExpireTime = Date.Now.AddSeconds(25)

            'Checks now 
            Dim writer As New PacketWriter
            writer.Create(InternalServerOpcodes.GATEWAY_SEND_USERAUTH)
            writer.DWord(tmp.UserIndex)

            If Shard.Server_Game.ContainsKey(tmp.GameServerId) Then
                If Shard.Server_Game(tmp.GameServerId).State = GameServer._ServerState.Online Then
                    writer.Byte(1)
                    writer.DWord(tmp.SessionId)
                    UserAuthCache.Add(tmp.SessionId, tmp)
                Else
                    writer.Byte(2)
                    writer.Byte(2)
                End If
            Else
                writer.Byte(2)
                writer.Byte(1)
            End If

            Server.Send(writer.GetBytes, Index_)
        End Sub

        Public Sub OnCheckUserAuth(ByVal packet As PacketReader, ByVal Index_ As Integer)
            Dim tmp As New _UserAuth
            tmp.UserIndex = packet.DWord
            tmp.SessionId = packet.DWord
            tmp.UserName = packet.String(packet.Word)
            tmp.UserPw = packet.String(packet.Word)

            Dim writer As New PacketWriter
            writer.Create(InternalServerOpcodes.GAMESERVER_CHECK_USERAUTH)
            writer.DWord(tmp.UserIndex)

            If UserAuthCache.ContainsKey(tmp.SessionId) Then
                If UserAuthCache(tmp.SessionId).UserName = tmp.UserName And UserAuthCache(tmp.SessionId).UserPw = tmp.UserPw Then
                    writer.Byte(1)
                Else
                    writer.Byte(2) 'Fail
                    writer.Byte(2) 'Wrong Id/Pw
                End If
            Else
                writer.Byte(2) 'Fail
                writer.Byte(1) 'Wrong SessionId
            End If

            Server.Send(writer.GetBytes, Index_)

        End Sub

    End Module
End Namespace
