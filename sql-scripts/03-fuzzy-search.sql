CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX ix_drug_name_trgm  ON drug USING GIN (drug_name  gin_trgm_ops);
CREATE INDEX ix_drug_brand_trgm ON drug USING GIN (drug_brand gin_trgm_ops);

CREATE INDEX ix_phys_firstname_trgm  ON phys USING GIN (phys_firstname  gin_trgm_ops);
CREATE INDEX ix_phys_lastname_trgm   ON phys USING GIN (phys_lastname   gin_trgm_ops);
CREATE INDEX ix_phys_middlename_trgm ON phys USING GIN (phys_middlename gin_trgm_ops)
    WHERE phys_middlename IS NOT NULL;

CREATE INDEX ix_org_name_trgm    ON org USING GIN (org_name    gin_trgm_ops);
CREATE INDEX ix_org_address_trgm ON org USING GIN (org_address gin_trgm_ops);
