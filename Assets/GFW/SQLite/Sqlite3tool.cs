using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zlib;
using LuaFramework;
using UnityEngine;
using GFW;
using CodeX;

public class Sqlite3tool
{
    private SQLiteDB m_sqlite_db = null;

    private bool m_debug_mode = false;

    private static ZlibCodec m_zlib_codec = new ZlibCodec();

	public bool DebugMode
	{
		get
		{
			return this.m_debug_mode;
		}
		set
		{
			this.m_debug_mode = value;
		}
	}

    #region -创建关闭
    public bool OpenOrCreateDb(string db_path)
	{
        if (m_sqlite_db == null)
		{
            if (File.Exists(db_path) || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.IPhonePlayer)
			{
				m_sqlite_db = new SQLiteDB();
				m_sqlite_db.Open(db_path);
				return true;
			}
			else
			{
				this.m_sqlite_db = new SQLiteDB();
				this.m_sqlite_db.Open(db_path);
				this.CreateDependTable(this.m_sqlite_db);
				return true;
			}
		}
		return false;
	}
    private void CreateDependTable(SQLiteDB db)
    {
        string create_table_sql = "create table file_info(\r\n\t\t    hash0 integer,\r\n\t\t    hash1 integer,\r\n\t\t    hash2 integer,\r\n\t\t    version integer,\r\n\t\t    file_name char(256),\r\n\t\t    file_md5 char(33),\r\n\t\t    data_type integer,\r\n\t\t    content blob,\r\n\t\t    data_len integer,\r\n\t\t    zip_flag integer,\r\n\t\t    unzip_len integer,\r\n\t\t    crypto_flag integer,\r\n\t\t    ctl_flag integer\r\n\t\t    );\r\n\r\n\t\t    CREATE UNIQUE INDEX hash_info ON file_info(hash0 ASC, hash1 ASC, hash2 ASC);\r\n\r\n\t\t    create table own_version(\r\n\t\t    version integer);\r\n\r\n\t\t    insert into own_version(version) values(2);";
        int result = db.ExcuteQueryNoResult(create_table_sql);
        bool debug_mode = this.m_debug_mode;
        if (debug_mode)
        {
            LogManager.Log(string.Format("excute sql {0}, result {1}", create_table_sql, result));
        }
    }
	public bool OpenDbFromMemory(byte[] buffer)
	{
		bool result;
		try
		{
			MemoryStream memStream = new MemoryStream();
			memStream.Write(buffer, 0, buffer.Length);
			this.m_sqlite_db = new SQLiteDB();
			this.m_sqlite_db.OpenStream("stream_db", memStream);
			result = true;
		}
		catch (Exception e)
		{
			LogManager.Log(string.Format("Create sqlitedb from memory failed!, error is {0}", e.Message));
			result = false;
		}
		return result;
	}
	public void CloseDb()
	{
		bool flag = this.m_sqlite_db != null;
		if (flag)
		{
			this.m_sqlite_db.Close();
			this.m_sqlite_db = null;
		}
	}
    #endregion

