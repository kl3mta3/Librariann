using System.Collections.Immutable;

namespace Librariann.Models.Constants;

/// <summary>
/// Role-based Security
/// </summary>
public static class PolicyConstants
{
    /// <summary>
    /// Admin User. Has all privileges
    /// </summary>
    public const string AdminRole = "Admin";
    /// <summary>
    /// Standard Librariann user. Additional capabilities must be granted by an administrator.
    /// </summary>
    public const string UserRole = "User";
    /// <summary>
    /// Legacy Librariann role retained so existing installations continue to authorize during migration.
    /// </summary>
    public const string PlebRole = "Pleb";
    /// <summary>
    /// Used to give a user ability to download files from the server
    /// </summary>
    public const string DownloadRole = "Download";
    /// <summary>
    /// Used to give a user ability to change their own password
    /// </summary>
    public const string ChangePasswordRole = "Change Password";
    /// <summary>
    /// Used to give a user ability to bookmark files on the server
    /// </summary>
    public const string BookmarkRole = "Bookmark";
    /// <summary>
    /// Used to give a user ability to Change Restrictions on their account
    /// </summary>
    public const string ChangeRestrictionRole = "Change Restriction";
    /// <summary>
    /// Used to give a user ability to Login to their account
    /// </summary>
    public const string LoginRole = "Login";
    /// <summary>
    /// Restricts the ability to manage their account without an admin
    /// </summary>
    /// <remarks>This is used explicitly for Demo Server. Not sure why it would be used in another fashion</remarks>
    public const string ReadOnlyRole = "Read Only";
    /// <summary>
    /// Ability to promote entities (Collections, Reading Lists, etc).
    /// </summary>
    public const string PromoteRole = "Promote";
    /// <summary>
    /// Can search configured indexers but cannot grab a release unless separately granted.
    /// </summary>
    public const string SearchIndexersRole = "Search Indexers";
    /// <summary>
    /// Can send approved releases to configured download clients.
    /// </summary>
    public const string GrabReleasesRole = "Grab Releases";
    /// <summary>
    /// Can edit, refresh, and write library item metadata.
    /// </summary>
    public const string ManageMetadataRole = "Manage Metadata";
    /// <summary>
    /// Can configure and operate libraries and their scanners.
    /// </summary>
    public const string ManageLibrariesRole = "Manage Libraries";
    /// <summary>
    /// Can configure indexers and download clients, including their protected credentials.
    /// </summary>
    public const string ManageAcquisitionRole = "Manage Acquisition";




    public static readonly ImmutableArray<string> ValidRoles =
        [
            AdminRole, UserRole, PlebRole, DownloadRole, ChangePasswordRole, BookmarkRole, ChangeRestrictionRole,
            LoginRole, ReadOnlyRole, PromoteRole, SearchIndexersRole, GrabReleasesRole, ManageMetadataRole,
            ManageLibrariesRole, ManageAcquisitionRole
        ];
}
