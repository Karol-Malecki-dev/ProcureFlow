namespace Application.Modules.Organization.GetActiveOrganization;

public sealed record ActiveOrganization(
    Guid Id,
    string Name,
    string Code);
