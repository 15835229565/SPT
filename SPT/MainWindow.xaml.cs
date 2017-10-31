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

namespace SPT
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXWindow
    {
        public static ViewModel vm = new ViewModel();

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = vm;
            baudComboBox.ItemsSource = Provider.arrBaudrate;
            sendComobox.ItemsSource = Provider.arrSendMode;
            vm.FocusLastItem += AutoScroll;
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
        private void AutoScroll()
        {
            StatusList.ScrollIntoView(StatusList.Items[StatusList.Items.Count - 1]);
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

    }
}
