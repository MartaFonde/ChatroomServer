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
    class Server
    {
        static readonly internal object l = new object();   //en cada acceso a clients --> recurso común
        static List<Client> clients = new List<Client>();

        internal static bool ShareMessage(string mensaje, Client c, bool nick)   
        {
            string msg;
            lock (l)
            {
                if (mensaje != null)
                {
                    if (nick)
                    {
                        msg = string.Format("{0}: {1}", c.Nick, mensaje);
                    }
                    else
                    {
                        msg = string.Format("------ {0}", mensaje);
                    }
                    Console.WriteLine(msg);                //server
                    //lock
                    for (int i = 0; i < clients.Count; i++)
                    {
                        IPEndPoint ieReceptor = (IPEndPoint)clients[i].S.RemoteEndPoint;
                        if (ieReceptor.Port != c.Port && clients[i].connected && clients[i].connectedChat)
                        {
                            using (NetworkStream ns = new NetworkStream(clients.ElementAt(i).S))
                            using (StreamReader sr = new StreamReader(ns))
                            using (StreamWriter sw = new StreamWriter(ns))
                            {
                                try
                                {
                                    sw.WriteLine(msg);
                                    sw.Flush();
                                }
                                catch (IOException)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    return true;
                }                
            }
            return false;                   
        }        

        internal static void Disconnect(Client c)
        {
            clients.Remove(c);               
        }

        internal static void List(Client c)
        {
            lock (l)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (c.Port == clients[i].Port)
                    {
                        using (NetworkStream ns = new NetworkStream(c.S))
                        using (StreamReader sr = new StreamReader(ns))
                        using (StreamWriter sw = new StreamWriter(ns))
                        {
                            try
                            {
                                for (int j = 0; j < clients.Count; j++)
                                {
                                    if (clients[i].connectedChat)
                                    {
                                        sw.WriteLine(clients[i].Nick);
                                        sw.Flush();
                                    }
                                }
                            }
                            catch (IOException ex)
                            {
                                sw.Write("Error: " + ex.Message);
                                Console.WriteLine("Error to list: " + ex.Message);
                            }
                        }
                        break;
                    }
                }
            }            
        }

        static void Main(string[] args)
        {
            int port = 31416;
            bool portFree = false;
            IPEndPoint ie = new IPEndPoint(IPAddress.Any, port);

            using (Socket server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                while (!portFree)
                {
                    try
                    {
                        server.Bind(ie);
                        server.Listen(10);
                        portFree = true;

                        Console.WriteLine("Server waiting at port {0}", ie.Port);

                        while (true)
                        {
                            Socket sClient = server.Accept();
                            lock (l)
                            {
                                clients.Add(new Client(sClient));  //lanzamos hilo
                            }
                        }
                    }
                    catch (SocketException e) when (e.ErrorCode == (int)SocketError.AddressAlreadyInUse)
                    {
                        Console.WriteLine($"Port {port} in use");
                        portFree = false;
                        port++;
                    }
                    catch (SocketException e)
                    {
                        Console.WriteLine("Error: " + e.Message);
                    }
                }                     
            }
        }
    }
}
