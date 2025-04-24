using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Diagnostics;
using UserModels;
using System.Windows.Controls.Primitives;

namespace DataGridNamespace.Admin
{
    public partial class MembersListView : Page
    {
        private ObservableCollection<User> allMembers;

        private ObservableCollection<User> filteredMembers;
        private CollectionViewSource membersViewSource;
        private int _currentPage = 1;
        private int _itemsPerPage = 10;
        private int _totalPages;
        private bool isDataLoaded = false;

        public MembersListView()
        {
            InitializeComponent();
            this.Loaded += (s, e) =>
            {
                if (!isDataLoaded)
                {
                    LoadMembers();
                    isDataLoaded = true;
                }
            };
        }

        private void LoadMembers()
        {
            try
            {
                allMembers = new ObservableCollection<User>();
                string connectionString = AppConfig.CloudSqlConnectionString;
                string query = "SELECT id, nom, email, role FROM users ORDER BY id LIMIT @offset, @limit";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        // Set the query parameters for pagination
                        cmd.Parameters.AddWithValue("@offset", (_currentPage - 1) * _itemsPerPage);
                        cmd.Parameters.AddWithValue("@limit", _itemsPerPage);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    int userId = reader.GetInt32("id");
                                    string userName = reader.IsDBNull(reader.GetOrdinal("nom")) ? "Unknown" : reader.GetString("nom");
                                    string email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email");
                                    string roleStr = reader.IsDBNull(reader.GetOrdinal("role")) ? "simpleuser" : reader.GetString("role");

                                    RoleUtilisateur role = ConvertStringToRole(roleStr);

                                    var user = new User
                                    {
                                        Id = userId,
                                        Nom = userName,
                                        Email = email,
                                        Role = role
                                    };
                                    allMembers.Add(user);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error reading user record: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                // Calculate total pages based on the number of members and total count from the database
                _totalPages = (int)Math.Ceiling((double)GetTotalMemberCount() / _itemsPerPage);
                membersViewSource = new CollectionViewSource { Source = allMembers };
                membersViewSource.Filter += MembersViewSource_Filter;
                MembersDataGrid.ItemsSource = membersViewSource.View;

                UpdatePaginationControls(); // Update pagination buttons after loading the members
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private int GetTotalMemberCount()
        {
            try
            {
                string connectionString = AppConfig.CloudSqlConnectionString;
                string countQuery = "SELECT COUNT(*) FROM users";
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(countQuery, conn))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting total member count: {ex.Message}");
                return 0; // Default to 0 if an error occurs
            }
        }


        private void UpdatePaginationControls()
        {

            // Update pagination buttons
            FirstPageButton.IsEnabled = _currentPage > 1;
            PreviousPageButton.IsEnabled = _currentPage > 1;
            NextPageButton.IsEnabled = _currentPage < _totalPages;
            LastPageButton.IsEnabled = _currentPage < _totalPages;

            // Clear current page buttons
            PaginationItemsControl.Items.Clear();

            // Add page number buttons dynamically
            for (int i = 1; i <= _totalPages; i++)
            {
                var pageButton = new Button
                {
                    Content = i.ToString(),
                    Tag = i,
                    Style = (i == _currentPage) ? (Style)FindResource("ActivePageButtonStyle") : (Style)FindResource("PaginationButtonStyle")
                };
                pageButton.Click += (sender, e) =>
                {
                    _currentPage = (int)((Button)sender).Tag;
                    LoadMembers();
                };
                PaginationItemsControl.Items.Add(pageButton);
            }
        }

        private void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadMembers();
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadMembers();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadMembers();
            }
        }

