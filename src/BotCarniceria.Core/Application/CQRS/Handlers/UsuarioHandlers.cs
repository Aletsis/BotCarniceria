using MediatR;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.CQRS.Commands;

using Microsoft.Extensions.Logging;

namespace BotCarniceria.Core.Application.CQRS.Handlers;

public class UsuarioHandlers : 
    IRequestHandler<GetAllUsuariosQuery, List<UsuarioDto>>,
    IRequestHandler<GetUsuarioByIdQuery, UsuarioDto?>,
    IRequestHandler<LoginUserQuery, UsuarioDto?>,
    IRequestHandler<CreateUsuarioCommand, bool>,
    IRequestHandler<UpdateUsuarioCommand, bool>,
    IRequestHandler<ToggleUsuarioActivoCommand, bool>,
    IRequestHandler<ResetUserLockoutCommand, bool>,
    IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UsuarioHandlers> _logger;

    public UsuarioHandlers(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher, ILogger<UsuarioHandlers> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<List<UsuarioDto>> Handle(GetAllUsuariosQuery request, CancellationToken cancellationToken)
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return users.Select(UsuarioDto.FromEntity).ToList();
    }

    public async Task<UsuarioDto?> Handle(GetUsuarioByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.Id);
        if (user == null) 
        {
            _logger.LogWarning("Usuario con ID {Id} no encontrado", request.Id);
        }
        return user != null ? UsuarioDto.FromEntity(user) : null;
    }

    public async Task<UsuarioDto?> Handle(LoginUserQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Procesando login para usuario: {Username}", request.Username);
        
        var user = await _unitOfWork.Users.GetByUsernameAsync(request.Username);
        
        // Anti-Timing Attack: Perform password verification even if user not found.
        string hashToVerify = user?.PasswordHash ?? Convert.ToBase64String(new byte[32]); // Dummy base64 string
        bool passwordResult = _passwordHasher.VerifyPassword(request.Password, hashToVerify);

        if (user == null)
        {
            _logger.LogWarning("Intento de login fallido: Usuario {Username} no existe o contraseña incorrecta", request.Username);
            return null;
        }

        // Brute Force Protection: Check if locked out
        if (user.EstaBloqueado())
        {
             _logger.LogWarning("Login bloqueado: Usuario {Username} ha excedido intentos fallidos. Bloqueado hasta: {LockoutEnd}", request.Username, user.LockoutEnd);
             return null;
        }

        if (!user.Activo)
        {
             _logger.LogWarning("Intento de login fallido: Usuario {Username} está inactivo", request.Username);
             return null;
        }

        if (!passwordResult)
        {
            _logger.LogWarning("Intento de login fallido: Contraseña incorrecta para usuario {Username}", request.Username);
            
            // Record failed attempt
            user.RegistrarIntentoFallido();
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return null;
        }

        // Success: Reset failed attempts and update access time
        user.ResetearAccesoFallido();
        user.ActualizarUltimoAcceso();
        
        await _unitOfWork.Users.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Login exitoso para usuario: {Username}", request.Username);
        return UsuarioDto.FromEntity(user);
    }

    public async Task<bool> Handle(CreateUsuarioCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Intentando crear usuario: {Username}", request.Username);
        var existing = await _unitOfWork.Users.GetByUsernameAsync(request.Username);
        if (existing != null) 
        {
            _logger.LogWarning("No se puede crear usuario {Username}: ya existe", request.Username);
            return false;
        }

        var hashedPassword = _passwordHasher.HashPassword(request.Password);
        var newUser = Usuario.Create(request.Username, hashedPassword, request.Nombre, request.Rol, request.Telefono);
        
        await _unitOfWork.Users.AddAsync(newUser);
        var result = await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
        
        if (result) _logger.LogInformation("Usuario {Username} creado exitosamente", request.Username);
        else _logger.LogError("Error al guardar usuario {Username} en base de datos", request.Username);

        return result;
    }

    public async Task<bool> Handle(UpdateUsuarioCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UsuarioID);
        if (user == null) return false;

        user.ActualizarDatos(request.Nombre, request.Telefono, request.Rol);
        await _unitOfWork.Users.UpdateAsync(user);
        _logger.LogInformation("Usuario con ID {Id} actualizado", request.UsuarioID);
        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> Handle(ToggleUsuarioActivoCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UsuarioID);
        if (user == null) return false;

        user.ToggleActivo(request.Activo);
        await _unitOfWork.Users.UpdateAsync(user);
        _logger.LogInformation("Estado de usuario {Id} cambiado a Activo={Activo}", request.UsuarioID, request.Activo);
        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> Handle(ResetUserLockoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UsuarioID);
        if (user == null) return false;

        user.ResetearAccesoFallido();
        await _unitOfWork.Users.UpdateAsync(user);
        _logger.LogInformation("Usuario ID {Id} desbloqueado manualmente", request.UsuarioID);
        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UsuarioID);
        if (user == null) return false;

        var hashedPassword = _passwordHasher.HashPassword(request.NewPassword);
        user.CambiarPassword(hashedPassword);
        await _unitOfWork.Users.UpdateAsync(user);
        _logger.LogInformation("Contraseña actualizada para usuario ID {Id}", request.UsuarioID);
        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }
}
