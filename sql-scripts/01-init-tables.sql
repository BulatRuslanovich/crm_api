CREATE TABLE org_type (
    org_type_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    org_type_name VARCHAR(100) NOT NULL UNIQUE
);

CREATE TABLE status (
    status_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    status_name VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE policy (
    policy_id   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    policy_name VARCHAR(100) NOT NULL UNIQUE
);

INSERT INTO org_type (org_type_name) VALUES
    ('Аптека'), ('Больница'), ('Поликлиника'), ('Клиника'), ('Другое');

INSERT INTO status (status_name) VALUES
    ('Запланирован'), ('Открыт'), ('Сохранен'), ('Закрыт'), ('Отменен');

INSERT INTO policy (policy_name) VALUES
    ('Admin'), ('Director'), ('Manager'), ('Representative');


CREATE TABLE org (
    org_id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    org_type_id   INT NOT NULL REFERENCES org_type (org_type_id),
    org_name      VARCHAR(255) NOT NULL,
    org_inn       VARCHAR(12) NOT NULL,
    org_latitude  DOUBLE PRECISION,
    org_longitude DOUBLE PRECISION,
    org_address   TEXT NOT NULL DEFAULT '-',
    is_deleted    BOOLEAN DEFAULT FALSE,

    CONSTRAINT chk_org_lat CHECK (org_latitude IS NULL OR org_latitude BETWEEN -90 AND 90),
    CONSTRAINT chk_org_lon CHECK (org_longitude IS NULL OR org_longitude BETWEEN -180 AND 180),
    CONSTRAINT chk_org_inn CHECK (org_inn ~ '^[0-9]{10,12}$')
);

CREATE TABLE spec (
    spec_id    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    spec_name  VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE phys (
    phys_id         INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    spec_id         INT REFERENCES spec (spec_id),
    phys_firstname  VARCHAR(100) NOT NULL,
    phys_lastname   VARCHAR(100) NOT NULL,
    phys_middlename VARCHAR(100),
    phys_phone      VARCHAR(30),
    phys_email      VARCHAR(150) NOT NULL,
    is_deleted      BOOLEAN DEFAULT FALSE
);

CREATE TABLE phys_org (
    phys_org_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    phys_id     INT NOT NULL REFERENCES phys (phys_id),
    org_id      INT NOT NULL REFERENCES org (org_id),
    is_main     BOOLEAN DEFAULT false,
    UNIQUE (phys_id, org_id)
);

CREATE UNIQUE INDEX uniq_phys_main_org
ON phys_org(phys_id)
WHERE is_main = true;


CREATE TABLE drug (
    drug_id    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    drug_name  VARCHAR(255) NOT NULL,
    drug_brand VARCHAR(255) NOT NULL,
    drug_form  VARCHAR(100) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE
);


CREATE TABLE usr (
    usr_id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usr_firstname     VARCHAR(100) NOT NULL,
    usr_lastname      VARCHAR(100) NOT NULL,
    usr_email         VARCHAR(150) NOT NULL,
    usr_login         VARCHAR(100) NOT NULL,
    usr_password_hash VARCHAR(255) NOT NULL,
    is_email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    is_deleted        BOOLEAN DEFAULT FALSE,

    CONSTRAINT chk_usr_email_not_empty CHECK (usr_email <> ''),
    CONSTRAINT chk_usr_login_not_empty CHECK (usr_login <> '')
);


CREATE TABLE department (
    department_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    department_name VARCHAR(255) NOT NULL,
    is_deleted BOOLEAN DEFAULT FALSE
);

CREATE TABLE usr_department (
    usr_id INT REFERENCES usr(usr_id),
    department_id INT REFERENCES department(department_id),
    PRIMARY KEY (usr_id, department_id)
);

CREATE TABLE usr_policy (
    usr_id    INT NOT NULL REFERENCES usr (usr_id),
    policy_id INT NOT NULL REFERENCES policy (policy_id),
    UNIQUE (usr_id, policy_id)
);


CREATE TABLE activ (
    activ_id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usr_id            INT NOT NULL REFERENCES usr (usr_id),
    org_id            INT REFERENCES org (org_id),
    phys_id           INT REFERENCES phys (phys_id),
    status_id         INT NOT NULL REFERENCES status (status_id),
    activ_start       TIMESTAMPTZ,
    activ_end         TIMESTAMPTZ,
    activ_description TEXT NOT NULL DEFAULT '-',
    activ_latitude    DOUBLE PRECISION,
    activ_longitude   DOUBLE PRECISION,
    is_deleted        BOOLEAN DEFAULT FALSE,

    CONSTRAINT chk_activ_dates CHECK (
        activ_end IS NULL OR activ_start IS NULL OR activ_end >= activ_start
    ),
    CONSTRAINT chk_activ_lat CHECK (activ_latitude IS NULL OR activ_latitude BETWEEN -90 AND 90),
    CONSTRAINT chk_activ_lon CHECK (activ_longitude IS NULL OR activ_longitude BETWEEN -180 AND 180)
);


CREATE TABLE activ_drug (
    activ_drug_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    activ_id      INT NOT NULL REFERENCES activ (activ_id),
    drug_id       INT NOT NULL REFERENCES drug (drug_id),
    UNIQUE (activ_id, drug_id)
);


CREATE TABLE refresh (
    refresh_id         BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usr_id             INT NOT NULL REFERENCES usr (usr_id) ON DELETE CASCADE,
    refresh_token_hash VARCHAR(255) NOT NULL UNIQUE,
    refresh_expires_at TIMESTAMPTZ NOT NULL
);


CREATE TABLE email_token (
    email_token_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usr_id         INT NOT NULL REFERENCES usr (usr_id) ON DELETE CASCADE,
    token_hash     VARCHAR(255) NOT NULL UNIQUE,
    token_type     SMALLINT NOT NULL,
    expires_at     TIMESTAMPTZ NOT NULL,
    attempt_count  INTEGER NOT NULL DEFAULT 0,

    CONSTRAINT chk_token_type CHECK (token_type IN (0, 1))
);


CREATE TABLE audit_log (
    audit_id     BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    entity_type  VARCHAR(50) NOT NULL,   -- 'activ', 'org', 'phys'
    entity_id    INT NOT NULL,
    action       VARCHAR(20) NOT NULL,   -- INSERT / UPDATE / DELETE
    field_name   VARCHAR(100),           -- NULL если whole entity
    old_value    TEXT,
    new_value    TEXT,
    changed_by   INT REFERENCES usr(usr_id),
    changed_at   TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);


-- INDEXES

CREATE INDEX idx_activ_usr ON activ(usr_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_activ_org ON activ(org_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_activ_phys ON activ(phys_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_activ_status ON activ(status_id) WHERE is_deleted = FALSE;
CREATE INDEX idx_activ_start ON activ(activ_start) WHERE is_deleted = FALSE;

CREATE INDEX idx_phys_org_phys ON phys_org(phys_id);
CREATE INDEX idx_phys_org_org  ON phys_org(org_id);

CREATE INDEX idx_phys_spec ON phys(spec_id);

CREATE INDEX idx_usr_department_usr ON usr_department(usr_id);
CREATE INDEX idx_usr_department_dep ON usr_department(department_id);

CREATE INDEX idx_usr_policy_policy ON usr_policy(policy_id);

CREATE INDEX idx_activ_drug_activ ON activ_drug(activ_id);
CREATE INDEX idx_activ_drug_drug  ON activ_drug(drug_id);

CREATE INDEX idx_refresh_usr ON refresh(usr_id);
CREATE INDEX idx_email_token_usr ON email_token(usr_id);

CREATE INDEX idx_audit_log_entity ON audit_log(entity_type, entity_id);
CREATE INDEX idx_audit_log_changed_at ON audit_log(changed_at DESC);
CREATE INDEX idx_audit_log_changed_by ON audit_log(changed_by) WHERE changed_by IS NOT NULL;

CREATE UNIQUE INDEX uniq_email_token_usr_type
ON email_token(usr_id, token_type);

CREATE UNIQUE INDEX uniq_usr_email_active
ON usr(LOWER(usr_email))
WHERE is_deleted = FALSE;

CREATE UNIQUE INDEX uniq_usr_login_active
ON usr(LOWER(usr_login))
WHERE is_deleted = FALSE;

CREATE UNIQUE INDEX uniq_department_name_active
ON department(department_name)
WHERE is_deleted = FALSE;

CREATE UNIQUE INDEX phys_usr_email_active
ON phys(LOWER(phys_email))
WHERE is_deleted = FALSE;

-- TODO: это временно 
INSERT INTO usr (usr_firstname, usr_lastname, usr_email, usr_login, usr_password_hash, is_email_confirmed) VALUES
    ('Bulat', 'Bikmukhametov', 'admin@crm.com', 'admin', '$2a$11$glMVgIr1zF2pjZij0AeecuMsmhVf/Xf/9xPNc6S4u.zhIt17HjxIS', true);

INSERT INTO usr_policy (usr_id, policy_id) VALUES
    (1, 1); 
