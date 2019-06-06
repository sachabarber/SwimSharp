package fi.jihartik.swim

import akka.actor._

class FailureDetector(udp: ActorRef, config: Config) extends Actor with ActorLogging {

  override def preStart() = udp ! RegisterReceiver(self)

  def receive = {
    case ProbeMembers(members) => probeMembers(members)
    case Ping(seqNo) => sender ! Ack(seqNo)
    case IndirectPing(seqNo, target) => forwardPing(seqNo, target)
  }

  def probeMembers(members: List[Member]) {
    def forwardersFor(target: Member) = Util.takeRandom(members.filterNot(_.name == target.name), config.indirectProbeCount)

    val probedMembers = Util.takeRandom(members, config.probedMemberCount)
    probedMembers.foreach(target => startDirectPinger(sender, target, forwardersFor(target), udp))
  }

  def forwardPing(seqNo: Long, target: Member) = context.actorOf(Props(classOf[ForwardPinger], sender, target, seqNo, udp, config))

  private def startDirectPinger(sender: ActorRef, target: Member, forwarders: List[Member], udp: ActorRef) = {
    context.actorOf(Props(classOf[DirectPinger], sender, target, forwarders, udp, config))
  }
}

case class ProbeMembers(members: List[Member])
case class ProbeTimedOut(member: Member)