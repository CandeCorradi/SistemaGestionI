# Imagen de producción de la API (SistemaGestion.API).
# Render construye esta imagen con el contexto en la raíz del repositorio:
#   dockerfilePath: ./src/SistemaGestion.API/Dockerfile
#   dockerContext:  .
#
# La configuración sensible NO se copia en la imagen: se inyecta como
# variables de entorno en Render (AGENTS.md §46).

########################################
# Etapa 1: compilación y publicación
########################################
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Respetar la versión de SDK declarada por el repositorio.
COPY global.json ./

# Copiar solo los .csproj para que el restore se cachee mientras no cambien
# las dependencias.
COPY src/SistemaGestion.Shared/SistemaGestion.Shared.csproj src/SistemaGestion.Shared/
COPY src/SistemaGestion.Domain/SistemaGestion.Domain.csproj src/SistemaGestion.Domain/
COPY src/SistemaGestion.Application/SistemaGestion.Application.csproj src/SistemaGestion.Application/
COPY src/SistemaGestion.Infrastructure/SistemaGestion.Infrastructure.csproj src/SistemaGestion.Infrastructure/
COPY src/SistemaGestion.API/SistemaGestion.API.csproj src/SistemaGestion.API/
RUN dotnet restore src/SistemaGestion.API/SistemaGestion.API.csproj

COPY src/ src/
RUN dotnet publish src/SistemaGestion.API/SistemaGestion.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

########################################
# Etapa 2: runtime
########################################
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# QuestPDF/SkiaSharp necesita fontconfig y al menos una fuente para generar
# PDFs en Linux; la imagen base no las incluye.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

# Render inyecta PORT en tiempo de ejecución; se expande en el entrypoint
# (un ENV de Dockerfile se resolvería en build y quedaría fijo).
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["/bin/sh", "-c", "exec dotnet SistemaGestion.API.dll --urls http://+:${PORT:-8080}"]
