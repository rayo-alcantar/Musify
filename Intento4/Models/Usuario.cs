using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

    public class Usuario
    {
        public ObjectId _id { get; set; }
        public string nombre { get; set; }
        public string correo_electronico { get; set; }
        public string contraseña { get; set; }

    [BsonRepresentation(BsonType.String)]
        public DateOnly fecha_nacimiento {get; set;} 
        public Array genero { get; set; }

    }

