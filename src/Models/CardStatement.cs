using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WonDay.Models;

[Table("card_statements")]
public class CardStatement : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    [Column("owner_user_id")]
    public Guid OwnerUserId { get; set; }

    [Column("is_shared")]
    public bool IsShared { get; set; }

    [Column("card_id")]
    public Guid CardId { get; set; }

    [Column("period_start")]
    public DateTime PeriodStart { get; set; }

    [Column("period_end")]
    public DateTime PeriodEnd { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("due_date")]
    public DateTime DueDate { get; set; }

    [Column("is_paid")]
    public bool IsPaid { get; set; }

    [Column("paid_from_account_id")]
    public Guid? PaidFromAccountId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
