#!/usr/bin/env bash
set -euo pipefail

COMPOSE="docker compose -f compose.app.yml"
DB_USER="crm_user"
DB_NAME="crm_db"

# Определяем путь к бекапам
if $COMPOSE exec -T db-backup ls /backups/crm_db/daily/ &>/dev/null; then
  BACKUP_DIR="/backups/crm_db/daily"
elif $COMPOSE exec -T db-backup ls /backups/daily/ &>/dev/null; then
  BACKUP_DIR="/backups/daily"
else
  echo "Директория с бекапами не найдена."
  exit 1
fi

# Получаем список бекапов
echo "Доступные бекапы:"
echo ""

mapfile -t BACKUPS < <($COMPOSE exec -T db-backup ls -1 "$BACKUP_DIR" 2>/dev/null | grep '\.sql\.gz$' | sort -r)

if [ ${#BACKUPS[@]} -eq 0 ]; then
  echo "Бекапы не найдены."
  exit 1
fi

for i in "${!BACKUPS[@]}"; do
  echo "  $((i + 1))) ${BACKUPS[$i]}"
done

echo ""
read -rp "Выбери номер бекапа: " CHOICE

if ! [[ "$CHOICE" =~ ^[0-9]+$ ]] || [ "$CHOICE" -lt 1 ] || [ "$CHOICE" -gt ${#BACKUPS[@]} ]; then
  echo "Неверный выбор."
  exit 1
fi

SELECTED="${BACKUPS[$((CHOICE - 1))]}"
echo ""
echo "Выбран: $SELECTED"
read -rp "Восстановить этот бекап? Текущие данные будут УДАЛЕНЫ. (y/n): " CONFIRM

if [ "$CONFIRM" != "y" ]; then
  echo "Отменено."
  exit 0
fi

TMPDIR=$(mktemp -d /tmp/crm_backup_XXXXXX)
trap 'rm -rf "$TMPDIR"' EXIT

echo ""
echo "1/5 Копирование бекапа..."
$COMPOSE cp "db-backup:${BACKUP_DIR}/${SELECTED}" "$TMPDIR/"

echo "2/5 Распаковка..."
gunzip -f "$TMPDIR/$SELECTED"
SQL_FILE="$TMPDIR/${SELECTED%.gz}"

echo "3/5 Остановка API..."
$COMPOSE stop api

echo "4/5 Пересоздание базы..."
$COMPOSE exec -T db dropdb -U "$DB_USER" --if-exists "$DB_NAME"
$COMPOSE exec -T db createdb -U "$DB_USER" "$DB_NAME"

echo "5/5 Загрузка бекапа..."
$COMPOSE exec -T db psql -U "$DB_USER" -d "$DB_NAME" < "$SQL_FILE"

echo ""
echo "Запуск API..."
$COMPOSE start api

echo ""
echo "Готово! База восстановлена из $SELECTED"
