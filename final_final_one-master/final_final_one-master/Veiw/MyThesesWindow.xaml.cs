using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using ThesesModels;
using UserModels;
using System.Data;
using MySql.Data.MySqlClient;
using DataGridNamespace.Admin;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Data;
using System.Globalization;

namespace DataGridNamespace
{
    public class IndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return (index + 1).ToString();
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string status)
            {
                return status.ToLower() switch
                {
                    "pending" => Color.FromRgb(255, 165, 0),  // Orange
                    "accepted" => Color.FromRgb(76, 175, 80),  // Green
                    "declined" => Color.FromRgb(244, 67, 54),  // Red
                    _ => Colors.Gray
                };
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public partial class MyThesesWindow : Page
    {
        private ObservableCollection<Theses> allTheses;
        private int currentUserId;
        private Theses selectedThesis;

        public MyThesesWindow()
        {
            InitializeComponent();
            currentUserId = Session.CurrentUserId;
            LoadUserTheses();
        }

        private void LoadUserTheses()
        {
            try
            {
                Debug.WriteLine("Starting LoadUserTheses...");
                string connectionString = AppConfig.CloudSqlConnectionString;
                Debug.WriteLine($"Connection string: {connectionString}");
                string query = @"SELECT id, titre, auteur, speciality, Type, mots_cles, annee, Resume, fichier, user_id, status 
                                   FROM theses 
                                   WHERE user_id = @userId";
                Debug.WriteLine($"Query: {query}");
                Debug.WriteLine($"Current User ID: {currentUserId}");

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        Debug.WriteLine("Attempting to open database connection...");
                        conn.Open();
                        Debug.WriteLine("Database connection opened successfully");

                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@userId", currentUserId);
                            Debug.WriteLine("Command parameters set");
                            
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                Debug.WriteLine("Creating DataTable...");
                                DataTable dt = new DataTable();
                                Debug.WriteLine("Filling DataTable...");
                                dt.Load(reader);
                                Debug.WriteLine($"DataTable filled with {dt.Rows.Count} rows");

                                allTheses = new ObservableCollection<Theses>();
                                foreach (DataRow row in dt.Rows)
                                {
                                    try
                                    {
                                        Debug.WriteLine($"Processing row ID: {row["id"]}");
                                        var thesis = new Theses
                                        {
                                            Id = Convert.ToInt32(row["id"]),
                                            Titre = row["titre"].ToString(),
                                            Auteur = row["auteur"].ToString(),
                                            Speciality = row["speciality"].ToString(),
                                            Type = Enum.TryParse<TypeThese>(row["type"].ToString(), out var typeEnum) ? typeEnum : default(TypeThese),
                                            Annee = (row["annee"] != DBNull.Value) ? Convert.ToDateTime(row["annee"]) : DateTime.MinValue,
                                            MotsCles = row["mots_cles"].ToString(),
                                            Resume = row["resume"].ToString(),
                                            Fichier = row["fichier"].ToString(),
                                            UserId = Convert.ToInt32(row["user_id"]),
                                            Status = row["status"].ToString()
                                        };
                                        Debug.WriteLine($"Thesis created with Status: {thesis.Status}");
                                        allTheses.Add(thesis);
                                    }
                                    catch (Exception rowEx)
                                    {
                                        Debug.WriteLine($"Error processing row: {rowEx.Message}");
                                        Debug.WriteLine($"Row data: {string.Join(", ", row.ItemArray.Select(x => x?.ToString() ?? "null"))}");
                                        // Continue processing other rows
                                    }
                                }
                            }
                        }
                    }
                    catch (MySqlException sqlEx)
                    {
                        Debug.WriteLine($"SQL Error: {sqlEx.Message}");
                        Debug.WriteLine($"Error Code: {sqlEx.Number}");
                        Debug.WriteLine($"SQL State: {sqlEx.SqlState}");
                        throw;
                    }
                }

                Debug.WriteLine($"Setting ItemsSource with {allTheses.Count} theses");
                ThesesDataGrid.ItemsSource = allTheses;
                UpdateThesisCounter();
                Debug.WriteLine("LoadUserTheses completed successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error in LoadUserTheses: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Debug.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                MessageBox.Show($"Error loading theses: {ex.Message}\n\nPlease check the debug output for more details.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateThesisCounter()
        {
            int count = allTheses?.Count ?? 0;
            ThesisCounterText.Text = $"({count} theses)";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTheses();
        }

        private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTheses();
        }

        private void FilterTheses()
        {
            if (allTheses == null) return;

            string searchText = SearchTextBox.Text.ToLower();
            string selectedType = (TypeFilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            var filteredTheses = allTheses.Where(t =>
                (string.IsNullOrEmpty(searchText) ||
                 t.Titre.ToLower().Contains(searchText) ||
                 t.Auteur.ToLower().Contains(searchText) ||
                 t.Speciality.ToLower().Contains(searchText) ||
                 t.Id.ToString().Contains(searchText)) &&
                (selectedType == "All Types" || t.Type.ToString() == selectedType)
            );

            ThesesDataGrid.ItemsSource = filteredTheses;
            UpdateThesisCounter();
        }

        private void ThesesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedThesis = ThesesDataGrid.SelectedItem as Theses;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedThesis != null)
            {
                try
                {
                    // If the thesis is declined, update its status to pending
                    if (selectedThesis.Status?.ToLower() == "declined")
                    {
                        string connectionString = AppConfig.CloudSqlConnectionString;
                        string updateQuery = "UPDATE theses SET status = 'pending' WHERE id = @thesisId";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@thesisId", selectedThesis.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        // Update the status in the local object
                        selectedThesis.Status = "pending";
                    }

                    var addThesisWindow = new AddThesisWindow(selectedThesis);
                    addThesisWindow.ShowDialog();
                    LoadUserTheses(); // Refresh the list after editing
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating thesis status: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedThesis != null)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this thesis? This action cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string connectionString = AppConfig.CloudSqlConnectionString;
                        string query = "DELETE FROM theses WHERE id = @thesisId";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@thesisId", selectedThesis.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        allTheses.Remove(selectedThesis);
                        FilterTheses();
                        MessageBox.Show("Thesis deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting thesis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
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
                        Width = 1000,
                        Height = SystemParameters.PrimaryScreenHeight * 0.9,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        Owner = Window.GetWindow(this),
                        WindowStyle = WindowStyle.SingleBorderWindow,
                        ResizeMode = ResizeMode.CanResize
                    };

                    // Create the content
                    var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                    var mainGrid = new Grid { Margin = new Thickness(0) };

                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Content
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

                    // Header with thesis title
                    var headerBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                        Padding = new Thickness(25, 20, 25, 20),
                    };

                    var headerText = new TextBlock
                    {
                        Text = thesis.Titre ?? "Thesis Details",
                        Foreground = Brushes.White,
                        FontSize = 22,
                        FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    };

                    headerBorder.Child = headerText;
                    Grid.SetRow(headerBorder, 0);
                    mainGrid.Children.Add(headerBorder);

                    // Content grid
                    var contentBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FFFF")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                        BorderThickness = new Thickness(1, 0, 1, 1),
                        Padding = new Thickness(25)
                    };

                    var contentGrid = new Grid();
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Author label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Author value
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Specialty label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Specialty value
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Type label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Type value
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Keywords label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Keywords value
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Year label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Year value
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Abstract label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Abstract value

                    // Author
                    AddDetailField(contentGrid, 0, "Author", thesis.Auteur ?? "N/A");

                    // Specialty
                    AddDetailField(contentGrid, 2, "Specialty", thesis.Speciality ?? "N/A");

                    // Type
                    AddDetailField(contentGrid, 4, "Type", thesis.Type.ToString());

                    // Keywords
                    AddDetailField(contentGrid, 6, "Keywords", thesis.MotsCles ?? "N/A");

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
                    AddDetailField(contentGrid, 8, "Year", yearText);

                    // Abstract
                    AddDetailField(contentGrid, 10, "Abstract", thesis.Resume ?? "N/A", true);

                    contentBorder.Child = contentGrid;
                    Grid.SetRow(contentBorder, 1);
                    mainGrid.Children.Add(contentBorder);

                    // Buttons panel
                    var buttonsPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 20, 25, 20)
                    };

                    // View PDF button (only if there's a file path)
                    if (!string.IsNullOrEmpty(thesis.Fichier))
                    {
                        var viewPdfButton = new Button
                        {
                            Content = "View PDF",
                            Width = 120,
                            Height = 40,
                            Margin = new Thickness(0, 0, 10, 0),
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                            Foreground = Brushes.White,
                            BorderThickness = new Thickness(0),
                            Style = (Style)FindResource("ActionButtonStyle"),
                            Tag = thesis
                        };
                        viewPdfButton.Click += ViewPdfButton_Click;
                        buttonsPanel.Children.Add(viewPdfButton);
                    }

                    // Close button
                    var closeButton = new Button
                    {
                        Content = "Close",
                        Width = 120,
                        Height = 40,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        Style = (Style)FindResource("ActionButtonStyle")
                    };
                    closeButton.Click += (s, args) => detailsWindow.Close();
                    buttonsPanel.Children.Add(closeButton);

                    Grid.SetRow(buttonsPanel, 2);
                    mainGrid.Children.Add(buttonsPanel);

                    scrollViewer.Content = mainGrid;
                    detailsWindow.Content = scrollViewer;
                    detailsWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error displaying thesis details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Cannot display thesis details. The thesis information may be incomplete.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ViewPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    if (thesis == null || string.IsNullOrEmpty(thesis.Fichier))
                    {
                        MessageBox.Show("No PDF file is associated with this thesis.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    Debug.WriteLine($"Attempting to open PDF document with object name: {thesis.Fichier}");
                    this.Cursor = Cursors.Wait;

                    // Use CloudStorageService to get a signed URL for the file
                    var cloudStorageService = new DataGridNamespace.Services.CloudStorageService();
                    string signedUrl = await cloudStorageService.GetSignedReadUrl(thesis.Fichier);
                    
                    if (string.IsNullOrEmpty(signedUrl))
                    {
                        Debug.WriteLine("Failed to generate signed URL for PDF document");
                        MessageBox.Show("Could not generate a download URL for this file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Cursor = Cursors.Arrow;
                        return;
                    }

                    Debug.WriteLine($"Successfully generated signed URL: {signedUrl}");

                    // Open the signed URL in the default browser
                    try
                    {
                        // Use ProcessStartInfo with UseShellExecute set to true to open URL in default browser
                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = signedUrl,
                            UseShellExecute = true
                        };
                        Process.Start(processStartInfo);
                        Debug.WriteLine("Successfully launched browser with signed URL");
                    }
                    catch (System.ComponentModel.Win32Exception win32Ex)
                    {
                        Debug.WriteLine($"Win32 error opening PDF URL: {win32Ex.Message} (Error code: {win32Ex.NativeErrorCode})");
                        
                        // Try an alternative approach if the first method fails
                        try
                        {
                            var baseUri = new Uri(signedUrl);
                            Debug.WriteLine($"Attempting to open URL using alternative approach: {baseUri}");
                            
                            // On Windows 10, this alternative method may work better in some cases
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "cmd",
                                Arguments = $"/c start \"\" \"{signedUrl}\"",
                                CreateNoWindow = true
                            });
                            Debug.WriteLine("Successfully launched using cmd start command");
                        }
                        catch (Exception altEx)
                        {
                            Debug.WriteLine($"Alternative method also failed: {altEx.Message}");
                            MessageBox.Show("Unable to open the PDF file. Please copy the URL and open it manually in your browser.", 
                                          "Browser Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            
                            // Offer to copy the URL to clipboard
                            var clipboardResult = MessageBox.Show("Would you like to copy the URL to your clipboard?", 
                                                               "Copy URL", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (clipboardResult == MessageBoxResult.Yes)
                            {
                                System.Windows.Clipboard.SetText(signedUrl);
                                MessageBox.Show("URL copied to clipboard. You can paste it into your browser.", 
                                              "URL Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    this.Cursor = Cursors.Arrow;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error opening PDF file: {ex.Message}\nStack trace: {ex.StackTrace}");
                    MessageBox.Show($"Error opening PDF file: {ex.Message}", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Cursor = Cursors.Arrow;
                }
            }
            else
            {
                MessageBox.Show("Cannot open PDF. The thesis information may be incomplete.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    // Add the thesis to favorites
                    string connectionString = AppConfig.CloudSqlConnectionString;
                    string query = "INSERT INTO favoris (user_id, these_id) VALUES (@userId, @thesisId)";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@userId", currentUserId);
                            cmd.Parameters.AddWithValue("@thesisId", thesis.Id);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Thesis added to favorites successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding thesis to favorites: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddDetailField(Grid grid, int rowIndex, string label, string value, bool isMultiline = false)
        {
            // Label
            var labelBlock = new TextBlock
            {
                Text = label + ":",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5),
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"))
            };
            Grid.SetRow(labelBlock, rowIndex);
            grid.Children.Add(labelBlock);
            
            // Value
            var valueBlock = new TextBlock
            {
                Text = value,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, isMultiline ? 20 : 10),
                FontSize = 14
            };
            
            if (isMultiline)
            {
                var abstractBorder = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(15),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                    MaxHeight = 180
                };
                
                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };
                
                scrollViewer.Content = valueBlock;
                abstractBorder.Child = scrollViewer;
                
                Grid.SetRow(abstractBorder, rowIndex + 1);
                grid.Children.Add(abstractBorder);
            }
            else
            {
                Grid.SetRow(valueBlock, rowIndex + 1);
                grid.Children.Add(valueBlock);
            }
        }
    }
} 