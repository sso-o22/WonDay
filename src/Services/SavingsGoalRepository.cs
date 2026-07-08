using WonDay.Models;

namespace WonDay.Services;

public class SavingsGoalRepository
{
    private readonly SupabaseService _supabase;

    public SavingsGoalRepository(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<SavingsGoal>> GetAllAsync()
    {
        var result = await _supabase.Client.From<SavingsGoal>().Get();
        return result.Models;
    }

    public async Task<SavingsGoal> CreateAsync(SavingsGoal goal, bool isShared)
    {
        goal.OwnerUserId = Guid.Parse(_supabase.CurrentUserId!);
        goal.HouseholdId = await _supabase.GetOrCreateHouseholdIdAsync();
        goal.IsShared = isShared;

        var result = await _supabase.Client.From<SavingsGoal>().Insert(goal);
        return result.Models.First();
    }

    public async Task DeleteAsync(Guid id)
    {
        await _supabase.Client.From<SavingsGoal>().Where(g => g.Id == id).Delete();
    }

    public async Task<List<GoalContribution>> GetContributionsAsync(Guid goalId)
    {
        var result = await _supabase.Client
            .From<GoalContribution>()
            .Where(c => c.GoalId == goalId)
            .Get();

        return result.Models;
    }

    public async Task<GoalContribution> AddContributionAsync(Guid goalId, decimal amount, DateTime date, string source = "manual")
    {
        var contribution = new GoalContribution
        {
            GoalId = goalId,
            Amount = amount,
            Date = date,
            Source = source,
            OwnerUserId = Guid.Parse(_supabase.CurrentUserId!),
            HouseholdId = await _supabase.GetOrCreateHouseholdIdAsync()
        };

        var result = await _supabase.Client.From<GoalContribution>().Insert(contribution);
        return result.Models.First();
    }

    public decimal CalculateCurrentAmount(List<GoalContribution> contributions) => contributions.Sum(c => c.Amount);

    // 매달 납입액 monthlyAmount를 months개월 동안 이자율 annualRatePercent(연리)로 넣었을 때 예상 만기액
    public static decimal ProjectFutureValue(decimal currentAmount, decimal monthlyAmount, int months, decimal annualRatePercent)
    {
        var monthlyRate = annualRatePercent / 100 / 12;
        var total = currentAmount;

        for (var i = 0; i < months; i++)
        {
            total = monthlyRate > 0 ? total * (1 + monthlyRate) + monthlyAmount : total + monthlyAmount;
        }

        return Math.Round(total);
    }
}
