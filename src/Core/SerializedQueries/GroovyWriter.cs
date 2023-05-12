﻿using System.Text;
using Gremlin.Net.Process.Traversal;

namespace ExRam.Gremlinq.Core
{
    internal readonly struct GroovyWriter
    {
        private static readonly ThreadLocal<StringBuilder> Builder = new(static () => new StringBuilder());

        private readonly bool _hasIdentifier;
        private readonly StringBuilder _builder;

        public GroovyWriter() : this(Builder.Value!.Clear())
        {
        }

        private GroovyWriter(StringBuilder builder, bool hasIdentifier = false)
        {
            _builder = builder;
            _hasIdentifier = hasIdentifier;
        }

        public string ToString(Bytecode bytecode) => Append(bytecode)._builder.ToString();

        private GroovyWriter Append(
            object obj,
            bool allowEnumerableExpansion = false)
        {
            switch (obj)
            {
                case Bytecode byteCode:
                {
                    var writer = 
                        StartTraversal();

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
                    return 
                        StartOperator(instruction.OperatorName)
                        .Append(instruction.Arguments, true)
                        .EndOperator();
                }
                case P { Value: P p1 } p:
                {
                    return 
                        Append(p1)
                        .StartOperator(p.OperatorName)
                        .Append(p.Other)
                        .EndOperator();
                }
                case P p:
                {
                    return 
                        StartOperator(p.OperatorName)
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
                    return 
                        WriteQuoted(str);
                }
                case DateTimeOffset dateTime:
                {
                    return 
                        WriteQuoted(dateTime.ToString("o"));
                }
                case DateTime dateTime:
                {
                    return 
                        WriteQuoted(dateTime.ToString("o"));
                }
                case bool b:
                {
                    return 
                        Write(b ? "true" : "false");
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
                case null:
                    return Write("null");
                default:
                    return Write(obj);
            }
        }

        private GroovyWriter StartTraversal() => Identifier(_builder.Length == 0
            ? "g"
            : "__");

        private GroovyWriter Identifier(string identifier) => new(
             _builder.Append(identifier),
            true);

        private GroovyWriter StartOperator(string operatorName)
        {
            var builder = _builder;

            if (_hasIdentifier)
                builder = builder.Append('.');

            return new(
                builder
                    .Append(operatorName)
                    .Append('('));
        }

        private GroovyWriter StartParameter(int parameterIndex)
        {
            var builder = _builder;

            if (parameterIndex > 0)
                builder = builder.Append(',');

            return new(
                builder,
                _hasIdentifier);
        }

        private GroovyWriter WriteLambda(string lambda) => new(
            _builder
                .Append('{')
                .Append(lambda)
                .Append('}'),
            _hasIdentifier);

        private GroovyWriter EndOperator() => new(
            _builder
                .Append(')'),
            true);

        private GroovyWriter WriteQuoted(object value)
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
                    .Append(value)
                    .Append('\''),
                _hasIdentifier);
#endif
        }

        private GroovyWriter Write(object value)
        {
#if NET6_0_OR_GREATER
            var handler = new StringBuilder.AppendInterpolatedStringHandler(0, 1, _builder);
            handler.AppendFormatted(value);

            return new(
                _builder.Append(ref handler),
                _hasIdentifier);
#else
            return new(
                _builder.Append(value),
                _hasIdentifier);
#endif
        }
    }
}