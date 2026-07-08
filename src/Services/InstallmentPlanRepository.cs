using WonDay.Models;

namespace WonDay.Services;

public class InstallmentPlanRepository
{
    private readonly SupabaseService _supabase;

    public InstallmentPlanRepository(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<InstallmentPlan>> GetAllAsync()
    {
        var result = await _supabase.Client.From<InstallmentPlan>().Get();
        return result.Models;
    }

    /// <summary>
    /// 새 할부를 등록합니다. startDate는 첫 할부금이 청구되는 달의 1일로 맞춰서 넘겨주세요.
    /// </summary>
    public async Task<InstallmentPlan> CreateAsync(
        Guid cardId, Guid? categoryId, string merchant, decimal totalAmount, int monthsCount, DateTime startDate, bool isShared)
    {
        var plan = new InstallmentPlan
        {
            CardId = cardId,
            CategoryId = categoryId,
            Merchant = merchant,
            TotalAmount = totalAmount,
            MonthsCount = monthsCount,
            MonthlyAmount = Math.Round(totalAmount / monthsCount, 0, MidpointRounding.AwayFromZero),
            StartDate = new DateTime(startDate.Year, startDate.Month, 1),
            MonthsGenerated = 0,
            OwnerUserId = Guid.Parse(_supabase.CurrentUserId!),
            HouseholdId = await _supabase.GetOrCreateHouseholdIdAsync(),
            IsShared = isShared
        };

        var result = await _supabase.Client.From<InstallmentPlan>().Insert(plan);
        return result.Models.First();
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supabase.Client.From<InstallmentPlan>().Where(p => p.Id == id).Delete();
    }

    /// <summary>
    /// 오늘 날짜 기준으로, 아직 기록 안 된 할부금 중 이미 지난 달들을 전부 거래 내역으로 생성합니다.
    /// 로그인 후 한 번 호출해두면, 1일이 지나서 다음에 앱을 열었을 때 그 사이 놓친 할부금이 자동으로 채워져요.
    /// </summary>
    public async Task GenerateDueTransactionsAsync()
    {
        var today = DateTime.Today;
        var plans = await GetAllAsync();

        foreach (var plan in plans.Where(p => p.MonthsGenerated < p.MonthsCount))
        {
            while (plan.MonthsGenerated < plan.MonthsCount)
            {
                var targetMonth = plan.StartDate.AddMonths(plan.MonthsGenerated);
                if (targetMonth > today) break; // 아직 안 지난 달이면 여기서 멈춤

                var isLastInstallment = plan.MonthsGenerated == plan.MonthsCount - 1;
                var amount = isLastInstallment
                    ? plan.TotalAmount - plan.MonthlyAmount * (plan.MonthsCount - 1) // 마지막 달은 반올림 오차 보정
                    : plan.MonthlyAmount;

                var transaction = new Transaction
                {
                    Date = targetMonth,
                    Amount = amount,
                    Type = "expense",
                    CategoryId = plan.CategoryId,
                    PaymentCardId = plan.CardId,
                    Memo = $"{plan.Merchant} 할부 {plan.MonthsGenerated + 1}/{plan.MonthsCount}",
                    InstallmentPlanId = plan.Id,
                    HouseholdId = plan.HouseholdId,
                    OwnerUserId = plan.OwnerUserId,
                    IsShared = plan.IsShared
                };

                await _supabase.Client.From<Transaction>().Insert(transaction);

                plan.MonthsGenerated += 1;
                await _supabase.Client.From<InstallmentPlan>().Update(plan);
            }
        }
    }
}
