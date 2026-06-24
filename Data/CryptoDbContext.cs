using Microsoft.EntityFrameworkCore;

namespace CryptoView.Data
{
    /// <summary>
    /// Contexto de base de datos para la aplicación CryptoView.
    /// Gestiona las entidades CryptoCurrency y UserNote con Entity Framework Core.
    /// </summary>
    public class CryptoDbContext : DbContext
    {
        /// <summary>
        /// Constructor del contexto
        /// </summary>
        /// <param name="options">Opciones de configuración del DbContext</param>
        public CryptoDbContext(DbContextOptions<CryptoDbContext> options) : base(options)
        {
        }

        /// <summary>
        /// Colección de criptomonedas en la base de datos
        /// </summary>
        public DbSet<CryptoCurrency> CryptoCurrencies { get; set; }

        /// <summary>
        /// Colección de notas de usuario en la base de datos
        /// </summary>
        public DbSet<UserNote> UserNotes { get; set; }

        /// <summary>
        /// Usuarios de la aplicación (admin único + usuarios normales registrados).
        /// </summary>
        public DbSet<AppUser> AppUsers { get; set; }

        /// <summary>
        /// User profiles containing extended profile data (one-to-one with AppUser).
        /// </summary>
        public DbSet<UserProfile> UserProfiles { get; set; }

        /// <summary>
        /// Configuración del modelo de datos y relaciones
        /// </summary>
        /// <param name="modelBuilder">Constructor del modelo</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de la entidad CryptoCurrency
            modelBuilder.Entity<CryptoCurrency>(entity =>
            {
                // Índice único en CoinId para evitar duplicados
                entity.HasIndex(e => e.CoinId).IsUnique();

                // Índice en Symbol para búsquedas rápidas
                entity.HasIndex(e => e.Symbol);

                // Índice en IsActive para filtrar monedas activas
                entity.HasIndex(e => e.IsActive);

                // Relación uno a muchos con UserNotes
                entity.HasMany(e => e.Notes)
                      .WithOne(e => e.CryptoCurrency)
                      .HasForeignKey(e => e.CryptoCurrencyId)
                      .OnDelete(DeleteBehavior.Cascade); // Eliminar notas al eliminar la criptomoneda
            });

            // Configuración de la entidad UserNote
            modelBuilder.Entity<UserNote>(entity =>
            {
                // Índice en CryptoCurrencyId para búsquedas rápidas
                entity.HasIndex(e => e.CryptoCurrencyId);

                // Índice en CreatedAt para ordenamiento
                entity.HasIndex(e => e.CreatedAt);
            });

            // Configuración de la entidad AppUser
            modelBuilder.Entity<AppUser>(entity =>
            {
                // Username must be unique across all users
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Role);
            });

            // Configuración de la entidad UserProfile
            modelBuilder.Entity<UserProfile>(entity =>
            {
                // One-to-one relationship with AppUser
                entity.HasOne(e => e.AppUser)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint on UserId to enforce one-to-one
                entity.HasIndex(e => e.UserId).IsUnique();

                // Índice en FullName para búsquedas rápidas
                entity.HasIndex(e => e.FullName);

                // Índice en Email para búsquedas rápidas
                entity.HasIndex(e => e.Email);
            });

            // Datos de prueba (seed data) - opcional
            SeedData(modelBuilder);
        }

        /// <summary>
        /// Método para agregar datos iniciales a la base de datos
        /// </summary>
        /// <param name="modelBuilder">Constructor del modelo</param>
        private void SeedData(ModelBuilder modelBuilder)
        {
            // Agregar algunas criptomonedas populares como ejemplo
            modelBuilder.Entity<CryptoCurrency>().HasData(
                new CryptoCurrency
                {
                    Id = 1,
                    CoinId = "bitcoin",
                    Name = "Bitcoin",
                    Symbol = "BTC",
                    CurrentPrice = 0,
                    MarketCap = 0,
                    Volume24h = 0,
                    Change24h = 0,
                    DateAdded = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    IsActive = true
                },
                new CryptoCurrency
                {
                    Id = 2,
                    CoinId = "ethereum",
                    Name = "Ethereum",
                    Symbol = "ETH",
                    CurrentPrice = 0,
                    MarketCap = 0,
                    Volume24h = 0,
                    Change24h = 0,
                    DateAdded = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    IsActive = true
                }
            );
        }
    }
}
