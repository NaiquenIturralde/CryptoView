namespace CryptoView.Data
{
    /// <summary>
    /// Tabla intermedia normalizada para la relación N:M entre AppUser y CryptoCurrency.
    /// Reemplaza progresivamente el campo JSON UserProfile.FavoriteCryptoIds.
    ///
    /// Clave primaria compuesta: (UserId, CryptoCurrencyId).
    /// </summary>
    public class UserFavorite
    {
        /// <summary>
        /// FK hacia AppUsers.Id. Parte de la PK compuesta.
        /// Al eliminar el usuario se eliminan sus favoritos (Cascade).
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// FK hacia CryptoCurrencies.Id. Parte de la PK compuesta.
        /// No se puede eliminar una criptomoneda que tenga favoritos (Restrict).
        /// </summary>
        public int CryptoCurrencyId { get; set; }

        /// <summary>
        /// Fecha y hora en que el usuario agregó la criptomoneda a favoritos.
        /// </summary>
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        // ── Navegación ──────────────────────────────────────────────────────

        /// <summary>
        /// Navegación hacia el usuario propietario del favorito.
        /// </summary>
        public virtual AppUser AppUser { get; set; } = null!;

        /// <summary>
        /// Navegación hacia la criptomoneda marcada como favorita.
        /// </summary>
        public virtual CryptoCurrency CryptoCurrency { get; set; } = null!;
    }
}
