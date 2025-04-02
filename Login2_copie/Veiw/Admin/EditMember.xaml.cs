using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using MySql.Data.MySqlClient;
using UserModels;

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
            NameTextBox.Text = user.Nom;
            EmailTextBox.Text = user.Email;
            RoleComboBox.SelectedItem = RoleComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString() == user.Role.ToString());
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

                if (RoleComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a role.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Validate password match if provided
                if (!string.IsNullOrEmpty(PasswordBox.Password) || !string.IsNullOrEmpty(ConfirmPasswordBox.Password))
                {
                    if (PasswordBox.Password != ConfirmPasswordBox.Password)
                    {
                        MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                string connectionString = "Server=localhost;Database=gestion_theses;User ID=root;Password=";
                string query = "UPDATE users SET Nom = @name, Role = @role, Email = @email";

                if (!string.IsNullOrEmpty(PasswordBox.Password))
                {
                    query += ", Password = @password";
                }

                query += " WHERE Id = @id";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", NameTextBox.Text);
                        cmd.Parameters.AddWithValue("@role", ((ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString());
                        cmd.Parameters.AddWithValue("@email", EmailTextBox.Text);
                        cmd.Parameters.AddWithValue("@id", _user.Id);

                        if (!string.IsNullOrEmpty(PasswordBox.Password))
                        {
                            cmd.Parameters.AddWithValue("@password", PasswordBox.Password);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                // Update the user object with new values
                _user.Nom = NameTextBox.Text;
                _user.Email = EmailTextBox.Text;
                _user.Role = Enum.Parse<RoleUtilisateur>(((ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString());
                if (!string.IsNullOrEmpty(PasswordBox.Password))
                {
                    _user.Password = PasswordBox.Password;
                }

                // Refresh the members list if callback provided
                _refreshCallback?.Invoke();

                MessageBox.Show("Member updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating member: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 