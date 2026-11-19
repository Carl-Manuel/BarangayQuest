using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using MySqlConnector;
using System.Security.Cryptography;

namespace BarangayQuest
{
    public partial class Form4 : Form
    {
        private readonly string connectionString = "server=localhost;user=root;password=091916;database=barangayquest_db";
        public Form4()
        {
            InitializeComponent();
            txtPassword.UseSystemPasswordChar = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form2 Form2 = new Form2();
            Form2.Show();
            this.Hide();
        }

        private void btnRegisterNewAcc_Click(object sender, EventArgs e)
        {
            Form3 Form3 = new Form3();
            Form3.Show();
            this.Hide();
        }

        private async void btnAccessDashboard_Click(object sender, EventArgs e)
        {
            await LoginCitizenWorkerAsync();
        }

        private async Task LoginCitizenWorkerAsync()
        {
            
            string identifier = txtLoginId.Text.Trim();
            string rawPassword = txtPassword.Text.Trim();

            if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace (rawPassword))
            {
                MessageBox.Show("Please enter your Username / Worker ID and Password.", "Missing Credentials", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnAccessDashboard.Enabled = false;

            try
            {

                using var conn = new MySqlConnection(connectionString);
                await conn.OpenAsync();

                string sql = @"
                    SELECT
                        u.UserID,
                        u.FullName,
                        u.Username,
                        u.BarangayIDNumber,
                        u.Points,
                        cw.WorkerID,
                        cw.AccountStatus
                    FROM Users u
                    INNER JOIN Citizen_Worker cw ON u.UserID = cw.UserID
                    WHERE u.Role = 'CitizenWorker'
                        AND (u.Username = @id OR u.BarangayIDNumber = @id OR CAST(cw.WorkerID AS CHAR) = @id)
                        AND u.Password = @password
                    LIMIT 1; ";

                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", identifier);
                cmd.Parameters.AddWithValue("@password", HashPasscode(rawPassword));

                using var reader = await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {

                    MessageBox.Show("Invalid Username/Worker ID or Password.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    return;

                }

                string status = reader.GetString("AccountStatus");
                if (!status.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"Your account status is currently '{status}'. Please contact your Barangay Dispatcher.", "Account Inactive", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                //Store the user details here!!
                UserSession.UserID = reader.GetInt32("UserID");
                UserSession.WorkerID = reader.GetInt32("WorkerID");
                UserSession.FullName = reader.GetString("FullName");
                UserSession.Username = reader.GetString("Username");
                UserSession.BarangayIDNumber = reader.GetString("BarangayIDNumber");
                UserSession.Points = reader.GetInt32("Points");

                MessageBox.Show($"Welcome back, {UserSession.FullName}!", "Login Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);

                var dashboard = new Form5();
                dashboard.FormClosed += (s, args) => this.Close();
                this.Hide();
                dashboard.Show();
            }

            catch (Exception ex)
            {
                MessageBox.Show($"Login error: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            finally
            {
                btnAccessDashboard.Enabled = true;        
            }

        }

        private static string HashPasscode(string rawPasscode)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawPasscode));
            return Convert.ToHexString(bytes);
        }

    }   

}
