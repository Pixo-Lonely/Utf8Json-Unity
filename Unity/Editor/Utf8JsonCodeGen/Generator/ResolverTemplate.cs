#if UNITY_EDITOR
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System;

namespace Utf8Json.CodeGenerator.Generator
{
    public partial class ResolverTemplate
    {
        public string TransformText()
        {
            var sb = new StringBuilder();

            sb.AppendLine("#pragma warning disable 618");
            sb.AppendLine("#pragma warning disable 612");
            sb.AppendLine("#pragma warning disable 414");
            sb.AppendLine("#pragma warning disable 168");
            sb.AppendLine();
            sb.AppendLine($"namespace {Namespace}");
            sb.AppendLine("{");
            sb.AppendLine("    using System;");
            sb.AppendLine("    using Utf8Json;");
            sb.AppendLine();
            sb.AppendLine($"    public class {ResolverName} : global::Utf8Json.IJsonFormatterResolver");
            sb.AppendLine("    {");
            sb.AppendLine($"        public static readonly global::Utf8Json.IJsonFormatterResolver Instance = new {ResolverName}();");
            sb.AppendLine();
            sb.AppendLine($"        {ResolverName}()");
            sb.AppendLine("        {");
            sb.AppendLine();
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public global::Utf8Json.IJsonFormatter<T> GetFormatter<T>()");
            sb.AppendLine("        {");
            sb.AppendLine("            return FormatterCache<T>.formatter;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        static class FormatterCache<T>");
            sb.AppendLine("        {");
            sb.AppendLine("            public static readonly global::Utf8Json.IJsonFormatter<T> formatter;");
            sb.AppendLine();
            sb.AppendLine("            static FormatterCache()");
            sb.AppendLine("            {");
            sb.AppendLine($"                var f = {ResolverName}GetFormatterHelper.GetFormatter(typeof(T));");
            sb.AppendLine("                if (f != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    formatter = (global::Utf8Json.IJsonFormatter<T>)f;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    internal static class {ResolverName}GetFormatterHelper");
            sb.AppendLine("    {");
            sb.AppendLine("        static readonly global::System.Collections.Generic.Dictionary<Type, int> lookup;");
            sb.AppendLine();
            sb.AppendLine($"        static {ResolverName}GetFormatterHelper()");
            sb.AppendLine("        {");
            sb.AppendLine($"            lookup = new global::System.Collections.Generic.Dictionary<Type, int>({registerInfos.Length})");
            sb.AppendLine("            {");

            for (var i = 0; i < registerInfos.Length; i++)
            {
                var x = registerInfos[i];
                sb.AppendLine($"                {{typeof({x.FullName}), {i} }},");
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        internal static object GetFormatter(Type t)");
            sb.AppendLine("        {");
            sb.AppendLine("            int key;");
            sb.AppendLine("            if (!lookup.TryGetValue(t, out key)) return null;");
            sb.AppendLine();
            sb.AppendLine("            switch (key)");
            sb.AppendLine("            {");

            for (var i = 0; i < registerInfos.Length; i++)
            {
                var x = registerInfos[i];
                var formatterName = x.FormatterName.StartsWith("global::")
                    ? x.FormatterName
                    : (!string.IsNullOrEmpty(FormatterNamespace) ? FormatterNamespace + "." : "") + x.FormatterName;
                sb.AppendLine($"                case {i}: return new {formatterName}();");
            }

            sb.AppendLine("                default: return null;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("#pragma warning disable 168");
            sb.AppendLine("#pragma warning restore 414");
            sb.AppendLine("#pragma warning restore 618");
            sb.AppendLine("#pragma warning restore 612");

            return sb.ToString();
        }
    }
}
#endif
