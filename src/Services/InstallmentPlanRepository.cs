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
            StartDate = DateTime.SpecifyKind(new DateTime(startDate.Year, startDate.Month, 1), DateTimeKind.Utc),
            MonthsGenerated = 0,
            OwnerUserId = Guid.Parse(_supabase.CurrentUserId!),
            HouseholdId = await _supabase.GetOrCreateHouseholdIdAsync(),
            IsShared = isShared
        };

        var result = await _supabase.Client.From<InstallmentPlan>().Insert(plan);
        return result.Models.First();
    }

    /// <summary>
    /// 할부 정보를 수정합니다. 총 금액/개월수가 바뀌면 월 납입금도 다시 계산돼요.
    /// 이미 생성된 과거 거래 내역은 그대로 두고, 앞으로 생성될 회차부터 새 금액이 적용됩니다.
    /// </summary>
    public async Task UpdateAsync(InstallmentPlan plan)
    {
        plan.MonthlyAmount = Math.Round(plan.TotalAmount / plan.MonthsCount, 0, MidpointRounding.AwayFromZero);
        await _supabase.Client.From<InstallmentPlan>().Update(plan);
    }

    public async Task DeleteAsync(Guid id)
    {
        // 계획만 지우면 이미 생성해둔 거래가 고아로 남아서 카드 대금에 계속 섞여요.
        // 계획을 지울 때 그 계획으로 생성된 거래도 같이 지웁니다.
        await _supabase.Client.From<Transaction>().Where(t => t.InstallmentPlanId == id).Delete();
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
            // 방어 로직: "여기까지 만들었다"는 기록(months_generated)이 서버에 저장이 안 됐을 수도 있으니,
            // 이 계획으로 실제 생성된 거래 개수를 다시 세어서 기록이 뒤처져 있으면 바로잡습니다.
            // (예: RLS 설정 문제로 이전 업데이트가 조용히 실패했던 경우에도 중복 생성을 막아줘요.)
            var existingResult = await _supabase.Client
                .From<Transaction>()
                .Where(t => t.InstallmentPlanId == plan.Id)
                .Get();
            var existingCount = existingResult.Models.Count;
            if (existingCount > plan.MonthsGenerated)
            {
                plan.MonthsGenerated = existingCount;
            }

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
