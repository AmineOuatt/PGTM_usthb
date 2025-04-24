using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace DataGridNamespace.Veiw.Converters
{
    public class ProfilePictureUrlConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string profilePicRef = value as string;

            if (!string.IsNullOrEmpty(profilePicRef))
            {
                // Replace 'your-bucket-name' with the actual name of your bucket
                string bucketName = "thesis-manager-files-hr-5173";
                string profilePicUrl = $"https://storage.googleapis.com/{bucketName}/{profilePicRef}";
                // Debug log to check the generated image URL
                Debug.WriteLine($"Generated Profile Picture URL: {profilePicUrl}");
                return profilePicUrl;
            }

            return null; // Or return a placeholder image URL if you have one
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
