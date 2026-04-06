CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                                                       "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
    );

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260423222821_Initial') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'config') THEN
CREATE SCHEMA config;
END IF;
END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260423222821_Initial') THEN
CREATE TABLE config.categories (
                                   "Id" character varying(64) NOT NULL,
                                   "UserId" character varying(64) NOT NULL,
                                   "Name" character varying(200) NOT NULL,
                                   "SortOrder" integer NOT NULL DEFAULT 0,
                                   CONSTRAINT "PK_categories" PRIMARY KEY ("Id")
);
END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260423222821_Initial') THEN
CREATE TABLE config.subcategories (
                                      "Id" character varying(64) NOT NULL,
                                      "CategoryId" character varying(64) NOT NULL,
                                      "Name" character varying(200) NOT NULL,
                                      "SortOrder" integer NOT NULL DEFAULT 0,
                                      CONSTRAINT "PK_subcategories" PRIMARY KEY ("Id")
);
END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260423222821_Initial') THEN
CREATE INDEX "IX_categories_UserId" ON config.categories ("UserId");
END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260423222821_Initial') THEN
CREATE INDEX "IX_subcategories_CategoryId" ON config.subcategories ("CategoryId");
END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260423222821_Initial') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260423222821_Initial', '10.0.7');
END IF;
END $EF$;
COMMIT;

