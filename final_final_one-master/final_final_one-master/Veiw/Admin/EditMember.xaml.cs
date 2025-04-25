using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using UserModels;
using DataGridNamespace;

namespace DataGridNamespace.Admin
{
    public partial class EditMember : Window
    {
        private readonly User _user;
        private readonly Action _refreshCallback;

        public EditMember(User user, Action refreshCallback = null)
        {
            InitializeComponent();
            _user = user;
            _refreshCallback = refreshCallback;

            // Populate fields with user data
            UserIdTextBox.Text = user.Id.ToString();
            NameTextBox.Text = user.Nom;
            EmailTextBox.Text = user.Email;
            
            // Set the role in the ComboBox
            var roleItem = RoleComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString().ToLower() == user.Role.ToString().ToLower());
            if (roleItem != null)
            {
                RoleComboBox.SelectedItem = roleItem;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Please enter a name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    MessageBox.Show("Please enter an email.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate email format
                try
                {
                    var email = new System.Net.Mail.MailAddress(EmailTextBox.Text);
                }
                catch
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (RoleComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a role.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Get the selected role string and convert it to proper case
                string selectedRoleStr = ((ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString().ToLower();
                RoleUtilisateur selectedRole = ConvertStringToRole(selectedRoleStr);

                // Check if trying to change admin@yourapp.com
                if (_user.Email.ToLower() == "admin@yourapp.com")
                {
                    MessageBox.Show("Cannot modify the super admin account.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Using AppConfig directly to get the connection string
                string connectionString = AppConfig.CloudSqlConnectionString;
                Debug.WriteLine("Updating user data with connection string from AppConfig");
                
                string query = "UPDATE users SET nom = @name, role = @role, email = @email WHERE id = @id";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", NameTextBox.Text);
                        cmd.Parameters.AddWithValue("@role", selectedRoleStr);
                        cmd.Parameters.AddWithValue("@email", EmailTextBox.Text);
                        cmd.Parameters.AddWithValue("@id", _user.Id);

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected == 0)
                        {
                            MessageBox.Show("No rows were updated. The user may no longer exist in the database.", 
                                "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }

                // Update the user object with new values
                _user.Nom = NameTextBox.Text;
                _user.Email = EmailTextBox.Text;
                _user.Role = selectedRole;

                // Refresh the members list if callback provided
                _refreshCallback?.Invoke();

                MessageBox.Show("Member updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating member: {ex.Message}");
                MessageBox.Show($"Error updating member: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private RoleUtilisateur ConvertStringToRole(string roleString)
        {
            switch (roleString.ToLower())
            {
                case "admin":
                    return RoleUtilisateur.Admin;
                case "etudiant":
                    return RoleUtilisateur.Etudiant;
                case "simpleuser":
                    return RoleUtilisateur.SimpleUser;
                default:
                    return RoleUtilisateur.SimpleUser;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}