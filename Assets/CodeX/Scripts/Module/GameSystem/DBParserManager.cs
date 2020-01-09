using System;
using GFW;

namespace CodeX
{
	public class DBParserManager : ManagerModule
	{
		public DBParser OpenDb(byte[] bytes)
		{
			DBParser db = new DBParser();
			db.OpenDbFromMemory(bytes);
			return db;
		}

		public DBParser OpenDb(string path)
		{
			DBParser db = new DBParser();
			db.InitDBFile(path);
			return db;
		}
	}
}
