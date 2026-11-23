using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form2 : Form
    {
        private readonly FormResizer _resizer;
        public Form2()
        {
            InitializeComponent();
            _resizer = new FormResizer(this);
        }

        private void CitizenWorkerBtn_Click(object sender, EventArgs e)
        {
            Form4 Form4 = new Form4();
            Form4.Show();
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Form9 Form9 = new Form9();
            Form9.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form16 Form16 = new Form16();
            Form16.Show();
            this.Hide();
        }
    }
}
