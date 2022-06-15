#if UNITY_EDITOR
using AnimatorAsCode.V0;
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

    public static bool[] int_to_bool(int input, int size = 12)
    {
        bool[] bools = new bool[12];

        for (int i = 0; i < size; i++)
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
            exit.Exits().When(unused);

            has_exit_state.Add(sm);
            has_exit_state_s.Add(exit);

            return exit;
        }
        
        // Copies a memory_word to another memory_word
        public static AacFlState copy_word(AacFlStateMachine sm, memory_word dest, memory_word src, int offset = 0, bool dir = false)
        {
            var copy = sm.NewState($"{funcs.get_substate_id()}_copy_word");

            for (int i = 0; i < 16; i++)
            {
                // this is really ugly
                var a = dir ? 15 - i + offset : i + offset;
                copy.DrivingCopies((a > 15 || a < 0) ? fx.BoolParameter("UNUSED") : src.bits[a], dest.bits[dir ? 15 - i : i]);
            }

            copy.Exits().Automatically();
            return copy;
        }

        public static AacFlStateMachine copy_from_mem_to_word(AacFlStateMachine sm, memory_word dest, memory src, memory_word PC)
        {
            for (int i = 0; i < src.words.Length; i++)
            {
                var cw = copy_word(sm, dest, src.words[i]);
                var temp = sm.EntryTransitionsTo(cw).WhenConditions();
                for (int l = 0; l < 12; l++)
                {
                    int l2 = 15 - l;
                    temp.And(PC.bits[l2].IsEqualTo(funcs.int_to_bool(i)[l]));
                }
                cw.Exits();
            }
            return sm;
        }
        public static AacFlStateMachine copy_from_word_to_mem(AacFlStateMachine sm, memory dest, memory_word src, memory_word PC)
        {
            for (int i = 0; i < dest.words.Length; i++)
            {
                var cw = copy_word(sm, dest.words[i], src);
                var temp = sm.EntryTransitionsTo(cw).WhenConditions();
                for (int l = 0; l < 12; l++)
                {
                    int l2 = 15 - l;
                    temp.And(PC.bits[l2].IsEqualTo(funcs.int_to_bool(i)[l]));
                }
                cw.Exits();
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
        public static AacFlTransitionContinuation set_conditions_for_OP(AacFlState sm_dest, AacFlStateMachine sm_src, bool[] opcode, memory_word IR)
        {
            var conditions = sm_src.TransitionsTo(sm_dest).WhenConditions();
            for (int i = 0; i < opcode.Length; i++)
            {
                conditions.And(IR.bits[i].IsEqualTo(opcode[i]));
            }

            return conditions;
        }
        public static AacFlTransitionContinuation set_conditions_for_OP(AacFlTransitionContinuation conditions, bool[] opcode, memory_word IR)
        {
            for (int i = 0; i < opcode.Length; i++)
            {
                conditions.And(IR.bits[i].IsEqualTo(opcode[i]));
            }

            return conditions;
        }

        public static AacFlStateMachine create_alu(AacFlStateMachine sm, memory_word ACC, memory_word reg_A, memory_word IR)
        {
            var adder = create_adder(sm, ACC, reg_A);
            var subtracter = create_subtracter(sm, ACC, reg_A);

            sm.EntryTransitionsTo(adder).When(IR.bits[3].IsFalse());
            sm.EntryTransitionsTo(subtracter).When(IR.bits[3].IsTrue());

            adder.Exits();
            subtracter.Exits();

            return sm;
        }

        private static AacFlStateMachine create_truth_table(AacFlStateMachine sm, AacFlBoolParameter[] input_p, AacFlBoolParameter[] output_p, bool[] input_t, bool[] output_t)
        {
            var sm_local = sm.NewSubStateMachine($"{funcs.get_substate_id()}_Thruth_table");

            var state_amount = input_t.Length / input_p.Length;
            for (int state = 0; state < state_amount; state++)
            {
                var tstate = sm_local.NewState($"{funcs.get_state_id()}_Thruth_table");
                var conditions = sm_local.EntryTransitionsTo(tstate).WhenConditions();

                for (int o = 0; o < output_p.Length; o++)
                {
                    tstate.Drives(output_p[o], output_t[o + state * output_p.Length]);
                }
                for (int i = 0; i < input_p.Length; i++)
                {
                    conditions.And(input_p[i].IsEqualTo(input_t[i + state * input_p.Length]));
                }
                tstate.Exits().When(unused);
            }

            return sm_local;
        }

        private static AacFlStateMachine create_subtracter(AacFlStateMachine sm, memory_word ACC, memory_word reg_A)
        {
            var sm_local = sm.NewSubStateMachine($"{funcs.get_substate_id()}_Subtracter");
            var reset_c = sm_local.NewState("reset_c").Drives(fx.BoolParameter("Carry"), false);
            sm_local.EntryTransitionsTo(reset_c);
            var last_sm = sm_local;

            bool[] sub_input = new bool[]
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
            bool[] sub_output = new bool[]
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

            for (int i = 0; i < 16; i++)
            {
                var fsub = create_truth_table(sm_local,
                    new AacFlBoolParameter[] { ACC.bits[15 - i], reg_A.bits[15 - i], fx.BoolParameter("Carry") },
                    new AacFlBoolParameter[] { ACC.bits[15 - i], fx.BoolParameter("Carry") },
                    sub_input, sub_output
                );

                if (i != 0)
                {
                    last_sm.TransitionsTo(fsub);
                }
                else
                {
                    reset_c.TransitionsTo(fsub).When(unused);
                }
                last_sm = fsub;
            }

            last_sm.Exits();

            return sm_local;
        }

        private static AacFlStateMachine create_adder(AacFlStateMachine sm, memory_word ACC, memory_word reg_A)
        {
            var sm_local = sm.NewSubStateMachine($"{funcs.get_substate_id()}_Adder");
            var reset_c = sm_local.NewState("reset_c").Drives(fx.BoolParameter("Carry"), false);
            sm_local.EntryTransitionsTo(reset_c);
            var last_sm = sm_local;

            bool[] add_input = new bool[]
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
            bool[] add_output = new bool[]
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

            for (int i = 0; i < 16; i++)
            {
                var fadd = create_truth_table(sm_local,
                    new AacFlBoolParameter[] { ACC.bits[15 - i], reg_A.bits[15 - i], fx.BoolParameter("Carry") },
                    new AacFlBoolParameter[] { ACC.bits[15 - i], fx.BoolParameter("Carry") },
                    add_input, add_output
                );

                if (i != 0)
                {
                    last_sm.TransitionsTo(fadd);
                }
                else
                {
                    reset_c.TransitionsTo(fadd).When(unused);
                }
                last_sm = fadd;
            }

            last_sm.Exits();

            return sm_local;
        }

        public static AacFlStateMachine create_counter(AacFlStateMachine sm, memory_word PC)
        {
            var last_sm = sm;

            var reset_c = sm.NewState("reset_c").Drives(fx.BoolParameter("Counter_B"), true);
            sm.EntryTransitionsTo(reset_c);

            bool[] input = new bool[]
            {
                false, false,
                false, true,
                true, false,
                true, true,
            };
            bool[] output = new bool[]
            {
                false, false,
                true, false,
                true, false,
                false, true,
            };

            for (int i = 0; i < 16; i++)
            {
                var hadd = create_truth_table(sm,
                    new AacFlBoolParameter[] { PC.bits[15 - i], fx.BoolParameter("Counter_B") }, 
                    new AacFlBoolParameter[] { PC.bits[15 - i], fx.BoolParameter("Counter_B") },
                    input, output
                );

                if (i != 0)
                {
                    last_sm.TransitionsTo(hadd);
                }
                else
                {
                    reset_c.TransitionsTo(hadd).When(unused);
                }
                last_sm = hadd;
            }

            last_sm.Exits();

            return sm;
        }

        public static void jump_conditions(AacFlState sm_dest, AacFlStateMachine sm_src, bool and_or, memory_word ACC)
        {
            if (and_or)
            {
                for (int i = 0; i < 16; i++)
                {
                    sm_src.EntryTransitionsTo(sm_dest).When(ACC.bits[i].IsEqualTo(true));
                }
            } else
            {
                var conditions = sm_src.EntryTransitionsTo(sm_dest).WhenConditions();
                for (int i = 0; i < 16; i++)
                {
                    conditions.And(ACC.bits[i].IsEqualTo(false));
                }
            }
            
        }

        public static AacFlState load_carry(AacFlLayer fx, memory_word ACC, memory_word IR)
        {
            var c = fx.NewState("Load_Carr");
            c.DrivingCopies(fx.BoolParameter("Carry"), ACC.bits[15]);

            for (int i = 0; i < 15; i++)
            {
                c.Drives(ACC.bits[i], false);
            }

            return c;
        }

        public static AacFlStateMachine shift(AacFlStateMachine sm, memory_word ACC, memory_word IR)
        {
            var shift_r = copy_word(sm, ACC, ACC, -1, true);
            var shift_l = copy_word(sm, ACC, ACC, 1, false);

            sm.EntryTransitionsTo(shift_r).When(IR.bits[15].IsFalse());
            sm.EntryTransitionsTo(shift_l).When(IR.bits[15].IsTrue());

            shift_r.Exits();
            shift_l.Exits();

            return sm;
        }
    }
}
#endif