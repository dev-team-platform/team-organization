BEGIN;

-- Permission ids are static UUIDv7 values.
INSERT INTO permissions (id, code, description, created_at)
VALUES
    ('01a10179-99b9-7fad-ada7-56cad2a9658a', 'user.self.update', 'Update own user profile', CURRENT_TIMESTAMP),
    ('01a10179-99b9-73f0-bea4-e81d88bb02e1', 'user.self.read', 'Read own user profile', CURRENT_TIMESTAMP),
    ('01a10179-99b9-7ba5-9b2f-c8c8cc44490d', 'user.create', 'Create users', CURRENT_TIMESTAMP),
    ('01a10179-99b9-74dd-ab29-c052215a0e82', 'user.read', 'Read users', CURRENT_TIMESTAMP),
    ('01a10179-99b9-7b2f-befb-6fbedcc055cd', 'user.update', 'Update users', CURRENT_TIMESTAMP),
    ('01a10179-99b9-71c8-ba97-be0ec7b90db3', 'admin.create', 'Create administrators', CURRENT_TIMESTAMP),
    ('01a10179-99b9-716b-bd89-c25c2c886d39', 'admin.read', 'Read administrators', CURRENT_TIMESTAMP),
    ('01a10179-99b9-76be-be2b-31fa41afea70', 'admin.update', 'Update administrators', CURRENT_TIMESTAMP);

-- SUPER_ADMIN already exists from 001.INSERT_SUPER_ADMIN.sql.
INSERT INTO roles (id, display_name, code, description, created_at)
VALUES
    ('01a10179-99b9-7e51-97f1-820152ff2e03', 'Admin', 'ADMIN', 'Administrator', CURRENT_TIMESTAMP),
    ('01a10179-99b9-7248-8f64-a2cfe7fbf266', 'Employee', 'EMPLOYEE', 'Employee', CURRENT_TIMESTAMP);

INSERT INTO role_permissions (id, role_id, permission_id, created_at)
VALUES
    -- SUPER_ADMIN: all permissions
    (
        '01a10179-99b9-7bcb-9c3d-9bf0b2d2fb41',
        '019fe588-d374-73de-9006-ed05d012da61',
        '01a10179-99b9-7fad-ada7-56cad2a9658a',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-7881-a42a-392c50bb9ddc',
        '019fe588-d374-73de-9006-ed05d012da61',
        '01a10179-99b9-73f0-bea4-e81d88bb02e1',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-7218-ad46-8f3a6e3b5829',
        '019fe588-d374-73de-9006-ed05d012da61',
        '01a10179-99b9-7ba5-9b2f-c8c8cc44490d',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-7d5b-86e7-002b8d075a63',
        '019fe588-d374-73de-9006-ed05d012da61',
        '01a10179-99b9-74dd-ab29-c052215a0e82',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-7801-8683-3a0c398bf37f',
        '019fe588-d374-73de-9006-ed05d012da61',
        '01a10179-99b9-7b2f-befb-6fbedcc055cd',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-71a4-954c-734483701a8c',
        '019fe588-d374-73de-9006-ed05d012da61',
        '01a10179-99b9-71c8-ba97-be0ec7b90db3',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-72d4-86c2-602038f98411',
        '019fe588-d374-73de-9006-ed05d012da61',
        '01a10179-99b9-716b-bd89-c25c2c886d39',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-7151-b9e2-c0c6de4f53a7',
        '019fe588-d374-73de-9006-ed05d012da61',
        '01a10179-99b9-76be-be2b-31fa41afea70',
        CURRENT_TIMESTAMP
    ),

    -- ADMIN: self-service and user management permissions
    (
        '01a10179-99b9-7426-8210-55edf29641db',
        '01a10179-99b9-7e51-97f1-820152ff2e03',
        '01a10179-99b9-7fad-ada7-56cad2a9658a',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-7cad-aa8a-717ebea05fb4',
        '01a10179-99b9-7e51-97f1-820152ff2e03',
        '01a10179-99b9-73f0-bea4-e81d88bb02e1',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-7b7f-aaf7-65132960736d',
        '01a10179-99b9-7e51-97f1-820152ff2e03',
        '01a10179-99b9-7ba5-9b2f-c8c8cc44490d',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-79c9-b6c6-e7f5e6d3755c',
        '01a10179-99b9-7e51-97f1-820152ff2e03',
        '01a10179-99b9-74dd-ab29-c052215a0e82',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-7889-a982-8addd45daca1',
        '01a10179-99b9-7e51-97f1-820152ff2e03',
        '01a10179-99b9-7b2f-befb-6fbedcc055cd',
        CURRENT_TIMESTAMP
    ),

    -- EMPLOYEE: self-service permissions
    (
        '01a10179-99b9-71d2-ab68-5af0834c693e',
        '01a10179-99b9-7248-8f64-a2cfe7fbf266',
        '01a10179-99b9-7fad-ada7-56cad2a9658a',
        CURRENT_TIMESTAMP
    ),
    (
        '01a10179-99b9-7f56-83bc-bde34f452a37',
        '01a10179-99b9-7248-8f64-a2cfe7fbf266',
        '01a10179-99b9-73f0-bea4-e81d88bb02e1',
        CURRENT_TIMESTAMP
    );

COMMIT;