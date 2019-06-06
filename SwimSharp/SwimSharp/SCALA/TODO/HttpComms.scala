package fi.jihartik.swim

import akka.actor.{Stash, Actor, Props, ActorRef}
import spray.can.Http
import akka.io.IO
import spray.routing.HttpServiceActor
import java.net.InetSocketAddress
import akka.util.Timeout
import scala.concurrent.duration._
import akka.pattern.ask
import akka.pattern.pipe
import spray.client.pipelining._
import spray.httpx.SprayJsonSupport._
import spray.json.CompactPrinter

class HttpComms(node: ActorRef, bindAddress: InetSocketAddress) extends Actor with Stash {
  import context.dispatcher
  import JsonSerialization._
  implicit val timeout = Timeout(5.seconds)

  val httpPipeline = sendReceive

  override def preStart() = IO(Http)(context.system) ! Http.Bind(self, bindAddress, 100, Nil, None)

  def receive = {
    case Http.Bound(_) => {
      unstashAll()
      context.become(listening(sender))
    }
    case msg => stash()  // Queue all messages until we are ready (= Bound message received)
}

  def listening(listener: ActorRef): Receive = {
    case Http.Connected(_, _) => sender ! Http.Register(context.system.actorOf(Props(new HttpHandler(node))))
    case SendMembers(to, members) => (httpPipeline ~> unmarshal[List[Member]]).apply(sendMembersRequest(to, members)).map(NewMembers) pipeTo node
    case Stop => {
      val originalSender = sender
      listener ? Http.Unbind pipeTo(originalSender)
    }
  }

  private def sendMembersRequest(to: InetSocketAddress, members: List[Member]) = Post(s"http://${to.getHostName}:${to.getPort}/members", members)
}

class HttpHandler(node: ActorRef) extends HttpServiceActor {
  import context.dispatcher
  import JsonSerialization._
  implicit val jsonPrinter = CompactPrinter
  implicit val timeout = Timeout(5.seconds)

  def receive = runRoute {
    path("members") {
      post {
        entity(as[List[Member]]) { members =>
          complete {
            node ! NewMembers(members)
            getMembers
          }
        }
      } ~
      get {
        complete {
          getMembers
        }
      }
    }
  }

  def getMembers = node.ask(GetMembers).mapTo[List[Member]]
}

case class SendMembers(to: InetSocketAddress, members: List[Member])
