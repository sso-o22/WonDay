using WonDay.Models;

namespace WonDay.Services;

public class CardRepository
{
    private readonly SupabaseService _supabase;

    public CardRepository(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<Card>> GetAllAsync()
    {
        var result = await _supabase.Client.From<Card>().Get();
        return result.Models;
    }

    public async Task<Card> CreateAsync(Card card, bool isShared)
    {
        card.OwnerUserId = Guid.Parse(_supabase.CurrentUserId!);
        card.HouseholdId = await _supabase.GetOrCreateHouseholdIdAsync();
        card.IsShared = isShared;

        var result = await _supabase.Client.From<Card>().Insert(card);
        return result.Models.First();
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supabase.Client.From<Card>().Where(c => c.Id == id).Delete();
    }

    // 이번 청구 기간(마감일 기준) 동안 이 카드로 쓴 금액 합계
    public decimal CalculateCurrentStatementAmount(Card card, List<Transaction> allTransactions, DateTime today)
    {
        var (periodStart, periodEnd) = GetCurrentPeriod(card.StatementDay, today);

        return allTransactions
            .Where(t => t.PaymentCardId == card.Id && t.Date >= periodStart && t.Date <= periodEnd)
            .Sum(t => t.Amount);
    }

    private static (DateTime start, DateTime end) GetCurrentPeriod(int statementDay, DateTime today)
    {
        // 이번 달 마감일이 아직 안 지났으면: 저번 달 마감일 다음날 ~ 이번 달 마감일
        // 이미 지났으면: 이번 달 마감일 다음날 ~ 다음 달 마감일
        var thisMonthStatement = new DateTime(today.Year, today.Month, Math.Min(statementDay, DateTime.DaysInMonth(today.Year, today.Month)));

        if (today <= thisMonthStatement)
        {
            var prevMonth = today.AddMonths(-1);
            var prevStatement = new DateTime(prevMonth.Year, prevMonth.Month, Math.Min(statementDay, DateTime.DaysInMonth(prevMonth.Year, prevMonth.Month)));
            return (prevStatement.AddDays(1), thisMonthStatement);
        }
        else
        {
            var nextMonth = today.AddMonths(1);
            var nextStatement = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(statementDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month)));
            return (thisMonthStatement.AddDays(1), nextStatement);
        }
    }
}
