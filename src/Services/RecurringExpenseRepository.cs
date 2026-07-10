using WonDay.Models;

namespace WonDay.Services;

public class RecurringExpenseRepository
{
    private readonly SupabaseService _supabase;

    public RecurringExpenseRepository(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<RecurringExpense>> GetAllAsync()
    {
        var result = await _supabase.Client.From<RecurringExpense>().Get();
        return result.Models;
    }

    public async Task<RecurringExpense> CreateAsync(
        string name, decimal amount, Guid? categoryId, Guid? accountId, Guid? cardId, int dayOfMonth, DateTime startMonth, bool isShared)
    {
        var item = new RecurringExpense
        {
            Name = name,
            Amount = amount,
            CategoryId = categoryId,
            PaymentAccountId = accountId,
            PaymentCardId = cardId,
            DayOfMonth = dayOfMonth,
            StartYear = startMonth.Year,
            StartMonth = startMonth.Month,
            OwnerUserId = Guid.Parse(_supabase.CurrentUserId!),
            HouseholdId = await _supabase.GetOrCreateHouseholdIdAsync(),
            IsShared = isShared
        };

        var result = await _supabase.Client.From<RecurringExpense>().Insert(item);
        return result.Models.First();
    }

    public async Task SetActiveAsync(RecurringExpense item, bool isActive)
    {
        item.IsActive = isActive;
        await _supabase.Client.From<RecurringExpense>().Update(item);
    }

    public async Task UpdateAsync(RecurringExpense item)
    {
        await _supabase.Client.From<RecurringExpense>().Update(item);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supabase.Client.From<RecurringExpense>().Where(r => r.Id == id).Delete();
    }

    /// <summary>
    /// 활성 상태인 정기결제 중, 오늘까지 지난 달인데 아직 기록 안 된 달들을 전부 거래로 생성합니다.
    /// 할부와 마찬가지로 앱을 열 때마다 호출해서 "놓친 만큼 자동으로 채워지게" 만들어요.
    /// </summary>
    public async Task GenerateDueTransactionsAsync()
    {
        var today = DateTime.Today;
        var items = await GetAllAsync();

        foreach (var item in items.Where(i => i.IsActive))
        {
            // 아직 한 번도 생성 안 했으면 시작월부터, 아니면 마지막 생성월 다음 달부터 시작
            var (year, month) = item.LastGeneratedYear is not null && item.LastGeneratedMonth is not null
                ? AddMonth(item.LastGeneratedYear.Value, item.LastGeneratedMonth.Value)
                : (item.StartYear, item.StartMonth);

            var changed = false;

            while (new DateTime(year, month, 1) <= today)
            {
                var day = Math.Min(item.DayOfMonth, DateTime.DaysInMonth(year, month));
                var date = DateTime.SpecifyKind(new DateTime(year, month, day), DateTimeKind.Utc);

                if (date <= today)
                {
                    var transaction = new Transaction
                    {
                        Date = date,
                        Amount = item.Amount,
                        Type = "expense",
                        CategoryId = item.CategoryId,
                        PaymentAccountId = item.PaymentAccountId,
                        PaymentCardId = item.PaymentCardId,
                        Memo = item.Name,
                        RecurringExpenseId = item.Id,
                        HouseholdId = item.HouseholdId,
                        OwnerUserId = item.OwnerUserId,
                        IsShared = item.IsShared
                    };

                    await _supabase.Client.From<Transaction>().Insert(transaction);

                    item.LastGeneratedYear = year;
                    item.LastGeneratedMonth = month;
                    changed = true;
                }

                (year, month) = AddMonth(year, month);
            }

            if (changed)
            {
                await _supabase.Client.From<RecurringExpense>().Update(item);
            }
        }
    }

    private static (int year, int month) AddMonth(int year, int month)
        => month == 12 ? (year + 1, 1) : (year, month + 1);
}
