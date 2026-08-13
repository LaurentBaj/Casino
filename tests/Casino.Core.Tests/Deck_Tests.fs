module Casino.Core.Tests.DeckTests

open Xunit
open FsUnit.Xunit
open Casino.Core.DeckCreation
open Casino.Core.Models

[<Fact>]
let ``Deck contains 52 unique cards`` () =
    let deck = initializeDeck UnShuffled
    deck.Length |> should equal 52
    deck |> Set.ofList |> Set.count |> should equal 52

[<Fact>]
let ``Create a shuffled deck`` () =
    let unShuffledDeck = initializeDeck UnShuffled
    let shuffledDeck = initializeDeck Shuffled

    shuffledDeck |> should not' (equal unShuffledDeck)
    shuffledDeck.Length |> should equal 52

    let unShuffledSet = Set.ofList unShuffledDeck
    let shuffledSet = Set.ofList shuffledDeck
    shuffledSet |> should equal unShuffledSet
