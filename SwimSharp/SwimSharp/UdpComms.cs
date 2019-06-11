using Akka.Actor;
using Akka.IO;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SwimSharp
{
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

        internal static Props Props(IPEndPoint bindAddress, ILogger logger)
        {
            return Akka.Actor.Props.Create(() => new UdpCommsActor(bindAddress, logger));
        }

        internal UdpCommsActor(IPEndPoint bindAddress, ILogger logger)
        {
            _logger = logger;
            _binddAddress = bindAddress;
            Receive();
        }

        protected override void PreStart()
        {
            Context.System.Udp().Tell(new Udp.Bind(Self, _binddAddress));
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
    }
}
