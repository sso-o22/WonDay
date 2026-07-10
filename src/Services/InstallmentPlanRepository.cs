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
    /// 등록하는 즉시 전체 회차를 미리 다 만들어둡니다 (몇 달 앞 카드 대금까지 바로 반영돼요).
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
        var created = result.Models.First();

        await RegenerateRemainingAsync(created);

        return created;
    }

    /// <summary>
    /// 할부 정보를 수정합니다. 이미 지난(실제로 청구된) 회차는 그대로 두고,
    /// 아직 안 지난 회차만 지운 뒤 새 총액/개월수/카드/카테고리 기준으로 다시 만듭니다.
    /// </summary>
    public async Task UpdateAsync(InstallmentPlan plan)
    {
        await RegenerateRemainingAsync(plan);
    }

    public async Task DeleteAsync(Guid id)
    {
        // 계획만 지우면 이미 생성해둔 거래가 고아로 남아서 카드 대금에 계속 섞여요.
        // 계획을 지울 때 그 계획으로 생성된 거래도 같이 지웁니다.
        await _supabase.Client.From<Transaction>().Where(t => t.InstallmentPlanId == id).Delete();
        await _supabase.Client.From<InstallmentPlan>().Where(p => p.Id == id).Delete();
    }

    /// <summary>
    /// 예전 방식(달마다 하나씩 생성)으로 만들어졌던 할부 계획 중 아직 다 못 채운 게 있으면
    /// 한 번에 나머지 회차를 전부 채웁니다. 로그인할 때 호출해두면 알아서 정리돼요.
    /// (새로 등록하는 할부는 CreateAsync에서 바로 전체 생성되므로 여기서 더 할 일이 없어요.)
    /// </summary>
    public async Task GenerateDueTransactionsAsync()
    {
        var plans = await GetAllAsync();

        foreach (var plan in plans.Where(p => p.MonthsGenerated < p.MonthsCount))
        {
            await RegenerateRemainingAsync(plan);
        }
    }

    /// <summary>
    /// 이 계획의 "아직 지나지 않은(미래)" 회차 거래를 전부 지우고, 새 총액/개월수 기준으로
    /// 다시 만듭니다. 이미 지난(오늘 이전) 회차는 실제로 청구된 내역이니 건드리지 않아요.
    /// 새로 만드는 계획은 지난 회차가 0개이므로 사실상 전체가 새로 생성됩니다.
    /// </summary>
    private async Task RegenerateRemainingAsync(InstallmentPlan plan)
    {
        var today = DateTime.Today;

        var existingResult = await _supabase.Client
            .From<Transaction>()
            .Where(t => t.InstallmentPlanId == plan.Id)
            .Get();

        var past = existingResult.Models.Where(t => t.Date <= today).OrderBy(t => t.Date).ToList();
        var future = existingResult.Models.Where(t => t.Date > today).ToList();

        var pastCount = past.Count;
        var remainingMonths = plan.MonthsCount - pastCount;

        if (remainingMonths < 0)
        {
            throw new InvalidOperationException("이미 지난 회차보다 적은 개월수로는 바꿀 수 없어요.");
        }

        // 아직 안 지난 회차는 지우고 새 값으로 다시 만듭니다.
        foreach (var f in future)
        {
            await _supabase.Client.From<Transaction>().Where(t => t.Id == f.Id).Delete();
        }

        var pastSum = past.Sum(t => t.Amount);
        var remainingAmount = plan.TotalAmount - pastSum;
        var perMonth = remainingMonths > 0
            ? Math.Round(remainingAmount / remainingMonths, 0, MidpointRounding.AwayFromZero)
            : 0;

        for (var i = 0; i < remainingMonths; i++)
        {
            var idx = pastCount + i;
            var isLastOfPlan = idx == plan.MonthsCount - 1;
            var amount = isLastOfPlan
                ? remainingAmount - perMonth * (remainingMonths - 1) // 마지막 회차는 반올림 오차 보정
                : perMonth;
            var targetMonth = plan.StartDate.AddMonths(idx);

            var transaction = new Transaction
            {
                Date = targetMonth,
                Amount = amount,
                Type = "expense",
                CategoryId = plan.CategoryId,
                PaymentCardId = plan.CardId,
                Memo = $"{plan.Merchant} 할부 {idx + 1}/{plan.MonthsCount}",
                InstallmentPlanId = plan.Id,
                HouseholdId = plan.HouseholdId,
                OwnerUserId = plan.OwnerUserId,
                IsShared = plan.IsShared
            };

            await _supabase.Client.From<Transaction>().Insert(transaction);
        }

        plan.MonthlyAmount = perMonth;
        plan.MonthsGenerated = plan.MonthsCount;
        await _supabase.Client.From<InstallmentPlan>().Update(plan);
    }
}
