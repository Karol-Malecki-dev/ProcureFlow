using Domain.ValueObjects;

namespace Domain.Models.Organizations.Organization;

public sealed class Organization
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public Address Address { get; private set; } = null!;
    public string Code { get; private set; } = string.Empty;
    public string? Bio { get; private set; }
    public bool IsArchived { get; private set; }

    private readonly List<Branch> _branches = new();

    private Organization()
    {
    }

    public Organization(
        string name,
        Address address,
        string code,
        string? bio)
    {
        ArgumentNullException.ThrowIfNull(address);

        Id = Guid.NewGuid();
        Name = NormalizeName(name);
        Address = address;
        Code = NormalizeCode(code);
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
    }

    public IReadOnlyCollection<Branch> Branches => _branches;

    public void UpdateDetails(
        string name,
        Address address,
        string? bio)
    {
        ArgumentNullException.ThrowIfNull(address);

        Name = NormalizeName(name);
        Address = address;
        Bio = string.IsNullOrWhiteSpace(bio) ? null : bio.Trim();
    }

    public void AddBranch(Branch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);

        if (IsArchived)
        {
            throw new InvalidOperationException("Archived organization cannot accept new branches.");
        }

        if (_branches.Any(existingBranch => existingBranch.Id == branch.Id))
        {
            throw new InvalidOperationException("Branch already exists.");
        }

        if (branch.OrganizationId != Id)
        {
            throw new InvalidOperationException("Branch belongs to a different organization.");
        }

        _branches.Add(branch);
    }

    public void ArchiveBranch(Branch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);

        if (!_branches.Contains(branch))
        {
            throw new InvalidOperationException("Branch does not exist.");
        }

        branch.Archive();
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
