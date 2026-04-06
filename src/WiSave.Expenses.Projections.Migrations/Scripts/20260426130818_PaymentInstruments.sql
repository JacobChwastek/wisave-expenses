START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260426130818_PaymentInstruments') THEN
CREATE TABLE projections.funding_payment_instruments (
                                                         "Id" text NOT NULL,
                                                         "FundingAccountId" text NOT NULL,
                                                         "UserId" text NOT NULL,
                                                         "Name" text NOT NULL,
                                                         "Kind" character varying(32) NOT NULL,
                                                         "LastFourDigits" character varying(4),
                                                         "Network" character varying(32),
                                                         "Color" text,
                                                         "IsActive" boolean NOT NULL,
                                                         "CreatedAt" timestamp with time zone NOT NULL,
                                                         "UpdatedAt" timestamp with time zone,
                                                         CONSTRAINT "PK_funding_payment_instruments" PRIMARY KEY ("FundingAccountId", "Id")
);
END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260426130818_PaymentInstruments') THEN
CREATE INDEX "IX_funding_payment_instruments_FundingAccountId" ON projections.funding_payment_instruments ("FundingAccountId");
END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260426130818_PaymentInstruments') THEN
CREATE INDEX "IX_funding_payment_instruments_UserId" ON projections.funding_payment_instruments ("UserId");
END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260426130818_PaymentInstruments') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260426130818_PaymentInstruments', '10.0.7');
END IF;
END $EF$;
COMMIT;
