using System;
using MySql.Data.MySqlClient;

namespace DataGrid.Models
{
	public class DatabaseConnection
	{
		private string connectionString = "Server=localhost;Database=gestion_theses;User ID=root;Password=";

		public void TestConnection()
		{
			try
			{
				using (MySqlConnection conn = new MySqlConnection(connectionString))
				{
					conn.Open();
					Console.WriteLine("Connexion r�ussie !");
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Erreur de connexion : {ex.Message}");
			}
		}
	}
}