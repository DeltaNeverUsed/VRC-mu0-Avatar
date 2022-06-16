
# VRC mu0 CPU avatar

A mu0 CPU on a VRChat avatar run entirely within the animator controller
## Features
- a 16*16 monochrome display ✓ This maps to the last 16 words of memory
- toggleable bits on the display ✓
- Original mu0 Instructions
    - 0000 LDA S ✓
    - 0001 STO S ✓
    - 0010 ADD S ✓
    - 0011 SUB S ✓
    - 0100 JMP S ✓
    - 0101 JGE S ✗ negative numbers are not implemented
    - 0110 JNE S ✓
    - 0111 STP ✓
- Extended Instructions
    - 0101 SA S ✓ Shift acc left or right depending on S
    - 1000 LDR S ✓ Loads a register into ACC. Current Registers ↓
        - 0x000 ACC
## Installation

Requirements
- [Animator As Code](https://github.com/hai-vr/av3-animator-as-code/tree/pr-sub-state-machines-rebase) make sure it's the sub state machine branch.
    and you'll need to apply this [patch]( https://github.com/hai-vr/av3-animator-as-code/commit/8e15564f019fa14ec20091c6e9c4bc4928930fbb) since the parameter drive branch hasn't been fully updated at the time of writing
- [ Av3Emulator](https://github.com/lyuma/Av3Emulator) [Optional] Only for debugger

Install
1) Clone the GitHub repo into your unity project
2) Add the mu0 script to an empty object in your scene hierarchy outside your avatar
    - (optional) Add the screen object from the mu0 folder onto your avatar 
3) Fill out the fields
    - Avatar: Your avatar's VRCAvatarDescriptor
    - Asset Container: Your FX controller
    - Screen (optional): The screen mesh
    - Mem_size: The amount of memory you want
    - Prog (optional): The program you want to load in Hex into the memory (not used while creating the animations)
4) Hit Create to create the animations for all the logic
5) Create Screen (optional)
    - Hit Create Screen to create all the animations for displaying the last 16 words of memory on the display
    - Hit Create Contacts for display to make said words toggle-able 
6) Load your program by hitting Load Program (optional)

## Writing Programs

There are 3 registers (plus an extra internal one you don't have to worry about)
1) ACC: the accumulator, this is the number you're currently operating on
    - LDA S: Loads memory address S into the ACC
    - STO S: Stores the ACC into memory address S
    - ADD S: Adds memory address S to the current value in the ACC
    - SUB S: Subtracts memory address S from the current value in the ACC
    - SA  S: Bit shifts whatever in the ACC left or right by 1 bit depending on S (0 is right, 1 is left)
2) PC: the program counter
    - JMP S: Sets the program counter to S
    - JNE S: Same as JMP, but only if the ACC is not equal to 0
3) Carry: The carry flag
    - LDR 0x000: loads the carry flag into the ACC

You can write programs in byte code, but i wouldn't recommend it. \
Therefor I wrote an [assembler](https://github.com/DeltaNeverUsed/mu0-assembler)
for this cpu to make the task of programming it easier.

This computer uses the Von Neumann Architecture, meaning your program inhibits the same space as your variables,
this allows things such as self changing code compared to the Harvard Architecture where your program and variables are separated,
which doesn't allow self changing code, but that's no fun.

Since we don't have an indirect load instruction we can make our own by modifying the code at runtime!

```asm
.Start
    lda lda_array       ; Loads the lda instruction into the ACC
    add Index           ; Loads the Offset into the ACC
    sto inst_to_replace ; Now we store the offset lda instruction in the next memory address after this instruction 
.inst_to_replace
    0x0                 ; This would then get replaced by 0x0008 or the assembly equivalent lda 0x008
    stp                 ; Stops the program
    
.lda_array    ; This isn't needed since lda is 0x0, We could just load Array directly into memory
    lda Array ; But this is here becuase you could replace lda with jmp to instead jump into the array
    
.Index ; This is the index we want to get into Array
    2  ; So we want the second element 

.Array ; Some array you want to index into
    0x0 0x1 0x2 0x3 0x4 
```

For more examples have a look [here](https://github.com/DeltaNeverUsed/mu0-assembler/tree/main/progs)

## Using the Debugger

The Debugger only works in the editor and requires Av3Emulator by lyuma. \
Start of by adding the debugger prefab into your scene, then fill out the parameters on it. \
Position it, hit play, and there you go!

![Debugger](/Images/Debugger.png)

Since only 1 nibble is used for the actual instruction it's split into Inst and InstData. \
Inst is the instruction \
InstData is the address that instruction is accessing