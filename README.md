# Stellar 项目说明

> 本文档根据当前仓库中的项目文件和代码结构编写，介绍应用的用途、组成、配置、数据存储与构建入口。部分模块涉及游戏网络流量处理；文档只描述其架构位置和可见的界面配置，不提供流量拦截、修改、注入或规避检测的操作说明。使用本软件前，请确认符合相关游戏、平台和网络服务的规则。

## 1. 项目概览

Stellar 是一个面向 Windows 的桌面应用程序，使用 C#、.NET 6 和 WPF 构建。程序围绕 Destiny 2 相关的运行状态与辅助界面组织，包含主控制窗口、可选的游戏内覆盖层、日志浏览、服务设置、脚本管理、快捷键和模块管理等部分。

项目同时包含网络数据提供器、数据包模型和多个游戏相关模块。它们由统一的管理器注册，并与 UI、配置及日志/数据库设施协作。此 README 不展开这些模块的网络处理实现细节。

### 主要能力一览

- **主窗口**：显示时间、连接信息和当前模块设置，并提供侧边栏操作。
- **覆盖层**：可跟随目标游戏窗口显示时间、计时、计数和已启用模块信息；支持偏移量及显示选项。
- **模块与快捷键**：选择模块、查看说明、启停监听、配置热键以及模块相关选项。
- **日志浏览器**：按时间、类型和连接等条件浏览本地日志及已保存的数据记录。
- **服务设置**：集中管理覆盖层、显示、记录及其他应用选项。
- **脚本管理器**：列出脚本目录中的 AutoHotkey 脚本，并提供管理界面。
- **本地配置与 SQLite 数据库**：配置写入程序目录下的 `Stellar.cfg`；记录数据库使用 SQLite 文件 `data.db`。

## 2. 技术栈与运行环境

| 项目 | 当前仓库设置 |
|---|---|
| 操作系统 | Windows |
| UI | WPF |
| 语言 | C# |
| 目标框架 | `net6.0-windows` |
| 数据访问 | Entity Framework Core + SQLite |
| 网络处理依赖 | `WindivertDotnet` |
| 配置序列化 | `System.Text.Json` |
| 通知区域图标 | `Hardcodet.Wpf.TaskbarNotification` |
| 其他 UI / 数据包依赖 | `MathConverter`、`OxyPlot.Wpf`、`DotNetBungieAPI` 等 |

项目文件为 `Stellar.csproj`，解决方案为 `Stellar.sln`。项目启用了 WPF 和 Nullable Reference Types，并包含 Windows 原生 API 调用（例如窗口效果及窗口管理相关调用）。因此它不是跨平台应用，普通非 Windows 环境不能直接运行其桌面界面。

## 3. 目录结构

```text
.
├── App.xaml / App.xaml.cs          WPF 应用入口、窗口效果和通用窗口辅助逻辑
├── Stellar.sln                     Visual Studio 解决方案
├── Stellar.csproj                   项目框架、资源和 NuGet 依赖
├── Database\                       SQLite 数据模型与 EF Core DbContext
├── Interception\                   数据提供器、模块注册与相关模型处理
│   ├── Modules\                    各个游戏相关模块
│   └── PacketProviders\            数据提供器实现
├── Models\                         配置模型、键码、数据包及协议辅助模型
├── Utility\                        配置、日志、身份检查、更新、按键等通用服务
└── Windows\                        WPF 窗口、控件、转换器及视觉资源
	├── Assets\                     图标、字体和控件/图标资源
	├── Controls\                   可复用 WPF 控件
	└── Converters\                 数据转换器及图像/效果辅助代码
```

