# 원데이 — 프로젝트 셋업 가이드

Blazor WASM PWA + Supabase(Postgres, Auth) 구조로 만든 부부 공유 가계부 앱입니다.
지금까지 대화에서 나온 화면(캘린더, 통계/예산 게이지, 목표 저축, 계좌/카드) 전부 실제 `.razor` 컴포넌트로 만들어져 있어요.
(샌드박스에 .NET SDK가 없어서 직접 `dotnet run`까지는 못 돌려봤습니다 — 본인 PC에서 실행하면서 컴파일 에러가 나면 알려주세요, 같이 고치면 돼요.)

## 폴더 구조

```
wonday-app/
├── build.sh                    # Cloudflare Pages 배포용 빌드 스크립트
├── database/
│   └── schema.sql              # Supabase 테이블 + RLS 정책 (household 공유 구조 포함)
└── src/
    ├── WonDay.csproj
    ├── Program.cs
    ├── App.razor
    ├── _Imports.razor
    ├── Layout/
    │   ├── MainLayout.razor     # 로그인 체크 + 전체 레이아웃
    │   └── NavMenu.razor        # 하단 탭바 (캘린더/통계/목표/계좌)
    ├── Pages/
    │   ├── Login.razor          # 구글 로그인
    │   ├── Calendar.razor       # 홈 화면 (캘린더 + 일별 내역, route: /)
    │   ├── Stats.razor          # 카테고리 통계 + 예산 게이지 (route: /stats)
    │   ├── Goals.razor          # 예적금/목표 + 저축 예상 계산기 (route: /goals)
    │   └── Accounts.razor       # 계좌/카드 관리 + 배우자 초대 (route: /accounts)
    ├── Components/
    │   ├── TransactionFormModal.razor  # 내역 추가 모달
    │   └── CategoryBudgetCard.razor    # 예산 게이지 카드
    ├── Models/                  # DB 테이블과 1:1 매칭되는 C# 모델
    ├── Services/                # Supabase 통신 + 계산 로직 (리포지토리 패턴)
    └── wwwroot/
        ├── index.html, manifest.json, service-worker*.js
        ├── css/app.css          # 파스텔 톤 전체 스타일
        └── appsettings.json     # Supabase URL/키 (본인 값으로 교체)
```

## 1단계 — 로컬 Blazor 프로젝트 생성

본인 PC(.NET SDK 설치된 환경)에서:

```bash
dotnet new blazorwasm --pwa -o WonDay
```

생성된 폴더 안의 파일들을 전부 지우고, 이 저장소의 `src/` 폴더 안에 있는 내용물을 그대로 복사해 넣으세요.
(이번엔 `App.razor`, `index.html` 등 화면 파일까지 전부 포함돼 있어서, 템플릿 기본 파일을 남겨둘 필요 없이 통째로 교체하시면 됩니다.)

그 다음 패키지 복원:

```bash
dotnet restore
```

## 2단계 — Supabase 프로젝트 만들기

