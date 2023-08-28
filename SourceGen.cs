using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using SingletonScritpableObjectSourceGen;
using System.Collections.Generic;
using System.Text;

namespace ExampleSourceGenerator {
    [Generator]
    public class ExampleSourceGenerator : ISourceGenerator {
        const string usings = @"
// This file is auto-generated. Don't edit it.
using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
";
        const string ns1 = @"
namespace $1 {
";
        const string body = @"
    $2 partial class $0 {
        private static $0 s_instance;
        public static $0 Instance {
            get {
                if (typeof($0).IsAbstract)
                    throw new InvalidOperationException($""Cannot get instance of abstract type {typeof($0).Name}"");
                if (s_instance) {
                    return s_instance;
                }
                s_instance = Resources.Load<$0>(ResourcesPath);
                if (!s_instance) {
                    Debug.LogWarning($""created new instance of {typeof($0).Name}. singleton instance not found at location: {ResourcesPath}"");
                    s_instance = CreateInstance<$0>();
                }
                return s_instance;
            }
        }

        protected const string ResourcesFolderPath = ""SingletonSOs"";
        protected static readonly string ResourcesPath = Path.Combine(ResourcesFolderPath, typeof($0).Name);

        protected virtual void Awake() {
            if (!s_instance || s_instance == this) return;
            Debug.LogError($""{typeof($0).Name} deleted. Another instance is already available."");
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(this);
            else
#endif
                Destroy(this);
        }

        protected virtual void OnDestroy() {
            if (s_instance == this) {
                Debug.LogWarning($""{typeof($0).Name} instance destroyed. Singleton instance is no longer available."");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// for In-Editor use only. Don't use it.
        /// </summary>
        [MenuItem(""Tools/Singleton Scriptable Objects/Game.Core/Select '$0'"")]
        static void Editor_SelectInstance() {
            Selection.activeObject = Instance;
            EditorGUIUtility.PingObject(Instance);
        }

        /// <summary>
        /// For In-Editor use only. Don't use it.
        /// </summary>
        [InitializeOnLoadMethod]
        static void Editor_EnsureInstanceExists() {
            if (typeof($0).IsAbstract) return;
            EditorApplication.delayCall += AssetManagementUtils.UpdateResourcesForSingletonAsset<$0>;
        }
#endif

    }
";
        const string ns2 = @"
}
";

        public void Initialize(GeneratorInitializationContext context) {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context) {
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver)) {
                return;
            }
            foreach (var type in receiver.Types) {
                var sb = new StringBuilder(usings);

                string ns = GetFullNamespace(type);
                if (!string.IsNullOrEmpty(ns))
                    sb.Append(ns1);

                sb.Append(body);
                sb.ReplaceArguments(type.Name, ns, type.DeclaredAccessibility.ToString().ToLower());

                if (!string.IsNullOrEmpty(ns))
                    sb.Append(ns2);

                context.AddSource($"{type.Name}_g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
            }

        }

        string GetFullNamespace(ITypeSymbol type) {
            var ns = type.ContainingNamespace;
            var sb = new StringBuilder();
            while (ns != null) {
                sb.Insert(0, ns.Name);
                sb.Insert(0, ".");
                ns = ns.ContainingNamespace;
            }
            return sb.ToString().TrimStart('.');
        }
    }

    class SyntaxReceiver : ISyntaxContextReceiver {
        public List<ITypeSymbol> Types { get; } = new List<ITypeSymbol>();
        public void OnVisitSyntaxNode(GeneratorSyntaxContext context) {
            if (context.Node is ClassDeclarationSyntax classDeclarationSyntax) {
                if (!classDeclarationSyntax.Modifiers.Any(SyntaxKind.PartialKeyword)) return;
                var model = context.SemanticModel;
                var symbol = model.GetDeclaredSymbol(classDeclarationSyntax);

                if (!(symbol is ITypeSymbol typeSymbol)) return;
                if (typeSymbol.IsAbstract) return;
                if (!IsDerivedFrom(typeSymbol.BaseType, "SingletonScriptableObject")) return;

                Types.Add(typeSymbol);
            }
        }

        private bool IsDerivedFrom(INamedTypeSymbol type, string target) {
            while (type != null) {
                if (type.Name == target)
                    return true;
                type = type.BaseType;
            }
            return false;
        }
    }
}