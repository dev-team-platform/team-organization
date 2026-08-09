BEGIN;

-- 1. SUPER_ADMIN role
INSERT INTO roles (
    id,
    display_name,
    code,
    description,
    created_at
)
VALUES (
    '019fe588-d374-73de-9006-ed05d012da61',
    'Super Admin',
    'SUPER_ADMIN',
    'System super administrator',
    CURRENT_TIMESTAMP
);

-- 2. Super admin user
INSERT INTO users (
    id,
    identity_subject,
    employee_code,
    username,
    email,
    first_name,
    last_name,
    display_name,
    status,
    created_at
)
VALUES (
    '019fe588-d374-7cbf-a1a5-9d712a88c49d',
    'f0a27760-2c4b-42cd-9008-c328772d0eb8',
    'SA0001',
    'tp-super-admin-1',
    'tp-super-admin-1@team-platform.local',
    'Super Admin 1',
    NULL,
    'Super Admin 1',
    'Active',
    CURRENT_TIMESTAMP
);

-- 3. Assign SUPER_ADMIN role
INSERT INTO user_roles (
    id,
    user_id,
    role_id,
    effective_from,
    created_at
)
VALUES (
    '019fe588-d374-7bc0-adcf-74a58aefd2e3',
    '019fe588-d374-7cbf-a1a5-9d712a88c49d',
    '019fe588-d374-73de-9006-ed05d012da61',
    CURRENT_TIMESTAMP,
    CURRENT_TIMESTAMP
);

COMMIT;