using System;
using UnityEditor;
using UnityEngine;

namespace DMZ.Legacy.BuildConfig
{
    public class BuildWindow : EditorWindow
    {
        [MenuItem("DMZ/Build Data Window")]
        public static void ShowWindow()
        {
            GetWindow<BuildWindow>("Build Data Window");
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
            var buildConfig = AssetDatabase.LoadAssetAtPath<BuildConfig>(BuildDataConstants.FilePath);
            EditorGUILayout.LabelField("Version", PlayerSettings.bundleVersion);
            EditorGUILayout.LabelField("Last build date", buildConfig != null ? buildConfig.BuildDate : "-");
            EditorGUILayout.HelpBox($"The build date and version are written to {BuildDataConstants.FilePath} on every build.", MessageType.Info);

            if (GUILayout.Button("Increment version"))
            {
                var oldVersion = PlayerSettings.bundleVersion;
                PlayerSettings.bundleVersion = GetBuildVersion(oldVersion);
                AssetDatabase.SaveAssets();
                Debug.Log($"Updated version from {oldVersion} to {PlayerSettings.bundleVersion}");
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