仓库中还可能包含 `bin\` 和 `obj\` 等构建产物目录；它们不是主要源代码，通常由构建流程生成。

## 4. 应用启动与组件关系

### 4.1 启动入口

- `App.xaml` 定义 WPF 应用级资源和启动行为。
- `App.xaml.cs` 提供应用目录解析、窗口模糊/圆角效果、窗口拖动等通用辅助逻辑。
- `MainWindow.xaml.cs` 构造主窗口并初始化应用层服务，例如键盘监听、模块管理、脚本管理，以及启动时的 UI 设置。

### 4.2 模块管理

`InterceptionManager` 是提供器和模块的注册中心。它创建提供器与模块实例，并根据配置恢复启用状态。模块基类 `PacketModuleBase` 统一处理模块名称、状态、监听生命周期、快捷键订阅、声音提示及提供器订阅关系。各具体模块位于 `Interception\Modules\`。

当前由 `InterceptionManager.Init()` 注册的模块如下。表格只概括源码中的职责类别和界面关联，不构成操作指南；模块最终行为以当前版本源码、界面描述和配置为准。

| 显示名称 | 类型 | 关联提供器/组件 | 说明 |
|---|---|---|---|
| `PVE` | `PveModule` | Xbox 提供器 | 有多个方向/模式开关、相应快捷键、缓存和自动重同步相关设置；设置区由主窗口的 PVE 专用面板呈现。 |
| `PVP` | `PvpModule` | Players 提供器 | 有方向开关、快捷键、缓存和自动重同步相关设置；设置区由 PVP 专用面板呈现。 |
| `Multishot` | `MultishotModule` | Players、Xbox | 有时间限制、检测、模式、方向和可切换行为等设置；构造时从配置还原；源码描述标记为 `[PATCHED]`。 |
| `Reconnect` | `ReconnectModule` | 30000 提供器 | 一次性动作类模块；描述涉及连接/实例状态。 |
| `Revive` | `ResModule` | Players 提供器 | 一次性动作类模块；类名为 `ResModule`，界面名称为 `Revive`。 |
| `API Block` | `ApiModule` | 7500 提供器 | 有自动禁用和缓存相关选项，由 API 专用设置面板显示。 |
| `Timer` | `TimerModule` | 无 | 简单的可切换计时模块，提供时钟图标。 |
| `GamePauser` | `PauserModule` | Windows 进程 API | 控制本地游戏进程暂停/恢复的模块；属于本机进程操作，不是数据提供器模块。 |
| `Solo` | `SoloModule` | Players 提供器 | 简单状态切换模块，界面描述为 matchmaking 相关。 |
| `З0K` | `InstanceModule` | 30000 提供器 | 有缓存选项；名称使用西里尔字母 `З`，配置键必须与代码中的名称一致。 |

`KickModule` 文件存在于 `Interception\Modules\KickModule.cs`，但当前 `InterceptionManager.Init()` 的注册列表中没有它，因此它不是通过当前常规模块列表初始化的模块。添加/启用模块前，需要确认注册、界面和配置路径一致。

模块基类（`PacketModuleBase`）处理公共的启停状态、热键订阅、提供器订阅、提示音、启用时间和统一日志。模块构造函数通常设置显示名称、图标、用户可读说明、所属提供器，并从 `Config.GetNamed(name)` 读取自身设置。模块 UI 不完全采用通用数据绑定：主窗口的 `UpdateSelectedModule()` 会按具体模块类型显示对应设置面板。

### 4.3 数据提供器与界面

`PacketProviderBase` 为提供器定义公共生命周期和模块订阅机制；具体提供器放在 `Interception\PacketProviders\`。模块通过基类与提供器建立关系。网络数据模型位于 `Models\`，本地数据记录模型位于 `Database\`。

UI 通过 WPF XAML 声明布局，再由 code-behind 处理按钮、窗口和设置行为。主窗口的侧栏入口定义在 `Windows\MainWindow.xaml`，对应交互主要位于 `Windows\MainWindowButtonHandlers.xaml.cs`。

## 5. 窗口与界面组成

| 窗口/界面 | 代码位置 | 用途概要 |
|---|---|---|
| 主窗口 | `Windows\MainWindow.xaml`、`MainWindow.xaml.cs` | 主状态面板、模块选择和模块设置；包含退出、最小化到通知区域、置顶、覆盖层、日志、设置、脚本和音量入口。 |
| 主窗口按钮处理 | `Windows\MainWindowButtonHandlers.xaml.cs` | 按钮点击、面板显示/关闭、快捷键及模块设置事件处理。 |
| 覆盖层 | `Windows\OverlayWindow.xaml`、`OverlayWindow.xaml.cs` | 游戏窗口上的透明覆盖层，展示配置启用的状态信息。 |
| 服务设置 | `Windows\ServiceWindow.xaml`、`ServiceWindow.xaml.cs` | 编辑覆盖层、追踪、记录、显示和脚本等设置。 |
| 日志浏览器 | `Windows\LogsViewer.xaml`、`LogsViewer.xaml.cs` | 查询并查看本地日志和数据库记录。 |
| 脚本管理器 | `Windows\AhkManagerWindow.xaml`、`AhkManagerWindow.xaml.cs` | 浏览和管理脚本目录中的 AutoHotkey 文件。 |
| 启动进度窗口 | `Windows\StartupProgressBar.xaml`、`StartupProgressBar.xaml.cs` | 应用启动期间的进度/状态显示。 |

可复用控件集中在 `Windows\Controls\`，例如自定义按钮、复选框、连接信息面板、模块计时器和窗口控制按钮。`Windows\Assets\ControlTemplates.xaml`、`Icons.xaml` 提供模板和图标资源。

### 5.1 主窗口区域与侧栏

主界面布局由 `MainWindow.xaml` 描述，主体分为两列：左侧 `MainContent` 展示信息和模块设置，右侧 `RightControls` 是固定宽度的垂直按钮栏。主内容包含：

1. **顶部信息块 `TopBlock`**：当前时间、`EnabledModules` 活动模块计时条目和连接信息 `Speed`。
2. **模块设置块 `ModuleSettingsBorder`**：当前模块图标/标题、说明折叠面板、通用启停与热键控件，以及条件显示的模块专属设置。
3. **模块选择器 `ModuleSelection`**：主窗口初始化时根据 `InterceptionManager.Modules` 动态创建模块按钮；按钮名由模块名称空格替换为下划线，用于 UI 查找。
4. **右侧控制栏**：退出、最小化到通知区域、置顶、覆盖层、日志、服务设置、脚本管理、音量面板及音量滑块。点击事件分别在 `MainWindowButtonHandlers.xaml.cs` 中实现。

模块快捷键按钮进入录制状态时，主窗口会暂时取消模块快捷键订阅，收集按下的键组合，然后显示新的组合并保存配置；`Esc` 表示清空。全局输入事件由 `KeyListener` 发出，模块与主窗口通过事件订阅分发。

### 5.2 覆盖层

`OverlayWindow` 是透明、置顶、无任务栏图标的 WPF 窗口。其布局按基准画布组织，再根据目标窗口尺寸缩放和定位；`TryFollowWindow()` 会计算窗口宽高比、偏移和缩放，配置的左侧/底部偏移会参与定位。覆盖层定时检查目标游戏窗口的前台状态，并按设置更新活动模块、时间和计数；游戏不在前台时会隐藏覆盖层。覆盖层显示的可选信息来自模块状态、`D2CharacterTracker` 和 `XboxProvider` 的活动时间状态。

### 5.3 日志浏览器与服务窗口

`LogsViewer` 维护日志和连接信息的可观察集合。加载时初始化筛选控件、默认时间范围及日志类别；刷新逻辑根据时间、记录类型、连接选择和长度条件分批查询 SQLite，然后更新界面集合。具体图表/统计展示以 XAML 中实际启用的控件为准。

`ServiceWindow` 在界面加载后把 `Config.Instance.Settings` 的值填入控件；设置变更通常立即写配置。偏移输入会要求整数并尝试让已打开的覆盖层重新定位。Bungie 名称输入经过格式检查，再交给 `D2CharacterTracker` 查询；成功后写入配置。

## 6. 配置说明

配置实现位于 `Utility\Config.cs`，数据模型位于 `Models\ConfigModel.cs`。配置文件名为 `Stellar.cfg`，按当前代码从应用程序目录读取或写入，并使用 JSON 序列化内容。

配置主要包含以下类别：

- **主界面和音量**：当前模块、音量等。
- **模块设置**：模块启用状态、键位组合及各模块自有选项。
- **覆盖层**：启动时是否启用、是否显示时钟/计时器、失去活动状态时的行为、显示筛选及 X/Y 偏移。
- **主窗口显示**：时钟、连接速度信息及计时显示时长。
- **追踪选项**：名称字段及活动计数相关选项。
- **记录选项**：是否保存数据记录、是否记录按键事件。
- **快捷键行为**：切换到其他窗口时是否抑制模块快捷键。
- **脚本选项**：AutoHotkey 的自动启动/关闭相关开关及上次打开脚本列表。

配置字段的默认值定义在 `Settings` 类中；具体模块配置在 `ModuleSettingsBase` 中。下面列出主要设置键，名称对应 `Models\ConfigModel.cs`：

| 配置区域 | 字段 | 代码中的用途 |
|---|---|---|
| 全局 | `CurrentModule` | 下次打开时选中的模块名称。 |
| 全局 | `Volume` | 模块提示音的音量百分比；变化时同步到模块的启用/禁用声音播放器。 |
| 身份 | `Username`、`Password` | 配置模型中存在的字符串字段；文档不假设它们当前均被实际读取。请勿分享包含这些字段的配置文件。 |
| 模块集合 | `Modules` | 以模块名称为键，保存启用状态、键位组合及模块特定设置。 |
| 脚本 | `LastOpenAhks` | 保存退出前记录到的已运行脚本名称列表，供启动逻辑读取。 |
| 追踪 | `Tracker_BungieName` | 账户标识字符串；为空时不按配置尝试加载账户。 |
| 追踪 | `Tracker_CountRaids`、`Tracker_CountDungeons` | 控制覆盖层计数纳入的活动类型。 |
| 输入 | `AltTabSupressKeybinds` | 在目标游戏未处于前台时是否抑制模块快捷键。 |
| 覆盖层 | `Overlay_StartOnLaunch` | 主窗口初始化时是否调用覆盖层开关逻辑。 |
| 覆盖层 | `Overlay_ShowTime`、`Overlay_ShowTimer` | 控制覆盖层显示当前时间或活动计时。 |
| 覆盖层 | `Overlay_DisableOnInactivity` | 覆盖层基于活动状态隐藏/淡出的选项。 |
| 覆盖层 | `Overlay_DisplayOnlyTogglable` | 控制覆盖层是否仅显示可切换模块。 |
| 覆盖层 | `Overlay_LeftOffset`、`Overlay_BottomOffset` | 覆盖层位置偏移量（整数）。 |
| 主窗口 | `Window_DisplayClock`、`Window_DisplaySpeed` | 控制主窗口是否显示时钟和连接信息面板。 |
| 主窗口 | `Window_TimerDecaySeconds` | 已停用的活动模块计时条目保留/淡出时间。 |
| 数据库 | `DB_SavePackets` | 是否保存数据提供器汇总的记录。 |
| 数据库 | `DB_KeyPresses` | 是否将按键事件交给日志系统记录。应审慎使用并注意本地隐私。 |
| 脚本 | `AHK_AutoClose`、`AHK_AutoOpen` | 脚本窗口及应用启动/退出流程读取的自动管理开关；以设置界面和具体代码为准。 |

模块项由 `ModuleSettingsBase` 提供：`Enabled` 记录是否监听，`Keybind` 保存模块主热键，`Settings` 用字典保存各模块扩展字段。`GetSettings<T>()` 在读取不存在的字段时设置默认值并触发保存；对于 JSON 反序列化得到的 `JsonElement`，当前代码专门处理布尔、双精度数和键位列表。扩展设置时需确认 JSON 类型与读写类型一致。

模块配置中的字段名是代码的一部分，主要包括：

| 模块配置键 | 字段示例 | 用途概要 |
|---|---|---|
| `PVE` | `Buffer`、`AutoResync`、`OutboundKeybind`、`SlowInboundKeybind`、`SlowOutboundKeybind` | PVE 面板开关和多个键位组合。 |
| `PVP` | `Buffer`、`AutoResync`、`OutboundKeybind` | PVP 面板选项和键位组合。 |
| `Multishot` | `TimeLimit`、`PlayersMode`、`ShotDetection`、`Inbound`、`Outbound`、`Togglable`、`PlayersKeybind` | 模块时间、检测、模式、方向和切换状态。 |
| `API Block` | `SelfDisable`、`Buffer` | 自动禁用和缓存选项。 |
| `З0K` | `Buffer` | 名称对应的模块缓存选项。注意首字符是西里尔字母而非拉丁字母。 |

其他已注册模块可能只使用通用 `Enabled` 和 `Keybind`，没有额外设置字段。字段的初始默认值不一定都集中在 `Settings` 类：模块自身可在构造函数中读取或提供默认值，新增字段时应检查具体使用处和 `Config.Save()`。

配置模型中声明的默认值（没有旧配置覆盖时）包括：追踪名称为空、Raid/Dungeon 计数开关为开启、切换窗口时抑制热键为开启；覆盖层启动为开启、显示时间为关闭、显示活动计时为开启、非活动时隐藏为开启、仅显示可切换模块为关闭、两个偏移均为 0；主窗口时钟和速度显示为开启、计时条目衰减为 10 秒；数据包保存为开启、按键日志为关闭；脚本自动关闭选项为开启、自动打开选项为关闭。新配置的音量默认值在配置初始化处设为 30。读取时的这些默认值与已有配置值、模块构造时赋值以及运行期间的 UI 修改共同决定最终状态。

配置文件可能包含用户名、密码字段、个人设置或键位信息。分享日志或配置之前应先检查并移除个人信息及其他敏感内容。

## 7. 本地数据与隐私

数据库上下文位于 `Database\StopwatchDbContext.cs`，数据库文件名为 `data.db`，连接字符串为相对路径；应用入口会将工作目录切换到程序目录。当前数据模型包含：

- `packets`：数据记录及时间、方向、地址、端口、长度、标志等字段。
- `log`：日志类型、文本和创建时间。

日志浏览器使用这些模型读取本地数据。数据库内容可能包含网络元数据或数据载荷，日志也可能包含错误详情、使用时间及按键记录（若相关选项开启）。在提交、分享或上传 `data.db`、`Stellar.cfg`、日志文件或构建包之前，请先检查内容并删除不应公开的信息。

## 8. 构建与开发

### 使用 Visual Studio

1. 使用支持 `.NET 6` 和 WPF 桌面开发的 Visual Studio 打开 `Stellar.sln`。
2. 还原 NuGet 依赖。
3. 选择 `Stellar` 项目及合适的配置（如 Debug），再执行生成。

### 使用 .NET CLI

在 Windows 且已安装兼容的 .NET SDK 时，可在仓库根目录执行：

```powershell
dotnet restore .\Stellar.csproj
dotnet build .\Stellar.csproj
```

构建和运行桌面应用需使用 Windows 环境。实际运行还依赖项目使用的第三方库、Windows 原生组件和本地配置；本 README 不包含网络处理组件的安装或启用指南。

### 主要 NuGet 包

项目依赖的版本以 `Stellar.csproj` 为准，主要包括：

- `DotNetBungieAPI`
- `Hardcodet.NotifyIcon.Wpf`
- `MathConverter`
- `Microsoft.EntityFrameworkCore.Sqlite`
- `OxyPlot.Wpf`
- `SharpCompress`
- `SQLitePCLRaw.lib.e_sqlite3`
- `System.Management`
- `System.ServiceProcess.ServiceController`
- `System.Text.Json`
- `WindivertDotnet`

修改包版本时应检查对应框架兼容性及其原生运行依赖，并在 Windows 环境重新构建验证。

## 9. 日志、故障定位与维护入口

- `Utility\Logger.cs`：应用日志入口；错误记录和模块状态信息由多个子系统调用。
- `Windows\LogsViewer.xaml.cs`：日志查询界面和筛选逻辑。
- `Utility\Config.cs`：配置读写和模块设置持久化。
- `Interception\InterceptionManager.cs`：模块及提供器注册、启动状态恢复。
- `Interception\PacketModuleBase.cs`：模块启停、快捷键订阅和统一状态管理。
- `Interception\PacketProviderBase.cs`：提供器抽象和公共队列/生命周期处理。
- `Database\StopwatchDbContext.cs`、`Database\DbModels.cs`：本地数据库结构。

排查界面问题时，先检查对应 XAML 与 code-behind；排查模块配置问题时，检查配置模型、配置读写以及对应模块；排查本地记录问题时，检查数据库模型、DbContext 和日志浏览器查询逻辑。涉及网络行为的排查应在获授权且安全的环境中进行，不应把真实用户或第三方服务的数据作为未经授权的测试对象。

## 10. 源码逐文件导航

本节按源代码职责列出最常用文件，方便从界面行为追踪到配置、数据和公共服务。XAML 窗口通常有同名 `.xaml.cs` 文件处理事件；图标、颜色、字体等资源集中在 `Windows\Assets\`。

### 10.1 应用与项目文件

| 文件 | 职责 |
|---|---|
| `App.xaml` | WPF 资源、启动窗口/样式等应用级声明。 |
| `App.xaml.cs` | 应用目录定位、窗口模糊和圆角等 Windows 窗口效果、标题栏/边框拖动辅助。 |
| `AssemblyInfo.cs` | 程序集元信息。 |
| `Stellar.csproj` | 框架目标、WPF 配置、资源项、包引用和应用清单入口。 |
| `Stellar.sln` | Visual Studio 解决方案入口。 |
| `app.manifest` | Windows 应用清单。 |

### 10.2 窗口和窗口事件

| 文件 | 责任边界 |
|---|---|
| `Windows\MainWindow.xaml` | 主窗口布局、主侧栏、时间/速度显示、模块选择和设置区域。 |
| `Windows\MainWindow.xaml.cs` | 主窗口初始化、模块按钮动态生成、活动模块展示、时钟刷新、全局热键录制和模块专属设置状态回填。 |
| `Windows\MainWindowButtonHandlers.xaml.cs` | 侧栏按钮、音量滑块、模块选择、面板显隐、通用模块启停及各设置控件的事件处理。 |
| `Windows\OverlayWindow.xaml` | 透明覆盖层的基准画布、模块条目、计时及计数 UI。 |
| `Windows\OverlayWindow.xaml.cs` | 覆盖层显示周期、游戏前台检测、目标窗口跟随、模块条目刷新和活动信息显示。 |
| `Windows\ServiceWindow.xaml` | 服务设置界面布局。 |
| `Windows\ServiceWindow.xaml.cs` | 设置加载/写回、偏移更新、账户标识查找、覆盖层及日志相关选项处理。 |
| `Windows\LogsViewer.xaml` | 日志筛选、记录列表和连接信息的可视布局。 |
| `Windows\LogsViewer.xaml.cs` | 记录查询、分批加载、筛选、连接统计集合更新。 |
| `Windows\AhkManagerWindow.xaml` | 脚本列表和管理按钮布局。 |
| `Windows\AhkManagerWindow.xaml.cs` | 脚本列表刷新、启动/停止事件及打开脚本文件夹；同文件的 `AhkManager` 负责脚本发现和进程管理。 |
| `Windows\StartupProgressBar.xaml`、`.xaml.cs` | 启动阶段的进度窗口。 |

### 10.3 模块与输入

| 文件 | 职责 |
|---|---|
| `Interception\InterceptionManager.cs` | 提供器和模块实例的注册表；只初始化一次，并按配置恢复模块监听状态。 |
| `Interception\PacketModuleBase.cs` | 模块公共基类：状态字段、热键订阅/检查、启停、声音、计时起点和提供器订阅。 |
| `Interception\Modules\PveModule.cs` | PVE 模块及其专属开关/快捷键状态。 |
| `Interception\Modules\PvpModule.cs` | PVP 模块及其专属开关/快捷键状态。 |
| `Interception\Modules\MultishotModule.cs` | Multishot 模块、配置恢复和模块专属时间/检测/模式选项。 |
| `Interception\Modules\ApiModule.cs` | API Block 模块及自动禁用/缓存选项。 |
| `Interception\Modules\InstanceModule.cs` | 名称为 `З0K` 的活动/实例模块及缓存选项。 |
| `Interception\Modules\ReconnectModule.cs` | Reconnect 一次性模块。 |
| `Interception\Modules\ResModule.cs` | Revive 一次性模块。 |
| `Interception\Modules\TimerModule.cs` | Timer 状态切换模块。 |
| `Interception\Modules\PauserModule.cs` | GamePauser 本地进程控制模块。 |
| `Interception\Modules\SoloModule.cs` | Solo 模块。 |
| `Interception\Modules\KickModule.cs` | 仓库中存在但当前管理器没有注册的模块类。 |
| `Utility\KeyListener.cs` | Windows 键盘/鼠标低级输入监听、事件队列及按下键集合；按键事件会广播给订阅者。 |
| `Models\Keycode.cs` | 键盘与鼠标键的虚拟键码枚举。 |

### 10.4 提供器、模型和数据

| 文件 | 职责 |
|---|---|
| `Interception\PacketProviderBase.cs` | 提供器抽象基类，管理订阅者、输入队列、连接缓存、异步处理/保存生命周期和资源释放。 |
| `Interception\PacketProviders\PlayersProvider.cs` | Players 类数据源实现。 |
| `Interception\PacketProviders\XboxProvider.cs` | Xbox 类数据源实现，并提供覆盖层/活动追踪使用的状态。 |
| `Interception\PacketProviders\30000_Provider.cs` | 30000 类 TCP 数据源实现。 |
| `Interception\PacketProviders\7500_Provider.cs` | 7500 类 TCP 数据源实现。 |
| `Interception\TcpReordering.cs` | TCP 连接相关状态及顺序辅助结构，供相关代码使用。 |
| `Interception\PacketExtensions.cs` | 数据模型上的协议/场景判断和辅助扩展。 |
| `Models\Packet.cs` | 统一的数据包对象：原生对象引用、地址/端口、方向、时间、标志、来源提供器及保存/延迟状态。 |
| `Models\TcpFlags.cs` | TCP 标志枚举。 |
| `Models\Structs.cs` | Windows/Native interop 使用的结构声明。 |
| `Models\ConfigModel.cs` | 全局配置、设置对象和模块设置容器。 |
| `Database\DbModels.cs` | SQLite 表模型 `DbPacket`、`DbLog` 和日志级别枚举。 |
| `Database\StopwatchDbContext.cs` | EF Core SQLite 上下文、表名和时间索引声明。 |

### 10.5 通用工具和 WPF 控件

| 文件 | 职责 |
|---|---|
| `Utility\Config.cs` | `Stellar.cfg` 的初始化、读取、模块设置访问和写回。 |
| `Utility\Logger.cs` | 日志级别入口；将启用的日志项异步写入 SQLite。 |
| `Utility\IdentityChecker.cs` | 身份/访问级别检查相关类型；服务窗口和主窗口会读取其名称、类型和时间信息。 |
| `Utility\D2CharacterTracker.cs` | 使用 Bungie API 客户端查询公开活动/角色数据，计算计数并辅助活动时间同步。 |
| `Utility\Updater.cs` | 版本清单检查、更新归档处理及补丁程序启动逻辑。 |
| `Utility\Crypto.cs` | 字节/字符串加密解密及 SHA-256 哈希帮助方法。 |
| `Utility\ExtensionMethods.cs` | JSON 序列化、WPF 显隐动画、字节格式化及其他跨模块扩展方法。 |
| `Windows\Controls\Button.xaml`、`.xaml.cs` | 自定义文本按钮。 |
| `Windows\Controls\Checkbox.xaml`、`.xaml.cs` | 自定义复选框和状态设置逻辑。 |
| `Windows\Controls\WindowControlButton.xaml`、`.xaml.cs` | 图标/窗口控制按钮及悬停外观。 |
| `Windows\Controls\EnabledModuleTimer.xaml`、`.xaml.cs` | 模块活动时间展示控件。 |
| `Windows\Controls\ConnectionInfoPanel.xaml`、`.xaml.cs` | 连接状态摘要面板。 |
| `Windows\Controls\CurrentTimeLabel.xaml`、`.xaml.cs` | 当前时间显示控件。 |
| `Windows\Controls\FilterCheckbox.xaml`、`.xaml.cs` | 日志筛选专用复选框。 |
| `Windows\Controls\ExitCommand.cs` | 通知区域菜单的退出命令。 |
| `Windows\Converters\*.cs` | 日期、十六进制、日志级别、数值、数据方向和发送状态转换器；`PincushionEffect.cs` 与着色器资源用于覆盖层视觉效果。 |
| `Windows\Assets\ControlTemplates.xaml`、`Icons.xaml` | 通用控件模板及图标/画笔等资源声明。 |

## 11. 关键运行流程

### 11.1 启动阶段（概念顺序）

1. WPF 创建应用和启动/主窗口；`App.ExeDirectory` 将程序目录作为相对文件操作的基准。
2. `MainWindow` 加载配置对象、身份检查对象和 UI 资源，创建 `KeyListener`。
3. 全局输入事件订阅建立后，`InterceptionManager.Init()` 注册提供器/模块，并按配置恢复模块监听。
4. `AhkManager.Init()` 准备脚本目录，并根据相关配置处理上次记录的脚本状态。
5. Loaded 阶段关闭启动进度窗口，依据配置显示主窗口区域、建立模块选择按钮、设置音量和当前模块，并启动 UI 定时刷新。
6. 如果配置启用启动覆盖层，主窗口会执行覆盖层开关逻辑。

上述是代码层面可见的组件关系摘要；实际初始化先后还受 WPF Dispatcher、异步任务和外部依赖影响。

### 11.2 模块启停与界面同步

- 模块监听状态和模块激活状态是不同概念：前者表示模块正在监听/订阅，后者表示模块内部功能处于活动状态。
- 基类负责公共监听生命周期；具体模块实现自己的 `Toggle()` 以及需要时重写数据处理策略。
- 用户点击主启停复选框时，`CurrentModuleToggle` 调用模块的 `StartListening()` 或 `StopListening()`，并同步模块按钮外观。
- 模块热键由 `KeyListener.KeysPressed` 事件分发；基类进行启用、节流和前台窗口条件检查，再执行切换。
- UI 定时刷新将活动模块同步到顶部列表，更新计时，并在配置时长后移除已结束条目。
- 模块专属设置控件直接写对应静态选项/模块状态，并调用 `Config.Save()`；切换模块时 `UpdateSelectedModule()` 把当前状态回填。

### 11.3 本地记录流程

- `PacketProviderBase` 将收到的对象包装为 `Models.Packet`，并维护短期运行队列和按远端连接组织的缓存。
- 各订阅模块的判断参与统一处理；处理生命周期由提供器负责，具体协议细节不在本 README 描述。
- 保存开关控制是否将应保留的记录写到 SQLite；`Logger` 负责应用事件/错误记录写入 `log` 表。
- `LogsViewer` 从 SQLite 读取记录并在 UI 线程更新 `ObservableCollection`。
- `DB_KeyPresses` 开启时，按键事件会被传给 `Logger.Key()`；按键日志可能包含用户实际按下的组合键，需明确告知用户并保护本地数据库。

## 12. 贡献与修改建议

- 遵循现有 WPF/XAML、C# 命名及目录组织。
- 新增可持久化设置时，同时检查配置模型、默认值、读写逻辑和设置界面。
- 新增模块时，检查注册、生命周期、UI 选择和配置恢复是否保持一致。
- 调整窗口行为时，检查重复点击、用户手动关闭、应用退出和窗口引用清理等生命周期场景。
- 修改数据模型时，评估现有 SQLite 文件的兼容性；当前代码使用 `EnsureCreated`，不应默认认为模型变更会自动迁移已有数据库。
- 不要提交 `bin\`、`obj\`、用户配置、数据库、日志、密钥或账户信息等生成物/敏感数据。
- 完成代码修改后，至少执行目标项目构建；行为变化应补充相应验证。
- 对新窗口应处理窗口实例的打开、重复点击、用户手动关闭和应用关闭；避免只在按钮事件中临时创建而无法同步生命周期。
- 对异步 UI 更新使用所属 Dispatcher；后台任务不要直接访问 WPF 控件。
- 对输入钩子、定时器、事件订阅和进程资源，修改后检查是否需要在停止/退出时解除订阅或释放。
- 配置字段命名应与 `ConfigModel`、`Config.Save()`、读取处及设置窗口一致；不要只新增 UI 控件而遗漏序列化。
- 列表控件更新应检查集合线程安全和重复项；UI 绑定集合一般需在 UI 线程修改。

## 13. 常见改动的定位路径

| 要修改的内容 | 建议先看这些文件 |
|---|---|
| 主窗口按钮、开关、面板显隐 | `Windows\MainWindow.xaml` → `Windows\MainWindowButtonHandlers.xaml.cs` |
| 某模块在选择列表中的图标/说明/出现状态 | 对应 `Interception\Modules\*.cs` → `InterceptionManager.Init()` → `MainWindow.xaml.cs` |
| 模块设置控件、设置默认值或保存 | `Windows\MainWindow.xaml` → `MainWindow.xaml.cs` 的 `UpdateSelectedModule()` → `MainWindowButtonHandlers.xaml.cs` → `Models\ConfigModel.cs` / `Utility\Config.cs` |
| 覆盖层布局和信息 | `Windows\OverlayWindow.xaml` → `OverlayWindow.xaml.cs` → `Utility\D2CharacterTracker.cs` 或对应状态来源 → `ServiceWindow` 配置处理 |
| 设置窗口新增选项 | `Windows\ServiceWindow.xaml` → `ServiceWindow.xaml.cs` → `Models\ConfigModel.cs` → `Utility\Config.cs` |
| 日志筛选或显示 | `Windows\LogsViewer.xaml` → `LogsViewer.xaml.cs` → `Database\DbModels.cs` / `StopwatchDbContext.cs` |
| 输入热键 | `Utility\KeyListener.cs` → `Models\Keycode.cs` → `PacketModuleBase.cs` 或主窗口热键录制代码 |
| 模块状态计时或提示音 | `PacketModuleBase.cs`、模块实现、`EnabledModuleTimer` 控件、`MainWindow.xaml.cs` 刷新逻辑 |
| 本地数据库字段 | `Database\DbModels.cs` → `StopwatchDbContext.cs` → 写入点/查询点；现有数据库更新策略需单独评估 |
| 脚本管理 | `Windows\AhkManagerWindow.xaml(.cs)` → 同文件中的 `AhkManager` → `ServiceWindow` 脚本设置和退出处理 |
| 资源/图标/动画 | `Windows\Assets\Icons.xaml`、`ControlTemplates.xaml`、`Windows\Controls\` 和 `Utility\ExtensionMethods.cs` |

## 14. 当前仓库的边界与注意事项

- `Stellar.csproj` 当前目标是 `net6.0-windows`；构建/运行环境应匹配 Windows、WPF 和相应 .NET SDK。
- 当前源码清单没有单独的测试项目；改动通常至少应通过 `dotnet build .\Stellar.csproj`。涉及持久化或状态机的改动，建议增加可重复的验证方案。
- SQLite 上下文使用 `Database.EnsureCreated()`，代码中未看到数据库迁移流程。表结构演进时需考虑已有 `data.db`，不要假设更新模型即可升级用户数据库。
- `Utility\Updater.cs`、`IdentityChecker.cs`、`KeyListener.cs` 和数据提供器涉及网络、Windows 原生 API、全局输入或本机进程等系统能力。修改和运行这些组件应限于获得授权的环境，并明确告知用户影响。
- 本文档反映此仓库当前可见的实现，不是用户手册，也不替代游戏、平台、Bungie API 或第三方服务的正式使用条款。

## 15. 补充说明

这份说明描述的是当前仓库可见的项目结构，不代表所有外部运行条件都包含在仓库内。第三方 NuGet 包、Windows 组件、游戏客户端、用户配置和本地数据库均可能影响运行结果。有关游戏规则、第三方服务权限和数据处理合规性，应以相应服务的正式规定为准。
