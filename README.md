# CrudCloudDb Platform ("CCD")

## Descripción General

El presente proyecto tiene como objetivo el desarrollo de una plataforma web tipo Clever Cloud, enfocada en la gestión automatizada de bases de datos en la nube. A través de esta plataforma, los usuarios podrán crear, administrar, escalar y eliminar instancias de bases de datos pertenecientes a distintos motores (MySQL, PostgreSQL, MongoDB, SQL Server) de manera centralizada, intuitiva y segura.

La plataforma está compuesta por una interfaz web desarrollada en **Vue.js** y un backend basado en **ASP.NET Core Web API**, comunicados mediante servicios REST y protegidos con autenticación JWT (JSON Web Token).

## ⚙️ Modelo de Planes y Precios

Al registrarse, el usuario accede automáticamente al plan gratuito. Posteriormente, puede ampliar sus capacidades mediante un sistema de membresías administrado a través de Mercado Pago.

-   **Plan Gratuito**: Hasta 2 bases de datos por motor.
-   **Plan Intermedio**: Hasta 5 bases de datos por motor — 💰 $5.000 COP/mes.
-   **Plan Avanzado**: Hasta 10 bases de datos por motor — 💰 $10.000 COP/mes.

## 🧱 Tecnologías Utilizadas

| Componente | Tecnología |
| :--- | :--- |
| **Frontend** | Vue.js |
| **Backend** | ASP.NET Core 8 Web API |
| **Base de Datos (App)** | Entity Framework Core 8 con PostgreSQL |
| **Autenticación** | JWT (JSON Web Token) |
| **Pasarela de Pagos** | Mercado Pago |
| **Correos Electrónicos** | SendGrid |
| **Contenerización** | Docker |

## 🧩 Funcionalidades Principales

1.  **Registro y Autenticación**: Creación de cuenta, verificación por correo, inicio de sesión JWT y recuperación de contraseña.
2.  **Gestión de Planes**: Asignación automática de plan gratuito y actualización a planes superiores mediante Mercado Pago.
3.  **Administración de Bases de Datos**:
    *   Soporte para **MySQL, PostgreSQL, MongoDB, SQL Server**.
    *   Generación y envío automático de credenciales por correo.
    *   Visualización controlada y rotación de credenciales.
4.  **Facturación y Pagos**: Creación de suscripciones y cobros mensuales vía Mercado Pago.
5.  **Notificaciones por Correo**: Alertas para creación de cuenta, creación/eliminación de bases de datos y cambios de plan.
6.  **Webhooks**:
    *   **Notificaciones de usuario**: Informa sobre creación de cuentas o bases de datos.
    *   **Reporte de errores**: Envía automáticamente excepciones de producción al equipo de desarrollo.

## Arquitectura

El proyecto está dividido en tres capas principales:

*   **CCD.Api**: Contiene los controladores de la API, DTOs y la configuración del servicio. Es el punto de entrada de la aplicación.
*   **CCD.Core**: Contiene la lógica de negocio principal, entidades, interfaces y DTOs. Es el núcleo de la aplicación y no depende de ninguna otra capa.
*   **CCD.Infrastructure**: Contiene la implementación de las interfaces definidas en `CCD.Core`. Esto incluye la configuración de la base de datos con Entity Framework Core, la implementación de repositorios y la integración con servicios de terceros como MercadoPago y SendGrid.

## Empezando (Backend)

### Prerrequisitos

