﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlobalManager.Core.Components
{
    public class ServerManager : IDisposable
    {
        private Dictionary<ushort, cGatewayServer> m_gatewayServers;
        private Dictionary<ushort, cDownloadServer> m_downloadServers;
        private Dictionary<ushort, cAgentServer> m_agentServers;
        private Dictionary<ushort, cGameServer> m_gameServers;

        private object m_lock;

        public ServerManager()
        {
            m_gatewayServers = new Dictionary<ushort, cGatewayServer>();
            m_downloadServers = new Dictionary<ushort, cDownloadServer>();
            m_agentServers = new Dictionary<ushort, cAgentServer>();
            m_gameServers = new Dictionary<ushort, cGameServer>();

            m_lock = new object();
        }

        public void Add(int index, ushort serverID, string clientname)
        {
            lock (m_lock)
            {
                switch (clientname)
                {
                    case "GatewayServer":
                        cGatewayServer tmp_gw = new cGatewayServer();
                        tmp_gw.Index = index;
                        tmp_gw.ID = serverID;
                        tmp_gw.Authorized = true;
                        m_gatewayServers.Add(serverID, tmp_gw);
                        break;
                    case "DownloadServer":
                        cDownloadServer tmp_dl = new cDownloadServer();
                        tmp_dl.Index = index;
                        tmp_dl.ID = serverID;
                        tmp_dl.Authorized = true;
                        m_downloadServers.Add(serverID, tmp_dl);
                        break;
                    case "Agentserver":
                        cAgentServer tmp_ag = new cAgentServer();
                        tmp_ag.Index = index;
                        tmp_ag.ID = serverID;
                        tmp_ag.Authorized = true;
                        m_agentServers.Add(serverID, tmp_ag);
                        break;
                    case "GameServer":
                        cGameServer tmp_gs = new cGameServer();
                        tmp_gs.Index = index;
                        tmp_gs.ID = serverID;
                        tmp_gs.Authorized = true;
                        m_gameServers.Add(serverID, tmp_gs);
                        break;
                }
            }
        }

        public cServer this[ushort serverID]
        {
            get
            {
                if (m_gatewayServers.ContainsKey(serverID))
                {
                    return m_gatewayServers[serverID];
                }
                else if (m_downloadServers.ContainsKey(serverID))
                {
                    return m_downloadServers[serverID];
                }
                else if (m_agentServers.ContainsKey(serverID))
                {
                    return m_agentServers[serverID];
                }
                else if (m_gameServers.ContainsKey(serverID))
                {
                    return m_gameServers[serverID];
                }
                else
                {
                    return null;
                }
            }
        }

        public cServer GetServerByIndex(int index)
        {
            lock (m_lock)
            {
                //Search in GatewayServers
                foreach (var server in m_gatewayServers.Values)
                {
                    if (server.Index == index)
                    {
                        return server;
                    }
                }

                //Search in DownloadServers
                foreach (var server in m_downloadServers.Values)
                {
                    if (server.Index == index)
                    {
                        return server;
                    }
                }

                //Search in AgentServers
                foreach (var server in m_agentServers.Values)
                {
                    if (server.Index == index)
                    {
                        return server;
                    }
                }

                //Search in GameServers
                foreach (var server in m_gameServers.Values)
                {
                    if (server.Index == index)
                    {
                        return server;
                    }
                }

                //Not found :(
                return null;
            }
        }

        public void Shutdown()
        {
            //Shut all servers down.
            lock (m_lock)
            {
                foreach (var server in m_gatewayServers.Values)
                {
                    server.Shutdown();
                }
                foreach (var server in m_downloadServers.Values)
                {
                    server.Shutdown();
                }
                foreach (var server in m_agentServers.Values)
                {
                    server.Shutdown();
                }
                foreach (var server in m_gameServers.Values)
                {
                    server.Shutdown();
                }

                Dispose();
            }
        }

        public void Shutdown(ushort serverID)
        {
            lock (m_lock)
            {
                if (m_gatewayServers.ContainsKey(serverID))
                {
                    m_gatewayServers[serverID].Shutdown();
                }
                else if (m_downloadServers.ContainsKey(serverID))
                {
                    m_downloadServers[serverID].Shutdown();
                }
                else if (m_agentServers.ContainsKey(serverID))
                {
                    m_agentServers[serverID].Shutdown();
                }
                else if (m_gameServers.ContainsKey(serverID))
                {
                    m_gameServers[serverID].Shutdown();
                }
                else
                {
                    throw new Exception("Unknow serverID");
                }
            }
        }

        #region IDisposable

        private bool disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //Free managed
                    m_gatewayServers.Clear();
                    m_downloadServers.Clear();
                    m_agentServers.Clear();
                    m_gameServers.Clear();
                }
                //Free Unmanaged and large fields.
                m_gatewayServers = null;
                m_downloadServers = null;
                m_agentServers = null;
                m_gameServers = null;

                disposed = true;
            }
        }

        ~ServerManager()
        {
            Dispose(false);
        }

        #endregion
    }
}
