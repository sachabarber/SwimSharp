using System;

namespace SwimSharp
{
    internal class Config
    {
        public TimeSpan ProbeInterval { get; set; } = TimeSpan.FromMilliseconds(500);
        public TimeSpan AckTimeout { get; set; } = TimeSpan.FromMilliseconds(300);
        public TimeSpan SuspectPeriod { get; set; } = TimeSpan.FromSeconds(2);
        public TimeSpan BroadcastInterval { get; set; } = TimeSpan.FromMilliseconds(200);
        public int ProbedMemberCount { get; set; } = 3;
        public int BroadcastMemberCount { get; set; } = 3;
        public int MaxBroadcastTransmitCount { get; set; } = 5;
        public int IndirectProbeCount { get; set; } = 3;
        public int MaxUdpMessageSize { get; set; } = 40000;

        internal Config()
        {

        }

        internal Config(
            TimeSpan probeInterval,
            TimeSpan ackTimeout,
            TimeSpan suspectPeriod,
            TimeSpan broadcastInterval,
            int probedMemberCount,
            int broadcastMemberCount,
            int maxBroadcastTransmitCount,
            int indirectProbeCount,
            int maxUdpMessageSize)
        {
            this.ProbeInterval = probeInterval;
            this.AckTimeout = ackTimeout;
            this.SuspectPeriod = suspectPeriod;
            this.BroadcastInterval = broadcastInterval;
            this.ProbedMemberCount = probedMemberCount;
            this.BroadcastMemberCount = broadcastMemberCount;
            this.MaxBroadcastTransmitCount = maxBroadcastTransmitCount;
            this.IndirectProbeCount = indirectProbeCount;
            this.MaxUdpMessageSize = maxUdpMessageSize;
        }
    }
    
    internal sealed class DefaultConfig : Config
    {
        public DefaultConfig() : base()
        {

        }
    }
}


