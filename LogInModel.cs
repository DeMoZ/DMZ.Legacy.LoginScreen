using System;

namespace DMZ.Legacy.LoginScreen
{
    public class LogInModel : IDisposable
    {
        public Action<bool> OnSetViewActive;
        public Action OnSwitchLogInClick;
        public Action OnSwitchSignUpClick;
        public Action OnBackClick;
        public Action OnLogInClick;
        public Action OnSignUpClick;
        public Action OnLogOutClick;
        public Action OnDeleteClick;
        public Action OnCloseClick;
        public Action OnSaveProgressClick;
        public Action<AuthenticationType> OnAuthenticationTypeClick;
        
        public Action<string> OnInputName;
        public Action<string> OnInputPassword;
        public Action OnClearInput;

        public Action<bool> OnRequestAwait;
        public Action<ResponseType> OnLoginRespond; // server response
        public Action<NameValidationType, PasswordValidationType> OnNameAndPasswordValidation;
        public Action<LoginViewState> OnCurrentLoginViewState;
        /// <summary>true when the signed-in player is a guest: the account view offers to keep the progress with a name and password</summary>
        public Action<bool> OnGuestChanged;
        /// <summary>true while a guest signs in or signs up from the account view (the log in form returns to the account view)</summary>
        public bool IsUpgradingGuest { get; set; }
        
        private LoginViewState _currentLoginViewState;        
        public LoginViewState CurrentLoginViewState
        {
            get => _currentLoginViewState;
            set
            {
                _currentLoginViewState = value;
                OnCurrentLoginViewState?.Invoke(_currentLoginViewState);   
            }
        }
        
        public void Dispose()
        {
        }
    }
}