using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using WonDay;
using WonDay.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// appsettings.json (wwwroot) 에서 Supabase URL/키를 읽어옵니다.
var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Supabase:Url 설정이 없습니다. wwwroot/appsettings.json을 확인하세요.");
var supabaseAnonKey = builder.Configuration["Supabase:AnonKey"]
    ?? throw new InvalidOperationException("Supabase:AnonKey 설정이 없습니다. wwwroot/appsettings.json을 확인하세요.");

// SupabaseService는 JS와 통신해야 해서(localStorage 접근), 앱이 실제로 켜진 뒤에
// 만들어야 합니다. 그래서 여기서는 바로 만들지 않고, 팩토리로 등록해뒀다가
// 아래에서 host.Build() 이후에 생성합니다.
builder.Services.AddSingleton(sp =>
{
    var jsRuntime = (IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>();
    return new SupabaseService(supabaseUrl, supabaseAnonKey, jsRuntime);
});

builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<HouseholdRepository>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<CardRepository>();
builder.Services.AddScoped<SavingsGoalRepository>();

builder.Services.AddSingleton(sp =>
{
    var jsRuntime = (IJSInProcessRuntime)sp.GetRequiredService<IJSRuntime>();
    return new ThemeService(jsRuntime);
});

var host = builder.Build();

// 이 시점부터는 JS interop이 가능해서, Supabase 초기화(저장된 로그인 세션 복원 포함)를 진행합니다.
var supabaseService = host.Services.GetRequiredService<SupabaseService>();
await supabaseService.InitializeAsync();

host.Services.GetRequiredService<ThemeService>().Initialize();

await host.RunAsync();
