using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using DataGridNamespace.Admin;
using MyProject;

namespace DataGridNamespace
{
    public partial class MainWindow : Window
    {
        private string currentUserRole;

        public MainWindow(string userRole)
        {
            try
            {
                InitializeComponent();
                currentUserRole = userRole;
                SetupUserInterface();
                
                if (currentUserRole.ToLower() == "admin")
                {
                    Dispatcher.BeginInvoke(new Action(() => {
                        try {
                            var membersView = new MembersListView();
                            MainFrame.Content = membersView;
                        }
                        catch (Exception ex) {
                            MessageBox.Show($"Navigation error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing MainWindow: {ex.Message}\nStack Trace: {ex.StackTrace}", 
                              "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupUserInterface()
        {
            try
            {
                MessageBox.Show($"Setting up UI for role: {currentUserRole}", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                
                if (SidebarContainer == null)
                {
                    MessageBox.Show("SidebarContainer is null! XAML not initialized properly.", 
                                  "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                switch (currentUserRole.ToLower())
                {
                    case "admin":
                        MessageBox.Show("Creating AdminSidebar...", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                        var adminSidebar = new AdminSidebar();
                        SidebarContainer.Content = adminSidebar;
                        MessageBox.Show("AdminSidebar set successfully", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case "student":
                        MessageBox.Show("Student sidebar coming soon!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    case "simpleuser":
                        MessageBox.Show("Simple User sidebar coming soon!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SetupUserInterface: {ex.Message}\nStack Trace: {ex.StackTrace}", 
                              "Setup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void NavigateToMembersManagement()
        {
            try
            {
                MessageBox.Show("Starting navigation to Members Management...", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                
                if (MainFrame == null)
                {
                    MessageBox.Show("MainFrame is null! This means the XAML is not properly initialized.", 
                                  "Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show("Creating MembersListView...", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                var membersView = new MembersListView();
                
                MessageBox.Show("Navigating to MembersListView...", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                MainFrame.Navigate(membersView);
                
                MessageBox.Show("Navigation completed successfully.", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in NavigateToMembersManagement:\n" +
                              $"Message: {ex.Message}\n" +
                              $"Type: {ex.GetType().Name}\n" +
                              $"Stack Trace: {ex.StackTrace}", 
                              "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show("Are you sure you want to logout?", 
                                           "Logout Confirmation", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var loginWindow = new MyProject.Login();
                    loginWindow.Show();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during logout: {ex.Message}\nStack Trace: {ex.StackTrace}", 
                              "Logout Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Window Movement
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // Window Controls
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Error during close: {ex.Message}\nStack Trace: {ex.StackTrace}", 
                              "Close Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
