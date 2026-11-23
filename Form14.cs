using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form14 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private DataGridView? dgvUsers;
        private DataGridView? dgvDispatchers;
        private Control? btnBack;
        private Control? btnNext;
        private Control? btnSuspend;
        private Control? btnModify;
        private Control? btnUpdate;

        private int currentPage = 0;
        private const int PageSize = 10;
        private int totalUsersCount = 0;

        public Form14()
        {
            InitializeComponent();
            BindUserManagementControls();
            _resizer = new FormResizer(this);

            this.Load += async (s, e) => await RefreshAllTablesAsync();
        }

        private void BindUserManagementControls()
        {
            var allControls = GetAllControls(this).ToList();

            // Locate the two DataGridViews (Left = Registered Users, Right = Registered Dispatchers)
            var grids = allControls
                .OfType<DataGridView>()
                .OrderBy(g => g.PointToScreen(Point.Empty).X)
                .ToList();

            if (grids.Count >= 1)
            {
                dgvUsers = grids[0];
                ConfigureGridDefaults(dgvUsers);
                if (dgvUsers.Columns.Count == 3)
                {
                    dgvUsers.Columns.Add("colStatus", "STATUS");
                }
                dgvUsers.SelectionChanged += (s, e) =>
                {
                    if (dgvUsers.SelectedRows.Count > 0 && dgvDispatchers != null)
                        dgvDispatchers.ClearSelection();
                };
            }

            if (grids.Count >= 2)
            {
                dgvDispatchers = grids[1];
                ConfigureGridDefaults(dgvDispatchers);
                if (dgvDispatchers.Columns.Count >= 3)
                {
                    dgvDispatchers.Columns[1].HeaderText = "DEPARTMENT ROLE";
                    dgvDispatchers.Columns[2].HeaderText = "ASSIGNED ZONE";
                }
                dgvDispatchers.SelectionChanged += (s, e) =>
                {
                    if (dgvDispatchers.SelectedRows.Count > 0 && dgvUsers != null)
                        dgvUsers.ClearSelection();
                };
            }

            // Bind the Designer-wired graphic buttons directly
            btnBack = this.backbutton3;
            btnNext = this.next;
            btnSuspend = this.suspendacc;
            btnUpdate = this.updatebutt;

            if (btnBack != null) btnBack.Cursor = Cursors.Hand;
            if (btnNext != null) btnNext.Cursor = Cursors.Hand;
            if (btnSuspend != null) btnSuspend.Cursor = Cursors.Hand;
            if (btnUpdate != null) btnUpdate.Cursor = Cursors.Hand;

            // Locate MODIFY USER PROFILE (by Name, Text, or directly to the right of 'suspendacc')
            btnModify = allControls.FirstOrDefault(c =>
                c != btnSuspend && c != btnUpdate && c != btnBack && c != btnNext &&
                (c.Name.Contains("modify", StringComparison.OrdinalIgnoreCase) ||
                 c.Text.Trim().Equals("MODIFY USER PROFILE", StringComparison.OrdinalIgnoreCase)));

            if (btnModify == null && btnSuspend != null)
            {
                Point suspendPos = btnSuspend.PointToScreen(Point.Empty);
                btnModify = allControls
                    .Where(c => c != btnSuspend && c != btnUpdate && (c is PictureBox || c is Button || c is Panel))
                    .Where(c => c.Width >= 110 && c.Width <= 320 && c.Height >= 25 && c.Height <= 85)
                    .Select(c => new { Ctrl = c, Pos = c.PointToScreen(Point.Empty) })
                    .Where(x => Math.Abs(x.Pos.Y - suspendPos.Y) < 35 && x.Pos.X > suspendPos.X)
                    .OrderBy(x => x.Pos.X)
                    .Select(x => x.Ctrl)
                    .FirstOrDefault();
            }

            if (btnModify != null)
            {
                btnModify.Cursor = Cursors.Hand;
                btnModify.Click += async (s, e) => await OpenModifyUserDialogAsync();
            }

            // Wire the left sidebar icons (1st = Admin Dashboard, 2nd = Inventory Management)
            var sidebarIcons = allControls
                .OfType<PictureBox>()
                .Where(p => p.Left < 100 && p.Top > 120 && p.Width < 90)
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
        }

        private static void ConfigureGridDefaults(DataGridView dgv)
        {
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.ReadOnly = true;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.RowHeadersVisible = false;
        }

        private async Task RefreshAllTablesAsync()
        {
            await LoadRegisteredUsersAsync();
            await LoadRegisteredDispatchersAsync();
        }

        private async Task LoadRegisteredUsersAsync()
        {
            if (dgvUsers == null) return;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                using (var countCmd = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Role <> 'LGUDispatcher';", conn))
                {
                    totalUsersCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

                if (dgvUsers.Columns.Count > 0)
                {
                    dgvUsers.Columns[0].HeaderText = $"USERS ({totalUsersCount})";
                }

                string sql = @"
                    SELECT 
                        u.UserID,
                        u.FullName,
                        u.Username,
                        u.Role,
                        COALESCE(cw.AccountStatus, 'Active') AS AccountStatus,
                        (
                            SELECT COUNT(*)
                            FROM Quest_Memberships qm
                            INNER JOIN Quests q ON qm.QuestID = q.QuestID
                            WHERE qm.WorkerID = cw.WorkerID
                              AND qm.Status = 'Joined'
                              AND q.Status IN ('Upcoming', 'Active')
                        ) AS ActiveQuests
                    FROM Users u
                    LEFT JOIN Citizen_Worker cw ON u.UserID = cw.UserID
                    WHERE u.Role <> 'LGUDispatcher'
                    ORDER BY u.UserID DESC
                    LIMIT @limit OFFSET @offset;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@limit", PageSize);
                cmd.Parameters.AddWithValue("@offset", currentPage * PageSize);

                dgvUsers.Rows.Clear();

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int userId = Convert.ToInt32(reader["UserID"]);
                    string displayUser = $"{reader["FullName"]} (@{reader["Username"]})";
                    int activeQuests = Convert.ToInt32(reader["ActiveQuests"]);
                    string role = reader["Role"]?.ToString() ?? "";
                    string status = reader["AccountStatus"]?.ToString() ?? "Active";

                    int rowIndex = dgvUsers.Rows.Add(displayUser, activeQuests, role, status);
                    var row = dgvUsers.Rows[rowIndex];
                    row.Tag = userId;

                    if (status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.BackColor = Color.MistyRose;
                        row.DefaultCellStyle.ForeColor = Color.DarkRed;
                    }
                }

                dgvUsers.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading registered users: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadRegisteredDispatchersAsync()
        {
            if (dgvDispatchers == null) return;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT 
                        u.UserID,
                        u.FullName,
                        u.Username,
                        COALESCE(d.DepartmentRole, 'Unassigned') AS DepartmentRole,
                        COALESCE(d.AssignedZone, u.Zone) AS AssignedZone
                    FROM Users u
                    LEFT JOIN LGU_Dispatcher d ON u.UserID = d.UserID
                    WHERE u.Role = 'LGUDispatcher'
                    ORDER BY u.UserID DESC;";

                using var cmd = new MySqlCommand(sql, conn);
                dgvDispatchers.Rows.Clear();

                int count = 0;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    count++;
                    int userId = Convert.ToInt32(reader["UserID"]);
                    string displayDispatcher = $"{reader["FullName"]} (@{reader["Username"]})";
                    string deptRole = reader["DepartmentRole"]?.ToString() ?? "";
                    string zone = reader["AssignedZone"]?.ToString() ?? "";

                    int rowIndex = dgvDispatchers.Rows.Add(displayDispatcher, deptRole, zone);
                    dgvDispatchers.Rows[rowIndex].Tag = userId;
                }

                if (dgvDispatchers.Columns.Count > 0)
                {
                    dgvDispatchers.Columns[0].HeaderText = $"DISPATCHERS ({count})";
                }

                dgvDispatchers.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dispatchers: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private int? GetSelectedUserId(out bool isDispatcherGrid)
        {
            isDispatcherGrid = false;

            if (dgvUsers != null && dgvUsers.SelectedRows.Count > 0 && dgvUsers.SelectedRows[0].Tag is int uid)
            {
                return uid;
            }

            if (dgvDispatchers != null && dgvDispatchers.SelectedRows.Count > 0 && dgvDispatchers.SelectedRows[0].Tag is int did)
            {
                isDispatcherGrid = true;
                return did;
            }

            return null;
        }

        private async Task ToggleSuspendSelectedUserAsync()
        {
            int? selectedId = GetSelectedUserId(out bool isDispatcher);
            if (selectedId == null)
            {
                MessageBox.Show("Select a user or dispatcher row first before suspending.", "No Account Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string checkSql = @"
                    SELECT u.FullName, u.Role, cw.WorkerID, COALESCE(cw.AccountStatus, 'Active') AS AccountStatus
                    FROM Users u
                    LEFT JOIN Citizen_Worker cw ON u.UserID = cw.UserID
                    WHERE u.UserID = @uid
                    LIMIT 1;";

                string fullName = "";
                string role = "";
                string currentStatus = "Active";
                bool hasWorkerRow = false;

                using (var cmd = new MySqlCommand(checkSql, conn))
                {
                    cmd.Parameters.AddWithValue("@uid", selectedId.Value);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync()) return;

                    fullName = reader["FullName"]?.ToString() ?? "";
                    role = reader["Role"]?.ToString() ?? "";
                    currentStatus = reader["AccountStatus"]?.ToString() ?? "Active";
                    hasWorkerRow = reader["WorkerID"] != DBNull.Value;
                }

                if (role.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("Super Admin accounts cannot be suspended.", "Action Blocked", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }

                if (role.Equals("CitizenWorker", StringComparison.OrdinalIgnoreCase))
                {
                    string nextStatus = currentStatus.Equals("Suspended", StringComparison.OrdinalIgnoreCase) ? "Active" : "Suspended";
                    string actionVerb = nextStatus == "Suspended" ? "suspend" : "reactivate";

                    var confirm = MessageBox.Show(
                        $"Are you sure you want to {actionVerb} Citizen Worker '{fullName}'?",
                        "Confirm Account Status Change",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm != DialogResult.Yes) return;

                    if (!hasWorkerRow)
                    {
                        using var insCmd = new MySqlCommand("INSERT INTO Citizen_Worker (UserID, AccountStatus) VALUES (@uid, @status);", conn);
                        insCmd.Parameters.AddWithValue("@uid", selectedId.Value);
                        insCmd.Parameters.AddWithValue("@status", nextStatus);
                        await insCmd.ExecuteNonQueryAsync();
                    }
                    else
                    {
                        using var updCmd = new MySqlCommand("UPDATE Citizen_Worker SET AccountStatus = @status WHERE UserID = @uid;", conn);
                        updCmd.Parameters.AddWithValue("@status", nextStatus);
                        updCmd.Parameters.AddWithValue("@uid", selectedId.Value);
                        await updCmd.ExecuteNonQueryAsync();
                    }

                    MessageBox.Show($"{fullName}'s account is now {nextStatus}.", "Status Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await RefreshAllTablesAsync();
                }
                else if (isDispatcher || role.Equals("LGUDispatcher", StringComparison.OrdinalIgnoreCase))
                {
                    var confirm = MessageBox.Show(
                        $"LGU Dispatcher accounts do not have a suspension flag in the database.\nDo you want to revoke/remove Dispatcher '{fullName}' instead?",
                        "Revoke Dispatcher Access",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm != DialogResult.Yes) return;

                    using var delCmd = new MySqlCommand("DELETE FROM Users WHERE UserID = @uid;", conn);
                    delCmd.Parameters.AddWithValue("@uid", selectedId.Value);
                    await delCmd.ExecuteNonQueryAsync();

                    MessageBox.Show($"Dispatcher '{fullName}' has been removed.", "Dispatcher Removed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    await RefreshAllTablesAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update account status: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task OpenModifyUserDialogAsync()
        {
            int? selectedId = GetSelectedUserId(out _);
            bool isCreateMode = selectedId == null;

            string barangayId = "", username = "", fullName = "", address = "Barangay Hall", zone = "Zone 1", phone = "", role = "LGUDispatcher", deptRole = "Operations Officer";
            int points = 0;

            if (!isCreateMode)
            {
                try
                {
                    using var conn = new MySqlConnection(connString);
                    await conn.OpenAsync();

                    string sql = @"
                        SELECT u.*, d.DepartmentRole, d.AssignedZone
                        FROM Users u
                        LEFT JOIN LGU_Dispatcher d ON u.UserID = d.UserID
                        WHERE u.UserID = @uid
                        LIMIT 1;";

                    using var cmd = new MySqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@uid", selectedId!.Value);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync()) return;

                    barangayId = reader["BarangayIDNumber"]?.ToString() ?? "";
                    username = reader["Username"]?.ToString() ?? "";
                    fullName = reader["FullName"]?.ToString() ?? "";
                    address = reader["Address"]?.ToString() ?? "";
                    zone = reader["AssignedZone"] != DBNull.Value ? reader["AssignedZone"]!.ToString()! : (reader["Zone"]?.ToString() ?? "");
                    phone = reader["Phone_Number"]?.ToString() ?? "";
                    role = reader["Role"]?.ToString() ?? "CitizenWorker";
                    points = Convert.ToInt32(reader["Points"]);
                    if (reader["DepartmentRole"] != DBNull.Value)
                        deptRole = reader["DepartmentRole"]!.ToString()!;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading user details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            using var dlg = new Form
            {
                Text = isCreateMode ? "Register New Dispatcher / User" : $"Modify User Profile — {fullName}",
                Size = new Size(440, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.FromArgb(45, 25, 12),
                ForeColor = Color.White
            };

            int y = 20;
            TextBox AddField(string labelText, string initialVal, bool isPass = false)
            {
                var lbl = new Label { Text = labelText, Left = 20, Top = y, Width = 150, ForeColor = Color.Gold };
                var txt = new TextBox { Left = 180, Top = y - 3, Width = 220, Text = initialVal, UseSystemPasswordChar = isPass };
                dlg.Controls.Add(lbl);
                dlg.Controls.Add(txt);
                y += 36;
                return txt;
            }

            var txtFullName = AddField("Full Name:", fullName);
            var txtUsername = AddField("Username:", username);
            var txtBarangayId = AddField("Barangay / Officer ID:", barangayId);
            var txtPhone = AddField("Phone Number:", phone);
            var txtAddress = AddField("Address:", address);
            var txtZone = AddField("Zone / Assigned Zone:", zone);

            var lblRole = new Label { Text = "Role:", Left = 20, Top = y, Width = 150, ForeColor = Color.Gold };
            var cmbRole = new ComboBox { Left = 180, Top = y - 3, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRole.Items.AddRange(new object[] { "CitizenWorker", "LGUDispatcher", "SuperAdmin" });
            cmbRole.SelectedItem = role;
            dlg.Controls.Add(lblRole);
            dlg.Controls.Add(cmbRole);
            y += 36;

            var txtDeptRole = AddField("Dispatcher Dept Role:", deptRole);

            var lblPoints = new Label { Text = "Points Balance:", Left = 20, Top = y, Width = 150, ForeColor = Color.Gold };
            var numPoints = new NumericUpDown { Left = 180, Top = y - 3, Width = 220, Maximum = 1000000, Minimum = 0, Value = Math.Max(0, points) };
            dlg.Controls.Add(lblPoints);
            dlg.Controls.Add(numPoints);
            y += 36;

            var txtNewPass = AddField(isCreateMode ? "Password:" : "New Password (Optional):", "", true);

            var btnSave = new Button
            {
                Text = isCreateMode ? "CREATE ACCOUNT" : "SAVE CHANGES",
                Left = 180,
                Top = y + 10,
                Width = 140,
                Height = 36,
                BackColor = Color.ForestGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            var btnCancel = new Button
            {
                Text = "CANCEL",
                Left = 330,
                Top = y + 10,
                Width = 70,
                Height = 36,
                BackColor = Color.Firebrick,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };

            btnCancel.Click += (s, e) => dlg.DialogResult = DialogResult.Cancel;
            btnSave.Click += async (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtBarangayId.Text))
                {
                    MessageBox.Show("Full Name, Username, and Barangay/Officer ID are required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (isCreateMode && string.IsNullOrWhiteSpace(txtNewPass.Text))
                {
                    MessageBox.Show("Please enter a password for the new account.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    using var conn = new MySqlConnection(connString);
                    await conn.OpenAsync();
                    using var tx = await conn.BeginTransactionAsync();

                    string chosenRole = cmbRole.SelectedItem?.ToString() ?? "CitizenWorker";
                    int targetUserId;

                    if (isCreateMode)
                    {
                        string insUser = @"
                            INSERT INTO Users (BarangayIDNumber, Role, Username, Password, FullName, Address, Zone, Phone_Number, Points)
                            VALUES (@bid, @role, @user, @pass, @name, @addr, @zone, @phone, @pts);
                            SELECT LAST_INSERT_ID();";

                        using var cmd = new MySqlCommand(insUser, conn, tx);
                        cmd.Parameters.AddWithValue("@bid", txtBarangayId.Text.Trim());
                        cmd.Parameters.AddWithValue("@role", chosenRole);
                        cmd.Parameters.AddWithValue("@user", txtUsername.Text.Trim());
                        cmd.Parameters.AddWithValue("@pass", HashPassword(txtNewPass.Text));
                        cmd.Parameters.AddWithValue("@name", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@addr", txtAddress.Text.Trim());
                        cmd.Parameters.AddWithValue("@zone", txtZone.Text.Trim());
                        cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(txtPhone.Text) ? "09000000000" : txtPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@pts", (int)numPoints.Value);
                        targetUserId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                    else
                    {
                        targetUserId = selectedId!.Value;
                        bool updatePass = !string.IsNullOrWhiteSpace(txtNewPass.Text);

                        string updUser = updatePass
                            ? @"UPDATE Users SET BarangayIDNumber=@bid, Role=@role, Username=@user, Password=@pass, FullName=@name, Address=@addr, Zone=@zone, Phone_Number=@phone, Points=@pts WHERE UserID=@uid;"
                            : @"UPDATE Users SET BarangayIDNumber=@bid, Role=@role, Username=@user, FullName=@name, Address=@addr, Zone=@zone, Phone_Number=@phone, Points=@pts WHERE UserID=@uid;";

                        using var cmd = new MySqlCommand(updUser, conn, tx);
                        cmd.Parameters.AddWithValue("@bid", txtBarangayId.Text.Trim());
                        cmd.Parameters.AddWithValue("@role", chosenRole);
                        cmd.Parameters.AddWithValue("@user", txtUsername.Text.Trim());
                        if (updatePass) cmd.Parameters.AddWithValue("@pass", HashPassword(txtNewPass.Text));
                        cmd.Parameters.AddWithValue("@name", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@addr", txtAddress.Text.Trim());
                        cmd.Parameters.AddWithValue("@zone", txtZone.Text.Trim());
                        cmd.Parameters.AddWithValue("@phone", txtPhone.Text.Trim());
                        cmd.Parameters.AddWithValue("@pts", (int)numPoints.Value);
                        cmd.Parameters.AddWithValue("@uid", targetUserId);
                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Ensure role inheritance row exists
                    if (chosenRole == "LGUDispatcher")
                    {
                        string upsertDisp = @"
                            INSERT INTO LGU_Dispatcher (UserID, DepartmentRole, AssignedZone)
                            VALUES (@uid, @dept, @zone)
                            ON DUPLICATE KEY UPDATE DepartmentRole = @dept, AssignedZone = @zone;";
                        using var dCmd = new MySqlCommand(upsertDisp, conn, tx);
                        dCmd.Parameters.AddWithValue("@uid", targetUserId);
                        dCmd.Parameters.AddWithValue("@dept", string.IsNullOrWhiteSpace(txtDeptRole.Text) ? "Operations Officer" : txtDeptRole.Text.Trim());
                        dCmd.Parameters.AddWithValue("@zone", txtZone.Text.Trim());
                        await dCmd.ExecuteNonQueryAsync();
                    }
                    else if (chosenRole == "CitizenWorker")
                    {
                        using var wCmd = new MySqlCommand("INSERT IGNORE INTO Citizen_Worker (UserID, AccountStatus) VALUES (@uid, 'Active');", conn, tx);
                        wCmd.Parameters.AddWithValue("@uid", targetUserId);
                        await wCmd.ExecuteNonQueryAsync();
                    }
                    else if (chosenRole == "SuperAdmin")
                    {
                        using var aCmd = new MySqlCommand("INSERT IGNORE INTO Super_Admin (UserID, StorageClearanceLevel, SystemLogAccess) VALUES (@uid, 1, 'FULL');", conn, tx);
                        aCmd.Parameters.AddWithValue("@uid", targetUserId);
                        await aCmd.ExecuteNonQueryAsync();
                    }

                    await tx.CommitAsync();
                    dlg.DialogResult = DialogResult.OK;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving account: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            dlg.Controls.Add(btnSave);
            dlg.Controls.Add(btnCancel);

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                await RefreshAllTablesAsync();
            }
        }

        private void NavigateTo(Form targetForm)
        {
            targetForm.FormClosed += (s, e) => this.Close();
            targetForm.Show();
            this.Hide();
        }

        private static string HashPassword(string rawPassword)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawPassword));
            return Convert.ToHexString(bytes);
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

        // Designer-bound click handlers for the graphic buttons
        private async void suspendacc_Click(object sender, EventArgs e)
        {
            if (suspendacc != null) suspendacc.Enabled = false;
            try
            {
                await ToggleSuspendSelectedUserAsync();
            }
            finally
            {
                if (suspendacc != null) suspendacc.Enabled = true;
            }
        }

        private async void updatebutt_Click(object sender, EventArgs e)
        {
            if (updatebutt != null) updatebutt.Enabled = false;
            try
            {
                await RefreshAllTablesAsync();
                MessageBox.Show("User and Dispatcher records have been updated.", "Synced", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                if (updatebutt != null) updatebutt.Enabled = true;
            }
        }

        private async void backbutton3_Click(object sender, EventArgs e)
        {
            if (currentPage > 0)
            {
                currentPage--;
                await LoadRegisteredUsersAsync();
            }
            else
            {
                NavigateTo(new X());
            }
        }

        private async void next_Click(object sender, EventArgs e)
        {
            if ((currentPage + 1) * PageSize < totalUsersCount)
            {
                currentPage++;
                await LoadRegisteredUsersAsync();
            }
            else
            {
                MessageBox.Show("You are already on the last page of registered users.", "End of List", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void label3_Click(object sender, EventArgs e) { }
    }
}