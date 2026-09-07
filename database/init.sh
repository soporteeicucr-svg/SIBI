#!/bin/bash
set -e

SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
MIGRATIONS_DIR="/docker-init/migrations"

# Envoltura para no repetir credenciales y no romper si la contraseña trae caracteres raros.
run_sql() { "$SQLCMD" -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C "$@"; }

# Iniciar SQL Server en segundo plano
/opt/mssql/bin/sqlservr &
SQL_PID=$!

echo "[SIBI-DB] Esperando que SQL Server esté listo..."
RETRIES=30
until run_sql -Q "SELECT 1" &>/dev/null; do
    RETRIES=$((RETRIES - 1))
    if [ "$RETRIES" -le 0 ]; then
        echo "[SIBI-DB] ERROR: SQL Server no respondió en el tiempo límite."
        exit 1
    fi
    echo "[SIBI-DB] No está listo aún, reintentando... ($RETRIES intentos restantes)"
    sleep 2
done
echo "[SIBI-DB] SQL Server listo."

# ── 1. Esquema inicial (solo la primera vez) ──────────────────────────────────
DB_EXISTS=$(run_sql -h -1 -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name = 'SIBI'" 2>/dev/null | tr -d '[:space:]')

if [ "$DB_EXISTS" = "0" ]; then
    echo "[SIBI-DB] Base de datos no encontrada. Creando esquema inicial (SIBI.sql)..."
    run_sql -b -i /docker-init/SIBI.sql
    echo "[SIBI-DB] Insertando datos iniciales (seed.sql)..."
    run_sql -b -d SIBI -i /docker-init/seed.sql
else
    echo "[SIBI-DB] Base de datos SIBI ya existe."
fi

# ── 2. Migraciones idempotentes, registradas en SchemaMigracion ───────────────
#    Se aplican SIEMPRE (fresh o existente): en un fresh son no-ops porque
#    SIBI.sql ya trae el esquema al día; en una base vieja actualizan el schema.
echo "[SIBI-DB] Verificando migraciones..."
run_sql -b -d SIBI -Q "
IF OBJECT_ID('dbo.SchemaMigracion','U') IS NULL
    CREATE TABLE SchemaMigracion (
        Archivo    NVARCHAR(200) NOT NULL CONSTRAINT PK_SchemaMigracion PRIMARY KEY,
        AplicadaEn DATETIME      NOT NULL DEFAULT GETDATE()
    );"

if [ -d "$MIGRATIONS_DIR" ]; then
    for f in "$MIGRATIONS_DIR"/*.sql; do
        [ -e "$f" ] || continue
        name=$(basename "$f")
        applied=$(run_sql -h -1 -d SIBI \
            -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM SchemaMigracion WHERE Archivo = N'$name'" \
            2>/dev/null | tr -d '[:space:]')
        if [ "$applied" = "0" ]; then
            echo "[SIBI-DB]   -> aplicando $name"
            run_sql -b -d SIBI -i "$f"
            run_sql -b -d SIBI -Q "INSERT INTO SchemaMigracion (Archivo) VALUES (N'$name');"
        else
            echo "[SIBI-DB]   -- $name ya aplicada"
        fi
    done
fi
echo "[SIBI-DB] Migraciones al día. Base de datos lista."

# Mantener SQL Server en primer plano
wait $SQL_PID
