using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using ThesesModels;
using System.Windows;
using System.Diagnostics;

namespace DataGridNamespace.Admin
{
    public partial class DemandsView : UserControl
    {
        private ObservableCollection<Theses> pendingTheses;

        public DemandsView()
        {
            try
            {
                InitializeComponent();
                pendingTheses = new ObservableCollection<Theses>();
                this.Loaded += DemandsView_Loaded;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DemandsView constructor: {ex.Message}");
                MessageBox.Show("Error initializing view. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DemandsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                LoadPendingTheses();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in DemandsView_Loaded: {ex.Message}");
                MessageBox.Show("Error loading theses. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPendingTheses()
        {
            try
            {
                if (pendingTheses == null)
                {
                    pendingTheses = new ObservableCollection<Theses>();
                }
                else
                {
                    pendingTheses.Clear();
                }

                string connectionString = AppConfig.CloudSqlConnectionString;
                
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"SELECT t.id, t.titre, t.auteur, t.speciality, t.Type, 
                                   t.mots_cles as MotsCles, t.annee, t.Resume, t.fichier, t.user_id as UserId, t.status
                                   FROM theses t
                                   WHERE t.status = 'pending'
                                   ORDER BY t.id ASC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    var thesis = new Theses
                                    {
                                        Id = reader.GetInt32("id"),
                                        Titre = reader.IsDBNull(reader.GetOrdinal("titre")) ? "" : reader.GetString("titre"),
                                        Auteur = reader.IsDBNull(reader.GetOrdinal("auteur")) ? "" : reader.GetString("auteur"),
                                        Speciality = reader.IsDBNull(reader.GetOrdinal("speciality")) ? "" : reader.GetString("speciality"),
                                        Type = (TypeThese)Enum.Parse(typeof(TypeThese), reader.GetString("Type")),
                                        MotsCles = reader.IsDBNull(reader.GetOrdinal("MotsCles")) ? "" : reader.GetString("MotsCles"),
                                        Annee = reader.GetDateTime("annee"),
                                        Resume = reader.IsDBNull(reader.GetOrdinal("Resume")) ? "" : reader.GetString("Resume"),
                                        Fichier = reader.IsDBNull(reader.GetOrdinal("fichier")) ? "" : reader.GetString("fichier"),
                                        UserId = reader.GetInt32("UserId"),
                                        Status = reader.GetString("status")
                                    };
                                    pendingTheses.Add(thesis);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error reading thesis record: {ex.Message}");
                                }
                            }
                        }
                    }
                }

                PendingThesesGrid.ItemsSource = pendingTheses;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadPendingTheses: {ex.Message}");
                MessageBox.Show($"Error loading pending theses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadPendingTheses();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RefreshButton_Click: {ex.Message}");
                MessageBox.Show("Error refreshing theses. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    var result = MessageBox.Show(
                        "Are you sure you want to accept this thesis?",
                        "Confirm Acceptance",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        string connectionString = AppConfig.CloudSqlConnectionString;
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "UPDATE theses SET status = 'accepted' WHERE id = @id";
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", thesis.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                
                                if (rowsAffected > 0)
                                {
                                    pendingTheses.Remove(thesis);
                                    MessageBox.Show("Thesis accepted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to accept thesis. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in AcceptButton_Click: {ex.Message}");
                    MessageBox.Show($"Error accepting thesis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DeclineButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    var result = MessageBox.Show(
                        "Are you sure you want to decline this thesis?",
                        "Confirm Decline",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        string connectionString = AppConfig.CloudSqlConnectionString;
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "UPDATE theses SET status = 'declined' WHERE id = @id";
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", thesis.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                
                                if (rowsAffected > 0)
                                {
                                    pendingTheses.Remove(thesis);
                                    MessageBox.Show("Thesis declined successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to decline thesis. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error in DeclineButton_Click: {ex.Message}");
                    MessageBox.Show($"Error declining thesis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
} 