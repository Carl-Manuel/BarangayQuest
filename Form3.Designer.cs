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
            txtFullName = new TextBox();
            txtContactNumber = new TextBox();
            txtUsername = new TextBox();
            txtPasscode = new TextBox();
            txtVerifyPasscode = new TextBox();
            txtBarangayID = new TextBox();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            label7 = new Label();
            label8 = new Label();
            btnRegister = new Button();
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
            returntorole.Click += returntorole_Click;
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
            // txtFullName
            // 
            txtFullName.BackColor = Color.FromArgb(64, 0, 0);
            txtFullName.BorderStyle = BorderStyle.FixedSingle;
            txtFullName.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtFullName.ForeColor = Color.LightGray;
            txtFullName.Location = new Point(84, 171);
            txtFullName.Name = "txtFullName";
            txtFullName.Size = new Size(252, 29);
            txtFullName.TabIndex = 4;
            // 
            // txtContactNumber
            // 
            txtContactNumber.BackColor = Color.FromArgb(64, 0, 0);
            txtContactNumber.BorderStyle = BorderStyle.FixedSingle;
            txtContactNumber.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtContactNumber.ForeColor = Color.LightGray;
            txtContactNumber.Location = new Point(84, 235);
            txtContactNumber.Name = "txtContactNumber";
            txtContactNumber.Size = new Size(252, 29);
            txtContactNumber.TabIndex = 5;
            // 
            // txtUsername
            // 
            txtUsername.BackColor = Color.FromArgb(64, 0, 0);
            txtUsername.BorderStyle = BorderStyle.FixedSingle;
            txtUsername.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtUsername.ForeColor = Color.LightGray;
            txtUsername.Location = new Point(439, 235);
            txtUsername.Name = "txtUsername";
            txtUsername.Size = new Size(252, 29);
            txtUsername.TabIndex = 7;
            // 
            // txtPasscode
            // 
            txtPasscode.BackColor = Color.FromArgb(64, 0, 0);
            txtPasscode.BorderStyle = BorderStyle.FixedSingle;
            txtPasscode.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtPasscode.ForeColor = Color.LightGray;
            txtPasscode.Location = new Point(84, 307);
            txtPasscode.Name = "txtPasscode";
            txtPasscode.Size = new Size(395, 29);
            txtPasscode.TabIndex = 8;
            txtPasscode.UseSystemPasswordChar = true;
            txtPasscode.TextChanged += textBox5_TextChanged;
            // 
            // txtVerifyPasscode
            // 
            txtVerifyPasscode.BackColor = Color.FromArgb(64, 0, 0);
            txtVerifyPasscode.BorderStyle = BorderStyle.FixedSingle;
            txtVerifyPasscode.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtVerifyPasscode.ForeColor = Color.LightGray;
            txtVerifyPasscode.Location = new Point(84, 379);
            txtVerifyPasscode.Name = "txtVerifyPasscode";
            txtVerifyPasscode.Size = new Size(395, 29);
            txtVerifyPasscode.TabIndex = 9;
            txtVerifyPasscode.UseSystemPasswordChar = true;
            // 
            // txtBarangayID
            // 
            txtBarangayID.BackColor = Color.FromArgb(64, 0, 0);
            txtBarangayID.BorderStyle = BorderStyle.FixedSingle;
            txtBarangayID.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtBarangayID.ForeColor = Color.LightGray;
            txtBarangayID.Location = new Point(439, 171);
            txtBarangayID.Name = "txtBarangayID";
            txtBarangayID.Size = new Size(252, 29);
            txtBarangayID.TabIndex = 6;
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
            // btnRegister
            // 
            btnRegister.BackgroundImage = (Image)resources.GetObject("btnRegister.BackgroundImage");
            btnRegister.BackgroundImageLayout = ImageLayout.Stretch;
            btnRegister.FlatStyle = FlatStyle.Popup;
            btnRegister.Location = new Point(412, 441);
            btnRegister.Name = "btnRegister";
            btnRegister.Size = new Size(181, 33);
            btnRegister.TabIndex = 16;
            btnRegister.UseVisualStyleBackColor = true;
            btnRegister.Click += btnRegister_Click;
            // 
            // Form3
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1009, 581);
            Controls.Add(btnRegister);
            Controls.Add(label8);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(txtVerifyPasscode);
            Controls.Add(txtPasscode);
            Controls.Add(txtUsername);
            Controls.Add(txtBarangayID);
            Controls.Add(txtContactNumber);
            Controls.Add(txtFullName);
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
        private TextBox txtFullName;
        private TextBox txtContactNumber;
        private TextBox txtUsername;
        private TextBox txtPasscode;
        private TextBox txtVerifyPasscode;
        private TextBox txtBarangayID;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Label label7;
        private Label label8;
        private Button btnRegister;
    }
}