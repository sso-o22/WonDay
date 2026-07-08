using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WonDay.Models;

[Table("installment_plans")]
public class InstallmentPlan : BaseModel
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

    [Column("category_id")]
    public Guid? CategoryId { get; set; }

    [Column("merchant")]
    public string Merchant { get; set; } = string.Empty;

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    [Column("months_count")]
    public int MonthsCount { get; set; }

    [Column("monthly_amount")]
    public decimal MonthlyAmount { get; set; }

    // 첫 할부금이 청구되는 달의 1일
    [Column("start_date")]
    public DateTime StartDate { get; set; }

    [Column("months_generated")]
    public int MonthsGenerated { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
