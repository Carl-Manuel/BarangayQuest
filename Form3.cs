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
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;"; //DB CONNECTION

        private async void btnRegister_Click(object sender, EventArgs e)
        {
            await RegisterCitizenWorkerAsync();
        }

        private async Task RegisterCitizenWorkerAsync()
        {
            string fullName = txtFullName.Text.Trim();
            string barangayId = txtBarangayID.Text.Trim();
            string contact = txtContactNumber.Text.Trim();
            string username = txtUsername.Text.Trim();
            string passcode = txtPasscode.Text;
            string verifyPasscode = txtVerifyPasscode.Text;

            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(barangayId) ||
                string.IsNullOrWhiteSpace(contact) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(passcode))
            {
                MessageBox.Show("Please fill in all required fields.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (passcode != verifyPasscode)
            {
                MessageBox.Show("Passcode / PIN and Verify Passcode do not match.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtVerifyPasscode.Clear();
                txtVerifyPasscode.Focus();
                return;
            }

            btnRegister.Enabled = false;

            try
            {
                using var conn = new MySqlConnection(connString);
                await conn.OpenAsync();

                using var transaction = await conn.BeginTransactionAsync();

                try
                {
                    string insertUserSql = @"
                        INSERT INTO Users 
                            (BarangayIDNumber, Role, Username, Password, FullName, Address, Zone, Phone_Number, Points)
                        VALUES 
                            (@brgyId, 'CitizenWorker', @username, @password, @fullName, 'Not Specified', 'Unassigned', @phone, 0);";

                    long newUserId;
                    using (var userCmd = new MySqlCommand(insertUserSql, conn, transaction))
                    {
                        userCmd.Parameters.AddWithValue("@brgyId", barangayId);
                        userCmd.Parameters.AddWithValue("@username", username);
                        userCmd.Parameters.AddWithValue("@password", HashPasscode(passcode));
                        userCmd.Parameters.AddWithValue("@fullName", fullName);
                        userCmd.Parameters.AddWithValue("@phone", contact);

                        await userCmd.ExecuteNonQueryAsync();
                        newUserId = userCmd.LastInsertedId;
                    }

                    string insertWorkerSql = @"
                    INSERT INTO Citizen_Worker (UserID, AccountStatus)
                    VALUES (@userId, 'Active');";

                    using (var workerCmd = new MySqlCommand(insertWorkerSql, conn, transaction))
                    {
                        workerCmd.Parameters.AddWithValue("@userId", newUserId);
                        await workerCmd.ExecuteNonQueryAsync();
                    }

                    await transaction.CommitAsync();

                    MessageBox.Show("Citizen Worker account registered successfully!", "Registration Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    ClearFields();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                MessageBox.Show("That Barangay ID Number or Username is already registered.", "Duplicate Account", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Registration failed: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnRegister.Enabled = true;
            }
        }

        private static string HashPasscode(string rawPasscode)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawPasscode));
            return Convert.ToHexString(bytes);
        }

        private void ClearFields()
        {
            txtFullName.Clear();
            txtBarangayID.Clear();
            txtContactNumber.Clear();
            txtUsername.Clear();
            txtPasscode.Clear();
            txtVerifyPasscode.Clear();
        }

        private void returntorole_Click(object sender, EventArgs e)
        {
            Form2 Form2 = new Form2();
            Form2.Show();
            this.Hide();
        }
    }
}
