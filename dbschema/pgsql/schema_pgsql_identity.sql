-- Database: PostgreSQL (min version 15)

DROP TABLE IF EXISTS identity_role_claims;
DROP TABLE IF EXISTS identity_user_claims;
DROP TABLE IF EXISTS identity_user_roles;
DROP TABLE IF EXISTS identity_roles;
DROP TABLE IF EXISTS identity_users;

CREATE TABLE identity_roles (
    role_id varchar(48) NOT NULL,
    role_name varchar(64) NULL,
    normalized_name varchar(64) NULL,
    role_desc varchar(256) NULL,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_identity_roles PRIMARY KEY (role_id)
);
CREATE UNIQUE INDEX uidx_identity_roles_role_name ON identity_roles (normalized_name) WHERE normalized_name IS NOT NULL;

CREATE TABLE identity_users (
    uid varchar(48) NOT NULL,
    given_name varchar(128) NULL,
    family_name varchar(128) NULL,
    uname varchar(48) NULL,
    normalized_name varchar(48) NULL,
    uemail varchar(100) NULL,
    normalized_email varchar(100) NULL,
    password_hash varchar(256) NULL,
    security_stamp varchar(48) NULL,
    concurrency_stamp varchar(48) NULL,
    user_metadata jsonb NULL,
    CONSTRAINT pk_identity_users PRIMARY KEY (uid)
);
CREATE UNIQUE INDEX uidx_identity_users_email ON identity_users (normalized_email) WHERE normalized_email IS NOT NULL;
CREATE UNIQUE INDEX uidx_identity_users_user_name ON identity_users (normalized_name) WHERE normalized_name IS NOT NULL;

CREATE TABLE identity_role_claims (
    role_id varchar(48) NOT NULL,
    claim_type varchar(32) NOT NULL,
    claim_value varchar(64) NOT NULL,
    CONSTRAINT pk_identity_role_claims PRIMARY KEY (role_id, claim_type, claim_value),
    CONSTRAINT fk_identity_role_claims_identity_roles_role_id FOREIGN KEY (role_id) REFERENCES identity_roles (role_id) ON DELETE CASCADE
);

CREATE TABLE identity_user_claims (
    user_id varchar(48) NOT NULL,
    claim_type varchar(32) NOT NULL,
    claim_value varchar(64) NOT NULL,
    CONSTRAINT pk_identity_user_claims PRIMARY KEY (user_id, claim_type, claim_value),
    CONSTRAINT fk_identity_user_claims_identity_users_user_id FOREIGN KEY (user_id) REFERENCES identity_users (uid) ON DELETE CASCADE
);

CREATE TABLE identity_user_roles (
    user_id varchar(48) NOT NULL,
    role_id varchar(48) NOT NULL,
    CONSTRAINT pk_identity_user_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_identity_user_roles_identity_roles_role_id FOREIGN KEY (role_id) REFERENCES identity_roles (role_id) ON DELETE CASCADE,
    CONSTRAINT fk_identity_user_roles_identity_users_user_id FOREIGN KEY (user_id) REFERENCES identity_users (uid) ON DELETE CASCADE
);
CREATE INDEX idx_identity_user_roles_role_id ON identity_user_roles (role_id);
