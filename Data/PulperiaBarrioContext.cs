using Microsoft.EntityFrameworkCore;
using PulperiaBarrio.Models;

namespace PulperiaBarrio.Data;

public class PulperiaBarrioContext(DbContextOptions<PulperiaBarrioContext> options) : DbContext(options)
{
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Producto> Productos => Set<Producto>();

    public DbSet<ProductoInventarioView> ProductosInventario => Set<ProductoInventarioView>();

    public DbSet<ReservaClienteView> ReservasClientes => Set<ReservaClienteView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Producto>()
            .Property(producto => producto.Atributos)
            .HasColumnType("jsonb");

        modelBuilder.Entity<ProductoInventarioView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_productos_inventario");
            entity.Property(producto => producto.ProductoId).HasColumnName("producto_id");
            entity.Property(producto => producto.Producto).HasColumnName("producto");
            entity.Property(producto => producto.Categoria).HasColumnName("categoria");
            entity.Property(producto => producto.Precio).HasColumnName("precio");
            entity.Property(producto => producto.MaxCantidadReserva).HasColumnName("max_cantidad_reserva");
            entity.Property(producto => producto.MaxHorasReserva).HasColumnName("max_horas_reserva");
            entity.Property(producto => producto.Atributos).HasColumnName("atributos").HasColumnType("jsonb");
            entity.Property(producto => producto.SucursalId).HasColumnName("sucursal_id");
            entity.Property(producto => producto.Sucursal).HasColumnName("sucursal");
            entity.Property(producto => producto.CantidadDisponible).HasColumnName("cantidad_disponible");
            entity.Property(producto => producto.CantidadReservada).HasColumnName("cantidad_reservada");
            entity.Property(producto => producto.ExistenciaMinima).HasColumnName("existencia_minima");
        });

        modelBuilder.Entity<ReservaClienteView>(entity =>
        {
            entity.HasNoKey();
            entity.ToView("vw_reservas_clientes");
            entity.Property(reserva => reserva.ReservaId).HasColumnName("reserva_id");
            entity.Property(reserva => reserva.ClienteId).HasColumnName("cliente_id");
            entity.Property(reserva => reserva.Cliente).HasColumnName("cliente");
            entity.Property(reserva => reserva.Sucursal).HasColumnName("sucursal");
            entity.Property(reserva => reserva.Producto).HasColumnName("producto");
            entity.Property(reserva => reserva.Cantidad).HasColumnName("cantidad");
            entity.Property(reserva => reserva.FechaReserva).HasColumnName("fecha_reserva");
            entity.Property(reserva => reserva.FechaVencimiento).HasColumnName("fecha_vencimiento");
            entity.Property(reserva => reserva.Resultado).HasColumnName("resultado");
        });
    }
}
