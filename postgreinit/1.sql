-- Create databases if they don't exist
CREATE DATABASE "VoterSystem";


-- Create user if not exists (PostgreSQL does not support IF NOT EXISTS for users)
DO
$$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '${server_access}') THEN
        CREATE USER "${server_access}" WITH PASSWORD '${test_password}';
    END IF;
END
$$;

-- Connect to the LocalKornerDb database to create the __EFMigrationsHistory table if it doesn't exist
\c "VoterSystem";

DO
$$
BEGIN
    -- Create the __EFMigrationsHistory table if it doesn't exist
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory') THEN
        CREATE TABLE "__EFMigrationsHistory" (
            "MigrationId" VARCHAR(150) NOT NULL,
            "ProductVersion" VARCHAR(32) NOT NULL,
            PRIMARY KEY ("MigrationId")
        );
    END IF;
END
$$;

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE "VoterSystem" TO "${server_access}";
ALTER USER "${server_access}" WITH SUPERUSER;
