using System.Data.SQLite;

namespace AIMP_Path_Replacer
{
	[SQLiteFunction(Name = "UNICODE", FuncType = FunctionType.Collation)]
	public class UnicodeCollation : SQLiteFunction
	{
		public override int Compare(string param1, string param2)
		{
			return string.Compare(param1, param2);
		}
	}
}
