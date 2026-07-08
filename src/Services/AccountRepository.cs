using WonDay.Models;

namespace WonDay.Services;

public class AccountRepository
{
    private readonly SupabaseService _supabase;

    public AccountRepository(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<Account>> GetAllAsync()
    {
        var result = await _supabase.Client.From<Account>().Get();
        return result.Models;
    }

    public async Task<Account> CreateAsync(Account account, bool isShared)
    {
        account.OwnerUserId = Guid.Parse(_supabase.CurrentUserId!);
        account.HouseholdId = await _supabase.GetOrCreateHouseholdIdAsync();
        account.IsShared = isShared;

        var result = await _supabase.Client.From<Account>().Insert(account);
        return result.Models.First();
    }

    public async Task UpdateAsync(Account account)
    {
        await _supabase.Client.From<Account>().Update(account);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supabase.Client.From<Account>().Where(a => a.Id == id).Delete();
    }

    // 계좌 잔액 = 시작 잔액 + 이 계좌로 들어온 거래 - 이 계좌에서 나간 거래
    public decimal CalculateBalance(Account account, List<Transaction> allTransactions)
    {
        var balance = account.InitialBalance;

        foreach (var t in allTransactions)
        {
            if (t.PaymentAccountId == account.Id)
            {
                balance += t.Type switch
                {
                    "income" => t.Amount,
                    "expense" => -t.Amount,
                    "transfer" => -t.Amount, // 이 계좌에서 나간 이체
                    _ => 0
                };
            }

            if (t.Type == "transfer" && t.ToAccountId == account.Id)
            {
                balance += t.Amount; // 이 계좌로 들어온 이체
            }
        }

        return balance;
    }
}
