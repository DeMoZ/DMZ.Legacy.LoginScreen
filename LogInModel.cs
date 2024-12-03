using System;

namespace DMZ.Legacy.LoginScreen
{
    public class LogInModel : IDisposable
    {
        public Action OnSwitchLogInClick;
        public Action OnSwitchSignUpClick;
        public Action OnLogInClick;
        public Action OnSignUpClick;
        public Action OnLogOutClick;
        public Action OnDeleteClick;
        
        public Action<string> OnInputName;
        public Action<string> OnInputPassword;
        public Action OnClearInput;

        public Action<bool> OnRequestAwait;
        public Action<ResponseType> OnLoginRespond; // server response
        public Action<bool, string> OnSetLogged;
        public Action<NameValidationType, PasswordValidationType> OnNameAndPasswordValidation;
        public Action<LoginViewState> OnCurrentLoginViewState;
        
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