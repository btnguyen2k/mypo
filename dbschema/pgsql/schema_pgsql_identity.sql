-- Database: PostgreSQL (min version 15)

DROP TABLE IF EXISTS mypo_role_claims;
DROP TABLE IF EXISTS mypo_user_claims;
DROP TABLE IF EXISTS mypo_user_roles;
DROP TABLE IF EXISTS mypo_roles;
DROP TABLE IF EXISTS mypo_users;

CREATE TABLE mypo_roles (
    role_id varchar(48) NOT NULL,
    role_name varchar(64) NULL,
    normalized_name varchar(64) NULL,
    role_desc varchar(256) NULL,
    concurrency_stamp varchar(48) NULL,
    CONSTRAINT pk_mypo_roles PRIMARY KEY (role_id)
);
CREATE UNIQUE INDEX uidx_mypo_roles_role_name ON mypo_roles (normalized_name) WHERE normalized_name IS NOT NULL;

CREATE TABLE mypo_users (
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
    CONSTRAINT pk_mypo_users PRIMARY KEY (uid)
);
CREATE UNIQUE INDEX uidx_mypo_users_email ON mypo_users (normalized_email) WHERE normalized_email IS NOT NULL;
CREATE UNIQUE INDEX uidx_mypo_users_user_name ON mypo_users (normalized_name) WHERE normalized_name IS NOT NULL;

CREATE TABLE mypo_role_claims (
    role_id varchar(48) NOT NULL,
    claim_type varchar(32) NOT NULL,
    claim_value varchar(64) NOT NULL,
    CONSTRAINT pk_mypo_role_claims PRIMARY KEY (role_id, claim_type, claim_value),
    CONSTRAINT fk_mypo_role_claims_mypo_roles_role_id FOREIGN KEY (role_id) REFERENCES mypo_roles (role_id) ON DELETE CASCADE
);

CREATE TABLE mypo_user_claims (
    user_id varchar(48) NOT NULL,
    claim_type varchar(32) NOT NULL,
    claim_value varchar(64) NOT NULL,
    CONSTRAINT pk_mypo_user_claims PRIMARY KEY (user_id, claim_type, claim_value),
    CONSTRAINT fk_mypo_user_claims_mypo_users_user_id FOREIGN KEY (user_id) REFERENCES mypo_users (uid) ON DELETE CASCADE
);

CREATE TABLE mypo_user_roles (
    user_id varchar(48) NOT NULL,
    role_id varchar(48) NOT NULL,
    CONSTRAINT pk_mypo_user_roles PRIMARY KEY (user_id, role_id),
    CONSTRAINT fk_mypo_user_roles_mypo_roles_role_id FOREIGN KEY (role_id) REFERENCES mypo_roles (role_id) ON DELETE CASCADE,
    CONSTRAINT fk_mypo_user_roles_mypo_users_user_id FOREIGN KEY (user_id) REFERENCES mypo_users (uid) ON DELETE CASCADE
);
CREATE INDEX idx_mypo_user_roles_role_id ON mypo_user_roles (role_id);
