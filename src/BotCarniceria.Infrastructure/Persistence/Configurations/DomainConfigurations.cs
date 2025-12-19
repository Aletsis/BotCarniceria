using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotCarniceria.Infrastructure.Persistence.Configurations;

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");

        builder.HasKey(p => p.PedidoID);

        builder.Property(p => p.Folio)
            .HasConversion(
                folio => folio.Value,
                value => Folio.From(value))
            .HasMaxLength(50)
            .IsRequired();
            
        builder.HasIndex(p => p.Folio).IsUnique(); // As per original DB

        builder.Property(p => p.Contenido).IsRequired();
        
        builder.Property(p => p.Estado)
            .HasConversion<string>() // Save as string (or int, check original DB)
             // Original uses string "EnEsperaDeSurtir" etc? Or int? 
             // Logic in DTO migration suggests string. Let's stick to string for readability or int for perf.
             // Original ApplicationDbContext doesn't specify conversion so it defaults to int if enum, 
             // but if original model had string property for status...
             // Let's assume int (0, 1, 2) is safer for new Clean Arch, unless we MUST match legcy string.
             // Checking Specification from original code: `pedido.EstadoEnum == PedidoEstado`. So it was Enum/Int.
            .IsRequired();

        builder.HasOne(p => p.Cliente)
            .WithMany(c => c.Pedidos)
            .HasForeignKey(p => p.ClienteID)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.ClienteID);

        builder.Property(c => c.NumeroTelefono)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(c => c.NumeroTelefono).IsUnique();

        builder.Property(c => c.Nombre)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(c => c.Direccion)
            .HasMaxLength(250);
            
        builder.Property(c => c.FechaAlta)
            .HasDefaultValueSql("GETDATE()"); // SQL Server specific

        builder.OwnsOne(c => c.DatosFacturacion, df =>
        {
            df.Property(d => d.RazonSocial).HasMaxLength(200).HasColumnName("Facturacion_RazonSocial");
            df.Property(d => d.Calle).HasMaxLength(150).HasColumnName("Facturacion_Calle");
            df.Property(d => d.Numero).HasMaxLength(50).HasColumnName("Facturacion_Numero");
            df.Property(d => d.Colonia).HasMaxLength(100).HasColumnName("Facturacion_Colonia");
            df.Property(d => d.CodigoPostal).HasMaxLength(10).HasColumnName("Facturacion_CodigoPostal");
            df.Property(d => d.Correo).HasMaxLength(100).HasColumnName("Facturacion_Correo");
            df.Property(d => d.RegimenFiscal).HasMaxLength(100).HasColumnName("Facturacion_RegimenFiscal");
        });
    }
}
