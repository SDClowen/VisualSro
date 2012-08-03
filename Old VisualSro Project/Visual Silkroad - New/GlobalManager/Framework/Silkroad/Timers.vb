﻿Imports System.Timers

Namespace Timers
    Module Timers

        Friend QueryTimer As New Timer
        Friend CleanUpTimer As New Timer
        Public PingTimer As New Timer

        Friend Sub LoadTimers()
            'Handlers

            AddHandler QueryTimer.Elapsed, AddressOf QueryTimer_Elapsed
            AddHandler CleanUpTimer.Elapsed, AddressOf CleanUp_Elapsed
            AddHandler PingTimer.Elapsed, AddressOf PingTimer_Elapsed

            'Starting
            QueryTimer.Interval = 2500
            QueryTimer.Start()

            CleanUpTimer.Interval = 2500
            CleanUpTimer.Start()

            PingTimer.Interval = 30000
            PingTimer.Start()
        End Sub


        Friend Sub QueryTimer_Elapsed(ByVal sender As Object, ByVal e As ElapsedEventArgs)
            QueryTimer.Stop()

            Try

                Database.ExecuteQuerys()


            Catch ex As Exception
                Log.WriteSystemLog("Timer Error: " & ex.Message & " Stack: " & ex.StackTrace & " Index: QT")
            End Try

            QueryTimer.Start()
        End Sub


        Friend Sub CleanUp_Elapsed(ByVal sender As Object, ByVal e As ElapsedEventArgs)
            CleanUpTimer.Stop()

            Try

                Dim tmplist = Agent.UserAuthCache.Keys.ToList
                For i = 0 To tmplist.Count - 1
                    Dim key As UInt32 = tmplist(i)
                    If Agent.UserAuthCache.ContainsKey(key) Then
                        If Date.Compare(Date.Now, Agent.UserAuthCache(key).ExpireTime) = 1 Then '(-1,1)
                            Agent.UserAuthCache.Remove(key)
                        End If
                    End If
                Next


            Catch ex As Exception
                Log.WriteSystemLog("Timer Error: " & ex.Message & " Stack: " & ex.StackTrace & " Index: CU")
            End Try

            CleanUpTimer.Start()
        End Sub

        Public Sub PingTimer_Elapsed(ByVal sender As Object, ByVal e As ElapsedEventArgs)
            PingTimer.Stop()

            'Excluded from ClientList 

            Try
                Dim Count As Integer = 0

                For i = 0 To Server.MaxClients - 1
                    Dim socket As Net.Sockets.Socket = Server.ClientList.GetSocket(i)
                    If socket IsNot Nothing AndAlso socket.Connected AndAlso SessionInfo(i) IsNot Nothing Then
                        If DateDiff(DateInterval.Second, Server.ClientList.LastPingTime(i), DateTime.Now) > 30 Then
                            Server.Disconnect(i)

                            'ElseIf SessionInfo(i).LoginAuthRequired And DateDiff(DateInterval.Second, SessionInfo(i).LoginAuthTimeout, DateTime.Now) > 0 Then
                            '    'Later for check for missing Handshake Packets, ClientInfo
                            '    Server.Disconnect(i)
                        Else
                            Count += 1
                        End If
                    End If
                Next

                'Server.OnlineClients = Count

            Catch ex As Exception
                Log.WriteSystemLog("Timer Error: " & ex.Message & " Stack: " & ex.StackTrace & " Index: PING")
            End Try

            PingTimer.Start()
        End Sub
    End Module
End Namespace
