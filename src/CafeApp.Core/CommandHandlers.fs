module CommandHandlers
open Chessie.ErrorHandling
open States
open Events
open System
open Domain
open Commands
open Errors

let (|NonOrderedDrink|_|) order drink =
  match List.contains drink order.Drinks with
  | false -> Some drink
  | true -> None

let (|NonOrderedFood|_|) order food =
  match List.contains food order.Foods with
  | false -> Some food
  | true -> None

let (|ServeDrinkCompletesOrder|_|) order drink =
  match isServingDrinkCompletesOrder order drink with
  | true -> Some drink
  | false -> None

let handleOpenTab tab = function
| ClosedTab _ -> [TabOpened tab] |> ok
| _ -> TabAlreadyOpened |> fail

let handlePlaceOrder order = function
| OpenedTab _ ->
  if List.isEmpty order.Foods && List.isEmpty order.Drinks then
    fail CanNotPlaceEmptyOrder
  else
    [OrderPlaced order] |> ok
| ClosedTab _ -> fail CanNotOrderWithClosedTab
| _ -> fail OrderAlreadyPlaced

let handleServeDrink drink tabId = function
| PlacedOrder order ->
  let event = DrinkServed (drink,tabId)
  match drink with
  | NonOrderedDrink order _ ->
    CanNotServeNonOrderedDrink drink |> fail
  | ServeDrinkCompletesOrder order _ ->
    let payment = {Tab = order.Tab; Amount = orderAmount order}
    event :: [OrderServed (order, payment)] |> ok
  | _ -> [event] |> ok
| ServedOrder _ -> OrderAlreadyServed |> fail
| OpenedTab _ -> CanNotServeForNonPlacedOrder |> fail
| ClosedTab _ -> CanNotServeWithClosedTab |> fail
| _ -> failwith "TODO"

let handlePrepareFood food tabId = function
| PlacedOrder order ->
  match food with
  | NonOrderedFood order _ ->
    CanNotPrepareNonOrderedFood food |> fail
  | _ -> [FoodPrepared (food, tabId)] |> ok
| ServedOrder _ -> OrderAlreadyServed |> fail
| OpenedTab _ -> CanNotPrepareForNonPlacedOrder |> fail
| ClosedTab _ -> CanNotPrepareWithClosedTab |> fail  
| _ -> failwith "TODO"

let execute state command =
    match command with
    | OpenTab tab -> handleOpenTab tab state
    | PlaceOrder order -> handlePlaceOrder order state
    | ServeDrink (drink, tabId) -> handleServeDrink drink tabId state
    | PrepareFood (food, tabId) -> handlePrepareFood food tabId state
    | _ -> failwith "Todo"

let evolve state command =
  match execute state command with
  | Ok (events,_) ->
    let newState = List.fold apply state events
    (newState, events) |> ok
  | Bad err -> Bad err