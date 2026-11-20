#!/usr/bin/env bash
set -euo pipefail

SQLCMD=/opt/mssql-tools18/bin/sqlcmd

for attempt in $(seq 1 60); do
	if "$SQLCMD" -S sqlserver -U sa -P "$MSSQL_SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
		exec "$SQLCMD" -S sqlserver -U sa -P "$MSSQL_SA_PASSWORD" -C \
			-v "APP_DB_PASSWORD=$APP_DB_PASSWORD" \
			-i /database/AlunosTech_Moderno.sql -b
	fi
	sleep 2
done

echo "SQL Server não ficou disponível no prazo de 120 segundos." >&2
exit 1
