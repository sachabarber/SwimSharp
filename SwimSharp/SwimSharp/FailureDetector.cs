using Akka.Actor;
using Akka.IO;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace SwimSharp
{ 

    internal class ProbeMembers
    {
        internal ProbeMembers(List<Member> members)
        {
            this.Members = members;
        }

        internal List<Member> Members { get; private set; }
    }

    internal class ProbeTimedOut
    {
        internal ProbeTimedOut(Member member)
        {
            this.Member = member;
        }

        internal Member Member { get; private set; }
    }


    internal class FailureDetectorActor : ReceiveActor
    {
        private ILogger _logger;
        private IActorRef _udp;
        private Config _config;

        internal static Props Props(ILogger logger, IActorRef udp, Config config)
        {
            return Akka.Actor.Props.Create(() => new FailureDetectorActor(logger, udp, config));
        }

        internal FailureDetectorActor(ILogger logger, IActorRef udp, Config config)
        {
            _logger = logger;
            _udp = udp;
            _config = config;

            Receive();
        }

        protected override void PreStart()
        {
            _udp.Tell(new RegisterReceiver(Self));
        }

        private void Receive()
        {
            Receive<ProbeMembers>(x =>
            {
                ProbeMembers(x.Members);
            });

            Receive<Ping>(x =>
            {
                Sender.Tell(new Ack(x.SeqNo));
            });

            Receive<IndirectPing>(x =>
            {
                ForwardPing(x.SeqNo, x.Target);
            });
        }

 
        private void ProbeMembers(List<Member> members)
        {
            List<Member> ForwardersFor(Member target)
            {
                return Util.TakeRandom(members.Where(x => x.Name != target.Name).ToList(), _config.IndirectProbeCount);
            }

            var probedMembers = Util.TakeRandom(members, _config.ProbedMemberCount);
            probedMembers.ForEach(target => StartDirectPinger(Sender, target, ForwardersFor(target), _udp));
        }

        private IActorRef ForwardPing(long seqNo, Member target)
        {
            return Context.ActorOf(ForwardPingerActor.Props(Sender, target, seqNo, _udp, _config), $"ForwardPingerActor_{Guid.NewGuid().ToString("N"}");
        }

        private IActorRef StartDirectPinger(IActorRef sender, Member target , List<Member>  forwarders, IActorRef udp)
        {
            return Context.ActorOf(DirectPingerActor.Props(Sender, target, forwarders, _udp, _config), $"DirectPingerActor_{Guid.NewGuid().ToString("N"}");
        }
    }
}
