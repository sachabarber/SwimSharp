package fi.jihartik.swim

import scala.concurrent.duration._

case class Config(
   probeInterval: FiniteDuration = 500.millis,
   ackTimeout: FiniteDuration = 300.millis,
   suspectPeriod: FiniteDuration = 2.seconds,
   probedMemberCount: Int = 3,
   broadcastInterval: FiniteDuration = 200.millis,
   broadcastMemberCount: Int = 3,
   maxBroadcastTransmitCount: Int = 5,
   indirectProbeCount: Int = 3,
   maxUdpMessageSize: Int = 40000
)

object DefaultConfig extends Config()