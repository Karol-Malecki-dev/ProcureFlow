namespace Domain.Entities;

/// <summary>
/// Monthly spending budget owned by one branch.
/// Used amount is increased inside the same persistence operation as an approval.
/// </summary>
public sealed class BranchMonthlyBudget
{
    public const int MonthMinimum = 1;
    public const int MonthMaximum = 12;
    public const int MoneyScale = 2;
    public const decimal MaximumAmount = 99_999_999_999.99m;

    private BranchMonthlyBudget()
    {
    }

    private BranchMonthlyBudget(
        Guid branchId,
        int year,
        int month,
        decimal limitAmount)
    {
        Id = Guid.NewGuid();
        BranchId = RequireIdentifier(branchId, nameof(branchId));
        ValidatePeriod(year, month);
        Year = year;
        Month = month;
        LimitAmount = NormalizeAmount(limitAmount, nameof(limitAmount));
        UsedAmount = 0m;
        ConcurrencyStamp = GenerateConcurrencyStamp();
    }

    public Guid Id { get; private set; }
    public Guid BranchId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public decimal LimitAmount { get; private set; }
    public decimal UsedAmount { get; private set; }
    public string ConcurrencyStamp { get; private set; } = string.Empty;

    public decimal AvailableAmount => LimitAmount - UsedAmount;

    public static BranchMonthlyBudget Create(
        Guid branchId,
        int year,
        int month,
        decimal limitAmount)
        => new(branchId, year, month, limitAmount);

    public void ChangeLimit(decimal limitAmount)
    {
        LimitAmount = NormalizeAmount(limitAmount, nameof(limitAmount));
        Touch();
    }

    public void Reserve(decimal amount)
    {
        var normalizedAmount = NormalizePositiveAmount(amount, nameof(amount));
        if (normalizedAmount > AvailableAmount)
        {
            throw new InvalidOperationException("The monthly budget does not have enough available amount.");
        }

        UsedAmount = NormalizeAmount(UsedAmount + normalizedAmount, nameof(amount));
        Touch();
    }

    public void ReserveOverLimit(decimal amount)
    {
        var normalizedAmount = NormalizePositiveAmount(amount, nameof(amount));
        UsedAmount = NormalizeAmount(UsedAmount + normalizedAmount, nameof(amount));
        Touch();
    }

    private static Guid RequireIdentifier(Guid identifier, string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException("Identifier is required.", parameterName);
        }

        return identifier;
    }

    private static void ValidatePeriod(int year, int month)
    {
        if (year < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(year), year, "Year must be positive.");
        }

        if (month is < MonthMinimum or > MonthMaximum)
        {
            throw new ArgumentOutOfRangeException(nameof(month), month, "Month must be between 1 and 12.");
        }
    }

    private static decimal NormalizePositiveAmount(decimal value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Amount must be greater than zero.");
        }

        return NormalizeAmount(value, parameterName);
    }

    private static decimal NormalizeAmount(decimal value, string parameterName)
    {
        if (value < 0 || value > MaximumAmount || decimal.Round(value, MoneyScale, MidpointRounding.AwayFromZero) != value)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Amount must be between 0 and {MaximumAmount} with at most {MoneyScale} decimal places.");
        }

        return value;
    }

    private void Touch()
        => ConcurrencyStamp = GenerateConcurrencyStamp();

    private static string GenerateConcurrencyStamp()
        => Guid.NewGuid().ToString("N");
}
