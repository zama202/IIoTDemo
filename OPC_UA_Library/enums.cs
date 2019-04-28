using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPC_UA_Library
{

  public enum SecurityPolicy
  {
    None = 0,
    Basic128,
    Basic256
  }

  public enum MessageSecurity
  {
    None,
    Sign,
    SignAndEncrypt
  }

}
