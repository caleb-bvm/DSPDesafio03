# Un solo Dockerfile para los tres proyectos web.
# docker-compose.yml indica cuál compilar con el argumento PROYECTO
# (EquiposAPI, EquiposMVC o EquiposReportes).

# ---- 1. Compilar con el SDK de .NET 10 ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG PROYECTO
WORKDIR /src

# Primero solo el .csproj: así los paquetes NuGet quedan en caché
# y no se descargan otra vez si solo cambia el código.
COPY ${PROYECTO}/${PROYECTO}.csproj ${PROYECTO}/
RUN dotnet restore ${PROYECTO}/${PROYECTO}.csproj

COPY ${PROYECTO}/ ${PROYECTO}/
RUN dotnet publish ${PROYECTO}/${PROYECTO}.csproj -c Release -o /app --no-restore

# ---- 2. Imagen final: solo el runtime de ASP.NET ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
ARG PROYECTO
ENV PROYECTO=${PROYECTO}
WORKDIR /app
COPY --from=build /app .

# La imagen de ASP.NET escucha en el puerto 8080 por defecto.
EXPOSE 8080
ENTRYPOINT ["sh", "-c", "exec dotnet ${PROYECTO}.dll"]
