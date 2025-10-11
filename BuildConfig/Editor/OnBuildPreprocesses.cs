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
            var buildConfig = AssetDatabase.LoadAssetAtPath<BuildConfig>(BuildDataConstants.FilePath);

            if (buildConfig == null)
            {
                Debug.LogWarning($"Build Config not found at path: {BuildDataConstants.FilePath}");
                buildConfig = ConfigUtils.CreateAndSave<BuildConfig>(BuildDataConstants.FilePath); 
            }
            
            Debug.Log($"Set build date to {buildConfig.BuildDate}");
            Debug.Log($"Set build version to {buildConfig.BandleVersion}");
        }
    }
}