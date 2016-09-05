using System.Collections.Generic;
using System.Text;
using System;


// # Layout
// |------- Head (32) ------------|---------- Playload ----------|
// |--id(16)--nodeId(8)--type(8)--|
// @Ref: Message.InsertHead()
// NOTE: 1. ignore nodeId (put 0)
// 
// # Messages
// 1. Staus message (Player => Controller)
//    MsgAlive, MsgVideoWall(TODO:rename to MsgHome), MsgStatusPlaying, x, x, device && video
//
// 2. CMD message (Controller => Player)
//    MsgCmdRegister, MsgCmdPlayByName, MsgCmdStop
//
// # TODO 1
// 1. Add Pause RequestDispatcher
// 2. Thus, add Paused Status
// 
// # TODO 2
// 1. Launch and Stop application cmds

// -------------------------------
// New Version
// New Status
//    1. STATUS_VIDEO_INFO && STATUS_LOADING_VIDEO
//    REMARK: An MsgSimple with type set to STATUS_VIDEO_INFO or STATUS_LOADING_VIDEO
//    Should be sent from Player to Controller

// New Commands
//    1. MsgRequestGetNodeInfo
//
//    2. MsgResponseGetNodeInfo
//
//    REMARK on Response UDP port:
//        a. Send response to Status Port plz.
//
//    REMARK on DeviceID:
//        a. Use MACAddress of the device in format 00:00:00:00:00:00
//        b. In Node Setting View of VPController, change DeviceID to that of your device and save.
//    REMARK on VideoID: 
//    VideoID is a hash code generated using Video NodeName and Video Content. 
//        a. Currently, VideoID is assigned a unique string manually.
//        In the video setting given to you, you have sent 0000-0000-0000-0003 for video Demo.mp4.
//        So, return 0000-0000-0000-0003 for Demo.mp4, and empty ("") for others
//        b. Bear in mind VideoID is generated. 
//		
//    3. MsgRequestLaunchPlayerFromService
//    REMARK: 
//        a. Ignore EXEPath, but use a default exe path to request player
//        b. Service port is 4900

public enum MessageType
{
    Alive = 0,
    CMD_Play = 16,
    CMD_ChangeVideo,
    CMD_Stop,
    CMD_Pause,
    CMD_Resume,
    CMD_Register,
    CMD_Seek,
    CMD_KeepAlive,
    ACK_Register_OK = 32,
    ACK_Play_OK,
    ACK_Stop_OK,
    ACK_Pause_OK,
    ACK_Resume_OK,
    ACK_Seek_OK,
    ACK_Launch_OK,
    ACK_Shutdown_OK,
    ACK_PingService_OK,
    STATUS_Home = 64,
    STATUS_Playing,
    STATUS_VideoInfo,
    STATUS_LoadingVideo,
    STATUS_Paused,
    STATUS_HMD,
    REQUEST_BY_NAME = 127,
    REQUEST_GetNodeInfo = 128,
    RESPONSE_GetNodeInfo,
    REQUEST_GetNodeInfoFromService,
    RESPONSE_GetNodeInfoFromService,
    REQUEST_LaunchPlayerFromService,
    REQUEST_ShutdownPlayer,
    REQUEST_PingService,
    RESPONSE_RunBatch = 512,
    Invalid = 0xFFFF,
}

//public enum MessageType
//{
//    Alive = 0,
//    CMD_Play = 16,
//    CMD_Stop,
//    CMD_Pause,
//    CMD_Resume,
//    CMD_Register,
//    CMD_ChangeVideo,
//    ACK_Register_OK = 32,
//    ACK_Play_OK,
//    ACK_Pause_OK,
//    ACK_Resume_OK,
//    ACK_Stop_OK,
//    STATUS_VideoWall = 64,
//    STATUS_Playing,
//    STATUS_VIDEO_INFO,
//    STATUS_LOADING_VIDEO,
//    STATUS_PASUED,
//    REQUEST_GetNodeInfo = 128,
//    RESPONSE_GetNodeInfo,
//    REQUEST_GetNodeInfoFromService,
//    RESPONSE_GetNodeInfoFromService,
//    REQUEST_LaunchPlayerFromService,
//    REQUEST_ShutdownPlayer,
//    Invalid,
//}


public class MessageHead
{
    public long ID = -1; // 8
    public int NodeID = -1; // 4
    public MessageType Type = MessageType.Invalid; // 4
    public const int LENGTH = 32;

    //
    public long RequestID = -1; // 8
    public long Checksum = 0; //TODO 8

    public override string ToString()
    {
        return string.Format("<ID:{0}, NodeID:{1}, Type:{2}, RequestID:{3}>", ID, NodeID, Type, RequestID);
    }
}

