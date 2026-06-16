# syntax=docker/dockerfile:1

# ---- Stage 1: frontend (Vite + Tailwind v4 + daisyUI 5) ----------------------------
# Runs FIRST so the compiled assets exist before dotnet publish. Tailwind's @source
# scans the .cshtml files for class names, so the whole web project is copied in.
FROM node:22-alpine AS frontend
WORKDIR /src
COPY MaceHub.Web/package.json MaceHub.Web/package-lock.json ./
RUN npm ci
COPY MaceHub.Web/ ./
RUN npm run build
# → /src/wwwroot/dist/{main.js,main.css}

# ---- Stage 2: .NET build / publish -------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restore against the pinned SDK (global.json) using just the project metadata first
# so the layer caches across source-only changes.
COPY global.json ./
COPY .config/ .config/
COPY MaceHub.Web/MaceHub.Web.csproj MaceHub.Web/
RUN dotnet restore MaceHub.Web/MaceHub.Web.csproj

COPY . .
# Bring in the compiled frontend assets BEFORE publish so they ship in the image.
COPY --from=frontend /src/wwwroot/dist MaceHub.Web/wwwroot/dist
RUN dotnet publish MaceHub.Web/MaceHub.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Stage 3: runtime (no Node, no SDK) --------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# Npgsql probes for Kerberos/GSSAPI during the auth handshake; the trimmed runtime
# image omits this library, which logs a (non-fatal) load error. Install it to keep
# the startup logs clean.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish ./
# ASP.NET Core listens on 8080 by default in the container images.
EXPOSE 8080
ENTRYPOINT ["dotnet", "MaceHub.Web.dll"]
