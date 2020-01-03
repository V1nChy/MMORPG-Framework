using System;

public class DataEncrypt
{
    public static bool m_is_init = false;

    public static ulong[] mCryptTable = new ulong[1280];

	public static void InitCryptTable()
	{
        if (!DataEncrypt.m_is_init)
		{
			uint seed = 1048577;
			for (uint index = 0; index < 256; index += 1)
			{
				uint index2 = index;
				int i = 0;
				while (i < 5)
				{
					seed = (seed * 125 + 3) % 2796203;
					uint temp = (seed & 65535) << 16;
					seed = (seed * 125 + 3) % 2796203;
					uint temp2 = seed & 65535;
					DataEncrypt.mCryptTable[(int)index2] = (ulong)(temp | temp2);
					i++;
					index2 += 256;
				}
			}
			DataEncrypt.m_is_init = true;
		}
	}

	public static ulong HashString(ulong type, string strIn)
	{
        if (!DataEncrypt.m_is_init)
		{
			DataEncrypt.InitCryptTable();
		}
        ulong seed = 0x7FED7FED;
        ulong seed2 = 0xFFFFFFFFEEEEEEEE;
		foreach (char value in strIn)
		{
			ulong ch = (ulong)value;
			seed = (DataEncrypt.mCryptTable[(int)(checked((IntPtr)(unchecked((type << 8) + ch))))] ^ seed + seed2);
			seed2 = ch + seed + seed2 + (seed2 << 5) + 3UL;
		}
		return seed;
	}

	public static PathHashInfo GetPathHashInfo(string regular_path)
	{
		return new PathHashInfo
		{
			hash0 = (int)DataEncrypt.HashString(0, regular_path),
			hash1 = (int)DataEncrypt.HashString(1, regular_path),
			hash2 = (int)DataEncrypt.HashString(2, regular_path)
		};
	}

	public static void EncryptHashTableData(IntPtr pData, uint dataLen, uint seed)
	{
	}

	public static void DecryptHashTableData(IntPtr pData, uint dataLen, uint seed)
	{
	}
}