public class Message
{
    public byte[] Data;
    public int Size;

    private MessageHead _head;
    public MessageHead Head
    {
        get { return _head ?? (_head = new MessageHead()); }
        set { _head = value; }
    }

    protected static int GetInt(out int value_, ref byte[] data_, int size_, int startIdx_)
    {
        if (size_ < (startIdx_ + sizeof(int))) throw new System.IndexOutOfRangeException();

        value_ = data_[startIdx_];

        int shift = 8;
        for (int i = 1; i < sizeof(int); i++)
        {
            value_ |= ((int)data_[startIdx_ + i] << shift);
            shift += 8;
        }
        return startIdx_ + sizeof(int);
    }

    protected static int InsertInt(int value_, ref byte[] data_, int size_, int startIdx_)
    {
        if (size_ < (startIdx_ + sizeof(int))) throw new System.IndexOutOfRangeException();

        for (int i = 0; i < sizeof(int); i++)
        {
            data_[startIdx_ + i] = (byte)value_;
            value_ >>= 8;
        }
        return sizeof(int) + startIdx_;
    }

    protected static int GetLong(out long value_, ref byte[] data_, int size_, int startIdx_)
    {
        if (size_ < (startIdx_ + sizeof(long))) throw new System.IndexOutOfRangeException();

        value_ = data_[startIdx_];

        int shift = 8;
        for (int i = 1; i < sizeof(long); i++)
        {
            value_ |= ((long)data_[startIdx_ + i] << shift);
            shift += 8;
        }
        return startIdx_ + sizeof(long);
    }

    protected static int InsertLong(long value_, ref byte[] data_, int size_, int startIdx_)
    {
        if (size_ < (startIdx_ + sizeof(long))) throw new System.IndexOutOfRangeException();

        for (int i = 0; i < sizeof(long); i++)
        {
            data_[startIdx_ + i] = (byte)value_;
            value_ >>= 8;
        }
        return sizeof(long) + startIdx_;
    }

    protected static int GetString(out string value_, ref byte[] data_, int size_, int startIdx_)
    {
        if (size_ < (startIdx_ + sizeof(int))) throw new System.IndexOutOfRangeException();
        int length;

        startIdx_ = GetInt(out length, ref data_, size_, startIdx_);
        //char[] bytes = new char[length];
        var bytes = new byte[length];
        System.Array.Copy(data_, startIdx_, bytes, 0, length);

        //value_ = new string(bytes);
        var unicode = System.Text.Encoding.Unicode;
        value_ = unicode.GetString(bytes);
        return startIdx_ + length;
    }

    protected static int InsertString(string value_, ref byte[] data_, int size_, int startIdx_)
    {
        //if (size_ < (startIdx_ + sizeof(int) + value_.Length)) throw new System.IndexOutOfRangeException();
        //startIdx_ = InsertInt(value_.Length, ref data_, size_, startIdx_);

        //for (int i = 0; i < value_.Length; i++)
        //{
        //    data_[startIdx_ + i] = (byte)value_[i];
        //}
        //return startIdx_ + value_.Length;
        var unicode = System.Text.Encoding.Unicode;
        var bytes = unicode.GetBytes(value_);

        if (size_ < (startIdx_ + sizeof(int) + bytes.Length)) throw new System.IndexOutOfRangeException();
        startIdx_ = InsertInt(bytes.Length, ref data_, size_, startIdx_);

        for (int i = 0; i < bytes.Length; i++)
        {
            data_[startIdx_ + i] = (byte)bytes[i];
        }
        return startIdx_ + bytes.Length;
    }

    public static MessageHead ParseHead(byte[] data_, int size_)
    {
        MessageHead head = new MessageHead();
        if (size_ < MessageHead.LENGTH)
        {
            head.Type = MessageType.Invalid;
        }
        else
        {
            int idx = 0;
            idx = GetLong(out head.ID, ref data_, size_, idx);
            idx = GetInt(out head.NodeID, ref data_, size_, idx);

            int ret;
            idx = GetInt(out ret, ref data_, size_, idx);
            head.Type = (MessageType)ret;

            idx = GetLong(out head.RequestID, ref data_, size_, idx);
        }
        return head;
    }

    private int InsertHead()
    {
        int idx = 0;
        idx = InsertLong(Head.ID, ref Data, Data.Length, idx);
        idx = InsertInt((int)Head.NodeID, ref Data, Data.Length, idx);
        idx = InsertInt((int)Head.Type, ref Data, Data.Length, idx);

        idx = InsertLong(Head.RequestID, ref Data, Data.Length, idx);
        return MessageHead.LENGTH;
    }

