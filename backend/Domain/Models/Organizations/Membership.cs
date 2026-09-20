using Domain.Models.Organizations.Enums;

namespace Domain.Models.Organizations
{
    public sealed class Membership
    {
        public Guid Id { get; private set; }
        public Guid OrganizationId { get; private set; }
        public Guid UserId { get; private set; }
        public Guid? BranchId { get; private set; }
        public BusinessRole Role { get; private set; }
        public bool IsActive { get; private set; }
    
        private Membership() { } // For EF Core

        public Membership(
            Guid organizationId,
            Guid userId,
            Guid? branchId,
            BusinessRole role)
        {
            if (organizationId == Guid.Empty)
            {
                throw new ArgumentException("Organization id cannot be empty.", nameof(organizationId));
            }
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User id cannot be empty.", nameof(userId));
            }
            if (branchId == Guid.Empty)
            {
                throw new ArgumentException("Branch id cannot be empty.", nameof(branchId));
            }

            ValidateRoleAndBranch(role, branchId);

            Id = Guid.NewGuid();
            OrganizationId = organizationId;
            UserId = userId;
            BranchId = branchId;
            Role = role;
            IsActive = true;
        }

        public void UpdateRole(BusinessRole newRole)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Cannot update role of an inactive membership.");
            }

            ValidateRoleAndBranch(newRole, BranchId);
            Role = newRole;
        }

        public void ChangeBranch(Guid? branchId)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Cannot update an inactive membership.");
            }

            if (branchId == Guid.Empty)
            {
                throw new ArgumentException("Branch id cannot be empty.", nameof(branchId));
            }

            ValidateRoleAndBranch(Role, branchId);
            BranchId = branchId;
        }

        public void UpdateAssignment(
            BusinessRole newRole,
            Guid? newBranchId)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Membership is not active.");
            }
            ValidateRoleAndBranch(newRole, newBranchId);
            Role = newRole;
            BranchId = newBranchId;
        }

        public void Deactivate()
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("Membership is already inactive.");
            }
            IsActive = false;
        }

        private static void ValidateRoleAndBranch(
            BusinessRole role,
            Guid? branchId)
        {
            if (!Enum.IsDefined(role))
            {
                throw new ArgumentOutOfRangeException(nameof(role), role, "Business role is not defined.");
            }

            if (branchId.HasValue && branchId.Value == Guid.Empty)
            {
                throw new ArgumentException("Branch id cannot be empty.", nameof(branchId));
            }

            var requiresBranch = role is BusinessRole.Employee or BusinessRole.Manager;
            if (requiresBranch && !branchId.HasValue)
            {
                throw new ArgumentException(
                    $"Business role '{role}' requires a branch.",
                    nameof(branchId));
            }

            if (role == BusinessRole.Procurement && branchId.HasValue)
            {
                throw new ArgumentException(
                    "Procurement membership must be organization-wide and cannot have a branch.",
                    nameof(branchId));
            }
        }
    }
}
