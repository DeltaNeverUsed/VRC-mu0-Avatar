
# VRC mu0 CPU avatar

A mu0 CPU on a VRChat avatar run entirely within the animator controller
## Features
- a 16*16 monochrome display ✓
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
