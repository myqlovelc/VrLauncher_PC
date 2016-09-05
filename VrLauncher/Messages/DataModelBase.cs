using System;
using System.Xml;
using System.Collections;
using System.Xml.Serialization;

public interface INotifyPropertyChanged
{
    event Action<INotifyPropertyChanged, object> PropertyChanged;
}

public class DataModelBase : INotifyPropertyChanged
{
    private bool _dirty;
    [XmlIgnore]
    public bool Dirty
    {
        get { return _dirty; }
        set { _dirty = value; OnPropertyChanged("Dirty"); }
    }

    public event System.Action<INotifyPropertyChanged, object> PropertyChanged;
    protected void OnPropertyChanged(string prop_)
    {
        if (prop_ != "Dirty") Dirty = true;

        if (PropertyChanged != null)
        {
            PropertyChanged(this, prop_);
        }
    }
}
