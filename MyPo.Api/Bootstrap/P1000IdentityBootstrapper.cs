using MyPo.Shared.Bootstrap;
using MyPo.Shared.EF.Identity;
using MyPo.Shared.Identity;
using Microsoft.AspNetCore.Identity;

namespace MyPo.Api.Bootstrap;

sealed class Defaults
{
	public static readonly PasswordOptions passwordOptions = new()
	{
		RequiredLength = 12,
		RequiredUniqueChars = 5,
		RequireDigit = true,
		RequireLowercase = true,
		RequireUppercase = true,
		RequireNonAlphanumeric = false,
	};

	public static readonly ClaimsIdentityOptions claimsIdentityOptions = new()
	{
		EmailClaimType = "ema",
		RoleClaimType = "rol",
		UserIdClaimType = "uid",
		UserNameClaimType = "una",
		SecurityStampClaimType = "sec",
	};
}

/// <summary>
/// Built-in bootstrapper that initializes Asp.Net Core Identity services.
/// </summary>
[Bootstrapper]
public class IdentityBootstrapper
{
	public static void ConfigureBuilder(WebApplicationBuilder appBuilder)
	{
		var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<IdentityBootstrapper>();
		logger.LogInformation("Configuring Identity services...");

		// https://github.com/dotnet/aspnetcore/issues/26119
		// Use .AddIdentityCore<User> then add necessary services manually (e.g. AddRoles, AddSignInManager, etc.)
		// instead of using .AddIdentity<User, Role>
		appBuilder.Services
			.AddIdentityCore<MyPoUser>(opts =>
			{
				opts.Password = Defaults.passwordOptions;
				opts.ClaimsIdentity = Defaults.claimsIdentityOptions;
			})
			// .AddRoles<MyPoRole>()
			// .AddSignInManager<SignInManager<MyPoUser>>()
			.AddEntityFrameworkStores<IdentityDbContextRepository>()
			;
	}
}
