using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using System.Linq.Expressions;

namespace BotCarniceria.Core.Application.Specifications;

public class AdminAndSupervisorUsersSpecification : Specification<Usuario>
{
    public override Expression<Func<Usuario, bool>> ToExpression()
    {
        return u => (u.Rol == RolUsuario.Admin || u.Rol == RolUsuario.Supervisor) && u.Activo;
    }
}
