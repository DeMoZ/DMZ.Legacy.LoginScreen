using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DMZ.Legacy.BuildConfig
{
    public class OnBuildPreprocesses : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var buildConfig = AssetDatabase.LoadAssetAtPath<BuildConfig>(BuildDataConstants.ConfigFilePath);
            if (buildConfig == null)
            {
                Debug.LogError("BuildConfig asset not found!");
            }
            
            // PlayerSettings.bundleVersion = buildConfig.BandleVersion;
            AssetDatabase.Refresh();
            
            Debug.Log($"Set build date to {buildConfig.BuildDate}");
            Debug.Log($"Set build version to {buildConfig.BandleVersion}");
        }
    }
}