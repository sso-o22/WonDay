using Microsoft.JSInterop;
using Newtonsoft.Json;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace WonDay.Services;

/// <summary>
/// Supabase 로그인 세션을 브라우저 localStorage에 저장해서, 새로고침하거나
/// 앱을 다시 열었을 때도 로그인이 유지되게 합니다.
/// (localStorage는 sessionStorage와 달리 브라우저를 완전히 닫아도 남아있어요.)
/// Blazor WASM은 브라우저 안에서 직접 도는 방식이라 JS 호출을 동기적으로 할 수 있어서
/// IJSInProcessRuntime을 씁니다 (Blazor Server였다면 이 방식은 못 써요).
/// </summary>
public class BrowserSessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string StorageKey = "wonday_supabase_session";
    private readonly IJSInProcessRuntime _js;

    public BrowserSessionPersistence(IJSInProcessRuntime js)
    {
        _js = js;
    }

    public void SaveSession(Session session)
    {
        try
        {
            var json = JsonConvert.SerializeObject(session);
            _js.InvokeVoid("localStorage.setItem", StorageKey, json);
        }
        catch
        {
            // 저장 실패해도 앱이 죽으면 안 되니 조용히 무시합니다 (다음 로그인 때 다시 시도됨).
        }
    }

    public void DestroySession()
    {
        try
        {
            _js.InvokeVoid("localStorage.removeItem", StorageKey);
        }
        catch
        {
            // ignore
        }
    }

    public Session? LoadSession()
    {
        try
        {
            var json = _js.Invoke<string?>("localStorage.getItem", StorageKey);
            return string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<Session>(json);
        }
        catch
        {
            return null;
        }
    }
}
