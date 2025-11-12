# --- Etapa de Compilación (Build Stage) ---
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia los archivos .csproj primero
COPY ["src/CCD.Api/CCD.Api.csproj", "src/CCD.Api/"]
COPY ["src/CCD.Core/CCD.Core.csproj", "src/CCD.Core/"]
COPY ["src/CCD.Infrastructure/CCD.Infrastructure.csproj", "src/CCD.Infrastructure/"]

# Restaura las dependencias
RUN dotnet restore "src/CCD.Api/CCD.Api.csproj"

# Copia todo el código fuente
COPY . .

# Establece el directorio de trabajo a la carpeta de la API para compilar
WORKDIR "/src/src/CCD.Api"
RUN dotnet build "CCD.Api.csproj" -c Release -o /app/build

# --- Etapa de Publicación (Publish Stage) ---
FROM build AS publish
RUN dotnet publish "CCD.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Etapa Final (Final Stage) ---
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CCD.Api.dll"]