namespace LuceRPG.Serialisation

open LuceRPG.Models

module PlayerDataSrl =
    let serialise (d: PlayerData): byte[] =
        let name = StringSrl.serialise d.name

        name

    let deserialise (bytes: byte[]): PlayerData DesrlResult =
        StringSrl.deserialise bytes
        |> DesrlResult.map PlayerData.create
