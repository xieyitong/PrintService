using System;
using System.Data;
using System.Data.SQLite;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using System.IO;

namespace SinoPrintForm
{
    class LocalSQLiteDb
    {
        public static string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
        public static string dbName = "data.db";
        public static string connStr = @"Data Source=" + path + @"\data.db;Pooling=true;FailIfMissing=false";

       
        public static SQLiteConnection sqlConn;
        public static SQLiteTransaction trans;
        public static LocalSQLiteDb instance;

        private LocalSQLiteDb()
        {
            sqlConn = new SQLiteConnection();
            sqlConn.ConnectionString = connStr; // @"Data Source=" + dbName + ";Pooling=true;FailIfMissing=false";
            sqlConn.Open();
        }

        public static LocalSQLiteDb GetInstance()
        {
            if (instance == null)
            {
                instance = new LocalSQLiteDb();
            }
            return instance;
        }

        public void close()
        {
            if (sqlConn != null)
            {
                sqlConn.Close();
                sqlConn = null;
            }
        }

        /// <summary>
        /// 开始事务
        /// </summary>
        public void beginTransaction()
        {
            trans = sqlConn.BeginTransaction();
        }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public void rollBackTransaction()
        {
            trans.Rollback();
            trans = null;
        }

        /// <summary>
        /// 提交事务
        /// </summary>
        public void commitTransaction()
        {
            trans.Commit();
            trans = null;
        }

        /// <summary>
        /// 保存对象（重载）
        /// </summary>
        public long Save<T>(T obj)
        {
            Type type = typeof(T);
            return Save(type, obj);
        }

        /// <summary>
        /// 保存对象（重载）
        /// </summary>
        public long Save(string typeName, object obj)
        {
            Type type = Type.GetType(Assembly.GetExecutingAssembly().GetName().Name + "." + typeName);
            return Save(type, obj);
        }

        /// <summary>
        /// 保存对象
        /// </summary>
        public long Save(Type type, object obj)
        {
            using (SQLiteCommand sqlCmd = sqlConn.CreateCommand())
            {
                if (trans != null) sqlCmd.Transaction = trans;
                string sql = string.Empty;
                PropertyInfo[] piList = type.GetProperties();
                List<SQLiteParameter> paramList = new List<SQLiteParameter>();

                string indexName = type.GetMethod("GetIndexName").Invoke(obj, null).ToString();
                long indexValue = long.Parse(type.GetMethod("GetIndexValue").Invoke(obj, null).ToString());

                if (indexValue == 0)
                {
                    string columns = string.Empty;
                    string values = string.Empty;
                    bool hasInited = false;
                    for (int i = 0; i < piList.Length; i++)
                    {
                        if (piList[i].Name.Equals(indexName))
                        {
                            continue;
                        }
                        if (!hasInited)
                        {
                            columns += piList[i].Name;
                            values += "@" + piList[i].Name;
                            paramList.Add(new SQLiteParameter("@" + piList[i].Name, piList[i].GetValue(obj, null)));
                            hasInited = true;
                        }
                        else
                        {
                            columns += "," + piList[i].Name;
                            values += ", @" + piList[i].Name;
                            paramList.Add(new SQLiteParameter("@" + piList[i].Name, piList[i].GetValue(obj, null)));
                        }
                    }
                    sql = "insert into " + type.Name + "(" + columns + ") values(" + values + ")";

                    sqlCmd.Parameters.AddRange(paramList.ToArray());
                    sqlCmd.CommandText = sql;
                    sqlCmd.ExecuteNonQuery();

                    sql = "select LAST_INSERT_ROWID() as [newid]";
                    sqlCmd.CommandText = sql;
                    SQLiteDataReader sqlDr = sqlCmd.ExecuteReader();
                    if (sqlDr.Read())
                    {
                        indexValue = sqlDr.GetInt64(0);
                    }
                    sqlDr.Close();

                    return indexValue;
                }
                else
                {
                    sql = "update " + type.Name + " set ";
                    bool hasInited = false;
                    for (int i = 0; i < piList.Length; i++)
                    {
                        if (piList[i].Name.Equals(indexName))
                        {
                            continue;
                        }
                        if (!hasInited)
                        {
                            sql += piList[i].Name + " = @" + piList[i].Name;
                            paramList.Add(new SQLiteParameter("@" + piList[i].Name, piList[i].GetValue(obj, null)));
                            hasInited = true;
                        }
                        else
                        {
                            sql += ", " + piList[i].Name + " = @" + piList[i].Name;
                            paramList.Add(new SQLiteParameter("@" + indexValue));
                        }
                    }
                    sql += " where " + indexName + " = @" + indexName;
                    paramList.Add(new SQLiteParameter("@" + indexName, indexValue));

                    sqlCmd.Parameters.AddRange(paramList.ToArray());
                    sqlCmd.CommandText = sql;
                    sqlCmd.ExecuteNonQuery();

                    return indexValue;
                }
            }
        }

