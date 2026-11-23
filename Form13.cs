using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form13 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";

        private TextBox? txtHeadline;
        private Control? txtAnnouncementBody;
        private ComboBox? cmbPriority;

        private Button? btnPublish;
        private Button? btnBack;

        public Form13()
        {
            InitializeComponent();
            BindAnnouncementControls();
        }

        private void BindAnnouncementControls()
        {
            var existingTextControls = GetAllControls(this)
                .Where(c => c is TextBox || c is RichTextBox)
                .OrderBy(c => c.PointToScreen(Point.Empty).Y)
                .ToList();

            if (existingTextControls.Count >= 2)
            {
                txtHeadline = existingTextControls[0] as TextBox;
                txtAnnouncementBody = existingTextControls[1];
            }
            else if (existingTextControls.Count == 1)
            {
                txtAnnouncementBody = existingTextControls[0];
            }
            else
            {
                InjectAnnouncementEditorIntoMaroonPanel();
            }

            var buttons = GetAllControls(this).OfType<Button>().ToList();
            btnPublish = FindButton(buttons, "PUBLISH");
            btnBack = FindButton(buttons, "BACK");

            if (publish != null)
            {
                publish.Cursor = Cursors.Hand;
            }

            if (btnPublish != null && !ReferenceEquals(btnPublish, publish))
            {
                btnPublish.Cursor = Cursors.Hand;
                btnPublish.Click += async (s, e) => await PublishAnnouncementAsync();
            }

            if (backbutton2 != null)
            {
                backbutton2.Cursor = Cursors.Hand;
            }

            if (btnBack != null && !ReferenceEquals(btnBack, backbutton2))
            {
                btnBack.Cursor = Cursors.Hand;
                btnBack.Click += (s, e) => NavigateTo(new Form11());
            }

            // Wire the left sidebar icons (1st = Dashboard, 2nd = Inventory, 3rd = User Management)
            var sidebarIcons = GetAllControls(this)
                .OfType<PictureBox>()
                .Where(p => p.Left < 110 && p.Top > 120 && p.Width < 90)
                .OrderBy(p => p.Top)
                .ToList();

            if (sidebarIcons.Count >= 1)
            {
                sidebarIcons[0].Cursor = Cursors.Hand;
                sidebarIcons[0].Click += (s, e) => NavigateTo(new X());
            }

            if (sidebarIcons.Count >= 2)
            {
                sidebarIcons[1].Cursor = Cursors.Hand;
                sidebarIcons[1].Click += (s, e) => NavigateTo(new Form15());
            }

            if (sidebarIcons.Count >= 3)
            {
                sidebarIcons[2].Cursor = Cursors.Hand;
                sidebarIcons[2].Click += (s, e) => NavigateTo(new Form14());
            }
        }

        private void InjectAnnouncementEditorIntoMaroonPanel()
        {
            // Find the inner dark-maroon panel in the center card
            var maroonPanel = GetAllControls(this)
                .OfType<Panel>()
                .Where(p => p.Width >= 350 && p.Height >= 110 && p.Left > 120)
                .OrderBy(p => p.Width * p.Height)
                .FirstOrDefault();

            if (maroonPanel == null) return;

            int pad = 16;
            int innerWidth = Math.Max(260, maroonPanel.ClientSize.Width - (pad * 2));

            var lblTitle = new Label
            {
                Text = "ANNOUNCEMENT HEADLINE:",
                Left = pad,
                Top = 12,
                Width = 220,
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            var lblPrio = new Label
            {
                Text = "PRIORITY:",
                Left = maroonPanel.ClientSize.Width - pad - 175,
                Top = 12,
                Width = 70,
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            cmbPriority = new ComboBox
            {
                Left = maroonPanel.ClientSize.Width - pad - 100,
                Top = 9,
                Width = 100,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(45, 10, 10),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            cmbPriority.Items.AddRange(new object[] { "Normal", "Urgent" });
            cmbPriority.SelectedIndex = 0;

            txtHeadline = new TextBox
            {
                Left = pad,
                Top = 34,
                Width = innerWidth,
                BackColor = Color.FromArgb(45, 10, 10),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                PlaceholderText = "Enter announcement title (e.g., Barangay Clean-Up Drive this Saturday)"
            };

            var lblBody = new Label
            {
                Text = "ANNOUNCEMENT MESSAGE:",
                Left = pad,
                Top = 66,
                Width = 220,
                ForeColor = Color.Gold,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.Transparent
            };

            int bodyTop = 88;
            int bodyHeight = Math.Max(60, maroonPanel.ClientSize.Height - bodyTop - 12);

            var rtbBody = new RichTextBox
            {
                Left = pad,
                Top = bodyTop,
                Width = innerWidth,
                Height = bodyHeight,
                BackColor = Color.FromArgb(45, 10, 10),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Regular)
            };

            txtAnnouncementBody = rtbBody;

            maroonPanel.Controls.AddRange(new Control[]
            {
                lblTitle, lblPrio, cmbPriority, txtHeadline, lblBody, rtbBody
            });
        }

        private async Task PublishAnnouncementAsync()
        {
            string bodyText = txtAnnouncementBody?.Text.Trim() ?? "";
            string headlineText = txtHeadline?.Text.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(bodyText))
            {
                MessageBox.Show("Please write your announcement message before clicking PUBLISH.", "Empty Announcement", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtAnnouncementBody?.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(headlineText))
            {
                headlineText = bodyText.Length <= 50 ? bodyText : bodyText.Substring(0, 47) + "...";
            }

            string priority = cmbPriority?.SelectedItem?.ToString() ?? "Normal";
            string postedBy = string.IsNullOrWhiteSpace(UserSession.FullName) ? "Super Admin" : UserSession.FullName.Trim();

            if (btnPublish != null) btnPublish.Enabled = false;
            if (publish != null) publish.Enabled = false;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS Announcements (
                        AnnouncementID INT AUTO_INCREMENT PRIMARY KEY,
                        Title VARCHAR(120) NOT NULL,
                        Message TEXT NOT NULL,
                        Priority ENUM('Normal', 'Urgent') NOT NULL DEFAULT 'Normal',
                        PostedBy VARCHAR(100) NOT NULL,
                        IsActive TINYINT(1) NOT NULL DEFAULT 1,
                        CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );";

                using (var createCmd = new MySqlCommand(createTableSql, conn))
                {
                    await createCmd.ExecuteNonQueryAsync();
                }

                string insertSql = @"
                    INSERT INTO Announcements (Title, Message, Priority, PostedBy, IsActive)
                    VALUES (@title, @msg, @prio, @by, 1);";

                using (var insCmd = new MySqlCommand(insertSql, conn))
                {
                    insCmd.Parameters.AddWithValue("@title", headlineText);
                    insCmd.Parameters.AddWithValue("@msg", bodyText);
                    insCmd.Parameters.AddWithValue("@prio", priority);
                    insCmd.Parameters.AddWithValue("@by", postedBy);
                    await insCmd.ExecuteNonQueryAsync();
                }

                MessageBox.Show("Announcement has been published to BarangayQuest!", "Announcement Published", MessageBoxButtons.OK, MessageBoxIcon.Information);

                txtHeadline?.Clear();
                if (txtAnnouncementBody is TextBoxBase tb) tb.Clear();
                if (cmbPriority != null) cmbPriority.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to publish announcement: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (btnPublish != null) btnPublish.Enabled = true;
                if (publish != null) publish.Enabled = true;
            }
        }

        private void NavigateTo(Form targetForm)
        {
            targetForm.FormClosed += (s, e) => this.Close();
            targetForm.Show();
            this.Hide();
        }

        private static Button? FindButton(IEnumerable<Button> buttons, string text)
        {
            return buttons.FirstOrDefault(b => b.Text.Trim().Equals(text, StringComparison.OrdinalIgnoreCase));
        }

        private static IEnumerable<Control> GetAllControls(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                yield return c;
                foreach (Control child in GetAllControls(c))
                    yield return child;
            }
        }

        private async void publish_Click(object sender, EventArgs e)
        {
            await PublishAnnouncementAsync();
        }

        private void backbutton2_Click(object sender, EventArgs e)
        {
            var adminDashboard = new X();
            adminDashboard.FormClosed += (s, args) => this.Close();
            adminDashboard.Show();
            this.Hide();
        }
    }
}