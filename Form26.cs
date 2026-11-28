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
    public partial class Form26 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private sealed class UserProfileRecord
        {
            public int UserID { get; set; }
            public string BarangayIDNumber { get; set; } = "";
            public string FullName { get; set; } = "";
            public string Username { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
            public string Role { get; set; } = "CitizenWorker";
            public int Points { get; set; }
        }

        private readonly List<UserProfileRecord> _records = new();
        private int _selectedUserId = 0;
        private int _preselectedUserId = 0;
        private string _preselectedName = "";
        private string _preselectedBarangayId = "";
        private bool _isBusy = false;

        private TextBox? _txtFullName;
        private TextBox? _txtAccountId;
        private TextBox? _txtContactNumber;
        private TextBox? _txtPassword;
        private TextBox? _txtPoints;

        private DataGridView? _dgvUsers;
        private Control? _btnCreate;
        private Control? _btnRead;
        private Control? _btnUpdate;
        private Control? _btnDelete;
        private Button? _btnBack;
        private Label? _lblStatusBar;

        public Form26() : this(0, "", "")
        {
        }

        public Form26(int preselectedUserId, string preselectedName = "", string preselectedBarangayId = "")
        {
            _preselectedUserId = preselectedUserId;
            _preselectedName = preselectedName;
            _preselectedBarangayId = preselectedBarangayId;

            InitializeComponent();
            BindControls();
            _resizer = new FormResizer(this);

            this.Load += async (s, e) =>
            {
                if (_preselectedUserId <= 0 &&
                    string.IsNullOrWhiteSpace(_preselectedName) &&
                    string.IsNullOrWhiteSpace(_preselectedBarangayId))
                {
                    ExtractSelectionFromOpenForm14();
                }

                await LoadUsersAsync();
                ApplyPreselectionIfAny();
            };
        }

        private void ExtractSelectionFromOpenForm14()
        {
            var form14 = Application.OpenForms.OfType<Form14>().FirstOrDefault(f => !f.IsDisposed);
            if (form14 == null) return;

            var grids = GetAllControls(form14)
                .OfType<DataGridView>()
                .OrderByDescending(g => g.Focused)
                .ThenBy(g => g.Left)
                .ToList();

            foreach (var grid in grids)
            {
                DataGridViewRow? row = grid.SelectedRows.Count > 0 ? grid.SelectedRows[0] : grid.CurrentRow;
                if (row == null || row.IsNewRow) continue;

                if (row.Tag is int tagId && tagId > 0)
                {
                    _preselectedUserId = tagId;
                    return;
                }

                for (int i = 0; i < row.Cells.Count; i++)
                {
                    string header = grid.Columns[i].HeaderText?.Trim() ?? "";
                    string colName = grid.Columns[i].Name?.Trim() ?? "";
                    string cellVal = row.Cells[i].Value?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(cellVal)) continue;

                    if (header.Equals("USERID", StringComparison.OrdinalIgnoreCase) ||
                        colName.Equals("UserID", StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(cellVal, out int parsedId) && parsedId > 0)
                        {
                            _preselectedUserId = parsedId;
                            return;
                        }
                    }

                    if (header.Contains("BARANGAY", StringComparison.OrdinalIgnoreCase) ||
                        header.Equals("ID", StringComparison.OrdinalIgnoreCase))
                    {
                        _preselectedBarangayId = cellVal;
                    }
                    else if (header.Contains("USER", StringComparison.OrdinalIgnoreCase) ||
                             header.Contains("DISPATCHER", StringComparison.OrdinalIgnoreCase) ||
                             header.Contains("NAME", StringComparison.OrdinalIgnoreCase))
                    {
                        _preselectedName = cellVal;
                    }
                }

                if (_preselectedUserId <= 0 &&
                    string.IsNullOrWhiteSpace(_preselectedName) &&
                    string.IsNullOrWhiteSpace(_preselectedBarangayId))
                {
                    for (int i = 0; i < row.Cells.Count; i++)
                    {
                        string val = row.Cells[i].Value?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            _preselectedName = val;
                            break;
                        }
                    }
                }

                if (_preselectedUserId > 0 ||
                    !string.IsNullOrWhiteSpace(_preselectedName) ||
                    !string.IsNullOrWhiteSpace(_preselectedBarangayId))
                {
                    return;
                }
            }
        }

        private void ApplyPreselectionIfAny()
        {
            if (_records.Count == 0) return;

            int matchIndex = -1;

            if (_preselectedUserId > 0)
            {
                matchIndex = _records.FindIndex(r => r.UserID == _preselectedUserId);
            }

            if (matchIndex < 0 && !string.IsNullOrWhiteSpace(_preselectedBarangayId))
            {
                matchIndex = _records.FindIndex(r =>
                    r.BarangayIDNumber.Equals(_preselectedBarangayId.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (matchIndex < 0 && !string.IsNullOrWhiteSpace(_preselectedName))
            {
                matchIndex = _records.FindIndex(r =>
                    r.FullName.Equals(_preselectedName.Trim(), StringComparison.OrdinalIgnoreCase) ||
                    r.Username.Equals(_preselectedName.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (matchIndex >= 0)
            {
                if (_dgvUsers != null && matchIndex < _dgvUsers.Rows.Count)
                {
                    _dgvUsers.ClearSelection();
                    _dgvUsers.Rows[matchIndex].Selected = true;
                    _dgvUsers.CurrentCell = _dgvUsers.Rows[matchIndex].Cells[0];
                    _dgvUsers.FirstDisplayedScrollingRowIndex = matchIndex;
                }

                PopulateInputsFromRecord(_records[matchIndex]);
            }
        }

        private void BindControls()
        {
            var allControls = GetAllControls(this).ToList();
            var allLabels = allControls.OfType<Label>().ToList();

            var lblFullName = allLabels.FirstOrDefault(l => l.Text.Trim().Equals("FULL NAME", StringComparison.OrdinalIgnoreCase));
            var lblAccountId = allLabels.FirstOrDefault(l => l.Text.Trim().Equals("ACCOUNT ID", StringComparison.OrdinalIgnoreCase));
            var lblContact = allLabels.FirstOrDefault(l => l.Text.Trim().Equals("CONTACT NUMBER", StringComparison.OrdinalIgnoreCase));
            var lblPassword = allLabels.FirstOrDefault(l => l.Text.Trim().Equals("PASSWORD", StringComparison.OrdinalIgnoreCase));
            var lblPoints = allLabels.FirstOrDefault(l => l.Text.Trim().Equals("POINTS", StringComparison.OrdinalIgnoreCase));

            _txtFullName = FindOrCreateInputBox(allControls, lblFullName, isPassword: false);
            _txtAccountId = FindOrCreateInputBox(allControls, lblAccountId, isPassword: false);
            _txtContactNumber = FindOrCreateInputBox(allControls, lblContact, isPassword: false);
            _txtPassword = FindOrCreateInputBox(allControls, lblPassword, isPassword: true);
            _txtPoints = FindOrCreateInputBox(allControls, lblPoints, isPassword: false);

            _dgvUsers = allControls.OfType<DataGridView>().FirstOrDefault();
            if (_dgvUsers != null)
            {
                _dgvUsers.AllowUserToAddRows = false;
                _dgvUsers.AllowUserToDeleteRows = false;
                _dgvUsers.ReadOnly = true;
                _dgvUsers.MultiSelect = false;
                _dgvUsers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                _dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                _dgvUsers.RowHeadersVisible = false;
                _dgvUsers.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                _dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

                _dgvUsers.CellClick += (s, e) =>
                {
                    if (e.RowIndex >= 0 && e.RowIndex < _records.Count)
                    {
                        PopulateInputsFromRecord(_records[e.RowIndex]);
                    }
                };

                _dgvUsers.SelectionChanged += (s, e) =>
                {
                    if (_dgvUsers.CurrentRow != null &&
                        _dgvUsers.CurrentRow.Index >= 0 &&
                        _dgvUsers.CurrentRow.Index < _records.Count)
                    {
                        PopulateInputsFromRecord(_records[_dgvUsers.CurrentRow.Index]);
                    }
                };
            }

            _btnCreate = FindActionControl(allControls, _dgvUsers, "CREATE", 0);
            _btnRead = FindActionControl(allControls, _dgvUsers, "READ", 1);
            _btnUpdate = FindActionControl(allControls, _dgvUsers, "UPDATE", 2);
            _btnDelete = FindActionControl(allControls, _dgvUsers, "DELETE", 3);

            if (_btnCreate != null)
            {
                _btnCreate.Cursor = Cursors.Hand;
                _btnCreate.Click += async (s, e) => await CreateUserAsync();
            }

            if (_btnRead != null)
            {
                _btnRead.Cursor = Cursors.Hand;
                _btnRead.Click += async (s, e) => await ReadOrSearchUsersAsync();
            }

            if (_btnUpdate != null)
            {
                _btnUpdate.Cursor = Cursors.Hand;
                _btnUpdate.Click += async (s, e) => await UpdateUserAsync();
            }

            if (_btnDelete != null)
            {
                _btnDelete.Cursor = Cursors.Hand;
                _btnDelete.Click += async (s, e) => await DeleteUserAsync();
            }

            _btnBack = new Button
            {
                Text = "← BACK",
                Left = 28,
                Top = 24,
                Width = 88,
                Height = 32,
                BackColor = Color.FromArgb(145, 42, 32),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _btnBack.FlatAppearance.BorderColor = Color.Goldenrod;
            _btnBack.Click += (s, e) => NavigateBack();
            this.Controls.Add(_btnBack);
            _btnBack.BringToFront();

            _lblStatusBar = new Label
            {
                Left = 40,
                Top = Math.Max(10, this.ClientSize.Height - 36),
                Width = Math.Max(300, this.ClientSize.Width - 80),
                Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Bisque,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Text = "MODIFY USER PROFILE • SELECT A ROW TO EDIT OR CLICK READ TO REFRESH / FILTER"
            };
            this.Controls.Add(_lblStatusBar);
            _lblStatusBar.BringToFront();
        }

        private static Control? FindActionControl(List<Control> allControls, DataGridView? grid, string labelText, int slotIndex)
        {
            var byText = allControls.FirstOrDefault(c =>
                !(c is Form) &&
                !(c is DataGridView) &&
                c.Text.Trim().Equals(labelText, StringComparison.OrdinalIgnoreCase));

            if (byText != null) return byText;

            if (grid != null)
            {
                Point gridScreen = grid.PointToScreen(Point.Empty);
                int gridBottomScreen = gridScreen.Y + grid.Height;

                var bottomButtons = allControls
                    .Where(c => (c is Button || c is PictureBox) && c != grid)
                    .Select(c => new { Ctrl = c, Pos = c.PointToScreen(Point.Empty) })
                    .Where(x => x.Pos.Y >= gridBottomScreen - 10 &&
                                x.Pos.Y <= gridBottomScreen + 120 &&
                                x.Pos.X >= gridScreen.X - 20 &&
                                x.Pos.X <= gridScreen.X + grid.Width + 20)
                    .OrderBy(x => x.Pos.X)
                    .Select(x => x.Ctrl)
                    .ToList();

                if (slotIndex >= 0 && slotIndex < bottomButtons.Count)
                {
                    return bottomButtons[slotIndex];
                }
            }

            return null;
        }

        private TextBox? FindOrCreateInputBox(List<Control> allControls, Label? anchorLabel, bool isPassword)
        {
            if (anchorLabel == null) return null;

            Point lblScreen = anchorLabel.PointToScreen(Point.Empty);

            var existing = allControls
                .OfType<TextBox>()
                .Select(t => new { Box = t, Pos = t.PointToScreen(Point.Empty) })
                .Where(x => Math.Abs(x.Pos.X - lblScreen.X) <= 35 &&
                            x.Pos.Y >= lblScreen.Y &&
                            x.Pos.Y <= lblScreen.Y + 55)
                .OrderBy(x => x.Pos.Y)
                .Select(x => x.Box)
                .FirstOrDefault();

            if (existing != null)
            {
                existing.ForeColor = Color.White;
                existing.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
                if (isPassword) existing.UseSystemPasswordChar = true;
                return existing;
            }

            Control? hostContainer = anchorLabel.Parent;
            if (hostContainer == null) return null;

            var slotPanel = hostContainer.Controls
                .OfType<Panel>()
                .Where(p => Math.Abs(p.Left - anchorLabel.Left) <= 20 &&
                            p.Top >= anchorLabel.Bottom - 2 &&
                            p.Top <= anchorLabel.Bottom + 16 &&
                            p.Height >= 18 && p.Height <= 48)
                .OrderBy(p => p.Top)
                .FirstOrDefault();

            if (slotPanel != null)
            {
                var txtInSlot = new TextBox
                {
                    Left = 8,
                    Top = Math.Max(3, (slotPanel.Height - 20) / 2),
                    Width = Math.Max(120, slotPanel.Width - 16),
                    BackColor = slotPanel.BackColor,
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                    UseSystemPasswordChar = isPassword
                };
                slotPanel.Controls.Add(txtInSlot);
                txtInSlot.BringToFront();
                return txtInSlot;
            }

            var fallbackTxt = new TextBox
            {
                Left = anchorLabel.Left,
                Top = anchorLabel.Bottom + 4,
                Width = Math.Max(160, hostContainer.Width - (anchorLabel.Left * 2)),
                BackColor = Color.FromArgb(62, 12, 12),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                UseSystemPasswordChar = isPassword
            };
            hostContainer.Controls.Add(fallbackTxt);
            fallbackTxt.BringToFront();
            return fallbackTxt;
        }

        private void PopulateInputsFromRecord(UserProfileRecord rec)
        {
            _selectedUserId = rec.UserID;

            if (_txtFullName != null) _txtFullName.Text = rec.FullName;
            if (_txtAccountId != null) _txtAccountId.Text = rec.BarangayIDNumber;
            if (_txtContactNumber != null) _txtContactNumber.Text = rec.PhoneNumber;
            if (_txtPoints != null) _txtPoints.Text = rec.Points.ToString();
            if (_txtPassword != null) _txtPassword.Clear();

            if (_lblStatusBar != null)
            {
                _lblStatusBar.Text =
                    $"SELECTED: {rec.FullName.ToUpperInvariant()} (ID: {rec.BarangayIDNumber} • ROLE: {rec.Role} • {rec.Points:N0} PTS) — LEAVE PASSWORD BLANK TO KEEP CURRENT";
            }
        }

        private void ClearInputs()
        {
            _selectedUserId = 0;
            _txtFullName?.Clear();
            _txtAccountId?.Clear();
            _txtContactNumber?.Clear();
            _txtPassword?.Clear();
            if (_txtPoints != null) _txtPoints.Text = "0";
            _dgvUsers?.ClearSelection();
        }

        private async Task LoadUsersAsync(string searchFilter = "")
        {
            _records.Clear();

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT 
                        UserID,
                        BarangayIDNumber,
                        FullName,
                        Username,
                        COALESCE(Phone_Number, '') AS Phone_Number,
                        Role,
                        COALESCE(Points, 0) AS Points
                    FROM Users
                    WHERE (@filter = ''
                           OR FullName LIKE @like
                           OR BarangayIDNumber LIKE @like
                           OR Username LIKE @like
                           OR Phone_Number LIKE @like)
                    ORDER BY UserID ASC;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@filter", searchFilter.Trim());
                cmd.Parameters.AddWithValue("@like", $"%{searchFilter.Trim()}%");

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    _records.Add(new UserProfileRecord
                    {
                        UserID = Convert.ToInt32(reader["UserID"]),
                        BarangayIDNumber = reader["BarangayIDNumber"]?.ToString() ?? "",
                        FullName = reader["FullName"]?.ToString() ?? "",
                        Username = reader["Username"]?.ToString() ?? "",
                        PhoneNumber = reader["Phone_Number"]?.ToString() ?? "",
                        Role = reader["Role"]?.ToString() ?? "CitizenWorker",
                        Points = Convert.ToInt32(reader["Points"])
                    });
                }

                if (_dgvUsers != null)
                {
                    _dgvUsers.Rows.Clear();
                    foreach (var r in _records)
                    {
                        _dgvUsers.Rows.Add(
                            r.BarangayIDNumber,
                            r.FullName,
                            r.BarangayIDNumber,
                            r.PhoneNumber
                        );
                    }
                    _dgvUsers.ClearSelection();
                }

                if (_lblStatusBar != null)
                {
                    _lblStatusBar.Text = string.IsNullOrWhiteSpace(searchFilter)
                        ? $"LOADED {_records.Count} USER PROFILE(S) FROM DATABASE"
                        : $"FOUND {_records.Count} MATCHING USER(S) FOR '{searchFilter.Trim()}'";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user profiles:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ReadOrSearchUsersAsync()
        {
            if (_isBusy) return;

            string query = "";
            if (_selectedUserId == 0)
            {
                query = !string.IsNullOrWhiteSpace(_txtFullName?.Text)
                    ? _txtFullName!.Text.Trim()
                    : (_txtAccountId?.Text.Trim() ?? "");
            }

            _selectedUserId = 0;
            await LoadUsersAsync(query);

            if (!string.IsNullOrWhiteSpace(query) && _records.Count == 0)
            {
                await LoadUsersAsync();
            }
        }

        private async Task CreateUserAsync()
        {
            if (_isBusy) return;

            string fullName = _txtFullName?.Text.Trim() ?? "";
            string accountId = _txtAccountId?.Text.Trim() ?? "";
            string contact = _txtContactNumber?.Text.Trim() ?? "";
            string rawPassword = _txtPassword?.Text ?? "";
            string pointsText = _txtPoints?.Text.Trim() ?? "0";

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(accountId) ||
                string.IsNullOrWhiteSpace(rawPassword))
            {
                MessageBox.Show(
                    "To CREATE a new user, please fill in FULL NAME, ACCOUNT ID, and PASSWORD.",
                    "Required Fields",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(pointsText, out int points) || points < 0)
            {
                MessageBox.Show("POINTS must be a valid non-negative whole number.", "Invalid Points", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPoints?.Focus();
                return;
            }

            _isBusy = true;
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                using (var dupCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE BarangayIDNumber = @bid OR Username = @uname;", conn))
                {
                    dupCmd.Parameters.AddWithValue("@bid", accountId);
                    dupCmd.Parameters.AddWithValue("@uname", accountId);
                    long exists = Convert.ToInt64(await dupCmd.ExecuteScalarAsync());
                    if (exists > 0)
                    {
                        MessageBox.Show(
                            $"Account ID '{accountId}' already exists. Click UPDATE to edit the existing user, or use a unique Account ID.",
                            "Duplicate Account ID",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }
                }

                string hashedPwd = HashPasswordForStorage(rawPassword);
                string role = InferRoleFromAccountId(accountId);

                using var tx = await conn.BeginTransactionAsync();

                string insertUserSql = @"
                    INSERT INTO Users
                        (BarangayIDNumber, Role, Username, Password, FullName, Address, Zone, Phone_Number, Points)
                    VALUES
                        (@bid, @role, @uname, @pwd, @name, 'Not Specified', 'Zone 1', @phone, @pts);
                    SELECT LAST_INSERT_ID();";

                int newUserId;
                using (var insCmd = new MySqlCommand(insertUserSql, conn, tx))
                {
                    insCmd.Parameters.AddWithValue("@bid", accountId);
                    insCmd.Parameters.AddWithValue("@role", role);
                    insCmd.Parameters.AddWithValue("@uname", accountId);
                    insCmd.Parameters.AddWithValue("@pwd", hashedPwd);
                    insCmd.Parameters.AddWithValue("@name", fullName);
                    insCmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(contact) ? DBNull.Value : contact);
                    insCmd.Parameters.AddWithValue("@pts", points);
                    newUserId = Convert.ToInt32(await insCmd.ExecuteScalarAsync());
                }

                if (role == "SuperAdmin")
                {
                    using var roleCmd = new MySqlCommand(
                        "INSERT IGNORE INTO Super_Admin (UserID, AdminLevel) VALUES (@uid, 'Master');", conn, tx);
                    roleCmd.Parameters.AddWithValue("@uid", newUserId);
                    await roleCmd.ExecuteNonQueryAsync();
                }
                else if (role == "LGUDispatcher")
                {
                    using var roleCmd = new MySqlCommand(
                        "INSERT IGNORE INTO LGU_Dispatcher (UserID, DepartmentRole, AssignedZone) VALUES (@uid, 'Operations Officer', 'Zone 1');", conn, tx);
                    roleCmd.Parameters.AddWithValue("@uid", newUserId);
                    await roleCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    using var roleCmd = new MySqlCommand(
                        "INSERT IGNORE INTO Citizen_Worker (UserID, TotalQuestsCompleted) VALUES (@uid, 0);", conn, tx);
                    roleCmd.Parameters.AddWithValue("@uid", newUserId);
                    await roleCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                MessageBox.Show(
                    $"User profile for '{fullName}' ({accountId}) created successfully!",
                    "User Created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                ClearInputs();
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create user profile:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async Task UpdateUserAsync()
        {
            if (_isBusy) return;

            if (_selectedUserId <= 0)
            {
                MessageBox.Show(
                    "Please select a user row from the table on the right before clicking UPDATE.",
                    "No User Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            string fullName = _txtFullName?.Text.Trim() ?? "";
            string accountId = _txtAccountId?.Text.Trim() ?? "";
            string contact = _txtContactNumber?.Text.Trim() ?? "";
            string newRawPassword = _txtPassword?.Text ?? "";
            string pointsText = _txtPoints?.Text.Trim() ?? "0";

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(accountId))
            {
                MessageBox.Show("FULL NAME and ACCOUNT ID cannot be empty.", "Required Fields", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(pointsText, out int points) || points < 0)
            {
                MessageBox.Show("POINTS must be a valid non-negative whole number.", "Invalid Points", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtPoints?.Focus();
                return;
            }

            _isBusy = true;
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                using (var dupCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM Users WHERE BarangayIDNumber = @bid AND UserID <> @uid;", conn))
                {
                    dupCmd.Parameters.AddWithValue("@bid", accountId);
                    dupCmd.Parameters.AddWithValue("@uid", _selectedUserId);
                    long collision = Convert.ToInt64(await dupCmd.ExecuteScalarAsync());
                    if (collision > 0)
                    {
                        MessageBox.Show($"Another user already uses Account ID '{accountId}'.", "Duplicate Account ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                bool changePassword = !string.IsNullOrWhiteSpace(newRawPassword);
                string updateSql = changePassword
                    ? @"UPDATE Users
                        SET BarangayIDNumber = @bid,
                            Username = @bid,
                            FullName = @name,
                            Phone_Number = @phone,
                            Points = @pts,
                            Password = @pwd
                        WHERE UserID = @uid;"
                    : @"UPDATE Users
                        SET BarangayIDNumber = @bid,
                            Username = @bid,
                            FullName = @name,
                            Phone_Number = @phone,
                            Points = @pts
                        WHERE UserID = @uid;";

                using var cmd = new MySqlCommand(updateSql, conn);
                cmd.Parameters.AddWithValue("@bid", accountId);
                cmd.Parameters.AddWithValue("@name", fullName);
                cmd.Parameters.AddWithValue("@phone", string.IsNullOrWhiteSpace(contact) ? DBNull.Value : contact);
                cmd.Parameters.AddWithValue("@pts", points);
                cmd.Parameters.AddWithValue("@uid", _selectedUserId);

                if (changePassword)
                {
                    cmd.Parameters.AddWithValue("@pwd", HashPasswordForStorage(newRawPassword));
                }

                await cmd.ExecuteNonQueryAsync();

                MessageBox.Show(
                    $"Profile for '{fullName}' updated successfully!",
                    "Profile Updated",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                ClearInputs();
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update user profile:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async Task DeleteUserAsync()
        {
            if (_isBusy) return;

            if (_selectedUserId <= 0)
            {
                MessageBox.Show(
                    "Please select a user row from the table before clicking DELETE.",
                    "No User Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            var selected = _records.FirstOrDefault(r => r.UserID == _selectedUserId);
            if (selected == null) return;

            if (selected.UserID == UserSession.UserID || selected.UserID == DispatcherSession.UserID)
            {
                MessageBox.Show("You cannot delete the currently signed-in account.", "Action Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Permanently delete user '{selected.FullName}' (Account ID: {selected.BarangayIDNumber})?\n\n" +
                "This removes their account and linked worker/dispatcher records.",
                "Confirm Delete User",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            _isBusy = true;
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();
                using var tx = await conn.BeginTransactionAsync();

                string[] cleanupQueries =
                {
                    "DELETE FROM Citizen_Worker WHERE UserID = @uid;",
                    "DELETE FROM LGU_Dispatcher WHERE UserID = @uid;",
                    "DELETE FROM Super_Admin WHERE UserID = @uid;",
                    "DELETE FROM Users WHERE UserID = @uid;"
                };

                foreach (string q in cleanupQueries)
                {
                    using var cmd = new MySqlCommand(q, conn, tx);
                    cmd.Parameters.AddWithValue("@uid", _selectedUserId);
                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                MessageBox.Show($"User '{selected.FullName}' has been deleted.", "User Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ClearInputs();
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Unable to delete user (they may have active quest submissions or redemption history):\n{ex.Message}",
                    "Delete Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private static string InferRoleFromAccountId(string accountId)
        {
            if (accountId.StartsWith("ADMIN", StringComparison.OrdinalIgnoreCase))
                return "SuperAdmin";
            if (accountId.StartsWith("BRGY", StringComparison.OrdinalIgnoreCase) ||
                accountId.StartsWith("BID", StringComparison.OrdinalIgnoreCase) ||
                accountId.StartsWith("DISP", StringComparison.OrdinalIgnoreCase))
                return "LGUDispatcher";

            return "CitizenWorker";
        }

        private static string HashPasswordForStorage(string plainPassword)
        {
            var helperType = typeof(Form26).Assembly.GetTypes()
                .FirstOrDefault(t => t.Name.Equals("PasswordHelper", StringComparison.Ordinal));
            if (helperType != null)
            {
                var hashMethod = helperType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m =>
                        m.ReturnType == typeof(string) &&
                        m.GetParameters().Length == 1 &&
                        m.GetParameters()[0].ParameterType == typeof(string));

                if (hashMethod != null && hashMethod.Invoke(null, new object[] { plainPassword }) is string hashed && !string.IsNullOrWhiteSpace(hashed))
                {
                    return hashed;
                }
            }

            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainPassword)));
        }

        private void NavigateBack()
        {
            var form14 = Application.OpenForms.OfType<Form14>().FirstOrDefault(f => !f.IsDisposed);
            if (form14 != null)
            {
                form14.Show();
                this.Hide();
                return;
            }

            Form? returnForm = Application.OpenForms.Cast<Form>()
                .FirstOrDefault(f => f != this && !f.IsDisposed);

            if (returnForm != null)
            {
                returnForm.Show();
                this.Hide();
            }
            else
            {
                var fallback = new Form17();
                fallback.FormClosed += (s, args) => Application.Exit();
                fallback.Show();
                this.Hide();
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
    }
}