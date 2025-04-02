using System;
using System.Windows;
using System.Windows.Controls;
using DataGridNamespace;

namespace DataGridNamespace.Admin
{
    public partial class AdminSidebar : UserControl
    {
        public AdminSidebar()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing AdminSidebar: {ex.Message}\nStack Trace: {ex.StackTrace}", 
                              "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MembersManagementButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    MessageBox.Show("Found MainWindow, attempting navigation...", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
                    mainWindow.NavigateToMembersManagement();
                }
                else
                {
                    MessageBox.Show("Could not find the main window. Current window type: " + 
                                  (Window.GetWindow(this)?.GetType().Name ?? "null"), 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in MembersManagementButton_Click: {ex.Message}\nStack Trace: {ex.StackTrace}", 
                              "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.LogoutButton_Click(sender, e);
                }
                else
                {
                    MessageBox.Show("Could not find the main window for logout.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in LogoutButton_Click: {ex.Message}\nStack Trace: {ex.StackTrace}", 
                              "Logout Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 