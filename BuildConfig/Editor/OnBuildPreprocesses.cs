using System;
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
            var buildConfig = LoadOrCreate();
            buildConfig.BundleVersion = PlayerSettings.bundleVersion;
            buildConfig.BuildDate = DateTime.Now.ToString(BuildDataConstants.DateFormat);
            EditorUtility.SetDirty(buildConfig);
            AssetDatabase.SaveAssetIfDirty(buildConfig);

            Debug.Log($"Set build version to {buildConfig.BundleVersion}, build date to {buildConfig.BuildDate}");
        }

        public static BuildConfig LoadOrCreate()
        {
            var buildConfig = AssetDatabase.LoadAssetAtPath<BuildConfig>(BuildDataConstants.FilePath);
            return buildConfig != null
                ? buildConfig
                : ConfigUtils.CreateAndSave<BuildConfig>(BuildDataConstants.FilePath);
        }
    }
}
