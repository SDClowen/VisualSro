﻿Namespace GameServer.Functions
    Module Movement

        Public Sub OnPlayerMovement(ByVal Index_ As Integer, ByVal packet As PacketReader)

            If PlayerData(Index_).Busy = True Then
                Exit Sub
            End If

            Dim tag As Byte = packet.Byte
            If tag = 1 Then
                '01 A8 60 61 02 FE FF 70 04
                Dim to_pos As New Position
                to_pos.XSector = packet.Byte
                to_pos.YSector = packet.Byte
                to_pos.X = packet.WordInt
                Dim zByte As Byte() = packet.ByteArray(2) 'real z converting
                to_pos.Z = BitConverter.ToInt16(zByte, 0)
                to_pos.Y = packet.WordInt


                Dim x = PlayerData(Index_).Position.X - to_pos.X
                Dim y = PlayerData(Index_).Position.Y - to_pos.Y
                Dim Distance = Math.Sqrt(x * x + y * y)

                Dim Traveltime = Distance / PlayerData(Index_).RunSpeed
                Console.WriteLine(Distance & "    " & Traveltime)

                Debug.Print(to_pos.XSector & " -- " & to_pos.YSector & " -- " & to_pos.X & " -- " & to_pos.Z & " -- " & to_pos.Y)
                Debug.Print("X:" & ((to_pos.XSector - 135) * 192) + (to_pos.X / 10) & " Y:" & ((to_pos.YSector - 92) * 192) + (to_pos.Y / 10))


                Dim writer As New PacketWriter
                writer.Create(ServerOpcodes.Movement)
                writer.DWord(PlayerData(Index_).UniqueId)
                writer.Byte(1) 'destination
                writer.Byte(to_pos.XSector)
                writer.Byte(to_pos.YSector)
                writer.Word(CUInt(to_pos.X))
                writer.Byte(zByte)
                writer.Word(CUInt(to_pos.Y))
                writer.Byte(0) '1= source


                DataBase.SaveQuery(String.Format("UPDATE characters SET xsect='{0}', ysect='{1}', xpos='{2}', zpos='{3}', ypos='{4}' where id='{5}'", PlayerData(Index_).Position.XSector, PlayerData(Index_).Position.YSector, Math.Round(PlayerData(Index_).Position.X), Math.Round(PlayerData(Index_).Position.Z), Math.Round(PlayerData(Index_).Position.Y), PlayerData(Index_).UniqueId))
                PlayerData(Index_).Position = to_pos

                ObjectSpawnCheck(Index_)

                Server.SendToAllInRange(writer.GetBytes, Index_)
            End If
        End Sub

        Public Sub ObjectSpawnCheck(ByVal Index_ As Integer)
            Try
                Dim range As Integer = ServerRange
                ObjectDeSpawnCheck(Index_)

                '=============Players============
                For refindex As Integer = 0 To Server.MaxClients
                    If (ClientList.GetSocket(refindex) IsNot Nothing) AndAlso (PlayerData(refindex) IsNot Nothing) AndAlso ClientList.GetSocket(refindex).Connected AndAlso Index_ <> refindex Then
                        If CheckRange(PlayerData(Index_).Position, PlayerData(refindex).Position) Then
                            If PlayerData(refindex).SpawnedPlayers.Contains(Index_) = False Then
                                Server.Send(CreateSpawnPacket(Index_), refindex)
                                PlayerData(refindex).SpawnedPlayers.Add(Index_)
                            End If
                            If PlayerData(refindex).SpawnedPlayers.Contains(Index_) = False Then
                                Server.Send(CreateSpawnPacket(refindex), Index_)
                                PlayerData(Index_).SpawnedPlayers.Add(refindex)
                            End If

                        End If
                    End If
                Next refindex
                '===========MOBS===================

                For i = 0 To MobList.Count - 1
                    If CheckRange(PlayerData(Index_).Position, MobList(i).Position) And CheckSectors(PlayerData(Index_).Position, MobList(i).Position) Then
                        Dim _mob As cMonster = MobList(i)
                        If PlayerData(Index_).SpawnedMonsters.Contains(i) = False Then
                            Server.Send(CreateMonsterSpawnPacket(i), Index_)
                            PlayerData(Index_).SpawnedMonsters.Add(i)
                            Debug.Print(GetObjectById(_mob.Pk2ID).Name)
                        End If
                    End If
                Next

                '===========NPCS===================
                For i = 0 To NpcList.Count - 1
                    If CheckRange(PlayerData(Index_).Position, NpcList(i).Position) Then
                        If PlayerData(Index_).SpawnedNPCs.Contains(i) = False Then
                            Server.Send(CreateNPCGroupSpawnPacket(i), Index_)
                            PlayerData(Index_).SpawnedNPCs.Add(i)
                        End If
                    End If
                Next


                Console.WriteLine("MOB:" & PlayerData(Index_).SpawnedMonsters.Count)
                Console.WriteLine("NPC:" & PlayerData(Index_).SpawnedNPCs.Count)

            Catch ex As Exception
                Console.WriteLine("ObjectSpawnCheck()::error...")
                Debug.Write(ex)
            End Try
        End Sub


        Public Sub ObjectDeSpawnCheck(ByVal Index_ As Integer)
            Try
                For Other_Index = 0 To Server.MaxClients
                    If PlayerData(Other_Index) IsNot Nothing And PlayerData(Index_).SpawnedPlayers.Contains(Other_Index) Then
                        If CheckRange(PlayerData(Index_).Position, PlayerData(Other_Index).Position) = False Then
                            'Despawn for both
                            Server.Send(CreateDespawnPacket(PlayerData(Index_).UniqueId), Other_Index)
                            PlayerData(Other_Index).SpawnedPlayers.Remove(Index_)
                            Server.Send(CreateDespawnPacket(PlayerData(Other_Index).UniqueId), Index_)
                            PlayerData(Index_).SpawnedPlayers.Remove(Other_Index)
                        End If
                    End If
                Next

                For i = 0 To MobList.Count - 1
                    If PlayerData(Index_).SpawnedMonsters.Contains(i) = True Then
                        Dim _mob As cMonster = MobList(i)
                        If CheckRange(PlayerData(Index_).Position, _mob.Position) = False Then
                            Server.Send(CreateDespawnPacket(_mob.UniqueID), Index_)
                            PlayerData(Index_).SpawnedMonsters.Remove(i)
                        End If
                    End If
                Next

                For i = 0 To NpcList.Count - 1
                    If PlayerData(Index_).SpawnedNPCs.Contains(i) = True Then
                        Dim _npc As cNPC = NpcList(i)
                        If CheckRange(PlayerData(Index_).Position, _npc.Position) = False Then
                            Server.Send(CreateDespawnPacket(_npc.UniqueID), Index_)
                            PlayerData(Index_).SpawnedNPCs.Remove(i)
                        End If
                    End If
                Next

            Catch ex As Exception

            End Try
        End Sub


        Public Function CheckRange(ByVal Pos_1 As Position, ByVal Pos_2 As Position) As Boolean
            Dim range As Integer = 70
            If Pos_1.X >= (Pos_2.X - range) AndAlso Pos_1.X <= ((Pos_2.X - range) + range * 2) AndAlso Pos_1.Y >= (Pos_2.Y - range) AndAlso Pos_1.Y <= ((Pos_2.Y - range) + range * 2) Then
                Return True
            Else
                Return False
            End If

        End Function

        ''' <summary>
        ''' Is for checking if Mob Spawn is possible
        ''' </summary>
        ''' <param name="Pos_1"></param>
        ''' <param name="Pos_2"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function CheckSectors(ByVal Pos_1 As Position, ByVal Pos_2 As Position) As Boolean

            '############
            '# 1 # 2 # 3#
            '# 4 # 5 # 6#
            '# 7 # 8 # 9#
            '############
            '############
            Dim PossibleSectors As New List(Of Position)
            For x = -1 To 1
                For y = -1 To 1
                    Dim pos As New Position
                    pos.XSector = Pos_1.XSector + x
                    pos.YSector = Pos_1.YSector + y
                Next
            Next

            For Each e As Position In PossibleSectors
                If e.XSector = Pos_2.XSector And e.YSector = Pos_2.YSector Then
                    Return True
                End If
            Next

            Return False
        End Function
    End Module
End Namespace
