namespace Librariann.Models.Constants;

/// <summary>
/// Constants for Higher level policy roles
/// </summary>
public static class PolicyGroups
{
    /// <summary>
    /// Requires admin to execute
    /// </summary>
    public const string AdminPolicy = "RequireAdminRole";
    /// <summary>
    /// Requires Admin or Download to execute
    /// </summary>
    public const string DownloadPolicy = "RequireDownloadRole";
    /// <summary>
    /// Requires Admin or Change Password to execute
    /// </summary>
    public const string ChangePasswordPolicy = "RequireChangePasswordRole";
    /// <summary>
    /// Requires Admin or Bookmark to execute
    /// </summary>
    public const string BookmarkPolicy = "RequireBookmarkRole";
    /// <summary>
    /// Requires Admin or Search Indexers.
    /// </summary>
    public const string SearchIndexersPolicy = "RequireSearchIndexersRole";
    /// <summary>
    /// Requires Admin or Grab Releases.
    /// </summary>
    public const string GrabReleasesPolicy = "RequireGrabReleasesRole";
    /// <summary>
    /// Requires Admin or Manage Metadata.
    /// </summary>
    public const string ManageMetadataPolicy = "RequireManageMetadataRole";
    /// <summary>
    /// Requires Admin or Manage Libraries.
    /// </summary>
    public const string ManageLibrariesPolicy = "RequireManageLibrariesRole";
    /// <summary>
    /// Requires Admin or Manage Acquisition.
    /// </summary>
    public const string ManageAcquisitionPolicy = "RequireManageAcquisitionRole";
}
