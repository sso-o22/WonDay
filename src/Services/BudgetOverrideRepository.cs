using WonDay.Models;

namespace WonDay.Services;

public class BudgetOverrideRepository
{
    private readonly SupabaseService _supabase;

    public BudgetOverrideRepository(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<CategoryBudgetOverride>> GetForMonthAsync(int year, int month)
    {
        var result = await _supabase.Client
            .From<CategoryBudgetOverride>()
            .Where(o => o.Year == year && o.Month == month)
            .Get();

        return result.Models;
    }

    /// <summary>해당 카테고리·연·월 조합에 이미 오버라이드가 있으면 갱신, 없으면 새로 만듭니다.</summary>
    public async Task SetAsync(Guid categoryId, int year, int month, decimal amount, bool isShared)
    {
        var existing = await _supabase.Client
            .From<CategoryBudgetOverride>()
            .Where(o => o.CategoryId == categoryId && o.Year == year && o.Month == month)
            .Get();

        if (existing.Models.Count > 0)
        {
            var row = existing.Models.First();
            row.Amount = amount;
            await _supabase.Client.From<CategoryBudgetOverride>().Update(row);
        }
        else
        {
            await _supabase.Client.From<CategoryBudgetOverride>().Insert(new CategoryBudgetOverride
            {
                CategoryId = categoryId,
                Year = year,
                Month = month,
                Amount = amount,
                IsShared = isShared,
                OwnerUserId = Guid.Parse(_supabase.CurrentUserId!),
                HouseholdId = await _supabase.GetOrCreateHouseholdIdAsync()
            });
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supabase.Client.From<CategoryBudgetOverride>().Where(o => o.Id == id).Delete();
    }
}
