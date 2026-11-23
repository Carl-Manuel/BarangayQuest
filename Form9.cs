using MySqlConnector;
using System;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form9 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";

        private TextBox? txtAdminUser;
        private TextBox? txtAdminPass;
        private Button? btnAdminLogin;

        public Form9()
        {
            InitializeComponent();
            BindAdminLoginControls();
        }

        private void BindAdminLoginControls()
        {
            // Automatically locate the two TextBoxes (top = Username/Officer ID, bottom = Password)
            var textBoxes = GetAllControls(this)
                .OfType<TextBox>()
                .OrderBy(t => t.PointToScreen(Point.Empty).Y)
                .ToList();

            if (textBoxes.Count >= 2)
            {
                txtAdminUser = textBoxes[0];
                txtAdminPass = textBoxes[1];
                txtAdminPass.UseSystemPasswordChar = true;
            }

            // Locate the login button (excluding Return to Role Selection)
            btnAdminLogin = GetAllControls(this)
                .OfType<Button>()
                .FirstOrDefault(b => b != returntoroleselection);

            if (btnAdminLogin != null)
            {
                btnAdminLogin.Cursor = Cursors.Hand;
                btnAdminLogin.Click += async (s, e) => await AuthenticateAdminAsync();
                this.AcceptButton = btnAdminLogin;
            }
        }

        private async Task AuthenticateAdminAsync()
        {
            if (txtAdminUser == null || txtAdminPass == null)
            {
                MessageBox.Show("Login input controls could not be found on Form9.", "UI Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string loginId = txtAdminUser.Text.Trim();
            string rawPassword = txtAdminPass.Text;

            if (string.IsNullOrWhiteSpace(loginId) || string.IsNullOrWhiteSpace(rawPassword))
            {
                MessageBox.Show("Please enter both your Admin Username / Officer ID and Password.", "Missing Credentials", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (string.IsNullOrWhiteSpace(loginId)) txtAdminUser.Focus();
                else txtAdminPass.Focus();
                return;
            }

            if (btnAdminLogin != null) btnAdminLogin.Enabled = false;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT UserID, Username, Password, FullName, Role
                    FROM Users
                    WHERE Username = @loginId OR BarangayIDNumber = @loginId
                    LIMIT 1;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@loginId", loginId);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    MessageBox.Show("Invalid Super Admin Username / ID or Password.", "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAdminPass.Clear();
                    txtAdminPass.Focus();
                    return;
                }

                string role = reader["Role"]?.ToString() ?? "";
                string storedPassword = reader["Password"]?.ToString() ?? "";
                string fullName = reader["FullName"]?.ToString() ?? loginId;

                // Strictly allow ONLY SuperAdmin (blocks LGUDispatcher and CitizenWorker)
                if (!role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Access Denied. This portal is strictly restricted to Super Admins only.", "Unauthorized Role", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    txtAdminPass.Clear();
                    txtAdminPass.Focus();
                    return;
                }

                // Support both SHA-256 hex hashes and plain-text test entries
                string hashedInput = HashPassword(rawPassword);
                bool passwordValid =
                    string.Equals(storedPassword, hashedInput, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(storedPassword, rawPassword, StringComparison.Ordinal);

                if (!passwordValid)
                {
                    MessageBox.Show("Invalid Super Admin Username / ID or Password.", "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtAdminPass.Clear();
                    txtAdminPass.Focus();
                    return;
                }

                UserSession.FullName = fullName;

                MessageBox.Show($"Welcome, Super Admin {fullName}!", "Access Granted", MessageBoxButtons.OK, MessageBoxIcon.Information);

                var adminDashboard = new X();
                adminDashboard.FormClosed += (s, args) => this.Close();
                adminDashboard.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (btnAdminLogin != null) btnAdminLogin.Enabled = true;
            }
        }

        private static string HashPassword(string rawPassword)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawPassword));
            return Convert.ToHexString(bytes);
        }

        private static System.Collections.Generic.IEnumerable<Control> GetAllControls(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                yield return c;
                foreach (Control child in GetAllControls(c))
                    yield return child;
            }
        }

        private void returntoroleselection_Click(object sender, EventArgs e)
        {
            Form2 Form2 = new Form2();
            Form2.FormClosed += (s, args) => this.Close();
            Form2.Show();
            this.Hide();
        }

        private void label4_Click(object sender, EventArgs e)
        {
        }

        private void accessverificationdesk_Click(object sender, EventArgs e)
        {

        }
    }
}