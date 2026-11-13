namespace BarangayQuest
{
    partial class Form3
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form3));
            pictureBox1 = new PictureBox();
            returntorole = new Button();
            label1 = new Label();
            label2 = new Label();
            fullnametxt = new TextBox();
            contactnumtxt = new TextBox();
            usernameacctxt = new TextBox();
            passcodetxt = new TextBox();
            verifypasscodetxt = new TextBox();
            barangayidnotxt = new TextBox();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            registernewaccount = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = Color.FromArgb(128, 64, 64);
            pictureBox1.BackgroundImage = (Image)resources.GetObject("pictureBox1.BackgroundImage");
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.BorderStyle = BorderStyle.Fixed3D;
            pictureBox1.Location = new Point(48, 22);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(921, 57);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // returntorole
            // 
            returntorole.BackgroundImage = (Image)resources.GetObject("returntorole.BackgroundImage");
            returntorole.BackgroundImageLayout = ImageLayout.Stretch;
            returntorole.FlatStyle = FlatStyle.Popup;
            returntorole.Location = new Point(784, 37);
            returntorole.Name = "returntorole";
            returntorole.Size = new Size(164, 24);
            returntorole.TabIndex = 1;
            returntorole.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(84, 116);
            label1.Name = "label1";
            label1.Size = new Size(0, 15);
            label1.TabIndex = 2;
            label1.Click += label1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.LightGreen;
            label2.Location = new Point(74, 116);
            label2.Name = "label2";
            label2.Size = new Size(138, 21);
            label2.TabIndex = 3;
            label2.Text = "New Registration";
            // 
            // fullnametxt
            // 
            fullnametxt.BackColor = Color.FromArgb(64, 0, 0);
            fullnametxt.BorderStyle = BorderStyle.FixedSingle;
            fullnametxt.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            fullnametxt.ForeColor = Color.LightGray;
            fullnametxt.Location = new Point(84, 171);
            fullnametxt.Name = "fullnametxt";
            fullnametxt.Size = new Size(252, 29);
            fullnametxt.TabIndex = 4;
            // 
            // contactnumtxt
            // 
            contactnumtxt.BackColor = Color.FromArgb(64, 0, 0);
            contactnumtxt.BorderStyle = BorderStyle.FixedSingle;
            contactnumtxt.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            contactnumtxt.ForeColor = Color.LightGray;
            contactnumtxt.Location = new Point(84, 235);
            contactnumtxt.Name = "contactnumtxt";
            contactnumtxt.Size = new Size(252, 29);
            contactnumtxt.TabIndex = 5;
            // 
            // usernameacctxt
            // 
            usernameacctxt.BackColor = Color.FromArgb(64, 0, 0);
            usernameacctxt.BorderStyle = BorderStyle.FixedSingle;
            usernameacctxt.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            usernameacctxt.ForeColor = Color.LightGray;
            usernameacctxt.Location = new Point(439, 235);
            usernameacctxt.Name = "usernameacctxt";
            usernameacctxt.Size = new Size(252, 29);
            usernameacctxt.TabIndex = 7;
            // 
            // passcodetxt
            // 
            passcodetxt.BackColor = Color.FromArgb(64, 0, 0);
            passcodetxt.BorderStyle = BorderStyle.FixedSingle;
            passcodetxt.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            passcodetxt.ForeColor = Color.LightGray;
            passcodetxt.Location = new Point(84, 307);
            passcodetxt.Name = "passcodetxt";
            passcodetxt.Size = new Size(395, 29);
            passcodetxt.TabIndex = 8;
            passcodetxt.TextChanged += textBox5_TextChanged;
            // 
            // verifypasscodetxt
            // 
            verifypasscodetxt.BackColor = Color.FromArgb(64, 0, 0);
            verifypasscodetxt.BorderStyle = BorderStyle.FixedSingle;
            verifypasscodetxt.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            verifypasscodetxt.ForeColor = Color.LightGray;
            verifypasscodetxt.Location = new Point(84, 379);
            verifypasscodetxt.Name = "verifypasscodetxt";
            verifypasscodetxt.Size = new Size(395, 29);
            verifypasscodetxt.TabIndex = 9;
            // 
            // barangayidnotxt
            // 
            barangayidnotxt.BackColor = Color.FromArgb(64, 0, 0);
            barangayidnotxt.BorderStyle = BorderStyle.FixedSingle;
            barangayidnotxt.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            barangayidnotxt.ForeColor = Color.LightGray;
            barangayidnotxt.Location = new Point(439, 171);
            barangayidnotxt.Name = "barangayidnotxt";
            barangayidnotxt.Size = new Size(252, 29);
            barangayidnotxt.TabIndex = 6;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("Times New Roman", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.Gold;
            label3.Location = new Point(84, 149);
            label3.Name = "label3";
            label3.Size = new Size(92, 17);
            label3.TabIndex = 10;
            label3.Text = "FULL NAME";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.BackColor = Color.Transparent;
            label4.Font = new Font("Times New Roman", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label4.ForeColor = Color.Gold;
            label4.Location = new Point(439, 149);
            label4.Name = "label4";
            label4.Size = new Size(141, 17);
            label4.TabIndex = 11;
            label4.Text = "BARANGAY ID NO.";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.BackColor = Color.Transparent;
            label5.Font = new Font("Times New Roman", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label5.ForeColor = Color.Gold;
            label5.Location = new Point(84, 215);
            label5.Name = "label5";
            label5.Size = new Size(146, 17);
            label5.TabIndex = 12;
            label5.Text = "CONTACT NUMBER";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.BackColor = Color.Transparent;
            label6.Font = new Font("Times New Roman", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label6.ForeColor = Color.Gold;
            label6.Location = new Point(439, 215);
            label6.Name = "label6";
            label6.Size = new Size(198, 17);
            label6.TabIndex = 13;
            label6.Text = "USERNAME / ACCOUNT ID ";
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.BackColor = Color.Transparent;
            label7.Font = new Font("Times New Roman", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label7.ForeColor = Color.Gold;
            label7.Location = new Point(84, 287);
            label7.Name = "label7";
            label7.Size = new Size(121, 17);
            label7.TabIndex = 14;
            label7.Text = "PASSCODE / PIN";
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.BackColor = Color.Transparent;
            label8.Font = new Font("Times New Roman", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label8.ForeColor = Color.Gold;
            label8.Location = new Point(84, 359);
            label8.Name = "label8";
            label8.Size = new Size(224, 17);
            label8.TabIndex = 15;
            label8.Text = "VERIFY YOUR PASSCODE / PIN";
            // 
            // registernewaccount
            // 
            registernewaccount.BackgroundImage = (Image)resources.GetObject("registernewaccount.BackgroundImage");
            registernewaccount.BackgroundImageLayout = ImageLayout.Stretch;
            registernewaccount.FlatStyle = FlatStyle.Popup;
            registernewaccount.Location = new Point(412, 441);
            registernewaccount.Name = "registernewaccount";
            registernewaccount.Size = new Size(181, 33);
            registernewaccount.TabIndex = 16;
            registernewaccount.UseVisualStyleBackColor = true;
            // 
            // Form3
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1009, 581);
            Controls.Add(registernewaccount);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(verifypasscodetxt);
            Controls.Add(passcodetxt);
            Controls.Add(usernameacctxt);
            Controls.Add(barangayidnotxt);
            Controls.Add(contactnumtxt);
            Controls.Add(fullnametxt);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(returntorole);
            Controls.Add(pictureBox1);
            DoubleBuffered = true;
            Name = "Form3";
            Text = "Form3";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private Button returntorole;
        private Label label1;
        private Label label2;
        private TextBox fullnametxt;
        private TextBox contactnumtxt;
        private TextBox usernameacctxt;
        private TextBox passcodetxt;
        private TextBox verifypasscodetxt;
        private TextBox barangayidnotxt;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private Button registernewaccount;
    }
}