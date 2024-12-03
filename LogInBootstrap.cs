using UnityEngine;
using UnityEngine.Serialization;

namespace DMZ.Legacy.LoginScreen
{
    public class LoginBootstrap : MonoBehaviour
    {
        [FormerlySerializedAs("logInUi")] [FormerlySerializedAs("loginUi")] [SerializeField] private LogInUi _logInUi;

        private LogInController _logInController;
        private LogInModel _logInModel = new ();

        private void Start()
        {
            _logInUi.Init(_logInModel);
            _logInController = new LogInController(_logInModel);
        }
    }
}