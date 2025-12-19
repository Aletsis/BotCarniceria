using BotCarniceria.Core.Domain.Entities;
using System.Linq.Expressions;

namespace BotCarniceria.Core.Application.Specifications;

public class ClienteByPhoneNumberSpecification : Specification<Cliente>
{
    private readonly string _phoneNumber;

    public ClienteByPhoneNumberSpecification(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
    }

    public override Expression<Func<Cliente, bool>> ToExpression()
    {
        return cliente => cliente.NumeroTelefono == _phoneNumber;
    }
}

public class ClientesActiveSpecification : Specification<Cliente>
{
    public override Expression<Func<Cliente, bool>> ToExpression()
    {
        return cliente => cliente.Activo;
    }
}

public class ClientesBySearchTermSpecification : Specification<Cliente>
{
    private readonly string _searchTerm;

    public ClientesBySearchTermSpecification(string searchTerm)
    {
        _searchTerm = searchTerm?.ToLower() ?? throw new ArgumentNullException(nameof(searchTerm));
    }

    public override Expression<Func<Cliente, bool>> ToExpression()
    {
        return cliente => (cliente.Nombre != null && cliente.Nombre.ToLower().Contains(_searchTerm))
                       || cliente.NumeroTelefono.Contains(_searchTerm);
    }
}
