namespace LuceRPG.Serialisation

open LuceRPG.Models

module RectSrl =
    let serialise (rect: Rect): byte[] =
        let topLeft = PointSrl.serialise rect.topLeft
        let size = PointSrl.serialise rect.size

        Array.append topLeft size

    let deserialise (bytes: byte[]): Rect DesrlResult =
        DesrlUtil.getTwo
            PointSrl.deserialise
            PointSrl.deserialise
            Rect.pointCreate
            bytes
