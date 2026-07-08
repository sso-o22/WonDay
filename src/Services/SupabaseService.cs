using Supabase;

namespace WonDay.Services;

/// <summary>
/// Supabase 클라이언트를 앱 전역에서 하나만 생성해 공유하는 서비스.
/// Program.cs에서 싱글톤으로 등록해서 사용합니다.
/// </summary>
public class SupabaseService
{
    public Client Client { get; }

    public SupabaseService(string url, string anonKey)
    {
        var options = new SupabaseOptions
        {
            AutoConnectRealtime = false // 실시간 구독이 필요해지면 true로 변경
        };

        Client = new Client(url, anonKey, options);
    }

    public async Task InitializeAsync()
    {
        await Client.InitializeAsync();
    }

    /// <summary>
    /// 구글 로그인 URL과 PKCE 검증값을 생성합니다.
    /// 이 메서드는 브라우저를 이동시키지 않으니, 호출한 쪽에서 PKCEVerifier를 저장해두고
    /// Uri로 직접 이동(JS window.location)시켜야 합니다.
    /// </summary>
    public async Task<Supabase.Gotrue.ProviderAuthState> GetGoogleSignInStateAsync(string redirectUrl)
    {
        return await Client.Auth.SignIn(Supabase.Gotrue.Constants.Provider.Google, new Supabase.Gotrue.SignInOptions
        {
            FlowType = Supabase.Gotrue.Constants.OAuthFlowType.PKCE,
            RedirectTo = redirectUrl
        });
    }

    /// <summary>
    /// 구글 로그인 후 돌아온 콜백에서, 저장해뒀던 PKCEVerifier와 URL의 code 파라미터로
    /// 실제 로그인 세션을 완성합니다.
    /// </summary>
    public async Task<Supabase.Gotrue.Session?> ExchangeCodeForSessionAsync(string pkceVerifier, string code)
    {
        return await Client.Auth.ExchangeCodeForSession(pkceVerifier, code);
    }

    public async Task SignOutAsync()
    {
        await Client.Auth.SignOut();
    }

    public bool IsLoggedIn => Client.Auth.CurrentUser is not null;

    public string? CurrentUserId => Client.Auth.CurrentUser?.Id;

    private Guid? _cachedHouseholdId;

    /// <summary>
    /// 로그인한 사용자가 속한 household의 ID를 가져옵니다.
    /// 아직 어느 household에도 속해있지 않으면(첫 로그인) 자동으로 하나 만들어줍니다.
    /// 앱 시작 시 한 번 호출해서 캐싱해두고 계속 재사용하세요.
    /// </summary>
    public async Task<Guid> GetOrCreateHouseholdIdAsync()
    {
        if (_cachedHouseholdId is not null)
            return _cachedHouseholdId.Value;

        var userId = Guid.Parse(CurrentUserId!);

        var membership = await Client
            .From<Models.HouseholdMember>()
            .Where(m => m.UserId == userId)
            .Get();

        if (membership.Models.Count > 0)
        {
            _cachedHouseholdId = membership.Models.First().HouseholdId;
            return _cachedHouseholdId.Value;
        }

        // 첫 로그인: 본인만 속한 household를 자동 생성
        var household = await Client
            .From<Models.Household>()
            .Insert(new Models.Household { Name = "우리집" });

        var householdId = household.Models.First().Id;

        await Client.From<Models.HouseholdMember>().Insert(new Models.HouseholdMember
        {
            HouseholdId = householdId,
            UserId = userId,
            Role = "owner"
        });

        _cachedHouseholdId = householdId;
        return householdId;
    }
}
