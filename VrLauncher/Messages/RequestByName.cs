using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VrService
{
    public class RequestByName : Message
    {
        protected string _name;
        public string Name
        {
            get { return _name ?? (_name = ""); }
        }

        public RequestByName()
        {
            Head.Type = MessageType.REQUEST_BY_NAME;

            var type = GetType();
            _name = type.Name;
        }

        public override int Serialize(byte[] data_)
        {
            var idx = base.Serialize(data_);
            return InsertString(_name, ref Data, Size, idx);
        }

        public override int Deserialize(byte[] data_, int size_)
        {
            var idx = base.Deserialize(data_, size_);
            return GetString(out _name, ref Data, Size, idx);
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
}
