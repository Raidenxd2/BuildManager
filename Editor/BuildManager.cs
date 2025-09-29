using System.Collections.Generic;
using System.IO;
using UnityEditor;
#if BUILDMANAGER_ADDRESSABLES
using UnityEditor.AddressableAssets.Settings;
#endif
using UnityEngine;

public class BuildManager : EditorWindow
{
    private static BuildManagerSettings bms;

    [MenuItem("Window/Build Manager")]
    public static void ShowWindow()
    {
        // If Assets/BuildManagerSettings.asset doesn't exist, create it
        if (!AssetDatabase.AssetPathExists("Assets/BuildManagerSettings.asset"))
        {
            BuildManagerSettings asset = CreateInstance<BuildManagerSettings>();

            AssetDatabase.CreateAsset(asset, "Assets/BuildManagerSettings.asset");
            AssetDatabase.SaveAssets();
        }

        bms = AssetDatabase.LoadAssetAtPath<BuildManagerSettings>("Assets/BuildManagerSettings.asset");

        BuildManager wnd = GetWindow<BuildManager>();
        wnd.titleContent = new("Build Manager");
    }

    void OnGUI()
    {
        if (bms == null)
        {
            LoadBms();
        }

        bms.BuildAssetBundles = GUILayout.Toggle(bms.BuildAssetBundles, "Build AssetBundles");
        bms.RemoveManifestFilesFromAssetBundleBuild = GUILayout.Toggle(bms.RemoveManifestFilesFromAssetBundleBuild, "Remove manifest files from AssetBundle build");
        bms.BuildAddressables = GUILayout.Toggle(bms.BuildAddressables, "Build Addressables");
        bms.CopyPDBFiles = GUILayout.Toggle(bms.CopyPDBFiles, "Copy PDB files (Mono Windows, Linux and macOS only)");
        bms.RemoveBurstDebugInformation = GUILayout.Toggle(bms.RemoveBurstDebugInformation, "Remove BurstDebugInformation");
        bms.IncrementBuildNumber = GUILayout.Toggle(bms.IncrementBuildNumber, "Increment Build Number");
        bms.CompressionType = (CompressionType)EditorGUILayout.EnumPopup("Compression Type", bms.CompressionType);
        bms.PlayerBuildOptions = (BuildOptions)EditorGUILayout.EnumFlagsField("Player Build Options", bms.PlayerBuildOptions);
        bms.AssetBundleBuildOptions = (BuildAssetBundleOptions)EditorGUILayout.EnumFlagsField("AssetBundle Build Options", bms.AssetBundleBuildOptions);
        bms.BuildCount = EditorGUILayout.IntField("Build Count", bms.BuildCount);
        bms.Branch = EditorGUILayout.TextField("Branch", bms.Branch);
        bms.VersionPrefix = EditorGUILayout.TextField("Version Prefix", bms.VersionPrefix);
        bms.ExeName = EditorGUILayout.TextField("Exe name", bms.ExeName);

        if (GUILayout.Button("Save settings"))
        {
            SaveSettings();
        }

        if (GUILayout.Button("Build Windows x64"))
        {
            Build(BuildTarget.StandaloneWindows64, bms);
        }

        if (GUILayout.Button("Build Windows x86"))
        {
            Build(BuildTarget.StandaloneWindows, bms);
        }

        if (GUILayout.Button("Build Windows ARM64"))
        {
            Build(BuildTarget.StandaloneWindows64, bms, true);
        }

        if (GUILayout.Button("Build macOS"))
        {
            Build(BuildTarget.StandaloneOSX, bms);
        }

        if (GUILayout.Button("Build Linux"))
        {
            Build(BuildTarget.StandaloneLinux64, bms);
        }

        if (GUILayout.Button("Build Android"))
        {
            Build(BuildTarget.Android, bms);
        }
    }

