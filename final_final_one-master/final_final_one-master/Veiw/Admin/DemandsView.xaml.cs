using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using ThesesModels;

namespace DataGridNamespace.Admin
{
    public partial class DemandsView : UserControl
    {
        private ObservableCollection<Theses> pendingTheses;

        public DemandsView()
        {
            InitializeComponent();
            this.Loaded += DemandsView_Loaded;
        }

        private void DemandsView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            LoadPendingTheses();
        }

        private void LoadPendingTheses()
        {
            try
            {
                pendingTheses = new ObservableCollection<Theses>();
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
                        }
                    }
                }

                PendingThesesGrid.ItemsSource = pendingTheses;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading pending theses: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
} 