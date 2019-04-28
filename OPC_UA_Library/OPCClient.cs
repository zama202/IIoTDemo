using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Timers;
using IComm_Library;
using IComm_Library.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System.Reflection;

namespace OPC_UA_Library
{
  public class OPCClient : INotifyPropertyChanged, IClient
  {
    #region Attributes

    internal Session ClientSession;
    internal SessionReconnectHandler SessionReconnect;
    internal Subscription ClientSubscription;

    internal ILogger Logger;

    internal SecurityPolicy? SecurityPolicy = null;
    internal MessageSecurity? MessageSecurity = null;

    // TagName is used as a key
    private Dictionary<string, OPCTag> _TagDictionary = new Dictionary<string, OPCTag>();
    public Dictionary<string, ITag> TagDictionary
    {
      get
      {
        var ITagDictionary = new Dictionary<string, ITag>();
        foreach (KeyValuePair<string, OPCTag> item in _TagDictionary)
        {
          ITagDictionary.Add(item.Key, item.Value);
        }
        return ITagDictionary;
      }
    }

    private Dictionary<string, OPCTag> TagSubscriptionDictionary = new Dictionary<string, OPCTag>();

    private List<OPCTag> TagCyclicalRefresh_List = new List<OPCTag>();

    //External Action to execute on tag value changed
    private Action<List<ITag>> OnTagCyclicalRefresh;

