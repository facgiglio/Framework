using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Models
{
    public class Menu
    {
        public int IdMenu { get; set; }
        public int IdPadre { get; set; }
        public List<Menu> SubMenu { get; set; }
        public int IdPermiso     { get; set; }
        public string Descripcion { get; set; }
        public string Url { get; set; }
        public bool Activo { get; set; }
    }
}