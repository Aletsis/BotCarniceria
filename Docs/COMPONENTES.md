# 🧩 Catálogo de Componentes del Sistema

Catálogo completo y detallado de los componentes arquitectónicos, servicios, repositorios, comandos CQRS y manejadores de estado implementados en **BotCarniceria**.

---

## 🗄️ Repositorios y Persistencia (Data Access)

Ubicación: `BotCarniceria.Infrastructure/Persistence/Repositories`

| Interfaz | Implementación | Descripción |
| :--- | :--- | :--- |
| `IOrderRepository` | `OrderRepository` | Gestión de pedidos, generación de folios correlativos, consultas de pedidos activos y pendientes. |
| `IClienteRepository` | `ClienteRepository` | Gestión de clientes, búsqueda por teléfono, actualización de preferencias y datos de facturación. |
| `ISessionRepository` | `SessionRepository` | Gestión de sesiones de chat, timeouts automáticos y persistencia de estado. |
| `ISolicitudFacturaRepository` | `SolicitudFacturaRepository` | Almacenamiento y consulta de solicitudes de facturas electrónicas generadas por clientes. |
| `IUsuarioRepository` | `UsuarioRepository` | Gestión de usuarios del dashboard, roles de seguridad y contraseñas cifradas. |
| `IConfiguracionRepository` | `ConfiguracionRepository` | Parámetros dinámicos del sistema almacenados en base de datos. |
| `IUnitOfWork` | `UnitOfWork` | Coordinador transaccional (`CommitAsync`, `RollbackAsync`) para operaciones de escritura. |

---

## 🧠 Servicios de Dominio e Infraestructura

Ubicación: `BotCarniceria.Infrastructure/Services` y `BotCarniceria.Core/Domain/Services`

| Servicio / Interfaz | Implementación | Capa | Responsabilidad |
| :--- | :--- | :--- | :--- |
| `IWhatsAppService` | `WhatsAppService` | Infrastructure | Envío de mensajes de texto, plantillas interactivas, botones y listas a través de la API oficial de Meta. |
| `IPrintingService` | `PrintingService` | Infrastructure | Comunicación vía sockets TCP/IP con impresoras térmicas ESC/POS para impresión automática de comandas. |
| `IDateTimeProvider` | `DateTimeProvider` | Infrastructure / Core | Manejo de fechas y conversión entre UTC y la zona horaria del negocio (`Central Standard Time (Mexico)`). |
| `ICacheService` | `CacheService` | Infrastructure | Gestión de caché de alta velocidad (MemoryCache/Redis) para configuración y sesiones. |
| `ISessionService` | `SessionService` | Application | Capa de coordinación de sesiones con lógica de caché + persistencia en base de datos. |
| `IConfigurationService`| `ConfigurationService` | Application | Carga, refresco y tipado de configuraciones del sistema. |
| `IPasswordHasher` | `PasswordHasher` | Infrastructure | Hash y verificación de contraseñas de usuarios del sistema con salting seguro. |
| `ISignalRNotificationService` | `SignalRNotificationService` | Infrastructure | Difusión de eventos en tiempo real a clientes conectados al `ChatHub`. |

---

## ⚡ Arquitectura CQRS (Commands, Queries & Handlers)

Ubicación: `BotCarniceria.Core/Application/CQRS/`

### 1. Comandos (Commands & Handlers)
* **Pedidos**:
  * `CreatePedidoCommand` / `CreatePedidoCommandHandler`: Registra una nueva orden en el sistema y dispara el evento de dominio `PedidoCreatedEvent`.
  * `UpdateEstadoPedidoCommand` / `UpdateEstadoPedidoCommandHandler`: Transiciona el estado de un pedido (`EnEspera` → `EnRuta` → `Entregado` / `Cancelado`).
* **Solicitudes de Factura**:
  * `CreateSolicitudFacturaCommand` / `CreateSolicitudFacturaCommandHandler`: Registra la solicitud de factura pública y envía notificaciones por WhatsApp a los supervisores.
  * `UpdateEstadoSolicitudFacturaCommand` / `UpdateEstadoSolicitudFacturaCommandHandler`: Cambia el estado de la solicitud (`Pendiente`, `EnProceso`, `Completada`, `Rechazada`).
* **Clientes**:
  * `CreateClienteCommand` / `UpdateClienteCommand` / Handlers: Creación y edición de datos de clientes y datos fiscales.
* **Configuración**:
  * `UpdateConfiguracionCommand` / `UpdateConfiguracionCommandHandler`: Modificación de variables del sistema e invalidación de caché.
* **Usuarios**:
  * `CreateUsuarioCommand`, `UpdateUsuarioCommand`, `ChangePasswordCommand` / Handlers: Administración de cuentas de usuario y roles.