    public virtual int Serialize(byte[] data_) { Data = data_; Size = data_.Length; return InsertHead(); }
    public virtual int Deserialize(byte[] data_, int size_) { Data = data_; Size = size_; Head = ParseHead(Data, Size); return MessageHead.LENGTH; }

    public override string ToString()
    {
        return string.Format("<id:{0}, nodeID:{1}, type={2}>", Head.ID, Head.NodeID, Head.Type);
    }
}

public class BatteryStatus : DataModelBase
{
    public enum ENUM_BATTERY_STATUS
    {
        Unknown = 0,
        Charging,
        Discharging,
        NotCharging,
        Full,
        Ignore = 16,
    }

    private ENUM_BATTERY_STATUS _battery_status = ENUM_BATTERY_STATUS.Ignore;
    public ENUM_BATTERY_STATUS Status
    {
        get { return _battery_status; }
        set { _battery_status = value; OnPropertyChanged("Status"); }
    }

    private int _battery_percent = 0;
    public int Percent
    {
        get { return _battery_percent; }
        set { _battery_percent = value; OnPropertyChanged("Percent"); }
    }

    private int _battery_temperature = 0;
    public int Temperature
    {
        get { return _battery_temperature; }
        set { _battery_temperature = value; OnPropertyChanged("Temperature"); }
    }
}

public class MsgAlive : Message
{
    private BatteryStatus _batteryStatus;
    public BatteryStatus Battery
    {
        get { return _batteryStatus ?? (_batteryStatus = new BatteryStatus()); }
        set { _batteryStatus = value; }
    }

    public MsgAlive(long id_ = 0, int nodeId_ = 0)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = nodeId_,
            Type = MessageType.Alive,
        };
    }

    public override int Serialize(byte[] data_)
    {
        var idx = base.Serialize(data_);
        idx = InsertInt((int)Battery.Status, ref Data, Size, idx);
        idx = InsertInt(Battery.Percent, ref Data, Size, idx);
        idx = InsertInt(Battery.Temperature, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        var idx = base.Deserialize(data_, size_);

        int status;
        idx = GetInt(out status, ref Data, Size, idx);
        Battery.Status = (BatteryStatus.ENUM_BATTERY_STATUS)status;

        int percent;
        idx = GetInt(out percent, ref Data, Size, idx);
        Battery.Percent = percent;

        int temperature;
        idx = GetInt(out temperature, ref Data, Size, idx);
        Battery.Temperature = temperature;
        return idx;
    }
}

public class MsgVideoWall : Message
{
    public string VideoName = "No Video";
    public long Remaining = 0;

    public MsgVideoWall(long id_ = 0)
    {
        Head = new MessageHead()
        {
            ID = id_,
            Type = MessageType.STATUS_Home,
        };
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertString(VideoName, ref Data, Data.Length, idx);
        idx = InsertLong(Remaining, ref Data, Data.Length, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        idx = GetString(out VideoName, ref Data, Size, idx);
        idx = GetLong(out Remaining, ref Data, Size, idx);
        return idx;
    }
}

public class MsgStatusPlaying : Message
{
    public long Video = -1;
    public string VideoName = "No Video";
    public long Remaining = 0;
    public long TotalFrame = 0;
    public long CurrentFrame = 0;

    public MsgStatusPlaying(long id_ = 0, int nodeId_ = 0)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = nodeId_,
            Type = MessageType.STATUS_Playing,
        };
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertString(VideoName, ref Data, Data.Length, idx);
        idx = InsertLong(Video, ref Data, Data.Length, idx);
        idx = InsertLong(Remaining, ref Data, Data.Length, idx);
        idx = InsertLong(TotalFrame, ref Data, Data.Length, idx);
        idx = InsertLong(CurrentFrame, ref Data, Data.Length, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        idx = GetString(out VideoName, ref Data, Size, idx);
        idx = GetLong(out Video, ref Data, Size, idx);
        idx = GetLong(out Remaining, ref Data, Size, idx);
        idx = GetLong(out TotalFrame, ref Data, Size, idx);
        idx = GetLong(out CurrentFrame, ref Data, Size, idx);
        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, videoname:{1}, video:{2}, remaining:{3}, totalframe:{4}, currentframe:{5}>",
           Head, VideoName, Video, Remaining, TotalFrame, CurrentFrame);
    }
}

public class MsgSimple : Message
{
    private string _info;
    public string Info
    {
        get { return _info ?? (_info = ""); }
        set { _info = value; }
    }

    public MsgSimple(long id_, int nodeId_, MessageType type_, string info_)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = nodeId_,
            Type = type_,
        };
        Info = info_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertString(Info, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        idx = GetString(out _info, ref Data, Size, idx);
        return idx;
    }
}

public class MsgCmdPlayByName : Message
{
    #region Properties

    public string Video;

