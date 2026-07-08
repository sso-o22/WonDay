@page "/categories"
@inject CategoryRepository CategoryRepo

<h1 style="font-size: 19px; margin: 0 0 16px;">카테고리</h1>

@if (_error is not null && !_showAddForm)
{
    <div class= "card" style = "background: var(--color-surface-alt);" >
        < p class= "text-danger" style = "font-size: 13px; margin: 0; word-break: break-all;" > @_error </ p >
    </ div >
}

< div class= "toggle-group" style = "margin-bottom: 16px;" >
    < button class= "@(_filterType == "expense" ? "active" : "")" @onclick='() => _filterType = "expense"'>지출</button>
    <button class= "@(_filterType == "income" ? "active" : "")" @onclick='() => _filterType = "income"'>수입</button>
</div>

@if (FilteredCategories.Count == 0)
{
    <div class= "empty-state" > 아직 등록된 카테고리가 없어요.</div>
}
else
{
    @foreach(var c in FilteredCategories)
    {
        < div class= "card" style = "display: flex; align-items: center; justify-content: space-between;" >
            < div style = "display: flex; align-items: center; gap: 10px;" >
                < div style = "width: 32px; height: 32px; border-radius: 50%; background: @(c.Color ?? "#b3a4d9")33; display: flex; align-items: center; justify-content: center;">
                    < i class= "ti @(c.Icon ?? "ti - tag")" style = "color: @(c.Color ?? "#b3a4d9");" aria-hidden="true"></i>
                </ div >
                < div >
                    < div style = "font-size: 15px; font-weight: 600;" > @c.Name </ div >
                    @if(c.BudgetAmount is > 0)
                    {
                        < div style = "font-size: 13px; color: var(--color-text-muted);" > @PeriodLabel(c.BudgetPeriod) 예산 @c.BudgetAmount.Value.ToString("N0")원 </ div >
                    }
                </ div >
            </ div >
            < button class= "btn" style = "padding: 6px 10px; font-size: 13px;" @onclick = "() => DeleteCategory(c.Id)" > 삭제 </ button >
        </ div >
    }
}

< button class= "btn btn-primary btn-full" style = "margin-top: 16px;" @onclick = "() => _showAddForm = true" > +카테고리 추가 </ button >

@if(_showAddForm)
{
    < div class= "modal-backdrop" @onclick = "() => _showAddForm = false" >
        < div class= "modal-sheet" @onclick: stopPropagation = "true" >
            < h2 style = "font-size: 17px; margin: 0 0 16px;" > 새 카테고리 </ h2 >

            < div class= "toggle-group" style = "margin-bottom: 16px;" >
                < button class= "@(_newType == "expense" ? "active" : "")" @onclick='() => _newType = "expense"'>지출</button>
                <button class= "@(_newType == "income" ? "active" : "")" @onclick='() => _newType = "income"'>수입</button>
            </div>

            <div class= "form-group" >
                < label class= "form-label" > 이름 </ label >
                < input class= "form-input" @bind = "_newName" placeholder = "예: 식비, 카페" />
            </ div >

            < div class= "form-group" >
                < label class= "form-label" > 색상 </ label >
                < div style = "display: flex; gap: 8px; flex-wrap: wrap;" >
                    @foreach(var color in PresetColors)
                    {
                        < button style = "width: 32px; height: 32px; border-radius: 50%; background: @color; border: 2px solid @(_newColor == color ? "var(--color - text)" : "transparent"); cursor: pointer;"
                                @onclick = "() => _newColor = color" ></ button >
                    }
                </ div >
            </ div >

            @if(_newType == "expense")
            {
                < div class= "form-group" >
                    < label class= "form-label" > 예산(선택, 나중에 통계 화면에서 게이지로 보여요) </ label >
                    < input class= "form-input" type = "number" @bind = "_newBudgetAmount" placeholder = "0" style = "margin-bottom: 8px;" />
                    < div class= "toggle-group" >
                        < button class= "@(_newBudgetPeriod == "daily" ? "active" : "")" @onclick='() => _newBudgetPeriod = "daily"'>하루</button>
                        <button class= "@(_newBudgetPeriod == "weekly" ? "active" : "")" @onclick='() => _newBudgetPeriod = "weekly"'>주</button>
                        <button class= "@(_newBudgetPeriod == "monthly" ? "active" : "")" @onclick='() => _newBudgetPeriod = "monthly"'>월</button>
                    </div>
                </div>
            }

            @if(_error is not null)
            {
                < p class= "text-danger" style = "font-size: 13px; margin-bottom: 12px;" > @_error </ p >
            }

            < div style = "display: flex; gap: 8px;" >
                < button class= "btn" style = "flex: 1;" @onclick = "() => _showAddForm = false" > 취소 </ button >
                < button class= "btn btn-primary" style = "flex: 2;" @onclick = "AddCategory" > 추가 </ button >
            </ div >
        </ div >
    </ div >
}

@code {
    List<Category> _categories = new();
string _filterType = "expense";

bool _showAddForm;
string _newType = "expense";
string _newName = "";
string _newColor = PresetColors[0];
decimal? _newBudgetAmount;
string _newBudgetPeriod = "monthly";
string? _error;

static readonly string[] PresetColors = { "#8fb0d9", "#8fcdb6", "#e8c37a", "#b3a4d9", "#e2a3bc", "#c9a67c" };

List<Category> FilteredCategories => _categories.Where(c => c.Type == _filterType).ToList();

protected override async Task OnInitializedAsync()
{
    try
    {
        await Load();
    }
    catch (Exception ex)
    {
        _error = $"불러오기 실패: {ex.GetType().Name} - {ex.Message}";
    }
}

async Task Load()
{
    _categories = await CategoryRepo.GetAllAsync();
}

static string PeriodLabel(string period) => period switch
{
    "daily" => "하루",
    "weekly" => "주간",
    _ => "월간"
};

async Task AddCategory()
{
    if (string.IsNullOrWhiteSpace(_newName))
    {
        _error = "이름을 입력해주세요.";
        return;
    }

    var category = new Category
    {
        Name = _newName,
        Type = _newType,
        Color = _newColor,
        Icon = "ti-tag",
        BudgetAmount = _newType == "expense" && _newBudgetAmount > 0 ? _newBudgetAmount : null,
        BudgetPeriod = _newBudgetPeriod
    };

    try
    {
        await CategoryRepo.CreateAsync(category);
    }
    catch (Exception ex)
    {
        _error = $"저장 실패: {ex.GetType().Name} - {ex.Message}";
        return;
    }

    _newName = "";
    _newBudgetAmount = null;
    _showAddForm = false;
    _error = null;

    await Load();
}

async Task DeleteCategory(Guid id)
{
    await CategoryRepo.DeleteAsync(id);
    await Load();
}
}
