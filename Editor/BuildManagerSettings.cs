using UnityEditor;
using UnityEngine;

public class BuildManagerSettings : ScriptableObject
{
    public int BuildCount = 0;
    public string Branch = "BranchName";
    public string VersionPrefix = "";
    public string ExeName = "Game";
    public bool BuildAddressables = true;
    public bool RemoveManifestFilesFromAssetBundleBuild = false;
    public bool BuildAssetBundles = true;
    public bool CopyPDBFiles = true;
    public bool RemoveBurstDebugInformation = true;
    public bool IncrementBuildNumber = true;
    public BuildOptions PlayerBuildOptions = BuildOptions.ShowBuiltPlayer;
    public BuildAssetBundleOptions AssetBundleBuildOptions = BuildAssetBundleOptions.AssetBundleStripUnityVersion;
    public CompressionType CompressionType;
}

public enum CompressionType
{
    Default,
    LZ4,
    LZ4HC
}