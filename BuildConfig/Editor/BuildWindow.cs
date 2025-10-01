using System;
using UnityEditor;
using UnityEngine;

namespace DMZ.Legacy.BuildConfig
{
    public class BuildWindow : EditorWindow
    {
        private BuildConfig buildConfig;

        private void OnEnable()
        {
            buildConfig = AssetDatabase.LoadAssetAtPath<BuildConfig>(BuildDataConstants.OverrideConfigFilePath);
            
            if (buildConfig == null)
            {
                Debug.LogWarning($"Build Config not found at path: {BuildDataConstants.OverrideConfigFilePath}");
                buildConfig = AssetDatabase.LoadAssetAtPath<BuildConfig>(BuildDataConstants.ConfigFilePath);
            }
        }

        [MenuItem("DMZ/Build Data Window")]
        public static void ShowWindow()
        {
            GetWindow<BuildWindow>("Build Data Widow");
        }

        [MenuItem("DMZ/Build WebGL")]
        public static void BuildWebGL()
        {
            string path = "Builds/WebGL/Chang";
            BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path, BuildTarget.WebGL, BuildOptions.Development);

            // Add hashes to files in Builds/WebGL/Chang
            foreach (var file in System.IO.Directory.GetFiles(path))
            {
                if (file.Contains("index.html"))
                {
                    continue;
                }

                string hash = DateTime.Now.Ticks.ToString();
                string newFileName = $"{file}_{hash}";
                System.IO.File.Move(file, newFileName);
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Data Set To Build Automatically", EditorStyles.boldLabel);
            var buildDateTextField = EditorGUILayout.TextField("Date", buildConfig.BuildDate);
            var buildVersionTextField = EditorGUILayout.TextField("Build", buildConfig.BandleVersion);
            var oldBuildVersion = buildConfig.BandleVersion;

            if (GUILayout.Button("Update build version"))
            {
                //buildConfig.BandleVersion = GetBuildVersion(buildConfig.BandleVersion);
                buildConfig.BandleVersion = GetBuildVersion(Application.version);
                buildConfig.BuildDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Debug.Log($"Updated Build Version from {oldBuildVersion} to {buildConfig.BandleVersion}");
            }

            if (GUILayout.Button("Save Config"))
            {
                PlayerSettings.bundleVersion = buildConfig.BandleVersion;
                EditorUtility.SetDirty(buildConfig);
                AssetDatabase.SaveAssets();
                Debug.Log($"Confivg Saved");
            }
        }

        private static string GetBuildVersion(string version)
        {
            var buildVer = version.Split(".");
            var lastElement = string.Empty;
            try
            {
                lastElement = (int.Parse(buildVer[^1]) + 1).ToString();
            }
            catch (Exception)
            {
                lastElement = buildVer[^1] + ".0";
            }
            finally
            {
                buildVer[^1] = lastElement.ToString();
            }

            return string.Join(".", buildVer);
        }
    }
}