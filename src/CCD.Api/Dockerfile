# --- Etapa de Compilación (Build Stage) ---
# Usa la imagen oficial del SDK de .NET 8 como base. Esta imagen tiene todas
# las herramientas necesarias para compilar tu proyecto.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Establece el directorio de Trabajo dentro del contenedor.
WORKDIR /build

# Copia los archivos .csproj para restaurar las dependencias primero.
# Esto aprovecha el sistema de caché de Docker: si los proyectos no han cambiado,
# no volverá a descargar las dependencias, haciendo la compilación más rápida.
COPY ["src/CCD.Api/CCD.Api.csproj", "CCD.Api/"]
COPY ["src/CCD.Core/CCD.Core.csproj", "CCD.Core/"]
COPY ["src/CCD.Infrastructure/CCD.Infrastructure.csproj", "CCD.Infrastructure/"]

# Ejecuta el comando para restaurar los paquetes NuGet.
RUN dotnet restore "CCD.Api/CCD.Api.csproj"

# Copia solo la carpeta src con el código fuerte al contenedor.
# Esto evita copiar archivos innecesarios como .git, .idea, etc.
COPY src/ .

# Establece el directorio de Trabajo a la carpeta del proyecto de la API.
WORKDIR "/build/CCD.Api"

# Compila la aplicación en modo "Release" y la guarda en una carpeta /app/build.
RUN dotnet build "CCD.Api.csproj" -c Release -o /app/build


# --- Etapa de Publicación (Publish Stage) ---
# Esta etapa crea la versión final y optimizada de la aplicación.
FROM build AS publish
RUN dotnet publish "CCD.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false


# --- Etapa Final (Final Stage) ---
# Usa una imagen base mucho más ligera, solo con lo necesario para ejecutar la aplicación.
# Esto hace que la imagen final sea mucho más pequeña y segura.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Establece el directorio de trabajo final.
WORKDIR /app

# Copia los archivos publicados desde la etapa de 'publish' a la etapa final.
COPY --from=publish /app/publish .

# El comando que se ejecutará cuando el contenedor se inicie.
ENTRYPOINT ["dotnet", "CCD.Api.dll"]