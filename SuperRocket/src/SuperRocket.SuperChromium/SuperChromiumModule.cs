using System;
using Microsoft.Practices.Unity;
using Prism.Modularity;
using SuperRocket.SuperChromium.Services;
using Autofac;

namespace SuperRocket.SuperChromium
{
    public class SuperChromiumModule : IModule
    {
        private readonly IContainer _container;
        
        public SuperChromiumModule(IContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException($"{nameof(container)}");
            }

            _container = container;
        }

        public void Initialize()
        {
            //BrowserManager manager = new BrowserManager();
            //_container.RegisterInstance(typeof(IBrowserManager), manager);
            //_container.RegisterType<IBrowserManager, BrowserManager>(new ContainerControlledLifetimeManager());
            var cb = new ContainerBuilder();
            cb.RegisterType<BrowserManager>().As<IBrowserManager>();
            cb.Update(_container);
        }
    }
}