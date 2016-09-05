using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace VrService
{
    public class CmdReceiver
    {
        #region Fields
        private int _inPort;

        public int InPort
        {
            get { return _inPort; }
            set { _inPort = value; }
        }
        private byte[] _inData = new byte[1024];

        public byte[] InData
        {
            get { return _inData; }
            set { _inData = value; }
        }
        private Socket _inSocket;

        public Socket InSocket
        {
            get { return _inSocket; }
            set { _inSocket = value; }
        }


        private int _outPort;

        public int OutPort
        {
            get { return _outPort; }
            set { _outPort = value; }
        }
        private byte[] _outData = new byte[1024];

        public byte[] OutData
        {
            get { return _outData; }
            set { _outData = value; }
        }
        private Socket _outSocket;

        public Socket OutSocket
        {
            get { return _outSocket; }
            set { _outSocket = value; }
        }


        private EndPoint _inRemoteEp;

        public EndPoint InRemoteEp
        {
            get { return _inRemoteEp; }
            set { _inRemoteEp = value; }
        }


        private string _filterIP = "";

        public string FilterIP
        {
            get { return _filterIP??(_filterIP =""); }
            set { _filterIP = value; }
        }

        private string _exePath;

        private int _nodeID;

        private IPEndPoint _observer;

        private bool _isRegister;
        #endregion

        #region Methods

        private static bool _encrypt = true;
        private static string exePathConfigFile = "ExePath.xml";

        private HandleRequest _handleRequest;

        public static int SendTo(Socket socket_, byte[] data_, int n_, SocketFlags flag_, EndPoint ep_)
        {
            if (_encrypt)
            {
                var encrypted = AES.AesEncrypt(data_, n_);
                return socket_.SendTo(encrypted, encrypted.Length, flag_, ep_);
            }
            else
            {
                return socket_.SendTo(data_, n_, flag_, ep_);
            }
        }

        public static int SendTo(Socket socket_, byte[] data_, int offset_, int n_, SocketFlags flag_, EndPoint ep_)
        {
            if (_encrypt)
            {
                var encrypted = AES.AesEncrypt(data_, n_, offset_);
                return socket_.SendTo(encrypted, encrypted.Length, flag_, ep_);
            }
            else
            {
                return socket_.SendTo(data_, offset_, n_, flag_, ep_);
            }
        }

        public static int ReceiveFrom(Socket socket_, byte[] data_,  ref EndPoint remoteEp_)
        {
            if (_encrypt)
            {
                int n = socket_.ReceiveFrom(data_, ref remoteEp_);
                var decrypted = AES.AesDecrypt(data_, n);
                Array.Copy(decrypted, data_, decrypted.Length);
                return decrypted.Length;
            }
            else
            {
                return socket_.ReceiveFrom(data_, ref remoteEp_);
            }
        }

        public void Init()
        {
            _isRegister = false;
            _inSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            
            try
            {
                _inSocket.Bind(new IPEndPoint(IPAddress.Any, _inPort));
            }
            catch (Exception ex)
            {
                LogUtil.WriteLog(string.Format("CmdRevceiver Init exception:{0}",ex.Message));
                throw ex;
            }

            _inRemoteEp = new IPEndPoint(IPAddress.Any, 0);
            _outSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _handleRequest = new HandleRequest();
            _handleRequest.CmdChannel = this;
        }

        public void Update()
        {
            int n = 0;
            //LogUtil.WriteLog("CmdReceiver Update Enter");
            while ((n = _inSocket.Available) > 0)
            {
                LogUtil.WriteLog("CmdReceiver Update GetData");
                MessageHead head = null;
                n = ReceiveFrom(_inSocket, _inData, ref _inRemoteEp);

                head = Message.ParseHead(_inData, _inData.Length);
                if (head == null)
                    break;

                var remoteIP = (_inRemoteEp as IPEndPoint).Address;
                LogUtil.WriteLog(string.Format("remoteIP:{0}",remoteIP.ToString()));
                if (FilterIP != "" && FilterIP != remoteIP.ToString())
                {
                    continue;
                }

                switch (head.Type)
                {
                    case MessageType.REQUEST_LaunchPlayerFromService:
                        {
                            MsgRequestLaunchPlayerFromService launch = new MsgRequestLaunchPlayerFromService(0, 0);
                            launch.Deserialize(_inData, n);
                            launch.ControllerIP = remoteIP.ToString();
                            LogUtil.WriteLog(string.Format("Receive msg {0}",launch));
                            //启动应用程序
                            string errorMsg = string.Empty;

                            _exePath = launch.EXEPath;
                            string appDir = new DirectoryInfo(AppDomain.CurrentDomain.SetupInformation.ApplicationBase).FullName;
                            string exePath = appDir + "\\" + exePathConfigFile;
                            LogUtil.WriteLog(exePath);

                            if (File.Exists(exePath))
                            {
                                try
                                {
                                    XmlDocument xmlDoc = new XmlDocument();
                                    xmlDoc.Load(exePath);
                                    XmlNode root = xmlDoc.SelectSingleNode("ExeConfig");
                                    XmlNode exePathNode = root.SelectSingleNode("ExePath");
                                    _exePath = exePathNode.InnerText;
                                    LogUtil.WriteLog("_exePath:" + _exePath);
                                }
                                catch (Exception ex)
                                {
                                    LogUtil.WriteLog("exception:"+ex.Message);
                                }
                            }
                            else
                            {
                                LogUtil.WriteLog(exePathConfigFile+"not exist");
                            }

                            //_exePath = "D:\\VideoPlayer.exe";
                            if (ProcessUtil.StartProgram(_exePath, "", ref errorMsg))
                            {
                                LogUtil.WriteLog(string.Format("StartProgram：Success"));

                                //发送反馈
                                LogUtil.WriteLog(string.Format("Send OK Ack Ip:{0} , Port:{1}",remoteIP.ToString(),launch.ControllerPort));
                                SendLaunchOK(launch, LaunchRequestResponse.ENUM_LAUNCH_STATUS.OK);
                                
                            }
                            else
                            {
                                LogUtil.WriteLog(string.Format("StartProgram：{0} Error", errorMsg));
                                LogUtil.WriteLog(string.Format("Send NotFound Ack Ip:{0} , Port:{1}", remoteIP.ToString(), launch.ControllerPort));
                                SendLaunchOK(launch, LaunchRequestResponse.ENUM_LAUNCH_STATUS.NotFound);
                            }
                            break;
                        }
                    case MessageType.REQUEST_ShutdownPlayer:
                        {
                            MsgRequestShutdownPlayer shutdown = new MsgRequestShutdownPlayer(0, 0);
                            shutdown.Deserialize(_inData, n);
                            shutdown.ControllerIP = remoteIP.ToString();
                            ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Receive msg REQUEST_ShutdownPlayer"));
                            LogUtil.WriteLog(string.Format("Receive msg {0}",shutdown));

                            _handleRequest.HandleShutdown(shutdown);

                            //string fileName = Path.GetFileName(_exePath);
                            //string processName = fileName.Substring(0, fileName.LastIndexOf('.'));
                            //LogUtil.WriteLog(string.Format("FileName : {0}， ProcessName : {1}",fileName,processName));
                            ////关闭应用程序
                            //string errorMsg = string.Empty;
                            //if (ProcessUtil.CloseProgram(_exePath,ref errorMsg))
                            //{
                            //    LogUtil.WriteLog(string.Format("CloseProgram：Success"));

                            //    LogUtil.WriteLog(string.Format("Send Shutdown Ack to Ip:{0} , Port:{1}",remoteIP.ToString(),shutdown.ControllerPort));
                            //    SendShutdownOK(shutdown, ShutdownRequestResponse.ENUM_SHUTDOWN_STATUS.OK);

                            //}
                            //else
                            //{
                            //    LogUtil.WriteLog(string.Format("CloseProgram：{0} Error", errorMsg));
                            //}
                           

                            break;
                        }
                    case MessageType.REQUEST_PingService:
                        {
                            ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Receive msg REQUEST_PingService"));
                            LogUtil.WriteLog(string.Format("Receive msg REQUEST_PingService"));
                            MsgRequestPingService ping = new MsgRequestPingService(0,0);
                            ping.Deserialize(_inData, n);
                            //反馈
                            LogUtil.WriteLog(string.Format("Ping Ack Ip: {0}, Port:{1}",remoteIP.ToString(),ping.ControllerPort));
                            SendPingOK(ping,remoteIP);
                            break;
                        }
                    case MessageType.REQUEST_BY_NAME:
                        {
                            ThreadStarter._instance.OnMessageSend(this, new MessageEventArgs("Receive msg REQUEST_BY_NAME"));
                            //按照名称启动应用
                            //启动成功后向控制端发送alive
                            var msg = new RequestByName();
                            msg.Deserialize(_inData, _inData.Length);
                            switch (msg.Name)
                            {
                                case "RunBatchRequest":
                                    {
                                        //var batch = new RunBatchRequest();
                                        //batch.Deserialize(_inData, _inData.Length);
                                        //batch.ControllerIP = remoteIP.ToString();
                                        //OnMessage(batch);
                                    }
                                    break;
                                case "LaunchAppRequest":
                                    {
                                        var launchApp = new LaunchAppRequest();
                                        launchApp.Deserialize(_inData, _inData.Length);
                                        launchApp.ControllerIP = remoteIP.ToString();
                                        //OnMessage(launchApp);
                                        _handleRequest.HandleLaunch(launchApp);
                                        
                                    }
                                    break;
                                default:
                                    //Debug.LogWarning("Failed to handle Request: " + msg);
                                    LogUtil.WriteLog("Failed to handle Request: " + msg);
                                    break;
                            }
                            //break;
                        }
                        break;
                    //case MessageType.CMD_Register:
                    //    {
                            
                    //        MsgCmdRegister register = new MsgCmdRegister(((IPEndPoint)_inRemoteEp).Address.ToString(), 0);
                    //        register.Deserialize(_inData, _inData.Length);
                    //        LogUtil.WriteLog(string.Format("Receive msg{0}", register));
                    //        _nodeID = register.PlayerID;

                    //        MsgAckRegisterOK registerOK = new MsgAckRegisterOK(0, _nodeID);

                    //        _outPort = register.CmdPort;
                    //        _observer = new IPEndPoint(IPAddress.Parse(register.IP), _outPort);
                    //        _isRegister = true;
                    //        SendRegisterOK(registerOK);
                    //        break;
                    //    }
                    default:
                        {
                            LogUtil.WriteLog(string.Format("Error: msg<head:<id:{0}, type:{1}>>",head.ID,head.Type));
                            break;
                        }
                }
            }
        }

        public void Destory()
        {
            if (_inSocket != null)
            {
                _inSocket.Close();
                _inSocket = null;
            }

            if (_outSocket != null)
            {
                _outSocket.Close();
                _outSocket = null;
            }
        }


        public void SendLaunchOK(MsgRequestLaunchPlayerFromService request_, LaunchRequestResponse.ENUM_LAUNCH_STATUS status_, string appName_ = "")
        {
            LaunchRequestResponse response = new LaunchRequestResponse(0, status_)
            {
                LaunchRequestID = request_.Head.ID,
                AppName = appName_,
            };

            var observer = new IPEndPoint(IPAddress.Parse(request_.ControllerIP), request_.ControllerPort);
            int n = response.Serialize(_outData);
            //Debug.Log(string.Format("SendLaunchOK: {0} to {1}", response, observer));
            LogUtil.WriteLog(string.Format("SendLaunchOK: {0} to {1}", response, observer));
            SendTo(_outSocket, _outData, n, SocketFlags.None, observer);
        }

        public void SendLaunchOK(LaunchAppRequest request_, LaunchRequestResponse.ENUM_LAUNCH_STATUS status_, string appName_ = "")
        {
            LaunchRequestResponse response = new LaunchRequestResponse(0, status_)
            {
                RequestID = request_.Head.ID,
                LaunchRequestID = request_.Head.ID,
                AppName = appName_,
            };

            var observer = new IPEndPoint(IPAddress.Parse(request_.ControllerIP), request_.ControllerPort);
            int n = response.Serialize(_outData);
            //Debug.Log(string.Format("SendLaunchOK: {0} to {1}", response, observer));
            LogUtil.WriteLog(string.Format("SendLaunchOK: {0} to {1}", response, observer));
            //UDPEndpoint.SendTo(_outSocket, _outData, n, SocketFlags.None, observer);
            SendTo(_outSocket, _outData, n, SocketFlags.None, observer);
        }

        public void SendShutdownOK(MsgRequestShutdownPlayer request_, ShutdownRequestResponse.ENUM_SHUTDOWN_STATUS status_)
        {
            ShutdownRequestResponse response = new ShutdownRequestResponse(0, status_)
            {
                RequestID = request_.Head.ID,
            };

            var observer = new IPEndPoint(IPAddress.Parse(request_.ControllerIP), request_.ControllerPort);
            int n = response.Serialize(_outData);
            //Debug.Log(string.Format("SendLaunchOK: {0} to {1}", response, observer));
            SendTo(_outSocket, _outData, n, SocketFlags.None, observer);
        }

        //public void SendRegisterOK(MsgAckRegisterOK ok)
        //{
        //    int n = ok.Serialize(_outData);
        //    Debug.Log(string.Format("Send msg{0} to {1}", ok, _observer));
        //    SendTo(_outSocket, _outData, n, SocketFlags.None, _observer);
        //}


        public void SendPingOK(MsgRequestPingService request,IPAddress ip)
        {
            PingServiceRequestResponse response = new PingServiceRequestResponse(request.Head.ID);
            var observer = new IPEndPoint(ip, request.ControllerPort);
            int n = response.Serialize(_outData);
            SendTo(_outSocket, _outData, n, SocketFlags.None, observer);
        }

        #endregion

    }
}
