using System;
using System.ComponentModel;
using IComm_Library;
using System.Collections.Generic;
using System.Linq;
using Opc.Ua;
using Opc.Ua.Client;

namespace OPC_UA_Library
{

  public class OPCTag : INotifyPropertyChanged, ITag
  {
    #region Attributes

    private OPCClient ClientBound;        //Reference to library value.


    //External Action to execute on tag value changed
    private Action<ITag> _OnTagValueChange;
    public Action<ITag> OnTagValueChange
    {
      get
      {
        return _OnTagValueChange;
      }
      private set
      {
        _OnTagValueChange = value;
        OnPropertyChanged(nameof(OnTagValueChange));
      }
    }

    private string _Name;
    public string Name
    {
      get
      {
        return _Name;
      }
      private set
      {
        _Name = value;
        OnPropertyChanged(nameof(Name));
      }
    }

    private string _Address;
    public string Address
    {
      get
      {
        return _Address;
      }
      private set
      {
        _Address = value;
        OnPropertyChanged(nameof(Address));
      }
    }

    private ushort _NameSpaceIndex;
    public ushort NameSpaceIndex
    {
      get
      {
        return _NameSpaceIndex;
      }
      private set
      {
        _NameSpaceIndex = value;
        OnPropertyChanged(nameof(NameSpaceIndex));
      }
    }

    private object _Value;
    public object Value
    {
      get
      {
        return _Value;
      }
      private set
      {
        _Value = value;
        OnPropertyChanged(nameof(Value));
      }
    }

    private bool _IsSubscribed;
    public bool IsSubscribed
    {
      get
      {
        return _IsSubscribed;
      }
      private set
      {
        _IsSubscribed = value;
        OnPropertyChanged(nameof(IsSubscribed));
      }
    }

    private bool _IsOnCyclicalRefresh;
    public bool IsOnCyclicalRefresh
    {
      get
      {
        return _IsOnCyclicalRefresh;
      }
      private set
      {
        _IsOnCyclicalRefresh = value;
        OnPropertyChanged(nameof(IsOnCyclicalRefresh));
      }
    }

    private bool _IsAvailable;
    public bool IsAvailable
    {
      get
      {
        return _IsAvailable;
      }
      private set
      {
        _IsAvailable = value;
        OnPropertyChanged(nameof(IsAvailable));
      }
    }

    private int _TagSize;
    public int TagSize
    {
      get
      {
        return _TagSize;
      }
    }

    private int _ElementN;
    public int ElementN
    {
      get
      {
        return _ElementN;
      }
    }


    private int? _TagTypeSize;
    public int? TagTypeSize
    {
      get
      {
        return _TagTypeSize;
      }
    }

    private Type _TagType;
    public Type TagType
    {
      get
      {
        return _TagType;
      }
      private set
      {
        _TagType = value;
        OnPropertyChanged(nameof(TagType));
      }
    }

    private int? _RefreshCycle;
    public int? RefreshCycle
    {
      get
      {
        return _RefreshCycle;
      }
      private set
      {
        _RefreshCycle = value;
        OnPropertyChanged(nameof(RefreshCycle));
      }
    }

    private int? _Subscription_MaxEventFrequency;
    public int? Subscription_MaxEventFrequency
    {
      get
      {
        return _Subscription_MaxEventFrequency;
      }
      private set
      {
        _Subscription_MaxEventFrequency = value;
        OnPropertyChanged(nameof(Subscription_MaxEventFrequency));
      }
    }

    private int? _Subscription_MaxDelayToFireEvent;
    public int? Subscription_MaxDelayToFireEvent
    {
      get
      {
        return _Subscription_MaxDelayToFireEvent;
      }
      private set
      {
        _Subscription_MaxDelayToFireEvent = value;
        OnPropertyChanged(nameof(Subscription_MaxDelayToFireEvent));
      }
    }

    private DateTime _LastUpdate;
    public DateTime LastUpdate
    {
      get
      {
        return _LastUpdate;
      }
      private set
      {
        _LastUpdate = value;
        OnPropertyChanged(nameof(LastUpdate));
      }
    }

    private TagQuality _Quality;
    public TagQuality Quality
    {
      get
      {
        return _Quality;
      }
      private set
      {
        _Quality = value;
        OnPropertyChanged(nameof(Quality));
      }
    }

    #endregion

    #region Constructors

    internal OPCTag(OPCClient refOPCClient, string name, string address, Type type, int? tagTypeSize = null, int elementN = 1)
    {

      //Reference to twincat client and to OPC client
      ClientBound = refOPCClient;

      _Quality = TagQuality.INIT;

      _Name = string.IsNullOrWhiteSpace(name) ? _Address : name;
      _TagType = type;
      _TagTypeSize = tagTypeSize;
      _ElementN = elementN;

      _Address = address;

      CheckItem();
    }

    #endregion

    #region ITag methods implementation

