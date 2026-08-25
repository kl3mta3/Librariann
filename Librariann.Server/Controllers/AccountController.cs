using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Librariann.API.Attributes;
using Librariann.API.Database;
using Librariann.API.Errors;
using Librariann.API.Repositories;
using Librariann.API.Services;
using Librariann.API.Services.SignalR;
using Librariann.Common;
using Librariann.Common.EnvironmentInfo;
using Librariann.Common.Extensions;
using Librariann.Common.Helpers;
using Librariann.Models.Builders;
using Librariann.Models.Constants;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.Account;
using Librariann.Models.DTOs.Email;
using Librariann.Models.DTOs.Settings;
using Librariann.Models.DTOs.SignalR;
using Librariann.Models.Entities;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Entities.Enums.User;
using Librariann.Models.Entities.User;
using Librariann.Models.Extensions;
using Librariann.Models.Mapping;
using Librariann.Server.Attributes;
using Librariann.Server.Extensions;
using Librariann.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Librariann.Server.Controllers;

/// <summary>
/// All Account matters
/// </summary>
/// <inheritdoc />
public class AccountController(UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    ITokenService tokenService, IUnitOfWork unitOfWork,
    ILogger<AccountController> logger,
    IAccountService accountService,
    IEmailService emailService, IEventHub eventHub,
    ILocalizationService localizationService,
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IAuthKeyService authKeyService, IOidcService oidcService) : BaseApiController
{
    // Hardcoded to avoid localization multiple enumeration: https://github.com/Kareadita/Librariann/issues/2829
    private const string BadCredentialsMessage = "Your credentials are not correct";

    /// <summary>
    /// Returns true if OIDC authentication cookies are present and the <see cref="IdentityServiceExtensions.OpenIdConnect"/>
    /// scheme has been registered
    /// </summary>
    /// <remarks>Makes no guarantee about their validity</remarks>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet("oidc-authenticated")]
    public async Task<ActionResult<bool>> OidcAuthenticated()
    {
        var oidcScheme = await authenticationSchemeProvider.GetSchemeAsync(IdentityServiceExtensions.OpenIdConnect);
        return Ok(oidcScheme != null && HttpContext.Request.Cookies.ContainsKey(OidcService.CookieName));
    }

    /// <summary>
    /// Remove the OIDC link for the authenticated user. This action will also remove the authentication cookie.
    /// The caller should take note and redirect to login if no other authentication is currently present (I.e. JWT)
    /// </summary>
    /// <returns></returns>
    [HttpPost("clear-oidc-link")]
    public async Task<IActionResult> ClearOidcLink()
    {
        await oidcService.ClearOidcIdForUser(UserId, HttpContext.RequestAborted);

        // OIDC is no longer connected, remove cookie
        if (HttpContext.Request.Cookies.ContainsKey(OidcService.CookieName))
        {
            HttpContext.Response.Cookies.Delete(OidcService.CookieName);
        }

        return Ok();
    }

    /// <summary>
    /// Returns the current user, as it would from login
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UnauthorizedAccessException"></exception>
    /// <remarks>Does not return tokens for the user</remarks>
    [HttpGet]
    public async Task<ActionResult<UserDto>> GetCurrentUserAsync()
    {
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(UserId, AppUserIncludes.UserPreferences | AppUserIncludes.SideNavStreams | AppUserIncludes.AuthKeys);
        if (user == null) throw new UnauthorizedAccessException();

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(PolicyConstants.LoginRole) && !roles.Contains(PolicyConstants.AdminRole)) return Unauthorized(await localizationService.TranslateAsync(user.Id, "disabled-account"));

        return Ok(await ConstructUserDto(user, roles, false));
    }

    /// <summary>
    /// Update a user's password
    /// </summary>
    /// <param name="resetPasswordDto"></param>
    /// <returns></returns>
    [HttpPost("reset-password")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> UpdatePassword(ResetPasswordDto resetPasswordDto)
    {
        var user = await userManager.Users.SingleOrDefaultAsync(x => x.UserName == resetPasswordDto.UserName);
        if (user == null) return Ok(); // Don't report BadRequest as that would allow brute forcing to find accounts on system

        logger.LogInformation("{UserName} is changing {ResetUser}'s password", Username!, resetPasswordDto.UserName.Sanitize());
        var isAdmin = User.IsInRole(PolicyConstants.AdminRole);

        if (resetPasswordDto.UserName == Username! && !(User.IsInRole(PolicyConstants.ChangePasswordRole) || isAdmin))
            return Unauthorized(await localizationService.TranslateAsync(UserId, "permission-denied"));

        if (resetPasswordDto.UserName != Username! && !isAdmin)
            return Unauthorized(await localizationService.TranslateAsync(UserId, "permission-denied"));

        if (string.IsNullOrEmpty(resetPasswordDto.OldPassword) && !isAdmin)
            return BadRequest(
                new ApiException(400,
                    await localizationService.TranslateAsync(UserId, "password-required")));

        var oidcConfig = (await unitOfWork.SettingsRepository.GetSettingsDtoAsync()).OidcConfig;
        if (user.IdentityProvider == IdentityProvider.OpenIdConnect  && oidcConfig is {Enabled: true, SyncUserSettings: true})
        {
            return BadRequest(await localizationService.TranslateAsync(user.Id, "oidc-managed"));
        }

        // If you're an admin and the username isn't yours, you don't need to validate the password
        var isResettingOtherUser = (resetPasswordDto.UserName != Username! && isAdmin);
        if (!isResettingOtherUser && !await userManager.CheckPasswordAsync(user, resetPasswordDto.OldPassword))
        {
            return BadRequest(await localizationService.TranslateAsync(UserId, "invalid-password"));
        }

        var errors = await accountService.ChangeUserPassword(user, resetPasswordDto.Password);
        if (errors.Any())
        {
            return BadRequest(errors);
        }

        logger.LogInformation("{User}'s Password has been reset", user.UserName!.Sanitize());
        return Ok();
    }

    /// <summary>
    /// Register the first user (admin) on the server. Will not do anything if an admin is already confirmed
    /// </summary>
    /// <param name="registerDto"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> RegisterFirstUser(RegisterDto registerDto)
    {
        var admins = await userManager.GetUsersInRoleAsync(PolicyConstants.AdminRole);
        if (admins.Count > 0) return BadRequest(await localizationService.GetAsync("en", "denied"));

        try
        {
            var usernameValidation = await accountService.ValidateUsername(registerDto.Username);
            if (usernameValidation.Any())
            {
                return BadRequest(usernameValidation);
            }

            // If Email is empty, default to the username
            if (string.IsNullOrEmpty(registerDto.Email))
            {
                registerDto.Email = registerDto.Username;
            }

            var user = new AppUserBuilder(registerDto.Username, registerDto.Email).Build();

            var result = await userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded) return BadRequest(result.Errors);

            await accountService.SeedUser(user);

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            if (string.IsNullOrEmpty(token)) return BadRequest(await localizationService.GetAsync("en", "confirm-token-gen"));
            if (!await ConfirmEmailToken(token, user)) return BadRequest(await localizationService.GetAsync("en",  "validate-email", token));


            var roleResult = await userManager.AddToRoleAsync(user, PolicyConstants.AdminRole);
            if (!roleResult.Succeeded) return BadRequest(result.Errors);
            await userManager.AddToRoleAsync(user, PolicyConstants.LoginRole);

            return Ok(await ConstructUserDto(user!, await userManager.GetRolesAsync(user!), ct: HttpContext.RequestAborted));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Something went wrong when registering user");
            // We need to manually delete the User as we've already committed
            var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(registerDto.Username);
            unitOfWork.UserRepository.Delete(user);
            await unitOfWork.CommitAsync();
        }

        return BadRequest(await localizationService.GetAsync("en",  "register-user"));
    }


    /// <summary>
    /// Perform a login. Will send JWT Token of the logged in user back.
    /// </summary>
    /// <param name="loginDto"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("Authentication")]
    public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
    {
        AppUser? user;
        if (!string.IsNullOrEmpty(loginDto.ApiKey))
        {
            user = await unitOfWork.UserRepository.GetUserByAuthKey(loginDto.ApiKey, HttpContext.RequestAborted);
        }
        else
        {
            user = await userManager.Users
                .Include(u => u.UserPreferences)
                .Include(u => u.AuthKeys)
                .AsSplitQuery()
                .SingleOrDefaultAsync(x => x.NormalizedUserName == loginDto.Username.ToUpperInvariant());
        }

        logger.LogInformation("{UserName} attempting to login from {IpAddress}", loginDto.Username.Sanitize(), HttpContext.Connection.RemoteIpAddress?.ToString());

        if (user == null)
        {
            logger.LogWarning("Attempted login by {UserName} failed due to unable to find account", loginDto.Username.Sanitize());
            return Unauthorized(BadCredentialsMessage);
        }
        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(PolicyConstants.LoginRole)) return Unauthorized(await localizationService.TranslateAsync(user.Id, "disabled-account"));

        var oidcConfig = (await unitOfWork.SettingsRepository.GetSettingsDtoAsync()).OidcConfig;
        // Setting only takes effect if OIDC is functional, and if we're not logging in via ApiKey
        var disablePasswordAuthentication = oidcConfig is {Enabled: true, DisablePasswordAuthentication: true} && string.IsNullOrEmpty(loginDto.ApiKey);
        if (disablePasswordAuthentication) return Unauthorized(await localizationService.TranslateAsync(user.Id, "password-authentication-disabled"));

        if (string.IsNullOrEmpty(loginDto.ApiKey))
        {
            var result = await signInManager
                .CheckPasswordSignInAsync(user, loginDto.Password, true);

            if (result.IsLockedOut)
            {
                await userManager.UpdateSecurityStampAsync(user);
                var errorStr = await localizationService.TranslateAsync(user.Id, "locked-out");
                logger.LogWarning("{UserName} failed to log in at {Time}: {Issue}", user.UserName!.Sanitize(), user.LastActive,
                    errorStr);
                return Unauthorized(errorStr);
            }

            if (!result.Succeeded)
            {
                var errorStr = result.IsNotAllowed
                                ? await localizationService.TranslateAsync(user.Id, "confirm-email")
                                : BadCredentialsMessage;
                logger.LogWarning("{UserName} failed to log in at {Time}: {Issue}", user.UserName!.Sanitize(), user.LastActive, errorStr);
                return Unauthorized(errorStr);
            }
        }

        unitOfWork.UserRepository.Update(user);
        await unitOfWork.CommitAsync();

        logger.LogInformation("{UserName} logged in at {Time}", user.UserName!.Sanitize(), user.LastActive);

        return Ok(await ConstructUserDto(user, roles, ct: HttpContext.RequestAborted));
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="user">Must include <see cref="AppUser.AuthKeys"/></param>
    /// <param name="roles"></param>
    /// <param name="includeTokens"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task<UserDto> ConstructUserDto(AppUser user, IList<string> roles, bool includeTokens = true, CancellationToken ct = default)
    {
        // TODO: Clean this up to be streamlined
        // UserMapping.ToUserDto() reads user.UserRoles (no default initializer on the entity - null unless
        // .Include()d or, for a freshly-built AppUser like on registration, never set at all). Its value is
        // always overwritten by the real `roles` parameter a few lines below, so it's safe to default here rather
        // than force every caller to Include a navigation whose mapped value is immediately discarded anyway.
        user.UserRoles ??= [];
        var dto = user.ToUserDto();

        if (includeTokens)
        {
            dto.Token = await tokenService.CreateToken(user, ct);
            dto.RefreshToken = await tokenService.CreateRefreshToken(user, ct);
        }

        dto.Roles = roles;
        dto.LibrariannVersion = BuildInfo.Version.ToString();
        dto.ApiKey = user.GetOpdsAuthKey();
        dto.AuthKeys = user.AuthKeys.Select(k => k.ToAuthKeyDto()).ToList();

        var pref = await unitOfWork.UserRepository.GetPreferencesAsync(user.UserName!, ct);
        if (pref != null)
        {
            dto.Preferences = pref.ToUserPreferencesDto();
        }

        return dto;
    }

    /// <summary>
    /// Returns an up-to-date user account
    /// </summary>
    /// <returns></returns>
    [HttpGet("refresh-account")]
    public async Task<ActionResult<UserDto>> RefreshAccount()
    {
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(UserId, AppUserIncludes.UserPreferences | AppUserIncludes.AuthKeys);
        if (user == null) return Unauthorized();

        var roles = await userManager.GetRolesAsync(user);

        return Ok(await ConstructUserDto(user, roles, !HttpContext.Request.Cookies.ContainsKey(OidcService.CookieName), HttpContext.RequestAborted));
    }

    /// <summary>
    /// Refreshes the user's JWT token
    /// </summary>
    /// <param name="tokenRequestDto"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [SkipDeviceTracking]
    [HttpPost("refresh-token")]
    public async Task<ActionResult<TokenRequestDto>> RefreshToken([FromBody] TokenRequestDto tokenRequestDto)
    {
        var token = await tokenService.ValidateRefreshToken(tokenRequestDto);
        if (token == null)
        {
            return Unauthorized(new { message = await localizationService.GetAsync("en", "invalid-token") });
        }

        return Ok(token);
    }

    /// <summary>
    /// Get All Roles back. See <see cref="PolicyConstants"/>
    /// </summary>
    /// <returns></returns>
    [HttpGet("roles")]
    public ActionResult<IList<string>> GetRoles()
    {
        return typeof(PolicyConstants)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .ToDictionary(f => f.Name,
                f => (string) f.GetValue(null)!).Values.ToList();
    }

    /// <summary>
    /// Initiates the flow to update a user's username.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("update/username")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult<InviteUserResponse>> UpdateUsername(UpdateUsernameRequestDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(Username!, ct: ct);
        if (user == null || string.IsNullOrEmpty(dto.Username))
            return BadRequest(await localizationService.TranslateAsync(UserId, "invalid-payload"));

        var oidcConfig = (await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct)).OidcConfig;
        if (user.IdentityProvider == IdentityProvider.OpenIdConnect  && oidcConfig is {Enabled: true, SyncUserSettings: true})
        {
            return BadRequest(await localizationService.TranslateAsync(user.Id, "oidc-managed"));
        }

        // Validate they are different
        if (user.UserName == dto.Username) return Ok();

        // Validate valid characters in the username and no other user's have this username - they must be unique
        var validationError = (await accountService.ValidateUsername(dto.Username, ct)).FirstOrDefault();
        if (validationError != null)
        {
            return BadRequest(await localizationService.TranslateAsync(validationError.Message!));
        }

        // If Valid, we will make the change then email the user to inform them (no confirmation needed)
        var oldUsername = user.UserName;
        user.UserName = dto.Username.Sanitize();
        await unitOfWork.CommitAsync(ct);

        await eventHub.SendMessageToAsync(MessageFactory.UserUpdate, MessageFactory.UserUpdateEvent(user.Id, oldUsername),
            user.Id, HttpContext.RequestAborted);

        var isValidEmailAddress = emailService.IsValidEmail(user.Email);
        var serverSettings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        var shouldEmailUser = serverSettings.IsEmailSetup() || !isValidEmailAddress;

        logger.LogInformation("[Update Username]: {User} has changed their username to {UserName}", Username!, dto.Username.Sanitize());
        if (!shouldEmailUser) return Ok();

        try
        {
            // Email the old address of the update change
            var invitingUser = (await unitOfWork.UserRepository.GetAdminUsersAsync(ct)).First().UserName!;
            BackgroundJob.Enqueue(() => emailService.SendUsernameChangeEmail(new UsernameChangeEmailDto()
            {
                EmailAddress = user.Email,
                LocaleUserId = user.Id,
                InvitingUser = invitingUser
            }));
        }
        catch (Exception)
        {
            /* Swallow exception */
        }

        return Ok();
    }


    /// <summary>
    /// Initiates the flow to update a user's email address.
    ///
    /// If email is not setup, then the email address is not changed in this API. A confirmation link is sent/dumped which will
    /// validate the email. It must be confirmed for the email to update.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns>Returns just if the email was sent or server isn't reachable</returns>
    [HttpPost("update/email")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult<InviteUserResponse>> UpdateEmail(UpdateEmailDto? dto)
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(Username!, ct: ct);
        if (user == null || dto == null || string.IsNullOrEmpty(dto.Email) || string.IsNullOrEmpty(dto.Password))
            return BadRequest(await localizationService.TranslateAsync(UserId, "invalid-payload"));

        var oidcConfig = (await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct)).OidcConfig;
        if (user.IdentityProvider == IdentityProvider.OpenIdConnect  && oidcConfig is {Enabled: true, SyncUserSettings: true})
        {
            return BadRequest(await localizationService.TranslateAsync(user.Id, "oidc-managed"));
        }

        // Validate this user's password
        if (!await userManager.CheckPasswordAsync(user, dto.Password))
        {
            logger.LogWarning("A user tried to change {UserName}'s email, but password didn't validate", user.UserName!.Sanitize());
            return BadRequest(await localizationService.TranslateAsync(UserId, "permission-denied"));
        }

        // Validate no other users exist with this email
        if (user.Email!.Equals(dto.Email))
            return BadRequest(await localizationService.TranslateAsync(UserId, "nothing-to-do"));

        // Check if email is used by another user
        var existingUserEmail = await unitOfWork.UserRepository.GetUserByEmailAsync(dto.Email, ct: ct);
        if (existingUserEmail != null)
        {
            return BadRequest(await localizationService.TranslateAsync(UserId, "share-multiple-emails"));
        }

        // All validations complete, generate a new token and email it to the user at the new address. Confirm email link will update the email
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        if (string.IsNullOrEmpty(token))
        {
            logger.LogError("There was an issue generating a token for the email");
            return BadRequest(await localizationService.TranslateAsync(UserId, "generate-token"));
        }

        var isValidEmailAddress = emailService.IsValidEmail(user.Email);
        var serverSettings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        var shouldEmailUser = serverSettings.IsEmailSetup() || !isValidEmailAddress;

        user.EmailConfirmed = !shouldEmailUser;
        user.ConfirmationToken = token;
        await userManager.UpdateAsync(user);

        var emailLink = await emailService.GenerateEmailLink(Request, user.ConfirmationToken, "confirm-email-update", dto.Email);
        logger.LogInformation("Generated an email-change confirmation for {UserName}", user.UserName!.Sanitize());

        if (!shouldEmailUser)
        {
            logger.LogInformation("Cannot email admin, email not setup or admin email invalid");
            return Ok(new InviteUserResponse
            {
                EmailLink = string.Empty,
                EmailSent = false,
                InvalidEmail = !isValidEmailAddress
            });
        }


        // Send a confirmation email
        try
        {
            if (!isValidEmailAddress)
            {
                logger.LogCritical("[Update Email]: User is trying to update their email, but their existing email ({Email}) isn't valid. No email will be send", user.Email);
                return Ok(new InviteUserResponse
                {
                    EmailLink = string.Empty,
                    EmailSent = false,
                    InvalidEmail = true,
                });
            }


            try
            {
                var invitingUser = (await unitOfWork.UserRepository.GetAdminUsersAsync(ct)).First().UserName!;
                // Email the old address of the update change
                BackgroundJob.Enqueue(() => emailService.SendEmailChangeEmail(new ConfirmationEmailDto()
                {
                    EmailAddress = string.IsNullOrEmpty(user.Email) ? dto.Email : user.Email,
                    LocaleUserId = user.Id,
                    InvitingUser = invitingUser,
                    ServerConfirmationLink = emailLink
                }));
            }
            catch (Exception)
            {
                /* Swallow exception */
            }

            return Ok(new InviteUserResponse
            {
                EmailLink = string.Empty,
                EmailSent = true,
                InvalidEmail = !isValidEmailAddress
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an error during invite user flow, unable to send an email");
        }

        await eventHub.SendMessageToAsync(MessageFactory.UserUpdate, MessageFactory.UserUpdateEvent(user.Id, user.UserName!), user.Id, ct);

        return Ok();
    }

    /// <summary>
    /// Change the Age Rating restriction for the user
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("update/age-restriction")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> UpdateAgeRestriction(UpdateAgeRestrictionDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(Username!, ct: ct);
        if (user == null) return Unauthorized(await localizationService.TranslateAsync(UserId, "permission-denied"));

        var oidcConfig = (await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct)).OidcConfig;
        if (user.IdentityProvider == IdentityProvider.OpenIdConnect  && oidcConfig is {Enabled: true, SyncUserSettings: true})
        {
            return BadRequest(await localizationService.TranslateAsync(user.Id, "oidc-managed"));
        }

        var isAdmin = await unitOfWork.UserRepository.IsUserAdminAsync(user, ct);
        var hasRole = await accountService.CanChangeAgeRestriction(user, ct);
        if (!hasRole) return BadRequest(await localizationService.TranslateAsync(UserId, "permission-denied"));

        user.AgeRestriction = isAdmin ? AgeRating.NotApplicable : dto.AgeRating;
        user.AgeRestrictionIncludeUnknowns = isAdmin || dto.IncludeUnknowns;

        unitOfWork.UserRepository.Update(user);

        if (!unitOfWork.HasChanges()) return Ok();
        try
        {
            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an error updating the age restriction");
            return BadRequest(await localizationService.TranslateAsync(UserId, "age-restriction-update"));
        }

        await eventHub.SendMessageToAsync(MessageFactory.UserUpdate, MessageFactory.UserUpdateEvent(user.Id, user.UserName!), user.Id, ct);

        return Ok();
    }

    /// <summary>
    /// Update the user account. This can only affect Username, Email (will require confirming), Roles, and Library access.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    /// <remarks>Users who's <see cref="AppUser.IdentityProvider"/> is not <see cref="IdentityProvider.Librariann"/> cannot be edited if <see cref="OidcConfigDto.SyncUserSettings"/> is true</remarks>
    [HttpPost("update")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> UpdateAccount(UpdateUserDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var adminUser = await unitOfWork.UserRepository.GetUserByUsernameAsync(Username!, ct: ct);
        if (adminUser == null) return Unauthorized();
        if (!await unitOfWork.UserRepository.IsUserAdminAsync(adminUser, ct)) return Unauthorized(await localizationService.TranslateAsync(UserId, "permission-denied"));

        var user = await unitOfWork.UserRepository.GetUserByIdAsync(dto.UserId, AppUserIncludes.SideNavStreams, ct);
        if (user == null) return BadRequest(await localizationService.TranslateAsync(UserId, "no-user"));

        try
        {
            if (await accountService.ChangeIdentityProvider(UserId, user, dto.IdentityProvider, ct)) return Ok();
        }
        catch (LibrariannException exception)
        {
            return BadRequest(exception.Message);
        }

        // Check if username is changing
        if (!user.UserName!.Equals(dto.Username))
        {
            // Validate username change
            var errors = await accountService.ValidateUsername(dto.Username, ct);
            if (errors.Any()) return BadRequest(await localizationService.TranslateAsync(UserId, "username-taken"));
            user.UserName = dto.Username;
            await userManager.UpdateNormalizedUserNameAsync(user);
            unitOfWork.UserRepository.Update(user);
        }

        // Check if email is changing for a non-admin user
        var isUpdatingAnotherAccount = user.Id != adminUser.Id;
        if (isUpdatingAnotherAccount && !string.IsNullOrEmpty(dto.Email) && user.Email != dto.Email)
        {
            // Validate username change
            var errors = await accountService.ValidateEmail(dto.Email, ct);
            if (errors.Any()) return BadRequest(await localizationService.TranslateAsync(UserId, "email-taken"));

            user.Email = dto.Email;
            user.EmailConfirmed = true; // When an admin performs the flow, we assume the email address is able to receive data

            await userManager.UpdateNormalizedEmailAsync(user);
            unitOfWork.UserRepository.Update(user);
        }

        // Update roles
        var existingRoles = await userManager.GetRolesAsync(user);
        var hasAdminRole = dto.Roles.Contains(PolicyConstants.AdminRole);
        if (!hasAdminRole)
        {
            dto.Roles.Add(PolicyConstants.UserRole);
        }

        if (existingRoles.Except(dto.Roles).Any() || dto.Roles.Except(existingRoles).Any())
        {
            var roles = dto.Roles;

            var roleResult = await userManager.RemoveFromRolesAsync(user, existingRoles);
            if (!roleResult.Succeeded) return BadRequest(roleResult.Errors);
            roleResult = await userManager.AddToRolesAsync(user, roles);
            if (!roleResult.Succeeded) return BadRequest(roleResult.Errors);
        }

        // We might want to check if they had admin and no longer, if so:
        // await _userManager.UpdateSecurityStampAsync(user); to force them to re-authenticate


        var allLibraries = (await unitOfWork.LibraryRepository.GetLibrariesAsync(ct: ct)).ToList();
        List<Library> libraries;
        if (hasAdminRole)
        {
            logger.LogInformation("{UserId} is being registered as admin. Granting access to all libraries",
                user.Id);
            libraries = allLibraries;
        }
        else
        {
            // Remove user from all libraries
            foreach (var lib in allLibraries)
            {
                lib.AppUsers ??= [];
                lib.AppUsers.Remove(user);
                user.RemoveSideNavFromLibrary(lib);
            }

            libraries = (await unitOfWork.LibraryRepository.GetLibraryForIdsAsync(dto.Libraries, LibraryIncludes.AppUser, ct)).ToList();
        }

        foreach (var lib in libraries)
        {
            lib.AppUsers ??= [];
            lib.AppUsers.Add(user);
            user.CreateSideNavFromLibrary(lib);
        }

        user.AgeRestriction = hasAdminRole ? AgeRating.NotApplicable : dto.AgeRestriction.AgeRating;
        user.AgeRestrictionIncludeUnknowns = hasAdminRole || dto.AgeRestriction.IncludeUnknowns;

        unitOfWork.UserRepository.Update(user);

        if (!unitOfWork.HasChanges() || await unitOfWork.CommitAsync(ct))
        {
            await eventHub.SendMessageToAsync(MessageFactory.UserUpdate, MessageFactory.UserUpdateEvent(user.Id, user.UserName), user.Id, ct);
            await eventHub.SendMessageToAsync(MessageFactory.SideNavUpdate, MessageFactory.SideNavUpdateEvent(user.Id), user.Id, ct);
            // If we adjust library access, dashboards should re-render
            await eventHub.SendMessageToAsync(MessageFactory.DashboardUpdate, MessageFactory.DashboardUpdateEvent(user.Id), user.Id, ct);
            return Ok();
        }

        await unitOfWork.RollbackAsync(ct);
        return BadRequest(await localizationService.TranslateAsync(UserId, "generic-user-update"));
    }

    /// <summary>
    /// Requests the Invite Url for the AppUserId. Will return error if user is already validated.
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="withBaseUrl">Include the "https://ip:port/" in the generated link</param>
    /// <returns></returns>
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    [HttpGet("invite-url")]
    public async Task<ActionResult<string>> GetInviteUrl(int userId, bool withBaseUrl)
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId, ct: ct);
        if (user == null) return Unauthorized();
        if (user.EmailConfirmed)
            return BadRequest(await localizationService.TranslateAsync(UserId, "user-already-confirmed"));
        if (string.IsNullOrEmpty(user.ConfirmationToken))
            return BadRequest(await localizationService.TranslateAsync(UserId, "manual-setup-fail"));

        return await emailService.GenerateEmailLink(Request, user.ConfirmationToken, "confirm-email", user.Email!, withBaseUrl);
    }


    /// <summary>
    /// Invites a user to the server. Will generate a setup link for continuing setup. If email is not setup, a link will be presented to user to continue setup.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    [HttpPost("invite")]
    public async Task<ActionResult<string>> InviteUser(InviteUserDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var userId = UserId;
        var adminUser = await unitOfWork.UserRepository.GetUserByIdAsync(userId, ct: ct);
        if (adminUser == null) return Unauthorized(await localizationService.TranslateAsync(userId, "permission-denied"));

        dto.Email = dto.Email.Trim();
        if (string.IsNullOrEmpty(dto.Email)) return BadRequest(await localizationService.TranslateAsync(userId, "invalid-payload"));

        return await CreateAndSendInviteAsync(dto.Email, dto.Roles, dto.Libraries, dto.AgeRestriction, adminUser, ct);
    }

    /// <summary>
    /// Creates the account, assigns roles/libraries, generates the confirmation token, and either mails the
    /// invite or returns the raw link (email not configured). Shared by the manual invite endpoint, approving a
    /// pending invite request, and auto-accepting one.
    /// </summary>
    private async Task<ActionResult<string>> CreateAndSendInviteAsync(string email, ICollection<string> rolesInput,
        IList<int> libraryIds, AgeRestrictionDto ageRestriction, AppUser adminUser, CancellationToken ct)
    {
        // Use adminUser's id rather than the ambient UserId - this helper is also called from the anonymous
        // auto-accept request-invite path, which has no authenticated caller to resolve a locale against.
        var userId = adminUser.Id;
        logger.LogInformation("{User} is inviting {Email} to the server", adminUser.UserName, email);

        // Check if there is an existing invite
        var emailValidationErrors = await accountService.ValidateEmail(email, ct);
        if (emailValidationErrors.Any())
        {
            var invitedUser = await unitOfWork.UserRepository.GetUserByEmailAsync(email, ct: ct);
            if (await userManager.IsEmailConfirmedAsync(invitedUser!))
                return BadRequest(await localizationService.TranslateAsync(userId, "user-already-registered", invitedUser!.UserName));
            return BadRequest(await localizationService.TranslateAsync(userId, "user-already-invited"));
        }

        // Create a new user
        var user = new AppUserBuilder(email, email).Build();
        unitOfWork.UserRepository.Add(user);

        try
        {
            // Invitations must not create an account with a shared or otherwise known temporary password.
            // The confirmation flow validates the invitation token before setting the user's chosen password.
            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            await accountService.SeedUser(user, ct);

            // Assign Roles - copy the input so callers reusing the same default permission set across multiple
            // invites (e.g. Invite All) don't accumulate roles onto their own list across iterations.
            var roles = new List<string>(rolesInput);
            var hasAdminRole = roles.Contains(PolicyConstants.AdminRole);
            if (!hasAdminRole)
            {
                roles.Add(PolicyConstants.UserRole);
            }
            else
            {
                roles.Remove(PolicyConstants.ReadOnlyRole);
            }

            foreach (var role in roles)
            {
                if (!PolicyConstants.ValidRoles.Contains(role)) continue;
                var roleResult = await userManager.AddToRoleAsync(user, role);
                if (!roleResult.Succeeded)
                    return BadRequest(roleResult.Errors);
            }

            // Grant access to libraries
            List<Library> libraries;
            if (hasAdminRole)
            {
                logger.LogInformation("{UserName} is being registered as admin. Granting access to all libraries",
                    user.UserName?.Sanitize());
                libraries = (await unitOfWork.LibraryRepository.GetLibrariesAsync(LibraryIncludes.AppUser, ct: ct)).ToList();
            }
            else
            {
                libraries = (await unitOfWork.LibraryRepository.GetLibraryForIdsAsync(libraryIds, LibraryIncludes.AppUser, ct)).ToList();
            }

            foreach (var lib in libraries)
            {
                lib.AppUsers ??= [];
                lib.AppUsers.Add(user);
                user.CreateSideNavFromLibrary(lib);
            }

            user.AgeRestriction = hasAdminRole ? AgeRating.NotApplicable : ageRestriction.AgeRating;
            user.AgeRestrictionIncludeUnknowns = hasAdminRole || ageRestriction.IncludeUnknowns;

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            if (string.IsNullOrEmpty(token))
            {
                logger.LogError("There was an issue generating a token for the email");
                return BadRequest(await localizationService.TranslateAsync(userId, "generic-invite-user"));
            }

            user.ConfirmationToken = token;
            unitOfWork.UserRepository.Update(user);
            await unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an error during invite user flow, unable to create user. Deleting user for retry");
            unitOfWork.UserRepository.Delete(user);
            await unitOfWork.CommitAsync(ct);
            return BadRequest(await localizationService.TranslateAsync(userId, "generic-invite-user"));
        }

        try
        {
            var emailLink = await emailService.GenerateEmailLink(Request, user.ConfirmationToken, "confirm-email", email);
            logger.LogInformation("Generated an invitation confirmation for {UserName}", user.UserName?.Sanitize());

            var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
            if (!emailService.IsValidEmail(email) || !settings.IsEmailSetup())
            {
                logger.LogInformation("[Invite User] {Email} doesn't appear to be an email or email is not setup", email.Sanitize());
                return Ok(new InviteUserResponse
                {
                    EmailLink = emailLink,
                    EmailSent = false,
                    InvalidEmail = true
                });
            }

            BackgroundJob.Enqueue(() => emailService.SendInviteEmail(new ConfirmationEmailDto()
            {
                EmailAddress = email,
                LocaleUserId = adminUser.Id, // Use the admin's locale for the invite to server
                InvitingUser = adminUser.UserName,
                ServerConfirmationLink = emailLink
            }));

            return Ok(new InviteUserResponse
            {
                EmailLink = emailLink,
                EmailSent = true
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an error during invite user flow, unable to send an email");
        }

        return BadRequest(await localizationService.TranslateAsync(userId, "generic-invite-user"));
    }

    /// <summary>
    /// Submitted anonymously from the login screen's "Request an Invite" link. Either queues the request for
    /// admin review, or - if AutoAcceptInviteRequests is on - immediately creates and mails the invite.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("request-invite")]
    [EnableRateLimiting("Authentication")]
    public async Task<ActionResult<RequestInviteResponseDto>> RequestInvite(RequestInviteDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        if (!settings.ShowRequestInviteLink) return NotFound();

        dto.Email = dto.Email.Trim();
        if (string.IsNullOrEmpty(dto.Email) || !emailService.IsValidEmail(dto.Email))
        {
            return BadRequest(await localizationService.TranslateAsync("invalid-payload"));
        }

        // Don't let a request pile up for an email that's already a registered/pending user - same duplicate
        // check the manual invite flow uses, just without exposing which case it is to an anonymous caller.
        var emailValidationErrors = await accountService.ValidateEmail(dto.Email, ct);
        if (emailValidationErrors.Any())
        {
            return Ok(new RequestInviteResponseDto { AutoAccepted = false });
        }

        if (settings.AutoAcceptInviteRequests && settings.IsEmailSetup())
        {
            var defaultAdmin = await unitOfWork.UserRepository.GetDefaultAdminUser();
            var permissions = settings.DefaultInvitePermissions;
            await CreateAndSendInviteAsync(dto.Email, permissions.Roles, permissions.Libraries,
                permissions.AgeRestriction, defaultAdmin, ct);
            return Ok(new RequestInviteResponseDto { AutoAccepted = true });
        }

        var existingRequest = await unitOfWork.InviteRequestRepository.GetByEmailAsync(dto.Email, ct);
        if (existingRequest == null)
        {
            unitOfWork.InviteRequestRepository.Add(new AppUserInviteRequest
            {
                Email = dto.Email,
                Name = dto.Name
            });
            await unitOfWork.CommitAsync(ct);
        }

        return Ok(new RequestInviteResponseDto { AutoAccepted = false });
    }

    /// <summary>
    /// Lists pending invite requests, oldest first, for the Users settings tab.
    /// </summary>
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    [HttpGet("pending-invites")]
    public async Task<ActionResult<IList<InviteRequestDto>>> GetPendingInvites()
    {
        var requests = await unitOfWork.InviteRequestRepository.GetAllAsync(HttpContext.RequestAborted);
        return Ok(requests.Select(r => new InviteRequestDto
        {
            Id = r.Id,
            Email = r.Email,
            Name = r.Name,
            RequestedUtc = r.CreatedUtc
        }).ToList());
    }

    /// <summary>
    /// Approves a single pending invite request using the server's default invite permissions - the same path
    /// the manual invite button uses, just pre-filled from the request instead of an admin-typed email.
    /// </summary>
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    [HttpPost("approve-invite/{id:int}")]
    public async Task<ActionResult<string>> ApproveInviteRequest(int id)
    {
        var ct = HttpContext.RequestAborted;
        var userId = UserId;
        var adminUser = await unitOfWork.UserRepository.GetUserByIdAsync(userId, ct: ct);
        if (adminUser == null) return Unauthorized(await localizationService.TranslateAsync(userId, "permission-denied"));

        var request = await unitOfWork.InviteRequestRepository.GetByIdAsync(id, ct);
        if (request == null) return BadRequest(await localizationService.TranslateAsync(userId, "invalid-payload"));

        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        var permissions = settings.DefaultInvitePermissions;
        var result = await CreateAndSendInviteAsync(request.Email, permissions.Roles, permissions.Libraries,
            permissions.AgeRestriction, adminUser, ct);

        if (result.Result is OkObjectResult)
        {
            unitOfWork.InviteRequestRepository.Delete(request);
            await unitOfWork.CommitAsync(ct);
        }

        return result;
    }

    /// <summary>
    /// Processes every pending invite request in order, using the default invite permissions. Requires email to
    /// be configured, same as the frontend's disabled state on this button - there is no meaningful "here's your
    /// link" fallback for a batch of unattended invites.
    /// </summary>
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    [HttpPost("invite-all-pending")]
    public async Task<ActionResult<int>> InviteAllPending()
    {
        var ct = HttpContext.RequestAborted;
        var userId = UserId;
        var adminUser = await unitOfWork.UserRepository.GetUserByIdAsync(userId, ct: ct);
        if (adminUser == null) return Unauthorized(await localizationService.TranslateAsync(userId, "permission-denied"));

        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        if (!settings.IsEmailSetup()) return BadRequest(await localizationService.TranslateAsync(userId, "email-settings-invalid"));

        var permissions = settings.DefaultInvitePermissions;
        var requests = await unitOfWork.InviteRequestRepository.GetAllAsync(ct);
        var invitedCount = 0;

        foreach (var request in requests)
        {
            var result = await CreateAndSendInviteAsync(request.Email, permissions.Roles, permissions.Libraries,
                permissions.AgeRestriction, adminUser, ct);

            if (result.Result is not OkObjectResult) continue;

            unitOfWork.InviteRequestRepository.Delete(request);
            await unitOfWork.CommitAsync(ct);
            invitedCount++;
        }

        return Ok(invitedCount);
    }

    /// <summary>
    /// The login screen's "Request an Invite" link visibility - the only piece of this feature anonymous
    /// visitors need to know about.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("invite-request-enabled")]
    public async Task<ActionResult<bool>> IsInviteRequestEnabled()
    {
        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(HttpContext.RequestAborted);
        return Ok(settings.ShowRequestInviteLink);
    }

    /// <summary>
    /// The Users settings tab's invite toggles and default permission set.
    /// </summary>
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    [HttpGet("invite-settings")]
    public async Task<ActionResult<UserInviteSettingsDto>> GetInviteSettings()
    {
        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(HttpContext.RequestAborted);
        return Ok(new UserInviteSettingsDto
        {
            ShowRequestInviteLink = settings.ShowRequestInviteLink,
            AutoAcceptInviteRequests = settings.AutoAcceptInviteRequests,
            DefaultInvitePermissions = settings.DefaultInvitePermissions
        });
    }

    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    [HttpPost("invite-settings")]
    public async Task<ActionResult<UserInviteSettingsDto>> UpdateInviteSettings(UserInviteSettingsDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);

        // AutoAccept can only be enabled when email is actually set up - it has no reviewer to fall back on.
        var autoAccept = dto.AutoAcceptInviteRequests && settings.IsEmailSetup();

        var showLinkSetting = await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.ShowRequestInviteLink, ct);
        showLinkSetting.Value = dto.ShowRequestInviteLink + string.Empty;
        unitOfWork.SettingsRepository.Update(showLinkSetting);

        var autoAcceptSetting = await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.AutoAcceptInviteRequests, ct);
        autoAcceptSetting.Value = autoAccept + string.Empty;
        unitOfWork.SettingsRepository.Update(autoAcceptSetting);

        var permissionsSetting = await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.DefaultInvitePermissions, ct);
        permissionsSetting.Value = JsonSerializer.Serialize(dto.DefaultInvitePermissions);
        unitOfWork.SettingsRepository.Update(permissionsSetting);

        await unitOfWork.CommitAsync(ct);

        return Ok(new UserInviteSettingsDto
        {
            ShowRequestInviteLink = dto.ShowRequestInviteLink,
            AutoAcceptInviteRequests = autoAccept,
            DefaultInvitePermissions = dto.DefaultInvitePermissions
        });
    }

    /// <summary>
    /// Emails a free-text notice to every user, or just admins, from the Users settings tab - for things like
    /// "the server is going down for maintenance."
    /// </summary>
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    [HttpPost("notify-users")]
    public async Task<ActionResult> NotifyUsers(NotifyUsersDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var userId = UserId;
        if (string.IsNullOrWhiteSpace(dto.Message)) return BadRequest(await localizationService.TranslateAsync(userId, "invalid-payload"));

        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        if (!settings.IsEmailSetup()) return BadRequest(await localizationService.TranslateAsync(userId, "email-settings-invalid"));

        var recipients = dto.AdminsOnly
            ? (IEnumerable<AppUser>) await userManager.GetUsersInRoleAsync(PolicyConstants.AdminRole)
            : await unitOfWork.UserRepository.GetAllUsersAsync(ct: ct);

        var sentCount = 0;
        foreach (var recipient in recipients)
        {
            if (!emailService.IsValidEmail(recipient.Email)) continue;

            BackgroundJob.Enqueue(() => emailService.SendServerNoticeEmail(recipient.Id, recipient.Email!, dto.Message));
            sentCount++;
        }

        logger.LogInformation("{User} sent a server notice to {Count} user(s) ({Scope})", userId, sentCount,
            dto.AdminsOnly ? "admins only" : "all users");

        return Ok(sentCount);
    }

    /// <summary>
    /// Last step in authentication flow, confirms the email token for email
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("confirm-email")]
    public async Task<ActionResult<UserDto>> ConfirmEmail(ConfirmEmailDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByEmailAsync(dto.Email, ct: ct);

        if (user == null)
        {
            logger.LogInformation("confirm-email failed from invalid registered email: {Email}", dto.Email);
            return BadRequest(await localizationService.GetAsync("en", "invalid-email-confirmation"));
        }

        // Validate Password and Username
        var validationErrors = new List<ApiException>();
        // This allows users that use a fake email with the same username to continue setting up the account
        if (!dto.Username.Equals(dto.Email) && !user.UserName!.Equals(dto.Username))
        {
            validationErrors.AddRange(await accountService.ValidateUsername(dto.Username, ct));
        }

        validationErrors.AddRange(await accountService.ValidatePassword(user, dto.Password, ct));

        if (validationErrors.Any())
        {
            return BadRequest(validationErrors);
        }


        if (!await ConfirmEmailToken(dto.Token, user))
        {
            logger.LogInformation("Email confirmation failed because the supplied token was invalid");
            return BadRequest(await localizationService.TranslateAsync(user.Id, "invalid-email-confirmation"));
        }

        user.UserName = dto.Username;
        user.ConfirmationToken = null;
        var errors = await accountService.ChangeUserPassword(user, dto.Password, ct);
        if (errors.Any())
        {
            return BadRequest(errors);
        }
        await unitOfWork.CommitAsync(ct);


        user = (await unitOfWork.UserRepository.GetUserByUsernameAsync(user.UserName,
            AppUserIncludes.UserPreferences | AppUserIncludes.AuthKeys, ct))!;

        return Ok(await ConstructUserDto(user, await userManager.GetRolesAsync(user), ct: HttpContext.RequestAborted));
    }

    /// <summary>
    /// Final step in email update change. Given a confirmation token and the email, this will finish the email change.
    /// </summary>
    /// <remarks>This will force connected clients to re-authenticate</remarks>
    /// <param name="dto"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("confirm-email-update")]
    public async Task<ActionResult> ConfirmEmailUpdate(ConfirmEmailUpdateDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByConfirmationToken(dto.Token, ct);
        if (user == null)
        {
            logger.LogInformation("confirm-email failed from invalid registered email: {Email}", dto.Email.Sanitize());
            return BadRequest(await localizationService.GetAsync("en", "invalid-email-confirmation"));
        }

        if (!await ConfirmEmailToken(dto.Token, user))
        {
            logger.LogInformation("Email change confirmation failed because the supplied token was invalid");
            return BadRequest(await localizationService.TranslateAsync(user.Id, "invalid-email-confirmation"));
        }

        logger.LogInformation("User is updating email from {OldEmail} to {NewEmail}", user.Email, dto.Email.Sanitize());
        var result = await userManager.SetEmailAsync(user, dto.Email);
        if (!result.Succeeded)
        {
            logger.LogError("Unable to update email for users: {Errors}", result.Errors.Select(e => e.Description));
            return BadRequest(await localizationService.TranslateAsync(user.Id, "generic-user-email-update"));
        }
        user.ConfirmationToken = null;
        user.EmailConfirmed = true;
        await unitOfWork.CommitAsync(ct);


        // For the user's connected devices to pull the new information in
        await eventHub.SendMessageToAsync(MessageFactory.UserUpdate,
            MessageFactory.UserUpdateEvent(user.Id, user.UserName!), user.Id, ct);

        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("confirm-password-reset")]
    public async Task<ActionResult<string>> ConfirmForgotPassword(ConfirmPasswordResetDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByEmailAsync(dto.Email, ct: ct);
        if (user == null)
        {
            return BadRequest(BadCredentialsMessage);
        }

        try
        {
            var result = await userManager.VerifyUserTokenAsync(user, TokenOptions.DefaultProvider,
                "ResetPassword", dto.Token);
            if (!result)
            {
                logger.LogInformation("Unable to reset password because the supplied email token was invalid");
                return BadRequest(BadCredentialsMessage);
            }

            var errors = await accountService.ChangeUserPassword(user, dto.Password, ct);
            return errors.Any() ? BadRequest(errors) : Ok(await localizationService.TranslateAsync(user.Id, "password-updated"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an unexpected error when confirming new password");
            return BadRequest(await localizationService.TranslateAsync(user.Id, "generic-password-update"));
        }
    }


    /// <summary>
    /// Will send user a link to update their password to their email or prompt them if not accessible
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting("Authentication")]
    public async Task<ActionResult<string>> ForgotPassword([FromQuery] string email)
    {
        var ct = HttpContext.RequestAborted;
        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        var user = await unitOfWork.UserRepository.GetUserByEmailAsync(email, ct: ct);
        if (user == null)
        {
            logger.LogError("There are no users with email: {Email} but user is requesting password reset", email.Sanitize().Censor());
            return Ok(await localizationService.GetAsync("en", "forgot-password-generic"));
        }

        var oidcConfig = (await unitOfWork.SettingsRepository.GetSettingsDtoAsync()).OidcConfig;
        if (user.IdentityProvider == IdentityProvider.OpenIdConnect  && oidcConfig is {Enabled: true, SyncUserSettings: true})
        {
            return BadRequest(await localizationService.TranslateAsync(user.Id, "oidc-managed"));
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any(r => r is PolicyConstants.AdminRole or PolicyConstants.ChangePasswordRole or PolicyConstants.ReadOnlyRole))
            return Unauthorized(await localizationService.TranslateAsync(user.Id, "permission-denied"));

        if (string.IsNullOrEmpty(user.Email) || !user.EmailConfirmed)
            return BadRequest(await localizationService.TranslateAsync(user.Id, "confirm-email"));



        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var emailLink = await emailService.GenerateEmailLink(Request, token, "confirm-reset-password", user.Email);

        user.ConfirmationToken = token;
        unitOfWork.UserRepository.Update(user);
        await unitOfWork.CommitAsync(ct);

        logger.LogInformation("Generated a password-reset confirmation for {UserName}", user.UserName!.Sanitize());

        if (!settings.IsEmailSetup()) return Ok(await localizationService.GetAsync("en", "email-not-enabled"));
        if (!emailService.IsValidEmail(user.Email))
        {
            logger.LogCritical("[Forgot Password]: The user's email is invalid. An administrator must correct it before password recovery can continue");
            return Ok(await localizationService.TranslateAsync(user.Id, "invalid-email"));
        }

        BackgroundJob.Enqueue(() => emailService.SendForgotPasswordEmail(new PasswordResetEmailDto()
        {
            EmailUserId = user.Id,
            EmailAddress = user.Email,
            ServerConfirmationLink = emailLink
        }));

        return Ok(await localizationService.TranslateAsync(user.Id, "email-sent"));
    }

    [HttpGet("email-confirmed")]
    public async Task<ActionResult<bool>> IsEmailConfirmed()
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByUsernameAsync(Username!, ct: ct);
        if (user == null) return Unauthorized();

        return Ok(user.EmailConfirmed);
    }

    [AllowAnonymous]
    [HttpPost("confirm-migration-email")]
    public async Task<ActionResult<UserDto>> ConfirmMigrationEmail(ConfirmMigrationEmailDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByEmailAsync(dto.Email, ct: ct);
        if (user == null) return BadRequest(BadCredentialsMessage);

        if (!await ConfirmEmailToken(dto.Token, user))
        {
            logger.LogInformation("confirm-migration-email email token is invalid");
            return BadRequest(BadCredentialsMessage);
        }

        await unitOfWork.CommitAsync(ct);

        user = await unitOfWork.UserRepository.GetUserByUsernameAsync(user.UserName!,
            AppUserIncludes.UserPreferences | AppUserIncludes.AuthKeys, ct);

        return Ok(await ConstructUserDto(user!, await userManager.GetRolesAsync(user!), ct: HttpContext.RequestAborted));
    }

    /// <summary>
    /// Resend an invite to a user already invited
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [Authorize(PolicyGroups.AdminPolicy)]
    [HttpPost("resend-confirmation-email")]
    [EnableRateLimiting("Authentication")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult<InviteUserResponse>> ResendConfirmationSendEmail([FromQuery] int userId)
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(userId, ct: ct);
        if (user == null) return BadRequest(await localizationService.GetAsync("en", "no-user"));

        if (string.IsNullOrEmpty(user.Email))
            return BadRequest(await localizationService.TranslateAsync(user.Id, "user-migration-needed"));

        if (user.EmailConfirmed) return BadRequest(await localizationService.TranslateAsync(user.Id, "user-already-confirmed"));

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        user.ConfirmationToken = token;
        unitOfWork.UserRepository.Update(user);
        await unitOfWork.CommitAsync(ct);

        var emailLink = await emailService.GenerateEmailLink(Request, token, "confirm-email-update", user.Email);
        logger.LogInformation("Generated an email-migration confirmation for {UserName}", user.UserName!.Sanitize());

        if (!emailService.IsValidEmail(user.Email))
        {
            logger.LogCritical("[Email Migration]: User {UserName} is trying to resend an invite flow, but their email ({Email}) isn't valid. No email will be send", user.UserName!.Sanitize(), user.Email);
        }


        var serverSettings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);
        var shouldEmailUser = serverSettings.IsEmailSetup() || !emailService.IsValidEmail(user.Email);

        if (!shouldEmailUser)
        {
            return Ok(new InviteUserResponse()
            {
                EmailLink = emailLink,
                EmailSent = false,
                InvalidEmail = !emailService.IsValidEmail(user.Email)
            });
        }

        BackgroundJob.Enqueue(() => emailService.SendInviteEmail(new ConfirmationEmailDto()
        {
            LocaleUserId = user.Id,
            EmailAddress = user.Email!,
            InvitingUser = Username!,
            ServerConfirmationLink = emailLink
        }));

        return Ok(new InviteUserResponse()
        {
            EmailLink = emailLink,
            EmailSent = true,
            InvalidEmail = !emailService.IsValidEmail(user.Email)
        });
    }

    private async Task<bool> ConfirmEmailToken(string token, AppUser user)
    {
        var result = await userManager.ConfirmEmailAsync(user, token);
        if (result.Succeeded) return true;

        logger.LogCritical("[Account] Email validation failed");
        if (!result.Errors.Any()) return false;

        foreach (var error in result.Errors)
        {
            logger.LogCritical("[Account] Email validation error: {Message}", error.Description);
        }

        return false;
    }

    /// <summary>
    /// Returns the OPDS url for this user
    /// </summary>
    /// <returns></returns>
    [HttpGet("opds-url")]
    public async Task<ActionResult<string>> GetOpdsUrl([FromQuery] string? authKeyName = null)
    {
        var ct = HttpContext.RequestAborted;
        var serverSettings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync(ct);

        var origin = HttpContext.Request.Scheme + "://" + HttpContext.Request.Host.Value;
        if (!string.IsNullOrEmpty(serverSettings.HostName)) origin = serverSettings.HostName;

        var baseUrl = string.Empty;
        if (!string.IsNullOrEmpty(serverSettings.BaseUrl) &&
            !serverSettings.BaseUrl.Equals(Configuration.DefaultBaseUrl))
        {
            baseUrl = serverSettings.BaseUrl + "/";
            if (baseUrl.EndsWith("//"))
            {
                baseUrl = baseUrl.Replace("//", "/");
            }

            if (baseUrl.StartsWith('/'))
            {
                baseUrl = baseUrl.Substring(1, baseUrl.Length - 1);
            }
        }

        authKeyName ??= AuthKeyHelper.OpdsKeyName;
        var opdsAuthKey = (await unitOfWork.UserRepository.GetAuthKeysForUserId(UserId, ct))
            .Where(k => k.Name == authKeyName)
            .Select(k => k.Key)
            .FirstOrDefault();

        if (opdsAuthKey == null)
            return NotFound();

        return Ok(origin + "/" + baseUrl + "api/opds/" + opdsAuthKey);
    }


    /// <summary>
    /// Is the user's current email valid or not
    /// </summary>
    /// <returns></returns>
    [HttpGet("is-email-valid")]
    public async Task<ActionResult<bool>> IsEmailValid()
    {
        var ct = HttpContext.RequestAborted;
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(UserId, ct: ct);
        if (user == null) return Unauthorized();
        if (string.IsNullOrEmpty(user.Email)) return Ok(false);

        return Ok(emailService.IsValidEmail(user.Email));
    }

    /// <summary>
    /// Returns all Auth Keys with the account
    /// </summary>
    /// <returns></returns>
    [HttpGet("auth-keys")]
    public async Task<ActionResult<IList<AuthKeyDto>>> GetAuthKeys()
    {
        var ct = HttpContext.RequestAborted;
        return Ok(await unitOfWork.UserRepository.GetAuthKeysForUserId(UserId, ct));
    }

    /// <summary>
    /// Rotate the Auth Key
    /// </summary>
    /// <param name="authKeyId"></param>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("rotate-auth-key")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult<AuthKeyDto>> RotateAuthKey([FromQuery] int authKeyId, RotateAuthKeyRequestDto dto)
    {
        var ct = HttpContext.RequestAborted;
        var authKey = await unitOfWork.UserRepository.GetAuthKeyById(authKeyId, ct);
        if (authKey?.AppUserId != UserId) return BadRequest();

        var oldKeyValue = authKey.Key;

        // Get original expiresAt - createdAt for offset to reset expiresAt
        if (authKey.ExpiresAtUtc != null)
        {
            var originalDuration = authKey.ExpiresAtUtc.Value - authKey.CreatedAtUtc;
            authKey.ExpiresAtUtc = DateTime.UtcNow.Add(originalDuration);
        }

        authKey.Key = AuthKeyHelper.GenerateKey(dto.KeyLength);

        await unitOfWork.CommitAsync(ct);

        await authKeyService.InvalidateAsync(oldKeyValue, ct);

        var newDto = authKey.ToAuthKeyDto();

        await eventHub.SendMessageToAsync(MessageFactory.AuthKeyUpdate, MessageFactory.AuthKeyUpdatedEvent(newDto), UserId, ct);

        return Ok(newDto);
    }

    /// <summary>
    /// Creates a new Auth Key for a user.
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("create-auth-key")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult<AuthKeyDto>> CreateAuthKey(RotateAuthKeyRequestDto dto)
    {
        var ct = HttpContext.RequestAborted;
        // Validate the name doesn't collide
        var authKeys = await unitOfWork.UserRepository.GetAuthKeysForUserId(UserId, ct);
        if (authKeys.Any(k => string.Equals(k.Name, dto.Name, StringComparison.InvariantCultureIgnoreCase)))
        {
            return BadRequest(await localizationService.TranslateAsync(UserId, "auth-key-unique"));
        }

        var newKey = new AppUserAuthKey()
        {
            Name = dto.Name,
            Key = AuthKeyHelper.GenerateKey(dto.KeyLength),
            AppUserId = UserId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = string.IsNullOrEmpty(dto?.ExpiresUtc) ? null : DateTime.Parse(dto.ExpiresUtc),
            Provider = AuthKeyProvider.User,
        };
        unitOfWork.UserRepository.Add(newKey);
        await unitOfWork.CommitAsync(ct);

        var newDto = newKey.ToAuthKeyDto();

        await eventHub.SendMessageToAsync(MessageFactory.AuthKeyUpdate, MessageFactory.AuthKeyUpdatedEvent(newDto), UserId, ct);

        return Ok(newDto);
    }

    /// <summary>
    /// Delete the Auth Key
    /// </summary>
    /// <param name="authKeyId"></param>
    /// <returns></returns>
    [HttpDelete("auth-key")]
    [DisallowRole(PolicyConstants.ReadOnlyRole)]
    public async Task<ActionResult> DeleteAuthKey(int authKeyId)
    {
        var ct = HttpContext.RequestAborted;
        var authKey = await unitOfWork.UserRepository.GetAuthKeyById(authKeyId, ct);
        if (authKey?.AppUserId != UserId) return BadRequest();
        if (authKey.Provider != AuthKeyProvider.User) return BadRequest();

        unitOfWork.UserRepository.Delete(authKey);
        await unitOfWork.CommitAsync(ct);

        await eventHub.SendMessageToAsync(MessageFactory.AuthKeyDeleted, MessageFactory.AuthKeyDeletedEvent(authKeyId), UserId, ct);

        return Ok();
    }
}
