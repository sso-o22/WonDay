using WonDay.Models;

namespace WonDay.Services;

public class CategoryBudgetStatus
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }

    public decimal BudgetAmount { get; set; }
    public string BudgetPeriod { get; set; } = "monthly";

    // 예산 주기의 시작일 (일=오늘, 주=이번주 월요일, 월=이번달 1일)
    public DateTime PeriodStart { get; set; }
    public int PeriodLengthDays { get; set; }
    public int DayOfPeriod { get; set; } // 주기 시작일로부터 오늘까지 며칠째인지 (1부터 시작)

    public decimal DailyBudget => BudgetAmount / PeriodLengthDays;

    // 주기 시작일부터 오늘까지 배정된 누적 예산
    public decimal AllowanceToDate => DailyBudget * DayOfPeriod;

    // 주기 시작일부터 오늘까지 실제 사용액
    public decimal SpentPeriodToDate { get; set; }

    public decimal SpentToday { get; set; }

    // 오늘 사용 가능 금액 = 누적 배정액 − 누적 사용액 (이월 반영)
    public decimal AvailableToday => AllowanceToDate - SpentPeriodToDate;

    public double UsageRatio => AllowanceToDate > 0 ? (double)(SpentPeriodToDate / AllowanceToDate) : 0;

    public bool IsOverBudget => AvailableToday < 0;
}

/// <summary>
/// 카테고리별 예산 주기(일/주/월)에 맞춰 "오늘 사용 가능" 금액을 계산합니다.
/// 주기 안에서는 안 쓴 만큼 다음날로 이월되고, 주기가 끝나면(주 → 다음 주, 월 → 다음 달) 리셋됩니다.
/// </summary>
public class BudgetService
{
    private readonly SupabaseService _supabase;

    public BudgetService(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    // 예산 주기의 시작일을 계산합니다. 주 단위는 월요일을 시작으로 잡습니다.
    private static DateTime GetPeriodStart(DateTime today, string period) => period switch
    {
        "daily" => today.Date,
        "weekly" => today.Date.AddDays(-(((int)today.DayOfWeek + 6) % 7)), // 이번 주 월요일
        _ => new DateTime(today.Year, today.Month, 1) // monthly
    };

    private static int GetPeriodLengthDays(DateTime periodStart, string period) => period switch
    {
        "daily" => 1,
        "weekly" => 7,
        _ => DateTime.DaysInMonth(periodStart.Year, periodStart.Month) // monthly
    };

    public async Task<List<CategoryBudgetStatus>> GetTodayStatusAsync(DateTime today)
    {
        var categoriesResult = await _supabase.Client
            .From<Category>()
            .Where(c => c.Type == "expense")
            .Get();

        var allCategories = categoriesResult.Models;

        // 이번 달에 해당하는 "이 달만 다르게" 오버라이드를 조회합니다.
        var overridesResult = await _supabase.Client
            .From<CategoryBudgetOverride>()
            .Where(o => o.Year == today.Year && o.Month == today.Month)
            .Get();

        var overridesByCategory = overridesResult.Models.ToDictionary(o => o.CategoryId, o => o.Amount);

        // 기본 반복 예산이 있거나, 이번 달 오버라이드가 있는 카테고리만 대상으로 합니다.
        var categories = allCategories
            .Where(c => c.BudgetAmount is > 0 || overridesByCategory.ContainsKey(c.Id))
            .ToList();

        if (categories.Count == 0)
            return new List<CategoryBudgetStatus>();

        // 카테고리마다 주기가 다를 수 있어서, 가장 이른 주기 시작일부터 오늘까지를 한 번에 조회
        var earliestPeriodStart = categories
            .Select(c => overridesByCategory.ContainsKey(c.Id)
                ? new DateTime(today.Year, today.Month, 1) // 오버라이드가 있으면 항상 월 단위로 취급
                : GetPeriodStart(today, c.BudgetPeriod))
            .Min();

        var transactions = await _supabase.Client
            .From<Transaction>()
            .Where(t => t.Date >= earliestPeriodStart && t.Date <= today.Date && t.Type == "expense")
            .Get();

        var byCategory = transactions.Models
            .Where(t => t.CategoryId.HasValue)
            .GroupBy(t => t.CategoryId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        return categories.Select(c =>
        {
            var hasOverride = overridesByCategory.TryGetValue(c.Id, out var overrideAmount);

            // 오버라이드가 있으면 이번 달 1일부터를 월 단위 예산으로 취급하고,
            // 없으면 카테고리에 설정된 기본 주기(하루/주/월)를 그대로 씁니다.
            var effectivePeriod = hasOverride ? "monthly" : c.BudgetPeriod;
            var effectiveAmount = hasOverride ? overrideAmount : (c.BudgetAmount ?? 0);

            var periodStart = hasOverride ? new DateTime(today.Year, today.Month, 1) : GetPeriodStart(today, effectivePeriod);
            var periodLength = GetPeriodLengthDays(periodStart, effectivePeriod);
            var dayOfPeriod = (today.Date - periodStart).Days + 1;

            byCategory.TryGetValue(c.Id, out var categoryTransactions);
            categoryTransactions ??= new List<Transaction>();

            var spentPeriodToDate = categoryTransactions
                .Where(t => t.Date >= periodStart)
                .Sum(t => t.Amount);

            var spentToday = categoryTransactions
                .Where(t => t.Date == today.Date)
                .Sum(t => t.Amount);

            return new CategoryBudgetStatus
            {
                CategoryId = c.Id,
                CategoryName = c.Name,
                Color = c.Color,
                Icon = c.Icon,
                BudgetAmount = effectiveAmount,
                BudgetPeriod = effectivePeriod,
                PeriodStart = periodStart,
                PeriodLengthDays = periodLength,
                DayOfPeriod = dayOfPeriod,
                SpentPeriodToDate = spentPeriodToDate,
                SpentToday = spentToday
            };
        }).ToList();
    }
}
