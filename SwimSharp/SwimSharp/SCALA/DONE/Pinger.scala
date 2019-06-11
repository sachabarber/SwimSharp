package fi.jihartik.swim

import akka.actor._
import java.util.concurrent.atomic.AtomicLong

object ProbeSeqNo {
  private val seqNo = new AtomicLong(0)
  def next = seqNo.getAndIncrement
}


trait Pinger extends Actor with ActorLogging {
  val seqNo = ProbeSeqNo.next

  override def preStart() = self ! Start

  def receive = {
    case Start => {
      udp ! RegisterReceiver(self)
      val task = Util.scheduleOnce(config.ackTimeout, self, AckTimedOut)
      sendPings
      context.become(waitingForAck(task))
    }
  }

  def waitingForAck(ackTimer: Cancellable): Receive = {
    case Ack(s) if(s == seqNo) => {
      ackTimer.cancel()
      ackReceived
      context.stop(self)
    }
    case AckTimedOut => {
      ackTimedOut
      context.stop(self)
    }
  }

  def udp: ActorRef
  def config: Config
  def sendPings: Unit
  def ackReceived: Unit = {}
  def ackTimedOut: Unit = {}

  case object Start
  case object AckTimedOut
}

class DirectPinger(receiver: ActorRef, target: Member, possibleForwarders: List[Member], val udp: ActorRef, val config: Config) extends Pinger {
  def sendPings = udp ! SendMessage(target, Ping(seqNo))
  override def ackTimedOut = {
    // Note context.system! IndirectPinger must not be created as a child of DirectPinger as it will terminate after its timeout and thus kill it children
    context.system.actorOf(Props(classOf[IndirectPinger], receiver, target, possibleForwarders, udp, config))
  }
}


class IndirectPinger(receiver: ActorRef, target: Member, forwarders: List[Member], val udp: ActorRef, val config: Config) extends Pinger {
  def sendPings = forwarders.foreach(fwd => udp ! SendMessage(fwd, IndirectPing(seqNo, target)))
  override def ackTimedOut = receiver ! ProbeTimedOut(target)
}


class ForwardPinger(receiver: ActorRef, target: Member, originalSeqNo: Long, val udp: ActorRef, val config: Config) extends Pinger {
  def sendPings = udp ! SendMessage(target, Ping(seqNo))
  override def ackReceived = receiver ! Ack(originalSeqNo)
}
