using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;

namespace DataGrid
{
    public partial class ProfileView : Page
    {
        private bool isEditing = false; // تتبع وضع التعديل

        public bool IsMaximize { get; private set; }
        public Ellipse ProfileAvatar { get; private set; }
        public object EditButton { get; private set; }

        public ProfileView()
        {
            InitializeComponent();
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            if (!isEditing)
            {
                // تفعيل وضع التعديل
                UsernameTextBox.IsReadOnly = false;
                EmailTextBox.IsReadOnly = false;
                UsernameTextBox.Background = Brushes.White;
                EmailTextBox.Background = Brushes.White;
                PasswordContainer.Visibility = Visibility.Visible;
                button.Content = "Save Changes";
                isEditing = true;
            }
            else
            {
                // حفظ التغييرات وإيقاف التعديل
                SaveChanges();
                UsernameTextBox.IsReadOnly = true;
                EmailTextBox.IsReadOnly = true;
                UsernameTextBox.Background = Brushes.Transparent;
                EmailTextBox.Background = Brushes.Transparent;
                PasswordContainer.Visibility = Visibility.Collapsed;
                button.Content = "Edit Profile";
                isEditing = false;
            }
        }

        private void SaveChanges()
        {
            string newUsername = UsernameTextBox.Text;
            string newEmail = EmailTextBox.Text;
            string newPassword = PasswordBox.Password;
            int userID = 1; // استبدل بمعرف المستخدم الحقيقي
            string connectionString = "server=localhost;database=gestion_theses;user=root;password=;";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "UPDATE Users SET Username = @Username, Email = @Email";
                    if (!string.IsNullOrWhiteSpace(newPassword))
                    {
                        query += ", Password = @Password";
                    }
                    query += " WHERE UserID = @UserID";
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@Username", newUsername);
                    cmd.Parameters.AddWithValue("@Email", newEmail);
                    cmd.Parameters.AddWithValue("@UserID", userID);
                    if (!string.IsNullOrWhiteSpace(newPassword))
                    {
                        cmd.Parameters.AddWithValue("@Password", newPassword);
                    }
                    int rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        MessageBox.Show("Profile updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Update failed!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (IsMaximize)
                {
                    this.NavigationService.Content = null; // أو إعادة تعيين الحجم الافتراضي
                    IsMaximize = false;
                }
                else
                {
                    // للتكبير، يمكنك تعديل حجم الصفحة حسب الحاجة
                    IsMaximize = true;
                }
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // استخدام DragMove ليس متاحاً للصفحات، فهذه الخاصية للنوافذ
            }
        }

        private void ChangeProfilePicture_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Choose a Profile Picture",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;
                ImageBrush brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                ((Ellipse)ProfileAvatar).Fill = brush;
            }
        }

        private void BackToDashboard_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
           
        }

    }
}
