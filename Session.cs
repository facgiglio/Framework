using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Framework.Helpers
{
    public class Session
    {
        public static Models.Usuario SessionUser
        {
            get
            {
                Models.Usuario usuario = null;
                if (HttpContext.Current.Session["SessionUser"] != null)
                {
                    usuario = (Models.Usuario)HttpContext.Current.Session["SessionUser"];
                }
                return usuario;                    
            }
        }

        public static void AddSessionUser(Framework.Models.Usuario usuario)
        {
            HttpContext.Current.Session.Add("SessionUser", usuario);
        }

        public static void LogOut()
        {
            Models.Usuario sessionUser = (Models.Usuario)HttpContext.Current.Session["SessionUser"];

            if (sessionUser == null)
            {
                HttpContext.Current.Session.Clear();
                HttpContext.Current.Response.Redirect(getAbsolutePath("aspxs/start/logout.aspx"), true);
            }
        }

        public static void ClearSession()
        {
            HttpContext.Current.Session.Clear();
        }

        //retornar la ruta abosula de un archivo
        public static string getAbsolutePath(String file)
        {
            String end = (HttpContext.Current.Request.ApplicationPath.EndsWith("/")) ? "" : "/";
            String path = HttpContext.Current.Request.ApplicationPath + end;

            return String.Format("http://{0}{1}{2}", HttpContext.Current.Request.Url.Authority, path, file);
        }
    }
}