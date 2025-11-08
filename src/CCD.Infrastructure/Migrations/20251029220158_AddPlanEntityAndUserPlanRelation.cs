using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CCD.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanEntityAndUserPlanRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Crear tabla Plans solo si no existe (usando SQL)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Plans"" (
                    ""Id"" uuid NOT NULL,
                    ""Name"" text NOT NULL,
                    ""MaxDatabases"" integer NOT NULL,
                    ""IsActive"" boolean NOT NULL,
                    CONSTRAINT ""PK_Plans"" PRIMARY KEY (""Id"")
                );
            ");

            // Agregar columnas faltantes a la tabla Plans si ya existía
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    -- Agregar MaxDatabases si no existe
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Plans' AND column_name = 'MaxDatabases'
                    ) THEN
                        ALTER TABLE ""Plans"" ADD COLUMN ""MaxDatabases"" integer NOT NULL DEFAULT 2;
                    END IF;
                    
                    -- Agregar IsActive si no existe
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Plans' AND column_name = 'IsActive'
                    ) THEN
                        ALTER TABLE ""Plans"" ADD COLUMN ""IsActive"" boolean NOT NULL DEFAULT true;
                    END IF;
                END $$;
            ");

            // Agregar PlanId solo si no existe
            // Verificar el tipo de dato de Plans.Id para usar el mismo tipo
            migrationBuilder.Sql(@"
                DO $$ 
                DECLARE
                    plans_id_type text;
                    default_plan_id integer := 1;
                BEGIN
                    -- Obtener el tipo de dato de la columna Id de Plans
                    SELECT data_type INTO plans_id_type
                    FROM information_schema.columns
                    WHERE table_name = 'Plans' AND column_name = 'Id';
                    
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Users' AND column_name = 'PlanId'
                    ) THEN
                        -- Si Plans.Id es integer, usar integer para PlanId
                        IF plans_id_type = 'integer' THEN
                            -- Obtener el ID del plan gratuito o usar 1 por defecto
                            SELECT COALESCE(MIN(""Id""), 1) INTO default_plan_id
                            FROM ""Plans""
                            WHERE ""Name"" IN ('Free', 'Gratuito')
                            LIMIT 1;
                            
                            ALTER TABLE ""Users"" ADD COLUMN ""PlanId"" integer NOT NULL DEFAULT default_plan_id;
                        -- Si Plans.Id es uuid, usar uuid para PlanId
                        ELSIF plans_id_type = 'uuid' THEN
                            ALTER TABLE ""Users"" ADD COLUMN ""PlanId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
                        END IF;
                    END IF;
                END $$;
            ");

            // Crear índice solo si no existe
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Users_PlanId"" ON ""Users"" (""PlanId"");
            ");

            // Agregar foreign key solo si no existe
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM pg_constraint 
                        WHERE conname = 'FK_Users_Plans_PlanId'
                    ) THEN
                        ALTER TABLE ""Users"" 
                        ADD CONSTRAINT ""FK_Users_Plans_PlanId"" 
                        FOREIGN KEY (""PlanId"") 
                        REFERENCES ""Plans"" (""Id"") 
                        ON DELETE CASCADE;
                    END IF;
                END $$;
            ");

            // Insertar el plan gratuito por defecto solo si la tabla tiene las columnas correctas
            // Verificar el tipo de dato de Id antes de insertar
            migrationBuilder.Sql(@"
                DO $$ 
                DECLARE
                    id_type text;
                BEGIN
                    -- Obtener el tipo de dato de la columna Id
                    SELECT data_type INTO id_type
                    FROM information_schema.columns
                    WHERE table_name = 'Plans' AND column_name = 'Id';
                    
                    -- Solo insertar si la tabla tiene la columna MaxDatabases
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Plans' 
                        AND column_name = 'MaxDatabases'
                    ) THEN
                        -- Si Id es integer, usar el próximo valor de la secuencia
                        IF id_type = 'integer' THEN
                            INSERT INTO ""Plans"" (""Name"", ""MaxDatabases"", ""IsActive"")
                            SELECT 'Free', 2, true
                            WHERE NOT EXISTS (SELECT 1 FROM ""Plans"" WHERE ""Name"" = 'Free' OR ""Name"" = 'Gratuito');
                        -- Si Id es uuid, usar gen_random_uuid()
                        ELSIF id_type = 'uuid' THEN
                            INSERT INTO ""Plans"" (""Id"", ""Name"", ""MaxDatabases"", ""IsActive"")
                            SELECT gen_random_uuid(), 'Free', 2, true
                            WHERE NOT EXISTS (SELECT 1 FROM ""Plans"" WHERE ""Name"" = 'Free' OR ""Name"" = 'Gratuito');
                        END IF;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Plans_PlanId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Users_PlanId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PlanId",
                table: "Users");
        }
    }
}
