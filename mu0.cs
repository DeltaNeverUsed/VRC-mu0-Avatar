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
using System.Linq;

public class mu0 : MonoBehaviour
{
    public VRCAvatarDescriptor avatar;
    public AnimatorController assetContainer;
    public string assetKey;

    [Header("You need atleast 16 words of ram for the screen, it maps to the last 16 words of ram")]
    public SkinnedMeshRenderer screen;
    [Space(10)]
    public int mem_size = 8;
    public string prog = "";
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
            { "SA",  new bool[] { false, true , false, true  } },
            { "JNE", new bool[] { false, true , true , false } },
            { "STP", new bool[] { false, true , true , true  } },
            { "LDR", new bool[] { true , false, false, false } }
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
            if (GUILayout.Button("Create All")) {
                EditorUtility.DisplayProgressBar("Creating Animations", "Step: 1", 1f / 3f);
                Create();
                EditorUtility.DisplayProgressBar("Creating Animations", "Step: 2", 2f / 3f);
                Create_screen();
                EditorUtility.DisplayProgressBar("Creating Animations", "Step: 3", 3f / 3f);
                CreateContractsAndLogicForDisplay();
                EditorUtility.ClearProgressBar();
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Create")) { Create(); }
            if (GUILayout.Button("Create Screen")) { Create_screen(); }
            if (GUILayout.Button("Create Contracts for display")) { CreateContractsAndLogicForDisplay(); }
            GUILayout.Space(10);
            if (GUILayout.Button("Load Program")) { Load(); }
        }

