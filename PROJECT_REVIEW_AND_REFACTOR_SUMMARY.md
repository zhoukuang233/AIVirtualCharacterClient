# AI 虚拟角色 Unity 前端项目 Review 与重构总结

> 本文档基于当前 Unity 工程源码与《AI 虚拟角色系统 Unity 前端架构讨论整理.md》整理。  
> 本次重构目标是：在不改变既有功能逻辑的前提下，清理重复职责、降低模块耦合、补全必要注释与默认配置，使项目更接近论文所需的“数据驱动式 AI 虚拟角色前端框架”。

---

## 1. 当前项目已有功能逻辑 Review

### 1.1 项目总体定位

当前工程已经不是单纯的 Live2D 展示 Demo，而是具备了以下 MVP 框架能力：

- 使用 `StreamingAssets/DefaultUserData` 提供默认运行数据；
- 启动时复制默认数据到 `Application.persistentDataPath/UserData`；
- 使用角色包结构管理角色配置、persona、Prompt 模板、表情映射、动作映射和 Live2D 资源；
- 使用 `emotion/action` 语义标签驱动前端表现，而不是让 LLM 直接控制具体 Live2D 文件；
- 使用 Mock 后端模拟一次完整对话返回；
- 将对话返回解析为文本、情绪、动作、语音风格；
- 使用表现映射系统把情绪/动作标签转换为具体表情/动作资源；
- 使用表现命令队列统一执行文本、表情、动作、语音命令；
- 使用 JSONL 方式记录实验交互日志。

当前项目已经具备论文第四章“系统实现”中可描述的前端框架雏形。

---

## 2. 各模块职责与已有逻辑

### 2.1 App 模块

核心类：

- `AppBootstrapper`

当前职责：

1. Unity 场景启动后执行全局初始化；
2. 初始化运行时路径；
3. 复制默认 UserData；
4. 调用角色系统扫描并加载第一个有效角色；
5. 初始化表现系统；
6. 为后续聊天和表现流程准备运行环境。

该模块目前承担的是 MVP 阶段的启动入口职责。

---

### 2.2 Infrastructure 模块

核心类：

- `RuntimePathInitializer`
- `RuntimePathInitializeResult`
- `SingletonMonoBehaviour<T>`

当前职责：

1. 统一维护运行时数据目录；
2. 将 `Assets/StreamingAssets/DefaultUserData` 中的默认数据复制到 persistentDataPath；
3. 提供单例 MonoBehaviour 基类；
4. 为 Character、Logging、Settings 等模块提供路径基础。

本次重构后，复制默认数据时会跳过 `.meta` 与 `.gitkeep` 等 Unity 编辑器辅助文件，避免把这些无运行意义的文件复制进用户数据目录。

---

### 2.3 Character 模块

核心类：

- `CharacterSystem`
- `CharacterPackageScanner`
- `CharacterPackageValidator`
- `CharacterPackageLoader`
- `CharacterPackagePathResolver`
- `CurrentCharacterContext`
- `CharacterDefinition`
- `CharacterPackageData`
- `CharacterValidationResult`

当前职责：

1. 扫描 `UserData/Characters` 下的角色包；
2. 校验角色包必要文件是否存在；
3. 读取 `character.json`；
4. 读取 `persona.txt`；
5. 读取 `prompt_template.txt`；
6. 读取 `expression_mapping.json`；
7. 读取 `motion_mapping.json`；
8. 扫描 Live2D 表情文件与动作文件；
9. 维护当前角色上下文；
10. 对外提供当前角色数据。

该模块是当前项目中最接近最终设计目标的模块之一。

---

### 2.4 Chat 模块

核心类：

- `ChatController`
- `ChatRequestBuilder`
- `ChatResponseParser`
- `ChatRequestDto`
- `ChatResponseDto`
- `ChatResponseData`

当前职责：

1. 接收用户输入；
2. 基于当前角色 persona、Prompt 模板和用户输入构造聊天请求；
3. 调用 Mock 后端获得结构化响应；
4. 解析响应中的：
   - `replyText`
   - `emotion`
   - `action`
   - `voiceStyle`
