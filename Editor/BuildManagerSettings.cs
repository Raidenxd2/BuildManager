using UnityEditor;
using UnityEngine;

public class BuildManagerSettings : ScriptableObject
{
    public int BuildCount;
    public string Branch = "BranchName";
    public string VersionPrefix = "";
    public string ExeName = "Game";
    public bool DedicatedServerBuild = false;
    public bool BuildAddressables = true;
    public bool RemoveManifestFilesFromAssetBundleBuild;
    public bool BuildAssetBundles = true;
    public bool DeleteBundlesBeforeBuilding;
    public bool CopyPDBFiles = true;
    public bool RemoveBurstDebugInformation = true;
    public bool IncrementBuildNumber = true;
    public bool AddGitCommitHashToVersion;
    public MobileTextureSubtarget AndroidTextureSubtarget = MobileTextureSubtarget.Generic;
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