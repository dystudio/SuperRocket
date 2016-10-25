using System;
using Microsoft.Practices.Unity;
using Prism.Modularity;


namespace SuperRocket.Framework
{
    public class FrameworkModule : IModule
    {
        private readonly IUnityContainer _container;

        public FrameworkModule(IUnityContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException($"{nameof(container)}");
            }

            _container = container;
        }

        public void Initialize()
        {
            //_container.RegisterType<ICustomerService, CustomerService>(new ContainerControlledLifetimeManager());
        }
    }
}