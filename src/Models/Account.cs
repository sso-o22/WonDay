using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WonDay.Models;

[Table("accounts")]
public class Account : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    [Column("owner_user_id")]
    public Guid OwnerUserId { get; set; }

    // true면 배우자도 보임, false면 나만 보임
    [Column("is_shared")]
    public bool IsShared { get; set; }

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    // "cash" 또는 "bank"
    [Column("type")]
    public string Type { get; set; } = "bank";

    [Column("initial_balance")]
    public decimal InitialBalance { get; set; }

    [Column("color")]
    public string? Color { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
