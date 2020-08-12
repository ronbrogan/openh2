﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenH2.Core.Scripting;
using OpenH2.Core.Tags.Scenario;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace OpenH2.ScriptAnalysis.GenerationState
{
    internal class IfStatementContext : BaseGenerationContext, IGenerationContext, IStatementContext
    {
        private readonly Scope containingScope;
        private readonly bool shouldHoistAndStoreResult;

        public override bool CreatesScope => true;

        private IdentifierNameSyntax resultVariable;

        private ExpressionSyntax condition = null;

        private bool doneWithTrueBlock = false;

        private List<StatementSyntax> whenTrueStatements = new List<StatementSyntax>();
        private List<StatementSyntax> whenFalseStatements = new List<StatementSyntax>();

        public IfStatementContext(ScenarioTag.ScriptSyntaxNode node, Scope containingScope) : base(node)
        {
            var resultVarName = "ifResult_" + this.GetHashCode();

            resultVariable = IdentifierName(resultVarName);
            this.containingScope = containingScope;
            this.shouldHoistAndStoreResult = containingScope.Type != ScriptDataType.Void;
        }

        public IGenerationContext AddExpression(ExpressionSyntax expression)
        {
            if(this.condition == null)
            {
                this.condition = expression;
            }
            else if(this.doneWithTrueBlock == false)
            {
                this.whenTrueStatements.Add(ExpressionStatement(expression));
                this.doneWithTrueBlock = true;
            }
            else
            {
                this.whenFalseStatements.Add(ExpressionStatement(expression));
            }

            return this;
        }

        public IStatementContext AddStatement(StatementSyntax statement)
        {
            if(this.condition == null)
            {
                var expStatement = statement as ExpressionStatementSyntax;
                Debug.Assert(expStatement != null, "First statement was not an expression statement, but is required for the condition of the 'if'");

                this.condition = expStatement.Expression;
            }
            else if(this.doneWithTrueBlock == false)
            {
                whenTrueStatements.Add(statement);
                if(statement.HasAnnotation(ScriptGenAnnotations.ResultStatement) || statement.HasAnnotation(ScriptGenAnnotations.FinalScopeStatement))
                {
                    doneWithTrueBlock = true;
                }
            }
            else
            {
                whenFalseStatements.Add(statement);
            }

            return this;
        }

        public bool TryCreateResultStatement(ExpressionSyntax resultValue, out StatementSyntax statement)
        {
            if (shouldHoistAndStoreResult)
            {
                statement = ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    resultVariable,
                    resultValue))
                .WithAdditionalAnnotations(ScriptGenAnnotations.ResultStatement);
                return true;
            }
            else
            {
                statement = default;
                return false;
            }
        }

        public void GenerateInto(Scope scope)
        {
            Debug.Assert(scope == this.containingScope, "Generating into an unexpected scope");
            Debug.Assert(this.condition != null, "Condition expression was not provided");
            Debug.Assert(whenTrueStatements.Any(), "WhenTrue was not provided");

            var generatedStatements = new List<StatementSyntax>();

            if (this.shouldHoistAndStoreResult)
            {
                generatedStatements.Add(LocalDeclarationStatement(VariableDeclaration(PredefinedType(Token(SyntaxKind.BoolKeyword)))
                    .WithVariables(SingletonSeparatedList(
                        VariableDeclarator(resultVariable.Identifier)
                            .WithInitializer(
                                EqualsValueClause(
                                    LiteralExpression(
                                        SyntaxKind.FalseLiteralExpression))))))
                    .WithAdditionalAnnotations(ScriptGenAnnotations.HoistedResultVar));

                if (HasResultVarAssignment(whenTrueStatements) == false)
                {
                    InsertResultVarAssignment(whenTrueStatements);
                }

                if(whenFalseStatements.Any() && HasResultVarAssignment(whenFalseStatements) == false)
                {
                    InsertResultVarAssignment(whenFalseStatements);
                }
            }

            var trueBlock = Block(whenTrueStatements);

            if (whenFalseStatements.Any())
            {
                StatementSyntax falseBlock = Block(whenFalseStatements);

                // Collapse if/else if/... 
                if (whenFalseStatements.Count == 1 && whenFalseStatements[0] is IfStatementSyntax ifStatement)
                {
                    falseBlock = ifStatement;
                }

                generatedStatements.Add(
                    IfStatement(condition, trueBlock, ElseClause(falseBlock))
                        .WithLeadingTrivia(SyntaxFactory.Whitespace(Environment.NewLine))
                        .WithAdditionalAnnotations(ScriptGenAnnotations.IfStatement));
            }
            else
            {
                generatedStatements.Add(
                    IfStatement(condition, trueBlock)
                        .WithLeadingTrivia(SyntaxFactory.Whitespace(Environment.NewLine))
                        .WithAdditionalAnnotations(ScriptGenAnnotations.IfStatement));
            }

            foreach(var statement in generatedStatements)
            {
                scope.StatementContext.AddStatement(statement);
            }

            
            if (this.shouldHoistAndStoreResult)
            {
                scope.Context.AddExpression(this.resultVariable);
            }
            else
            {
                //var result = scope.StatementContext.CreateResultStatement(this.resultVariable);
                //scope.StatementContext.AddStatement(result);
            }
            
        }

        private bool HasResultVarAssignment(IEnumerable<SyntaxNode> roots)
        {
            foreach(var root in roots)
            {
                var checker = new ResultVarChecker(this.resultVariable);

                checker.Visit(root);

                if (checker.HasResultVarAssignment)
                    return true;
            }

            return false;
        }

        private void InsertResultVarAssignment(List<StatementSyntax> statements)
        {
            var lastStatement = statements.Last();

            if (lastStatement.TryGetContainingSimpleExpression(out var simple))
            {
                var updatedLastTrue = ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    resultVariable,
                    simple));

                statements.Remove(lastStatement);
                statements.Add(updatedLastTrue);
            }
            else if (lastStatement.TryGetLeftHandExpression(out var rhs))
            {
                var rhsAssignment = ExpressionStatement(AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    resultVariable,
                    rhs));

                statements.Add(rhsAssignment);
            }
        }

        private class ResultVarChecker : CSharpSyntaxWalker
        {
            private readonly IdentifierNameSyntax resultVar;

            public bool HasResultVarAssignment { get; private set; } = false;

            public ResultVarChecker(IdentifierNameSyntax resultVar)
            {
                this.resultVar = resultVar;
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                if (node.Left.IsEquivalentTo(resultVar))
                    HasResultVarAssignment = true;

                base.VisitAssignmentExpression(node);
            }
        }

        private class ResultVarRemover  : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
            {
                if(node.HasAnnotation(ScriptGenAnnotations.ResultStatement))
                {
                    var assignment = node.Expression as AssignmentExpressionSyntax;

                    var rhs = assignment.Right;

                    if(SyntaxUtil.IsSimpleExpression(rhs))
                    {
                        return ExpressionStatement(rhs);
                    }
                    else
                    {

                    }

                    
                }

                return base.VisitExpressionStatement(node);
            }
        }
    }
}