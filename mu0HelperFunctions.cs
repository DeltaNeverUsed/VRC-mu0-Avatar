﻿using AnimatorAsCode.V0;
using UnityEngine;
using System;
using System.Collections.Generic;

using DeltaNeverUsed.mu0CPU;
using DeltaNeverUsed.mu0CPU.Structs;

public static class funcs
{
    private static int mem_amount = 0;
    private static int substate_amount = 0;
    public static int get_mem_id() { mem_amount++; return mem_amount - 1; }
    public static int get_substate_id() { substate_amount++; return substate_amount - 1; }

    private static int state_amount = 0;
    public static int get_state_id() { state_amount++; return state_amount - 1; }

    public static bool[] int12_to_bool(int input)
    {
        bool[] bools = new bool[12];

        for (int i = 0; i < 12; i++)
        {
            int temp = input >> i;
            bools[i] = Convert.ToBoolean(temp & 1);
        }

        return bools;
    }
}

namespace DeltaNeverUsed.mu0CPU.Functions
{
    public static class mu0HelperFunctions
    {
        public static AacFlLayer fx;
        public static IAacFlCondition unused;

        private static List<AacFlStateMachine> has_exit_state = new List<AacFlStateMachine>();
        private static List<AacFlState> has_exit_state_s = new List<AacFlState>();

        public static void init(AacFlLayer t_fx)
        {
            fx = t_fx;
            unused = fx.BoolParameter("UNUSED").IsFalse();
        }

        public static AacFlState exit_state(AacFlStateMachine sm)
        {
            var test = has_exit_state.IndexOf(sm);
            if (test != -1) { return has_exit_state_s[test]; }

            var exit = sm.NewState("EXIT");
            exit.Exits().AfterAnimationFinishes();

            has_exit_state.Add(sm);
            has_exit_state_s.Add(exit);

            return exit;
        }

        // Copies a boolean to another boolean
        public static AacFlStateMachine copy_bool(AacFlStateMachine sm, AacFlBoolParameter dest, AacFlBoolParameter src)
        {
            var sm_local = sm.NewSubStateMachine($"{funcs.get_substate_id()}_copy_bool");
            var one = sm_local.NewState($"{funcs.get_state_id()}_1").Drives(dest, true);
            var zero = sm_local.NewState($"{funcs.get_state_id()}_0").Drives(dest, false);

            sm_local.EntryTransitionsTo(one).When(src.IsTrue());
            sm_local.EntryTransitionsTo(zero).When(src.IsFalse());
            one.Exits().AfterAnimationFinishes();
            zero.Exits().AfterAnimationFinishes();

            return sm_local;
        }
        // Copies a memory_word to another memory_word
        public static AacFlStateMachine copy_word(AacFlStateMachine sm, memory_word dest, memory_word src)
        {
            var sm_local = sm.NewSubStateMachine($"{funcs.get_substate_id()}_copy_word");
            var last_sm = sm_local;

            for (int i = 0; i < 16; i++)
            {
                var cpb = copy_bool(sm_local, dest.bits[i], src.bits[i]);

                last_sm.TransitionsTo(cpb).When(unused);

                last_sm = cpb;
            }

            last_sm.TransitionsTo(exit_state(sm_local)).When(unused);

            return sm_local;
        }

        public static AacFlStateMachine copy_from_mem_to_word(AacFlStateMachine sm, memory_word dest, memory src, memory_word PC)
        {
            //var what = sm.NewState("No idea why i need this");
            //sm.EntryTransitionsTo(what).When(unused);

            for (int i = 0; i < src.words.Length; i++)
            {
                var cw = copy_word(sm, dest, src.words[i]);
                var temp = sm.EntryTransitionsTo(cw).WhenConditions();
                for (int l = 0; l < 12; l++)
                {
                    int l2 = 15 - l;
                    //Debug.Log(funcs.int12_to_bool(i)[l]);
                    temp.And(PC.bits[l2].IsEqualTo(funcs.int12_to_bool(i)[l]));
                }
                
            }
            return sm;
        }

        public static AacFlTransitionContinuation set_conditions_for_OP(AacFlStateMachine sm_dest, AacFlStateMachine sm_src, bool[] opcode, memory_word IR)
        {
            var conditions = sm_src.TransitionsTo(sm_dest).WhenConditions();
            for (int i = 0; i < opcode.Length; i++)
            {
                conditions.And(IR.bits[i].IsEqualTo(opcode[i]));
            }

            return conditions;
        }

