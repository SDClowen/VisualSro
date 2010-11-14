﻿Namespace GameServer.Functions
    Module MonsterSpawn

        Public MobList As New List(Of cMonster)

        Public Function CreateMonsterSpawnPacket(ByVal mob_index As Integer) As Byte()
            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.SingleSpawn)
            writer.DWord(MobList(mob_index).Pk2ID)
            writer.DWord(MobList(mob_index).UniqueID)
            writer.Byte(MobList(mob_index).Position.XSector)
            writer.Byte(MobList(mob_index).Position.YSector)
            writer.Float(MobList(mob_index).Position.X)
            writer.Float(MobList(mob_index).Position.Z)
            writer.Float(MobList(mob_index).Position.Y)

            writer.Word(MobList(mob_index).Angle)
            writer.Byte(0) 'dest
            writer.Byte(1) 'walk run flag
            writer.Byte(0) 'dest
            writer.Word(MobList(mob_index).Angle)
            writer.Byte(0) 'unknown
            writer.Byte(0) 'death flag
            writer.Byte(0) 'berserker

            writer.Float(20) 'walkspeed
            writer.Float(50) 'runspeed
            writer.Float(100) 'berserkerspeed

            writer.Word(0) 'unknwown  
            writer.Byte(MobList(mob_index).Mob_Type)
            writer.Byte(0) 'mhm

            Return writer.GetBytes
        End Function

        Public Sub SpawnMob(ByVal MobID As UInteger, ByVal Type As Byte, ByVal Position As Position)
            Dim mob_ As Object_ = GetObjectById(MobID)
            Dim toadd As New cMonster
            toadd.UniqueID = DatabaseCore.GetUnqiueID
            toadd.Pk2ID = mob_.Id
            toadd.Position = Position
            toadd.Mob_Type = Type
            toadd.HP_Cur = mob_.Hp
            MobList.Add(toadd)

            If Type = 3 Then
                SendUniqueSpawn(MobID)
            End If

            Dim MyIndex As UInteger = MobList.IndexOf(toadd)
            Dim range As Integer = ServerRange

            For refindex As Integer = 0 To Server.MaxClients
                Dim socket As Net.Sockets.Socket = ClientList.GetSocket(refindex)
                Dim player As [cChar] = PlayerData(refindex) 'Check if Player is ingame
                If (socket IsNot Nothing) AndAlso (player IsNot Nothing) AndAlso socket.Connected Then
                    If CheckRange(player.Position, Position) < range Then
                        If PlayerData(refindex).SpawnedNPCs.Contains(MyIndex) = False Then
                            Server.Send(CreateMonsterSpawnPacket(MyIndex), refindex)
                            PlayerData(refindex).SpawnedMonsters.Add(MyIndex)
                        End If
                    End If
                End If
            Next refindex
        End Sub

        Public Sub SendUniqueSpawn(ByVal PK2ID As UInteger)
            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.UniqueAnnonce)
            writer.Byte(5) 'Spawn
            writer.DWord(PK2ID)
            Server.SendToAllIngame(writer.GetBytes)
        End Sub




    End Module
End Namespace
