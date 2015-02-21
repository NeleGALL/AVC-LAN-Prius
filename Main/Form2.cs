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
using System.Web;
using System.Xml;


namespace WindowsFormsApplication1
{
    public partial class Form2 : Form
    {
        DataSet ds = new DataSet();
        DataTable dt = new DataTable("Actions");
        BindingSource bs = new BindingSource();
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            ds.Tables.Add(dt);
            System.Threading.Thread.Sleep(100);
            dataGridView1.DataSource = ds;
            dataGridView1.DataMember = "Actions";
            DataGridViewComboBoxColumn myCombo = new DataGridViewComboBoxColumn();
            myCombo.HeaderText = "Does";
            myCombo.Name = "Does";
            //this.dataGridView1.Columns.Insert(1, myCombo);
            myCombo.Items.Add("AIMP Next");
            myCombo.Items.Add("AIMP Prev");
            myCombo.Items.Add("AIMP Play/Pause");
            myCombo.Items.Add("AIMP Stop");
        }

        private void dataGridView1_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            dataGridView1.EndEdit();
            ds.WriteXml("actions.xml");
        }
    }
}
