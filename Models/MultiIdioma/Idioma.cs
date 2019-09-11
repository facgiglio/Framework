using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Models.Attributes;

namespace Framework.Models
{
    public class Idioma
    {
        public int IdIdioma { get; set; }
        [Insertable, Updatable]
        public string Iso { get; set; }
        [Insertable, Updatable]
        public string Descripcion { get; set; }
    }
}