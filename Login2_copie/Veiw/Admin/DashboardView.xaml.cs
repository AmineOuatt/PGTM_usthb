using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DataGridNamespace;
using MyProject;

namespace DataGrid
{
    public partial class DashboardView : Window
    {
        public DashboardView()
        {
            InitializeComponent();
        }

        // السماح بسحب النافذة عند النقر على الحافة الخارجية
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        // زر Dashboard
        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            DashboardButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
            ThesisButton.Background = Brushes.Transparent;
            MembersButton.Background = Brushes.Transparent;
            ProfileButton.Background = Brushes.Transparent;
            FavoritesButton.Background = Brushes.Transparent;
            MainFrame.Content = null;
        }

        // زر Thesis
        private void ThesisButton_Click(object sender, RoutedEventArgs e)
        {
            ThesisButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
            DashboardButton.Background = Brushes.Transparent;
            MembersButton.Background = Brushes.Transparent;
            ProfileButton.Background = Brushes.Transparent;
            FavoritesButton.Background = Brushes.Transparent;
            MainFrame.Content = null;
        }

        // زر Members
        private void MembersButton_Click(object sender, RoutedEventArgs e)
        {
            MembersButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
            DashboardButton.Background = Brushes.Transparent;
            ThesisButton.Background = Brushes.Transparent;
            ProfileButton.Background = Brushes.Transparent;
            FavoritesButton.Background = Brushes.Transparent;
            MainFrame.Navigate(new DataGridNamespace.MainWindow("admin"));
        }

        // زر Profile
        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            ProfileButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
            DashboardButton.Background = Brushes.Transparent;
            ThesisButton.Background = Brushes.Transparent;
            MembersButton.Background = Brushes.Transparent;
            FavoritesButton.Background = Brushes.Transparent;
            MainFrame.Navigate(new ProfileView());
        }

        // زر Logout في الشريط الجانبي
        private void LogoutButton_Click(object sender, RoutedEventArgs e)
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

        // زر التصغير
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        // زر التكبير/استعادة الحجم
        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        // زر الإغلاق
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
