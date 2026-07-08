using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WonDay.Models;

[Table("savings_goals")]
public class SavingsGoal : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    [Column("owner_user_id")]
    public Guid OwnerUserId { get; set; }

    [Column("is_shared")]
    public bool IsShared { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    // "deposit" / "installment_saving" / "free_goal"
    [Column("type")]
    public string Type { get; set; } = "free_goal";

    [Column("linked_account_id")]
    public Guid? LinkedAccountId { get; set; }

    [Column("target_amount")]
    public decimal TargetAmount { get; set; }

    [Column("target_date")]
    public DateTime? TargetDate { get; set; }

    [Column("interest_rate")]
    public decimal? InterestRate { get; set; }

    [Column("color")]
    public string? Color { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("goal_contributions")]
public class GoalContribution : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    [Column("owner_user_id")]
    public Guid OwnerUserId { get; set; }

    [Column("is_shared")]
    public bool IsShared { get; set; }

    [Column("goal_id")]
    public Guid GoalId { get; set; }

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    // "manual" / "auto"
    [Column("source")]
    public string Source { get; set; } = "manual";

    [Column("from_account_id")]
    public Guid? FromAccountId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

[Table("recurring_rules")]
public class RecurringRule : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    [Column("owner_user_id")]
    public Guid OwnerUserId { get; set; }

    [Column("is_shared")]
    public bool IsShared { get; set; }

    [Column("goal_id")]
    public Guid GoalId { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("day_of_month")]
    public int DayOfMonth { get; set; }

    [Column("from_account_id")]
    public Guid? FromAccountId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