5. 交给表现系统解析表情/动作；
6. 生成表现命令并加入命令队列；
7. 记录一次交互日志。

当前 Chat 模块已经能走通“不依赖真实 LLM 的前端交互 MVP”。

---

### 2.5 Service 模块

核心类：

- `MockChatBackendClient`

当前职责：

1. 模拟后端返回结构化响应；
2. 根据用户输入中的关键词返回不同 emotion/action 标签；
3. 用于在真实后端接入前测试 Unity 前端链路。

当前 Service 模块仍处于 Mock 阶段，还没有实现真实 HTTP 后端请求。

---

### 2.6 Presentation 模块

核心类：

- `PresentationSystem`
- `BehaviorMappingResolver`
- `PresentationResolveResult`
- `PresentationCommandQueue`
- `PresentationCommandQueueService`
- `IPresentationCommand`
- `ShowTextCommand`
- `ExpressionCommand`
- `MotionCommand`
- `VoiceCommand`

当前职责：

1. 根据当前角色的 `expression_mapping.json` 和 `motion_mapping.json` 初始化表现映射；
2. 将 LLM/Mock 返回的 emotion/action 标签解析为具体资源路径；
3. 在标签不存在时执行 fallback；
4. 生成文本、表情、动作、语音命令；
5. 通过命令队列统一执行表现命令。

本次重构后，Presentation 模块不再重复定义 Character 模块中已有的映射 DTO，改为复用统一数据结构。

---

### 2.7 ExperimentLogging 模块

核心类：

- `ExperimentLogger`
- `FrontendInteractionLogEntry`

当前职责：

1. 记录用户输入；
2. 记录角色回复；
3. 记录 emotion/action 标签；
4. 记录映射后的表情/动作资源；
5. 记录 fallback 是否发生；
6. 记录错误信息；
7. 以 JSONL 形式保存前端实验日志。

该模块已经能支撑后续论文实验数据采集的基础需求。

---

### 2.8 DeveloperConsole 与 Settings 模块

当前状态：

- 目录已存在；
- 当前主要是占位结构；
- 尚未形成完整运行时设置系统和开发者控制台 UI。

后续应优先补齐这两个模块，因为它们直接影响实验可操作性和系统可展示性。

---

## 3. 本次重构做了什么

### 3.1 新增统一路径解析器

新增文件：

- `CharacterPackagePathResolver.cs`

作用：

1. 统一处理角色包内资源路径；
2. 支持普通文件名、角色包相对路径和绝对路径；
3. 支持在 `live2d` 目录下递归查找资源；
4. 避免 Character 验证器、CharacterPackageInfo、Presentation 映射解析器各自维护一套路径解析逻辑。

收益：

- 降低重复代码；
- 降低后续路径规则变更成本；
- 让角色包资源路径策略更集中、更清楚。

---

### 3.2 收敛 CharacterSystem 与 CurrentCharacterContext 的职责

重构前：

- `CurrentCharacterContext` 除了保存当前角色，还重复承担了一部分扫描、校验、加载角色的工作；
- `CharacterSystem` 与 `CurrentCharacterContext` 之间职责边界不够清晰。

重构后：

- `CharacterSystem` 负责扫描、校验、加载、切换角色；
- `CurrentCharacterContext` 只负责保存当前角色状态和触发当前角色变更事件。

收益：

- 角色系统职责更符合“System 统一调度，Context 保存状态”的结构；
- 后续实现角色切换 UI、开发者控制台、实验回放时不容易出现重复入口。

---

### 3.3 Presentation 复用 Character 映射 DTO

重构前：

- `BehaviorMappingResolver` 内部重复定义了 Expression/Motion 映射配置类；
- Character 模块中已经存在同类 DTO；
- 两套结构长期维护容易出现字段不一致。

重构后：

- `BehaviorMappingResolver` 直接复用：
  - `ExpressionMappingConfig`
  - `ExpressionMappingEntry`
  - `MotionMappingConfig`
  - `MotionMappingEntry`

