using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DMZ.Legacy.LoginScreen
{
    public class LogInView : MonoBehaviour
    {
        [Header("States")] 
        [SerializeField] private Transform _selectLoginTypeState;
        [SerializeField] private Transform _logInState;
        [SerializeField] private Transform _loggedState;

        [Header("Select Login Type State")] 
        [SerializeField] private Button _userAndPasswordBtn;
        [SerializeField] private Button _guestBtn;

        [Header("Log In State")]
        [SerializeField] private TMP_Text _nameLbl;
        [SerializeField] private TMP_InputField _loginFld;
        [SerializeField] private TMP_Text _passwordLbl;
        [SerializeField] private TMP_InputField _passwordFld;
        [SerializeField] private TMP_Text _statusLbl;
        [SerializeField] private Button _backBtn;
        [SerializeField] private Button _signUpBtn;
        [SerializeField] private Button _logInBtn;
        [SerializeField] private Toggle _switchLogInTgl;
        [SerializeField] private Toggle _switchSignUpTgl;

        [Header("Logged State")]
        [SerializeField] private GameObject _logoutContent;
        [SerializeField] private Button _logOutBtn;
        [SerializeField] private Button _deleteBtn;
        [SerializeField] private GameObject _confirmContent;
        [SerializeField] private Button _notSureBtn;
        [SerializeField] private Button _sureBtn;
        [SerializeField] private Button _closeBtn;

        [Header("Validation Text Color")]
        [SerializeField] private Color _validColor = Color.gray;
        [SerializeField] private Color _invalidColor = Color.white;
        [SerializeField] private Color _validInputTextColor = Color.black;
        [SerializeField] private Color _invalidInputTextColor = Color.red;

        [Header("Loading Blocker")]
        [SerializeField] private GameObject _loadingBlocker;
        
        [Header("Misk")]
        [SerializeField] private UnityEvent<bool> _onEnable;
        
        private readonly StringBuilder _hintBuilder = new();

        private LogInModel _model;
        private Color _color;

        private void Awake()
        {
            _selectLoginTypeState.gameObject.SetActive(false);
            _logInState.gameObject.SetActive(false);
            _loggedState.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            _onEnable?.Invoke(true);
        }
        
        private void OnDisable()
        {
            _onEnable?.Invoke(false);
        }

        public void Init(LogInModel model)
        {
            _model = model;

            _loginFld.onValueChanged.AddListener(s =>
            {
                _statusLbl.text = string.Empty;
                _model.OnInputName?.Invoke(s);
                _loginFld.textComponent.color = _validInputTextColor;
            });

            _passwordFld.onValueChanged.AddListener(s => _model.OnInputPassword?.Invoke(s));

            _switchSignUpTgl.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    _model.OnSwitchSignUpClick?.Invoke();
                }
            });
            _switchLogInTgl.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    _model.OnSwitchLogInClick?.Invoke();
                }
            });

            _backBtn.onClick.AddListener(() => _model.OnBackClick?.Invoke());
            _logInBtn.onClick.AddListener(() => _model.OnLogInClick?.Invoke());
            _signUpBtn.onClick.AddListener(() => _model.OnSignUpClick?.Invoke());

            _logOutBtn.onClick.AddListener(() => _model.OnLogOutClick?.Invoke());
            _deleteBtn.onClick.AddListener(() => EnableConfirmContent(true));

            _notSureBtn.onClick.AddListener(() => EnableConfirmContent(false));
            _sureBtn.onClick.AddListener(() => _model.OnDeleteClick?.Invoke());
            _closeBtn.onClick.AddListener(() => _model.OnCloseClick?.Invoke());

            _guestBtn.onClick.AddListener(() => _model.OnAuthenticationTypeClick?.Invoke(AuthenticationType.Guest));
            _userAndPasswordBtn.onClick.AddListener(() => _model.OnAuthenticationTypeClick?.Invoke(AuthenticationType.UserAndPassword));

            _model.OnSetViewActive += SetViewActive;
            _model.OnLoginRespond += OnSignUpRespond;
            _model.OnRequestAwait += OnSetAllInteractable;
            _model.OnCurrentLoginViewState += OnSetLoginState;
            _model.OnCurrentLoginViewState += OnSetLoginState;
            _model.OnNameAndPasswordValidation += OnNameAndPasswordValidation;
            _model.OnClearInput += OnClearInput;
        }

        private void OnDestroy()
        {
            Debug.LogWarning($"LogInView OnDestroy");
            _switchLogInTgl.onValueChanged.RemoveAllListeners();
            _switchSignUpTgl.onValueChanged.RemoveAllListeners();

            _loginFld.onValueChanged.RemoveAllListeners();
            _passwordFld.onValueChanged.RemoveAllListeners();

            _backBtn.onClick.RemoveAllListeners();
            _signUpBtn.onClick.RemoveAllListeners();
            _logInBtn.onClick.RemoveAllListeners();

            _notSureBtn.onClick.RemoveAllListeners();
            _sureBtn.onClick.RemoveAllListeners();
            _closeBtn.onClick.RemoveAllListeners();

            _model.OnSetViewActive -= SetViewActive;
            _model.OnLoginRespond -= OnSignUpRespond;
            _model.OnRequestAwait -= OnSetAllInteractable;
            _model.OnCurrentLoginViewState -= OnSetLoginState;
            _model.OnNameAndPasswordValidation -= OnNameAndPasswordValidation;
            _model.OnClearInput -= OnClearInput;
        }

        private void SetViewActive(bool active)
        {
            gameObject.SetActive(active);
        }

        private void OnClearInput()
        {
            _loginFld.text = string.Empty;
            _passwordFld.text = string.Empty;
        }

        private void EnableConfirmContent(bool isEnable)
        {
            _logoutContent.SetActive(!isEnable);
            _confirmContent.SetActive(isEnable);
        }

        private void OnSetLoginState(LoginViewState state)
        {
            _statusLbl.text = string.Empty;
            _loginFld.textComponent.color = _validInputTextColor;

            _selectLoginTypeState.gameObject.SetActive(state == LoginViewState.SelectLoginType);
            _loggedState.gameObject.SetActive(state == LoginViewState.Signed);

            // Login or SignUp
            {
                _logInState.gameObject.SetActive(state is LoginViewState.LogIn or LoginViewState.SignUp);

                _switchLogInTgl.SetIsOnWithoutNotify(state == LoginViewState.LogIn);
                _switchSignUpTgl.SetIsOnWithoutNotify(state == LoginViewState.SignUp);
                _logInBtn.gameObject.SetActive(state == LoginViewState.LogIn);
                _signUpBtn.gameObject.SetActive(state == LoginViewState.SignUp);
            }

            switch (state)
            {
                case LoginViewState.SelectLoginType:
                    break;
                case LoginViewState.LogIn:
                    ClearNameAndPasswordLabels();
                    break;
                case LoginViewState.SignUp:
                    break;
                case LoginViewState.Signed:
                    EnableConfirmContent(false);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }

        private void OnSetAllInteractable(bool isInteractable)
        {
            Debug.LogError($"blockerActive {isInteractable}");
           // _loadingBlocker.SetActive(isInteractable);
        }

        private void OnSignUpRespond(ResponseType responseType)
        {
            Debug.LogError("todo roman, Not implemented pop-up message with response type: " + responseType);

            switch (responseType)
            {
                case ResponseType.None:
                    _statusLbl.text = string.Empty;
                    break;
                case ResponseType.ExistsAlready:
                    _statusLbl.text = "Already exists";
                    _loginFld.textComponent.color = _invalidInputTextColor;
                    break;
                case ResponseType.InvalidPassword:
                    _statusLbl.text = "Invalid login or password";
                    break;
                case ResponseType.Success:
                    _statusLbl.text = "Success";
                    break;
                case ResponseType.Error:
                    _statusLbl.text = "Some Error";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(responseType), responseType, null);
            }
        }

        private void OnNameAndPasswordValidation(NameValidationType nameValidationType, PasswordValidationType passwordValidationType)
        {
            switch (_model.CurrentLoginViewState)
            {
                case LoginViewState.SignUp:
                    SignUpNameValidation(nameValidationType);
                    SignUpPasswordValidation(passwordValidationType);
                    _signUpBtn.interactable = nameValidationType == 0 && passwordValidationType == 0;
                    break;

                case LoginViewState.LogIn:
                    LogInNameValidation(nameValidationType);
                    LogInPasswordValidation(passwordValidationType);
                    _logInBtn.interactable = nameValidationType == 0 && passwordValidationType == 0;
                    break;

                default:
                    //throw new NotImplementedException();
                    break;
            }
        }

        private void LogInNameValidation(NameValidationType _)
        {
            _nameLbl.text = string.Empty;
        }

        private void LogInPasswordValidation(PasswordValidationType _)
        {
            _passwordLbl.text = string.Empty;
        }

        private void SignUpNameValidation(NameValidationType validationType)
        {
            _hintBuilder.Clear();

            _color = validationType.HasFlag(NameValidationType.MinLength) ? _invalidColor : _validColor;
            _hintBuilder.Append(AddColor("Min", _color));

            _color = validationType.HasFlag(NameValidationType.InvalidCharacters) ? _invalidColor : _validColor;
            _hintBuilder.Append(AddColor("@#$", _color));

            _color = validationType.HasFlag(NameValidationType.MaxLength) ? _invalidColor : _validColor;
            _hintBuilder.Append(AddColor("Max", _color));

            _nameLbl.text = _hintBuilder.ToString();
        }

        private void SignUpPasswordValidation(PasswordValidationType validationType)
        {
            _hintBuilder.Clear();

            _color = validationType.HasFlag(PasswordValidationType.UpperCase) ? _invalidColor : _validColor;
            _hintBuilder.Append(AddColor("A-Z", _color));

            _color = validationType.HasFlag(PasswordValidationType.LowerCase) ? _invalidColor : _validColor;
            _hintBuilder.Append(AddColor("a-z", _color));

            _color = validationType.HasFlag(PasswordValidationType.Digits) ? _invalidColor : _validColor;
            _hintBuilder.Append(AddColor("1-9", _color));

            _color = validationType.HasFlag(PasswordValidationType.Symbols) ? _invalidColor : _validColor;
            _hintBuilder.Append(AddColor("@#$", _color));

            _color = validationType.HasFlag(PasswordValidationType.MinLength) ? _invalidColor : _validColor;
            _hintBuilder.Append(AddColor("Min 8", _color));

            _color = validationType.HasFlag(PasswordValidationType.MaxLength) ? _invalidColor : _validColor;
            _hintBuilder.Append(AddColor("Max 30", _color));

            _passwordLbl.text = _hintBuilder.ToString();
        }

        private void ClearNameAndPasswordLabels()
        {
            _nameLbl.text = string.Empty;
            _passwordLbl.text = string.Empty;
        }

        private string AddColor(string value, Color color)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGBA(color)}>{value}, </color>";
        }
    }
}