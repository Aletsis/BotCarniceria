using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Application.Specifications;

public class UsuarioSpecificationsTests
{
    private static Usuario CreateUser(string username, RolUsuario rol, bool activo = true, string? telefono = null)
    {
        var user = Usuario.Create(username, "hash123", username, rol, telefono);
        if (!activo)
        {
            user.ToggleActivo(false);
        }
        return user;
    }

    #region AdminAndSupervisorUsersSpecification Tests

    [Theory]
    [InlineData(RolUsuario.Admin)]
    [InlineData(RolUsuario.Supervisor)]
    public void AdminAndSupervisorUsersSpecification_WhenActiveAdminOrSupervisor_ReturnsTrue(RolUsuario rol)
    {
        // Arrange
        var user = CreateUser("testuser", rol, activo: true);
        var spec = new AdminAndSupervisorUsersSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeTrue();
    }

    [Theory]
    [InlineData(RolUsuario.Admin)]
    [InlineData(RolUsuario.Supervisor)]
    public void AdminAndSupervisorUsersSpecification_WhenInactive_ReturnsFalse(RolUsuario rol)
    {
        // Arrange
        var user = CreateUser("inactiveuser", rol, activo: false);
        var spec = new AdminAndSupervisorUsersSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeFalse();
    }

    [Theory]
    [InlineData(RolUsuario.Editor)]
    [InlineData(RolUsuario.Viewer)]
    public void AdminAndSupervisorUsersSpecification_WhenOtherRole_ReturnsFalse(RolUsuario rol)
    {
        // Arrange
        var user = CreateUser("otherrole", rol, activo: true);
        var spec = new AdminAndSupervisorUsersSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeFalse();
    }

