using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Reflection;
using System.Linq.Expressions;

namespace DomainManager
{
    [Serializable]
    public class PluginDomain<T> where T : class
    {
        /// <summary>
        /// Key to set / get the Data in AppDomain 
        /// </summary>
        private const String _GetDataPluginTypes = "pluginType";
        /// <summary>
        /// Created AppDomain
        /// </summary>
        private System.AppDomain _appDom;
        /// <summary>
        /// Instance from the loaded plugin
        /// </summary>
        private T _Instance;
        /// <summary>
        /// Type which implements the commited Interface
        /// </summary>
        private String _PluginType = String.Empty;
        /// <summary>
        /// path to the plugin
        /// </summary>
        private String _AssemblyPath = String.Empty;

        /// <summary>
        /// Erstellt eine neue Domain für die übergebene Assembly.
        /// Created a new AppDomain for the commited assembly
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly</param>
        /// <param name="InterfaceOfPlugin">Typ from the interface which the plugin implements</param>
        public PluginDomain(string assemblyPath, System.Type InterfaceOfPlugin)
        {
            _AssemblyPath = assemblyPath;
            //Informationen über die zu ladende Assembly holen
            //Get some informations from the loaded assembly
            System.IO.FileInfo fileinfo = new System.IO.FileInfo(assemblyPath);

            System.AppDomainSetup appDomainSetup = new System.AppDomainSetup();
            appDomainSetup.ApplicationName = fileinfo.Name;
            appDomainSetup.ApplicationBase = Environment.CurrentDirectory;

            appDomainSetup.ConfigurationFile = fileinfo.Name + ".config";

            //appDomainSetup.PrivateBinPath = fileinfo.Directory.FullName;
            //appDomainSetup.PrivateBinPathProbe = fileinfo.Directory.FullName;
            appDomainSetup.DisallowBindingRedirects = true;
            appDomainSetup.DisallowCodeDownload = true;
            appDomainSetup.ShadowCopyFiles = Boolean.TrueString;

            //Beim erstellen der AppDomain wird die Funktion GetInterfaceTypes aufgerufen
            //By creating the AppDomain the Method GetInterfaceTypes will be called
            appDomainSetup.AppDomainInitializer = new AppDomainInitializer(GetInterfaceTypes);
            //Parameter der Funktion übergeben, Pfad des Plugins und nach welchem Interface gesucht werden soll
            //Set the parameter for the calling method GetInterfaceTypes
            appDomainSetup.AppDomainInitializerArguments = new string[] { assemblyPath, InterfaceOfPlugin.ToString() };

            //Definiert den Satz von Informationen, der als Eingabe für Entscheidungen über Sicherheitsrichtlinien verwendet wird. Diese Klasse kann nicht vererbt werden.
            //http://msdn.microsoft.com/de-de/library/system.security.policy.evidence(VS.90).aspx
            //Defines the set of information that constitutes input to security policy decisions. This class cannot be inherited.
            //http://msdn.microsoft.com/en-us/library/system.security.policy.evidence(VS.90).aspx
            Evidence adevidence = System.AppDomain.CurrentDomain.Evidence;

            _appDom = System.AppDomain.CreateDomain(fileinfo.Name, adevidence, appDomainSetup);

            //GetInterfaceTypes Methode wurde ausgeführt. Die Daten welche gesetzt wurden - abholen
            //The GetInterfaceTypes method was executed now we can catch our data
            _PluginType = _appDom.GetData(_GetDataPluginTypes) as String;

            this.Instance = InstantiatePlugin();
        }
        /// <summary>
        /// Erstellt eine neue Instanz eines angegebenen Typs, der in der angegebenen Assemblydatei definiert ist. 
        /// http://msdn.microsoft.com/de-de/library/y7h7t2a2(VS.90).aspx
        /// Creates a new instance from the given Typ which is defined in the plugin assembly
        /// </summary>
        private T InstantiatePlugin()
        {
            _Instance = this._appDom.CreateInstanceFromAndUnwrap(_AssemblyPath, _PluginType) as T;
            return _Instance;
        }
        /// <summary>
        /// Diese Methode sucht in der übgergebenen Datei alle Klassen die das übergebene Interface implementieren.
        /// This method searches in the commmited file all types which implements the plugins interface
        /// </summary>
        /// <param name="args">Der erste Parameter ist der Datepfad der Datei die nach dem gesuchten Interface durchsucht werden soll.
        /// Im zweiten Parameter wird das Interface übergeben.
        /// The first parameter is the path to the assembly in which we look for up the interface</param>
        private static void GetInterfaceTypes(string[] args)
        {
            AppDomain appDomain = System.AppDomain.CurrentDomain;

            //Laden wir die Plugin Assembly in unsere AppDomain
            //load the plugin assembly in our created appdomain
            System.Reflection.Assembly assembly = System.Reflection.Assembly.LoadFrom(args[0]);

            String pluginType = String.Empty;

            foreach (Type type in assembly.GetTypes())
            {
                //Check only Public Types
                if (type.IsPublic)
                {
                    //Check only not abstract Types
                    if (!type.IsAbstract)
                    {
                        //look up the interface
                        Type typeInterface = type.GetInterface(args[1], true);
                        if (typeInterface != null)
                        {
                            //return the type which implements our interface
                            pluginType = type.FullName;
                        }
                    }
                }
            }
            //Daten über die Methode SetData etzen, weil der AppDomainInitializer Delegate kein Rückgabewert hat. Mit GetData können die Daten
            //wieder abgeholt werden
            //Set the data with the method SetData because the AppDomainInitializer Delegate got no return value. With GetData we can get the setted data
            //http://msdn.microsoft.com/en-us/library/system.appdomaininitializer.aspx
            appDomain.SetData(_GetDataPluginTypes, pluginType);

        }

