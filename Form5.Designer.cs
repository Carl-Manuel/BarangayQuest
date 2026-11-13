namespace BarangayQuest
{
    partial class Form5
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form5));
            pictureBox1 = new PictureBox();
            label1 = new Label();
            DISPLAYPOINTS = new Label();
            pictureBox2 = new PictureBox();
            activequest = new Label();
            activequestdescription = new Label();
            label2 = new Label();
            pointsreward = new Label();
            supplyreward = new Label();
            label3 = new Label();
            toolsredeemed = new Label();
            citizenprofile = new Button();
            questmap = new Button();
            questlist = new Button();
            pointshop = new Button();
            pictureBox3 = new PictureBox();
            pictureBox4 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).BeginInit();
            SuspendLayout();
            // 
            // pictureBox1
            // 
            pictureBox1.BackgroundImage = (Image)resources.GetObject("pictureBox1.BackgroundImage");
            pictureBox1.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox1.Location = new Point(33, 25);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(939, 64);
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Times New Roman", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.YellowGreen;
            label1.Location = new Point(477, 119);
            label1.Name = "label1";
            label1.Size = new Size(120, 21);
            label1.TabIndex = 1;
            label1.Text = "Reward Points";
            // 
            // DISPLAYPOINTS
            // 
            DISPLAYPOINTS.AutoSize = true;
            DISPLAYPOINTS.BackColor = Color.Transparent;
            DISPLAYPOINTS.Font = new Font("Nirmala Text", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            DISPLAYPOINTS.ForeColor = Color.Moccasin;
            DISPLAYPOINTS.Location = new Point(625, 147);
            DISPLAYPOINTS.Name = "DISPLAYPOINTS";
            DISPLAYPOINTS.Size = new Size(190, 32);
            DISPLAYPOINTS.TabIndex = 2;
            DISPLAYPOINTS.Text = "POINTS EARNED";
            // 
            // pictureBox2
            // 
            pictureBox2.BackgroundImage = (Image)resources.GetObject("pictureBox2.BackgroundImage");
            pictureBox2.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox2.Location = new Point(477, 227);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(132, 21);
            pictureBox2.TabIndex = 3;
            pictureBox2.TabStop = false;
            // 
            // activequest
            // 
            activequest.AutoSize = true;
            activequest.BackColor = Color.SaddleBrown;
            activequest.FlatStyle = FlatStyle.Popup;
            activequest.Font = new Font("Times New Roman", 21.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            activequest.Location = new Point(574, 266);
            activequest.Name = "activequest";
            activequest.Size = new Size(310, 33);
            activequest.TabIndex = 4;
            activequest.Text = "ACTIVE QUEST SHOW";
            // 
            // activequestdescription
            // 
            activequestdescription.AutoSize = true;
            activequestdescription.BackColor = Color.Transparent;
            activequestdescription.ForeColor = Color.DarkGray;
            activequestdescription.Location = new Point(575, 310);
            activequestdescription.Name = "activequestdescription";
            activequestdescription.Size = new Size(309, 15);
            activequestdescription.TabIndex = 5;
            activequestdescription.Text = "THIS SHOWS THE DESCRIPTION OF THE ACTICVE QUEST";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = Color.FromArgb(64, 0, 0);
            label2.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.ForeColor = Color.LightGray;
            label2.Location = new Point(492, 351);
            label2.Name = "label2";
            label2.Size = new Size(79, 13);
            label2.TabIndex = 6;
            label2.Text = "TASK REWARD";
            // 
            // pointsreward
            // 
            pointsreward.AutoSize = true;
            pointsreward.BackColor = Color.FromArgb(64, 0, 0);
            pointsreward.Font = new Font("Nirmala Text", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            pointsreward.ForeColor = Color.Moccasin;
            pointsreward.Location = new Point(541, 375);
            pointsreward.Name = "pointsreward";
            pointsreward.Size = new Size(155, 25);
            pointsreward.TabIndex = 7;
            pointsreward.Text = "REWARD POINTS";
            // 
            // supplyreward
            // 
            supplyreward.AutoSize = true;
            supplyreward.BackColor = Color.FromArgb(64, 0, 0);
            supplyreward.Font = new Font("Nirmala Text", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            supplyreward.ForeColor = Color.Moccasin;
            supplyreward.Location = new Point(773, 375);
            supplyreward.Name = "supplyreward";
            supplyreward.Size = new Size(155, 25);
            supplyreward.TabIndex = 8;
            supplyreward.Text = "REWARD SUPPLY";
            supplyreward.Click += supplyreward_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = Color.FromArgb(64, 0, 0);
            label3.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.LightGray;
            label3.Location = new Point(541, 428);
            label3.Name = "label3";
            label3.Size = new Size(103, 13);
            label3.TabIndex = 9;
            label3.Text = "TOOL/s REDEEMED";
            // 
            // toolsredeemed
            // 
            toolsredeemed.AutoSize = true;
            toolsredeemed.BackColor = Color.FromArgb(64, 0, 0);
            toolsredeemed.Font = new Font("Nirmala Text", 14.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            toolsredeemed.ForeColor = Color.Moccasin;
            toolsredeemed.Location = new Point(637, 441);
            toolsredeemed.Name = "toolsredeemed";
            toolsredeemed.Size = new Size(167, 25);
            toolsredeemed.TabIndex = 10;
            toolsredeemed.Text = "REDEEMED TOOLS";
            // 
            // citizenprofile
            // 
            citizenprofile.BackColor = Color.Transparent;
            citizenprofile.BackgroundImage = (Image)resources.GetObject("citizenprofile.BackgroundImage");
            citizenprofile.BackgroundImageLayout = ImageLayout.Stretch;
            citizenprofile.FlatStyle = FlatStyle.Flat;
            citizenprofile.ForeColor = SystemColors.ButtonHighlight;
            citizenprofile.Location = new Point(33, 166);
            citizenprofile.Name = "citizenprofile";
            citizenprofile.Size = new Size(402, 66);
            citizenprofile.TabIndex = 11;
            citizenprofile.UseVisualStyleBackColor = false;
            // 
            // questmap
            // 
            questmap.BackColor = Color.Transparent;
            questmap.BackgroundImage = (Image)resources.GetObject("questmap.BackgroundImage");
            questmap.BackgroundImageLayout = ImageLayout.Stretch;
            questmap.FlatStyle = FlatStyle.Flat;
            questmap.ForeColor = SystemColors.ButtonHighlight;
            questmap.Location = new Point(33, 238);
            questmap.Name = "questmap";
            questmap.Size = new Size(402, 66);
            questmap.TabIndex = 12;
            questmap.UseVisualStyleBackColor = false;
            // 
            // questlist
            // 
            questlist.BackColor = Color.Transparent;
            questlist.BackgroundImage = (Image)resources.GetObject("questlist.BackgroundImage");
            questlist.BackgroundImageLayout = ImageLayout.Stretch;
            questlist.FlatStyle = FlatStyle.Flat;
            questlist.ForeColor = SystemColors.ButtonHighlight;
            questlist.Location = new Point(33, 310);
            questlist.Name = "questlist";
            questlist.Size = new Size(402, 66);
            questlist.TabIndex = 13;
            questlist.UseVisualStyleBackColor = false;
            // 
            // pointshop
            // 
            pointshop.BackColor = Color.Transparent;
            pointshop.BackgroundImage = (Image)resources.GetObject("pointshop.BackgroundImage");
            pointshop.BackgroundImageLayout = ImageLayout.Stretch;
            pointshop.FlatStyle = FlatStyle.Flat;
            pointshop.ForeColor = SystemColors.ButtonHighlight;
            pointshop.Location = new Point(33, 382);
            pointshop.Name = "pointshop";
            pointshop.Size = new Size(402, 66);
            pointshop.TabIndex = 14;
            pointshop.UseVisualStyleBackColor = false;
            // 
            // pictureBox3
            // 
            pictureBox3.BackgroundImage = (Image)resources.GetObject("pictureBox3.BackgroundImage");
            pictureBox3.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox3.Location = new Point(477, 339);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(495, 70);
            pictureBox3.TabIndex = 15;
            pictureBox3.TabStop = false;
            pictureBox3.Click += pictureBox3_Click;
            // 
            // pictureBox4
            // 
            pictureBox4.BackgroundImage = (Image)resources.GetObject("pictureBox4.BackgroundImage");
            pictureBox4.BackgroundImageLayout = ImageLayout.Stretch;
            pictureBox4.Location = new Point(525, 421);
            pictureBox4.Name = "pictureBox4";
            pictureBox4.Size = new Size(392, 51);
            pictureBox4.TabIndex = 16;
            pictureBox4.TabStop = false;
            // 
            // Form5
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(1009, 581);
            Controls.Add(pointshop);
            Controls.Add(questlist);
            Controls.Add(questmap);
            Controls.Add(citizenprofile);
            Controls.Add(toolsredeemed);
            Controls.Add(label3);
            Controls.Add(supplyreward);
            Controls.Add(pointsreward);
            Controls.Add(label2);
            Controls.Add(activequestdescription);
            Controls.Add(activequest);
            Controls.Add(pictureBox2);
            Controls.Add(DISPLAYPOINTS);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            Controls.Add(pictureBox3);
            Controls.Add(pictureBox4);
            DoubleBuffered = true;
            Name = "Form5";
            Text = "Form5";
            Load += Form5_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox4).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pictureBox1;
        private Label label1;
        private Label DISPLAYPOINTS;
        private PictureBox pictureBox2;
        private Label activequest;
        private Label activequestdescription;
        private Label label2;
        private Label pointsreward;
        private Label supplyreward;
        private Label label3;
        private Label toolsredeemed;
        private Button citizenprofile;
        private Button questmap;
        private Button questlist;
        private Button pointshop;
        private PictureBox pictureBox3;
        private PictureBox pictureBox4;
    }
}