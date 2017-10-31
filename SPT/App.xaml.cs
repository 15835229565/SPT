using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using DevExpress.Xpf.Core;

namespace SPT
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void OnAppStartup_UpdateThemeName(object sender, StartupEventArgs e)
        {
            DevExpress.Xpf.Core.ApplicationThemeHelper.UpdateApplicationThemeName();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            SPT.Properties.Settings.Default.Save();
            //关闭所有线程，即关闭此进程
            System.Environment.Exit(0);
        }
    }
}
