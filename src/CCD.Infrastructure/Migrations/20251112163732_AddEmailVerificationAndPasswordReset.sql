-- Migración: AddEmailVerificationAndPasswordReset
-- Ejecutar este script directamente en PostgreSQL si dotnet ef no funciona

-- Agregar columnas de verificación de email
ALTER TABLE "Users" 
ADD COLUMN IF NOT EXISTS "EmailVerified" boolean NOT NULL DEFAULT false;

ALTER TABLE "Users" 
ADD COLUMN IF NOT EXISTS "EmailVerificationToken" text NULL;

ALTER TABLE "Users" 
ADD COLUMN IF NOT EXISTS "EmailVerificationTokenExpiry" timestamp with time zone NULL;

-- Agregar columnas de recuperación de contraseña
ALTER TABLE "Users" 
ADD COLUMN IF NOT EXISTS "PasswordResetToken" text NULL;

ALTER TABLE "Users" 
ADD COLUMN IF NOT EXISTS "PasswordResetTokenExpiry" timestamp with time zone NULL;

-- Verificar que las columnas se agregaron correctamente
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns 
WHERE table_name = 'Users' 
AND column_name IN ('EmailVerified', 'EmailVerificationToken', 'EmailVerificationTokenExpiry', 'PasswordResetToken', 'PasswordResetTokenExpiry')
ORDER BY column_name;

