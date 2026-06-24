using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CryptoView.Data;

namespace CryptoView.Controllers
{
    /// <summary>
    /// Controlador API para gestionar notas de usuario asociadas a criptomonedas
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class NotesController : ControllerBase
    {
        private readonly CryptoDbContext _context;
        private readonly ILogger<NotesController> _logger;

        public NotesController(CryptoDbContext context, ILogger<NotesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/Notes/crypto/{cryptoId}
        /// Obtiene todas las notas de una criptomoneda específica
        /// </summary>
        [HttpGet("crypto/{cryptoId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<UserNote>>> GetNotesByCrypto(int cryptoId)
        {
            _logger.LogInformation("GET: Obteniendo notas para criptomoneda {CryptoId}", cryptoId);

            var crypto = await _context.CryptoCurrencies.FindAsync(cryptoId);
            if (crypto == null)
            {
                return NotFound(new { message = $"Criptomoneda con ID {cryptoId} no encontrada" });
            }

            var notes = await _context.UserNotes
                .Where(n => n.CryptoCurrencyId == cryptoId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return Ok(notes);
        }

        /// <summary>
        /// GET: api/Notes/{id}
        /// Obtiene una nota específica
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UserNote>> GetNote(int id)
        {
            _logger.LogInformation("GET: Obteniendo nota {Id}", id);

            var note = await _context.UserNotes.FindAsync(id);
            if (note == null)
            {
                return NotFound(new { message = $"Nota con ID {id} no encontrada" });
            }

            return Ok(note);
        }

        /// <summary>
        /// POST: api/Notes
        /// Crea una nueva nota
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<UserNote>> CreateNote(UserNote note)
        {
            _logger.LogInformation("POST: Creando nueva nota para criptomoneda {CryptoId}", note.CryptoCurrencyId);

            // Verificar que la criptomoneda existe
            var crypto = await _context.CryptoCurrencies.FindAsync(note.CryptoCurrencyId);
            if (crypto == null)
            {
                return BadRequest(new { message = $"Criptomoneda con ID {note.CryptoCurrencyId} no encontrada" });
            }

            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;

            _context.UserNotes.Add(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Nota creada con ID {Id}", note.Id);

            return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
        }

        /// <summary>
        /// PUT: api/Notes/{id}
        /// Actualiza una nota existente
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNote(int id, UserNote note)
        {
            _logger.LogInformation("PUT: Actualizando nota {Id}", id);

            if (id != note.Id)
            {
                return BadRequest(new { message = "El ID no coincide" });
            }

            var existingNote = await _context.UserNotes.FindAsync(id);
            if (existingNote == null)
            {
                return NotFound(new { message = $"Nota con ID {id} no encontrada" });
            }

            existingNote.Title = note.Title;
            existingNote.Content = note.Content;
            existingNote.Priority = note.Priority;
            existingNote.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Nota {Id} actualizada", id);

            return NoContent();
        }

        /// <summary>
        /// DELETE: api/Notes/{id}
        /// Elimina una nota
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteNote(int id)
        {
            _logger.LogInformation("DELETE: Eliminando nota {Id}", id);

            var note = await _context.UserNotes.FindAsync(id);
            if (note == null)
            {
                return NotFound(new { message = $"Nota con ID {id} no encontrada" });
            }

            _context.UserNotes.Remove(note);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Nota {Id} eliminada", id);

            return NoContent();
        }
    }
}
