using System;
using System.Collections.Generic;
using Framework.Models.Attributes;

namespace Framework.Models
{
    public class Usuario
    {
        [PrimaryKey]
        public int IdUsuario { get; set; }

        [Insertable, Updatable]
        public int IdIdioma { get; set; }

        [EntityMany("UsuarioRol", "Rol", "IdUsuario", "IdRol")]
        public List<Models.Rol> Roles { get; set; }

        [Insertable, Updatable]
        public string Email { get; set; }

        [Insertable, Updatable]
        public string Nombre { get; set; }

        [Insertable, Updatable]
        public string Apellido { get; set; }

        [Insertable]
        public string Contrasena { get; set; }

        [Insertable, Updatable]
        public Int16 IntentosFallidos { get; set; }

        [Insertable]
        public DateTime FechaAlta { get; set; }

    }
}
