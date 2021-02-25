namespace LuceRPG.Serialisation

open LuceRPG.Models

module PlayerDataSrl =
    let serialise (d: CharacterData): byte[] =
        let name = StringSrl.serialise d.name

        name

    let deserialise (bytes: byte[]): CharacterData DesrlResult =
        StringSrl.deserialise bytes
        |> DesrlResult.map CharacterData.create
