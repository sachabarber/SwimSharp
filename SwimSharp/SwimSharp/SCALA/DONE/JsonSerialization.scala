package fi.jihartik.swim

import spray.json._

object JsonSerialization extends DefaultJsonProtocol {
  implicit object MemberStateFormat extends RootJsonFormat[MemberState] {
    def write(state: MemberState): JsValue = state match {
      case Alive => JsString("alive")
      case Dead => JsString("dead")
      case Suspect => JsString("suspect")
    }

    def read(json: JsValue): MemberState = json match {
      case JsString("alive") => Alive
      case JsString("dead") => Dead
      case JsString("suspect") => Suspect
      case _ => spray.json.deserializationError("State must by 'alive', 'dead' or 'suspect")
    }
  }
  implicit val memberFormat = jsonFormat5(Member)
}
