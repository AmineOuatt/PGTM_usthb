using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using DataGridNamespace.Admin;
using MyProject;
using DataGrid;
using System.Collections.ObjectModel;
using System.Linq;
using UserModels;
using ThesesModels;
using FavorisModels;
using System.Diagnostics;
using DataGridNamespace.Services;
using System.Threading.Tasks;

namespace DataGridNamespace
{
    public partial class MainWindow : Window
    {
        private User currentUser;
        private bool IsMaximize = false;
        private readonly CloudStorageService _cloudStorageService = new CloudStorageService();

        public MainWindow()
        {
            InitializeComponent();
            
            // Get current user from Session
            int userId = Session.CurrentUserId;
            string userName = Session.CurrentUserName;
            RoleUtilisateur userRole = Session.CurrentUserRole;
            
            currentUser = new User
            {
                Id = userId,
                Nom = userName,
                Role = userRole
            };

            // Load profile picture from cloud storage
            LoadUserProfilePicture();
            
            // Set window to maximize on startup
            this.WindowState = WindowState.Maximized;
            IsMaximize = true;
            
            LoadRoleSpecificContent();
        }

        public MainWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            
            // Load profile picture from cloud storage
            LoadUserProfilePicture();
            
            // Set window to maximize on startup
            this.WindowState = WindowState.Maximized;
            IsMaximize = true;
            
            LoadRoleSpecificContent();
        }

