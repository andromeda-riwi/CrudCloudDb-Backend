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
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Users' AND column_name = 'PlanId'
                    ) THEN
                        ALTER TABLE ""Users"" ADD COLUMN ""PlanId"" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
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
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'Plans' 
                        AND column_name = 'MaxDatabases'
                    ) THEN
                        INSERT INTO ""Plans"" (""Id"", ""Name"", ""MaxDatabases"", ""IsActive"")
                        SELECT gen_random_uuid(), 'Free', 2, true
                        WHERE NOT EXISTS (SELECT 1 FROM ""Plans"" WHERE ""Name"" = 'Free');
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
