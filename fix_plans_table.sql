-- Script para agregar todas las columnas faltantes a la tabla Plans
-- Ejecutar este script directamente en PostgreSQL antes de usar la aplicación

DO $$ 
BEGIN
    -- Agregar DatabaseLimitPerEngine si no existe
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'Plans' 
        AND column_name = 'DatabaseLimitPerEngine'
    ) THEN
        ALTER TABLE "Plans" 
        ADD COLUMN "DatabaseLimitPerEngine" integer NOT NULL DEFAULT 2;
        RAISE NOTICE 'Columna DatabaseLimitPerEngine agregada exitosamente';
    END IF;
    
    -- Agregar Price si no existe
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'Plans' 
        AND column_name = 'Price'
    ) THEN
        ALTER TABLE "Plans" 
        ADD COLUMN "Price" decimal(18,2) NOT NULL DEFAULT 0;
        RAISE NOTICE 'Columna Price agregada exitosamente';
    END IF;
    
    -- Agregar MercadoPagoPriceId si no existe
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'Plans' 
        AND column_name = 'MercadoPagoPriceId'
    ) THEN
        ALTER TABLE "Plans" 
        ADD COLUMN "MercadoPagoPriceId" text NOT NULL DEFAULT 'N/A';
        RAISE NOTICE 'Columna MercadoPagoPriceId agregada exitosamente';
    END IF;
    
    -- Agregar MaxDatabases si no existe
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'Plans' 
        AND column_name = 'MaxDatabases'
    ) THEN
        ALTER TABLE "Plans" 
        ADD COLUMN "MaxDatabases" integer NOT NULL DEFAULT 2;
        RAISE NOTICE 'Columna MaxDatabases agregada exitosamente';
    END IF;
    
    -- Agregar IsActive si no existe
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns 
        WHERE table_schema = 'public' 
        AND table_name = 'Plans' 
        AND column_name = 'IsActive'
    ) THEN
        ALTER TABLE "Plans" 
        ADD COLUMN "IsActive" boolean NOT NULL DEFAULT true;
        RAISE NOTICE 'Columna IsActive agregada exitosamente';
    END IF;
END $$;

-- Verificar que todas las columnas existen
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_schema = 'public' 
AND table_name = 'Plans'
ORDER BY column_name;

