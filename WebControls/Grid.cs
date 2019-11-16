using System.Collections.Generic;
using System.Web.UI;
using System.Text;
using Newtonsoft.Json;
using System;
using System.Globalization;

[assembly: TagPrefix("Samples.AspNet", "Sample")]
namespace Framework.WebControls
{

    #region Column With Enum
    public enum ColumnWith
    {
        column1, column2, column3, column4, column5, column6,
        column7, column8, column9, column10, column11, column12,
        column13, column14, column15, column16, column17, column18,
        column19, column20, column21, column22, column23, column24

    }
    #endregion

    #region Column Type Enum
    public enum ColumnType
    {
        Data, CheckBox, TextBox, Datetime
    }
    #endregion

    public class GridConfig
    {
        public List<GridColumn> Columns { get; set; }
        public List<GridContextMenu> ContextMenus { get; set; }

        public bool Condense { get; set; }
        public GridConfig()
        {
            Condense = false;
            Columns = new List<GridColumn>();
            ContextMenus = new List<GridContextMenu>();
        }
    }
    public class GridColumn
    {
        public string Title { get; set; }
        public ColumnWith Width { get; set; }
        public ColumnType Type { get; set; }
        public string Expression { get; set; }
        public string ExpressionId { get; set; }
        public bool IsAccesible { get; set; }
        public bool IsVisible { get; set; }
    }
    public class GridContextMenu
    {
        public string Modo { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public string IdMenu { get; set; }
        public string Modal { get; set; }
        public string Descripcion { get; set; }
        public string OnClick { get; set; }
    }

    public class Grid : System.Web.UI.WebControls.WebControl
    {
        #region Properties
        //private List<GridColumn> Columns { get; set; }
        //private List<GridContextMenu> ContextMenus { get; set; }

        public GridConfig Config{ get; set; }
        public List<object> DataSource { get; set; }
        #endregion

        public Grid()
        {
            Config = new GridConfig();
        }

        #region AddContextMenu

        public void AddContextMenu(string idMenu, string descripcion, string modo, string icon, string color, string modal)
        {
            GridContextMenu contextMenu = new GridContextMenu
            {
                IdMenu = idMenu,
                Descripcion = descripcion,
                Modo = modo,
                Icon = icon,
                Color = color,
                Modal = modal,
                OnClick = ""
            };

            Config.ContextMenus.Add(contextMenu);
        }

        public void AddContextMenuClick(string idMenu, string descripcion, string modo, string icon, string color, string onClick)
        {
            GridContextMenu contextMenu = new GridContextMenu
            {
                IdMenu = idMenu,
                Descripcion = descripcion,
                Modo = modo,
                Icon = icon,
                Color = color,
                Modal = "",
                OnClick = onClick
            };

            Config.ContextMenus.Add(contextMenu);
        }

        #endregion

        #region AddColumn
        public void AddColumn(string title, ColumnType type, string expression, string expressionId, bool isAccesible, bool isVisible)
        {
            GridColumn column = new GridColumn
            {
                Title = title,
                Type = type,
                Expression = expression,
                ExpressionId = expressionId,
                IsAccesible = isAccesible,
                IsVisible = isVisible
            };

            Config.Columns.Add(column);
        }

        public void AddColumn(string title, ColumnWith width, ColumnType type, string expression, bool isAccesible, bool isVisible)
        {

            GridColumn column = new GridColumn
            {
                Title = title,
                Width = width,
                Type = type,
                Expression = expression,
                IsAccesible = isAccesible,
                IsVisible = isVisible
            };

            Config.Columns.Add(column);
        }

        #endregion

        #region Render Control
        public override void RenderBeginTag(HtmlTextWriter writer)
        {
            writer.Write("<span id='" + this.ID + "_Container'>");
        }

        public override void RenderEndTag(HtmlTextWriter writer)
        {
            writer.Write("</span>");
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            writer.Write(Render());

            /*
            writer.Write(ToScript(
                this.ID + "Config = " + JsonConvert.SerializeObject(this.Config) + ";" +
                this.ID + " = new Grid('" + this.ID + "');" + System.Environment.NewLine +
                this.ID + "._constructor()"
            ));
            */
        }

        public string RenderContextMenu()
        {
            var contextMenu = "<ul class=\"dropdown-menu\" id=\"" + this.ID + "CM\" aria-type=\"context-menu\">{item}</ul>";
            var item = "<li id=\"{0}\" data-toggle=\"modal\" data-target=\"#{1}\" data-mode=\"{2}\" onclick=\"{6}\"><a href=\"#\"><span class=\"{3}\" style=\"color: {4}; width: 25px\"></span><span runat=\"server\"></span>{5}</a></li>";

            foreach (var menu in Config.ContextMenus)
            {
                contextMenu = contextMenu.Replace("{item}", string.Format(item, menu.IdMenu, menu.Modal, menu.Modo, menu.Icon, menu.Color, menu.Descripcion, menu.OnClick) + "{item}");
            }
            
            return contextMenu.Replace("{item}", "");
        }

