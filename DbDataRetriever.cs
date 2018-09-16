using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace AIMP_Path_Replacer
{
	public class DbDataRetriever
	{
		private SQLiteConnection m_conn;

		public int DiskId { get; private set; }
		public char DiskLetter { get; private set; }
		public string DiskTitle { get; private set; }

		public List<string> UsedExtensions { get; private set; }

		public IDictionary<AimpFile, IList<string>> Files { get; private set; }

		public DbDataRetriever(SQLiteConnection sqlCon)
		{
			m_conn = sqlCon;
		}

		public bool SelectDisk()
		{
			// Retrieve disk numbers
			using (SQLiteCommand com = new SQLiteCommand("SELECT ID, iLetter, sTitle FROM MediaDrives", m_conn))
			{
				SQLiteDataReader reader = com.ExecuteReader();
				Console.WriteLine("Available drives:");
				while (reader.Read())
				{
					Console.WriteLine("ID: {0}\tLetter: {1}\tTitle: {2}", reader[0], Convert.ToChar(reader[1]), reader[2]);
				}

				int id;
				Console.Write("\nEnter ID: ");
				if (int.TryParse(Console.ReadLine(), out id) == false)
				{
					Console.WriteLine("Invalid ID!");
					return false;
				}
				DiskId = id;
			}
			
			// Select disk
			using (SQLiteCommand com = new SQLiteCommand("SELECT iLetter, sTitle FROM MediaDrives WHERE ID = " + DiskId, m_conn))
			{
				SQLiteDataReader reader = com.ExecuteReader();
				reader.Read();

				DiskLetter = Convert.ToChar(reader[0]);
				DiskTitle = (string) reader[1];
			}
			
			Console.WriteLine();
			return true;
		}

		public void LookupExtensions()
		{
			UsedExtensions = new List<string>();
			using (SQLiteCommand com = new SQLiteCommand(@"SELECT DISTINCT CASE WHEN instr(ext, ':') != 0 THEN substr(ext, 0, instr(ext, ':')) ELSE ext END FROM (
															SELECT DISTINCT replace(sName, rtrim(sName, replace(sName, '.', '')), '') AS ext
															FROM MediaBase
															WHERE iDriveID = " + DiskId + ")", m_conn))
			{
				SQLiteDataReader reader = com.ExecuteReader();
				while (reader.Read())
				{
					UsedExtensions.Add("." + reader[0]);
				}
			}

			Console.WriteLine("Retrieved {0} extensions: {1}", UsedExtensions.Count, string.Join(", ", UsedExtensions));
		}

		public void RetrieveData(DirectoryInfo folder)
		{
			Files = new Dictionary<AimpFile, IList<string>>();
			using (SQLiteCommand com = new SQLiteCommand(string.Format("SELECT ID, sName FROM MediaBase WHERE iDriveID = {0} AND sName LIKE '{1}%'", DiskId, folder.FullName.Substring(2)), m_conn))
			{
				SQLiteDataReader reader = com.ExecuteReader();
				while (reader.Read())
				{
					AimpFile file = new AimpFile
					{
						Id = (long) reader["ID"],
						Name = (string) reader["sName"]
					};

					int plsIndex = file.Name.LastIndexOf(':');
					if (plsIndex != -1)
					{
						file.PlaylistNo = Convert.ToInt32(file.Name.Substring(plsIndex + 1));
						file.Name = file.Name.Substring(0, plsIndex);
					}
					else
					{
						file.PlaylistNo = -1;
					}

					file.Name = Path.GetFileName(file.Name);

					var list = new List<string>();
					list.Add((string) reader[1]);
					try
					{
						Files.Add(file, list);
					}
					catch(ArgumentException) // Same file exists twice
					{
						Files[file].Add((string) reader[1]);
					}
				}
			}
		}
	}
}
