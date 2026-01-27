using MyPo.Shared.Api;
using MyPo.Shared.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MyPo.Api.Controllers;

public partial class UsersController
{
	/// <summary>
	/// Gets all available roles.
	/// </summary>
	/// <returns></returns>
	[HttpGet(IApiClient.API_ENDPOINT_USERS)]
	[Authorize(Policy = BuiltinPolicies.POLICY_NAME_ADMIN_ROLE_OR_USER_MANAGER)]
	public async Task<ActionResult<ApiResp<IEnumerable<UserResp>>>> GetAllUsers()
	{
		var users = await IdentityRepository.GetAllUsersAsync(UserFetchOptions.FETCH_ROLES_AND_CLAIMS_ONLY);
		var result = new List<UserResp>();
		foreach (var user in users)
		{
			result.Add(UserResp.BuildFromUser(user));
		}
		return ResponseOk(result);
	}

	/// <summary>
	/// Gets a user by id.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[HttpGet(IApiClient.API_ENDPOINT_USERS_ID)]
	[Authorize(Policy = BuiltinPolicies.POLICY_NAME_ADMIN_ROLE_OR_USER_MANAGER)]
	public async Task<ActionResult<ApiResp<UserResp>>> GetUser([FromRoute] string id)
	{
		var user = await IdentityRepository.GetUserByIDAsync(id, UserFetchOptions.DEFAULT.FetchRoles().FetchClaims());
		if (user == null)
		{
			return ResponseNoData(404, $"User '{id}' not found.");
		}
		user.Roles ??= await IdentityRepository.GetRolesAsync(user);
		user.Claims ??= await IdentityRepository.GetClaimsAsync(user);
		return ResponseOk(UserResp.BuildFromUser(user));
	}

	/// <summary>
	/// Creates a new user.
	/// </summary>
	/// <param name="req"></param>
	/// <param name="lookupNormalizer"></param>
	/// <param name="passwordValidator"></param>
	/// <param name="passwordHasher"></param>
	/// <param name="userManager"></param>
	/// <param name="appConfig"></param>
	/// <returns></returns>
	/// <response code="200">User created successfully.</response>
	/// <response code="400">Input validation failed (e.g. user already exists).</response>
	/// <response code="500">Failed to create user.</response>
	/// <response code="509">Failed to add claims to user or user to roles, but user was created.</response>
	[HttpPost(IApiClient.API_ENDPOINT_USERS)]
	[Authorize(Policy = BuiltinPolicies.POLICY_NAME_ADMIN_ROLE_OR_CREATE_USER_PERM)]
	public async Task<ActionResult<ApiResp<UserResp>>> CreateUser(
		CreateOrUpdateUserReq req,
		ILookupNormalizer lookupNormalizer,
		IPasswordValidator<MyPoUser> passwordValidator,
		IPasswordHasher<MyPoUser> passwordHasher,
		UserManager<MyPoUser> userManager,
		IConfiguration appConfig)
	{
		var (authErrorResult, _) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		// validate the username
		var username = req.Username?.ToLower().Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(username))
		{
			return ResponseNoData(400, "Username is required.");
		}

