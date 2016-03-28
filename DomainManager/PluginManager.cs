using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace DomainManager
{
    //at the moment this class is waste
    public class PluginManager<T> where T : class
    {
        public ObservableCollection<PluginDomain<T>> _PluginDomainList = new ObservableCollection<PluginDomain<T>>();

        public PluginManager()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        public void AddPlugin(String pAssembly)
        {
            // Neue Domain erstellen
            PluginDomain<T> pluginDomain = new PluginDomain<T>(pAssembly, typeof(T));
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppDomain appDomain = sender as AppDomain;
            Exception exception = e.ExceptionObject as Exception;

            //Schauen wir in welcher Assembly / Plugin die Exception aufgetreten ist
            String assembly = exception.TargetSite.Module.Assembly.Location;

            //Schauen wir ob wir das Plugin finden können
            var result = _PluginDomainList.ToList().Find(delegate(PluginDomain<T> plugin)
            {
                return plugin.PluginAssembly == assembly;
            });

            //Wenn ja löschen wir das Plugin
            //if (result != null)
            //{
            //    AppDomain.Unload(result.AppDomain);
            //    _PluginDomainList.Remove(result);

            //}
            //else
            //{

            //}
            Console.WriteLine("1");

        }

    }
    public sealed class PluginDomainConnector
    {
        //public PluginDomainConnector(PluginDomain<T> pPluginDomain)
        //{
        //    PluginDomain = pPluginDomain;
        //}
        //public PluginDomain<T> PluginDomain { get; private set; }
    }
}
