using Domain.ValueObjects;

namespace Domain.Models.Organizations.Organization;

public sealed class Branch
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;
    public string Code { get; private set; } = string.Empty;
    public bool IsArchived { get; private set; }
    public Guid OrganizationId { get; private set; }

    private Branch()
    {
    }

    public Branch(
        string name,
        Address address,
        string code,
        Guid organizationId)
    {
        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException("Organization id cannot be empty.", nameof(organizationId));
        }

        ArgumentNullException.ThrowIfNull(address);

        Id = Guid.NewGuid();
        Name = NormalizeName(name);
        Address = address;
        Code = NormalizeCode(code);
        OrganizationId = organizationId;
    }

    public void UpdateDetails(
        string name,
        Address address,
        string code)
    {
        if (IsArchived)
        {
            throw new InvalidOperationException("Archived branch cannot be updated.");
        }

        ArgumentNullException.ThrowIfNull(address);

        Name = NormalizeName(name);
        Code = NormalizeCode(code);
        Address = address;
    }

    public void Archive()
    {
        if (IsArchived)
        {
            return;
        }

        IsArchived = true;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        }

        return name.Trim();
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));
        }

        return code.Trim().ToUpperInvariant();
    }
}
