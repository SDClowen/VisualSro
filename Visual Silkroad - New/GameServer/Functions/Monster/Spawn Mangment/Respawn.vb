﻿Namespace GameServer.Functions
    Module Respawn
        Dim Random As New Random

        Public Sub CheckForRespawns()
            Dim SpotIndex

            Try
                Dim Random As New Random

                For SpotIndex = 0 To RefRespawns.Count - 1
                    If GetSpawnCount(RefRespawns(SpotIndex).SpotID) < Settings.Server_SpawnRate Then
                        If GetCountPerSector(RefRespawns(SpotIndex).Position.XSector, RefRespawns(SpotIndex).Position.YSector) <= Settings.Server_SpawnsPerSec Then
                            If Random.Next(0, 7) = 0 Then
                                ReSpawnMob(SpotIndex)
                            End If
                        Else
                            Debug.Print("CountPerSec overflow: " & GetCountPerSector(RefRespawns(SpotIndex).Position.XSector, RefRespawns(SpotIndex).Position.YSector))
                        End If
                    End If
                Next


                For SpotIndex = 0 To RefRespawnsUnique.Count - 1
                    If IsUniqueSpawned(RefRespawnsUnique(SpotIndex).Pk2ID) = False Then
                        ReSpawnUnique(SpotIndex)
                    End If
                Next
            Catch ex As Exception

            End Try

        End Sub


        Public Sub ReSpawnMob(ByVal SpotIndex As Integer)
            Try
                Dim re = RefRespawns(SpotIndex)
                Dim obj_ As Object_ = GetObjectById(RefRespawns(SpotIndex).Pk2ID)

                Select Case obj_.Type
                    Case Object_.Type_.Mob_Normal
                        SpawnMob(RefRespawns(SpotIndex).Pk2ID, GetRadomMobType, RefRespawns(SpotIndex).Position, 0, re.SpotID)
                    Case Object_.Type_.Mob_Cave
                        SpawnMob(RefRespawns(SpotIndex).Pk2ID, GetRadomMobType, RefRespawns(SpotIndex).Position, 0, re.SpotID)
                    Case Object_.Type_.Npc
                        SpawnNPC(RefRespawns(SpotIndex).Pk2ID, RefRespawns(SpotIndex).Position, RefRespawns(SpotIndex).Angle)
                End Select
            Catch ex As Exception

            End Try
        End Sub

        Public Sub ReSpawnUnique(ByVal UniqueListID As Integer)
            Dim tmp As ReSpawnUnique_ = RefRespawnsUnique(UniqueListID)
            Dim selector As Integer = Random.Next(0, tmp.Spots.Count - 1)

            SpawnMob(tmp.Pk2ID, 3, tmp.Spots(selector), 0, -2)
        End Sub


        Public Sub AddUnqiueRespawn(ByVal Spot As ReSpawn_)
            For i = 0 To RefRespawnsUnique.Count - 1
                If RefRespawnsUnique(i).Pk2ID = Spot.Pk2ID Then
                    RefRespawnsUnique(i).Spots.Add(Spot.Position)
                    Exit Sub
                End If
            Next

            'Only if no Resapwn Colletor Exitis
            Dim tmp As New ReSpawnUnique_
            tmp.Pk2ID = Spot.Pk2ID
            tmp.Spots = New List(Of Position)
            RefRespawnsUnique.Add(tmp)
        End Sub

#Region "Helper Functions"
        Private Function GetSpawnCount(ByVal SpotID As Long) As Integer
            Dim Count As Integer = 0
            For Each key In MobList.Keys.ToList
                If MobList.ContainsKey(key) Then
                    Dim Mob_ As cMonster = MobList.Item(key)
                    If Mob_.SpotID = SpotID Then
                        Count += 1
                    End If
                End If
            Next
            Return Count
        End Function

        Private Function IsUniqueSpawned(ByVal Pk2ID As UInteger) As Boolean

            For Each key In MobList.Keys.ToList
                If MobList.ContainsKey(key) Then
                    Dim Mob_ As cMonster = MobList.Item(key)
                    If Mob_.Pk2ID = Pk2ID And Mob_.SpotID <> -1 Then
                        Return True
                    End If
                End If
            Next
            Return False
        End Function

        Private Function GetCountPerSector(ByVal Xsec As Byte, ByVal Ysec As Byte) As Integer
            Dim Count As Integer

            For Each key In MobList.Keys.ToList
                If MobList.ContainsKey(key) Then
                    Dim Mob_ As cMonster = MobList.Item(key)
                    If Mob_.Position_Spawn.XSector = Xsec And Mob_.Position_Spawn.YSector = Ysec Then
                        Count += 1
                    End If
                End If
            Next
            Return Count
        End Function
#End Region

        Structure ReSpawn_
            Public SpotID As Long
            Public Pk2ID As UInteger
            Public Position As Position
            Public Angle As UShort
        End Structure

        Structure ReSpawnUnique_
            Public Pk2ID As UInteger
            Public Spots As List(Of Position)
        End Structure


    End Module
End Namespace
