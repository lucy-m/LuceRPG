namespace LuceRPG.Serialisation

open LuceRPG.Models
open System

module PointSrl =
    let serialise (p: Point): byte[] =
        let x = IntSrl.serialise p.x
        let y = IntSrl.serialise p.y

        Array.append x y

    let deserialise (bytes: byte[]): Point DesrlResult =
        DesrlUtil.getTwo
            IntSrl.deserialise
            IntSrl.deserialise
            Point.create
            bytes
