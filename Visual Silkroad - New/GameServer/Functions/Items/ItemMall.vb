﻿Namespace GameServer.Functions
    Module ItemMall
        Public Sub OnSendSilks(ByVal Index_ As Integer)
            Dim UserIndex As Integer = GameServer.DatabaseCore.GetUserWithAccID(PlayerData(Index_).AccountID)
            Dim writer As New PacketWriter
            writer.Create(ServerOpcodes.Silk)
            writer.DWord(DatabaseCore.Users(UserIndex).Silk)
            writer.DWord(DatabaseCore.Users(UserIndex).Silk_Bonus)
            writer.DWord(DatabaseCore.Users(UserIndex).Silk_Points)
            Server.Send(writer.GetBytes, Index_)
        End Sub

        Public Sub OnBuyItemFromMall(ByVal packet As PacketReader, ByVal index_ As Integer)
            Dim type1 As Byte = packet.Byte
            Dim type2 As Byte = packet.Byte
            Dim type3 As Byte = packet.Byte
            Dim type4 As Byte = packet.Byte
            Dim type5 As Byte = packet.Byte
            Dim LongName As String = packet.String(packet.Word)
            Dim RefObject As MallPackage_ = GetItemMallItemByName(LongName)
            Dim writer As New PacketWriter

            Dim UserIndex As Integer = DatabaseCore.GetUserWithAccID(PlayerData(index_).AccountID)

            If LongName = RefObject.Name_Package Then
                If DatabaseCore.Users(UserIndex).Silk - RefObject.Price >= 0 Then
                    Dim _Refitem As cItem = GetItemByName(RefObject.Name_Normal)
                    Dim slot As Byte = GetFreeItemSlot(index_)
                    Dim item As cInvItem = Inventorys(index_).UserItems(slot)
                    Select Case _Refitem.CLASS_A
                        Case 1
                            item.Pk2Id = _Refitem.ITEM_TYPE
                            item.Durability = 30
                            item.Amount = 0
                            UpdateItem(item)
                        Case 3
                            item.Pk2Id = _Refitem.ITEM_TYPE
                            item.Durability = 30
                            item.Amount = RefObject.Amout
                            UpdateItem(item)
                    End Select

                    DatabaseCore.Users(UserIndex).Silk -= RefObject.Price
                    DataBase.SaveQuery(String.Format("UPDATE users SET silk='{0}' where id='{1}'", DatabaseCore.Users(UserIndex).Silk, PlayerData(index_).AccountID))
                    OnSendSilks(index_)

                    writer.Create(ServerOpcodes.ItemMove)
                    writer.Byte(1)
                    writer.Byte(24)
                    writer.Byte(type1)
                    writer.Byte(type2)
                    writer.Byte(type3)
                    writer.Byte(type4)
                    writer.Byte(type5)
                    writer.Byte(1)
                    writer.Byte(slot)
                    writer.Word(1)
                    Server.Send(writer.GetBytes, index_)
                Else
                    writer.Create(ServerOpcodes.ItemMove)
                    writer.Byte(2)
                    writer.Byte(24)
                    writer.Byte(1)
                    Server.Send(writer.GetBytes, index_)
                End If
            End If
        End Sub
    End Module
End Namespace
