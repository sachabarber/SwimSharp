using Akka.Actor;
using Akka.Configuration;
using Akka.DI.Core;
using Autofac;
using Serilog;
using SwimSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SwimSharp
{
    class Program
    {


        static void Main(string[] args)
        {

            var items = new List<int> { 1, 2, 3, 4 };
            items.Shuffle();
            foreach (var item in items)
            {
                Console.WriteLine(item);
            }

            items.Shuffle();
            foreach (var item in items)
            {
                Console.WriteLine(item);
            }




            Console.ReadLine();
        }




        public static void ActorMessabout()
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
