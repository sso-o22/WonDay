-- ============================================================
-- 원데이 데이터베이스 스키마 v2 (가구 공유 기능 포함)
-- 기존 schema.sql을 대체합니다. Supabase SQL Editor에서 실행하세요.
-- ============================================================

-- 가구 (부부/커플 단위, 혼자 쓰는 경우도 가구 1개로 취급)
create table households (
  id uuid primary key default gen_random_uuid(),
  name text not null default '우리집',
  created_at timestamptz not null default now()
);

-- 가구 구성원
create table household_members (
  id uuid primary key default gen_random_uuid(),
  household_id uuid not null references households(id) on delete cascade,
  user_id uuid not null references auth.users(id) on delete cascade,
  role text not null default 'member' check (role in ('owner', 'member')),
  joined_at timestamptz not null default now(),
  unique (household_id, user_id)
);

-- 가구 초대 (이메일로 배우자 초대, 수락 전까지 대기 상태)
create table household_invites (
  id uuid primary key default gen_random_uuid(),
  household_id uuid not null references households(id) on delete cascade,
  invited_email text not null,
  invited_by uuid not null references auth.users(id) on delete cascade,
  status text not null default 'pending' check (status in ('pending', 'accepted', 'declined')),
  created_at timestamptz not null default now()
);

-- 1. 계좌/현금
create table accounts (
  id uuid primary key default gen_random_uuid(),
  household_id uuid not null references households(id) on delete cascade,
  owner_user_id uuid not null references auth.users(id) on delete cascade,
  is_shared boolean not null default false,
  name text not null,
  type text not null check (type in ('cash', 'bank')),
  initial_balance numeric not null default 0,
  color text,
  icon text,
  created_at timestamptz not null default now()
);

-- 2. 신용카드
create table cards (
  id uuid primary key default gen_random_uuid(),
  household_id uuid not null references households(id) on delete cascade,
  owner_user_id uuid not null references auth.users(id) on delete cascade,
  is_shared boolean not null default false,
  name text not null,
  linked_account_id uuid references accounts(id) on delete set null,
  statement_day int not null check (statement_day between 1 and 31),
  payment_day int not null check (payment_day between 1 and 31),
  color text,
  icon text,
  created_at timestamptz not null default now()
);

-- 3. 카테고리
create table categories (
  id uuid primary key default gen_random_uuid(),
  household_id uuid not null references households(id) on delete cascade,
  owner_user_id uuid not null references auth.users(id) on delete cascade,
  is_shared boolean not null default true, -- 카테고리는 기본적으로 같이 쓰는 게 자연스러워서 기본값 true
  name text not null,
  type text not null check (type in ('income', 'expense')),
  color text,
  icon text,
  budget_amount numeric,
  budget_period text check (budget_period in ('daily', 'weekly', 'monthly')) default 'monthly',
  created_at timestamptz not null default now()
);

-- 4. 거래 내역
create table transactions (
  id uuid primary key default gen_random_uuid(),
  household_id uuid not null references households(id) on delete cascade,
  owner_user_id uuid not null references auth.users(id) on delete cascade,
  is_shared boolean not null default false,
  date date not null,
  amount numeric not null,
  type text not null check (type in ('income', 'expense', 'transfer')),
  category_id uuid references categories(id) on delete set null,
  payment_account_id uuid references accounts(id) on delete set null,
  payment_card_id uuid references cards(id) on delete set null,
  to_account_id uuid references accounts(id) on delete set null,
  memo text,
  created_at timestamptz not null default now(),
  constraint one_payment_method check (
    (payment_account_id is not null and payment_card_id is null) or
    (payment_account_id is null and payment_card_id is not null) or
    (type = 'transfer')
  )
);

-- 5. 카드 청구서
create table card_statements (
  id uuid primary key default gen_random_uuid(),
  household_id uuid not null references households(id) on delete cascade,
  owner_user_id uuid not null references auth.users(id) on delete cascade,
  is_shared boolean not null default false,
  card_id uuid not null references cards(id) on delete cascade,
  period_start date not null,
  period_end date not null,
  total_amount numeric not null default 0,
  due_date date not null,
  is_paid boolean not null default false,
  paid_from_account_id uuid references accounts(id) on delete set null,
  created_at timestamptz not null default now()
);

