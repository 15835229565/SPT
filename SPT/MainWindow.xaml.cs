using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;
using DevExpress.Xpf.Core;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;
using System.ComponentModel;

namespace SPT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow
    {
        public static ViewModel vm = new ViewModel();
        /// <summary>
        /// 定义一个后台运行Worker
        /// </summary>
        public BackgroundWorker backgroundWorker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = vm;
            List<string> packList = new List<string> { };
            for (int i = 0; i < 16; i++)
            {
                packList.Add(string.Format("PACK#{0}", i));
            }
            PackComoBox.ItemsSource = packList;
            PackComoBox.SelectedIndex = 0;
            baudComboBox.ItemsSource = Provider.arrBaudrate;
            sendComobox.ItemsSource = Provider.arrSendMode;
            MessageList.ItemsSource = vm.ListMessage;
            vm.FocusLastItem += AutoScroll;
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// 后台操作线程 DoWork
        /// </summary>             
        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            DataFrame reqFrame = (DataFrame)e.Argument;
            string strReciveMsg;

            try
            {
                strReciveMsg = vm.SendAndReceiveMessage(reqFrame.ToBytes());
                if (strReciveMsg == null)
                {
                    Log4Helper.Error(GetType(), "串口未打开等异常！", null);
                    return;//串口未打开等异常
                }
                e.Result = new CommResult(reqFrame, strReciveMsg);
            }

            catch (TimeoutException)
            {
                strReciveMsg = string.Empty;
                e.Result = new CommResult(reqFrame, strReciveMsg);
            }
            catch (Exception ex)
            {
                strReciveMsg = string.Empty;
                e.Result = new CommResult(reqFrame, strReciveMsg, ex.Message);
            }

            if (worker.CancellationPending == true)
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 后台操作线程 RunWorkerCompleted
        /// </summary>         
        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                OpenPort_Unchecked(this, null);
                DXMessageBox.Show("操作异常！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);

                CommandManager.InvalidateRequerySuggested();
                return;
            }
            else if (e.Cancelled)
            {
                return;
            }

            CommResult result = (CommResult)e.Result;

            if (result.RecieveStr == string.Empty)
            {
                if (result.ErrMsg == string.Empty) //-接收超时
                {
                    DXMessageBox.Show("反馈超时！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else  //-error
                {
                    OpenPort_Unchecked(this, null);
                    DXMessageBox.Show("操作异常！" + result.ErrMsg, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                return;
            }
            try
            {
                DataFrame rcvFrame = DataFrame.Parse(result.RecieveStr);

                if (rcvFrame.CID2 != DataFrame.RTN_NoneErr)
                {
                    DXMessageBox.Show(DataFrame.GetReturnMessage(rcvFrame.CID2), "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TreatRcvFrame(result.RequestFrame, rcvFrame);

            }

            catch (Exception ex)
            {
                Log4Helper.Error(GetType(), "数据接收或者处理异常！", ex);
                DXMessageBox.Show("数据接收或者处理异常！" + ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// 处理正常通信消息（根据请求和回复消息体）
        /// </summary>
        /// <param name="request">请求消息体</param>
        /// <param name="response">回复消息体</param>
        private void TreatRcvFrame(DataFrame request, DataFrame response)
        {
            switch (response.CID2)
            {
                case ProtocolCommand.CID2_TeleMeter:
                    break;
                case ProtocolCommand.CID2_Adjust:
                    break;
                default:
                    break;
            }
        }

        private void TxdRequestFrame(DataFrame frame)
        {
            if (frame == null)
            {
                return;
            }
            if (backgroundWorker.IsBusy == false)
            {
                backgroundWorker.RunWorkerAsync(frame);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            portComboBox_DropDownOpened(this, null);
            portComboBox.SelectedIndex = 0;

            if (Properties.Settings.Default.FilePath != string.Empty)
            {
                if (vm.LoadCase(Properties.Settings.Default.FilePath) != null)
                {
                    DXMessageBox.Show("加载Case失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                string filePath = AppDomain.CurrentDomain.BaseDirectory + "\\" + "Case.xls";
                if (File.Exists(filePath))
                {
                    if (vm.LoadCase(filePath) != null)
                    {
                        DXMessageBox.Show("加载Case失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            DXSplashScreen.Close();
        }

        private void SimpleButton_Click(object sender, RoutedEventArgs e)
        {
            vm.SendAndReceiveMessage(vm.GetSendBytes(selfSendText.Text.Trim()));
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            OpenPort_Unchecked(this, null);
        }

        private void portComboBox_DropDownOpened(object sender, EventArgs e)
        {
            string[] ports = SerialPort.GetPortNames();
            portComboBox.ItemsSource = ports;
            if (ports.Count() <= 0)
            {
                vm.CommPortSelected = false;
            }
        }

        private void OpenPort_Checked(object sender, RoutedEventArgs e)
        {
            if (portComboBox.SelectedItem == null)
            {
                OpenPort.IsChecked = false;
                return;
            }
            OpenPort.ToolTip = "关闭串口";
            if (!vm.OpenPort(portComboBox.SelectedItem.ToString()))
            {
                string errMsg = String.Format("{0}已经被其他程序占用!   \n\n 请选择其他端口.  ", portComboBox.SelectedItem.ToString());
                DXMessageBox.Show(errMsg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                OpenPort.IsChecked = false;
            }
        }
        private void OpenPort_Unchecked(object sender, RoutedEventArgs e)
        {
            vm.ClosePort();
            OpenPort.ToolTip = "打开串口";
        }

        /// <summary>
        /// 滚动条自动滚动
        /// </summary>
        private void AutoScroll(DataType _type)
        {
            switch (_type)
            {
                case DataType.LogStatus:
                    if (StatusList.Items.Count > 0)
                    {
                        StatusList.ScrollIntoView(StatusList.Items[StatusList.Items.Count - 1]);
                    }
                    break;
                case DataType.MessageText:
                    if (MessageList.Items.Count > 0)
                    {
                        MessageList.ScrollIntoView(MessageList.Items[MessageList.Items.Count - 1]);
                    }
                    break;
                default:
                    break;
            }
        }

        private void allSelect_Checked(object sender, RoutedEventArgs e)
        {
            vm.AllSelect();
        }

        private void allSelect_Unchecked(object sender, RoutedEventArgs e)
        {
            vm.UnAllSelect();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            vm.AddRow();
        }
        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            vm.RemoveRow();
        }

        private void DXTabControl_SelectionChanged(object sender, TabControlSelectionChangedEventArgs e)
        {
            if (TabControlTest.SelectedIndex == 0)
            {
                addRowButton.Visibility = System.Windows.Visibility.Collapsed;
                removeRowButton.Visibility = System.Windows.Visibility.Collapsed;
                PackComoBox.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                addRowButton.Visibility = System.Windows.Visibility.Visible;
                removeRowButton.Visibility = System.Windows.Visibility.Visible;
                PackComoBox.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void Flag_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex re = new Regex("[^0-9]");
            e.Handled = re.IsMatch(e.Text);
        }


        #region Command-打开Case
        /// <summary>
        /// 加载案例命令
        /// </summary>
        private static RoutedUICommand loadNewCase = new RoutedUICommand("LoadNewCase", "LoadNewCase", typeof(MainWindow));
        public static RoutedUICommand LoadNewCase
        {
            get { return loadNewCase; }
        }


        private void LoadNewCase_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void LoadNewCase_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog();
            op.Filter = "Excel文件|*.xls|CSV文件|*.CSV";
            op.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            bool? result = op.ShowDialog();
            if (result == true)
            {
                if (vm.LoadCase(op.FileName) != null)
                {
                    DXMessageBox.Show("加载Case失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    TabControlTest.SelectedIndex = 1;
                }
            }
        }
        #endregion

        #region Command-导出case
        /// <summary>
        /// 导出案例命令
        /// </summary>
        private static RoutedUICommand exportCase = new RoutedUICommand("ExportCase", "ExportCase", typeof(MainWindow));
        public static RoutedUICommand ExportCase
        {
            get { return exportCase; }
        }


        private void ExportCase_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void ExportCase_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
            saveFileDialog.AddExtension = true;
            saveFileDialog.OverwritePrompt = true;
            saveFileDialog.CheckPathExists = true;
            if (e.Parameter.ToString() == "Case")
            {
                saveFileDialog.DefaultExt = "*.CSV";
                saveFileDialog.Filter = "CSV files|*.CSV";
                saveFileDialog.FileName = "NewCase";
                bool? result = saveFileDialog.ShowDialog();
                if (result == true && saveFileDialog.FileName != null) //打开保存文件对话框
                {
                    if (vm.ExportCSV(saveFileDialog.FileName))
                    {
                        DXMessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        DXMessageBox.Show("导出失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            else
            {
                saveFileDialog.Filter = "文本文件(.txt)|*.txt";
                saveFileDialog.FileName = "Log";
                bool? result = saveFileDialog.ShowDialog();
                if (result == true && saveFileDialog.FileName != null) //打开保存文件对话框
                {
                    if (vm.ExportTxt(saveFileDialog.FileName))
                    {
                        DXMessageBox.Show("导出成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        DXMessageBox.Show("导出失败！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

        }
        #endregion

        #region Command-Test
        /// <summary>
        /// 下发命令
        /// </summary>
        private static RoutedUICommand sendCommand = new RoutedUICommand("SendCommand", "SendCommand", typeof(MainWindow));
        public static RoutedUICommand SendCommand
        {
            get { return sendCommand; }
        }


        private void SendCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void SendCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            vm.SendSelectedRowData();
        }
        #endregion

        #region Command-Start/Stop
        /// <summary>
        /// 测试
        /// </summary>
        private static RoutedUICommand operateTest = new RoutedUICommand("OperateTest", "OperateTest", typeof(MainWindow));
        public static RoutedUICommand OperateTest
        {
            get { return operateTest; }
        }


        private void OperateTest_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void OperateTest_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter.ToString() == "Start")
            {
                vm.StartTest();
            }
            else
            {
                vm.EndTest(true);
            }
        }
        #endregion

        #region Command-关机指令
        /// <summary>
        /// 测试
        /// </summary>
        private static RoutedUICommand exitApp = new RoutedUICommand("ExitApp", "ExitApp", typeof(MainWindow));
        public static RoutedUICommand ExitApp
        {
            get { return exitApp; }
        }
        private void ExitApp_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void ExitApp_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region Command-端口设置
        /// <summary>
        /// 测试
        /// </summary>
        private static RoutedUICommand portSet = new RoutedUICommand("PortSet", "PortSet", typeof(MainWindow));
        public static RoutedUICommand PortSet
        {
            get { return portSet; }
        }
        private void PortSet_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }

        private void PortSet_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            PortSettings ps = new PortSettings(vm);
            ps.ShowDialog();
        }

        #endregion

        #region Command-下发命令
        /// <summary>
        /// 测试
        /// </summary>
        private static RoutedUICommand sendMessageCommand = new RoutedUICommand("SendMessageCommand", "SendMessageCommand", typeof(MainWindow));
        public static RoutedUICommand SendMessageCommand
        {
            get { return sendMessageCommand; }
        }
        private void SendMessageCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = OpenPort.IsChecked == true;
            e.Handled = true;
        }

        private void SendMessageCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            DataFrame requestFrame = null;
            if (e.Parameter.ToString() == "ReadRemoteInfo")
            {
                ParaInfo[] paraList = new ParaInfo[] { new ParaInfo { Value = PackComoBox.SelectedIndex, ByteNum = 0x01 } };
                requestFrame = new DataFrame((byte)vm.DataFrameVER, ProtocolCommand.CID2_TeleMeter, paraList);
                requestFrame.Comment = string.Format("读取Pack{0}遥测信息", PackComoBox.SelectedIndex);
            }
            else if (e.Parameter.ToString() == "adjustZero")
            {
                ParaInfo[] paraList = new ParaInfo[3];
                paraList[0].Value = PackComoBox.SelectedIndex; paraList[0].ByteNum = 1;
                paraList[1].Value = AdjustCommand.adjustZero; paraList[1].ByteNum = 1;
                paraList[2].Value = vm.ZeroValue; paraList[2].ByteNum = 4;
                requestFrame = new DataFrame((byte)vm.DataFrameVER, ProtocolCommand.CID2_Adjust, paraList);
                requestFrame.Comment = string.Format("下发Pack{0}零点校准", PackComoBox.SelectedIndex);
            }
            else if (e.Parameter.ToString() == "adjustC")
            {
                ParaInfo[] paraList = new ParaInfo[3];
                paraList[0].Value = PackComoBox.SelectedIndex; paraList[0].ByteNum = 1;
                paraList[1].Value = AdjustCommand.adjustCurrent; paraList[1].ByteNum = 1;
                paraList[2].Value = vm.CurrentValue; paraList[2].ByteNum = 4;
                requestFrame = new DataFrame((byte)vm.DataFrameVER, ProtocolCommand.CID2_Adjust, paraList);
                requestFrame.Comment = string.Format("下发Pack{0}电流K值校准", PackComoBox.SelectedIndex);
            }
            else if (e.Parameter.ToString() == "adjustV")
            {
                ParaInfo[] paraList = new ParaInfo[3];
                paraList[0].Value = PackComoBox.SelectedIndex; paraList[0].ByteNum = 1;
                paraList[1].Value = AdjustCommand.adjustVoltage; paraList[1].ByteNum = 1;
                paraList[2].Value = vm.VoltageValue; paraList[2].ByteNum = 4;
                requestFrame = new DataFrame((byte)vm.DataFrameVER, ProtocolCommand.CID2_Adjust, paraList);
                requestFrame.Comment = string.Format("下发Pack{0}电压K值校准", PackComoBox.SelectedIndex);
            }
            else if (e.Parameter.ToString() == "adjustID")
            {
                ParaInfo[] paraList = new ParaInfo[3];
                paraList[0].Value = PackComoBox.SelectedIndex; paraList[0].ByteNum = 1;
                paraList[1].Value = AdjustCommand.adjustPakID; paraList[1].ByteNum = 1;
                paraList[2].Value = vm.PackIDValue; paraList[2].ByteNum = 4;
                requestFrame = new DataFrame((byte)vm.DataFrameVER, ProtocolCommand.CID2_Adjust, paraList);
                requestFrame.Comment = string.Format("下发Pack{0}ID校准", PackComoBox.SelectedIndex);
            }
            TxdRequestFrame(requestFrame);
        }

        #endregion

    }
}
