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

    public async Task UpdateAsync(Card card)
    {
        await _supabase.Client.From<Card>().Update(card);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supabase.Client.From<Card>().Where(c => c.Id == id).Delete();
    }

    // 카드 거래는 실제 결제일(청구 마감일 기준 다음 결제일)에 지출로 잡히도록 "표시용 날짜"를 계산합니다.
    // 1) 구매일이 이번 달 마감일 이전이면 이번 달 마감 주기, 이후면 다음 달 마감 주기에 속해요.
    // 2) 결제일이 마감일보다 늦은 날짜면 마감과 같은 달에 결제되고, 마감일보다 이른 날짜면 마감 다음 달에 결제돼요.
    public DateTime GetDisplayDate(Transaction t, Dictionary<Guid, Card> cardsById)
    {
        if (t.PaymentCardId is null || !cardsById.TryGetValue(t.PaymentCardId.Value, out var card))
        {
            return t.Date;
        }

        var purchaseDate = t.Date;
        var thisMonthStatement = new DateTime(purchaseDate.Year, purchaseDate.Month,
            Math.Min(card.StatementDay, DateTime.DaysInMonth(purchaseDate.Year, purchaseDate.Month)));

        // 이 구매가 속한 청구주기가 마감되는 달
        var closingMonthAnchor = purchaseDate <= thisMonthStatement ? purchaseDate : purchaseDate.AddMonths(1);

        // 결제일이 마감일과 같거나 늦으면 마감과 같은 달에 결제, 이르면 다음 달에 결제
        var paymentMonthAnchor = card.PaymentDay >= card.StatementDay
            ? closingMonthAnchor
            : closingMonthAnchor.AddMonths(1);

        var day = Math.Min(card.PaymentDay, DateTime.DaysInMonth(paymentMonthAnchor.Year, paymentMonthAnchor.Month));
        return DateTime.SpecifyKind(new DateTime(paymentMonthAnchor.Year, paymentMonthAnchor.Month, day), DateTimeKind.Utc);
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
