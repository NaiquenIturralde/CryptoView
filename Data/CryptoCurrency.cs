using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoView.Data
{
    /// <summary>
    /// Representa una criptomoneda en el sistema.
    /// Esta clase contiene toda la información relevante de una criptomoneda favorita del usuario.
    /// </summary>
    public class CryptoCurrency
    {
        /// <summary>
        /// Identificador único de la criptomoneda en la base de datos
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID único de la criptomoneda en CoinGecko API (ej: "bitcoin", "ethereum")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string CoinId { get; set; } = string.Empty;

        /// <summary>
        /// Nombre completo de la criptomoneda (ej: "Bitcoin", "Ethereum")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Símbolo de la criptomoneda (ej: "BTC", "ETH")
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Precio actual en USD (actualizado desde la API)
        /// </summary>
        [Column(TypeName = "decimal(18,8)")]
        public decimal CurrentPrice { get; set; }

        /// <summary>
        /// Capitalización de mercado en USD
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal MarketCap { get; set; }

        /// <summary>
        /// Volumen de trading en las últimas 24 horas
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Volume24h { get; set; }

        /// <summary>
        /// Cambio de precio en las últimas 24 horas (porcentaje)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Change24h { get; set; }

        /// <summary>
        /// Umbral de precio para notificaciones (opcional)
        /// </summary>
        [Column(TypeName = "decimal(18,8)")]
        public decimal? PriceThreshold { get; set; }

        /// <summary>
        /// Fecha y hora de la última actualización
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha y hora en que se agregó a favoritos
        /// </summary>
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indica si la moneda está activa en la lista de favoritos
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Colección de notas del usuario asociadas a esta criptomoneda
        /// </summary>
        public virtual ICollection<UserNote> Notes { get; set; } = new List<UserNote>();
    }
}
