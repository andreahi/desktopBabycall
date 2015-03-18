using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using NAudio.Wave;
using System.Threading;
using System.IO;
namespace desktopBabycall
{
    
    public partial class Form1 : Form
    {
        System.Net.Sockets.TcpClient clientSocket = new System.Net.Sockets.TcpClient();
        System.Net.Sockets.TcpClient audioSocket = new System.Net.Sockets.TcpClient();
        System.Net.Sockets.TcpClient videoSocket = new System.Net.Sockets.TcpClient();
        NetworkStream videoStream;
        NetworkStream audioStream;
        System.Net.Sockets.TcpClient serverSocket = new System.Net.Sockets.TcpClient();
        NetworkStream serverStream;
        Image img;
        int imgHeight = 0;
        int imgWidth = 0;
        Video video = new Video();
        List<ClientData> listBoxData = new List<ClientData>();
        BindingList<ClientData> data = new BindingList<ClientData>();


        public const byte USE_VIDEO = 5;
        public const byte USE_HD_VIDEO = 6;
        public const byte PASSIVE_STATUS = 7;
        public const byte AUDIO_DATA = 8;
        public const byte REGISTER = 9;
        public const byte VIDEO_DATA = 10;
        public const byte VIDEO_HEIGHT = 11;
        public const byte VIDEO_WIDTH = 12;

        public const byte GET = 101;
        public const byte CLIENT_LIST = 102;
        public const byte LISTEN = 103;

        public Form1()
        {

            InitializeComponent();
            listBoxClients.DataSource = data;
            listBoxClients.DisplayMember = "Name";
    
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

         


            //listBox1.Items.AddRange(GetCustomers().ToArray());            
        }
        //setup the audio connection
        private Boolean setupAudio(String ip)
        {
            try
            {
                this.audioSocket.Connect(ip, 13271);
            this.audioStream = audioSocket.GetStream();
            }
            catch (SocketException)
            { 
                return false; 
            }
            return true;
        }
       
        //read audio from socket and play it
        private void playAudio()
        {
          
            WaveOut waveout = new WaveOut();
            //44100, AudioFormat.CHANNEL_IN_MONO, AudioFormat.ENCODING_PCM_16BIT,
            BufferedWaveProvider wavProv = new BufferedWaveProvider(new WaveFormat(8000, 16, 1));
            waveout.Init(wavProv);
            waveout.Play();
            int offset = 0;
            byte[] buffer = new byte[2048];
            while (true)
            {
                int recv = this.audioStream.Read(buffer, 0, 2048);
                wavProv.AddSamples(buffer, 0, recv);
                if (offset > 0)
                    offset += recv;
            }
        }

        //convert a byte array to an int
        public static  int byteArrayToInt(byte[] array) {

            return (int)((((uint) array[0]) << 24) & 0xff000000
        | (((uint)array[1]) << 16) & 0x00ff0000
        | (((uint)array[2]) << 8) & 0x0000ff00
        | ((uint)array[3]) & 0x000000ff);
        }

        //setup the video connection
        private Boolean setupVideo(string ip){
            try
            {
                videoSocket.Connect(ip, 13272);
                videoStream = videoSocket.GetStream();
            }
            catch (SocketException)
            {
                return false;
            }
            return true;
        }

        //read image data from socket and display it
        private void playVideo()
        {
            while (true)
            {

                byte[] sizeBuffer = new byte[4];
                byte[] widthBuffer = new byte[4];
                byte[] heigthBuffer = new byte[4];

                //read the width
                int recvW = videoStream.Read(widthBuffer, 0, 4);
                this.imgWidth = byteArrayToInt(widthBuffer);

                //read the height
                int recvH = videoStream.Read(heigthBuffer, 0, 4);
                this.imgHeight = byteArrayToInt(heigthBuffer);

                //read size of image
                int recvS = videoStream.Read(sizeBuffer, 0, 4);
                int imageSize = byteArrayToInt(sizeBuffer);
                byte[] buffer = new byte[imageSize]; //282996

                int recvB = 0;
                //read the image
                while (recvB < imageSize)
                {
                    int rec = videoStream.Read(buffer, recvB, imageSize - recvB);
                    if (rec > 0)
                        recvB += rec;
                }

                //load and display the image
                MemoryStream ms = new MemoryStream(buffer);
                ms.Position = 0;
                this.img = Image.FromStream(ms, true, true);
                this.Invoke((MethodInvoker)delegate
                {
                    updatePicture();
                });
            }
        }
        private void updatePicture()
        {
            video.Width = imgWidth;
            video.Height = imgHeight;
            video.updatePicture(img, imgHeight, imgWidth);
         
        }

