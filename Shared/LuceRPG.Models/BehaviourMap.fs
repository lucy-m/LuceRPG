namespace LuceRPG.Models

module BehaviourMap =

    type Model = Map<Id.WorldObject, Behaviour>

    type UpdateResult =
        {
            model: Model
            intentions: Intention.Type seq
        }

    type UpdateAcc =
        {
            updated: (Id.WorldObject * Behaviour) List
            intentions: Intention.Type seq
        }

    let update
            (now: int64)
            (objectBusyMap: ObjectBusyMap)
            (model: Model)
            : UpdateResult =

        let initial: UpdateAcc =
            {
                updated = []
                intentions = []
            }

        let accResult =
            model
            |> Map.fold (fun acc id b ->
                let isBusy =
                    objectBusyMap
                    |> Map.tryFind id
                    |> Option.map (fun busyTime -> busyTime > now)
                    |> Option.defaultValue false

                if isBusy
                then
                    {
                        intentions = acc.intentions
                        updated = (id, b)::acc.updated
                    }
                else
                    let objectlessIntentions, newBehaviour = b.update now

                    let intentions =
                        objectlessIntentions
                        |> Seq.map (fun f -> f id)
                        |> Seq.append acc.intentions

                    let updated =
                        match newBehaviour with
                        | Option.Some nb -> (id, nb)::acc.updated
                        | Option.None -> acc.updated

                    {
                        intentions = intentions
                        updated = updated
                    }
            ) initial

        let result: UpdateResult =
            let model = accResult.updated |> Map.ofList
            let intentions = accResult.intentions

            {
                model = model
                intentions = intentions
            }

        result

type BehaviourMap = BehaviourMap.Model
