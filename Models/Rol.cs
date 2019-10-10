using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Framework.Models.Attributes;

namespace Framework.Models
{
    public class Rol
    {
        [PrimaryKey]
        public int IdRol { get; set; }

        [Insertable, Updatable]
        public string Descripcion { get; set; }
    }
}