using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Globalization;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Diagnostics;
using UserModels;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace DataGridNamespace.Admin
{
    public partial class MembersListView : Page
    {
        private ObservableCollection<User> allMembers;
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
                                    string roleStr = reader.IsDBNull(reader.GetOrdinal("role")) ? "simpleuser" : reader.GetString("role").ToLower();

                                    // Convert role string to enum, defaulting to SimpleUser if not recognized
                                    RoleUtilisateur role = roleStr switch
                                    {
                                        "admin" => RoleUtilisateur.Admin,
                                        "etudiant" => RoleUtilisateur.Etudiant,
                                        _ => RoleUtilisateur.SimpleUser
                                    };

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
                int totalCount = GetTotalMemberCount();
                _totalPages = (int)Math.Ceiling((double)totalCount / _itemsPerPage);
                
                // Create a new CollectionViewSource for proper filtering and sorting
                membersViewSource = new CollectionViewSource { Source = allMembers };
                membersViewSource.Filter += MembersViewSource_Filter;
                
                // Set the ItemsSource to the filtered view
                MembersDataGrid.ItemsSource = membersViewSource.View;

                // Update the members counter text
                MembersCounterText.Text = $"Total: {totalCount} Members";

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
            if (e.Item is User user)
            {
                bool matchesSearch = true;
                bool matchesFilter = true;

                // Apply search filter
                if (!string.IsNullOrEmpty(SearchTextBox.Text))
                {
                    string searchText = SearchTextBox.Text.ToLower();
                    matchesSearch = (!string.IsNullOrEmpty(user.Nom) && user.Nom.ToLower().Contains(searchText)) ||
                                  (!string.IsNullOrEmpty(user.Email) && user.Email.ToLower().Contains(searchText)) ||
                                  user.Role.ToString().ToLower().Contains(searchText) ||
                                  user.Id.ToString().Contains(searchText);
                }

                // Apply role filter
                if (FilterComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content.ToString() != "All Members")
                {
                    string selectedRole = selectedItem.Content.ToString();
                    matchesFilter = user.Role.ToString() == selectedRole;
                }

                e.Accepted = matchesSearch && matchesFilter;
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (membersViewSource != null)
            {
                membersViewSource.View.Refresh();
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

        private void MembersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle the selection change logic here
            if (MembersDataGrid.SelectedItem is User selectedUser)
            {
                // For example, we can show details for the selected member
                Debug.WriteLine($"Selected user: {selectedUser.Nom}");
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

                // Check if the user is trying to delete themselves
                if (user.Id == DataGridNamespace.Session.CurrentUserId)
                {
                    MessageBox.Show("You cannot delete your own account.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Ask for confirmation with more details
                var result = MessageBox.Show($"Are you sure you want to delete the following user?\n\n" +
                                           $"Name: {user.Nom}\n" +
                                           $"Email: {user.Email}\n" +
                                           $"Role: {user.Role}\n\n" +
                                           "This action cannot be undone.",
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
                                    
                                    // Update pagination if needed
                                    int newTotalCount = GetTotalMemberCount();
                                    _totalPages = (int)Math.Ceiling((double)newTotalCount / _itemsPerPage);
                                    if (_currentPage > _totalPages)
                                    {
                                        _currentPage = _totalPages;
                                    }
                                    UpdatePaginationControls();
                                    
                                    MessageBox.Show("User deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to delete user. User not found in the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        Debug.WriteLine($"Database error deleting user: {ex.Message}");
                        MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting user: {ex.Message}");
                        MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is User user)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(user.Email))
                    {
                        MessageBox.Show("No email address is available for this member.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Open Gmail compose in the default browser
                    string url = $"https://mail.google.com/mail/?view=cm&fs=1&to={Uri.EscapeDataString(user.Email)}";

                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true   // ensures the URL opens in the default browser
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to open browser: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadMembers();
                MessageBox.Show("Members list refreshed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing members list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}



