using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

namespace DMZ.Legacy.LoginScreen
{
    public class LogInController : IDisposable
    {
        /// <summary>
        /// triggered when user logged out or the session expired and could not be restored
        /// </summary>
        public event Action OnLoggedOut;

        /// <summary>
        /// triggered when a guest signed in to an existing account from the account view: another player is signed in now
        /// </summary>
        public event Action OnAccountSwitched;

        /// <summary>
        /// Awaited while the guest is still signed in, right before it is signed out to sign in to an existing account,
        /// e.g. to keep the guest progress. An exception cancels the switch, the guest stays signed in.
        /// </summary>
        public Func<Task> BeforeAccountSwitch { get; set; }

        /// <summary>
        /// Called when the sign-in to an existing account failed after <see cref="BeforeAccountSwitch"/>, the guest is signed in again
        /// </summary>
        public Action AccountSwitchFailed { get; set; }

        private readonly LogInModel _model;
        private readonly string _environmentName;
        private readonly LogInInputValidator _inputValidator = new();

        private string _nameText;
        private string _passwordText;
        private bool _isInitialized;
        private Task _initializeTask;
        private TaskCompletionSource<bool> _loginTcs;
        private TaskCompletionSource<bool> _logoutTcs;
        private bool _isSwitchingAccount;

        /// <param name="environmentName">Unity Gaming Services environment; null uses the one from the project settings</param>
        public LogInController(LogInModel model, string environmentName = null)
        {
            _model = model;
            _environmentName = environmentName;
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
            _model.OnSaveProgressClick -= OnSaveProgressClick;
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
            _model.IsUpgradingGuest = false;
            _model.CurrentLoginViewState = LoginViewState.Signed;
            _model.OnGuestChanged?.Invoke(false);
            SetViewActive(true);

            try
            {
                _model.OnRequestAwait?.Invoke(true);
                try
                {
                    _model.OnGuestChanged?.Invoke(await IsGuestAsync());
                }
                finally
                {
                    _model.OnRequestAwait?.Invoke(false);
                }

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
                var options = new InitializationOptions();
                if (!string.IsNullOrEmpty(_environmentName))
                {
                    options.SetEnvironmentName(_environmentName);
                }

                await UnityServices.InitializeAsync(options);
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
            _model.OnSaveProgressClick += OnSaveProgressClick;
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

            if (_isSwitchingAccount)
            {
                // the form stays open until the switch is complete or its error is shown
                return;
            }

            _model.CurrentLoginViewState = LoginViewState.Signed;
            _loginTcs?.TrySetResult(true);
        }

        private void OnSignedOut()
        {
            if (_isSwitchingAccount)
            {
                DebugLog("The guest is signed out to sign in to another account.");
                return;
            }

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
            _model.IsUpgradingGuest = false;
            _model.CurrentLoginViewState = LoginViewState.SelectLoginType;
            _model.OnClearInput?.Invoke();
            _logoutTcs?.TrySetResult(true);
            OnLoggedOut?.Invoke();
        }

        private void OnBackClick()
        {
            if (_model.IsUpgradingGuest)
            {
                _model.IsUpgradingGuest = false;
                _model.OnClearInput?.Invoke();
                _model.CurrentLoginViewState = LoginViewState.Signed;
                return;
            }

            _model.CurrentLoginViewState = LoginViewState.SelectLoginType;
        }

        /// <summary>
        /// The guest opens the log in form from the account view: sign up keeps the guest player (the name and password are added to it),
        /// log in switches to an existing account
        /// </summary>
        private void OnSaveProgressClick()
        {
            DebugLog("OnSaveProgressClick");
            _model.IsUpgradingGuest = true;
            _model.OnClearInput?.Invoke();
            _model.CurrentLoginViewState = LoginViewState.SignUp;
            ValidateNameAndPassword();
        }

        private void OnLogInClick()
        {
            DebugLog("OnLogInClick");
            if (_model.IsUpgradingGuest)
            {
                SwitchToAccountAsync(_nameText, _passwordText);
                return;
            }

            SignInAsync(() => AuthenticationService.Instance.SignInWithUsernamePasswordAsync(_nameText, _passwordText), true);
        }

        private void OnSignUpClick()
        {
            DebugLog("OnSignUpClick");
            if (_model.IsUpgradingGuest)
            {
                AddUsernamePasswordAsync(_nameText, _passwordText);
                return;
            }

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
                await TryRequestAsync(signIn, withCredentials);
            }
            finally
            {
                _model.OnRequestAwait?.Invoke(false);
            }
        }

