using BotCarniceria.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BotCarniceria.Infrastructure.Persistence.Configurations;

public class ConversacionConfiguration : IEntityTypeConfiguration<Conversacion>
{
    public void Configure(EntityTypeBuilder<Conversacion> builder)
    {
        builder.ToTable("Conversaciones");

        builder.HasKey(c => c.NumeroTelefono); // PK is Phone

        builder.Property(c => c.NumeroTelefono)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.Estado)
            .HasConversion<string>() // Save state as string for readability in DB
            .HasMaxLength(50);

        builder.Property(c => c.NombreTemporal)
            .HasMaxLength(100);

        builder.Property(c => c.FacturaTemp_Folio).HasMaxLength(50);
        builder.Property(c => c.FacturaTemp_Total).HasMaxLength(20);
        builder.Property(c => c.FacturaTemp_UsoCFDI).HasMaxLength(100);

        builder.Property(c => c.UltimaActividad)
            .IsRequired();

        builder.Property(c => c.NotificacionTimeoutEnviada)
            .HasDefaultValue(false);
    }
}

public class MensajeConfiguration : IEntityTypeConfiguration<Mensaje>
{
    public void Configure(EntityTypeBuilder<Mensaje> builder)
    {
        builder.ToTable("Mensajes");

        builder.HasKey(m => m.MensajeID);

        builder.Property(m => m.NumeroTelefono)
            .HasMaxLength(20)
            .IsRequired();
            
        builder.HasIndex(m => m.NumeroTelefono);
        builder.HasIndex(m => m.Fecha);

        builder.Property(m => m.Contenido)
            .IsRequired(); // Max length? WhatsApp messages can be long.

        builder.Property(m => m.Origen)
            .HasConversion<int>(); 

        builder.Property(m => m.TipoContenido)
            .HasConversion<int>();
            
        builder.Property(m => m.Estado)
            .HasConversion<int>();

        builder.Property(m => m.WhatsAppMessageId)
            .HasMaxLength(100);
    }
}

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");

        builder.HasKey(u => u.UsuarioID);

        builder.Property(u => u.Username)
            .HasMaxLength(50)
            .IsRequired();
            
        builder.HasIndex(u => u.Username).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Nombre).HasMaxLength(100);
        
        builder.Property(u => u.Rol)
            .HasConversion<string>() // Save roles as strings ("Admin", "Editor") for clarity
            .HasMaxLength(20);
            
         // Seed initial admin user if needed, but better in Seed script or separate seeder
    }
}

public class ConfiguracionConfiguration : IEntityTypeConfiguration<Configuracion>
{
    public void Configure(EntityTypeBuilder<Configuracion> builder)
    {
        builder.ToTable("Configuraciones");

        builder.HasKey(c => c.ConfigID);

        builder.Property(c => c.Clave)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.HasIndex(c => c.Clave).IsUnique();

        builder.Property(c => c.Valor).IsRequired(); // Assuming value is always required

        builder.Property(c => c.Tipo)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
