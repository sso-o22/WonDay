using WonDay.Models;

namespace WonDay.Services;

/// <summary>
/// 배우자 초대 흐름: 초대 생성 → 상대방이 이메일로 확인 → 수락 시 같은 household에 합류
/// </summary>
public class HouseholdRepository
{
    private readonly SupabaseService _supabase;

    public HouseholdRepository(SupabaseService supabase)
    {
        _supabase = supabase;
    }

    // 배우자 이메일로 초대 생성
    public async Task<HouseholdInvite> InviteAsync(string invitedEmail)
    {
        var householdId = await _supabase.GetOrCreateHouseholdIdAsync();
        var userId = Guid.Parse(_supabase.CurrentUserId!);

        var invite = new HouseholdInvite
        {
            HouseholdId = householdId,
            InvitedEmail = invitedEmail,
            InvitedBy = userId,
            Status = "pending"
        };

        var result = await _supabase.Client
            .From<HouseholdInvite>()
            .Insert(invite);

        return result.Models.First();
    }

    // 나에게 온 대기 중인 초대 목록 조회 (로그인한 이메일 기준)
    public async Task<List<HouseholdInvite>> GetPendingInvitesForMeAsync()
    {
        var result = await _supabase.Client
            .From<HouseholdInvite>()
            .Where(i => i.Status == "pending")
            .Get();

        return result.Models;
    }

    // 초대 수락: 기존에 혼자 쓰던 household는 그대로 두고,
    // 이 계정을 초대받은 household의 구성원으로 추가합니다.
    // (기존 개인 데이터를 새 household로 옮기는 마이그레이션은 필요 시 별도 구현)
    public async Task AcceptInviteAsync(Guid inviteId, Guid householdId)
    {
        var userId = Guid.Parse(_supabase.CurrentUserId!);

        await _supabase.Client.From<HouseholdMember>().Insert(new HouseholdMember
        {
            HouseholdId = householdId,
            UserId = userId,
            Role = "member"
        });

        await _supabase.Client
            .From<HouseholdInvite>()
            .Where(i => i.Id == inviteId)
            .Set(i => i.Status, "accepted")
            .Update();
    }
}
