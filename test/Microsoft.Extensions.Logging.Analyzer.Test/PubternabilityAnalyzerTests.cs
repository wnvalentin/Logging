// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging.Analyzers;
using Xunit;

namespace Microsoft.Extensions.Logging.Analyzer.Test
{
    public class PubternabilityAnalyzerTests : DiagnosticVerifier
    {
        [Theory]
        [MemberData(nameof(PublicMemberDefinitions))]
        public void PublicExposureOfPubternalTypeProducesPUB0001(string member)
        {
            var code = $@"
namespace A
{{
    public class T
    {{
        {member}
    }}
}}";
            var diagnostic = Assert.Single(GetDiagnosticFromNamespaceDeclaration(code));
            Assert.Equal("PUB0001", diagnostic.Id);
        }

        [Theory]
        [MemberData(nameof(PublicTypeDefinitions))]
        public void PublicExposureOfPubternalTypeProducesInTypeDefinitionPUB0001(string member)
        {
            var code = $@"
namespace A
{{
    {member}
}}";
            var diagnostic = Assert.Single(GetDiagnosticFromNamespaceDeclaration(code));
            Assert.Equal("PUB0001", diagnostic.Id);
        }

        [Theory]
        [MemberData(nameof(PublicMemberDefinitions))]
        public void PrivateUsageOfPubternalTypeDoesNotProduce(string member)
        {
            var code = $@"
namespace A
{{
    internal class T
    {{
        {member}
    }}
}}";
            var diagnostics = GetDiagnosticFromNamespaceDeclaration(code);
            Assert.Empty(diagnostics);
        }

        [Theory]
        [MemberData(nameof(PrivateMemberDefinitions))]
        public void PrivateUsageOfPubternalTypeDoesNotProduceInPublicClasses(string member)
        {
            var code = $@"
namespace A
{{
    public class T
    {{
        {member}
    }}
}}";
            var diagnostics = GetDiagnosticFromNamespaceDeclaration(code);
            Assert.Empty(diagnostics);
        }

        public static IEnumerable<object[]> PublicMemberDefinitions =>
            ApplyModifiers(MemberDefinitions, "public", "protected");

        public static IEnumerable<object[]> PublicTypeDefinitions =>
            ApplyModifiers(TypeDefinitions, "public");

        public static IEnumerable<object[]> PrivateMemberDefinitions =>
            ApplyModifiers(MemberDefinitions, "private", "internal");

        public static string[] MemberDefinitions => new string[]
        {
            "C c;",
            "T(C c) {}",
            "T(C c) {}",
            "CD c { get; }",
            "event CD c;",
            "delegate C WOW();"
        };

        public static string[] TypeDefinitions => new string[]
        {
            "delegate C WOW();",
            "class T: I<C> { } interface I<T> {}"
        };

        private static IEnumerable<object[]> ApplyModifiers(string[] code, params string[] mods)
        {
            foreach (var mod in mods)
            {
                foreach (var s in code)
                {
                    yield return new object[] { mod + " " + s };
                }
            }
        }
        private static Diagnostic[] GetDiagnosticFromNamespaceDeclaration(string namespaceDefinition)
        {
            var code = $@"
using A.Internal;
namespace A.Internal
{{
   public class C {{}}
   public delegate C CD ();
   public class Program
   {{
       public static void Main() {{ }}
   }}
}}

" + namespaceDefinition;
            return GetDiagnostics(code);
        }

        private static Diagnostic[] GetDiagnostics(string code)
        {
            return GetSortedDiagnosticsAsync(new[] { code }, new PubternalityAnalyzer(), Array.Empty<string>()).Result;
        }
    }
}
