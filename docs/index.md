
***

Sequence Alignment
==================

Intro
-----
*SequenceAlignment* app contains solutions for two exercises from lessons of *Computional Biology*:

1. Optimal aligment of 2 sequences 
	* with penalty function 
	* without penalty function; but with linear memory complexity
2. Aligment of multiple sequences
	* consensus word, multialignment profile
	* alignment of 2 multialignments
	* progressive multi alignment of multiple sequences

Application has been written in F#. Source code can be found on *GitHub*: [https://github.com/theimowski/SequenceAlignment](https://github.com/theimowski/SequenceAlignment)

Application manual
------------------
This is a console app, to invoke it from command line:

	SequenceAlignment.exe <command> [-v|--verbose]

Available commands: 

* Gotoh

	Run algorithm for aligning 2 sequences with penalty function ``f(x) = -x -1``, known as *Gotoh*.

* NeedlemanWunsch

	Run Needleman-Wunsch algorithm for aligning 2 sequences without penalty function.

* Hirschberg

	Run Hirschberg algorithm for aligning 2 sequences without penalty function.

* profile

	Count profile of a multialignment.

* cons 

	Count consensus word for a multialignment.

* malign 

	Align two multialignments into one.

* UPGMA

	Run progressive multi align algorithm for multiple sequences.
	Unweighted Pair Grouping Method with Arithmetic Mean has been used for choosing two clusters to align in each step of the algorithm.

Flags:

* [*-v|--verbose*]
	
	Writes steps of current algorithm to the standard output.

Input data format
-----------------

The application operates only on the following alphabet of Nucletoids : [A;C;G;T]

Program requires similarity matrix on the standard input. Given:

	 1;
	 0; 1;
	 0; 0; 1;
	 0; 0; 0; 1;
	 0; 0; 0; 0; 1;

The similarity matrix is parsed to the following :

| 	| A | C | G | T | - |
| - | - | - | - | - | - |
| A | 1 | 0 | 0 | 0 | 0 |
| C | 0 | 1 | 0 | 0 | 0 |
| G | 0 | 0 | 1 | 0 | 0 |
| T | 0 | 0 | 0 | 1 | 0 |
| - | 0 | 0 | 0 | 0 | 1 |

For *Gotoh*, there's no need to lookup similarity of ``-`` (a break) with other Nucleotide, because the algorithm uses the penalty function. Therefore for *Gotoh* command the **last line of the matrix is ignored**.

Likewise, for *profile* command, the similarity matrix is **completely ignored**.

Despite the fact that the lines are being ignored, they are required for the program to run.

--- 

Next, program expects an empty line on the standard input.
List of sequences / multialignment comes afterwards.

For *Gotoh*, *NeedlemanWunsch* and *Hirschberg* commands program reads two seqences:

	ACTATGGGGTTCC
	ACCCGGGTTC

For *profile* and *cons* commands program reads multialignment:

	A-TCCC
	AGTC-C
	-GTCCC
	AGTCC-

For *malign* command program reads two multialignments, separated by new line: 

	A-TCCC
	AGTC-C
	-GTCCC
	AGTCC-

	GTCC-
	GTCCC
	-TAC-

For *UPGMA* command program reads sequences until EOF:

	ACTATGGGGTTCC
	ACCCGGGTTC
	ACCCGGGTTGTC
	ACCGGCTTAG
	CCCCGTTAGCC

---

Example of correct program execution:

	.\SequenceAlignment.exe Hirschberg

Standard input:

	 2
	 0; 2
	 0; 0; 2
	 0; 0; 0; 2
	-1;-1;-1;-1; 2

	ACTGATTAA
	ATAA

Standard output:

	ACTGATTAA
	A-----TAA

Description of the algorithms
-----------------------------