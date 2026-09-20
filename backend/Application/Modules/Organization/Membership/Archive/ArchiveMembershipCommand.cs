namespace Application.Modules.Organization.Membership.Archive;

public sealed record ArchiveMembershipCommand(
    Guid OrganizationId,
    Guid MembershipId
);
