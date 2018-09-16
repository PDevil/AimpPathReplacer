using System;
using System.Collections.Generic;

namespace AIMP_Path_Replacer
{
	public class MusicDataLinker
	{
		private Func<AimpFile, IList<string>, int> m_dbColFunc;
		private Func<AimpFile, string, IList<string>, int> m_phyColFunc;
		private Func<AimpFile, string, string> m_fnfFunc;

		public IDictionary<AimpFile, IList<string>> LogicalFiles { get; set; }
		public IDictionary<AimpFile, IList<string>> PhysicalFiles { get; set; }

		public IDictionary<long, string> Result { get; private set; }

		public void SetDbCollisionFunction(Func<AimpFile, IList<string>, int> fn)
		{
			m_dbColFunc = fn;
		}

		public void SetPhyCollisionFunction(Func<AimpFile, string, IList<string>, int> fn)
		{
			m_phyColFunc = fn;
		}

		public void SetFileNotFoundFunction(Func<AimpFile, string, string> fn)
		{
			m_fnfFunc = fn;
		}

		public void Run()
		{
			Result = new Dictionary<long, string>();
			foreach (var dbEntry in LogicalFiles)
			{
				int workingIndex = 0;
				if (dbEntry.Value.Count > 1)
				{
					workingIndex = m_dbColFunc(dbEntry.Key, dbEntry.Value); // -1 - stahp, 0..n - wórk
					if (workingIndex == -1)
						continue;
				}

				AimpFile key = dbEntry.Key;
				IList<string> phyEntries;
			lookup:
				if(PhysicalFiles.TryGetValue(key, out phyEntries) == false) // No entries
				{
					var ret = m_fnfFunc(key, dbEntry.Value[workingIndex]);
					if (ret == "")
						continue;

					key.Name = ret;
					goto lookup;
				}
				else // 1+ entries
				{
					int workingPhyIndex = 0;
					if(phyEntries.Count > 1)
					{
						workingPhyIndex = m_phyColFunc(key, dbEntry.Value[workingIndex], phyEntries);
						if (workingPhyIndex == -1)
							continue;
					}
					
					if (key.PlaylistNo == -1)
						Result[key.Id] = phyEntries[workingPhyIndex];
					else
						Result[key.Id] = string.Join(":", phyEntries[workingPhyIndex], key.PlaylistNo);

					Console.WriteLine("'{0}' -> '{1}'", dbEntry.Value[workingIndex].Length > 50 ? (dbEntry.Value[workingIndex].Substring(0, 50) + "...") : dbEntry.Value[workingIndex],
													phyEntries[workingPhyIndex].Length > 50 ? (phyEntries[workingPhyIndex].Substring(0, 50) + "...") : phyEntries[workingPhyIndex]);
				}
			}
		}
	}
}
