using System.Threading.Tasks;

namespace DMZ.Legacy.LoginScreen
{
    /// <summary>
    /// An external sign-in provider (Google, Apple, Telegram…) for <see cref="LogInController"/>.
    /// The provider part asks the player to sign in and returns a credential (e.g. an ID token);
    /// the Unity Authentication part signs in or links the guest with it.
    /// The view binds its buttons by <see cref="Id"/>; buttons of unsupported providers are hidden.
    /// </summary>
    public interface IExternalSignInProvider
    {
        /// <summary>Unique id, e.g. "google". The view's provider buttons use the same id.</summary>
        string Id { get; }

        /// <summary>false on platforms or builds where the provider can't be used (its buttons are hidden)</summary>
        bool IsSupported { get; }

        /// <summary>
        /// Asks the player to sign in with the provider.
        /// </summary>
        /// <returns>the credential for <see cref="SignInAsync"/> and <see cref="LinkAsync"/>, null if the player cancelled</returns>
        /// <exception cref="System.Exception">the provider failed, the error is shown to the player</exception>
        Task<string> RequestCredentialAsync();

        /// <summary>Signs in to the player that has this identity, a new player is created if there is none.</summary>
        Task SignInAsync(string credential);

        /// <summary>
        /// Adds the identity to the signed-in player (a guest): the player id stays.
        /// Throws <see cref="Unity.Services.Authentication.AuthenticationException"/> with
        /// <see cref="Unity.Services.Authentication.AuthenticationErrorCodes.AccountAlreadyLinked"/> when another player has it.
        /// </summary>
        Task LinkAsync(string credential);
    }
}
