﻿module SequenceAlignment.Tests

open Xunit
open FsCheck
open FsCheck.Xunit
open Xunit.Extensions
open System

let sim' (x,y) = 
        match x,y with
        | Break,_ | _,Break -> -2.
        | a,b when a = b -> 2.
        | _ -> 0.
let sim (x,y) = if x = y then 2. else 0.

let shouldEqual (x : 'a) (y : 'a) = Assert.Equal<'a>(x, y)

[<Property>]
let ``Gotoh - removing breaks gives input`` (fstSeq : Sequence, sndSeq : Sequence) =
    let _,sequence =  Gotoh.run(fstSeq,sndSeq, sim)
    let f,s = sequence |> List.unzip
    f |> List.filter ((<>) Break) = (fstSeq |> Array.toList |> List.map Nucl) &&
    s |> List.filter ((<>) Break) = (sndSeq |> Array.toList |> List.map Nucl)

[<Property>]
let ``Gotoh - alignment is correct`` (fstSeq : Sequence, sndSeq : Sequence) =
    let alignment,sequence =  Gotoh.run(fstSeq,sndSeq, sim)
    let rec count (sequence,breakLength,sum) =
        match sequence with 
        | [] -> 
            let penalty = if breakLength > 0 then Gotoh.p(breakLength) else 0.
            sum + penalty
        | (Break,_) :: t | (_,Break) :: t ->
            count(t,breakLength+1,sum)
        | (f,s) :: t ->
            let penalty = if breakLength > 0 then Gotoh.p(breakLength) else 0.
            count(t,0,sum + penalty + sim(f,s))

    count(sequence,0,0.) = alignment

[<Property>]
let ``Gotoh - no double breaks in sequence`` (fstSeq : Sequence, sndSeq : Sequence) =
    let _,sequence =  Gotoh.run(fstSeq,sndSeq, sim)
    sequence
    |> List.forall (fun (f,s) -> not (f = Break && s = Break))

