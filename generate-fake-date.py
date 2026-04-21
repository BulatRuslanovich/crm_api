import os
import random
from datetime import timedelta

import psycopg2
from psycopg2.extras import execute_batch
from faker import Faker


fake = Faker("ru_RU")

from faker.providers import internet
fake.add_provider(internet)

conn = psycopg2.connect(
    dbname=os.getenv("DB_NAME", "crm_db"),
    user=os.getenv("DB_USER", "crm_user"),
    password=os.getenv("DB_PASSWORD", "change_me_dev_password"),
    host=os.getenv("DB_HOST", "localhost"),
    port=int(os.getenv("DB_PORT", "5432"))
)

cur = conn.cursor()

# ---------------------------
# НАСТРОЙКИ
# ---------------------------
NUM_ORG = 120
NUM_PHYS = 400
NUM_DRUG = 120
ACTIV_PER_USER = 300

# РФ города (чтобы не было мусора)
CITIES = [
    "Москва", "Санкт-Петербург", "Казань", "Новосибирск", "Екатеринбург",
    "Нижний Новгород", "Челябинск", "Самара", "Омск", "Ростов-на-Дону",
    "Уфа", "Красноярск", "Пермь", "Воронеж", "Волгоград"
]

# ---------------------------
# DEPARTMENTS
# ---------------------------
departments = ["Москва", "Северо-Запад", "Поволжье"]

execute_batch(cur,
    "INSERT INTO department (department_name) VALUES (%s) ON CONFLICT DO NOTHING",
    [(d,) for d in departments]
)

cur.execute("SELECT department_id FROM department")
department_ids = [r[0] for r in cur.fetchall()]

# ---------------------------
# USERS + POLICIES
# ---------------------------
users = []

# 1 Director (глобальный)
users.append(("Director", None))

for dep_id in department_ids:
    # 1 Manager
    users.append(("Manager", dep_id))

    # 3 Representatives
    for _ in range(3):
        users.append(("Representative", dep_id))

usr_records = []
for role, dep_id in users:
    first = fake.first_name()
    last = fake.last_name()
    login = fake.user_name() + str(random.randint(100, 999))

    usr_records.append((
        first,
        last,
        fake.unique.email(),
        login,
        "hash",
        True,
        False
    ))

execute_batch(cur, """
INSERT INTO usr (
    usr_firstname, usr_lastname, usr_email,
    usr_login, usr_password_hash,
    is_email_confirmed, is_deleted
)
VALUES (%s,%s,%s,%s,%s,%s,%s)
""", usr_records)

conn.commit()

cur.execute("SELECT usr_id FROM usr ORDER BY usr_id DESC LIMIT %s", (len(usr_records),))
usr_ids = [r[0] for r in cur.fetchall()]

# policy map
cur.execute("SELECT policy_id, policy_name FROM policy")
policy_map = {name: pid for pid, name in cur.fetchall()}

usr_policy = []
usr_department = []

for (role, dep_id), uid in zip(users, usr_ids):

    # POLICY
    usr_policy.append((uid, policy_map[role]))

    # DEPARTMENT (кроме директора)
    if dep_id is not None:
        usr_department.append((uid, dep_id))

execute_batch(cur,
    "INSERT INTO usr_policy (usr_id, policy_id) VALUES (%s,%s)",
    usr_policy
)

execute_batch(cur,
    "INSERT INTO usr_department (usr_id, department_id) VALUES (%s,%s)",
    usr_department
)

conn.commit()

# ---------------------------
# ORG (только РФ)
# ---------------------------
orgs = []
for _ in range(NUM_ORG):
    city = random.choice(CITIES)

    orgs.append((
        random.randint(1, 5),
        f"{fake.company()} ({city})",
        str(fake.random_number(digits=10, fix_len=True)),
        float(fake.latitude()),
        float(fake.longitude()),
        f"{city}, {fake.street_address()}"
    ))

execute_batch(cur, """
INSERT INTO org (org_type_id, org_name, org_inn, org_latitude, org_longitude, org_address)
VALUES (%s,%s,%s,%s,%s,%s)
""", orgs)

