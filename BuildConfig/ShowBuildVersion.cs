using TMPro;
using UnityEngine;

namespace DMZ.Legacy.BuildConfig
{
    public class ShowBuildVersion : MonoBehaviour
    {
        [SerializeField] private BuildConfig onBuildConfig;
        [SerializeField] private TMP_Text versionText;

        private void Start()
        {
            versionText.text = $"build: {Application.version}\ndate: {onBuildConfig.BuildDate}";
        }
    }
}