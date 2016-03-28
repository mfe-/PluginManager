using System;
using System.Collections.Generic;
using System.Linq;
using Plugin;
using DomainManager;
using System.Threading;

namespace ConsoleApplication1
{
    public class Program
    {
        public static List<PluginDomain<IPlugin>> _PluginDomainList = new List<PluginDomain<IPlugin>>();
        public static Dictionary<PluginDomain<IPlugin>,Thread> _PluginDomainThreadList = new Dictionary<PluginDomain<IPlugin>,Thread>();

        public static void Main(string[] Args)
        {
            System.Console.WriteLine("Start");
            Print(System.AppDomain.CurrentDomain.GetAssemblies());

            //Unbehandelte Exceptions abfangen
            //catch unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(appdomain_UnhandledException);

            //Neue Domain erstellen
            //Create new Domain
            PluginDomain<IPlugin> pluginDomain = new PluginDomain<IPlugin>(System.Environment.CurrentDirectory + @"\Plugin.dll", typeof(IPlugin));

            _PluginDomainList.Add(pluginDomain);

            System.Console.WriteLine("PluginDomain erstellt");

            //Execute a method of the plugin in the created plugins appdomain
            pluginDomain.ExecuteFunctionOfPlugin(pluginDomain.Instance.GetFunctionName(x => x.Execute()),null);

            //Execute a method of the plugin in the created plugins appdomain, but we commit the function as delegate and not as string
            //pluginDomain.ExecuteFunctionOfPlugin(new Action(()=>pluginDomain.Instance.Execute()),null);

            pluginDomain.Instance.Execute();

            Print(System.AppDomain.CurrentDomain.GetAssemblies());

            ParameterizedThreadStart parameterizedThreadStart = new ParameterizedThreadStart(RunPlugin);

            Thread thread = new Thread(parameterizedThreadStart);

            //Den thread mit dem plugin registrieren
            _PluginDomainThreadList.Add(pluginDomain, thread);

            //the above code is only for testing 
            //i try to execute the plugins in a seperate thread because if <legacyUnhandledExceptionPolicy enabled="true" /> in the app.conf is set
            //an thread which crashes will have no effect to our programm
            //for more information check this blog entry http://ikickandibite.blogspot.com/2010/04/appdomains-and-true-isolation.html
            try
            {
                thread.Start(pluginDomain);
                thread.Join();
            }
            catch(Exception e)
            {
                //bringt nichts
                //thread.Abort();
                System.Console.WriteLine(e.Message);
            }
            


            System.Console.WriteLine("Programm wurde fertig ausgeführt");
            Console.ReadLine();
            
        }

        public static void RunPlugin(object pPluginDomain)
        {
            PluginDomain<IPlugin> pluginDomain = pPluginDomain as PluginDomain<IPlugin>;

            System.Console.WriteLine("Execute Plugin Method");
            System.Console.WriteLine(pluginDomain.Instance.Execute());

            //Unser Plugin entladen
            AppDomain.Unload(pluginDomain.AppDomain);

            System.Console.WriteLine("Appdomain unloaded");

            try
            {
                System.Console.WriteLine("Plugin Method executed");
                pluginDomain.Instance.Execute();
            }
            catch (Exception e)
            {
                //"Die Zielanwendung wurde entladen"
                System.Console.WriteLine(e.Message);
            }


            Print(System.AppDomain.CurrentDomain.GetAssemblies());

        }
        
        private static void appdomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppDomain appDomain = sender as AppDomain;
            Exception exception = e.ExceptionObject as Exception;

            //Schauen wir in welcher Assembly / Plugin die Exception aufgetreten ist
            //check out in which assembly / plugin the exception occours
            String assembly = exception.TargetSite.Module.Assembly.Location;

            //Schauen wir ob wir das Plugin finden können
            //check if we can find the plugin
            var result = _PluginDomainList.Find(delegate(PluginDomain<IPlugin> plugin)
            {
                return plugin.PluginAssembly == assembly;
            });

            //Wenn ja löschen wir das Plugin
            //unload and delete plugin
            if (result != null)
            {


            }
            else
            {
                //Thread abbrechen damit wir das plugin löschen können
                //_PluginDomainThreadList[result].Abort(); würde wiederum eine AbortExceptin auslösen -->rekursion

                AppDomain.Unload(result.AppDomain);

                _PluginDomainList.Remove(result);
            }
            Console.WriteLine("1");





        }


        static void Print(System.Reflection.Assembly[] Assemblies)
        {
            int i = 0;
            foreach (System.Reflection.Assembly assem in Assemblies)
                System.Console.WriteLine("[{0}] {1}", ++i, assem.GetName().Name);
        }

    }
}