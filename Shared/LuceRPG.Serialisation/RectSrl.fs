namespace LuceRPG.Serialisation

open LuceRPG.Models

module RectSrl =
    let serialise (rect: Rect): byte[] =
        let btmLeft = PointSrl.serialise rect.btmLeft
        let size = PointSrl.serialise rect.size

        Array.append btmLeft size

    let deserialise (bytes: byte[]): Rect DesrlResult =
        DesrlUtil.getTwo
            PointSrl.deserialise
            PointSrl.deserialise
            Rect.pointCreate
            bytes
