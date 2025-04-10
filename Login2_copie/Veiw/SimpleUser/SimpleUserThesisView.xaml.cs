using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DataGridNamespace.SimpleUser
{
    public partial class SimpleUserThesisView : UserControl
    {
        private List<Thesis> theses;

        public SimpleUserThesisView()
        {
            InitializeComponent();
            LoadTheses();
        }

        private void LoadTheses()
        {
            try
            {
                // TODO: Load theses from database
                theses = new List<Thesis>();
                ThesisListView.ItemsSource = theses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading theses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTheses(SearchTextBox.Text);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FilterTheses(SearchTextBox.Text);
        }

        private void FilterTheses(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ThesisListView.ItemsSource = theses;
                return;
            }

            var filteredTheses = theses.FindAll(t =>
                t.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                t.Author.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                t.Year.Contains(searchText, StringComparison.OrdinalIgnoreCase));

            ThesisListView.ItemsSource = filteredTheses;
        }

        private void ViewThesisButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Thesis thesis)
            {
                // TODO: Open thesis details view
                MessageBox.Show($"Viewing thesis: {thesis.Title}", "Thesis Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class Thesis
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Year { get; set; }
    }
} 