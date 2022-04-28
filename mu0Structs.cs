using AnimatorAsCode.V0;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using DeltaNeverUsed.mu0CPU.Functions;

namespace DeltaNeverUsed.mu0CPU.Structs
{
    public struct memory
    {
        public memory_word[] words;

        public memory init(AacFlLayer layer, int mem_size)
        {
            words = new memory_word[mem_size];
            for (int i = 0; i < mem_size; i++) { words[i].init(layer); }
            return this;
        }
    }

    public struct memory_word
    {
        public AacFlBoolParameter[] bits;

        public memory_word init(AacFlLayer layer)
        {
            bits = new AacFlBoolParameter[16];
            int mem_id = funcs.get_mem_id();
            for (int i = 0; i < 16; i++)
            {
                bits[i] = layer.BoolParameter($"{mem_id}_mw{i}");
            }
            return this;
        }
    }
}