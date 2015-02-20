using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApplication1
{
    public partial class Form2 : Form
    {
        List<String> asd = new List<String>();
        DataTable dt = new DataTable("Actions");
        FileStream f;
        BindingSource bs = new BindingSource();
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            FileStream f = File.OpenWrite("actions.xml");
            f.Close();
            System.Threading.Thread.Sleep(100);
            try { dt.ReadXml("actions.xml"); }
            catch { }
            
            bs.DataSource = dt; 
            dataGridView1.DataSource = dt;
            DataGridViewComboBoxColumn myCombo = new DataGridViewComboBoxColumn();
            myCombo.HeaderText = "Команда";
            myCombo.Name = "Does";
            this.dataGridView1.Columns.Insert(1, myCombo);
            myCombo.Items.Add("AIMP Next");
            myCombo.Items.Add("AIMP Prev");
            myCombo.Items.Add("AIMP Play/Pause");
            myCombo.Items.Add("AIMP Stop");
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            dt.WriteXml("actions.xml", XmlWriteMode.WriteSchema);
        }
    }
}
