using GFW;
using System;
using System.Collections.Generic;
using System.IO;

namespace CodeX
{
	public class Pathtool
	{
		public static string CombinePath(string path1, string path2)
		{
			string[] paths = path1.Split(new char[]
			{
				'/'
			});
			string[] paths2 = path2.Split(new char[]
			{
				'/'
			});
			List<string> paths1_list = new List<string>();
			foreach (string value in paths)
			{
				paths1_list.Add(value);
			}
			for (int i = 0; i < paths2.Length; i++)
			{
				bool flag = paths2[i] == "..";
				if (flag)
				{
					paths1_list.RemoveAt(paths1_list.Count - 1);
				}
				else
				{
					bool flag2 = paths2[i] != ".";
					if (flag2)
					{
						paths1_list.Add(paths2[i]);
					}
				}
			}
			string out_path = "";
			for (int j = 0; j < paths1_list.Count; j++)
			{
				bool flag3 = j == 0;
				if (flag3)
				{
					out_path = paths1_list[0];
				}
				else
				{
					bool flag4 = out_path.EndsWith("/");
					if (flag4)
					{
						out_path += paths1_list[j];
					}
					else
					{
						out_path = out_path + "/" + paths1_list[j];
					}
				}
			}
			return out_path;
		}

		public static void CreatePath(string path)
		{
			string NewPath = path.Replace("\\", "/");
			string[] strs = NewPath.Split(new char[]
			{
				'/'
			});
			string p = "";
			for (int i = 0; i < strs.Length; i++)
			{
				p += strs[i];
				bool flag = i != strs.Length - 1;
				if (flag)
				{
					p += "/";
				}
				bool flag2 = !Path.HasExtension(p);
				if (flag2)
				{
					bool flag3 = !Directory.Exists(p);
					if (flag3)
					{
						Directory.CreateDirectory(p);
					}
				}
			}
		}

		public static bool SaveDataToFile(string path, byte[] buffer)
		{
			if (path.IndexOf(AppConst.StreamingAssets) != -1 && AppConst.UpdateMode)
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
				if (Directory.Exists(path))
				{
					Directory.Delete(path);
				}
			}
			if ((!File.Exists(path) && path.IndexOf(AppConst.StreamingAssets) == -1) || !AppConst.UpdateMode)
			{
				Pathtool.CreatePath(path);
			}
			try
			{
				FileStream fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
				fs.Write(buffer, 0, buffer.Length);
				fs.Close();
			}
			catch (Exception ex)
			{
				LogManager.LogError("Can't create local resource" + path);
				return false;
			}
			return true;
		}

		public static void DeleteToFile(string path)
		{
			bool flag = File.Exists(path);
			if (flag)
			{
				File.Delete(path);
				Pathtool.CreatePath(path);
			}
		}

		public static bool GetDataToFile(string path, out byte[] buffer)
		{
			buffer = null;
            if (File.Exists(path))
			{
				try
				{
					FileStream fs = new FileStream(path, FileMode.Open);
					int nLength = (int)fs.Length;
					byte[] buffs = new byte[nLength];
					fs.Read(buffs, 0, nLength);
					fs.Close();
					buffer = buffs;
					return true;
				}
				catch (Exception ex)
				{
					LogManager.LogError("Can't create local resource" + path);
					return false;
				}
			}
			return false;
		}
	}
}
