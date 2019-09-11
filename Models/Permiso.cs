using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Framework.Models.Attributes;

namespace Framework.Models
{
    public class Permiso
    {
        [PrimaryKey]
        public int IdPermiso { get; set; }
        [Insertable, Updatable]
        public string Descripcion { get; set; }
    }
}