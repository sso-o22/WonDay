using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WonDay.Models;

[Table("recurring_expenses")]
public class RecurringExpense : BaseModel
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

    [Column("amount")]
    public decimal Amount { get; set; }

    [Column("category_id")]
    public Guid? CategoryId { get; set; }

    [Column("payment_account_id")]
    public Guid? PaymentAccountId { get; set; }

    [Column("payment_card_id")]
    public Guid? PaymentCardId { get; set; }

    // 매달 며칠에 나가는지 (31일 넘는 달은 말일로 자동 조정)
    [Column("day_of_month")]
    public int DayOfMonth { get; set; }

    [Column("start_year")]
    public int StartYear { get; set; }

    [Column("start_month")]
    public int StartMonth { get; set; }

    [Column("last_generated_year")]
    public int? LastGeneratedYear { get; set; }

    [Column("last_generated_month")]
    public int? LastGeneratedMonth { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
