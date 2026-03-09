# Utf8Json Unity Editor 代码生成器

## 功能介绍

本工具将 `Utf8Json.UniversalCodeGenerator` 的核心代码生成逻辑移植到了 Unity Editor 脚本中，使其能在 Unity Build 时自动生成 Resolver（`IJsonFormatterResolver`）代码，无需外部 `.exe` 工具。

生成的代码适用于 **IL2CPP (AOT)** 环境，解决了在 iOS/WebGL 等平台因动态代码生成受限而导致的序列化问题。

---

## 安装步骤

### 1. 将文件放入 Unity 项目

将整个 `Unity/Editor/` 目录复制到你的 Unity 项目的 `Assets/` 目录下，例如：

```
Assets/
└── Plugins/
    └── Utf8JsonCodeGen/
        ├── CodeAnalysis/
        │   ├── Definitions.cs
        │   └── TypeCollector.cs
        ├── Generator/
        │   ├── FormatterTemplate.cs
        │   ├── ResolverTemplate.cs
        │   └── TemplatePartials.cs
        ├── Roslyn/
        │   └── RoslynExtensions.cs
        └── Utf8JsonEditorGenerator.cs
```

> **重要**：以上所有脚本都必须放在 `Editor` 文件夹（或其子文件夹）中，或者确保它们只在编辑器模式下编译（已通过 `#if UNITY_EDITOR` 保护）。

### 2. 获取 Roslyn 依赖

本工具依赖以下 Roslyn NuGet 包：

- `Microsoft.CodeAnalysis.CSharp`（通常包含 `Microsoft.CodeAnalysis.Common`）

#### 推荐方式：从 NuGet 获取

1. 在 Unity 项目外，创建一个临时的 .NET 控制台项目
2. 添加 NuGet 包：
   ```
   dotnet add package Microsoft.CodeAnalysis.CSharp --version 3.x.x
   ```
3. 构建后，从 `bin/Release/netstandard2.0/` 目录中取出以下 DLL：
   - `Microsoft.CodeAnalysis.dll`
   - `Microsoft.CodeAnalysis.CSharp.dll`
   - `System.Runtime.CompilerServices.Unsafe.dll`（如需）
4. 将 DLL 放入 Unity 项目的 `Assets/Plugins/Editor/` 目录

#### 或使用 Unity Package Manager（实验性）

可通过 [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity) 直接在 Unity 中安装 Roslyn 包。

---

## 配置说明

在 `Utf8JsonEditorGenerator.cs` 的静态字段中修改配置：

```csharp
// 要扫描的输入目录列表（支持多个目录）
public static List<string> InputDirectories = new List<string>
{
    Path.Combine(Application.dataPath, "Scripts"),
    // 添加其他目录...
};

// 生成代码的输出文件路径
public static string OutputPath = Path.Combine(Application.dataPath, "Utf8Json.Generated.cs");

// 生成的 Resolver 类名
public static string ResolverName = "GeneratedResolver";

// 生成代码的命名空间根
public static string NamespaceRoot = "Utf8Json";

// 条件编译符号
public static List<string> ConditionalSymbols = new List<string>();

// 是否包含 internal 类型
public static bool AllowInternal = false;
```

你也可以在 `[InitializeOnLoad]` 静态构造器或其他编辑器初始化代码中动态设置这些配置。

---

## 使用方式

### 自动生成（推荐）

工具实现了 `IPreprocessBuildWithReport` 接口，**每次 Build 前**会自动调用代码生成。无需任何额外操作。

如果生成失败，Build 会被中断并显示错误信息。

### 手动生成

通过 Unity 菜单手动触发：

**Tools → Utf8Json → Generate Resolver**

生成完毕后，Unity 会自动刷新 Asset 数据库。

---

## 运行时注册 Resolver

生成的文件（如 `Utf8Json.Generated.cs`）包含 `GeneratedResolver` 类，位于 `Utf8Json.Resolvers` 命名空间下。

在游戏启动时注册此 Resolver（建议在 `RuntimeInitializeOnLoadMethod` 中）：

```csharp
using Utf8Json;
using Utf8Json.Resolvers;
using UnityEngine;

public static class JsonStartup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        CompositeResolver.RegisterAndSetAsDefault(
            GeneratedResolver.Instance,
            StandardResolver.Default
        );
    }
}
```

---

## 注意事项

1. **Editor Only**：所有代码生成脚本均用 `#if UNITY_EDITOR` 包裹，不会被打入运行时包体。

2. **Roslyn DLL 必须放在 `Editor` 文件夹**：确保 Roslyn 相关 DLL 只在编辑器下加载，避免增大运行时包体。

3. **生成文件不应被排除于版本控制**：建议将生成的 `Utf8Json.Generated.cs` 提交到版本库，以便 CI/CD 环境中不需要 Unity 编辑器也能编译。

4. **IL2CPP 平台**：本工具专为 AOT 平台（iOS、WebGL、某些主机平台）设计。在 Mono 运行时平台（PC、Mac、Android）上，即使不使用生成的 Resolver，Utf8Json 也可通过反射正常工作。

5. **T4 模板**：原始 `Utf8Json.UniversalCodeGenerator` 使用 T4 模板生成代码。本工具用手写的 `StringBuilder` 替代了 T4 运行时，在 Unity Editor 环境中完全兼容。

6. **DataMember 属性**：需要序列化的类型应标注 `[DataContract]`/`[DataMember]` 属性。使用 `[IgnoreDataMember]` 排除不需要序列化的字段/属性。

---

## 目录结构参考

```
Unity/
├── Editor/
│   └── Utf8JsonCodeGen/
│       ├── CodeAnalysis/
│       │   ├── Definitions.cs       ← 类型定义（ObjectSerializationInfo 等）
│       │   └── TypeCollector.cs     ← Roslyn 类型收集器
│       ├── Generator/
│       │   ├── FormatterTemplate.cs ← IJsonFormatter 代码生成模板
│       │   ├── ResolverTemplate.cs  ← IJsonFormatterResolver 代码生成模板
│       │   └── TemplatePartials.cs  ← 模板 partial class 属性定义
│       ├── Roslyn/
│       │   └── RoslynExtensions.cs  ← Roslyn 扩展方法
│       └── Utf8JsonEditorGenerator.cs ← Unity Editor 入口脚本
└── README.md                          ← 本文档
```
