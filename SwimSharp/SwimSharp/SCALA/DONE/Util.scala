package fi.jihartik.swim

import scala.util.Random
import scala.concurrent.duration.FiniteDuration
import akka.actor.{ActorContext, ActorRef}

object Util {
  def takeRandom(member: List[Member], howMany: Int) = Random.shuffle(member).take(howMany)
  def scheduleOnce(delay: FiniteDuration, receiver: ActorRef, message: Any)(implicit context: ActorContext) = {
    context.system.scheduler.scheduleOnce(delay, receiver, message)(context.dispatcher)
  }
  def schedule(interval: FiniteDuration, receiver: ActorRef, message: Any)(implicit context: ActorContext) = {
    context.system.scheduler.schedule(interval, interval, receiver, message)(context.dispatcher)
  }
}
