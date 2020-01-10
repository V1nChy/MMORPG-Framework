using System;
using System.Collections.Generic;
using GFW;
using LuaFramework;
using UnityEngine;
using Object = UnityEngine.Object;

public class RealAssetBundle
{
    public AssetBundle assetBundle;

    private Dictionary<string, Object> m_CacheLoadedAssets = new Dictionary<string, Object>();

    public Object LoadAssetAsync(string assetName, Type type)
	{
        if (this.assetBundle)
		{
            Object request = null;
			if (!this.m_CacheLoadedAssets.TryGetValue(assetName, out request))
			{
				request = this.assetBundle.LoadAsset(assetName, type);
				this.m_CacheLoadedAssets.Add(assetName, request);
			}
			return request;
		}
		else
		{
			LogMgr.LogWarning(assetName + "has not loaded");
            return null;
		}
	}

	public bool CheckHasLoadedAsset(string assetName)
	{
		return this.m_CacheLoadedAssets.ContainsKey(assetName);
	}

	public void Unload(bool unloadAllLoadedObjects)
	{
		this.m_CacheLoadedAssets = null;
        if (this.assetBundle)
		{
			this.assetBundle.Unload(unloadAllLoadedObjects);
		}
	}
}
