# AIVirtualCharacterClient 注释化代码包

这份代码包用于给当前 Unity 前端项目的核心 C# 脚本补充更完整的中文注释。

## 覆盖范围

已补充注释的模块：

- `Assets/_Project/App/Scripts`
- `Assets/_Project/Infrastructure/Scripts`
- `Assets/_Project/Character/Scripts`
- `Assets/_Project/Presentation/Scripts`
- `Assets/_Project/Editor/Scripts`

未改动或未生成代码的模块：

- `Chat`
- `Service`
- `Settings`
- `ExperimentLogging`
- `DeveloperConsole`

这些目录目前在仓库中主要是占位状态，建议等你开始实现对应功能时再按同样规范补注释。

## 使用方式

1. 在 Git 中先提交或备份你当前项目。
2. 把本压缩包解压到 Unity 项目根目录。
3. 允许覆盖同名 `.cs` 文件。
4. 用 Git diff 检查改动。
5. 打开 Unity，等待重新编译。
6. 如果 Unity Console 有编译错误，优先对比你本地最新代码与本包中的对应文件。

## 注释规范

每个主要类都尽量说明：

- 这个类负责什么。
- 怎么使用。
- 对外暴露了哪些属性、字段或方法。
- 当前 MVP 阶段的边界。
- 后续应该完善的 TODO。

重要方法都尽量说明：

- 方法功能。
- 参数含义。
- 返回值含义。
- 可能抛出的异常或调用注意事项。

## 额外说明

本包主要是“注释增强版”，但我顺手做了两个小的语义调整：

1. `CharacterPackageValidator` 中，表情映射项的 `expression` 为空时不再作为错误，而是作为警告，因为 neutral / 默认脸可以合理表示为“不绑定 exp3.json”。
2. `BehaviorMappingResolver.BuildMotionPath` 的标准动作目录写成 `live2d/motions`，同时仍然会递归搜索整个 `live2d` 目录，以兼容模型导出的其他目录结构。

如果你想严格保持当前行为，可以只保留注释，把这两处逻辑改回原来的判断。
