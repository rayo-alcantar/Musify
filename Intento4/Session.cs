using MongoDB.Bson;

namespace Intento4
{
    /// <summary>
    /// Almacena los datos de la sesión del usuario que ha iniciado sesión.
    /// Se usa en toda la aplicación para identificar al propietario de playlists,
    /// filtrar contenido privado, etc.
    /// </summary>
    public static class Session
    {
        /// <summary>
        /// Identificador único del usuario en MongoDB.
        /// </summary>
        public static ObjectId UserId { get; set; }

        /// <summary>
        /// Nombre del usuario (para mostrar en la interfaz).
        /// </summary>
        public static string UserName { get; set; } = string.Empty;
    }
}