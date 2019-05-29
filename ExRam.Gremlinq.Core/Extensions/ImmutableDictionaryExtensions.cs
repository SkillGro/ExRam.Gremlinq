﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using ExRam.Gremlinq.Core;
using LanguageExt;

namespace System.Linq
{
    public static class ImmutableDictionaryExtensions
    {
        internal static IImmutableDictionary<MemberInfo, PropertyMetadata> ConfigureNames(this IImmutableDictionary<MemberInfo, PropertyMetadata> metadata, Func<MemberInfo, Option<string>, Option<string>> transformation)
        {
            return metadata
                .SetItems(metadata
                    .Select(kvp => new KeyValuePair<MemberInfo, PropertyMetadata>(
                        kvp.Key,
                        new PropertyMetadata(
                            transformation(kvp.Key, kvp.Value.NameOverride),
                            kvp.Value.SerializationBehaviour))));
        }
    }
}
