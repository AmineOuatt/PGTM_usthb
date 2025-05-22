using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using ThesesModels;
using System.Diagnostics;

namespace DataGridNamespace.Admin
{
    public partial class DemandsView : UserControl
    {
        private ObservableCollection<Theses> pendingTheses;
        private ObservableCollection<Theses> filteredTheses;
        private const int ItemsPerPage = 10;
        private int currentPage = 1;
        private int totalPages = 1;

        public DemandsView()
        {
            try
            {
                InitializeComponent();
                pendingTheses = new ObservableCollection<Theses>();
                filteredTheses = new ObservableCollection<Theses>();
                LoadPendingTheses();
                PopulateYearFilter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DemandsView constructor: {ex.Message}");
                MessageBox.Show("Error initializing view. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DemandsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                LoadPendingTheses();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DemandsView_Loaded: {ex.Message}");
                MessageBox.Show("Error loading theses. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPendingTheses()
        {
            try
            {
                pendingTheses.Clear();
                filteredTheses.Clear();

                string connectionString = AppConfig.CloudSqlConnectionString;
                
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT t.id, t.titre, t.auteur, t.speciality, t.Type, 
                                   t.mots_cles as MotsCles, t.annee, t.Resume, t.fichier, t.user_id as UserId, t.status
                                   FROM theses t
                                   WHERE t.status = 'pending'
                                   ORDER BY t.id ASC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    var thesis = new Theses
                                    {
                                        Id = reader.GetInt32("id"),
                                        Titre = reader.IsDBNull(reader.GetOrdinal("titre")) ? "" : reader.GetString("titre"),
                                        Auteur = reader.IsDBNull(reader.GetOrdinal("auteur")) ? "" : reader.GetString("auteur"),
                                        Speciality = reader.IsDBNull(reader.GetOrdinal("speciality")) ? "" : reader.GetString("speciality"),
                                        Type = (TypeThese)Enum.Parse(typeof(TypeThese), reader.GetString("Type")),
                                        MotsCles = reader.IsDBNull(reader.GetOrdinal("MotsCles")) ? "" : reader.GetString("MotsCles"),
                                        Annee = reader.GetDateTime("annee"),
                                        Resume = reader.IsDBNull(reader.GetOrdinal("Resume")) ? "" : reader.GetString("Resume"),
                                        Fichier = reader.IsDBNull(reader.GetOrdinal("fichier")) ? "" : reader.GetString("fichier"),
                                        UserId = reader.GetInt32("UserId"),
                                        Status = reader.GetString("status")
                                    };
                                    pendingTheses.Add(thesis);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error reading thesis record: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                ApplyFilters();
                UpdatePagination();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadPendingTheses: {ex.Message}");
                MessageBox.Show($"Error loading pending theses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PopulateYearFilter()
        {
            try
            {
                var years = pendingTheses.Select(t => t.Annee.Year).Distinct().OrderByDescending(y => y).ToList();
                YearFilterComboBox.Items.Clear();
                YearFilterComboBox.Items.Add(new ComboBoxItem { Content = "All Years", IsSelected = true });
                foreach (var year in years)
                {
                    YearFilterComboBox.Items.Add(new ComboBoxItem { Content = year.ToString() });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error populating year filter: {ex.Message}");
                MessageBox.Show($"Error populating year filter: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = pendingTheses.AsEnumerable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    string searchText = SearchTextBox.Text.ToLower();
                    filtered = filtered.Where(t =>
                        (t.Titre != null && t.Titre.ToLower().Contains(searchText)) ||
                        (t.Auteur != null && t.Auteur.ToLower().Contains(searchText)) ||
                        (t.Speciality != null && t.Speciality.ToLower().Contains(searchText)));
                }

                // Apply type filter
                if (TypeFilterComboBox.SelectedItem is ComboBoxItem selectedType &&
                    selectedType.Content.ToString() != "All Types")
                {
                    if (Enum.TryParse(selectedType.Content.ToString(), out TypeThese selectedTypeThese))
                    {
                        filtered = filtered.Where(t => t.Type == selectedTypeThese);
                    }
                }

                // Apply year filter
                if (YearFilterComboBox.SelectedItem is ComboBoxItem selectedYear &&
                    selectedYear.Content.ToString() != "All Years")
                {
                    if (int.TryParse(selectedYear.Content.ToString(), out int year))
                    {
                         filtered = filtered.Where(t => t.Annee.Year == year);
                    }
                }

                filteredTheses.Clear();
                foreach (var thesis in filtered)
                {
                    filteredTheses.Add(thesis);
                }

                UpdatePagination();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying filters: {ex.Message}");
                MessageBox.Show($"Error applying filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdatePagination()
        {
            try
            {
                totalPages = (int)Math.Ceiling(filteredTheses.Count / (double)ItemsPerPage);
                currentPage = Math.Min(currentPage, totalPages);
                currentPage = Math.Max(1, currentPage);

                // Update pagination buttons
                FirstPageButton.IsEnabled = currentPage > 1;
                PreviousPageButton.IsEnabled = currentPage > 1;
                NextPageButton.IsEnabled = currentPage < totalPages;
                LastPageButton.IsEnabled = currentPage < totalPages;

                // Update page number buttons
                PaginationItemsControl.Items.Clear();
                int startPage = Math.Max(1, currentPage - 2);
                int endPage = Math.Min(totalPages, startPage + 4);
                startPage = Math.Max(1, endPage - 4);

                for (int i = startPage; i <= endPage; i++)
                {
                    var pageButton = new Button
                    {
                        Content = i.ToString(),
                        Style = i == currentPage ? 
                            (Style)FindResource("ActivePageButtonStyle") : 
                            (Style)FindResource("PaginationButtonStyle"),
                        Tag = i
                    };
                    pageButton.Click += PageButton_Click;
                    PaginationItemsControl.Items.Add(pageButton);
                }

                // Update displayed items
                var pagedItems = filteredTheses
                    .Skip((currentPage - 1) * ItemsPerPage)
                    .Take(ItemsPerPage);
                PendingThesesGrid.ItemsSource = pagedItems;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error updating pagination: {ex.Message}");
                MessageBox.Show($"Error updating pagination: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int page)
            {
                currentPage = page;
                UpdatePagination();
            }
        }

        private void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = 1;
            UpdatePagination();
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                UpdatePagination();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                UpdatePagination();
            }
        }

        private void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = totalPages;
            UpdatePagination();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void YearFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadPendingTheses();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RefreshButton_Click: {ex.Message}");
                MessageBox.Show("Error refreshing theses. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to accept the thesis '{thesis.Titre}'?",
                        "Confirm Acceptance",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        string connectionString = AppConfig.CloudSqlConnectionString;
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "UPDATE theses SET status = 'accepted' WHERE id = @id";
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", thesis.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                
                                if (rowsAffected > 0)
                                {
                                    pendingTheses.Remove(thesis);
                                    ApplyFilters();
                                    MessageBox.Show("Thesis accepted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to accept thesis. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in AcceptButton_Click: {ex.Message}");
                    MessageBox.Show($"Error accepting thesis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    var result = MessageBox.Show(
                        $"Are you sure you want to decline the thesis '{thesis.Titre}'?",
                        "Confirm Decline",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        string connectionString = AppConfig.CloudSqlConnectionString;
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "UPDATE theses SET status = 'declined' WHERE id = @id";
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", thesis.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                
                                if (rowsAffected > 0)
                                {
                                    pendingTheses.Remove(thesis);
                                    ApplyFilters();
                                    MessageBox.Show("Thesis declined successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to decline thesis. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in DeclineButton_Click: {ex.Message}");
                    MessageBox.Show($"Error declining thesis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
} 