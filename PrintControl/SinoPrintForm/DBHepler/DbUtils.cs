using System;

using System.Collections.Generic;
using System.Text;
using System.Data;

namespace SinoPrintForm
{
    class DbUtils
    {
        /// <summary>
        /// 初始化数据库
        /// </summary>
        /// <param name="fileName">数据库源</param>
        public virtual void dBInit(string fileName) { }

        /// <summary>
        /// 打开数据库
        /// </summary>
        public virtual bool openDB() { return false; }

        /// <summary>
        /// 关闭数据库
        /// </summary>
        public virtual bool closeDB() { return false; }

        /// <summary>
        /// 开始事务
        /// </summary>
        public virtual void beginTransaction() { }

        /// <summary>
        /// 回滚事务
        /// </summary>
        public virtual void rollBackTransaction() { }

        /// <summary>
        /// 提交事务
        /// </summary>
        public virtual void commitTransaction() { }

        /// <summary>
        /// 判断记录是否存在
        /// </summary>
        public virtual bool isRecordExist(string sql) { return false; }

        /// <summary>
        /// 执行SQL
        /// </summary>
        public virtual bool executeSql(string sql) { return false; }

        /// <summary>
        /// 执行查询语句
        /// </summary>
        public virtual DataTable executeQueryDt(string sql) { return null; }

        /// <summary>
        /// 将查询结果汇总成字符串
        /// </summary>
        public virtual string executeReader(string sql) { return null; }
    }
}
