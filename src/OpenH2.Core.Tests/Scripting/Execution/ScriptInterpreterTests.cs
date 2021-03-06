﻿using Moq;
using OpenH2.Core.Scripting;
using OpenH2.Core.Scripting.Execution;
using OpenH2.Core.Tags.Scenario;
using System;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace OpenH2.Core.Tests.Scripting.Execution
{
    public class ScriptInterpreterTests
    {
        private readonly ITestOutputHelper output;


        public ScriptInterpreterTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void Interpret_ExpressionReturnsResult()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    Node(NodeType.Expression, ScriptDataType.Float, op: (ushort)ScriptDataType.Float, data: From(2f)),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(2f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_ScopeReturnsResult()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                    Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f)),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(2f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_ScopeCast()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Short, op: 7, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Add, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 4),
                                    Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f)),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Short, result.DataType);
            Assert.Equal(6, result.Short);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_BeginReturnsLast()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Begin, next: 2),
                            ScopeNode(ScriptDataType.Float, op:6, child: 3, next: 4),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1f)),
                            ScopeNode(ScriptDataType.Float, op:6, child: 5, next: 6),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f)),
                            ScopeNode(ScriptDataType.Float, op:6, child: 7),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(3f)),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(3, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_BeginExecutesAll()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptStrings = Encoding.UTF8.GetBytes("hi\0hey\0hello\0"),
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Void, op: 4, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Begin, next: 2),
                            ScopeNode(ScriptDataType.Void, op:6, child: 3, next: 5),
                                Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Print, next: 4),
                                    Node(NodeType.Expression, ScriptDataType.String, op: 9, stringIndex: 0),
                            ScopeNode(ScriptDataType.Void, op:6, child: 6, next: 8),
                                Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Print, next: 7),
                                    Node(NodeType.Expression, ScriptDataType.String, op: 9, stringIndex: 3),
                            ScopeNode(ScriptDataType.Void, op:6, child: 9),
                                Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Print, next: 10),
                                    Node(NodeType.Expression, ScriptDataType.String, op: 9, stringIndex: 7)
                }
            };

            var engine = new Mock<IScriptEngine>();
            engine.Setup(e => e.print(It.IsAny<string>())).Callback((string s) => output.WriteLine(s));

            var interpreter = new ScriptInterpreter(scen, engine.Object);

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Void, result.DataType);
            Assert.Equal(ushort.MaxValue, n);

            engine.Verify(e => e.print("hi"), Times.Once);
            engine.Verify(e => e.print("hey"), Times.Once);
            engine.Verify(e => e.print("hello"), Times.Once);
        }

        [Fact]
        public void Interpret_IfTrueTakesFirst()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.If, next: 2),
                            ScopeNode(ScriptDataType.Boolean, op:5, child: 3, next: 4),
                                Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1),
                            ScopeNode(ScriptDataType.Float, op:6, child: 5, next: 6),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f)),
                            ScopeNode(ScriptDataType.Float, op:6, child: 7),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(3f)),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(2f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_IfFalseTakesLast()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.If, next: 2),
                            ScopeNode(ScriptDataType.Boolean, op:5, child: 3, next: 4),
                                Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 0),
                            ScopeNode(ScriptDataType.Float, op:6, child: 5, next: 6),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f)),
                            ScopeNode(ScriptDataType.Float, op:6, child: 7),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(3f)),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(3f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_VariableInitialization()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptVariables = new ScenarioTag.ScriptVariableDefinition[3]
                {
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Int, Value_32 = 0 },
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Float, Value_32 = 1 },
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Boolean, Value_32 = 2 }
                },
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    Node(NodeType.Expression, ScriptDataType.Int, op: 8, data: 123),
                    Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(12f)),
                    Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            Assert.Equal(ScriptDataType.Int, interpreter.GetVariable(0).DataType);
            Assert.Equal(123, interpreter.GetVariable(0).Int);

            Assert.Equal(ScriptDataType.Float, interpreter.GetVariable(1).DataType);
            Assert.Equal(12f, interpreter.GetVariable(1).Float);

            Assert.Equal(ScriptDataType.Boolean, interpreter.GetVariable(2).DataType);
            Assert.True(interpreter.GetVariable(2).Boolean);
        }

        [Fact]
        public void Interpret_VariableSet()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptVariables = new ScenarioTag.ScriptVariableDefinition[3]
                {
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Int, Value_32 = 4 },
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Float, Value_32 = 5 },
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Boolean, Value_32 = 6 }
                },
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Void, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Set, next: 2),
                            Node(NodeType.VariableAccess, ScriptDataType.Float, op: 6, data: 1, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f)),

                    Node(NodeType.Expression, ScriptDataType.Int, op: 8, data: 123),
                    Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(12f)),
                    Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            Assert.Equal(12f, interpreter.GetVariable(1));

            interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(2f, interpreter.GetVariable(1));
        }

        [Fact]
        public void Interpret_VariableGet()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptVariables = new ScenarioTag.ScriptVariableDefinition[3]
                {
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Int, Value_32 = 2 },
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Float, Value_32 = 3 },
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Boolean, Value_32 = 4 }
                },
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.VariableAccess, ScriptDataType.Float, op: 6, data: 1),

                    Node(NodeType.Expression, ScriptDataType.Int, op: 8, data: 123),
                    Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(12f)),
                    Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            Assert.Equal(12f, interpreter.GetVariable(1));

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(12f, result.Float);
            Assert.Equal(12f, interpreter.GetVariable(1));
        }

        [Fact]
        public void Interpret_VariableGetSet()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptVariables = new ScenarioTag.ScriptVariableDefinition[3]
                {
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Int, Value_32 = 4 },
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Float, Value_32 = 5 },
                    new ScenarioTag.ScriptVariableDefinition(){ DataType = ScriptDataType.Boolean, Value_32 = 6 }
                },
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Void, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Set, next: 2),
                            Node(NodeType.VariableAccess, ScriptDataType.Float, op: 6, data: 1, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f)),

                    Node(NodeType.Expression, ScriptDataType.Int, op: 8, data: 123),
                    Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(12f)),
                    Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1),

                    ScopeNode(ScriptDataType.Float, op: 6, child: 8),
                        Node(NodeType.VariableAccess, ScriptDataType.Float, op: 6, data: 1),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            // Ensure initialization worked
            Assert.Equal(12f, interpreter.GetVariable(1));

            // Set
            interpreter.Interpret(scen.ScriptSyntaxNodes[0], out _);
            Assert.Equal(2f, interpreter.GetVariable(1));

            // Get
            var getResult = interpreter.Interpret(scen.ScriptSyntaxNodes[7], out _);
            Assert.Equal(2f, getResult.Float);
        }

        [Fact]
        public void Interpret_Add()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Add, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 4),
                                    Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f)),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(6f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_Subtract()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Subtract, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(0f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_Multiply()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Multiply, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 4),
                                    Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f)),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(8f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_Divide()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Divide, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(1f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_MinA()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Min, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(1f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_MinB()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Min, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1f))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(1f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_MaxA()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Max, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(2f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_MaxB()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Float, op: 6, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Max, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1f))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Float, result.DataType);
            Assert.Equal(2f, result.Float);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_EqualsFloat()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Equals, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotEqualsFloat()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Equals, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2f), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1f))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_EqualsInt()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Equals, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Int, op: 8, data: 2, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Int, op: 8, data: 2)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotEqualsInt()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Equals, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Int, op: 8, data: 2, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Int, op: 8, data: 1)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_EqualsShort()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Equals, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: 2, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: 2)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotEqualsShort()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Equals, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 1)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_LessThanShort()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.LessThan, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 1, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotLessThanShort()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.LessThan, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 1)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_LessThanOrEqualShort()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.LessThanOrEqual, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotLessThanOrEqualShort()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.LessThanOrEqual, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 1)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_GreaterThanShort()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.GreaterThan, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 1)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotGreaterThanShort()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.GreaterThan, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 1, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_GreaterThanOrEqualShort()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.GreaterThanOrEqual, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotGreaterThanOrEqualShort()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.GreaterThanOrEqual, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 1, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Short, op: 7, data: 2)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_LessThanFloat()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.LessThan, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotLessThanFloat()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.LessThan, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_LessThanOrEqualFloat()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.LessThanOrEqual, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotLessThanOrEqualFloat()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.LessThanOrEqual, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_GreaterThanFloat()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.GreaterThan, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotGreaterThanFloat()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.GreaterThan, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_GreaterThanOrEqualFloat()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.GreaterThanOrEqual, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotGreaterThanOrEqualFloat()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.GreaterThanOrEqual, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(1), next: 3),
                                Node(NodeType.Expression, ScriptDataType.Float, op: 6, data: From(2))
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_And()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.And, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1, next: 4),
                                    Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotAnd()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.And, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1, next: 4),
                                    Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 0),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_OrAll()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Or, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1, next: 4),
                                    Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_OrAny()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Or, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 0, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1, next: 4),
                                    Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 0),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotOr()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Or, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 0, next: 3),
                                Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 0, next: 4),
                                    Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 0),
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotA()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Not, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 1)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.False(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        [Fact]
        public void Interpret_NotB()
        {
            var scen = new ScenarioTag(0)
            {
                ScriptSyntaxNodes = new ScenarioTag.ScriptSyntaxNode[]
                {
                    ScopeNode(ScriptDataType.Boolean, op: 5, child: 1),
                        Node(NodeType.Expression, ScriptDataType.MethodOrOperator, op: ScriptOps.Not, next: 2),
                            Node(NodeType.Expression, ScriptDataType.Boolean, op: 5, data: 0)
                }
            };

            var interpreter = new ScriptInterpreter(scen, Mock.Of<IScriptEngine>());

            var result = interpreter.Interpret(scen.ScriptSyntaxNodes[0], out var n);

            Assert.Equal(ScriptDataType.Boolean, result.DataType);
            Assert.True(result.Boolean);
            Assert.Equal(ushort.MaxValue, n);
        }

        private ScenarioTag.ScriptSyntaxNode ScopeNode(ScriptDataType dt, ushort op, ushort child, ushort next = 65535)
        {
            return Node(NodeType.BuiltinInvocation, dt, op, next, child);
        }

        private ScenarioTag.ScriptSyntaxNode Node(NodeType nt, ScriptDataType dt, ushort op, ushort next = 65535, uint data = 0, ushort stringIndex = 0)
        {
            return new ScenarioTag.ScriptSyntaxNode()
            {
                NodeType = nt,
                DataType = dt,
                NextIndex = next,
                OperationId = op,
                NodeData_32 = data,
                NodeString = stringIndex
            };
        }

        private static uint From(float f)
        {
            return (uint)BitConverter.SingleToInt32Bits(f);
        }
    }
}
