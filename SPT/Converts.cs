using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;

namespace SPT
{
    #region 取反转换
    public class ReverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter.ToString()=="Start")
            {
                if ((bool)value)
                {
                    return Visibility.Collapsed; 
                }
                else
                {
                    return Visibility.Visible; 
                }
            }
            else if (parameter.ToString() == "Stop")
            {
                if ((bool)value)
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Hidden;
                }
            }
            else
            {
                bool state = !(bool)value;
                return state;
            }            
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region 发送方向图片转换
    public class ReverseGlphyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((Direction)value == Direction.Received)
            {
                if (parameter.ToString() == "Text")
                {
                    return "接收";
                }
                else
                    return Provider.GetImage("Assets/backward_32x32.png");
            }
            else
            {
                if (parameter.ToString() == "Text")
                {
                    return "发送";
                }
                else
                    return Provider.GetImage("Assets/forward_32x32.png");
            }
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    #endregion
}
