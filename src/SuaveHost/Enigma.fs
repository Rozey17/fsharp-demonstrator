﻿module SuaveHost.EnigmaApi

type RotorRequest = { RotorId : int; WheelPosition : char; RingSetting : char }
type PlugboardMapping = { From : char; To : char }
type Configuration = {
    ReflectorId : int
    Left : RotorRequest
    Middle : RotorRequest
    Right : RotorRequest
    PlugBoard : PlugboardMapping array }
type TranslationRequest =
    { Character : char
      CharacterIndex : int
      Configuration : Configuration }
type TranslationResponse =
    { Character : char
      Left : string
      Middle : string
      Right : string }

open Enigma
open System

let getReflector reflectorId =
    match reflectorId with
    | 1 -> Components.ReflectorA
    | 2 -> Components.ReflectorB
    | _ -> failwith "Unknown Reflector Id. Must be either 1 or 2."
    |> fun (Reflector x) -> x

let getRotor rotorId =
    Components.Rotors
    |> List.tryFind(fun rotor -> rotor.ID = rotorId)
    |> function
    | None -> failwith "Invalid rotor. Must be between 1 and 8."
    | Some rotor -> rotor

/// Generates an Enigma machine from a public request.
let toEnigma (request:TranslationRequest) =   
    let getPlugboard (plugboard:PlugboardMapping array) =
        let duplicates =
            plugboard
            |> Array.collect(fun pb -> [| pb.From; pb.To |])
            |> Array.countBy id
            |> Array.exists (snd >> ((<>) 1))
        if duplicates then failwith "Found duplicates in the Plugboard mapping."
        plugboard
        |> Array.map(fun pb -> String [| pb.From; pb.To |])
        |> String.concat " "

    { defaultEnigma with 
        Reflector = getReflector request.Configuration.ReflectorId |> Reflector
        Left = getRotor request.Configuration.Left.RotorId
        Middle = getRotor request.Configuration.Middle.RotorId
        Right = getRotor request.Configuration.Right.RotorId }
    |> withPlugBoard (getPlugboard request.Configuration.PlugBoard)
    |> withWheelPositions request.Configuration.Left.WheelPosition request.Configuration.Middle.WheelPosition request.Configuration.Right.WheelPosition
    |> withRingSettings request.Configuration.Left.RingSetting request.Configuration.Middle.RingSetting request.Configuration.Right.RingSetting

/// Translates an API request.
let performTranslation (request:TranslationRequest) : TranslationResponse =
    let enigma = request |> toEnigma |> moveForwardBy request.CharacterIndex
    let translatedCharacter, newEnigma =
        Operations.translateChar enigma request.Character

    { Character = translatedCharacter
      Left = String newEnigma.Left.Mapping
      Middle = String newEnigma.Middle.Mapping
      Right = String newEnigma.Right.Mapping }
