using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes; // Required for BsonId
using System.Collections.Generic;

/// <summary>
/// Representa una playlist creada por un usuario.
/// </summary>
public class Playlist
{
    /// <summary>
    /// Identificador único de la playlist en MongoDB.
    /// </summary>
    [BsonId] // Atributo para marcar como clave primaria de MongoDB
    [BsonRepresentation(BsonType.ObjectId)] // Especifica cómo se debe serializar el ObjectId
    public ObjectId _id { get; set; }

    /// <summary>
    /// ID del usuario (de la colección Usuario) que es propietario de esta playlist.
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)]
    public ObjectId usuarioId { get; set; }

    /// <summary>
    /// Nombre de la playlist, asignado por el usuario o generado automáticamente.
    /// </summary>
    public string nombre { get; set; }

    /// <summary>
    /// Lista de IDs de canciones (de la colección Cancion o de GridFS) que pertenecen a esta playlist.
    /// </summary>
    [BsonRepresentation(BsonType.ObjectId)] // Asegura que los ObjectIds en la lista se serialicen correctamente
    public List<ObjectId> canciones { get; set; }
}
