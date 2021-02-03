namespace LuceRPG.Serialisation

open LuceRPG.Models

module InteractionSrl =

    let serialiseOne (one: Interaction.One): byte[] =
        let label =
            match one with
            | Interaction.Chat _ -> 1uy

        let addtInfo =
            match one with
            | Interaction.Chat s -> StringSrl.serialise s

        Array.append [|label|] addtInfo

    let serialisePayload (p: Interaction.Payload): byte[] =
        ListSrl.serialise serialiseOne p

    let serialise (i: Interaction): byte[] =
        WithIdSrl.serialise serialisePayload i

    let deserialiseOne (bytes: byte[]): Interaction.One DesrlResult =
        let getTagged (tag: byte) (objectBytes: byte[]): Interaction.One DesrlResult =
            match tag with
            | 1uy ->
                StringSrl.deserialise objectBytes
                |> DesrlResult.map Interaction.One.Chat
            | _ ->
                printfn "Unknown tag for Interaction %u" tag
                Option.None

        DesrlUtil.getTagged getTagged bytes

    let deserialisePayload (bytes: byte[]): Interaction.Payload DesrlResult =
        ListSrl.deserialise deserialiseOne bytes

    let deserialise (bytes: byte[]): Interaction DesrlResult =
        WithIdSrl.deserialise deserialisePayload bytes

