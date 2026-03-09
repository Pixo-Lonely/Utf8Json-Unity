#if UNITY_EDITOR
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System;

namespace Utf8Json.CodeGenerator.Generator
{
    public partial class FormatterTemplate
    {
        public string TransformText()
        {
            var sb = new StringBuilder();

            sb.AppendLine("#pragma warning disable 618");
            sb.AppendLine("#pragma warning disable 612");
            sb.AppendLine("#pragma warning disable 414");
            sb.AppendLine("#pragma warning disable 219");
            sb.AppendLine("#pragma warning disable 168");
            sb.AppendLine();
            sb.AppendLine($"namespace {Namespace}");
            sb.AppendLine("{");
            sb.AppendLine("    using System;");
            sb.AppendLine("    using Utf8Json;");
            sb.AppendLine();

            foreach (var objInfo in objectSerializationInfos)
            {
                sb.AppendLine($"    public sealed class {objInfo.Name}Formatter : global::Utf8Json.IJsonFormatter<{objInfo.FullName}>");
                sb.AppendLine("    {");
                sb.AppendLine("        readonly global::Utf8Json.Internal.AutomataDictionary ____keyMapping;");
                sb.AppendLine("        readonly byte[][] ____stringByteKeys;");
                sb.AppendLine();
                sb.AppendLine($"        public {objInfo.Name}Formatter()");
                sb.AppendLine("        {");
                sb.AppendLine("            this.____keyMapping = new global::Utf8Json.Internal.AutomataDictionary()");
                sb.AppendLine("            {");

                var index = 0;
                foreach (var x in objInfo.Members)
                {
                    sb.AppendLine($"                {{ JsonWriter.GetEncodedPropertyNameWithoutQuotation(\"{x.Name}\"), {index++} }},");
                }

                sb.AppendLine("            };");
                sb.AppendLine();
                sb.AppendLine("            this.____stringByteKeys = new byte[][]");
                sb.AppendLine("            {");

                index = 0;
                foreach (var x in objInfo.Members.Where(x => x.IsReadable))
                {
                    if (index++ == 0)
                    {
                        sb.AppendLine($"                JsonWriter.GetEncodedPropertyNameWithBeginObject(\"{x.Name}\"),");
                    }
                    else
                    {
                        sb.AppendLine($"                JsonWriter.GetEncodedPropertyNameWithPrefixValueSeparator(\"{x.Name}\"),");
                    }
                }

                sb.AppendLine("                ");
                sb.AppendLine("            };");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine($"        public void Serialize(ref JsonWriter writer, {objInfo.FullName} value, global::Utf8Json.IJsonFormatterResolver formatterResolver)");
                sb.AppendLine("        {");

                if (objInfo.IsClass)
                {
                    sb.AppendLine("            if (value == null)");
                    sb.AppendLine("            {");
                    sb.AppendLine("                writer.WriteNull();");
                    sb.AppendLine("                return;");
                    sb.AppendLine("            }");
                }

                sb.AppendLine("            ");
                sb.AppendLine();

                index = 0;
                foreach (var x in objInfo.Members.Where(x => x.IsReadable))
                {
                    sb.AppendLine($"            writer.WriteRaw(this.____stringByteKeys[{index++}]);");
                    sb.AppendLine($"            {x.GetSerializeMethodString()};");
                }

                sb.AppendLine("            ");
                sb.AppendLine("            writer.WriteEndObject();");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine($"        public {objInfo.FullName} Deserialize(ref JsonReader reader, global::Utf8Json.IJsonFormatterResolver formatterResolver)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (reader.ReadIsNull())");
                sb.AppendLine("            {");

                if (objInfo.IsClass)
                {
                    sb.AppendLine("                return null;");
                }
                else
                {
                    sb.AppendLine("                throw new InvalidOperationException(\"typecode is null, struct not supported\");");
                }

                sb.AppendLine("            }");
                sb.AppendLine("            ");

                if (!objInfo.HasConstructor)
                {
                    sb.AppendLine();
                    sb.AppendLine("\t        throw new InvalidOperationException(\"generated serializer for IInterface does not support deserialize.\");");
                }
                else
                {
                    sb.AppendLine();

                    foreach (var x in objInfo.Members)
                    {
                        sb.AppendLine($"            var __{x.MemberName}__ = default({x.Type});");
                        sb.AppendLine($"            var __{x.MemberName}__b__ = false;");
                    }

                    sb.AppendLine(@"
            var ____count = 0;
            reader.ReadIsBeginObjectWithVerify();
            while (!reader.ReadIsEndObjectWithSkipValueSeparator(ref ____count))
            {
                var stringKey = reader.ReadPropertyNameSegmentRaw();
                int key;
                if (!____keyMapping.TryGetValueSafe(stringKey, out key))
                {
                    reader.ReadNextBlock();
                    goto NEXT_LOOP;
                }

                switch (key)
                {");

                    index = 0;
                    foreach (var x in objInfo.Members)
                    {
                        sb.AppendLine($"                    case {index++}:");
                        sb.AppendLine($"                        __{x.MemberName}__ = {x.GetDeserializeMethodString()};");
                        sb.AppendLine($"                        __{x.MemberName}__b__ = true;");
                        sb.AppendLine("                        break;");
                    }

                    sb.AppendLine("                    default:");
                    sb.AppendLine("                        reader.ReadNextBlock();");
                    sb.AppendLine("                        break;");
                    sb.AppendLine("                }");
                    sb.AppendLine();
                    sb.AppendLine("        NEXT_LOOP:");
                    sb.AppendLine("                continue;");
                    sb.AppendLine("            }");
                    sb.AppendLine();
                    sb.AppendLine($"            var ____result = new {objInfo.GetConstructorString()};");

                    foreach (var x in objInfo.Members.Where(x => x.IsWritable))
                    {
                        sb.AppendLine($"            if(__{x.MemberName}__b__) ____result.{x.MemberName} = __{x.MemberName}__;");
                    }

                    sb.AppendLine();
                    sb.AppendLine("            return ____result;");
                }

                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("#pragma warning disable 168");
            sb.AppendLine("#pragma warning restore 219");
            sb.AppendLine("#pragma warning restore 414");
            sb.AppendLine("#pragma warning restore 618");
            sb.AppendLine("#pragma warning restore 612");

            return sb.ToString();
        }
    }
}
#endif