    private string _videoHashCode;
    public string VideoHashCode
    {
        get { return _videoHashCode ?? (_videoHashCode = ""); }
        set { _videoHashCode = value; }
    }

    private string _deviceId;
    public string DeviceID
    {
        get { return _deviceId ?? (_deviceId = ""); }
        set { _deviceId = value; }
    }

    private bool _checkHashCode;
    public bool CheckHashCode
    {
        get { return _checkHashCode; }
        set { _checkHashCode = value; }
    }

    private bool _checkDevice;
    public bool CheckDevice
    {
        get { return _checkDevice; }
        set { _checkDevice = value; }
    }

    #endregion

    public MsgCmdPlayByName(int id_, int nodeId_, string video_)
    {
        Head = new MessageHead()
        {
            Type = MessageType.CMD_Play,
            ID = id_,
            NodeID = nodeId_,
        };

        Video = video_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertString(Video, ref Data, Size, idx);
        idx = InsertString(VideoHashCode, ref Data, Size, idx);
        idx = InsertString(DeviceID, ref Data, Size, idx);
        idx = InsertInt(CheckHashCode ? 1 : 0, ref Data, Size, idx);
        idx = InsertInt(CheckDevice ? 1 : 0, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        idx = GetString(out Video, ref Data, Size, idx);
        idx = GetString(out _videoHashCode, ref Data, Size, idx);
        idx = GetString(out _deviceId, ref Data, Size, idx);


        int checkHashCode = 0;
        idx = GetInt(out checkHashCode, ref Data, Size, idx);
        CheckHashCode = checkHashCode == 1;

        int checkDevice = 0;
        idx = GetInt(out checkDevice, ref Data, Size, idx);
        CheckDevice = checkDevice == 1;
        return idx;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendFormat("<b>MsgCmdPlayByName</b>\n");
        builder.AppendFormat("<b>head</b>:{0}\n", Head);
        builder.AppendFormat("<b>Video</b>: {0}\n", Video);
        builder.AppendFormat("<b>VideoHashCode</b>: {0}\n", VideoHashCode);
        builder.AppendFormat("<b>DeviceID</b>: {0}\n", DeviceID);
        builder.AppendFormat("<b>CheckHashCode</b>: {0}\n", CheckHashCode);
        builder.AppendFormat("<b>CheckDevice</b>: {0}\n", CheckDevice);

        return builder.ToString();
    }
}

public class PlayRequestResponse : Message
{
    public enum ENUM_STATUS
    {
        OK = 0,
        FileNotFound = 1,
        VRDeviceError = 2,  // VRDeviceError
        HMDNotReady = 3,
        HashCodeError = 4,
        VRDeviceAndHashCodeError = 5,
        InvalidVideoSuffix = 6,
        CommandBuffered = 7,
    }

    private long _requestId = 0;
    public long RequestID { get { return _requestId; } set { _requestId = value; } }

    private ENUM_STATUS _status = ENUM_STATUS.OK;
    public ENUM_STATUS Status { get { return _status; } set { _status = value; } }

    private string _videoName = "";
    public string VideoName
    {
        get { return _videoName ?? (_videoName = ""); }
        set { _videoName = value; }
    }

    public PlayRequestResponse(long id_, long requestId_, ENUM_STATUS status_ = ENUM_STATUS.OK)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = 0,
            Type = MessageType.ACK_Play_OK
        };
        _requestId = requestId_;
        Status = status_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertLong(RequestID, ref Data, Size, idx);
        idx = InsertString(VideoName, ref Data, Size, idx);
        idx = InsertInt((int)Status, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);

        idx = GetLong(out _requestId, ref Data, Size, idx);
        idx = GetString(out _videoName, ref Data, Size, idx);

        int status;
        idx = GetInt(out status, ref Data, Size, idx);
        Status = (ENUM_STATUS)status;

        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, RequestID:{1}, VideoName:{2}, Status:{3}>", Head, RequestID, VideoName, Status);
    }
}

public class MsgCmdStop : Message
{
    public long Video;
    public MsgCmdStop(int id_, int nodeId_, int video_)
    {
        Head = new MessageHead()
        {
            Type = MessageType.CMD_Stop,
            ID = id_,
            NodeID = nodeId_,
        };
        Video = video_;
    }
    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        InsertLong(Video, ref Data, Size, 0);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        idx = GetLong(out Video, ref Data, Size, idx);
        return idx;
    }
}

public class StopRequestResponse : Message
{
    public enum ENUM_STATUS
    {
        OK = 0,
        CommandIgnored = 1,
        HMDNotReady = 3
    }

    private long _requestId = 0;
    public long RequestID { get { return _requestId; } set { _requestId = value; } }

