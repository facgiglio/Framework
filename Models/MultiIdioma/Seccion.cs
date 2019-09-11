using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Models.Attributes;

namespace Framework.Models
{
    public class Seccion
    {
        public int IdSeccion { get; set; }
        [Insertable, Updatable]
        public string Descripcion { get; set; }
        [Insertable, Updatable]
        public int IdUsuario { get; set; }
    }
}