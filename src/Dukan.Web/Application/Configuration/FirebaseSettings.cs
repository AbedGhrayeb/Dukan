namespace Dukan.Web.Application.Configuration;

public sealed class FirebaseSettings
{
    public const string SectionName = "Firebase";

    public string ProjectId { get; init; } = "";

    /// <summary>
    /// Absolute path to service-account JSON file.
    /// For IIS: C:\secrets\dukkan-firebase.json  (not committed)
    /// Alternatively set <see cref="CredentialJson"/> inline (base64 not required).
    /// </summary>
    public string ServiceAccountPath { get; init; } = "";

    /// <summary>
    /// Inline JSON content of service-account file. Preferred for env-var injection:
    /// Firebase__CredentialJson = { ...json... }
    /// Takes precedence over <see cref="ServiceAccountPath"/> if non-empty.
    /// </summary>
    public string CredentialJson { get; init; } = "";

    public bool Enabled { get; init; } = true;
}
