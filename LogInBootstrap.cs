using UnityEngine;

namespace DMZ.Legacy.LoginScreen
{
    public class LoginBootstrap : MonoBehaviour
    {
        [SerializeField] private LogInView logInView;

        private async void Start()
        {
            var  logInModel = new LogInModel();
            logInView.Init(logInModel);
            var logInController = new LogInController(logInModel);
            await logInController.TryRestoreCurrentSessionAsync();
        }
    }
}