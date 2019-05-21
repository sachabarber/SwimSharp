using Akka.Actor;
using Autofac;
using Serilog;
using System;

namespace NancySelfHost.IOC
{
    public static class IoC
    {
        public static IContainer LetThereBeIoC()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<SmtpService>().As<ISmtpService>();



            var logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Information()
                .CreateLogger();

            Serilog.Log.Logger = logger;
            builder.RegisterSelf();
            builder.RegisterInstance<ILogger>(Serilog.Log.Logger);
            builder.RegisterType<MyActor>();


            builder.Register<ActorSystem>(c =>
            {
                var system = ActorSystem.Create("my-test-system", "akka { loglevel=INFO,  loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]}");
                system.UseAutofac(c.Resolve<IContainer>());
                return system;

            });



            //var assemblies = new[]
            //{
            //    typeof (IoC).Assembly
            //};

            //builder.RegisterAssemblyModules(assemblies);

            return builder.Build();
        }


        public static void RegisterSelf(this ContainerBuilder builder)
        {
            IContainer container = null;
            builder.Register(c => container).AsSelf();
            builder.RegisterBuildCallback(c => container = c);
        }
    }
}