    public List<DbFileInfo> CacheFileList()
    {
        List<DbFileInfo> file_info_array = new List<DbFileInfo>();
        if (this.m_sqlite_db != null)
        {
            string sql = "select hash0, hash1, hash2, version, file_name, file_md5, data_type, data_len, zip_flag, unzip_len, crypto_flag, ctl_flag from file_info";
            SQLiteQuery qr = new SQLiteQuery(this.m_sqlite_db, sql);
            while (qr.Step())
            {
                DbFileInfo fi = new DbFileInfo();
                fi.hash_info = new PathHashInfo();
                fi.hash_info.hash0 = qr.GetInteger("hash0");
                fi.hash_info.hash1 = qr.GetInteger("hash1");
                fi.hash_info.hash2 = qr.GetInteger("hash2");
                fi.version = qr.GetInteger("version");
                fi.file_name = qr.GetString("file_name");
                fi.file_md5 = qr.GetString("file_md5");
                fi.data_type = qr.GetInteger("data_type");
                fi.data_len = qr.GetInteger("data_len");
                fi.zip_flag = qr.GetInteger("zip_flag");
                fi.unzip_len = qr.GetInteger("unzip_len");
                fi.crypto_flag = qr.GetInteger("crypto_flag");
                fi.ctl_flag = qr.GetInteger("ctl_flag");
                file_info_array.Add(fi);
            }
        }
        return file_info_array;
    }
    public bool ReplaceFileInfoToDb(DbFileInfo file_info)
	{
		bool flag = this.m_sqlite_db == null;
		bool result2;
		if (flag)
		{
			result2 = false;
		}
		else
		{
			string replace_sql = string.Format("replace into file_info( hash0, hash1, hash2, data_len, version, file_name, file_md5, data_type, zip_flag, unzip_len, crypto_flag, ctl_flag) values('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}')", new object[]
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
			int result = this.m_sqlite_db.ExcuteQueryNoResult(replace_sql);
			bool debug_mode = this.m_debug_mode;
			if (debug_mode)
			{
				LogManager.Log(string.Format("excute sql:{0}, result:{1}", replace_sql, result));
			}
			result2 = (result == 0);
		}
		return result2;
	}
	public bool QueryFileVersionByHashInfo(string file_path, out int file_version)
	{
		PathHashInfo hash_info = DataEncrypt.GetPathHashInfo(file_path);
		bool is_suc = false;
		file_version = 0;
		bool flag = this.m_sqlite_db != null;
		if (flag)
		{
			string sql = string.Format("select version from file_info where hash0 = '{0}' and hash1 = '{1}' and hash2 = '{2}'", hash_info.hash0, hash_info.hash1, hash_info.hash2);
			SQLiteQuery qr = new SQLiteQuery(this.m_sqlite_db, sql);
			if (qr.Step())
			{
				file_version = qr.GetInteger("version");
				is_suc = true;
			}
		}
		return is_suc;
	}
	public bool DeleteFileInfo(PathHashInfo hash_info)
	{
		string del_sql = string.Format("delete from file_info where hash0 = '{0}' and hash1 = '{1}' and hash2 = '{2}'", hash_info.hash0, hash_info.hash1, hash_info.hash2);
		int result = this.m_sqlite_db.ExcuteQueryNoResult(del_sql);
		return result == 0;
	}
	public bool ClearDependInfo()
	{
		bool flag = this.m_sqlite_db == null;
		bool result2;
		if (flag)
		{
			result2 = false;
		}
		else
		{
			string clear_sql = "DELETE FROM ab_depend_info";
			int result = this.m_sqlite_db.ExcuteQueryNoResult(clear_sql);
			result2 = (result == 0);
		}
		return result2;
	}

    public static bool OpenCyMzq(string file_name, ref Dictionary<string, DbFileInfo> map)
    {
        FileStream fs = new FileStream(file_name, FileMode.Open);
        int nLength = (int)fs.Length;
        BinaryReader tmp_reader = new BinaryReader(fs);
        int unzip = tmp_reader.ReadInt32();

        bool result;
        if (unzip == 0)
        {
            result = false;
        }
        else
        {
            int md5_data_size = 4;
            int version_data_size = 4;
            int len_data_size = 4;

            byte[] buffs = tmp_reader.ReadBytes(nLength - 4);
            int len = 0;
            byte[] DecompBuffs = Sqlite3tool.DecompNetBuffer(buffs, nLength, unzip, out len);
            if (unzip != len)
            {
                LogManager.LogError("error: unzip != len");
            }
            MemoryStream decomp_ms = new MemoryStream(DecompBuffs);
            BinaryReader decomp_reader = new BinaryReader(decomp_ms);

            int file_path_count = decomp_reader.ReadInt32();
            int file_count = decomp_reader.ReadInt32();
            byte[] file_path_byte = decomp_reader.ReadBytes(file_path_count);
            byte[] md5_code_byte = decomp_reader.ReadBytes(file_count * md5_data_size);
            byte[] version_byte = decomp_reader.ReadBytes(file_count * version_data_size);
            byte[] len_byte = decomp_reader.ReadBytes(file_count * len_data_size);

            MemoryStream md5_code_mem = new MemoryStream(md5_code_byte);
            BinaryReader md5_code_all = new BinaryReader(md5_code_mem);
            MemoryStream version_mem = new MemoryStream(version_byte);
            BinaryReader version_all = new BinaryReader(version_mem);
            MemoryStream len_mem = new MemoryStream(len_byte);
            BinaryReader len_all = new BinaryReader(len_mem);

            int file_path_begin_pos = 0;
            for (int index = 0; index < file_count; index++)
            {
                string file_path = "";
                byte[] md5 = md5_code_all.ReadBytes(4);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < md5.Length; i++)
                {
                    sb.Append(md5[i].ToString("X2"));
                }

                int version = version_all.ReadInt32();
                int length = len_all.ReadInt32();
                int end_pos = 0;
                Sqlite3tool.ParseFilePath(ref file_path_byte, file_path_begin_pos, file_path_count, out end_pos, out file_path);
                file_path_begin_pos = end_pos;

                DbFileInfo info = new DbFileInfo();
                info.file_name = file_path;
                info.hash_info = DataEncrypt.GetPathHashInfo(file_path);
                info.version = version;
                info.data_len = length;
                info.unzip_len = length;
                info.file_md5 = sb.ToString();
                map.Add(info.file_name, info);
            }
            fs.Close();
            fs.Dispose();
            result = true;
        }
#if  UNITY_EDITOR
        //ExportCyMzq(file_name);
#endif
        return result;
    }
    private static void ExportCyMzq(string file_name)
    {
        string _name = Path.GetFileNameWithoutExtension(file_name);
        string _dir = Path.GetDirectoryName(file_name);
        string _file = string.Format("{0}/{1}.txt", _dir, _name);
        if (File.Exists(_file))
        {
            return;
        }
        StringBuilder log_sb = new StringBuilder();

        FileStream fs = new FileStream(file_name, FileMode.Open);
        int nLength = (int)fs.Length;
        BinaryReader tmp_reader = new BinaryReader(fs);
        int unzip = tmp_reader.ReadInt32();
        log_sb.Append("unzip = " + unzip + "\n");
        if (unzip != 0)
        {
            int md5_data_size = 4;
            int version_data_size = 4;
            int len_data_size = 4;

            byte[] buffs = tmp_reader.ReadBytes(nLength - 4);
            int len = 0;
            byte[] DecompBuffs = Sqlite3tool.DecompNetBuffer(buffs, nLength, unzip, out len);
            if (unzip != len)
            {
                LogManager.LogError("error: unzip != len");
            }
            MemoryStream decomp_ms = new MemoryStream(DecompBuffs);
            BinaryReader decomp_reader = new BinaryReader(decomp_ms);

            int file_path_count = decomp_reader.ReadInt32();
            int file_count = decomp_reader.ReadInt32();
            byte[] file_path_byte = decomp_reader.ReadBytes(file_path_count);
            byte[] md5_code_byte = decomp_reader.ReadBytes(file_count * md5_data_size);
            byte[] version_byte = decomp_reader.ReadBytes(file_count * version_data_size);
            byte[] len_byte = decomp_reader.ReadBytes(file_count * len_data_size);

            MemoryStream md5_code_mem = new MemoryStream(md5_code_byte);
            BinaryReader md5_code_all = new BinaryReader(md5_code_mem);
            MemoryStream version_mem = new MemoryStream(version_byte);
            BinaryReader version_all = new BinaryReader(version_mem);
            MemoryStream len_mem = new MemoryStream(len_byte);
            BinaryReader len_all = new BinaryReader(len_mem);

            int file_path_begin_pos = 0;
            for (int index = 0; index < file_count; index++)
            {
                string file_path = "";
                byte[] md5 = md5_code_all.ReadBytes(4);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < md5.Length; i++)
                {
                    sb.Append(md5[i].ToString("X2"));
                }

                int version = version_all.ReadInt32();
                int length = len_all.ReadInt32();
                int end_pos = 0;
                Sqlite3tool.ParseFilePath(ref file_path_byte, file_path_begin_pos, file_path_count, out end_pos, out file_path);
                file_path_begin_pos = end_pos;

                PathHashInfo _hash = DataEncrypt.GetPathHashInfo(file_path);
                log_sb.Append("hash0: ");
                log_sb.Append(_hash.hash0);
                log_sb.Append(", ");
                log_sb.Append("hash1: ");
                log_sb.Append(_hash.hash1);
                log_sb.Append(", ");
                log_sb.Append("hash2: ");
                log_sb.Append(_hash.hash1);
                log_sb.Append(", ");
                log_sb.Append("version: ");
                log_sb.Append(version);
                log_sb.Append(", ");
                log_sb.Append("file_name: ");
                log_sb.Append(Util.GetAssetsBundlePathFromBase64(file_path));
                log_sb.Append(", ");
                log_sb.Append("file_mds: ");
                log_sb.Append(sb.ToString());
                log_sb.Append(", ");
                log_sb.Append("data_len: ");
                log_sb.Append(length);
                log_sb.Append(", ");
                log_sb.Append("unzip_len: ");
                log_sb.Append(length);
                log_sb.Append("\n");
            }
            fs.Close();
            fs.Dispose();
        }

