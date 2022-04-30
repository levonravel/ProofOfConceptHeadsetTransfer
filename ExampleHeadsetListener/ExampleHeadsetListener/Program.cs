using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ExampleHeadsetListener
{
    //run this program on different computers around your house on same network
    //this will act just like a Qivx application headset
    public class Program
    {
        public class ClientEndpointInfo
        {
            public string Address;
            public int Port;
        }

        private static TcpListener tcpListener;
        private static UdpClient udpClient;
        private static ClientEndpointInfo clientInfo = new ClientEndpointInfo();
        private static string defaultDirectory = "C:\\VRHeadsetFiles";
        static void Main(string[] args)
        {
            //check if the default directory exists
            if(!Directory.Exists(defaultDirectory))
            {
                Directory.CreateDirectory(defaultDirectory);
            }
            //setup the listeners for udp / tcp traffic
            //udp traffic is to return the tcp listeners address and port
            Listen();
            while (Console.ReadKey().Key != ConsoleKey.Escape) { };
        }
        static void Listen()
        {
            //setup tcpListener
            tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, 0));
            tcpListener.Start();
            //populate the client info for json serialization 
            IPEndPoint localAddress = (IPEndPoint)tcpListener.LocalEndpoint;
            clientInfo.Address = GetInternalAddress();
            clientInfo.Port = localAddress.Port;
            //start listening for requests
            Task.Run(() =>
            {
                while (true)
                {
                    //accept the socket connection and start reading the file(s)
                    var client = tcpListener.AcceptTcpClient();
                    var stream = client.GetStream();
                    //Todo have to send the file name over first then read the data stream to file, for now this is proof of concept.
                    using (var fileStream = new FileStream($"{defaultDirectory}\\test", FileMode.OpenOrCreate))
                    {
                        stream.CopyTo(fileStream);
                    }
                }
            });
            //setup udpClient
            udpClient = new UdpClient();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 63001));
            var endPoint = new IPEndPoint(IPAddress.Any, 0);
            //start the udp listener
            Task.Run(() =>
            {
                while(true)
                {
                    var buffer = udpClient.Receive(ref endPoint);
                    //Send the clientInfo to the endpoint
                    var jsonString = JsonConvert.SerializeObject(clientInfo);
                    udpClient.Client.SendTo(Encoding.UTF8.GetBytes(jsonString), endPoint);
                }
            });
        }
        static string GetInternalAddress()
        {
            string HostName = Dns.GetHostName();
            IPHostEntry MyEntry = Dns.GetHostByName(Dns.GetHostName());
            IPAddress MyAddress = new IPAddress(MyEntry.AddressList[0].Address);
            return MyAddress.ToString();
        }
    }
}
