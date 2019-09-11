using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Models.Attributes;

namespace Framework.Models
{
    public class Traduccion
    {
        [PrimaryKey]
        public int IdTraduccion { get; set; }
        [Insertable, Updatable]
        public int IdMultiIdioma { get; set; }
        [Insertable, Updatable]
        public int Ididioma { get; set; }
        [Insertable, Updatable]
        public string Texto { get; set; }
    }
}