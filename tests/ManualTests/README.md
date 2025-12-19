# Pruebas Manuales y de Integración (Webhook Flow)

Esta carpeta contiene herramientas para verificar el flujo completo de la aplicación (End-to-End) de manera manual o semi-automatizada, simulando eventos externos como Webhooks de WhatsApp.

## Contenido

- `payload.json`: Un archivo JSON que estructura un mensaje de webhook de WhatsApp simulado.
- `Simulate-Webhook.ps1`: Script de PowerShell para enviar el `payload.json` al endpoint de la API local.

## Prerrequisitos

Para ejecutar estas pruebas, necesitas tener corriendo:

1.  **Base de Datos**: LocalDB o SQL Server configurado.
2.  **API**: El proyecto `BotCarniceria.Presentation.API` corriendo en `http://localhost:5091`.
    *   *Nota*: Asegúrate de que la validación de firma de WhatsApp esté deshabilitada en entorno de desarrollo (`Development`) o configurada correctamente.
3.  **Blazor (UI)**: El proyecto `BotCarniceria.Presentation.Blazor` corriendo en `http://localhost:5014` (para verificar la recepción en tiempo real).

## Cómo Ejecutar

1.  Abre una terminal en esta carpeta (`tests/ManualTests`).
2.  Ejecuta el script:
    ```powershell
    .\Simulate-Webhook.ps1
    ```
3.  **Verificación**:
    *   **Consola API**: Deberías ver logs indicando la recepción y procesamiento del mensaje.
    *   **Base de Datos**: Verifica que se haya creado un registro en la tabla `Mensajes` y una `Conversacion` para el número `5215512345678`.
    *   **Blazor UI**: Navega a `http://localhost:5014/chats`. Deberías ver la nueva conversación y el mensaje entrante.

## Personalización

Puedes editar `payload.json` para:
*   Cambiar el número de teléfono (`from`, `wa_id`).
*   Cambiar el contenido del mensaje (`text.body`).
*   Simular otros tipos de mensajes (botones, listas) ajustando la estructura JSON según la documentación de WhatsApp API.
