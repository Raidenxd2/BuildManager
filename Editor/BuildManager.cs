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

        bms.DevBuild = GUILayout.Toggle(bms.DevBuild, "Development Build");
#if BUILDMANAGER_ADDRESSABLES
        bms.BuildAssetBundles = GUILayout.Toggle(bms.BuildAssetBundles, "Build AssetBundles");
#endif
        bms.StripUnneededFilesFromAssetBundleBuild = GUILayout.Toggle(bms.StripUnneededFilesFromAssetBundleBuild, "Remove unneeded files from AssetBundle build");
        bms.BuildAddressables = GUILayout.Toggle(bms.BuildAddressables, "Build Addressables");
        bms.RemoveBurstDebugInformation = GUILayout.Toggle(bms.RemoveBurstDebugInformation, "Remove BurstDebugInformation");
        bms.IncrementBuildNumber = GUILayout.Toggle(bms.IncrementBuildNumber, "Increment Build Number");
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
            Build(BuildTarget.StandaloneWindows64);
        }

        if (GUILayout.Button("Build Windows x86"))
        {
            Build(BuildTarget.StandaloneWindows);
        }

        if (GUILayout.Button("Build Windows ARM64"))
        {
            Build(BuildTarget.StandaloneWindows64, true);
        }

        if (GUILayout.Button("Build macOS"))
        {
            Build(BuildTarget.StandaloneOSX);
        }

        if (GUILayout.Button("Build Linux"))
        {
            Build(BuildTarget.StandaloneLinux64);
        }

        if (GUILayout.Button("Build Android"))
        {
            Build(BuildTarget.Android);
        }
    }

    private void Build(BuildTarget bt, bool winArm64 = false)
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
            BuildAssetBundles(bt);
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

        string BuildPath = Application.dataPath + "/../Builds/" + bms.BuildCount;

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

        BuildOptions bo = BuildOptions.None;

        if (bms.DevBuild)
        {
            bo = BuildOptions.ShowBuiltPlayer | BuildOptions.Development;
        }
        else if (bt == BuildTarget.Android)
        {
            bo = BuildOptions.ShowBuiltPlayer;
        }
        else
        {
            bo = BuildOptions.ShowBuiltPlayer | BuildOptions.CompressWithLz4;
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

        // Analytics-related file deletion
        if (bt == BuildTarget.StandaloneWindows || bt == BuildTarget.StandaloneWindows64 || bt == BuildTarget.StandaloneLinux64)
        {
            DeleteFileIfExists(BuildPath + "/" + bms.ExeName + "_Data/Managed/UnityEngine.UnityAnalyticsCommonModule.dll");
            DeleteFileIfExists(BuildPath + "/" + bms.ExeName + "_Data/Managed/UnityEngine.UnityAnalyticsCommonModule.pdb");
            DeleteFileIfExists(BuildPath + "/" + bms.ExeName + "_Data/Managed/UnityEngine.UnityAnalyticsModule.dll");
            DeleteFileIfExists(BuildPath + "/" + bms.ExeName + "_Data/Managed/UnityEngine.UnityAnalyticsModule.pdb");
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
    }

    [MenuItem("Tools/Build AssetBundles")]
    private static void BuildAssetBundlesMenuItem()
    {
        if (bms == null)
        {
            LoadBms();
        }

        BuildAssetBundles(EditorUserBuildSettings.activeBuildTarget);

        AssetDatabase.Refresh();
    }

    private static void BuildAssetBundles(BuildTarget bt)
    {
        string bundleDirectory = "Assets/AssetBundleBuild";
        string bundleDirectoryName = "AssetBundleBuild";

        if (!Directory.Exists(bundleDirectory))
        {
            Directory.CreateDirectory(bundleDirectory);
        }

        BuildPipeline.BuildAssetBundles(bundleDirectory, BuildAssetBundleOptions.AssetBundleStripUnityVersion, bt);

        // Removes meta, manifest, and other files from the built AssetBundles
        if (bms.StripUnneededFilesFromAssetBundleBuild)
        {
            File.Delete(bundleDirectory + "/" + bundleDirectoryName);
            File.Delete(bundleDirectory + "/" + bundleDirectoryName + ".meta");
            File.Delete(bundleDirectory + "/" + bundleDirectoryName + ".manifest");

            DirectoryInfo buildFiles = new(bundleDirectory);
            if (buildFiles.Exists)
            {
                foreach (var file in buildFiles.EnumerateFiles())
                {
                    if (file.Extension == "manifest")
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
                Debug.LogError("(BuildManager) Assets/AssetBundlebuild doesn't exist? (Failed)");
                return;
            }
        }

        if (!Directory.Exists("Assets/StreamingAssets/Bundles"))
        {
            Directory.CreateDirectory("Assets/StreamingAssets/Bundles");
        }

        DirectoryInfo bundlesFolder = new("Assets/StreamingAssets/Bundles");
        if (bundlesFolder.Exists)
        {
            foreach (var file in bundlesFolder.EnumerateFiles())
            {
                Debug.Log("(BuildManager) Deleting built AssetBundle from StreamingAssets/Bundles " + file.Name);
                File.Delete(file.FullName);
            }
        }
        else
        {
            Debug.LogError("(BuildManager) Assets/StreamingAssets/Bundles doesn't exist? (Failed)");
            return;
        }

        DirectoryInfo buildFiles2 = new(bundleDirectory);
        if (buildFiles2.Exists)
        {
            foreach (var file in buildFiles2.EnumerateFiles())
            {
                if (file.Extension == "meta")
                {
                    continue;
                }

                Debug.Log("(BuildManager) Copying AssetBundle " + file.Name + " (" + file.Length + ") to StreamingAssets");
                File.Copy(file.FullName, "Assets/StreamingAssets/Bundles/" + file.Name);
            }
        }
        else
        {
            Debug.LogError("(BuildManager) Assets/AssetBundleBuild doesn't exist? (Failed)");
            return;
        }
    }

    private void DeleteFileIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void SaveSettings()
    {
        EditorUtility.SetDirty(bms);
        AssetDatabase.Refresh();
    }

    private static void LoadBms()
    {
        bms = AssetDatabase.LoadAssetAtPath<BuildManagerSettings>("Assets/BuildManagerSettings.asset");
    }
}