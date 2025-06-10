using System;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Configuration;
using System.IO;
using MongoDB.Driver.GridFS;

// Define el espacio de nombres si es necesario, aunque en el original no estaba.
// namespace Intento4.Servicios { }

/// <summary>
/// Servicio para interactuar con la base de datos MongoDB.
/// Proporciona métodos para operaciones CRUD en usuarios, canciones, playlists y archivos de canciones en GridFS.
/// </summary>
public class MongoDBService
{
    /// <summary>
    /// Cadena de conexión centralizada para MongoDB Atlas.
    /// Usada por toda la aplicación para conectarse a la base de datos.
    /// </summary>
    public static readonly string ConnectionString = "mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority";

    // Nombre de la base de datos. Podría también ser una constante estática si se desea.
    private const string DatabaseName = "Musify";

    private IGridFSBucket _gridFSBucket;  // Para operaciones con archivos grandes (canciones).
    private IMongoDatabase _database;     // Representa la base de datos.
    private IMongoCollection<Usuario> _usuariosCollection; // Colección de usuarios.
    private IMongoCollection<Cancion> _cancionesCollection; // Colección de metadatos de canciones (si se usa además de GridFS).
    private IMongoCollection<Playlist> _playlistsCollection; // Colección de playlists.

    /// <summary>
    /// Inicializa una nueva instancia de MongoDBService.
    /// Establece la conexión con la base de datos y obtiene las colecciones.
    /// </summary>
    public MongoDBService()
    {
        var client = new MongoClient(ConnectionString); // Usa la cadena de conexión centralizada.
        _database = client.GetDatabase(DatabaseName);

        _gridFSBucket = new GridFSBucket(_database);

        _usuariosCollection = _database.GetCollection<Usuario>("Usuario");
        _cancionesCollection = _database.GetCollection<Cancion>("Cancion"); // Asumiendo que 'Cancion' es el nombre de la colección.
        _playlistsCollection = _database.GetCollection<Playlist>("Playlist"); // Asumiendo que 'Playlist' es el nombre de la colección.
    }

    // --- Métodos de GridFS (Canciones) ---

    /// <summary>
    /// Sube un archivo de canción a MongoDB GridFS.
    /// </summary>
    /// <param name="filePath">Ruta local del archivo de canción.</param>
    /// <param name="fileName">Nombre con el que se guardará el archivo en GridFS (puede incluir metadatos si se configura UploadOptions).</param>
    public void UploadSong(string filePath, string fileName)
    {
        using (var stream = File.OpenRead(filePath))
        {
            // Considerar añadir metadatos (título, artista, álbum) aquí usando GridFSUploadOptions
            _gridFSBucket.UploadFromStream(fileName, stream);
        }
    }

    /// <summary>
    /// Descarga un archivo de canción desde MongoDB GridFS.
    /// </summary>
    /// <param name="fileName">Nombre del archivo a descargar de GridFS.</param>
    /// <returns>Un Stream con los datos de la canción.</returns>
    public Stream DownloadSong(string fileName)
    {
        var fileStream = new MemoryStream();
        _gridFSBucket.DownloadToStreamByName(fileName, fileStream);
        fileStream.Seek(0, SeekOrigin.Begin); // Rebobina el stream para su lectura.
        return fileStream;
    }

    // --- Métodos de Usuario ---
    public void GuardarUsuario(Usuario usuario)
    {
        _usuariosCollection.InsertOne(usuario);
    }

    public Usuario ObtenerUsuarioCorreo(string correo)
    {
        var filter = Builders<Usuario>.Filter.Eq(u => u.correo_electronico, correo);
        return _usuariosCollection.Find(filter).FirstOrDefault();
    }

    // --- Métodos de Cancion (metadatos, si aplica) ---
    public List<Cancion> ObtenerCanciones()
    {
        // Esto obtendría metadatos de una colección 'Cancion'.
        // Si las canciones solo están en GridFS, este método podría necesitar lógica diferente
        // o ser eliminado si no se usa una colección separada para metadatos.
        return _cancionesCollection.Find(c => true).ToList();
    }

    public void AgregarCancion(Cancion cancion)
    {
        _cancionesCollection.InsertOne(cancion);
    }

    public void ActualizarCancion(Cancion cancion)
    {
        var filtro = Builders<Cancion>.Filter.Eq(c => c._id, cancion._id);
        _cancionesCollection.ReplaceOne(filtro, cancion);
    }

    public void EliminarCancion(string titulo)
    {
        var filtro = Builders<Cancion>.Filter.Eq(c => c.titulo, titulo);
        _cancionesCollection.DeleteOne(filtro);
    }

    // --- Métodos de Playlist ---

    /// <summary>
    /// Guarda una nueva playlist en la base de datos.
    /// </summary>
    /// <param name="playlist">La playlist a guardar.</param>
    public void GuardarPlaylist(Playlist playlist)
    {
        _playlistsCollection.InsertOne(playlist);
    }

    /// <summary>
    /// Obtiene todas las playlists pertenecientes a un usuario específico.
    /// </summary>
    /// <param name="usuarioId">El ID del usuario cuyas playlists se quieren obtener.</param>
    /// <returns>Una lista de playlists del usuario.</returns>
    public List<Playlist> ObtenerPlaylistsPorUsuario(ObjectId usuarioId)
    {
        return _playlistsCollection.Find(p => p.usuarioId == usuarioId).ToList();
    }

    /// <summary>
    /// Actualiza una playlist existente en la base de datos.
    /// Reemplaza el documento completo de la playlist con el objeto playlist proporcionado.
    /// </summary>
    /// <param name="playlist">La playlist con los datos actualizados.</param>
    public void ActualizarPlaylist(Playlist playlist)
    {
        var filter = Builders<Playlist>.Filter.Eq(p => p._id, playlist._id);
        // Reemplaza el documento de la playlist existente que coincide con el filtro.
        // Esto es útil para actualizar la lista de canciones, el nombre, etc.
        var result = _playlistsCollection.ReplaceOne(filter, playlist);

        // Opcional: Verificar result.ModifiedCount para confirmar si la actualización tuvo efecto.
        // if (result.ModifiedCount == 0) { /* Log o manejar caso donde no se modificó */ }
    }
}
