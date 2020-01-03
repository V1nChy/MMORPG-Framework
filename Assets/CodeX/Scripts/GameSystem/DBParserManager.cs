using System;
using GFW.ManagerSystem;
using GFW;

namespace CodeX
{
	public class DBParserManager : Manager
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
