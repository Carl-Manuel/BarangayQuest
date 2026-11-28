using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class X : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private Label? lblTotalUsersCount;
        private Label? lblActiveQuestsCount;

        public X()
        {
            InitializeComponent();
            InitializeAdminDashboard();
        }

        private void InitializeAdminDashboard()
        {
            var allLabels = GetAllControls(this).OfType<Label>().ToList();

            // Locate the larger value labels inside the TOTAL USERS and ACTIVE QUESTS boxes
            lblTotalUsersCount = allLabels
                .Where(l => l.Text.Trim().Equals("TOTAL USERS", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.Font.Size)
                .ThenByDescending(l => l.Top)
                .FirstOrDefault();

            lblActiveQuestsCount = allLabels
                .Where(l => l.Text.Trim().Equals("ACTIVE QUESTS", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(l => l.Font.Size)
                .ThenByDescending(l => l.Top)
                .FirstOrDefault();

            // Forward clicks on child labels/icons inside each card to its Designer click handler
            ForwardCardChildrenToHandler("INVENTORY MANAGEMENT", () => inventorymanagement_Click(this, EventArgs.Empty));
            ForwardCardChildrenToHandler("ADMIN PROFILE & SYSTEM CONTROLS", () => adminprofile_Click(this, EventArgs.Empty));
            ForwardCardChildrenToHandler("USER MANAGEMENT", () => usermanagement_Click(this, EventArgs.Empty));
            ForwardCardChildrenToHandler("QUEST & MAP DISPATCHER", () => questmapdispatch_Click(this, EventArgs.Empty));
        }

        private void ForwardCardChildrenToHandler(string cardTitleText, Action triggerHandler)
        {
            var allControls = GetAllControls(this).ToList();

            var titleLabel = allControls
                .OfType<Label>()
                .FirstOrDefault(l => NormalizeText(l.Text).Equals(cardTitleText, StringComparison.OrdinalIgnoreCase));

            if (titleLabel == null) return;

            if (titleLabel.Parent is Panel pnl && pnl.Width < this.ClientSize.Width - 100)
            {
                foreach (Control child in GetAllControls(pnl))
                {
                    child.Cursor = Cursors.Hand;
                    child.Click += (s, e) => triggerHandler();
                }
                return;
            }

            Point titleCenter = titleLabel.PointToScreen(new Point(titleLabel.Width / 2, titleLabel.Height / 2));
            var cardBackground = allControls
                .OfType<PictureBox>()
                .Where(pb => pb.Width >= 180 && pb.Width < this.ClientSize.Width - 100 && pb.Height >= 70)
                .FirstOrDefault(pb =>
                {
                    Rectangle pbRect = new Rectangle(pb.PointToScreen(Point.Empty), pb.Size);
                    return pbRect.Contains(titleCenter);
                });

            if (cardBackground != null)
            {
                Rectangle cardRect = new Rectangle(cardBackground.PointToScreen(Point.Empty), cardBackground.Size);
                foreach (Control ctrl in allControls)
                {
                    if (ctrl == this || ctrl is Form || ctrl == cardBackground) continue;
                    if (ctrl.Width >= this.ClientSize.Width - 100) continue;

                    Rectangle ctrlRect = new Rectangle(ctrl.PointToScreen(Point.Empty), ctrl.Size);
                    if (cardRect.IntersectsWith(ctrlRect))
                    {
                        ctrl.Cursor = Cursors.Hand;
                        ctrl.Click += (s, e) => triggerHandler();
                    }
                }
            }
        }

        private static string NormalizeText(string text)
        {
            return string.Join(" ", text
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private async void X_Load(object sender, EventArgs e)
        {
            await LoadDashboardMetricsAsync();
        }

        private async Task LoadDashboardMetricsAsync()
        {
            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                using (var userCmd = new MySqlCommand("SELECT COUNT(*) FROM Users;", conn))
                {
                    int totalUsers = Convert.ToInt32(await userCmd.ExecuteScalarAsync());
                    if (lblTotalUsersCount != null)
                        lblTotalUsersCount.Text = totalUsers.ToString("N0");
                }

                using (var questCmd = new MySqlCommand("SELECT COUNT(*) FROM Quests WHERE Status IN ('Upcoming', 'Active');", conn))
                {
                    int activeQuests = Convert.ToInt32(await questCmd.ExecuteScalarAsync());
                    if (lblActiveQuestsCount != null)
                        lblActiveQuestsCount.Text = activeQuests.ToString("N0");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load dashboard metrics: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void adminprofile_Click(object sender, EventArgs e)
        {
            Form11 form11 = new Form11();
            form11.Show();
            this.Hide();
        }

        private void questmapdispatch_Click(object sender, EventArgs e)
        {
            Form24 form24 = new Form24();
            form24.Show();
            this.Hide();
        }

        private void usermanagement_Click(object sender, EventArgs e)
        {
            Form14 form14 = new Form14();
            form14.Show();
            this.Hide();
        }

        private void inventorymanagement_Click(object sender, EventArgs e)
        {
            Form15 form15 = new Form15();
            form15.Show();
            this.Hide();
        }

        private void codeverifier_Click(object sender, EventArgs e)
        {
            {
                Form29 form29 = new Form29();
                form29.Show();
                this.Hide();
            }
        }
    }
}