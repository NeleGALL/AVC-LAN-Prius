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
        Form2 FRM2 = new Form2();
        static Boolean Aimp_RC = false;
        static Int32 Aimp_Port = 3333;

        string COM;
        public Form1()
        {
            InitializeComponent();
            InitVolume();
            this.Size = new Size(286, 277);
        }
        public void InitVolume()
        {
            devEnum = new MMDeviceEnumerator();
            MMDeviceCollection devColl = devEnum.EnumerateAudioEndPoints(EDataFlow.eRender, EDeviceState.DEVICE_STATE_ACTIVE);
            int devNum = devColl.Count;
            comboBox2.Items.Clear();
            comboBox2.Items.Add("Default");
            comboBox3.Items.Add("0");
            for (int i = 0; i < devNum; i++)
            {
                comboBox2.Items.Add(devColl[i].FriendlyName);
                comboBox3.Items.Add(devColl[i].ID);
            }
            string devid = Properties.Settings.Default.SelectedDevice;
            for (int f = 0; f < comboBox3.Items.Count; f++)
            {
                if (comboBox3.Items[f].ToString() == devid)
                {
                    comboBox2.SelectedIndex = f;
                }

            }
            if(comboBox2.SelectedIndex < 1){comboBox2.SelectedIndex = 0;}
        }
        public void WriteToRichTextBox(string message)
        {
            richTextBox1.Text += message;
            richTextBox1.Text += (char)13;
        }
        public void ReInitVolume()
        {
            devEnum = new MMDeviceEnumerator();
            if (comboBox2.SelectedIndex < 1)
            {
                defaultDevice = devEnum.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            }
            else
            {
                defaultDevice = devEnum.GetDevice(comboBox3.Items[comboBox2.SelectedIndex].ToString());
            }            
        }
        static public async Task Reconnect()
        {
            var result = await Task<Boolean>.Factory.StartNew(() =>
            {
                Thread.Sleep(100);
                try{sp.Open();}catch{}
                return true;
            });
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }
        private void Form1_Shown(object sender, EventArgs e)
        {
            Properties.Settings.Default.Reload();
            //comboBox1
            comboBox1.Items.Clear();
            if (Properties.Settings.Default.Port == string.Empty)
            {
                comboBox1.Items.AddRange(SerialPort.GetPortNames());
            }
            COM = Properties.Settings.Default.Port;
            if (COM != "" && comboBox1.Items.Contains(COM) == false) { comboBox1.Items.Add(COM); }
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
                button2.Enabled = true;
            }
            //numericUpDown1
            numericUpDown1.Value = Properties.Settings.Default.Aimp_Port;
            Aimp_Port = Properties.Settings.Default.Aimp_Port;
            //checkBox1
            checkBox1.Checked = Properties.Settings.Default.Aimp_RC;
            Aimp_RC = Properties.Settings.Default.Aimp_RC;
            //checkBox2
            checkBox2.Checked = Properties.Settings.Default.AutoConnect;
            if (Properties.Settings.Default.AutoConnect) { connect(); }
            ReInitVolume();
        }
        private void button1_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
        }
        private void button2_Click(object sender, EventArgs e)
        {
            connect();
        }
        public async void connect()
        {
            try
            {
                sp.PortName = comboBox1.SelectedItem.ToString();
                sp.BaudRate = 250000;
                sp.ReadTimeout = 500;
                button2.Enabled = false;
                button2.Text = "...";
                do { await Reconnect(); } while (sp.IsOpen == false);
                button2.Text = "Online";
                readThread.Start();
            }
            catch { }
        }
        public static async void Read()
        {
            while (true)
            {
                Boolean err = true;
                try
                {
                    string message = sp.ReadLine();
                    CheckAndDo(message);
                    err = false;
                }
                catch (Exception) 
                {
                    err = true;
                }
                if (err) { do { await Reconnect(); } while (sp.IsOpen == false); }
            }
        }
        static public async void CheckAndDo(string str)
        {
            if (str.Contains("0F7431F110"))
            {
                int where = str.IndexOf("0F7431F110");
                string val = str.Remove(0, where + "0F7431F110".Length);
                val = val.Remove(2);
                await SetVolume(Convert.ToInt32(val), 51);
            }
            else if (str.Contains("0C0025238408"))
            {
                if (Aimp_RC)
                {
                    await aimp_SendRPCHTTP(aimp_next);
                }
                else
                {
                    await SendKey("{F7}");
                }
            }
            else if (str.Contains("0C0025238404"))
            {             
                if (Aimp_RC)
                {
                    await aimp_SendRPCHTTP(aimp_prev);
                }
                else
                {
                    await SendKey("{F6}"); 
                }
            }
            else if (str.Contains("05002532801D"))
            {     
                if (Aimp_RC)
                {
                    await aimp_SendRPCHTTP(aimp_pause);
                }
                else
                {
                    await SendKey("{F8}"); 
                }
            }
        }

        #region Does
        static public Task SetVolume(int val, int max)
        {
            float newval = (float)val / (float)max;
            try { defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = newval; }
            catch { Form1 frm = new Form1(); frm.ReInitVolume(); try { defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar = newval; } catch { } }
            return null;
        }
        static public Task SendKey(string keys)
        {
            SendKeys.SendWait(keys);
            return null;
        }

        static public void MouseSet(int x, int y)
        {
            uint dx = (uint)x;
            uint dy = (uint)y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, dx, dy, 0, 0);
        }
        #endregion

        #region AIMP_RPC
        static public Task aimp_SendRPCHTTP(string command)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:" + Convert.ToString(Aimp_Port) + "/RPC_JSON");
            httpWebRequest.ContentType = "text/json";
            httpWebRequest.Method = "POST";
            try
            {
                using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(command);
                    streamWriter.Flush();
                    streamWriter.Close();
                    HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                    using (StreamReader streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                    }
                }
            }
            catch { return null; }
            return null;
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
            Aimp_RC = checkBox1.Checked;
            Properties.Settings.Default.Save();
            if (checkBox1.Checked) { numericUpDown1.Enabled = false; } else { numericUpDown1.Enabled = true; }
        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.SelectedDevice = comboBox3.Items[comboBox2.SelectedIndex].ToString();
            Properties.Settings.Default.Save();
        }
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.AutoConnect = checkBox2.Checked;
            Properties.Settings.Default.Save();
        }
        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.Aimp_Port = Convert.ToInt32(numericUpDown1.Value);
            Aimp_Port = Convert.ToInt32(numericUpDown1.Value);
            Properties.Settings.Default.Save();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            if (Form1.ActiveForm.Size.Height > 500)
            {
                Form1.ActiveForm.Size = new Size(286, 277);
                button3.Text = "ν  Expand log  ν";
            }
            else
            {
                Form1.ActiveForm.Size = new Size(286, 677);
                button3.Text = "^  Hide log  ^";
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            if (FRM2.IsDisposed) { FRM2 = new Form2(); }
            FRM2.Show();
            FRM2.Activate();
        }
    }
}
