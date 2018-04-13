// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging.Internal;

namespace Microsoft.Extensions.Logging.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PubternalityAnalyzer : DiagnosticAnalyzer
    {
        public PubternalityAnalyzer()
        {
            SupportedDiagnostics = ImmutableArray.Create(new[]
            {
                PubturnalityDescriptors.PUB0001,
                PubturnalityDescriptors.PUB0002
            });
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(analysisContext =>
            {
                analysisContext.RegisterSyntaxNodeAction(syntaxContext => AnalyzeTypeUsage(syntaxContext), SyntaxKind.IdentifierName);
            });
        }

        private void AnalyzeTypeUsage(SyntaxNodeAnalysisContext syntaxContext)
        {
            var identifier = (IdentifierNameSyntax)syntaxContext.Node;

            var symbolInfo = ModelExtensions.GetTypeInfo(syntaxContext.SemanticModel, identifier, syntaxContext.CancellationToken);
            if (symbolInfo.Type == null)
            {
                return;
            }

            var type = symbolInfo.Type;
            if (type.ContainingNamespace.Name != "Internal")
            {
                // don't care about non-pubternal type references
                return;
            }

            if (!syntaxContext.ContainingSymbol.ContainingAssembly.Equals(type.ContainingAssembly))
            {
                syntaxContext.ReportDiagnostic(Diagnostic.Create(PubturnalityDescriptors.PUB0002, identifier.GetLocation()));
                return;
            }


            var declaredSymbol = syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node.Parent);
            if (declaredSymbol != null)
            {
                if (IsPrivate(declaredSymbol.ContainingType?.DeclaredAccessibility) ||
                    IsPrivate(declaredSymbol.ContainingSymbol?.DeclaredAccessibility))
                {
                    return;
                }

                var accessibility = declaredSymbol.DeclaredAccessibility;
                if (accessibility != Accessibility.Private &&
                    accessibility != Accessibility.Internal &&
                    declaredSymbol.ContainingNamespace.Name != "Internal")
                {
                    syntaxContext.ReportDiagnostic(Diagnostic.Create(PubturnalityDescriptors.PUB0001, identifier.GetLocation()));
                }
            }
        }

        private static bool IsPrivate(Accessibility? declaredAccessibility)
        {
            return declaredAccessibility == Accessibility.Private ||
                   declaredAccessibility == Accessibility.Internal;
        }
    }
}