        ¨//listen to incomming data
        void listen()
        {
            StringBuilder sb = new StringBuilder();
            WaveOut waveout = new WaveOut();
            //44100, AudioFormat.CHANNEL_IN_MONO, AudioFormat.ENCODING_PCM_16BIT,
            BufferedWaveProvider wavProv = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
            waveout.Init(wavProv);
            waveout.Play();
            while (true)
            {
                byte [] size_buffer = new byte[4];
                int bytesRead = 0;
                while (bytesRead < 4)
                {
                    int recv = this.serverStream.Read(size_buffer, bytesRead, size_buffer.Length - bytesRead);
                    if (recv > 0)
                        bytesRead += recv;
                }

                int packageSize = byteArrayToInt(size_buffer);
                byte[] buffer = new byte[packageSize];
                bytesRead = 0;
                while (bytesRead < packageSize)
                {
                    int recv = this.serverStream.Read(buffer, bytesRead, packageSize - bytesRead);
                    if (recv > 0)
                        bytesRead += recv;
                }
                if (buffer[0] == CLIENT_LIST)
                {
                    string clientName = Encoding.ASCII.GetString(buffer, 1, packageSize-1);
                     //listBoxClients.Items.Clear();
                        //listBoxClients.Items.Add(cd);
                    //listBoxClients.DisplayMember = "Name";
                    this.Invoke((MethodInvoker)delegate
                    {
                        data.Add(new ClientData() { id = buffer.Skip(1).Take(buffer.Length-1).ToArray(), Name = clientName });

                    });

                    //listBoxClients.DataSource = listBoxData;
                 
                }
                else if (buffer[0] == AUDIO_DATA)
                {
                    byte [] data = buffer.Skip(1).Take(buffer.Length - 1).ToArray();
                    wavProv.AddSamples(data, 0, data.Length);
                }
                else if (buffer[0] == VIDEO_DATA)
                {
                 

                    byte[] data = buffer.Skip(1).Take(buffer.Length - 1).ToArray();
                    MemoryStream ms = new MemoryStream(data);
                    ms.Position = 0;
                    this.img = Image.FromStream(ms, true, true);
                    this.Invoke((MethodInvoker)delegate
                    {
                        updatePicture();
                    });
                }
                else if (buffer[0] == VIDEO_WIDTH)
                {
                    byte[] data = buffer.Skip(1).Take(buffer.Length - 1).ToArray();

                    this.imgWidth = byteArrayToInt(data);
                }
                else if (buffer[0] == VIDEO_HEIGHT)
                {
                    byte[] data = buffer.Skip(1).Take(buffer.Length - 1).ToArray();

                    this.imgHeight = byteArrayToInt(data);
                }
            }
            
        }
       public class SomeData
{
    public string Value { get; set; }
    public string Text { get; set; }
}
       //try connecting to an IP
        private Boolean tryIP(string ip)
        {
            try
            {            
                System.Net.Sockets.TcpClient tmpSocket = new System.Net.Sockets.TcpClient();
                tmpSocket.Connect(ip, 13270); //192.168.0.100
                this.Invoke((MethodInvoker)delegate
                {
                    textIP.Text = ip;
                    tmpSocket.Close();
                });
                return true;
            }
            catch (SocketException)
            {
                //connection failed
                return false;
            }
        }
        

        //search for simmilar ip addresses that accept connections
        private void ipSearch()
        {
            for (int i = 0; i < 255; i++)
            {
                string ip = "192.168.0." + i;
                Thread thread = new Thread(() => tryIP(ip));
                thread.Start();
            }
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            //create new connection
            if (btnConnect.Text.Equals("Connect")) 
            {
                try
                {
                    
                        labelStatus.Text = "listening";
                        byte [] id = (listBoxClients.SelectedItem as ClientData).id;
                        video.Show();
                        Send(serverStream, LISTEN, id);
                       
                    
                
                }
                catch (SocketException)
                {
                    //labelStatus.Text = "Connection failed";
                    return;
                }
                /*
                //did the audio and video setup go ok?
                if (setupVideo(textIP.Text) && setupAudio(textIP.Text))
                {
                    Thread audioThread = new Thread(new ThreadStart(playAudio));
                    audioThread.Start();
                    Thread videoThread = new Thread(new ThreadStart(playVideo));
                    videoThread.Start();
                    video.Show();
                    labelStatus.Text = "Connection successful";
                    btnConnect.Text = "Disconnect";
                }
                else
                    labelStatus.Text = "Connection failed";
                 */ 
                //new System.Web.FileContentResult(byteArray, "image/jpeg");
            }
            else
            {
                //TODO: disconnect
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //search for possible clients
            ipSearch();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btn_server_Click(object sender, EventArgs e)
        {
            if (this.serverStream == null)
            {
                this.serverSocket.Connect("37.191.219.122", 13270);
                this.serverStream = serverSocket.GetStream();
                 Thread thread = new Thread(() => listen());
                 thread.Start();
            }
            data.Clear();
            Send(serverStream, GET, null);
           
            
        }
        public static byte[] intToByteArray(uint value)
        {
            return new byte[] {
				(byte)(value >> 24),
				(byte)(value >> 16),
				(byte)(value >> 8),
				(byte)value};
        }

        private static void Send(NetworkStream stream, byte tag, byte[] data)
        {
            // Convert the string data to byte data using ASCII encoding.
            int data_length = (data == null ? 0 : data.Length); 
            int package_length = data_length + 1;
            // Begin sending the data to the remote device.
            byte[] tagA = { tag };
            stream.Write(intToByteArray((uint)package_length), 0, 4);
            stream.Write(tagA, 0, tagA.Length);
            if(data_length > 0)
            stream.Write(data, 0, data_length);

        }

        private void listBoxClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            textIP.Text = listBoxClients.GetItemText(listBoxClients.SelectedItem);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            data.Add(new ClientData() { id = null, Name = "Andreas" });

        }
       
    }
    public class ClientData
    {
        public string Name { get; set; }
        public byte[]  id { get; set; }
    }
}
