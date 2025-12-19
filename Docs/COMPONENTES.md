# 🧩 Componentes del Sistema

Catálogo de los componentes principales creados en las capas de Infraestructura y Aplicación.

## 🗄️ Repositories (Acceso a Datos)

Ubicación: `BotCarniceria.Infrastructure/Repositories`

| Interfaz | Implementación | Descripción |
|----------|----------------|-------------|
| `IOrderRepository` | `OrderRepository` | Gestión de Pedidos, generación de Folios, consultas complejas. |
| `IClienteRepository` | `ClienteRepository` | Gestión de clientes, búsqueda por teléfono. |
| `ISessionRepository` | `SessionRepository` | Gestión de sesiones de chat, timeouts y expiración. |
| `IUnitOfWork` | `UnitOfWork` | Coordinador de transacciones para todas las operaciones de escritura. |

## 🧠 Services (Lógica de Aplicación)

Ubicación: `BotCarniceria.Application/Services` o `.Infrastructure/Services`

| Servicio | Descripción |
|----------|-------------|
| `SessionService` | Capa de alto nivel para gestión de sesiones con Caché + DB. |
| `WhatsAppService` | Cliente HTTP para comunicar con la API de Meta. |
| `PrintingService` | Servicio para enviar comandos ESC/POS a impresoras térmicas. |
| `CacheService` | Servicio de caché abstracto (Memory/Redis) para configuración y sesiones. |
| `ConfigurationService` | Gestión de configuración dinámica guardada en BD. |

## 🔎 Specifications (Consultas)

Ubicación: `BotCarniceria.Core/Specifications`

### Pedidos
- `PedidosActiveSpecification`: Pedidos no entregados/cancelados.
- `PedidosByClienteSpecification`: Historial de un cliente.
- `PedidosByDateRangeSpecification`: Reportes por fecha.
- `PedidosByFolioSpecification`: Búsqueda exacta.
- `PedidosPendingSpecification`: Nuevos pedidos para tablero.

### Clientes
- `ClienteByPhoneNumberSpecification`: Búsqueda principal.
- `ClientesActiveSpecification`: Clientes no bloqueados.
- `ClientesByNameSpecification`: Búsqueda por nombre (partial match).

## 🎮 Handlers (Flujo)

Ubicación: `BotCarniceria.Application.Bot/StateMachine/Handlers`

- `StartStateHandler`: Bienvenida.
- `MenuStateHandler`: Router principal.
- `TakingOrderStateHandler`: Lógica NLP básica para tomar nota.
- `AskAddressStateHandler`: Validación de direcciones.
- `SelectPaymentStateHandler`: Finalización de compra.

## 🔌 Commands (Global)

- `GlobalCommandHandler`: Intercepta comandos como "Cancelar" en cualquier punto del flujo.
