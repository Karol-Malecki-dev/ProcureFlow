namespace Application.Modules.Organization.GetActiveOrganization;

public enum GetActiveOrganizationStatus
{
    Success,
    NotFound
}

public sealed record GetActiveOrganizationResult(
    GetActiveOrganizationStatus Status,
    ActiveOrganization? Value = null,
    string Message = "Success")
{
    public bool IsSuccess => Status == GetActiveOrganizationStatus.Success;

    public static GetActiveOrganizationResult Success(ActiveOrganization value)
        => new(GetActiveOrganizationStatus.Success, value);

    public static GetActiveOrganizationResult NotFound(string message)
        => new(GetActiveOrganizationStatus.NotFound, null, message);
}
