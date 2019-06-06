using SwimSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Akka.Actor;

namespace SwimSharp
{
    public static class Util
    {
        public static List<Member> TakeRandom(List<Member> member, int howMany) 
            => member.Shuffle().Take(howMany).ToList();

        public static void ScheduleOnce(TimeSpan delay, IActorRef receiver, object message, Akka.Actor.IScheduler scheduler)
        {
            scheduler.ScheduleTellOnce(delay, receiver, message, ActorRefs.NoSender);
        }

        public static void Schedule(TimeSpan interval, IActorRef receiver, object message, Akka.Actor.IScheduler scheduler)
        {
            scheduler.ScheduleTellRepeatedly(interval, interval, receiver, message, ActorRefs.NoSender);
        }
    }
}