		// validate the email
		var email = req.Email?.ToLower().Trim() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(email))
		{
			return ResponseNoData(400, "Email is required.");
		}

		var disabledLocalAuth = appConfig.GetValue<bool>(MyPo.Shared.Globals.CONF_AUTH_DISABLED_LOCAL_AUTH);

		// validate the password
		var password = req.Password?.Trim() ?? string.Empty;
		if (!disabledLocalAuth && string.IsNullOrWhiteSpace(password))
		{
			return ResponseNoData(400, "Password is required.");
		}
		if (!disabledLocalAuth)
		{
			var vPasswordResult = await passwordValidator.ValidateAsync(userManager, null!, password);
			if (vPasswordResult != IdentityResult.Success)
			{
				return ResponseNoData(400, vPasswordResult.ToString());
			}
		}

		// check if the username is already taken
		var existingUserName = await IdentityRepository.GetUserByUserNameAsync(username);
		if (existingUserName != null)
		{
			return ResponseNoData(400, $"User '{username}' already exists.");
		}

		// check if the email is already taken
		var existingUserEmail = await IdentityRepository.GetUserByEmailAsync(email);
		if (existingUserEmail != null)
		{
			return ResponseNoData(400, $"Email '{email}' has been used by another user.");
		}

		// verify if the claims are valid
		var uniqueClaims = (req.Claims?.Distinct() ?? []).Select(c => new Claim(c.Type, c.Value)).ToList();
		if (uniqueClaims.Count > 0)
		{
			var invalidClaim = uniqueClaims.Where(c => !BuiltinClaims.ALL_CLAIMS.Contains(c, ClaimEqualityComparer.INSTANCE)).FirstOrDefault();
			if (invalidClaim != null)
			{
				return ResponseNoData(400, $"Claim '{invalidClaim.Type}:{invalidClaim.Value}' is not valid.");
			}
		}

		// verify if the roles are valid
		var uniqueRoles = (req.Roles?.Distinct() ?? []).Select(r => new KeyValuePair<string, MyPoRole?>(r, IdentityRepository.GetRoleByIDAsync(r).Result)).ToList();
		if (uniqueRoles.Count > 0)
		{
			var invalidRole = uniqueRoles.Where(r => r.Value == null).FirstOrDefault();
			if (invalidRole.Value != null)
			{
				return ResponseNoData(400, $"Role '{invalidRole.Key}' does not exist.");
			}
		}

		// first, create the user
		var user = new MyPoUser
		{
			UserName = username,
			NormalizedUserName = lookupNormalizer.NormalizeName(username),
			Email = email,
			NormalizedEmail = lookupNormalizer.NormalizeEmail(email),
			PasswordHash = passwordHasher.HashPassword(null!, !disabledLocalAuth ? password : Guid.NewGuid().ToString()), // if local auth is disabled, set a random password
			FamilyName = req.FamilyName?.Trim(),
			GivenName = req.GivenName?.Trim(),
		};
		var iresultCreate = await IdentityRepository.CreateAsync(user);
		if (!iresultCreate.Succeeded)
		{
			return ResponseNoData(500, $"Failed to create user: {iresultCreate}");
		}

		// then add the claims
		if (uniqueClaims.Count > 0)
		{
			var iresultAddClaims = await IdentityRepository.AddClaimsAsync(user, uniqueClaims);
			if (!iresultAddClaims.Succeeded)
			{
				return ResponseNoData(509, $"Failed to add claims to user: {iresultAddClaims} / Note: User has been created.");
			}
		}

		// then add the roles
		if (uniqueRoles.Count > 0)
		{
			var iresultAddRoles = await IdentityRepository.AddToRolesAsync(user, uniqueRoles.Select(r => r.Value!));
			if (!iresultAddRoles.Succeeded)
			{
				return ResponseNoData(509, $"Failed to add roles to user: {iresultAddRoles} / Note: User has been created.");
			}
		}

		return ResponseOk(UserResp.BuildFromUser(user));
	}

	/// <summary>
	/// Updates an existing user.
	/// </summary>
	/// <param name="id"></param>
	/// <param name="req"></param>
	/// <param name="lookupNormalizer"></param>
	/// <returns></returns>
	/// <response code="200">User updated successfully.</response>
	/// <response code="400">Input validation failed (e.g. user's name already used by another one).</response>
	/// <response code="404">User not found.</response>
	/// <response code="500">Failed to update user.</response>
	/// <response code="509">Failed to update user's roles or claims, but other user data was updated.</response>
	[HttpPut(IApiClient.API_ENDPOINT_USERS_ID)]
	[Authorize(Policy = BuiltinPolicies.POLICY_NAME_ADMIN_ROLE_OR_MODIFY_USER_PERM)]
	public async Task<ActionResult<ApiResp<UserResp>>> UpdateUser(
		[FromRoute] string id,
		CreateOrUpdateUserReq req,
		ILookupNormalizer lookupNormalizer)
	{
		var (authErrorResult, _) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var targetUser = await IdentityRepository.GetUserByIDAsync(id, UserFetchOptions.DEFAULT.FetchRoles().FetchClaims());
		if (targetUser == null)
		{
			return ResponseNoData(404, $"User '{id}' not found.");
		}

		var username = req.Username?.ToLower().Trim() ?? targetUser.UserName; // if not provided, keep the original username
		if (!string.IsNullOrWhiteSpace(username))
		{
			var existingUserName = await IdentityRepository.GetUserByUserNameAsync(username);
			if (existingUserName != null && !existingUserName.Id.Equals(targetUser.Id, StringComparison.InvariantCulture))
			{
				return ResponseNoData(400, $"Username '{username}' already exists.");
			}
		}

		var email = req.Email?.ToLower().Trim() ?? targetUser.Email; // if not provided, keep the original email
		if (!string.IsNullOrWhiteSpace(email))
		{
			var existingUserEmail = await IdentityRepository.GetUserByEmailAsync(email);
			if (existingUserEmail != null && !existingUserEmail.Id.Equals(targetUser.Id, StringComparison.InvariantCulture))
			{
				return ResponseNoData(400, $"Email '{email}' has been used by another user.");
			}
		}

		// verify if the claims are valid
		var uniqueClaimsNew = (req.Claims?.Distinct() ?? []).Select(c => new Claim(c.Type, c.Value)).ToList();
		if (uniqueClaimsNew.Count > 0)
		{
			var invalidClaim = uniqueClaimsNew.Where(c => !BuiltinClaims.ALL_CLAIMS.Contains(c, ClaimEqualityComparer.INSTANCE)).FirstOrDefault();
			if (invalidClaim != null)
			{
				return ResponseNoData(400, $"Claim '{invalidClaim.Type}:{invalidClaim.Value}' is not valid.");
			}
		}

		// verify if the roles are valid
		var uniqueRolesNew = (req.Roles?.Distinct() ?? []).Select(r => new KeyValuePair<string, MyPoRole?>(r, IdentityRepository.GetRoleByIDAsync(r).Result)).ToList();
		if (uniqueRolesNew.Count > 0)
		{
			var invalidRole = uniqueRolesNew.Where(r => r.Value == null).FirstOrDefault();
			if (invalidRole.Value != null)
			{
				return ResponseNoData(400, $"Role '{invalidRole.Key}' does not exist.");
			}
		}

		// first, update the user
		targetUser.UserName = username;
		targetUser.NormalizedUserName = lookupNormalizer.NormalizeName(username);
		targetUser.Email = email;
		targetUser.NormalizedEmail = lookupNormalizer.NormalizeEmail(email);
		targetUser.FamilyName = req.FamilyName?.Trim() ?? targetUser.FamilyName; // if not provided, keep the original family name
		targetUser.GivenName = req.GivenName?.Trim() ?? targetUser.GivenName; // if not provided, keep the original given name
		var iresultUpdate = await IdentityRepository.UpdateAsync(targetUser);
		if (iresultUpdate == null)
		{
			return ResponseNoData(500, $"Failed to update user.");
		}

		// then, update the roles, only if provided
		if (req.Roles is not null)
		{
			if (targetUser.Roles != null && targetUser.Roles.Any())
			{
				var iresultRemoveRoles = await IdentityRepository.RemoveFromRolesAsync(targetUser, targetUser.Roles);
				if (!iresultRemoveRoles.Succeeded)
				{
					return ResponseNoData(509, $"Failed to update user's roles: {iresultRemoveRoles} / Note: User's data was updated.");
				}
			}
			if (uniqueRolesNew.Count > 0 )
			{
				var iresultAddRoles = await IdentityRepository.AddToRolesAsync(targetUser, uniqueRolesNew.Select(r => r.Value!));
				if (!iresultAddRoles.Succeeded)
				{
					return ResponseNoData(509, $"Failed to update user's roles: {iresultAddRoles} / Note: User's data was updated.");
				}
			}

		}

		// finally, update the claims, only if provided
		if (req.Claims is not null)
		{
			if (targetUser.Claims != null && targetUser.Claims.Any())
			{
				var iresultRemoveClaims = await IdentityRepository.RemoveClaimsAsync(targetUser, targetUser.Claims.Select(c => new Claim(c.ClaimType!, c.ClaimValue!)));
				if (!IIdentityRepository.IsSucceededOrNoChangesSaved(iresultRemoveClaims))
				{
					return ResponseNoData(509, $"Failed to update user's claims: {iresultRemoveClaims} / Note: User's data was updated.");
				}
			}
			if (uniqueClaimsNew.Count > 0)
			{
				var iresultAddClaims = await IdentityRepository.AddClaimsAsync(targetUser, uniqueClaimsNew);
				if (!IIdentityRepository.IsSucceededOrNoChangesSaved(iresultAddClaims))
				{
					return ResponseNoData(509, $"Failed to update user's claims: {iresultAddClaims} / Note: User's data was updated.");
				}
			}
		}

		return ResponseOk(UserResp.BuildFromUser(targetUser));
	}

	/// <summary>
	/// Deletes a user by id.
	/// </summary>
	/// <param name="id"></param>
	/// <returns></returns>
	[HttpDelete(IApiClient.API_ENDPOINT_USERS_ID)]
	[Authorize(Policy = BuiltinPolicies.POLICY_NAME_ADMIN_ROLE_OR_DELETE_USER_PERM)]
	public async Task<ActionResult<ApiResp<UserResp>>> DeleteUser([FromRoute] string id)
	{
		var (authErrorResult, currentUser) = await VerifyAuthTokenAndCurrentUser();
		if (authErrorResult != null)
		{
			// current auth token and signed-in user should all be valid
			return authErrorResult;
		}

		var user = await IdentityRepository.GetUserByIDAsync(id);
		if (user == null)
		{
			return ResponseNoData(404, $"User '{id}' not found.");
		}
		if (user.Id.Equals(currentUser.Id, StringComparison.InvariantCulture))
		{
			return ResponseNoData(400, "You cannot delete yourself.");
		}
		var iresult = await IdentityRepository.DeleteAsync(user);
		if (!iresult.Succeeded)
		{
			return ResponseNoData(500, $"Failed to delete user: {iresult}");
		}
		return ResponseOk(UserResp.BuildFromUser(user));
	}
}
