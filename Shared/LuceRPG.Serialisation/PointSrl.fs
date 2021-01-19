namespace LuceRPG.Serialisation

open LuceRPG.Models
open System

module PointSrl =
    let serialise (p: Point): byte[] =
        let x = BitConverter.GetBytes(p.x)
        let y = BitConverter.GetBytes(p.y)

        Array.append x y

    let deserialise (bytes: byte[]): Point DesrlResult =
        DesrlUtil.getTwo
            IntSrl.deserialise
            IntSrl.deserialise
            Point.create
            bytes
