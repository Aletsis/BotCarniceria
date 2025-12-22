# Implementación de Notificaciones a Supervisores

Se ha modificado el handler `CreateSolicitudFacturaCommandHandler` en el proyecto `BotCarniceria.Core` para enviar notificaciones de WhatsApp a los usuarios con rol **Supervisor** cuando un cliente solicita una factura desde la página web.

## Cambios Realizados

1.  **Inyección de `IWhatsAppService`**: Se ha inyectado el servicio de WhatsApp en el constructor del handler.
2.  **Lógica de Notificación**: Se ha replicado la lógica existente en el bot (`BillingStateHandler`) para construir y enviar el mensaje de notificación.
    - Se utiliza `SupervisorsWithPhoneSpecification` para obtener a todos los supervisores con número de teléfono registrado.
    - Se envía un mensaje con los detalles del cliente, los datos de facturación y los detalles de la compra (folio, total, uso CFDI).
3.  **Manejo de Errores**: La notificación se envuelve en un bloque `try-catch` para asegurar que si falla el envío del mensaje (e.g., error de API), la solicitud de factura se guarde correctamente en la base de datos de todos modos.

## Acciones Requeridas

*   **Reiniciar la Aplicación**: Para que los cambios surtan efecto y el contenedor de inyección de dependencias registre correctamente el servicio actualizado en el handler, es necesario detener y volver a iniciar la aplicación (API y Blazor).
*   **Verificación**: Puede probar la funcionalidad solicitando una factura desde la página web y verificando si los supervisores reciben el mensaje de WhatsApp.
