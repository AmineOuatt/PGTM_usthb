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

namespace DataGridNamespace
{
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
            SetupDataGridColumns();
        }

        private void SetupDataGridColumns()
        {
            ThesesDataGrid.Columns.Clear();
            ThesesDataGrid.Columns.Add(new DataGridTextColumn { Header = "Title", Binding = new System.Windows.Data.Binding("Titre"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            ThesesDataGrid.Columns.Add(new DataGridTextColumn { Header = "Author", Binding = new System.Windows.Data.Binding("Auteur"), Width = 150 });
            ThesesDataGrid.Columns.Add(new DataGridTextColumn { Header = "Speciality", Binding = new System.Windows.Data.Binding("Speciality"), Width = 150 });
            ThesesDataGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new System.Windows.Data.Binding("Type"), Width = 100 });
            ThesesDataGrid.Columns.Add(new DataGridTextColumn { Header = "Year", Binding = new System.Windows.Data.Binding("Annee") { StringFormat = "yyyy" }, Width = 100 });

            // Add Actions column with buttons
            var actionsColumn = new DataGridTemplateColumn
            {
                Header = "Actions",
                Width = 200
            };

            var cellTemplate = new DataTemplate();
            var stackPanel = new FrameworkElementFactory(typeof(StackPanel));
            stackPanel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackPanel.SetValue(StackPanel.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // View Details Button
            var viewDetailsButton = new FrameworkElementFactory(typeof(Button));
            viewDetailsButton.SetValue(Button.StyleProperty, FindResource("ActionButtonStyle"));
            viewDetailsButton.SetValue(Button.WidthProperty, 35.0);
            viewDetailsButton.SetValue(Button.HeightProperty, 35.0);
            viewDetailsButton.SetValue(Button.MarginProperty, new Thickness(2, 0, 2, 0));
            viewDetailsButton.SetValue(Button.ToolTipProperty, "View Details");
            viewDetailsButton.SetValue(Button.TagProperty, new Binding());
            viewDetailsButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(ViewDetailsButton_Click));

            var viewDetailsIcon = new FrameworkElementFactory(typeof(TextBlock));
            viewDetailsIcon.SetValue(TextBlock.TextProperty, "ℹ️");
            viewDetailsIcon.SetValue(TextBlock.FontSizeProperty, 18.0);
            viewDetailsButton.AppendChild(viewDetailsIcon);

            // View PDF Button
            var viewPdfButton = new FrameworkElementFactory(typeof(Button));
            viewPdfButton.SetValue(Button.StyleProperty, FindResource("ActionButtonStyle"));
            viewPdfButton.SetValue(Button.WidthProperty, 35.0);
            viewPdfButton.SetValue(Button.HeightProperty, 35.0);
            viewPdfButton.SetValue(Button.MarginProperty, new Thickness(2, 0, 2, 0));
            viewPdfButton.SetValue(Button.ToolTipProperty, "View PDF");
            viewPdfButton.SetValue(Button.TagProperty, new Binding());
            viewPdfButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(ViewPdfButton_Click));

            var viewPdfIcon = new FrameworkElementFactory(typeof(TextBlock));
            viewPdfIcon.SetValue(TextBlock.TextProperty, "📄");
            viewPdfIcon.SetValue(TextBlock.FontSizeProperty, 18.0);
            viewPdfButton.AppendChild(viewPdfIcon);

            // Favorite Button
            var favoriteButton = new FrameworkElementFactory(typeof(Button));
            favoriteButton.SetValue(Button.StyleProperty, FindResource("ActionButtonStyle"));
            favoriteButton.SetValue(Button.WidthProperty, 35.0);
            favoriteButton.SetValue(Button.HeightProperty, 35.0);
            favoriteButton.SetValue(Button.MarginProperty, new Thickness(2, 0, 2, 0));
            favoriteButton.SetValue(Button.ToolTipProperty, "Add to Favorites");
            favoriteButton.SetValue(Button.TagProperty, new Binding());
            favoriteButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(FavoriteButton_Click));

            var favoriteIcon = new FrameworkElementFactory(typeof(TextBlock));
            favoriteIcon.SetValue(TextBlock.TextProperty, "⭐");
            favoriteIcon.SetValue(TextBlock.FontSizeProperty, 18.0);
            favoriteButton.AppendChild(favoriteIcon);

            // Edit Button
            var editButton = new FrameworkElementFactory(typeof(Button));
            editButton.SetValue(Button.StyleProperty, FindResource("EditButtonStyle"));
            editButton.SetValue(Button.WidthProperty, 35.0);
            editButton.SetValue(Button.HeightProperty, 35.0);
            editButton.SetValue(Button.MarginProperty, new Thickness(2, 0, 2, 0));
            editButton.SetValue(Button.ToolTipProperty, "Edit Thesis");
            editButton.SetValue(Button.TagProperty, new Binding());
            editButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(EditButton_Click));

            var editIcon = new FrameworkElementFactory(typeof(TextBlock));
            editIcon.SetValue(TextBlock.TextProperty, "✏️");
            editIcon.SetValue(TextBlock.FontSizeProperty, 18.0);
            editButton.AppendChild(editIcon);

            // Delete Button
            var deleteButton = new FrameworkElementFactory(typeof(Button));
            deleteButton.SetValue(Button.StyleProperty, FindResource("DeleteButtonStyle"));
            deleteButton.SetValue(Button.WidthProperty, 35.0);
            deleteButton.SetValue(Button.HeightProperty, 35.0);
            deleteButton.SetValue(Button.MarginProperty, new Thickness(2, 0, 2, 0));
            deleteButton.SetValue(Button.ToolTipProperty, "Delete Thesis");
            deleteButton.SetValue(Button.TagProperty, new Binding());
            deleteButton.AddHandler(Button.ClickEvent, new RoutedEventHandler(DeleteButton_Click));

            var deleteIcon = new FrameworkElementFactory(typeof(TextBlock));
            deleteIcon.SetValue(TextBlock.TextProperty, "🗑️");
            deleteIcon.SetValue(TextBlock.FontSizeProperty, 18.0);
            deleteButton.AppendChild(deleteIcon);

            stackPanel.AppendChild(viewDetailsButton);
            stackPanel.AppendChild(viewPdfButton);
            stackPanel.AppendChild(favoriteButton);
            stackPanel.AppendChild(editButton);
            stackPanel.AppendChild(deleteButton);

            cellTemplate.VisualTree = stackPanel;
            actionsColumn.CellTemplate = cellTemplate;

            ThesesDataGrid.Columns.Add(actionsColumn);
        }

        private void LoadUserTheses()
        {
            try
            {
                string connectionString = AppConfig.CloudSqlConnectionString;
                string query = "SELECT * FROM theses WHERE user_id = @userId";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", currentUserId);
                        
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            allTheses = new ObservableCollection<Theses>();
                            foreach (DataRow row in dt.Rows)
                            {
                                allTheses.Add(new Theses
                                {
                                    Id = Convert.ToInt32(row["id"]),
                                    Titre = row["titre"].ToString(),
                                    Auteur = row["auteur"].ToString(),
                                    Speciality = row["speciality"].ToString(),
                                    Type = (TypeThese)Enum.Parse(typeof(TypeThese), row["type"].ToString()),
                                    Annee = Convert.ToDateTime(row["annee"]),
                                    MotsCles = row["mots_cles"].ToString(),
                                    Resume = row["resume"].ToString(),
                                    Fichier = row["fichier"].ToString(),
                                    UserId = Convert.ToInt32(row["user_id"])
                                });
                            }
                        }
                    }
                }

                ThesesDataGrid.ItemsSource = allTheses;
                UpdateThesisCounter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading theses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                 t.Speciality.ToLower().Contains(searchText)) &&
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
                var addThesisWindow = new AddThesisWindow(selectedThesis);
                addThesisWindow.ShowDialog();
                LoadUserTheses(); // Refresh the list after editing
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
                        Width = 800,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = Window.GetWindow(this)
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