using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechLib;
using System.IO;
using System.Net.Sockets;
using System.Net;

namespace PartyDataRetrivalLib
{
    public class PartyDataRetrivalClient
    {
        Socket Socket_sender;
        int client_packet_size=400;

        public PartyDataRetrivalClient()
        {
            StreamReader sr = new StreamReader("ip_client_config.txt");

            string ip = sr.ReadLine();
            int port = Convert.ToInt32(sr.ReadLine());

            sr.Close();

            Socket_sender = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress server_ip_adr = IPAddress.Parse(ip);
            IPEndPoint server_endpoint = new IPEndPoint(server_ip_adr, port);

            while (!Socket_sender.Connected)
            {
                try
                {
                    Socket_sender.Connect(server_endpoint);
                }
                catch { }
            }
        }

        public void Send(WoWLivingObj obj)
        {
            bool con_lost = false;
            Converter.CodeAndSend<WoWLivingObj>(Socket_sender, obj, ref con_lost, client_packet_size);
            if (con_lost)
                throw new Exception("Разорвано соединение с сервером!");
        }

        public void Close()
        {
            byte[] arr = new byte[0];
            Socket_sender.Send(arr);
            Socket_sender.Shutdown(SocketShutdown.Both);
            Socket_sender.Close();
        }
    }

}
