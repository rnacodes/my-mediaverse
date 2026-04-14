START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    ALTER TABLE "Notes" ADD "AiDescription" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    ALTER TABLE "Notes" ADD "AiDescriptionGeneratedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    ALTER TABLE "Notes" ADD "Embedding" real[];
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    ALTER TABLE "Notes" ADD "EmbeddingGeneratedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    ALTER TABLE "Notes" ADD "EmbeddingModel" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    ALTER TABLE "Notes" ADD "IsDescriptionManual" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    ALTER TABLE "MediaItems" ADD "Embedding" real[];
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    ALTER TABLE "MediaItems" ADD "EmbeddingGeneratedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    ALTER TABLE "MediaItems" ADD "EmbeddingModel" character varying(100);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    CREATE INDEX "IX_Notes_IsDescriptionManual" ON "Notes" ("IsDescriptionManual");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260112002842_AddAIEmbeddingFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260112002842_AddAIEmbeddingFields', '9.0.7');
    END IF;
END $EF$;
COMMIT;

