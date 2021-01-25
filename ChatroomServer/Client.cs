using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatroomServer
{
    class Client
    {        
        internal IPAddress ip;
        internal int port;
        internal string usuario;
        internal Socket s;
        IPEndPoint ie;
        internal string nick;

        const string CMD_SALIR = "#salir";
        const string CMD_LISTA = "#lista";

        internal bool connected = false;
        internal bool connectedChat = false;
       
        public Client(Socket socket)
        {
            s = socket;

            ie = (IPEndPoint)socket.RemoteEndPoint;
            port = ie.Port;

            String localHost = Dns.GetHostName();
            infoHostClient(localHost);

            Thread t = new Thread(chat);
            t.Start();
        }

        private void infoHostClient(string name)
        {
            IPHostEntry hostInfo;
            hostInfo = Dns.GetHostEntry(name);
            //usuario = hostInfo.HostName;
            foreach (IPAddress ips in hostInfo.AddressList)
            {
                if (ips.AddressFamily == AddressFamily.InterNetwork)
                {
                   ip = ips;
                }
            }
        }

        private void introChat()
        {
            string mensaje = null;
            try
            {
                using (NetworkStream ns = new NetworkStream(s))
                using (StreamReader sr = new StreamReader(ns))
                using (StreamWriter sw = new StreamWriter(ns))
                {
                    string welcome = "Welcome to this great Server: ";
                    sw.WriteLine(welcome);
                    sw.Flush();

                    //nick = string.Format("{0}@{1}", usuario, ip);
                    //string nuevo = string.Format("Connected with client {0} at port {1}", nick, port);
                    //lock (l)
                    //{
                    //    Server.pasarMensaje(nuevo, this, false);
                    //    connectedChat = true;
                    //    connected = true;
                    //}
                        
                    while (!connected)
                    {
                        sw.WriteLine("Introduce nombre de usuario");
                        sw.Flush();
                        mensaje = sr.ReadLine();

                        if (mensaje != null && mensaje.Trim().Length > 0)
                        {
                            usuario = mensaje;                            
                            nick = string.Format("{0}@{1}", usuario, ip);
                            string nuevo = string.Format("Connected with client {0} at port {1}", nick, port);
                            Server.pasarMensaje(nuevo, this, false);
                            connected = true;
                            connectedChat = true;
                                                           
                        }
                    }
                }
            }
            catch (IOException)
            {
                finConexion(false);
            }                
        }

        public void chat()
        {
            string mensaje = "";

            introChat();

            if (connected)
            {
                using (NetworkStream ns = new NetworkStream(s))
                using (StreamReader sr = new StreamReader(ns))
                using (StreamWriter sw = new StreamWriter(ns))
                {
                    try
                    {
                        while (connected)
                        {
                            while (connectedChat)
                            {
                                mensaje = sr.ReadLine();
                                if (mensaje != null)
                                {
                                    if (mensaje.Length > 0)
                                    {
                                        if (!comandos(mensaje.Trim(), sw))
                                        {
                                            Server.pasarMensaje(mensaje, this, true);
                                        }                                            
                                    }
                                }
                                else
                                {
                                    finConexion(true);
                                }
                            }
                            if (mensaje != null)
                            {
                                mensaje = sr.ReadLine();
                                comandos(mensaje, sw);
                            }                                                       
                        }
                        finConexion(false);
                    }
                    catch (IOException)
                    {
                        finConexion(true);
                    }
                }                                             
                
            }           
        }


        private void finConexion(bool mensaje)
        {
            
            if (mensaje)
            {
                Server.pasarMensaje(nick+" disconnected ", this, false);
            }
            lock (Server.l)
            {
                s.Close();
                Server.disconnect(this);
                connectedChat = false;
                connected = false;
            }            
            
        }
        
        private bool comandos(string msg, StreamWriter sw)
        {
            switch (msg)
            {
                case CMD_LISTA:
                    Server.lista(this);
                    return true;

                case CMD_SALIR:
                    connectedChat = !connectedChat;
                    Server.pasarMensaje(usuario + (connectedChat?" connected ":" disconnected ")+"chat", this, false);
                    sw.WriteLine("------ "+ usuario + (connectedChat?" connected ":" disconnected ")+"chat");
                    sw.Flush();                                       
                    return true;
            }
            return false;
        }
    }
}

