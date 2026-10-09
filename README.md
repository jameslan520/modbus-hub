# SCADA - Modbus RTU 多通道采集系统

基于 **WPF (.NET 9)** 的 SCADA 上位机软件，支持多串口通道并行通信，遵循 **Modbus RTU** 协议。

> 当前版本：**v1.0.1** · 应用名（exe 名）：**Modbus RTU 多通道采集系统**

## 功能特性

- **多通道通信**：每个通道独立对应一个串口（COM），可并行采集
- **完整串口参数**：除 COM 口、波特率外，还可设置
  - 数据位：`7` / `8`
  - 校验位：无校验(None) / 奇校验(Odd) / 偶校验(Even)
  - 停止位：`1` / `2`

  > 不提供 `1.5` 停止位：Windows 上它仅在「数据位=5」时可用，
  > 而本程序只提供 7/8 数据位，暴露出来必然导致串口打开失败。
- **改参数即时生效**：通道运行中修改串口参数会自动重连；
  仅修改名称 / 从机ID / 轮询间隔则不会打断采集
- **Modbus RTU 功能码**
  - `03` 读保持寄存器（所有点采集均使用 03）
  - `06` 写单个寄存器
  - `16` 写多个寄存器
- **数据类型**：`Float CDAB`（32 位浮点，字序 CDAB）、`UInt16`
- **实时数据采集**：可配置轮询间隔，显示数据质量与时间戳
- **读写控制**：功能码 06/16 的点可写入
- **连接状态**：串口打开成功后界面持续显示「已连接」，直到手动停止
- **配置持久化**：与程序同目录的 `channels.json`（`bin/Debug`、`bin/Release` 或 `publish` 下）
- **通信日志**：连接、读写、异常记录，支持清空

## 系统要求

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Visual Studio 2022（推荐，需安装「.NET 桌面开发」工作负载）

> WPF 仅支持 Windows 平台。

仓库根目录的 `global.json` 锁定 **SDK 9.0.318**。若本机同时装有更高版本 SDK（如 .NET 10），
`dotnet` 默认会挑最新的那个；有此文件才会按要求用 9.0 编译。

## 快速开始

```bash
dotnet restore
dotnet build
dotnet run --project src/ScadaApp/ScadaApp.csproj
```

或在 Visual Studio 中打开 `ScadaApp.sln`，按 F5 运行。

配置文件为程序目录下的 `channels.json`：

- Visual Studio / `dotnet run`：`src/ScadaApp/bin/Debug/net9.0-windows/channels.json`（Release 同理）
- 发布后：与 `Modbus RTU 多通道采集系统.exe` 同目录（publish 文件夹）

首次启动会自动生成；若以前用过 `%AppData%\ScadaApp\channels.json`，会复制到上述目录。

> 应用名由 `ScadaApp.csproj` 的 `AssemblyName` / `AssemblyTitle` / `Product` 共同决定，
> 三者需一起改——只改 `AssemblyName` 的话，exe 文件名变了，
> 但文件属性里的「产品名称」和任务管理器里的名字仍是旧的。

## 使用说明

1. **添加通道**：点击左侧 `+` 添加串口通道，配置 COM 口、波特率、**数据位 / 停止位 / 校验位**、从机ID、轮询间隔
2. **配置标签**：选中通道后点击「添加标签」，功能码选 03/06/16，Float 使用 CDAB 字序
3. **启动采集**：点击「启动通道」或「启动全部」；连接成功后通道徽章、日志栏和状态栏保持「已连接」
4. **写入数据**：功能码 06/16 的点可点击「写入」（Float 按 CDAB 写两个寄存器）
5. **清空日志**：通信日志右上角「清空日志」（不会把连接状态改回离线）

## channels.json 串口参数字段

通道级串口参数（下列为默认值）：

```json
{
  "portName": "COM1",
  "baudRate": 9600,
  "dataBits": 8,
  "parity": 0,
  "stopBits": 1
}
```

`parity` 与 `stopBits` 存的是**数字**（.NET 枚举的默认序列化行为）：

| 字段 | 取值 |
|------|------|
| `dataBits` | `7` / `8` |
| `parity` | `0`=无校验(None)、`1`=奇校验(Odd)、`2`=偶校验(Even) |
| `stopBits` | `1`=1 位、`2`=2 位 |

界面上以中文显示，无需记忆数字；手工编辑配置文件时按上表填。

## 发布

仓库提供两个 publish profile，均在 `src/ScadaApp/Properties/PublishProfiles/`：

| Profile | 命令 | 产物 | 适用 |
|---------|------|------|------|
| **SlimProfile** | `dotnet publish src/ScadaApp/ScadaApp.csproj -p:PublishProfile=SlimProfile` | `publish/slim-win-x64/` 下 **单个 exe，约 1 MB** | framework-dependent 单文件，**不含运行时**，目标机须装 [.NET 9 桌面运行时](https://dotnet.microsoft.com/download/dotnet/9.0)（要点 **Desktop Runtime**，不是 SDK / ASP.NET Core） |
| **FolderProfile** | `dotnet publish src/ScadaApp/ScadaApp.csproj -p:PublishProfile=FolderProfile` | `publish/win-x64/` 下 exe + 5 个 WPF 原生 dll，约 **129 MB** | self-contained 单文件，自带运行时，目标机无需装 .NET |

> **分发自包含版时注意**：有 5 个 WPF 原生 dll 打不进单文件
> （`D3DCompiler_47_cor3.dll`、`PenImc_cor3.dll`、`PresentationNative_cor3.dll`、
> `vcruntime140_cor3.dll`、`wpfgfx_cor3.dll`），**必须与 exe 放在同一目录**，
> 不能只拷 exe 出来。
>
> **瘦身版无此限制**——这些库由共享框架目录提供，exe 可以单独拷到任意位置运行。

未签名的自包含单文件程序可能被 Windows Defender 拦截，选择「更多信息 → 仍要运行」即可。

## Float CDAB

32 位浮点占用 2 个保持寄存器：第一个寄存器为 **CD**（低字），第二个为 **AB**（高字）。

## 项目结构

```
ScadaApp.sln
global.json                          # 锁定 .NET SDK 9.0.318
src/ScadaApp/
├── Models/
├── Services/
├── ViewModels/
├── Views/
├── Converters/
├── Themes/
└── Properties/PublishProfiles/
    ├── FolderProfile.pubxml         # 自包含版（含运行时，129 MB）
    └── SlimProfile.pubxml           # 瘦身版（不含运行时，1 MB）
```

## 依赖库

| 包名 | 用途 |
|------|------|
| NModbus / NModbus.Serial | Modbus RTU |
| CommunityToolkit.Mvvm | MVVM |
| System.IO.Ports | 串口 |

## 许可证

MIT
