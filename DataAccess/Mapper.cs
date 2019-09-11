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
        protected void GetIdentityColumn(string tableName)
        {
            var oString = "SELECT name, is_identity FROM sys.columns WHERE[object_id] = object_id(@Table) and is_identity = 1";
            var oCmd = new SqlCommand(oString, Connection.GetSQLConnection());
            var table = new DataTable();

            oCmd.Parameters.AddWithValue("@Table", tableName);

            // create data adapter
            SqlDataAdapter da = new SqlDataAdapter(oCmd);
            // this will query your database and return the result to your datatable
            da.Fill(identityColumns);
        }
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

            return columns;
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
                    if (parameter.SqlDbType == SqlDbType.VarChar)
                    {
                        where += parameter.ParameterName.Replace("@", "") + " like " + parameter.ParameterName + " AND ";
                    }
                    else
                    {
                        where += parameter.ParameterName.Replace("@", "") + " = " + parameter.ParameterName + " AND ";
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
        #endregion

        #region Insert
        public Int32 Insert(T entity)
        {
            try
            {
                GetIdentityColumn(entity.GetType().Name);

                SqlCommand insertCommand = new SqlCommand();
                string columns = "", parameters = "";
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
                    //Controlo que la propiedad no sea Identity, si es, no va en el Insert.
                    var result = identityColumns.Select("name = '" + _property.Name + "'");

                    if (result.Length == 0)
                    {
                        columns += "[" + _property.Name + "],";
                        parameters += "@" + paramCount.ToString() + ",";

                        // In the command, there are some parameters denoted by @, you can  change their value on a condition, in my code they're hardcoded.
                        insertCommand.Parameters.Add(new SqlParameter(paramCount.ToString(), _property.GetValue(entity)));
                        paramCount++;
                    }
                }

                var query = InsertQuery;

                query = query.Replace("#COLUMNS#", columns.Remove(columns.Length - 1));
                query = query.Replace("#PARAMETERS#", parameters.Remove(parameters.Length - 1));
                query = query.Replace("#TABLE#", entity.GetType().Name.ToString());
                query = query + " SELECT @@IDENTITY";

                Connection.GetSQLConnection().Open();
                insertCommand.Connection = Connection.GetSQLConnection();
                insertCommand.CommandText = query;
                identity = Convert.ToInt32(insertCommand.ExecuteScalar());
                Connection.GetSQLConnection().Close();

                //Guardo el id del objeto generado, para poder utilizarlo en las entidades relacionadas.
                entity.GetType().GetProperty(propId.FirstOrDefault().Name).SetValue(entity, identity);
               
                //Inserto las entidades relacionadas.
                foreach (var _property in entity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(EntityMany))))
                {
                    EntityMany attr = (EntityMany)_property.GetCustomAttribute(typeof(EntityMany));
                    System.Collections.IList list = (System.Collections.IList)_property.GetValue(entity, null);
                    InsertRelationMany(list, entity, attr);
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

        #region InsertRelationMany
        public void InsertRelationMany(System.Collections.IList entityMany, T t, EntityMany attr)
        {
            try
            {
                SqlCommand command = new SqlCommand();
                var queryDel = "DELETE FROM #TABLE# WHERE #WHERE#";
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

                //Ejecuto los querys para borrar e insertar los datos.
                Connection.GetSQLConnection().Open();
                command.Connection = Connection.GetSQLConnection();
                command.CommandText = queryDel + " ; " + queryIns;
                command.ExecuteNonQuery();
                Connection.GetSQLConnection().Close();
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
        #endregion

        #region Update
        public void Update(T entity)
        {
            try
            {
                GetIdentityColumn(entity.GetType().Name);

                SqlCommand updateCommand = new SqlCommand();
                string columnsValues = "", where = "";
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
                    //Controlo que la propiedad no sea Identity, si es, no va en el Insert.
                    var result = identityColumns.Select("name = '" + _property.Name + "'");

                    if (result.Length == 0)
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

                Connection.GetSQLConnection().Open();
                updateCommand.Connection = Connection.GetSQLConnection();
                updateCommand.CommandText = query;
                updateCommand.ExecuteNonQuery();
                Connection.GetSQLConnection().Close();

                //Inserto las entidades relacionadas.
                foreach (var _property in entity.GetType().GetProperties().Where(x => Attribute.IsDefined(x, typeof(EntityMany))))
                {
                    EntityMany attr = (EntityMany)_property.GetCustomAttribute(typeof(EntityMany));
                    System.Collections.IList list = (System.Collections.IList)_property.GetValue(entity, null);
                    InsertRelationMany(list, entity, attr);
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
            T entity = (T)Activator.CreateInstance(typeof(T));
            GetIdentityColumn(entity.GetType().Name);

            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@" + identityColumns.Rows[0]["name"], id)
            };

            Delete(parameters.ToArray());
        }

        public void Delete(SqlParameter[] parameters)
        {
            try
            {
                T entity = (T)Activator.CreateInstance(typeof(T));
                SqlCommand deleteCommand = new SqlCommand();
                string where = "";

                if (parameters != null)
                {
                    where = "WHERE ";

                    //Armo el where del query.
                    foreach (var parameter in parameters)
                    {
                        where += parameter.ParameterName.Replace("@", "") + " = " + parameter.ParameterName + " AND ";
                    }

                    where = where.Remove(where.Length - 5);
                }

                var query = DeleteQuery;

                query = query.Replace("#TABLE#", entity.GetType().Name.ToString());
                query = query.Replace("#WHERE#", where);

                Connection.GetSQLConnection().Open();

                deleteCommand.Connection = Connection.GetSQLConnection();
                deleteCommand.CommandText = query;
                deleteCommand.Parameters.AddRange(parameters);
                deleteCommand.ExecuteNonQuery();
                Connection.GetSQLConnection().Close();
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

            GetIdentityColumn(entity.GetType().Name);
            List<SqlParameter> parameters = new List<SqlParameter>() {
                new SqlParameter("@" + identityColumns.Rows[0]["name"], id)
            };

            return GetByWhere(parameters.ToArray());
        }
        public T GetByWhere(SqlParameter[] parameters)
        {
            T entity = (T)Activator.CreateInstance(typeof(T));
            string columns = "", where = "";

            //Armo el listado de columnas a traer.
            foreach (var _property in entity.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType == false && !Attribute.IsDefined(x, typeof(Entity))))
            {
                columns += _property.Name + ",";
            }

            where = GetWhere(parameters);


            var query = GetQuery;

            query = query.Replace("#COLUMNS#", columns.Remove(columns.Length - 1));
            query = query.Replace("#TABLE#", entity.GetType().Name.ToString());
            query = query.Replace("#WHERE#", where);

            try
            {
                Connection.GetSQLConnection().Open();

                _command.Connection = Connection.GetSQLConnection();
                _command.CommandText = query;

                SqlDataReader reader = _command.ExecuteReader();

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
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Connection.GetSQLConnection().Close();
            }
        }
        public List<object> GetList(SqlParameter[] parameters)
        {
            T entity = (T)Activator.CreateInstance(typeof(T));
            List<T> entityList = (List<T>)Activator.CreateInstance(typeof(List<T>));
            List<object> entityList_ = new List<object>();

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

            //Connection.GetSQLConnection().Open();
            dataAdapter.Fill(data);
            //Connection.GetSQLConnection().Close();

            //Recorro cada fila y cargo la entidad correspondiente.
            foreach (DataRow row in data.Tables[0].Rows)
            {
                //Creo una nueva entidad por cada fila.
                entity = (T)Activator.CreateInstance(typeof(T));

                foreach (DataColumn column in data.Tables[0].Columns)
                {
                    PropertyInfo propertyInfo = entity.GetType().GetProperty(column.ToString());

                    switch (propertyInfo.PropertyType.Name)
                    {

                        case "DateTime":

                            DateTime valueDateTime;

                            if (row.IsNull(column))
                            {
                                valueDateTime = default(DateTime);
                            }
                            else
                            {
                                DateTime.TryParse(row[column].ToString(), out valueDateTime);
                            }

                            propertyInfo.SetValue(entity, valueDateTime, null);

                            break;
                        case "Int32":
                            Int32 valueInt32;
                            if (row.IsNull(column))
                            {
                                valueInt32 = default(Int32);
                            }
                            else
                            {
                                valueInt32 = Convert.ToInt32(row[column]);
                            }


                            propertyInfo.SetValue(entity, valueInt32, null);
                            break;
                        default:
                            propertyInfo.SetValue(entity, Convert.ChangeType(row[column], propertyInfo.PropertyType), null);
                            break;

                    }


                }

                entityList_.Add(entity);
            }


            return entityList_;

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

            //Recorro cada fila y cargo la entidad correspondiente.
            foreach (DataRow row in data.Tables[0].Rows)
            {
                //Creo una nueva entidad por cada fila.
                entity = (T)Activator.CreateInstance(typeof(T));

                foreach (DataColumn column in data.Tables[0].Columns)
                {
                    PropertyInfo propertyInfo = entity.GetType().GetProperty(column.ToString());

                    switch (propertyInfo.PropertyType.Name)
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

                            propertyInfo.SetValue(entity, result, null);

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

                            propertyInfo.SetValue(entity, resultBoolean, null);

                            break;
                        default:
                            propertyInfo.SetValue(entity, Convert.ChangeType(row[column], propertyInfo.PropertyType), null);
                            break;
                    }
                }

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
            T parentEntity = (T)Activator.CreateInstance(typeof(T));
            Y childEntity = (Y)Activator.CreateInstance(typeof(Y));
            List<Y> entityList = (List<Y>)Activator.CreateInstance(typeof(List<Y>));

            GetIdentityColumn(parentEntity.GetType().Name);

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
                _command.Parameters.Add(new SqlParameter("@" + identityColumns.Rows[0]["name"], Id));

                SqlDataReader reader = _command.ExecuteReader();

                if (reader.HasRows)
                {
                    //Por cada reagistro del SqlDataReader, recorro las columnas.
                    while (reader.Read())
                    {
                        //New instance of the entity.
                        childEntity = (Y)Activator.CreateInstance(typeof(Y));

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
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                Connection.GetSQLConnection().Close();
            }

            /*
            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            DataSet data = new DataSet();

            string columns = "", where = "";

            //Armo el listado de columnas a traer.
            foreach (var _property in entity.GetType().GetProperties().Where(x => x.PropertyType.IsGenericType == false && !Attribute.IsDefined(x, typeof(Entity))))
            {
                columns += _property.Name + ",";
            }

            where = GetWhere(parameters);

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

            //Recorro cada fila y cargo la entidad correspondiente.
            foreach (DataRow row in data.Tables[0].Rows)
            {
                //Creo una nueva entidad por cada fila.
                entity = (T)Activator.CreateInstance(typeof(T));

                foreach (DataColumn column in data.Tables[0].Columns)
                {
                    PropertyInfo propertyInfo = entity.GetType().GetProperty(column.ToString());

                    switch (propertyInfo.PropertyType.Name)
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

                            propertyInfo.SetValue(entity, result, null);

                            break;
                        default:
                            propertyInfo.SetValue(entity, Convert.ChangeType(row[column], propertyInfo.PropertyType), null);
                            break;
                    }
                }

                entityList.Add(entity);
            }


            return entityList;
            */
        }
    }

    public class SqlHelper
    {
        #region ExecuteQuery
        public static void ExecuteQuery(string storeProcedure)
        {
            using (Connection.GetSQLConnection())
            {
                SqlCommand command = new SqlCommand();
                //string columns = "", parameters = "";

                Connection.GetSQLConnection().Open();
                command.Connection = Connection.GetSQLConnection();
                command.CommandText = storeProcedure;
                command.ExecuteNonQuery();
                Connection.GetSQLConnection().Close();
            }

            //Bitácora
            //Logger.Log(0, Session.SessionUser == null ? 0 : Session.SessionUser.Id, "", storeProcedure, "Ejecución", DateTime.Now);
        }

        public static void ExecuteQueryMaster(string storeProcedure)
        {
            using (Connection.GetMasterSQLConnection())
            {
                SqlCommand command = new SqlCommand();
                //string columns = "", parameters = "";

                Connection.GetMasterSQLConnection().Open();
                command.Connection = Connection.GetMasterSQLConnection();
                command.CommandText = storeProcedure;
                command.ExecuteNonQuery();
                Connection.GetMasterSQLConnection().Close();
            }

            //Bitácora
            //Logger.Log(0, Session.SessionUser == null ? 0 : Session.SessionUser.Id, "", storeProcedure, "Ejecución", DateTime.Now);

        }

        public DataSet ExecuteDataSet(string storePro, SqlParameter[] parameters)
        {
            SqlCommand command = new SqlCommand();
            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            DataSet data = new DataSet();

            try
            {
                command.Connection = Connection.GetSQLConnection();
                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = storePro;
                command.Parameters.AddRange(parameters);

                dataAdapter.SelectCommand = command;

                Connection.GetSQLConnection().Open();
                dataAdapter.Fill(data);
                Connection.GetSQLConnection().Close();

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

