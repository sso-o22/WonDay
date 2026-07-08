using Microsoft.JSInterop;

namespace WonDay.Services;

/// <summary>
/// 테마 색상(purple/green/yellow/pink/sky)과 라이트/다크 모드를 localStorage에 저장하고,
/// html 태그의 data-theme / data-mode 속성을 바꿔서 CSS 변수가 실제로 반영되게 합니다.
/// </summary>
public class ThemeService
{
    private const string ThemeKey = "wonday_theme_color";
    private const string ModeKey = "wonday_theme_mode";

    private readonly IJSInProcessRuntime _js;

    public string CurrentTheme { get; private set; } = "purple";
    public string CurrentMode { get; private set; } = "light";

    public static readonly (string Id, string Label)[] Themes =
    {
        ("purple", "보라"),
        ("green", "연두"),
        ("yellow", "노랑"),
        ("pink", "분홍"),
        ("sky", "하늘"),
    };

    public ThemeService(IJSInProcessRuntime js)
    {
        _js = js;
    }

    // 앱 시작 시 한 번 호출해서 저장된 값(없으면 기본값)을 화면에 적용합니다.
    public void Initialize()
    {
        CurrentTheme = _js.Invoke<string?>("localStorage.getItem", ThemeKey) ?? "purple";
        CurrentMode = _js.Invoke<string?>("localStorage.getItem", ModeKey) ?? "light";
        Apply();
    }

    public void SetTheme(string theme)
    {
        CurrentTheme = theme;
        _js.InvokeVoid("localStorage.setItem", ThemeKey, theme);
        Apply();
    }

    public void SetMode(string mode)
    {
        CurrentMode = mode;
        _js.InvokeVoid("localStorage.setItem", ModeKey, mode);
        Apply();
    }

    private void Apply()
    {
        _js.InvokeVoid("document.documentElement.setAttribute", "data-theme", CurrentTheme);
        _js.InvokeVoid("document.documentElement.setAttribute", "data-mode", CurrentMode);
    }
}