-- 6. 예적금 / 목표 저축
create table savings_goals (
  id uuid primary key default gen_random_uuid(),
  household_id uuid not null references households(id) on delete cascade,
  owner_user_id uuid not null references auth.users(id) on delete cascade,
  is_shared boolean not null default false,
  name text not null,
  type text not null check (type in ('deposit', 'installment_saving', 'free_goal')),
  linked_account_id uuid references accounts(id) on delete set null,
  target_amount numeric not null,
  target_date date,
  interest_rate numeric,
  color text,
  icon text,
  created_at timestamptz not null default now()
);

-- 7. 목표 납입 내역
create table goal_contributions (
  id uuid primary key default gen_random_uuid(),
  household_id uuid not null references households(id) on delete cascade,
  owner_user_id uuid not null references auth.users(id) on delete cascade,
  is_shared boolean not null default false,
  goal_id uuid not null references savings_goals(id) on delete cascade,
  date date not null,
  amount numeric not null,
  source text not null check (source in ('manual', 'auto')),
  from_account_id uuid references accounts(id) on delete set null,
  created_at timestamptz not null default now()
);

-- 8. 자동 납입 규칙
create table recurring_rules (
  id uuid primary key default gen_random_uuid(),
  household_id uuid not null references households(id) on delete cascade,
  owner_user_id uuid not null references auth.users(id) on delete cascade,
  is_shared boolean not null default false,
  goal_id uuid not null references savings_goals(id) on delete cascade,
  amount numeric not null,
  day_of_month int not null check (day_of_month between 1 and 31),
  from_account_id uuid references accounts(id) on delete set null,
  is_active boolean not null default true,
  created_at timestamptz not null default now()
);

-- ============================================================
-- Row Level Security
-- 규칙: is_shared = true 면 같은 가구 구성원 전부 접근 가능
--       is_shared = false 면 owner_user_id 본인만 접근 가능
-- ============================================================

alter table households enable row level security;
alter table household_members enable row level security;
alter table household_invites enable row level security;
alter table accounts enable row level security;
alter table cards enable row level security;
alter table categories enable row level security;
alter table transactions enable row level security;
alter table card_statements enable row level security;
alter table savings_goals enable row level security;
alter table goal_contributions enable row level security;
alter table recurring_rules enable row level security;

-- 내가 속한 household인지 확인하는 헬퍼 함수
create or replace function is_household_member(target_household_id uuid)
returns boolean as $$
  select exists (
    select 1 from household_members
    where household_id = target_household_id and user_id = auth.uid()
  );
$$ language sql security definer stable;

-- households / household_members 정책
create policy "select_my_households" on households for select
  using (is_household_member(id));

create policy "select_my_membership" on household_members for select
  using (is_household_member(household_id));

create policy "insert_household_member_self" on household_members for insert
  with check (user_id = auth.uid());

-- household_invites: 초대한 사람과 초대받은 이메일 당사자만 조회 가능
create policy "select_relevant_invites" on household_invites for select
  using (invited_by = auth.uid() or invited_email = auth.jwt() ->> 'email');

create policy "insert_invite_by_member" on household_invites for insert
  with check (is_household_member(household_id));

-- 데이터 테이블 공통 정책 (is_shared 여부에 따라 분기)
do $$
declare
  t text;
begin
  foreach t in array array['accounts','cards','categories','transactions','card_statements','savings_goals','goal_contributions','recurring_rules']
  loop
    execute format($p$
      create policy "select_%1$s" on %1$s for select using (
        (is_shared = true and is_household_member(household_id))
        or (is_shared = false and owner_user_id = auth.uid())
      );
    $p$, t);

    execute format($p$
      create policy "insert_%1$s" on %1$s for insert with check (
        owner_user_id = auth.uid() and is_household_member(household_id)
      );
    $p$, t);

    execute format($p$
      create policy "update_%1$s" on %1$s for update using (
        (is_shared = true and is_household_member(household_id))
        or (is_shared = false and owner_user_id = auth.uid())
      );
    $p$, t);

    execute format($p$
      create policy "delete_%1$s" on %1$s for delete using (
        owner_user_id = auth.uid()
      );
    $p$, t);
  end loop;
end $$;

-- ============================================================
-- 인덱스
-- ============================================================
create index idx_transactions_household_date on transactions(household_id, date);
create index idx_transactions_category on transactions(category_id);
create index idx_goal_contributions_goal on goal_contributions(goal_id);
create index idx_household_members_user on household_members(user_id);
