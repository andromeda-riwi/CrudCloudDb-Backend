# 🚀 CrudCloudDb Platform - Backend API

![Status](https://img.shields.io/badge/status-production-success)
![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![License](https://img.shields.io/badge/license-MIT-green)

**Plataforma de gestión automatizada de bases de datos en la nube tipo Clever Cloud**

---

## 📋 Tabla de Contenidos

- [Descripción](#-descripción)
- [Arquitectura](#-arquitectura)
- [Tecnologías y Dependencias](#-tecnologías-y-dependencias)
- [Características Implementadas](#-características-implementadas)
- [Endpoints de la API](#-endpoints-de-la-api)
- [Configuración](#️-configuración)
- [Instalación y Despliegue](#-instalación-y-despliegue)
- [Sistema de Planes](#-sistema-de-planes)
- [Motores de Bases de Datos](#-motores-de-bases-de-datos)
- [Integraciones](#-integraciones)
- [Seguridad](#-seguridad)
- [Estructura del Proyecto](#-estructura-del-proyecto)

---

## 📖 Descripción

**CrudCloudDb (CCD)** es una API REST construida con **ASP.NET Core 8** que permite a los usuarios gestionar instancias de bases de datos en la nube de manera automatizada. Los usuarios pueden crear, administrar y eliminar bases de datos de distintos motores (PostgreSQL, MySQL, MongoDB, SQL Server) mediante una interfaz programática segura y escalable.

### 🎯 Objetivo Principal

Proporcionar un backend robusto para una plataforma de gestión de bases de datos que permita:
- Registro y autenticación de usuarios con verificación de email
- Creación automática de bases de datos con credenciales únicas
- Gestión de planes (gratuito, intermedio, avanzado)
- Integración con pasarela de pagos (Mercado Pago)
- Sistema de webhooks personalizables
- Auditoría completa de acciones
- Notificaciones por email automatizadas

### 🌐 Dominios

- **Backend API**: `https://service.andromeda.andrescortes.dev`
- **Frontend**: `https://andromeda.andrescortes.dev`
- **Swagger UI**: `https://service.andromeda.andrescortes.dev/swagger`

---

## 🏗️ Arquitectura

El proyecto sigue una **arquitectura en capas (Clean Architecture)** para mantener la separación de responsabilidades y facilitar el mantenimiento:

```
┌─────────────────────────────────────────────────────────┐
│                    CCD.Api (Presentación)               │
│  Controllers, Middleware, DTOs, Program.cs              │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│              CCD.Core (Lógica de Negocio)               │
│  Interfaces, Entidades, Reglas de negocio               │
└─────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────┐
│          CCD.Infrastructure (Datos y Servicios)         │
│  Repositorios, DbContext, Servicios externos            │
└─────────────────────────────────────────────────────────┘
```

### Capas del Proyecto

#### **1. CCD.Api (Capa de Presentación)**
- **Propósito**: Exponer endpoints HTTP y manejar peticiones/respuestas
- **Responsabilidades**:
  - Controladores REST
  - Configuración de JWT
  - Middleware de excepciones global
  - Validación de entrada
  - Documentación con Swagger

#### **2. CCD.Core (Capa de Dominio)**
- **Propósito**: Contener la lógica de negocio pura
- **Responsabilidades**:
  - Entidades del dominio (User, DatabaseInstance, Webhook, etc.)
  - Interfaces de servicios
  - DTOs compartidos
  - Reglas de negocio

#### **3. CCD.Infrastructure (Capa de Infraestructura)**
- **Propósito**: Implementar persistencia y servicios externos
- **Responsabilidades**:
  - Entity Framework Core y migraciones
  - Implementación de repositorios
  - Servicios de email (SendGrid)
  - Servicios de pago (Mercado Pago)
  - Provisión de bases de datos
  - Sistema de webhooks

---

## 🛠️ Tecnologías y Dependencias

### Stack Principal

| Tecnología | Versión | Propósito |
|------------|---------|-----------|
| .NET | 8.0 | Framework principal |
| ASP.NET Core | 8.0 | Web API |
| Entity Framework Core | 8.0.4 | ORM para PostgreSQL |
| PostgreSQL | 16+ | Base de datos principal |
| Docker | Latest | Contenedores |

### 📦 Dependencias Principales

#### **CCD.Api**

```xml
<!-- Autenticación y Autorización -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.4" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.0" />

<!-- Documentación API -->
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2" />

<!-- Email -->
<PackageReference Include="SendGrid" Version="9.29.3" />

<!-- Utilidades -->
<PackageReference Include="DotNetEnv" Version="3.1.1" />
<PackageReference Include="TimeZoneConverter" Version="6.1.0" />
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />

<!-- Entity Framework Design Tools -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.4" />
```

**Explicación de dependencias CCD.Api:**

- **`Microsoft.AspNetCore.Authentication.JwtBearer`**: Implementa autenticación mediante tokens JWT (JSON Web Tokens). Los usuarios reciben un token al iniciar sesión que debe incluirse en cada petición protegida.

- **`Swashbuckle.AspNetCore`**: Genera automáticamente la documentación interactiva Swagger/OpenAPI para probar los endpoints de la API desde el navegador.

- **`SendGrid`**: Cliente oficial de SendGrid para enviar correos electrónicos (verificación de cuenta, credenciales de BD, recuperación de contraseña).

- **`DotNetEnv`**: Permite cargar variables de entorno desde archivos `.env` para configuración local y producción.

- **`TimeZoneConverter`**: Maneja conversión de zonas horarias, útil para registros de auditoría y timestamps.

- **`Newtonsoft.Json`**: Librería de serialización/deserialización JSON, usada para procesar respuestas de Mercado Pago y webhooks.

#### **CCD.Infrastructure**

```xml
<!-- ORM y Base de Datos -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.4" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.4" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.4" />

<!-- Drivers de Bases de Datos -->
<PackageReference Include="MySql.Data" Version="8.4.0" />
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.0" />
<PackageReference Include="MongoDB.Driver" Version="2.28.0" />

<!-- Integración de Pagos -->
<PackageReference Include="mercadopago-sdk" Version="2.10.1" />

<!-- Email -->
<PackageReference Include="SendGrid" Version="9.29.3" />

<!-- HTTP Client -->
<PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />

<!-- Configuración -->
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Binder" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
```

**Explicación de dependencias CCD.Infrastructure:**

- **`Microsoft.EntityFrameworkCore`**: ORM (Object-Relational Mapper) que permite trabajar con bases de datos usando objetos C# en lugar de SQL directo. Maneja migraciones, consultas LINQ y seguimiento de cambios.

- **`Npgsql.EntityFrameworkCore.PostgreSQL`**: Proveedor de Entity Framework Core para PostgreSQL, la base de datos principal de la aplicación donde se almacenan usuarios, planes, instancias de BD, webhooks y auditoría.

- **`MySql.Data`**: Driver oficial de MySQL para .NET. Permite ejecutar comandos SQL directos en servidores MySQL para crear bases de datos, usuarios y gestionar permisos.

- **`Microsoft.Data.SqlClient`**: Cliente moderno de SQL Server para .NET. Ejecuta comandos T-SQL para provisionar bases de datos en SQL Server.

- **`MongoDB.Driver`**: Driver oficial de MongoDB para .NET. Permite conectarse a servidores MongoDB y crear bases de datos usando comandos nativos de Mongo.

- **`mercadopago-sdk`**: SDK oficial de Mercado Pago para integración de pagos. Permite crear preferencias de pago y procesar notificaciones IPN (Instant Payment Notification).

- **`SendGrid`**: Cliente de SendGrid duplicado en Infrastructure para envío directo de emails desde servicios.

- **`Microsoft.Extensions.Http`**: Proporciona `HttpClientFactory` para hacer peticiones HTTP a webhooks de usuarios y APIs externas de forma eficiente.

### 🗄️ Bases de Datos Requeridas

El sistema utiliza múltiples bases de datos:

1. **PostgreSQL** (Principal)
   - Base de datos de la aplicación (`ccd_dev_db`)
   - Almacena: usuarios, planes, instancias de BD, webhooks, auditoría
   - Puerto: `5432`

2. **MySQL** (Provisión)
   - Servidor para crear bases de datos de usuarios
   - Puerto: `3306`

3. **MongoDB** (Provisión)
   - Servidor para crear bases de datos de usuarios
   - Puerto: `27017`

4. **SQL Server** (Provisión)
   - Servidor para crear bases de datos de usuarios
   - Puerto: `1433`

---

## ✨ Características Implementadas

### ✅ Sistema de Autenticación Completo

- **Registro de usuarios** con validación de datos
- **Verificación de email** obligatoria con token temporal (24h)
- **Login con JWT** (token válido por 24 horas)
- **Recuperación de contraseña** con token temporal (1h)
- **Cambio de contraseña** para usuarios autenticados
- **Hash seguro de contraseñas** usando HMACSHA512 con salt único por usuario

### ✅ Gestión de Bases de Datos

- **Creación automática** de bases de datos en 4 motores
- **Generación de credenciales únicas** (usuario, contraseña, puerto, host)
- **Aislamiento completo** entre bases de datos de diferentes usuarios
- **Validación de cuotas** según plan del usuario
- **Eliminación segura** con limpieza completa (base de datos + usuario)
- **Visualización de credenciales** solo la primera vez (por seguridad)
- **Notificación por email** con credenciales al crear BD

### ✅ Sistema de Planes

| Plan | Bases de Datos | Precio | Estado |
|------|----------------|--------|--------|
| **Gratuito** | 2 por motor | Gratis | Asignado automáticamente al registrarse |
| **Intermedio** | 5 por motor | $5,000 COP | Mediante pago en Mercado Pago |
| **Avanzado** | 10 por motor | $10,000 COP | Mediante pago en Mercado Pago |

**Nota**: El sistema acepta pagos únicos, NO suscripciones recurrentes.

### ✅ Integración con Mercado Pago

- **Creación de preferencias** de pago para planes
- **Webhook IPN** para recibir notificaciones de pago
- **Actualización automática** del plan tras confirmación
- **Registro de transacciones** en historial de pagos

### ✅ Sistema de Webhooks Personalizados

Los usuarios pueden configurar **webhooks personalizados** para recibir notificaciones en tiempo real de eventos:

**Eventos disponibles:**
- `account.created` - Usuario registrado
- `database.created` - Base de datos creada
- `database.deleted` - Base de datos eliminada
- `plan.changed` - Plan actualizado
- `payment.confirmed` - Pago confirmado

**Características:**
- ✅ Firma HMAC-SHA256 para seguridad
- ✅ Reintentos automáticos (4 intentos)
- ✅ Historial completo de envíos
- ✅ Activar/desactivar individualmente
- ✅ Probar webhook manualmente

**⚠️ Importante**: Los webhooks están completamente implementados en el backend con 8 endpoints funcionales, pero **NO tienen interfaz de usuario en el frontend**. Se gestionan mediante:
- Swagger UI
- Postman o herramientas similares
- Llamadas directas a la API

### ✅ Sistema de Emails Automatizados

Correos enviados automáticamente usando **SendGrid**:

1. **Verificación de cuenta** - Al registrarse (token válido 24h)
2. **Credenciales de BD** - Al crear una base de datos
3. **Confirmación de eliminación** - Al eliminar una BD
4. **Confirmación de pago** - Tras pago exitoso
5. **Recuperación de contraseña** - Token válido 1h
6. **Cambio de plan** - Al actualizar plan

### ✅ Sistema de Auditoría

Registro completo de acciones importantes:

- ✅ Registro de usuarios
- ✅ Inicios de sesión
- ✅ Creación de bases de datos
- ✅ Eliminación de bases de datos
- ✅ Cambios de plan
- ✅ Almacenamiento de IP y timestamp
- ✅ Consulta de historial por usuario

### ✅ Error Reporting

- ✅ Middleware global de excepciones
- ✅ Captura automática de errores
- ✅ Logs detallados en consola
- ✅ Endpoint para reportar errores manualmente

---

## 🔌 Endpoints de la API

Total: **31 endpoints**

### 🔐 Autenticación (6 endpoints)

```http
POST   /api/auth/register              # Registrar nuevo usuario
POST   /api/auth/login                 # Iniciar sesión (devuelve JWT)
POST   /api/auth/verify-email          # Verificar email con token
POST   /api/auth/resend-verification   # Reenviar email de verificación
POST   /api/auth/forgot-password       # Solicitar recuperación de contraseña
POST   /api/auth/reset-password        # Restablecer contraseña con token
```

**Ejemplo de registro:**
```json
POST /api/auth/register
{
  "name": "Juan",
  "lastName": "Pérez",
  "userName": "juanperez",
  "email": "juan@example.com",
  "password": "SecurePass123!"
}
```

**Respuesta:**
```json
{
  "message": "Usuario registrado exitosamente. Por favor verifica tu correo electrónico.",
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

### 👤 Usuarios (4 endpoints)

```http
GET    /api/users/me                   # Obtener datos del usuario actual
GET    /api/users/plan                 # Obtener información del plan actual
POST   /api/users/change-password      # Cambiar contraseña
PUT    /api/users/profile              # Actualizar perfil (nombre, apellido)
```

### 🗄️ Bases de Datos (6 endpoints)

```http
GET    /api/databases                  # Listar todas las BD del usuario
POST   /api/databases                  # Crear nueva base de datos
GET    /api/databases/{id}             # Obtener detalles de una BD
GET    /api/databases/{id}/credentials # Ver credenciales (solo 1ª vez)
DELETE /api/databases/{id}             # Eliminar base de datos
GET    /api/databases/stats            # Estadísticas del dashboard
```

**Ejemplo de creación de BD:**
```json
POST /api/databases
Authorization: Bearer {jwt_token}
{
  "engine": "PostgreSQL",
  "timeZoneId": "America/Bogota"
}
```

**Respuesta:**
```json
{
  "id": "db-guid-123",
  "name": "user_02122f9a_b8b3",
  "engine": "PostgreSQL",
  "status": "Active",
  "createdAt": "2025-11-20T10:30:00Z",
  "connectionDetails": {
    "host": "49.12.100.202",
    "port": 5432,
    "databaseName": "user_02122f9a_b8b3",
    "username": "user_affde09948be",
    "password": "********",
    "connectionString": "Server=49.12.100.202;Port=5432;Database=user_02122f9a_b8b3;..."
  },
  "credentialsViewedAt": null
}
```

### 💳 Pagos (3 endpoints)

```http
POST   /api/payments/preference        # Crear preferencia de pago en Mercado Pago
GET    /api/payments/history           # Historial de pagos del usuario
GET    /api/payments/plans             # Listar planes disponibles
```

### 🔔 Webhooks (8 endpoints)

```http
GET    /api/webhooks                   # Listar webhooks del usuario
GET    /api/webhooks/{id}              # Obtener webhook específico
POST   /api/webhooks                   # Crear nuevo webhook
PUT    /api/webhooks/{id}              # Actualizar webhook
PATCH  /api/webhooks/{id}/toggle       # Activar/desactivar webhook
DELETE /api/webhooks/{id}              # Eliminar webhook
GET    /api/webhooks/{id}/history      # Historial de eventos del webhook
POST   /api/webhooks/{id}/test         # Probar webhook manualmente
```

**⚠️ Solo disponibles por API (Swagger/Postman), sin UI en frontend**

### 📊 Auditoría (2 endpoints)

```http
GET    /api/audit                      # Logs del usuario actual
GET    /api/audit/all                  # Todos los logs (requiere permisos admin)
```

### ⚙️ Sistema (2 endpoints)

```http
POST   /api/webhook/mercadopago        # Webhook IPN de Mercado Pago
POST   /api/error-report               # Reportar errores de producción
```

---

## ⚙️ Configuración

### Variables de Entorno

Crear archivo `.env` en la raíz del proyecto:

```bash
# Entorno
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=https://+:443

# JWT
JWT_SECRET_TOKEN=tu-secreto-jwt-super-seguro-minimo-32-caracteres

# Mercado Pago
MERCADOPAGO_ACCESS_TOKEN=APP_USR-1234567890123456-123456-xxxxxxxxxxxxxxxxxxxxxxxx-123456789
MERCADOPAGO_WEBHOOK_SECRET=tu-secreto-webhook-mercadopago

# SendGrid
SENDGRID_API_KEY=SG.xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
SENDGRID_FROM_EMAIL=noreply@tudominio.com
SENDGRID_FROM_NAME=Tu Plataforma

# Dashboard URL
APP_DASHBOARD_URL=https://tudominio.com/dashboard

# Base de datos principal (PostgreSQL - Aplicación)
DEFAULT_CONNECTION=Server=localhost;Port=5432;Database=ccd_dev_db;User Id=root;Password=tu_password

# Conexiones para provisión de BD
ADMIN_POSTGRES_CONNECTION=Host=localhost;Port=5432;Database=postgres;Username=root;Password=tu_password
ADMIN_MYSQL_CONNECTION=Server=localhost;Port=3306;Database=mysql;User Id=root;Password=tu_password
ADMIN_SQLSERVER_CONNECTION=Server=localhost,1433;Database=master;User Id=sa;Password=tu_password;TrustServerCertificate=True
ADMIN_MONGO_CONNECTION=mongodb://root:tu_password@localhost:27017
```

### Configuración de appsettings.json

El archivo `appsettings.json` se gestiona automáticamente con las variables de entorno. Estructura básica:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Jwt": {
    "Key": "${JWT_SECRET_TOKEN}",
    "Issuer": "CCD.Api",
    "Audience": "CCD.Client",
    "ExpirationHours": 24
  },
  "ConnectionStrings": {
    "DefaultConnection": "${DEFAULT_CONNECTION}"
  }
}
```

---

## 🚀 Instalación y Despliegue

### Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/) (opcional, recomendado)
- [PostgreSQL 16+](https://www.postgresql.org/download/)
- Servidores de BD: MySQL, MongoDB, SQL Server

### Instalación Local

#### 1. Clonar el repositorio

```bash
git clone https://github.com/tu-usuario/CrudCloudDb-Backend.git
cd CrudCloudDb-Backend
```

#### 2. Restaurar dependencias

```bash
dotnet restore
```

#### 3. Configurar variables de entorno

```bash
cp .env.example .env
# Editar .env con tus credenciales
```

#### 4. Ejecutar migraciones

```bash
# Asegurarse de que PostgreSQL esté corriendo
dotnet ef database update --project src/CCD.Infrastructure --startup-project src/CCD.Api
```

#### 5. Ejecutar la aplicación

```bash
cd src/CCD.Api
dotnet run
```

La API estará disponible en:
- `http://localhost:5000` (HTTP)
- `https://localhost:5001` (HTTPS)
- `http://localhost:5000/swagger` (Documentación)

### Despliegue con Docker

#### 1. Construir imagen

```bash
docker build -t ccd-backend:latest -f src/CCD.Api/Dockerfile .
```

#### 2. Ejecutar contenedor

```bash
docker run -d \
  --name ccd_api \
  -p 8080:8080 \
  --env-file .env \
  ccd-backend:latest
```

#### 3. Con Docker Compose (recomendado)

```bash
docker-compose up -d
```

El archivo `docker-compose.yml` incluye:
- Backend API
- PostgreSQL
- MySQL
- MongoDB
- SQL Server (opcional)

### Verificar Despliegue

```bash
# Health check
curl http://localhost:8080/api/health

# Swagger
curl http://localhost:8080/swagger
```

---

## 💰 Sistema de Planes

### Plan Gratuito (Default)

- ✅ Asignado automáticamente al registrarse
- ✅ **2 bases de datos por motor** (total: 8 BD)
- ✅ Acceso a todos los endpoints
- ✅ Emails de notificación
- ✅ Webhooks personalizados (sin límite)

### Plan Intermedio ($5,000 COP)

- ✅ **5 bases de datos por motor** (total: 20 BD)
- ✅ Todas las características del plan gratuito
- ✅ Soporte prioritario

### Plan Avanzado ($10,000 COP)

- ✅ **10 bases de datos por motor** (total: 40 BD)
- ✅ Todas las características del plan intermedio
- ✅ Soporte dedicado

### Validación de Cuotas

El sistema valida automáticamente:
1. Número de bases de datos actuales del usuario por motor
2. Límite del plan actual
3. Bloquea creación si se alcanza el límite
4. Actualiza cuotas al cambiar de plan

**Ejemplo de error al exceder cuota:**
```json
{
  "error": "Has alcanzado el límite de bases de datos PostgreSQL para tu plan. Actualiza tu plan para crear más.",
  "currentCount": 2,
  "limit": 2,
  "planName": "Gratuito"
}
```

---

## 🗄️ Motores de Bases de Datos

### Soportados ✅

#### 1. PostgreSQL

**Características:**
- Puerto: `5432`
- Versión mínima: `12+`
- Encoding: `UTF8`
- Locale: `en_US.utf8`

**Proceso de creación:**
1. Generar nombre único: `user_{userId}_{random}`
2. Generar usuario único: `user_{random}`
3. Crear base de datos con `CREATE DATABASE`
4. Crear usuario con contraseña segura
5. Asignar privilegios completos al usuario
6. Revocar acceso público

#### 2. MySQL

**Características:**
- Puerto: `3306`
- Versión mínima: `8.0+`
- Charset: `utf8mb4`
- Collation: `utf8mb4_unicode_ci`

**Proceso de creación:**
1. Generar nombre único
2. Crear base de datos: `CREATE DATABASE`
3. Crear usuario: `CREATE USER`
4. Otorgar permisos: `GRANT ALL PRIVILEGES ON db.* TO user`
5. Aplicar cambios: `FLUSH PRIVILEGES`

#### 3. MongoDB

**Características:**
- Puerto: `27017`
- Versión mínima: `6.0+`
- Autenticación: SCRAM-SHA-256

**Proceso de creación:**
1. Generar nombre único
2. Crear base de datos (se crea al insertar primer documento)
3. Crear usuario con rol `dbOwner`
4. Asignar permisos de lectura/escritura

#### 4. SQL Server

**Características:**
- Puerto: `1433`
- Versión: `2022+`
- Autenticación: SQL Server Authentication
- Collation: `SQL_Latin1_General_CP1_CI_AS`

**Proceso de creación:**
1. Generar nombre único
2. Crear base de datos: `CREATE DATABASE`
3. Crear login: `CREATE LOGIN`
4. Crear usuario vinculado al login
5. Asignar rol `db_owner`

### No Soportados ❌

- **Redis**: No implementado
- **Cassandra**: No implementado

---

## 🔗 Integraciones

### 1. Mercado Pago

**SDK**: `mercadopago-sdk` v2.10.1

**Funcionalidades:**
- ✅ Crear preferencias de pago
- ✅ Webhook IPN para notificaciones
- ✅ Validación de pagos
- ✅ Actualización automática de planes

**Configuración:**
```bash
MERCADOPAGO_ACCESS_TOKEN=APP_USR-xxxxx
MERCADOPAGO_WEBHOOK_SECRET=xxxxx
```

**Webhook URL en Mercado Pago:**
```
https://service.andromeda.andrescortes.dev/api/webhook/mercadopago
```

**Eventos soportados:**
- `payment` - Notificación de pago

**Flujo de pago:**
1. Usuario solicita cambio de plan
2. Backend crea preferencia en Mercado Pago
3. Usuario completa pago
4. Mercado Pago envía IPN al webhook
5. Backend valida y actualiza plan
6. Usuario recibe email de confirmación

### 2. SendGrid

**SDK**: `SendGrid` v9.29.3

**Funcionalidades:**
- ✅ Envío de emails transaccionales
- ✅ Plantillas HTML responsivas
- ✅ Tracking de envíos

**Configuración:**
```bash
SENDGRID_API_KEY=SG.xxxxx
SENDGRID_FROM_EMAIL=noreply@tudominio.com
SENDGRID_FROM_NAME=Tu Plataforma
```

**Tipos de correos:**
1. Verificación de cuenta (con token)
2. Credenciales de BD
3. Recuperación de contraseña
4. Confirmación de pago
5. Eliminación de BD
6. Cambio de plan

### 3. Webhooks Personalizados

**Características:**
- ✅ HTTP/HTTPS
- ✅ Firma HMAC-SHA256
- ✅ Reintentos automáticos (4 intentos)
- ✅ Historial completo
- ✅ Logs detallados

**Eventos:**
- `account.created`
- `database.created`
- `database.deleted`
- `plan.changed`
- `payment.confirmed`

**Ejemplo de payload:**
```json
{
  "eventType": "database.created",
  "timestamp": "2025-11-20T10:30:00Z",
  "data": {
    "databaseId": "db-guid-123",
    "name": "user_db_postgres_123",
    "engine": "PostgreSQL",
    "userId": "user-guid-456"
  }
}
```

**Verificación de firma:**
```javascript
// Header: X-Webhook-Signature: t=1234567890,v1=hash
const crypto = require('crypto');

function verifyWebhook(payload, signature, secret) {
    const [timestamp, hash] = signature.split(',');
    const t = timestamp.split('=')[1];
    const v1 = hash.split('=')[1];
    
    const data = `${t}.${JSON.stringify(payload)}`;
    const expectedHash = crypto
        .createHmac('sha256', secret)
        .update(data)
        .digest('hex');
    
    return v1 === expectedHash;
}
```

---

## 🔒 Seguridad

### Implementaciones de Seguridad

#### 1. Autenticación JWT

- ✅ Token firmado con HMACSHA256
- ✅ Expiración configurable (24 horas)
- ✅ Claims personalizados (UserId, Email, PlanId)
- ✅ Validación en cada request protegido

**Estructura del token:**
```json
{
  "userId": "guid",
  "email": "user@example.com",
  "planId": "1",
  "exp": 1700500000,
  "iss": "CCD.Api",
  "aud": "CCD.Client"
}
```

#### 2. Hash de Contraseñas

- ✅ Algoritmo: HMACSHA512
- ✅ Salt único por usuario (128 bytes)
- ✅ Hash de 64 bytes
- ✅ Nunca se almacena la contraseña en texto plano

**Implementación:**
```csharp
using (var hmac = new HMACSHA512(passwordSalt))
{
    passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
}
```

#### 3. Validaciones

**Contraseñas:**
- Mínimo 8 caracteres
- Al menos 1 mayúscula
- Al menos 1 minúscula
- Al menos 1 número
- Al menos 1 símbolo especial

**Emails:**
- Formato válido con Regex
- Verificación obligatoria
- Token de verificación único

**Nombres de BD:**
- Solo letras, números y guiones bajos
- Prevención de SQL Injection
- Validación contra lista blanca de motores

#### 4. CORS

Configurado para dominios específicos:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder => builder
            .WithOrigins("https://andromeda.andrescortes.dev")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});
```

#### 5. Middleware de Excepciones

Captura global de errores:
- ✅ Oculta detalles técnicos al cliente
- ✅ Registra errores completos en logs
- ✅ Retorna mensajes amigables
- ✅ Incluye TraceId para debugging

#### 6. HTTPS

- ✅ Obligatorio en producción
- ✅ Redirección automática HTTP → HTTPS
- ✅ HSTS habilitado

---

## 📁 Estructura del Proyecto

```
CrudCloudDb-Backend/
├── src/
│   ├── CCD.Api/                      # Capa de Presentación
│   │   ├── Controllers/              # Controladores REST (8)
│   │   │   ├── AuthController.cs           # Autenticación
│   │   │   ├── UsersController.cs          # Gestión de usuarios
│   │   │   ├── DatabasesController.cs      # CRUD de bases de datos
│   │   │   ├── PaymentsController.cs       # Pagos y planes
│   │   │   ├── WebhooksController.cs       # Webhooks personalizados
│   │   │   ├── WebhookController.cs        # Webhook Mercado Pago
│   │   │   ├── AuditController.cs          # Auditoría
│   │   │   └── ErrorReportController.cs    # Error reporting
│   │   ├── Dtos/                     # Data Transfer Objects (13)
│   │   │   ├── UserRegisterDto.cs
│   │   │   ├── UserLoginDto.cs
│   │   │   ├── DatabaseCreateDto.cs
│   │   │   ├── DatabaseResponseDto.cs
│   │   │   ├── CreatePreferenceRequestDto.cs
│   │   │   └── ...
│   │   ├── Middleware/               # Middleware personalizado
│   │   │   └── GlobalExceptionMiddleware.cs
│   │   ├── Program.cs                # Punto de entrada
│   │   ├── appsettings.json          # Configuración
│   │   ├── Dockerfile                # Imagen Docker
│   │   └── CCD.Api.csproj            # Archivo de proyecto
│   │
│   ├── CCD.Core/                     # Capa de Dominio
│   │   ├── Entities/                 # Entidades del dominio (7)
│   │   │   ├── User.cs                     # Usuario
│   │   │   ├── Plan.cs                     # Plan (gratuito/intermedio/avanzado)
│   │   │   ├── DatabaseInstance.cs         # Instancia de BD
│   │   │   ├── DatabaseConnectionDetails.cs
│   │   │   ├── Webhook.cs                  # Webhook personalizado
│   │   │   ├── WebhookEvent.cs             # Evento de webhook
│   │   │   └── AuditLog.cs                 # Log de auditoría
│   │   ├── Interfaces/               # Contratos de servicios (6)
│   │   │   ├── IAuthRepository.cs
│   │   │   ├── IDatabaseProvisioner.cs
│   │   │   ├── IEmailService.cs
│   │   │   ├── IPaymentService.cs
│   │   │   ├── IWebhookService.cs
│   │   │   └── IAuditService.cs
│   │   ├── Dtos/                     # DTOs compartidos
│   │   └── CCD.Core.csproj
│   │
│   └── CCD.Infrastructure/           # Capa de Infraestructura
│       ├── Data/                     # Entity Framework
│       │   └── ApplicationDbContext.cs     # DbContext principal
│       ├── Migrations/               # Migraciones EF Core (15+)
│       │   ├── 20251118000000_InitialCreate.cs
│       │   ├── 20251119000000_AddAuditLogs.cs
│       │   └── ...
│       ├── Services/                 # Implementaciones de servicios (6)
│       │   ├── AuthRepository.cs           # Autenticación
│       │   ├── DatabaseProvisioner.cs      # Provisión de BD
│       │   ├── SendGridEmailService.cs     # Emails
│       │   ├── PaymentService.cs           # Mercado Pago
│       │   ├── WebhookService.cs           # Webhooks
│       │   └── AuditService.cs             # Auditoría
│       └── CCD.Infrastructure.csproj
│
├── diagrams/                         # Diagramas del proyecto
│   ├── DiagramaCasosdeuso.png              # Casos de uso
│   ├── DiagramaClases.png                  # Diagrama de clases
│   └── Diagramadelfujo.png                 # Diagrama de flujo
│
├── .env.example                      # Plantilla de variables de entorno
├── .env                              # Variables de entorno (no en git)
├── .gitignore                        # Archivos ignorados por git
├── docker-compose.yml                # Orquestación Docker
├── CCD.sln                           # Solución Visual Studio
└── README.md                         # Este archivo
```

### Descripción de Capas

#### **CCD.Api**
Punto de entrada de la aplicación. Contiene:
- **Controllers**: Endpoints HTTP organizados por dominio
- **DTOs**: Objetos de transferencia para requests/responses
- **Middleware**: Lógica transversal (excepciones, logging)
- **Program.cs**: Configuración de servicios, JWT, CORS, Swagger

#### **CCD.Core**
Lógica de negocio pura. No depende de infraestructura:
- **Entities**: Modelos del dominio con propiedades y validaciones
- **Interfaces**: Contratos que Infrastructure debe implementar
- **DTOs**: Objetos compartidos entre capas

#### **CCD.Infrastructure**
Implementación de persistencia y servicios externos:
- **Data**: DbContext y configuración de Entity Framework
- **Migrations**: Historial de cambios en la BD
- **Services**: Implementación de interfaces de Core

---

## 📊 Base de Datos - Esquema

### Tablas Principales

#### **Users**
```sql
CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "UserName" VARCHAR(50) UNIQUE NOT NULL,
    "Email" VARCHAR(255) UNIQUE NOT NULL,
    "PasswordHash" BYTEA NOT NULL,
    "PasswordSalt" BYTEA NOT NULL,
    "EmailVerified" BOOLEAN DEFAULT FALSE,
    "EmailVerificationToken" VARCHAR(500),
    "EmailVerificationTokenExpiry" TIMESTAMP,
    "PasswordResetToken" VARCHAR(500),
    "PasswordResetTokenExpiry" TIMESTAMP,
    "PlanId" INTEGER NOT NULL,
    FOREIGN KEY ("PlanId") REFERENCES "Plans"("Id")
);
```

#### **Plans**
```sql
CREATE TABLE "Plans" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL,
    "MaxDatabasesPerEngine" INTEGER NOT NULL,
    "Price" DECIMAL(10, 2) NOT NULL
);

-- Datos iniciales
INSERT INTO "Plans" VALUES
(1, 'Gratuito', 2, 0.00),
(2, 'Intermedio', 5, 5000.00),
(3, 'Avanzado', 10, 10000.00);
```

#### **DatabaseInstances**
```sql
CREATE TABLE "DatabaseInstances" (
    "Id" UUID PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "Engine" VARCHAR(50) NOT NULL,
    "UserId" UUID NOT NULL,
    "Status" VARCHAR(50) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL,
    "UpdatedAt" TIMESTAMP,
    "CredentialsViewedAt" TIMESTAMP,
    "TimeZoneId" VARCHAR(100) NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);
```

#### **DatabaseConnectionDetails**
```sql
CREATE TABLE "DatabaseConnectionDetails" (
    "Id" UUID PRIMARY KEY,
    "DatabaseInstanceId" UUID NOT NULL UNIQUE,
    "Host" VARCHAR(255) NOT NULL,
    "Port" INTEGER NOT NULL,
    "DatabaseName" VARCHAR(255) NOT NULL,
    "Username" VARCHAR(255) NOT NULL,
    "Password" TEXT NOT NULL,
    "ConnectionString" TEXT NOT NULL,
    FOREIGN KEY ("DatabaseInstanceId") REFERENCES "DatabaseInstances"("Id") ON DELETE CASCADE
);
```

#### **Webhooks**
```sql
CREATE TABLE "Webhooks" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Url" TEXT NOT NULL,
    "Secret" VARCHAR(500) NOT NULL,
    "Events" TEXT NOT NULL, -- JSON array
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);
```

#### **WebhookEvents**
```sql
CREATE TABLE "WebhookEvents" (
    "Id" UUID PRIMARY KEY,
    "WebhookId" UUID NOT NULL,
    "EventType" VARCHAR(100) NOT NULL,
    "Payload" TEXT NOT NULL, -- JSON
    "Status" VARCHAR(50) NOT NULL,
    "StatusCode" INTEGER,
    "Attempts" INTEGER DEFAULT 0,
    "LastAttemptAt" TIMESTAMP,
    "Error" TEXT,
    "CreatedAt" TIMESTAMP NOT NULL,
    FOREIGN KEY ("WebhookId") REFERENCES "Webhooks"("Id") ON DELETE CASCADE
);
```

#### **AuditLogs**
```sql
CREATE TABLE "AuditLogs" (
    "Id" UUID PRIMARY KEY,
    "UserId" UUID NOT NULL,
    "Action" VARCHAR(100) NOT NULL,
    "Details" TEXT,
    "IpAddress" VARCHAR(50),
    "CreatedAt" TIMESTAMP NOT NULL,
    FOREIGN KEY ("UserId") REFERENCES "Users"("Id") ON DELETE CASCADE
);
```

### Relaciones

```
Users (1) ─── (N) DatabaseInstances
Users (1) ─── (N) Webhooks
Users (1) ─── (N) AuditLogs
Plans (1) ─── (N) Users

DatabaseInstances (1) ─── (1) DatabaseConnectionDetails

Webhooks (1) ─── (N) WebhookEvents
```

---

## 🧪 Testing

### Probar con Swagger

1. Abrir `https://service.andromeda.andrescortes.dev/swagger`
2. Registrar usuario: `POST /api/auth/register`
3. Verificar email con token recibido: `POST /api/auth/verify-email`
4. Iniciar sesión: `POST /api/auth/login` (copiar token JWT)
5. Click en **Authorize** (arriba derecha)
6. Pegar: `Bearer {tu_token}`
7. Probar endpoints protegidos

### Probar con cURL

**Registrar usuario:**
```bash
curl -X POST "https://service.andromeda.andrescortes.dev/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test",
    "lastName": "User",
    "userName": "testuser",
    "email": "test@example.com",
    "password": "SecurePass123!"
  }'
```

**Login:**
```bash
curl -X POST "https://service.andromeda.andrescortes.dev/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "SecurePass123!"
  }'
```

**Crear BD (con token):**
```bash
curl -X POST "https://service.andromeda.andrescortes.dev/api/databases" \
  -H "Authorization: Bearer {tu_token}" \
  -H "Content-Type: application/json" \
  -d '{
    "engine": "PostgreSQL",
    "timeZoneId": "America/Bogota"
  }'
```

---

## 📈 Monitoreo y Logs

### Logs en Producción (Docker)

```bash
# Ver logs en tiempo real
docker logs -f ccd_api

# Últimos 100 logs
docker logs ccd_api --tail 100

# Logs con timestamps
docker logs -t ccd_api

# Filtrar por palabra clave
docker logs ccd_api 2>&1 | grep "ERROR"
```

### Logs en Desarrollo

Los logs se muestran automáticamente en la consola al ejecutar `dotnet run`.

**Niveles de log:**
- `Information` - Operaciones normales
- `Warning` - Situaciones inusuales pero manejables
- `Error` - Errores capturados que no detienen la app
- `Critical` - Errores graves que requieren atención inmediata

---

## 🚨 Troubleshooting

### Problema: SQL Server no acepta conexiones

**Síntomas:**
```
Error: "Connection refused: getsockopt"
```

**Solución:**
```bash
# 1. Verificar que el contenedor está corriendo
docker ps | grep sqlserver

# 2. Si no está corriendo, limitar memoria e iniciar
docker update --memory 768m --memory-swap 768m sqlserver2022
docker start sqlserver2022

# 3. Verificar logs
docker logs sqlserver2022 --tail 50

# 4. Verificar puerto
netstat -tuln | grep 1433
```

### Problema: Alto uso de memoria

**Síntomas:**
- Servidor lento
- `free -h` muestra >90% memoria usada

**Solución:**
```bash
# 1. Verificar memoria
free -h

# 2. Limpiar caché
sudo sync && sudo sysctl -w vm.drop_caches=3

# 3. Verificar SWAP
swapon --show

# 4. Si no hay SWAP, crear uno
sudo fallocate -l 2G /swapfile
sudo chmod 600 /swapfile
sudo mkswap /swapfile
sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab

# 5. Limitar contenedores
docker update --memory 512m --memory-swap 512m mysql_server && docker restart mysql_server
docker update --memory 256m --memory-swap 256m mongodb_server && docker restart mongodb_server
docker update --memory 512m --memory-swap 512m postgres_server && docker restart postgres_server
docker update --memory 768m --memory-swap 768m sqlserver2022 && docker restart sqlserver2022
```

### Problema: Error al crear migraciones

**Síntomas:**
```
Unable to create an object of type 'ApplicationDbContext'
```

**Solución:**
```bash
# 1. Instalar herramienta EF Core
dotnet tool install --global dotnet-ef

# 2. Restaurar herramientas
dotnet tool restore

# 3. Crear migración
dotnet ef migrations add NombreMigracion --project src/CCD.Infrastructure --startup-project src/CCD.Api

# 4. Aplicar migración
dotnet ef database update --project src/CCD.Infrastructure --startup-project src/CCD.Api
```

### Problema: Webhooks no se envían

**Verificar:**
1. URL del webhook es accesible públicamente
2. Webhook está activo (`isActive: true`)
3. Eventos configurados correctamente
4. Ver logs: `docker logs -f ccd_api | grep "WebhookService"`

---

## 📚 Documentación Adicional

- **Swagger UI**: `https://service.andromeda.andrescortes.dev/swagger`
- **Diagramas**: Carpeta `/diagrams`
  - Casos de uso
  - Diagrama de clases
  - Diagrama de flujo

---

## 👥 Equipo

- **Backend Developer**:Andromeda
- **Versión**: 1.0.0

---

## 📝 Licencia

Este proyecto es parte de un trabajo académico de la plataforma CrudCloudDb.

---

## 🎯 Estado del Proyecto

| Aspecto | Estado |
|---------|--------|
| Backend API | ✅ 100% Completo |
| Endpoints | ✅ 31/31 Implementados |
| Seguridad | ✅ JWT, Hash, CORS, HTTPS |
| Integraciones | ✅ Mercado Pago, SendGrid |
| Webhooks | ✅ 8 Endpoints (sin UI frontend) |
| Bases de Datos | ✅ PostgreSQL, MySQL, MongoDB, SQL Server |
| Auditoría | ✅ Completa |
| Testing | ✅ Swagger disponible |
| Producción | ✅ Desplegado |
| Documentación | ✅ Completa |

**Notas:**
- ⚠️ Redis y Cassandra NO implementados
- ⚠️ Webhooks personalizados sin UI en frontend (solo API)
- ✅ Pagos únicos, NO suscripciones recurrentes

---

**¿Preguntas o problemas?** Consulta los logs o contacta al equipo de desarrollo.

