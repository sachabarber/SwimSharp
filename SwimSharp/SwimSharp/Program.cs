using Akka.Actor;
using Akka.Configuration;
using Akka.DI.Core;
using Autofac;
using Serilog;
using System;

namespace SwimSharp
{
    class Program
    {


        static void Main(string[] args)
        {
            var logger = new LoggerConfiguration()
                 .WriteTo.Console()
                 .MinimumLevel.Information()
                 .CreateLogger();

            Serilog.Log.Logger = logger;

            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance<ILogger>(Serilog.Log.Logger);
            builder.RegisterType<MyActor>();
            IContainer container = builder.Build(); 


            var system = ActorSystem.Create("my-test-system", "akka { loglevel=INFO,  loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]}");
            system.UseAutofac(container);


            //var actor = system.ActorOf(MyActor.Props(), $"demo-{Guid.NewGuid().ToString("N")}");
            var actor = system.ActorOf(system.DI().Props<MyActor>(), $"demo-{Guid.NewGuid().ToString("N")}");

            actor.Tell("dddsdsadsada");


            Console.ReadLine();
        }
    }

    public class MyActor : ReceiveActor
    {

        public MyActor(ILogger logger)
        {
            Receive<string>(message => {
                logger.Information($"Got message {message}");
            });
        }

        public static Props Props(ILogger logger)
        {
            return Akka.Actor.Props.Create(() => new MyActor(logger));
        }
    }

}
