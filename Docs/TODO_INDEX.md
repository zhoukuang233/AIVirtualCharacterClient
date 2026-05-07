# TODO 索引

## App

- `AppBootstrapper`
  - 初始化 SettingsSystem。
  - 初始化 CharacterSystem，扫描角色包并加载默认角色。
  - 初始化 PresentationSystem，为当前角色创建 BehaviorMappingResolver。
  - 初始化 DeveloperConsole。

## Infrastructure

- `RuntimePathInitializer`
  - Android / WebGL 等平台需要改造 StreamingAssets 复制逻辑。

## Character

- `CharacterDefinition`
  - CharacterModelConfig 后续可以扩展模型缩放、初始位置、默认 idle motion、默认显示参数。
  - CharacterPersonaConfig 后续需要明确 Prompt 模板由 Unity 端还是后端管理。
  - CharacterVoiceConfig 后续可以拆成强类型 VoiceConfig DTO。
- `CharacterPackageData`
  - VoiceConfigJson 后续应改成强类型配置对象。
- `CharacterPackageScanner`
  - 后续可以增加角色包排序规则。
- `CharacterPackageLoader`
  - 后续可以增加缓存机制。
  - 后续可以增加异步加载版本。
- `CharacterPackageValidator`
  - 后续可以增加 schemaVersion。
  - 后续可以增加 warning/error 错误码。

## Presentation

- `PresentationCommand`
  - 后续可以增加 ExecuteAsync、Cancel、Priority、Duration、CanInterrupt。
- `ExpressionCommand`
  - 后续可以增加 FadeIn、FadeOut、Duration。
  - 后续需要约定空 ExpressionFileName 表示清空表情 / 恢复默认脸。
- `MotionCommand`
  - 后续可以增加 MotionGroup、FadeIn、FadeOut、InterruptPolicy。
- `VoiceCommand`
  - 后续可以增加音频文件路径、音频时长、TTS 延迟、speakerId、音量、语速和口型同步参数。
- `PresentationResolveResult`
  - 后续 ExperimentLogging 可以直接记录该对象中的字段。
- `PresentationCommandQueue`
  - 后续可以引入优先级、动作打断和异步执行。
- `BehaviorMappingResolver`
  - 后续可以复用 Character 模块中的 DTO。
  - 后续可以增加映射表热重载。
  - 后续可以把 Debug.Log 改成结构化日志事件。

## Editor

- `AutoNamespaceOnCreate`
  - 后续可以把 ProjectRoot、NamespacePrefix、ScriptsFolderName 抽成 Editor 配置。
