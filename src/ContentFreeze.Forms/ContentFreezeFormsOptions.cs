namespace ContentFreeze.Forms;

/// <summary>
/// Settings bound from the <c>ContentFreeze:Forms</c> section in appsettings. Submissions from the website are
/// never blocked, whatever these say.
/// </summary>
public class ContentFreezeFormsOptions
{
    public const string SectionName = "ContentFreeze:Forms";

    /// <summary>Forms (with their workflows), folders, data sources and prevalue sources.</summary>
    public bool FreezeForms { get; set; } = true;

    /// <summary>Editing, approving, rejecting and deleting entries, and retrying workflows, in the backoffice.</summary>
    public bool FreezeEntries { get; set; }

    /// <summary>Form permissions per user and user group.</summary>
    public bool FreezeSecurity { get; set; }
}
