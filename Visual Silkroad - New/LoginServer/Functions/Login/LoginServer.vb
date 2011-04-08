﻿Namespace LoginServer.Functions
    Module Login


        Public Sub ClientInfo(ByVal packet As LoginServer.PacketReader, ByVal Index_ As Integer)
            ClientList.SessionInfo(Index_).Locale = packet.Byte
            ClientList.SessionInfo(Index_).ClientName = packet.String(packet.Word)
            ClientList.SessionInfo(Index_).Version = packet.DWord

            If Log_Connect Then
                With ClientList.SessionInfo(Index_)
                    Log.WriteGameLog(Index_, "Client_Connect", "(None)", String.Format("Locale: {0}, Name: {1}, Version: {2}", .Locale, .ClientName, .Version))
                End With
            End If
        End Sub

        Public Sub GateWay(ByVal index As Integer)
            Dim writer As New LoginServer.PacketWriter
            Dim name As String = "GatewayServer"
            writer.Create(ServerOpcodes.ServerInfo)
            writer.Word(name.Length)
            writer.HexString(name)
            writer.Byte(0)
            LoginServer.Server.Send(writer.GetBytes, index)
        End Sub

        Public Sub SendPatchInfo(ByVal Index_ As Integer)

            'Note: Patch Info for Rsro
            Dim writer As New LoginServer.PacketWriter
            writer.Create(ServerOpcodes.MassiveMessage)
            writer.Byte(1) 'Header Byte
            writer.Word(1) '1 Data Packet
            writer.Word(&H2005)
            Server.Send(writer.GetBytes, Index_)

            writer.Create(ServerOpcodes.MassiveMessage)
            writer.Byte(0) 'Data
            writer.Word(1)
            writer.Byte(1)
            writer.Byte(8)
            writer.Byte(&HA)
            writer.DWord(5)
            writer.Byte(2)
            Server.Send(writer.GetBytes, Index_)

            '====================================
            writer.Create(ServerOpcodes.MassiveMessage)
            writer.Byte(1) 'Header Byte
            writer.Word(1) '1 Data Packet
            writer.Word(&H6005)
            Server.Send(writer.GetBytes, Index_)

            writer.Create(ServerOpcodes.MassiveMessage)
            writer.Byte(0) 'Data
            writer.Word(3)
            writer.Word(2)
            writer.Byte(2)
            Server.Send(writer.GetBytes, Index_)

            '====================================
            writer.Create(ServerOpcodes.MassiveMessage)
            writer.Byte(1) 'Header Byte
            writer.Word(1) '1 Data Packet
            writer.Word(&HA100)
            Server.Send(writer.GetBytes, Index_)

            writer.Create(ServerOpcodes.MassiveMessage)
            writer.Byte(0) 'Data

            Dim i = Settings.Server_CurrectVersion
            If ClientList.SessionInfo(Index_).Version = Settings.Server_CurrectVersion Then
                writer.Byte(1)
                Server.Send(writer.GetBytes, Index_)
            ElseIf ClientList.SessionInfo(Index_).Version > Settings.Server_CurrectVersion Then
                'Client too new 
                writer.Byte(2)
                writer.Byte(1)
                Server.Send(writer.GetBytes, Index_)
            ElseIf ClientList.SessionInfo(Index_).Version < Settings.Server_CurrectVersion Then
                'Client too old 
                writer.Byte(2)
                writer.Byte(4)
                Server.Send(writer.GetBytes, Index_)
            End If


            '2005
            '6005
            'A100

            '05 00 0D 60 00 00 01 01 00 05 60 
            '06 00 0D 60 00 00 00  03 00 02 00 02 
            '05 00 0D 60 00 00 01 01 00 00 A1 
            '02 00 0D 60 00 00 00 01                                  

        End Sub

        Public Sub SendLauncherInfo(ByVal Index_ As Integer)

            Dim writer As New LoginServer.PacketWriter
            writer.Create(ServerOpcodes.MassiveMessage)
            writer.Byte(1) 'Header Byte
            writer.Word(1) '1 Data Packet
            writer.Word(&HA104)
            Server.Send(writer.GetBytes, Index_)



            writer.Create(ServerOpcodes.MassiveMessage)
            writer.Byte(0) 'Data
            writer.Byte(LoginDb.News.Count) 'nummer of news

            For i = 0 To LoginDb.News.Count - 1
                writer.Word(LoginDb.News(i).Title.Length)
                writer.String(LoginDb.News(i).Title.Length)

                writer.Word(LoginDb.News(i).Text.Length)
                writer.String(LoginDb.News(i).Text.Length)

                writer.Word(LoginDb.News(i).Time.Year) 'jahr
                writer.Word(LoginDb.News(i).Time.Month)
                writer.Word(LoginDb.News(i).Time.Day)
                writer.Word(LoginDb.News(i).Time.Hour) 'hour
                writer.Word(LoginDb.News(i).Time.Minute) 'minute
                writer.Word(LoginDb.News(i).Time.Second) 'secound
                writer.DWord(LoginDb.News(i).Time.Millisecond) 'millisecond
            Next


            Server.Send(writer.GetBytes, Index_)


        End Sub

        Public Sub SendServerList(ByVal Index_ As Integer)

            Dim NameServer As String = "SRO_Russia_Official"
            Dim writer As New PacketWriter

            writer.Create(ServerOpcodes.ServerList)
            writer.Byte(1) 'Nameserver
            writer.Byte(57) 'Nameserver ID
            writer.Word(NameServer.Length)
            writer.String(NameServer)
            writer.Byte(0) 'Out of nameservers


            For i = 0 To Servers.Count - 1
                writer.Byte(1) 'New Gameserver
                writer.Word(Servers(i).ServerId)
                writer.Word(Servers(i).Name.Length)
                writer.String(Servers(i).Name)
                writer.Word(Servers(i).AcUs)
                writer.Word(Servers(i).MaxUs)
                writer.Byte(Servers(i).State)
            Next

            writer.Byte(0)

            Server.Send(writer.GetBytes, Index_)

            Dim txt = "Hello"

            writer.Create(ServerOpcodes.LoginAuthInfo)
            writer.Byte(4) 'failed
            writer.Word(txt.Length)
            writer.String(txt) 'grund
            'Server.Send(writer.GetBytes, index)
        End Sub

        Public Sub HandleLogin(ByVal packet As LoginServer.PacketReader, ByVal Index_ As Integer)

            Dim LoginMethod As Byte = packet.Byte()
            Dim ID As String = packet.String(packet.Word)
            Dim Pw As String = packet.String(packet.Word)
            Dim ServerID As Integer = packet.Word
            Dim writer As New LoginServer.PacketWriter

            writer.Create(ServerOpcodes.LoginAuthInfo)
            Dim UserIndex As Integer = GetUserWithID(ID)

            If UserIndex = -1 Then
                'User exestiert nicht == We register a User

                If Settings.Auto_Register = True Then
                    If CheckIfUserCanRegister(ClientList.GetSocket(Index_).RemoteEndPoint.ToString) = True Then
                        RegisterUser(ID, Pw, Index_)
                        Dim reason As String = String.Format("A new Account with the ID: {0} and Password: {1}. You can login in 60 Secounds.", ID, Pw)
                        writer.Byte(2) 'failed
                        writer.Byte(2) 'gebannt
                        writer.Byte(1) 'unknown
                        writer.Word(reason.Length)
                        writer.String(reason) 'grund
                        writer.Word(2015) 'jahr
                        writer.Word(12) 'monat
                        writer.Word(12) 'tag
                        writer.DWord(0) 'unknwon
                        writer.DWord(0) 'unknwon
                        writer.Word(0) 'unknwon
                    Else
                        Dim reason As String = String.Format("You can only register {0} a day!", Max_RegistersPerDay)
                        writer.Byte(2) 'failed
                        writer.Byte(2) 'gebannt
                        writer.Byte(1) 'unknown
                        writer.Word(reason.Length)
                        writer.String(reason) 'grund
                        writer.Word(3000) 'jahr
                        writer.Word(12) 'monat
                        writer.Word(12) 'tag
                        writer.DWord(0) 'unknwon
                        writer.DWord(0) 'unknwon
                        writer.Word(0) 'unknwon
                    End If


                ElseIf Auto_Register = False Then
                    'Normal Fail
                    writer.Byte(2) 'login failed
                    writer.Byte(1)
                    writer.Byte(3)
                    writer.Word(0)
                    writer.Byte(0)
                    writer.Byte(1) 'number of falied logins
                    writer.Word(0)
                    writer.Byte(0)
                End If

                Server.Send(writer.GetBytes, Index_)
            Else
                CheckBannTime(UserIndex)

                If Users(UserIndex).Banned = True Then

                    writer.Byte(2) 'failed
                    writer.Byte(2) 'gebannt
                    writer.Byte(1) 'unknown
                    writer.Word(Users(UserIndex).BannReason.Length)
                    writer.String(Users(UserIndex).BannReason) 'grund
                    writer.Word(Users(UserIndex).BannTime.Year) 'jahr
                    writer.Word(Users(UserIndex).BannTime.Month) 'monat
                    writer.Word(Users(UserIndex).BannTime.Day) 'tag
                    writer.Word(Users(UserIndex).BannTime.Hour) 'tag
                    writer.Word(Users(UserIndex).BannTime.Minute) 'tag
                    writer.DWord(Users(UserIndex).BannTime.Millisecond) 'tag

                    Server.Send(writer.GetBytes, Index_)


                ElseIf Users(UserIndex).Pw <> Pw Then
                    'pw falsch
                    Dim user As UserArray = Users(UserIndex)
                    user.FailedLogins += 1
                    Users(UserIndex) = user

                    DataBase.InsertData(String.Format("UPDATE users SET failed_logins = '{0}' WHERE id = '{1}'", user.FailedLogins, user.AccountId))

                    writer.Byte(2) 'login failed
                    writer.Byte(1)
                    writer.DWord(Max_FailedLogins) 'Max Failed Logins
                    writer.DWord(user.FailedLogins) 'number of falied logins
                    Server.Send(writer.GetBytes, Index_)

                    If user.FailedLogins >= Max_FailedLogins Then
                        user.FailedLogins = 0
                        Users(UserIndex) = user

                        DataBase.InsertData(String.Format("UPDATE users SET failed_logins = '0' WHERE id = '{0}'", user.AccountId))
                        BanUser(Date.Now.AddMinutes(10), UserIndex) 'Ban for 10 mins
                    End If



                ElseIf Users(UserIndex).Name = ID And Users(UserIndex).Pw = Pw Then
                    Dim ServerIndex As Integer = GetServerIndexById(ServerID)

                    If (Servers(ServerIndex).AcUs + 1) >= Servers(ServerIndex).MaxUs Then
                        writer.Byte(4)
                        writer.Byte(2) 'Server traffic... 
                        Server.Send(writer.GetBytes, Index_)

                    Else
                        'Sucess!
                        writer.Byte(1)
                        writer.DWord(GetKey(Index_))
                        writer.Word(Servers(ServerIndex).IP.Length)
                        writer.String(Servers(ServerIndex).IP)
                        writer.Word(Servers(ServerIndex).Port)
                        Server.Send(writer.GetBytes, Index_)

                        If Log_Login Then
                            Log.WriteGameLog(Index_, "Login", "Sucess", String.Format("Name: {0}, Server: {1}", ID, Servers(ServerIndex).Name))
                        End If
                    End If
                End If
            End If
        End Sub

        Private Function GetKey(ByVal Index_ As Integer) As UInt32
            Dim split1 As String() = ClientList.GetSocket(Index_).RemoteEndPoint.ToString.Split(":")
            Dim split2 As String() = split1(0).Split(".")
            Dim key As UInt32 = CUInt(split2(0)) + CUInt(split2(1)) + CUInt(split2(2)) + CUInt(split2(3))
            Return key
        End Function
    End Module
End Namespace
