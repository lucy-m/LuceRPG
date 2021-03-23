namespace LuceRPG.Models

module Behaviour =
    type ObjectlessIntention = Id.WorldObject -> Intention.Type

    type Model =
        {
            update: int64 -> ObjectlessIntention seq * Model Option
        }

    type MovementStep =
        | Move of Direction * byte
        | Wait of int64

    let rec patrol
        (steps: MovementStep List)
        (repeat: bool)
        (tWaitUntil: int64 Option)
        : Model =

        let update (timestamp: int64): ObjectlessIntention seq * Model Option =
            match steps with
            | nextStep::tl ->
                match nextStep with
                | Move (d, n) ->
                    let intention (id: Id.WorldObject)
                        = Intention.Type.Move (id, d, n)
                    let newSteps =
                        if repeat
                        then List.append tl [nextStep]
                        else tl

                    Seq.singleton intention, Option.Some (patrol newSteps repeat Option.None)

                | Wait waitDuration ->
                    match tWaitUntil with
                    | Option.Some waitUntil ->
                        // Wait has been initiated, check whether the wait is finished
                        if timestamp >= waitUntil
                        then
                            // Finished waiting, move on to next step
                            // Initiate next step immediately
                            let newSteps =
                                if repeat
                                then List.append tl [nextStep]
                                else tl

                            (patrol newSteps repeat Option.None).update timestamp
                        else
                            // Continue waiting
                            Seq.empty, Option.Some (patrol steps repeat tWaitUntil)
                    | Option.None ->
                        // Initiate wait from this timestamp
                        // This timestamp should be the point at which the object
                        //   stopped being busy
                        let waitUntil = timestamp + waitDuration |> Option.Some
                        Seq.empty, Option.Some (patrol steps repeat waitUntil)

            | [] -> Seq.empty, Option.None

        {
            update = update
        }

type Behaviour = Behaviour.Model
