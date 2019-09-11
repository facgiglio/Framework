using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Framework.Helpers;
using System.Data.SqlClient;
using System.Data;
using System.Web.UI;

[assembly: TagPrefix("Samples.AspNet", "Sample")]
namespace Framework.WebControls
{
    
    public class Menu : System.Web.UI.WebControls.WebControl
    {
        private List<Models.Menu> listMenu = new List<Models.Menu>();
        private DataSet data;

        protected override void RenderContents(HtmlTextWriter writer)
        {
            GetMenu();
            string link = "<a href='#URL#'>#DESCRIPCION#</a>";

            //INI - Contenedor de la grilla
            writer.Write("<ul id='cssmenu1'>");

            foreach (Models.Menu padre in listMenu)
            {
                var htmlMenu = "<li>";
                var disponible = false;

                htmlMenu += link.Replace("#URL#", Page.ResolveClientUrl(padre.Url)).Replace("#DESCRIPCION#", padre.Descripcion);

                if (padre.SubMenu.Count > 0)
                {
                    htmlMenu += "<ul>";

                    foreach ( Models.Menu hijo in padre.SubMenu)
                    {
                        if (Security.IsAuthorized(hijo.IdPermiso))
                        {
                            disponible = true;
                            htmlMenu += "<li>" + link.Replace("#URL#", Page.ResolveClientUrl(hijo.Url)).Replace("#DESCRIPCION#", hijo.Descripcion) + "</li>";
                        }
                    }

                    htmlMenu += "</ul>";
                }
                else
                {
                    disponible = true;
                    htmlMenu += "</li>";
                }


                //Si el menú cumple con todas las condiciones, se hace el render en la página.
                if (disponible)
                {
                    writer.Write(htmlMenu);
                }
            }

            writer.Write("</ul>");
        }

        private void GetMenu()
        {
            SqlHelper sqlHelper = new SqlHelper();
            List<SqlParameter> parameters = new List<SqlParameter>();

            Models.Menu menu;

            data = sqlHelper.ExecuteDataSet("ListadoMenu", parameters.ToArray());
            
            foreach (DataRow row in data.Tables[0].Select("IdPadre = 0"))
            {
                menu = new Models.Menu
                {
                    IdMenu = Convert.ToInt32(row["IdMenu"]),
                    IdPadre = Convert.ToInt32(row["IdPermiso"]),
                    IdPermiso = Convert.ToInt32(row["IdPermiso"]),
                    Descripcion = Convert.ToString(row["Descripcion"]),
                    Url = Convert.ToString(row["Url"])
                };

                GetSubMenu(menu);

                listMenu.Add(menu);
            }
        }
        private void GetSubMenu(Models.Menu menuPadre)
        {
            Models.Menu menu;

            menuPadre.SubMenu = new List<Models.Menu>();

            foreach (DataRow row in data.Tables[0].Select("IdPadre = " + menuPadre.IdMenu))
            {
                menu = new Models.Menu
                {
                    IdMenu = Convert.ToInt32(row["IdMenu"]),
                    IdPadre = Convert.ToInt32(row["IdPadre"]),
                    IdPermiso = Convert.ToInt32(row["IdPermiso"]),
                    Descripcion = Convert.ToString(row["Descripcion"]),
                    Url = Convert.ToString(row["Url"]),
                };

                menuPadre.SubMenu.Add(menu);
            }
        }
    }
}