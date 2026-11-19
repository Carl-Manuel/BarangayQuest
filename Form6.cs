using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form6 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";

        public Form6()
        {
            InitializeComponent();
            this.Load += Form6_Load;
        }

        private async void Form6_Load(object? sender, EventArgs e)
        {
            ResetProfileDefaults();
            await LoadCitizenProfileAsync();
        }

        private void logout_Click(object sender, EventArgs e)
        {
            UserSession.Clear();
            Form4 Form4 = new Form4();
            Form4.FormClosed += (s, args) => this.Close();
            Form4.Show();
            this.Hide();
        }

        private void backbutton_Click(object sender, EventArgs e)
        {
            Form5 Form5 = new Form5();
            Form5.FormClosed += (s, args) => this.Close();
            Form5.Show();
            this.Hide();
        }

        private void ResetProfileDefaults()
        {
            lblFullName.Text = "None"; // Username
            lblUserId.Text = UserSession.WorkerID > 0 ? $"WORKER ID: #{UserSession.WorkerID}" : "USER ID: None";
            lblWorkerRank.Text = "NEWBIE WORKER";
            lblTasksFinished.Text = "TASKS FINISHED: 0";
            lblLguName.Text = string.IsNullOrWhiteSpace(UserSession.FullName) ? "None" : UserSession.FullName.ToUpper(); // Full Name
            lblRole.Text = "CITIZEN WORKER"; // Role
            lblTotalPoints.Text = UserSession.Points.ToString();
            lblPhoneNumber.Text = "None";
        }

        private async Task LoadCitizenProfileAsync()
        {
            if (UserSession.UserID <= 0) return;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                string profileSql = @"
                    SELECT u.*, cw.WorkerID
                    FROM Users u
                    INNER JOIN Citizen_Worker cw ON u.UserID = cw.UserID
                    WHERE u.UserID = @userId
                    LIMIT 1;";

                using (var cmd = new MySqlCommand(profileSql, conn))
                {
                    cmd.Parameters.AddWithValue("@userId", UserSession.UserID);

                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        string username = HasColumn(reader, "Username") && reader["Username"] != DBNull.Value
                            ? reader["Username"].ToString() ?? ""
                            : "";
                        string fullName = HasColumn(reader, "FullName") && reader["FullName"] != DBNull.Value
                            ? reader["FullName"].ToString() ?? ""
                            : "";
                        string role = HasColumn(reader, "Role") && reader["Role"] != DBNull.Value
                            ? reader["Role"].ToString() ?? ""
                            : "Citizen Worker";
                        string brgyId = HasColumn(reader, "BarangayIDNumber") && reader["BarangayIDNumber"] != DBNull.Value
                            ? reader["BarangayIDNumber"].ToString() ?? ""
                            : "";
                        string phone = HasColumn(reader, "Phone_Number") && reader["Phone_Number"] != DBNull.Value
                            ? reader["Phone_Number"].ToString() ?? ""
                            : "";
                        int points = HasColumn(reader, "Points") && reader["Points"] != DBNull.Value
                            ? Convert.ToInt32(reader["Points"])
                            : 0;
                        int workerId = Convert.ToInt32(reader["WorkerID"]);

                        UserSession.FullName = fullName;
                        UserSession.WorkerID = workerId;
                        UserSession.BarangayIDNumber = brgyId;
                        UserSession.Points = points;

                        // Username
                        lblFullName.Text = string.IsNullOrWhiteSpace(username) ? "None" : username;

                        // User ID
                        lblUserId.Text = string.IsNullOrWhiteSpace(brgyId)
                            ? $"WORKER ID: #{workerId}"
                            : $"ID: {brgyId}  |  WORKER #{workerId}";

                        // Full Name
                        lblLguName.Text = string.IsNullOrWhiteSpace(fullName) ? "None" : fullName.ToUpper();

                        // Role
                        lblRole.Text = string.IsNullOrWhiteSpace(role) ? "CITIZEN WORKER" : role.ToUpper();

                        // Points & Phone Number
                        lblTotalPoints.Text = points.ToString();
                        lblPhoneNumber.Text = string.IsNullOrWhiteSpace(phone) ? "None" : phone;
                    }
                }

                int completedCount = await GetCompletedQuestCountAsync(conn);
                lblTasksFinished.Text = $"TASKS FINISHED: {completedCount}";
                lblWorkerRank.Text = completedCount switch
                {
                    >= 6 => "EXPERIENCED WORKER",
                    _ => "NEWBIE WORKER"
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load profile: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<int> GetCompletedQuestCountAsync(MySqlConnection conn)
        {
            try
            {
                string countSql = @"
                    SELECT COUNT(*)
                    FROM Quest_Assignments
                    WHERE WorkerID = @workerId
                      AND AssignmentStatus = 'Completed';";

                using var cmd = new MySqlCommand(countSql, conn);
                cmd.Parameters.AddWithValue("@workerId", UserSession.WorkerID);

                object? result = await cmd.ExecuteScalarAsync();
                return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
            }
            catch (MySqlException)
            {
                return 0;
            }
        }

        private static bool HasColumn(MySqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}