package fi.jihartik.swim

trait MemberState
case object Alive extends MemberState
case object Dead extends MemberState
case object Suspect extends MemberState

case class Member(name: String, ip: String, port: Int, state: MemberState, incarnation: Long)


case class ClusterState(localName: String, memberMap: Map[String, Member]) {
  def +(member: Member) = this.copy(memberMap = memberMap + (member.name -> member))
  def -(member: Member) = this.copy(memberMap = memberMap - member.name)
  def updateOurIncarnation(newIncarnation: Long) = this.`+`(us.copy(incarnation = newIncarnation))
  def members = memberMap.values.toList
  def remotes = (memberMap - localName).values.toList
  def notDeadRemotes = remotes.filterNot(_.state == Dead)
  def isAlive(member: Member) = isInState(member)(Alive)
  def isSuspected(member: Member) = isInState(member)(Suspect)
  def isNotDead(member: Member) = hasState(member)(_ != Dead)
  def alreadyKnown(member: Member) = hasState(member)(_ == member.state)
  def isUs(member: Member) = member.name == localName
  def hasWeakerIncarnationFor(member: Member) = storedIncarnationOf(member) < member.incarnation
  def hasSameOrWeakerIncarnationFor(member: Member) = storedIncarnationOf(member) <= member.incarnation
  def hasStrongerIncarnationFor(member: Member) = ! hasSameOrWeakerIncarnationFor(member)
  def us = memberMap(localName)

  private def isInState(member: Member)(state: MemberState) = hasState(member)(_ == state)
  private def hasState(member: Member)(predicate: MemberState => Boolean) = get(member).map(_.state).exists(predicate)
  private def storedIncarnationOf(member: Member) = get(member).map(_.incarnation).getOrElse(Long.MinValue)  // Default to weakest possible incarnation
  private def get(member: Member) = memberMap.get(member.name)
}
