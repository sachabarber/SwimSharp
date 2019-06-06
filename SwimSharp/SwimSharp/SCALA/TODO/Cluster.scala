package fi.jihartik.swim

import akka.actor.{ActorRef, ActorLogging, Actor}
import java.util.concurrent.atomic.AtomicLong

class Cluster(host: String, port: Int, broadcaster: ActorRef, config: Config) extends Actor with ActorLogging {
  val localName = s"Node $host:$port"

  val incarnationNo = new AtomicLong(0)
  var state = ClusterState(localName, Map(localName -> Member(localName, host, port, Alive, incarnationNo.get)))

  def receive = {
    case GetMembers => sender ! state.members
    case GetNotDeadRemotes => sender ! state.notDeadRemotes

    case NewMembers(newMembers) => mergeMembers(newMembers)
    case AliveMember(member) => handleAlive(member)
    case SuspectMember(member) => (ignoreOldIncarnations orElse refute orElse suspectMember orElse ignore)(member)
    case ConfirmSuspicion(member) => confirmSuspicion(member)
    case DeadMember(member) => (ignoreOldIncarnations orElse refute orElse announceDead orElse ignore)(member)
  }

  def mergeMembers(remoteMembers: List[Member]) {
    remoteMembers.foreach {
      case remote if(state.alreadyKnown(remote)) => // Already known, do nothing
      case remote => remote.state match {
        case Alive => self ! AliveMember(remote)
        case Suspect | Dead => self ! SuspectMember(remote)
      }
    }
  }

  def handleAlive(member: Member) {
    if(state.hasWeakerIncarnationFor(member)) {
      log.info("Alive: " + member)
      broadcast(AliveMember(member))
      state += member
    }
  }

  def suspectMember: PartialFunction[Member, Unit] = {
    case member if(state.isAlive(member)) => {
      log.info("Suspect: " + member)
      broadcast(SuspectMember(member))
      state += member.copy(state = Suspect)
      Util.scheduleOnce(config.suspectPeriod, self, ConfirmSuspicion(member))
    }
  }

  def confirmSuspicion(member: Member) {
    if(state.isSuspected(member)) self ! DeadMember(member)  // Member has not been able to refute and is still suspected -> mark as dead
  }

  def announceDead: PartialFunction[Member, Unit] = {
    case member if(state.isNotDead(member)) => {
      broadcast(DeadMember(member))
      log.info("Dead: " + member)
      state += member.copy(state = Dead, incarnation = 0)
    }
  }

  def refute: PartialFunction[Member, Unit] = {
    case offendingMember if (state.isUs(offendingMember)) => {
      incarnationNo.set(offendingMember.incarnation + 1)  // beat offending incarnation
      state = state.updateOurIncarnation(incarnationNo.get)
      log.info("Refuting: " + state.us)
      broadcast(AliveMember(state.us))  // Refute our suspicion / death
    }
  }

  def ignoreOldIncarnations: PartialFunction[Member, Unit] = {
    case member if (state.hasStrongerIncarnationFor(member)) => // Catch, but ignore
  }
  def ignore: PartialFunction[Member, Unit] = { case _ => Unit }
  def broadcast(message: MemberStateMessage) = broadcaster ! message

  case class ConfirmSuspicion(member: Member)
}

case object GetMembers
case object GetNotDeadRemotes