[<Property>]
let ``Needleman-Wunsch - removing breaks gives input`` (fstSeq : Sequence, sndSeq : Sequence) =
    let _,sequence =  NeedlemanWunsch.run(fstSeq,sndSeq, sim')
    let f,s = sequence |> List.unzip
    f |> List.filter ((<>) Break) = (fstSeq |> Array.toList |> List.map Nucl) &&
    s |> List.filter ((<>) Break) = (sndSeq |> Array.toList |> List.map Nucl)

[<Property>]
let ``Needleman-Wunsch - no double breaks in sequence`` (fstSeq : Sequence, sndSeq : Sequence) =
    let _,sequence =  NeedlemanWunsch.run(fstSeq,sndSeq, sim')
    sequence
    |> List.forall (fun (f,s) -> not (f = Break && s = Break))


[<Property>]
let ``Needleman-Wunsch last-row equals last row of 2D array`` (fstSeq : Sequence, sndSeq : Sequence) =
    let a2d =  NeedlemanWunsch.runScore(fstSeq,sndSeq,sim')
    let a = NeedlemanWunsch.runScoreLastRow(fstSeq,sndSeq,sim')

    a |> shouldEqual (a2d.[Array2D.length1 a2d-1,*])

[<Property>]
let ``Hirschberg - split doesn't return empty partition`` 
    (fstSeq : Sequence, sndSeq : Sequence) =
    let notEmpty = Array.isEmpty >> not
    
    (fstSeq |> notEmpty && sndSeq |> notEmpty && (fstSeq.Length > 1 && sndSeq.Length > 1)) ==>
    
    let f1,f2,s1,s2 = Hirschberg.split(fstSeq,sndSeq,sim')
    (f1 |> notEmpty || s1 |> notEmpty) && (f2 |> notEmpty || s2 |> notEmpty)

[<Property>]
let ``Hirschberg - removing breaks gives input`` (fstSeq : Sequence, sndSeq : Sequence) =
    let _,sequence =  Hirschberg.run(fstSeq,sndSeq, sim')
    let f,s = sequence |> List.unzip
    f |> List.filter ((<>) Break) = (fstSeq |> Array.toList |> List.map Nucl) &&
    s |> List.filter ((<>) Break) = (sndSeq |> Array.toList |> List.map Nucl)


[<Property>]
let ``Hirschberg - no double breaks in sequence`` (fstSeq : Sequence, sndSeq : Sequence) =
    let _,sequence =  Hirschberg.run(fstSeq,sndSeq, sim')
    sequence
    |> List.forall (fun (f,s) -> not (f = Break && s = Break))

[<Fact>]
let ``Needleman-Wunsch gives correct score`` () =
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
    
    let sim' (x,y) = 
        match x,y with
        | Break,_ | _,Break -> -2.
        | a,b when a = b -> 2.
        | _ -> -1.

    Assert.Equal(expected, NeedlemanWunsch.runScore([|A;G;T;A;C;G;C;A|],[|T;A;T;G;C|],sim'))


[<Theory>]
[<InlineData(
    "AGTACGCA",
    "TATGC",
    -2.,

    1.,
    "AGTACGCA",
    "--TATGC-")>]

[<InlineData(
    "ACTGACCT",
    "TGTCC",
    -1.,

    4.,
    "ACTGACCT",
    "--TGTCC-")>]
let ``Needleman-Wunsch gives correct result`` (in1, in2, indelCost, expectedSim, expectedFst, expectedSnd) =
    let sim' (x,y) = 
        match x,y with
        | Break,_ | _,Break -> indelCost
        | a,b when a = b -> 2.
        | _ -> -1.

    let a,seq = NeedlemanWunsch.run(Program.parseLine in1, Program.parseLine in2, sim')
    let fstSeq,sndSeq = seq |> List.unzip

    Assert.Equal(expectedSim, a)
    formatSeq fstSeq |> shouldEqual expectedFst
    formatSeq sndSeq |> shouldEqual expectedSnd

let toMultiAlignment (x: string) = 
    x.Split([|Environment.NewLine|], StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun s -> s.Trim())
    |> Array.map (Seq.map Program.parse')
    |> array2D

[<Theory>]
[<InlineData(
    "AGTACGCA",
    "TATGC",
    -2.,

    1.,
    "AGTACGCA",
    "--TATGC-")>]

[<InlineData(
    "ACTGACCT",
    "TGTCC",
    -1.,

    4.,
    "ACTGACCT",
    "--TGTCC-")>]

[<InlineData(
    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
    -1.,

    1080.,
    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT",
    "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAATTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTTT")>]
let ``Hirschberg gives correct result`` (in1, in2, indelCost, expectedSim, expectedFst, expectedSnd) =
            
    let sim' (x,y) = 
        match x,y with
        | Break,_ | _,Break -> indelCost
        | a,b when a = b -> 2.
        | _ -> -1.

    let a,seq = Hirschberg.run(Program.parseLine in1, Program.parseLine in2, sim')
    let fstSeq,sndSeq = seq |> List.unzip

    Assert.Equal(expectedSim, a)
    formatSeq fstSeq |> shouldEqual expectedFst
    formatSeq sndSeq |> shouldEqual expectedSnd




[<Theory>]
[<InlineData(
    """
AGC-A
AGAGA
ACCG-
CG-GC
""",

    "AGCGA")>]
let ``Consensus word gives correct result`` (input: string, expected) =
    let sim (a,b) = if a = b  then 2. else -1.

    let malign = input |> toMultiAlignment 

    let word = 
        MultiAlign.consensusWord(malign, sim)
        |> Array.map formatNucl
        |> String.concat ""

    word |> shouldEqual expected


let formatMultiAlignment (a : MultiAlignment) =
    [0..Array2D.length1 a - 1]
    |> List.map (fun i -> a.[i,*])
    |> List.map (Array.map formatNucl)
    |> List.map (String.concat "")
    |> String.concat Environment.NewLine

[<Theory>]
[<InlineData(
    """
TAG
CAT
""",
    """
TC-
AGG
""",

    """
TAG-
CAT-
-TC-
-AGG
""")>]

[<InlineData(
    """
TAG
CAT
T-G
""",
    """
TC-
AGG
ATC
""",

    """
TAG-
CAT-
T-G-
-TC-
-AGG
-ATC
""")>]
let ``Align by profiles gives correct result`` (input1: string, input2: string, expected : string) =
    let sim (a,b) = if a = b  then 2. else -1.

    let malign1 = input1 |> toMultiAlignment 
    let malign2 = input2 |> toMultiAlignment 

    
    let result = MultiAlign.alignByProfiles(malign1,malign2,sim)

    result |> shouldEqual (expected |> toMultiAlignment)


let trim (x:string) = 
        let chars = Environment.NewLine.ToCharArray()
        x.TrimEnd(chars).TrimStart(chars)


[<Theory>]
[<InlineData(
    """
TAG
CAT
TGT
""",

    """
TAG-
T-GT
CA-T
""")>]
[<InlineData(
    """
GCTTGTCCGTTACGAT
ACTTGTCTGTTACGAT
ACTTGTCCGAAACGAT
ACTTGACCGTTTCCTT
AGATGACCGTTTCGAT
ACTACACCCTTATGAG
""",

    """
GCTT-GTCCGTTACGAT
ACTT-GTCTGTTACGAT
ACTT-GTCCGAAACGAT
ACTT-GACCGTTTCCTT
AGAT-GACCGTTTCGAT
AC-TACACCCTTATGAG
""")>]
let ``Progressive multi alignment gives correct result`` (input: string,  expected : string) =
    let sim (a,b) = if a = b  then 1. else 0.

    let seqs =
        input.Split([|Environment.NewLine|], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map Program.parseLine

    let result = MultiAlign.UPGMA(seqs, sim) |> formatMultiAlignment
    result |> trim |> shouldEqual (expected |> trim)
    
    MultiAlign.score(input |> toMultiAlignment, sim) <= MultiAlign.score(expected |> toMultiAlignment, sim) |> shouldEqual true