using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SocketExampleDesktop
{
    public partial class Form1 : Form
    {
        public class ClientEndpointInfo
        {
            public string Address;
            public int Port;
        }

        //this client is used to send files Reliably
        private TcpClient tcpClient;
        //this client is used to find the TcpClients
        private UdpClient udpClient;
        private IPEndPoint subnetMask = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 63001);

        public Form1()
        {
            InitializeComponent();
            tcpClient = new TcpClient();
            tcpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
            //start a listening service this can be done via a coroutine in unity3d
            Task.Run(() =>
            {
                var buffer = new byte[1024];
                EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                while (true)
                {
                    //this is a blocking call it waits until a message is received all we really want from this request
                    //is the buffer it contains the message for the clients tcp Address to send data on
                    udpClient.Client.ReceiveFrom(buffer, ref endPoint);
                    var clientInfo = JsonConvert.DeserializeObject<ClientEndpointInfo>(Encoding.UTF8.GetString(buffer));
                    //add the ip to the listbox
                    var tcpEndpoint = new IPEndPoint(IPAddress.Parse(clientInfo.Address), clientInfo.Port);

                    //cross threaded have to invoke manually
                    listBox1.Invoke((MethodInvoker)(() =>
                    {
                        listBox1.Items.Add(tcpEndpoint);
                    }));                    
                }
            });
        }

        private void findButton_Click(object sender, EventArgs e)
        {
            //can create some sort of security measure here that way the other sockets wont respond unless the data lines up
            //for now this is just an example to show a proof of concept.
            var buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6 };
            //sends a request against the whole network anything listening on 63001 will return a response
            udpClient.Client.SendTo(buffer, subnetMask);
        }

        private void uploadButton_Click(object sender, EventArgs e)
        {
            //open the file browser for the user to choose a file after the selection is made upload that file directly to the connections in the list
            fileExplorer.Filter = "All files (*.*)|*.*";
            //maybe we want to transfer many lessons
            fileExplorer.Multiselect = true;
            fileExplorer.ShowDialog();
            //send to each of the addresses under the listBox1.Items could have a progress bar but this is just proof of concept
            foreach(var address in listBox1.Items)
            {
                var clientAddress = (IPEndPoint)address;                
                tcpClient.Connect(clientAddress);
                //skim through all of the files and upload the data to each of the clients
                foreach (var fileLocation in fileExplorer.FileNames)
                {
                    tcpClient.Client.SendFile(fileLocation);
                    tcpClient.Close();
                    ShowConfirmationUpload(clientAddress);
                }
            }
        }
        private void ShowConfirmationUpload(EndPoint clientAddress)
        {
            var confirmResult = MessageBox.Show($"File Transfer",
                                     $"File transfer complete sent to {clientAddress}",
                                     MessageBoxButtons.OK);
        }
    }
}
