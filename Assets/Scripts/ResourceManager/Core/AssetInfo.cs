using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AssetInfo
{
    public string assetName;
    public string bundleName;
    public List<string> dependencies;
    public string assetPath;
    public string resourcesPath;
    public string assetType;
    public string fallbackAssetName;
    public List<string> referencedBundles;
    public List<string> subAssets;
    public uint crc;
}
