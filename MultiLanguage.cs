using Framework;
using Framework.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Web;
using System.Linq;

namespace Framework
{
    public class MultiLanguage
    {
        public static List<Models.DTO_MultiLengjuaje> translates()
        {
            if (HttpContext.Current.Cache["MultiLanguage"] is null)
            {
                HttpContext.Current.Cache["MultiLanguage"] = GetMultiLanguageList2(0);
            }

            return (List<Models.DTO_MultiLengjuaje>)HttpContext.Current.Cache["MultiLanguage"];
        }

        public static void RefreshCache()
        {
            HttpContext.Current.Cache.Remove("MultiLanguage");
        }

        public static List<object> GetMultiLanguageList(int idSection)
        {
            SqlHelper sqlHelper = new SqlHelper();
             
            List<object> multiLanguage = new List<object>();
            List<SqlParameter> parameters = new List<SqlParameter>();

            if (idSection != 0) parameters.Add(new SqlParameter("@IdSeccion", idSection));

            DataSet data = sqlHelper.ExecuteDataSet("ListadoMultiIdioma", parameters.ToArray());

            foreach (DataRow row in data.Tables[0].Rows)
            {
                multiLanguage.Add(new Framework.Models.DTO_MultiLengjuaje
                {
                    IdMultiLenguaje = Convert.ToInt32(row["IdMultiLenguaje"]),
                    Descripcion = Convert.ToString(row["Descripcion"]),
                    Seccion = Convert.ToString(row["Seccion"]),
                    IdEs = Convert.ToString(row["IdEs"]),
                    es = Convert.ToString(row["es"]),
                    IdEn = Convert.ToString(row["IdEn"]),
                    en = Convert.ToString(row["en"])
                });
            }

            return multiLanguage;
        }

        public static List<Models.DTO_MultiLengjuaje> GetMultiLanguageList2(int idSection)
        {
            SqlHelper sqlHelper = new SqlHelper();

            List<Models.DTO_MultiLengjuaje> multiIdioma = new List<Models.DTO_MultiLengjuaje>();
            List<SqlParameter> parameters = new List<SqlParameter>();

            if (idSection != 0) parameters.Add(new SqlParameter("@IdSeccion", idSection));

            DataSet data = sqlHelper.ExecuteDataSet("ListadoMultiIdioma", parameters.ToArray());

            foreach (DataRow row in data.Tables[0].Rows)
            {
                multiIdioma.Add(new Framework.Models.DTO_MultiLengjuaje
                {
                    IdMultiLenguaje = Convert.ToInt32(row["IdMultiLenguaje"]),
                    Descripcion = Convert.ToString(row["Descripcion"]),
                    Seccion = Convert.ToString(row["Seccion"]),
                    IdEs = Convert.ToString(row["IdEs"]),
                    es = Convert.ToString(row["es"]),
                    IdEn = Convert.ToString(row["IdEn"]),
                    en = Convert.ToString(row["en"])
                });
            }

            return multiIdioma;
        }
        public static string GetTranslate(string key)
        {
            return GetTranslate("Generico", key);
        }
        public static string GetTranslate(string section, string key)
        {
            var translate = translates().Where(w => w.Seccion == section && w.Descripcion == key).FirstOrDefault();

            //Si no encuentro la clave en el diccionario, devuelvo la entrada.
            if (translate == null) return key;

            //Si no hay nadie logueado, devuelvo español.
            if (Session.User is null) return translate.es; //TODO: poder cambiar el idioma si no está logueado. Crear un User sin log. 

            //Devuelvo la traducción correspondiente.
            switch (Session.User.IdIdioma)
            {
                case 1:
                    return translate.es;
                case 2:
                    return translate.en;
                default:
                    return translate.en;
            }

        }
    }

}



