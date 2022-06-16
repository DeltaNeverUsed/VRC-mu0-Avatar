using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using DeltaNeverUsed.mu0CPU.Functions;

public class mu0Debug : MonoBehaviour
{
    public Animator animator;
    public mu0 mu0Object;

    private TextMesh _text;

    private void Start()
    {
        _text = GetComponent<TextMesh>();
    }

    private void Update()
    {
        ushort IR  = (ushort)funcs.bool_to_int(get_word(mu0Object.mem_size));
        ushort PC  = (ushort)funcs.bool_to_int(get_word(mu0Object.mem_size+1));
        ushort ACC = (ushort)funcs.bool_to_int(get_word(mu0Object.mem_size+2));
        
        PC = (ushort)(PC << 4);
        PC = (ushort)(PC >> 4);
        
        _text.text = $"PC      : {PC:X4}, {PC:D6}\n" +
                     $"Inst    : {(ushort)(IR >> 12):X1}   , {(ushort)(IR >> 12):D2}\n" +
                     $"InstData:  {(ushort)(IR << 4):X3},   {(ushort)(IR << 4):D4}\n" +
                     $"ACC     : {ACC:X4}, {ACC:D6}\n";
    }

    private bool[] get_word(int index)
    {
        bool[] bools = new bool[16];
        for (int i = 0; i < 16; i++)
        {
            bools[i] = animator.GetBool($"{index}_mw{i}");
        }

        return bools;
    }
    
    
}