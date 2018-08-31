using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;
using System.Data.Odbc;
using System.Data;
using System.IO;
using JRO;

namespace bq_HVCB.DataFactory
{
    public class AccessHelper
    {
        public static string ConnString = string.Format(@"Driver={0};dbq={1}\DataBase.mdb;Uid=;Pwd=BQ000000bq;", "{Microsoft Access Driver (*.mdb)}", AppDomain.CurrentDomain.BaseDirectory);//连接字符串  
        /// <summary>
        /// 打开数据测试
        /// </summary>
        /// <returns></returns>
        public static bool OpenConn()
        {
            bool flag = false;
            try
            {
                using (OdbcConnection conn = new OdbcConnection(ConnString))
                {
                    if (conn.State == ConnectionState.Closed)
                    {
                        conn.Open();
                        flag = true;
                    }
                }
            }
            catch (Exception ex)
            {
                flag = false;
                throw (ex);
            }
            return flag;

        }
        /// <summary>
        /// 返回受影响的行数
        /// </summary>
        /// <param name="comText"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static int ExecuteNonQuery(string comText, params OdbcParameter[] param)
        {
            try
            {
                int val = 0;
                using (OdbcConnection conn = new OdbcConnection(ConnString))
                {
                    using (OdbcCommand cmd = new OdbcCommand(comText, conn))
                    {

                        if (param != null && param.Length != 0)
                        {
                            cmd.Parameters.AddRange(param);
                        }
                        if (conn.State == ConnectionState.Closed)
                        {
                            conn.Open();
                        }
                        val = cmd.ExecuteNonQuery();

                    }
                }
                return val;
            }
            catch (Exception ex)
            {
                throw (ex);
            }
        }
        /// <summary>
        /// 返回数据对象
        /// </summary>
        /// <param name="comText"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static object ExecuteScalar(string comText, params OdbcParameter[] param)
        {
            using (OdbcConnection conn = new OdbcConnection(ConnString))
            {
                using (OdbcCommand cmd = new OdbcCommand(comText, conn))
                {
                    if (param != null && param.Length != 0)
                    {
                        cmd.Parameters.AddRange(param);
                    }
                    if (conn.State == ConnectionState.Closed)
                    {
                        conn.Open();
                    }
                    return cmd.ExecuteScalar();
                }
            }
        }
        /// <summary>
        /// 返回table
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static DataTable Adapter(string cmdText, params OdbcParameter[] param)
        {
            DataTable dt = new DataTable();
            try
            {
                using (OdbcConnection conn = new OdbcConnection(ConnString))
                {
                    using (OdbcDataAdapter oda = new OdbcDataAdapter())
                    {
                        using (OdbcCommand command = new OdbcCommand(cmdText, conn))
                        {
                            oda.SelectCommand = command;
                            if (param != null && param.Length != 0)
                            {
                                oda.SelectCommand.Parameters.AddRange(param);
                            }
                            if (conn.State == ConnectionState.Closed)
                            {
                                conn.Open();
                            }
                            oda.Fill(dt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw (ex);
            }
            return dt;
        }
        /// <summary>
        /// 向前读取记录
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static OdbcDataReader ExectueReader(string cmdText, params OdbcParameter[] param)
        {
            using (OdbcConnection conn = new OdbcConnection(ConnString))
            {
                using (OdbcCommand cmd = new OdbcCommand(cmdText, conn))
                {
                    if (param != null && param.Length != 0)
                    {
                        cmd.Parameters.AddRange(param);
                    }
                    if (conn.State == ConnectionState.Closed)
                    {
                        conn.Open();
                    }
                    return cmd.ExecuteReader(CommandBehavior.CloseConnection);
                }
            }
        }
        /// <summary>
        /// 读取存储过程
        /// </summary>
        /// <param name="cmdText"></param>
        /// <param name="type"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        public static DataTable GetPro(string cmdText, CommandType type, params OdbcParameter[] param)
        {
            DataTable dt = new DataTable();
            using (OdbcDataAdapter sda = new OdbcDataAdapter(cmdText, ConnString))
            {
                new OdbcCommand().CommandType = CommandType.StoredProcedure;
                if (param != null && param.Length != 0)
                {
                    sda.SelectCommand.Parameters.AddRange(param);
                }
                sda.Fill(dt);
            }
            return dt;
        }
        #region 压缩Access数据库
        /// <summary>
        /// 压缩Access数据库
        /// </summary>
        /// <param name="DBPath">数据库绝对路径</param>
        public static bool CompactAccess()
        {
            string DBPath = ConnString;
            bool Isok = true;
            try
            {
                if (!File.Exists(DBPath))
                {
                    throw new Exception("目标数据库不存在,无法压缩");
                }

                //声明临时数据库名称
                string temp = DateTime.Now.Year.ToString();
                temp += DateTime.Now.Month.ToString();
                temp += DateTime.Now.Day.ToString();
                temp += DateTime.Now.Hour.ToString();
                temp += DateTime.Now.Minute.ToString();
                temp += DateTime.Now.Second.ToString() + ".bak";
                temp = DBPath.Substring(0, DBPath.LastIndexOf("\\") + 1) + temp;
                //定义临时数据库的连接字符串
                string temp2 = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + temp + @";JET OLEDB:Database Password=BQ000000bq;";//";Jet OLEDB:Database Password=123456";
                //定义目标数据库的连接字符串
                string DBPath2 = "Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + DBPath + @";JET OLEDB:Database Password=BQ000000bq;";//JET OLEDB:Engine Type=5";// ";Jet OLEDB:Database Password=123456";
                //创建一个JetEngineClass对象的实例
                JRO.JetEngineClass jt = new JRO.JetEngineClass();
                //使用JetEngineClass对象的CompactDatabase方法压缩修复数据库
                jt.CompactDatabase(DBPath2, temp2);
                //拷贝临时数据库到目标数据库(覆盖)
                File.Copy(temp, DBPath, true);
                //最后删除临时数据库
                File.Delete(temp);
            }
            catch
            {
                Isok = false;
            }
            return Isok;
        }
        #endregion
    }
}