    public static void Build(BuildTarget bt, BuildManagerSettings bms, bool winArm64 = false)
    {
        if (bms.IncrementBuildNumber)
        {
            bms.BuildCount++;

            SaveSettings();
        }

        PlayerSettings.bundleVersion = bms.VersionPrefix + "_" + bms.BuildCount.ToString() + " (" + bms.Branch + ")";
        PlayerSettings.Android.bundleVersionCode = bms.BuildCount;
        PlayerSettings.macOS.buildNumber = bms.BuildCount.ToString();

        if (!Directory.Exists(Application.streamingAssetsPath))
        {
            Directory.CreateDirectory(Application.streamingAssetsPath);
        }

        AssetDatabase.Refresh();

        File.Delete(Application.streamingAssetsPath + "/build");

        int build = PlayerSettings.Android.bundleVersionCode;

        File.WriteAllText(Application.streamingAssetsPath + "/build", build.ToString());

        BuildTargetGroup btg = BuildTargetGroup.Unknown;

        switch (bt)
        {
            case BuildTarget.StandaloneWindows:
                btg = BuildTargetGroup.Standalone;
                break;
            case BuildTarget.StandaloneWindows64:
                btg = BuildTargetGroup.Standalone;
                break;
            case BuildTarget.StandaloneOSX:
                btg = BuildTargetGroup.Standalone;
                break;
            case BuildTarget.StandaloneLinux64:
                btg = BuildTargetGroup.Standalone;
                break;
            case BuildTarget.Android:
                btg = BuildTargetGroup.Android;
                break;
        }

#if UNITY_STANDALONE_WIN
        if (winArm64)
        {
            UnityEditor.WindowsStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.ARM64;
        }
        else if (bt == BuildTarget.StandaloneWindows64)
        {
            UnityEditor.WindowsStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64;
        }
        else if (bt == BuildTarget.StandaloneWindows)
        {
            UnityEditor.WindowsStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x86;
        }
#endif

#if UNITY_STANDALONE_OSX
        if (bt == BuildTarget.StandaloneOSX)
        {
            UnityEditor.OSXStandalone.UserBuildSettings.architecture = UnityEditor.Build.OSArchitecture.x64ARM64;
        }
#endif

        if (!EditorUserBuildSettings.SwitchActiveBuildTarget(btg, bt))
        {
            return;
        }

        if (bms.BuildAssetBundles)
        {
            BuildAssetBundles(bt, bms);
        }

#if BUILDMANAGER_ADDRESSABLES
        if (bms.BuildAddressables)
        {
            AddressableAssetSettings.BuildPlayerContent();
        }
#endif

        // Get all scenes in Build Settings/Build Profiles
        List<string> scenes = new();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
            }
        }

        if (bms == null)
        {
            LoadBms();
        }

        string BuildPath = Application.dataPath + "/../Builds/" + bms.BuildCount + "_" + bt;

        if (winArm64)
        {
            BuildPath += "_" + "ARM64";
        }

        if (!Directory.Exists(Application.dataPath + "/../Builds"))
        {
            Directory.CreateDirectory(Application.dataPath + "/../Builds");
        }

        if (!Directory.Exists(BuildPath))
        {
            Directory.CreateDirectory(BuildPath);
        }

        string exeName = BuildPath + "/default.exe";
        
        switch (bt)
        {
            case BuildTarget.StandaloneWindows:
                exeName = BuildPath + "/" + bms.ExeName + ".exe";
                break;
            case BuildTarget.StandaloneWindows64:
                exeName = BuildPath + "/" + bms.ExeName + ".exe";
                break;
            case BuildTarget.StandaloneOSX:
                exeName = BuildPath + "/" + bms.ExeName + ".app";
                break;
            case BuildTarget.StandaloneLinux64:
                exeName = BuildPath + "/" + bms.ExeName + ".x86_64";
                break;
            case BuildTarget.Android:
                exeName = BuildPath + "/" + bms.ExeName + ".apk";
                break;
        }

        BuildOptions bo = bms.PlayerBuildOptions;

        switch (bms.CompressionType)
        {
            case CompressionType.Default:
                break;
            case CompressionType.LZ4:
                bo |= BuildOptions.CompressWithLz4;
                break;
            case CompressionType.LZ4HC:
                bo |= BuildOptions.CompressWithLz4HC;
                break;
        }

        BuildPipeline.BuildPlayer(scenes.ToArray(), exeName, bt, bo);

        // Copy files from Assets/_Project/BuildOutput/win to the built folder for Windows
        if (bt == BuildTarget.StandaloneWindows || bt == BuildTarget.StandaloneWindows64)
        {
            if (Directory.Exists("Assets/_Project/BuildOutput/win"))
            {
                DirectoryInfo boDir = new("Assets/_Project/BuildOutput/win");
                FileInfo[] boInfo = boDir.GetFiles("*.*");

                foreach (FileInfo file in boInfo)
                {
                    if (file.Extension == ".meta")
                    {

                    }
                    else
                    {
                        File.Copy("Assets/_Project/BuildOutput/win/" + file.Name, BuildPath + "/" + Path.GetFileName(file.Name), true);
                    }
                }
            }
        }

        // Delete BurstDebugInformation if its enabled
        if (bms.RemoveBurstDebugInformation)
        {
            if (bt == BuildTarget.StandaloneWindows || bt == BuildTarget.StandaloneWindows64 || bt == BuildTarget.StandaloneLinux64)
            {
                if (Directory.Exists(BuildPath + "/" + PlayerSettings.productName + "_BurstDebugInformation_DoNotShip"))
                {
                    Directory.Delete(BuildPath + "/" + PlayerSettings.productName + "_BurstDebugInformation_DoNotShip", true);
                }
            }
        }

        // Copy files from Assets/CopyToStreamingAssets only for Windows or Linux
        if (bt == BuildTarget.StandaloneWindows || bt == BuildTarget.StandaloneWindows64 || bt == BuildTarget.StandaloneLinux64)
        {
            if (Directory.Exists("Assets/CopyToStreamingAssets"))
            {
                DirectoryInfo boDir2 = new("Assets/CopyToStreamingAssets");
                FileInfo[] boInfo2 = boDir2.GetFiles("*.*");

                foreach (FileInfo file in boInfo2)
                {
                    if (file.Extension == ".meta")
                    {

                    }
                    else
                    {
                        File.Copy("Assets/CopyToStreamingAssets/" + file.Name, BuildPath + "/" + bms.ExeName + "_Data/StreamingAssets/" + Path.GetFileName(file.Name));
                    }
                }
            }
        }

        if (bms.CopyPDBFiles)
        {
            if ((bt == BuildTarget.StandaloneWindows || bt == BuildTarget.StandaloneWindows64 || bt == BuildTarget.StandaloneLinux64 || bt == BuildTarget.StandaloneOSX) && PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone) != ScriptingImplementation.IL2CPP && PlayerSettings.GetManagedStrippingLevel(UnityEditor.Build.NamedBuildTarget.Standalone) == ManagedStrippingLevel.Disabled)
            {
                if (Directory.Exists("Library/Bee/PlayerScriptAssemblies"))
                {
                    DirectoryInfo boDir2 = new("Library/Bee/PlayerScriptAssemblies");
                    FileInfo[] boInfo2 = boDir2.GetFiles("*.pdb");

                    foreach (FileInfo file in boInfo2)
                    {
                        if (file.Extension == ".dll")
                        {

                        }
                        else
                        {
                            if (bt == BuildTarget.StandaloneOSX)
                            {
                                File.Copy("Library/Bee/PlayerScriptAssemblies/" + file.Name, BuildPath + "/" + bms.ExeName + ".app/Contents/Resources/Data/Managed/" + Path.GetFileName(file.Name));
                            }
                            else
                            {
                                File.Copy("Library/Bee/PlayerScriptAssemblies/" + file.Name, BuildPath + "/" + bms.ExeName + "_Data/Managed/" + Path.GetFileName(file.Name));
                            }
                        }
                    }
                }
            }
        }
    }

    [MenuItem("Tools/Build AssetBundles")]
    private static void BuildAssetBundlesMenuItem()
    {
        if (bms == null)
        {
            LoadBms();
        }

        BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget, bms);

        AssetDatabase.Refresh();
    }

    public static void BuildAssetBundles(BuildTarget bt, BuildManagerSettings bms)
    {
        string bundleDirectory = "Assets/StreamingAssets/Bundles";
        string bundleDirectoryName = "Bundles";

        if (!Directory.Exists(bundleDirectory))
        {
            Directory.CreateDirectory(bundleDirectory);
        }

        BuildPipeline.BuildAssetBundles(bundleDirectory, bms.AssetBundleBuildOptions, bt);

        // Removes manifest files from the built AssetBundles
        if (bms.RemoveManifestFilesFromAssetBundleBuild)
        {
            File.Delete(bundleDirectory + "/" + bundleDirectoryName);
            File.Delete(bundleDirectory + "/" + bundleDirectoryName + ".meta");
            File.Delete(bundleDirectory + "/" + bundleDirectoryName + ".manifest");
            File.Delete(bundleDirectory + "/" + bundleDirectoryName + ".manifest.meta");

            DirectoryInfo buildFiles = new(bundleDirectory);
            if (buildFiles.Exists)
            {
                foreach (var file in buildFiles.EnumerateFiles())
                {
                    if (file.Extension == ".manifest")
                    {
                        File.Delete(file.FullName);
                        if (File.Exists(file.FullName + ".meta"))
                        {
                            File.Delete(file.FullName + ".meta");
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("(BuildManager) Assets/StreamingAssets/Bundles doesn't exist? (Failed)");
                return;
            }
        }
    }

    public static void SaveSettings()
    {
        EditorUtility.SetDirty(bms);
        AssetDatabase.Refresh();
    }

    private static void LoadBms()
    {
        bms = AssetDatabase.LoadAssetAtPath<BuildManagerSettings>("Assets/BuildManagerSettings.asset");
    }
}