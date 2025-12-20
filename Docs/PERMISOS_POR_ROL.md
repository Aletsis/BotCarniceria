# Matriz de Permisos por Rol - BotCarniceria

## Fecha de Actualización
19 de Diciembre de 2025

## Roles del Sistema

El sistema cuenta con 4 roles definidos en `RolUsuario.cs`:
- **Admin (0)**: Administrador con acceso completo
- **Supervisor (1)**: Supervisor con acceso limitado
- **Editor (2)**: Editor con acceso a operaciones básicas
- **Viewer (3)**: Visualizador con acceso de solo lectura

## Matriz de Permisos

| Página/Funcionalidad | Admin | Supervisor | Editor | Viewer |
|---------------------|-------|------------|--------|--------|
| **Inicio** | ✅ | ✅ | ✅ | ✅ |
| **Chats** | ✅ | ✅ | ✅ | ✅ |
| **Pedidos** | ✅ | ✅ | ✅ | ❌ |
| **Facturas** | ✅ | ✅ | ❌ | ❌ |
| **Clientes** | ✅ | ✅ | ❌ | ❌ |
| **Conversaciones** | ✅ | ❌ | ❌ | ❌ |
| **Usuarios** | ✅ | ❌ | ❌ | ❌ |
| **Configuraciones** | ✅ | ❌ | ❌ | ❌ |

## Descripción de Permisos por Rol

### 👑 Admin
- **Acceso completo** a todas las funcionalidades del sistema
- Puede gestionar usuarios y configuraciones
- Acceso exclusivo a la página de Conversaciones
- Puede ver y modificar todo

### 👨‍💼 Supervisor
- Acceso a operaciones del día a día
- Puede gestionar: Chats, Pedidos, Facturas y Clientes
- **NO** tiene acceso a: Conversaciones, Usuarios y Configuraciones
- Puede modificar datos operativos

### ✏️ Editor
- Acceso limitado a operaciones básicas
- Puede gestionar: Chats y Pedidos
- **NO** tiene acceso a: Facturas, Clientes, Conversaciones, Usuarios y Configuraciones
- Enfocado en la gestión de pedidos del día

### 👁️ Viewer
- Acceso de **solo lectura**
- Solo puede ver: Chats
- **NO** tiene acceso a ninguna otra funcionalidad
- Rol para monitoreo básico

## Implementación Técnica

### NavMenu.razor
El menú de navegación se ajusta dinámicamente según el rol del usuario:

```csharp
// Todos los roles ven Chats
<MudNavLink Href="chats">Chats</MudNavLink>

// Editor y superiores ven Pedidos
@if (_isAdmin || _isSupervisor || _isEditor)
{
    <MudNavLink Href="pedidos">Pedidos</MudNavLink>
}

// Supervisor y Admin ven Facturas y Clientes
@if (_isAdmin || _isSupervisor)
{
    <MudNavLink Href="facturas">Facturas</MudNavLink>
    <MudNavLink Href="clientes">Clientes</MudNavLink>
}

// Solo Admin ve Conversaciones y Administración
@if (_isAdmin)
{
    <MudNavLink Href="conversaciones">Conversaciones</MudNavLink>
    <MudNavGroup Title="Administración">...</MudNavGroup>
}
```

### Atributos de Autorización en Páginas

| Archivo | Atributo Authorize |
|---------|-------------------|
| `Chats.razor` | `[Authorize(Roles = "admin,supervisor,editor,viewer")]` |
| `Orders.razor` | `[Authorize(Roles = "admin,supervisor,editor")]` |
| `Facturas.razor` | `[Authorize(Roles = "admin,supervisor")]` |
| `Clients.razor` | `[Authorize(Roles = "admin,supervisor")]` |
| `Conversations.razor` | `[Authorize(Roles = "admin")]` |
| `Users.razor` | `[Authorize(Roles = "admin")]` |
| `Configs.razor` | `[Authorize(Roles = "admin")]` |

## Notas Importantes

1. **Seguridad en Capas**: La autorización se implementa tanto en el NavMenu (UI) como en los atributos de las páginas (servidor).

2. **Acceso Directo**: Aunque un usuario no vea un enlace en el menú, si intenta acceder directamente a la URL, el atributo `[Authorize]` lo bloqueará.

3. **Viewer - Solo Lectura**: Actualmente el rol Viewer puede ver Chats. Se recomienda implementar lógica adicional para ocultar botones de edición/envío para este rol.

4. **Editor - Restricción de Fecha**: En la página de Pedidos, los Editores solo pueden ver pedidos del día actual (implementado en `Orders.razor`).

## Recomendaciones Futuras

1. **Implementar permisos granulares** para el rol Viewer en la página de Chats (ocultar botón de envío de mensajes).

2. **Agregar auditoría** de acciones por rol para tracking de cambios.

3. **Considerar permisos adicionales** como:
   - Permiso para imprimir pedidos
   - Permiso para cambiar estados
   - Permiso para ver información sensible

4. **Implementar políticas de autorización** más complejas usando `IAuthorizationService` si se requieren reglas de negocio más sofisticadas.
