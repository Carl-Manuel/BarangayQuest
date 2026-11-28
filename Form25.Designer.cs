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
            LABEL.Location = new Point(468, 58);
            LABEL.Name = "LABEL";
            LABEL.Size = new Size(224, 39);
            LABEL.TabIndex = 59;
            LABEL.Text = "SUSPENSION";
            // 
            // pictureBox3
            // 
            pictureBox3.BackColor = Color.Transparent;
            pictureBox3.BackgroundImage = (Image)resources.GetObject("pictureBox3.BackgroundImage");
            pictureBox3.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox3.Location = new Point(134, 125);
            pictureBox3.Margin = new Padding(3, 4, 3, 4);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(896, 484);
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
            announcementtxt.Location = new Point(224, 225);
            announcementtxt.Margin = new Padding(3, 4, 3, 4);
            announcementtxt.Name = "announcementtxt";
            announcementtxt.Size = new Size(717, 35);
            announcementtxt.TabIndex = 88;
            // 
            // pictureBox5
            // 
            pictureBox5.BackColor = Color.FromArgb(60, 40, 0);
            pictureBox5.BackgroundImage = (Image)resources.GetObject("pictureBox5.BackgroundImage");
            pictureBox5.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox5.Location = new Point(185, 147);
            pictureBox5.Margin = new Padding(3, 4, 3, 4);
            pictureBox5.Name = "pictureBox5";
            pictureBox5.Size = new Size(801, 341);
            pictureBox5.TabIndex = 87;
            pictureBox5.TabStop = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.FromArgb(64, 0, 0);
            label3.Font = new Font("Times New Roman", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.LightGray;
            label3.Location = new Point(212, 189);
            label3.Name = "label3";
            label3.Size = new Size(203, 19);
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
            suspendacc.Location = new Point(495, 508);
            suspendacc.Margin = new Padding(3, 4, 3, 4);
            suspendacc.Name = "suspendacc";
            suspendacc.Size = new Size(178, 59);
            suspendacc.TabIndex = 91;
            suspendacc.UseVisualStyleBackColor = false;
            // 
            // backbutton3
            // 
            backbutton3.BackColor = Color.Transparent;
            backbutton3.BackgroundImage = (Image)resources.GetObject("backbutton3.BackgroundImage");
            backbutton3.BackgroundImageLayout = ImageLayout.Stretch;
            backbutton3.FlatStyle = FlatStyle.Flat;
            backbutton3.ForeColor = Color.Black;
            backbutton3.Location = new Point(943, 666);
            backbutton3.Margin = new Padding(3, 4, 3, 4);
            backbutton3.Name = "backbutton3";
            backbutton3.Size = new Size(152, 76);
            backbutton3.TabIndex = 90;
            backbutton3.UseVisualStyleBackColor = false;
            // 
            // Form25
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1153, 775);
            Controls.Add(suspendacc);
            Controls.Add(backbutton3);
            Controls.Add(label3);
            Controls.Add(announcementtxt);
            Controls.Add(pictureBox5);
            Controls.Add(pictureBox3);
            Controls.Add(LABEL);
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