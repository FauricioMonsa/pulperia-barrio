# Mi Pulpería de Barrio

Sistema de inventarios y reservas desarrollado con ASP.NET Core MVC, jQuery y PostgreSQL. Permite administrar sucursales, clientes y productos, además de consultar reservas e inventario por sucursal.

## Requisitos

- .NET 10 SDK
- PostgreSQL

## Crear la base de datos

Desde una terminal con las herramientas de PostgreSQL disponibles, crea la base de datos:

```bash
createdb -U postgres pulperia_barrio
```

Ejecuta el script del esquema desde la raíz del proyecto:

```bash
psql -U postgres -d pulperia_barrio -f database/01_pulperia_schema.sql
```

## Configurar la conexión

Guarda la cadena de conexión en User Secrets. Sustituye `TU_CONTRASENA` por la contraseña configurada en tu propio entorno; no la incluyas en archivos del proyecto:

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=pulperia_barrio;Username=postgres;Password=TU_CONTRASENA" --project PulperiaBarrio.csproj
```

## Ejecutar

Desde la raíz del proyecto, inicia la aplicación:

```bash
dotnet run
```
