using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace SPT
{
    /// <summary>
    /// 定义一个COMMAND_INFO的简单数据结构，value=命令内容，ByteNum=字节数
    /// </summary>
    public struct ParaInfo
    {
        public int Value;
        public int ByteNum;
    }
    public enum FrameDirection { Request, Response };
    /// <summary>
    /// 定义一个消息体的标准格式及常用处理方法
    /// </summary>
    public class DataFrame : object
    {
        /// <summary>
        /// 消息方向，Request表示请求消息，Reponse表示返回消息
        /// </summary>
        public FrameDirection Direction;
        /// <summary>
        /// 起始位标志,ASCII码7EH（固定)
        /// </summary>
        public const Byte SOI = 0x7E;
        /// <summary>
        /// 协议版本号
        /// </summary>
        public Byte VER;
        /// <summary>
        /// 设备地址,HEX码00H（固定）
        /// </summary>
        public const Byte ADR = 0x00;
        /// <summary>
        /// 设备标识码,HEX码46H（固定）
        /// </summary>
        public const Byte CID1 = 0x46;
        /// <summary>
        /// 命令信息:控制标识码
        /// 响应信息：返回码
        /// </summary>
        public Byte CID2;
        /// <summary>
        /// INFO字节长度
        /// </summary>
        public int LENGTH;
        /// <summary>
        /// 命令信息：控制数据信息（COMMAND_INFO）
        /// 应答信息：应答数据信息（DATA_INFO）
        /// </summary>
        public List<Byte> INFO;
        /// <summary>
        /// 校验和码
        /// </summary>
        public UInt16 CHECKSUM;
        /// <summary>
        /// 结束码
        /// </summary>
        public const Byte EOI = 0x0D;

        /// <summary>
        /// 消息备注，表示当前消息体（Reponse或者Request）的意义
        /// </summary>
        public string Comment;

        public DataFrame() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ver">协议版本号</param>
        /// <param name="cid2">命令信息/应答信息</param>
        /// <param name="paraArray">命令内容/应答内容</param>
        public DataFrame(Byte ver, Byte cid2, params ParaInfo[] paraArray)
        {
            Direction = FrameDirection.Request;

            VER = ver;
            CID2 = cid2;

            if (paraArray.Length == 0)
            {
                LENGTH = 0;
                INFO = null;
            }
            else
            {
                INFO = new List<byte>();

                foreach (ParaInfo para in paraArray)
                {
                    if (para.ByteNum == 4)
                    {
                        INFO.Add((byte)((para.Value >> 24) & 0x0FF));
                        INFO.Add((byte)((para.Value >> 16) & 0x0FF));
                        INFO.Add((byte)((para.Value >> 8) & 0x0FF));
                    }

                    if (para.ByteNum == 2)
                    {
                        INFO.Add((byte)((para.Value >> 8) & 0x0FF));
                    }

                    INFO.Add((byte)(para.Value & 0x0FF));
                }

                LENGTH = (UInt16)(INFO.Count);
            }
        }


        /// <summary>
        ///  对接收的数据Reponse进行初步划分（根据通信协议）      
        /// </summary>
        /// <param name="frameStr">接收的数据Reponse</param>
        /// <param name="frameStr">ASCII标识</param>
        /// <returns>返回处理后的数据（DataFrame类型数据）</returns>
        public static DataFrame Parse(byte[] frameByte)
        {
            DataFrame dataFrame = new DataFrame();

            dataFrame.Direction = FrameDirection.Response;

            if (frameByte[0] != DataFrame.SOI)
                throw new Exception("Format error(SOI)");

            dataFrame.VER = frameByte[1];

            Byte adr = frameByte[2]; ;
            if (adr != DataFrame.ADR)
                throw new Exception("Format error(ADR)");

            Byte cid1 = frameByte[3]; ;
            if (cid1 != DataFrame.CID1)
                throw new Exception("Format error(CID1)");

            dataFrame.CID2 = frameByte[4]; ;


            dataFrame.LENGTH = (frameByte[5] << 8) + frameByte[6];
            dataFrame.LENGTH &= 0xFFFF;

            if (dataFrame.LENGTH > 0)
            {
                dataFrame.INFO = new List<Byte>();

                for (int i = 0; i < dataFrame.LENGTH; i++)
                {
                    dataFrame.INFO.Add(frameByte[7 + i]);
                }
            }

            dataFrame.CHECKSUM = (ushort)((frameByte[7 + dataFrame.LENGTH] << 8) + frameByte[7 + dataFrame.LENGTH + 1]);

            if (dataFrame.CHECKSUM != getFrameCRC(frameByte))
                throw new Exception("Frame Checksum error");

            if (frameByte[frameByte.Length - 1] != DataFrame.EOI)
                throw new Exception("Format error(EOI)");

            return dataFrame;

        }

        //-convert DataFrame to string
        /// <summary>
        ///  对要发送的数据Request进行规范化(整合成字符数组）      
        /// </summary>
        /// <returns>返回处理后的数据（string类型数据）</returns>
        public byte[] ToBytes()
        {
            List<byte> frameList = new List<byte> { };
            frameList.Add(VER);
            frameList.Add(DataFrame.ADR);
            frameList.Add(DataFrame.CID1);
            frameList.Add(CID2);
            frameList.Add((byte)(LENGTH >> 8));
            frameList.Add((byte)(LENGTH & 0x00FF));
            frameList.AddRange(INFO);

            ushort checkSum = GetCheckSum(frameList);

            frameList.Add((byte)(checkSum >> 8));
            frameList.Add((byte)(checkSum & 0x00FF));

            frameList.Insert(0, DataFrame.SOI);
            frameList.Add(DataFrame.EOI);

            return frameList.ToArray();

        }
        private ushort GetCheckSum(List<byte> list)
        {
            int crc_sum = 0;
            foreach (var ch in list)
                crc_sum += ch;
            crc_sum %= 65536;
            crc_sum = ~crc_sum + 1;
            crc_sum = crc_sum & 0x0FFFF;
            return (ushort)crc_sum;
        }
        /// <summary>
        /// CHKSUM的数据格式，用于校验CHKSUM        
        /// </summary>
        /// <param name="frameStr">接收到的数据Reponse</param>
        /// <returns>返回处理后的数据长度</returns>
        private static ushort getFrameCRC(byte[] frameBytes)
        {
            int crc_sum = 0;

            for (int i = 1; i < frameBytes.Length - 3; i++)
                crc_sum += frameBytes[i];
            crc_sum %= 65536;
            crc_sum = ~crc_sum + 1;
            ushort sum = (ushort)(crc_sum & 0x0FFFF);

            return sum;
        }

        /// <summary>
        ///  CID2返回码表示意义       
        /// </summary>
        /// <param name="RTN_code">接收到的CID2返回码</param>
        /// <returns>返回string表示意义</returns>
        public static string GetReturnMessage(Byte RTN_code)
        {
            string msg;

            switch (RTN_code)
            {
                case RTN_NoneErr: msg = "通信正常响应"; break;
                case RTN_VersionErr: msg = "协议版本错误"; break;
                case RTN_CheckSumErr: msg = "数和校验错误"; break;
                case RTN_LenCheckErr: msg = "长度校验错误"; break;
                case RTN_CID2Err: msg = "命令错误不支持"; break;
                case RTN_FormatErr: msg = "数据格式错误"; break;
                case RTN_InvaidDataErr: msg = "设置数据无效"; break;
                case RTN_AddrGroupErr: msg = "寻址组号错误"; break;
                case RTN_MemExternalErr: msg = "存储外设错误"; break;

                default:
                    if (RTN_code >= 0x80 && RTN_code <= 0xEF)
                        msg = "User Defined RTN error";
                    else
                        msg = "Undefined RTN Error";
                    break;
            }

            return msg;
        }

        #region 根据DATA_INFO的内部索引位置（或者 起始位置+字节数）获取指定的内容
        public int GetByteData(int loc)
        {
            return this.INFO[loc];
        }

        public int GetIntData(int start, int byteNum)
        {
            if (byteNum > 4)
                throw new Exception("Not Supported");

            int tmpVal = 0;
            for (int i = 0; i < byteNum; i++)
            {
                tmpVal <<= 8;
                tmpVal |= this.INFO[start + i];
            }
            return tmpVal;
        }

        //-bigendian
        public float GetFloatData(int start, int byteNum)
        {
            Int64 tmpVal = 0;
            for (int i = 0; i < byteNum; i++)
            {
                tmpVal <<= 8;
                tmpVal |= this.INFO[start + i];
            }
            return (float)tmpVal;
        }

        public string GetStringData(int start, int charNum)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < charNum; i++)
            {
                sb.Append((char)this.INFO[start + i]);
            }
            return sb.ToString();
        }

        public DateTime GetDateTime(int start, int byteNum)
        {
            if (byteNum != 7)
                throw new Exception("Illeagle byte Num for DateTime");

            int year = this.GetIntData(start, 2);
            int month = this.INFO[start + 2];
            int day = this.INFO[start + 3];
            int hour = this.INFO[start + 4];
            int minute = this.INFO[start + 5];
            int second = this.INFO[start + 6];

            return new DateTime(year, month, day, hour, minute, second);
        }

        public string GetTextData(int start, int charNum)
        {
            char[] charArray = new char[charNum];

            for (int i = 0; i < charNum; i++)
            {
                charArray[i] = (char)this.INFO[start + i];
            }
            return new string(charArray);
        }
        #endregion

        #region RTN  define CID2所表示的返回码定义
        /// <summary>
        /// 通信正常响应
        /// </summary>
        public const Byte RTN_NoneErr = 0x00;
        /// <summary>
        /// 协议版本错误
        /// </summary>
        public const Byte RTN_VersionErr = 0x01;
        /// <summary>
        /// 数和校验错误
        /// </summary>
        public const Byte RTN_CheckSumErr = 0x02;
        /// <summary>
        /// 长度校验错误
        /// </summary>
        public const Byte RTN_LenCheckErr = 0x03;
        /// <summary>
        /// 命令错误不支持
        /// </summary>
        public const Byte RTN_CID2Err = 0x04;
        /// <summary>
        /// 数据格式错误
        /// </summary>
        public const Byte RTN_FormatErr = 0x05;
        /// <summary>
        /// 设置数据无效
        /// </summary>
        public const Byte RTN_InvaidDataErr = 0x06;
        /// <summary>
        /// 寻址组号错误
        /// </summary>
        public const Byte RTN_AddrGroupErr = 0x07;
        /// <summary>
        /// 存储外设错误
        /// </summary>
        public const Byte RTN_MemExternalErr = 0x08;
        #endregion
    }
    /// <summary>
    /// CID2控制标识码定义       
    /// </summary>
    public class ProtocolCommand
    {
        /// <summary>
        /// 遥测量信息命令       
        /// </summary>     
        public const Byte CID2_TeleMeter = 0x42;
        /// <summary>
        /// 校准命令  
        /// </summary>     
        public const Byte CID2_Adjust = 0xA0;
    }
    /// <summary>
    /// 校准命令标识码定义       
    /// </summary>
    public class AdjustCommand
    {
        /// <summary>
        /// 零点校准
        /// </summary>     
        public const Byte adjustZero = 0x00;
        /// <summary>
        /// 电流K值命令  
        /// </summary>     
        public const Byte adjustCurrent = 0x01;
        /// <summary>
        /// 电压K值命令  
        /// </summary>     
        public const Byte adjustVoltage = 0x02;
        /// <summary>
        /// PackID校准 
        /// </summary>     
        public const Byte adjustPakID = 0x03;
    }
}
