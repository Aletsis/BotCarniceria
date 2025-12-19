using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using System.Linq.Expressions;

namespace BotCarniceria.Core.Application.Specifications;

public class SupervisorsWithPhoneSpecification : Specification<Usuario>
{
    public override Expression<Func<Usuario, bool>> ToExpression()
    {
        return u => u.Activo && u.Rol == RolUsuario.Supervisor && !string.IsNullOrEmpty(u.Telefono);
    }
}
