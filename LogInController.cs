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

        private bool _isRequestAwaited;
        private string _nameText;
        private string _passwordText;
        private bool _isInitialized;

        private CancellationTokenSource _cts = new();

        public LogInController(LogInModel model)
        {
            _model = model;
            InitializeUnityServiceAsync();
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();

            _model.OnSwitchSignUpClick -= OnSwitchSignUpClick;
            _model.OnSwitchLogInClick -= OnSwitchLogInClick;
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

        private async void InitializeUnityServiceAsync()
        {
            if (_isInitialized)
            {
                return;
            }

            _isRequestAwaited = true;
            _model.OnRequestAwait?.Invoke(false);
            _model.CurrentLoginViewState = LoginViewState.None;

            try
            {
                await UnityServices.InitializeAsync();
                _isInitialized = true;

                _model.OnSwitchSignUpClick += OnSwitchSignUpClick;
                _model.OnSwitchLogInClick += OnSwitchLogInClick;
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
                _isRequestAwaited = false;
            }
        }

        public async Task TryRestoreCurrentSessionAsync()
        {
            while (_isRequestAwaited)
            {
                await Task.Delay(50, _cts.Token);
            }

            if (AuthenticationService.Instance.IsSignedIn)
            {
                DebugLog("Already signed in");
                OnSignedIn();
                return;
            }

            if (AuthenticationService.Instance.SessionTokenExists)
            {
                _isRequestAwaited = true;
                var result = await TryAnonymousSignInAsync();
                _isRequestAwaited = false;
                if (result)
                {
                    return;
                }
            }

            _model.CurrentLoginViewState = LoginViewState.LogIn;
            ValidateNameAndPassword(_nameText, _passwordText);
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

        private async Task<bool> TryAnonymousSignInAsync()
        {
            DebugLog("Session token exists, attempt to automatic sign-in...");

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

            _model.OnSetLogged(true, $"PlayerID: {AuthenticationService.Instance.PlayerId}");
            _model.CurrentLoginViewState = LoginViewState.Signed;
        }

        private void OnSignedOut()
        {
            DebugLog("LogOut is successful.");

            _model.OnSetLogged(false, $"Logged out");
            _model.CurrentLoginViewState = LoginViewState.LogIn;
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
            if (_isRequestAwaited)
                return;

            _isRequestAwaited = true;
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

            _isRequestAwaited = false;
            _model.OnRequestAwait?.Invoke(false);
        }

        private void TryLogOut()
        {
            if (_isRequestAwaited)
                return;

            _isRequestAwaited = true;
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

            _isRequestAwaited = false;
            _model.OnRequestAwait?.Invoke(false);
        }

        private async void TryDeleteAsync()
        {
            if (_isRequestAwaited)
                return;

            _isRequestAwaited = true;
            _model.OnRequestAwait?.Invoke(true);

            try
            {
                var handle = AuthenticationService.Instance.DeleteAccountAsync();
                await handle;
                DebugLog("TryDeleteAsync Delete is successful.");
                _model.OnSetLogged(false, $"Account Deleted");
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

            _isRequestAwaited = false;
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

    // todo roman is this for what?
    [Serializable]
    public struct TestPlayerProfile
    {
        public PlayerInfo playerInfo;
        public string Name;
    }
}