    [Fact]
    public void AdminAndSupervisorUsersSpecification_WorksWithLinq()
    {
        // Arrange
        var users = new List<Usuario>
        {
            CreateUser("u1", RolUsuario.Admin, activo: true),
            CreateUser("u2", RolUsuario.Supervisor, activo: true),
            CreateUser("u3", RolUsuario.Admin, activo: false),
            CreateUser("u4", RolUsuario.Editor, activo: true),
            CreateUser("u5", RolUsuario.Viewer, activo: true)
        };
        var spec = new AdminAndSupervisorUsersSpecification();

        // Act
        var result = users.AsQueryable().Where(spec.ToExpression()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(u => u.Username).Should().BeEquivalentTo(new[] { "u1", "u2" });
    }

    #endregion

    #region AdminsWithPhoneSpecification Tests

    [Fact]
    public void AdminsWithPhoneSpecification_WhenActiveAdminWithPhone_ReturnsTrue()
    {
        // Arrange
        var user = CreateUser("admin1", RolUsuario.Admin, activo: true, telefono: "5551234567");
        var spec = new AdminsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void AdminsWithPhoneSpecification_WhenPhoneNullOrEmpty_ReturnsFalse(string? phone)
    {
        // Arrange
        var user = CreateUser("admin_nophone", RolUsuario.Admin, activo: true, telefono: phone);
        var spec = new AdminsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeFalse();
    }

    [Fact]
    public void AdminsWithPhoneSpecification_WhenInactive_ReturnsFalse()
    {
        // Arrange
        var user = CreateUser("inactive_admin", RolUsuario.Admin, activo: false, telefono: "5551234567");
        var spec = new AdminsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeFalse();
    }

    [Fact]
    public void AdminsWithPhoneSpecification_WhenSupervisorOrOtherRole_ReturnsFalse()
    {
        // Arrange
        var supervisor = CreateUser("super1", RolUsuario.Supervisor, activo: true, telefono: "5551234567");
        var editor = CreateUser("editor1", RolUsuario.Editor, activo: true, telefono: "5551234567");
        var spec = new AdminsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(supervisor).Should().BeFalse();
        spec.IsSatisfiedBy(editor).Should().BeFalse();
    }

    [Fact]
    public void AdminsWithPhoneSpecification_WorksWithLinq()
    {
        // Arrange
        var users = new List<Usuario>
        {
            CreateUser("admin1", RolUsuario.Admin, activo: true, telefono: "5551111111"),
            CreateUser("admin2", RolUsuario.Admin, activo: true, telefono: null),
            CreateUser("admin3", RolUsuario.Admin, activo: false, telefono: "5552222222"),
            CreateUser("super1", RolUsuario.Supervisor, activo: true, telefono: "5553333333")
        };
        var spec = new AdminsWithPhoneSpecification();

        // Act
        var result = users.AsQueryable().Where(spec.ToExpression()).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Username.Should().Be("admin1");
    }

    #endregion

    #region AdminsAndSupervisorsWithPhoneSpecification Tests

    [Theory]
    [InlineData(RolUsuario.Admin)]
    [InlineData(RolUsuario.Supervisor)]
    public void AdminsAndSupervisorsWithPhoneSpecification_WhenActiveWithPhone_ReturnsTrue(RolUsuario rol)
    {
        // Arrange
        var user = CreateUser("user1", rol, activo: true, telefono: "5551234567");
        var spec = new AdminsAndSupervisorsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeTrue();
    }

    [Theory]
    [InlineData(RolUsuario.Admin, null)]
    [InlineData(RolUsuario.Admin, "")]
    [InlineData(RolUsuario.Supervisor, null)]
    [InlineData(RolUsuario.Supervisor, "")]
    public void AdminsAndSupervisorsWithPhoneSpecification_WhenPhoneNullOrEmpty_ReturnsFalse(RolUsuario rol, string? phone)
    {
        // Arrange
        var user = CreateUser("user_nophone", rol, activo: true, telefono: phone);
        var spec = new AdminsAndSupervisorsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeFalse();
    }

    [Theory]
    [InlineData(RolUsuario.Admin)]
    [InlineData(RolUsuario.Supervisor)]
    public void AdminsAndSupervisorsWithPhoneSpecification_WhenInactive_ReturnsFalse(RolUsuario rol)
    {
        // Arrange
        var user = CreateUser("inactive_user", rol, activo: false, telefono: "5551234567");
        var spec = new AdminsAndSupervisorsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeFalse();
    }

    [Theory]
    [InlineData(RolUsuario.Editor)]
    [InlineData(RolUsuario.Viewer)]
    public void AdminsAndSupervisorsWithPhoneSpecification_WhenOtherRole_ReturnsFalse(RolUsuario rol)
    {
        // Arrange
        var user = CreateUser("other_role", rol, activo: true, telefono: "5551234567");
        var spec = new AdminsAndSupervisorsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeFalse();
    }

    [Fact]
    public void AdminsAndSupervisorsWithPhoneSpecification_WorksWithLinq()
    {
        // Arrange
        var users = new List<Usuario>
        {
            CreateUser("admin1", RolUsuario.Admin, activo: true, telefono: "5551111111"),
            CreateUser("super1", RolUsuario.Supervisor, activo: true, telefono: "5552222222"),
            CreateUser("admin_inactive", RolUsuario.Admin, activo: false, telefono: "5553333333"),
            CreateUser("super_nophone", RolUsuario.Supervisor, activo: true, telefono: null),
            CreateUser("editor1", RolUsuario.Editor, activo: true, telefono: "5554444444")
        };
        var spec = new AdminsAndSupervisorsWithPhoneSpecification();

        // Act
        var result = users.AsQueryable().Where(spec.ToExpression()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Select(u => u.Username).Should().BeEquivalentTo(new[] { "admin1", "super1" });
    }

    #endregion

    #region SupervisorsWithPhoneSpecification Tests

    [Fact]
    public void SupervisorsWithPhoneSpecification_WhenActiveSupervisorWithPhone_ReturnsTrue()
    {
        // Arrange
        var user = CreateUser("super1", RolUsuario.Supervisor, activo: true, telefono: "5551234567");
        var spec = new SupervisorsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SupervisorsWithPhoneSpecification_WhenPhoneNullOrEmpty_ReturnsFalse(string? phone)
    {
        // Arrange
        var user = CreateUser("super_nophone", RolUsuario.Supervisor, activo: true, telefono: phone);
        var spec = new SupervisorsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeFalse();
    }

    [Fact]
    public void SupervisorsWithPhoneSpecification_WhenInactive_ReturnsFalse()
    {
        // Arrange
        var user = CreateUser("inactive_super", RolUsuario.Supervisor, activo: false, telefono: "5551234567");
        var spec = new SupervisorsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeFalse();
    }

    [Theory]
    [InlineData(RolUsuario.Admin)]
    [InlineData(RolUsuario.Editor)]
    [InlineData(RolUsuario.Viewer)]
    public void SupervisorsWithPhoneSpecification_WhenNotSupervisor_ReturnsFalse(RolUsuario rol)
    {
        // Arrange
        var user = CreateUser("other_user", rol, activo: true, telefono: "5551234567");
        var spec = new SupervisorsWithPhoneSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(user).Should().BeFalse();
    }

    [Fact]
    public void SupervisorsWithPhoneSpecification_WorksWithLinq()
    {
        // Arrange
        var users = new List<Usuario>
        {
            CreateUser("super1", RolUsuario.Supervisor, activo: true, telefono: "5551111111"),
            CreateUser("admin1", RolUsuario.Admin, activo: true, telefono: "5552222222"),
            CreateUser("super_inactive", RolUsuario.Supervisor, activo: false, telefono: "5553333333"),
            CreateUser("super_nophone", RolUsuario.Supervisor, activo: true, telefono: null),
            CreateUser("editor1", RolUsuario.Editor, activo: true, telefono: "5554444444")
        };
        var spec = new SupervisorsWithPhoneSpecification();

        // Act
        var result = users.AsQueryable().Where(spec.ToExpression()).ToList();

        // Assert
        result.Should().HaveCount(1);
        result.First().Username.Should().Be("super1");
    }

    #endregion
}
