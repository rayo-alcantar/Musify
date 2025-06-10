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



public class MongoDBService
{
    private IGridFSBucket _gridFSBucket;  // Variable de GridFS
    private IMongoDatabase _database;
    private IMongoCollection<Usuario> _usuariosCollection;
    private IMongoCollection<Cancion> _cancionesCollection;
    private IMongoCollection<Playlist> _playlistsCollection;

    public MongoDBService()
    {
        string connectionString = "mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority"; // Actualiza con tus credenciales y base de datos
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase("Musify"); // Nombre de la base de datos

        // Inicializar GridFSBucket
        _gridFSBucket = new GridFSBucket(_database); // Inicializar _gridFSBucket

        _usuariosCollection = _database.GetCollection<Usuario>("Usuario");
        _cancionesCollection = _database.GetCollection<Cancion>("Cancion");
        _playlistsCollection = _database.GetCollection<Playlist>("Playlist");
    }

    // Método para subir una canción a GridFS
    public void UploadSong(string filePath, string fileName)
    {
        // Abrimos el archivo en modo de lectura
        using (var stream = File.OpenRead(filePath))
        {
            // Subimos el archivo a GridFS
            _gridFSBucket.UploadFromStream(fileName, stream);
        }
    }


    // Método para obtener una canción desde GridFS
    public Stream DownloadSong(string fileName)
    {
        var fileStream = new MemoryStream();
        _gridFSBucket.DownloadToStreamByName(fileName, fileStream);
        fileStream.Seek(0, SeekOrigin.Begin);
        return fileStream;
    }

    public void GuardarUsuario(Usuario usuario)
    {
        _usuariosCollection.InsertOne(usuario);
    }

    public Usuario ObtenerUsuarioCorreo(string correo)
    {
        var filter = Builders<Usuario>.Filter.Eq(u => u.correo_electronico, correo);
        return _usuariosCollection.Find(filter).FirstOrDefault();
    }

    public List<Cancion> ObtenerCanciones()
    {
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

    public void GuardarPlaylist(Playlist playlist)
    {
        _playlistsCollection.InsertOne(playlist);
    }

    public List<Playlist> ObtenerPlaylistsPorUsuario(ObjectId usuarioId)
    {
        return _playlistsCollection.Find(p => p.usuarioId == usuarioId).ToList();
    }
}