        public void ExecuteFunctionOfPlugin(Delegate pFunctionToExecute, object[] pParameter)
        {
            if (Instance == null) return;
            //Hier sieht man dass die Funktion in der AppDomain ausgeführt wird in der Sie aufgerufen wurde
            AppDomain appDomain = System.AppDomain.CurrentDomain;

            this.AppDomain.SetData("pFunctionToExecute", pFunctionToExecute);
            this.AppDomain.SetData("pParameter", pParameter);

            try
            {
                //will not work because the delegate isnt serialized
                this.AppDomain.DoCallBack(new CrossAppDomainDelegate(this.ExecuteFunctionOfPlugin));
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e);
            }
        }
        public void ExecuteFunctionOfPlugin(String pFunctionToExecute, object[] pParameter)
        {
            if (Instance == null) return;
            //Hier sieht man dass die Funktion in der AppDomain ausgeführt wird in der Sie aufgerufen wurde
            //the CurrentDomain will be not our created appdomain
            AppDomain appDomain = System.AppDomain.CurrentDomain;

            //this.AppDomain.SetData("pFunctionToExecute", pFunctionToExecute);
            this.AppDomain.SetData("pFunctionToExecute", pFunctionToExecute);
            this.AppDomain.SetData("pParameter", pParameter);

            try
            {
                this.AppDomain.DoCallBack(new CrossAppDomainDelegate(this.ExecuteFunctionOfPlugin));
            }
            catch (Exception e)
            {
                System.Diagnostics.Trace.WriteLine(e);
            }
        }

        private void ExecuteFunctionOfPlugin()
        {
            string pFunctionToExecute = this.AppDomain.GetData("pFunctionToExecute") as String;
            object[] pParameter = this.AppDomain.GetData("pParameter") as object[];

            //Hier sieht man dass die Funktion in der Plugin AppDomain ausgeführt wird
            //Now because we did a callback the CurrentDomain must be the created appdomain
            AppDomain appDomain = System.AppDomain.CurrentDomain;

            //Zuerst prüfen wir ob die Methode überhaupt in unserer Instanz vorhanden ist
            //Check if the function to execute exists in our instance
            if (Instance.GetType().GetMethods().ToList<MethodInfo>().Where(a => a.Name.Equals(pFunctionToExecute)).Count().Equals(0))
            {
                throw new ArgumentException(pFunctionToExecute + "doesnt exists in ");
            }
            //Invoke and execute method
            Instance.GetType().GetMethods().ToList<MethodInfo>().Where(a => a.Name.Equals(pFunctionToExecute)).First().Invoke(Instance, pParameter);

        }

        public String PluginAssembly { get { return _AssemblyPath; } }

        public System.AppDomain AppDomain
        {
            get
            {
                return this._appDom;
            }
            protected set
            {
                this._appDom = value;
            }
        }

        public String PluginType
        {
            get
            {
                return _PluginType;
            }
        }

        public virtual Assembly[] LoadedAssemblies
        {
            get
            {
                return _appDom.GetAssemblies();
            }
        }

        public String PluginName
        {
            get
            {
                return _appDom.FriendlyName;
            }
        }

        public T Instance
        {
            get
            {
                return this._Instance;
            }
            protected set
            {
                this._Instance = value;
            }
        }
    }
    public static class Extensions
    {
        public static string GetMemberName<TEntity, TProperty>(this TEntity instance, Expression<Func<TEntity, TProperty>> projection)
        {
            return ((MemberExpression)projection.Body).Member.Name;
        }

        public static string GetFunctionName<TEntity, TMethod>(this TEntity instance, Expression<Func<TEntity, TMethod>> projection)
        {
            //MethodBase.GetCurrentMethod().Name; 
            //http://www.mycsharp.de/wbb2/thread.php?threadid=17222
            //this is crap if the function got some parameters you will get the false functionname
            return projection.Body.ToString().Remove(0, 2).Replace("()", String.Empty); ;
        }
    }
}
