namespace BarangayQuest
{
    partial class Form24
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form24));
            panel1 = new Panel();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.Location = new Point(94, 65);
            panel1.Name = "panel1";
            panel1.Size = new Size(823, 455);
            panel1.TabIndex = 199;
            // 
            // pictureBox1
            // 
            pictureBox1.BackColor = Color.FromArgb(54, 4, 2);
            pictureBox1.BackgroundImage = (Image)resources.GetObject("pictureBox1.BackgroundImage");
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Location = new Point(68, 39);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(873, 503);
            pictureBox1.TabIndex = 198;
            pictureBox1.TabStop = false;
            // 
            // Form24
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = Properties.Resources.Copy_of_ADMIN_DASHBOARD__2_;
            ClientSize = new Size(1009, 581);
            Controls.Add(panel1);
            Controls.Add(pictureBox1);
            Name = "Form24";
            Text = "Form24";
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Panel panel1;
        private PictureBox pictureBox1;
    }
}