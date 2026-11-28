namespace BarangayQuest
{
    partial class Form25
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form25));
            LABEL = new Label();
            pictureBox3 = new PictureBox();
            announcementtxt = new TextBox();
            pictureBox5 = new PictureBox();
            label3 = new Label();
            suspendacc = new Button();
            backbutton3 = new Button();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox5).BeginInit();
            SuspendLayout();
            // 
            // LABEL
            // 
            LABEL.AutoSize = true;
            LABEL.BackColor = Color.FromArgb(60, 40, 0);
            LABEL.FlatStyle = FlatStyle.Flat;
            LABEL.Font = new Font("Times New Roman", 19.8000011F, FontStyle.Regular, GraphicsUnit.Point, 0);
            LABEL.ForeColor = Color.FromArgb(255, 224, 96);
            LABEL.Location = new Point(410, 44);
            LABEL.Name = "LABEL";
            LABEL.Size = new Size(175, 31);
            LABEL.TabIndex = 59;
            LABEL.Text = "SUSPENSION";
            // 
            // pictureBox3
            // 
            pictureBox3.BackColor = Color.Transparent;
            pictureBox3.BackgroundImage = (Image)resources.GetObject("pictureBox3.BackgroundImage");
            pictureBox3.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox3.Location = new Point(117, 94);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(784, 363);
            pictureBox3.TabIndex = 60;
            pictureBox3.TabStop = false;
            pictureBox3.Click += pictureBox3_Click;
            // 
            // announcementtxt
            // 
            announcementtxt.BackColor = Color.FromArgb(64, 0, 0);
            announcementtxt.BorderStyle = BorderStyle.None;
            announcementtxt.Font = new Font("Segoe UI", 15.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            announcementtxt.ForeColor = Color.LightGray;
            announcementtxt.Location = new Point(196, 169);
            announcementtxt.Name = "announcementtxt";
            announcementtxt.Size = new Size(627, 28);
            announcementtxt.TabIndex = 88;
            // 
            // pictureBox5
            // 
            pictureBox5.BackColor = Color.FromArgb(60, 40, 0);
            pictureBox5.BackgroundImage = (Image)resources.GetObject("pictureBox5.BackgroundImage");
            pictureBox5.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox5.Location = new Point(162, 110);
            pictureBox5.Name = "pictureBox5";
            pictureBox5.Size = new Size(701, 256);
            pictureBox5.TabIndex = 87;
            pictureBox5.TabStop = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.FromArgb(64, 0, 0);
            label3.Font = new Font("Times New Roman", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.LightGray;
            label3.Location = new Point(186, 142);
            label3.Name = "label3";
            label3.Size = new Size(154, 15);
            label3.TabIndex = 89;
            label3.Text = "REASON OF SUSPENSION";
            // 
            // suspendacc
            // 
            suspendacc.BackColor = Color.FromArgb(60, 40, 0);
            suspendacc.BackgroundImage = (Image)resources.GetObject("suspendacc.BackgroundImage");
            suspendacc.BackgroundImageLayout = ImageLayout.Stretch;
            suspendacc.FlatStyle = FlatStyle.Popup;
            suspendacc.ForeColor = Color.FromArgb(60, 40, 0);
            suspendacc.Location = new Point(433, 381);
            suspendacc.Name = "suspendacc";
            suspendacc.Size = new Size(156, 44);
            suspendacc.TabIndex = 91;
            suspendacc.UseVisualStyleBackColor = false;
            suspendacc.Click += suspendacc_Click;
            // 
            // backbutton3
            // 
            backbutton3.BackColor = Color.Transparent;
            backbutton3.BackgroundImage = (Image)resources.GetObject("backbutton3.BackgroundImage");
            backbutton3.BackgroundImageLayout = ImageLayout.Stretch;
            backbutton3.FlatStyle = FlatStyle.Flat;
            backbutton3.ForeColor = Color.Black;
            backbutton3.Location = new Point(825, 500);
            backbutton3.Name = "backbutton3";
            backbutton3.Size = new Size(133, 57);
            backbutton3.TabIndex = 90;
            backbutton3.UseVisualStyleBackColor = false;
            // 
            // Form25
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1009, 581);
            Controls.Add(suspendacc);
            Controls.Add(backbutton3);
            Controls.Add(label3);
            Controls.Add(announcementtxt);
            Controls.Add(pictureBox5);
            Controls.Add(pictureBox3);
            Controls.Add(LABEL);
            Margin = new Padding(3, 2, 3, 2);
            Name = "Form25";
            Text = "Form25";
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox5).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label LABEL;
        private PictureBox pictureBox3;
        private TextBox announcementtxt;
        private PictureBox pictureBox5;
        private Label label3;
        private Button suspendacc;
        private Button backbutton3;
    }
}