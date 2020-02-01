using System;

public class PathHashInfo
{
    public int hash0 = 0;
    public int hash1 = 0;
    public int hash2 = 0;
	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		PathHashInfo v = obj as PathHashInfo;
		return v.hash0 == this.hash0 && v.hash1 == this.hash1 && v.hash2 == this.hash2;
	}

	public static bool operator <(PathHashInfo a, PathHashInfo b)
	{
		bool flag = a.hash0 < b.hash0;
		bool result;
		if (flag)
		{
			result = true;
		}
		else
		{
			bool flag2 = a.hash0 > b.hash0;
			if (flag2)
			{
				result = false;
			}
			else
			{
				bool flag3 = a.hash1 < b.hash1;
				if (flag3)
				{
					result = true;
				}
				else
				{
					bool flag4 = a.hash1 > b.hash1;
					result = (!flag4 && a.hash2 < b.hash2);
				}
			}
		}
		return result;
	}

	public static bool operator >(PathHashInfo a, PathHashInfo b)
	{
		bool flag = a.hash0 > b.hash0;
		bool result;
		if (flag)
		{
			result = true;
		}
		else
		{
			bool flag2 = a.hash0 < b.hash0;
			if (flag2)
			{
				result = false;
			}
			else
			{
				bool flag3 = a.hash1 > b.hash1;
				if (flag3)
				{
					result = true;
				}
				else
				{
					bool flag4 = a.hash1 < b.hash1;
					result = (!flag4 && a.hash2 > b.hash2);
				}
			}
		}
		return result;
	}

	public static bool operator ==(PathHashInfo a, PathHashInfo b)
	{
		return a.hash0 == b.hash0 && a.hash1 == b.hash1 && a.hash2 == b.hash2;
	}

	public static bool operator !=(PathHashInfo a, PathHashInfo b)
	{
		return a.hash0 != b.hash0 || a.hash1 != b.hash1 || a.hash2 != b.hash2;
	}
}
