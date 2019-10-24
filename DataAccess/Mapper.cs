using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Reflection;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Models.Attributes;

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Framework.Helpers
{

    public class BaseMapper
    {
        #region Query Cons
        protected const string GetManyQuery = "SELECT t3.* FROM #TABLE_PARENT# t1 INNER JOIN #TABLE_MANY# t2 on t2.#ID_PARENT# = t1.#ID_PARENT# INNER JOIN #TABLE_CHILD# t3 ON t2.#ID_CHILD# = t3.#ID_CHILD# WHERE t1.#ID_PARENT# = @#ID_PARENT#";
        #endregion

        protected DataTable identityColumns = new DataTable();
        protected SqlCommand _command = new SqlCommand();

        #region Get Helpers
        protected string GetTableName(Type type)
        {
            var tableName = type.GetCustomAttributes(typeof(TableName), true).OfType<TableName>().FirstOrDefault();
            if (tableName == null)
            {
                throw new Exception("TableName attribute not defined in entity");
            }
            return tableName.DbTableName;
        }
        protected string GetColumns(object entity)
        {
            var columns = "";

            //Armo el listado de columnas a traer.
            foreach (var _property in entity.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType == false && !Attribute.IsDefined(x, typeof(Entity))))
            {
                columns += "[" + _property.Name + "],";
            }
            
            //Return the columns without the last comma.
            return columns.Remove(columns.Length - 1);
        }
        protected string GetWhere(SqlParameter[] parameters)
        {
            //Limpio los parametros antes de comenzar.
            _command.Parameters.Clear();

            var where = "";

            if (!(parameters is null))
            {
                where = "WHERE ";
                //Armo el where del query.
                foreach (var parameter in parameters)
                {
                    switch (parameter.SqlDbType)
                    {
                        case SqlDbType.NVarChar:
                        case SqlDbType.VarChar:
                            where += parameter.ParameterName.Replace("@", "") + " like '%' + ISNULL(" + parameter.ParameterName + ", '') + '%' AND ";

                            if (parameter.Value.ToString() == "")
                                parameter.Value = DBNull.Value;
                            break;

                        default:
                            where += parameter.ParameterName.Replace("@", "") + " = " + parameter.ParameterName + " AND ";
                            break;
                    }
                }

                where = where.Remove(where.Length - 5);

                _command.Parameters.AddRange(parameters);

                return where;
            }

            return "";
        }
        private string GetParameter(SqlParameter parameter)
        {
            switch (parameter.SqlDbType)
            {
                case SqlDbType.VarChar:
                    return "'" + parameter.Value.ToString() + "'";

                default:
                    return parameter.Value.ToString();

            }
        }
        #endregion
    }

    public class Mapper<T> : BaseMapper, IDisposable
    {
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        #region Constates
        const string InsertQuery = "INSERT INTO #TABLE# (#COLUMNS#) VALUES (#PARAMETERS#)";
        const string UpdateQuery = "UPDATE #TABLE# SET #COLUMNS_VALUES# WHERE #WHERE#";
        const string DeleteQuery = "DELETE FROM #TABLE# #WHERE#";
        const string GetQuery = "SELECT #COLUMNS# FROM #TABLE# #WHERE#";

        T entity = (T)Activator.CreateInstance(typeof(T));
        #endregion

        #region Insert
        public Int32 Insert(T entity)
        {
            try
            {
                var insertCommand = new SqlCommand();
                var columns = "";
                var parameters = "";
                int paramCount = 0;
                int identity = 0;
                var propertys = entity.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType == false && Attribute.IsDefined(x, typeof(Insertable)));
                var propId = entity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(PrimaryKey)));

                //Controlo que tenga las propiedades definidas.
                if (propertys.Count() == 0)
                {
                    throw new Exception("La entidad " + entity.GetType().Name + " no tiene las propiedades \"Insertable\" definida");
                }

                foreach (var _property in propertys)
                {
                    columns += "[" + _property.Name + "],";
                    parameters += "@" + paramCount.ToString() + ",";

                    // In the command, there are some parameters denoted by @, you can  change their value on a condition, in my code they're hardcoded.
                    insertCommand.Parameters.Add(new SqlParameter(paramCount.ToString(), _property.GetValue(entity)));
                    paramCount++;
                }

                //Format the insert query to execute.
                var query = InsertQuery;
                query = query.Replace("#COLUMNS#", columns.Remove(columns.Length - 1));
                query = query.Replace("#PARAMETERS#", parameters.Remove(parameters.Length - 1));
                query = query.Replace("#TABLE#", entity.GetType().Name.ToString());
                query = query + " SELECT @@IDENTITY";

                using (var conn = Connection.GetSQLConnection())
                {
                    insertCommand.Connection = conn;
                    insertCommand.CommandText = query;
                    conn.Open();
                    identity = Convert.ToInt32(insertCommand.ExecuteScalar());
                    conn.Close();
                }

                //Guardo el id del objeto generado, para poder utilizarlo en las entidades relacionadas.
                entity.GetType().GetProperty(propId.FirstOrDefault().Name).SetValue(entity, identity);
               
                //Inserto las entidades relacionadas.
                foreach (var _property in entity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(EntityMany))))
                {
                    EntityMany attr = (EntityMany)_property.GetCustomAttribute(typeof(EntityMany));
                    System.Collections.IList list = (System.Collections.IList)_property.GetValue(entity, null);
                    InsertRelationMany(list, entity, attr, true);
                }

                return identity;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Connection.GetSQLConnection().State == ConnectionState.Open)
                {
                    Connection.GetSQLConnection().Close();
                }
            }

        }
        public void Insert(List<T> entidades)
        {
            foreach (T entidad in entidades)
            {
                this.Insert(entidad);
            }
        }
        #endregion

        #region Insert Relations
        public void InsertRelation(T entity, string property)
        {
            try
            {
                var propertys = entity.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType == false && Attribute.IsDefined(x, typeof(Insertable)));

                //Controlo que tenga las propiedades definidas.
                if (propertys.Count() == 0)
                {
                    throw new Exception("La entidad " + entity.GetType().Name + " no tiene las propiedades \"Insertable\" definida");
                }

                //Inserto las entidades relacionadas.
                foreach (var _property in entity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(EntityMany))))
                {
                    EntityMany attr = (EntityMany)_property.GetCustomAttribute(typeof(EntityMany));

                    if (attr.TableRela == property)
                    {
                        System.Collections.IList list = (System.Collections.IList)_property.GetValue(entity, null);
                        InsertRelationMany(list, entity, attr, false);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Connection.GetSQLConnection().State == ConnectionState.Open)
                {
                    Connection.GetSQLConnection().Close();
                }
            }
        }
        public void InsertRelationMany(System.Collections.IList entityMany, T t, EntityMany attr, bool del)
        {
            try
            {
                SqlCommand command = new SqlCommand();
                var queryDel = (del ? "DELETE FROM #TABLE# WHERE #WHERE#" : ""); ;
                var queryIns = "INSERT INTO #TABLE# (#COLUMNS#) VALUES #PARAMETERS#";
                var queryParam = "";

                if (entityMany == null)
                {
                    throw new Exception("La entidad de relación: " + attr.TableRela + " no puede ser nulla, debe contener una entidad vacía.");
                }

                foreach (var entity in entityMany)
                {
                    queryParam += "(" + t.GetType().GetProperty(attr.Field1).GetValue(t) + "," + entity.GetType().GetProperty(attr.Field2).GetValue(entity) + "),";
                }

                //Borro todos los datos 
                queryDel = queryDel
                    .Replace("#TABLE#", attr.TableMany)
                    .Replace("#WHERE#", attr.Field1 + " = " + t.GetType().GetProperty(attr.Field1).GetValue(t));

                //Si la lista está vacía, no ejecuto nada en el insert.
                if (entityMany.Count > 0)
                {
                    queryIns = queryIns
                        .Replace("#TABLE#", attr.TableMany)
                        .Replace("#COLUMNS#", attr.Field1 + "," + attr.Field2)
                        .Replace("#PARAMETERS#", queryParam.Remove(queryParam.Length - 1));
                }
                else
                {
                    queryIns = "";
                }

                using (var conn = Connection.GetSQLConnection())
                {
                    //Ejecuto los querys para borrar e insertar los datos.
                    command.Connection = conn;
                    command.CommandText = queryDel + " ; " + queryIns;
                    conn.Open();
                    command.ExecuteNonQuery();
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Connection.GetSQLConnection().State == ConnectionState.Open)
                {
                    Connection.GetSQLConnection().Close();
                }
            }
        }
        #endregion

        #region Update
        public void Update(T entity)
        {
            try
            {
                var updateCommand = new SqlCommand();
                var columnsValues = "";
                var where = "";
                int paramCount = 0;
                var propertys = entity.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType == false && Attribute.IsDefined(x, typeof(Updatable)));
                var propId = entity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(PrimaryKey)));

                //Controlo que tenga las propiedades definidas.
                if (propertys.Count() == 0)
                {
                    throw new Exception("La entidad " + entity.GetType().Name + " no tiene las propiedades \"Updatable\" definida");
                }

                foreach (var _property in propertys)
                {
                    columnsValues += _property.Name + " = " + "@" + paramCount.ToString() + ",";

                    // In the command, there are some parameters denoted by @, you can  change their value on a condition, in my code they're hardcoded.
                    var value = _property.GetValue(entity);

                    switch (_property.PropertyType.Name)
                    {
                        case "DateTime":

                            if (value.ToString() == DateTime.MinValue.ToString())
                            {
                                updateCommand.Parameters.Add(new SqlParameter("@" + paramCount.ToString(), DBNull.Value));
                            }
                            else
                            {
                                updateCommand.Parameters.Add(new SqlParameter("@" + paramCount.ToString(), value));
                            }

                            break;
                        default:
                            if (value == null)
                            {
                                updateCommand.Parameters.Add(new SqlParameter("@" + paramCount.ToString(), DBNull.Value));
                            }
                            else
                            {
                                updateCommand.Parameters.Add(new SqlParameter("@" + paramCount.ToString(), value));
                            }
                            break;
                    }

                    paramCount++;
                }

                
                //Armo el where del query.
                foreach (var prop in propId)
                {
                    PropertyInfo propertyInfo = entity.GetType().GetProperty(prop.Name);
                    where += prop.Name + " = " + propertyInfo.GetValue(entity, null) + " AND ";
                }

                var query = UpdateQuery;

                query = query.Replace("#COLUMNS_VALUES#", columnsValues.Remove(columnsValues.Length - 1));
                query = query.Replace("#TABLE#", entity.GetType().Name.ToString());
                query = query.Replace("#WHERE#", where.Remove(where.Length - 5));

                using (var conn = Connection.GetSQLConnection())
                {
                    updateCommand.Connection = conn;
                    updateCommand.CommandText = query;
                    updateCommand.CommandType = CommandType.Text;
                    conn.Open();
                    updateCommand.ExecuteNonQuery();
                }
                

                //Inserto las entidades relacionadas.
                foreach (var _property in entity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(EntityMany))))
                {
                    EntityMany attr = (EntityMany)_property.GetCustomAttribute(typeof(EntityMany));
                    System.Collections.IList list = (System.Collections.IList)_property.GetValue(entity, null);
                    InsertRelationMany(list, entity, attr, true);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Connection.GetSQLConnection().State == ConnectionState.Open)
                {
                    Connection.GetSQLConnection().Close();
                }
            }
        }
        public void Update(List<T> entidades)
        {
            foreach (T entidad in entidades)
            {
                this.Insert(entidad);
            }
        }
        #endregion

        #region Delete
        public void Delete(Int32 id)
        {
            var entity = (T)Activator.CreateInstance(typeof(T));
            var propId = entity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(PrimaryKey)));

            SqlParameter[] parameters = { new SqlParameter("@" + propId.FirstOrDefault().Name, id)};

            Delete(parameters);
        }

        public void Delete(SqlParameter[] parameters)
        {
            try
            {
                var query = DeleteQuery;

                query = query.Replace("#TABLE#", entity.GetType().Name.ToString());
                query = query.Replace("#WHERE#", GetWhere(parameters));

                using (var conn = Connection.GetSQLConnection())
                {
                    _command.Connection = Connection.GetSQLConnection();
                    _command.CommandType = CommandType.Text;
                    _command.CommandText = query;

                    conn.Open();
                    _command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Connection.GetSQLConnection().State == ConnectionState.Open)
                {
                    Connection.GetSQLConnection().Close();
                }
            }
        }
        #endregion

        #region Get
        public T GetById(Int32 id)
        {
            var entity = (T)Activator.CreateInstance(typeof(T));
            var propId = entity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(PrimaryKey)));

            SqlParameter[] parameters =  {new SqlParameter("@" + propId.FirstOrDefault().Name, id)};

            return GetByWhere(parameters);
        }
        public T GetByWhere(SqlParameter[] parameters)
        {
            var query = GetQuery;

            query = query.Replace("#COLUMNS#", GetColumns(entity));
            query = query.Replace("#TABLE#", entity.GetType().Name.ToString());
            query = query.Replace("#WHERE#", GetWhere(parameters));

            try
            {
                using (var conn = Connection.GetSQLConnection())
                {
                    _command.Connection = conn;
                    _command.CommandText = query;
                    _command.CommandType = CommandType.Text;
                    conn.Open();

                    var reader = _command.ExecuteReader();
                    
                    //Retun the mapping of the entity.
                    return Entity(reader);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Connection.GetSQLConnection().State == ConnectionState.Open)
                {
                    Connection.GetSQLConnection().Close();
                }
            }
        }
        public List<object> GetList(SqlParameter[] parameters)
        {
            try
            {
                var dataAdapter = new SqlDataAdapter();
                var data = new DataSet();
                var query = GetQuery;

                query = query.Replace("#COLUMNS#", GetColumns(entity));
                query = query.Replace("#TABLE#", entity.GetType().Name.ToString());
                query = query.Replace("#WHERE#", GetWhere(parameters));

                using (var conn = Connection.GetSQLConnection())
                {
                    //Ejecuto el query y obtengo los datos.
                    _command.Connection = conn;
                    _command.CommandType = CommandType.Text;
                    _command.CommandText = query;
                    dataAdapter.SelectCommand = _command;
                    dataAdapter.Fill(data);
                }

                //Return the entitylist in objecto to the grid.
                return EntityList(data).Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (Connection.GetSQLConnection().State == ConnectionState.Open)
                {
                    Connection.GetSQLConnection().Close();
                }
            }
            
        }
        public List<T> GetListEntity(SqlParameter[] parameters)
        {
            T entity = (T)Activator.CreateInstance(typeof(T));
            List<T> entityList = (List<T>)Activator.CreateInstance(typeof(List<T>));
            
            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            DataSet data = new DataSet();

            var columns = GetColumns(entity);
            var where = GetWhere(parameters);
            var query = GetQuery;

            query = query.Replace("#COLUMNS#", columns.Remove(columns.Length - 1));
            query = query.Replace("#TABLE#", entity.GetType().Name.ToString());
            query = query.Replace("#WHERE#", where);

            //Ejecuto el query y obtengo los datos.
            _command.Connection = Connection.GetSQLConnection();
            _command.CommandText = query;
            dataAdapter.SelectCommand = _command;

            Connection.GetSQLConnection().Open();
            dataAdapter.Fill(data);
            Connection.GetSQLConnection().Close();

            return EntityList(data);

        }
        public List<T> GetListEntity(string storeProcedure, SqlParameter[] parameters)
        {
            var dataAdapter = new SqlDataAdapter();
            var data = new DataSet();

            try
            {
                using (var conn = Connection.GetSQLConnection())
                {
                    //Ejecuto el query y obtengo los datos.
                    _command.Connection = conn;
                    _command.CommandType = CommandType.StoredProcedure;
                    _command.CommandText = storeProcedure;
                    _command.Parameters.AddRange(parameters);
                    dataAdapter.SelectCommand = _command;
                    //Open the connection to execute the store procedure.
                    conn.Open();
                    dataAdapter.Fill(data);

                    //Return the list of entities mapped with the dataset.
                    return EntityList(data);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Connection.GetSQLConnection().Close();
            }
        }

        #endregion

        #region Entity Mappers
        protected T Entity(SqlDataReader reader)
        {
            var entity = (T)Activator.CreateInstance(typeof(T));

            if (reader.HasRows)
            {
                //Por cada reagistro del SqlDataReader, recorro las columnas.
                while (reader.Read())
                {
                    for (int i = 0; i <= reader.FieldCount - 1; i++)
                    {
                        PropertyInfo propertyInfo = entity.GetType().GetProperty(reader.GetName(i));

                        if (reader.IsDBNull(i))
                        {
                            propertyInfo.SetValue(entity, null, null);
                        }
                        else
                        {
                            propertyInfo.SetValue(entity, Convert.ChangeType(reader[i], propertyInfo.PropertyType), null);
                        }
                    }
                }
                return entity;
            }
            else
            {
                return default(T);
            }
        }
        protected List<T> EntityList(DataSet data)
        {
            var entityList = (List<T>)Activator.CreateInstance(typeof(List<T>));
            //Recorro cada fila y cargo la entidad correspondiente.
            foreach (DataRow row in data.Tables[0].Rows)
            {
                //Creo una nueva entidad por cada fila.
                entity = (T)Activator.CreateInstance(typeof(T));
                var properties = entity.GetType().GetProperties().Where(x => 
                    x.PropertyType.IsGenericType == false && 
                    !Attribute.IsDefined(x, typeof(Entity)) &&
                    !Attribute.IsDefined(x, typeof(EntityMany))
                    );

                //For each property in the entity, assing each value to his property. 
                foreach (var property in properties)
                {
                    var column = property.Name;

                    switch (property.PropertyType.Name)
                    {
                        case "DateTime":

                            DateTime result;

                            if (row.IsNull(column))
                            {
                                result = default(DateTime);
                            }
                            else
                            {
                                DateTime.TryParse(row[column].ToString(), out result);
                            }

                            property.SetValue(entity, result, null);

                            break;
                        case "Boolean":
                            var resultBoolean = false;

                            if (row.IsNull(column))
                            {
                                resultBoolean = false;
                            }
                            else
                            {
                                Boolean.TryParse(row[column].ToString(), out resultBoolean);
                            }

                            property.SetValue(entity, resultBoolean, null);

                            break;
                        default:
                            property.SetValue(entity, Convert.ChangeType(row[column], property.PropertyType), null);
                            break;
                    }
                }
                
                //Agrego la entidad a la colección.
                entityList.Add(entity);
            }

            return entityList;
        }
        #endregion

        #region Disponsal
        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
                //
            }

            disposed = true;
        }
        #endregion
    }

    public class MapperMany<T, Y> : BaseMapper
    {
        public List<Y> GetListEntityMany(int Id)
        {
            var query = GetManyQuery;
            var parentEntity = (T)Activator.CreateInstance(typeof(T));
            var childEntity = (Y)Activator.CreateInstance(typeof(Y));
            
            var propId = parentEntity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(PrimaryKey)));
            

            //Recorro las propiedades para armar
            foreach (var _property in parentEntity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(EntityMany))))
            {
                EntityMany attr = (EntityMany)_property.GetCustomAttribute(typeof(EntityMany));

                if (attr.TableRela == childEntity.GetType().Name)
                {
                    query = query.Replace("#TABLE_PARENT#", parentEntity.GetType().Name);
                    query = query.Replace("#TABLE_MANY#", attr.TableMany);
                    query = query.Replace("#TABLE_CHILD#", childEntity.GetType().Name);
                    query = query.Replace("#ID_PARENT#", attr.Field1);
                    query = query.Replace("#ID_CHILD#", attr.Field2);

                    break;
                }
            }

            try
            {
                Connection.GetSQLConnection().Open();

                _command.Connection = Connection.GetSQLConnection();
                _command.CommandText = query;
                _command.Parameters.Add(new SqlParameter("@" + propId.FirstOrDefault().Name, Id));

                SqlDataReader reader = _command.ExecuteReader();

                return EntityManyList(reader);
                
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Connection.GetSQLConnection().Close();
            }
        }

        public List<object> GetListObjectMany(int Id)
        {
            //The list of entities
            var entityList = GetListEntityMany(Id);

            if (entityList != null)
            {
                //Return the entitylist in objecto to the grid.
                return entityList.Cast<object>().ToList();
            }
            else
            {
                //Para evitar errores, devuelvo
                return new List<object>();
            }
            
        }


        #region Entity Many Mappers
        protected List<Y> EntityManyList(SqlDataReader reader)
        {
            var entityList = (List<Y>)Activator.CreateInstance(typeof(List<Y>));

            if (reader.HasRows)
            {
                //Por cada reagistro del SqlDataReader, recorro las columnas.
                while (reader.Read())
                {
                    //New instance of the entity.
                    var childEntity = (Y)Activator.CreateInstance(typeof(Y));

                    for (int i = 0; i <= reader.FieldCount - 1; i++)
                    {
                        PropertyInfo propertyInfo = childEntity.GetType().GetProperty(reader.GetName(i));

                        switch (propertyInfo.PropertyType.Name)
                        {
                            case "DateTime":

                                DateTime result;

                                if (reader.IsDBNull(i))
                                {
                                    result = default(DateTime);
                                }
                                else
                                {
                                    DateTime.TryParse(reader[i].ToString(), out result);
                                }

                                propertyInfo.SetValue(childEntity, result, null);

                                break;
                            default:
                                propertyInfo.SetValue(childEntity, Convert.ChangeType(reader[i], propertyInfo.PropertyType), null);
                                break;
                        }
                    }

                    //Agrego la entidad a la lista.
                    entityList.Add(childEntity);
                }
            }
            else
            {
                return default(List<Y>);
            }

            return entityList;
        }
        #endregion
    }

    public class SqlHelper
    {
        #region ExecuteQuery
        public static void ExecuteQuery(string storeProcedure, SqlParameter[] parameters)
        {
            var command = new SqlCommand();

            try
            {
                using (var conn = Connection.GetSQLConnection())
                {
                    command.Connection = conn;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = storeProcedure;
                    command.Parameters.AddRange(parameters);

                    conn.Open();
                    command.ExecuteNonQuery();
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Connection.GetSQLConnection().Close();
            }
        }

        public DataSet ExecuteDataSet(string storeProcedure, SqlParameter[] parameters)
        {
            var command = new SqlCommand();
            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            DataSet data = new DataSet();

            try
            {
                using (var conn = Connection.GetSQLConnection())
                {
                    command.Connection = conn;
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = storeProcedure;
                    command.Parameters.AddRange(parameters);

                    dataAdapter.SelectCommand = command;

                    conn.Open();
                    dataAdapter.Fill(data);
                    conn.Close();
                }

                return data;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Connection.GetSQLConnection().Close();
            }
        }
        #endregion


    }
}

