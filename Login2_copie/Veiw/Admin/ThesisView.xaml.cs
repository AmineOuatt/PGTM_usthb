using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using ThesesModels;

namespace DataGridNamespace
{
    public partial class ThesisView : UserControl
    {
        public ObservableCollection<Theses> Theses { get; set; }

        public ThesisView()
        {
            InitializeComponent();
           
            DataContext = this;
        }

        // Handle the hyperlink click to open the PDF link
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // Open the URL in the default browser
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening PDF link: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Handle the delete button click to remove the thesis
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                // Confirm deletion
                var result = MessageBox.Show($"Are you sure you want to delete '{thesis.Titre}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    Theses.Remove(thesis);
                }
            }
        }
    }
}