1. [supabase.com](https://supabase.com) 무료 계정 생성 후 새 프로젝트 생성
2. 프로젝트 대시보드 → **SQL Editor** → `database/schema.sql` 내용을 붙여넣고 실행
3. 대시보드 → **Project Settings → API** 에서 `Project URL`과 `anon public` 키를 복사
4. `src/wwwroot/appsettings.json`의 값을 방금 복사한 값으로 교체
5. 테스트를 위해 카테고리를 몇 개 직접 추가해두면 좋아요 (예: 식비, 카페, 교통 — `type: expense`, `budget_amount`와 `budget_period`도 같이 넣으면 통계 화면에서 바로 게이지가 보여요)

## 3단계 — 구글 로그인 설정

1. [Google Cloud Console](https://console.cloud.google.com) → OAuth 클라이언트 ID 생성 (유형: 웹 애플리케이션)
2. 승인된 리디렉션 URI에 Supabase가 알려주는 콜백 URL 등록
   (Supabase 대시보드 → Authentication → Providers → Google 화면에 표시됨)
3. 생성된 클라이언트 ID/시크릿을 Supabase 대시보드의 Google Provider 설정에 입력, 활성화

## 4단계 — 로컬 실행

```bash
dotnet run
```

브라우저에서 `https://localhost:5001` (포트는 콘솔 출력 확인) 접속.

## 5단계 — 아이폰/아이패드에 PWA로 설치

배포한 뒤 (또는 로컬 테스트 시):
- 사파리로 접속 → 공유 버튼 → **홈 화면에 추가**
- 안드로이드는 크롬으로 접속 → 메뉴 → **홈 화면에 추가**

## 화면별로 참고할 점

- **Calendar.razor**: 날짜 탭하면 그 날 내역이 아래 카드에 뜨고, 우측 하단 + 버튼으로 `TransactionFormModal`을 엽니다.
- **Stats.razor**: `BudgetService.GetTodayStatusAsync`로 카테고리별 "오늘 사용 가능" 게이지를 계산해요. 안 쓴 만큼 다음날로 이월되는 방식이고, 카테고리마다 예산 주기(하루/주/월)를 다르게 설정할 수 있어요.
- **Goals.razor**: 목표 리스트 + 저축 예상 계산기가 한 화면에 있어요. 슬라이더를 움직이면 `SavingsGoalRepository.ProjectFutureValue`로 즉시 재계산됩니다.
- **Accounts.razor**: 계좌 잔액과 카드 청구 예정액을 최근 12개월 거래를 모아서 계산해요. 배우자 초대 폼도 여기 있습니다.

## 부부 공유 기능

- 로그인하면 자동으로 본인만 속한 household(가구)가 하나 생성됩니다 (혼자 쓰는 흐름은 그대로 유지).
- `Accounts.razor`의 "배우자 초대"에 이메일을 입력하면 `HouseholdRepository.InviteAsync`로 초대가 생성됩니다.
- `Account`, `Category`, `Transaction` 등 데이터마다 `IsShared` 값으로 "배우자도 보임(공동)" / "나만 보임(개인)"을 구분해요.
- RLS 정책이 이 규칙을 서버 단에서도 강제해서, 개인 데이터는 배우자 계정으로도 절대 조회되지 않습니다.

## 아직 비어있는 부분 (다음에 이어서 만들 것)

- **카테고리 관리 화면** — 지금은 Supabase 대시보드에서 직접 카테고리를 추가해야 해요. `CategoryRepository`는 이미 만들어져 있어서 화면만 하나 얹으면 됩니다.
- **초대 수락 화면** — `HouseholdRepository.GetPendingInvitesForMeAsync` / `AcceptInviteAsync`는 만들어져 있지만, 이걸 보여줄 화면이 없어요. 지금은 상대방이 수락하려면 코드를 직접 호출해야 해요.
- **카드 청구서 자동 생성 배치** — 지금은 청구 기간 금액을 그때그때 계산만 하고, `card_statements` 테이블에 실제로 기록/결제 처리하는 로직은 없어요.
- **PWA 아이콘 파일** — `manifest.json`이 `icon-192.png`, `icon-512.png`를 참조하는데 실제 이미지 파일은 없어요. 원하는 아이콘 이미지를 같은 이름으로 `wwwroot/`에 넣어주세요.
- **service-worker 오프라인 캐싱** — 지금 것은 최소 기능만 있어요. 공식 PWA 템플릿 수준의 정교한 캐싱이 필요하면 `dotnet new blazorwasm --pwa`로 빈 프로젝트를 하나 만들어서 그 안의 `service-worker.published.js`를 참고해 교체하세요.
- **거래 내역 수정/삭제 UI** — 리포지토리에 `UpdateAsync`/`DeleteAsync`는 있지만, 화면에서 누르는 버튼은 아직 없어요.

## 배포 — Cloudflare Pages

아이디가 URL에 노출되는 게 싫어서 GitHub Pages 대신 Cloudflare Pages로 배포합니다.
주소는 `프로젝트이름.pages.dev` 형태로 나옵니다.

### 1. GitHub에 코드 올리기

```bash
git init
git add .
git commit -m "init"
git remote add origin https://github.com/본인아이디/저장소이름.git
git push -u origin main
```

이 저장소 루트에 있는 `build.sh`가 같이 올라가 있어야 합니다.
(Cloudflare Pages 빌드 서버에는 .NET이 안 깔려있어서, 빌드할 때마다 이 스크립트가 알아서 설치해줍니다.)

### 2. Cloudflare 대시보드에서 설정

1. [Cloudflare 대시보드](https://dash.cloudflare.com) → **Workers & Pages** → **Create application** → **Pages** 탭 → **Connect to Git**
2. 방금 만든 GitHub 저장소 선택
3. **Set up builds and deployments** 항목을 아래처럼 입력:
   - **Build command**: `chmod +x build.sh && ./build.sh`
   - **Build output directory**: `output/wwwroot`
   - (프로젝트가 저장소 루트가 아니라 하위 폴더에 있다면 **Root directory**에 그 경로 지정)
4. **Save and Deploy** 클릭

이후로는 GitHub에 push할 때마다 자동으로 재빌드/재배포돼요.

### 주의할 점

- 첫 빌드 시 `.wasm` 파일이 25MiB 제한에 걸린다는 에러가 나오면, Blazor의 Brotli 압축 파일을 로드하도록 설정을 바꾸거나 에셋 크기를 줄여야 해요.
- `build.sh`의 `.dotnet-install.sh -c 9.0` 부분은 실제 사용 중인 .NET 버전과 맞춰주세요.

### 커스텀 도메인을 나중에 붙이고 싶다면

Cloudflare Pages 프로젝트 설정 → **Custom domains**에서 구입한 도메인을 연결하면 됩니다.
