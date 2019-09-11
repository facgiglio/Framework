using System;
using Framework.Models.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Models
{
    public class Bitacora
    {        
        [PrimaryKey]
        public string IdBitacora { get; set; }
        [Insertable]
        public DateTime Fecha { get; set; }
        [Insertable]
        public string Mensaje { get; set; }
        [Insertable]
        public string Criticidad { get; set; }
        [Insertable]
        public int IdUsuario { get; set; }
        
    }
}