﻿Namespace GameServer.Functions
    Module UseItem
        Public Sub OnUseItem(ByVal packet As PacketReader, ByVal Index_ As Integer)
            Dim slot As Byte = packet.Byte
            Dim ID1 As Byte = packet.Byte
            Dim ID2 As Byte = packet.Byte

            Debug.Print("[USE_ITEM][ID1:" & ID1 & "][2:" & ID2 & "]")

            If ID1 = &HEC Then
                Select Case ID2
                    Case &H8 'HP-Pot
                        OnUseHPPot(slot, Index_)
                    Case &H9 'Return Scroll
                        OnUseReturnScroll(slot, Index_)
                    Case &H10 'MP-Pot 
                        OnUseMPPot(slot, Index_)
                    Case &HE 'Speed Drug


                End Select

            ElseIf ID1 = &HED Then
                Select Case ID2
                    Case &H19 'Reverse
                        OnUseReverseScroll(slot, Index_, packet)

                    Case &H29 'Globals
                        OnUseGlobal(slot, Index_, packet)
                End Select
            End If

            Inventorys(Index_).ReOrderItems(Index_)
        End Sub

        Public Sub OnUseHPPot(ByVal Slot As Byte, ByVal Index_ As Integer)
            Dim _item As cInvItem = Inventorys(Index_).UserItems(Slot)

            If _item.Pk2Id <> 0 Then
                Dim refitem As cItem = GetItemByID(_item.Pk2Id)

                If refitem.CLASS_A = 3 And refitem.CLASS_B = 1 And refitem.CLASS_C = 1 Then
                    If PlayerData(Index_).CHP + refitem.USE_TIME_HP >= PlayerData(Index_).HP Then
                        PlayerData(Index_).CHP = PlayerData(Index_).HP
                    Else
                        PlayerData(Index_).CHP += refitem.USE_TIME_HP
                    End If

                    If Inventorys(Index_).UserItems(Slot).Slot - 1 <= 0 Then
                        'Despawn Item

                        _item.Pk2Id = 0
                        _item.Durability = 0
                        _item.Plus = 0
                        _item.Amount = 0

                        Inventorys(Index_).UserItems(Slot) = _item
                        DeleteItemFromDB(Slot, Index_)

                    ElseIf Inventorys(Index_).UserItems(Slot).Slot - 1 > 0 Then
                        _item.Amount -= 1
                        Inventorys(Index_).UserItems(Slot) = _item
                        UpdateItem(_item)
                    End If


                    UpdateHP(Index_)

                    Dim writer As New PacketWriter
                    writer.Create(ServerOpcodes.ItemUse)
                    writer.Byte(1)
                    writer.Byte(Slot)
                    writer.Word(Inventorys(Index_).UserItems(Slot).Amount)
                    writer.Byte(&HEC)
                    writer.Byte(&H8)
                    Server.Send(writer.GetBytes, Index_)

                    ShowOtherPlayerItemUse(refitem.ITEM_TYPE, Index_)
                End If
            End If
        End Sub

        Public Sub OnUseMPPot(ByVal Slot As Byte, ByVal Index_ As Integer)
            Dim _item As cInvItem = Inventorys(Index_).UserItems(Slot)

            If _item.Pk2Id <> 0 Then
                Dim refitem As cItem = GetItemByID(_item.Pk2Id)

                If refitem.CLASS_A = 3 And refitem.CLASS_B = 1 And refitem.CLASS_C = 2 Then
                    If PlayerData(Index_).CMP + refitem.USE_TIME_MP >= PlayerData(Index_).MP Then
                        PlayerData(Index_).CMP = PlayerData(Index_).MP
                    Else
                        PlayerData(Index_).CMP += refitem.USE_TIME_MP
                    End If

                    If Inventorys(Index_).UserItems(Slot).Slot - 1 <= 0 Then
                        'Despawn Item

                        _item.Pk2Id = 0
                        _item.Durability = 0
                        _item.Plus = 0
                        _item.Amount = 0

                        Inventorys(Index_).UserItems(Slot) = _item
                        DeleteItemFromDB(Slot, Index_)

                    ElseIf Inventorys(Index_).UserItems(Slot).Slot - 1 > 0 Then
                        _item.Amount -= 1
                        Inventorys(Index_).UserItems(Slot) = _item
                        UpdateItem(_item)
                    End If


                    UpdateMP(Index_)

                    Dim writer As New PacketWriter
                    writer.Create(ServerOpcodes.ItemUse)
                    writer.Byte(1)
                    writer.Byte(Slot)
                    writer.Word(Inventorys(Index_).UserItems(Slot).Amount)
                    writer.Byte(&HEC)
                    writer.Byte(&H10)
                    Server.Send(writer.GetBytes, Index_)

                    ShowOtherPlayerItemUse(refitem.ITEM_TYPE, Index_)
                End If
            End If
        End Sub

        Public Sub OnUseReverseScroll(ByVal Slot As Byte, ByVal Index_ As Integer, ByVal packet As PacketReader)
            Dim _item As cInvItem = Inventorys(Index_).UserItems(Slot)
            Dim writer As New PacketWriter
            Dim tag As Byte = packet.Byte '2 = recall 3= dead point
            Dim id As UInteger = 0

            If PlayerData(Index_).UsedItem <> UseItemTypes.None Then
                writer.Create(ServerOpcodes.ItemUse)
                writer.Byte(2) 'error
                writer.Byte(&H5D) 'already teleporting
                Server.Send(writer.GetBytes, Index_)
                Exit Sub
            End If

            If _item.Pk2Id <> 0 Then
                Dim refitem As cItem = GetItemByID(_item.Pk2Id)
                If refitem.CLASS_A = 3 And refitem.CLASS_B = 3 And refitem.CLASS_C = 3 Then 'Check for right Item
                    If Inventorys(Index_).UserItems(Slot).Slot - 1 <= 0 Then
                        'Despawn Item

                        _item.Pk2Id = 0
                        _item.Durability = 0
                        _item.Plus = 0
                        _item.Amount = 0

                        Inventorys(Index_).UserItems(Slot) = _item
                        DeleteItemFromDB(Slot, Index_)
                    ElseIf Inventorys(Index_).UserItems(Slot).Slot - 1 > 0 Then
                        _item.Amount -= 1
                        Inventorys(Index_).UserItems(Slot) = _item
                        UpdateItem(_item)
                    End If

                    UpdateState(&HB, 1, Index_)



                    writer.Create(ServerOpcodes.ItemUse)
                    writer.Byte(1)
                    writer.Byte(Slot)
                    writer.Word(Inventorys(Index_).UserItems(Slot).Amount)
                    writer.Byte(&HED)
                    writer.Byte(&H19)
                    Server.Send(writer.GetBytes, Index_)

                    ShowOtherPlayerItemUse(refitem.ITEM_TYPE, Index_)

                    Timers.UsingItemTimer(Index_).Interval = refitem.USE_TIME_HP
                    Timers.UsingItemTimer(Index_).Start()

                    Select Case tag
                        Case 2
                            PlayerData(Index_).UsedItem = UseItemTypes.Reverse_Scroll_Recall
                        Case 3
                            PlayerData(Index_).UsedItem = UseItemTypes.Reverse_Scroll_Dead
                        Case 7
                            id = packet.DWord
                            PlayerData(Index_).UsedItem = UseItemTypes.Reverse_Scroll_Point
                            PlayerData(Index_).UsedItemParameter = id
                    End Select

                    PlayerData(Index_).Busy = True
                End If
            End If
        End Sub

        Public Sub OnUseReturnScroll(ByVal Slot As Byte, ByVal Index_ As Integer)
            Dim _item As cInvItem = Inventorys(Index_).UserItems(Slot)
            Dim writer As New PacketWriter

            If PlayerData(Index_).UsedItem <> UseItemTypes.None Then
                writer.Create(ServerOpcodes.ItemUse)
                writer.Byte(2) 'error
                writer.Byte(&H5D) 'already teleporting
                Server.Send(writer.GetBytes, Index_)
                Exit Sub
            End If

            If _item.Pk2Id <> 0 Then
                Dim refitem As cItem = GetItemByID(_item.Pk2Id)

                If refitem.CLASS_A = 3 And refitem.CLASS_B = 3 And refitem.CLASS_C = 1 Then
                    If Inventorys(Index_).UserItems(Slot).Slot - 1 <= 0 Then
                        'Despawn Item

                        _item.Pk2Id = 0
                        _item.Durability = 0
                        _item.Plus = 0
                        _item.Amount = 0

                        Inventorys(Index_).UserItems(Slot) = _item
                        DeleteItemFromDB(Slot, Index_)
                    ElseIf Inventorys(Index_).UserItems(Slot).Slot - 1 > 0 Then
                        _item.Amount -= 1
                        Inventorys(Index_).UserItems(Slot) = _item
                        UpdateItem(_item)
                    End If

                    UpdateState(&HB, 1, Index_)

                    writer.Create(ServerOpcodes.ItemUse)
                    writer.Byte(1)
                    writer.Byte(Slot)
                    writer.Word(Inventorys(Index_).UserItems(Slot).Amount)
                    writer.Byte(&HEC)
                    writer.Byte(&H9)
                    Server.Send(writer.GetBytes, Index_)

                    ShowOtherPlayerItemUse(refitem.ITEM_TYPE, Index_)

                    Timers.UsingItemTimer(Index_).Interval = refitem.USE_TIME_HP
                    Timers.UsingItemTimer(Index_).Start()
                    PlayerData(Index_).UsedItem = UseItemTypes.Return_Scroll
                    PlayerData(Index_).Busy = True
                End If
            End If
        End Sub

        Public Sub OnUseGlobal(ByVal Slot As Byte, ByVal Index_ As Integer, ByVal packet As PacketReader)
            Dim messagelength As UInt16 = packet.Word
            Dim bmessage As Byte() = packet.ByteArray(messagelength * 2)
            Dim message As String = System.Text.Encoding.Unicode.GetString(bmessage)

            Dim _item As cInvItem = Inventorys(Index_).UserItems(Slot)
            Dim writer As New PacketWriter


            If _item.Pk2Id <> 0 Then
                Dim refitem As cItem = GetItemByID(_item.Pk2Id)

                If refitem.CLASS_A = 3 And refitem.CLASS_B = 3 And refitem.CLASS_C = 5 Then
                    If Inventorys(Index_).UserItems(Slot).Slot - 1 <= 0 Then
                        'Despawn Item

                        _item.Pk2Id = 0
                        _item.Durability = 0
                        _item.Plus = 0
                        _item.Amount = 0

                        Inventorys(Index_).UserItems(Slot) = _item
                        DeleteItemFromDB(Slot, Index_)
                    ElseIf Inventorys(Index_).UserItems(Slot).Slot - 1 > 0 Then
                        _item.Amount -= 1
                        Inventorys(Index_).UserItems(Slot) = _item
                        UpdateItem(_item)
                    End If


                    writer.Create(ServerOpcodes.ItemUse)
                    writer.Byte(1)
                    writer.Byte(Slot)
                    writer.Word(Inventorys(Index_).UserItems(Slot).Amount)
                    writer.Byte(&HED)
                    writer.Byte(&H29)
                    Server.Send(writer.GetBytes, Index_)

                    ShowOtherPlayerItemUse(refitem.ITEM_TYPE, Index_)

                    OnGlobalChat(message, Index_)
                End If
            End If
        End Sub

        Public Sub OnReturnScroll_Cancel(ByVal Index_ As Integer)
            If PlayerData(Index_).UsedItem <> UseItemTypes.None Then
                PlayerData(Index_).UsedItem = UseItemTypes.None
                Timers.UsingItemTimer(Index_).Stop()
                UpdateState(&HB, 0, Index_)
            End If
        End Sub

        Public Sub ShowOtherPlayerItemUse(ByVal ItemID As Integer, ByVal Index_ As Integer)
            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.ItemUseOtherPlayer)
            writer.DWord(PlayerData(Index_).UniqueId)
            writer.DWord(ItemID)
            Server.SendToAllInRange(writer.GetBytes, Index_)
        End Sub

    End Module
End Namespace
