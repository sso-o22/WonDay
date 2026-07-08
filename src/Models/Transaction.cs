using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace WonDay.Models;

[Table("transactions")]
public class Transaction : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    [Column("owner_user_id")]
    public Guid OwnerUserId { get; set; }

    [Column("is_shared")]
    public bool IsShared { get; set; }

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("amount")]
    public decimal Amount { get; set; }

    // "income" / "expense" / "transfer"
    [Column("type")]
    public string Type { get; set; } = "expense";

    [Column("category_id")]
    public Guid? CategoryId { get; set; }

    // 계좌 또는 카드 중 하나만 채워짐
    [Column("payment_account_id")]
    public Guid? PaymentAccountId { get; set; }

    [Column("payment_card_id")]
    public Guid? PaymentCardId { get; set; }

    // 이체일 때만 사용
    [Column("to_account_id")]
    public Guid? ToAccountId { get; set; }

    [Column("memo")]
    public string? Memo { get; set; }

    // 할부에서 자동 생성된 거래면 원본 할부 계획을 가리켜요.
    [Column("installment_plan_id")]
    public Guid? InstallmentPlanId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
