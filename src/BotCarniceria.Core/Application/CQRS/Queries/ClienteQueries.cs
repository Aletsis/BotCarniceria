using BotCarniceria.Core.Application.DTOs;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Queries;

public class GetClienteByPhoneQuery : IRequest<ClienteDto?>
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class GetActiveClientesQuery : IRequest<List<ClienteDto>>
{
}

public class GetAllClientesQuery : IRequest<List<ClienteDto>>
{
    public string? SearchTerm { get; set; }
}

public class GetClienteByIdQuery : IRequest<ClienteDto?>
{
    public int Id { get; set; }
    
    public GetClienteByIdQuery(int id)
    {
        Id = id;
    }
}
