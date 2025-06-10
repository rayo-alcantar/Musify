using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


    public class Cancion
    {
        public ObjectId _id { get; set; }
        public string titulo { get; set; }
        public string artista { get; set; } 
        public string album { get; set; }
        public string genero { get; set; }
        public string momento_dia { get; set; }
        public string estado_animo { get; set; }
        public string actividad { get; set; }
        public string archivo { get; set; }
}
    

