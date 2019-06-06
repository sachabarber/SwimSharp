using Akka.IO;
using Newtonsoft.Json;
using SwimSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SwimSharp
{
    internal abstract class UdpMessage
    {
        internal abstract String ToMessageString();
        internal ByteString ToByteString() => ByteString.FromString(ToMessageString());

        internal static UdpMessage Apply(ByteString payload)
        {
            return Apply(payload.ToString(Encoding.UTF8));
        }

        internal static UdpMessage Apply(String decoded)
        {
            var msgType = int.Parse(new String(decoded.Take(1).ToArray()));
            var message = new String(decoded.Skip(1).ToArray());
            switch(msgType)
            {
                case 0:
                    return new Ping(long.Parse(message));
                case 1:
                    var seqNo = long.Parse(new String(message.TakeWhile(x => x != ' ').ToArray()));
                    var idx = message.IndexOf(' ') + 1;
                    var member = JsonConvert.DeserializeObject<Member>(message.Substring(idx));
                    return new IndirectPing(seqNo, member);
                case 2:
                    return new Ack(long.Parse(message));
                case 3:
                    var member = JsonConvert.DeserializeObject<Member>(message);
                    return new AliveMember(member);
                case 4:
                    var member = JsonConvert.DeserializeObject<Member>(message);
                    return new SuspectMember(member);
                case 5:
                    var member = JsonConvert.DeserializeObject<Member>(message);
                    return new DeadMember(member);
                case 6:
                    return ParseCompoundMessage(message);
                default:
                    throw new InvalidProgramException("Out of range value for msgType in UdpMessage.Apply()")
            }
        }

        private CompoundUdpMessage ParseCompoundMessage(string messageOuter)
        {
            List<UdpMessage> ParseNextMessage(string message, List<UdpMessage> messages)
            {
                if(message != "")
                {
                    var (size, rest) = message.SplitAt(message.IndexOf(" "));
                    var messageBody = new String(rest.Skip(1).Take(int.Parse(size)).ToArray());
                    messages.Add(Apply(messageBody));
                    return ParseNextMessage(
                        new String(rest.Skip(1).Skip(int.Parse(size)).ToArray()), messages);
                }
                else
                {
                    return messages;
                }
            }
            return new CompoundUdpMessage(ParseNextMessage(messageOuter, new List<UdpMessage>()));
        }
    }

    internal abstract class FailureDetectionMessage : UdpMessage
    {
    }

    internal interface ClusterStateMessage
    {
    }

    internal class Ping : FailureDetectionMessage
    {
        internal Ping(long seqNo)
        {
            this.SeqNo = seqNo;
        }

        internal long SeqNo { get; private set; }

        internal override string ToMessageString()
        {
            return $"0{SeqNo}";
        }
    }

    internal class IndirectPing : FailureDetectionMessage
    {
        internal IndirectPing(long seqNo, Member target)
        {
            this.SeqNo = seqNo;
            this.Target = target;
        }

        internal long SeqNo { get; private set; }
        internal Member Target { get; private set; }

        internal override string ToMessageString()
        {
            var targetJsonString = JsonConvert.SerializeObject(Target, Formatting.None);
            return $"1{SeqNo} {targetJsonString}";
        }
    }

    internal class Ack : FailureDetectionMessage
    {
        internal Ack(long seqNo)
        {
            this.SeqNo = seqNo;
        }

        internal long SeqNo { get; private set; }

        internal override string ToMessageString()
        {
            return $"2{SeqNo}";
        }
    }

    internal abstract class MemberStateMessage : UdpMessage, ClusterStateMessage
    {
        public MemberStateMessage(Member member, int msgType)
        {
            this.Member = member;
            this.MsgType = msgType;
        }

        internal Member Member { get; private set; }
        internal int MsgType { get; private set; }

        internal override string ToMessageString()
        {
            var targetJsonString = JsonConvert.SerializeObject(Member, Formatting.None);
            return $"{MsgType}{targetJsonString}";
        }
    }

    internal class AliveMember : MemberStateMessage
    {
        public AliveMember(Member member) : base(member, 3) { }
    }

    internal class SuspectMember : MemberStateMessage
    {
        public SuspectMember(Member member) : base(member, 4) { }
    }


    internal class DeadMember : MemberStateMessage
    {
        public DeadMember(Member member) : base(member, 5) { }
    }

    internal class CompoundUdpMessage : UdpMessage, ClusterStateMessage
    {
        public CompoundUdpMessage(List<UdpMessage> messages)
        {
            this.Messages = messages;
        }

        public List<UdpMessage> Messages { get; private set; }

        internal override string ToMessageString()
        {
            var messages = Messages.Select(msg =>
                           {
                               var msgBody = msg.ToMessageString();
                               var msgHeader = msgBody.Length + " ";
                               return msgHeader + msgBody;

                           });
            var joined = String.Join("", messages);
            return $"6{joined}";
        }
    }

    internal class NewMembers : ClusterStateMessage
    {
        public NewMembers(List<Member> members)
        {
            this.Members = members;
        }

        public List<Member> Members { get; private set; }
    }
}



