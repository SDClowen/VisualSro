﻿Namespace GameServer.Functions
    Module PlayerAttack

        Public Rand As New Random

        Public Sub OnPlayerAttack(ByVal packet As PacketReader, ByVal Index_ As Integer)
            Dim found As Boolean = False
            Dim type1 As Byte = packet.Byte

            If type1 = 1 Then
                'Attacking
                Select Case packet.Byte
                    Case 1
                        'Normal Attack
                        packet.Skip(2)
                        Dim ObjectID As UInt32 = packet.DWord
                        If PlayerData(Index_).Attacking = False Then
                            If MobList.ContainsKey(ObjectID) And MobList(ObjectID).UniqueID = ObjectID And MobList(ObjectID).Death = False Then
                                PlayerAttackNormal(Index_, ObjectID)
                            End If

                        End If
                    Case 2
                        'Pickup
                        packet.Skip(1)
                        Dim ObjectID As UInt32 = packet.DWord

                        For i = 0 To ItemList.Count - 1
                            If ItemList(i).UniqueID = ObjectID Then
                                PickUp(i, Index_)
                            End If
                        Next
                    Case 4
                        Dim skillid As UInteger = packet.DWord
                        Dim type As Byte = packet.Byte() 'Type = 1--> Monster Attack --- Type = 0 --> Buff

                        Select Case type
                            Case 1
                                Dim ObjectID As UInt32 = packet.DWord

                                If PlayerData(Index_).Attacking = False Then
                                    If MobList.ContainsKey(ObjectID) And MobList(ObjectID).UniqueID = ObjectID And MobList(ObjectID).Death = False Then
                                        PlayerAttackBeginSkill(skillid, Index_, ObjectID)
                                        found = True
                                    End If

                                Else
                                    If PlayerData(Index_).AttackType = AttackType_.Normal Then
                                        If MobList.ContainsKey(ObjectID) And MobList(ObjectID).UniqueID = ObjectID And MobList(ObjectID).Death = False Then
                                            'Cleanup the regular Attack before using a Skill
                                            Timers.PlayerAttackTimer(Index_).Stop()
                                            PlayerData(Index_).Attacking = False
                                            PlayerData(Index_).Busy = False
                                            PlayerData(Index_).AttackType = AttackType_.Normal
                                            PlayerData(Index_).AttackedId = 0
                                            PlayerData(Index_).UsingSkillId = 0
                                            PlayerData(Index_).SkillOverId = 0

                                            PlayerAttackBeginSkill(skillid, Index_, ObjectID)
                                            found = True
                                        End If
                                    End If
                                End If
                            Case 0
                                PlayerBuff_Beginn(skillid, Index_)


                        End Select
                End Select




                If found = False Then
                    Dim writer As New PacketWriter
                    writer.Create(ServerOpcodes.Attack_Reply)
                    writer.Byte(2)
                    writer.Byte(0)
                    Server.Send(writer.GetBytes, Index_)
                End If
            Else
                'Attack Abort
                Dim writer As New PacketWriter
                writer.Create(ServerOpcodes.Attack_Reply)
                writer.Byte(2)
                writer.Byte(0)
                Server.Send(writer.GetBytes, Index_)

                MobSetAttackingFromPlayer(Index_, PlayerData(Index_).AttackedId, False)
                PlayerData(Index_).Attacking = False
                PlayerData(Index_).Busy = False
                PlayerData(Index_).AttackedId = 0
                PlayerData(Index_).UsingSkillId = 0
                PlayerData(Index_).AttackType = AttackType_.Normal
                PlayerAttackTimer(Index_).Stop()
            End If

        End Sub

        Public Sub PlayerAttackNormal(ByVal Index_ As Integer, ByVal MobUniqueId As Integer)
            Dim NumberAttack = 1, NumberVictims = 1, AttackType, afterstate As UInteger
            Dim RefWeapon As New cItem
            Dim AttObject As Object_ = GetObjectById(MobList(MobUniqueId).Pk2ID)



            If Inventorys(Index_).UserItems(6).Pk2Id <> 0 Then
                'Weapon
                RefWeapon = GetItemByID(Inventorys(Index_).UserItems(6).Pk2Id)
            Else
                'No Weapon
                RefWeapon.ATTACK_DISTANCE = 36
            End If

            If AttObject.TypeName Is Nothing Or PlayerData(Index_).Busy Then
                Exit Sub
            End If

            Dim Distance As Double = CalculateDistance(PlayerData(Index_).Position, MobList(MobUniqueId).Position)
            If Distance >= (RefWeapon.ATTACK_DISTANCE) Then
                MoveUserToMonster(Index_, MobUniqueId)
                Exit Sub
            End If


            Select Case RefWeapon.CLASS_C
                Case 0
                    AttackType = 1
                Case 2
                    NumberAttack = 2
                    AttackType = 2
                Case 3
                    NumberAttack = 2
                    AttackType = 2
                Case 4
                    AttackType = 40
                Case 5
                    AttackType = 40
                Case 6
                    AttackType = 70
                Case 7
                    AttackType = 7127
                Case 8
                    AttackType = 7128
                Case 9
                    NumberAttack = 2
                    AttackType = 7129
                Case 10
                    AttackType = 9069
                Case 11
                    AttackType = 8454
                Case 12
                    AttackType = 7909
                Case 13
                    NumberAttack = 2
                    AttackType = 7910
                Case 14
                    AttackType = 9606
                Case 15
                    AttackType = 9970
            End Select

            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.Attack_Reply)
            writer.Byte(1)
            writer.Byte(1)
            Server.Send(writer.GetBytes, Index_)


            writer.Create(ServerOpcodes.Attack_Main)
            writer.Byte(1)
            writer.Byte(2)
            writer.Byte(&H30)

            writer.DWord(AttackType)
            writer.DWord(PlayerData(Index_).UniqueId)
            writer.DWord(Id_Gen.GetSkillOverId)
            writer.DWord(MobUniqueId)

            writer.Byte(1)
            writer.Byte(NumberAttack)
            writer.Byte(NumberVictims) '1 victim

            For d = 0 To NumberVictims - 1
                writer.DWord(MobList(MobUniqueId).UniqueID)

                For i = 0 To NumberAttack - 1
                    Dim Damage As UInteger = CalculateDamageMob(Index_, AttObject, AttackType)
                    Dim Crit As Byte = Attack_GetCritical()

                    If Crit = True Then
                        Damage = Damage * 2
                        Crit = 2
                    End If
                    If CLng(MobList(MobUniqueId).HP_Cur) - Damage > 0 Then
                        MobList(MobUniqueId).HP_Cur -= Damage
                        MobAddDamageFromPlayer(Damage, Index_, MobList(MobUniqueId).UniqueID, True)
                    ElseIf CLng(MobList(MobUniqueId).HP_Cur) - Damage <= 0 Then
                        'Dead
                        afterstate = &H80
                        MobAddDamageFromPlayer(MobList(MobUniqueId).HP_Cur, Index_, MobList(MobUniqueId).UniqueID, False) 'Done the last Damage
                        MobList(MobUniqueId).HP_Cur = 0
                    End If

                    writer.Byte(afterstate)
                    writer.Byte(Crit)
                    writer.DWord(Damage)
                    writer.Byte(0)
                    writer.Word(0)
                Next
            Next
            Server.SendToAllInRange(writer.GetBytes, PlayerData(Index_).Position)

            If afterstate = &H80 Then
                GetEXPFromMob(MobList(MobUniqueId))
                KillMob(MobList(MobUniqueId).UniqueID)
                Attack_SendAttackEnd(Index_)

                PlayerData(Index_).Attacking = False
                PlayerData(Index_).Busy = False
                PlayerData(Index_).AttackType = AttackType_.Normal
                PlayerData(Index_).AttackedId = 0
                PlayerData(Index_).UsingSkillId = 0
                PlayerData(Index_).SkillOverId = 0
            Else
                PlayerData(Index_).Attacking = True
                PlayerData(Index_).Busy = True
                PlayerData(Index_).AttackedId = MobList(MobUniqueId).UniqueID
                PlayerData(Index_).UsingSkillId = AttackType
                PlayerData(Index_).AttackType = AttackType_.Normal
                If PlayerAttackTimer(Index_).Enabled = False Then
                    PlayerAttackTimer(Index_).Interval = 2500
                    PlayerAttackTimer(Index_).Start()
                End If
            End If

        End Sub

        Public Sub PlayerAttackBeginSkill(ByVal SkillID As UInt32, ByVal Index_ As Integer, ByVal MobUniqueId As Integer)
            Dim RefSkill As Skill_ = GetSkillById(SkillID)
            Dim RefWeapon As New cItem
            Dim Mob_ As cMonster = MobList(MobUniqueId)

            If PlayerData(Index_).Busy Or CheckIfUserOwnSkill(SkillID, Index_) = False Then
                Exit Sub
            End If


            If CalculateDistance(PlayerData(Index_).Position, Mob_.Position) >= RefSkill.Distance Then
                'MoveUserToMonster(Index_, MobIndex)
                'Exit Sub
            End If

            If CInt(PlayerData(Index_).CMP) - RefSkill.RequiredMp < 0 Then
                'Not enough MP
                Attack_SendNotEnoughMP(Index_)
                Exit Sub
            Else
                PlayerData(Index_).CMP -= RefSkill.RequiredMp
                UpdateMP(Index_)
            End If

            PlayerData(Index_).AttackedId = Mob_.UniqueID
            PlayerData(Index_).Attacking = True
            PlayerData(Index_).UsingSkillId = SkillID
            PlayerData(Index_).AttackType = AttackType_.Skill
            PlayerData(Index_).SkillOverId = Id_Gen.GetSkillOverId
            MobSetAttackingFromPlayer(Index_, Mob_.UniqueID, True)

            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.Attack_Reply)
            writer.Byte(1)
            writer.Byte(1)
            Server.Send(writer.GetBytes, Index_)

            writer.Create(ServerOpcodes.Attack_Main)
            writer.Byte(1)
            writer.Byte(2)
            writer.Byte(&H30)

            writer.DWord(PlayerData(Index_).UsingSkillId)
            writer.DWord(PlayerData(Index_).UniqueId)
            writer.DWord(PlayerData(Index_).SkillOverId)
            writer.DWord(PlayerData(Index_).AttackedId)
            writer.Byte(0)
            Server.SendIfPlayerIsSpawned(writer.GetBytes, Index_)

            If RefSkill.CastTime > 0 Then
                PlayerAttackTimer(Index_).Interval = RefSkill.CastTime
                PlayerAttackTimer(Index_).Start()
            Else
                PlayerAttackEndSkill(Index_)
            End If
        End Sub

        Public Sub PlayerAttackEndSkill(ByVal Index_ As Integer)
            Dim RefSkill As Skill_ = GetSkillById(PlayerData(Index_).UsingSkillId)
            Dim AttObject As New Object_
            Dim RefWeapon As New cItem
            Dim Mob_ As cMonster = MobList(PlayerData(Index_).AttackedId)
            Dim afterstate, NumberVictims As Integer
            NumberVictims = 1

            If Inventorys(Index_).UserItems(6).Pk2Id <> 0 Then
                'Weapon
                RefWeapon = GetItemByID(Inventorys(Index_).UserItems(6).Pk2Id)
            Else
                'No Weapon
                Exit Sub
            End If


            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.Buff_Info)
            writer.Byte(1)

            writer.DWord(PlayerData(Index_).SkillOverId)
            writer.DWord(PlayerData(Index_).AttackedId)

            writer.Byte(1)
            writer.Byte(RefSkill.NumberOfAttacks)
            writer.Byte(NumberVictims) '1 victim

            For d = 0 To NumberVictims - 1
                writer.DWord(PlayerData(Index_).AttackedId)

                For i = 0 To RefSkill.NumberOfAttacks - 1
                    Dim Damage As UInteger = CalculateDamageMob(Index_, AttObject, RefSkill.Id)
                    Dim Crit As Byte = Attack_GetCritical()

                    If Crit = True Then
                        Damage = Damage * 2
                        Crit = 2
                    End If

                    If CLng(Mob_.HP_Cur) - Damage > 0 Then
                        Mob_.HP_Cur -= Damage
                        MobAddDamageFromPlayer(Damage, Index_, Mob_.UniqueID, False)
                    ElseIf CLng(Mob_.HP_Cur) - Damage <= 0 Then
                        'Dead
                        afterstate = &H80
                        MobAddDamageFromPlayer(Mob_.HP_Cur, Index_, Mob_.UniqueID, False)
                        Mob_.HP_Cur = 0
                    End If

                    writer.Byte(afterstate)
                    writer.Byte(Crit)
                    writer.DWord(Damage)
                    writer.Byte(0)
                    writer.Word(0)
                Next
            Next
            Server.SendIfPlayerIsSpawned(writer.GetBytes, Index_)

            If afterstate = &H80 Then
                GetEXPFromMob(Mob_)
                KillMob(Mob_.UniqueID)
                Attack_SendAttackEnd(Index_)

                PlayerData(Index_).Attacking = False
                PlayerData(Index_).Busy = False
                PlayerData(Index_).AttackType = AttackType_.Normal
                PlayerData(Index_).AttackedId = 0
                PlayerData(Index_).UsingSkillId = 0
                PlayerData(Index_).SkillOverId = 0
            Else
                PlayerData(Index_).Attacking = True
                PlayerData(Index_).Busy = True
                PlayerData(Index_).AttackType = AttackType_.Normal
                If PlayerAttackTimer(Index_).Enabled = False Then
                    PlayerAttackTimer(Index_).Interval = 2500
                    PlayerAttackTimer(Index_).Start()
                End If

                'Monster Attack back
                Mob_.AttackTimer_Start(5)
            End If
        End Sub


        Function CalculateDamageMob(ByVal Index_ As Integer, ByVal Mob As Object_, ByVal SkillID As UInt32) As UInteger
            Dim RefWeapon As New cItem
            Dim RefSkill As Skill_ = GetSkillById(SkillID)
            Dim FinalDamage As UInteger
            Dim Balance As Double
            'If (CSng(PlayerData(Index_).Level) - Mob.Level) > -10 Then
            '    Balance = (1 + ((CSng(PlayerData(Index_).Level) - Mob.Level) / 100))
            'Else
            '    Balance = 0.01
            'End If
            Balance = (1 + ((CSng(PlayerData(Index_).Level) - Mob.Level) / 100))
            If Balance < 0 Then
                Balance = 0.01
            End If

            Dim DamageMin As Double
            Dim DamageMax As Double

            If Inventorys(Index_).UserItems(6).Pk2Id <> 0 Then
                'Weapon
                GetItemByID(Inventorys(Index_).UserItems(6).Pk2Id)
            Else
                'No Weapon
            End If

            If RefSkill.Type = TypeTable.Phy Then
                DamageMin = ((PlayerData(Index_).MinPhy + RefSkill.PwrMin) * (1 + 0) / (1 + 0) - Mob.PhyDef) * Balance * (1 + 0) * (RefSkill.PwrPercent / 10)
                DamageMax = ((PlayerData(Index_).MaxPhy + RefSkill.PwrMax) * (1 + 0) / (1 + 0) - Mob.PhyDef) * Balance * (1 + 0) * (RefSkill.PwrPercent / 10)
            ElseIf RefSkill.Type = TypeTable.Mag Then
                DamageMin = ((PlayerData(Index_).MinMag + RefSkill.PwrMin) * (1 + 0) / (1 + 0) - Mob.MagDef) * Balance * (1 + 0) * (RefSkill.PwrPercent / 10)
                DamageMax = ((PlayerData(Index_).MaxMag + RefSkill.PwrMax) * (1 + 0) / (1 + 0) - Mob.MagDef) * Balance * (1 + 0) * (RefSkill.PwrPercent / 10)
            End If


            If DamageMin <= 0 Or DamageMin >= UInteger.MaxValue Then
                DamageMin = 1
            End If
            If DamageMax <= 0 Or DamageMax >= UInteger.MaxValue Then
                DamageMax = 4
            End If


            If DamageMax < DamageMin Then
                Log.WriteSystemLog(String.Format("Ply Max Dmg over Min Dmg. Min {0} Max {1}. Char: {2}, Mob:{3}, Skill:{4}", DamageMin, DamageMax, PlayerData(Index_).CharacterName, Mob.TypeName, SkillID))
                DamageMax = DamageMin + 1
            End If


            Dim Radmon As Integer = Rnd() * 100
            FinalDamage = DamageMin + (((DamageMax - DamageMin) / 100) * Radmon)

            'A = Basic Attack Power
            'B = Skill Attack Power
            'C = Attack Power Increasing rate
            'D = Enemy 's total accessories Absorption rate
            'E = Enemy 's Defence Power
            'F = Balance rate
            'G = Total Damage Increasing rate
            'H = Skill Attack Power rate
            'A final damage formula:

            'Damage = ((A + B) * (1 + C) / (1 + D) - E) * F * (1 + G) * H

            Return FinalDamage
        End Function

        Public Sub Attack_SendNotEnoughMP(ByVal Index_ As Integer)
            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.Attack_Reply)
            writer.Byte(3)
            writer.Byte(0)
            writer.Byte(4)
            writer.Byte(&H40)
            Server.Send(writer.GetBytes, Index_)

            writer.Create(ServerOpcodes.Attack_Main)
            writer.Byte(2)
            writer.Byte(4)
            writer.Byte(&H30)
            Server.Send(writer.GetBytes, Index_)
        End Sub

        Public Sub Attack_SendAttackEnd(ByVal Index_ As Integer)
            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.Attack_Reply)
            writer.Byte(2)
            writer.Byte(0)
            Server.Send(writer.GetBytes, Index_)
        End Sub

        Public Function Attack_GetCritical() As Boolean
            If Math.Round(Rnd() * 5) = 5 Then
                Return True
            Else
                Return False
            End If
        End Function

    End Module
End Namespace