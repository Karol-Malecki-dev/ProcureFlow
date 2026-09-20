
namespace Domain.Models.Organizations.Enums
{
    public enum MembershipOperationStatus
    {
        Success = 0,
        NotFound = 1,
        Conflict = 2,
        ValidationError = 3,
        Forbidden = 4
    }
}
