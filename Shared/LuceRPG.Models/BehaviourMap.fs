namespace LuceRPG.Models

module BehaviourMap =

    type Model = Map<Id.WorldObject, Behaviour WithId>

    type UpdateResult =
        {
            model: Model
            intentions: Intention.Type seq
            logs: string seq
        }

    type UpdateAcc =
        {
            updated: (Id.WorldObject * Behaviour WithId) List
            intentions: Intention.Type seq
            logs: string List
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
                logs = []
            }

        let accResult =
            model
            |> Map.fold (fun acc objId b ->
                let isBusy =
                    objectBusyMap
                    |> Map.tryFind objId
                    |> Option.map (fun busyTime -> busyTime > now)
                    |> Option.defaultValue false

                if isBusy
                then
                    let log = sprintf "Behaviour %s did nothing as object is busy" b.id
                    {
                        intentions = acc.intentions
                        updated = (objId, b)::acc.updated
                        logs = log::acc.logs
                    }
                else
                    let objectlessIntentions, newBehaviour =
                        b.value.update now
                        |> (fun (oi, nb) -> List.ofSeq oi, nb)

                    let intentions =
                        objectlessIntentions
                        |> Seq.map (fun f -> f objId)
                        |> Seq.append acc.intentions

                    let updated =
                        match newBehaviour with
                        | Option.Some nb -> (objId, WithId.useId b.id nb)::acc.updated
                        | Option.None -> acc.updated

                    let logs =
                        let intentionCount =
                            let c = objectlessIntentions.Length
                            if c = 0
                            then Option.None
                            else Option.Some (sprintf "Behaviour %s produced %i intentions" b.id c)

                        let isFinished =
                            match newBehaviour with
                            | Option.Some _ -> Option.None
                            | Option.None -> Option.Some (sprintf "Behaviour %s is complete" b.id)

                        [ intentionCount; isFinished ]
                        |> List.choose id
                        |> List.append acc.logs

                    {
                        intentions = intentions
                        updated = updated
                        logs = logs
                    }
            ) initial

        let result: UpdateResult =
            let model = accResult.updated |> Map.ofList
            let intentions = accResult.intentions

            {
                model = model
                intentions = intentions
                logs = accResult.logs
            }

        result

type BehaviourMap = BehaviourMap.Model
