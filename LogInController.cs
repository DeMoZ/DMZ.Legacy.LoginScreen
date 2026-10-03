using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace DMZ.Legacy.LoginScreen
{
    public class LogInController : IDisposable
    {
        /// <summary>
        /// triggered when user logged out or the session expired and could not be restored
        /// </summary>
        public event Action OnLoggedOut;

        private readonly LogInModel _model;
        private readonly LogInInputValidator _inputValidator = new();

        private string _nameText;
        private string _passwordText;
        private bool _isInitialized;
        private Task _initializeTask;
        private TaskCompletionSource<bool> _loginTcs;
        private TaskCompletionSource<bool> _logoutTcs;

        public LogInController(LogInModel model)
        {
            _model = model;
            _model.OnSetViewActive?.Invoke(false);
        }

        public void Dispose()
        {
            _loginTcs?.TrySetCanceled();
            _logoutTcs?.TrySetCanceled();

            if (!_isInitialized)
            {
                return;
            }

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
            AuthenticationService.Instance.SignedOut -= OnSignedOut;
            AuthenticationService.Instance.Expired -= OnExpired;
        }

        public void SetViewActive(bool isActive)
        {
            _model.OnSetViewActive?.Invoke(isActive);
        }

        /// <summary>
        /// Completes when the player is signed in: restores the cached session if possible, otherwise waits for the user to sign in.
        /// Throws if Unity Services could not be initialized.
        /// </summary>
        public async Task LoginAsync()
        {
            await EnsureInitializedAsync();

            if (AuthenticationService.Instance.IsSignedIn)
            {
                DebugLog("LoginAsync: Already signed in");
                OnSignedIn();
                return;
            }

            if (await TrySilentSignInAsync())
            {
                return;
            }

            DebugLog("LoginAsync: Not signed in, waiting for user");
            _loginTcs?.TrySetCanceled();
            _loginTcs = new TaskCompletionSource<bool>();
            _model.CurrentLoginViewState = LoginViewState.SelectLoginType;
            await _loginTcs.Task;
        }

        /// <summary>
        /// Shows the account view and completes when the user logged out, deleted the account or closed the view.
        /// </summary>
        public async Task LogOutAsync()
        {
            await EnsureInitializedAsync();

            _logoutTcs?.TrySetCanceled();
            _logoutTcs = new TaskCompletionSource<bool>();
            SetViewActive(true);

            try
            {
                await _logoutTcs.Task;
            }
            finally
            {
                SetViewActive(false);
            }
        }

        private Task EnsureInitializedAsync()
        {
            if (_initializeTask == null || _initializeTask.IsFaulted || _initializeTask.IsCanceled)
            {
                _initializeTask = InitializeUnityServiceAsync();
            }

            return _initializeTask;
        }

        private async Task InitializeUnityServiceAsync()
        {
            _model.OnRequestAwait?.Invoke(true);
            _model.CurrentLoginViewState = LoginViewState.SelectLoginType;

            try
            {
                await UnityServices.InitializeAsync();
            }
            finally
            {
                _model.OnRequestAwait?.Invoke(false);
            }

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
            AuthenticationService.Instance.SignedOut += OnSignedOut;
            AuthenticationService.Instance.Expired += OnExpired;

            _isInitialized = true;
        }

        /// <summary>
        /// Signs in with the cached session token without user interaction. Errors are not shown to the user.
        /// </summary>
        private async Task<bool> TrySilentSignInAsync()
        {
            // without a session token SignInAnonymouslyAsync would create a new anonymous account
            if (!AuthenticationService.Instance.SessionTokenExists)
            {
                return false;
            }

            DebugLog("TrySilentSignInAsync: Session token exists, attempt to automatic sign-in...");
            _model.OnRequestAwait?.Invoke(true);

            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                return true;
            }
            catch (Exception e)
            {
                DebugLogWarning($"TrySilentSignInAsync: Failed to restore session:\n{e}");
                return false;
            }
            finally
            {
                _model.OnRequestAwait?.Invoke(false);
            }
        }

        private void OnAuthenticationTypeClick(AuthenticationType type)
        {
            switch (type)
            {
                case AuthenticationType.Guest:
                    DebugLog("OnAuthenticationTypeClick: Guest");
                    SignInAsync(() => AuthenticationService.Instance.SignInAnonymouslyAsync(), false);
                    break;
                case AuthenticationType.UserAndPassword:
                    _model.CurrentLoginViewState = LoginViewState.LogIn;
                    break;
                default:
                    DebugLogError($"OnAuthenticationTypeClick: Not handled authentication type {type}");
                    break;
            }
        }

        private void OnInputName(string text)
        {
            _nameText = text;
            ValidateNameAndPassword();
        }

        private void OnInputPassword(string text)
        {
            _passwordText = text;
            ValidateNameAndPassword();
        }

        private void ValidateNameAndPassword()
        {
            NameValidationType nameValidation;
            PasswordValidationType passwordValidation;

            switch (_model.CurrentLoginViewState)
            {
                case LoginViewState.SignUp:
                    nameValidation = _inputValidator.ValidateSignUpInputName(_nameText);
                    passwordValidation = _inputValidator.ValidateSignUpInputPassword(_passwordText);
                    break;

                case LoginViewState.LogIn:
                case LoginViewState.SelectLoginType:
                    nameValidation = _inputValidator.ValidateLogInInputName(_nameText);
                    passwordValidation = _inputValidator.ValidateLogInInputPassword(_passwordText);
                    break;

                default:
                    // no input fields in other states
                    return;
            }

            _model.OnNameAndPasswordValidation?.Invoke(nameValidation, passwordValidation);
        }

        private void OnSignedIn()
        {
            DebugLog($"OnSignedIn. PlayerID: {AuthenticationService.Instance.PlayerId}");

            _model.CurrentLoginViewState = LoginViewState.Signed;
            _loginTcs?.TrySetResult(true);
        }

        private void OnSignedOut()
        {
            DebugLog("LogOut is successful.");
            HandleLoggedOut();
        }

        /// <summary>
        /// The SDK refreshes the access token automatically, Expired is raised only when the refresh failed.
        /// Try to restore the session once, otherwise treat it as log out.
        /// </summary>
        private async void OnExpired()
        {
            DebugLogWarning("Player session could not be refreshed and expired, trying to restore it...");

            if (await TrySilentSignInAsync())
            {
                return;
            }

            DebugLogError("Player session expired and could not be restored.");
            HandleLoggedOut();
        }

        private void HandleLoggedOut()
        {
            _model.CurrentLoginViewState = LoginViewState.SelectLoginType;
            _model.OnClearInput?.Invoke();
            _logoutTcs?.TrySetResult(true);
            OnLoggedOut?.Invoke();
        }

        private void OnBackClick()
        {
            _model.CurrentLoginViewState = LoginViewState.SelectLoginType;
        }

        private void OnLogInClick()
        {
            DebugLog("OnLogInClick");
            SignInAsync(() => AuthenticationService.Instance.SignInWithUsernamePasswordAsync(_nameText, _passwordText), true);
        }

        private void OnSignUpClick()
        {
            DebugLog("OnSignUpClick");
            SignInAsync(() => AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(_nameText, _passwordText), true);
        }

        private void OnLogOutClick()
        {
            DebugLog("OnLogOutClick");
            // raises SignedOut synchronously
            AuthenticationService.Instance.SignOut(true);
        }

        private void OnDeleteClick()
        {
            DebugLog("OnDeleteClick");
            DeleteAccountAsync();
        }

        private void OnCloseClick()
        {
            DebugLog("OnCloseClick");
            SetViewActive(false);
            _logoutTcs?.TrySetResult(false);
        }

        /// <summary>
        /// Success is handled by the SignedIn event, errors are shown to the user.
        /// </summary>
        private async void SignInAsync(Func<Task> signIn, bool withCredentials)
        {
            _model.OnRequestAwait?.Invoke(true);

            try
            {
                await signIn();
            }
            catch (RequestFailedException e)
            {
                var response = ToResponseType(e, withCredentials);
                if (response == ResponseType.Error)
                {
                    DebugLogError($"SignInAsync, Not handled RequestFailedException:\n{e}");
                }

                _model.OnLoginRespond?.Invoke(response);
            }
            catch (Exception e)
            {
                DebugLogError($"SignInAsync, Unexpected error during sign-in:\n{e}");
                _model.OnLoginRespond?.Invoke(ResponseType.Error);
            }
            finally
            {
                _model.OnRequestAwait?.Invoke(false);
            }
        }

        private static ResponseType ToResponseType(RequestFailedException e, bool withCredentials)
        {
            if (!withCredentials)
            {
                return ResponseType.Error;
            }

            // server "ENTITY_EXISTS" (sign-up with a taken username) is mapped by the SDK to AccountAlreadyLinked
            if (e.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
            {
                return ResponseType.ExistsAlready;
            }

            // server "WRONG_USERNAME_PASSWORD" has no mapping in the SDK and comes as Unknown
            if (e.ErrorCode == CommonErrorCodes.Unknown)
            {
                return ResponseType.InvalidPassword;
            }

            return ResponseType.Error;
        }

        /// <summary>
        /// On success the SDK signs out and raises SignedOut.
        /// </summary>
        private async void DeleteAccountAsync()
        {
            _model.OnRequestAwait?.Invoke(true);

            try
            {
                await AuthenticationService.Instance.DeleteAccountAsync();
                DebugLog("DeleteAccountAsync: Delete is successful.");
            }
            catch (Exception e)
            {
                DebugLogError($"DeleteAccountAsync, Failed to delete account:\n{e}");
                _model.OnLoginRespond?.Invoke(ResponseType.Error);
            }
            finally
            {
                _model.OnRequestAwait?.Invoke(false);
            }
        }

        private void OnSwitchLogInClick()
        {
            _model.CurrentLoginViewState = LoginViewState.LogIn;
            ValidateNameAndPassword();
        }

        private void OnSwitchSignUpClick()
        {
            _model.CurrentLoginViewState = LoginViewState.SignUp;
            ValidateNameAndPassword();
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
