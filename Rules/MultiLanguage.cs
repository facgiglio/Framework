using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Framework.Helpers;

namespace Framework.Rules
{
    public class MultiLenguaje
    {
        Mapper<Models.MultiLenguaje> mapper = new Mapper<Models.MultiLenguaje>();

        #region Insertar
        public void Insertar(Models.MultiLenguaje multiLanguage)
        {
            mapper.Insert(multiLanguage);
            //(new Integridad()).SaveDV(Integridad.Tablas.MultiIdioma);
        }
        #endregion

        #region Modificar
        public void Modificar(Models.MultiLenguaje multiLanguage)
        {
            mapper.Update(multiLanguage);
        }
        #endregion

        #region Eliminar
        public void Eliminar(int Id)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@IdMultiLenguaje", Id)
            };

            mapper.Delete(parameters.ToArray());
        }
        #endregion

        #region Get
        public Framework.Models.MultiLenguaje GetById(int Id)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@IdMultiLenguaje", Id)
            };

            return mapper.GetByWhere(parameters.ToArray());
        }
        public List<object> GetList()
        {
            return mapper.GetList(null);
        }
        #endregion

        #region GetMultiLanguageList
        public List<object> GetMultiLanguageList(int idSeccion)
        {
            return Framework.MultiLanguage.GetMultiLanguageList(idSeccion);
        }
        #endregion
    }
}
