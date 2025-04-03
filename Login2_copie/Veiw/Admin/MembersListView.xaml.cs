using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using UserModels;
using System.Diagnostics;

namespace DataGridNamespace.Admin
{
    public partial class MembersListView : Page
    {
        private ObservableCollection<User> _members;

        public MembersListView()
        {
            InitializeComponent();
            _members = new ObservableCollection<User>();
            MembersDataGrid.ItemsSource = _members;
            LoadMembers();
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
                    throw new ArgumentException($"Unknown role: {roleString}");
            }
        }

        private void LoadMembers()
        {
            try
            {
                _members.Clear();
                string connectionString = "Server=localhost;Database=gestion_theses;User ID=root;Password=";
                string query = "SELECT Id, Nom, Email, Role FROM users";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    try
                                    {
                                        var roleString = reader.GetString("Role");
                                        var role = ConvertStringToRole(roleString);

                                        var user = new User
                                        {
                                            Id = reader.GetInt32("Id"),
                                            Nom = reader.GetString("Nom"),
                                            Email = reader.GetString("Email"),
                                            Role = role
                                        };
                                        _members.Add(user);
                                    }
                                    catch (Exception ex)
                                    {
                                        MessageBox.Show($"Error processing user data: {ex.Message}\nPlease check the database structure.", 
                                            "Data Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        MessageBox.Show($"Database connection error: {ex.Message}\nPlease check your database connection settings.", 
                            "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}\nPlease contact system administrator.", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (MembersDataGrid.SelectedItem is User selectedUser)
            {
                var editWindow = new EditMember(selectedUser, LoadMembers);
                editWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Please select a member to edit.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (MembersDataGrid.SelectedItem is User selectedUser)
            {
                var result = MessageBox.Show("Are you sure you want to delete this member?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string connectionString = "Server=localhost;Database=gestion_theses;User ID=root;Password=";
                        string query = "DELETE FROM users WHERE Id = @id";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", selectedUser.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        _members.Remove(selectedUser);
                        MessageBox.Show("Member deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting member: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a member to delete.", "Selection Required", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}