        System.IO.File.WriteAllText(_file, log_sb.ToString());
        log_sb.Clear();
    }
    private static void ParseFilePath(ref byte[] buf, int begin_pos, int max_len, out int end_pos, out string value)
    {
        bool stop = false;
        int m_cur_pos = begin_pos;
        end_pos = begin_pos;
        value = "";
        while (!stop && m_cur_pos < max_len)
        {
            if (buf[m_cur_pos] == 0)
            {
                value = Encoding.Default.GetString(buf, begin_pos, m_cur_pos - begin_pos);
                stop = true;
            }
            m_cur_pos++;
            end_pos = m_cur_pos;
        }
    }
	public static byte[] DecompNetBuffer(byte[] data, int data_len, int unzip_len, out int out_len)
	{
		MemoryStream m_inflate_stream = new MemoryStream(unzip_len);
		byte[] m_inflate_buffer = new byte[unzip_len];
		m_inflate_stream.SetLength(0L);
		Sqlite3tool.m_zlib_codec.InputBuffer = data;
		Sqlite3tool.m_zlib_codec.NextIn = 0;
		Sqlite3tool.m_zlib_codec.AvailableBytesIn = data_len;
		Sqlite3tool.m_zlib_codec.OutputBuffer = m_inflate_buffer;
		int rc = Sqlite3tool.m_zlib_codec.InitializeInflate();
		for (;;)
		{
			Sqlite3tool.m_zlib_codec.NextOut = 0;
			Sqlite3tool.m_zlib_codec.AvailableBytesOut = m_inflate_buffer.Length;
			rc = Sqlite3tool.m_zlib_codec.Inflate(FlushType.None);
			bool flag = rc != 0 && rc != 1;
			if (flag)
			{
				break;
			}
			m_inflate_stream.Write(m_inflate_buffer, 0, m_inflate_buffer.Length - Sqlite3tool.m_zlib_codec.AvailableBytesOut);
			bool flag2 = rc == 1;
			if (flag2)
			{
				goto Block_3;
			}
			if (Sqlite3tool.m_zlib_codec.AvailableBytesIn <= 0)
			{
				goto IL_D9;
			}
		}
		LogManager.LogError(string.Format("zlib inflate error, code is {0}", rc));
		Block_3:
		IL_D9:
		Sqlite3tool.m_zlib_codec.EndInflate();
		out_len = (int)Sqlite3tool.m_zlib_codec.TotalBytesOut;
		return m_inflate_stream.GetBuffer();
	}
    public bool ReplaceFileInfoListToDb(List<DbFileInfo> file_info)
    {
        bool flag = this.m_sqlite_db == null;
        bool result2;
        if (flag)
        {
            result2 = false;
        }
        else
        {
            StringBuilder sqlstring = new StringBuilder();
            sqlstring.Append("replace into file_info( hash0, hash1, hash2, data_len, version, file_name, file_md5, data_type, zip_flag, unzip_len, crypto_flag, ctl_flag) values");
            for (int i = 0; i < file_info.Count; i++)
            {
                sqlstring.AppendFormat("('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}')", new object[]
                {
                    file_info[i].hash_info.hash0.ToString(),
                    file_info[i].hash_info.hash1.ToString(),
                    file_info[i].hash_info.hash2.ToString(),
                    file_info[i].data_len.ToString(),
                    file_info[i].version.ToString(),
                    file_info[i].file_name.ToString(),
                    file_info[i].file_md5.ToString(),
                    file_info[i].data_type.ToString(),
                    file_info[i].zip_flag.ToString(),
                    file_info[i].unzip_len.ToString(),
                    file_info[i].crypto_flag.ToString(),
                    file_info[i].ctl_flag.ToString()
                });
                bool flag2 = i < file_info.Count - 1;
                if (flag2)
                {
                    sqlstring.Append(",");
                }
            }
            int result = this.m_sqlite_db.ExcuteQueryNoResult(sqlstring.ToString());
            bool flag3 = result == 0;
            if (flag3)
            {
                result2 = true;
            }
            else
            {
                LogManager.Log(string.Format("excute sql:{0}, result:{1}", sqlstring.ToString(), result));
                result2 = false;
            }
        }
        return result2;
    }
}
