// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using System.Collections.Generic;
using System.Composition;
using System.Composition.Hosting;
using System.Diagnostics;
using System.Reflection;

namespace SmartDisplay
{
    /// <summary>
    /// This class uses MEF to import classes from a known set of assemblies.
    /// (Note that UWP apps can't load assemblies from outside the app package.)
    /// 
    /// To define external interface implementations:
    /// 1. Add a reference to the NuGet package System.Composition in your class library.
    /// 2. Add [System.Composition.Export(IMyInterface)] attributes to classes you want to export.
    /// 
    /// To import external classes in AppComposer:
    /// 1. Add a project reference to your class library.
    /// 2. Add your assembly to the KnownSatelliteAssemblies list.
    /// 3. Add a public property with the [ImportMany] attribute in this class.
    /// 4. Call AppComposer.Initialize() during app startup.
    /// 5. Use AppComposer.Imports.* to get the imported classes.
    /// </summary>
    internal class AppComposer
    {
        [ImportMany]
        public IEnumerable<IFeature> Features { get; private set; } = new IFeature[0];

        [ImportMany]
        public IEnumerable<ITelemetryService> TelemetryServices { get; private set; } = new ITelemetryService[0];

        public static List<string> KnownSatelliteAssemblies { get; } = new List<string>
        {
            // The current .exe will always be searched in addition to these assemblies
            "SmartDisplay.SelfHost",
            "SmartDisplay.Features",
        };

        private static AppComposer _instance;
        public static AppComposer Imports
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppComposer();
                }
                return _instance;
            }
        }

        public static void Initialize()
        {
            SatisfyImports(Imports);
        }

        public static void SatisfyImports(object objectWithLooseImports)
        {
            CompositionHost.SatisfyImports(objectWithLooseImports);
        }

        private static CompositionHost _compositionHost;
        private static CompositionHost CompositionHost
        {
            get
            {
                if (_compositionHost == null)
                {
                    ContainerConfiguration configuration = new ContainerConfiguration().WithAssembly(typeof(AppComposer).GetTypeInfo().Assembly);
                    foreach (string assemblyName in KnownSatelliteAssemblies)
                    {
                        try
                        {
                            Assembly assembly = Assembly.Load(new AssemblyName(assemblyName));
                            configuration = configuration.WithAssembly(assembly);
                        }
                        catch
                        {
                            Debug.WriteLine($"Couldn't load assembly '{assemblyName}'");
                        }
                    }

                    _compositionHost = configuration.CreateContainer();
                }
                return _compositionHost;
            }
        }
    }
}
