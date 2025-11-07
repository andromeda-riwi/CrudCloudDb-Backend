# CrudCloudDb (CCD) - Backend

Plataforma web para gestión automatizada de bases de datos en la nube, similar a Clever Cloud.

## 🧭 Descripción General

El presente proyecto desarrolla una plataforma web tipo Clever Cloud enfocada en la gestión automatizada de bases de datos en la nube. A través de esta solución, las personas usuarias pueden crear, administrar, escalar y eliminar instancias de múltiples motores de base de datos (PostgreSQL, MySQL, MongoDB, SQL Server, Redis y Cassandra) desde un panel centralizado, intuitivo y seguro.

La arquitectura se compone de un frontend en Vue.js y un backend en ASP.NET Core Web API comunicados mediante servicios REST y protegidos con autenticación JWT. Al registrarse, cualquier usuario obtiene el plan gratuito (hasta dos bases de datos por motor) y puede ampliar su capacidad mediante planes pagos gestionados con Mercado Pago. Cada plan define cuotas diferenciadas y el sistema ajusta permisos y recursos disponibles.

## 🚀 Descripción

Este backend permite a los usuarios:
- **Registro y autenticación JWT**: alta de cuentas, login y emisión de tokens.
- **Provisionamiento automatizado**: creación de bases de datos PostgreSQL (motores adicionales en hoja de ruta).
- **Gestión multi-instancia**: control de múltiples bases de datos por usuario.
- **Cuotas por plan**: límites dinámicos según plan gratuito, intermedio o avanzado.
- **Notificaciones por correo**: envío de credenciales mediante servicios como SendGrid.

## ⚙️ Funcionalidad del Sistema

- **Plan gratuito**: permite hasta 2 bases de datos por motor.
- **Plan intermedio**: hasta 5 bases de datos por motor — 💰 $5.000 COP/mes.
- **Plan avanzado**: hasta 10 bases de datos por motor — 💰 $10.000 COP/mes.
- **Aislamiento de credenciales**: cada instancia cuenta con usuario, contraseña, puerto y permisos independientes.

## 🧱 Tecnologías a Utilizar

| Componente | Tecnología |
|------------|------------|
| Frontend | Vue.js |
| Backend | ASP.NET Core Web API |
| Autenticación | JWT (JSON Web Token) |
| Pasarela de pagos | Mercado Pago |
| Correos electrónicos | SendGrid / SMTP configurable |
| Notificaciones externas | Webhooks |

## 🧩 Funcionalidades Principales

- **Registro y Autenticación**: creación de cuentas, verificación por correo, inicio de sesión JWT y recuperación de contraseña.
- **Gestión de Planes y Membresías**: asignación automática del plan gratuito, upgrades vía Mercado Pago y control de cuotas por motor.
- **Provisionamiento de Bases de Datos**: selección de motor, generación automática de credenciales, visualización controlada y rotación según necesidad.
- **Facturación y Pagos**: suscripciones mensuales, validación de cobros y actualización automática del plan.
- **Notificaciones por Correo**: alta de cuenta, creación/eliminación de instancias y cambios de plan.
- **Webhooks**: eventos para acciones de usuario y alertas de errores críticos.
- **Panel de Control**: dashboard con plan vigente, cuotas, listado de bases y herramientas de facturación y webhooks.

## 🔒 Requisitos de Seguridad

- **Aislamiento de acceso**: cada base de datos debe tener usuarios y permisos independientes.
- **Comunicación segura**: todo tráfico entre cliente y servidor viaja sobre HTTPS.
- **Protección de claves**: contraseñas cifradas, nunca en texto plano.
- **Auditoría y logging**: registro de acciones relevantes y trazabilidad de errores.

## 📣 Requisitos de Comunicación y Reportes

- **Auditoría**: almacenar eventos significativos del sistema.
- **Reportes de error**: enviar fallos en producción mediante webhooks.
- **Estado de notificaciones**: registrar si cada correo o webhook fue enviado correctamente.

## ✅ Resultados Esperados (Demo)

- **Cuenta activa**: alta y acceso a la plataforma.
- **Provisionamiento multi-motor**: creación de bases en al menos dos motores distintos.
- **Upgrade de plan**: cambio de plan con Mercado Pago (sandbox).
- **Notificaciones funcionales**: recepción de correos y webhooks en los flujos principales.
- **Panel operativo**: interfaz moderna y utilizable.

