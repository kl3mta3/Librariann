using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Librariann.API.Database;
using Librariann.API.Services;
using Librariann.Common;
using Librariann.Common.Extensions;
using Librariann.Common.Helpers;
using Librariann.Models;
using Librariann.Models.Constants;
using Librariann.Models.DTOs;
using Librariann.Models.DTOs.Email;
using Librariann.Models.DTOs.LibrariannPlus.Metadata;
using Librariann.Models.DTOs.Metadata;
using Librariann.Models.DTOs.Reader;
using Librariann.Models.DTOs.Settings;
using Librariann.Models.Entities.Enums;
using Librariann.Models.Mapping;
using Librariann.Server.Extensions;
using Librariann.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TaskScheduler = Librariann.Services.TaskScheduler;

namespace Librariann.Server.Controllers;

public class SettingsController(
    ILogger<SettingsController> logger,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    ILocalizationService localizationService,
    ISettingsService settingsService,
    IAuthenticationSchemeProvider authenticationSchemeProvider,
    IOidcService oidcService,
    IKokoroTtsService kokoroTtsService,
    IKokoroReleaseService kokoroReleaseService,
    IKokoroProcessService kokoroProcessService,
    IKokoroInstallService kokoroInstallService,
    IFfmpegInstallService ffmpegInstallService)
    : BaseApiController
{
    /// <summary>
    /// Returns the base url for this instance (if set)
    /// </summary>
    /// <returns></returns>
    [HttpGet("base-url")]
    public async Task<ActionResult<string>> GetBaseUrl()
    {
        var settingsDto = await unitOfWork.SettingsRepository.GetSettingsDtoAsync();
        return Ok(settingsDto.BaseUrl);
    }

    /// <summary>
    /// Returns the server settings
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<ServerSettingDto>> GetSettings()
    {
        var settingsDto = await unitOfWork.SettingsRepository.GetSettingsDtoAsync();
        MaskRecoverableCredentials(settingsDto);
        return Ok(settingsDto);
    }

    [HttpPost("reset")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<ServerSettingDto>> ResetSettings()
    {
        logger.LogInformation("{UserName} is resetting Server Settings", Username!);

        return await UpdateSettings(Defaults.DefaultSettings.ToServerSettingDto());
    }

    /// <summary>
    /// Resets the IP Addresses
    /// </summary>
    /// <returns></returns>
    [HttpPost("reset-ip-addresses")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<ServerSettingDto>> ResetIpAddressesSettings()
    {
        logger.LogInformation("{UserName} is resetting IP Addresses Setting", Username!);
        var ipAddresses = await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.IpAddresses);
        ipAddresses.Value = Configuration.DefaultIpAddresses;
        unitOfWork.SettingsRepository.Update(ipAddresses);

        if (!await unitOfWork.CommitAsync())
        {
            await unitOfWork.RollbackAsync();
        }

        var settingsDto = await unitOfWork.SettingsRepository.GetSettingsDtoAsync();
        MaskRecoverableCredentials(settingsDto);
        return Ok(settingsDto);
    }

    /// <summary>
    /// Resets the Base url
    /// </summary>
    /// <returns></returns>
    [HttpPost("reset-base-url")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<ServerSettingDto>> ResetBaseUrlSettings()
    {
        logger.LogInformation("{UserName} is resetting Base Url Setting", Username!);
        var baseUrl = await unitOfWork.SettingsRepository.GetSettingAsync(ServerSettingKey.BaseUrl);
        baseUrl.Value = Configuration.DefaultBaseUrl;
        unitOfWork.SettingsRepository.Update(baseUrl);

        if (!await unitOfWork.CommitAsync())
        {
            await unitOfWork.RollbackAsync();
        }

        Configuration.BaseUrl = baseUrl.Value;
        var settingsDto = await unitOfWork.SettingsRepository.GetSettingsDtoAsync();
        MaskRecoverableCredentials(settingsDto);
        return Ok(settingsDto);
    }

    /// <summary>
    /// Is the minimum information setup for Email to work
    /// </summary>
    /// <returns></returns>
    [HttpGet("is-email-setup")]
    public async Task<ActionResult<bool>> IsEmailSetup()
    {
        var settings = await unitOfWork.SettingsRepository.GetSettingsDtoAsync();
        return Ok(settings.IsEmailSetup());
    }


    /// <summary>
    /// Update Server settings
    /// </summary>
    /// <param name="updateSettingsDto"></param>
    /// <returns></returns>
    [HttpPost]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<ServerSettingDto>> UpdateSettings(ServerSettingDto updateSettingsDto)
    {
        logger.LogInformation("{UserName} is updating Server Settings", Username!);

        try
        {
            var d = await settingsService.UpdateSettings(updateSettingsDto);
            // The request may contain a newly supplied plaintext secret. Never reflect it in the response.
            MaskRecoverableCredentials(d);
            return Ok(d);
        }
        catch (LibrariannException ex)
        {
            return BadRequest(await localizationService.TranslateAsync(UserId, ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an exception when updating server settings");
            return BadRequest(await localizationService.TranslateAsync(UserId, "generic-error"));
        }
    }

    internal static void MaskRecoverableCredentials(ServerSettingDto settingsDto)
    {
        // A fixed mask avoids disclosing either the value or its length.
        if (!string.IsNullOrEmpty(settingsDto.OidcConfig.Secret))
            settingsDto.OidcConfig.Secret = ServerSettingCredentialScopes.MaskedValue;
        if (!string.IsNullOrEmpty(settingsDto.SmtpConfig.Password))
            settingsDto.SmtpConfig.Password = ServerSettingCredentialScopes.MaskedValue;
    }

    /// <summary>
    /// All values allowed for Task Scheduling APIs. A custom cron job is not included. Disabled is not applicable for Cleanup.
    /// </summary>
    /// <returns></returns>
    [HttpGet("task-frequencies")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public ActionResult<IEnumerable<string>> GetTaskFrequencies()
    {
        return Ok(CronConverter.Options);
    }

    [HttpGet("library-types")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public ActionResult<IEnumerable<string>> GetLibraryTypes()
    {
        return Ok(Enum.GetValues<LibraryType>().Select(t => t.ToDescription()));
    }

    [HttpGet("log-levels")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public ActionResult<IEnumerable<string>> GetLogLevels()
    {
        return Ok(new[] {"Trace", "Debug", "Information", "Warning", "Critical"});
    }

    [HttpGet("opds-enabled")]
    public async Task<ActionResult<bool>> GetOpdsEnabled()
    {
        var settingsDto = await unitOfWork.SettingsRepository.GetSettingsDtoAsync();
        return Ok(settingsDto.EnableOpds);
    }

    /// <summary>
    /// Is the cron expression valid for Librariann's scheduler
    /// </summary>
    /// <param name="cronExpression"></param>
    /// <returns></returns>
    [HttpGet("is-valid-cron")]
    public ActionResult<bool> IsValidCron(string cronExpression)
    {
        // NOTE: This must match Hangfire's underlying cron system. Hangfire is unique
        return Ok(CronHelper.IsValidCron(cronExpression));
    }

    /// <summary>
    /// Sends a test email to see if email settings are hooked up correctly
    /// </summary>
    /// <returns></returns>
    [HttpPost("test-email-url")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<EmailTestResultDto>> TestEmailServiceUrl()
    {
        var user = await unitOfWork.UserRepository.GetUserByIdAsync(UserId);
        if (string.IsNullOrEmpty(user?.Email)) return BadRequest("Your account has no email on record. Cannot email.");
        return Ok(await emailService.SendTestEmail(user!.Email));
    }

    /// <summary>
    /// Pings the configured Kokoro TTS server and reports whether it's reachable, plus whatever diagnostic
    /// info it exposes (model precision, GPU status, voice count, version, uptime). Backs the "Check Status"
    /// button next to the Kokoro URL field in Settings -> Media.
    /// </summary>
    [HttpGet("kokoro-status")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<KokoroStatusDto>> GetKokoroStatus()
    {
        return Ok(await kokoroTtsService.GetStatusAsync(HttpContext.RequestAborted));
    }

    /// <summary>
    /// Checks GitHub for the latest published github.com/kl3mta3/Librariann-Kokoro-Server release. Informational
    /// only - does not download or install anything. Backs the "Check for Updates" button next to the Kokoro
    /// URL field in Settings -> Media (shown once something is already installed - see kokoro-install for the
    /// "nothing installed yet" case).
    /// </summary>
    [HttpGet("kokoro-latest-release")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<KokoroLatestReleaseDto>> GetKokoroLatestRelease()
    {
        return Ok(await kokoroReleaseService.GetLatestReleaseAsync(HttpContext.RequestAborted));
    }

    /// <summary>
    /// Downloads the latest Librariann-Kokoro-Server release and extracts it to a default install folder (or
    /// the currently configured one, if reinstalling/updating), then points KokoroExecutablePath at it. Returns
    /// immediately - the download runs in the background, poll kokoro-install-status for progress. Backs the
    /// "Install"/"Download" button shown when nothing is installed yet.
    /// </summary>
    [HttpPost("kokoro-install")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public ActionResult<KokoroInstallStatusDto> StartKokoroInstall()
    {
        return Ok(kokoroInstallService.StartInstall());
    }

    /// <summary>Progress of an in-flight or just-finished Kokoro install - poll this after kokoro-install.</summary>
    [HttpGet("kokoro-install-status")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public ActionResult<KokoroInstallStatusDto> GetKokoroInstallStatus()
    {
        return Ok(kokoroInstallService.GetStatus());
    }

    /// <summary>
    /// Downloads a static ffmpeg build (github.com/BtbN/FFmpeg-Builds) and extracts it to a default install
    /// folder, then points FfmpegPath at it and syncs a managed Kokoro install's own ffmpeg path if enabled.
    /// Returns immediately - the download runs in the background, poll ffmpeg-install-status for progress.
    /// Backs the "Install ffmpeg" button next to the ffmpeg path field in Settings -> Media - an alternative
    /// to typing a path manually for admins who don't already have ffmpeg on their system PATH.
    /// </summary>
    [HttpPost("ffmpeg-install")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public ActionResult<FfmpegInstallStatusDto> StartFfmpegInstall()
    {
        return Ok(ffmpegInstallService.StartInstall());
    }

    /// <summary>Progress of an in-flight or just-finished ffmpeg install - poll this after ffmpeg-install.</summary>
    [HttpGet("ffmpeg-install-status")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public ActionResult<FfmpegInstallStatusDto> GetFfmpegInstallStatus()
    {
        return Ok(ffmpegInstallService.GetStatus());
    }

    /// <summary>
    /// Status of the Kokoro process Librariann itself started, if any. Never reports on a Kokoro server the
    /// admin runs/points KokoroEndpointUrl at manually - see IKokoroProcessService's doc comment.
    /// </summary>
    [HttpGet("kokoro-process-status")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<KokoroProcessStatusDto>> GetKokoroProcessStatus()
    {
        return Ok(await kokoroProcessService.GetStatusAsync(HttpContext.RequestAborted));
    }

    /// <summary>
    /// Launches Librariann-Kokoro-Server from the configured KokoroExecutablePath folder as a supervised child
    /// process. No-ops if Librariann already has one running.
    /// </summary>
    [HttpPost("kokoro-start")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<KokoroProcessStatusDto>> StartKokoroProcess()
    {
        return Ok(await kokoroProcessService.StartAsync(HttpContext.RequestAborted));
    }

    /// <summary>Stops the Kokoro process Librariann itself started, if any. No-ops otherwise.</summary>
    [HttpPost("kokoro-stop")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<KokoroProcessStatusDto>> StopKokoroProcess()
    {
        return Ok(await kokoroProcessService.StopAsync(HttpContext.RequestAborted));
    }

    /// <summary>
    /// Get the metadata settings for Librariann+ users.
    /// </summary>
    /// <returns></returns>
    [HttpGet("metadata-settings")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<MetadataSettingsDto>> GetMetadataSettings()
    {
        return Ok(await unitOfWork.SettingsRepository.GetMetadataSettingDto());

    }

    /// <summary>
    /// Update the metadata settings for Librariann+ Metadata feature
    /// </summary>
    /// <param name="dto"></param>
    /// <returns></returns>
    [HttpPost("metadata-settings")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<MetadataSettingsDto>> UpdateMetadataSettings(MetadataSettingsDto dto)
    {
        try
        {
            return Ok(await settingsService.UpdateMetadataSettings(dto));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue when updating metadata settings");
            return BadRequest(ex.Message);
        }
    }

    [Authorize(PolicyGroups.AdminPolicy)]
    [HttpPost("run-metadata-mappings")]
    public async Task<ActionResult> RunMetadataMappings([FromBody] RunMetadataMappingsRequestDto requestDto)
    {
        if (!requestDto.AllLibraries && requestDto.IncludedLibraries.Count == 0)
        {
            return BadRequest(await localizationService.TranslateAsync("mappings-re-run-no-libraries-selected"));
        }

        if (requestDto.ExcludedLibraries.Intersect(requestDto.IncludedLibraries).Any())
        {
            return BadRequest(await localizationService.TranslateAsync("mappings-re-run-libraries-overlap"));
        }

        if (TaskScheduler.IsMethodRunningOrEnqueued(nameof(IMetadataService.RunMetadataMappings), TaskSchedulerConstants.ScanQueue))
        {
            return BadRequest(await localizationService.TranslateAsync("mappings-re-run-in-progress"));
        }

        BackgroundJob.Enqueue<IMetadataService>(s => s.RunMetadataMappings(requestDto, CancellationToken.None));

        return Ok();
    }

    /// <summary>
    /// Import field mappings
    /// </summary>
    /// <returns></returns>
    [HttpPost("import-field-mappings")]
    [Authorize(Policy = PolicyGroups.AdminPolicy)]
    public async Task<ActionResult<FieldMappingsImportResultDto>> ImportFieldMappings([FromBody] ImportFieldMappingsDto dto)
    {
        try
        {
            return Ok(await settingsService.ImportFieldMappings(dto.Data, dto.Settings));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "There was an issue importing field mappings");
            return BadRequest(ex.Message);
        }
    }


    /// <summary>
    /// Retrieve publicly required configuration regarding Oidc
    /// </summary>
    /// <returns></returns>
    [AllowAnonymous]
    [HttpGet("oidc")]
    public async Task<ActionResult<OidcPublicConfigDto>> GetOidcConfig()
    {
        var oidcScheme = await authenticationSchemeProvider.GetSchemeAsync(IdentityServiceExtensions.OpenIdConnect);

        var settings = (await unitOfWork.SettingsRepository.GetSettingsDtoAsync()).OidcConfig;
        var publicConfig = settings.ToOidcPublicConfigDto();
        publicConfig.Enabled = oidcScheme != null &&
                               !string.IsNullOrEmpty(settings.Authority) &&
                               !string.IsNullOrEmpty(settings.ClientId) &&
                               !string.IsNullOrEmpty(settings.Secret);

        return Ok(publicConfig);
    }

    [Authorize(PolicyGroups.AdminPolicy)]
    [HttpPost("reset-external-ids")]
    public async Task<IActionResult> ResetExternalIds()
    {
        await oidcService.ClearOidcIds();

        return Ok();
    }

    /// <summary>
    /// Validate if the given authority is reachable from the server
    /// </summary>
    /// <param name="authority"></param>
    /// <returns></returns>
    [Authorize(PolicyGroups.AdminPolicy)]
    [HttpPost("is-valid-authority")]
    public async Task<ActionResult<AuthorityValidationResult>> IsValidAuthority([FromBody] AuthorityValidationDto authority)
    {
        return Ok(await settingsService.IsValidAuthority(authority.Authority));
    }


}
