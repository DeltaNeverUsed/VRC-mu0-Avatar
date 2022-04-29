#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using AnimatorAsCodeFramework.Examples;
using AnimatorAsCode.V0;
using System.Collections.Generic;
using System;

using DeltaNeverUsed.mu0CPU.Structs;
using DeltaNeverUsed.mu0CPU.Functions;

public class mu0 : MonoBehaviour
{
    public VRCAvatarDescriptor avatar;
    public AnimatorController assetContainer;
    public string assetKey;
    [Space(10)]
    public int mem_size = 8;
    public string prog = "";

    [HideInInspector] 
    public memory mem; // not a fan
    [HideInInspector]
    public AacFlLayer fx;
}

namespace DeltaNeverUsed.mu0CPU
{
    [CustomEditor(typeof(mu0), true)]
    public class mu0Editor : Editor
    {
        private mu0 my;
        private AacFlBase aac;

        private Dictionary<string, bool[]> opcodes = new Dictionary<string, bool[]>()
        {
            { "LDA", new bool[] { false, false, false, false } },
            { "STO", new bool[] { false, false, false, true  } },
            { "ADD", new bool[] { false, false, true , false } },
            { "SUB", new bool[] { false, false, true , true  } },
            { "JMP", new bool[] { false, true , false, false } },
            { "JGE", new bool[] { false, true , false, true  } },
            { "JNE", new bool[] { false, true , true , false } },
            { "STP", new bool[] { false, true , true , true  } }
        };

        public override void OnInspectorGUI()
        {
            var prop = serializedObject.FindProperty("assetKey");
            if (prop.stringValue.Trim() == "")
            {
                prop.stringValue = GUID.Generate().ToString();
                serializedObject.ApplyModifiedProperties();
            }

            DrawDefaultInspector();

            if (GUILayout.Button("Create"))
            {
                Create();
            }
            if (GUILayout.Button("Load Program"))
            {
                Load();
            }
        }

        private void Load()
        {
            my = (mu0)target;

            bool[] int4_to_bool(int input) // same code as in Helper Functions for for a whole word
            {
                bool[] bools = new bool[4];

                for (int i = 0; i < 4; i++)
                {
                    int temp = input >> i;
                    bools[i] = Convert.ToBoolean(temp & 1);
                }

                return bools;
            }

            var aac = AacExample.AnimatorAsCode("mu0", my.avatar, my.assetContainer, my.assetKey);
            var load = aac.CreateSupportingFxLayer("Load Prog");

            var text = my.prog.Replace(" ", "");
            if (text.Length/4 > my.mem_size)
            {
                Debug.LogError("Input Program is bigger than memory");
                return;
            } else if (text.Length / 4 < my.mem_size)
            {
                Debug.Log("Input Program is smaller than memory.. padding with zeros..");
                text = text.PadRight(my.mem_size * 4, '0');
            }

                int pos = 0;
            int a = 0; // i'm lazy
            foreach (char hex in text)
            {
                if (a > 15) { a = 0; }
                var hex_to_int = int.Parse(hex.ToString(), System.Globalization.NumberStyles.HexNumber);
                var int_to_bool = int4_to_bool(hex_to_int);
                Array.Reverse(int_to_bool);

                foreach (bool bit in int_to_bool)
                {
                    var parameters = my.assetContainer.parameters;
                    //Debug.Log($"{(float)pos / 4f}_mw{a}");
                    load.OverrideValue(load.BoolParameter($"{Math.Floor((float)pos / 4f)}_mw{a}"), bit);
                    a++;
                }
                
                pos++;
            }
        }

