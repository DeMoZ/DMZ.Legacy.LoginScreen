using UnityEngine;
using UnityEngine.UI;

namespace DMZ.Legacy.BuildConfig
{
    public class BuildVersion : MonoBehaviour
    {
        [SerializeField] private BuildConfig onBuildConfig;
        [SerializeField] private Text versionText;

        private void Start()
        {
            versionText.text = $"build: {Application.version}, date: {onBuildConfig.BuildDate}";
        }
    }
}