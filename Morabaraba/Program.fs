﻿module Morabaraba

type Shade =
    | Dark
    | Light

type Junction = Junction of string

type Competitor = { Shade: Shade; Hand: int }

type Board =
    { Player: Competitor
      Opponent: Competitor
      Occupants: Map<Junction, Shade> }

type Action =
    { Source: option<Junction>
      Destination: Junction }

type Game = { Board: Board; History: list<Action> }

type Executor =
    | Executor of (Game -> Action -> option<Game>) * child: Executor * failed: Executor
    | NoExecutor

let initialGame =
    let player = { Shade = Dark; Hand = 12 }
    let opponent = { player with Shade = Light }

    let board =
        { Player = player
          Opponent = opponent
          Occupants = Map.empty }

    { History = []; Board = board }

let execute game action =
    let updatedOccupants =
        Map.add action.Destination game.Board.Player.Shade game.Board.Occupants

    let board =
        { game.Board with
            Occupants = updatedOccupants }

    Some { game with Board = board }

[<EntryPoint>]
let main _ = 0
