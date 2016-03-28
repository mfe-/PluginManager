using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plugin
{
    [System.Serializable()]
    public class Plugin : System.MarshalByRefObject, IPlugin
    {
        public string Execute()
        {
            //Microsoft.SqlServer.Server.SqlContext sqlcontext;
            throw new NotImplementedException();
            return "Martin";
        }

    }



}
