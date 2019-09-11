using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Framework.Helpers;

namespace Framework.Rules
{
    public class Bitacora
    {
        Mapper<Models.Bitacora> sqlHelper = new Mapper<Models.Bitacora>();

        #region Insertar
        public void Insertar(Models.Bitacora bitacora)
        {
            if (bitacora.Criticidad == null)
                bitacora.Criticidad = "Baja";

            bitacora.Fecha = DateTime.Now;

            sqlHelper.Insert(bitacora);
        }
        #endregion

        #region Get
        public Models.Bitacora GetById(int Id)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@Id", Id)
            };

            return sqlHelper.GetByWhere(parameters.ToArray());
        }

        public List<Models.Bitacora> GetListBitacora(DateTime fechaDesde, DateTime fechaHasta, string idUsuario, string mensaje, List<string> criticidades)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@Fecha", fechaDesde),
                new SqlParameter("@Fecha", fechaHasta),
                new SqlParameter("@IdUsuario", idUsuario),
                new SqlParameter("@Mensaje", mensaje)/*,
                new SqlParameter("@Citicidad", criticidades.ToArray())*/
            };

            return sqlHelper.GetListEntity(parameters.ToArray());
        }
        #endregion

    }
}
