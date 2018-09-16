using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace AIMP_Path_Replacer
{
	class Program
	{
		static int Main(string[] args)
		{
			// Usage
			if (args.Length < 1)
			{
				Console.WriteLine("usage: {0} <db file>", AppDomain.CurrentDomain.FriendlyName);
				return 1;
			}
			
			// Open DB
			if (!File.Exists(args[0]))
			{
				Console.WriteLine("Database does not exist!");
				return 1;
			}

			SQLiteFunction.RegisterFunction(typeof(UnicodeCollation));
			SQLiteConnection con = new SQLiteConnection("Data Source=" + args[0]);
			con.Open();

			DbDataRetriever dbRetriever = new DbDataRetriever(con);
			dbRetriever.SelectDisk();
			dbRetriever.LookupExtensions();
			
			// Select folder
			Console.Write(@"Enter folder name you want to search (`\` only): {0}:\", dbRetriever.DiskLetter);
			DirectoryInfo folder = new DirectoryInfo(string.Format(@"{0}:\{1}", dbRetriever.DiskLetter, Console.ReadLine()));

			dbRetriever.RetrieveData(folder);
			Console.WriteLine("Files to find: {0}", dbRetriever.Files.Count);
			
			// Create log file
			/*StreamWriter logger = new StreamWriter("aimp-path-replacer.log", false);
			logger.AutoFlush = false;*/
			
			// Search files and save them
			DiskDataRetriever diskRetriever = new DiskDataRetriever(folder);
			diskRetriever.Extensions = dbRetriever.UsedExtensions;
			diskRetriever.RetrieveData();
			
			// Save new paths
			MusicDataLinker linker = new MusicDataLinker();
			linker.LogicalFiles = dbRetriever.Files;
			linker.PhysicalFiles = diskRetriever.Files;
			linker.SetFileNotFoundFunction((AimpFile file, string output) =>
			{
				string errMsg = string.Format("Could not find \"{0}\" from folder \"{1}\". Insert name manually (without path) or leave empty to skip: ", file.Name, Path.GetDirectoryName(output));
				Console.WriteLine(errMsg);
				//logger.Write(errMsg);
				
				string manualName = Console.ReadLine();
				/*if (manualName != "")
				{
					logger.WriteLine(manualName);
					return manualName;
				}*/

				//logger.WriteLine("<Skipped>");
				return manualName;
			});
			linker.SetDbCollisionFunction((AimpFile file, IList<string> outputs) =>
			{
				Console.WriteLine("There are multiple entries in database for \"{0}\".\nSelect one to work with for now or leave empty to skip", file.Name);
				Console.WriteLine();
				for (int i = 0; i < outputs.Count; ++i)
				{
					Console.WriteLine("   [{0}] {1}", i + 1, outputs[i]);
				}
				Console.WriteLine();

				string line = Console.ReadLine();
				if (line == "")
					return -1;

				int entry;
				if (int.TryParse(line, out entry) == false)
				{
					return -1;
				}

				return entry - 1;
			});
			linker.SetPhyCollisionFunction((AimpFile file, string fullName, IList<string> outputs) =>
			{
				Console.WriteLine("Select a file that is connected to \"{0}\".", fullName);
				Console.WriteLine();
				for (int i = 0; i < outputs.Count; ++i)
					Console.WriteLine("   [{0}] {1}", i + 1, outputs[i]);

				Console.WriteLine();

				int entry;
				if (int.TryParse(Console.ReadLine(), out entry) == false)
				{
					return -1;	
				}

				return entry - 1;
			});
			linker.Run();

			// Update new paths
			using (SQLiteTransaction transaction = con.BeginTransaction())
			{
				foreach (var path in linker.Result)
				{
					SQLiteCommand comm = new SQLiteCommand(@"UPDATE MediaBase SET sName = ? WHERE ID = ?", con, transaction);
					var sName = new SQLiteParameter(DbType.String) { Value = path.Value };
					var ID = new SQLiteParameter(DbType.Int64) { Value = path.Key };
					comm.Parameters.Add(sName);
					comm.Parameters.Add(ID);

					comm.ExecuteNonQuery();
					comm.Dispose();
				}
				transaction.Commit();
			}

			con.Close();
			con.Dispose();
			Console.WriteLine("Done.");
			Console.Read();
			return 0;
		}
	}
}
