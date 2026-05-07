using System;
using System.Collections.Generic;
using UnityEngine;

public class BundleInfo
{
    public string bundleName;
    public AssetBundle bundle;
    public int refCount;
    public int instanceCount;
    public LoadState state;
    public bool isPermanent;
    public HashSet<string> loadedAssets;
    public float loadTimestamp;
    public float loadDuration;
    public long estimatedSize;

    public BundleInfo(string name)
    {
        bundleName = name;
        bundle = null;
        refCount = 0;
        instanceCount = 0;
        state = LoadState.Unloaded;
        isPermanent = false;
        loadedAssets = new HashSet<string>();
        loadTimestamp = 0;
        loadDuration = 0;
        estimatedSize = 0;
    }

    public bool CanUnload => refCount == 0 && instanceCount == 0;
}
