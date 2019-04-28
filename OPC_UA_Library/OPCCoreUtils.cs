using System;
using System.Linq;

using Opc.Ua;

namespace OPC_UA_Library
{

  // TODO move to internal
  public static class OPCCoreUtils
  {

    public static EndpointDescription SelectEndpointBySecurity(string discoveryUrl, SecurityPolicy securityPolicy, MessageSecurity messageSecurity, int operationTimeout = -1)
    {
      Uri uri = new Uri(discoveryUrl);
      

      EndpointConfiguration configuration = EndpointConfiguration.Create();
      if (operationTimeout > 0)
      {
        configuration.OperationTimeout = operationTimeout;
      }

      EndpointDescription selectedEndpoint = null;
      MessageSecurityMode actualMessageSecurity;

      switch(messageSecurity)
      {
        case MessageSecurity.SignAndEncrypt:
          actualMessageSecurity = MessageSecurityMode.SignAndEncrypt;
          break;
        case MessageSecurity.Sign:
          actualMessageSecurity = MessageSecurityMode.Sign;
          break;
        case MessageSecurity.None:
        default:
          actualMessageSecurity = MessageSecurityMode.None;
          break;
      }

      // Connect to the server's discovery endpoint and find the available configuration.
      using (DiscoveryClient client = DiscoveryClient.Create(uri, configuration))
      {
        EndpointDescriptionCollection endpoints = client.GetEndpoints(null);

        selectedEndpoint = endpoints.FirstOrDefault(ep => ep.SecurityMode == actualMessageSecurity && ep.SecurityPolicyUri.Contains(securityPolicy.ToString()));
      }

      return selectedEndpoint;
    }

  }

}