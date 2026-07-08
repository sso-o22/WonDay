using WonDay.Models;

namespace WonDay.Services;

public class CategoryRepository
{
    private readonly SupabaseService _supabase;

    public CategoryRepository(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<Category>> GetAllAsync()
    {
        var result = await _supabase.Client.From<Category>().Get();
        return result.Models;
    }

    public async Task<Category> CreateAsync(Category category, bool isShared = true)
    {
        category.OwnerUserId = Guid.Parse(_supabase.CurrentUserId!);
        category.HouseholdId = await _supabase.GetOrCreateHouseholdIdAsync();
        category.IsShared = isShared;

        var result = await _supabase.Client.From<Category>().Insert(category);
        return result.Models.First();
    }

    public async Task UpdateAsync(Category category)
    {
        await _supabase.Client.From<Category>().Update(category);
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supabase.Client.From<Category>().Where(c => c.Id == id).Delete();
    }
}
