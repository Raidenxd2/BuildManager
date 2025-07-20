using UnityEngine;

public class BuildManagerSettings : ScriptableObject
{
    public int BuildCount;
    public string Branch;
    public string VersionPrefix;
    public string ExeName;
    public bool DevBuild = false;
    public bool BuildAddressables = true;
    public bool StripUnneededFilesFromAssetBundleBuild = true;
    public bool BuildAssetBundles = true;
    public bool RemoveBurstDebugInformation = true;
    public bool IncrementBuildNumber = true;
    public CompressionType CompressionType;
}

public enum CompressionType
{
    Default,
    LZ4,
    LZ4HC
}