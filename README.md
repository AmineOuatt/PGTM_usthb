# PPR - Thesis Management System

A comprehensive desktop application for managing academic theses and dissertations, built with WPF and .NET 9.0.

## 📋 Overview

The Thesis Management System (PPR) is a Windows desktop application designed to facilitate the management, search, and organization of academic theses (Doctorate and Master's degrees). The system provides role-based access control, cloud storage integration, and a modern user interface.

## ✨ Features

### User Management
- **Role-based Access Control**: Three user roles (Admin, SimpleUser, Etudiant)
- **User Authentication**: Firebase Authentication integration
- **Profile Management**: User profiles with cloud-stored profile pictures
- **Member Management**: Admin dashboard for managing system members

### Thesis Management
- **Thesis CRUD Operations**: Create, read, update, and delete theses
- **Advanced Search**: Search theses by title, author, keywords, and more
- **Filtering**: Filter by type (Doctorat/Master), year, and specialty
- **Pagination**: Efficient browsing with paginated results
- **PDF Viewing**: Built-in PDF viewer for thesis documents
- **Cloud Storage**: Secure file storage using Google Cloud Storage

### Additional Features
- **Favorites System**: Save and manage favorite theses
- **News Feed**: News and announcements system
- **Contact System**: Communication between users
- **Dashboard**: Admin dashboard with system statistics
- **Modern UI**: Material Design interface with responsive layouts

## 🛠️ Technology Stack

- **Framework**: .NET 9.0
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Database**: MySQL (via Cloud SQL)
- **Cloud Storage**: Google Cloud Storage
- **Authentication**: Firebase Authentication
- **UI Libraries**:
  - MaterialDesignThemes
  - MahApps.Metro.IconPacks

## 📦 Dependencies

- `Google.Apis.Auth` (v1.69.0)
- `Google.Cloud.Storage.V1` (v4.11.0)
- `MahApps.Metro.IconPacks.Material` (v4.11.0)
- `MaterialDesignThemes` (v4.9.0)
- `MySql.Data` (v9.2.0)
- `Newtonsoft.Json` (v13.0.3)
- `System.Net.Http.Json` (v8.0.0)

## 🚀 Getting Started

### Prerequisites

- .NET 9.0 SDK
- MySQL Database (or Cloud SQL)
- Google Cloud Platform account (for Cloud Storage and Firebase)
- Visual Studio 2022 or later (recommended)

### Installation

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd PPR
   ```

2. Navigate to the project directory:
   ```bash
   cd final_final_one-master/final_final_one-master
   ```

3. Configure the database connection in `AppConfig.cs`:
   ```csharp
   public const string CloudSqlConnectionString = "Server=YOUR_SERVER;Port=3306;Database=gestion_theses;Uid=YOUR_USER;Pwd=YOUR_PASSWORD;SslMode=None;";
   ```

4. Configure Firebase and Cloud Storage settings in `AppConfig.cs`:
   - Update `FirebaseApiKey`
   - Update `StorageBucket`
   - Update Cloud Functions endpoints

5. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

6. Build the project:
   ```bash
   dotnet build
   ```

7. Run the application:
   ```bash
   dotnet run
   ```

## 📁 Project Structure

```
final_final_one-master/
├── Models/              # Data models
│   ├── User.cs
│   ├── Theses.cs
│   ├── Favoris.cs
│   ├── NewsItem.cs
│   └── Contacts.cs
├── Veiw/                # Views and UI components
│   ├── Admin/          # Admin-specific views
│   ├── Etudiant/       # Student-specific views
│   ├── SimpleUser/     # Regular user views
│   └── Converters/     # Value converters
├── Services/            # Business logic and services
│   ├── CloudStorageService.cs
│   └── EmailService.cs
├── CloudFunctions/      # Cloud Functions for file handling
│   ├── generateUploadUrl/
│   └── generateReadUrl/
└── Styles/              # UI styles and themes
```

## 🔐 User Roles

- **Admin**: Full system access, member management, thesis approval
- **Etudiant**: Student access, thesis browsing, favorites management
- **SimpleUser**: Basic access, thesis viewing, contact features

## 📝 Database Schema

The system uses MySQL with the following main tables:
- `users`: User accounts and profiles
- `theses`: Thesis documents and metadata
- `favoris`: User favorites
- `contacts`: User communications
- `news`: News and announcements

## 🔧 Configuration

Key configuration settings are located in `AppConfig.cs`:

- **Database Connection**: Cloud SQL connection string
- **Firebase**: API key and authentication endpoints
- **Cloud Storage**: Bucket name and function endpoints

## 📄 License

[Specify your license here]

## 👥 Authors

[Add author information]

## 📞 Support

For issues and questions, please open an issue in the repository.

---

**Note**: Make sure to configure all cloud services (Firebase, Cloud Storage, Cloud SQL) before running the application.
