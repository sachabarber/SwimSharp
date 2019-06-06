using Akka.Actor;
using Akka.IO;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SwimSharp
{




    //class UdpComms(bindAddress: InetSocketAddress) extends Actor with ActorLogging
    //    {
    //  import context.system
    //  import context.dispatcher
    //  implicit val timeout = Timeout(5.seconds)
    //  var receivers = Set [ActorRef]()

    //  override def preStart() = IO(Udp) ! Udp.Bind(self, bindAddress)

    //  def receive = handleRegistrations orElse
    //        {
    //    case Udp.Bound(_) => context.become(ready(sender))
    //  }

    //        def ready(ioActor: ActorRef): Receive = handleRegistrations orElse
    //        {
    //    case SendMessage(member, msg) => send(ioActor, member, msg)
    //          case Udp.Received(data, from) => receivers.foreach (receiver => (receiver ? UdpMessage(data)).mapTo[UdpMessage].foreach (reply => send(ioActor, from, reply)))
    //  }

    //        def handleRegistrations: Receive = {
    //    case RegisterReceiver(receiver) => {
    //                receivers += receiver
    //      context.watch(receiver)  // Get notified of death -> unregister on notification
    //    }
    //    case Terminated(victim) => receivers -= victim
    //        }

    //        def send(ioActor: ActorRef, to: Member, message: UdpMessage): Unit = send(ioActor, addressFor(to), message)
    //  def send(ioActor: ActorRef, to: InetSocketAddress, message: UdpMessage) = ioActor ! Udp.Send(message.toByteString, to)
    //  def addressFor(member: Member) = new InetSocketAddress(member.ip, member.port)
    //}



    internal class SendMessage
    {
        internal SendMessage(Member to, UdpMessage message)
        {
            this.To = to;
            this.Message = message;
        }

        internal Member To { get; private set; }
        internal UdpMessage Message { get; private set; }
    }

    internal class RegisterReceiver
    {
        internal RegisterReceiver(IActorRef registree)
        {
            this.Registree = registree;
        }

        internal IActorRef Registree { get; private set; }
    }



    internal class UdpCommsActor : ReceiveActor
    {
        private ILogger _logger;
        private IPEndPoint _binddAddress;
        private IActorRef _ioActor;
        private HashSet<IActorRef> receivers = new HashSet<IActorRef>();

        public UdpCommsActor(IPEndPoint bindAddress, ILogger logger)
        {
            _logger = logger;
            _binddAddress = bindAddress;

            //Receive<string>(message => {
            //    logger.Information($"Got message {message}");
            //});

            Receive();
        }

        private void Receive()
        {
            Receive<Udp.Bound>(x =>
            {
                _ioActor = Sender;
                Become(Ready);
            });

            Receive<RegisterReceiver>(x =>
            {
                receivers.Add(x.Registree);

                // Get notified of death -> unregister on notification
                Context.Watch(x.Registree);
            });

            Receive<Terminated>(x =>
            {
                receivers.Remove(x.ActorRef);
            });
        }

        private void Ready()
        {
            Receive<SendMessage>(x =>
            {
                Send(_ioActor, x.To, x.Message);
            });

            ReceiveAsync<Udp.Received>(async x =>
            {
                foreach (var receiver in receivers)
                {
                    var reply = (UdpMessage)await receiver.Ask(UdpMessage.Apply(x.Data), TimeSpan.FromSeconds(5));
                    Send(_ioActor, x.Sender, reply);
                }
            });

            Receive<RegisterReceiver>(x =>
            {
                receivers.Add(x.Registree);

                // Get notified of death -> unregister on notification
                Context.Watch(x.Registree);
            });

            Receive<Terminated>(x =>
            {
                receivers.Remove(x.ActorRef);
            });
        }

 
        private Send(IActorRef theIoActor, Member to, UdpMessage message)
        {
            Send(theIoActor, AddressFor(member), message);
        }

        private Send(IActorRef theIoActor, IPEndPoint to, UdpMessage message)
        {
            theIoActor.Tell(Udp.Send.Create(message.ToByteString(), to));
        }

        private IPEndPoint AddressFor(Member member)
        {
            return new IPEndPoint(member.Ip, member.Port);
        }


        protected override void PreStart()
        {
            base.PreStart();
            Context.System.Udp().Tell(new Udp.Bind(Self, _binddAddress));

        }

        public static Props Props(IPEndPoint bindAddress, ILogger logger)
        {
            return Akka.Actor.Props.Create(() => new MyActor(logger));
        }
    }
}
