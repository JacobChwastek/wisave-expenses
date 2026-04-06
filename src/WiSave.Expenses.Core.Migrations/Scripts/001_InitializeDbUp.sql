DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = 'config') THEN
        CREATE SCHEMA config;
    END IF;
END $$;
