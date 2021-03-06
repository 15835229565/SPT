﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.IO;
using System.IO.Ports;
using System.Data;
using System.Windows.Threading;

namespace SPT
{

    /// <summary>
    /// 通信结果情况
    /// </summary>
    public class CommResult
    {
        public CommResult(DataFrame request, string rcecieve, string errMsg = "")
        {
            RequestFrame = request;
            RecieveStr = rcecieve;
            ErrMsg = errMsg;
        }
        /// <summary>
        /// 此通信的请求消息体
        /// </summary>
        public DataFrame RequestFrame;
        /// <summary>
        /// 此通信接收的内容
        /// </summary>
        public string RecieveStr;
        /// <summary>
        /// 此通信的异常信息
        /// </summary>
        public string ErrMsg;
    }
    public class ViewModel : INotifyPropertyChanged
    {
        public static SerialPort serialPort1 = new SerialPort();
        public static DispatcherTimer ExcuteTimer = new DispatcherTimer();

        public byte DataFrameVER = 0x26;

        public ViewModel()
        {
            TestTable.Columns.Add("Select", typeof(bool));
            TestTable.Columns.Add("Comment", typeof(string));
            TestTable.Columns.Add("Content", typeof(string));
            ExcuteTimer.Tick += new EventHandler(ExcuteTimer_Tick);
            //初始化SerialPort对象  
            serialPort1.RtsEnable = true;//根据实际情况吧。  
            //添加事件注册  
            serialPort1.DataReceived += comm_DataReceived;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

        #region 串口变量
        private int baudRate = Properties.Settings.Default.BaudRate;
        public int BaudRate
        {
            get { return baudRate; }
            set
            {
                baudRate = value;
                Properties.Settings.Default.BaudRate = baudRate;
                OnPropertyChanged(new PropertyChangedEventArgs("BaudRate"));
            }
        }

        private int dataBits = Properties.Settings.Default.DataBits;
        public int DataBits
        {
            get { return dataBits; }
            set
            {
                dataBits = value;
                OnPropertyChanged(new PropertyChangedEventArgs("DataBits"));
            }
        }

        private string stopBits = Properties.Settings.Default.StopBits.ToString();
        public string StopBits
        {
            get { return stopBits; }
            set
            {
                stopBits = value;
                OnPropertyChanged(new PropertyChangedEventArgs("StopBits"));
            }
        }
        private string parity = Properties.Settings.Default.Parity.ToString();
        public string Parity
        {
            get { return parity; }
            set
            {
                parity = value;
                OnPropertyChanged(new PropertyChangedEventArgs("Parity"));
            }
        }
        #endregion

        private bool isStart = false;
        public bool IsStart
        {
            get { return isStart; }
            set
            {
                isStart = value;
                OnPropertyChanged(new PropertyChangedEventArgs("IsStart"));
            }
        }

        private int interval = Properties.Settings.Default.Interval;
        public int Interval
        {
            get { return interval; }
            set
            {
                interval = value;
                ExcuteTimer.Interval = new TimeSpan(0, 0, 0, Interval);
                Properties.Settings.Default.Interval = interval;
                OnPropertyChanged(new PropertyChangedEventArgs("Interval"));
            }
        }

        private int modeIndex = Properties.Settings.Default.SelectModeIndex;
        public int ModeIndex
        {
            get { return modeIndex; }
            set
            {
                modeIndex = value;
                Properties.Settings.Default.SelectModeIndex = modeIndex;
                OnPropertyChanged(new PropertyChangedEventArgs("ModeIndex"));
            }
        }

        private DataTable testTable = new DataTable();
        public DataTable TestTable
        {
            get { return testTable; }
            set
            {
                testTable = value;
                OnPropertyChanged(new PropertyChangedEventArgs("TestTable"));
            }
        }

        private bool commPortSelected = false;
        public bool CommPortSelected
        {
            get { return commPortSelected; }
            set
            {
                commPortSelected = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CommPortSelected"));
            }
        }

        private int selectedRowIndex = 0;
        public int SelectedRowIndex
        {
            get { return selectedRowIndex; }
            set
            {
                selectedRowIndex = value;
                OnPropertyChanged(new PropertyChangedEventArgs("SelectedRowIndex"));
            }
        }

        private int selectPackID = 0;
        public int SelectPackID
        {
            get { return selectPackID; }
            set
            {
                selectPackID = value;
                OnPropertyChanged(new PropertyChangedEventArgs("SelectPackID"));
            }
        }

        /// <summary>
        /// 状态列表
        /// </summary>
        public ObservableCollection<string> ListStatus = new ObservableCollection<string> { };

        /// <summary>
        /// 报文列表
        /// </summary>
        public ObservableCollection<MessageModel> ListMessage = new ObservableCollection<MessageModel> { };

        #region 高压测试板校准汇总

        private int zeroValue = 0;
        /// <summary>
        /// 零点校准值
        /// </summary>
        public int ZeroValue
        {
            get { return zeroValue; }
            set
            {
                zeroValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ZeroValue"));
            }
        }
        private int currentValue = 0;
        /// <summary>
        /// 电流校准值
        /// </summary>
        public int CurrentValue
        {
            get { return currentValue; }
            set
            {
                currentValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CurrentValue"));
            }
        }
        private int voltageValue = 0;
        /// <summary>
        /// 电压校准值
        /// </summary>
        public int VoltageValue
        {
            get { return voltageValue; }
            set
            {
                voltageValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs("VoltageValue"));
            }
        }
        private int packIDValue = 0;
        /// <summary>
        /// packID校准值
        /// </summary>
        public int PackIDValue
        {
            get { return packIDValue; }
            set
            {
                packIDValue = value;
                OnPropertyChanged(new PropertyChangedEventArgs("PackIDValue"));
            }
        }
        #endregion

