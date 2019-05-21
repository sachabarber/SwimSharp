using Autofac;
using Nancy.Bootstrappers.Autofac;
using Nancy.Configuration;

namespace NancySelfHost.IOC
{

    public class Bootstrapper : AutofacNancyBootstrapper
    {
        private ILifetimeScope _scope;


        public Bootstrapper(ILifetimeScope scope)
        {
            _scope = scope;
        }


        protected override ILifetimeScope GetApplicationContainer()
        {
            return _scope;
        }



    }
}
