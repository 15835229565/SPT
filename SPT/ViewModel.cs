using System;
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
    public class ViewModel : INotifyPropertyChanged
    {
        public static SerialPort serialPort1 = new SerialPort();
        public static DispatcherTimer ExcuteTimer = new DispatcherTimer();

        public ViewModel()
        {
            TestTable.Columns.Add("Select", typeof(bool));
            TestTable.Columns.Add("Comment", typeof(string));
            TestTable.Columns.Add("Content", typeof(string));
            ExcuteTimer.Tick += new EventHandler(ExcuteTimer_Tick);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

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
        private ObservableCollection<string> listStatus = new ObservableCollection<string> { };
        public ObservableCollection<string> ListStatus
        {
            get { return listStatus; }
            set
            {
                listStatus = value;
                OnPropertyChanged(new PropertyChangedEventArgs("ListStatus"));
            }
        }


        public bool OpenPort(string ComName)
        {
            serialPort1.DataBits = Properties.Settings.Default.DataBits;
            serialPort1.StopBits = Properties.Settings.Default.StopBits;
            serialPort1.Parity = Properties.Settings.Default.Parity;
            //超时设定为6秒
            serialPort1.ReadTimeout = 6000;

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
            serialPort1.Close();
            EndTest(false);
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
            if (SelectedRowIndex < 0)
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
                string[] arrStr = testDr["Content"].ToString().Trim().Split(' ');
                byte[] arrByte = new byte[arrStr.Length];
                for (int i = 0; i < arrStr.Length; i++)
                {
                    arrByte[i] = Convert.ToByte(arrStr[i], 16);
                }
                serialPort1.DiscardInBuffer();
                serialPort1.Write(arrByte, 0, arrByte.Length);
                AddItemsToStatus(true, testDr["Comment"].ToString().Trim());
            }
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
            FocusLastItem();
        }
        /// <summary>
        /// 委托定义，用于控制界面元素
        /// </summary>
        public delegate void ScrollToEnd();
        public ScrollToEnd FocusLastItem = null;

    }
}
