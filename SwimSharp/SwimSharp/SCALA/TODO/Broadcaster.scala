package fi.jihartik.swim

import akka.actor.{ActorLogging, ActorRef, Actor}
import scala.annotation.tailrec

class Broadcaster(udp: ActorRef, config: Config) extends Actor with ActorLogging {
  var state = BroadcastState(Map())

  def receive = {
    case msg: MemberStateMessage => state += Broadcast(msg, transmitCount = 0)
    case SendBroadcasts(members: List[Member]) => sendBroadcasts(members)
  }

  def sendBroadcasts(members: List[Member]) {
    val toBeSend = state.broadcasts
    if(! toBeSend.isEmpty) {
      val (combinedBroadcasts, compoundMsg) = createCompoundMessage(toBeSend)
      val targetMembers = Util.takeRandom(members, config.broadcastMemberCount)
      sendMessage(targetMembers, compoundMsg)
      state = state.updatedWithTransmit(combinedBroadcasts)
    }
  }

  def sendMessage(members: List[Member], message: CompoundUdpMessage) {
    members.foreach(member => udp ! SendMessage(member, message))
  }

  def createCompoundMessage(broadcasts: List[Broadcast]) = {
    val sortedByTransmitCount = broadcasts.sortBy(_.transmitCount)
    @tailrec
    def addBroadcasts(toBeSent: List[Broadcast], sent: List[Broadcast], createdMessage: CompoundUdpMessage): (List[Broadcast], CompoundUdpMessage) = {
      toBeSent match {
        case Nil => (sent, createdMessage)
        case x :: xs => {
          val nextCandidate = createdMessage.copy(messages = createdMessage.messages :+ x.message)
          if(createdMessage.toByteString.length < config.maxUdpMessageSize) {
            addBroadcasts(xs, sent :+ x, nextCandidate)
          } else {
            (sent, createdMessage)
          }
        }
      }
    }
    addBroadcasts(sortedByTransmitCount, Nil, CompoundUdpMessage(Nil))
  }


  case class Broadcast(message: MemberStateMessage, transmitCount: Int)

  case class BroadcastState(val broadcastMap: Map[String, Broadcast]) {
    def +(broadcast: Broadcast) = this.copy(broadcastMap + (broadcast.message.member.name -> broadcast))
    def -(broadcast: Broadcast) = this.copy(broadcastMap - broadcast.message.member.name)
    def broadcasts = broadcastMap.values.toList
    def updatedWithTransmit(transmitted: List[Broadcast]) = {
      val newBroadcastMap = transmitted.foldLeft(broadcastMap) { case (map, broadcast) =>
        if(broadcast.transmitCount < config.maxBroadcastTransmitCount - 1) {  // Transmit count has not yet been updated
          map + (broadcast.message.member.name -> broadcast.copy(transmitCount = broadcast.transmitCount + 1))
        } else {
          map - broadcast.message.member.name
        }
      }
      this.copy(newBroadcastMap)
    }
  }
}

case class SendBroadcasts(members: List[Member])
