
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;


namespace BarangayQuest {
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.KeyDown += Form1_KeyDown;
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            StartApp();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            StartApp();
        }

        private void StartApp()
        {
            Form2 Form2 = new Form2();
            Form2.Show();
            this.Hide();

        }

    }
}
