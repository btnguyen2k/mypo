namespace MyPo.Shared;

public sealed class Globals
{
	/// <summary>
	/// The string comparison to use throughout the application.
	/// </summary>
	public static readonly StringComparison StringComparison = StringComparison.InvariantCultureIgnoreCase;

	/// <summary>
	/// Configuration key for disabling local authentication.
	/// </summary>
	public const string CONF_AUTH_DISABLED_LOCAL_AUTH = "Authentication:DisabledLocalAuth";

	/// <summary>
	/// Configuration key for enabling automatic user creation on external authentication.
	/// </summary>
	public const string CONF_AUTH_AUTO_CREATE_USERS = "Authentication:AutoCreateUsers";
}
