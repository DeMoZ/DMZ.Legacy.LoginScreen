using System;

namespace DMZ.Legacy.LoginScreen
{
    public enum LoginViewState
    {
        None,
        LogIn,
        SignUp,
        Signed
    }

    public enum ResponseType
    {
        None,
        Success,
        Error,
        ExistsAlready,
        InvalidPassword,
    }

    [Flags]
    public enum NameValidationType
    {
        MinLength = 1 << 0,
        MaxLength = 1 << 1,
        InvalidCharacters = 1 << 2,
    }

    [Flags]
    public enum PasswordValidationType
    {
        UpperCase = 1 << 0,
        LowerCase = 1 << 1,
        Digits = 1 << 2,
        Symbols = 1 << 3,
        MinLength = 1 << 4,
        MaxLength = 1 << 5,
    }
}