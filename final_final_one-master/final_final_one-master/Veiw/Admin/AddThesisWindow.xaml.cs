using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.IO;
using ThesesModels;
using DataGridNamespace.Services;
using System.Threading.Tasks;

namespace DataGridNamespace.Admin
{
    public partial class AddThesisWindow : Window
    {
        private readonly CloudStorageService _cloudStorageService;
        private Theses existingThesis;
        private bool isEditMode;
        
        public AddThesisWindow()
        {
            InitializeComponent();
            
            // Set default date to current date
            YearDatePicker.SelectedDate = DateTime.Now;
            
            // Set default type to first item
            TypeComboBox.SelectedIndex = 0;
            
            // Initialize cloud storage service
            _cloudStorageService = new CloudStorageService();
            
            // Set window title
            Title = "Add New Thesis";
        }

        public AddThesisWindow(Theses thesis) : this()
        {
            existingThesis = thesis;
            isEditMode = true;
            
            // Set window title
            Title = "Edit Thesis";
            
            // Populate fields with existing thesis data
            TitleTextBox.Text = thesis.Titre;
            AuthorTextBox.Text = thesis.Auteur;
            SpecialtyTextBox.Text = thesis.Speciality;
            
            // Set type
            foreach (ComboBoxItem item in TypeComboBox.Items)
            {
                if (item.Content.ToString() == thesis.Type.ToString())
                {
                    TypeComboBox.SelectedItem = item;
                    break;
                }
            }
            
            KeywordsTextBox.Text = thesis.MotsCles;
            YearDatePicker.SelectedDate = thesis.Annee;
            AbstractTextBox.Text = thesis.Resume;
            FilePathTextBox.Text = thesis.Fichier;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Select Thesis PDF File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate all fields
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
                {
                    MessageBox.Show("Please enter a title.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TitleTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(AuthorTextBox.Text))
                {
                    MessageBox.Show("Please enter an author.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    AuthorTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(SpecialtyTextBox.Text))
                {
                    MessageBox.Show("Please enter a specialty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SpecialtyTextBox.Focus();
                    return;
                }

                if (TypeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TypeComboBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(KeywordsTextBox.Text))
                {
                    MessageBox.Show("Please enter keywords.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    KeywordsTextBox.Focus();
                    return;
                }

                if (YearDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Please select a date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    YearDatePicker.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(AbstractTextBox.Text))
                {
                    MessageBox.Show("Please enter an abstract.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    AbstractTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(FilePathTextBox.Text))
                {
                    MessageBox.Show("Please select a PDF file.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    BrowseButton.Focus();
                    return;
                }

                // Get current user ID and role from session
                int currentUserId = DataGridNamespace.Session.CurrentUserId;
                UserModels.RoleUtilisateur currentUserRole = DataGridNamespace.Session.CurrentUserRole;

                // Determine the status based on user role
                string thesisStatus = (currentUserRole == UserModels.RoleUtilisateur.Admin) ? "accepted" : "pending";

                // Show "Please wait" message
                SaveButton.IsEnabled = false;
                CancelButton.IsEnabled = false;
                
                this.Cursor = Cursors.Wait;
                
                string uploadedObjectName = FilePathTextBox.Text;
                
                // Only upload new file if it's different from the existing one
                if (isEditMode && FilePathTextBox.Text != existingThesis.Fichier)
                {
                    // Upload file to Cloud Storage
                    string fileName = Path.GetFileName(FilePathTextBox.Text);
                    string objectName = $"theses/{Guid.NewGuid()}/{fileName}";
                    
                    try
                    {
                        uploadedObjectName = await _cloudStorageService.UploadFileViaSignedUrl(FilePathTextBox.Text, objectName);
                        
                        if (uploadedObjectName == null)
                        {
                            MessageBox.Show("Failed to upload file to cloud storage.", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            
                            SaveButton.IsEnabled = true;
                            CancelButton.IsEnabled = true;
                            this.Cursor = Cursors.Arrow;
                            
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error uploading file: {ex.Message}");
                        MessageBox.Show($"Error uploading file: {ex.Message}", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        
                        SaveButton.IsEnabled = true;
                        CancelButton.IsEnabled = true;
                        this.Cursor = Cursors.Arrow;
                        
                        return;
                    }
                }

                // Get type from ComboBox
                string typeStr = ((ComboBoxItem)TypeComboBox.SelectedItem).Content.ToString();
                TypeThese type = (TypeThese)Enum.Parse(typeof(TypeThese), typeStr);

                // Save to database
                string query;
                if (isEditMode)
                {
                    query = @"UPDATE theses 
                            SET titre = @titre, auteur = @auteur, speciality = @speciality, 
                                Type = @type, mots_cles = @motsCles, annee = @annee, 
                                Resume = @resume, fichier = @fichier 
                            WHERE id = @id AND user_id = @userId";
                }
                else
                {
                    // Include status column in the INSERT query
                    query = @"INSERT INTO theses (titre, auteur, speciality, Type, mots_cles, annee, Resume, fichier, user_id, status) 
                            VALUES (@titre, @auteur, @speciality, @type, @motsCles, @annee, @resume, @fichier, @userId, @status)";
                }

                using (MySqlConnection conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@titre", TitleTextBox.Text);
                        cmd.Parameters.AddWithValue("@auteur", AuthorTextBox.Text);
                        cmd.Parameters.AddWithValue("@speciality", SpecialtyTextBox.Text);
                        cmd.Parameters.AddWithValue("@type", type.ToString());
                        cmd.Parameters.AddWithValue("@motsCles", KeywordsTextBox.Text);
                        cmd.Parameters.AddWithValue("@annee", YearDatePicker.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@resume", AbstractTextBox.Text);
                        cmd.Parameters.AddWithValue("@fichier", uploadedObjectName);
                        cmd.Parameters.AddWithValue("@userId", currentUserId);
                        
                        if (isEditMode)
                        {
                            cmd.Parameters.AddWithValue("@id", existingThesis.Id);
                        }
                        else
                        {
                            // Add status parameter for new theses
                            cmd.Parameters.AddWithValue("@status", thesisStatus);
                        }

                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show($"Thesis {(isEditMode ? "updated" : "added")} successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Close the window with success result
                            DialogResult = true;
                            Close();
                        }
                        else
                        {
                            MessageBox.Show($"Failed to {(isEditMode ? "update" : "add")} thesis. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            
                            SaveButton.IsEnabled = true;
                            CancelButton.IsEnabled = true;
                            this.Cursor = Cursors.Arrow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error {(isEditMode ? "updating" : "adding")} thesis: {ex.Message}");
                MessageBox.Show($"Error {(isEditMode ? "updating" : "adding")} thesis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                SaveButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
                this.Cursor = Cursors.Arrow;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 