收益：

- 减少重复模型；
- 符合“角色包数据由 Character 模块统一定义”的边界；
- Presentation 专注解析和表现命令生成，不再定义角色包数据结构。

---

### 3.4 ChatController 不再绕过 PresentationSystem

重构前：

- `ChatController` 内部直接创建 `BehaviorMappingResolver`；
- 这会绕过 `PresentationSystem` 的统一入口；
- 不利于后续替换真实表现层、命令队列、Live2D 播放器。

重构后：

- `ChatController` 通过 `PresentationSystem.Instance.Resolve(...)` 解析表现；
- 如测试场景没有走正常启动流程，会自动尝试初始化 PresentationSystem；
- 文本回复也会以 `ShowTextCommand` 形式进入表现命令队列。

收益：

- Chat 模块只负责交互流程，不直接关心映射细节；
- 表现系统入口统一；
- 更接近“LLM Response -> PresentationCommandQueue”的设计。

---

### 3.5 简化表现命令队列

重构前：

- `PresentationCommandQueue` 在调试执行时手动判断每一种命令类型；
- 命令队列对具体命令类有不必要的依赖。

重构后：

- 统一调用 `command.ExecuteDebug()`；
- 每个命令自己负责自己的调试执行逻辑。

收益：

- 命令队列只关心队列调度；
- 后续新增命令类型时无需修改队列内部逻辑；
- 更符合命令模式的基本设计。

---

### 3.6 补全默认配置文件

补全文件：

- `Assets/StreamingAssets/DefaultUserData/Settings/app_settings.json`
- `Assets/StreamingAssets/DefaultUserData/Services/service_config.json`

补全内容：

- 默认角色 ID；
- 默认服务配置名；
- 日志开关；
- 开发者模式开关；
- 默认语言；
- 实验模式配置；
- Mock LLM/TTS 配置；
- 后端地址占位配置。

收益：

- 避免默认配置为空文件；
- 后续实现 SettingsSystem 和 ServiceSystem 时有稳定数据基础；
- 符合“Unity 保留当前后端地址和服务配置名，不保存 API Key”的设计边界。

---

### 3.7 RuntimePathInitializer 跳过无运行意义文件

重构前：

- 默认数据复制到 persistentDataPath 时会连同 `.meta`、`.gitkeep` 一起复制。

重构后：

- 跳过 `.meta` 与 `.gitkeep` 文件。

收益：

- 用户运行数据目录更干净；
- 避免后续角色包扫描、日志导出或用户编辑时混入 Unity 编辑器辅助文件。

---

## 4. 本次未做的事情

为了避免改变既有功能逻辑，本次没有做以下操作：

1. 没有删除第三方 Live2D Cubism SDK；
2. 没有调整 Unity 场景引用；
3. 没有强行迁移资源目录；
4. 没有接入真实 LLM/TTS 后端；
5. 没有实现真实 Live2D 表情/动作播放 API；
6. 没有实现完整 SettingsSystem UI；
7. 没有实现 DeveloperConsole UI；
8. 没有改动角色包核心数据结构的字段语义。

---

## 5. 当前项目阶段总结

当前项目可以定义为：

> 数据驱动式 AI 虚拟角色 Unity 前端 MVP 骨架阶段。

已经完成：

- 角色包目录结构；
- 默认 UserData 初始化；
- 角色包扫描；
- 角色包校验；
- 角色配置读取；
- persona 与 Prompt 模板读取；
- 表情/动作映射表读取；
- emotion/action 到具体资源的映射；
- fallback 机制；
- Mock 对话后端；
- 聊天请求构造；
- 结构化响应解析；
- 表现命令队列；
- 前端 JSONL 实验日志。

尚未完成：

- SettingsSystem 的运行时读写；
- ServiceSystem 的真实 HTTP 请求；
- Live2D 表情/动作播放器的实际接入；
- TTS 与口型同步；
- DeveloperConsole 可视化调试面板；
- 实验 CSV 导出；
- Replay 回放；
- Unity Play Mode / Edit Mode 自动化测试。

