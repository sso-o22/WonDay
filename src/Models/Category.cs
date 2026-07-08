using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WonDay.Models;

[Table("categories")]
public class Category : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    [Column("owner_user_id")]
    public Guid OwnerUserId { get; set; }

    // 카테고리는 기본적으로 공유되는 게 자연스러워서 기본값 true
    [Column("is_shared")]
    public bool IsShared { get; set; } = true;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    // "income" 또는 "expense"
    [Column("type")]
    public string Type { get; set; } = "expense";

    [Column("color")]
    public string? Color { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("budget_amount")]
    public decimal? BudgetAmount { get; set; }

    // "daily" / "weekly" / "monthly"
    [Column("budget_period")]
    public string BudgetPeriod { get; set; } = "monthly";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
