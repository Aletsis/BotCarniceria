# 🥩 Bot de WhatsApp para Carnicería

Sistema profesional de Bot de WhatsApp para gestión de pedidos de carnicería, construido con **Clean Architecture**, **DDD** y **SOLID Principles**. Desarrollado con ASP.NET Core 8.0, Blazor Server y MudBlazor.

## 📋 Características Principales

### 🤖 Bot de WhatsApp Inteligente
- **Máquina de Estados Finita**: Flujo controlado de conversación (START → MENU → TAKING_ORDER...).
- **Patrón Strategy**: Manejo extensible de tipos de mensajes (Texto, Interactivo, Ubicación, etc.).
- **Sesiones Persistentes**: Gestión de estado con timeout automático y caché (Redis/Memory).
- **Comandos Globales**: `cancelar`, `reiniciar`, `menu` disponibles en todo momento.
- **Soporte Multimedia**: Manejo nativo de imágenes, documentos, contactos y ubicaciones.
- **Resiliencia**: Manejo robusto de errores y reintentos automáticos.

### 📊 Dashboard Administrativo (Blazor)
- **Vista en Tiempo Real**: Monitorización de conversaciones activas.
- **Chat en Vivo**: Interfaz tipo WhatsApp Web para intervenir conversaciones.
- **Gestión de Pedidos**: Panel Kanban/Lista para seguimiento de estados.
- **Catálogos**: Administración de clientes y productos.
- **Configuración Dinámica**: Ajustes del sistema sin reinicios (Caché).

### 🏗️ Arquitectura Técnica
- **Clean Architecture**: Separación estricta en capas (API, Application, Core, Infrastructure).
- **Patrones de Diseño**:
  - **Repository & Unit of Work**: Abstracción de datos y transaccionalidad.
  - **Specification**: Lógica de consultas reutilizable y combinable.
  - **Strategy & Factory**: Manejo polimórfico de mensajes y estados.
  - **Caching**: Capa de caché para alto rendimiento (Config y Sesiones).
- **Testing**: 100% de cobertura en lógica de negocio (Unit Tests).

## 🛠️ Tecnologías

- **Core**: .NET 8.0 (C# 12)
- **Web**: ASP.NET Core Web API + Blazor Server
- **UI**: MudBlazor 7.0
- **Datos**: 
  - Entity Framework Core 8
  - SQL Server
  - MemoryCache / Redis (Abstracción)
- **Integración**: WhatsApp Business API (Meta)
- **Herramientas**: Serilog, Mapster/AutoMapper, FluentValidation

## 📚 Documentación Técnica

Para profundizar en la implementación técnica, consulta los siguientes documentos en la carpeta `Docs/`:

- [📐 ARQUITECTURA.md](Docs/ARQUITECTURA.md) - Arquitectura Clean, capas y patrones implementados.
- [📩 MANEJO_MENSAJES.md](Docs/MANEJO_MENSAJES.md) - Explicación detallada del flujo de mensajes, estados y handlers.
- [🧩 COMPONENTES.md](Docs/COMPONENTES.md) - Catálogo de servicios, repositorios y especificaciones clave.

## 🚀 Guía de Inicio Rápido

### Prerrequisitos
- .NET 8.0 SDK
- SQL Server (LocalDB o instancia completa)
- Cuenta desarrollador de Meta (para WhatsApp API)

### Instalación

1. **Clonar el repositorio**
   ```bash
   git clone <repository-url>
   cd BotCarniceria
   ```

2. **Configurar `appsettings.json`**
   Asegúrate de configurar la cadena de conexión y las credenciales de WhatsApp en `src/BotCarniceria.Presentation.API/appsettings.json`.

3. **Aplicar Migraciones**
   ```bash
   dotnet ef database update --project src/BotCarniceria.Infrastructure --startup-project src/BotCarniceria.Presentation.API
   ```

4. **Iniciar la Solución**
   Puedes iniciar tanto la API como el Dashboard:
   ```bash
   # Terminal 1 - API
   dotnet run --project src/BotCarniceria.Presentation.API
   
   # Terminal 2 - Dashboard
   dotnet run --project src/BotCarniceria.Presentation.Blazor
   ```

## 🔄 Flujo de Desarrollo

El proyecto sigue una metodología estricta de **Clean Architecture**.

1. **Core**: Definir Entidades, Interfaces de Repositorio y Especificaciones.
2. **Application**: Implementar Servicios, Handlers y Casos de Uso.
3. **Infrastructure**: Implementar Repositorios, Servicios Externos (WhatsApp) y DB Context.
4. **Presentation**: Exponer vía API o UI (Blazor).

## 📄 Licencia

Este proyecto es privado y propiedad de Carnicería La Mejor.
Copyright © 2025. Todos los derechos reservados.
