using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DataGridNamespace.Services
{
    public class EmailService
    {
        public Task SendThesisStatusEmailAsync(string toEmail, string thesisTitle, string status, string rejectionReason = null)
        {
            try
            {
                string subject = $"Thesis Status Update: {thesisTitle}";
                string body = GenerateEmailBody(thesisTitle, status, rejectionReason);

                // Create mailto URL with subject and body
                string mailtoUrl = $"mailto:{toEmail}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

                // Open default email client
                Process.Start(new ProcessStartInfo
                {
                    FileName = mailtoUrl,
                    UseShellExecute = true
                });

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening email client: {ex.Message}");
                return Task.FromException(ex);
            }
        }

        private string GenerateEmailBody(string thesisTitle, string status, string rejectionReason)
        {
            string statusMessage = status.ToLower() == "accepted" 
                ? "Your thesis has been accepted!"
                : "Your thesis has been declined.";

            string body = $"Dear Student,\n\n" +
                         $"{statusMessage}\n\n" +
                         $"Thesis Title: {thesisTitle}\n";

            if (status.ToLower() == "declined" && !string.IsNullOrEmpty(rejectionReason))
            {
                body += $"\nReason for Rejection:\n{rejectionReason}\n";
            }

            body += "\nThank you for using our Thesis Management System.\n\n" +
                   "Best regards,\nThe Admin Team";

            return body;
        }
    }
} 