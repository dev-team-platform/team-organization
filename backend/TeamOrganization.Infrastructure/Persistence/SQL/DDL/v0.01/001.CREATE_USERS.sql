CREATE TABLE users (
    id UUID PRIMARY KEY,
    identity_subject VARCHAR(255) NOT NULL,
    employee_code VARCHAR(10) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    email VARCHAR(255) NOT NULL,
    avatar_url TEXT NULL,
    is_email_verified BOOLEAN NOT NULL DEFAULT FALSE,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    
    last_login_at TIMESTAMPTZ NULL,
    last_logout_at TIMESTAMPTZ NULL,

    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    deleted_at TIMESTAMPTZ NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_id UUID NOT NULL,

    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_by_id UUID NOT NULL
);

ALTER TABLE users
ADD CONSTRAINT uq_users_identity_subject
    UNIQUE (identity_subject),
ADD CONSTRAINT uq_users_email
    UNIQUE (email),
ADD CONSTRAINT uq_users_employee_code
    UNIQUE (employee_code),
ADD CONSTRAINT fk_users_created_by_id
    FOREIGN KEY (created_by_id) REFERENCES users (id),
ADD CONSTRAINT fk_users_updated_by_id
    FOREIGN KEY (updated_by_id) REFERENCES users (id);

CREATE INDEX idx_users_display_name
ON users (display_name);