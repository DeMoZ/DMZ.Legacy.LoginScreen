using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DMZ.Legacy.BuildConfig
{
    public class ConfigUtils
    {
        // path is "Assets/MyFolder/MyAsset.asset"
        public static T CreateAndSave<T>(string path) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"Path is not valid: {path}");
                return null;
            }

            AssetDatabase.StartAssetEditing();
            T config = null;

            try
            {
                string directory = Path.GetDirectoryName(path);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    CreateFolders(directory);
                }

                config = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(config, path);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create and save asset at path: {path}\n{e}");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            return config;
        }

        private static void CreateFolders(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string[] folders = path.Split('/');

            if (folders.Length == 0)
            {
                return;
            }

            if (!string.Equals(folders[0], "Assets"))
            {
                Debug.LogError($"Path should start with 'Assets'. Path:\n{path}");
                return;
            }

            string currentPath = folders[0];

            foreach (string folder in folders.Skip(1))
            {
                string folderPath = Path.Combine(currentPath, folder);
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    AssetDatabase.CreateFolder(currentPath, folder);
                    Debug.Log($"Folders created at:\n{path}");
                    RestartEditAssetDatabase();
                }

                currentPath = folderPath;
            }
        }

        private static void RestartEditAssetDatabase()
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AssetDatabase.StartAssetEditing();
        }
    }
}