        private void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = _totalPages;
            LoadMembers();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (membersViewSource != null)
            {
                membersViewSource.View.Refresh();
            }
        }


        private void MembersViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (SearchTextBox == null)
            {
                e.Accepted = true;
                return;
            }

            if (e.Item is User user && !string.IsNullOrEmpty(SearchTextBox.Text))
            {
                string searchText = SearchTextBox.Text.ToLower();
                bool nameMatches = !string.IsNullOrEmpty(user.Nom) && user.Nom.ToLower().Contains(searchText);
                bool emailMatches = !string.IsNullOrEmpty(user.Email) && user.Email.ToLower().Contains(searchText);
                bool roleMatches = user.Role.ToString().ToLower().Contains(searchText);

                e.Accepted = nameMatches || emailMatches || roleMatches;
            }
            else
            {
                e.Accepted = true;
            }
        }


        private RoleUtilisateur ConvertStringToRole(string roleString)
        {
            if (string.Equals(roleString, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RoleUtilisateur.Admin;
            }
            else if (string.Equals(roleString, "etudiant", StringComparison.OrdinalIgnoreCase))
            {
                return RoleUtilisateur.Etudiant;
            }
            else
            {
                return RoleUtilisateur.SimpleUser;
            }
        }
   
    private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is User user)
            {
                try
                {
                    EditMember editWindow = new EditMember(user);
                    if (editWindow.ShowDialog() == true)
                    {
                        // Refresh the list after editing
                        LoadMembers();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error opening edit window: {ex.Message}");
                    MessageBox.Show($"Error editing user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is User user)
            {
                if (user.Role == RoleUtilisateur.Admin)
                {
                    MessageBox.Show("Cannot delete an administrator account.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Ask for confirmation
                var result = MessageBox.Show($"Are you sure you want to delete the user: {user.Nom}?\nThis action cannot be undone.",
                                          "Delete Confirmation",
                                          MessageBoxButton.YesNo,
                                          MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Delete from database
                        string connectionString = AppConfig.CloudSqlConnectionString;
                        string query = "DELETE FROM users WHERE id = @userId";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@userId", user.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // Remove from the list
                                    allMembers.Remove(user);
                                    membersViewSource.View.Refresh();
                                    MessageBox.Show("User deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to delete user. User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting user: {ex.Message}");
                        MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private void RoleFilterButton_Click(object sender, RoutedEventArgs e)
        {
            // Create the ComboBox for role selection
            ComboBox roleFilterComboBox = new ComboBox();
            roleFilterComboBox.Items.Add("All Roles");
            roleFilterComboBox.Items.Add("Admin");
            roleFilterComboBox.Items.Add("Etudiant");
            roleFilterComboBox.Items.Add("SimpleUser");
            roleFilterComboBox.Items.Add("Enseignant");

            // Set default value
            roleFilterComboBox.SelectedIndex = 0;

            // Define the button for closing the filter dropdown
            Button applyFilterButton = new Button
            {
                Content = "Apply Filter",
                Style = (Style)FindResource("ActionButtonStyle"),
                Width = 100,
                Height = 35
            };
            applyFilterButton.Click += (s, args) =>
            {
                ApplyRoleFilter(roleFilterComboBox.SelectedItem.ToString());
            };

            // Display the ComboBox and button within a StackPanel
            StackPanel filterStackPanel = new StackPanel();
            filterStackPanel.Children.Add(roleFilterComboBox);
            filterStackPanel.Children.Add(applyFilterButton);

            // Create the Popup to show the dropdown
            Popup filterPopup = new Popup
            {
                Child = filterStackPanel,
                PlacementTarget = RoleFilterButton,
                Placement = PlacementMode.Bottom
            };

            filterPopup.IsOpen = true;
        }

        private void ApplyRoleFilter(string selectedRole)
        {
            // Apply the role filter logic here
            if (selectedRole == "All Roles")
            {
                membersViewSource.View.Filter = null; // No filter, show all
            }
            else
            {
                membersViewSource.View.Filter = (obj) =>
                {
                    if (obj is User user)
                    {
                        return user.Role.ToString() == selectedRole;
                    }
                    return false;
                };
            }

            // Refresh the DataGrid with the applied filter
            membersViewSource.View.Refresh();
        }
        private void MembersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle the selection change logic here
            if (MembersDataGrid.SelectedItem is User selectedUser)
            {
                // For example, we can show details for the selected member
                Debug.WriteLine($"Selected user: {selectedUser.Nom}");
            }
        }


    }

}



