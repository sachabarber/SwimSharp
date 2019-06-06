using Nancy.Hosting.Self;
using NancySelfHost.IOC;
using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.DI.Core;
using Autofac;
using Serilog;
using System;
using SwimSharp.Extensions;
using System.Linq;

namespace NancySelfHost
{
    class Program
    {
        static void Main(string[] args)
        {

            IContainer container = IoC.LetThereBeIoC();
            var bootstrapper = new Bootstrapper(container);


            var system = container.Resolve<ActorSystem>();


            //var system = ActorSystem.Create("my-test-system", "akka { loglevel=INFO,  loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]}");
            //system.UseAutofac(container);


            //var actor = system.ActorOf(MyActor.Props(), $"demo-{Guid.NewGuid().ToString("N")}");
            var actor = system.ActorOf(system.DI().Props<MyActor>(), $"demo-{Guid.NewGuid().ToString("N")}");

            actor.Tell("dddsdsadsada");



            using (var host = new NancyHost(bootstrapper,new Uri("http://localhost:1234")))
            {
                host.Start();
                Console.WriteLine("Running on http://localhost:1234");
                Console.ReadLine();
            }
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
