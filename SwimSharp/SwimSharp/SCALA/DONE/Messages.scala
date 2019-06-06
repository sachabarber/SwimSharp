package fi.jihartik.swim

import akka.util.ByteString
import spray.json._


trait UdpMessage {
  def toMessageString: String
  def toByteString: ByteString = ByteString(toMessageString)
}
trait FailureDetectionMessage extends UdpMessage
trait ClusterStateMessage

case class Ping(seqNo: Long) extends FailureDetectionMessage {
  def toMessageString = s"0$seqNo"
}
case class IndirectPing(seqNo: Long, target: Member) extends FailureDetectionMessage {
  import JsonSerialization._
  def toMessageString = s"1$seqNo ${target.toJson.compactPrint}"
}
case class Ack(seqNo: Long) extends FailureDetectionMessage {
  def toMessageString = s"2$seqNo"
}
abstract class MemberStateMessage(msgType: Int) extends UdpMessage with ClusterStateMessage {
  import JsonSerialization._
  def member: Member
  def toMessageString = s"$msgType${member.toJson.compactPrint}"
}
case class AliveMember(member: Member) extends MemberStateMessage(3)
case class SuspectMember(member: Member) extends MemberStateMessage(4)
case class DeadMember(member: Member) extends MemberStateMessage(5)
case class CompoundUdpMessage(messages: List[UdpMessage]) extends UdpMessage with ClusterStateMessage {
  def toMessageString = "6" ++ messages.map { msg =>
    val msgBody = msg.toMessageString
    val msgHeader = msgBody.size + " "
    msgHeader ++ msgBody
  }.foldLeft("")(_ + _)
}
case class NewMembers(members: List[Member]) extends ClusterStateMessage


object UdpMessage {
  import JsonSerialization._

  def apply(payload: ByteString): UdpMessage = apply(payload.decodeString("UTF-8"))

  def apply(decoded: String): UdpMessage = {
    val msgType = decoded.take(1).toInt
    val message = decoded.drop(1)
    msgType match {
      case 0 => Ping(message.toLong)
      case 1 => IndirectPing(message.takeWhile(_ != ' ').toLong, message.dropWhile(_ != ' ').drop(1).asJson.convertTo[Member])
      case 2 => Ack(message.toLong)
      case 3 => AliveMember(message.asJson.convertTo[Member])
      case 4 => SuspectMember(message.asJson.convertTo[Member])
      case 5 => DeadMember(message.asJson.convertTo[Member])
      case 6 => parseCompoundMessage(message)
    }
  }

  private def parseCompoundMessage(message: String) = {
    def parseNextMessage(message: String, messages: List[UdpMessage]): List[UdpMessage] = {
      if(message != "") {
        val (size, rest) = message.splitAt(message.indexOf(" "))
        val messageBody = rest.drop(1).take(size.toInt)
        parseNextMessage(rest.drop(1).drop(size.toInt), apply(messageBody) :: messages)
      } else {
        messages
      }
    }
    CompoundUdpMessage(parseNextMessage(message, Nil))
  }
}
