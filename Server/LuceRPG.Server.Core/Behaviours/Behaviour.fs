namespace LuceRPG.Server.Core

open LuceRPG.Models

module Behaviour =
    type ObjectlessIntention = Id.WorldObject -> Intention.Type

    type Model =
        {
            update: int64 -> ObjectlessIntention seq * Model Option
        }

    type MovementStep =
        | Move of Direction * byte
        | TurnTo of Direction
        | Wait of int64

    let rec patrol
            (steps: MovementStep List)
            (repeat: bool)
            (tWaitUntil: int64 Option)
            : Model =

        let update (timestamp: int64): ObjectlessIntention seq * Model Option =
            match steps with
            | nextStep::tl ->
                let newSteps =
                    if repeat
                    then List.append tl [nextStep]
                    else tl

                match nextStep with
                | Move (d, n) ->
                    let intention (id: Id.WorldObject)
                        = Intention.Type.Move (id, d, n)

                    // if this is the last step then terminate
                    let next =
                        if newSteps |> List.isEmpty
                        then Option.None
                        else Option.Some (patrol newSteps repeat Option.None)

                    Seq.singleton intention, next

                | TurnTo d ->
                    let intention (id: Id.WorldObject)
                        = Intention.Type.TurnTowards (id, d)

                    // if this is the last step then terminate
                    let next =
                        if newSteps |> List.isEmpty
                        then Option.None
                        else Option.Some (patrol newSteps repeat Option.None)

                    Seq.singleton intention, next

                | Wait waitDuration ->
                    match tWaitUntil with
                    | Option.Some waitUntil ->
                        // Wait has been initiated, check whether the wait is finished
                        if timestamp >= waitUntil
                        then
                            // Finished waiting, move on to next step
                            // Initiate next step immediately

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

    let patrolUniform
            (moves: (Direction * byte) seq)
            (pauseBetween: System.TimeSpan)
            (repeat: bool)
            : Model =
        let pauseBetweenTicks = pauseBetween.Ticks
        let steps =
            moves
            |> Seq.collect (fun (d, n) ->
                let move = MovementStep.Move (d,n)

                if pauseBetweenTicks >= 0L
                then
                    let wait = MovementStep.Wait pauseBetweenTicks
                    [move; wait]
                else
                    [move]
            )
            |> List.ofSeq

        patrol steps repeat Option.None

    let spinner (pauseBetween: System.TimeSpan): Model =
        let wait = MovementStep.Wait pauseBetween.Ticks

        let steps =
            [
                Direction.North
                Direction.East
                Direction.South
                Direction.West
            ]
            |> List.collect (fun d ->
                let turn = MovementStep.TurnTo d

                if pauseBetween.Ticks >= 0L
                then [turn; wait]
                else [turn]
            )

        patrol steps true Option.None

    /// minWait and maxWait should be <= 3 minutes due to number truncation
    let randomWalk
            (minWait: System.TimeSpan)
            (maxWait: System.TimeSpan)
            : Model =
        let r = System.Random()

        let makePatrol (): Model =
            let wait =
                let min = int32(minWait.Ticks)
                let max = int32(maxWait.Ticks)

                if max <= min
                then []
                else
                    int64(r.Next(min, max))
                    |> MovementStep.Wait
                    |> List.singleton

            let dir =
                match r.Next(0, 4) with
                | 0 -> Direction.North
                | 1 -> Direction.East
                | 2 -> Direction.South
                | _ -> Direction.West

            let steps =
                MovementStep.Move (dir, 1uy)::wait

            patrol steps false Option.None

        let rec randomWalkInner (currentPatrol: Model): Model =
            let update (timestamp: int64): ObjectlessIntention seq * Model Option =
                let patrolResult = currentPatrol.update timestamp

                let intentions = fst patrolResult |> List.ofSeq

                if intentions |> List.isEmpty && snd patrolResult |> Option.isNone
                then
                    // Finished waiting, need to move on to next step immediately
                    (randomWalkInner (makePatrol())).update(timestamp)
                else
                    let behaviour =
                        snd patrolResult
                        |> Option.map (fun p -> randomWalkInner p)
                        |> Option.defaultValue (randomWalkInner (makePatrol()))
                        |> Option.Some

                    (intentions |> Seq.ofList), behaviour

            {
                update = update
            }

        randomWalkInner (makePatrol())

type Behaviour = Behaviour.Model