    private string _ClientName;
    public string ClientName
    {
      get
      {
        return _ClientName;
      }
      private set
      {
        _ClientName = value;
        OnPropertyChanged(nameof(ClientName));
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

    private int _Port;
    public int Port
    {
      get
      {
        return _Port;
      }
      private set
      {
        _Port = value;
        OnPropertyChanged(nameof(Port));
      }
    }

    private string _OPC_User;
    public string OPC_User
    {
      get
      {
        return _OPC_User;
      }
      private set
      {
        _OPC_User = value;
        OnPropertyChanged(nameof(OPC_User));
      }
    }

    private string _OPC_Password;
    public string OPC_Password
    {
      get
      {
        return _OPC_Password;
      }
      private set
      {
        _OPC_Password = value;
        OnPropertyChanged(nameof(OPC_Password));
      }
    }

    private bool _IsConnected;
    public bool IsConnected
    {
      get
      {
        return _IsConnected;
      }
      private set
      {
        _IsConnected = value;
        OnPropertyChanged(nameof(IsConnected));
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

    private bool _IsCyclicalRefreshActive;
    public bool IsCyclicalRefreshActive
    {
      get
      {
        return _IsCyclicalRefreshActive;
      }
      private set
      {
        _IsCyclicalRefreshActive = value;
        OnPropertyChanged(nameof(IsCyclicalRefreshActive));
      }
    }

    private bool _IsSubscriptionActive;
    public bool IsSubscriptionActive
    {
      get
      {
        return _IsSubscriptionActive;
      }
      private set
      {
        _IsSubscriptionActive = value;
        OnPropertyChanged(nameof(IsSubscriptionActive));
      }
    }
    #endregion

    #region Constructors

    // Hide the default constructor
    private OPCClient() { }

    // Full constructor
    public OPCClient(string OPCAddress, string username, string password, ILogger logger, string clientName, SecurityPolicy securityPolicy, MessageSecurity messageSecurity)
    {
      //Initialize Logger
      Logger = logger;
      Logger?.Init();

      _Address = OPCAddress;     //Example:  opc.tcp://localhost:4840
      _ClientName = clientName;
      _OPC_User = username;
      _OPC_Password = password;

      SecurityPolicy = securityPolicy;
      MessageSecurity = messageSecurity;

      //Create opc client session
      Init();

      if (IsAvailable)
      {
        Logger?.LogDebug($"OPC - Created new client: {clientName}. Address: {_Address}.");
      }
      else
      {
        Logger?.LogDebug($"OPC - Client: {clientName} initialization failure. Started reconnection procedure.");
        //Check Connection Every X seconds
        Set_CheckConnection();
      }
    }


    public OPCClient(string OPCAddress, string username, string password, ILogger logger, string clientName)
    {
      //Initialize Logger
      Logger = logger;
      Logger?.Init();

      _Address = OPCAddress;     //Example:  opc.tcp://localhost:4840
      _ClientName = clientName;
      _OPC_User = username;
      _OPC_Password = password;

      //Create opc client session
      Init();

      if (IsAvailable)
      {
        Logger?.LogDebug($"OPC - Created new client: {clientName}. Address: {_Address}.");
      }
      else
      {
        Logger?.LogDebug($"OPC - Client: {clientName} initialization failure. Started reconnection procedure.");
        //Check Connection Every X seconds
        Set_CheckConnection();
      }
    }

    public OPCClient(string OPCAddress)
        : this(OPCAddress, "", "", null, "OPC_Client") { }

    public OPCClient(string OPCAddress, string username, string password)
        : this(OPCAddress, username, password, null, "OPC_Client") { }

    public OPCClient(string OPCAddress, ILogger logger)
        : this(OPCAddress, "", "", logger, "OPC_Client") { }

    private void Init()
    {
      try
      {
        ApplicationInstance application = new ApplicationInstance
        {
          ApplicationName = ClientName,
          ApplicationType = ApplicationType.Client,
          ConfigSectionName = string.Empty 
        };


        var config = new ApplicationConfiguration()
        {
          ApplicationName = ClientName,
          ApplicationUri = $"urn:localhost:{ClientName}",          //"urn:localhost:OPCFoundation:CoreSampleClient",  $"urn:localhost:{ClientName}",

          ApplicationType = ApplicationType.Client,
          ProductUri = "http://opcfoundation.org/UA/CoreSampleClient",
          SecurityConfiguration = new SecurityConfiguration
          {
            ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%LocalApplicationData%\OPC Foundation\CertificateStores\MachineDefault", SubjectName = Utils.Format(@"CN={0}, DC={1}", ClientName, "localhost") },//System.Net.Dns.GetHostName()) },
            TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%LocalApplicationData%/OPC Foundation/pki/issuer" },
            TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%LocalApplicationData%/OPC Foundation/pki/trusted" },
            RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%LocalApplicationData%/OPC Foundation/pki/rejected" },
            AutoAcceptUntrustedCertificates = true,
            RejectSHA1SignedCertificates = false,
            MinimumCertificateKeySize = 512
          },
          TransportConfigurations = new TransportConfigurationCollection(),
          TransportQuotas = new TransportQuotas
          {
            OperationTimeout = 15000,
            MaxStringLength = 1048576,
            MaxByteStringLength = 4194304,
            MaxBufferSize = 65535,
            ChannelLifetime = 300000,
            SecurityTokenLifetime = 3600000
          },

          ClientConfiguration = new ClientConfiguration
          {
            DefaultSessionTimeout = 60000,
            WellKnownDiscoveryUrls = new StringCollection(new List<string> { "opc.tcp://{0}:4840/UADiscovery" }),
            DiscoveryServers = new EndpointDescriptionCollection(),
            MinSubscriptionLifetime = 10000

          },
          TraceConfiguration = new TraceConfiguration()
          {
            OutputFilePath = @"%LocalApplicationData%/Logs/Opc.Ua.CoreSampleClient.log.txt",
            DeleteOnLoad = true
          },
          CertificateValidator = new CertificateValidator(),
        };


        application.ApplicationConfiguration = ApplicationInstance.FixupAppConfig(config);
        config.Validate(ApplicationType.Client);


        // check the application certificate.
        bool haveAppCertificate = application.CheckApplicationInstanceCertificate(false, 0).Result;
        if (!haveAppCertificate)
        {
          throw new Exception("Application instance certificate invalid!");
        }

        if (haveAppCertificate)
        {
          config.ApplicationUri = Utils.GetApplicationUriFromCertificate(config.SecurityConfiguration.ApplicationCertificate.Certificate);
          config.SecurityConfiguration.AutoAcceptUntrustedCertificates = true;
          config.SecurityConfiguration.RejectSHA1SignedCertificates = false;
          //config.SecurityConfiguration.MinimumCertificateKeySize = 1024;
          config.CertificateValidator.CertificateValidation += CertificateValidator_CertificateValidation;
        }

        var securityEnabled = true;
        if (string.IsNullOrWhiteSpace(_OPC_User) && string.IsNullOrWhiteSpace(_OPC_Password))
        {
          securityEnabled = false;
        }

        EndpointDescription selectedEndpoint;
        if (SecurityPolicy.HasValue && MessageSecurity.HasValue)
        {
          selectedEndpoint = OPCCoreUtils.SelectEndpointBySecurity(Address, SecurityPolicy.Value, MessageSecurity.Value, 15000);
        }
        else
        {

          selectedEndpoint = CoreClientUtils.SelectEndpoint(Address, useSecurity: securityEnabled, operationTimeout: 15000);
        }
        Logger?.LogDebug($"Endpoint selected: {selectedEndpoint.SecurityPolicyUri} {selectedEndpoint.SecurityMode}");  // log the security mode used
        var endpointConfiguration = EndpointConfiguration.Create(config);
        var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfiguration);

        // set the port (avoid splitting strings)
        Port = endpoint.EndpointUrl.Port;

        UserIdentity UI;
        if (securityEnabled)
        {
          UI = new UserIdentity(OPC_User, OPC_Password);
        }
        else
        {
          UI = new UserIdentity(new AnonymousIdentityToken());
        }

        // N.B. do  NOT  lower the sessionTimeout when creating ClientSession 
        ClientSession = Session.Create(config, endpoint, false, $"{ClientName}", 10000, UI, null).Result;

        // register Event handlers
        ClientSession.KeepAlive += Client_KeepAlive;
        ClientSession.Notification += NotificationEventHandler;
        ClientSession.SubscriptionsChanged += SubscriptionChangedHandler;
        ClientSession.PublishError += PublishErrorEventHandler;
        ClientSession.RenewUserIdentity += RenewUserIdentityEventHandler;
        ClientSession.SessionClosing += SessionClosingHandler;

        IsAvailable = true;

      }
      catch (Exception ex)
      {
        Logger?.LogDebug($"OPC - Client Initilization error. Ex: {ex.Message} - Type: {ex.InnerException?.Message}");
        IsAvailable = false;
      }
    }

    #endregion

    private void CertificateValidator_CertificateValidation(CertificateValidator validator, CertificateValidationEventArgs e)
    {
      if (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted)
      {
        e.Accept = true;
        Logger?.LogDebug($"OPC - Certificate accepted: {e.Certificate.Subject}.");
      }
    }

    [Obsolete("Not required by library", false)]
    public bool Connect(bool LogConnectionError = false)
    {
      try
      {
        return IsConnected;
      }
      catch (Exception ex)
      {
        IsConnected = false;
        IsAvailable = false;
        if (LogConnectionError)
        {
          Logger?.LogException($"OPC - Client connection error. Ex: {ex.Message}.");
        }
        return false;
      }

    }

    public void Disconnect()
    {
      Logger?.LogDebug($"OPC - Started Disconnection procedure.");

      if (IsCyclicalRefreshActive)
      {
        StopCyclicalRefresh();
      }

      UnsubscribeTags();
      if (ClientSubscription != null)
      {
        ClientSession.RemoveSubscription(ClientSubscription);
      }
      ClientSession?.Close();
    }

    public void Dispose()
    {
      Disconnect();
      TagCyclicalRefresh_List.Clear();
      TagSubscriptionDictionary.Clear();
      _TagDictionary.Clear();
      ClientSession.Dispose();
    }

    #region OPC Event
    private void Client_KeepAlive(Session sender, KeepAliveEventArgs e)
    {
      //Update Status
      IsConnected = sender.Connected;


      if (e.Status != null && ServiceResult.IsNotGood(e.Status))
      {
        Logger?.LogDebug($"OPC - Client not connected. Status: {e.Status} - { sender.OutstandingRequestCount} / {sender.DefunctRequestCount}.");

        if (SessionReconnect == null)
        {
          Logger?.LogDebug($"OPC - Client RECONNECTING.");
          SessionReconnect = new SessionReconnectHandler();
          SessionReconnect.BeginReconnect(sender, 10 * 1000, Client_ReconnectComplete);
        }
      }
    }
    private void Client_ReconnectComplete(object sender, EventArgs e)
    {
      // ignore callbacks from discarded objects.
      if (!Object.ReferenceEquals(sender, SessionReconnect))
      {
        return;
      }

      ClientSession = SessionReconnect.Session;
      SessionReconnect.Dispose();
      SessionReconnect = null;

      foreach (Subscription subscription in ClientSession.Subscriptions)
      {
        ClientSubscription = subscription;
        break;
      }

      Logger?.LogDebug($"OPC - Client RECONNECTED.");
    }

    private void NotificationEventHandler(Session session, NotificationEventArgs e)
    {

    }

    private void SubscriptionChangedHandler(object sender, EventArgs e)
    {
      Logger?.LogDebug($"OPC - Client subscription changed. N subscription: {((Session)sender).SubscriptionCount}.");
    }

    private void PublishErrorEventHandler(Session session, PublishErrorEventArgs e)
    {

    }

    public IUserIdentity RenewUserIdentityEventHandler(Session session, IUserIdentity identity)
    {
      if (identity == null || identity.TokenType != UserTokenType.IssuedToken)
      {
        return identity;
      }
      else
      {
        return new UserIdentity(OPC_User, OPC_Password);
      }
    }

    public void SessionClosingHandler(object sender, EventArgs e)
    {

    }

    //private void OnServerConnectionStatusUpdate(Session sender, ServerConnectionStatusUpdateEventArgs e)
    //{
    //    Logger?.LogDebug($"OPC - connection status change. Previous Status: {e.PreviousStatus.ToString()}. Current Status: { e.Status.ToString()}");

    //    if (e.Status ==  ServerConnectionStatus.Connected)
    //    {
    //        IsConnected = true;
    //    }
    //    else
    //    {
    //        IsConnected = false;
    //        OPCStatus = OPCConnectionState.Disconnected;
    //    }
    //}


    #endregion

    #region Tags Management Methods

    public ITag GetTag(string TagName)
    {
      if (_TagDictionary.ContainsKey(TagName))
      {
        return _TagDictionary[TagName];
      }
      else
      {
        return null;
      }
    }

    public ITag AddTag(string TagName, string TagAddress, Type TagType, int? TagTypeSize = null, int ElementN = 1)
    {
      if (!_TagDictionary.ContainsKey(TagName))
      {
        OPCTag T = new OPCTag(this, TagName, TagAddress, TagType);
        _TagDictionary.Add(TagName, T);
        return T;
      }
      else
      {
        return _TagDictionary[TagName];
      }
    }

    public void RemoveTag(string TagName)
    {
      if (!_TagDictionary.ContainsKey(TagName))
      {
        _TagDictionary[TagName].RemoveFromSubscription();
        _TagDictionary.Remove(TagName);
      }
    }
    public object ReadTag(string tagName)
    {
      OPCTag T = (OPCTag)GetTag(tagName);
      if (T != null)
      {
        if (T.ReadItem())
        {
          return T.Value;
        }
      }
      return null;
    }
    public bool WriteTag(string tagName, object tagValue)
    {
      OPCTag T = (OPCTag)GetTag(tagName);
      if (T != null)
      {
        return T.WriteItem(tagValue);
      }
      else
      {
        return false;
      }
    }
    public bool ReadTags(List<string> tagNameList)
    {
      List<ITag> tagList = new List<ITag>();
      foreach (var item in tagNameList)
      {
        OPCTag T = (OPCTag)GetTag(item);
        if (T == null)
        {
          Logger?.LogException($"OPC - Multiple read error. Tag: {item} not found in dictionary.");
          return false;
        }
        tagList.Add(T);
      }
      return ReadTags(tagList);

    }
    public bool ReadTags(List<ITag> tagList)
    {
      try
      {
        if (!IsAvailable || !IsConnected)
        {
          return false;
        }


        List<NodeId> NodeIdList = new List<NodeId>();
        List<Type> ExpectedTypesList = new List<Type>();
        List<object> ObjectList = new List<object>();
        List<ServiceResult> ServiceResultList = new List<ServiceResult>();
        foreach (var tag in tagList)
        {
          NodeIdList.Add(new NodeId(tag.Address));
          ExpectedTypesList.Add(tag.TagType);
        }

        ClientSession.ReadValues(NodeIdList, ExpectedTypesList, out ObjectList, out ServiceResultList);
        int index = 0;
        bool Success = true;
        foreach (var MyServiceResult in ServiceResultList)
        {
          OPCTag tag = tagList[index] as OPCTag;
          if (!tag.UpdateTag(ServiceResultList[index], ObjectList[index]))
          {
            Success = false;
          }
          index++;
        }
        return Success;
      }
      catch (Exception ex)
      {
        Logger?.LogException($"OPC - Multiple read error. Ex: {ex.Message}.");
        return false;
      }
    }
    public bool WriteTags(List<string> tagNameList, List<object> objectList)
    {
      List<ITag> tagList = new List<ITag>();
      foreach (var item in tagNameList)
      {
        OPCTag T = (OPCTag)GetTag(item);
        if (T == null)
        {
          Logger?.LogException($"OPC - Multiple write error. Tag: {item} not found in dictionary.");
          return false;
        }
        tagList.Add(T);
      }
      return WriteTags(tagList, objectList);
    }
    public bool WriteTags(List<ITag> tagList, List<object> objectList)
    {
      try
      {
        if (!IsAvailable || !IsConnected)
        {
          return false;
        }



        if (tagList.Count() != objectList.Count())
        {
          Logger?.LogException($"OPC - WriteTags wrong values. Tags request: {tagList.Count()}. Value given: {objectList.Count()}.");
          return false;
        }



        WriteValueCollection nodesToWriteCollection = new WriteValueCollection();
        int index = 0;
        DataValue val = new DataValue();
        foreach (var tag in tagList)
        {
          Type varType = tagList[index].TagType;
          object varValue = objectList[index];
          var MyValueCasted = Convert.ChangeType(varValue, varType);
          val.Value = MyValueCasted;
          nodesToWriteCollection.Add(new WriteValue()
          {
            NodeId = new NodeId(tagList[index].Address),
            AttributeId = Attributes.Value,
            Value = val
          });
          index++;
        }

        StatusCodeCollection results;
        DiagnosticInfoCollection diagnosticInfos;

        RequestHeader requestHeader = new RequestHeader();
        //requestHeader.ReturnDiagnostics = 0;

        ResponseHeader RS = ClientSession.Write(
            requestHeader,
            nodesToWriteCollection,
            out results,
            out diagnosticInfos);

        bool Success = true;

        foreach (var result in results)
        {
          if (result.Code != StatusCodes.Good)
          {
            Success = false;
          }
        }

        index = 0;
        foreach (var item in results)
        {
          OPCTag tag = tagList[index] as OPCTag;
          if (!tag.UpdateTag(item))
          {
            Success = false;
          }
          index++;
        }
        return Success;
      }
      catch (Exception ex)
      {
        Logger?.LogException($"OPC - Multiple read error. Ex: {ex.Message}.");
        return false;
      }
    }

    #endregion

    #region Cyclical Refresh

    System.Timers.Timer RefreshSignal_Timer = new System.Timers.Timer();
    object RefreshSignal_Lock = new object();

    public void StartCyclicalRefresh(List<ITag> TagsToRead, int Interval = 100, Action<List<ITag>> OnTagCyclicalRefreshAction = null)
    {
      if (!IsCyclicalRefreshActive)
      {
        if (TagsToRead.Count() > 0)
        {
          IsCyclicalRefreshActive = true;
          foreach (OPCTag item in TagsToRead)
          {
            if (item.IsSubscribed)
            {
              Logger?.LogDebug($"OPC - Cyclical Tags read - Tags: {item.Name} cannot be set in cyclical update because is already in subscription.");
            }
            else if (!item.IsAvailable)
            {
              Logger?.LogDebug($"OPC - Cyclical Tags read - Tags: {item.Name} cannot be set in cyclical update because is not available on TwinCat.");
            }
            else
            {
              item.SetToCyclicalRefresh(Interval);
              TagCyclicalRefresh_List.Add(item);
            }
          }


          RefreshSignal_Timer.Elapsed += new ElapsedEventHandler(OnRefreshSignalEvent);
          RefreshSignal_Timer.Interval = Interval;
          RefreshSignal_Timer.Enabled = true;
          RefreshSignal_Timer.AutoReset = true;
          OnTagCyclicalRefresh = OnTagCyclicalRefreshAction;
          Logger?.LogDebug($"OPC - Cyclical Tags read - Started - Tags: {TagsToRead.Count()} Interval: {Interval} ms.");
        }
      }
      else
      {
        Logger?.LogDebug($"OPC - Cyclical Tags read - Requested a new start with cycle already running. Stop the current instance first.");
      }
    }

    public void StartCyclicalRefresh(List<string> TagsToReadName, int Interval = 100, Action<List<ITag>> OnTagCyclicalRefreshAction = null)
    {
      List<ITag> TagsToRead = new List<ITag>();
      foreach (string item in TagsToReadName)
      {
        OPCTag T = (OPCTag)GetTag(item);
        if (T != null)
        {
          TagsToRead.Add(T);
        }
      }
      StartCyclicalRefresh(TagsToRead, Interval, OnTagCyclicalRefreshAction);
    }

    public void StopCyclicalRefresh()
    {
      try
      {
        if (IsCyclicalRefreshActive)
        {
          OnTagCyclicalRefresh = null;
          RefreshSignal_Timer.Elapsed -= OnRefreshSignalEvent;

          IsCyclicalRefreshActive = false;
          RefreshSignal_Timer.Enabled = false;
          RefreshSignal_Timer.AutoReset = false;
          RefreshSignal_Timer.Stop();


          Logger?.LogDebug($"OPC - Cyclical Tags read - Stopped.");

          foreach (OPCTag item in TagCyclicalRefresh_List)
          {
            item.RemoveFromCyclicalRefresh();
          }
        }
      }
      catch (Exception ex)
      {
        Logger?.LogDebug($"OPC - Cyclical Tags read stop error. Ex: {ex.Message}");
      }

    }

    private void OnRefreshSignalEvent(object source, ElapsedEventArgs e)
    {

      if (Monitor.TryEnter(RefreshSignal_Lock))
      {
        try
        {
          if (IsConnected && IsAvailable)
          {
            List<ITag> TagRead_List = new List<ITag>();
            foreach (OPCTag item in TagCyclicalRefresh_List)
            {
              //item.ReadItem();
              TagRead_List.Add((ITag)item);
            }
            ReadTags(TagRead_List);

            if (OnTagCyclicalRefresh != null)
            {
              //The method is executed in the same task that has created the library
              MainContext.Post(new SendOrPostCallback((o) => { OnTagCyclicalRefresh?.Invoke(TagRead_List); }), null);
            }
          }
        }
        catch (Exception ex)
        {
          Logger?.LogException($"OPC - Error in cyclical Tag refresh cycle. Ex: {ex.Message}.");
        }
        finally
        {
          Monitor.Exit(RefreshSignal_Lock);
        }
      }
      else
      {
        // previous timer tick took too long.
        // so do nothing this time through.
        Logger?.LogWarning($"OPC - Cyclical Tags read - Warning refresh frequency is too high! Not enought time to complete refresh task.");
      }
    }

    #endregion

    #region Value Change Subscription

    public void SubscribeTags(List<string> TagsToSubscribeNames, int EventMaxFrequency = 0, int MaxDelayToFireEvent = 0, Action<ITag> OnTagValueChangeAction = null)
    {
      foreach (string item in TagsToSubscribeNames)
      {
        if (!TagSubscriptionDictionary.ContainsKey(item))
        {
          OPCTag T = (OPCTag)GetTag(item);
          if (T != null)
          {
            TagSubscriptionDictionary.Add(T.Name, T);
            T.SetToSubscription(EventMaxFrequency, MaxDelayToFireEvent, OnTagValueChangeAction);
          }
        }
      }
      CreateSubscription();
    }
    public void SubscribeTags(List<ITag> TagsToSubscribe, int EventMaxFrequency = 0, int MaxDelayToFireEvent = 0, Action<ITag> OnTagValueChangeAction = null)
    {
      int SubscribedTagsN = 0;
      if (TagsToSubscribe.Count > 0)
      {
        foreach (var item in TagsToSubscribe)
        {
          if (!TagSubscriptionDictionary.ContainsKey(item.Name))
          {
            OPCTag T = item as OPCTag;
            TagSubscriptionDictionary.Add(T.Name, T);
            T.SetToSubscription(EventMaxFrequency, MaxDelayToFireEvent, OnTagValueChangeAction);
          }
        }
        CreateSubscription();
        Logger?.LogDebug($"OPC - Tag Subscription - Added to subscription: {SubscribedTagsN} Tags. Event max frequency:{EventMaxFrequency}. Total tags in subscription: {TagSubscriptionDictionary.Count()}");
      }
    }
    public void SubscribeTag(string TagToSubscribeName, int EventMaxFrequency = 0, int MaxDelayToFireEvent = 0, Action<ITag> OnTagValueChangeAction = null)
    {
      OPCTag T = (OPCTag)GetTag(TagToSubscribeName);
      if (!TagSubscriptionDictionary.ContainsKey(TagToSubscribeName))
      {
        TagSubscriptionDictionary.Add(T.Name, T);
        T.SetToSubscription(EventMaxFrequency, MaxDelayToFireEvent, OnTagValueChangeAction);
      }
      CreateSubscription();

    }
    public void SubscribeTag(ITag TagToSubscribe, int EventMaxFrequency = 0, int MaxDelayToFireEvent = 0, Action<ITag> OnTagValueChangeAction = null)
    {
      if (!TagSubscriptionDictionary.ContainsKey(TagToSubscribe.Name))
      {
        OPCTag T = TagToSubscribe as OPCTag;
        TagSubscriptionDictionary.Add(TagToSubscribe.Name, TagToSubscribe as OPCTag);
        T.SetToSubscription(EventMaxFrequency, MaxDelayToFireEvent, OnTagValueChangeAction);
      }
      CreateSubscription();
    }
    public void UnsubscribeTags()
    {
      if (TagSubscriptionDictionary.Count > 0)
      {
        List<ITag> Tag_List = new List<ITag>();
        foreach (var item in TagSubscriptionDictionary)
        {
          OPCTag T = item.Value as OPCTag;
          Tag_List.Add(T);
          T.RemoveFromSubscription();
        }
        RemoveSubscription(Tag_List);

        Logger?.LogDebug($"OPC - Tag Subscription - Removed all subscription Tags: {TagSubscriptionDictionary.Count()}.");
        TagSubscriptionDictionary.Clear();
      }
    }
    public void UnsubscribeTags(List<ITag> TagsToUnSubscribe)
    {
      if (TagsToUnSubscribe.Count > 0)
      {
        RemoveSubscription(TagsToUnSubscribe);

        Logger?.LogDebug($"OPC - Tag Subscription - Removed from subscription {TagsToUnSubscribe.Count()} Tags. Total tags in subscription: {TagSubscriptionDictionary.Count()}");

        foreach (var item in TagsToUnSubscribe)
        {
          OPCTag T = item.Value as OPCTag;
          TagSubscriptionDictionary.Remove(T.Name);
          T.RemoveFromSubscription();
        }
      }
    }
    public void UnsubscribeTags(List<string> TagsToUnSubscribeNames)
    {
      List<ITag> Tag_List = new List<ITag>();
      if (TagsToUnSubscribeNames.Count > 0)
      {
        foreach (var itemName in TagsToUnSubscribeNames)
        {
          OPCTag T = (OPCTag)this.GetTag(itemName);
          Tag_List.Add(T);
          T.RemoveFromSubscription();
        }

        //Remove tag from dictionary
        RemoveSubscription(Tag_List);
        Logger?.LogDebug($"OPC - Tag Subscription - Removed from subscription {TagsToUnSubscribeNames.Count()} Tags. Total tags in subscription: {TagSubscriptionDictionary.Count()}");
        foreach (var item in Tag_List)
        {
          TagSubscriptionDictionary.Remove(item.Name);
        }
      }
    }
    public void UnsubscribeTag(ITag TagToUnSubscribe)
    {
      if (TagSubscriptionDictionary.ContainsKey(TagToUnSubscribe.Name))
      {
        ////Remove tag from dictionary
        List<ITag> TagsToUnSubscribe = new List<ITag>() { TagToUnSubscribe };
        RemoveSubscription(TagsToUnSubscribe);
        OPCTag T = TagToUnSubscribe as OPCTag;
        T.RemoveFromSubscription();
        Logger?.LogDebug($"OPC - Tag Subscription - Removed from subscription Tag: {TagToUnSubscribe.Name}. Total tags in subscription: {TagSubscriptionDictionary.Count()}");
        TagSubscriptionDictionary.Remove(TagToUnSubscribe.Name);
      }
    }
    public void UnsubscribeTag(string TagToUnSubscribeName)
    {
      if (TagSubscriptionDictionary.ContainsKey(TagToUnSubscribeName))
      {
        //Remove tag from dictionary
        OPCTag T = (OPCTag)this.GetTag(TagToUnSubscribeName);
        List<ITag> TagsToUnSubscribe = new List<ITag>() { T };
        RemoveSubscription(TagsToUnSubscribe);

        T.RemoveFromSubscription();
        Logger?.LogDebug($"OPC - Tag Subscription - Removed from subscription Tag: {TagToUnSubscribeName}. Total tags in subscription: {TagSubscriptionDictionary.Count()}");
        TagSubscriptionDictionary.Remove(TagToUnSubscribeName);
      }
    }

    #region Private Methods
    private void CreateSubscription()
    {
      if (IsAvailable)
      {
        if (ClientSubscription == null)
        {
          // initialize subscription.
          ClientSubscription = new Subscription(ClientSession.DefaultSubscription) { PublishingInterval = 100 };
          //ClientSubscription.MaxNotificationsPerPublish = 0;
          //ClientSubscription.Priority = 0;
          //ClientSubscription.PublishingEnabled = true;
        }
        List<MonitoredItem> MonitoredItem_List = new List<MonitoredItem>();
        foreach (var item in TagSubscriptionDictionary)
        {
          OPCTag MyTag = item.Value as OPCTag;

          MonitoredItem MyMonitoredItem = new MonitoredItem(ClientSubscription.DefaultItem)
          {
            DisplayName = MyTag.Name,
            StartNodeId = MyTag.Address
          };
          MyMonitoredItem.Notification += MyTag.OnSubscriptionNotification;
          MonitoredItem_List.Add(MyMonitoredItem);
        }

        ClientSubscription.AddItems(MonitoredItem_List);

        if (!IsSubscriptionActive)
        {
          ClientSession.AddSubscription(ClientSubscription);
          ClientSubscription.Create();
          IsSubscriptionActive = true;
        }
        else
        {
          ClientSubscription.Modify();
        }
      }
      else
      {
        Logger?.LogWarning($"OPC - Tag Subscription - Client current state is not available. Subscription will be created on next connection.");
      }

    }
    private void RemoveSubscription(List<ITag> TagsToUnSubscribe)
    {
      try
      {
        List<MonitoredItem> MonitoredItem_List = new List<MonitoredItem>();
        foreach (var item in TagsToUnSubscribe)
        {
          OPCTag MyTag = item.Value as OPCTag;
          MonitoredItem MyMonitoredItem = new MonitoredItem(ClientSubscription.DefaultItem)
          {
            DisplayName = MyTag.Name,
            StartNodeId = MyTag.Address
          };
          MonitoredItem_List.Add(MyMonitoredItem);
        }

        ClientSubscription.RemoveItems(MonitoredItem_List);
      }
      catch (Exception ex)
      {

      }
    }
    #endregion
    #endregion

    #region Check Connection and Try to reconnect

    System.Timers.Timer CheckConnection_Timer = new System.Timers.Timer();
    object CheckConnection_Lock = new object();

    SynchronizationContext MainContext = SynchronizationContext.Current;
    private void Set_CheckConnection()
    {

      CheckConnection_Timer.Elapsed += OnCheckConnectionEvent;
      CheckConnection_Timer.Interval = 20000;
      CheckConnection_Timer.Enabled = true;
      CheckConnection_Timer.AutoReset = true;

    }

    private void OnCheckConnectionEvent(object source, ElapsedEventArgs e)
    {
      try
      {
        if (Monitor.TryEnter(CheckConnection_Lock))
        {
          if ((ClientSession == null) || (!IsAvailable))
          {
            Init();
          }
          else
          {
            IsConnected = ClientSession.Connected;
            Stop_CheckConnection();
            CreateSubscription();
          }
        }
      }
      catch (Exception ex)
      {
        Logger?.LogException($"OPC - Connection check error. Ex: {ex.Message}");
      }
    }

    private void Stop_CheckConnection()
    {
      try
      {
        CheckConnection_Timer.Stop();
        CheckConnection_Timer.Enabled = false;
        CheckConnection_Timer.AutoReset = false;
        CheckConnection_Timer.Elapsed -= OnCheckConnectionEvent;
      }
      catch (Exception ex)
      {
        Logger?.LogException($"OPC - Connection check stop error. Ex: {ex.Message}");
      }

    }

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