        // New method to load profile picture from cloud storage
        private async void LoadUserProfilePicture()
        {
            try
            {
                // Get profile picture reference from database
                string profilePicRef = await GetUserProfilePicRef(currentUser.Id);
                
                if (!string.IsNullOrEmpty(profilePicRef))
                {
                    await LoadProfilePictureFromCloud(profilePicRef);
                }
                else
                {
                    Debug.WriteLine("No profile picture reference found for user");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading profile picture: {ex.Message}");
                // Silently fail and use default image
            }
        }

        // Method to fetch profile pic reference from database
        private async Task<string> GetUserProfilePicRef(int userId)
        {
            try
            {
                string connectionString = AppConfig.CloudSqlConnectionString;
                string query = "SELECT profile_pic_ref FROM users WHERE id = @userId";
                
                using (MySql.Data.MySqlClient.MySqlConnection conn = new MySql.Data.MySqlClient.MySqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (MySql.Data.MySqlClient.MySqlCommand cmd = new MySql.Data.MySqlClient.MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        
                        object result = await cmd.ExecuteScalarAsync();
                        return result != null && result != DBNull.Value ? result.ToString() : null;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting profile picture reference: {ex.Message}");
                return null;
            }
        }

        // Method to load profile picture from cloud storage
        private async Task LoadProfilePictureFromCloud(string profilePicRef)
        {
            try
            {
                Debug.WriteLine($"Attempting to load profile picture with object name: {profilePicRef}");
                
                // Get a signed URL for the profile picture from Cloud Storage
                string signedUrl = await _cloudStorageService.GetSignedReadUrl(profilePicRef);
                
                if (string.IsNullOrEmpty(signedUrl))
                {
                    Debug.WriteLine("Failed to get signed URL for profile picture");
                    return;
                }
                
                Debug.WriteLine($"Successfully generated signed URL for profile picture");
                
                // Load the image using the signed URL
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(signedUrl);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load the image right away so the URL doesn't expire
                bitmap.EndInit();

                // Set the image as the profile picture in the sidebar
                Application.Current.Dispatcher.Invoke(() => {
                    var brush = new ImageBrush(bitmap);
                    ProfilePictureEllipse.Fill = brush; // You'll need to add a name to the Ellipse in XAML
                });
                
                Debug.WriteLine("Successfully displayed profile picture in sidebar");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading profile picture: {ex.Message}");
                // Don't show message box for profile picture loading errors
            }
        }

        private void LoadRoleSpecificContent()
        {
            switch (currentUser.Role)
            {
                case RoleUtilisateur.Admin:
                    LoadAdminContent();
                    break;
                case RoleUtilisateur.SimpleUser:
                    LoadSimpleUserContent();
                    break;
                case RoleUtilisateur.Etudiant:
                    LoadEtudiantContent();
                    break;
                default:
                    LoadSimpleUserContent();
                    break;
            }
        }

        private void LoadAdminContent()
        {
            try
            {
                // Show admin-specific buttons
                MembersButton.Visibility = Visibility.Visible;
                DashboardButton.Visibility = Visibility.Visible;
                ThesisButton.Visibility = Visibility.Visible;
                ProfileButton.Visibility = Visibility.Visible;
                FavoritesButton.Visibility = Visibility.Visible;

                // Set initial view to dashboard
                DashboardButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                ThesisButton.Background = Brushes.Transparent;
                MembersButton.Background = Brushes.Transparent;
                ProfileButton.Background = Brushes.Transparent;
                FavoritesButton.Background = Brushes.Transparent;
                
                // Load dashboard view by default
                var dashboardView = new DashboardView();
                MainFrame.Navigate(dashboardView);

                // Set up admin-specific event handlers
                DashboardButton.Click += DashboardButton_Click;
                ThesisButton.Click += ThesisButton_Click;
                MembersButton.Click += MembersButton_Click;
                ProfileButton.Click += ProfileButton_Click;
                FavoritesButton.Click += FavoritesButton_Click;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading admin content: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Remaining code unchanged...
        private void LoadSimpleUserContent()
        {
            try
            {
                // Hide all buttons first
                DashboardButton.Visibility = Visibility.Collapsed;
                MembersButton.Visibility = Visibility.Collapsed;
                
                // Show only simple user specific buttons
                ThesisButton.Visibility = Visibility.Visible;
                ProfileButton.Visibility = Visibility.Visible;
                FavoritesButton.Visibility = Visibility.Visible;

                // Set initial view to profile
                ProfileButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                
                // Set up user-specific event handlers
                ThesisButton.Click += ThesisButton_Click;
                ProfileButton.Click += ProfileButton_Click;
                FavoritesButton.Click += FavoritesButton_Click;

                // Load profile view by default
                var profileView = new ProfileView();
                MainFrame.Navigate(profileView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading simple user content: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEtudiantContent()
        {
            try
            {
                // Hide all buttons first
                DashboardButton.Visibility = Visibility.Collapsed;
                MembersButton.Visibility = Visibility.Collapsed;
                
                // Show only etudiant specific buttons
                ThesisButton.Visibility = Visibility.Visible;
                ProfileButton.Visibility = Visibility.Visible;
                FavoritesButton.Visibility = Visibility.Visible;

                // Set initial view to profile
                ProfileButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                
                // Set up etudiant-specific event handlers
                ThesisButton.Click += ThesisButton_Click;
                ProfileButton.Click += ProfileButton_Click;
                FavoritesButton.Click += FavoritesButton_Click;

                // Load profile view by default
                var profileView = new ProfileView();
                MainFrame.Navigate(profileView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading etudiant content: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DashboardButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                ThesisButton.Background = Brushes.Transparent;
                MembersButton.Background = Brushes.Transparent;
                ProfileButton.Background = Brushes.Transparent;
                FavoritesButton.Background = Brushes.Transparent;
                
                var dashboardView = new DashboardView();
                MainFrame.Navigate(dashboardView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Dashboard view: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThesisButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThesisButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                DashboardButton.Background = Brushes.Transparent;
                MembersButton.Background = Brushes.Transparent;
                ProfileButton.Background = Brushes.Transparent;
                FavoritesButton.Background = Brushes.Transparent;
                
                // Load thesis view based on user role
                if (currentUser.Role == RoleUtilisateur.Admin)
                {
                    var thesisView = new Admin.ThesisView();
                    MainFrame.Navigate(thesisView);
                }
                else
                {
                    // For all other user types, use the admin thesis view with restricted rights
                    var thesisView = new Admin.ThesisView();
                    MainFrame.Navigate(thesisView);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Thesis view: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MembersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MembersButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                DashboardButton.Background = Brushes.Transparent;
                ThesisButton.Background = Brushes.Transparent;
                ProfileButton.Background = Brushes.Transparent;
                FavoritesButton.Background = Brushes.Transparent;
                
                var membersView = new MembersListView();
                MainFrame.Navigate(membersView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Members view: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProfileButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                ThesisButton.Background = Brushes.Transparent;
                FavoritesButton.Background = Brushes.Transparent;
                
                var profileView = new ProfileView();
                MainFrame.Navigate(profileView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Profile view: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FavoritesButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                DashboardButton.Background = Brushes.Transparent;
                ThesisButton.Background = Brushes.Transparent;
                MembersButton.Background = Brushes.Transparent;
                ProfileButton.Background = Brushes.Transparent;
                
                // Load favorites view based on user role
                var favoritesView = new Admin.FavoritesView();
                MainFrame.Navigate(favoritesView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Favorites view: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutConfirmationWindow confirmWindow = new LogoutConfirmationWindow();
            bool? result = confirmWindow.ShowDialog();
            if (result == true && confirmWindow.IsConfirmed)
            {
                Login loginWindow = new Login();
                loginWindow.Show();
                this.Close();
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeRestoreButton_Click(sender, e);
            }
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsMaximize)
            {
                this.WindowState = WindowState.Normal;
                this.Width = 1280;
                this.Height = 720;
                IsMaximize = false;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                IsMaximize = true;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to exit?", 
                                       "Exit Confirmation", 
                                       MessageBoxButton.YesNo, 
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }
    }
}