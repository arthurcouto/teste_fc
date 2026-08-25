namespace CashFlow.Consolidation.Api.Authentication;

public enum AuthenticationMode
{
    Required,
    Disabled
}

public sealed class ApiAuthenticationOptions
{
    public const string SectionName = "Authentication";

    public AuthenticationMode Mode { get; set; } = AuthenticationMode.Required;

    public string? Authority { get; set; }

    public string? Audience { get; set; }

    public bool RequireHttpsMetadata { get; set; } = true;
}
