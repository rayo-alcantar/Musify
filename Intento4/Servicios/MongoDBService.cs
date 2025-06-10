using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Intento4   // ← el archivo estaba sin namespace; lo añadimos
{
    public class MongoDBService
    {
        private readonly IGridFSBucket _gridFSBucket;
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<Usuario>  _usuariosCollection;
        private readonly IMongoCollection<Cancion>  _cancionesCollection;
        private readonly IMongoCollection<Playlist> _playlistsCollection;

        public MongoDBService()
        {
            const string conn =
                "mongodb+srv://Musify:Spiderman123@cluster0.ok95e.mongodb.net/Musify?retryWrites=true&w=majority";

            var client  = new MongoClient(conn);
            _database   = client.GetDatabase("Musify");

            _gridFSBucket        = new GridFSBucket(_database);
            _usuariosCollection  = _database.GetCollection<Usuario>("Usuario");
            _cancionesCollection = _database.GetCollection<Cancion>("Cancion");
            _playlistsCollection = _database.GetCollection<Playlist>("Playlist");
        }

        /* ============  CANCIONES  ============ */

        public void UploadSong(string filePath, string fileName)
        {
            using var stream = File.OpenRead(filePath);
            _gridFSBucket.UploadFromStream(fileName, stream);
        }

        public Stream DownloadSong(string fileName)
        {
            var ms = new MemoryStream();
            _gridFSBucket.DownloadToStreamByName(fileName, ms);
            ms.Position = 0;
            return ms;
        }

        public List<Cancion> ObtenerCanciones() =>
            _cancionesCollection.Find(_ => true).ToList();

        public void AgregarCancion(Cancion c) =>
            _cancionesCollection.InsertOne(c);

        public void ActualizarCancion(Cancion c) =>
            _cancionesCollection.ReplaceOne(x => x._id == c._id, c);

        public void EliminarCancion(string titulo) =>
            _cancionesCollection.DeleteOne(c => c.titulo == titulo);

        /* ============  USUARIOS  ============ */

        public void GuardarUsuario(Usuario u) =>
            _usuariosCollection.InsertOne(u);

        public Usuario? ObtenerUsuarioCorreo(string correo) =>
            _usuariosCollection.Find(u => u.correo_electronico == correo).FirstOrDefault();

        /* ============  PLAYLISTS  ============ */

        public void GuardarPlaylist(Playlist p) =>
            _playlistsCollection.InsertOne(p);

        /// <summary>
        /// Guarda una playlist privada para el usuario en sesión.
        /// </summary>
        public void GuardarPlaylistPrivada(string nombre, IEnumerable<ObjectId> canciones)
        {
            var playlist = new Playlist
            {
                _id       = ObjectId.GenerateNewId(),
                usuarioId = Session.UserId,   // Session viene del mismo namespace
                nombre    = nombre,
                canciones = canciones.ToList()
            };
            GuardarPlaylist(playlist);
        }

        public List<Playlist> ObtenerPlaylistsPorUsuario(ObjectId usuarioId) =>
            _playlistsCollection.Find(p => p.usuarioId == usuarioId).ToList();
    }
}
