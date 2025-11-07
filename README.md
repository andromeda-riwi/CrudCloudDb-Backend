# CrudCloudDb (CCD) - Backend

Plataforma web para gestión automatizada de bases de datos en la nube, similar a Clever Cloud.

## 🚀 Descripción

Este backend permite a los usuarios:
- Registrarse y autenticarse con JWT
- Crear bases de datos PostgreSQL automáticamente
- Gestionar múltiples instancias de bases de datos
- Controlar cuotas por plan (Gratuito, Intermedio, Avanzado)
- Recibir credenciales por correo electrónico

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

Si no configuras la API Key, el servicio lanzará una excepción en tiempo de ejecución y el backend no iniciará.

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
