﻿using System.Collections.Generic;
using ExRam.Gremlinq.Core.Steps;

namespace ExRam.Gremlinq.Core
{
    internal static class FinalContinuationBuilderExtensions
    {
        public static FinalContinuationBuilder<TOuterQuery> AddSteps<TOuterQuery>(this FinalContinuationBuilder<TOuterQuery> builder, IEnumerable<Step> steps)
            where TOuterQuery : GremlinQueryBase
        {
            foreach (var step in steps)
            {
                builder = builder.AddStep(step);
            }

            return builder;
        }
    }
}