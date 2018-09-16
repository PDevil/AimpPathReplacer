using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AIMP_Path_Replacer
{
	public class DiskDataRetriever
	{
		public DirectoryInfo Directory { get; private set; }
		public IList<string> Extensions { get; set; }

		public IDictionary<AimpFile, IList<string>> Files { get; private set; }

		public DiskDataRetriever(DirectoryInfo directory)
		{
			Directory = directory;
		}

		public void RetrieveData()
		{
			Files = new Dictionary<AimpFile, IList<string>>();
			RecursiveSearch(Directory);
		}

		private void RecursiveSearch(DirectoryInfo dir)
		{
			foreach (var fsinfo in dir.EnumerateFileSystemInfos())
			{
				if (fsinfo is DirectoryInfo)
				{
					RecursiveSearch(fsinfo as DirectoryInfo);
				}
				else if (fsinfo is FileInfo)
				{
					var file = fsinfo as FileInfo;
					if (Extensions.Any(ext => file.Extension == ext))
					{
						var aimpFile = new AimpFile { Name = file.Name };
						var list = new List<string>();
						list.Add(file.FullName.Substring(2));
						try
						{
							Files.Add(aimpFile, list);
						}
						catch (ArgumentException)
						{
							Files[aimpFile].Add(file.FullName.Substring(2));
						}
					}
				}
			}
		}
	}
}
