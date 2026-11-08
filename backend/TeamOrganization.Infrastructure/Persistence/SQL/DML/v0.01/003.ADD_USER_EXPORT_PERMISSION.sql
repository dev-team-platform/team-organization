BEGIN;

INSERT INTO permissions (id, code, description, created_at)
VALUES ('01a216c0-0000-7000-8000-000000000001', 'user.export', 'Export users', CURRENT_TIMESTAMP)
ON CONFLICT (code) DO NOTHING;

INSERT INTO role_permissions (id, role_id, permission_id, created_at)
SELECT CASE r.code WHEN 'ADMIN' THEN '01a216c0-0000-7000-8000-000000000002'::uuid
                   ELSE '01a216c0-0000-7000-8000-000000000003'::uuid END,
       r.id, p.id, CURRENT_TIMESTAMP
FROM roles r CROSS JOIN permissions p
WHERE r.code IN ('ADMIN', 'SUPER_ADMIN') AND p.code = 'user.export'
  AND NOT EXISTS (SELECT 1 FROM role_permissions rp WHERE rp.role_id = r.id AND rp.permission_id = p.id);

COMMIT;
