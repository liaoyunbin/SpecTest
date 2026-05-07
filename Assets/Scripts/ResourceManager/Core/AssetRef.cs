using System;
using System.Collections.Generic;
using UnityEngine;

public class AssetRef
{
    public string assetName;
    public UnityEngine.Object asset;
    public int refCount;
    public string bundleName;
    public LoadState state;
    public List<Action<UnityEngine.Object>> pendingCallbacks;

    public AssetRef(string name)
    {
        assetName = name;
        asset = null;
        refCount = 0;
        bundleName = null;
        state = LoadState.Unloaded;
        pendingCallbacks = new List<Action<UnityEngine.Object>>();
    }
}
