using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VrService
{
    //class LaunchAppRequest
    //{
    //}
    public class LaunchAppRequest : RequestByName
    {
        private string _exePath;
        public string EXEPath
        {
            get { return _exePath ?? (_exePath = ""); }
            set { _exePath = value; }
        }

        private string _arguments;
        public string Arguments
        {
            get { return _arguments ?? (_arguments = ""); }
            set { _arguments = value; }
        }

        private string _controllerIP;
        public string ControllerIP
        {
            get { return _controllerIP ?? (_controllerIP = ""); }
            set { _controllerIP = value; }
        }

        private int _controllerPort;
        public int ControllerPort
        {
            get { return _controllerPort; }
            set { _controllerPort = value; }
        }

        private int _keepAlive = 2;
        public int KeepAlive
        {
            get { return _keepAlive; }
            set { _keepAlive = value; }
        }

        public override int Serialize(byte[] data_)
        {
            var idx = base.Serialize(data_);

            idx = InsertString(EXEPath, ref Data, Size, idx);
            idx = InsertString(Arguments, ref Data, Size, idx);

            idx = InsertString(ControllerIP, ref Data, Size, idx);
            idx = InsertInt(ControllerPort, ref Data, Size, idx);
            idx = InsertInt(KeepAlive, ref Data, Size, idx);
            return idx;
        }

        public override int Deserialize(byte[] data_, int size_)
        {
            var idx = base.Deserialize(data_, size_);

            idx = GetString(out _exePath, ref Data, Size, idx);
            idx = GetString(out _arguments, ref Data, Size, idx);

            idx = GetString(out _controllerIP, ref Data, Size, idx);
            idx = GetInt(out _controllerPort, ref Data, Size, idx);
            idx = GetInt(out _keepAlive, ref Data, Size, idx);
            return idx;
        }

        public override string ToString()
        {
            var info = "LaunchAppRequest:\n";
            info += string.Format("<b>EXEPath</b>: {0}\n", EXEPath);
            info += string.Format("<b>Arguments</b>: {0}\n", Arguments);
            info += string.Format("<b>ControllerIP</b>: {0}\n", ControllerIP);
            info += string.Format("<b>ControllerPort</b>: {0}\n", ControllerPort);
            info += string.Format("<b>KeepAlive</b>: {0}\n", KeepAlive);
            return info;
        }
    }
}