    private ENUM_STATUS _status = ENUM_STATUS.OK;
    public ENUM_STATUS Status { get { return _status; } set { _status = value; } }

    public StopRequestResponse(long id_, long requestId_, ENUM_STATUS status_ = ENUM_STATUS.OK)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = 0,
            Type = MessageType.ACK_Stop_OK
        };
        _requestId = requestId_;
        Status = status_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertLong(RequestID, ref Data, Size, idx);
        idx = InsertInt((int)Status, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);

        idx = GetLong(out _requestId, ref Data, Size, idx);

        int status;
        idx = GetInt(out status, ref Data, Size, idx);
        Status = (ENUM_STATUS)status;

        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, RequestID:{1}, Status:{2}>", Head, RequestID, Status);
    }
}

public class MsgCmdRegister : Message
{
    #region Fields

    private ControllerConfig _controller;
    public int PlayerID;

    #endregion

    #region Properties

    public string IP
    {
        get { return _controller.ip; }
    }
    public int AlivePort
    {
        get { return _controller.alive_port; }
    }

    public int StatusPort
    {
        get { return _controller.status_port; }
    }

    public int CmdPort
    {
        get { return _controller.cmd_port; }
    }

    public string DefaultVideo
    {
        get;
        set;
    }

    #endregion

    public MsgCmdRegister(int id, ControllerConfig controller, int playerID)
    {
        Head = new MessageHead()
        {
            ID = id,
            NodeID = 0,
            Type = MessageType.CMD_Register,
        };

        _controller = controller;
        PlayerID = playerID;
    }

    public MsgCmdRegister(string controllerIP_, int controllerId_)
    {
        // receive cmd register
        _controller = new ControllerConfig();
        _controller.ip = controllerIP_;
        _controller.id = controllerId_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertInt(PlayerID, ref Data, Size, idx);
        idx = InsertInt(_controller.alive_port, ref Data, Size, idx);
        idx = InsertInt(_controller.status_port, ref Data, Size, idx);
        idx = InsertInt(_controller.cmd_port, ref Data, Size, idx);
        idx = InsertString(DefaultVideo, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        idx = GetInt(out PlayerID, ref Data, Size, idx);
        idx = GetInt(out _controller.alive_port, ref Data, Size, idx);
        idx = GetInt(out _controller.status_port, ref Data, Size, idx);
        idx = GetInt(out _controller.cmd_port, ref Data, Size, idx);

        string defaultVideo;
        idx = GetString(out defaultVideo, ref Data, Size, idx);
        DefaultVideo = defaultVideo;

        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, controller:<id:{1}, ip:{2}, alive_port:{3}, status_port:{4}, cmd_port:{5}>, playerID:{6}>",
            Head, _controller.id, _controller.ip, _controller.alive_port, _controller.status_port, _controller.cmd_port, PlayerID);
    }
}

public class MsgCmdChangeVideo : Message
{
    public string Video;

    public MsgCmdChangeVideo(int id_, int nodeId_, string video_)
    {
        Head = new MessageHead();
        Head.Type = MessageType.CMD_ChangeVideo;
        Head.ID = id_;
        Head.NodeID = nodeId_;
        Video = video_;
    }
    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertString(Video, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        idx = GetString(out Video, ref Data, Size, idx);
        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, video:{1}>", Head, Video);
    }
}

//GetNodeInfo
public class MsgRequestGetNodeInfo : Message
{
    private List<string> _videoList;
    public List<string> VideoList
    {
        get { return _videoList ?? (_videoList = new List<string>()); }
        set { _videoList = value; }
    }

    public MsgRequestGetNodeInfo(long id_, int nodeId_)
    {
        Head = new MessageHead();
        Head.ID = id_;
        Head.Type = MessageType.REQUEST_GetNodeInfo;
        Head.NodeID = nodeId_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertInt(VideoList.Count, ref Data, Size, idx);
        VideoList.ForEach(video_ =>
        {
            idx = InsertString(video_, ref Data, Size, idx);
        });
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        VideoList = new List<string>();

        int videoCount = 0;
        idx = GetInt(out videoCount, ref Data, Size, idx);
        for (int i = 0; i < videoCount; i++)
        {
            string video;
            idx = GetString(out video, ref Data, Size, idx);
            VideoList.Add(video);
        }

        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, videoCount:{1}>", Head, VideoList.Count);
    }
}

//GetNodeInfo
public class MsgResponseGetNodeInfo : Message
{
    private string _deviceId = "";
    public string DeviceID
    {
        get { return _deviceId; }
        set { _deviceId = value; }
    }

    private List<VideoIDInfo> _videoIdInfoList;
    public List<VideoIDInfo> VideoIDInfoList
    {
        get { return _videoIdInfoList ?? (_videoIdInfoList = new List<VideoIDInfo>()); }
        set { _videoIdInfoList = value; }
    }

