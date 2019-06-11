using Akka.Actor;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace SwimSharp
{
    internal static class ProbeSeqNo
    {
        private static long _seqNo = 0;
        internal static long Next => Interlocked.Increment(ref _seqNo);
    }

    internal class Start
    {

    }

    internal class AckTimedOut
    {

    }


    internal abstract class PingerActor : ReceiveActor
    {
        private ILogger _logger;
        private ICancelable _task;


        public PingerActor()
        {
            SeqNo = ProbeSeqNo.Next;
        }

        internal PingerActor(ILogger logger, IActorRef udp, Config config)
        {
            _logger = logger;
            Udp = udp;
            Config = config;

            Receive();
        }

        protected override void PreStart()
        {
            Self.Tell(new Start());
        }

        public long SeqNo { get; private set; }
        public IActorRef Udp { get; private set; }
        public Config Config { get; private set; }


        internal abstract void SendPings();

        internal virtual void AckReceived()
        {

        }

        internal virtual void AckTimedOut()
        {

        }

        private void Receive()
        {
            Receive<Start>(x =>
            {
                Udp.Tell(new RegisterReceiver(Self));
                _task = Util.ScheduleOnce(Config.AckTimeout, Self, new AckTimedOut(), Context.System.Scheduler);
                SendPings();
                Become(WaitingForAck);
            });
        }

        private void WaitingForAck()
        {
            Receive<Ack>(x =>
            {
                if(x.SeqNo == SeqNo)
                {
                    _task.Cancel();
                    AckReceived();
                    Context.Stop(Self);
                }
            });

            Receive<AckTimedOut>(x =>
            {
                AckTimedOut();
                Context.Stop(Self);
            });
        }
    }


    internal class DirectPingerActor : PingerActor
    {
        private IActorRef _receiver;
        private Member _target;
        private List<Member> possibleForwarders;

        internal static Props Props(ILogger logger, IActorRef receiver, Member target, List<Member> possibleForwarders, IActorRef udp, Config config)
        {
            return Akka.Actor.Props.Create(() => new DirectPingerActor(logger, receiver, target, possibleForwarders, udp, config));
        }

        internal DirectPingerActor(ILogger logger, IActorRef receiver, Member target, List<Member> possibleForwarders, IActorRef udp, Config config)
            : base(logger,udp,config)
        {
            _receiver = receiver;
            _target = target;
            _possibleForwarders = possibleForwarders;
        }

        internal override void SendPings()
        {
            Udp.Tell(new SendMessage(_target, new Ping(SeqNo)));
        }

        internal override void AckTimedOut()
        {
            return Context.ActorOf(IndirectPingerActor.Props(receiver, target, possibleForwarders, Udp, Config), $"IndirectPingerActor_{Guid.NewGuid().ToString("N"}");
        }
    }

//    class IndirectPinger(receiver: ActorRef, target: Member, forwarders: List[Member], val udp: ActorRef, val config: Config) extends Pinger
//    {
//        def sendPings = forwarders.foreach(fwd => udp ! SendMessage(fwd, IndirectPing(seqNo, target)))
//  override def ackTimedOut = receiver ! ProbeTimedOut(target)
//}


    internal class IndirectPingerActor : PingerActor
    {
        private IActorRef _receiver;
        private Member _target;
        private List<Member>  _forwarders;

        internal static Props Props(ILogger logger, IActorRef receiver, Member target, List<Member> forwarders, IActorRef udp, Config config)
        {
            return Akka.Actor.Props.Create(() => new IndirectPingerActor(logger, receiver, target, forwarders, udp, config));
        }

        internal IndirectPingerActor(ILogger logger, IActorRef receiver, Member target, List<Member> forwarders, IActorRef udp, Config config)
            : base(logger, udp, config)
        {
            _receiver = receiver;
            _target = target;
            _forwarders = forwarders;
        }

        internal override void SendPings()
        {
            _forwarders.ForEach(fwd =>
            {
                Udp.Tell(new SendMessage(fwd, new IndirectPing(SeqNo, _target)));
            });
        }

        internal override void AckTimedOut()
        {
            _receiver.Tell(new ProbeTimedOut(_target));
        }
    }


    internal class ForwardPingerActor : PingerActor
    {
        private IActorRef _receiver;
        private Member _target;
        private long _originalSeqNo;

        internal static Props Props(ILogger logger, IActorRef receiver, Member target, long originalSeqNo, IActorRef udp, Config config)
        {
            return Akka.Actor.Props.Create(() => new ForwardPingerActor(logger, receiver, target, originalSeqNo, udp, config));
        }

        internal ForwardPingerActor(ILogger logger, IActorRef receiver, Member target, long originalSeqNo, IActorRef udp, Config config)
            : base(logger, udp, config)
        {
            _receiver = receiver;
            _target = target;
            _originalSeqNo = originalSeqNo;
        }

        internal override void SendPings()
        {
            Udp.Tell(new SendMessage(_target, new Ping(SeqNo)));
        }

        internal override void AckReceived()
        {
            _receiver.Tell(new Ack(_originalSeqNo));
        }
    }
}
