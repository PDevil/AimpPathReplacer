using System;

namespace AIMP_Path_Replacer
{
	public struct AimpFile
	{
		public long Id { get; set; }
		public string Name { get; set; }
		public int PlaylistNo { get; set; }

		public override bool Equals(object obj)
		{
			if (obj == null || GetType() != obj.GetType())
				return false;

			AimpFile o = (AimpFile) obj;

			bool result = this.Name == o.Name;
			if (this.Id != 0 && o.Id != 0)
				result &= this.Id == o.Id;

			//if (Playlist == true)
				//result &= o.Playlist == true;

			return result;
		}

		public override int GetHashCode()
		{
			return Name.GetHashCode();
		}
	}
}
