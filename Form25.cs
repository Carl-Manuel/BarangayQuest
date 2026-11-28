using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form25 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private int _targetUserId;
        private string _targetUserName = "";
        private string _targetBarangayId = "";
        private string _targetRole = "";
        private bool _isSubmitting = false;

        private TextBox? _txtReason;
        private Label? _lblTargetBanner;

        // Default constructor: automatically detects the selected user from Form14 if open
        public Form25() : this(0, "", "", "")
        {
        }

        // Direct constructor: pass the selected user's details from Form14
        public Form25(int targetUserId, string targetUserName = "", string targetBarangayId = "", string targetRole = "")
        {
            _targetUserId = targetUserId;
            _targetUserName = targetUserName;
            _targetBarangayId = targetBarangayId;
            _targetRole = targetRole;

            InitializeComponent();
            BindSuspensionControls();
            _resizer = new FormResizer(this);

            this.Load += async (s, e) => await LoadSelectedUserFromForm14Async();
        }

        private void BindSuspensionControls()
        {
            var allControls = GetAllControls(this).ToList();
            var allLabels = allControls.OfType<Label>().ToList();

            // Locate "REASON OF SUSPENSION" and embed a multiline TextBox inside its dark-maroon container
            var lblReasonHeader = allLabels.FirstOrDefault(l =>
                l.Text.Trim().Equals("REASON OF SUSPENSION", StringComparison.OrdinalIgnoreCase));

            if (lblReasonHeader != null && lblReasonHeader.Parent is Control boxContainer)
            {
                _lblTargetBanner = new Label
                {
                    AutoSize = false,
                    Top = lblReasonHeader.Top,
                    Left = lblReasonHeader.Right + 10,
                    Width = Math.Max(180, boxContainer.Width - lblReasonHeader.Right - 24),
                    Height = 22,
                    TextAlign = ContentAlignment.MiddleRight,
                    ForeColor = Color.Gold,
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    BackColor = Color.Transparent,
                    Text = "TARGET: LOADING..."
                };
                boxContainer.Controls.Add(_lblTargetBanner);
                _lblTargetBanner.BringToFront();

                _txtReason = boxContainer.Controls.OfType<TextBox>().FirstOrDefault();
                if (_txtReason == null)
                {
                    int reasonTop = lblReasonHeader.Bottom + 8;
                    _txtReason = new TextBox
                    {
                        Left = lblReasonHeader.Left,
                        Top = reasonTop,
                        Width = Math.Max(220, boxContainer.Width - (lblReasonHeader.Left * 2)),
                        Height = Math.Max(90, boxContainer.Height - reasonTop - 14),
                        Multiline = true,
                        ScrollBars = ScrollBars.Vertical,
                        BackColor = boxContainer.BackColor,
                        ForeColor = Color.White,
                        BorderStyle = BorderStyle.None,
                        Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
                        PlaceholderText = "Enter the reason for suspending this account..."
                    };
                    boxContainer.Controls.Add(_txtReason);
                }
                else
                {
                    _txtReason.Multiline = true;
                    _txtReason.ForeColor = Color.White;
                }

                _txtReason.BringToFront();
            }

            // Wire SUSPEND ACCOUNT button
            if (suspendacc != null)
            {
                suspendacc.Cursor = Cursors.Hand;
            }

            // Wire BACK button to return to Form14
            var btnBack = allControls.FirstOrDefault(c =>
                c != suspendacc &&
                (c.Name.Equals("back", StringComparison.OrdinalIgnoreCase) ||
                 c.Name.Equals("btnBack", StringComparison.OrdinalIgnoreCase) ||
                 c.Text.Trim().Equals("BACK", StringComparison.OrdinalIgnoreCase)));

            if (btnBack != null)
            {
                btnBack.Cursor = Cursors.Hand;
                btnBack.Click += (s, e) => ReturnToForm14();
            }
        }

        private async Task LoadSelectedUserFromForm14Async()
        {
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();
                await EnsureSuspensionSchemaAsync(conn);

                // If Form25 was opened via new Form25() with no arguments, inspect the open Form14 grids
                if (_targetUserId <= 0 && string.IsNullOrWhiteSpace(_targetUserName))
                {
                    ExtractSelectionFromOpenForm14();
                }

                // Resolve full user details from MySQL using UserID, BarangayIDNumber, or FullName
                if (_targetUserId > 0)
                {
                    using var cmd = new MySqlCommand(
                        "SELECT UserID, BarangayIDNumber, FullName, Role FROM Users WHERE UserID = @uid LIMIT 1;", conn);
                    cmd.Parameters.AddWithValue("@uid", _targetUserId);
                    using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        _targetUserId = Convert.ToInt32(r["UserID"]);
                        _targetBarangayId = r["BarangayIDNumber"]?.ToString() ?? "";
                        _targetUserName = r["FullName"]?.ToString() ?? "";
                        _targetRole = r["Role"]?.ToString() ?? "";
                    }
                }
                else if (!string.IsNullOrWhiteSpace(_targetBarangayId) || !string.IsNullOrWhiteSpace(_targetUserName))
                {
                    string lookupSql = @"
                        SELECT UserID, BarangayIDNumber, FullName, Role
                        FROM Users
                        WHERE (@bid <> '' AND BarangayIDNumber = @bid)
                           OR (@name <> '' AND (FullName = @name OR Username = @name))
                        ORDER BY UserID ASC
                        LIMIT 1;";
                    using var cmd = new MySqlCommand(lookupSql, conn);
                    cmd.Parameters.AddWithValue("@bid", _targetBarangayId.Trim());
                    cmd.Parameters.AddWithValue("@name", _targetUserName.Trim());
                    using var r = await cmd.ExecuteReaderAsync();
                    if (await r.ReadAsync())
                    {
                        _targetUserId = Convert.ToInt32(r["UserID"]);
                        _targetBarangayId = r["BarangayIDNumber"]?.ToString() ?? "";
                        _targetUserName = r["FullName"]?.ToString() ?? "";
                        _targetRole = r["Role"]?.ToString() ?? "";
                    }
                }

                if (_targetUserId <= 0)
                {
                    MessageBox.Show(
                        "No user was selected in User Management (Form 14).\nPlease select a user row first before clicking SUSPEND ACCOUNT.",
                        "No User Selected",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    ReturnToForm14();
                    return;
                }

                if (_lblTargetBanner != null)
                {
                    string idDisplay = string.IsNullOrWhiteSpace(_targetBarangayId) ? $"ID #{_targetUserId}" : _targetBarangayId;
                    _lblTargetBanner.Text = $"TARGET: {_targetUserName.ToUpperInvariant()} ({idDisplay})";
                }

                _txtReason?.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load selected user:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

                // Check if row.Tag holds a UserID or object with UserID
                if (row.Tag is int tagId && tagId > 0)
                {
                    _targetUserId = tagId;
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
                            _targetUserId = parsedId;
                            return;
                        }
                    }

                    if (header.Contains("BARANGAY", StringComparison.OrdinalIgnoreCase) ||
                        header.Equals("ID", StringComparison.OrdinalIgnoreCase))
                    {
                        _targetBarangayId = cellVal;
                    }
                    else if (header.Contains("USER", StringComparison.OrdinalIgnoreCase) ||
                             header.Contains("DISPATCHER", StringComparison.OrdinalIgnoreCase) ||
                             header.Contains("NAME", StringComparison.OrdinalIgnoreCase))
                    {
                        _targetUserName = cellVal;
                    }
                }

                // Fallback: first non-empty cell in the selected row is the user's name/ID
                if (_targetUserId <= 0 && string.IsNullOrWhiteSpace(_targetUserName) && string.IsNullOrWhiteSpace(_targetBarangayId))
                {
                    for (int i = 0; i < row.Cells.Count; i++)
                    {
                        string val = row.Cells[i].Value?.ToString()?.Trim() ?? "";
                        if (!string.IsNullOrWhiteSpace(val))
                        {
                            _targetUserName = val;
                            break;
                        }
                    }
                }

                if (_targetUserId > 0 || !string.IsNullOrWhiteSpace(_targetUserName) || !string.IsNullOrWhiteSpace(_targetBarangayId))
                    return;
            }
        }

        private static async Task EnsureSuspensionSchemaAsync(MySqlConnection conn)
        {
            string createTableSql = @"
                CREATE TABLE IF NOT EXISTS User_Suspensions (
                    SuspensionID INT AUTO_INCREMENT PRIMARY KEY,
                    UserID INT NOT NULL,
                    Reason TEXT NOT NULL,
                    SuspendedBy VARCHAR(100) NOT NULL,
                    Status ENUM('Active', 'Lifted') NOT NULL DEFAULT 'Active',
                    SuspendedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );";
            using (var createCmd = new MySqlCommand(createTableSql, conn))
            {
                await createCmd.ExecuteNonQueryAsync();
            }

            string checkColSql = @"
                SELECT COUNT(*)
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_SCHEMA = DATABASE()
                  AND TABLE_NAME = 'Users'
                  AND COLUMN_NAME = 'IsSuspended';";
            using (var checkCmd = new MySqlCommand(checkColSql, conn))
            {
                long exists = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
                if (exists == 0)
                {
                    using var alterCmd = new MySqlCommand(
                        "ALTER TABLE Users ADD COLUMN IsSuspended TINYINT(1) NOT NULL DEFAULT 0, ADD COLUMN SuspensionReason TEXT NULL;", conn);
                    await alterCmd.ExecuteNonQueryAsync();
                }
            }
        }

        private async void suspendacc_Click(object sender, EventArgs e)
        {
            if (_isSubmitting) return;

            if (_targetUserId <= 0)
            {
                MessageBox.Show("No valid user is selected to suspend.", "Invalid Target", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_targetRole.Equals("SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                _targetUserId == UserSession.UserID ||
                _targetUserId == DispatcherSession.UserID)
            {
                MessageBox.Show("Super Admin or currently signed-in accounts cannot be suspended.", "Action Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string reason = _txtReason?.Text.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("Please enter the REASON OF SUSPENSION first.", "Reason Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtReason?.Focus();
                return;
            }

            var confirm = MessageBox.Show(
                $"Suspend account for '{_targetUserName}'?\n\nReason:\n{reason}",
                "Confirm Account Suspension",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            _isSubmitting = true;
            if (suspendacc != null) suspendacc.Enabled = false;

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();
                await EnsureSuspensionSchemaAsync(conn);

                using var tx = await conn.BeginTransactionAsync();

                string suspendedBy = !string.IsNullOrWhiteSpace(UserSession.FullName)
                    ? UserSession.FullName
                    : (!string.IsNullOrWhiteSpace(DispatcherSession.FullName) ? DispatcherSession.FullName : "Super Admin");

                using (var updCmd = new MySqlCommand(
                    "UPDATE Users SET IsSuspended = 1, SuspensionReason = @reason WHERE UserID = @uid;", conn, tx))
                {
                    updCmd.Parameters.AddWithValue("@reason", reason);
                    updCmd.Parameters.AddWithValue("@uid", _targetUserId);
                    await updCmd.ExecuteNonQueryAsync();
                }

                using (var logCmd = new MySqlCommand(
                    "INSERT INTO User_Suspensions (UserID, Reason, SuspendedBy, Status) VALUES (@uid, @reason, @by, 'Active');", conn, tx))
                {
                    logCmd.Parameters.AddWithValue("@uid", _targetUserId);
                    logCmd.Parameters.AddWithValue("@reason", reason);
                    logCmd.Parameters.AddWithValue("@by", suspendedBy);
                    await logCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                MessageBox.Show(
                    $"Account '{_targetUserName}' has been suspended.",
                    "Account Suspended",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                ReturnToForm14();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to suspend account:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isSubmitting = false;
                if (suspendacc != null) suspendacc.Enabled = true;
            }
        }

        private void ReturnToForm14()
        {
            var form14 = Application.OpenForms.OfType<Form14>().FirstOrDefault(f => !f.IsDisposed);
            if (form14 != null)
            {
                form14.Show();
                this.Hide();
            }
            else
            {
                var nextForm = new Form14();
                nextForm.FormClosed += (s, args) => Application.Exit();
                nextForm.Show();
                this.Hide();
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            suspendacc_Click(sender, e);
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            ReturnToForm14();
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