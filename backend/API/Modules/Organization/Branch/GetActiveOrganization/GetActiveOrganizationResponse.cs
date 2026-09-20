namespace API.Modules.Organization.Branch.GetActiveOrganization;

/// <summary>
/// Identifies the single active organization used by the current MVP.
/// </summary>
public sealed record GetActiveOrganizationResponse(
    Guid Id,
    string Name,
    string Code);
