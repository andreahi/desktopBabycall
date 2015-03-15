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
        Image img;
        int imgHeight = 0;
        int imgWidth = 0;
        Video video = new Video();
        public Form1()
        {
            InitializeComponent();
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
            BufferedWaveProvider wavProv = new BufferedWaveProvider(new WaveFormat(44100, 16, 1));
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
                    clientSocket.Connect(textIP.Text, 13270); //192.168.0.100
                    NetworkStream serverStream = clientSocket.GetStream();
                    byte[] VERSION = { 0, 2, 1 };
                    serverStream.Write(VERSION, 0, VERSION.Length);
                    serverStream.Flush();
                    byte[] inStream = new byte[1024];
                    serverStream.Read(inStream, 0, 3);
                }
                catch (SocketException)
                {
                    labelStatus.Text = "Connection failed";
                    return;
                }

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
    }
}