        public bool OpenPort(string ComName)
        {
            serialPort1.DataBits = Properties.Settings.Default.DataBits;
            serialPort1.StopBits = Properties.Settings.Default.StopBits;
            serialPort1.Parity = Properties.Settings.Default.Parity;
            //超时设定为6秒
            serialPort1.ReadTimeout = 3000;

            if (serialPort1.IsOpen == false)
            {
                try
                {
                    serialPort1.BaudRate = BaudRate;
                    serialPort1.PortName = ComName;
                    serialPort1.Open();
                }
                catch (UnauthorizedAccessException)
                {
                    return false;
                }
            }
            return true;
        }

        public void ClosePort()
        {
            Closing = true;
            while (Listening) ;
            //打开时点击，则关闭串口  
            serialPort1.Close();
            Closing = false;
            EndTest(false);
        }

        private bool Listening = false;//是否没有执行完invoke相关操作  
        private bool Closing = false;//是否正在关闭串口，执行Application.DoEvents，并阻止再次invoke 
        private void comm_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (Closing) return;//如果正在关闭，忽略操作，直接返回，尽快的完成串口监听线程的一次循环  
            try
            {
                Listening = true;//设置标记，说明我已经开始处理数据，一会儿要使用系统UI的。  
                int n = serialPort1.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致  
                byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据    
                serialPort1.Read(buf, 0, n);//读取缓冲数据  
                if (DisposeReceivedData != null)
                {
                    DisposeReceivedData(buf);
                }
            }
            finally
            {
                Listening = false;//我用完了，ui可以关闭串口了。  
            }
        }

        public void SendMessage(byte[] arrFrame)
        {
            if (serialPort1.IsOpen)
            {
                serialPort1.DiscardInBuffer();
                serialPort1.Write(arrFrame, 0, arrFrame.Length);
                AddMessageToList(new MessageModel(Direction.Send, GetMessage(arrFrame)));
            }
        }

        public string SendAndReceiveMessage(byte[] arrFrame)
        {
            if (arrFrame == null)
            {
                return null;
            }
            string strReciveMsg = null;
            try
            {
                if (serialPort1.IsOpen)
                {
                    serialPort1.DiscardInBuffer();
                    string txdString = Encoding.ASCII.GetString(arrFrame);
                    serialPort1.Write(txdString);

                    AddMessageToList(new MessageModel(Direction.Send, GetMessage(arrFrame)));

                    strReciveMsg = serialPort1.ReadTo("\x0D");
                    if (strReciveMsg != string.Empty)
                        strReciveMsg += "\x0D";

                    if (strReciveMsg == txdString) //-this is for RS485 Comm
                    {
                        strReciveMsg = serialPort1.ReadTo("\x0D");
                        if (strReciveMsg != string.Empty)
                            strReciveMsg += "\x0D";
                    }
                    AddMessageToList(new MessageModel(Direction.Received, GetMessage(Encoding.ASCII.GetBytes(strReciveMsg))));
                }
            }
            catch (Exception ex)
            {

                throw (ex);
            }

            return strReciveMsg;
        }

