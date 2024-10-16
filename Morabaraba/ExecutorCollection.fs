module Morabaraba.ExecutorCollection

open Morabaraba
open OccupationService

let checkPlacingHand board _ =
    let player = board.Player
    if player.Hand > 0 then Some board else None

let checkPlacingDestination board action =
    let occupants = board.Occupants

    if Map.containsKey action.Destination occupants then
        None
    else
        Some board

let place board action =
    let updatedOccupants = Map.add action.Destination board.Player.Shade board.Occupants

    Some
        { board with
            Occupants = updatedOccupants }

let switchTurns board _ =
    let player, opponent = board.Player, board.Opponent

    Some
        { board with
            Player = opponent
            Opponent = player }

let decreaseHand board _ =
    let player = board.Player

    let updatedPlayer = { player with Hand = player.Hand - 1 }

    Some { board with Player = updatedPlayer }

let checkShootingTargetShade board action =
    let isShadeAppropriate =
        Map.tryFind action.Destination board.Occupants = Some board.Opponent.Shade

    if isShadeAppropriate then Some board else None

let checkShootingTargetNotInMill board action =
    let shade = board.Opponent.Shade
    let occupants = board.Occupants

    let isDestinationInMill =
        List.filter (isAMill shade occupants) lines
        |> List.exists (List.contains action.Destination)

    if isDestinationInMill then None else Some board

let checkAllOpponentCowsAreInMills board _ =
    let opponentOccupants = occupantsByShade board.Opponent.Shade board.Occupants

    let junctionsInOpponentMills =
        junctionsInMillsByShade board.Opponent.Shade board.Occupants

    if List.length junctionsInOpponentMills = Map.count opponentOccupants then
        Some board
    else
        None

let checkPlayerMillIsNew board _ =
    let junctionsInPlayerMills =
        junctionsInMillsByShade board.Player.Shade board.Occupants

    List.tryHead board.History
    |> Option.map (fun { Destination = d } ->
        if List.contains d junctionsInPlayerMills then
            Some board
        else
            None)
    |> Option.flatten

let shoot board action =
    let updatedOccupants = Map.remove action.Destination board.Occupants

    Some
        { board with
            Occupants = updatedOccupants }

let checkMovingJunctions board action =
    let areJunctionsNeighbours =
        match action.Source with
        | Some source ->
            List.contains (source, action.Destination) neighbours
            || List.contains (action.Destination, source) neighbours
        | None -> false

    if areJunctionsNeighbours then Some board else None

let checkLegalMillFormation board action =
    let occupantsWithBrokenMill =
        Map.add action.Destination board.Player.Shade board.Occupants

    let wasInAMill =
        let millJunctions =
            junctionsInMillsByShade board.Player.Shade occupantsWithBrokenMill

        List.contains action.Destination millJunctions

    let isInAMill =
        let millJunctions = junctionsInMillsByShade board.Player.Shade board.Occupants

        List.exists (fun junction -> Some junction = action.Source) millJunctions

    let wasOpponentLastActionAShot =
        let lastOpponentAction = List.tryHead board.History

        match lastOpponentAction with
        | Some { Source = None; Destination = d } -> not <| Map.containsKey d board.Occupants
        | _ -> false

    let isActionReverseOfPrevious =
        // Action indexed 0 is the opponent's last action
        // Action indexed 1 is the player's last shot action
        // Action indexed 2 might the player's last non-action action without a shot
        // Action indexed 3 might be the player last action before a shot
        let lastNonShotActionIndex = if wasOpponentLastActionAShot then 3 else 2
        let lastNonShotAction = List.tryItem lastNonShotActionIndex board.History

        match lastNonShotAction with
        | Some { Source = Some previousSource
                 Destination = previousDestination } ->
            action = { Source = Some previousDestination
                       Destination = previousSource }
        | _ -> false

    if wasInAMill && isInAMill && isActionReverseOfPrevious then
        None
    else
        Some board

let move board action =
    Option.map (fun source -> Map.remove source board.Occupants) action.Source
    |> Option.map (Map.add action.Destination board.Player.Shade)
    |> Option.map (fun occupants -> { board with Occupants = occupants })

let checkCowCountAllowsFlying board _ =
    let occupantCount = occupantsByShade board.Player.Shade board.Occupants |> Map.count
    if occupantCount = 3 then Some board else None

let saveAction board action =
    Some
        { board with
            History = action :: board.History }

let winIfNoMovesForOpponent board _ =
    let opponentJunctions =
        occupantsByShade board.Opponent.Shade board.Occupants |> Map.keys |> List.ofSeq

    let neighbourOfOccupant occupantJunction (junction1, junction2) =
        if junction1 = occupantJunction then Some junction2
        else if junction2 = occupantJunction then Some junction1
        else None

    let neighboursOfOccupant occupantJunction =
        List.map (neighbourOfOccupant occupantJunction) neighbours
        |> List.filter Option.isSome
        |> List.map Option.get

    let areAllNeighboursOccupied =
        List.collect neighboursOfOccupant opponentJunctions
        |> List.forall (fun junction -> Map.containsKey junction board.Occupants)

    if areAllNeighboursOccupied then
        Some { board with Status = Won }
    else
        None

let winIfOpponentHasTwoCowsLeft board _ =
    let opponentJunctions =
        occupantsByShade board.Opponent.Shade board.Occupants |> Map.keys |> List.ofSeq

    if List.length opponentJunctions = 2 then
        Some { board with Status = Won }
    else
        None

let drawIfNoShotsInTenMoves board _ =
    let hasAtLeastTenMoves = List.length board.History >= 10

    let playerCowCount =
        occupantsByShade board.Player.Shade board.Occupants |> Map.count

    let isPlayerFlying = board.Player.Hand = 0 && 3 = playerCowCount

    let lastMovesHaveASource history =
        List.map (_.Source) history |> List.exists Option.isSome

    if
        hasAtLeastTenMoves
        && isPlayerFlying
        && lastMovesHaveASource board.History.[0..9]
    then
        Some { board with Status = Drew }
    else
        None
