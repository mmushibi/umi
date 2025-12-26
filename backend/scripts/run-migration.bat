@echo off
REM UmiHealth Database Migration Script (Windows Batch)
REM This script runs the initial database migration

setlocal enabledelayedexpansion

echo Starting UmiHealth Database Migration...

REM Set PostgreSQL path
set PSQL_PATH="C:\Program Files\PostgreSQL\18\bin\psql.exe"

REM Check if psql is available
if not exist %PSQL_PATH% (
    echo Error: PostgreSQL client (psql) not found at %PSQL_PATH%. Please install PostgreSQL.
    pause
    exit /b 1
)

echo Found PostgreSQL client

REM Check if migration file exists
set MIGRATION_FILE=src\UmiHealth.Infrastructure\Migrations\InitialCreate.sql
if not exist "%MIGRATION_FILE%" (
    echo Error: Migration file not found at %MIGRATION_FILE%
    pause
    exit /b 1
)

echo Running migration from: %MIGRATION_FILE%

REM Set PGPASSWORD environment variable
set PGPASSWORD=root

REM Run the migration
%PSQL_PATH% -h localhost -U postgres -d umihealth -f %MIGRATION_FILE%

if errorlevel 1 (
    echo Migration failed with exit code: %errorlevel%
    set PGPASSWORD=
    pause
    exit /b %errorlevel%
)

echo Migration completed successfully!

REM Clean up environment variable
set PGPASSWORD=

echo Database migration process completed.
pause
