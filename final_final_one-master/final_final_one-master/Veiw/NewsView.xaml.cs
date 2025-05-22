using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DataGridNamespace.Models;
using MySql.Data.MySqlClient;
using UserModels;

namespace DataGridNamespace.Veiw
{
    public partial class NewsView : UserControl
    {
        public ObservableCollection<NewsItem> News { get; set; }
        public bool IsAdmin { get; set; }

        public NewsView()
        {
            InitializeComponent();
            News = new ObservableCollection<NewsItem>();
            this.DataContext = this;
            IsAdmin = DataGridNamespace.Session.CurrentUserRole == RoleUtilisateur.Admin;
            NewsItemsControl.ItemsSource = News;
            Loaded += NewsView_Loaded;
        }

        private async void NewsView_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadNewsItemsAsync();
        }

        private async Task LoadNewsItemsAsync()
        {
            News.Clear();
            
            try
            {
                using (MySqlConnection connection = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT NewsItemID, Title, Content, PublishedDate, Author FROM NewsItems ORDER BY PublishedDate DESC";
                    
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                News.Add(new NewsItem
                                {
                                    NewsItemID = reader.GetInt32(reader.GetOrdinal("NewsItemID")),
                                    Title = reader.GetString(reader.GetOrdinal("Title")),
                                    Content = reader.GetString(reader.GetOrdinal("Content")),
                                    PublishedDate = reader.GetDateTime(reader.GetOrdinal("PublishedDate")),
                                    Author = reader.IsDBNull(reader.GetOrdinal("Author")) ? null : reader.GetString(reader.GetOrdinal("Author"))
                                });
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"A database error occurred while fetching news: {ex.Message}\n\nPlease ensure you are connected to the internet and the Cloud SQL Proxy is running.", 
                               "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred while fetching news: {ex.Message}", 
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddNewsButton_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddNewsWindow();
            if (addWindow.ShowDialog() == true)
            {
                _ = LoadNewsItemsAsync();
            }
        }

        private async void DeleteNewsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int newsId)
            {
                var result = MessageBox.Show("Are you sure you want to delete this news item?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (var conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                        {
                            await conn.OpenAsync();
                            string query = "DELETE FROM NewsItems WHERE NewsItemID = @id";
                            using (var cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", newsId);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        await LoadNewsItemsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to delete news: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
} 