using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using ThesesModels;
using System.Diagnostics;
using DataGridNamespace.Services;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DataGridNamespace.Admin
{
    public partial class DemandsView : UserControl
    {
        private ObservableCollection<Theses> pendingTheses;
        private ObservableCollection<Theses> filteredTheses;
        private const int ItemsPerPage = 10;
        private int currentPage = 1;
        private int totalPages = 1;
        private readonly EmailService _emailService;

        public DemandsView()
        {
            try
            {
                InitializeComponent();
                pendingTheses = new ObservableCollection<Theses>();
                filteredTheses = new ObservableCollection<Theses>();
                _emailService = new EmailService();
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
                var years = pendingTheses
                    .Select(t => t.Annee.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToList();

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
                // Ensure pendingTheses is not null before proceeding
                if (pendingTheses == null)
                {
                    Debug.WriteLine("pendingTheses collection is null in ApplyFilters.");
                    return;
                }

                var filtered = pendingTheses.AsEnumerable()
                    // Add a safety check for null thesis objects in the collection
                    .Where(t => t != null);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    string searchText = SearchTextBox.Text.ToLower();
                    filtered = filtered.Where(t =>
                        // Explicit null checks for properties before accessing them
                        (t.Titre != null && t.Titre.ToLower().Contains(searchText)) ||
                        (t.Auteur != null && t.Auteur.ToLower().Contains(searchText)) ||
                        (t.Speciality != null && t.Speciality.ToLower().Contains(searchText)));
                }

                // Apply type filter
                // Explicitly check if TypeFilterComboBox is not null
                if (TypeFilterComboBox != null && TypeFilterComboBox.SelectedItem is ComboBoxItem selectedType &&
                    selectedType.Content.ToString() != "All Types")
                {
                    // Ensure the string can be parsed to TypeThese and the thesis Type is not null (though TypeThese is a value type, safer check)
                    if (Enum.TryParse(selectedType.Content.ToString(), out TypeThese selectedTypeThese))
                    {
                        filtered = filtered.Where(t => t.Type == selectedTypeThese);
                    }
                    // If parsing fails, the filter won't be applied for this selection, which is safer.
                }

                // Apply year filter
                // Explicitly check if YearFilterComboBox is not null
                if (YearFilterComboBox != null && YearFilterComboBox.SelectedItem is ComboBoxItem selectedYear &&
                    selectedYear.Content.ToString() != "All Years")
                {
                    // Ensure the string can be parsed to an integer year
                    if (int.TryParse(selectedYear.Content.ToString(), out int year))
                    {
                         // Annee is DateTime (non-nullable), so no null check or ?. needed here
                         filtered = filtered.Where(t => t.Annee.Year == year);
                    }
                    // If parsing fails or Annee is null (though should not happen with DateTime), the filter won't be applied.
                }

                // Ensure filteredTheses is not null before clearing and adding
                if (filteredTheses == null)
                {
                     Debug.WriteLine("filteredTheses collection is null in ApplyFilters before clearing.");
                     filteredTheses = new ObservableCollection<Theses>(); // Re-initialize if null
                }

                filteredTheses.Clear();
                foreach (var thesis in filtered)
                {
                     // Add a safety check for null thesis object before adding to filtered collection
                     if (thesis != null)
                     {
                          filteredTheses.Add(thesis);
                     }
                     else
                     {
                         Debug.WriteLine("Null thesis object found in filtered enumerable.");
                     }
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

                // Update pagination buttons - Add null checks
                if (FirstPageButton != null) FirstPageButton.IsEnabled = currentPage > 1;
                if (PreviousPageButton != null) PreviousPageButton.IsEnabled = currentPage > 1;
                if (NextPageButton != null) NextPageButton.IsEnabled = currentPage < totalPages;
                if (LastPageButton != null) LastPageButton.IsEnabled = currentPage < totalPages;

                // Update page number buttons - Add null check
                if (PaginationItemsControl != null)
                {
                    PaginationItemsControl.Items.Clear();
                    int startPage = Math.Max(1, currentPage - 2);
                    int endPage = Math.Min(totalPages, startPage + 4);
                    startPage = Math.Max(1, endPage - 4);

                    for (int i = startPage; i <= endPage; i++)
                    {
                        // Add null check before finding resource
                        Style buttonStyle = (i == currentPage) ? 
                            (FindResource("ActivePageButtonStyle") as Style) : 
                            (FindResource("PaginationButtonStyle") as Style);

                        var pageButton = new Button
                        {
                            Content = i.ToString(),
                            Style = buttonStyle,
                            Tag = i
                        };
                        pageButton.Click += PageButton_Click;
                        PaginationItemsControl.Items.Add(pageButton);
                    }
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

        private async Task<string> GetThesisOwnerEmailAsync(int thesisId)
        {
            try
            {
                string connectionString = AppConfig.CloudSqlConnectionString;
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT u.email 
                                   FROM users u 
                                   INNER JOIN theses t ON u.id = t.user_id 
                                   WHERE t.id = @thesisId";
                    
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@thesisId", thesisId);
                        object result = await cmd.ExecuteScalarAsync();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting thesis owner email: {ex.Message}");
                throw;
            }
        }

        private string ShowRejectionReasonDialog()
        {
            var dialog = new Window
            {
                Title = "Rejection Reason",
                Width = 400,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) });

            var textBlock = new TextBlock
            {
                Text = "Please provide a reason for rejecting this thesis:",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(textBlock, 0);

            var textBox = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(textBox, 1);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 2);

            var okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 75,
                Height = 25
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            grid.Children.Add(textBlock);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            string result = null;

            okButton.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text))
                {
                    MessageBox.Show("Please provide a reason for rejection.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                result = textBox.Text;
                dialog.DialogResult = true;
                dialog.Close();
            };

            cancelButton.Click += (s, e) =>
            {
                dialog.DialogResult = false;
                dialog.Close();
            };

            if (dialog.ShowDialog() == true)
            {
                return result;
            }

            return null;
        }

        private async void AcceptButton_Click(object sender, RoutedEventArgs e)
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
                                    // Get the thesis owner's email
                                    string ownerEmail = await GetThesisOwnerEmailAsync(thesis.Id);
                                    if (!string.IsNullOrEmpty(ownerEmail))
                                    {
                                        await _emailService.SendThesisStatusEmailAsync(ownerEmail, thesis.Titre, "accepted");
                                    }

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

        private async void DeclineButton_Click(object sender, RoutedEventArgs e)
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
                        // Show rejection reason dialog
                        string rejectionReason = ShowRejectionReasonDialog();
                        if (rejectionReason == null)
                        {
                            return; // User cancelled
                        }

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
                                    // Get the thesis owner's email
                                    string ownerEmail = await GetThesisOwnerEmailAsync(thesis.Id);
                                    if (!string.IsNullOrEmpty(ownerEmail))
                                    {
                                        await _emailService.SendThesisStatusEmailAsync(ownerEmail, thesis.Titre, "declined", rejectionReason);
                                    }

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

        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    // Create a window to display thesis details
                    var detailsWindow = new Window
                    {
                        Title = "Thesis Details",
                        Width = 700,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        Background = new SolidColorBrush(Colors.White)
                    };

                    // Create the content
                    var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                    var grid = new Grid { Margin = new Thickness(30) };

                    // Define rows for the grid
                    for (int i = 0; i < 15; i++)
                    {
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }

                    // Add a header
                    var headerBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                        Padding = new Thickness(20, 15, 20, 15),
                        CornerRadius = new CornerRadius(8, 8, 0, 0)
                    };
                    var headerTitle = new TextBlock
                    {
                        Text = thesis.Titre ?? "Thesis Details",
                        Foreground = Brushes.White,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        TextWrapping = TextWrapping.Wrap
                    };
                    headerBorder.Child = headerTitle;
                    Grid.SetRow(headerBorder, 0);
                    Grid.SetColumnSpan(headerBorder, 2);
                    grid.Children.Add(headerBorder);

                    // Add a content border
                    var contentBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FFFF")),
                        Padding = new Thickness(25),
                        CornerRadius = new CornerRadius(0, 0, 8, 8),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                        BorderThickness = new Thickness(1)
                    };
                    var contentGrid = new Grid();
                    for (int i = 0; i < 14; i++)
                    {
                        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }
                    contentBorder.Child = contentGrid;
                    Grid.SetRow(contentBorder, 1);
                    Grid.SetColumnSpan(contentBorder, 2);
                    grid.Children.Add(contentBorder);

                    // Title
                    AddDetailRow(contentGrid, 0, "Title:", thesis.Titre ?? "N/A");

                    // Author
                    AddDetailRow(contentGrid, 2, "Author:", thesis.Auteur ?? "N/A");

                    // Specialty
                    AddDetailRow(contentGrid, 4, "Specialty:", thesis.Speciality ?? "N/A");

                    // Type
                    AddDetailRow(contentGrid, 6, "Type:", thesis.Type.ToString());

                    // Keywords
                    AddDetailRow(contentGrid, 8, "Keywords:", thesis.MotsCles ?? "N/A");

                    // Year
                    string yearText = "N/A";
                    if (thesis.Annee != default)
                    {
                        try
                        {
                            yearText = thesis.Annee.Year.ToString();
                        }
                        catch (Exception)
                        {
                            yearText = "N/A";
                        }
                    }
                    AddDetailRow(contentGrid, 10, "Year:", yearText);

                    // Abstract
                    AddDetailRow(contentGrid, 12, "Abstract:", thesis.Resume ?? "N/A", true);

                    // Add buttons at the bottom
                    var buttonsPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 20, 0, 0)
                    };

                    // Close button
                    var closeButton = new Button
                    {
                        Content = "Close",
                        Width = 120,
                        Height = 40,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        Style = (Style)FindResource("CircularActionButtonStyle")
                    };
                    closeButton.Click += (s, args) => detailsWindow.Close();
                    buttonsPanel.Children.Add(closeButton);

                    // Add buttons to the grid
                    Grid.SetRow(buttonsPanel, 3);
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.Children.Add(buttonsPanel);

                    scrollViewer.Content = grid;
                    detailsWindow.Content = scrollViewer;
                    detailsWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error showing thesis details: {ex.Message}");
                    MessageBox.Show($"Error showing thesis details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddDetailRow(Grid grid, int row, string label, string value, bool isMultiline = false)
        {
            // Ensure columns are defined
            if (grid.ColumnDefinitions.Count == 0)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Label column
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Value column
            }

            // Label
            var labelText = new TextBlock
            {
                Text = label,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRow(labelText, row);
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);

            // Value
            var valueText = new TextBlock
            {
                Text = value,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16),
                VerticalAlignment = VerticalAlignment.Top
            };
            if (isMultiline)
            {
                valueText.TextWrapping = TextWrapping.Wrap;
            }
            Grid.SetRow(valueText, row);
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);
        }

        private void ViewPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    if (string.IsNullOrEmpty(thesis.Fichier))
                    {
                        MessageBox.Show("No PDF file available for this thesis.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Create a window to display the PDF
                    var pdfWindow = new Window
                    {
                        Title = $"PDF Viewer - {thesis.Titre}",
                        Width = 800,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen
                    };

                    // Create a WebBrowser control to display the PDF
                    var webBrowser = new WebBrowser();
                    webBrowser.Navigate(new Uri(thesis.Fichier));
                    pdfWindow.Content = webBrowser;

                    pdfWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error viewing PDF: {ex.Message}");
                    MessageBox.Show($"Error viewing PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
} 