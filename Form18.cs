using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form18 : Form
    {
        private readonly string _connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";
        private readonly FormResizer _resizer;

        private DataGridView? _dgvUsers;
        private Label? _lblStatusBar;

        private int _currentPage = 0;
        private const int PageSize = 10;
        private int _totalUsersCount = 0;

        public Form18()
        {
            InitializeComponent();
            BindControls();
            _resizer = new FormResizer(this);

            this.Load += async (s, e) => await LoadRegisteredUsersAsync();
        }

        private void BindControls()
        {
            // Bind and style the Registered Users DataGridView
            _dgvUsers = GetAllControls(this).OfType<DataGridView>().FirstOrDefault();
            if (_dgvUsers != null)
            {
                _dgvUsers.AllowUserToAddRows = false;
                _dgvUsers.AllowUserToDeleteRows = false;
                _dgvUsers.ReadOnly = true;
                _dgvUsers.MultiSelect = false;
                _dgvUsers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                _dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                _dgvUsers.RowHeadersVisible = false;
                _dgvUsers.BackgroundColor = Color.FromArgb(58, 38, 12);
                _dgvUsers.DefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                _dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);

                if (_dgvUsers.Columns.Count == 3)
                {
                    _dgvUsers.Columns.Add("colStatus", "STATUS");
                }

                _dgvUsers.CellDoubleClick += async (s, e) =>
                {
                    if (e.RowIndex >= 0 && _dgvUsers.Rows[e.RowIndex].Tag is int userId)
                    {
                        await ShowUserDetailsPopupAsync(userId);
                    }
                };
            }

            //  Set hand cursors on all sidebar icons and action buttons
            Control?[] interactiveControls =
            {
                dispatcherdashboard,
                dispatcherprofile,
                worksubmissions,
                back,
                next,
                update
            };

            foreach (var ctrl in interactiveControls)
            {
                if (ctrl != null)
                {
                    ctrl.Cursor = Cursors.Hand;
                }
            }

            //  Add live status & pagination text inside the bottom maroon bar
            _lblStatusBar = new Label
            {
                Left = 48,
                Top = Math.Max(10, this.ClientSize.Height - 50),
                Width = Math.Max(300, this.ClientSize.Width - 96),
                Height = 30,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.Bisque,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BackColor = Color.Transparent,
                Text = "Loading registered community users..."
            };
            this.Controls.Add(_lblStatusBar);
            _lblStatusBar.BringToFront();
        }

        private async Task LoadRegisteredUsersAsync()
        {
            if (_dgvUsers == null) return;

            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                using (var countCmd = new MySqlCommand("SELECT COUNT(*) FROM Users;", conn))
                {
                    _totalUsersCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

                int totalPages = Math.Max(1, (int)Math.Ceiling(_totalUsersCount / (double)PageSize));
                if (_currentPage >= totalPages)
                {
                    _currentPage = totalPages - 1;
                }

                if (_dgvUsers.Columns.Count > 0)
                {
                    _dgvUsers.Columns[0].HeaderText = $"USER ({_totalUsersCount} TOTAL)";
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
                    ORDER BY FIELD(u.Role, 'CitizenWorker', 'LGUDispatcher', 'SuperAdmin'), u.UserID DESC
                    LIMIT @limit OFFSET @offset;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@limit", PageSize);
                cmd.Parameters.AddWithValue("@offset", _currentPage * PageSize);

                _dgvUsers.Rows.Clear();

                int shownCount = 0;
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    shownCount++;
                    int userId = Convert.ToInt32(reader["UserID"]);
                    string displayUser = $"{reader["FullName"]} (@{reader["Username"]})";
                    int activeQuests = Convert.ToInt32(reader["ActiveQuests"]);
                    string role = reader["Role"]?.ToString() ?? "";
                    string status = reader["AccountStatus"]?.ToString() ?? "Active";

                    int rowIndex = _dgvUsers.Rows.Add(displayUser, activeQuests, role, status);
                    var row = _dgvUsers.Rows[rowIndex];
                    row.Tag = userId;

                    if (status.Equals("Suspended", StringComparison.OrdinalIgnoreCase))
                    {
                        row.DefaultCellStyle.BackColor = Color.MistyRose;
                        row.DefaultCellStyle.ForeColor = Color.DarkRed;
                    }
                }

                _dgvUsers.ClearSelection();

                if (_lblStatusBar != null)
                {
                    int startItem = _totalUsersCount == 0 ? 0 : (_currentPage * PageSize) + 1;
                    int endItem = (_currentPage * PageSize) + shownCount;
                    _lblStatusBar.Text =
                        $"PAGE {_currentPage + 1} OF {totalPages}   •   SHOWING {startItem}–{endItem} OF {_totalUsersCount} REGISTERED USERS   •   DOUBLE-CLICK A ROW TO INSPECT";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading registered users:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task ShowUserDetailsPopupAsync(int userId)
        {
            try
            {
                using var conn = new MySqlConnection(_connString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT 
                        u.FullName,
                        u.Username,
                        u.BarangayIDNumber,
                        u.Role,
                        u.Zone,
                        u.Phone_Number,
                        u.Points,
                        COALESCE(cw.AccountStatus, 'Active') AS AccountStatus,
                        (
                            SELECT COUNT(*)
                            FROM Quest_Memberships qm
                            WHERE qm.WorkerID = cw.WorkerID AND qm.Status = 'Completed'
                        ) AS CompletedQuests
                    FROM Users u
                    LEFT JOIN Citizen_Worker cw ON u.UserID = cw.UserID
                    WHERE u.UserID = @uid
                    LIMIT 1;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@uid", userId);
                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync()) return;

                string info =
                    $"Full Name: {reader["FullName"]}\n" +
                    $"Username: @{reader["Username"]}\n" +
                    $"Barangay / Officer ID: {reader["BarangayIDNumber"]}\n" +
                    $"Role: {reader["Role"]}\n" +
                    $"Assigned / Home Zone: {reader["Zone"]}\n" +
                    $"Contact Number: {reader["Phone_Number"]}\n" +
                    $"Current Points Balance: {reader["Points"]} PTS\n" +
                    $"Completed Quests: {reader["CompletedQuests"]}\n" +
                    $"Account Status: {reader["AccountStatus"]}";

                MessageBox.Show(info, "Community User Activity Summary", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load user summary:\n{ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NavigateTo(Form nextForm)
        {
            nextForm.FormClosed += (s, args) => Application.Exit();
            nextForm.Show();
            this.Hide();
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

        // Designer-bound event handlers
        private async void update_Click(object sender, EventArgs e)
        {
            if (update != null) update.Enabled = false;
            try
            {
                await LoadRegisteredUsersAsync();
                MessageBox.Show("Registered users list has been updated.", "Synced", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            finally
            {
                if (update != null) update.Enabled = true;
            }
        }

        private void dispatcherdashboard_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form17());
        }

        private void dispatcherprofile_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form19());
        }

        private void worksubmissions_Click(object sender, EventArgs e)
        {
            NavigateTo(new Form22());
        }

        private async void back_Click(object sender, EventArgs e)
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                await LoadRegisteredUsersAsync();
            }
            else
            {
                NavigateTo(new Form17());
            }
        }

        private async void next_Click(object sender, EventArgs e)
        {
            if ((_currentPage + 1) * PageSize < _totalUsersCount)
            {
                _currentPage++;
                await LoadRegisteredUsersAsync();
            }
            else
            {
                MessageBox.Show("You are already on the last page of registered users.", "End of List", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}