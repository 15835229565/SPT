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
using System.Windows.Shapes;
using DevExpress.Xpf.Core;
using System.IO;


namespace SPT
{
    /// <summary>
    /// Interaction logic for PortSettings.xaml
    /// </summary>
    public partial class PortSettings : DXWindow
    {
        private ViewModel vm = new ViewModel();
        public PortSettings(ViewModel _viewModel)
        {
            InitializeComponent();
            vm = _viewModel;
            this.DataContext = vm;
        }

        private void PortSettings_Loaded(object sender, RoutedEventArgs e)
        {
            baudComobox.ItemsSource = Provider.arrBaudrate;

            parityComobox.ItemsSource = Provider.arrCheckMode;

            dataComobox.ItemsSource = Provider.arrDataBit;

            stopComobox.ItemsSource = Provider.arrStopBits;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DataBits = (int)dataComobox.SelectedItem;
            Properties.Settings.Default.StopBits = Provider.GetStopBits(stopComobox.SelectedItem.ToString());
            Properties.Settings.Default.Parity = Provider.GetParity(parityComobox.SelectedItem.ToString());
            Properties.Settings.Default.BaudRate = (int)baudComobox.SelectedItem;
            Properties.Settings.Default.Save();
            this.Close();
        }
    }
}
