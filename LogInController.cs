using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace DMZ.Legacy.LoginScreen
{
    public class LogInController : IDisposable
    {
        /// <summary>
        /// triggered when user logged out
        /// </summary>
        public event Action OnLoggedOut;

        private const int ErrorCodeExistsAlready = 10003;
        private const int ErrorCodeInvalidNameOrPassword = 0;
        private readonly LogInModel _model;
        private readonly LogInInputValidator _inputValidator = new();
        private readonly CancellationTokenSource _cts = new();

        private string _nameText;
        private string _passwordText;
        private bool _isInitialized;
        private CancellationTokenSource _loginCts;
        private CancellationTokenSource _logoutCts;

        public LogInController(LogInModel model)
        {
            _model = model;
            _model.OnSetViewActive?.Invoke(false);
            InitializeUnityServiceAsync();
        }

        public void Dispose()
        {
            _loginCts?.Cancel();
            _loginCts?.Dispose();
            _logoutCts?.Cancel();
            _logoutCts?.Dispose();
            _cts?.Cancel();
            _cts?.Dispose();

            _model.OnAuthenticationTypeClick -= OnAuthenticationTypeClick;
            _model.OnSwitchSignUpClick -= OnSwitchSignUpClick;
            _model.OnSwitchLogInClick -= OnSwitchLogInClick;
            _model.OnBackClick -= OnBackClick;
            _model.OnLogInClick -= OnLogInClick;
            _model.OnSignUpClick -= OnSignUpClick;
            _model.OnLogOutClick -= OnLogOutClick;
            _model.OnDeleteClick -= OnDeleteClick;
            _model.OnCloseClick -= OnCloseClick;
            _model.OnInputName -= OnInputName;
            _model.OnInputPassword -= OnInputPassword;

            AuthenticationService.Instance.SignedIn -= OnSignedIn;
            AuthenticationService.Instance.SignInFailed -= OnSignInFailed;
            AuthenticationService.Instance.SignedOut -= OnSignedOut;
            AuthenticationService.Instance.Expired -= OnExpired;
        }

        private async void InitializeUnityServiceAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _model.OnRequestAwait?.Invoke(true);
            _model.CurrentLoginViewState = LoginViewState.SelectLoginType;

            try
            {
                await UnityServices.InitializeAsync();
                _isInitialized = true;

                _model.OnAuthenticationTypeClick += OnAuthenticationTypeClick;
                _model.OnSwitchSignUpClick += OnSwitchSignUpClick;
                _model.OnSwitchLogInClick += OnSwitchLogInClick;
                _model.OnBackClick += OnBackClick;
                _model.OnLogInClick += OnLogInClick;
                _model.OnSignUpClick += OnSignUpClick;
                _model.OnLogOutClick += OnLogOutClick;
                _model.OnDeleteClick += OnDeleteClick;
                _model.OnCloseClick += OnCloseClick;
                _model.OnInputName += OnInputName;
                _model.OnInputPassword += OnInputPassword;

                AuthenticationService.Instance.SignedIn += OnSignedIn;
                AuthenticationService.Instance.SignInFailed += OnSignInFailed;
                AuthenticationService.Instance.SignedOut += OnSignedOut;
                AuthenticationService.Instance.Expired += OnExpired;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _model.OnRequestAwait?.Invoke(false);
            }
        }

        public void SetViewActive(bool isActive)
        {
            _model.OnSetViewActive?.Invoke(isActive);
        }

        public async Task LoginAsync()
        {
            var loginTask = RunLogInTask();
            TryAutoLoginAsync();
            await loginTask;

            // var loggedInData = new LoggedInData
            // {
            //     PlayerId = AuthenticationService.Instance.PlayerId,
            //     AccessToken = AuthenticationService.Instance.AccessToken,
            // };
            // return loggedInData;
        }

        public async void LogOutAsync()
        {
            var loginTask = RunLogOutTask();
            SetViewActive(true);

            await loginTask;
            SetViewActive(false);
        }

        private Task RunLogInTask()
        {
            _loginCts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<bool>();
            var task = tcs.Task;
            _loginCts.Token.Register(() => tcs.TrySetResult(true));
            return task;
        }

        private Task RunLogOutTask()
        {
            _logoutCts?.Cancel();
            _logoutCts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<bool>();
            var task = tcs.Task;
            _logoutCts.Token.Register(() => tcs.TrySetResult(true));
            return task;
        }

        private async void TryAutoLoginAsync()
        {
            if (AuthenticationService.Instance.IsSignedIn)
            {
                DebugLog("TryAutoLoginAsync: Already signed in");
                OnSignedIn();
                return;
            }

            if (AuthenticationService.Instance.SessionTokenExists)
            {
                DebugLog("TryAutoLoginAsync: Session token exists, attempt to automatic sign-in...");

                _model.OnRequestAwait?.Invoke(true);
                await TryAnonymousSignInAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                DebugLog("TryAutoLoginAsync: Not signed in");
                _model.CurrentLoginViewState = LoginViewState.SelectLoginType;
            }
        }

        private void OnAuthenticationTypeClick(AuthenticationType type)
        {
            switch (type)
            {
                case AuthenticationType.Guest:
                    GuestSignInAsync();
                    break;
                case AuthenticationType.UserAndPassword:
                    _model.CurrentLoginViewState = LoginViewState.LogIn;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void OnInputName(string text)
        {
            _nameText = text;
            ValidateNameAndPassword(_nameText, _passwordText);
        }

        private void OnInputPassword(string text)
        {
            _passwordText = text;
            ValidateNameAndPassword(_nameText, _passwordText);
        }

        private void ValidateNameAndPassword(string nameText, string passwordText)
        {
            NameValidationType nameValidation = 0;
            PasswordValidationType passwordValidation = 0;

            switch (_model.CurrentLoginViewState)
            {
                case LoginViewState.SignUp:
                    nameValidation = _inputValidator.ValidateSignUpInputName(nameText);
                    passwordValidation = _inputValidator.ValidateSignUpInputPassword(passwordText);
                    break;

                case LoginViewState.LogIn:
                    nameValidation = _inputValidator.ValidateLogInInputName(nameText);
                    passwordValidation = _inputValidator.ValidateLogInInputPassword(passwordText);
                    break;

                default:
                    throw new NotImplementedException();
            }

            _model.OnNameAndPasswordValidation?.Invoke(nameValidation, passwordValidation);
        }

        private async void GuestSignInAsync()
        {
            DebugLog("GuestSignInAsync");
            //RunRequestTask();
            await TryAnonymousSignInAsync();
            //StopRequestTask();
        }

        private async Task TryAnonymousSignInAsync()
        {
            try
            {
                _model.OnRequestAwait?.Invoke(true);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (RequestFailedException)
            {
            }
            catch (Exception e)
            {
                DebugLogError($"TryAnonymousSignInAsync, Unexpected error during anonymous sign-in:\n{e}");
            }
        }

        private void OnSignedIn()
        {
            DebugLog($"OnSignedIn. PlayerID: {AuthenticationService.Instance.PlayerId}");
            DebugLog($"OnSignedIn. Access Token: {AuthenticationService.Instance.AccessToken}");

            _model.CurrentLoginViewState = LoginViewState.Signed;
            _loginCts?.Cancel();
            _model.OnRequestAwait?.Invoke(false);
        }

        private void OnSignedOut()
        {
            DebugLog("LogOut is successful.");

            _model.CurrentLoginViewState = LoginViewState.SelectLoginType;
            _model.OnClearInput?.Invoke();
            _logoutCts?.Cancel();
            _model.OnRequestAwait?.Invoke(false);
            OnLoggedOut?.Invoke();
        }

        // todo roman
        private void OnExpired()
        {
            DebugLogError("Player session could not be refreshed and expired.");
            _model.OnRequestAwait?.Invoke(false);
        }

        private void OnSignInFailed(RequestFailedException e)
        {
            switch (e.ErrorCode)
            {
                case ErrorCodeExistsAlready:
                    _model.OnLoginRespond?.Invoke(ResponseType.ExistsAlready);
                    break;
                case ErrorCodeInvalidNameOrPassword:
                    _model.OnLoginRespond?.Invoke(ResponseType.InvalidPassword);
                    break;
                default:
                    DebugLogError($"OnSignInFailed, Not handled RequestFailedException:\n{e}");
                    _model.OnLoginRespond?.Invoke(ResponseType.Error);
                    break;
            }
            
            _model.OnRequestAwait?.Invoke(false);
        }

        private void OnBackClick()
        {
            _model.CurrentLoginViewState = LoginViewState.SelectLoginType;
        }

        private void OnLogInClick()
        {
            DebugLog("OnLoginClickAsync");
            TrySignAsync(true);
            //_loginCts.Cancel();
        }

        private void OnSignUpClick()
        {
            DebugLog("OnSignUpClickAsync");
            TrySignAsync(false);
        }

        private void OnLogOutClick()
        {
            DebugLog("OnLogOutClick");
            TryLogOut();
        }

        private void OnDeleteClick()
        {
            DebugLog("OnDeleteClick");
            TryDeleteAsync();
        }

        private void OnCloseClick()
        {
            DebugLog("OnCloseClick");
            SetViewActive(false);
            _logoutCts.Cancel();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isLogin">true for LogIn, false for SingUp</param>
        private async void TrySignAsync(bool isLogin)
        {
            // if (_requestTask is { IsCompleted: false })
            //     return;
            //
            // RunRequestTask();
            _model.OnRequestAwait?.Invoke(true);

            try
            {
                var handle = isLogin
                    ? AuthenticationService.Instance.SignInWithUsernamePasswordAsync(_nameText, _passwordText)
                    : AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(_nameText, _passwordText);

                await handle;
            }
            catch (RequestFailedException)
            {
            }
            catch (Exception e)
            {
                DebugLogError($"TrySignAsync, Unexpected error during username/password sign-in:\n{e}");
            }
            finally
            {
                //StopRequestTask();
            }

           // _model.OnRequestAwait?.Invoke(false);
        }

        private void TryLogOut()
        {
            // if (_requestTask is { IsCompleted: false })
            //     return;
            //
            // RunRequestTask();
            

            try
            {
                _model.OnRequestAwait?.Invoke(true);
                AuthenticationService.Instance.SignOut(true);
            }
            catch (RequestFailedException e)
            {
                switch (e.ErrorCode)
                {
                    default:
                        DebugLogError($"TryLogOut, Not handled RequestFailedException:\n{e}");
                        _model.OnLoginRespond?.Invoke(ResponseType.Error);
                        break;
                }
            }
            // catch (Exception e)
            // {
            //     DebugLogError($"TryLogOut, Unexpected error during log-out:\n{e}");
            //     _model.OnLoginRespond?.Invoke(ResponseType.Error);
            // }
            finally
            {
                // StopRequestTask();
            }

            //_model.OnRequestAwait?.Invoke(false);
        }

        private async void TryDeleteAsync()
        {
            // if (_requestTask is { IsCompleted: false })
            //     return;
            //
            // RunRequestTask();
            

            try
            {
                _model.OnRequestAwait?.Invoke(true);
                var handle = AuthenticationService.Instance.DeleteAccountAsync();
                await handle;
                DebugLog("TryDeleteAsync Delete is successful.");
            }
            catch (RequestFailedException e)
            {
                DebugLogError("TryDeleteAsync, Not handled RequestFailedException:\n" + e);
                _model.OnLoginRespond?.Invoke(ResponseType.Error);
            }
            catch (Exception e)
            {
                DebugLogError($"TryDeleteAsync, Unexpected error during username/password sign-in:\n{e.Message}");
                _model.OnLoginRespond?.Invoke(ResponseType.Error);
            }
            finally
            {
                // StopRequestTask();
            }
        }

        private void OnSwitchLogInClick()
        {
            _model.CurrentLoginViewState = LoginViewState.LogIn;
            ValidateNameAndPassword(_nameText, _passwordText);
        }

        private void OnSwitchSignUpClick()
        {
            _model.CurrentLoginViewState = LoginViewState.SignUp;
            ValidateNameAndPassword(_nameText, _passwordText);
        }

        private void DebugLog(string message)
        {
            Debug.Log($"[{nameof(LogInController)}] {message}");
        }

        private void DebugLogWarning(string message)
        {
            Debug.LogWarning($"[{nameof(LogInController)}] {message}");
        }
        
        private void DebugLogError(string message)
        {
            Debug.LogError($"[{nameof(LogInController)}] {message}");
        }
    }
}