using System;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Linq;
using ThesesModels;
using UserModels;
using System.Data;
using MySql.Data.MySqlClient;
using DataGridNamespace.Admin;

namespace DataGridNamespace
{
    public partial class MyThesesWindow : Page
    {
        private ObservableCollection<Theses> allTheses;
        private int currentUserId;
        private Theses selectedThesis;

        public MyThesesWindow()
        {
            InitializeComponent();
            currentUserId = Session.CurrentUserId;
            LoadUserTheses();
            SetupDataGridColumns();
        }

        private void SetupDataGridColumns()
        {
            ThesesDataGrid.Columns.Clear();
            ThesesDataGrid.Columns.Add(new DataGridTextColumn { Header = "Title", Binding = new System.Windows.Data.Binding("Titre"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            ThesesDataGrid.Columns.Add(new DataGridTextColumn { Header = "Author", Binding = new System.Windows.Data.Binding("Auteur"), Width = 150 });
            ThesesDataGrid.Columns.Add(new DataGridTextColumn { Header = "Speciality", Binding = new System.Windows.Data.Binding("Speciality"), Width = 150 });
            ThesesDataGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new System.Windows.Data.Binding("Type"), Width = 100 });
            ThesesDataGrid.Columns.Add(new DataGridTextColumn { Header = "Year", Binding = new System.Windows.Data.Binding("Annee") { StringFormat = "yyyy" }, Width = 100 });
        }

        private void LoadUserTheses()
        {
            try
            {
                string connectionString = AppConfig.CloudSqlConnectionString;
                string query = "SELECT * FROM theses WHERE user_id = @userId";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", currentUserId);
                        
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            DataTable dt = new DataTable();
                            adapter.Fill(dt);

                            allTheses = new ObservableCollection<Theses>();
                            foreach (DataRow row in dt.Rows)
                            {
                                allTheses.Add(new Theses
                                {
                                    Id = Convert.ToInt32(row["id"]),
                                    Titre = row["titre"].ToString(),
                                    Auteur = row["auteur"].ToString(),
                                    Speciality = row["speciality"].ToString(),
                                    Type = (TypeThese)Enum.Parse(typeof(TypeThese), row["type"].ToString()),
                                    Annee = Convert.ToDateTime(row["annee"]),
                                    MotsCles = row["mots_cles"].ToString(),
                                    Resume = row["resume"].ToString(),
                                    Fichier = row["fichier"].ToString(),
                                    UserId = Convert.ToInt32(row["user_id"])
                                });
                            }
                        }
                    }
                }

                ThesesDataGrid.ItemsSource = allTheses;
                UpdateThesisCounter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading theses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateThesisCounter()
        {
            int count = allTheses?.Count ?? 0;
            ThesisCounterText.Text = $"({count} theses)";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTheses();
        }

        private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterTheses();
        }

        private void FilterTheses()
        {
            if (allTheses == null) return;

            string searchText = SearchTextBox.Text.ToLower();
            string selectedType = (TypeFilterComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();

            var filteredTheses = allTheses.Where(t =>
                (string.IsNullOrEmpty(searchText) ||
                 t.Titre.ToLower().Contains(searchText) ||
                 t.Auteur.ToLower().Contains(searchText) ||
                 t.Speciality.ToLower().Contains(searchText)) &&
                (selectedType == "All Types" || t.Type.ToString() == selectedType)
            );

            ThesesDataGrid.ItemsSource = filteredTheses;
            UpdateThesisCounter();
        }

        private void ThesesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedThesis = ThesesDataGrid.SelectedItem as Theses;
            EditButton.IsEnabled = selectedThesis != null;
            DeleteButton.IsEnabled = selectedThesis != null;
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedThesis != null)
            {
                var addThesisWindow = new AddThesisWindow(selectedThesis);
                addThesisWindow.ShowDialog();
                LoadUserTheses(); // Refresh the list after editing
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedThesis != null)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this thesis? This action cannot be undone.",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string connectionString = AppConfig.CloudSqlConnectionString;
                        string query = "DELETE FROM theses WHERE id = @thesisId";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@thesisId", selectedThesis.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        allTheses.Remove(selectedThesis);
                        FilterTheses();
                        MessageBox.Show("Thesis deleted successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting thesis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
} 