    public MsgResponseGetNodeInfo(long id_)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = 0,
            Type = MessageType.RESPONSE_GetNodeInfo,
        };
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertString(DeviceID, ref Data, Size, idx);

        idx = InsertInt(VideoIDInfoList.Count, ref Data, Size, idx);
        VideoIDInfoList.ForEach(info_ =>
        {
            idx = InsertString(info_.VideoName, ref Data, Size, idx);
            idx = InsertInt((int)info_.Type, ref Data, Size, idx);
            idx = InsertString(info_.VideoID, ref Data, Size, idx);
        });

        return idx;
    }


    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);

        string deviceId;
        idx = GetString(out deviceId, ref Data, Size, idx);
        DeviceID = deviceId;

        VideoIDInfoList.Clear();

        int infoCount;
        idx = GetInt(out infoCount, ref Data, Size, idx);
        for (int i = 0; i < infoCount; i++)
        {
            string videoName, videoId;
            int type;
            idx = GetString(out videoName, ref Data, Size, idx);
            idx = GetInt(out type, ref Data, Size, idx);
            idx = GetString(out videoId, ref Data, Size, idx);

            VideoIDInfo idInfo = new VideoIDInfo()
            {
                VideoName = videoName,
                Type = (VideoIDInfo.ENUM_INFO_TYPE)type,
                VideoID = videoId,
            };

            VideoIDInfoList.Add(idInfo);
        }
        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, deviceId:{1}, videoInfoListCount:{2}>", Head, DeviceID, VideoIDInfoList.Count);
    }
}

//GetNodeInfoFromService
public class MsgRequestGetNodeInfoFromService : Message
{
    private int _responsePort;
    public int ResponsePort
    {
        get { return _responsePort; }
        set { _responsePort = value; }
    }

    public MsgRequestGetNodeInfoFromService(long id_, int nodeId_)
    {
        Head = new MessageHead();
        Head.ID = id_;
        Head.Type = MessageType.REQUEST_GetNodeInfoFromService;
        Head.NodeID = nodeId_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertInt(ResponsePort, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        idx = GetInt(out _responsePort, ref Data, Size, idx);
        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, resposePort:{1}>", Head, ResponsePort);
    }
}

//GetNodeInfoFromService
public class MsgResponseGetNodeInfoFromService : Message
{
    private string _deviceId = "";
    public string DeviceID
    {
        get { return _deviceId; }
        set { _deviceId = value; }
    }

    public MsgResponseGetNodeInfoFromService(long id_)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = 0,
            Type = MessageType.RESPONSE_GetNodeInfoFromService,
        };
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertString(DeviceID, ref Data, Size, idx);
        return idx;
    }
    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        string deviceId;
        idx = GetString(out deviceId, ref Data, Size, idx);
        DeviceID = deviceId;
        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, deviceId:{1}>", Head, DeviceID);
    }
}

public class MsgRequestPingService : Message
{
    private int _controllerPort;
    public int ControllerPort
    {
        get { return _controllerPort; }
        set { _controllerPort = value; }
    }

    public MsgRequestPingService(long id_, int nodeId_)
    {
        Head = new MessageHead();
        Head.ID = id_;
        Head.Type = MessageType.REQUEST_PingService;
        Head.NodeID = nodeId_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertInt(ControllerPort, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        idx = GetInt(out _controllerPort, ref Data, Size, idx);
        return idx;
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendFormat("<b>{0}</b>\n", "MsgRequestPingService");
        builder.AppendFormat("<b>Head</b>: {0}\n", Head);
        builder.AppendFormat("<b>ControllerPort</b>: {0}\n", ControllerPort);
        return builder.ToString();
    }
}

public class PingServiceRequestResponse : Message
{
    public enum ENUM_PING_SERVICE_STATUS
    {
        OK = 0,
    }

    private long _requestId;
    public long RequestID
    {
        get { return _requestId; }
        set { _requestId = value; }
    }

    private ENUM_PING_SERVICE_STATUS _status;
    public ENUM_PING_SERVICE_STATUS Status
    {
        get { return _status; }
        set { _status = value; }
    }

    public PingServiceRequestResponse(long id_ = 0, ENUM_PING_SERVICE_STATUS status_ = ENUM_PING_SERVICE_STATUS.OK)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = 0,
            Type = MessageType.ACK_PingService_OK,
        };

