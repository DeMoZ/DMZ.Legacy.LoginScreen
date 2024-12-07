using UnityEngine;

namespace DMZ.Legacy.BuildConfig
{
    [CreateAssetMenu(menuName = "DMZ/Create BuildConfig", fileName = "OnBuildConfig")]
    public class BuildConfig : ScriptableObject
    {
        public string BandleVersion;
        public string BuildDate;
    }
}