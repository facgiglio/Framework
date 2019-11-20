using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;

namespace Framework.Helpers
{
    public static class Connection
    {
        private static SqlConnection conn;
        private static SqlConnection connMaster;

        /*
        private static string connStringHome = @"Server=FACAXNOTEBOOK\FACAXSQL;Database=ChatBotTFI;User Id=sa; Password=1024;";
        private static string masterConnStringHome = @"Server=FACAXNOTEBOOK\FACAXSQL;Database=master;User Id=sa; Password=1024;";
        private static string connStringWork = @"Server=GSNBK016;Database=ChatbotTFI;User Id=userConn; Password=1024;";
        private static string masterConnStringWork = @"Server=GSNBK016;Database=master;User Id=sa; Password=1024;";
        private static string connStringHomePc = @"Server=DESKTOP-HEFB77K;Database=ChatBotTFI;User Id=sa; Password=1024;";
        private static string masterConnStringHomePc = @"Server=DESKTOP-HEFB77K;Database=master;User Id=sa; Password=1024;";
        */
        public static SqlConnection GetSQLConnection()
        {
            if (conn is null || conn.ConnectionString == "")
            {   
                conn = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString);
            }

            return conn;
        }

        public static SqlConnection GetMasterSQLConnection()
        {
            if (connMaster is null || connMaster.ConnectionString == "")
            {
                connMaster = new SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["ConnectionStringMaster"].ConnectionString);
            }
            return connMaster;
        }
    }
}