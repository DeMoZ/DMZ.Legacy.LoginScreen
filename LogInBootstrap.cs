using System.Threading;
using System.Threading.Tasks;
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
            logInController.Login(_cts);
            
            await Task.Run(() => _cts.Token.WaitHandle.WaitOne());
            
            // logInController.SetViewActive(false);
            // todo roman debug log logged in ddta
        }

        private void OnDestroy()
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}