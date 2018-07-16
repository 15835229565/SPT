using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DevExpress.Mvvm;

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
        public MessageModel(Direction _direction, string _content, string _arror = "")
        {
            MessageDirection = _direction;
            MessageContent = _content;
            MessageArror = _arror;
            MessageTime = DateTime.Now;
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
        /// 异常信息
        /// </summary>
        private string messageArror;
        public string MessageArror
        {
            get { return messageArror; }
            set
            {
                SetProperty<string>(ref messageArror, value, "MessageArror");
            }
        }
    }    
}
