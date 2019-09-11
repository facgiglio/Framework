using System;

namespace Framework.Models.Attributes
{

    public class TableName : Attribute
    {
        public string DbTableName { get; set; }

        public TableName(string tableName)
        {
            DbTableName = tableName;
        }
    }

    public class PrimaryKey : Attribute { }
    public class Insertable : Attribute { }
    public class Updatable : Attribute { }
    public class Entity : Attribute { }
    public class EntityMany : Attribute
    {
        public string TableMany { get; set; }
        public string TableRela { get; set; }
        public string Field1 { get; set; }
        public string Field2 { get; set; }

        public EntityMany(string tableMany, string tableRela, string field1, string field2)
        {
            TableMany = tableMany;
            TableRela = tableRela;
            Field1 = field1;
            Field2 = field2;
        }
    }
    public class Timestamp : Attribute { }
    public class Uppercase : Attribute { }
}