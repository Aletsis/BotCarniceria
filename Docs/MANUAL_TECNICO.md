# 🛠️ Manual Técnico y de Despliegue - Bot de Carnicería

Guía técnica integral para desarrolladores, arquitectos de software, administradores de sistemas y DevOps sobre la arquitectura, configuración, despliegue y mantenimiento de la plataforma **BotCarniceria**.

---

## 📑 Tabla de Contenidos
1. [Stack Tecnológico y Prerrequisitos](#1-stack-tecnológico-y-prerrequisitos)
2. [Estructura del Proyecto y Capas](#2-estructura-del-proyecto-y-capas)
3. [Patrones Arquitectónicos y Flujo de Datos](#3-patrones-arquitectónicos-y-flujo-de-datos)
4. [Configuración del Sistema (`appsettings.json` y BD)](#4-configuración-del-sistema-appsettingsjson-y-bd)
5. [Base de Datos, Migraciones y Seeding](#5-base-de-datos-migraciones-y-seeding)
6. [Integración con WhatsApp Business Cloud API](#6-integración-con-whatsapp-business-cloud-api)
7. [Subsistema de Impresión Térmica (ESC/POS)](#7-subsistema-de-impresión-térmica-escpos)
8. [Tiempo Real y Notificaciones (SignalR + Hangfire)](#8-tiempo-real-y-notificaciones-signalr--hangfire)
9. [Estrategia de Testing](#9-estrategia-de-testing)
10. [Guía de Despliegue en Producción](#10-guía-de-despliegue-en-producción)
11. [Monitoreo, Diagnóstico y Troubleshooting](#11-monitoreo-diagnóstico-y-troubleshooting)

---

## 1. Stack Tecnológico y Prerrequisitos

* **Framework**: .NET 8.0 SDK (C# 12)
* **Backend Web**: ASP.NET Core Web API (Webhook & Endpoints REST)
* **Frontend Web**: Blazor Server (.NET 8) con MudBlazor 7.0 (Material Design)
* **ORM & Persistencia**: Entity Framework Core 8.0 con SQL Server
* **Caché**: Abstracción `ICacheService` (MemoryCache / Redis)
* **Procesamiento Asíncrono en Background**: Hangfire 1.8 (gestión de reintentos y colas)
* **Resiliencia HTTP**: Polly Circuit Breaker
* **Logging**: Serilog (Console Sink & Rolling File Sinks)
* **Tiempo Real**: ASP.NET Core SignalR (`ChatHub`)
* **Integraciones Externas**: Meta WhatsApp Cloud API (Graph API v18+), Impresoras térmicas ESC/POS (TCP/IP socket)

---

## 2. Estructura del Proyecto y Capas

La solución sigue **Clean Architecture** y **DDD**:

```
BotCarniceria/
├── src/
│   ├── BotCarniceria.Core/               # Dominio puro y Aplicación CQRS (sin dependencias externas)
│   │   ├── Domain/                      # Entidades, ValueObjects, Enums, Eventos, Domain Services
│   │   └── Application/                 # CQRS Commands, Queries, Handlers (MediatR), DTOs
│   ├── BotCarniceria.Application.Bot/   # Máquina de Estados del Bot de WhatsApp y Webhooks
│   │   ├── StateMachine/                # 11 Handlers de estados de conversación y Factory
│   │   ├── Services/                    # IncomingMessageHandler y coordinadores de flujo
│   │   └── EventHandlers/               # Manejadores de eventos de dominio (ej. PedidoCreatedEventHandler)
│   ├── BotCarniceria.Infrastructure/    # Implementación técnica y adaptadores externos
│   │   ├── Persistence/                 # DbContext EF Core, Repositorios, Migraciones, DbInitializer
│   │   ├── Services/                    # WhatsAppService, PrintingService, DateTimeProvider, CacheService
│   │   └── Hubs/                        # SignalR ChatHub
│   ├── BotCarniceria.Shared/            # Constantes compartidas, Helpers (TimeZoneHelper)
│   ├── BotCarniceria.Presentation.API/  # Web API: Webhooks de Meta WhatsApp, Auth, Controllers
│   └── BotCarniceria.Presentation.Blazor/# Dashboard Web Administrativo y Portal Público de Facturas
└── tests/                               # Suites de pruebas unitarias, integración, arquitectura y E2E
```

---

## 3. Patrones Arquitectónicos y Flujo de Datos

### CQRS (Command Query Responsibility Segregation)
* **Commands**: Operaciones de mutación (`CreatePedidoCommand`, `UpdateClienteCommand`, `CreateSolicitudFacturaCommand`, `PrintTicketCommand`). Modifican el estado mediante `IUnitOfWork`.
* **Queries**: Operaciones de lectura optimizadas (`GetPedidosQuery`, `GetDashboardStatsQuery`, `GetClienteByPhoneQuery`).
* **MediatR**: Desacopla la invocación de la ejecución, permitiendo inyectar comportamientos transversales (Logging, Validación).

### Máquina de Estados Finita (FSM)
* Cada estado de conversación del bot (`START`, `MENU`, `TAKING_ORDER`, `ADDING_MORE`, `CONFIRM_LATE_ORDER`, `ASK_ADDRESS`, `CONFIRM_ADDRESS`, `SELECT_PAYMENT`, `AWAITING_CONFIRM`, `BILLING_*`) implementa `IConversationStateHandler`.
* El `StateHandlerFactory` resuelve el handler necesario mediante inyección de dependencias.

### Eventos de Dominio (Domain Events)
* Al crearse un pedido, se dispara `PedidoCreatedEvent`.
* `PedidoCreatedEventHandler` captura el evento y encola automáticamente la orden de impresión térmica a través de `IPrintingService` y notifica a los clientes conectados vía SignalR.

---

## 4. Configuración del Sistema (`appsettings.json` y BD)

### Configuración en `appsettings.json` (API y Blazor)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=tcp:sql-server-ip,1433;Database=BotCarniceriaDb;User Id=sa;Password=TuPassword123!;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "WhatsAppCircuitBreaker": {
    "FailureThreshold": 5,
    "DurationOfBreakInSeconds": 30,
    "MaxRetries": 3,
    "TimeoutInSeconds": 10
  },
  "Hangfire": {
    "WorkerCount": 5,
    "EnableDashboard": true,
    "DashboardPath": "/hangfire"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.txt",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### Parámetros Dinámicos en Base de Datos (Tabla `Configuraciones`)
El sistema lee dinámicamente sus credenciales y parámetros de negocio desde la base de datos (con caché en memoria):

* `WhatsApp.PhoneNumberId`: ID del número de teléfono asignado en Meta App Developer.
* `WhatsApp.AccessToken`: Token de acceso del sistema (Bearer token permanente).
* `WhatsApp.VerifyToken`: Token secreto para validar el Webhook en el portal de Meta.
* `WhatsApp.AppSecret`: Secreto de la aplicación Meta para validar la firma `X-Hub-Signature-256`.
* `Printers.IpAddress` y `Printers.Port`: Conexión de red a la impresora térmica (por defecto puerto 9100).
* `System.TimeZoneId`: ID estándar de zona horaria (ej. `Central Standard Time (Mexico)`).
* `Orders.LateOrderWarningStartHour`: Hora límite para aviso de entregas demoradas (ej. `16:00`).

---

## 5. Base de Datos, Migraciones y Seeding

### Aplicar Migraciones EF Core
Desde la raíz del repositorio:

```bash
dotnet ef database update \
  --project src/BotCarniceria.Infrastructure \
  --startup-project src/BotCarniceria.Presentation.API
```

### Inicialización y Seeding Automático (`DbInitializer`)
Al arrancar la aplicación API o Blazor, `DbInitializer.InitializeAsync()` se ejecuta automáticamente:
1. Aplica migraciones pendientes.
2. Si no existen registros en la tabla `Configuraciones`, inserta los valores predeterminados.
3. Verifica la existencia del usuario administrador inicial (`admin` / `Admin123!`).

---

## 6. Integración con WhatsApp Business Cloud API

### Verificación del Webhook (GET `/api/webhook`)
Meta realiza una petición GET de verificación:
* Parámetros: `hub.mode=subscribe`, `hub.verify_token`, `hub.challenge`.
* La API valida que `hub.verify_token` coincida con `WhatsApp.VerifyToken` y responde con el `hub.challenge`.

### Procesamiento Seguro de Mensajes (POST `/api/webhook`)
1. **Validación de Firma**: Se verifica el encabezado `X-Hub-Signature-256` calculando el HMAC-SHA256 del cuerpo crudo con `WhatsApp.AppSecret`.
2. **Procesamiento Asíncrono**: `IncomingMessageHandler` procesa el mensaje sin bloquear la respuesta HTTP 200 a Meta (evitando reintentos de Meta).
3. **Manejo de Errores y Circuit Breaker**: Si la API de Meta responde con errores 5xx, Polly abre el circuito temporalmente para proteger el hilo de ejecución.

---

## 7. Subsistema de Impresión Térmica (ESC/POS)

* **Protocolo**: Raw TCP Socket a la IP y puerto configurados (generalmente puerto `9100`).
* **Implementación**: `BotCarniceria.Infrastructure.Services.PrintingService`.
* **Formato del Ticket**:
  * Encabezado con datos del negocio y fecha en hora local (`IDateTimeProvider.Now`).
  * Folio del pedido y datos del cliente (Nombre, Teléfono, Dirección).
  * Detalle de productos y observaciones de preparación.
  * Total a pagar y método de pago elegido.
  * Corte automático de papel (`ESC/POS command: GS V 66 0`).

---

## 8. Tiempo Real y Notificaciones (SignalR + Hangfire)

* **SignalR Hub (`ChatHub`)**:
  * Mapeado en `/chathub`.
  * Transmite en tiempo real eventos `ReceiveMessage`, `NewOrderNotification`, `SessionUpdated`.
  * La interfaz Blazor Server consume este Hub para actualizar la UI sin recargar la página.
* **Hangfire**:
  * Dashboard disponible en `/hangfire`.
  * Maneja colas de reintentos en envíos fallidos de WhatsApp e impresiones.

---

## 9. Estrategia de Testing

El proyecto cuenta con suites de pruebas completas en la carpeta `tests/`:

```bash
# Ejecutar todas las pruebas unitarias y de integración
dotnet test

# Generar reporte de cobertura de código
./generate-coverage.ps1
```

* **Unit Tests**: Pruebas sobre la máquina de estados, comandos CQRS y lógica de dominio.
* **Architecture Tests**: Validación con NetArchTest para asegurar que las dependencias entre capas no se rompan.
* **Integration & Manual Tests**: Ubicados en `tests/ManualTests/` con script PowerShell `Simulate-Webhook.ps1` y `payload.json` para pruebas E2E locales.

---

## 10. Guía de Despliegue en Producción

### Pipeline Automatizado de CI/CD (GitHub Actions)

La plataforma cuenta con integración y entrega continua configurada mediante GitHub Actions:

1. **Integración Continua (`.github/workflows/ci.yml`)**:
   - Se dispara en cada `push` o `pull_request` a `main`.
   - Restaura dependencias y compila la solución en modo `Release`.
   - Ejecuta las pruebas automatizadas (Unit, Architecture, Integration, API, Blazor) con recolección de métricas de cobertura Cobertura.
   - Genera y adjunta el reporte de cobertura en el resumen de la ejecución.

2. **Entrega Continua / Publicación de Artefactos (`.github/workflows/cd.yml`)**:
   - Se ejecuta en cada `push` a `main` o al crear un tag de versión (`v*.*.*`).
   - Compila y publica `BotCarniceria.Presentation.API` y `BotCarniceria.Presentation.Blazor`.
   - Genera archivos comprimidos `.zip` (`botcarniceria-api.zip` y `botcarniceria-blazor.zip`) y los sube como artefactos de workflow (disponibles por 30 días).
   - Si se etiqueta un commit con un tag (ej: `git tag v1.0.0 && git push origin v1.0.0`), crea automáticamente una **GitHub Release** formal adjuntando los paquetes `.zip`.

3. **Pruebas End-to-End (`.github/workflows/e2e.yml`)**:
   - Se ejecuta bajo demanda (`workflow_dispatch`) con un contenedor Docker de SQL Server 2022 y navegadores Playwright.

---

### Opción A: Despliegue en Linux (Ubuntu/Debian) con Nginx y Systemd

Los artefactos `.zip` generados por el CD pueden descomprimirse directamente en el servidor:
```bash
# Ejemplo: Descomprimir artefactos descargados del release
unzip -o botcarniceria-api.zip -d /var/www/botcarniceria-api
unzip -o botcarniceria-blazor.zip -d /var/www/botcarniceria-blazor
systemctl restart botcarniceria-api
systemctl restart botcarniceria-blazor
```

O compilar manualmente en el servidor:

1. **Publicar las aplicaciones**:
   ```bash
   dotnet publish src/BotCarniceria.Presentation.API -c Release -o /var/www/botcarniceria-api
   dotnet publish src/BotCarniceria.Presentation.Blazor -c Release -o /var/www/botcarniceria-blazor
   ```

2. **Crear servicio Systemd para la API** (`/etc/systemd/system/botcarniceria-api.service`):
   ```ini
   [Unit]
   Description=BotCarniceria API Service
   After=network.target

   [Service]
   WorkingDirectory=/var/www/botcarniceria-api
   ExecStart=/usr/bin/dotnet /var/www/botcarniceria-api/BotCarniceria.Presentation.API.dll
   Restart=always
   RestartSec=10
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

   [Install]
   WantedBy=multi-user.target
   ```

3. **Configuración de Proxy Inverso en Nginx con SSL (Certbot)**:
   * Habilitar `proxy_set_header Upgrade $http_upgrade;` y `proxy_set_header Connection "upgrade";` para soportar WebSockets de Blazor y SignalR.

---

## 11. Monitoreo, Diagnóstico y Troubleshooting

### Logs de la Aplicación
Los logs se almacenan con rotación diaria en la carpeta `logs/`:
* API: `logs/api-YYYYMMDD.txt`
* Blazor: `logs/blazor-YYYYMMDD.txt`

### Diagnóstico de Problemas Frecuentes:
1. **Error de Validación de Webhook en Meta**:
   * Verifique que el URL sea accesible públicamente por HTTPS (certificado válido).
   * Verifique que `WhatsApp.VerifyToken` en la base de datos coincida exactamente con el token configurado en el portal de Meta.
2. **WebSockets desconectados en Blazor**:
   * Asegúrese de que el proxy inverso (Nginx / Cloudflare / IIS) tenga habilitado el soporte para WebSockets.
3. **Timeouts en Base de Datos**:
   * Revise las conexiones activas en SQL Server y la cadena de conexión `DefaultConnection`.
