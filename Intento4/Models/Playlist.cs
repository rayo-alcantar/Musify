using MongoDB.Bson;
using System.Collections.Generic;

public class Playlist
{
    public ObjectId _id { get; set; }
    public ObjectId usuarioId { get; set; } // Referencia al usuario dueño de la playlist
    public string nombre { get; set; } // Nombre de la playlist
    public List<ObjectId> canciones { get; set; } // Lista de IDs de canciones
}
