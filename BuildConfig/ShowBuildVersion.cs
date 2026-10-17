using TMPro;
using UnityEngine;

namespace DMZ.Legacy.BuildConfig
{
    public class ShowBuildVersion : MonoBehaviour
    {
        [SerializeField] private TMP_Text versionText;

        private void Start()
        {
            var buildConfig = Resources.Load<BuildConfig>(BuildDataConstants.ResourcesPath);
            var date = buildConfig != null ? buildConfig.BuildDate : "editor";
            versionText.text = $"build: {Application.version}\ndate: {date}";
        }
    }
}
