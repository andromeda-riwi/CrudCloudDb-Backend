# -----------------------------
# Etapa 1: Build (Compilación del SDK)
# Usa la imagen del SDK de .NET 8 para compilar el proyecto
# -----------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar todos los archivos .csproj y restaurar las dependencias primero
# Esto aprovecha el caché de Docker para acelerar futuras compilaciones
COPY ["src/CCD.Api/CCD.Api.csproj", "src/CCD.Api/"]
COPY ["src/CCD.Core/CCD.Core.csproj", "src/CCD.Core/"]
COPY ["src/CCD.Infrastructure/CCD.Infrastructure.csproj", "src/CCD.Infrastructure/"]
RUN dotnet restore "src/CCD.Api/CCD.Api.csproj"

# Copiar todo el código fuente del proyecto
COPY . .
WORKDIR "/src/src/CCD.Api"
RUN dotnet build "CCD.Api.csproj" -c Release -o /app/build

# -----------------------------
# Etapa 2: Publish (Publicación)
# -----------------------------
FROM build AS publish
RUN dotnet publish "CCD.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# -----------------------------
# Etapa 3: Runtime (Ejecución final)
# Usa la imagen más ligera de ASP.NET, que no incluye el SDK de compilación
# -----------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# Copiar los archivos publicados desde la etapa de 'publish'
COPY --from=publish /app/publish .

# El comando que se ejecutará cuando el contenedor inicie
ENTRYPOINT ["dotnet", "CCD.Api.dll"]