using DataGrid;
using DataGridNamespace;
using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UserModels;
using ThesesModels;
using FavorisModels;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyProject
{
    public partial class Login : Window
    {
        private readonly HttpClient httpClient = new HttpClient();

        public Login()
        {
            InitializeComponent();
        }

        private void CloseImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeRestoreButton_Click(sender, e);
            }
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                this.Width = 1280;
                this.Height = 720;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
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

        private void GoToLayout2_Click(object sender, RoutedEventArgs e)
        {
            Layout1.Visibility = Visibility.Collapsed;
            Layout2.Visibility = Visibility.Visible;
        }

        private void GoToLayout1_Click(object sender, RoutedEventArgs e)
        {
            Layout2.Visibility = Visibility.Collapsed;
            Layout1.Visibility = Visibility.Visible;
        }

        // Layout1 Events
        private void textUser_L1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtUser_L1.Focus();
        }

        private void txtUser_L1_TextChanged(object sender, TextChangedEventArgs e)
        {
            textUser_L1.Visibility = string.IsNullOrEmpty(txtUser_L1.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void textPassword_L1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPassword_L1.Focus();
        }

        private void txtPassword_L1_PasswordChanged(object sender, RoutedEventArgs e)
        {
            textPassword_L1.Visibility = string.IsNullOrEmpty(txtPassword_L1.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // Sign In button in Layout1
        // Sign In button in Layout1
        private async void SignIn_L1_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUser_L1.Text) || string.IsNullOrEmpty(txtPassword_L1.Password))
            {
                MessageBox.Show("Please fill Username & Password.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isConnected = DataGrid.Models.DatabaseConnection.TestConnection();
            if (!isConnected)
            {
                MessageBox.Show("Cannot connect to database. Please check your database settings.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Cursor = Cursors.Wait;

                string email = txtUser_L1.Text;
                string password = txtPassword_L1.Password;

                var firebaseResponse = await SignInWithFirebase(email, password);

                if (firebaseResponse == null)
                {
                    Cursor = Cursors.Arrow;
                    return;
                }

                string firebaseUid = firebaseResponse.LocalId;
                string idToken = firebaseResponse.IdToken;

                Debug.WriteLine($"Successfully authenticated with Firebase. UID: {firebaseUid}");

                // Check email verification - bypass for admin@yourapp.com
                var userInfo = await GetUserInfo(idToken);
                if (userInfo == null)
                {
                    MessageBox.Show("Failed to get user information. Please try again.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Cursor = Cursors.Arrow;
                    return;
                }

                // Skip email verification for admin@yourapp.com
                if (!string.Equals(email.Trim(), "admin@yourapp.com", StringComparison.OrdinalIgnoreCase))
                {
                    if (!userInfo.EmailVerified)
                    {
                        var result = MessageBox.Show(
                            "Your email is not verified. Would you like to resend the verification email?",
                            "Email Not Verified",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            try
                            {
                                await SendEmailVerification(idToken);
                                MessageBox.Show("Verification email has been sent. Please check your email and verify your account before logging in.",
                                    "Verification Email Sent",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Failed to send verification email: {ex.Message}",
                                    "Verification Error",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                            }
                        }
                        Cursor = Cursors.Arrow;
                        return;
                    }
                }

                string query = "SELECT id, nom, role, email FROM users WHERE firebase_uid = @FirebaseUid";

                User foundUser = null;

                using (var conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FirebaseUid", firebaseUid);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32("id");
                                string username = reader.GetString("nom");
                                string role = reader.GetString("role");
                                string userEmail = reader.GetString("email");

                                RoleUtilisateur userRole = RoleUtilisateur.SimpleUser;

                                if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                                {
                                    userRole = RoleUtilisateur.Admin;
                                }
                                else if (string.Equals(role, "etudiant", StringComparison.OrdinalIgnoreCase))
                                {
                                    userRole = RoleUtilisateur.Etudiant;
                                }

                                foundUser = new User
                                {
                                    Id = userId,
                                    Nom = username,
                                    Email = userEmail,
                                    Role = userRole,
                                    FirebaseUid = firebaseUid
                                };
                            }
                        }
                    }
                }

                if (foundUser == null)
                {
                    MessageBox.Show("User authenticated but not found in the database.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Cursor = Cursors.Arrow;
                    return;
                }

                Session.Initialize(
                    foundUser.Id,
                    foundUser.Nom,
                    foundUser.Role,
                    foundUser.FirebaseUid,
                    idToken
                );

                Debug.WriteLine($"User successfully logged in: {foundUser.Nom} (ID: {foundUser.Id}, Role: {foundUser.Role})");

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during login: {ex.Message}");
                MessageBox.Show($"Error during login: {ex.Message}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }


        // Layout2 Events
        private void textUser_L2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtUser_L2.Focus();
        }

        private void txtUser_L2_TextChanged(object sender, TextChangedEventArgs e)
        {
            textUser_L2.Visibility = string.IsNullOrEmpty(txtUser_L2.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void textEmail_L2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtEmail_L2.Focus();
        }

        private void txtEmail_L2_TextChanged(object sender, TextChangedEventArgs e)
        {
            textEmail_L2.Visibility = string.IsNullOrEmpty(txtEmail_L2.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void textPassword_L2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPassword_L2.Focus();
        }

        private void txtPassword_L2_PasswordChanged(object sender, RoutedEventArgs e)
        {
            textPassword_L2.Visibility = string.IsNullOrEmpty(txtPassword_L2.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // Sign Up button in Layout2
        // Sign Up button in Layout2
        private async void SignUp_L2_Click(object sender, RoutedEventArgs e)
        {
            string role = ((ComboBoxItem)roleCombo_L2.SelectedItem).Content.ToString();
            string email = txtEmail_L2.Text;

            if (!EmailIsValidForRole(role, email))
            {
                MessageBox.Show("For a student account, you must use an email in the form: utilisateur@etu.usthb.dz",
                                "Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;    
            }

            if (string.IsNullOrEmpty(txtUser_L2.Text))
            {
                MessageBox.Show("Please enter a username.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtEmail_L2.Text) || !IsValidEmail(txtEmail_L2.Text))
            {
                MessageBox.Show("Please enter a valid email address.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(txtPassword_L2.Password) || txtPassword_L2.Password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isConnected = DataGrid.Models.DatabaseConnection.TestConnection();
            if (!isConnected)
            {
                MessageBox.Show("Cannot connect to database. Please check your database settings.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Cursor = Cursors.Wait;

                string username = txtUser_L2.Text;
                string password = txtPassword_L2.Password;

                string checkQuery = "SELECT COUNT(*) FROM users WHERE email = @Email";

                using (var conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);

                        long count = (long)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("This email address is already registered.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            Cursor = Cursors.Arrow;
                            return;
                        }
                    }
                }

                var firebaseResponse = await SignUpWithFirebase(email, password);

                if (firebaseResponse == null)
                {
                    Cursor = Cursors.Arrow;
                    return;
                }

                string firebaseUid = firebaseResponse.LocalId;
                string idToken = firebaseResponse.IdToken;

                // Send email verification link
                try
                {
                    await SendEmailVerification(idToken);
                    MessageBox.Show($"A verification link has been sent to {email}. Please verify your email before logging in.", "Verify Email", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to send verification email: {ex.Message}", "Verification Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Cursor = Cursors.Arrow;
                    return;
                }

                string insertQuery = @"
                    INSERT INTO users (nom, email, password, role, firebase_uid)
                    VALUES (@Username, @Email, 'FIREBASE_AUTH', @Role, @FirebaseUid)";

                using (var conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Role", role);
                        cmd.Parameters.AddWithValue("@FirebaseUid", firebaseUid);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected <= 0)
                        {
                            MessageBox.Show("Failed to create user in database.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Cursor = Cursors.Arrow;
                            return;
                        }

                        cmd.CommandText = "SELECT LAST_INSERT_ID()";
                        int userId = Convert.ToInt32(cmd.ExecuteScalar());

                        // Convert the role string to RoleUtilisateur enum
                        RoleUtilisateur userRole = RoleUtilisateur.SimpleUser;
                        if (string.Equals(role, "Etudiant", StringComparison.OrdinalIgnoreCase))
                        {
                            userRole = RoleUtilisateur.Etudiant;
                        }
                        else if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
                        {
                            userRole = RoleUtilisateur.Admin;
                        }

                        Session.Initialize(
                            userId,
                            username,
                            userRole,
                            firebaseUid,
                            idToken
                        );

                        // Return to login screen after successful registration
                        Layout2.Visibility = Visibility.Collapsed;
                        Layout1.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during registration: {ex.Message}");
                MessageBox.Show($"Error during registration: {ex.Message}", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }




        private async Task<FirebaseAuthResponse> SignInWithFirebase(string email, string password)
        {
            try
            {
                string signInEndpoint = $"{AppConfig.FirebaseAuthBaseUrl}:signInWithPassword?key={AppConfig.FirebaseApiKey}";

                var requestData = new
                {
                    email,
                    password,
                    returnSecureToken = true
                };

                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(signInEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Firebase Sign-In Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<FirebaseAuthResponse>(responseContent);
                }
                else
                {
                    var errorObj = JObject.Parse(responseContent);
                    string errorMessage = errorObj["error"]["message"].ToString();
                    string userFriendlyMessage = GetUserFriendlyFirebaseError(errorMessage);

                    MessageBox.Show(userFriendlyMessage, "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during Firebase sign-in: {ex.Message}");
                MessageBox.Show($"Error during authentication: {ex.Message}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private async Task<FirebaseAuthResponse> SignUpWithFirebase(string email, string password)
        {
            try
            {
                string signUpEndpoint = $"{AppConfig.FirebaseAuthBaseUrl}:signUp?key={AppConfig.FirebaseApiKey}";

                var requestData = new
                {
                    email,
                    password,
                    returnSecureToken = true
                };

                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(signUpEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"Firebase Sign-Up Response: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<FirebaseAuthResponse>(responseContent);
                }
                else
                {
                    var errorObj = JObject.Parse(responseContent);
                    string errorMessage = errorObj["error"]["message"].ToString();
                    string userFriendlyMessage = GetUserFriendlyFirebaseError(errorMessage);

                    MessageBox.Show(userFriendlyMessage, "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during Firebase sign-up: {ex.Message}");
                MessageBox.Show($"Error during registration: {ex.Message}", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private async Task SendEmailVerification(string idToken)
        {
            try
            {
                var requestData = new
                {
                    requestType = "VERIFY_EMAIL",
                    idToken = idToken
                };

                string verifyEndpoint = $"{AppConfig.FirebaseAuthBaseUrl}:sendOobCode?key={AppConfig.FirebaseApiKey}";

                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(verifyEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(responseContent);
                    string errorMessage = errorObj["error"]["message"].ToString();
                    throw new Exception($"Failed to send verification email: {errorMessage}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during email verification: {ex.Message}");
                throw;
            }
        }

        private async void ForgotPasswordLink_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var email = txtUser_L1.Text;
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Please enter your email address first.", "Forgot Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidEmail(email))
            {
                MessageBox.Show("Please enter a valid email address.", "Forgot Password", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Cursor = Cursors.Wait;
                await SendPasswordResetEmail(email);
                MessageBox.Show($"A password reset link has been sent to {email}. Please check your email.", "Password Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("No account found with this email address"))
                {
                    var result = MessageBox.Show(
                        "No account found with this email address. Would you like to sign up for a new account?",
                        "Account Not Found",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Navigate to sign-up form
                        Layout1.Visibility = Visibility.Collapsed;
                        Layout2.Visibility = Visibility.Visible;
                        // Pre-fill the email field
                        txtEmail_L2.Text = email;
                    }
                }
                else
                {
                    MessageBox.Show($"Failed to send password reset email: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private async Task SendPasswordResetEmail(string email)
        {
            try
            {
                // Use the correct Firebase endpoint for password reset
                string resetEndpoint = $"{AppConfig.FirebaseAuthBaseUrl}:sendOobCode?key={AppConfig.FirebaseApiKey}";

                var requestData = new
                {
                    requestType = "PASSWORD_RESET",
                    email = email,
                    returnSecureToken = true
                };

                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Log the request details
                Debug.WriteLine("=== Password Reset Request Details ===");
                Debug.WriteLine($"Endpoint: {resetEndpoint}");
                Debug.WriteLine($"Request Data: {jsonContent}");
                Debug.WriteLine($"API Key: {AppConfig.FirebaseApiKey.Substring(0, 5)}...");

                var response = await httpClient.PostAsync(resetEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Log the response details
                Debug.WriteLine("=== Password Reset Response Details ===");
                Debug.WriteLine($"Status Code: {response.StatusCode}");
                Debug.WriteLine($"Response Content: {responseContent}");

                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = JObject.Parse(responseContent);
                    string errorMessage = errorObj["error"]["message"].ToString();
                    
                    // Log the error details
                    Debug.WriteLine("=== Error Details ===");
                    Debug.WriteLine($"Error Message: {errorMessage}");
                    Debug.WriteLine($"Full Error Object: {errorObj}");

                    // Check if the error is due to email not found
                    if (errorMessage == "EMAIL_NOT_FOUND")
                    {
                        throw new Exception("No account found with this email address. Please check your email or sign up for a new account.");
                    }
                    
                    // Check for other common Firebase errors
                    switch (errorMessage)
                    {
                        case "INVALID_EMAIL":
                            throw new Exception("The email address is invalid. Please check your email address.");
                        case "MISSING_EMAIL":
                            throw new Exception("Email address is required.");
                        case "OPERATION_NOT_ALLOWED":
                            throw new Exception("Password reset is not enabled for this project. Please contact support.");
                        case "TOO_MANY_ATTEMPTS_TRY_LATER":
                            throw new Exception("Too many attempts. Please try again later.");
                        default:
                            throw new Exception($"Failed to send password reset email: {GetUserFriendlyFirebaseError(errorMessage)}");
                    }
                }

                // If we get here, the request was successful
                Debug.WriteLine("=== Success ===");
                Debug.WriteLine("Password reset email sent successfully");
                Debug.WriteLine($"Response: {responseContent}");

                // Verify the response contains the expected data
                var responseObj = JObject.Parse(responseContent);
                if (responseObj["email"] == null)
                {
                    Debug.WriteLine("Warning: Response does not contain email confirmation");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("=== Exception Details ===");
                Debug.WriteLine($"Exception Message: {ex.Message}");
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                Debug.WriteLine($"Inner Exception: {ex.InnerException?.Message}");
                throw;
            }
        }

        private async Task<FirebaseUserInfo> GetUserInfo(string idToken)
        {
            try
            {
                string userInfoEndpoint = $"{AppConfig.FirebaseAuthBaseUrl}:lookup?key={AppConfig.FirebaseApiKey}";

                var requestData = new
                {
                    idToken = idToken
                };

                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(userInfoEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var responseObj = JObject.Parse(responseContent);
                    var user = responseObj["users"]?[0];
                    if (user != null)
                    {
                        return new FirebaseUserInfo
                        {
                            EmailVerified = user["emailVerified"]?.ToObject<bool>() ?? false,
                            Email = user["email"]?.ToString()
                        };
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception getting user info: {ex.Message}");
                return null;
            }
        }

        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        private string GetUserFriendlyFirebaseError(string errorCode)
        {
            switch (errorCode)
            {
                case "EMAIL_EXISTS":
                    return "This email address is already in use by another account.";
                case "OPERATION_NOT_ALLOWED":
                    return "Password sign-in is disabled for this project.";
                case "TOO_MANY_ATTEMPTS_TRY_LATER":
                    return "Too many unsuccessful login attempts. Please try again later.";
                case "EMAIL_NOT_FOUND":
                    return "There is no user account with this email address.";
                case "INVALID_PASSWORD":
                    return "The password is invalid.";
                case "USER_DISABLED":
                    return "This user account has been disabled by an administrator.";
                default:
                    return $"Authentication error: {errorCode}";
            }
        }

        private class FirebaseAuthResponse
        {
            [JsonProperty("idToken")]
            public string IdToken { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("refreshToken")]
            public string RefreshToken { get; set; }

            [JsonProperty("expiresIn")]
            public string ExpiresIn { get; set; }

            [JsonProperty("localId")]
            public string LocalId { get; set; }
        }

        private class FirebaseUserInfo
        {
            public bool EmailVerified { get; set; }
            public string Email { get; set; }
        }

    
        private static bool EmailIsValidForRole(string role, string email)
        {
            if (!string.Equals(role?.Trim(), "Etudiant", StringComparison.OrdinalIgnoreCase))
                return true;

            if (string.IsNullOrWhiteSpace(email))
                return false;

            email = email.Trim();                             

            const string pattern = @"^[A-Za-z0-9._%+-]+@etu\.usthb\.dz$";

            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }







    }
}