        public string GetMessage(byte[] buffer)
        {
            string message = string.Empty;
            foreach (var item in buffer)
            {
                message += string.Format("{0:X2} ", item);
            }
            return message;
        }
        public void AddMessageToList(MessageModel mm)
        {
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(delegate()
                {
                    ListMessage.Add(mm);
                    int count = ListMessage.Count;
                    if (count > 1000)
                    {
                        for (int i = 0; i < count - 1000; i++)
                        {
                            ListMessage.RemoveAt(0);
                        }
                    }
                    if (FocusLastItem != null)
                    {
                        FocusLastItem(DataType.MessageText);
                    }
                }));
        }

        public string LoadCase(string filePath)
        {
            DataTable dt = new DataTable();
            try
            {
                if (Path.GetExtension(filePath).ToUpper() == ".CSV")
                {
                    dt = CSVFileHelper.OpenCSV(filePath);
                }
                else
                {
                    dt = Provider.ExcelToDS(filePath).Tables[0];
                }
                TestTable.Rows.Clear();
                foreach (DataRow item in dt.Rows)
                {
                    DataRow dr = TestTable.NewRow();
                    dr["Select"] = true;
                    dr["Comment"] = item[1].ToString();
                    dr["Content"] = item[2].ToString();
                    TestTable.Rows.Add(dr);
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
            Properties.Settings.Default.FilePath = filePath;
            Properties.Settings.Default.Save();
            return null;

        }

        public bool ExportCSV(string filePath)
        {
            if (!CSVFileHelper.SaveCSV(TestTable, filePath))
            {
                return false;
            }
            return true;

        }
        public bool ExportTxt(string filePath)
        {
            if (!Provider.SaveTxt(filePath, ListStatus.ToArray()))
            {
                return false;
            }
            return true;

        }

        public void AddRow()
        {
            if (SelectedRowIndex < 0)
            {
                return;
            }
            DataRow dr = TestTable.NewRow();
            TestTable.Rows.InsertAt(dr, SelectedRowIndex);
        }
        public void RemoveRow()
        {
            if (SelectedRowIndex < 0 || TestTable.Rows.Count == 0)
            {
                return;
            }
            TestTable.Rows.RemoveAt(SelectedRowIndex);
        }

        public void AllSelect()
        {
            foreach (DataRow item in TestTable.Rows)
            {
                item[0] = true;
            }
        }
        public void UnAllSelect()
        {
            foreach (DataRow item in TestTable.Rows)
            {
                item[0] = false;
            }
        }

        public void SendSelectedRowData()
        {
            WriteSerial(TestTable.Rows[SelectedRowIndex]);
        }

        private int sendIndex = 0;
        private DataTable sendDt = new DataTable();
        private void ExcuteTimer_Tick(object sender, EventArgs e)
        {
            WriteSerial(sendDt.Rows[sendIndex]);
            sendIndex++;
            if (sendIndex == sendDt.Rows.Count)
            {
                if (modeIndex == 1)
                    sendIndex = 0;
                else
                    EndTest(true);
            }
        }

        public void StartTest()
        {
            sendDt = TestTable.Clone();
            foreach (DataRow item in TestTable.Rows)
            {
                if (item[0].ToString().ToLower() == "true")
                {
                    sendDt.Rows.Add(item.ItemArray);
                }
            }
            if (sendDt.Rows.Count > 0)
            {
                ExcuteTimer.Interval = new TimeSpan(0, 0, 0, Interval);
                ExcuteTimer.Start();
                AddItemsToStatus(false, "测试开始！");
                IsStart = true;
            }
        }

        public void EndTest(bool OnTest)
        {
            if (OnTest)
            {
                AddItemsToStatus(false, "测试结束！");
            }

            IsStart = false;
            ExcuteTimer.Stop();
            sendIndex = 0;
            sendDt = new DataTable();
        }
        public void WriteSerial(DataRow testDr)
        {
            if (serialPort1.IsOpen)
            {
                var sendByte = GetSendBytes(testDr["Content"].ToString());
                if (sendByte == null)
                {
                    return;
                }
                serialPort1.DiscardInBuffer();
                serialPort1.Write(sendByte, 0, sendByte.Length);
                AddItemsToStatus(true, testDr["Comment"].ToString().Trim());
            }
        }
        public byte[] GetSendBytes(string content)
        {
            if (content.Trim() == string.Empty)
            {
                return null;
            }
            string[] arrStr = content.Trim().Split(' ');
            byte[] arrByte = new byte[arrStr.Length];
            for (int i = 0; i < arrStr.Length; i++)
            {
                arrByte[i] = Convert.ToByte(arrStr[i], 16);
            }
            return arrByte;
        }
        /// <summary>
        /// 增加状态信息到列表里
        /// </summary>
        /// <param name="isCommand"></param>
        /// <param name="strStatus"></param>
        public void AddItemsToStatus(bool isCommand, string strStatus)
        {
            if (isCommand)
            {
                strStatus = DateTime.Now.ToString() + "： 已下发" + "“" + strStatus + "”" + "测试指令";
            }
            else
            {
                strStatus = DateTime.Now.ToString() + strStatus;
            }

            ListStatus.Add(strStatus);
            if (FocusLastItem != null)
            {
                FocusLastItem(DataType.LogStatus);
            }
        }
        /// <summary>
        /// 委托定义，用于控制界面报文信息刷新
        /// </summary>
        public Action<DataType> FocusLastItem = null;
        /// <summary>
        /// 委托定义，用于处理接收的信息并且反馈到界面
        /// </summary>
        public Action<byte[]> DisposeReceivedData = null;

    }
    public enum DataType
    {
        LogStatus,
        MessageText
    }
}
