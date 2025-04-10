using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using DataGridNamespace.Admin;
using MyProject;
using DataGrid;
using System.Collections.ObjectModel;
using System.Linq;
using UserModels;

namespace DataGridNamespace
{
    public partial class MainWindow : Window
    {
        private string userRole;
        private bool IsMaximize = false;
        private ObservableCollection<Member> members;

        public MainWindow(string role)
        {
            InitializeComponent();
            userRole = role;
            members = new ObservableCollection<Member>();
            
            // Set window to maximize on startup
            this.WindowState = WindowState.Maximized;
            IsMaximize = true;
            
            LoadRoleSpecificContent();
        }

        private void LoadRoleSpecificContent()
        {
            switch (userRole.ToLower())
            {
                case "admin":
                    LoadAdminContent();
                    break;
                case "simple user":
                    LoadSimpleUserContent();
                    break;
                case "student":
                    LoadStudentContent();
                    break;
                default:
                    MessageBox.Show("Unknown role. Defaulting to admin view.");
                    LoadAdminContent();
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

        private void LoadMembersData()
        {
            try
            {
                // TODO: Load members from database
                // For now, using sample data
                members.Clear();
                members.Add(new Member { Id = 1, Name = "Admin User", Role = "Admin", Email = "admin@example.com" });
                members.Add(new Member { Id = 2, Name = "Student User", Role = "Student", Email = "student@example.com" });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSimpleUserContent()
        {
            try
            {
                // Hide admin-specific buttons
                MembersButton.Visibility = Visibility.Collapsed;
                DashboardButton.Visibility = Visibility.Visible;
                ThesisButton.Visibility = Visibility.Visible;
                ProfileButton.Visibility = Visibility.Visible;
                FavoritesButton.Visibility = Visibility.Visible;

                // Set initial view to dashboard
                DashboardButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                
                // Set up user-specific event handlers
                DashboardButton.Click += DashboardButton_Click;
                ThesisButton.Click += ThesisButton_Click;
                ProfileButton.Click += ProfileButton_Click;
                FavoritesButton.Click += FavoritesButton_Click;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading simple user content: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadStudentContent()
        {
            try
            {
                // Hide admin-specific buttons
                MembersButton.Visibility = Visibility.Collapsed;
                DashboardButton.Visibility = Visibility.Visible;
                ThesisButton.Visibility = Visibility.Visible;
                ProfileButton.Visibility = Visibility.Visible;
                FavoritesButton.Visibility = Visibility.Visible;

                // Set initial view to dashboard
                DashboardButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                
                // Set up student-specific event handlers
                DashboardButton.Click += DashboardButton_Click;
                ThesisButton.Click += ThesisButton_Click;
                ProfileButton.Click += ProfileButton_Click;
                FavoritesButton.Click += FavoritesButton_Click;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading student content: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddMemberButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a new user for editing
                var newUser = new User
                {
                    Id = 0, // New user
                    Nom = "",
                    Role = RoleUtilisateur.SimpleUser,
                    Email = ""
                };

                var editMemberWindow = new EditMember(newUser, () =>
                {
                    // Handle the updated user
                    LoadMembersData();
                });

                editMemberWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding member: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadMembersData();
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

        private void LoadDashboardData()
        {
            try
            {
                // TODO: Load dashboard statistics and data
                // This will be implemented based on your specific requirements
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                
                var thesisView = new ThesisView();
                MainFrame.Navigate(thesisView);
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
                DashboardButton.Background = Brushes.Transparent;
                ThesisButton.Background = Brushes.Transparent;
                MembersButton.Background = Brushes.Transparent;
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
                
                var favoritesView = new FavoritesView();
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

    public class Member
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
    }
}
