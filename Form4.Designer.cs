namespace BarangayQuest
{
    partial class Form4
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form4));
            pictureBox1 = new PictureBox();
            returntorole2 = new Button();
            pictureBox2 = new PictureBox();
            pictureBox3 = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            citizenusernametxt = new TextBox();
            label6 = new Label();
            label3 = new Label();
            passwordtxt = new TextBox();
            registernewacc = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = (Image)resources.GetObject("pictureBox1.BackgroundImage");
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Location = new Point(255, 97);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(500, 329);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // returntorole2
            // 
            returntorole2.BackgroundImage = (Image)resources.GetObject("returntorole2.BackgroundImage");
            returntorole2.BackgroundImageLayout = ImageLayout.Stretch;
            returntorole2.FlatStyle = FlatStyle.Popup;
            returntorole2.Location = new Point(788, 37);
            returntorole2.Name = "returntorole2";
            returntorole2.Size = new Size(164, 24);
            returntorole2.TabIndex = 3;
            returntorole2.UseVisualStyleBackColor = true;
            returntorole2.Click += button1_Click;
            // 
            // pictureBox2
            // 
            pictureBox2.BackColor = Color.FromArgb(128, 64, 64);
            pictureBox2.BackgroundImage = (Image)resources.GetObject("pictureBox2.BackgroundImage");
            pictureBox2.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox2.BorderStyle = BorderStyle.Fixed3D;
            pictureBox2.Location = new Point(48, 21);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(921, 57);
            pictureBox2.TabIndex = 2;
            pictureBox2.TabStop = false;
            // 
            // pictureBox3
            // 
            pictureBox3.BackColor = Color.FromArgb(45, 21, 0);
            pictureBox3.BackgroundImage = (Image)resources.GetObject("pictureBox3.BackgroundImage");
            pictureBox3.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox3.Location = new Point(456, 107);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(100, 57);
            pictureBox3.TabIndex = 4;
            pictureBox3.TabStop = false;
            pictureBox3.Click += pictureBox3_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.FromArgb(45, 21, 0);
            label1.Font = new Font("Times New Roman", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.LightGreen;
            label1.Location = new Point(420, 167);
            label1.Name = "label1";
            label1.Size = new Size(183, 23);
            label1.TabIndex = 5;
            label1.Text = "CITIZEN WORKER";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.FromArgb(45, 21, 0);
            label2.Font = new Font("Times New Roman", 5.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.LightGreen;
            label2.Location = new Point(469, 190);
            label2.Name = "label2";
            label2.Size = new Size(87, 8);
            label2.TabIndex = 6;
            label2.Text = "CITIZEN WORKERS ONLY";
            // 
            // citizenusernametxt
            // 
            citizenusernametxt.BackColor = Color.FromArgb(64, 0, 0);
            citizenusernametxt.BorderStyle = BorderStyle.FixedSingle;
            citizenusernametxt.Location = new Point(317, 234);
            citizenusernametxt.Name = "citizenusernametxt";
            citizenusernametxt.Size = new Size(373, 23);
            citizenusernametxt.TabIndex = 7;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.BackColor = Color.FromArgb(45, 21, 0);
            label6.Font = new Font("Times New Roman", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label6.ForeColor = Color.Gold;
            label6.Location = new Point(317, 216);
            label6.Name = "label6";
            label6.Size = new Size(202, 15);
            label6.TabIndex = 14;
            label6.Text = "CITIZEN USERNAME / WORKER ID";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.FromArgb(45, 21, 0);
            label3.Font = new Font("Times New Roman", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.Gold;
            label3.Location = new Point(317, 282);
            label3.Name = "label3";
            label3.Size = new Size(71, 15);
            label3.TabIndex = 15;
            label3.Text = "PASSWORD";
            // 
            // passwordtxt
            // 
            passwordtxt.BackColor = Color.FromArgb(64, 0, 0);
            passwordtxt.BorderStyle = BorderStyle.FixedSingle;
            passwordtxt.Location = new Point(317, 300);
            passwordtxt.Name = "passwordtxt";
            passwordtxt.Size = new Size(373, 23);
            passwordtxt.TabIndex = 16;
            // 
            // registernewacc
            // 
            registernewacc.BackColor = SystemColors.Control;
            registernewacc.BackgroundImage = (Image)resources.GetObject("registernewacc.BackgroundImage");
            registernewacc.BackgroundImageLayout = ImageLayout.Stretch;
            registernewacc.FlatStyle = FlatStyle.Popup;
            registernewacc.Location = new Point(436, 383);
            registernewacc.Name = "registernewacc";
            registernewacc.Size = new Size(140, 26);
            registernewacc.TabIndex = 17;
            registernewacc.UseVisualStyleBackColor = false;
            // 
            // Form4
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1009, 581);
            Controls.Add(registernewacc);
            Controls.Add(passwordtxt);
            Controls.Add(label3);
            Controls.Add(label6);
            Controls.Add(citizenusernametxt);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(pictureBox3);
            Controls.Add(returntorole2);
            Controls.Add(pictureBox2);
            Controls.Add(pictureBox1);
            DoubleBuffered = true;
            Name = "Form4";
            Text = "Form4";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private Button returntorole2;
        private PictureBox pictureBox2;
        private PictureBox pictureBox3;
        private Label label1;
        private Label label2;
        private TextBox citizenusernametxt;
        private Label label6;
        private Label label3;
        private TextBox passwordtxt;
        private Button registernewacc;
    }
}