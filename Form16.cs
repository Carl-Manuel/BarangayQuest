using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form16 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private TextBox? _txtDispatcherId;
        private TextBox? _txtPassword;

        public Form16()
        {
            InitializeComponent();
            BindPortalControls();
            _resizer = new FormResizer(this);
        }

        private void BindPortalControls()
        {
            var textBoxes = GetAllControls(this)
                .OfType<TextBox>()
                .OrderBy(t => t.PointToScreen(Point.Empty).Y)
                .ToList();

            if (textBoxes.Count >= 1)
            {
                _txtDispatcherId = textBoxes[0];
                _txtDispatcherId.ForeColor = Color.White;
                _txtDispatcherId.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
                _txtDispatcherId.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        _txtPassword?.Focus();
                    }
                };
            }

            if (textBoxes.Count >= 2)
            {
                _txtPassword = textBoxes[1];
                _txtPassword.ForeColor = Color.White;
                _txtPassword.Font = new Font("Segoe UI", 10.5f, FontStyle.Regular);
                _txtPassword.UseSystemPasswordChar = true;
                _txtPassword.KeyDown += async (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        await AuthenticateDispatcherAsync();
                    }
                };
            }

            if (accessverificationdesk2 != null)
            {
                accessverificationdesk2.Cursor = Cursors.Hand;
            }
        }

        private async void accessverificationdesk2_Click(object sender, EventArgs e)
        {
            await AuthenticateDispatcherAsync();
        }

        private async Task AuthenticateDispatcherAsync()
        {
            string identifier = _txtDispatcherId?.Text.Trim() ?? "";
            string password = _txtPassword?.Text ?? "";

            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show(
                    "Please enter your Dispatcher Username / Officer ID and Password.",
                    "Missing Credentials",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                if (string.IsNullOrWhiteSpace(identifier)) _txtDispatcherId?.Focus();
                else _txtPassword?.Focus();
                return;
            }

            if (accessverificationdesk2 != null) accessverificationdesk2.Enabled = false;

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT 
                        u.UserID,
                        u.BarangayIDNumber,
                        u.Role,
                        u.Username,
                        u.Password,
                        u.FullName,
                        u.Zone,
                        d.DispatcherID,
                        d.DepartmentRole,
                        d.AssignedZone
                    FROM Users u
                    LEFT JOIN LGU_Dispatcher d ON u.UserID = d.UserID
                    WHERE u.Username = @id OR u.BarangayIDNumber = @id
                    LIMIT 1;";

                int userId = 0;
                string barangayId = "";
                string role = "";
                string username = "";
                string storedPassword = "";
                string fullName = "";
                string userZone = "";
                int? dispatcherId = null;
                string deptRole = "";
                string assignedZone = "";

                using (var cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@id", identifier);
                    using var reader = await cmd.ExecuteReaderAsync();

                    if (!await reader.ReadAsync())
                    {
                        MessageBox.Show(
                            "No dispatcher account matched that Username or Officer ID.",
                            "Access Denied",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }

                    userId = Convert.ToInt32(reader["UserID"]);
                    barangayId = reader["BarangayIDNumber"]?.ToString() ?? "";
                    role = reader["Role"]?.ToString() ?? "";
                    username = reader["Username"]?.ToString() ?? "";
                    storedPassword = reader["Password"]?.ToString() ?? "";
                    fullName = reader["FullName"]?.ToString() ?? "";
                    userZone = reader["Zone"]?.ToString() ?? "Zone 1";

                    if (reader["DispatcherID"] != DBNull.Value)
                        dispatcherId = Convert.ToInt32(reader["DispatcherID"]);
                    deptRole = reader["DepartmentRole"]?.ToString() ?? "Operations Officer";
                    assignedZone = reader["AssignedZone"]?.ToString() ?? userZone;
                }

                if (!role.Equals("LGUDispatcher", StringComparison.OrdinalIgnoreCase) &&
                    !role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show(
                        "Access Denied. This gateway is restricted to Authorized Barangay Dispatchers and Officials only.",
                        "Unauthorized Role",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Stop);
                    return;
                }

                if (!VerifyPassword(password, storedPassword))
                {
                    MessageBox.Show(
                        "Incorrect password. Please verify your credentials and try again.",
                        "Authentication Failed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    _txtPassword?.Clear();
                    _txtPassword?.Focus();
                    return;
                }

                if (dispatcherId == null)
                {
                    string ensureDispSql = @"
                        INSERT INTO LGU_Dispatcher (UserID, DepartmentRole, AssignedZone)
                        VALUES (@uid, @dept, @zone);
                        SELECT LAST_INSERT_ID();";
                    using var insCmd = new MySqlCommand(ensureDispSql, conn);
                    insCmd.Parameters.AddWithValue("@uid", userId);
                    insCmd.Parameters.AddWithValue("@dept", "Operations Officer");
                    insCmd.Parameters.AddWithValue("@zone", string.IsNullOrWhiteSpace(userZone) ? "Zone 1" : userZone);
                    dispatcherId = Convert.ToInt32(await insCmd.ExecuteScalarAsync());
                }

                DispatcherSession.DispatcherID = dispatcherId.Value;
                DispatcherSession.UserID = userId;
                DispatcherSession.BarangayIDNumber = barangayId;
                DispatcherSession.Username = username;
                DispatcherSession.FullName = fullName;
                DispatcherSession.DepartmentRole = deptRole;
                DispatcherSession.AssignedZone = assignedZone;

                UserSession.FullName = fullName;

                MessageBox.Show(
                    $"Welcome, {fullName}!\nRole: {deptRole} ({assignedZone})",
                    "Dispatcher Gateway Authorized",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Replace Form17 with your LGU Dispatcher Verification Desk form if named differently
                var controlCenter = new Form17();
                controlCenter.FormClosed += (s, args) => this.Close();
                controlCenter.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to connect to the Dispatcher Gateway database:\n{ex.Message}",
                    "Database Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                if (accessverificationdesk2 != null) accessverificationdesk2.Enabled = true;
            }
        }

        private static bool VerifyPassword(string plainPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash)) return false;

            var helperType = typeof(Form16).Assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Equals("PasswordHelper", StringComparison.Ordinal));
            if (helperType != null)
            {
                var verifyMethod = helperType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.ReturnType == typeof(bool) &&
                        m.GetParameters().Length == 2 &&
                        m.GetParameters()[0].ParameterType == typeof(string) &&
                        m.GetParameters()[1].ParameterType == typeof(string));

                if (verifyMethod != null && verifyMethod.Invoke(null, new object[] { plainPassword, storedHash }) is bool isValid && isValid)
                {
                    return true;
                }
            }

            string sha256Hex = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainPassword)));
            if (storedHash.Equals(sha256Hex, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return storedHash == plainPassword;
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
    }

    public static class DispatcherSession
    {
        public static int DispatcherID { get; set; }
        public static int UserID { get; set; }
        public static string BarangayIDNumber { get; set; } = "";
        public static string Username { get; set; } = "";
        public static string FullName { get; set; } = "";
        public static string DepartmentRole { get; set; } = "";
        public static string AssignedZone { get; set; } = "";

        public static void Clear()
        {
            DispatcherID = 0;
            UserID = 0;
            BarangayIDNumber = "";
            Username = "";
            FullName = "";
            DepartmentRole = "";
            AssignedZone = "";
        }
    }
}