        private void Create()
        {
            my = (mu0)target;
            aac = AacExample.AnimatorAsCode("mu0", my.avatar, my.assetContainer, my.assetKey);

            aac.RemoveAllMainLayers();

            var fx = my.fx;
            fx = aac.CreateMainFxLayer();
            mu0HelperFunctions.init(fx);


            memory mem = my.mem;
            mem = new memory().init(fx, my.mem_size);

            memory_word IR = new memory_word().init(fx);

            memory_word PC = new memory_word().init(fx);

            memory_word ACC = new memory_word().init(fx);
            memory_word reg_A = new memory_word().init(fx);

            AacFlBoolParameter adder_mode = fx.BoolParameter("adder_mode");

            var start = fx.NewState("Start");

            var load_IR = mu0HelperFunctions.copy_from_mem_to_word(fx.NewSubStateMachine("load_IR"), IR, mem, PC);

            var exit = mu0HelperFunctions.create_counter(fx.NewSubStateMachine("PC++"), PC);
            exit.Exits();

            start.TransitionsTo(load_IR).When(fx.BoolParameter("Enabled").IsTrue());

            // Loading and saving the ACC
            var load_ACC = mu0HelperFunctions.copy_from_mem_to_word(fx.NewSubStateMachine("load_ACC"), ACC, mem, IR).Shift(load_IR, 0, 1).Shift(load_IR, 1, -3);
            mu0HelperFunctions.set_conditions_for_OP(load_ACC, load_IR, opcodes["LDA"], IR);

            var store_ACC = mu0HelperFunctions.copy_from_word_to_mem(fx.NewSubStateMachine("store_ACC"), mem, ACC, IR);
            mu0HelperFunctions.set_conditions_for_OP(store_ACC, load_IR, opcodes["STO"], IR);

            
            var load_reg_A = mu0HelperFunctions.copy_from_mem_to_word(fx.NewSubStateMachine("load_reg_A"), reg_A, mem, IR); // gonna be using this for like everything
            mu0HelperFunctions.set_conditions_for_OP(load_reg_A, load_IR, opcodes["ADD"], IR);
            mu0HelperFunctions.set_conditions_for_OP(load_reg_A, load_IR, opcodes["SUB"], IR);

            // Adding and subtracting
            var ALU = mu0HelperFunctions.create_alu(fx.NewSubStateMachine("ALU"), adder_mode, ACC, reg_A, IR).Shift(load_reg_A, 1, -3); ; // Lummpy__Bunzz
            mu0HelperFunctions.set_conditions_for_OP(ALU, load_reg_A, opcodes["ADD"], IR);
            mu0HelperFunctions.set_conditions_for_OP(ALU, load_reg_A, opcodes["SUB"], IR);

            // too lazy to rewrite some code so this is ugly and slow
            var jump = fx.NewSubStateMachine("JMP"); 
            var JMP = mu0HelperFunctions.copy_word(jump, PC, IR);
            var jump_ge_0 = fx.NewSubStateMachine("JGE");
            var JGE = mu0HelperFunctions.copy_word(jump_ge_0, PC, IR);
            var jump_not_0 = fx.NewSubStateMachine("JNE");
            var JNE = mu0HelperFunctions.copy_word(jump_not_0, PC, IR);

            mu0HelperFunctions.jump_conditions(JGE, jump_ge_0, false, ACC);
            mu0HelperFunctions.jump_conditions(JNE, jump_not_0, true, ACC);
            jump_ge_0.Exits(); // exits if the above conditions don't return true
            jump_not_0.Exits();

            JMP.Exits();
            JGE.Exits();
            JNE.Exits();
            mu0HelperFunctions.set_conditions_for_OP(jump, load_IR, opcodes["JMP"], IR);
            mu0HelperFunctions.set_conditions_for_OP(jump_ge_0, load_IR, opcodes["JGE"], IR);
            mu0HelperFunctions.set_conditions_for_OP(jump_not_0, load_IR, opcodes["JNE"], IR);

            // just turn of enable when the stop instruction is used
            var STP = fx.NewState("STP").Drives(fx.BoolParameter("Enabled"), false);
            mu0HelperFunctions.set_conditions_for_OP_to_state(STP, load_IR, opcodes["STP"], IR);
            STP.Exits().AfterAnimationFinishes();

            load_ACC.TransitionsTo(exit);
            store_ACC.TransitionsTo(exit);
            jump.TransitionsTo(exit);
            ALU.TransitionsTo(exit);

        }
    }
}

#endif