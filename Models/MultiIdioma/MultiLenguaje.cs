using Framework.Models.Attributes;

namespace Framework.Models
{
    public class MultiLenguaje
    {
        [PrimaryKey]
        public int IdMultiLenguaje { get; set; }
        [Insertable, Updatable]
        public int IdSeccion { get; set; }
        [Insertable, Updatable]
        public string Descripcion { get; set; }
    }

    public class DTO_MultiLengjuaje
    {
        #region Propertys
        public int IdMultiLenguaje { get; set; }
        public string Descripcion { get; set; }
        public string Seccion { get; set; }
        public string IdEs { get; set; }
        public string es { get; set; }
        public string IdEn { get; set; }
        public string en { get; set; }
        #endregion
    }
}