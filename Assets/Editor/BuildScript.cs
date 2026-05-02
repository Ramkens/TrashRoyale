using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TrashRoyale.EditorTools
{
    public static class BuildScript
    {
        const string PackageName = "com.kuniman.trashroyale";
        const string CompanyName = "Kuniman";
        const string ProductName = "Trash Royale";

        static readonly string[] Scenes = new[]
        {
            "Assets/Scenes/Main.unity",
            "Assets/Scenes/Battle.unity"
        };

        [MenuItem("TrashRoyale/Configure Player Settings")]
        public static void ConfigurePlayerSettings()
        {
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Build] PlayerSettings applied");
        }

        [MenuItem("TrashRoyale/Build Android APK")]
        public static void BuildAndroidMenu()
        {
            string outDir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "Builds");
            Build(outDir);
        }

        public static void BuildAndroid()
        {
            string outDir = Environment.GetEnvironmentVariable("BUILD_OUTPUT_DIR");
            if (string.IsNullOrEmpty(outDir))
            {
                outDir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? "", "Builds");
            }
            Build(outDir);
        }

        static void Build(string outDir)
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ApplyPlayerSettings();
            EnsureScenes();

            Directory.CreateDirectory(outDir);
            string apkPath = Path.Combine(outDir, "TrashRoyale.apk");

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
                locationPathName = apkPath,
            };
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Build] Result: {summary.result}, total errors: {summary.totalErrors}, total warnings: {summary.totalWarnings}, output: {summary.outputPath}");
            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
            else
            {
                EditorApplication.Exit(0);
            }
        }

        static void EnsureScenes()
        {
            var existing = EditorBuildSettings.scenes.Select(s => s.path).ToList();
            var needAdd = Scenes.Where(s => !existing.Contains(s)).ToArray();
            if (needAdd.Length > 0)
            {
                var list = EditorBuildSettings.scenes.ToList();
                foreach (var s in Scenes)
                {
                    if (!list.Any(x => x.path == s))
                    {
                        list.Add(new EditorBuildSettingsScene(s, true));
                    }
                }
                EditorBuildSettings.scenes = list.ToArray();
            }
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.applicationIdentifier = PackageName;
            PlayerSettings.bundleVersion = "0.6.0";
            PlayerSettings.Android.bundleVersionCode = 6;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.Android, ApiCompatibilityLevel.NET_Standard_2_0);
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            // Splash
            PlayerSettings.SplashScreen.show = false;

            // App icon
            ApplyIcons();

            // IL2CPP / mobile optimizations
            PlayerSettings.muteOtherAudioSources = false;
            PlayerSettings.runInBackground = false;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }

        static void ApplyIcons()
        {
            try
            {
                var iconPath = "Assets/Resources/Branding/icon_512.png";
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
                if (icon == null)
                {
                    iconPath = "Assets/Resources/Branding/avatar.png";
                    icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
                }
                if (icon != null)
                {
                    var icons = new[] { icon };
                    PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, icons);
                    PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
                    Debug.Log($"[Build] App icon set to {iconPath}");
                }
                else
                {
                    Debug.LogWarning("[Build] No icon texture found in Resources/Branding");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Build] icon apply failed: {e.Message}");
            }
        }
    }
}
