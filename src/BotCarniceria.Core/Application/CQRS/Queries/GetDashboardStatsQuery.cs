using BotCarniceria.Core.Application.DTOs;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Queries;

public class GetDashboardStatsQuery : IRequest<DashboardStatsDto>
{
}
