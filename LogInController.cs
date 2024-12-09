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
        private const int ErrorCodeExistsAlready = 10003;
        private const int ErrorCodeInvalidNameOrPassword = 0;
        private readonly LogInModel _model;
        private readonly LogInInputValidator _inputValidator = new();
        private readonly CancellationTokenSource _cts = new();

        //private bool _isRequestAwaited;
        private string _nameText;
        private string _passwordText;
        private bool _isInitialized;
        private CancellationTokenSource _loginCts;
        private CancellationTokenSource _requestCts;
        private Task<bool> _requestTask;

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
            _cts?.Cancel();
            _cts?.Dispose();

            _model.OnAuthenticationTypeClick -= OnAuthenticationTypeClick;
            _model.OnSwitchSignUpClick -= OnSwitchSignUpClick;
            _model.OnSwitchLogInClick -= OnSwitchLogInClick;
            _model.OnBackClick += OnBackClick;
            _model.OnLogInClick -= OnLogInClick;
            _model.OnSignUpClick -= OnSignUpClick;
            _model.OnLogOutClick -= OnLogOutClick;
            _model.OnDeleteClick -= OnDeleteClick;
            _model.OnInputName -= OnInputName;
            _model.OnInputPassword -= OnInputPassword;

            AuthenticationService.Instance.SignedIn -= OnSignedIn;
            AuthenticationService.Instance.SignInFailed -= OnSignInFailed;
            AuthenticationService.Instance.SignedOut -= OnSignedOut;
            AuthenticationService.Instance.Expired -= OnExpired;
        }

        private void RunRequestTask()
        {
            StopRequestTask();
            _requestCts = new CancellationTokenSource();
            var tcs = new TaskCompletionSource<bool>();
            _requestTask = tcs.Task;
            _requestCts.Token.Register(() => tcs.TrySetResult(true));
        }

        private void StopRequestTask()
        {
            _requestCts?.Cancel();
            _requestTask = null;
        }

        private async void InitializeUnityServiceAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            RunRequestTask();

            _model.OnRequestAwait?.Invoke(true);
            _model.CurrentLoginViewState = LoginViewState.None;

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
                StopRequestTask();
                _model.OnRequestAwait?.Invoke(false);
            }
        }

        public void SetViewActive(bool isActive)
        {
            _model.OnSetViewActive?.Invoke(isActive);
        }

        public async Task Login()
        {
            _loginCts?.Cancel();
            _loginCts = new CancellationTokenSource();

            var tcs = new TaskCompletionSource<bool>();
            var loginTask = tcs.Task;
            _loginCts.Token.Register(() => tcs.TrySetResult(true));
            
            TryAutoLoginAsync(_cts.Token);
            await loginTask;
            
            // var loggedInData = new LoggedInData
            // {
            //     PlayerId = AuthenticationService.Instance.PlayerId,
            //     AccessToken = AuthenticationService.Instance.AccessToken,
            // };
            // return loggedInData;
        }

        private async void TryAutoLoginAsync(CancellationToken ct)
        {
            if (_requestTask is { IsCompleted: false })
                await _requestTask;

            if (AuthenticationService.Instance.IsSignedIn)
            {
                DebugLog("Already signed in");
                OnSignedIn();
                return;
            }

            if (AuthenticationService.Instance.SessionTokenExists)
            {
                DebugLog("Session token exists, attempt to automatic sign-in...");

                RunRequestTask();
                var result = await TryAnonymousSignInAsync();
                StopRequestTask();

                if (result)
                {
                    return;
                }
            }

            _model.CurrentLoginViewState = LoginViewState.None;
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
            RunRequestTask();
            await TryAnonymousSignInAsync();
            StopRequestTask();
        }

        private async Task<bool> TryAnonymousSignInAsync()
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                return true;
            }
            catch (RequestFailedException)
            {
            }
            catch (Exception e)
            {
                DebugLogError($"TryAnonymousSignInAsync, Unexpected error during anonymous sign-in:\n{e}");
            }

            return false;
        }

        private void OnSignedIn()
        {
            DebugLog($"PlayerID: {AuthenticationService.Instance.PlayerId}");
            DebugLog($"Access Token: {AuthenticationService.Instance.AccessToken}");

            _model.CurrentLoginViewState = LoginViewState.Signed;
            _loginCts.Cancel();
        }

        private void OnSignedOut()
        {
            DebugLog("LogOut is successful.");

            _model.CurrentLoginViewState = LoginViewState.None;
            _model.OnClearInput?.Invoke();
        }

        // todo roman
        private void OnExpired()
        {
            DebugLogError("Player session could not be refreshed and expired.");
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
                    DebugLog($"OnSignInFailed, Not handled RequestFailedException:\n{e}");
                    _model.OnLoginRespond?.Invoke(ResponseType.Error);
                    break;
            }
        }

        private void OnBackClick()
        {
            _model.CurrentLoginViewState = LoginViewState.None;
        }

        private void OnLogInClick()
        {
            DebugLog("OnLoginClickAsync");
            TrySignAsync(true);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="isLogin">true for LogIn, false for SingUp</param>
        private async void TrySignAsync(bool isLogin)
        {
            if (_requestTask is { IsCompleted: false })
                return;

            RunRequestTask();
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
                StopRequestTask();
            }

            _model.OnRequestAwait?.Invoke(false);
        }

        private void TryLogOut()
        {
            if (_requestTask is { IsCompleted: false })
                return;

            RunRequestTask();
            _model.OnRequestAwait?.Invoke(true);

            try
            {
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
            catch (Exception e)
            {
                DebugLogError($"TryLogOut, Unexpected error during log-out:\n{e}");
                _model.OnLoginRespond?.Invoke(ResponseType.Error);
            }
            finally
            {
                StopRequestTask();
            }

            _model.OnRequestAwait?.Invoke(false);
        }

        private async void TryDeleteAsync()
        {
            if (_requestTask is { IsCompleted: false })
                return;

            RunRequestTask();
            _model.OnRequestAwait?.Invoke(true);

            try
            {
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
                StopRequestTask();
            }

            _model.OnRequestAwait?.Invoke(false);
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

        private void DebugLogError(string message)
        {
            Debug.LogError($"[{nameof(LogInController)}] {message}");
        }
    }
}