        public string Render()
        {
            StringBuilder writer = new StringBuilder("");

            writer.Append(RenderContextMenu());

            //1 - INI - Contenedor de la grilla
            writer.Append("<div class=\"table-responsive\">");

            //2 - INI - Tabla
            writer.Append("<table class=\"table table-striped " + (Config.Condense ? "table-condensed" : "") + " table-hover\" id=\"" + this.ID + "\">");

            //3 - INI - Header
            writer.Append("<thead>");
            writer.Append("<tr>");

            //4 - INI - Encabezado de la grilla.
            foreach (GridColumn column in Config.Columns)
            {
                if (column.IsVisible)
                    writer.Append("<th>" + column.Title + "</th>");
                //writer.Append("  <div class=\"column " + column.Width + "\" style=\"text-align:center\">" + column.Title + "</div>");
            }
            //4 - FIN - Encabezado de la grilla.

            //3 - FIN - Header
            writer.Append("</thead>");
            writer.Append("</tr>");

            //3 - INI - Body
            writer.Append("<tbody>");

            foreach (object row in DataSource)
            {
                //4 - INI - Json Build
                string json = "";
                string jsonProp = "\"{prop}\":\"{val}\"";

                foreach (GridColumn column in Config.Columns)
                {
                    if (column.IsAccesible)
                    {
                        string data = row.GetType().GetProperty(column.Expression).GetValue(row, null).ToString();

                        jsonProp.Replace("{prop}", column.Expression);
                        json += (json != "" ? "," : "").ToString() + jsonProp.Replace("{prop}", column.Expression).Replace("{val}", data.Replace(@"\", @"\\"));
                    }
                }

                json = "{" + json + "}";
                //4 - FIN - Json Build

                //4 - INI -  Row
                writer.Append("<tr " + (json == "{}" ? "" : "rowData='" + json + "'").ToString() + ">");

                foreach (GridColumn column in Config.Columns)
                {
                    if (column.IsVisible)
                    {
                        var id = "";
                        var value = "";
                        var contenido = "";

                        switch (column.Type)
                        {
                            case ColumnType.Datetime:
                                contenido = row.GetType().GetProperty(column.Expression).GetValue(row, null).ToString();
                                contenido = Convert.ToDateTime(contenido).ToString(new CultureInfo("es-AR")).ToString();

                                writer.Append("<td style =\"text-align:left\">" + contenido + "</td>");
                                break;
                                
                            case ColumnType.Data:
                                contenido = row.GetType().GetProperty(column.Expression).GetValue(row, null).ToString();
                                writer.Append("<td style =\"text-align:left\">" + contenido + "</td>");
                                break;
                            case ColumnType.CheckBox:
                                id = "chk_" + row.GetType().GetProperty(column.ExpressionId).GetValue(row, null).ToString();
                                value = row.GetType().GetProperty(column.Expression).GetValue(row, null).ToString();
                                contenido = "<input type=\"checkbox\" id=\"" + id + "\" >";

                                writer.Append("<td style =\"text-align:left\">" + contenido + "</td>");
                                break;
                            case ColumnType.TextBox:
                                id = "txtTraduccion_" + row.GetType().GetProperty(column.ExpressionId).GetValue(row, null).ToString();
                                value = row.GetType().GetProperty(column.Expression).GetValue(row, null).ToString();
                                contenido = "<input type=\"text\" class=\"form-control form-control-sm\" id=\"" + id + "\" value=\"" + value + "\" />";

                                writer.Append("<td style =\"text-align:left\">" + contenido + "</td>");
                                break;

                        }

                    }
                }
                //4 - FIN -  Row
                writer.Append("</tr>");
            }

            //3 - FIN -  Body
            writer.Append("</tbody>" + System.Environment.NewLine);

            //2 - FIN - Tabla
            writer.Append("</table>" + System.Environment.NewLine);

            //1 - FIN - Contenedor de la grilla
            writer.Append("</div>" + System.Environment.NewLine);

            //Creo el control IUGrid en JavaScript.
            writer.Append(ToScript(
                "var " + this.ID + "Config = " + JsonConvert.SerializeObject(this.Config) + ";" +
                "var " + this.ID + " = new Grid('" + this.ID + "');" + System.Environment.NewLine +
                this.ID + "._constructor()"
            ));

            return writer.ToString();
        }
        #endregion

        #region To Script
        public string ToScript(string pCadena)
        {
            string mSalida = "";

            mSalida += "<script language='javascript'>" + System.Environment.NewLine + "<!--" + System.Environment.NewLine;
            mSalida += pCadena;
            mSalida += "//-->" + System.Environment.NewLine + "</script>" + System.Environment.NewLine;

            return mSalida;
        }
        #endregion
    }


}