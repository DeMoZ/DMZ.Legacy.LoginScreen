using System;
using UnityEngine;

namespace DMZ.Legacy.BuildConfig
{
    [CreateAssetMenu(menuName = "DMZ/Create BuildConfig", fileName = "OnBuildConfig")]
    public class BuildConfig : ScriptableObject
    {
        public string BandleVersion = "0.0.1";
        public string BuildDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}