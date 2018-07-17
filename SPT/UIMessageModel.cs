using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DevExpress.Mvvm;
using System.Windows.Media;

namespace SPT
{
    public enum Direction
    {
        Send,
        Received
    }
    /// <summary>
    /// 报文管理
    /// </summary>
    public class MessageModel : ViewModelBase
    {
        public MessageModel(Direction _direction, string _content, string error = "")
        {
            MessageDirection = _direction;
            MessageContent = _content;
            MessageTime = DateTime.Now;
            MessageError = error;
            ForeColor = MessageDirection == Direction.Received ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Gold);
        }
        /// <summary>
        /// 报文时间
        /// </summary>
        private DateTime messageTime;
        public DateTime MessageTime
        {
            get { return messageTime; }
            set
            {
                SetProperty<DateTime>(ref messageTime, value, "MessageTime");
            }
        }
        private SolidColorBrush foreColor;
        public SolidColorBrush ForeColor
        {
            get { return foreColor; }
            set
            {
                SetProperty<SolidColorBrush>(ref foreColor, value, "ForeColor");
            }
        }
        /// <summary>
        /// 报文方向
        /// </summary>
        private Direction messageDirection;
        public Direction MessageDirection
        {
            get { return messageDirection; }
            set
            {
                SetProperty<Direction>(ref messageDirection, value, "MessageDirection");
            }
        }
        /// <summary>
        /// 报文内容
        /// </summary>
        private string messageContent;
        public string MessageContent
        {
            get { return messageContent; }
            set
            {
                SetProperty<string>(ref messageContent, value, "MessageContent");
            }
        }
        /// <summary>
        /// 报文错误信息
        /// </summary>
        private string messageError;
        public string MessageError
        {
            get { return messageError; }
            set
            {
                SetProperty<string>(ref messageError, value, "MessageError");
            }
        }
    }
}