* **Impresión**:
  * `PrintTicketCommand` / `PrintTicketCommandHandler`: Envía a la cola la orden de impresión de tickets térmicos.
* **Sesión**:
  * `UpdateSessionStateCommand` / `SessionHandlers`: Actualiza el estado de la conversación activa del usuario.

### 2. Consultas (Queries & Handlers)
* **Dashboard**:
  * `GetDashboardStatsQuery` / `DashboardQueryHandler`: Estadísticas de pedidos del día, clientes activos y ventas.
* **Pedidos**:
  * `GetPedidosQuery`, `GetPedidoByIdQuery`, `GetPedidosActivosQuery` / `PedidoQueryHandlers`.
* **Clientes**:
  * `GetClientesQuery`, `GetClienteByPhoneQuery`, `GetClienteByIdQuery` / `ClienteHandlers`.
* **Solicitudes de Factura**:
  * `GetSolicitudesFacturaQuery`, `GetSolicitudFacturaByIdQuery` / `SolicitudFacturaHandlers`.
* **Usuarios**:
  * `GetUsuariosQuery`, `GetUsuarioByIdQuery` / `UsuarioHandlers`.
* **Configuraciones**:
  * `GetConfiguracionesQuery`, `GetConfiguracionByKeyQuery` / `ConfiguracionHandlers`.

---

## 🎮 Handlers de la Máquina de Estados (State Machine)

Ubicación: `BotCarniceria.Application.Bot/StateMachine/Handlers/`

Todos los handlers implementan la interfaz `IConversationStateHandler` y son administrados por `StateHandlerFactory`:

| Handler | Estado Asociado | Descripción / Responsabilidad |
| :--- | :--- | :--- |
| `StartStateHandler` | `START` | Saludo inicial de bienvenida y presentación de opciones al usuario. |
| `MenuStateHandler` | `MENU` | Router principal del menú interactivo (hacer pedido, consultar horario, facturación). |
| `AskNameStateHandler` | `ASK_NAME` | Solicita y captura el nombre del cliente para nuevos usuarios. |
| `AskAddressStateHandler` | `ASK_ADDRESS` | Solicita y procesa la dirección de entrega (texto libre o ubicación de WhatsApp). |
| `ConfirmAddressStateHandler` | `CONFIRM_ADDRESS` | Presenta la dirección capturada y solicita confirmación al cliente. |
| `TakingOrderStateHandler` | `TAKING_ORDER` | Captura la descripción inicial de productos y valida el horario de atención. |
| `ConfirmLateOrderStateHandler` | `CONFIRM_LATE_ORDER` | Alerta al cliente si el pedido es tardío (cerca de la hora de cierre) y solicita confirmación para continuar. |
| `AddingMoreStateHandler` | `ADDING_MORE` | Permite al cliente agregar más artículos a su pedido de manera acumulativa. |
| `SelectPaymentStateHandler` | `SELECT_PAYMENT` | Presenta los métodos de pago disponibles (Efectivo, Tarjeta, Transferencia). |
| `AwaitingConfirmStateHandler` | `AWAITING_CONFIRM` | Presenta el resumen completo del pedido para confirmación final por el cliente. |
| `BillingStateHandler` | `BILLING_*` | Flujo guiado de captura de datos fiscales (RFC, Razón Social, Dirección, Régimen, Folio de ticket y Uso CFDI). |

---

## 📢 Eventos de Dominio y Event Handlers

Ubicación: `BotCarniceria.Application.Bot/EventHandlers/`

* **`PedidoCreatedEvent`** / **`PedidoCreatedEventHandler`**:
  * Se dispara inmediatamente después de que un pedido es creado y persistido con éxito.
  * Ejecuta la orden de impresión del ticket térmico en segundo plano (`IPrintingService`).
  * Notifica a través de SignalR (`ChatHub`) al Dashboard administrativo para alertar al personal sobre la nueva comanda.

---

## 🔎 Especificaciones de Consulta (Specification Pattern)

Ubicación: `BotCarniceria.Core/Domain/Specifications/`

* **Pedidos**:
  * `PedidosActiveSpecification`: Pedidos en estado activo (no cancelados ni entregados).
  * `PedidosByClienteSpecification`: Historial de pedidos de un cliente específico.
  * `PedidosByDateRangeSpecification`: Filtro de pedidos por rango de fechas (usando UTC).
  * `PedidosByFolioSpecification`: Búsqueda exacta de pedido por folio.
  * `PedidosPendingSpecification`: Pedidos en estado `EnEspera`.
* **Clientes**:
  * `ClienteByPhoneNumberSpecification`: Búsqueda por número de teléfono normalizado.
  * `ClientesActiveSpecification`: Clientes activos.
  * `SupervisorsWithPhoneSpecification`: Supervisores con teléfono configurado para notificaciones automáticas.
