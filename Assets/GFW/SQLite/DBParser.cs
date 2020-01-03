using System;
using System.IO;

namespace GFW
{
    public class DBParser
    {
        private SQLiteDB m_sqlite_db = null;
        public bool InitDBFile(string path)
        {
            bool result;
            try
            {
                this.m_sqlite_db = new SQLiteDB();
                this.m_sqlite_db.Open(path);
                result = true;
            }
            catch (Exception e)
            {
                LogManager.Log(string.Format("Create sqlitedb from file failed!, error is {0}", e.Message));
                result = false;
            }
            return result;
        }

        public void DeleteMe()
        {
            this.m_sqlite_db.Close();
            this.m_sqlite_db = null;
        }

        public bool OpenDbFromMemory(byte[] bytes)
        {
            bool result;
            try
            {
                MemoryStream memStream = new MemoryStream();
                memStream.Write(bytes, 0, bytes.Length);
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

        public string GetConfigItem(int type_id)
        {
            bool flag = this.m_sqlite_db != null;
            if (flag)
            {
                string sql = string.Format("select val from lua_config where id = '{0}'", type_id);
                SQLiteQuery qr = new SQLiteQuery(this.m_sqlite_db, sql);
                if (qr.Step())
                {
                    return qr.GetString("val");
                }
            }
            return null;
        }

        public string GetConfigItem(string type_id)
        {
            bool flag = this.m_sqlite_db != null;
            if (flag)
            {
                LogManager.Log("type_id=" + type_id);
                string sql = string.Format("select val from lua_config where id = '{0}'", type_id);
                SQLiteQuery qr = new SQLiteQuery(this.m_sqlite_db, sql);
                if (qr.Step())
                {
                    return qr.GetString("val");
                }
            }
            return null;
        }
    }
}
