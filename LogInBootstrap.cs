using System.Threading;
using UnityEngine;

namespace DMZ.Legacy.LoginScreen
{
    public class LoginBootstrap : MonoBehaviour
    {
        [SerializeField] private LogInView logInView;

        private CancellationTokenSource _cts = new ();

        private async void Start()
        {
            var  logInModel = new LogInModel();
            logInView.Init(logInModel);
            var logInController = new LogInController(logInModel);
            logInController.SetViewActive(true);
            var loggedInData =  await logInController.Login(_cts.Token);
            // todo roman debug log logged in ddta
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}