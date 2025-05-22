using System;
using System.Windows;
using MySql.Data.MySqlClient;
using DataGridNamespace.Models;
using System.Windows.Media;

namespace DataGridNamespace.Veiw
{
    public partial class AddNewsWindow : Window
    {
        public AddNewsWindow()
        {
            InitializeComponent();
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            bool valid = true;
            TitleError.Visibility = Visibility.Collapsed;
            ContentError.Visibility = Visibility.Collapsed;
            TitleTextBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);
            ContentTextBox.ClearValue(System.Windows.Controls.Control.BorderBrushProperty);

            string title = TitleTextBox.Text.Trim();
            string content = ContentTextBox.Text.Trim();
            string author = AuthorTextBox.Text.Trim();

            if (string.IsNullOrEmpty(title))
            {
                TitleError.Text = "Title is required.";
                TitleError.Visibility = Visibility.Visible;
                TitleTextBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
                TitleTextBox.BorderThickness = new Thickness(2);
                valid = false;
            }
            if (string.IsNullOrEmpty(content))
            {
                ContentError.Text = "Content is required.";
                ContentError.Visibility = Visibility.Visible;
                ContentTextBox.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E53935"));
                ContentTextBox.BorderThickness = new Thickness(2);
                valid = false;
            }
            if (!valid)
                return;

            try
            {
                using (var conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    await conn.OpenAsync();
                    string query = "INSERT INTO NewsItems (Title, Content, PublishedDate, Author) VALUES (@title, @content, @date, @author)";
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@title", title);
                        cmd.Parameters.AddWithValue("@content", content);
                        cmd.Parameters.AddWithValue("@date", DateTime.UtcNow);
                        cmd.Parameters.AddWithValue("@author", string.IsNullOrEmpty(author) ? (object)DBNull.Value : author);
                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to add news: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 