        Status = status_;
    }

    public PingServiceRequestResponse(long requestId_)
    {
        Head = new MessageHead()
        {
            ID = 0,
            NodeID = 0,
            Type = MessageType.ACK_PingService_OK,
        };

        RequestID = requestId_;
        Status = ENUM_PING_SERVICE_STATUS.OK;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertLong(RequestID, ref Data, Size, idx);
        idx = InsertInt((int)Status, ref Data, Size, idx);

        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);

        idx = GetLong(out _requestId, ref Data, Size, idx);

        int status;
        idx = GetInt(out status, ref Data, Size, idx);
        Status = (ENUM_PING_SERVICE_STATUS)status;

        return idx;
    }

    public override string ToString()
    {
        var info = new StringBuilder();
        info.AppendFormat("<b>Head</b>: {0}\n", Head);
        info.AppendFormat("<b>RequestID</b>: {0}\n", RequestID);
        info.AppendFormat("<b>Status</b>: {0}\n", Status);
        return info.ToString();
    }
}


public class MsgRequestLaunchPlayerFromService : Message
{
    private string _exePath;
    public string EXEPath
    {
        get { return _exePath; }
        set { _exePath = value; }
    }

    private int _controllerPort;
    public int ControllerPort
    {
        get { return _controllerPort; }
        set { _controllerPort = value; }
    }

    private string _controllerIP;
    public string ControllerIP
    {
        get { return _controllerIP; }
        set { _controllerIP = value; }
    }

    public MsgRequestLaunchPlayerFromService(long id_, int nodeId_)
    {
        Head = new MessageHead();
        Head.ID = id_;
        Head.Type = MessageType.REQUEST_LaunchPlayerFromService;
        Head.NodeID = nodeId_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertString(EXEPath, ref Data, Size, idx);
        idx = InsertInt(ControllerPort, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        string path;
        idx = GetString(out path, ref Data, Size, idx);
        EXEPath = path;

        idx = GetInt(out _controllerPort, ref Data, Size, idx);
        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, exePath:{1}, controllerPort:{2}>", Head, EXEPath, ControllerPort);
    }
}


public class LaunchRequestResponse : RequestResponse
{
    public enum ENUM_LAUNCH_STATUS
    {
        OK = 0,
        NotFound,
        IsRunning,
        Error,
    }

    private long _requestId;
    public long LaunchRequestID
    {
        get { return _requestId; }
        set { _requestId = value; }
    }

    private ENUM_LAUNCH_STATUS _status;
    public ENUM_LAUNCH_STATUS Status
    {
        get { return _status; }
        set { _status = value; }
    }

    private string _appName;
    public string AppName
    {
        get { return _appName ?? (_appName = ""); }
        set { _appName = value; }
    }

    public LaunchRequestResponse(long id_, ENUM_LAUNCH_STATUS status_)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = 0,
            Type = MessageType.ACK_Launch_OK,
        };

        Status = status_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertLong(LaunchRequestID, ref Data, Size, idx);
        idx = InsertInt((int)Status, ref Data, Size, idx);
        idx = InsertString(AppName, ref Data, Size, idx);

        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);

        idx = GetLong(out _requestId, ref Data, Size, idx);

        int status;
        idx = GetInt(out status, ref Data, Size, idx);
        Status = (ENUM_LAUNCH_STATUS)status;

        if (Status != ENUM_LAUNCH_STATUS.OK)
        {
            idx = GetString(out _appName, ref Data, Size, idx);
        }

        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, RequestID:{1}, Status:{2}, AppName:{3}", Head, LaunchRequestID, Status, AppName);
    }
}

public class MsgRequestShutdownPlayer : Message
{
    private int _controllerPort;
    public int ControllerPort
    {
        get { return _controllerPort; }
        set { _controllerPort = value; }
    }

    private string _controllerIP;
    public string ControllerIP
    {
        get { return _controllerIP; }
        set { _controllerIP = value; }
    }

    public MsgRequestShutdownPlayer(long id_, int nodeId_)
    {
        Head = new MessageHead();
        Head.ID = id_;
        Head.Type = MessageType.REQUEST_ShutdownPlayer;
        Head.NodeID = nodeId_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertInt(ControllerPort, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);
        idx = GetInt(out _controllerPort, ref Data, Size, idx);
        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, ControllerPort:{1}>", Head, ControllerPort);
    }
}

public class ShutdownRequestResponse : Message
{
    public enum ENUM_SHUTDOWN_STATUS
    {
        OK = 0,
    }

    private long _requestId;
    public long RequestID
    {
        get { return _requestId; }
        set { _requestId = value; }
    }

    private ENUM_SHUTDOWN_STATUS _status;
    public ENUM_SHUTDOWN_STATUS Status
    {
        get { return _status; }
        set { _status = value; }
    }

