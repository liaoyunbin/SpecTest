using System.Collections.Generic;
using UnityEngine;

public class AssetInfoConfig : ScriptableObject
{
    public List<AssetInfo> assetInfos = new List<AssetInfo>();

    private Dictionary<string, AssetInfo> _cache;

    private void BuildCache()
    {
        if (_cache == null)
        {
            _cache = new Dictionary<string, AssetInfo>();
            foreach (var info in assetInfos)
            {
                if (!string.IsNullOrEmpty(info.assetName) && !_cache.ContainsKey(info.assetName))
                {
                    _cache[info.assetName] = info;
                }
            }
        }
    }

    public AssetInfo GetByName(string assetName)
    {
        BuildCache();
        _cache.TryGetValue(assetName, out var info);
        return info;
    }

    public BundleInfo GetBundleInfo(string bundleName)
    {
        return null;
    }
}
