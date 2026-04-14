DO $$ BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = 'projections') THEN
        CREATE SCHEMA projections;
    END IF;
END $$;
