﻿module SequenceAlignment.Tests

open Xunit
open FsCheck.Xunit

let p = fun (x:int) -> -1. - (1. * float x)
let sim (x,y) = if x = y then 2. else 0.

let shouldEqual (x : 'a) (y : 'a) = Assert.Equal<'a>(x, y)

[<Property>]
let ``Gotoh - removing breaks gives input`` (fstSeq : Sequence, sndSeq : Sequence) =
    let _,sequence =  Gotoh.run((fstSeq,sndSeq), sim, p)
    let f,s = sequence |> List.unzip
    f |> List.filter ((<>) Break) = (fstSeq |> Array.toList |> List.map Nucl) &&
    s |> List.filter ((<>) Break) = (sndSeq |> Array.toList |> List.map Nucl)

[<Property>]
let ``Gotoh - alignment is correct`` (fstSeq : Sequence, sndSeq : Sequence) =
    let alignment,sequence =  Gotoh.run((fstSeq,sndSeq), sim, p)
    let rec count (sequence,breakLength,sum) =
        match sequence with 
        | [] -> 
            let penalty = if breakLength > 0 then p(breakLength) else 0.
            sum + penalty
        | (Break,_) :: t | (_,Break) :: t ->
            count(t,breakLength+1,sum)
        | (f,s) :: t ->
            let penalty = if breakLength > 0 then p(breakLength) else 0.
            count(t,0,sum + penalty + sim(f,s))

    count(sequence,0,0.) = alignment

[<Property>]
let ``Gotoh - no double breaks in sequence`` (fstSeq : Sequence, sndSeq : Sequence) =
    let _,sequence =  Gotoh.run((fstSeq,sndSeq), sim, p)
    sequence
    |> List.forall (fun (f,s) -> not (f = Break && s = Break))
//
//[<Property>]
//let ``Hirschberg - removing breaks gives input`` (fstSeq : Sequence, sndSeq : Sequence, indelCost : float) =
//    let _,sequence =  Hirschberg.run(fstSeq,sndSeq, sim, indelCost)
//    let f,s = sequence |> List.unzip
//    f |> List.filter ((<>) Break) = (fstSeq |> Array.toList |> List.map Nucl) &&
//    s |> List.filter ((<>) Break) = (sndSeq |> Array.toList |> List.map Nucl)
//
//
//[<Property>]
//let ``Hirschberg - no double breaks in sequence`` (fstSeq : Sequence, sndSeq : Sequence, indelCost : float) =
//    let _,sequence =  Hirschberg.run(fstSeq,sndSeq, sim, indelCost)
//    sequence
//    |> List.forall (fun (f,s) -> not (f = Break && s = Break))

[<Fact>]
let ``Needleman-Wunsch gives correct result`` () =
    let expected =
        [[   0;  -2;  -4;  -6;  -8; -10]
         [  -2;  -1;   0;  -2;  -4;  -6]
         [  -4;  -3;  -2;  -1;   0;  -2]
         [  -6;  -2;  -4;   0;  -2;  -1]
         [  -8;  -4;   0;  -2;  -1;  -3]
         [ -10;  -6;  -2;  -1;  -3;   1]
         [ -12;  -8;  -4;  -3;   1;  -1]
         [ -14; -10;  -6;  -5;  -1;   3]
         [ -16; -12;  -8;  -7;  -3;   1]] |> List.map (List.map float) |> array2D
    
    let sim (a,b) = if a = b  then 2. else -1.

    Assert.Equal(expected, Hirschberg.NeedlemanWunsch([|A;G;T;A;C;G;C;A|],[|T;A;T;G;C|],sim,-2.))

let parse = function '-' -> Break | 'A' -> Nucl A | 'C' -> Nucl C | 'G' -> Nucl G | 'T' -> Nucl T | _ -> failwith "parse error" 

[<Fact>]
let ``Hirschberg gives correct result`` () =
    let expectedFst = "AGTACGCA"
    let expectedSnd = "--TATGC-"
        
    let sim (a,b) = if a = b  then 2. else -1.
    
    let a,seq = Hirschberg.run([|A;G;T;A;C;G;C;A|],[|T;A;T;G;C|],sim,-2.)
    let fstSeq,sndSeq = seq |> List.unzip

    Assert.Equal(1., a)
    Program.formatSeq fstSeq |> shouldEqual expectedFst
    Program.formatSeq sndSeq |> shouldEqual expectedSnd