        /// <returns>false if the request failed, the error is shown to the user</returns>
        private async Task<bool> TryRequestAsync(Func<Task> request, bool withCredentials)
        {
            try
            {
                await request();
                return true;
            }
            catch (RequestFailedException e)
            {
                var response = ToResponseType(e, withCredentials);
                if (response == ResponseType.Error)
                {
                    DebugLogError($"TryRequestAsync, Not handled RequestFailedException:\n{e}");
                }

                _model.OnLoginRespond?.Invoke(response);
            }
            catch (Exception e)
            {
                DebugLogError($"TryRequestAsync, Unexpected error:\n{e}");
                _model.OnLoginRespond?.Invoke(ResponseType.Error);
            }

            return false;
        }

        /// <summary>
        /// The name and password are added to the guest player: the player id and all its data stay
        /// </summary>
        private async void AddUsernamePasswordAsync(string username, string password)
        {
            _model.OnRequestAwait?.Invoke(true);

            try
            {
                if (await TryRequestAsync(() => AuthenticationService.Instance.AddUsernamePasswordAsync(username, password), true))
                {
                    DebugLog($"AddUsernamePasswordAsync: the guest {AuthenticationService.Instance.PlayerId} has a name now");
                    _model.IsUpgradingGuest = false;
                    _model.OnClearInput?.Invoke();
                    _model.CurrentLoginViewState = LoginViewState.Signed;
                    _model.OnGuestChanged?.Invoke(false);
                }
            }
            finally
            {
                _model.OnRequestAwait?.Invoke(false);
            }
        }

        /// <summary>
        /// The guest signs in to an existing account. The SDK can sign in only when signed out, so the guest is signed out first
        /// keeping its session token: if the sign-in fails, the guest is signed in again with it.
        /// </summary>
        private async void SwitchToAccountAsync(string username, string password)
        {
            _model.OnRequestAwait?.Invoke(true);
            string guestId = AuthenticationService.Instance.PlayerId;
            bool isSwitched = false;

            try
            {
                if (BeforeAccountSwitch != null && !await TryRequestAsync(BeforeAccountSwitch, false))
                {
                    return;
                }

                _isSwitchingAccount = true;
                AuthenticationService.Instance.SignOut(false);

                isSwitched = await TryRequestAsync(() => AuthenticationService.Instance.SignInWithUsernamePasswordAsync(username, password), true);
                if (!isSwitched)
                {
                    AccountSwitchFailed?.Invoke();
                    if (!await TrySilentSignInAsync())
                    {
                        DebugLogError("SwitchToAccountAsync: the guest could not be signed in again.");
                        _isSwitchingAccount = false;
                        HandleLoggedOut();
                    }
                }
            }
            finally
            {
                _isSwitchingAccount = false;
                _model.OnRequestAwait?.Invoke(false);
            }

            if (isSwitched)
            {
                DebugLog($"SwitchToAccountAsync: the guest {guestId} switched to the account {AuthenticationService.Instance.PlayerId}");
                _model.IsUpgradingGuest = false;
                _model.OnClearInput?.Invoke();
                _model.CurrentLoginViewState = LoginViewState.Signed;
                _logoutTcs?.TrySetResult(true);
                OnAccountSwitched?.Invoke();
            }
        }

        /// <summary>
        /// A guest has no name and password and no external identity (Google, Apple…)
        /// </summary>
        private static async Task<bool> IsGuestAsync()
        {
            var auth = AuthenticationService.Instance;
            if (!auth.IsSignedIn)
            {
                return false;
            }

            // the sign-in response usually has the player info, otherwise it is requested
            PlayerInfo info = auth.PlayerInfo;
            if (info == null || IsGuest(info))
            {
                try
                {
                    info = await auth.GetPlayerInfoAsync();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[{nameof(LogInController)}] IsGuestAsync: Failed to get the player info:\n{e}");
                    return false;
                }
            }

            return info != null && IsGuest(info);
        }

        private static bool IsGuest(PlayerInfo info)
        {
            return string.IsNullOrEmpty(info.Username) && (info.Identities == null || info.Identities.Count == 0);
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
