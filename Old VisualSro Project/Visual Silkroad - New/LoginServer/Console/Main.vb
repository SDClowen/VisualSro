﻿Imports SRFramework
Imports LoginServer.Framework

Friend Class Program

    Public Shared Logpackets As Boolean = False


    Shared Sub Main()
        AddHandler Server.OnClientConnect, AddressOf Program.Server_OnClientConnect
        AddHandler Server.OnClientDisconnect, AddressOf Program.Server_OnClientDisconnect
        AddHandler Server.OnReceiveData, AddressOf Program.Server_OnReceiveData
        AddHandler Server.OnServerError, AddressOf Program.Server_OnServerError
        AddHandler Server.OnServerStarted, AddressOf Program.Server_OnServerStarted

        AddHandler Database.OnDatabaseError, AddressOf Program.db_OnDatabaseError
        AddHandler Database.OnDatabaseConnected, AddressOf Program.db_OnConnectedToDatabase
        AddHandler Database.OnDatabaseLog, AddressOf Program.db_OnDatabaseLog

        AddHandler GlobalManagerCon.OnGlobalManagerInit, AddressOf Program.gmc_OnGlobalManagerInit
        AddHandler GlobalManagerCon.OnGlobalManagerShutdown, AddressOf Program.gmc_OnGlobalManagerInit
        AddHandler GlobalManagerCon.OnError, AddressOf Program.gmc_OnGlobalManagerError
        AddHandler GlobalManagerCon.OnLog, AddressOf Program.gmc_OnGlobalManagerLog
        AddHandler GlobalManagerCon.OnPacketReceived, AddressOf Functions.ParseGlobalManager


        Console.WindowHeight = 10
        Console.BufferHeight = 30
        Console.WindowWidth = 60
        Console.BufferWidth = 60
        Console.BackgroundColor = ConsoleColor.White
        Console.ForegroundColor = ConsoleColor.DarkGreen
        Console.Clear()
        Console.Title = "LOGINSERVER ALPHA"
        Log.WriteSystemLog("Starting Server")

        Log.WriteSystemLog("Loading Settings.")
        Settings.LoadSettings()
        Settings.SetToServer()

        Log.WriteSystemLog("Loaded Settings. Conneting Database.")
        Database.Connect()

        Log.WriteSystemLog("Connected Database. Starting Server now.")
        LoginDb.UpdateData()
        ClientList.SetupClientList(Server.MaxClients)
        Timers.LoadTimers(Server.MaxClients)

        Log.WriteSystemLog("Inital Loading complete! Waiting for Globalmanager...")
        Log.WriteSystemLog("Latest Version: " & Settings.Server_CurrectVersion)
        Log.WriteSystemLog("Slotcount: " & Settings.Server_Slots)


        Do While True
            Dim msg As String = Console.ReadLine()
            CheckCommand(msg)
            Threading.Thread.Sleep(10)
        Loop
    End Sub

    Private Shared Sub Server_OnClientConnect(ByVal ip As String, ByVal index As Integer)
        Log.WriteSystemLog("Client Connected : " & ip)
        Server.OnlineClient += 1

        Dim writer As New PacketWriter
        writer.Create(ServerOpcodes.Handshake)
        writer.Byte(1)
        Server.Send(writer.GetBytes, index)
    End Sub

    Private Shared Sub Server_OnReceiveData(ByVal buffer() As Byte, ByVal index_ As Integer)
        Dim read As Integer = 0

        Do While True
            Dim length As Integer = BitConverter.ToUInt16(buffer, read)
            Dim opc As Integer = BitConverter.ToUInt16(buffer, read + 2)

            If length = 0 And opc = 0 Then 'endless prevention
                Exit Do
            End If

            Dim newbuff(length + 5) As Byte
            Array.ConstrainedCopy(buffer, read, newbuff, 0, length + 6)
            read = read + length + 6

            Dim packet As New PacketReader(newbuff)
            If Logpackets = True Then
                'Log.LogPacket(newbuff, False)
            End If

            Functions.Parser.Parse(packet, index_)
        Loop
    End Sub

    Private Shared Sub Server_OnServerError(ByVal ex As Exception, ByVal index As Integer)
        Log.WriteSystemLog("Server Error: " & ex.Message & " Index: " & index & " Stacktrace: " & ex.StackTrace) '-1 = on client connect + -2 = on server start
    End Sub

    Private Shared Sub Server_OnServerStarted(ByVal time As String)
        Log.WriteSystemLog("Server Started: " & time)
    End Sub

    Private Shared Sub Server_OnServerStopped(ByVal time As String)
        Log.WriteSystemLog("Server Stopped: " & time)
    End Sub

    Private Shared Sub Server_OnClientDisconnect(ByVal ip As String, ByVal index As Integer)
        If Timers.LoginInfoTimer(index).Enabled Then
            Timers.LoginInfoTimer(index).Stop()
        End If

        Server.OnlineClient -= 1
        Server.RevTheard(index).Abort()
    End Sub

    Private Shared Sub db_OnConnectedToDatabase()
        Log.WriteSystemLog("Connected to database at: " & DateTime.Now.ToString())
    End Sub

    Private Shared Sub db_OnDatabaseLog(ByVal message As String)
        Log.WriteSystemLog("Database Log: " & message)
    End Sub

    Private Shared Sub db_OnDatabaseError(ByVal ex As Exception, ByVal command As String)
        Log.WriteSystemLog("Database error: " & ex.Message & " Command: " & command)
    End Sub

    Private Shared Sub gmc_OnGlobalManagerInit()
        Server.Start()
    End Sub

    Private Shared Sub gmc_OnGlobalManagerShutdown()
        For i = 0 To ClientList.SessionInfo.Count - 1
            If ClientList.SessionInfo(i) IsNot Nothing Then
                Server.Dissconnect(i)
            End If
        Next
        Server.Stop()
        Database.ExecuteQuerys()

        Log.WriteSystemLog("Server stopped, Data is save. Feel free to close!")
    End Sub

    Private Shared Sub gmc_OnGlobalManagerLog(ByVal message As String)
        Log.WriteSystemLog("GMC Log: " & message)
    End Sub

    Private Shared Sub gmc_OnGlobalManagerError(ByVal ex As Exception, ByVal command As String)
        Log.WriteSystemLog("GMC error: " & ex.Message & " Command: " & command)
    End Sub
End Class