    public ShutdownRequestResponse(long id_, ENUM_SHUTDOWN_STATUS status_)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = 0,
            Type = MessageType.ACK_Shutdown_OK,
        };

        Status = status_;
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertLong(_requestId, ref Data, Size, idx);
        idx = InsertInt((int)Status, ref Data, Size, idx);

        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        int idx = base.Deserialize(data_, size_);

        idx = GetLong(out _requestId, ref Data, Size, idx);

        int status;
        idx = GetInt(out status, ref Data, Size, idx);
        Status = (ENUM_SHUTDOWN_STATUS)status;
        return idx;
    }

    public override string ToString()
    {
        return string.Format("<head:{0}, RequestID:{1}, Status:{2}", Head, RequestID, Status);
    }
}

//KeepAlive
public class KeepAliveRequest : Message
{
    public KeepAliveRequest(long id_)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = 0,
            Type = MessageType.CMD_KeepAlive,
        };
    }
}

public class MsgStatusPaused : Message
{
    public MsgStatusPaused(long id_ = 0)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = 0,
            Type = MessageType.STATUS_Paused,
        };
    }

    public override int Serialize(byte[] data_)
    {
        return base.Serialize(data_);
    }
    public override int Deserialize(byte[] data_, int size_)
    {
        return base.Deserialize(data_, size_);
    }
}

public class MsgStatusHMD : Message
{

    private bool _IsHMDOn;
    public bool On
    {
        get { return _IsHMDOn; }
        set { _IsHMDOn = value; }
    }

    public MsgStatusHMD(long id_ = 0)
    {
        Head = new MessageHead()
        {
            ID = id_,
            NodeID = 0,
            Type = MessageType.STATUS_HMD,
        };
    }

    public override int Serialize(byte[] data_)
    {
        int idx = base.Serialize(data_);
        idx = InsertInt(On ? 1 : 0, ref Data, Size, idx);
        return idx;
    }

    public override int Deserialize(byte[] data_, int size_)
    {
        var idx = base.Deserialize(data_, size_);
        int value;
        idx = GetInt(out value, ref Data, Size, idx);
        On = value == 1;
        return idx;
    }
}

public class CVideoInfo
{
    public long id;
    public string name;
}

public class MsgAckRegisterOK : Message
{
    public List<CVideoInfo> videos;

    public MsgAckRegisterOK(int _id, int nodeID)
    {
        Head = new MessageHead();
        Head.ID = _id;
        Head.NodeID = nodeID;
        Head.Type = MessageType.ACK_Register_OK;
        videos = new List<CVideoInfo>();
    }

    private int InsertVideos(List<CVideoInfo> videos, ref byte[] _data, int _size, int startIdx)
    {
        int n = videos.Count;
        int idx = InsertLong(n, ref _data, _size, startIdx);
        for (int i = 0; i < n; i++)
        {
            idx = InsertLong(videos[i].id, ref _data, _size, idx);
            idx = InsertString(videos[i].name, ref _data, _size, idx);
        }
        return idx;
    }
    private int GetVideos(out List<CVideoInfo> videos, ref byte[] _data, int _size, int startIdx)
    {
        videos = new List<CVideoInfo>();
        var idx = startIdx;
        try
        {
            long n = 0;
            idx = GetLong(out n, ref _data, _size, startIdx);
            for (int i = 0; i < n; i++)
            {
                CVideoInfo info = new CVideoInfo();
                idx = GetLong(out info.id, ref _data, _size, idx);
                idx = GetString(out info.name, ref _data, _size, idx);
            }
        }
        catch (IndexOutOfRangeException)
        {
            //("Ingore no video info");
        }
        return idx;
    }
    public override int Serialize(byte[] _data)
    {
        int idx = base.Serialize(_data);
        idx = InsertVideos(videos, ref Data, Size, idx);
        return idx;
    }
    public override int Deserialize(byte[] _data, int _size)
    {
        int idx = base.Deserialize(_data, _size);
        idx = GetVideos(out videos, ref Data, Size, idx);
        return idx;
    }
}

//GetNodeInfo
public class VideoIDInfo
{
    public enum ENUM_INFO_TYPE
    {
        OK = 0,
        VideoNotFound,
    }

    private string _videoName = "";
    public string VideoName
    {
        get { return _videoName; }
        set { _videoName = value; }
    }

    private ENUM_INFO_TYPE _type = ENUM_INFO_TYPE.OK;
    public ENUM_INFO_TYPE Type
    {
        get { return _type; }
        set { _type = value; }
    }

    private string _videoId = "";
    public string VideoID
    {
        get { return _videoId; }
        set { _videoId = value; }
    }

    public override string ToString()
    {
        return string.Format("<videoName:{0}, type:{1},videoId:{2}>", VideoName, Type, VideoID);
    }
}

public class RequestResponse : Message
{
    public long RequestID
    {
        get { return Head.RequestID; }
        set { Head.RequestID = value; }
    }
}