        private void CreateContractsAndLogicForDisplay()
        {
            my = (mu0)target;

            var aac = AacExample.AnimatorAsCode("mu0displayInteract", my.avatar, my.assetContainer, GUID.Generate().ToString());

            var display = aac.CreateMainFxLayer();

            var entry = display.NewState("Entry");

            var tempList = my.screen.transform.Cast<Transform>().ToList(); // taken straight from the unity forums 
            foreach (var child in tempList)
            {
                DestroyImmediate(child.gameObject);
            }

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {   
                    GameObject contact = new GameObject();
                    contact.transform.parent = my.screen.transform;
                    var cr = contact.GetOrAddComponent<VRC.SDK3.Dynamics.Contact.Components.VRCContactReceiver>();
                    cr.radius = 0.01f;
                    cr.collisionTags.Add("FingerIndex");
                    cr.receiverType = VRC.Dynamics.ContactReceiver.ReceiverType.OnEnter;
                    cr.parameter = $"DProx_X{x}_Y{y}";

                    var pos = new Vector3(0.9375f, 0, 0.9375f);
                    pos.x -= (float)x * 0.125f;
                    pos.z += (float)y * 0.125f;
                    contact.transform.localPosition = pos;
                    contact.name = $"X {x}, Y {y}";

                    for (int zo = 0; zo < 2; zo++)
                    {
                        var y_s = y + my.mem_size - 16;
                        var state = display.NewState($"X {x}, Y {y}")
                            .Drives(display.BoolParameter($"{y_s}_mw{x}"), Convert.ToBoolean(zo));
                        entry.TransitionsTo(state)
                            .When(display.BoolParameter($"DProx_X{x}_Y{y}").IsEqualTo(true))
                            .And(display.BoolParameter($"{y_s}_mw{x}").IsEqualTo(!Convert.ToBoolean(zo)));
                        state.Exits().AfterAnimationFinishes();
                    }
                }
            }
            
        }

        private void Load()
        {
            my = (mu0)target;

            bool[] int4_to_bool(int input) // same code as in Helper Functions for for a nibble
            {
                bool[] bools = new bool[4];

                for (int i = 0; i < 4; i++)
                {
                    int temp = input >> i;
                    bools[i] = Convert.ToBoolean(temp & 1);
                }

                return bools;
            }

            var aac = AacExample.AnimatorAsCode("mu0load", my.avatar, my.assetContainer, GUID.Generate().ToString());

            var load = aac.CreateSupportingFxLayer("Load Prog");

            var text = my.prog.Replace(" ", "");
            if (text.Length / 4 > my.mem_size)
            {
                Debug.LogError("Input Program is bigger than memory");
                return;
            }
            else if (text.Length / 4 < my.mem_size)
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

        private void Create_screen()
        {
            my = (mu0)target;
            if (my.mem_size < 16) { Debug.LogError("Not enough memory"); return; }

            var aac = AacExample.AnimatorAsCode("mu0screen", my.avatar, my.assetContainer, GUID.Generate().ToString());

            aac.RemoveAllSupportingLayers("DisplayMem");
            var display = aac.CreateSupportingFxLayer("DisplayMem");

            AacFlState[] last_states = new AacFlState[2];

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    var driver_param = display.BoolParameter($"{my.mem_size - 16 + y}_mw{x}");
                    var display_1 = display.NewState($"x{x}_y{y}_1")
                        .WithAnimation(aac.NewClip().Animating(clip =>
                        {
                            clip.Animates(my.screen, $"blendShape.{y}_{x}").WithOneFrame(0f);
                        }));
                    var display_0 = display.NewState($"x{x}_y{y}_0")
                        .WithAnimation(aac.NewClip().Animating(clip =>
                        {
                            clip.Animates(my.screen, $"blendShape.{y}_{x}").WithOneFrame(80f);
                        }));

                    if (y+x == 0)
                    {
                        display.EntryTransitionsTo(display_1).When(driver_param.IsEqualTo(true));
                        display.EntryTransitionsTo(display_0).When(driver_param.IsEqualTo(false));
                    } else {
                        for (int i = 0; i < 2; i++)
                        {
                            last_states[i].TransitionsTo(display_1).When(driver_param.IsEqualTo(true));
                            last_states[i].TransitionsTo(display_0).When(driver_param.IsEqualTo(false));
                        }
                    }
                    last_states[0] = display_1;
                    last_states[1] = display_0;
                }
            }
            for (int i = 0; i < 2; i++)
            {
                last_states[i].Exits().AfterAnimationFinishes();
            }
        }
        
        private void Create()
        {
            my = (mu0)target;
            var aac = AacExample.AnimatorAsCode("mu0", my.avatar, my.assetContainer, my.assetKey);

            aac.RemoveAllMainLayers();

            var fx = aac.CreateMainFxLayer();
            mu0HelperFunctions.init(fx);

            memory mem = new memory().init(fx, my.mem_size);

            memory_word IR = new memory_word().init(fx);

            memory_word PC = new memory_word().init(fx);

            memory_word ACC = new memory_word().init(fx);
            memory_word reg_A = new memory_word().init(fx);

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
            var ALU = mu0HelperFunctions.create_alu(fx.NewSubStateMachine("ALU"), ACC, reg_A, IR).Shift(load_reg_A, 1, -3); // Lummpy__Bunzz
            mu0HelperFunctions.set_conditions_for_OP(ALU, load_reg_A, opcodes["ADD"], IR);
            mu0HelperFunctions.set_conditions_for_OP(ALU, load_reg_A, opcodes["SUB"], IR);

            // shifting ACC left and right
            var shift = mu0HelperFunctions.shift(fx.NewSubStateMachine("shift"), ACC, IR);
            mu0HelperFunctions.set_conditions_for_OP(shift, load_IR, opcodes["SA"], IR);
            shift.TransitionsTo(exit);

            // loading a register into ACC
            var reg_load = mu0HelperFunctions.load_reg(fx.NewSubStateMachine("reg_load"), ACC, IR);
            mu0HelperFunctions.set_conditions_for_OP(reg_load, load_IR, opcodes["LDR"], IR);
            reg_load.TransitionsTo(exit);

            // too lazy to rewrite some code so this is ugly and slow
            var jump = fx.NewSubStateMachine("JMP"); 
            var JMP = mu0HelperFunctions.copy_word(jump, PC, IR);
            var jump_not_0 = fx.NewSubStateMachine("JNE");
            var JNE = mu0HelperFunctions.copy_word(jump_not_0, PC, IR);

            mu0HelperFunctions.jump_conditions(JNE, jump_not_0, true, ACC);
            var t2 = jump_not_0.NewState("exit if not true"); // exits if the above conditions don't return true
            t2.TransitionsTo(exit).AfterAnimationFinishes();
            jump_not_0.EntryTransitionsTo(t2);

            JMP.Exits();
            JNE.Exits();
            jump.Exits();
            jump_not_0.Exits();
            mu0HelperFunctions.set_conditions_for_OP(jump, load_IR, opcodes["JMP"], IR);
            mu0HelperFunctions.set_conditions_for_OP(jump_not_0, load_IR, opcodes["JNE"], IR);

            // just turn of enable when the stop instruction is used
            var STP = fx.NewState("STP").Drives(fx.BoolParameter("Enabled"), false);
            mu0HelperFunctions.set_conditions_for_OP_to_state(STP, load_IR, opcodes["STP"], IR);
            STP.Exits().AfterAnimationFinishes();

            load_ACC.TransitionsTo(exit);
            store_ACC.TransitionsTo(exit);
            ALU.TransitionsTo(exit);

        }
    }
}

#endif