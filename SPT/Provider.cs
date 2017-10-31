using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.OleDb;
using System.Data;
using System.IO;
using System.IO.Ports;

namespace SPT
{
    public class Provider
    {

        public static int[] arrBaudrate = new int[] { 600, 1200, 2400, 4800, 9600, 14400, 19200, 28800 };
        public static int[] arrDataBit = new int[] { 5, 6, 7, 8 };
        public static string[] arrSendMode = new string[] { "顺序发送", "循环发送" };
        public static string[] arrCheckMode = new string[] { Parity.Even.ToString(), Parity.Mark.ToString(), Parity.None.ToString(), Parity.Odd.ToString(), Parity.Space.ToString() };
        public static string[] arrStopBits = new string[] { StopBits.None.ToString(), StopBits.One.ToString(), StopBits.OnePointFive.ToString(), StopBits.Two.ToString() };

        public static StopBits GetStopBits(string StopBits)
        {
            switch (StopBits)
            {
                case "None":
                    return System.IO.Ports.StopBits.None;
                case "One":
                    return System.IO.Ports.StopBits.One;
                case "OnePointFive":
                    return System.IO.Ports.StopBits.OnePointFive;
                case "Two":
                    return System.IO.Ports.StopBits.Two;
                default:
                    return System.IO.Ports.StopBits.One;
            }

        }
        public static Parity GetParity(string Parity)
        {
            switch (Parity)
            {
                case "Even":
                    return System.IO.Ports.Parity.Even;
                case "Mark":
                    return System.IO.Ports.Parity.Mark;
                case "None":
                    return System.IO.Ports.Parity.None;
                case "Odd":
                    return System.IO.Ports.Parity.Odd;
                case "Space":
                    return System.IO.Ports.Parity.Space;
                default:
                    return System.IO.Ports.Parity.None;
            }
        }

        public static DataSet ExcelToDS(string Path)
        {
            if (Path.Length <= 0)
            {
                return null;
            }
            string strConn = "";
            string tableName = "";

            //需要安装下载新的驱动引擎http://download.microsoft.com/download/7/0/3/703ffbcb-dc0c-4e19-b0da-1463960fdcdb/AccessDatabaseEngine.exe
            strConn = "Provider=Microsoft.ACE.OLEDB.12.0;" + "Data Source=" + Path + ";" + "Extended Properties=Excel 12.0;";
            OleDbConnection conn = new OleDbConnection(strConn);
            try
            {
                conn.Open();
            }
            catch (Exception)
            {
                if (Path.Substring(Path.Length - 1, 1) == "s")
                    strConn = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + Path + ";" + "Extended Properties=Excel 8.0;";
                else if (Path.Substring(Path.Length - 1, 1) == "x")
                {
                    throw new Exception("仅支持.xls版本导入");
                }
                conn.Open();
            }
            string strExcel = "";
            OleDbDataAdapter myCommand = null;

            DataSet ds = new DataSet(); ;
            DataTable schemaTable = conn.GetOleDbSchemaTable(System.Data.OleDb.OleDbSchemaGuid.Tables, null);
            for (int i = 0; i < schemaTable.Rows.Count; i++)
            {
                tableName = schemaTable.Rows[i][2].ToString().Trim();
                if (!tableName.Contains("FilterDatabase") && tableName.Substring(tableName.Length - 1, 1) != "_")
                {
                    ds.Tables.Add(tableName);
                    strExcel = string.Format("select * from [{0}]", tableName);
                    myCommand = new OleDbDataAdapter(strExcel, strConn);
                    myCommand.Fill(ds, tableName);
                }
            }
            conn.Close();
            return ds;
        }

        public static bool SaveTxt(string filePath, params string[] arrLog)
        {
            using (StreamWriter sw = new StreamWriter(filePath, false))
            {
                try
                {
                    for (int i = 0; i < arrLog.Length; i++)
                    {
                        sw.WriteLine(arrLog[i]);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                sw.Close();
                return true;
            }
        }

    }

    public class CSVFileHelper
    {
        /// <summary>
        /// 将DataTable中数据写入到CSV文件中
        /// </summary>
        /// <param name="dt">提供保存数据的DataTable</param>
        /// <param name="fileName">CSV的文件路径</param>
        public static bool SaveCSV(DataTable dt, string fullPath)
        {
            FileInfo fi = new FileInfo(fullPath);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            using (FileStream fs = new FileStream(fullPath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                //StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.Default);
                using (StreamWriter sw = new StreamWriter(fs, System.Text.Encoding.UTF8))
                {
                    try
                    {
                        string data = "";
                        //写出列名称
                        for (int i = 0; i < dt.Columns.Count; i++)
                        {
                            data += dt.Columns[i].ColumnName.ToString();
                            if (i < dt.Columns.Count - 1)
                            {
                                data += ",";
                            }
                        }
                        sw.WriteLine(data);
                        //写出各行数据
                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            data = "";
                            for (int j = 0; j < dt.Columns.Count; j++)
                            {
                                string str = dt.Rows[i][j].ToString();
                                str = str.Replace("\"", "\"\"");//替换英文冒号 英文冒号需要换成两个冒号
                                if (str.Contains(',') || str.Contains('"')
                                    || str.Contains('\r') || str.Contains('\n')) //含逗号 冒号 换行符的需要放到引号中
                                {
                                    str = string.Format("\"{0}\"", str);
                                }

                                data += str;
                                if (j < dt.Columns.Count - 1)
                                {
                                    data += ",";
                                }
                            }
                            sw.WriteLine(data);
                        }
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    finally
                    {
                        sw.Close();
                        fs.Close();
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// 将CSV文件的数据读取到DataTable中
        /// </summary>
        /// <param name="fileName">CSV文件路径</param>
        /// <returns>返回读取了CSV数据的DataTable</returns>
        public static DataTable OpenCSV(string filePath)
        {
            //Encoding encoding = Common.GetType(filePath); //Encoding.ASCII;//
            DataTable dt = new DataTable();
            using (FileStream fs = new FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
                {
                    //StreamReader sr = new StreamReader(fs, encoding);
                    //string fileContent = sr.ReadToEnd();
                    //encoding = sr.CurrentEncoding;
                    //记录每次读取的一行记录
                    string strLine = "";
                    //记录每行记录中的各字段内容
                    string[] aryLine = null;
                    string[] tableHead = null;
                    //标示列数
                    int columnCount = 0;
                    //标示是否是读取的第一行
                    bool IsFirst = true;
                    //逐行读取CSV中的数据
                    while ((strLine = sr.ReadLine()) != null)
                    {
                        //strLine = Common.ConvertStringUTF8(strLine, encoding);
                        //strLine = Common.ConvertStringUTF8(strLine);

                        if (IsFirst == true)
                        {
                            tableHead = strLine.Split(',');
                            IsFirst = false;
                            columnCount = tableHead.Length;
                            //创建列
                            for (int i = 0; i < columnCount; i++)
                            {
                                DataColumn dc = new DataColumn(tableHead[i]);
                                dt.Columns.Add(dc);
                            }
                        }
                        else
                        {
                            aryLine = strLine.Split(',');
                            DataRow dr = dt.NewRow();
                            for (int j = 0; j < columnCount; j++)
                            {
                                dr[j] = aryLine[j];
                            }
                            dt.Rows.Add(dr);
                        }
                    }
                    if (aryLine != null && aryLine.Length > 0)
                    {
                        dt.DefaultView.Sort = tableHead[0] + " " + "asc";
                    }

                    sr.Close();
                    fs.Close();
                }
            }
            return dt;
        }
    }
}