    public bool WriteItem(object ValueToWrite)
    {
      if (ClientBound != null && ClientBound.IsConnected && ClientBound.IsAvailable && this.IsAvailable)
      {
        try
        {

          var MyValueCasted = Convert.ChangeType(ValueToWrite, TagType);
          DataValue val = new DataValue();
          val.Value = MyValueCasted;


          List<WriteValue> nodesToWrite = new List<WriteValue>();
          nodesToWrite.Add(new WriteValue()
          {
            NodeId = new NodeId(Address),
            AttributeId = Attributes.Value,
            Value = val
          });
          WriteValueCollection nodesToWriteCollection = new WriteValueCollection(nodesToWrite);
          StatusCodeCollection results;
          DiagnosticInfoCollection diagnosticInfos;

          RequestHeader requestHeader = new RequestHeader();
          requestHeader.ReturnDiagnostics = 0;

          ResponseHeader RS = ClientBound.ClientSession.Write(
              requestHeader,
              nodesToWriteCollection,
              out results,
              out diagnosticInfos);

          StatusCode code = results.First();
          if (StatusCode.IsGood(code))
          {
            Quality = TagQuality.GOOD;
            LastUpdate = DateTime.Now;
            return true;
          }
          else
          {
            Quality = TagQuality.UNKNOWN;
            LastUpdate = DateTime.Now;
            ClientBound.Logger?.LogWarning($"OPC - Write Error on: {this.Name} @ {this.Address}. Reason: {code.ToString()}");
            return false;
          }
        }
        catch (Exception ex)
        {
          Quality = TagQuality.ERROR;
          ClientBound.Logger?.LogWarning($"OPC - Write Error on: {this.Name} @ {this.Address}. Ex: {ex.Message}");
          return false;
        }
      }
      else
      {
        Quality = TagQuality.DISCONNECTED;
        return false;
      }
    }
    public bool ReadItem()
    {
      if (ClientBound != null && ClientBound.IsConnected && ClientBound.IsAvailable)
      {
        try
        {
          //var resss = ClientBound.ClientSession.ReadValue(new NodeId(Address), TagType);
          DataValue result = ClientBound.ClientSession.ReadValue(new NodeId(Address));
          if (StatusCode.IsGood(result.StatusCode))
          {
            Value = result.Value;
            TagType = result.Value.GetType();
            Quality = TagQuality.GOOD;
            LastUpdate = result.SourceTimestamp;
            return true;
          }
          else if (StatusCode.IsUncertain(result.StatusCode))
          {
            Quality = TagQuality.UNKNOWN;
            LastUpdate = result.SourceTimestamp;
            return false;
          }
          else
          {
            Quality = TagQuality.NOT_AVAILABLE;
            LastUpdate = result.SourceTimestamp;
            return false;
          }
        }
        catch (Exception ex)
        {
          Quality = TagQuality.ERROR;
          ClientBound.Logger?.LogWarning($"OPC - Read Error on: {this.Name} @ {this.Address}. Ex: {ex.Message}");
          return false;
        }
      }
      else
      {
        Quality = TagQuality.DISCONNECTED;
        return false;
      }
    }

    public void SubscribeItem(int? EventMaxFrequency = 0, int? MaxDelayToFireEvent = 0, Action<ITag> OnTagValueChangeAction = null)
    {
      var eventMaxFrequency = (EventMaxFrequency.HasValue) ? EventMaxFrequency.Value : 0;
      var maxDelayToFireEvent = (MaxDelayToFireEvent.HasValue) ? MaxDelayToFireEvent.Value : 0;

      if (ClientBound != null)
      {
        //Call OPC client method. [Subscription dictionary must be updated]
        ClientBound.SubscribeTag(this, eventMaxFrequency, maxDelayToFireEvent, OnTagValueChangeAction);
      }

    }

    public void UnsubscribeItem()
    {
      if (ClientBound != null)
      {
        ClientBound.UnsubscribeTag(this);
      }
    }

    #endregion

    #region Private \ Internal Methods

    #region Tag Creation

    internal void CheckItem()
    {
      try
      {
        //Try to check if Tag is available
        if ((ClientBound.ClientSession != null) && ClientBound.IsConnected)
        {
          //Try to read item to check if is available
          IsAvailable = this.ReadItem();
        }
        else
        {
          //Tag always available if online
          IsAvailable = true;
        }
      }
      catch (Exception ex)
      {
        Quality = TagQuality.NOT_AVAILABLE;
        ClientBound.Logger?.LogException($"OPC - Tag creation error on: {this.Name} @ {this.Address}. Ex: {ex.Message}");
        IsAvailable = false;
      }
    }

