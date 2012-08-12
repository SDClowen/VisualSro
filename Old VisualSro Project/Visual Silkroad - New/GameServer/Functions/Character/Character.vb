﻿Imports SRFramework

Namespace Functions
    Module Character
        Public Sub HandleCharPacket(ByVal Index_ As Integer, ByVal pack As PacketReader)
            If SessionInfo(Index_).SRConnectionSetup = cSessionInfo_GameServer.SRConnectionStatus.AUTH Or SessionInfo(Index_).SRConnectionSetup = cSessionInfo_GameServer.SRConnectionStatus.CHARLIST Then
                SessionInfo(Index_).SRConnectionSetup = cSessionInfo_GameServer.SRConnectionStatus.CHARLIST
            Else
                Server.Disconnect(Index_)
            End If

            Dim tag As Byte = pack.Byte

            Select Case tag
                Case 1 'Crweate
                    OnCreateChar(pack, Index_)
                Case 2 'Char List
                    OnCharList(Index_)
                Case 3 'Char delete
                    OnDeleteChar(pack, Index_)
                Case 4 'Nick Check
                    OnCheckNick(pack, Index_)
                Case 5 'Restore
                    OnRestoreChar(pack, Index_)
            End Select
        End Sub

        Public Sub OnCharList(ByVal Index_ As Integer)

            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.GAME_CHARACTER)

            GameDB.FillCharList(CharListing(Index_))

            writer.Byte(2)
            'Char List
            writer.Byte(1)

            writer.Byte(CharListing(Index_).NumberOfChars)

            If CharListing(Index_).NumberOfChars = 0 Then
                Server.Send(writer.GetBytes, Index_)

            ElseIf CharListing(Index_).NumberOfChars > 0 Then
                For i = 0 To (CharListing(Index_).NumberOfChars - 1)
                    If CharListing(Index_).Chars(i) Is Nothing Then
                        Log.WriteSystemLog("Charlist, Char is nothing, AccID: " & CharListing(Index_).LoginInformation.Name)
                        Server.Disconnect(Index_)
                    End If

                    With CharListing(Index_).Chars(i)
                        writer.DWord(.Pk2ID)
                        writer.Word(.CharacterName.Length)
                        writer.String(.CharacterName)
                        writer.Byte(.Volume)
                        writer.Byte(.Level)
                        writer.QWord(.Experience)
                        writer.Word(.Strength)
                        writer.Word(.Intelligence)
                        writer.Word(.Attributes)
                        writer.DWord(.CHP)
                        writer.DWord(.CMP)
                        If .Deleted = True Then
                            Dim diff As Long = DateDiff(DateInterval.Minute, DateTime.Now, .DeletionTime)
                            writer.Byte(1) 'to delete
                            writer.DWord(diff)
                        Else
                            writer.Byte(0)
                        End If

                        writer.Word(0) 'Job Alias
                        writer.Byte(0) 'In Academy

                        'Now Items
                        Dim Inventory As cInventory = New cInventory(.CharacterId, .MaxInvSlots)

                        Dim playerItemCount As Integer = 0
                        For slot = 0 To 9
                            If Inventory.UserItems(slot).ItemID <> 0 Then
                                playerItemCount += 1
                            End If
                        Next

                        writer.Byte(playerItemCount)

                        For slot = 0 To 9
                            If Inventory.UserItems(slot).ItemID <> 0 Then
                                Dim item As cItem = GameDB.Items(Inventory.UserItems(slot).ItemID)
                                writer.DWord(item.ObjectID)
                                writer.Byte(item.Plus)
                            End If
                        Next

                        'Avatars

                        Dim avatarItemCount As Integer = 0
                        For slot = 0 To 4
                            If Inventory.AvatarItems(slot).ItemID <> 0 Then
                                avatarItemCount += 1
                            End If
                        Next

                        writer.Byte(avatarItemCount)

                        For slot = 0 To 4
                            If Inventory.AvatarItems(slot).ItemID <> 0 Then
                                Dim item As cItem = GameDB.Items(Inventory.AvatarItems(slot).ItemID)
                                writer.DWord(item.ObjectID)
                                writer.Byte(item.Plus)
                            End If
                        Next
                    End With
                Next


                'unknown byte thief - hunterr flag?
                writer.Byte(0)

                Server.Send(writer.GetBytes, Index_)

            End If
        End Sub

        Public Sub OnCheckNick(ByVal packet As PacketReader, ByVal Index_ As Integer)
            Dim nick As String = packet.String(packet.Word)

            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.GAME_CHARACTER)
            writer.Byte(4)
            'nick check

            If GameDB.CheckNick(nick) And CheckForAbuse(nick) = False Then
                writer.Byte(1)
            Else
                writer.Byte(2)
                writer.Byte(&H10)
                writer.Byte(4)
                '260
            End If

            Server.Send(writer.GetBytes, Index_)
        End Sub

        Public Sub OnSendNickError(ByVal index_ As Integer, ByVal Errornum As UInt16)
            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.GAME_CHARACTER)
            writer.Byte(4)
            writer.Byte(2)
            writer.Word(Errornum)
            Server.Send(writer.GetBytes, index_)
        End Sub

        Public Function CheckForAbuse(ByVal nick As String) As Boolean
            Dim tmp As String = nick.ToLowerInvariant
            For i = 0 To RefAbuseList.Count - 1
                If tmp.Contains(RefAbuseList(i)) = True Then
                    Return True
                End If
            Next
            Return False
        End Function

        Public Sub OnDeleteChar(ByVal packet As PacketReader, ByVal Index_ As Integer)
            Dim nick As String = packet.String(packet.Word)
            For i = 0 To CharListing(Index_).NumberOfChars - 1
                If CharListing(Index_).Chars(i).CharacterName = nick Then
                    CharListing(Index_).Chars(i).Deleted = True
                    Dim dat As DateTime = DateTime.Now
                    Dim dat1 = dat.AddDays(7)
                    CharListing(Index_).Chars(i).DeletionTime = dat1
                    Database.SaveQuery(
                        String.Format("UPDATE characters SET deletion_mark='1', deletion_time='{0}' where id='{1}'",
                                      dat1.ToString, CharListing(Index_).Chars(i).CharacterId))

                    Dim writer As New PacketWriter
                    writer.Create(ServerOpcodes.GAME_CHARACTER)
                    writer.Byte(3)   'type = delte
                    writer.Byte(1) 'success
                    Server.Send(writer.GetBytes, Index_)
                End If
            Next
        End Sub


        Public Sub OnRestoreChar(ByVal packet As PacketReader, ByVal Index_ As Integer)
            Dim nick As String = packet.String(packet.Word)
            For i = 0 To CharListing(Index_).NumberOfChars - 1
                If CharListing(Index_).Chars(i).CharacterName = nick Then
                    CharListing(Index_).Chars(i).Deleted = False
                    Database.SaveQuery(String.Format("UPDATE characters SET deletion_mark='0' where id='{0}'",
                                                     CharListing(Index_).Chars(i).CharacterId))

                    Dim writer As New PacketWriter
                    writer.Create(ServerOpcodes.GAME_CHARACTER)
                    writer.Byte(5) 'type = restore
                    writer.Byte(1) 'success
                    Server.Send(writer.GetBytes, Index_)
                End If
            Next
        End Sub

        Public Sub OnCreateChar(ByVal pack As PacketReader, ByVal Index_ As Integer)
            Dim nick As String = pack.String(pack.Word)
            Dim model As UInt32 = pack.DWord
            Dim volume As Byte = pack.Byte
            Dim _items(4) As UInt32
            _items(1) = pack.DWord
            _items(2) = pack.DWord
            _items(3) = pack.DWord
            _items(4) = pack.DWord

            'Check it

            If IsCharChinese(model) = False And IsCharEurope(model) = False Then
                'Wrong Model Code! 
                Server.Disconnect(Index_)
                Log.WriteSystemLog(String.Format("[Character Creation][Wrong Model: {0}][Index: {1}]", model, Index_))
            End If

            Dim _refitems(4) As cRefItem
            _refitems(1) = GetItemByID(_items(1))
            _refitems(2) = GetItemByID(_items(2))
            _refitems(3) = GetItemByID(_items(3))
            _refitems(4) = GetItemByID(_items(4))

            For i = 1 To 4
                If _refitems(i).ITEM_TYPE_NAME.EndsWith("_DEF") = False Then
                    Server.Disconnect(Index_)
                    Log.WriteSystemLog(String.Format("[Character Creation][Wrong Item: {0}][Index: {1}]",
                                                     _refitems(i).ITEM_TYPE_NAME, Index_))
                End If
                Debug.Print("[Character Creation][" & i & "][ID:" & _items(i) & "]")
            Next

            'Creation
            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.GAME_CHARACTER)
            writer.Byte(1)
            'create


            If GameDB.CheckNick(nick) And CheckForAbuse(nick) = True Then
                writer.Byte(2)
                writer.Word(216)
                Server.Send(writer.GetBytes, Index_)
            Else

                Array.Resize(GameDB.Chars, GameDB.Chars.Count + 1)

                Dim newCharacterIndex As Integer = GameDB.Chars.Count - 1

                GameDB.Chars(newCharacterIndex) = New cCharacter

                With GameDB.Chars(newCharacterIndex)
                    .AccountID = CharListing(Index_).LoginInformation.Id
                    .CharacterName = nick
                    .CharacterId = Id_Gen.GetNewCharId
                    .UniqueID = Id_Gen.GetUnqiueId
                    .HP = 200
                    .MP = 200
                    .CHP = 200
                    .CMP = 200
                    .Pk2ID = model
                    .Volume = volume
                    .Level = Settings.Player_StartLevel
                    .Gold = Settings.Player_StartGold
                    .SkillPoints = Settings.Player_StartSP
                    .GM = Settings.Player_StartGM

                    .WalkSpeed = 16
                    .RunSpeed = 50
                    .BerserkSpeed = 100
                    .Strength = 20
                    .Intelligence = 20
                    .PVP = 0
                    .MaxInvSlots = 45
                    .PositionDead = Settings.Player_StartReturnPos
                    .PositionRecall = Settings.Player_StartReturnPos
                    .PositionReturn = Settings.Player_StartReturnPos
                    If IsCharChinese(model) Then
                        .PosTracker = New cPositionTracker(Settings.PlayerStartPosCh, .WalkSpeed, .RunSpeed,
                                                            .BerserkSpeed)
                    ElseIf IsCharEurope(model) Then
                        .PosTracker = New cPositionTracker(Settings.Player_StartPos_Eu, .WalkSpeed, .RunSpeed,
                                                            .BerserkSpeed)
                    End If
                    Const magdefmin As Double = 3.0
                    Const phydefmin As Double = 6.0
                    Const phyatkmin As UShort = 6
                    Const phyatkmax As UShort = 9
                    Const magatkmin As UShort = 6
                    Dim magatkmax As UShort = 9
                    Dim hit As UShort = 11
                    Dim parry As UShort = 11

                    .MinPhy = phyatkmin
                    .MaxPhy = phyatkmax
                    .MinMag = magatkmin
                    .MaxMag = magatkmax
                    .PhyDef = phydefmin
                    .MagDef = magdefmin
                    .Hit = hit
                    .Parry = parry

                    .SetCharGroundStats()

                    Database.SaveQuery(
                        String.Format(
                            "INSERT INTO characters (id, account, name, chartype, volume, level, gold, sp, gm) VALUE ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}')",
                            .CharacterId, .AccountID,
                            .CharacterName, .Pk2ID,
                            .Volume, .Level,
                            .Gold, .SkillPoints, CInt(.GM)))
                    Database.SaveQuery(
                        String.Format(
                            "UPDATE characters SET xsect='{0}', ysect='{1}', xpos='{2}', zpos='{3}', ypos='{4}' where id='{5}'",
                           .Position.XSector, .Position.YSector,
                            Math.Round(.Position.X),
                            Math.Round(.Position.Z),
                            Math.Round(.Position.Y),
                            GameDB.Chars(.CharacterId)))
                    Database.SaveQuery(String.Format("INSERT INTO char_pos (OwnerCharID) VALUE ('{0}')",
                                                     .CharacterId))
                    Database.SaveQuery(String.Format("INSERT INTO guild_member (charid) VALUE ('{0}')",
                                                     .CharacterId))

                    ' Masterys

                    If IsCharChinese(model) Then
                        'Chinese Char
                        '257 - 259

                        Dim mastery As New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 257
                        AddMasteryToDB(mastery)

                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 258
                        AddMasteryToDB(mastery)

                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 259
                        AddMasteryToDB(mastery)

                        '273 - 276
                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 273
                        AddMasteryToDB(mastery)

                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 274
                        AddMasteryToDB(mastery)

                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 275
                        AddMasteryToDB(mastery)

                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 276
                        AddMasteryToDB(mastery)


                    ElseIf IsCharEurope(model) Then

                        'Europe Char
                        '513 - 518
                        Dim mastery As New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 513
                        AddMasteryToDB(mastery)

                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 514
                        AddMasteryToDB(mastery)

                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 515
                        AddMasteryToDB(mastery)

                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 516
                        AddMasteryToDB(mastery)

                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 517
                        AddMasteryToDB(mastery)

                        mastery = New cMastery
                        mastery.OwnerID = .CharacterId
                        mastery.Level = Settings.Player_StartMasteryLevel
                        mastery.MasteryID = 518
                        AddMasteryToDB(mastery)
                    End If


                    'ITEMS
                    'Inventory
                    For slot = 0 To 109
                        Dim tmp_item As New cInventoryItem
                        tmp_item.OwnerID = .CharacterId
                        tmp_item.Slot = slot
                        tmp_item.ItemID = 0
                        ItemManager.AddInvItem(tmp_item, cInventoryItem.Type.Inventory)
                    Next
                    'Avatar
                    For slot = 0 To .MaxAvatarSlots
                        Dim tmp_item As New cInventoryItem
                        tmp_item.OwnerID = .CharacterId
                        tmp_item.Slot = slot
                        tmp_item.ItemID = 0
                        ItemManager.AddInvItem(tmp_item, cInventoryItem.Type.AvatarInventory)
                    Next


                    '1 = Body = Inv 1
                    '2 = Legs = Inv 4
                    '3 = foot = Inv 5
                    '4 = Waffe = Inv 6
                    Dim item As New cItem 'Body
                    item.ObjectID = _items(1)
                    item.Plus = Math.Round(Rand.Next(Settings.Player_StartItemsPlusMin, Settings.Player_StartItemsPlusMax))
                    item.Data = _refitems(1).MAX_DURA
                    Dim ID_1 As UInt64 = ItemManager.AddItem(item)
                    ItemManager.UpdateInvItem(.CharacterId, 1, ID_1, cInventoryItem.Type.Inventory)

                    item = New cItem 'legs
                    item.ObjectID = _items(2)
                    item.Plus = Math.Round(Rand.Next(Settings.Player_StartItemsPlusMin, Settings.Player_StartItemsPlusMax))
                    item.Data = _refitems(2).MAX_DURA
                    Dim ID_2 As UInt64 = ItemManager.AddItem(item)
                    ItemManager.UpdateInvItem(.CharacterId, 4, ID_2, cInventoryItem.Type.Inventory)

                    item = New cItem 'Foot
                    item.ObjectID = _items(3)
                    item.Plus = Math.Round(Rand.Next(Settings.Player_StartItemsPlusMin, Settings.Player_StartItemsPlusMax))
                    item.Data = _refitems(3).MAX_DURA
                    Dim ID_3 As UInt64 = ItemManager.AddItem(item)
                    ItemManager.UpdateInvItem(.CharacterId, 5, ID_3, cInventoryItem.Type.Inventory)

                    item = New cItem 'Weapon
                    item.ObjectID = _items(4)
                    item.Plus = Math.Round(Rand.Next(Settings.Player_StartItemsPlusMin, Settings.Player_StartItemsPlusMax))
                    item.Data = _refitems(4).MAX_DURA
                    Dim ID_4 As UInt64 = ItemManager.AddItem(item)
                    ItemManager.UpdateInvItem(.CharacterId, 6, ID_4, cInventoryItem.Type.Inventory)

                    If _items(4) = 3632 Or _items(4) = 3633 Then 'Sword or Blade need a Shield
                        item = New cItem
                        item.ObjectID = 251
                        item.Plus = Math.Round(Rand.Next(Settings.Player_StartItemsPlusMin, Settings.Player_StartItemsPlusMax))
                        item.Data = GetItemByID(251).MAX_DURA
                        Dim ID_5 As UInt64 = ItemManager.AddItem(item)
                        ItemManager.UpdateInvItem(.CharacterId, 7, ID_5, cInventoryItem.Type.Inventory)


                    ElseIf _items(4) = 3636 Then 'Bow --> Give some Arrows
                        item = New cItem
                        item.ObjectID = 62
                        item.Data = 100
                        Dim ID_5 As UInt64 = ItemManager.AddItem(item)
                        ItemManager.UpdateInvItem(.CharacterId, 7, ID_5, cInventoryItem.Type.Inventory)

                    ElseIf _items(4) = 10730 Or _items(4) = 10734 Or _items(4) = 10737 Then 'EU Weapons who need a shield
                        item = New cItem
                        item.ObjectID = 10738
                        item.Plus = Math.Round(Rand.Next(Settings.Player_StartItemsPlusMin, Settings.Player_StartItemsPlusMax))
                        item.Data = GetItemByID(251).MAX_DURA
                        Dim ID_5 As UInt64 = ItemManager.AddItem(item)
                        ItemManager.UpdateInvItem(.CharacterId, 7, ID_5, cInventoryItem.Type.Inventory)

                    ElseIf _items(4) = 10733 Then 'Armbrust --> Bolt
                        item = New cItem
                        item.ObjectID = 10376
                        item.Data = 100
                        Dim ID_5 As UInt64 = ItemManager.AddItem(item)
                        ItemManager.UpdateInvItem(.CharacterId, 7, ID_5, cInventoryItem.Type.Inventory)
                    End If

                    'Hotkeys
                    For i = 0 To 50
                        Dim toadd As New cHotKey
                        toadd.OwnerID = .CharacterId
                        toadd.Slot = i
                        AddHotKeyToDB(toadd)
                    Next

                    'Mods
                    GameMod.Damage.OnPlayerCreate(.CharacterId, Index_)

                End With

                'Finish
                writer.Byte(1)
                'success
                Server.Send(writer.GetBytes, Index_)
            End If
        End Sub

        Public Sub AddMasteryToDB(ByVal toadd As cMastery)
            Array.Resize(GameDB.Masterys, GameDB.Masterys.Length + 1)
            GameDB.Masterys(GameDB.Masterys.Length - 1) = toadd

            Database.SaveQuery(String.Format("INSERT INTO char_mastery(owner, mastery, level) VALUE ('{0}','{1}','{2}')",
                                             toadd.OwnerID, toadd.MasteryID, toadd.Level))
        End Sub

        Public Sub AddHotKeyToDB(ByVal toadd As cHotKey)
            GameDB.Hotkeys.Add(toadd)
            Database.SaveQuery(String.Format("INSERT INTO hotkeys(OwnerID, slot) VALUE ('{0}','{1}')", toadd.OwnerID,
                                             toadd.Slot))
        End Sub

        Public Sub CharLoading(ByVal Index_ As Integer, ByVal pack As PacketReader)
            If SessionInfo(Index_).SRConnectionSetup = cSessionInfo_GameServer.SRConnectionStatus.CHARLIST Then
                SessionInfo(Index_).SRConnectionSetup = cSessionInfo_GameServer.SRConnectionStatus.GOING_INGAME
            Else
                Server.Disconnect(Index_)
            End If

            Dim SelectedNick As String = pack.String(pack.Word)
            Dim writer As New PacketWriter

            writer.Create(ServerOpcodes.GAME_INGAME_REQ_REPLY)
            writer.Byte(1)
            Server.Send(writer.GetBytes, Index_)

            'Main
            For i = 0 To CharListing(Index_).Chars.Count - 1
                If CharListing(Index_).Chars(i).CharacterName = SelectedNick Then
                    PlayerData(Index_) = CharListing(Index_).Chars(i)
                    PlayerData(Index_).UniqueID = Id_Gen.GetUnqiueId()

                    Inventorys(Index_) = New cInventory(CharListing(Index_).Chars(i).CharacterId, CharListing(Index_).Chars(i).MaxInvSlots)

                    SessionInfo(Index_).Charname = SelectedNick
                    Exit For
                End If
            Next

            'No Char....
            If SessionInfo(Index_).Charname = "" Then
                Server.Disconnect(Index_)
                Exit Sub
            End If

            'Prepare
            CleanUpPlayerComplete(Index_)
            Player_CheckDeath(Index_, True)
            GameMod.Damage.OnPlayerLogon(Index_)


            'Packet's
            writer = New PacketWriter
            writer.Create(ServerOpcodes.GAME_LOADING_START)
            Server.Send(writer.GetBytes, Index_)

            OnCharacterInfo(Index_)

            writer = New PacketWriter
            writer.Create(ServerOpcodes.GAME_LOADING_END)
            Server.Send(writer.GetBytes, Index_)

            writer = New PacketWriter
            writer.Create(ServerOpcodes.GAME_CHARACTER_ID)
            writer.DWord(PlayerData(Index_).UniqueID) 'charid
            writer.Word(Date.Now.Day) 'moon pos
            writer.Byte(Date.Now.Hour) 'hours
            writer.Byte(Date.Now.Minute) 'minute
            Server.Send(writer.GetBytes, Index_)
        End Sub

        Public Sub OnCharacterInfo(ByVal Index_ As Integer)
            PlayerData(Index_).SetCharGroundStats()
            PlayerData(Index_).AddItemsToStats(Index_)

            Dim writer As New PacketWriter
            Dim chari As cCharacter = PlayerData(Index_)
            writer = New PacketWriter
            writer.Create(ServerOpcodes.GAME_CHARACTER_INFO)
            writer.DWord(3912288588)  '@@@@@@@@@@@@@@@@@
            writer.DWord(chari.Pk2ID)      ' Character Model
            writer.Byte(chari.Volume)    ' Volume & Height
            writer.Byte(chari.Level)
            writer.Byte(chari.Level) ' Highest Level

            writer.QWord(chari.Experience)
            writer.DWord(chari.SkillPointBar)
            writer.QWord(chari.Gold)
            writer.DWord(chari.SkillPoints)
            writer.Word(chari.Attributes)
            writer.Byte(chari.BerserkBar)
            writer.DWord(0)
            writer.DWord(chari.CHP)
            writer.DWord(chari.CMP)
            writer.Byte(chari.HelperIcon)
            writer.Byte(0)   ' Daily PK (/15)
            writer.Word(0)   ' Total PK
            writer.DWord(0)   ' PK Penalty Point
            writer.Byte(0) ' Rank
            writer.Byte(0)  ' Unknown

            'INVENTORY
            Inventorys(Index_).CalculateItemCount()
            writer.Byte(chari.MaxInvSlots)       ' Max Item Slot (0 Minimum + 13) (4 Seiten x 24 Slots = 96 Maximum + 13 --> 109)
            writer.Byte(Inventorys(Index_).ItemCount)         ' Amount of Items  

            For Each item As cInventoryItem In Inventorys(Index_).UserItems
                If item.ItemID <> 0 Then
                    writer.Byte(item.Slot)

                    AddItemDataToPacket(GameDB.Items(item.ItemID), writer)
                End If
            Next

            'Avatar Inventory
            Inventorys(Index_).CalculateAvatarCount()
            writer.Byte(5)  ' Avatar Item Max
            writer.Byte(Inventorys(Index_).AvatarCount)    ' Amount of Avatars

            For Each avatar As cInventoryItem In Inventorys(Index_).AvatarItems
                If avatar.ItemID <> 0 Then
                    Dim item As cItem = GameDB.Items(avatar.ItemID)
                    Dim refitem As cRefItem = GetItemByID(item.ObjectID)
                    writer.Byte(GetExternalAvatarSlot(refitem))
                    AddItemDataToPacket(item, writer)
                End If
            Next

            writer.Byte(0)  ' Duplicate List (00 - None) (01 - Duplicate)
            writer.Word(11) 'Unknown
            writer.Byte(0)  'Unknown

            'Mastery List
            For i = 0 To GameDB.Masterys.Length - 1
                If (GameDB.Masterys(i) IsNot Nothing) AndAlso GameDB.Masterys(i).OwnerID = chari.CharacterId Then
                    writer.Byte(1) 'Mastery start
                    writer.DWord(GameDB.Masterys(i).MasteryID)
                    writer.Byte(GameDB.Masterys(i).Level)
                End If
            Next

            writer.Byte(2)     'Mastery list end
            writer.Byte(0)     'Skill list start


            For i = 0 To GameDB.Skills.Length - 1
                If (GameDB.Skills(i) IsNot Nothing) AndAlso GameDB.Skills(i).OwnerID = chari.CharacterId Then
                    writer.Byte(1)    'skill start
                    writer.DWord(GameDB.Skills(i).SkillID)    'skill id
                    writer.Byte(1)   ' skill end
                End If
            Next

            writer.Byte(2)  'Skill List End



            writer.Word(0) ' Amount of Completed Quests
            'writer.DWord(1) 'event

            writer.Word(0) ' Amount of Pending Quests



            ' ID, Position, State, Speed
            writer.DWord(0)
            writer.DWord(chari.UniqueID)
            writer.Byte(chari.Position.XSector)
            writer.Byte(chari.Position.YSector)
            writer.Float(chari.Position.X)
            writer.Float(chari.Position.Z)
            writer.Float(chari.Position.Y)
            writer.Word(chari.Angle)
            writer.Byte(0)    ' Destination
            writer.Byte(1) ' Walk & Run Flag
            writer.Byte(0)   ' No Destination
            writer.Word(chari.Angle)
            writer.Byte(0)
            writer.Byte(0)
            writer.Byte(0)
            writer.Byte(chari.Berserk)
            writer.Float(chari.WalkSpeed)
            writer.Float(chari.RunSpeed)
            writer.Float(chari.BerserkSpeed)


            writer.Byte(0) ' Buff Flag

            writer.Word(chari.CharacterName.Length)
            writer.String(chari.CharacterName)
            writer.Word(chari.AliasName.Length)
            writer.String(chari.AliasName)
            writer.Word(0)
            writer.Byte(0)
            '0=not selected, 1 = hunter
            writer.Byte(1)
            writer.QWord(0)
            writer.DWord(0)
            writer.Byte(&HFF)    'PVP Flag


            'Account
            writer.QWord(0)
            writer.DWord(chari.AccountID)
            writer.Byte(chari.GM)
            writer.Byte(7)


            Dim hotkeycount As UInteger = 0
            For i = 0 To GameDB.Hotkeys.Count - 1
                If GameDB.Hotkeys(i).OwnerID = chari.CharacterId Then
                    If GameDB.Hotkeys(i).Type <> 0 And GameDB.Hotkeys(i).IconID <> 0 Then
                        hotkeycount += 1
                    End If
                End If
            Next

            writer.Byte(hotkeycount)

            For i = 0 To GameDB.Hotkeys.Count - 1
                If GameDB.Hotkeys(i).OwnerID = chari.CharacterId Then
                    If GameDB.Hotkeys(i).Type <> 0 And GameDB.Hotkeys(i).IconID <> 0 Then
                        writer.Byte(GameDB.Hotkeys(i).Slot)
                        writer.Byte(GameDB.Hotkeys(i).Type)
                        writer.DWord(GameDB.Hotkeys(i).IconID)
                    End If
                End If
            Next


            ' Autopotion
            writer.Word(chari.PotHp)
            writer.Word(chari.PotMp)
            writer.Word(chari.PotAbormal)
            writer.Byte(chari.PotDelay)


            writer.Byte(0)  ' Amount of Players Blocked
            writer.Byte(0)  'Other Block Shit (Trade or PTM)


            ' Other
            writer.Word(1)
            writer.Word(1)
            writer.Byte(0)
            writer.Byte(2)

            Server.Send(writer.GetBytes, Index_)
        End Sub

        Public Sub OnJoinWorldRequest(ByVal Index_ As Integer)
            If SessionInfo(Index_).SRConnectionSetup = cSessionInfo_GameServer.SRConnectionStatus.GOING_INGAME Then
                SessionInfo(Index_).SRConnectionSetup = cSessionInfo_GameServer.SRConnectionStatus.INGAME
            Else
                Server.Disconnect(Index_)
            End If

            PlayerData(Index_).Ingame = True

            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.GAME_JOIN_WORLD_REPLY)
            writer.Byte(1)
            writer.Byte(&HB4)
            Server.Send(writer.GetBytes, Index_)

            'UpdateState(4, 2, Index_) 'Untouchable Status
            OnStatsPacket(Index_)
            OnSendSilks(Index_)
            If PlayerData(Index_).InGuild = True Then
                SendGuildInfo(Index_, False)
                LinkPlayerToGuild(Index_)
            End If

            ObjectSpawnCheck(Index_)
        End Sub

        Public Sub OnCharacterNamechangeRequest(ByVal Index_ As Integer, ByVal packet As PacketReader)
            Dim tag As Byte = packet.Byte
            Dim OldCharname As String = packet.String(packet.Word)
            Dim NewCharname As String = packet.String(packet.Word)

            GameDB.FillCharList(CharListing(Index_))

            For i = 0 To CharListing(Index_).Chars.Count - 1
                If CharListing(Index_).Chars(i) Is Nothing Then
                    Log.WriteSystemLog("Charlist, Char is nothing, AccID: " & CharListing(Index_).LoginInformation.Name)
                    Server.Disconnect(Index_)
                End If

                With CharListing(Index_).Chars(i)
                    If .CharacterName = OldCharname Then
                        'Found the Char, Change is only possible when it contains a @
                        If .CharacterName.Contains("@") Then
                            'Check the New Name
                            If GameDB.CheckNick(NewCharname) And CheckForAbuse(NewCharname) = False Then
                                'Free ;)
                                .CharacterName = NewCharname

                                GameDB.SaveNameUpdate(.CharacterId, .CharacterName)
                            End If
                        End If
                    End If
                End With
            Next
        End Sub

        Private Sub Player_CheckDeath(ByVal Index_ As Integer, ByVal SetPosToTown As Boolean)
            If PlayerData(Index_).CHP = 0 Then
                PlayerData(Index_).Alive = True
                PlayerData(Index_).CHP = PlayerData(Index_).HP / 2
                GameDB.SaveHP(Index_)
            End If

            If SetPosToTown Then
                PlayerData(Index_).SetPosition = PlayerData(Index_).PositionReturn
                GameDB.SavePosition(Index_)
            End If
        End Sub

        Public Function IsCharChinese(ByVal Model As UInteger) As Boolean
            If Model >= 1907 And Model <= 1932 Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Function IsCharEurope(ByVal Model As UInteger) As Boolean
            If Model >= 14717 And Model <= 14743 Then
                Return True
            Else
                Return False
            End If
        End Function
    End Module
End Namespace
