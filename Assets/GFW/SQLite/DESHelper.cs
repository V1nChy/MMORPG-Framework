using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public class DESHelper
{
    private static byte[] Keys = new byte[]
	{
		17,
		51,
		82,
		116,
		133,
		167,
		198,
		243
	};

    private static byte[] IV = new byte[]
	{
		22,
		34,
		95,
		137,
		145,
		168,
		205,
		231
	};

	public static string DESEncrypt(string context)
	{
		string result;
		try
		{
			byte[] byteArray = Encoding.UTF8.GetBytes(context);
			DESCryptoServiceProvider des = new DESCryptoServiceProvider();
			des.Key = DESHelper.Keys;
			des.IV = DESHelper.IV;
			using (MemoryStream mStream = new MemoryStream())
			{
				CryptoStream encStream = new CryptoStream(mStream, des.CreateEncryptor(), CryptoStreamMode.Write);
				encStream.Write(byteArray, 0, byteArray.Length);
				encStream.FlushFinalBlock();
				encStream.Close();
				result = Convert.ToBase64String(mStream.ToArray());
			}
		}
		catch (Exception)
		{
			result = context;
		}
		return result;
	}

	public static string DESDecrypt(string context)
	{
		string result;
		try
		{
			DESCryptoServiceProvider des = new DESCryptoServiceProvider();
			des.Key = DESHelper.Keys;
			des.IV = DESHelper.IV;
			using (MemoryStream ms = new MemoryStream())
			{
				byte[] byteArray = Convert.FromBase64String(context);
				CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
				cs.Write(byteArray, 0, byteArray.Length);
				cs.FlushFinalBlock();
				cs.Close();
				result = Encoding.UTF8.GetString(ms.ToArray());
			}
		}
		catch (Exception)
		{
			result = context;
		}
		return result;
	}

	public static string DESEncrypt(byte[] context)
	{
		string result;
		try
		{
			DESCryptoServiceProvider des = new DESCryptoServiceProvider();
			des.Key = DESHelper.Keys;
			des.IV = DESHelper.IV;
			using (MemoryStream mStream = new MemoryStream())
			{
				CryptoStream encStream = new CryptoStream(mStream, des.CreateEncryptor(), CryptoStreamMode.Write);
				encStream.Write(context, 0, context.Length);
				encStream.FlushFinalBlock();
				encStream.Close();
				result = Convert.ToBase64String(mStream.ToArray());
			}
		}
		catch (Exception)
		{
			result = Convert.ToBase64String(context);
		}
		return result;
	}

	public static string DESDecrypt(byte[] context)
	{
		string result;
		try
		{
			DESCryptoServiceProvider des = new DESCryptoServiceProvider();
			des.Key = DESHelper.Keys;
			des.IV = DESHelper.IV;
			using (MemoryStream ms = new MemoryStream())
			{
				byte[] byteArray = Convert.FromBase64String(Encoding.UTF8.GetString(context));
				CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
				cs.Write(byteArray, 0, byteArray.Length);
				cs.FlushFinalBlock();
				cs.Close();
				result = Encoding.UTF8.GetString(ms.ToArray());
			}
		}
		catch (Exception)
		{
			result = Convert.ToBase64String(context);
		}
		return result;
	}
}
