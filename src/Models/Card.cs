using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WonDay.Models;

[Table("cards")]
public class Card : BaseModel
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

    [Column("linked_account_id")]
    public Guid? LinkedAccountId { get; set; }

    // 매월 청구 마감일 (예: 15)
    [Column("statement_day")]
    public int StatementDay { get; set; }

    // 매월 실제 출금일 (예: 25)
    [Column("payment_day")]
    public int PaymentDay { get; set; }

    [Column("color")]
    public string? Color { get; set; }

    [Column("icon")]
    public string? Icon { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
