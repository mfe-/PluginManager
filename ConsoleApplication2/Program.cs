using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Runtime.Remoting;

namespace ConsoleApplication2
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get a reference to the AppDomain that the calling thread is executing in
            // Ist das gleiche wie AppDomain.CurrentDomain
            AppDomain currentDomain = Thread.GetDomain();

            // Every AppDomain is assigned a friendly string name, which is helpful

            // for debugging. Get this AppDomain's friendly name and display it

            String callingDomainName = currentDomain.FriendlyName;
            Console.WriteLine("Default AppDomain's friendly name={0}", callingDomainName);

            // Get & display the assembly in our AppDomain that contains the 'Main' method

            String exeAssembly = Assembly.GetEntryAssembly().FullName;

            Console.WriteLine("Main assembly={0}", exeAssembly);

            // Define a local variable that can refer to an AppDomain

            AppDomain ad2 = null;
            // DEMO 1: Cross-AppDomain communication using Marshal-by-Reference

            Console.WriteLine("{0}Demo #1: Marshal-by-Reference", Environment.NewLine);

            // Create a new AppDomain (security & configuration match current AppDomain)

            ad2 = AppDomain.CreateDomain("AD #2", null, null);

            // Load our assembly into the new AppDomain, construct an object, marshal

            //it back to our AD (we really get a reference to a proxy)

            MarshalByRefObject mbrt = (MarshalByRefObject)
            ad2.CreateInstanceAndUnwrap(exeAssembly, "MarshalByRefObject");
            Type t = mbrt.GetType();
            // Prove that we got a reference to a proxy object

            Console.WriteLine("Is proxy={0}", RemotingServices.IsTransparentProxy(mbrt));

            // This looks as if we're calling a method on a MarshalByRefType instance, but

            // we're not. We're calling a method on an instance of a proxy type.

            // The proxy transitions the thread to the AppDomain owning the object and

            // calls this method on the real object

            //mbrt.SomeMethod(callingDomainName);
            // Unload the new AppDomain

            AppDomain.Unload(ad2);

            // mbrt refers to a valid proxy object;

            // this proxy refers to an invalid AppDomain now

            try
            {
                // We're calling a method on the proxy type object.

                // The AD is invalid, an exception is thrown

                //mbrt.SomeMethod(callingDomainName);
                Console.WriteLine("Successful call.");

            }
            catch (AppDomainUnloadedException)
            {
                Console.WriteLine("Failed call.");

            }

        }
    }
}
