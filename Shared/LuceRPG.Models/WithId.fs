namespace LuceRPG.Models

module WithId =
    type 'T Model =
        {
            id: string
            value: 'T
        }

    let id (t: 'T Model): string = t.id
    let value (t: 'T Model): 'T = t.value

    let useId (id: string) (value: 'T): 'T Model =
        {
            id = id
            value = value
        }

    let create (value: 'T): 'T Model =
        let id = Id.make()
        {
            id = id
            value = value
        }

    let map (mapFn: 'T -> 'U) (withId: 'T Model): 'U Model =
        useId (withId.id) (mapFn withId.value)

    let toMap (items: 'T Model List): Map<string, 'T Model> =
        items
        |> List.map (fun i -> (i.id, i))
        |> Map.ofList

    let toList (map: Map<string, 'T Model>): 'T Model List =
        map
        |> Map.toList
        |> List.map snd

type 'T WithId = 'T WithId.Model
