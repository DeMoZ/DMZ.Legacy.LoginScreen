# DMZ.Legacy.LoginScreen
Implementation of unity login and password authentication

## External sign-in providers

Google, Apple, Telegram… plug in through `IExternalSignInProvider`, passed to the `LogInController` constructor:

- `RequestCredentialAsync` asks the player to sign in with the provider and returns its credential (an ID token), `null` if cancelled;
- `SignInAsync` / `LinkAsync` call Unity Authentication with it (`SignInWithGoogleAsync` / `LinkWithGoogleAsync`…).

`LogInView` has `_providerBtns` (the login screen) and `_guestProviderBtns` (the guest card), each bound to a provider id.
Buttons of providers that are not registered or not supported on the platform are hidden.

- Signed out: the button signs in (a new player is created if nobody has the identity).
- A signed-in guest: the identity is linked to the guest, the player id stays. If another player has the identity
  (`AccountAlreadyLinked`), the guest switches to that player the same way as the log in with a name from the guest card
  (`BeforeAccountSwitch`, `OnAccountSwitched`, `AccountSwitchFailed`).
