using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using System.Linq.Expressions;

namespace BotCarniceria.Core.Application.Specifications;

public class AdminsWithPhoneSpecification : Specification<Usuario>
{
    public override Expression<Func<Usuario, bool>> ToExpression()
    {
        return u => u.Activo && u.Rol == RolUsuario.Admin && !string.IsNullOrEmpty(u.Telefono);
    }
}
