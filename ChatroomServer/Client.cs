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
        internal Socket S { set; get; }
        IPEndPoint Ie { set; get; }
        internal IPAddress Ip { set; get; }
        internal int Port { set; get; }
        internal string UserName { set; get; }        
        internal string Nick { set; get; }  //username@ip

        const string CMD_EXIT = "#exit";
        const string CMD_LIST = "#list";

        internal bool connected = false;
        internal bool connectedChat = false;
       
        public Client(Socket socket)
        {
            S = socket;

            Ie = (IPEndPoint)socket.RemoteEndPoint;
            Port = Ie.Port;

            String localHost = Dns.GetHostName();
            InfoHostClient(localHost);

            Thread t = new Thread(Chat);
            t.Start();
        }

        private void InfoHostClient(string name)
        {
            IPHostEntry hostInfo;
            hostInfo = Dns.GetHostEntry(name);
            foreach (IPAddress ip in hostInfo.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                   Ip = ip;
                }
            }
        }

        private void IntroChat()
        {
            string mensaje = null;
            try
            {
                using (NetworkStream ns = new NetworkStream(S))
                using (StreamReader sr = new StreamReader(ns))
                using (StreamWriter sw = new StreamWriter(ns))
                {
                    string welcome = "Welcome to this great Server: ";
                    sw.WriteLine(welcome);
                    sw.Flush();
                        
                    while (!connected)
                    {
                        sw.WriteLine("Introduce user name");
                        sw.Flush();
                        mensaje = sr.ReadLine();

                        if (mensaje != null && mensaje.Trim().Length > 0)
                        {
                            UserName = mensaje;                            
                            Nick = string.Format("{0}@{1}", UserName, Ip);
                            string nuevo = string.Format("Connected with client {0} at port {1}", Nick, Port);
                            Server.ShareMessage(nuevo, this, false);
                            connected = true;
                            connectedChat = true;                                                           
                        }
                    }
                }
            }
            catch (IOException)
            {
                EndConnection(false);
            }                
        }

        public void Chat()
        {
            string menssage = "";

            IntroChat();

            if (connected)
            {
                using (NetworkStream ns = new NetworkStream(S))
                using (StreamReader sr = new StreamReader(ns))
                using (StreamWriter sw = new StreamWriter(ns))
                {
                    try
                    {
                        while (connected)
                        {
                            while (connectedChat)
                            {
                                menssage = sr.ReadLine();
                                if (menssage != null)
                                {
                                    if (menssage.Length > 0)
                                    {
                                        if (!Commands(menssage.Trim(), sw))
                                        {
                                            Server.ShareMessage(menssage, this, true);
                                        }                                        
                                    }
                                }
                                else
                                {
                                    EndConnection(true);
                                }
                            }
                            if (menssage != null)
                            {
                                menssage = sr.ReadLine();
                                Commands(menssage, sw);
                            }                                                       
                        }
                        EndConnection(false);
                    }
                    catch (IOException)
                    {
                        EndConnection(true);
                    }
                }                                                             
            }           
        }


        private void EndConnection(bool msg)
        {            
            if (msg)
            {
                Server.ShareMessage(Nick+" disconnected ", this, false);
            }
            lock (Server.l)
            {
                S.Close();
                Server.Disconnect(this);        
                connectedChat = false;
                connected = false;
            }                        
        }
        
        private bool Commands(string msg, StreamWriter sw)
        {
            switch (msg)
            {
                case CMD_LIST:
                    Server.List(this);
                    return true;

                case CMD_EXIT:
                    connectedChat = !connectedChat;
                    Server.ShareMessage(UserName + (connectedChat?" connected ":" disconnected ")+"chat", this, false);
                    sw.WriteLine("------ "+ UserName + (connectedChat?" connected ":" disconnected ")+"chat");
                    sw.Flush();                                       
                    return true;
            }
            return false;
        }
    }
}

