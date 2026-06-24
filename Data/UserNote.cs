using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoView.Data
{
    /// <summary>
    /// Representa una nota del usuario asociada a una criptomoneda específica.
    /// Permite al usuario agregar observaciones, estrategias o recordatorios.
    /// </summary>
    public class UserNote
    {
        /// <summary>
        /// Identificador único de la nota
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID de la criptomoneda asociada (clave foránea)
        /// </summary>
        [Required]
        public int CryptoCurrencyId { get; set; }

        /// <summary>
        /// Título de la nota
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Contenido de la nota
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Fecha y hora de creación de la nota
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Fecha y hora de la última modificación
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Prioridad de la nota (1 = Baja, 2 = Media, 3 = Alta)
        /// </summary>
        [Range(1, 3)]
        public int Priority { get; set; } = 2;

        /// <summary>
        /// Navegación hacia la criptomoneda asociada
        /// </summary>
        [ForeignKey("CryptoCurrencyId")]
        public virtual CryptoCurrency? CryptoCurrency { get; set; }
    }
}
