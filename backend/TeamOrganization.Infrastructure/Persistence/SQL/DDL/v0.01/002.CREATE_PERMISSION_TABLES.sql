CREATE TABLE permissions (
    id UUID PRIMARY KEY,
    code VARCHAR(150) NOT NULL,
    description TEXT NULL,

    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_id UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by_id UUID NULL
);

ALTER TABLE permissions
ADD CONSTRAINT uq_permissions_code
    UNIQUE (code),
ADD CONSTRAINT fk_permissions_created_by_id
    FOREIGN KEY (created_by_id) REFERENCES users (id),
ADD CONSTRAINT fk_permissions_updated_by_id
    FOREIGN KEY (updated_by_id) REFERENCES users (id);

CREATE TABLE role_permissions (
    id UUID PRIMARY KEY,
    role_id UUID NOT NULL,
    permission_id UUID NOT NULL,

    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_id UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by_id UUID NULL
);

ALTER TABLE role_permissions
ADD CONSTRAINT uq_role_permissions_role_id_permission_id
    UNIQUE (role_id, permission_id),
ADD CONSTRAINT fk_role_permissions_role_id
    FOREIGN KEY (role_id) REFERENCES roles (id),
ADD CONSTRAINT fk_role_permissions_permission_id
    FOREIGN KEY (permission_id) REFERENCES permissions (id),
ADD CONSTRAINT fk_role_permissions_created_by_id
    FOREIGN KEY (created_by_id) REFERENCES users (id),
ADD CONSTRAINT fk_role_permissions_updated_by_id
    FOREIGN KEY (updated_by_id) REFERENCES users (id);
