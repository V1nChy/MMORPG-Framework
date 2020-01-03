using System;
using System.IO;
using System.Text;

namespace CodeX
{
	public class CompressHelper
	{
		public static void Compress(string root_path, string[] sourceFileList, string saveFullPath, long zip_file_size)
		{
			MemoryStream ms = null;
			uint file_index = 0u;
			uint src_file_count = 0u;
			foreach (string filePath in sourceFileList)
			{
				if (ms == null)
				{
					ms = new MemoryStream();
				}
				if (File.Exists(filePath))
				{
					string fileName = filePath.Replace(root_path, "");
					byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);
					byte[] sizeBytes = BitConverter.GetBytes(fileNameBytes.Length);
					ms.Write(sizeBytes, 0, sizeBytes.Length);
					ms.Write(fileNameBytes, 0, fileNameBytes.Length);
					byte[] fileContentBytes = File.ReadAllBytes(filePath);
					ms.Write(BitConverter.GetBytes(fileContentBytes.Length), 0, 4);
					ms.Write(fileContentBytes, 0, fileContentBytes.Length);
				}
				src_file_count += 1u;
				if (ms.Length >= zip_file_size || (ulong)src_file_count == (ulong)((long)sourceFileList.Length))
				{
					ms.Flush();
					ms.Position = 0L;
					using (FileStream zipFileStream = File.Create(saveFullPath + file_index + ".zip"))
					{
						ms.Position = 0L;
						ms.WriteTo(zipFileStream);
					}
					ms.Close();
					ms = null;
					file_index += 1u;
				}
			}
		}

		public static void DeCompressFromByte(string targetPath, byte[] content)
		{
			using (MemoryStream ms = new MemoryStream(content))
			{
				CompressHelper.DeCompress(targetPath, ms);
			}
		}

		public static void DeCompressFromFile(string zipPath, string targetPath)
		{
			if (File.Exists(zipPath))
			{
				using (FileStream fStream = File.Open(zipPath, FileMode.Open))
				{
					using (MemoryStream ms = new MemoryStream())
					{
						byte[] array = new byte[4096];
						int count;
						while ((count = fStream.Read(array, 0, array.Length)) != 0)
						{
							ms.Write(array, 0, count);
						}
						CompressHelper.DeCompress(targetPath, ms);
					}
				}
			}
		}

        public static void DeCompress(string targetPath, MemoryStream ms)
        {
            byte[] fileSize = new byte[4];
            ms.Position = 0L;
            while (ms.Position != ms.Length)
            {
                ms.Read(fileSize, 0, fileSize.Length);
                int fileNameLength = BitConverter.ToInt32(fileSize, 0);
                byte[] fileNameBytes = new byte[fileNameLength];
                ms.Read(fileNameBytes, 0, fileNameBytes.Length);
                string fileName = Encoding.UTF8.GetString(fileNameBytes);
                string fileFulleName = targetPath + fileName;
                if (File.Exists(fileFulleName))
                {
                    File.Delete(fileFulleName);
                }
                string path = Path.GetDirectoryName(fileFulleName);
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                ms.Read(fileSize, 0, 4);
                int fileContentLength = BitConverter.ToInt32(fileSize, 0);
                byte[] fileContentBytes = new byte[fileContentLength];
                ms.Read(fileContentBytes, 0, fileContentBytes.Length);
                using (FileStream childFileStream = File.Create(fileFulleName))
                {
                    childFileStream.Write(fileContentBytes, 0, fileContentBytes.Length);
                }
            }
        }
    }
}
