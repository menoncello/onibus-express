using Microsoft.EntityFrameworkCore;
using OniBus.Domain.Reservas;
using OniBus.Domain.Viagens;

namespace OniBus.Api.Persistence;

public class OniBusDbContext(DbContextOptions<OniBusDbContext> options) : DbContext(options)
{
    public DbSet<Rota> Rotas => Set<Rota>();

    public DbSet<Viagem> Viagens => Set<Viagem>();

    public DbSet<Reserva> Reservas => Set<Reserva>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rota>(rota =>
        {
            rota.ToTable("rotas");
            rota.HasKey(r => r.Id);
            rota.Property(r => r.Id).HasColumnName("id");
            rota.Property(r => r.Origem).HasColumnName("origem").HasMaxLength(120).IsRequired();
            rota.Property(r => r.Destino).HasColumnName("destino").HasMaxLength(120).IsRequired();
            rota.Property(r => r.DuracaoEstimada).HasColumnName("duracao_estimada").IsRequired();

            // AD-15: idempotência do seed é por chave natural — o banco, não só a aplicação,
            // garante que (origem, destino) nunca duplica.
            rota.HasIndex(r => new { r.Origem, r.Destino })
                .IsUnique()
                .HasDatabaseName("ux_rotas_origem_destino");
        });

        modelBuilder.Entity<Viagem>(viagem =>
        {
            viagem.ToTable("viagens");
            viagem.HasKey(v => v.Id);
            viagem.Property(v => v.Id).HasColumnName("id");
            viagem.Property(v => v.RotaId).HasColumnName("rota_id").IsRequired();
            viagem.Property(v => v.Partida).HasColumnName("partida").IsRequired();
            viagem.Property(v => v.PrecoBase).HasColumnName("preco_base").HasColumnType("decimal(10,2)").IsRequired();

            viagem.HasOne<Rota>()
                .WithMany()
                .HasForeignKey(v => v.RotaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Reserva>(reserva =>
        {
            reserva.ToTable("reservas");
            reserva.HasKey(r => r.Id);
            reserva.Property(r => r.Id).HasColumnName("id");
            reserva.Property(r => r.ViagemId).HasColumnName("viagem_id").IsRequired();
            reserva.Property(r => r.NumeroAssento).HasColumnName("numero_assento").IsRequired();
            reserva.Property(r => r.Codigo).HasColumnName("codigo").HasMaxLength(9).IsRequired();
            reserva.Property(r => r.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            reserva.HasOne<Viagem>()
                .WithMany()
                .HasForeignKey(r => r.ViagemId)
                .OnDelete(DeleteBehavior.Restrict);

            // AD-6/AD-7: os dois índices únicos fundacionais, presentes desde a primeira
            // migração, mesmo sem endpoint de reserva ainda.
            reserva.HasIndex(r => r.Codigo)
                .IsUnique()
                .HasDatabaseName("ux_reservas_codigo");

            reserva.HasIndex(r => new { r.ViagemId, r.NumeroAssento })
                .IsUnique()
                .HasDatabaseName("ux_reservas_viagem_assento_confirmada")
                .HasFilter("status = 'Confirmada'");
        });
    }
}