## 📦 Entregables

- **Documento de arquitectura**: diagramas, flujos y dependencias.
- **Backend ASP.NET Core**: API con autenticación JWT y endpoints operativos.
- **Frontend Vue.js**: aplicación con rutas, componentes y estilos.
- **Integración Mercado Pago**: cobros funcionales en producción o sandbox.
- **Sistema de correos y webhooks**: implementado y documentado.
- **Video demostrativo**: recorrido del flujo principal.
- **Repositorio completo**: documentación y README actualizados.

## 🛠️ Recomendaciones de Desarrollo

- **Control de versiones**: trabajar con Git/GitHub y flujos colaborativos.
- **Arquitectura en capas**: mantener separación de responsabilidades clara.
- **Entornos separados**: definir ambientes de desarrollo, pruebas y producción.
- **Validación y manejo de errores**: sanitizar entradas y capturar excepciones.
- **Logging y auditoría**: instrumentar telemetría desde el backend.

## 🎯 Competencias a Desarrollar

- **Diseño de APIs seguras** con JWT y buenas prácticas.
- **Integración de pagos** mediante Mercado Pago.
- **Automatización de recursos** y despliegues en servidores.
- **Interfaces reactivas** con Vue.js.
- **Notificaciones y observabilidad** (correos, webhooks, logs).
- **Trabajo colaborativo** y uso efectivo de Git.

## 📊 Criterios de Evaluación

| Criterio | Descripción | Peso |
|----------|-------------|------|
| Arquitectura del sistema | Diseño estructurado, separación de capas, buenas prácticas | 20% |
| Funcionalidad backend | Autenticación, gestión de bases de datos, webhooks | 25% |
| Interfaz frontend | Usabilidad, experiencia de usuario, diseño | 20% |
| Integraciones externas | Mercado Pago, correos y webhooks | 15% |
| Seguridad y errores | JWT, cifrado, control de excepciones | 10% |
| Documentación y demo | README, diagramas, video, repositorio | 10% |

## 📋 Requisitos Previos

- **.NET 8 SDK** o superior
- **PostgreSQL 14+** instalado y corriendo
- **Usuario superusuario de PostgreSQL** (generalmente `postgres`)
- Editor de código (Visual Studio, VS Code, Rider)

## ⚙️ Configuración Inicial

### 1. Clonar el repositorio

```bash
git clone <url-del-repo>
cd CrudCloudDb-Backend
```

### 2. Configurar las cadenas de conexión y servicios

Abre el archivo `src/CCD.Api/appsettings.json` y **reemplaza** la contraseña del usuario `postgres`. También define las claves para el servicio de correo y la URL del panel:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=49.12.100.202;Port=5432;Database=ccd_dev_db;User Id=root;Password=xbI4PLOvlMwRwHn7SdZXHivFOZwc98",
    "AdminPostgresConnection": "Host=49.12.100.202;Port=5432;Database=postgres;Username=postgres;Password=TU_PASSWORD_REAL_AQUI"
  },
  "SendGrid": {
    "ApiKey": "TU_API_KEY_AQUI",
    "FromEmail": "no-reply@tu-dominio.com",
    "FromName": "CCD Platform"
  },
  "App": {
    "DashboardUrl": "https://tu-dominio.com/dashboard"
  }
}
```

**Explicación de las conexiones:**

- **`DefaultConnection`**: Base de datos de gestión donde se guardan usuarios, planes y registros de bases de datos creadas.
- **`AdminPostgresConnection`**: Conexión de **superusuario** que permite crear nuevas bases de datos y usuarios. **Debe tener permisos de `CREATE DATABASE` y `CREATE ROLE`.**

### 3. Aplicar migraciones de Entity Framework

Desde la carpeta raíz del proyecto:

```bash
cd src/CCD.Api
dotnet ef database update
```

Esto creará las tablas `Users`, `Plans` y `DatabaseInstances` en la base de datos `ccd_dev_db`.

### 4. Ejecutar el proyecto

```bash
dotnet run
```

El backend estará disponible en:
- **Swagger UI**: `https://localhost:5001` o `http://localhost:5000`
- **API**: `https://localhost:5001/api`

> ℹ️ Puedes usar variables de entorno o secretos de usuario (`dotnet user-secrets`) para evitar almacenar valores sensibles directamente en el repositorio.

## 🔐 Endpoints Principales

### Autenticación

