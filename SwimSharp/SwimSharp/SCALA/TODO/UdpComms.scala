package fi.jihartik.swim

import akka.actor.{Terminated, ActorLogging, Actor, ActorRef}
import java.net.InetSocketAddress
import akka.io.{Udp, IO}
import akka.pattern.ask
import akka.util.Timeout
import scala.concurrent.duration._


class UdpComms(bindAddress: InetSocketAddress) extends Actor with ActorLogging {
  import context.system
  import context.dispatcher
  implicit val timeout = Timeout(5.seconds)
  var receivers = Set[ActorRef]()

  override def preStart() = IO(Udp) ! Udp.Bind(self, bindAddress)

  def receive = handleRegistrations orElse {
    case Udp.Bound(_) => context.become(ready(sender))
  }

  def ready(ioActor: ActorRef): Receive = handleRegistrations orElse {
    case SendMessage(member, msg) => send(ioActor, member, msg)
    case Udp.Received(data, from) => receivers.foreach(receiver => (receiver ? UdpMessage(data)).mapTo[UdpMessage].foreach(reply => send(ioActor, from, reply)))
  }

  def handleRegistrations: Receive = {
    case RegisterReceiver(receiver) => {
      receivers += receiver
      context.watch(receiver)  // Get notified of death -> unregister on notification
    }
    case Terminated(victim) => receivers -= victim
  }

  def send(ioActor: ActorRef, to: Member, message: UdpMessage): Unit = send(ioActor, addressFor(to), message)
  def send(ioActor: ActorRef, to: InetSocketAddress, message: UdpMessage) = ioActor ! Udp.Send(message.toByteString, to)
  def addressFor(member: Member) = new InetSocketAddress(member.ip, member.port)
}


case class SendMessage(to: Member, message: UdpMessage)
case class RegisterReceiver(registree: ActorRef)