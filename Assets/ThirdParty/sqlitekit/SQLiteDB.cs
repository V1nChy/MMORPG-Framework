using System;
using System.IO;
using Community.CsharpSqlite;

public class SQLiteDB
{
    private Sqlite3.sqlite3 db;

    public SQLiteDB()
    {
        db = null;
    }

    public void Open(string filename)
    {
        if (db != null)
        {
            throw new Exception("Error database already open!");
        }

        if (Sqlite3.sqlite3_open(filename, out db) != Sqlite3.SQLITE_OK)
        {
            db = null;
            throw new IOException("Error with opening database " + filename + " !");
        }
    }

    public void OpenInMemory()
    {
        if (db != null)
        {
            throw new Exception("Error database already open!");
        }

        if (Sqlite3.sqlite3_open(":memory:", out db) != Sqlite3.SQLITE_OK)
        {
            db = null;
            throw new IOException("Error with opening database :memory:!");
        }
    }

    public void OpenStream(string name, Stream io)
    {
        if (db != null)
        {
            throw new Exception("Error database already open!");
        }

        Sqlite3.sqlite3_stream stream = Sqlite3.sqlite3_stream_create(name, io);

        if (Sqlite3.sqlite3_stream_register(stream) != Sqlite3.SQLITE_OK)
        {
            throw new IOException("Error with opening database with stream " + name + "!");
        }

        if (Sqlite3.sqlite3_open_v2(name, out db, Sqlite3.SQLITE_OPEN_READWRITE, "stream") != Sqlite3.SQLITE_OK)
        {
            db = null;
            throw new IOException("Error with opening database with stream " + name + "!");
        }
    }

    public void Key(string hexkey)
    {
        Sqlite3.sqlite3_key(db, hexkey, hexkey.Length);
    }

    public void Rekey(string hexkey)
    {
        Sqlite3.sqlite3_rekey(db, hexkey, hexkey.Length);
    }

    public Sqlite3.sqlite3 Connection()
    {
        return db;
    }

    public long LastInsertRowId()
    {
        if (db == null)
        {
            throw new Exception("Error database not ready!");
        }

        return Sqlite3.sqlite3_last_insert_rowid(db);
    }

    public void Close()
    {
        if (db != null)
        {
            Sqlite3.sqlite3_close(db);
            db = null;
        }
    }

    /// <summary>
    /// 执行sql语句
    /// </summary>
    public int ExcuteQueryNoResult(string sql)
    {
        int result;
        if (db != null)
        {
            result = Sqlite3.exec(db, sql, 1, 1, 0);
        }
        else
        {
            result = 1;
        }
        return result;
    }
}
