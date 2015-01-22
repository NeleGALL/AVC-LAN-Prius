using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using CoreAudioApi;
using CoreAudioApi.Interfaces;
using System.Net;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form

    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        static SerialPort sp = new SerialPort();
        Thread readThread = new Thread(Read);
        static MMDeviceEnumerator devEnum;
        static MMDevice defaultDevice;

        string COM;
        public Form1()
        {
            InitializeComponent();
        }
        static public void ReInitVolume()
        {
            devEnum = new MMDeviceEnumerator();
            defaultDevice = devEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
        }
        static public Boolean Reconnect()
        {
            while(true)
            {
                try
                {
                    sp.Open();
                    return true;
                }
                catch{}
            }
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            if(Properties.Settings.Default.Port == string.Empty)
            {
                comboBox1.Items.AddRange(SerialPort.GetPortNames());
            }
            COM = Properties.Settings.Default.Port;
            if(COM != "" && comboBox1.Items.Contains(COM) == false){comboBox1.Items.Add(COM);}
            if(comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
                button2.Enabled = true;
            }
            ReInitVolume();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                sp.PortName = comboBox1.SelectedItem.ToString();
                sp.BaudRate = 250000;
                sp.ReadTimeout = 500;
                Reconnect();
                readThread.Start();
                button2.Enabled = false;
            }
            catch { }
        }
        public static void Read()
        {
            while (true)
            {
                try
                {
                    string message = sp.ReadLine();
                    CheckAndDo(message);
                }
                catch (TimeoutException) { }
                catch (Exception) 
                {
                    if (!sp.IsOpen)
                    {
                        try { Reconnect(); }
                        catch { }
                    }
                }
            }
        }
        static public Boolean CheckAndDo(string str)
        {
            if (str.Contains("0F7431F110"))
            {
                int where = str.IndexOf("0F7431F110");
                string val = str.Remove(0, where + "0F7431F110".Length);
                val = val.Remove(2);
                SetVolume(Convert.ToInt32(val), 51);

            }
            else if (str.Contains("0C0025238408"))
            {
                if (Properties.Settings.Default.Aimp_RC)
                {
                    aimp_SendRPCHTTP(aimp_next);
                }
                else
                {
                    SendKey("{F7}");
                }
            }
            else if (str.Contains("0C0025238404"))
            {             
                if (Properties.Settings.Default.Aimp_RC)
                {
                aimp_SendRPCHTTP(aimp_prev);
                }
                else
                {
                    SendKey("{F6}"); 
                }
            }
            else if (str.Contains("05002532801D"))
            {     
                if (Properties.Settings.Default.Aimp_RC)
                {
                    aimp_SendRPCHTTP(aimp_pause);
                }
                else
                {
                    SendKey("{F8}"); 
                }
            }
            return false;
        }

        #region Does
        static public void SetVolume(int val, int max)
        {
            ReInitVolume();
            float newval = (float)val / (float)max;
            defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = newval;
        }
        static public void SendKey(string keys)
        {
            SendKeys.SendWait(keys);
        }

        static public void MouseSet(int x, int y)
        {
            uint dx = (uint)x;
            uint dy = (uint)y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, dx, dy, 0, 0);
        }
        #endregion

        #region AIMP_RPC
        static public void aimp_SendRPCHTTP(string command)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:3333/RPC_JSON");
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(command);
                streamWriter.Flush();
                streamWriter.Close();
                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }
            }
        }
        static public string aimp_pause = "{\"id\": \"1\",\"method\": \"Pause\", \"params\": {}, \"version\": \"1.1\"}";
        static public string aimp_prev = "{\"id\": \"1\",\"method\": \"PlayPrevious\", \"params\": {}, \"version\": \"1.1\"}";
        static public string aimp_next = "{\"id\": \"1\",\"method\": \"PlayNext\", \"params\": {}, \"version\": \"1.1\"}";
        #endregion
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            sp.Close();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Aimp_RC = checkBox1.Checked;
        }
    }
}
