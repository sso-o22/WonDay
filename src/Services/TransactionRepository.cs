using WonDay.Models;
using Supabase.Postgrest;

namespace WonDay.Services;

/// <summary>
/// Transaction 테이블 CRUD 예시.
/// Account, Category, SavingsGoal 등도 같은 패턴으로 리포지토리를 만들면 됩니다.
/// RLS가 걸려 있어서 별도로 user_id 필터를 안 걸어도 로그인한 본인 데이터만 돌아오지만,
/// insert 시에는 user_id를 직접 채워서 보내야 합니다.
/// </summary>
public class TransactionRepository
{
    private readonly SupabaseService _supabase;

    public TransactionRepository(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    // 특정 월의 거래 내역 조회 (캘린더 홈 화면용)
    public async Task<List<Transaction>> GetByMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var result = await _supabase.Client
            .From<Transaction>()
            .Where(t => t.Date >= start && t.Date <= end)
            .Order(t => t.Date, Constants.Ordering.Ascending)
            .Get();

        return result.Models;
    }

    // 특정 날짜의 거래 내역 조회 (날짜 탭했을 때 상세 리스트용)
    public async Task<List<Transaction>> GetByDateAsync(DateTime date)
    {
        var result = await _supabase.Client
            .From<Transaction>()
            .Where(t => t.Date == date.Date)
            .Get();

        return result.Models;
    }

    // isShared: true면 배우자도 보이는 공동 거래, false면 나만 보이는 개인 거래
    public async Task<Transaction> CreateAsync(Transaction transaction, bool isShared)
    {
        transaction.OwnerUserId = Guid.Parse(_supabase.CurrentUserId!);
        transaction.HouseholdId = await _supabase.GetOrCreateHouseholdIdAsync();
        transaction.IsShared = isShared;

        var result = await _supabase.Client
            .From<Transaction>()
            .Insert(transaction);

        return result.Models.First();
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        await _supabase.Client
            .From<Transaction>()
            .Update(transaction);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supabase.Client
            .From<Transaction>()
            .Where(t => t.Id == id)
            .Delete();
    }
}
