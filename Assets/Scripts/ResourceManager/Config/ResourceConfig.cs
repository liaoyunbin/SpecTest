using UnityEngine;

[CreateAssetMenu(fileName = "ResourceConfig", menuName = "ResourceManager/ResourceConfig")]
public class ResourceConfig : ScriptableObject
{
    public LoadMode loadMode = LoadMode.Editor;
    public string bundleRootPath = "AssetBundles";
    public string baseUrl = "http://localhost/assetbundles";
    public int maxConcurrentLoads = 5;
    public bool enableHotUpdate = false;
}
