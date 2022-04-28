#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using AnimatorAsCodeFramework.Examples;
using AnimatorAsCode.V0;
using System.Collections.Generic;

using DeltaNeverUsed.mu0CPU.Structs;
using DeltaNeverUsed.mu0CPU.Functions;

public class mu0 : MonoBehaviour
{
    public VRCAvatarDescriptor avatar;
    public AnimatorController assetContainer;
    public string assetKey;
    [Space(10)]
    public int mem_size = 8;
}

namespace DeltaNeverUsed.mu0CPU
{
    [CustomEditor(typeof(mu0), true)]
    public class mu0Editor : Editor
    {
        private mu0 my;
        private AacFlBase aac;

        private AacFlLayer fx;

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
        }

        private void Create()
        {
            my = (mu0)target;
            aac = AacExample.AnimatorAsCode("mu0", my.avatar, my.assetContainer, my.assetKey);

            aac.RemoveAllMainLayers();

            fx = aac.CreateMainFxLayer();
            mu0HelperFunctions.init(fx);
            

            memory mem = new memory().init(fx, my.mem_size);

            memory_word IR = new memory_word().init(fx);

            memory_word PC = new memory_word().init(fx);

            memory_word ACC = new memory_word().init(fx);
            memory_word reg_A = new memory_word().init(fx);

            AacFlBoolParameter adder_mode = fx.BoolParameter("adder_mode");

            var start = fx.NewState("Start");

            var exit = fx.NewState("exit");
            exit.Exits().AfterAnimationFinishes();

            var load_IR = mu0HelperFunctions.copy_from_mem_to_word(fx.NewSubStateMachine("load_IR"), IR, mem, PC);

            //mu0HelperFunctions.copy_word(test, IR, ACC).Exits();

            start.TransitionsTo(load_IR).When(fx.BoolParameter("Enabled").IsTrue());

            var load_reg_A = mu0HelperFunctions.copy_from_mem_to_word(fx.NewSubStateMachine("load_reg_A"), reg_A, mem, IR);
            mu0HelperFunctions.set_conditions_for_OP(load_reg_A, load_IR, opcodes["ADD"], IR);
            mu0HelperFunctions.set_conditions_for_OP(load_reg_A, load_IR, opcodes["SUB"], IR);

            var ALU = mu0HelperFunctions.create_alu(fx.NewSubStateMachine("ALU"), adder_mode, ACC, reg_A, IR); // Lummpy__Bunzz
            load_reg_A.TransitionsTo(ALU);

            ALU.TransitionsTo(exit);



            //mu0HelperFunctions.copy_word(fx, ACC, mem.words[0]);

        }
    }
}

#endif