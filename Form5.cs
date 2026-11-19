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
    public partial class Form5 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;";

        public Form5()
        {
            InitializeComponent();
        }

        private async void Form5_Load(object sender, EventArgs e)
        {
            ResetDisplayToDefaults();
            await LoadDashboardDisplayAsync();
        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void citizenprofile_Click(object sender, EventArgs e)
        {
            Form6 Form6 = new Form6();
            Form6.Show();
            this.Hide();
        }

        // Set blank values to 0/None
        private void ResetDisplayToDefaults()
        {
            lblPointsEarned.Text = "0";
            lblActiveQuestTitle.Text = "None";
            lblActiveQuestDesc.Text = "None";
            lblRewardPoints.Text = "0";
            lblRewardSupply.Text = "None";
            lblRedeemedTools.Text = "None";
        }

        private async Task LoadDashboardDisplayAsync()
        {
            if (UserSession.UserID <= 0)
            {
                ResetDisplayToDefaults();
                return;
            }

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                await LoadPointsEarnedAsync(conn);

                await LoadActiveQuestAsync(conn);

                await LoadRedeemedToolsAsync(conn);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task LoadPointsEarnedAsync(MySqlConnection conn)
        {
            string sql = "SELECT COALESCE(Points, 0) FROM Users WHERE UserID = @userId LIMIT 1;";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", UserSession.UserID);

            object? result = await cmd.ExecuteScalarAsync();
            int points = result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;

            UserSession.Points = points;
            lblPointsEarned.Text = points.ToString();
        }

        private async Task LoadActiveQuestAsync(MySqlConnection conn)
        {
            try
            {
                string sql = @"
                    SELECT q.*
                    FROM Quests q
                    INNER JOIN Quest_Assignments qa ON q.QuestID = qa.QuestID
                    WHERE qa.WorkerID = @workerId
                      AND qa.AssignmentStatus IN ('Accepted', 'In Progress', 'Active')
                    ORDER BY qa.AssignedDate DESC
                    LIMIT 1;";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@workerId", UserSession.WorkerID);

                using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    lblActiveQuestTitle.Text = "None";
                    lblActiveQuestDesc.Text = "None";
                    lblRewardPoints.Text = "0";
                    lblRewardSupply.Text = "None";
                    return;
                }

                string title = reader["Title"]?.ToString() ?? "";
                lblActiveQuestTitle.Text = string.IsNullOrWhiteSpace(title) ? "None" : title;

                string desc = HasColumn(reader, "Description") && reader["Description"] != DBNull.Value
                    ? reader["Description"].ToString() ?? ""
                    : $"{reader["Category"]} ({reader["Difficulty"]})";
                lblActiveQuestDesc.Text = string.IsNullOrWhiteSpace(desc) ? "None" : desc;

                int rewardPts = reader["PointsReward"] != DBNull.Value ? Convert.ToInt32(reader["PointsReward"]) : 0;
                lblRewardPoints.Text = rewardPts.ToString();

                if (HasColumn(reader, "RewardSupply") && reader["RewardSupply"] != DBNull.Value && !string.IsNullOrWhiteSpace(reader["RewardSupply"].ToString()))
                {
                    lblRewardSupply.Text = reader["RewardSupply"].ToString();
                }
                else if (HasColumn(reader, "BasePay") && reader["BasePay"] != DBNull.Value && Convert.ToDecimal(reader["BasePay"]) > 0)
                {
                    lblRewardSupply.Text = $"PHP {Convert.ToDecimal(reader["BasePay"]):N2}";
                }
                else
                {
                    lblRewardSupply.Text = "None";
                }
            }
            catch (MySqlException)
            {
                lblActiveQuestTitle.Text = "None";
                lblActiveQuestDesc.Text = "None";
                lblRewardPoints.Text = "0";
                lblRewardSupply.Text = "None";
            }
        }

        private async Task LoadRedeemedToolsAsync(MySqlConnection conn)
        {
            try
            {
                string sql = @"
                    SELECT ItemName
                    FROM Redemptions
                    WHERE WorkerID = @workerId
                      AND Category = 'Tool';";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@workerId", UserSession.WorkerID);

                using var reader = await cmd.ExecuteReaderAsync();
                var tools = new List<string>();

                while (await reader.ReadAsync())
                {
                    string item = reader["ItemName"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(item))
                        tools.Add(item);
                }

                lblRedeemedTools.Text = tools.Count > 0 ? string.Join(", ", tools) : "None";
            }
            catch (MySqlException)
            {
                lblRedeemedTools.Text = "None";
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

        private void questmap_Click(object sender, EventArgs e)
        {
            var Form23 = new Form23(); 
            Form23.Show();
            this.Hide();
        }
    }
}