    //private bool SetTagAddress(string address)
    //{
    //    //get Namespace
    //    string[] TagAddress = address.Split(';');
    //    if (TagAddress.Length < 2 || (address.Count(f => f == '=')!=2))
    //    {
    //        //WRONG ADDRESS TYPE
    //        IsAvailable = false;
    //        _Address = address;
    //        ClientBound.Logger?.LogException($"OPC - Tag creation error on: {this.Name} @ {this.Address}. Address must be in the format: ns=x, s=xxx");
    //        return false;
    //    }
    //    else
    //    {
    //        string[] NamespacePart = TagAddress[0].Split('=');
    //        if (NamespacePart.Length >= 2)
    //        {
    //            int NS = 0;
    //            if (Int32.TryParse(NamespacePart[1], out NS))
    //            {
    //                _NameSpaceIndex = (ushort)NS;
    //            }
    //        }
    //        string[] AddressPart = TagAddress[1].Split('=');
    //        if (AddressPart.Length >= 2)
    //        {
    //            _Address = AddressPart[1];
    //        }
    //        return true;
    //    }
    //}

    #endregion

    #region Tag Subscription

    internal void OnSubscriptionNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
    {
      try
      {
        foreach (var value in item.DequeueValues())
        {
          //Update tag
          if (StatusCode.IsGood(value.StatusCode))
          {
            Value = value.Value;
            LastUpdate = value.SourceTimestamp;
            Quality = TagQuality.GOOD;
            //Execute external event
            _OnTagValueChange?.Invoke(this);
          }
          else
          {
            if (StatusCode.IsUncertain(value.StatusCode))
            {
              Quality = TagQuality.UNKNOWN;
            }
            else if (StatusCode.IsBad(value.StatusCode))
            {
              Quality = TagQuality.ERROR;
            }
            else
            {
              Quality = TagQuality.UNKNOWN;
            }
            ClientBound.Logger?.LogWarning($"OPCTag {this.Name} quality's {this.Quality}");
          }
        }
      }
      catch (Exception ex)
      {
        ClientBound.Logger?.LogException($"OPC - Tag Subscription Error. Address: {this.Address}. Ex: {ex.Message}");
      }
    }
    internal void SetToSubscription(int MaxSubscriptionFrequency, int MaxDelayToFireEvent, Action<ITag> OnTagValueChangeAction = null)
    {
      try
      {
        if (ClientBound != null && !IsSubscribed && this.IsAvailable)
        {
          //TODO    
          //_NotificationHandler = ClientBound.ClientUA.SubscribeDataChange(
          //    ClientBound.Address,
          //    this.Address,
          //    MaxSubscriptionFrequency,
          //    (sender, eventArgs) => GetSubscriptionValue(eventArgs));


          _OnTagValueChange = OnTagValueChangeAction;
          Subscription_MaxEventFrequency = MaxSubscriptionFrequency;
          //Subscription_MaxDelayToFireEvent = MaxDelayToFireEvent;
          IsSubscribed = true;
        }
      }
      catch (Exception ex)
      {
        Quality = TagQuality.ERROR;
        ClientBound.Logger?.LogException($"OPC - Tag Subscription error. Address: {this.Address}. Ex: {ex.Message}");
      }
    }
    internal void RemoveFromSubscription()
    {
      try
      {
        //TODO
        //if (ClientBound != null && IsSubscribed && this.IsAvailable)
        //{
        //    ClientBound.ClientUA.UnsubscribeMonitoredItem(_NotificationHandler);

        //    _NotificationHandler = 0;
        //    IsSubscribed = false;
        //    Subscription_MaxEventFrequency = null;
        //    Subscription_MaxDelayToFireEvent = null;
        //}
      }
      catch (Exception ex)
      {
        Quality = TagQuality.ERROR;
        ClientBound.Logger?.LogException($"OPC - Failed to remove Subscription. Address: {this.Address}. Ex: {ex.Message}");
      }
    }

    #endregion

    #region Tag Cyclical refresh
    internal void SetToCyclicalRefresh(int RefreshCycleInterval)
    {
      IsOnCyclicalRefresh = true;
      RefreshCycle = RefreshCycleInterval;
    }

    internal void RemoveFromCyclicalRefresh()
    {
      IsOnCyclicalRefresh = false;
      RefreshCycle = null;
    }
    #endregion

    #region Tag update
    internal bool UpdateTag(ServiceResult result, object val = null)
    {
      if (StatusCode.IsGood(result.StatusCode))
      {
        if (val != null)
        {
          Value = val;
        }
        Quality = TagQuality.GOOD;
        LastUpdate = DateTime.Now;
        return true;
      }
      else if (StatusCode.IsUncertain(result.StatusCode))
      {
        Quality = TagQuality.UNKNOWN;
        LastUpdate = DateTime.Now;
        return false;
      }
      else
      {
        Quality = TagQuality.NOT_AVAILABLE;
        LastUpdate = DateTime.Now;
        return false;
      }
    }

    #endregion
    #endregion


    #region Property Changed

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(name));
      }
    }

    #endregion
  }
}
