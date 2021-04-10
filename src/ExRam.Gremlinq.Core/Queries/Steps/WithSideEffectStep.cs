﻿namespace ExRam.Gremlinq.Core
{
    public sealed class WithSideEffectStep : Step
    {
        public WithSideEffectStep(StepLabel label, object value)
        {
            Label = label;
            Value = value;
        }

        public object Value { get; }
        public StepLabel Label { get; }
    }
}