        public static AacFlStateMachine create_alu(AacFlStateMachine sm, AacFlBoolParameter add_sub, memory_word ACC, memory_word reg_A, memory_word IR)
        {
            var set_add_sub = copy_bool(sm, add_sub, IR.bits[3]);

            var adder = create_adder(sm, ACC, reg_A);
            var subtracter = create_subtracter(sm, ACC, reg_A);

            set_add_sub.TransitionsTo(adder).When(add_sub.IsFalse());
            set_add_sub.TransitionsTo(subtracter).When(add_sub.IsTrue());

            adder.Exits().When(unused);
            subtracter.Exits().When(unused);

            return sm;
        }

        private static AacFlStateMachine create_subtracter(AacFlStateMachine sm, memory_word ACC, memory_word reg_A)
        {
            var sm_local = sm.NewSubStateMachine($"{funcs.get_substate_id()}_Subtracter");
            var last_sm = sm;

            for (int i = 0; i < 16; i++)
            {
                var fsub = create_full_subtracter(sm_local, fx.BoolParameter("Carry"), ACC.bits[i], reg_A.bits[i]);

                last_sm.TransitionsTo(fsub).When(unused);
                last_sm = fsub;
            }

            var reset_c = sm_local.NewState("reset_c").Drives(fx.BoolParameter("Carry"), false);
            reset_c.Exits().AfterAnimationFinishes();
            last_sm.TransitionsTo(reset_c);

            return sm_local;
        }

        private static AacFlStateMachine create_full_subtracter(AacFlStateMachine sm, AacFlBoolParameter carry, AacFlBoolParameter a, AacFlBoolParameter b)
        {
            var sm_local = sm.NewSubStateMachine($"{funcs.get_substate_id()}_Adder");

            bool[] input = new bool[]
            {
                false, false, false,
                false, false, true,
                false, true, false,
                false, true, true,
                true, false, false,
                true, false, true,
                true, true, false,
                true, true, true,
            };
            bool[] output = new bool[]
            {
                false, false,
                true, true,
                true, true,
                false, true,
                true, false,
                false, false,
                false, false,
                true, true
            };

            for (int i = 0; i < 8; i++)
            {
                var sub = sm_local.NewState($"{funcs.get_state_id()}_Subtracter");
                sub.Drives(carry, output[1 + i * 2]);
                sub.Drives(a, output[i * 2]);

                sub.TransitionsFromEntry()
                    .When(a.IsEqualTo(input[0 + i * 2]))
                    .And(b.IsEqualTo(input[1 + i * 2]))
                    .And(carry.IsEqualTo(input[2 + i * 2]));

                sub.Exits().AfterAnimationFinishes();
            }

            return sm_local;
        }

        private static AacFlStateMachine create_adder(AacFlStateMachine sm, memory_word ACC, memory_word reg_A)
        {
            var sm_local = sm.NewSubStateMachine($"{funcs.get_substate_id()}_Adder");
            var last_sm = sm;

            for (int i = 0; i < 16; i++)
            {
                var fadd = create_full_adder(sm_local, fx.BoolParameter("Carry"), ACC.bits[i], reg_A.bits[i]);

                last_sm.TransitionsTo(fadd).When(unused);
                last_sm = fadd;
            }

            var reset_c = sm_local.NewState("reset_c").Drives(fx.BoolParameter("Carry"), false);
            reset_c.Exits().AfterAnimationFinishes();
            last_sm.TransitionsTo(reset_c);

            return sm_local;
        }

        private static AacFlStateMachine create_full_adder(AacFlStateMachine sm, AacFlBoolParameter carry, AacFlBoolParameter a, AacFlBoolParameter b)
        {
            var sm_local = sm.NewSubStateMachine($"{funcs.get_substate_id()}_Adder");

            bool[] input = new bool[]
            {
                false, false, false,
                false, false, true,
                false, true, false,
                false, true, true,
                true, false, false,
                true, false, true,
                true, true, false,
                true, true, true,
            };
            bool[] output = new bool[]
            {
                false, false,
                true, false,
                true, false,
                false, true,
                true, false,
                false, true,
                false, true,
                true, true
            };

            for (int i = 0; i < 8; i++)
            {
                var add = sm_local.NewState($"{funcs.get_state_id()}_Adder");
                add.Drives(carry, output[1 + i * 2]);
                add.Drives(a, output[i * 2]);

                add.TransitionsFromEntry()
                    .When(a.IsEqualTo(input[0 + i * 2]))
                    .And(b.IsEqualTo(input[1 + i * 2]))
                    .And(carry.IsEqualTo(input[2 + i * 2]));

                add.Exits().AfterAnimationFinishes();
            }

            return sm_local;
        }
    }
}