conn.commit()

cur.execute("SELECT org_id FROM org")
org_ids = [r[0] for r in cur.fetchall()]

# ---------------------------
# SPEC
# ---------------------------
specs = ["Терапевт", "Кардиолог", "Невролог", "Педиатр", "Хирург"]

execute_batch(cur,
    "INSERT INTO spec (spec_name) VALUES (%s) ON CONFLICT DO NOTHING",
    [(s,) for s in specs]
)

cur.execute("SELECT spec_id FROM spec")
spec_ids = [r[0] for r in cur.fetchall()]

# ---------------------------
# PHYS
# ---------------------------
phys = []
for _ in range(NUM_PHYS):
    phys.append((
        random.choice(spec_ids),
        fake.first_name(),
        fake.last_name(),
        fake.middle_name(),
        fake.phone_number(),
        fake.unique.email()
    ))

execute_batch(cur, """
INSERT INTO phys (spec_id, phys_firstname, phys_lastname, phys_middlename, phys_phone, phys_email)
VALUES (%s,%s,%s,%s,%s,%s)
""", phys)

conn.commit()

cur.execute("SELECT phys_id FROM phys")
phys_ids = [r[0] for r in cur.fetchall()]

# ---------------------------
# PHYS_ORG
# ---------------------------
phys_org = []
for pid in phys_ids:
    main_org = random.choice(org_ids)

    phys_org.append((pid, main_org, True))

    for _ in range(random.randint(0, 2)):
        phys_org.append((pid, random.choice(org_ids), False))

execute_batch(cur, """
INSERT INTO phys_org (phys_id, org_id, is_main)
VALUES (%s,%s,%s)
ON CONFLICT DO NOTHING
""", phys_org)

conn.commit()

# ---------------------------
# DRUG
# ---------------------------
forms = ["Таблетки", "Капсулы", "Сироп", "Раствор"]

drugs = []
for _ in range(NUM_DRUG):
    drugs.append((
        fake.word().capitalize(),
        fake.company(),
        random.choice(forms)
    ))

execute_batch(cur,
    "INSERT INTO drug (drug_name, drug_brand, drug_form) VALUES (%s,%s,%s)",
    drugs
)

conn.commit()

cur.execute("SELECT drug_id FROM drug")
drug_ids = [r[0] for r in cur.fetchall()]

# ---------------------------
# ACTIV (главное)
# ---------------------------
activs = []

rep_user_ids = [
    uid for (role, _), uid in zip(users, usr_ids)
    if role == "Representative"
]

for uid in rep_user_ids:
    for _ in range(ACTIV_PER_USER):

        start = fake.date_time_between(start_date="-365d", end_date="now")

        if random.random() < 0.3:
            end = None
        else:
            end = start + timedelta(days=random.randint(1, 20))

        # либо org, либо phys
        if random.random() < 0.5:
            org_id = random.choice(org_ids)
            phys_id = None
        else:
            org_id = None
            phys_id = random.choice(phys_ids)

        activs.append((
            uid,
            org_id,
            phys_id,
            random.randint(1, 4),
            start,
            end,
            fake.sentence(nb_words=8)
        ))

execute_batch(cur, """
INSERT INTO activ (
    usr_id, org_id, phys_id, status_id,
    activ_start, activ_end, activ_description
)
VALUES (%s,%s,%s,%s,%s,%s,%s)
""", activs)

conn.commit()

# ---------------------------
# ACTIV_DRUG
# ---------------------------
cur.execute("SELECT activ_id FROM activ ORDER BY activ_id DESC LIMIT %s",
            (len(activs),))
activ_ids = [r[0] for r in cur.fetchall()]

links = []
for aid in activ_ids:
    for _ in range(random.randint(1, 3)):
        links.append((aid, random.choice(drug_ids)))

execute_batch(cur,
    "INSERT INTO activ_drug (activ_id, drug_id) VALUES (%s,%s) ON CONFLICT DO NOTHING",
    links
)

conn.commit()

print("✅ DATA GENERATED")

cur.close()
conn.close()
