namespace VoterSystem.DataAccess.Config;

public sealed class SamlSettings
{
    public bool Enabled { get; set; }

    public string? PublicOrigin { get; set; }

    public string CallbackPath { get; set; } = "/api/v1/users/external-callback-saml";

    public string? ReturnUrl { get; set; }

    public string ServiceProviderEntityId { get; set; } = string.Empty;

    public string IdentityProviderEntityId { get; set; } = string.Empty;

    public string? MetadataUrl { get; set; }

    public string? SingleSignOnUrl { get; set; }

    public string? SingleLogoutUrl { get; set; }

    public bool AllowUnsolicitedAuthnResponse { get; set; } = true;

    public bool SignAuthnRequests { get; set; }

    public string? SigningCertificateBase64 { get; set; }

    public string? SigningCertificatePassword { get; set; }
}
