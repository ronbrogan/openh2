﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace OpenH2.Core.Scripting.Execution
{
    [StructLayout(LayoutKind.Explicit)]
    public struct InterpreterResult
    {
        [FieldOffset(0)]
        public bool Boolean;

        [FieldOffset(0)]
        public int Int;

        [FieldOffset(0)]
        public short Short;

        [FieldOffset(0)]
        public float Float;

        [FieldOffset(4)]
        public ScriptDataType DataType;

        [FieldOffset(6)]
        public ushort VariableIndex;

        [FieldOffset(8)]
        public object? Object;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetFloat()
        {
            if (DataType == ScriptDataType.Float)
                return Float;
            else
                return Int;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetInt()
        {
            if (DataType == ScriptDataType.Float)
                return (int)Float;
            else
                return Int;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short GetShort()
        {
            if (DataType == ScriptDataType.Float)
                return (short)Float;
            else
                return Short;
        }

        public void Add(InterpreterResult operand)
        {
            if (DataType == ScriptDataType.Float)
                this.Float = this.GetFloat() + operand.GetFloat();
            else
                this.Short = (short)(this.GetShort() + operand.GetShort());
        }

        public void Subtract(InterpreterResult operand)
        {
            if (DataType == ScriptDataType.Float)
                this.Float = this.GetFloat() - operand.GetFloat();
            else
                this.Short = (short)(this.GetShort() - operand.GetShort());
        }

        public void Multiply(InterpreterResult operand)
        {
            if (DataType == ScriptDataType.Float)
                this.Float = this.GetFloat() * operand.GetFloat();
            else
                this.Short = (short)(this.GetShort() * operand.GetShort());
        }

        public void Divide(InterpreterResult operand)
        {
            if (DataType == ScriptDataType.Float)
                this.Float = this.GetFloat() / operand.GetFloat();
            else
                this.Short = (short)(this.GetShort() / operand.GetShort());
        }

        public static InterpreterResult Min(InterpreterResult left, InterpreterResult right)
        {
            Debug.Assert(left.DataType == ScriptDataType.Float);

            return Difference(left, right) < 0 ? left : right;
        }

        public static InterpreterResult Max(InterpreterResult left, InterpreterResult right)
        {
            Debug.Assert(left.DataType == ScriptDataType.Float);

            return Difference(left, right) < 0 ? right : left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Difference(InterpreterResult left, InterpreterResult right)
        {
            Debug.Assert(left.DataType == ScriptDataType.Float);

            return left.Float - right.GetFloat();
        }

        public static InterpreterResult From(bool v, ScriptDataType t = ScriptDataType.Boolean) => new InterpreterResult() { Boolean = v, DataType = t };
        public static InterpreterResult From(int v, ScriptDataType t = ScriptDataType.Int) => new InterpreterResult() { Int = v, DataType = t };
        public static InterpreterResult From(uint v, ScriptDataType t = ScriptDataType.Int) => new InterpreterResult() { Int = (int)v, DataType = t };
        public static InterpreterResult From(short v, ScriptDataType t = ScriptDataType.Short) => new InterpreterResult() { Short = v, DataType = t };
        public static InterpreterResult From(ushort v, ScriptDataType t = ScriptDataType.Short) => new InterpreterResult() { Short = (short)v, DataType = t };
        public static InterpreterResult From(float v, ScriptDataType t = ScriptDataType.Float) => new InterpreterResult() { Float = v, DataType = t };
        public static InterpreterResult From(object? v, ScriptDataType t = ScriptDataType.Entity) => new InterpreterResult() { Object = v, DataType = t };
        public static InterpreterResult From() => new InterpreterResult() { DataType = ScriptDataType.Void };

        public static implicit operator float(InterpreterResult r)
        {
            return r.Float;
        }

        public static implicit operator int(InterpreterResult r)
        {
            return r.Int;
        }

        public static implicit operator short(InterpreterResult r)
        {
            return r.Short;
        }
    }
}
