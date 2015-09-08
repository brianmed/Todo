using System;

using SQLite;
using System.Threading.Tasks;

namespace Todo
{
	public class Globals
	{
		public static string SQLiteDir = Environment.GetFolderPath (Environment.SpecialFolder.Personal);

		public static SQLiteConnection SQLite;

		public static Settings theSettings = null;

		public static string EndpointBase = "";
	}

	public class Settings
	{
		[PrimaryKey, AutoIncrement]
		public int Id { get; set; }
		[MaxLength(128)]
		public string API_Key { get; set; }
		[MaxLength(128)]
		public string username { get; set; }
	}		
}