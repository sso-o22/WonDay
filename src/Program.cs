using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WonDay.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// appsettings.json (wwwroot) 에서 Supabase URL/키를 읽어옵니다.
// 절대 소스코드에 직접 하드코딩하지 마세요 - anon key는 공개돼도 되지만
// 프로젝트별로 값이 다르니 설정 파일로 분리하는 게 관리하기 편합니다.
var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Supabase:Url 설정이 없습니다. wwwroot/appsettings.json을 확인하세요.");
var supabaseAnonKey = builder.Configuration["Supabase:AnonKey"]
    ?? throw new InvalidOperationException("Supabase:AnonKey 설정이 없습니다. wwwroot/appsettings.json을 확인하세요.");

var supabaseService = new SupabaseService(supabaseUrl, supabaseAnonKey);
await supabaseService.InitializeAsync();

builder.Services.AddSingleton(supabaseService);
builder.Services.AddScoped<TransactionRepository>();
builder.Services.AddScoped<HouseholdRepository>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<CardRepository>();
builder.Services.AddScoped<SavingsGoalRepository>();
// 이후 AccountRepository, CategoryRepository, SavingsGoalRepository 등도
// TransactionRepository와 같은 패턴으로 추가해서 여기에 등록하면 됩니다.

await builder.Build().RunAsync();
