﻿using System;
using System.Text;
using System.Threading;
using ExRam.Gremlinq.Core.Serialization;
using Gremlin.Net.Process.Traversal;

namespace ExRam.Gremlinq.Core
{
    internal sealed class GroovyGremlinQueryDebugger : IGremlinQueryDebugger
    {
        private readonly struct GroovyWriter
        {
            private readonly bool _hasIdentifier;
            private readonly StringBuilder _builder;

            public GroovyWriter(StringBuilder builder) : this(builder, false)
            {
            }

            public GroovyWriter(StringBuilder builder, bool hasIdentifier)
            {
                _builder = builder;
                _hasIdentifier = hasIdentifier;
            }

            public GroovyWriter Append(
                object obj,
                bool allowEnumerableExpansion = false)
            {
                switch (obj)
                {
                    case Bytecode byteCode:
                    {
                        var writer = this
                            .StartTraversal();

                        foreach (var instruction in byteCode.SourceInstructions)
                        {
                            writer = writer
                                .Append(instruction);
                        }

                        foreach (var instruction in byteCode.StepInstructions)
                        {
                            writer = writer
                                .Append(instruction);
                        }

                        return writer;
                    }
                    case Instruction instruction:
                    {
                        return this
                            .StartOperator(instruction.OperatorName)
                            .Append(instruction.Arguments, true)
                            .EndOperator();
                    }
                    case P { Value: P p1 } p:
                    {
                        return this
                            .Append(p1)
                            .StartOperator(p.OperatorName)
                            .Append(p.Other)
                            .EndOperator();
                    }
                    case P p:
                    {
                        return this
                            .StartOperator(p.OperatorName)
                            .Append((object)p.Value, true)
                            .EndOperator();
                    }
                    case EnumWrapper t:
                    {
                        return Write(t.EnumValue);
                    }
                    case ILambda lambda:
                    {
                        return WriteLambda(lambda.LambdaExpression);
                    }
                    case string str:
                    {
                        return this
                            .WriteQuoted(str);
                    }
                    case DateTimeOffset dateTime:
                    {
                       return this
                           .WriteQuoted(dateTime.ToString("o"));
                    }
                    case DateTime dateTime:
                    {
                        return this
                            .WriteQuoted(dateTime.ToString("o"));
                    }
                    case bool b:
                    {
                        return this
                            .Write(b ? "true" : "false");
                    }
                    case Type type:
                    {
                        return Write(type.Name);
                    }
                    case object[] objectArray when allowEnumerableExpansion:
                    {
                        var writer = this;

                        for (var i = 0; i < objectArray.Length; i++)
                        {
                            writer = writer
                                .StartParameter(i)
                                .Append(objectArray[i]);
                        }

                        return writer;
                    }
                    default:
                    {
                        return Write(obj);
                    }
                }
            }

            public GroovyWriter StartTraversal() => Identifier(_builder.Length == 0
                ? "g"
                : "__");

            public GroovyWriter Identifier(string identifier) => new(
                 _builder.Append(identifier),
                true);

            public GroovyWriter StartOperator(string operatorName)
            {
                var builder = _builder;

                if (_hasIdentifier)
                    builder = builder.Append('.');

                return new(
                    builder
                        .Append(operatorName)
                        .Append('('),
                    false);
            }

            public GroovyWriter StartParameter(int parameterIndex)
            {
                var builder = _builder;

                if (parameterIndex > 0)
                    builder = builder.Append(',');

                return new(
                    builder,
                    _hasIdentifier);
            }

            public GroovyWriter WriteLambda(string lambda) => new(
                _builder
                    .Append('{')
                    .Append(lambda)
                    .Append('}'),
                _hasIdentifier);

            public GroovyWriter EndOperator() => new(
                _builder
                    .Append(')'),
                true);

            public GroovyWriter WriteQuoted(object value)
            {
#if NET6_0_OR_GREATER
                var handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, _builder);
                handler.AppendLiteral("'");
                handler.AppendFormatted(value);
                handler.AppendLiteral("'");

                return new(
                    _builder.Append(ref handler),
                    _hasIdentifier);
#else
                return new(
                    _builder
                        .Append('\'')
                        .Append(value.ToString())
                        .Append('\''),
                    _hasIdentifier);
#endif
            }

            public GroovyWriter Write(object value)
            {
#if NET6_0_OR_GREATER
                var handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, _builder);
                handler.AppendFormatted(value);

                return new(
                    _builder.Append(ref handler),
                    _hasIdentifier);
#else
                return new(
                    _builder.Append(value.ToString()),
                    _hasIdentifier);
#endif
            }

            public override string ToString() => _builder.ToString();
        }

        private static readonly ThreadLocal<StringBuilder> Builder = new(static () => new StringBuilder());

        public string? TryToString(ISerializedGremlinQuery serializedQuery, IGremlinQueryEnvironment environment)
        {
            if (serializedQuery is BytecodeGremlinQuery byteCodeQuery)
            {
                return new GroovyWriter(Builder.Value!.Clear())
                    .Append(byteCodeQuery.Bytecode)
                    .ToString();
            }

            return default;
        }
    }
}