- **POST** `/api/auth/register` - Registrar nuevo usuario
- **POST** `/api/auth/login` - Iniciar sesión (devuelve JWT)

### Gestión de Bases de Datos (requiere JWT)

- **GET** `/api/databases` - Listar bases de datos del usuario
- **GET** `/api/databases/stats` - Estadísticas del dashboard
- **POST** `/api/databases` - Crear nueva base de datos
- **DELETE** `/api/databases/{id}` - Eliminar base de datos

## 📝 Ejemplo de Uso

### 1. Registrar un usuario

```bash
POST /api/auth/register
Content-Type: application/json

{
  "name": "Juan",
  "lastName": "Pérez",
  "userName": "juanp",
  "email": "juan@example.com",
  "password": "MiPassword123!"
}
```

### 2. Iniciar sesión

```bash
POST /api/auth/login
Content-Type: application/json

{
  "email": "juan@example.com",
  "password": "MiPassword123!"
}
```

Respuesta:
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### 3. Crear una base de datos PostgreSQL

```bash
POST /api/databases
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: application/json

{
  "engine": "PostgreSQL"
}
```

Respuesta (201 Created):
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "user_3fa85f64_a3b2",
  "engine": "PostgreSQL",
  "status": "Active"
}
```

## 🛠️ Estructura del Proyecto

```
CrudCloudDb-Backend/
├── src/
│   ├── CCD.Api/              # Capa de presentación (Controllers, DTOs)
│   ├── CCD.Core/             # Entidades y contratos (Interfaces)
│   └── CCD.Infrastructure/   # Implementaciones (Repositorios, Servicios)
```

## 🔧 Solución de Problemas

### Error: "AdminPostgresConnection no está configurada"

**Causa**: Falta la cadena de conexión o está vacía.

**Solución**: Verifica que `appsettings.json` tenga `AdminPostgresConnection` con la contraseña correcta del usuario `postgres`.

### Error: "permission denied to create database"

**Causa**: El usuario en `AdminPostgresConnection` no tiene permisos de superusuario.

**Solución**: Conéctate a PostgreSQL y ejecuta:

```sql
ALTER USER postgres WITH SUPERUSER;
```

### Error: "database already exists" o "role already exists"

**Causa**: Intento de crear una base de datos o usuario que ya existe.

**Solución**: El sistema genera nombres aleatorios, pero si persiste, verifica colisiones manualmente.

## ✉️ Notificaciones por Correo (SendGrid)

El servicio `SendGridEmailService` en `src/CCD.Infrastructure/Services/SendGridEmailService.cs` envía correos de bienvenida y credenciales utilizando la configuración definida en `appsettings.json`. Asegúrate de:

- **`SendGrid:ApiKey`**: API Key activa de tu cuenta SendGrid.
- **`SendGrid:FromEmail` / `SendGrid:FromName`**: Remitente visible en los correos.
- **`App:DashboardUrl`**: URL que aparece en los mensajes enviados a los usuarios.

Si no configuras la API Key, el servicio continúa operando pero registrará advertencias y omitirá el envío de correos hasta que proporciones la clave válida.

## 🐳 Despliegue con Docker Compose

Desde la raíz del proyecto (`CrudCloudDb-Backend/`):

```bash
docker compose build            # Construye la imagen de la API
docker compose up -d --build    # Levanta/redpliega los contenedores
docker compose ps               # Verifica el estado
```

Para aplicar cambios de configuración en producción:

```bash
docker compose down --remove-orphans
docker compose up -d --build --force-recreate
```

El contenedor principal expone la API en el puerto `8082` (`http://localhost:8082`). Ajusta los puertos o variables de entorno en `docker-compose.yml` según tus necesidades.

## 📦 Próximos Pasos (TODOs)

- [x] Integrar servicio de correo electrónico (SendGrid)
- [ ] Implementar webhooks para notificaciones
- [ ] Agregar soporte para MySQL, MongoDB, SQL Server
- [ ] Integrar Mercado Pago para planes pagos
- [ ] Implementar eliminación real de bases de datos en PostgreSQL

## 🌐 Despliegue

- **Backend**: `service.voyager.andrescortes.dev`
- **Frontend**: `voyager.andrescortes.dev`

## 👥 Equipo

Proyecto desarrollado como parte del bootcamp de desarrollo web.

## 📄 Licencia

Este proyecto es de uso educativo.