*   [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
*   [Docker](https://www.docker.com/products/docker-desktop) (para la base de datos)
*   Una cuenta de SendGrid para el envío de correos electrónicos.
*   Una cuenta de MercadoPago para el procesamiento de pagos.

### Instalación

1.  Clona el repositorio:
    ```bash
    git clone https://github.com/tu-usuario/CrudCloudDb-Backend.git
    ```
2.  Navega al directorio del proyecto:
    ```bash
    cd CrudCloudDb-Backend
    ```
3.  Crea un archivo `.env` a partir del ejemplo:
    ```bash
    copy .env.example .env
    ```
4.  Actualiza el archivo `.env` con tus propias credenciales para la base de datos, SendGrid y MercadoPago.
5.  Inicia la base de datos usando Docker Compose:
    ```bash
    docker-compose up -d
    ```
6.  Ejecuta la aplicación:
    ```bash
    dotnet run --project src/CCD.Api/CCD.Api.csproj
    ```

La API estará disponible en `http://localhost:5063`. Puedes encontrar la especificación de OpenAPI (Swagger) en `http://localhost:5063/swagger`.

## Endpoints de la API

A continuación se muestra un resumen de los endpoints de la API disponibles.

### Autenticación (`/api/auth`)

*   `POST /register`: Registrar un nuevo usuario.
*   `POST /login`: Iniciar sesión y obtener un token JWT.
*   `POST /verify-email`: Verificar el correo electrónico de un usuario.
*   `POST /forgot-password`: Solicitar un restablecimiento de contraseña.
*   `POST /reset-password`: Restablecer la contraseña de un usuario.

### Usuarios (`/api/users`)

*   `GET /me`: Obtener los detalles del usuario autenticado.

### Bases de Datos (`/api/databases`)

*   `POST /`: Crear una nueva instancia de base de datos.
*   `GET /`: Obtener una lista de todas las instancias de bases de datos para el usuario autenticado.
*   `GET /{id}`: Obtener los detalles de una instancia de base de datos específica.
*   `DELETE /{id}`: Eliminar una instancia de base de datos.

### Pagos (`/api/payments`)

*   `POST /create-preference`: Crear una preferencia de pago en MercadoPago.

### Webhooks (`/api/webhook`)

*   `POST /mercadopago`: Recibir notificaciones de webhook de MercadoPago.

## Variables de Entorno

Para ejecutar este proyecto, necesitarás añadir las siguientes variables de entorno a tu archivo `.env`:

`ASPNETCORE_ENVIRONMENT`: Entorno de la aplicación (ej. `Development`, `Production`).
`ASPNETCORE_URLS`: URLs en las que la aplicación escuchará.
`JWT_SECRET_TOKEN`: Clave secreta para firmar los tokens JWT.
`MERCADOPAGO_ACCESS_TOKEN`: Token de acceso de MercadoPago.
`MERCADOPAGO_WEBHOOK_SECRET`: Secreto del Webhook de MercadoPago.
`SENDGRID_API_KEY`: Clave de la API de SendGrid.
`SENDGRID_FROM_EMAIL`: Dirección de correo electrónico del remitente.
`SENDGRID_FROM_NAME`: Nombre del remitente.
`APP_DASHBOARD_URL`: URL del panel de control de la aplicación frontend.
`DEFAULT_CONNECTION`: Cadena de conexión a la base de datos principal de la aplicación.
`ADMIN_POSTGRES_CONNECTION`: Cadena de conexión para administrar bases de datos PostgreSQL.
`ADMIN_MYSQL_CONNECTION`: Cadena de conexión para administrar bases de datos MySQL.
`ADMIN_SQLSERVER_CONNECTION`: Cadena de conexión para administrar bases de datos SQL Server.
`ADMIN_MONGO_CONNECTION`: Cadena de conexión para administrar bases de datos MongoDB.

## Requisitos de Seguridad

-   **Aislamiento**: Cada base de datos debe tener usuarios y permisos independientes.
-   **Comunicaciones**: Toda la comunicación cliente-servidor se realiza mediante HTTPS.
-   **Cifrado**: Las contraseñas se cifran y nunca se almacenan en texto plano.
-   **Auditoría**: Se implementa un manejo de errores y logs para auditar eventos importantes.

## Despliegue

El proyecto se despliega en los siguientes subdominios:

-   **Backend**: `service.voyager.andrescortes.dev`
-   **Frontend**: `voyager.andrescortes.dev`

## 📦 Entregables del Proyecto

-   **Documento de arquitectura**: Diagramas, flujos y dependencias.
-   **Backend**: API funcional en ASP.NET Core.
-   **Frontend**: Interfaz funcional en Vue.js.
-   **Integraciones**: Mercado Pago, sistema de correos y webhooks operativos.
-   **Video demostrativo** y **Repositorio del proyecto**.
