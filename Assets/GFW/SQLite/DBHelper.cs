using CodeX;
using LuaFramework;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DBHelper : Singleton<DBHelper>
{
    
	public void ExportDB(string dbPath, List<string> paths)
    {
        Debug.Log("dbPath = " + dbPath);

        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }

        for(int i=0;i<paths.Count;i++)
        {
            if (File.Exists(paths[i]))
            {
                InsertFile(paths[i]);
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(paths[i]);
                FileInfo[] fi = di.GetFiles();
                foreach(var file in fi)
                {
                    InsertFile(file.FullName);
                }

            }
        }

        //SQLiteDB db = new SQLiteDB();
        //db.Open(dbPath);

        //string create_table_sql = "create table file_info(\r\n\t\t    hash0 integer,\r\n\t\t    hash1 integer,\r\n\t\t    hash2 integer,\r\n\t\t    version integer,\r\n\t\t    file_name char(256),\r\n\t\t    file_md5 char(33),\r\n\t\t    data_type integer,\r\n\t\t    content blob,\r\n\t\t    data_len integer,\r\n\t\t    zip_flag integer,\r\n\t\t    unzip_len integer,\r\n\t\t    crypto_flag integer,\r\n\t\t    ctl_flag integer\r\n\t\t    );\r\n\r\n\t\t    CREATE UNIQUE INDEX hash_info ON file_info(hash0 ASC, hash1 ASC, hash2 ASC);\r\n\r\n\t\t    create table own_version(\r\n\t\t    version integer);\r\n\r\n\t\t    insert into own_version(version) values(2);";
        //db.ExcuteQueryNoResult(create_table_sql);


    }

    private void InsertFile(string filePath)
    {
        Debug.Log(filePath);
        DbFileInfo file_info = new DbFileInfo();
        file_info.hash_info = new PathHashInfo();
        file_info.hash_info.hash0 = -567439084;
        file_info.hash_info.hash1 = -94960054;
        file_info.hash_info.hash2 = -1365269703;
        file_info.version = 1;
        file_info.file_name = "/client/assets/icon/0.png";
        file_info.file_md5 = "BEAD7EDBF049F6E8CEADE7EC7D94EAC5";
        file_info.data_type = 0;
        file_info.data_len = 17820;
        file_info.zip_flag = 0;
        file_info.unzip_len = 17820;
        file_info.crypto_flag = 0;
        file_info.ctl_flag = 0;

        string insert_sql = string.Format("insert into file_info( hash0, hash1, hash2, data_len, version, file_name, file_md5, data_type, zip_flag, unzip_len, crypto_flag, ctl_flag) values('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}')", new object[]
			{
				file_info.hash_info.hash0.ToString(),
				file_info.hash_info.hash1.ToString(),
				file_info.hash_info.hash2.ToString(),
				file_info.data_len.ToString(),
				file_info.version.ToString(),
				file_info.file_name.ToString(),
				file_info.file_md5.ToString(),
				file_info.data_type.ToString(),
				file_info.zip_flag.ToString(),
				file_info.unzip_len.ToString(),
				file_info.crypto_flag.ToString(),
				file_info.ctl_flag.ToString()
			});
        //db.ExcuteQueryNoResult(insert_sql);
    }

    public void ExportBIN(string dbPath, List<string> paths)
    {

    }
}
