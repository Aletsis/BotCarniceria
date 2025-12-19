# 📩 Manejo de Mensajes y Flujo del Bot

Este documento detalla cómo el sistema procesa los mensajes entrantes de WhatsApp y gestiona el flujo de conversación.

## 🔄 Flujo de Procesamiento

1. **Recepción (Webhook)**: 
   - Meta envía un POST a `/api/webhook`.
   - `WebhookController` valida la firma HMAC y pasa el payload al `WebhookProcessor`.
   
2. **Procesamiento Inicial**:
   - `WebhookProcessor` extrae la información relevante y normaliza el mensaje.
   - Se invoca a `IncomingMessageHandler`.

3. **Estrategia de Tipo de Mensaje**:
   - `IncomingMessageHandler` utiliza `IMessageTypeHandlerFactory` para obtener el handler adecuado para el tipo de mensaje (Texto, Botón, Lista, Imagen, etc.).
   - Estrategias implementadas:
     - `TextMessageTypeHandler`: Procesa texto libre.
     - `InteractiveMessageTypeHandler`: Procesa respuestas a botones y listas.
     - `UnsupportedMessageTypeHandler`: Maneja tipos no soportados (audio, video) enviando un mensaje amigable.

4. **Comandos Globales**:
   - Antes de procesar el estado, el sistema verifica si es un comando global (`cancelar`, `menu`, `reiniciar`).
   - Estos comandos tienen prioridad y pueden interrumpir cualquier flujo.

5. **Máquina de Estados (State Machine)**:
   - Si no es comando, se recupera la **Sesión** del usuario.
   - Se instancia el **StateHandler** correspondiente al estado actual de la sesión (ej. `TakingOrderStateHandler`).
   - El Handler procesa la entrada, ejecuta lógica de negocio, y determina el **Nuevo Estado**.

## 🚦 Estados de la Conversación

El bot sigue una máquina de estados finita:

| Estado | Descripción | Inputs Esperados |
|--------|-------------|------------------|
| **START** | Primer contacto o reinicio. | Cualquier mensaje inicia el saludo. |
| **MENU** | Menú principal. | Selección de botón (Pedido, Info, Estado). |
| **ASK_NAME** | Solicitud de nombre (nuevos usuarios). | Texto libre (Nombre). |
| **ASK_ADDRESS** | Solicitud de dirección. | Texto libre o Ubicación. |
| **TAKING_ORDER** | Toma de pedido. | Texto libre (descripción de productos). |
| **AWAITING_CONFIRM** | Confirmación final. | Botones Sí/No. |
| **SELECT_PAYMENT** | Selección de método de pago. | Lista/Botones de formas de pago. |

## 🛠️ Extensión del Bot

### Agregar un Nuevo Estado
1. Crear clase en `Application.Bot/StateMachine/Handlers/` implementando `IStateHandler`.
2. Registrar en `StateHandlerFactory`.
3. Agregar valor al enum `ConversationState`.
4. Definir la lógica de `HandleAsync` (procesar input) y `ShowPromptAsync` (mostrar mensaje inicial del estado).

### Agregar un Nuevo Tipo de Mensaje
1. Crear implementación de `IMessageTypeHandler`.
2. Registrar en `MessageTypeHandlerFactory`.
3. Definir la lógica de procesamiento.

## 📸 Soporte Multimedia

El bot tiene soporte nativo para recibir:
- **Imágenes**: Se pueden guardar o procesar (actualmente respuesta genérica).
- **Ubicaciones**: Se detectan y convierten a dirección en texto si es posible.
- **Documentos/Contactos**: Se manejan con handlers específicos o genéricos.

Mensajes no soportados reciben una respuesta automática indicando al usuario que envíe texto o use las opciones del menú, manteniendo el flujo activo.