---

## 6. 后续阶段建议

### 阶段一：Unity 工程验证与场景接线

目标：确认当前 MVP 链路在 Unity Editor 中无编译错误，并能在场景中跑通。

建议任务：

1. 打开 Unity 工程；
2. 检查 Console 编译错误；
3. 确认 `AppBootstrapper` 所在场景对象存在；
4. 确认 `ChatController` UI 引用完整；
5. 点击发送按钮，验证 Mock 聊天流程；
6. 检查 persistentDataPath 下是否生成 UserData 与 Logs。

---

### 阶段二：补齐 SettingsSystem

目标：让配置不再散落在代码中。

建议任务：

1. 定义 `AppSettings` DTO；
2. 实现 `SettingsLoader`；
3. 实现 `SettingsSystem`；
4. 支持读取当前角色 ID；
5. 支持读取当前服务配置名；
6. 支持日志开关、开发者模式、实验模式；
7. 为后续 UI 配置面板预留保存接口。

---

### 阶段三：实现真实 ServiceSystem

目标：把 Mock 后端替换为真实后端 API。

建议任务：

1. 定义 `IChatBackendClient` 接口；
2. 让 `MockChatBackendClient` 实现该接口；
3. 新增 `HttpChatBackendClient`；
4. 使用 `UnityWebRequest` 请求 Python/Spring Boot 后端；
5. 处理超时、网络错误、JSON 错误；
6. 保持 ChatController 不感知具体后端类型。

---

### 阶段四：接入 Live2D 表现播放器

目标：让 PresentationCommand 不再只 Debug.Log，而是真正驱动 Live2D。

建议任务：

1. 新增 `Live2DModelController`；
2. 新增 `Live2DExpressionPlayer`；
3. 新增 `Live2DMotionPlayer`；
4. 将 `ExpressionCommand` 和 `MotionCommand` 对接播放器；
5. 实现 idle motion；
6. 处理动作优先级、打断、回到 idle。

---

### 阶段五：开发 DeveloperConsole/TestLab

目标：支持论文开发阶段的快速测试与演示。

建议任务：

1. 创建 `TestLab.unity`；
2. 实现角色包扫描结果面板；
3. 实现角色包校验结果面板；
4. 实现 emotion/action 手动测试按钮；
5. 实现 Mock/HTTP 服务切换；
6. 实现日志查看与导出按钮。

---

### 阶段六：增强实验日志与论文实验支持

目标：从“能记录”升级为“可统计、可复现实验”。

建议任务：

1. 增加 CSV 导出；
2. 增加 sessionId/turnId 管理；
3. 增加响应耗时统计；
4. 增加 fallback 统计；
5. 增加错误码体系；
6. 实现 Replay 回放；
7. 为第五章实验准备统计指标。

---

## 7. 建议写进论文第四章的当前成果

当前项目已经可以支撑以下实现描述：

1. 基于角色包的虚拟角色资源组织方式；
2. StreamingAssets 到 persistentDataPath 的运行时数据初始化机制；
3. 角色配置、persona、Prompt 模板与表现映射表的统一加载机制；
4. emotion/action 语义标签与 Live2D 表现资源之间的解耦映射机制；
5. 表现命令队列机制；
6. Mock 后端驱动的前端闭环测试流程；
7. 面向实验分析的前端交互日志记录机制。

---

## 8. 验证说明

本次在当前环境中完成了：

- C# 文件括号结构静态检查；
- 关键 JSON 配置文件格式检查；
- 自研代码引用关系检查；
- 低风险重构与注释补全。

由于当前环境无法启动 Unity Editor，因此尚未执行：

- Unity 编译验证；
- Unity Play Mode 测试；
- 场景对象引用完整性验证；
- Live2D SDK 实际运行验证。

建议你解压优化后的工程后，第一步先在 Unity 中打开项目，观察 Console 是否有编译错误。若有错误，优先检查场景中旧脚本引用和 Inspector 绑定项。