        public void Delete<T>(T obj)
        {
            using (SQLiteCommand sqlCmd = sqlConn.CreateCommand())
            {
                Type type = typeof(T);
                string indexName = type.GetMethod("GetIndexName").Invoke(obj, null).ToString();
                long indexValue = long.Parse(type.GetMethod("GetIndexValue").Invoke(obj, null).ToString());

                string sql = "delete from " + type.Name + " where " + indexName + " = " + indexValue.ToString();
                if (trans != null) sqlCmd.Transaction = trans;
                sqlCmd.CommandText = sql;
                sqlCmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 根据主键获取信息
        /// </summary>
        public T GetById<T>(long id)
        {
            using (SQLiteCommand sqlCmd = sqlConn.CreateCommand())
            {
                if (trans != null) sqlCmd.Transaction = trans;
                Type type = typeof(T);
                string sql = "select ";
                PropertyInfo[] piList = type.GetProperties();
                for (int i = 0; i < piList.Length; i++)
                {
                    if (i == 0)
                    {
                        sql += piList[i].Name;
                    }
                    else
                    {
                        sql += ", " + piList[i].Name;
                    }
                }
                sql += " from " + type.Name + " where " + type.GetMethod("GetIndexName").Invoke(null, null).ToString() + " = " + id.ToString();
                sqlCmd.CommandText = sql;
                SQLiteDataReader sqlDr = sqlCmd.ExecuteReader();
                T obj = Activator.CreateInstance<T>();
                if (sqlDr.Read())
                {
                    for (int i = 0; i < sqlDr.FieldCount; i++)
                    {
                        PropertyInfo pi = type.GetProperty(sqlDr.GetName(i));
                        pi.SetValue(obj, sqlDr.GetValue(i), null);
                    }
                }
                sqlDr.Close();
                return obj;
            }
        }

        ///// <summary>
        ///// 判断是否存在（重载）
        ///// </summary>
        //public bool IsRecordExist(String typeName, List<SQLiteParameter> paramList)
        //{
        //    Type type = Type.GetType(Assembly.GetExecutingAssembly().GetName().Name + "." + typeName);
        //    String sql = "select " + type.GetMethod("GetIndexName").Invoke(null, null).ToString() + " from " + typeName + querySql;
        //    return IsRecordExist(sql);
        //}

        /// <summary>
        /// 判断是否存在
        /// </summary>
        public bool IsRecordExist(String sql, List<SQLiteParameter> paramList)
        {
            using (SQLiteCommand sqlCmd = sqlConn.CreateCommand())
            {
                if (trans != null) sqlCmd.Transaction = trans;
                if (paramList != null && paramList.Count > 0)
                {
                    sqlCmd.Parameters.AddRange(paramList.ToArray());
                }
                sqlCmd.CommandText = sql;
                SQLiteDataReader sqlDr = sqlCmd.ExecuteReader();
                if (sqlDr.Read())
                {
                    sqlDr.Close();
                    return true;
                }
                else
                {
                    sqlDr.Close();
                    return false;
                }
            }
        }

        /// <summary>
        /// 执行SQL语句
        /// </summary>
        public long ExecuteSql(string sql)
        {
            using (SQLiteCommand sqlCmd = sqlConn.CreateCommand())
            {
                if (trans != null) sqlCmd.Transaction = trans;
                sqlCmd.CommandText = sql;
                int num= sqlCmd.ExecuteNonQuery();
               return num;
            }
        }

        /// <summary>
        /// 以 List 方式返回查询结果
        /// </summary>
        public List<T> Query<T>()
        {
            using (SQLiteCommand sqlCmd = sqlConn.CreateCommand())
            {
                if (trans != null) sqlCmd.Transaction = trans;
                List<T> result = new List<T>();
                Type type = typeof(T);

                string sql = "select ";
                PropertyInfo[] piList = type.GetProperties();
                for (int i = 0; i < piList.Length; i++)
                {
                    if (i == 0)
                    {
                        sql += piList[i].Name;
                    }
                    else
                    {
                        sql += ", " + piList[i].Name;
                    }
                }
                sql += " from " + type.Name;

                sqlCmd.CommandText = sql;
                SQLiteDataReader sqlDr = sqlCmd.ExecuteReader();
                while (sqlDr.Read())
                {
                    T obj = Activator.CreateInstance<T>();
                    for (int i = 0; i < sqlDr.FieldCount; i++)
                    {
                        PropertyInfo pi = type.GetProperty(sqlDr.GetName(i));
                        pi.SetValue(obj, sqlDr.GetValue(i), null);
                    }
                    result.Add(obj);
                }
                sqlDr.Close();
                return result;
            }
        }

        /// <summary>
        /// 以 DATATABLE 方式返回查询结果
        /// </summary>
        public DataTable Query(string sql)
        {
            using (SQLiteCommand sqlCmd = sqlConn.CreateCommand())
            {
                if (trans != null) sqlCmd.Transaction = trans;
                SQLiteDataAdapter sqlDa = new SQLiteDataAdapter(sqlCmd);
                DataTable dt = new DataTable();
                sqlDa.Fill(dt);
                if (dt != null && dt.Rows.Count == 1)
                {
                    if (dt.Rows[0][0].ToString().Equals("{}"))
                    {
                        dt.Rows.RemoveAt(0);
                    }
                }
                return dt;
            }
        }

    }
}
