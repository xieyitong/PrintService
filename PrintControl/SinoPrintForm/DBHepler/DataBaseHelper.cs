using System;
using System.Collections.Generic;
using System.Text;
using System.Data.SQLite;
using System.Data;
using System.Windows.Forms;
using System.Data.Common;
using System.IO;


namespace SinoPrintForm
{
    /// <summary>
    /// 初始化数据库Wilson
    /// </summary>
    public class DataBaseHelper
    {
        public static string DBInit()
        {
            string ret = "";
            try
            {
                //不存在数据库，需要创建数据库和数据表
                if (!File.Exists(LocalSQLiteDb.path + @"/"+LocalSQLiteDb.dbName))
                {
                    SQLiteConnection.CreateFile(LocalSQLiteDb.path + @"/" + LocalSQLiteDb.dbName);
                    LocalSQLiteDb.sqlConn = new SQLiteConnection();
                    LocalSQLiteDb.sqlConn.ConnectionString = LocalSQLiteDb.connStr;
                    //SQLiteDb.sqlConn.Open();//加密
                    //SQLiteDb.sqlConn.ChangePassword("sinoservice;");//加密
                    LocalSQLiteDb.GetInstance().beginTransaction();
                  
                    string sql = "";
                    //创建打印机配置表
                    sql = @"CREATE TABLE [PrintSetting](
	                [ID] INTEGER PRIMARY KEY AUTOINCREMENT,
	                [reporttemplate] [nvarchar](500),
	                [printName] [nvarchar](500),
                    [printSetting] [nvarchar](Max)
	            
                )";
                    LocalSQLiteDb.GetInstance().ExecuteSql(sql);
                    LocalSQLiteDb.GetInstance().commitTransaction();
                    ret = "创表成功";
                }
            }
            catch (Exception e)
            {
                LocalSQLiteDb.GetInstance().rollBackTransaction();
                ret = "创表失败";
            }
            return ret;
        }
    }
}
