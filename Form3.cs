using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarangayQuest
{
    public partial class Form3 : Form
    {
        private readonly string connString = "Server=localhost;Database=barangayquest_db;User ID=root;Password=091916;"; //DB CONNECTION

        public Form3()
        {
            InitializeComponent();
            WireRegistrationUX();
        }

        private void WireRegistrationUX()
        {
            // Mask Passcode / PIN fields
            txtPasscode.UseSystemPasswordChar = true;
            txtVerifyPasscode.UseSystemPasswordChar = true;

            // Enforce numeric-only input up to 11 digits on Contact Number
            txtContactNumber.MaxLength = 11;
            txtContactNumber.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
        }

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

            // Check required fields
            if (string.IsNullOrWhiteSpace(fullName) ||
                string.IsNullOrWhiteSpace(barangayId) ||
                string.IsNullOrWhiteSpace(contact) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(passcode) ||
                string.IsNullOrWhiteSpace(verifyPasscode))
            {
                MessageBox.Show("Please fill in all required registration fields.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate Full Name
            if (fullName.Length < 2 || fullName.All(char.IsDigit))
            {
                MessageBox.Show("Please enter a valid Full Name (at least 2 letters).", "Invalid Full Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtFullName.Focus();
                return;
            }

            // Validate Barangay ID Number
            if (barangayId.Length < 3)
            {
                MessageBox.Show("Please enter a valid Barangay ID Number.", "Invalid Barangay ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtBarangayID.Focus();
                return;
            }

            // Validate Contact Number (must be strictly 11 digits)
            if (contact.Length != 11 || !contact.All(char.IsDigit))
            {
                MessageBox.Show("Contact Number must be exactly 11 numeric digits (e.g., 09123456789).", "Invalid Contact Number", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtContactNumber.Focus();
                txtContactNumber.SelectAll();
                return;
            }

            // Validate Username
            if (username.Length < 4 || username.Contains(' '))
            {
                MessageBox.Show("Username / Account ID must be at least 4 characters long and contain no spaces.", "Invalid Username", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            // Validate Passcode / PIN
            if (passcode.Length < 4)
            {
                MessageBox.Show("Passcode / PIN must be at least 4 characters long.", "Weak Passcode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPasscode.Focus();
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

                //  Pre-check duplicate Username or Barangay ID for clear error feedback
                string duplicateSql = @"
                    SELECT Username, BarangayIDNumber
                    FROM Users
                    WHERE Username = @username OR BarangayIDNumber = @brgyId
                    LIMIT 1;";

                using (var dupCmd = new MySqlCommand(duplicateSql, conn))
                {
                    dupCmd.Parameters.AddWithValue("@username", username);
                    dupCmd.Parameters.AddWithValue("@brgyId", barangayId);

                    using var reader = await dupCmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        string existingUser = reader["Username"]?.ToString() ?? "";
                        if (string.Equals(existingUser, username, StringComparison.OrdinalIgnoreCase))
                        {
                            MessageBox.Show("That Username / Account ID is already taken. Please choose another one.", "Duplicate Username", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtUsername.Focus();
                            txtUsername.SelectAll();
                        }
                        else
                        {
                            MessageBox.Show("That Barangay ID Number is already registered to an account.", "Duplicate Barangay ID", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            txtBarangayID.Focus();
                            txtBarangayID.SelectAll();
                        }
                        return;
                    }
                }

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
            Form2.FormClosed += (s, args) => this.Close();
            Form2.Show();
            this.Hide();
        }

        // Taena di ko to mahanap
        private void label1_Click(object sender, EventArgs e) { }
        private void textBox3_TextChanged(object sender, EventArgs e) { }
        private void textBox5_TextChanged(object sender, EventArgs e) { }
    }
}