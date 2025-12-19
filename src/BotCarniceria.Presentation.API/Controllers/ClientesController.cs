using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BotCarniceria.Presentation.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrUpdateClienteCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
    
    [HttpGet("phone/{phoneNumber}")]
    public async Task<IActionResult> GetByPhone(string phoneNumber)
    {
        var result = await _mediator.Send(new GetClienteByPhoneQuery { PhoneNumber = phoneNumber });
        if (result == null) return NotFound();
        return Ok(result);
    }
}
