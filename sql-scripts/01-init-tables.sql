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
    ('Аптека'), ('Аптечная сеть'), ('Больница'), ('Поликлиника'), ('Клиника');

INSERT INTO status (status_name) VALUES
    ('Запланирован'), ('Открыт'), ('Сохранен'), ('Закрыт');

INSERT INTO policy (policy_name) VALUES
    ('Admin'), ('Director'), ('Manager'), ('Representative');


CREATE TABLE org (
    org_id        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    org_type_id   INT NOT NULL REFERENCES org_type (org_type_id),
    org_name      VARCHAR(255) NOT NULL,
    org_inn       VARCHAR(12) NOT NULL,
    org_latitude  DOUBLE PRECISION DEFAULT 0,
    org_longitude DOUBLE PRECISION DEFAULT 0,
    org_address   TEXT NOT NULL DEFAULT '-',
    created_at    TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at    TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    is_deleted    BOOLEAN   DEFAULT FALSE
);

CREATE TABLE spec (
    spec_id    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    spec_name  VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    is_deleted BOOLEAN  DEFAULT FALSE
);

CREATE TABLE phys (
    phys_id         INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    spec_id         INT REFERENCES spec (spec_id),
    phys_firstname  VARCHAR(100) NOT NULL,
    phys_lastname   VARCHAR(100) NOT NULL,
    phys_middlename VARCHAR(100),
    phys_phone      VARCHAR(30),
    phys_email      VARCHAR(150) NOT NULL,
    created_at      TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at      TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    is_deleted      BOOLEAN   DEFAULT FALSE
);

CREATE TABLE phys_org (
    phys_org_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    phys_id     INT NOT NULL REFERENCES phys (phys_id),
    org_id      INT NOT NULL REFERENCES org (org_id),
    is_main     BOOLEAN DEFAULT false, -- физик может работать в разных организациях, true, если организация основная
    created_at  TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (phys_id, org_id)
);

CREATE TABLE drug (
    drug_id          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    drug_name        VARCHAR(255) NOT NULL,
    drug_brand       VARCHAR(255) NOT NULL,
    drug_form        VARCHAR(100) NOT NULL,
    created_at       TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at       TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    is_deleted       BOOLEAN   DEFAULT FALSE
);

CREATE TABLE usr (
    usr_id            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usr_firstname     VARCHAR(100) NOT NULL,
    usr_lastname      VARCHAR(100) NOT NULL,
    usr_email         VARCHAR(150) NOT NULL,
    usr_login         VARCHAR(100) NOT NULL,
    usr_password_hash VARCHAR(255) NOT NULL,
    created_at        TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at        TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    is_email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    is_deleted        BOOLEAN   DEFAULT FALSE
);

CREATE TABLE department (
    department_id INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    department_name VARCHAR(255) NOT NULL,
    is_deleted    BOOLEAN   DEFAULT FALSE
);

CREATE TABLE usr_department (
    usr_id INT REFERENCES usr(usr_id),
    department_id INT REFERENCES department(department_id),
    created_at    TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (usr_id, department_id)
);

CREATE TABLE usr_policy (
    usr_id        INT NOT NULL REFERENCES usr (usr_id),
    policy_id     INT NOT NULL REFERENCES policy (policy_id),
    created_at    TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
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
    created_at        TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at        TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    is_deleted        BOOLEAN   DEFAULT FALSE,  
    CHECK (
        (org_id IS NOT NULL AND phys_id IS NULL) OR
        (org_id IS NULL AND phys_id IS NOT NULL)
    )
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
    refresh_expires_at TIMESTAMPTZ   NOT NULL,
    created_at         TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE email_token (
    email_token_id BIGINT    GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    usr_id         INT       NOT NULL REFERENCES usr (usr_id) ON DELETE CASCADE,
    token_hash     VARCHAR(255) NOT NULL UNIQUE,
    token_type     SMALLINT  NOT NULL, -- 0 = EmailConfirmation, 1 = PasswordReset
    expires_at     TIMESTAMPTZ NOT NULL,
    attempt_count  INTEGER NOT NULL DEFAULT 0,
    created_at     TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_activ_usr
ON activ(usr_id)
WHERE is_deleted = FALSE;

CREATE INDEX idx_activ_org
ON activ(org_id)
WHERE is_deleted = FALSE;

CREATE INDEX idx_activ_phys
ON activ(phys_id)
WHERE is_deleted = FALSE;

CREATE INDEX idx_activ_status
ON activ(status_id)
WHERE is_deleted = FALSE;

CREATE INDEX idx_activ_start
ON activ(activ_start)
WHERE is_deleted = FALSE;

CREATE INDEX idx_phys_org_phys ON phys_org(phys_id);
CREATE INDEX idx_phys_org_org  ON phys_org(org_id);
CREATE INDEX idx_usr_department_usr ON usr_department(usr_id);
CREATE INDEX idx_usr_department_dep ON usr_department(department_id);
CREATE INDEX idx_activ_drug_activ ON activ_drug(activ_id);
CREATE INDEX idx_activ_drug_drug  ON activ_drug(drug_id);

CREATE INDEX idx_refresh_usr ON refresh(usr_id);
CREATE INDEX idx_email_token_usr ON email_token(usr_id);

CREATE UNIQUE INDEX uniq_usr_email_active
ON usr(usr_email)
WHERE is_deleted = FALSE;

CREATE UNIQUE INDEX uniq_usr_login_active
ON usr(usr_login)
WHERE is_deleted = FALSE;

CREATE UNIQUE INDEX uniq_department_name_active
ON department(department_name)
WHERE is_deleted = FALSE;

CREATE UNIQUE INDEX phys_usr_email_active
ON phys(phys_email)
WHERE is_deleted = FALSE;




CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_org_updated_at    BEFORE UPDATE ON org    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_phys_updated_at   BEFORE UPDATE ON phys   FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_drug_updated_at   BEFORE UPDATE ON drug   FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_usr_updated_at    BEFORE UPDATE ON usr    FOR EACH ROW EXECUTE FUNCTION set_updated_at();
CREATE TRIGGER trg_activ_updated_at  BEFORE UPDATE ON activ  FOR EACH ROW EXECUTE FUNCTION set_updated_at();
