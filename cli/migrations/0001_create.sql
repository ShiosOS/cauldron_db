create table if not exists food_items (
    id uuid primary key,
    name text not null,
    expires date null,
    created_at timestamptz not null default now()
);
