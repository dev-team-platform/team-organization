CREATE TABLE users (
    id UUID PRIMARY KEY,
    identity_subject VARCHAR(255) NOT NULL,
    employee_code VARCHAR(10) NOT NULL,
    username VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NULL,
    display_name VARCHAR(200) NOT NULL,
    avatar_url TEXT NULL,
    status VARCHAR(50) NOT NULL,
    
    last_login_at TIMESTAMPTZ NULL,

    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_id UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by_id UUID NULL
);

ALTER TABLE users
ADD CONSTRAINT uq_users_identity_subject
    UNIQUE (identity_subject),
ADD CONSTRAINT uq_users_email
    UNIQUE (email),
ADD CONSTRAINT uq_users_username
    UNIQUE (username),
ADD CONSTRAINT uq_users_employee_code
    UNIQUE (employee_code),
ADD CONSTRAINT fk_users_created_by_id
    FOREIGN KEY (created_by_id) REFERENCES users (id),
ADD CONSTRAINT fk_users_updated_by_id
    FOREIGN KEY (updated_by_id) REFERENCES users (id);

CREATE INDEX idx_users_display_name
ON users (display_name);


CREATE TABLE roles (
    id UUID PRIMARY KEY,
    display_name VARCHAR(100) NOT NULL,
    code VARCHAR(100) NOT NULL,
    description TEXT NULL,

    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_id UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by_id UUID NULL
);

ALTER TABLE roles
ADD CONSTRAINT uq_roles_code
    UNIQUE (code),
ADD CONSTRAINT fk_roles_created_by_id
    FOREIGN KEY (created_by_id) REFERENCES users (id),
ADD CONSTRAINT fk_roles_updated_by_id
    FOREIGN KEY (updated_by_id) REFERENCES users (id);


CREATE TABLE user_roles (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    role_id UUID NOT NULL,
    effective_from TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    effective_to TIMESTAMPTZ NULL,

    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_id UUID NULL,

    updated_at TIMESTAMPTZ NULL,
    updated_by_id UUID NULL
);

ALTER TABLE user_roles
ADD CONSTRAINT uq_user_roles_user_id_role_id
    UNIQUE (user_id, role_id),
ADD CONSTRAINT fk_user_roles_user_id
    FOREIGN KEY (user_id) REFERENCES users (id),
ADD CONSTRAINT fk_user_roles_role_id
    FOREIGN KEY (role_id) REFERENCES roles (id),
ADD CONSTRAINT fk_user_roles_created_by_id
    FOREIGN KEY (created_by_id) REFERENCES users (id),
ADD CONSTRAINT fk_user_roles_updated_by_id
    FOREIGN KEY (updated_by_id) REFERENCES users (id);
