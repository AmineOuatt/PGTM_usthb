using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DataGridNamespace.SimpleUser
{
    public partial class SimpleUserFavoritesView : UserControl
    {
        private List<FavoriteItem> favorites;

        public SimpleUserFavoritesView()
        {
            InitializeComponent();
            LoadFavorites();
        }

        private void LoadFavorites()
        {
            try
            {
                // TODO: Load favorites from database
                favorites = new List<FavoriteItem>();
                FavoritesListView.ItemsSource = favorites;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading favorites: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is FavoriteItem item)
            {
                // TODO: Open item details view based on type
                MessageBox.Show($"Viewing {item.Type}: {item.Title}", "Item Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is FavoriteItem item)
            {
                var result = MessageBox.Show($"Are you sure you want to remove {item.Title} from favorites?", 
                                           "Remove Favorite", 
                                           MessageBoxButton.YesNo, 
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // TODO: Remove from database
                    favorites.Remove(item);
                    FavoritesListView.ItemsSource = null;
                    FavoritesListView.ItemsSource = favorites;
                }
            }
        }
    }

    public class FavoriteItem
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public string DateAdded { get; set; }
    }
} 