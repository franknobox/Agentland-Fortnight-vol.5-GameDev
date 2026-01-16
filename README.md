# Agentland-Fortnight-vol.5-GameDev
Unity 项目协作约定（2 人 / GitHub / Unity 2022 LTS）
一、项目基础信息

引擎版本：Unity 2022 LTS

协作方式：GitHub（无 Git LFS）

项目类型：2D

分工方式：Prefab 分工 + Scene 单人编辑

二、Unity 项目统一设置（必须一致）

所有成员必须在 Unity 中完成以下设置：

路径：Edit > Project Settings > Editor

Version Control：Visible Meta Files

Asset Serialization：Force Text

⚠️ 未开启上述设置的成员不得提交代码。

三、仓库文件提交规则
允许提交

Assets/

Packages/

ProjectSettings/

禁止提交

Library/

Temp/

Obj/

Logs/

UserSettings/

仓库使用 Unity 官方 .gitignore。

四、Scene 协作规则（重点）
1. Scene 文件默认单人编辑

主场景路径：
Assets/Scenes/Main.unity

同一时间 只允许一人编辑 Scene

编辑 Scene 前需在沟通渠道中说明

编辑完成后需立即提交并 push

2. 禁止行为

两人同时修改同一 .unity Scene 文件

在未拉取最新代码的情况下修改 Scene

五、Prefab 分工原则
推荐目录结构
Assets/
 ├─ Scenes/
 ├─ Prefabs/
 ├─ Scripts/
 ├─ Art/
 └─ Audio/

分工方式

Prefab、脚本、资源文件可并行开发

主 Scene 仅用于：

摆放 Prefab

连接引用

优先将逻辑、UI、角色等拆分为 Prefab

六、Git 使用流程（极简）
开始工作前
git pull

完成工作后
git add .
git commit -m "简要说明本次修改"
git push

冲突处理原则

若发生 Scene 冲突：

不进行复杂手动合并

由一人回退并重新整理 Scene

冲突本质通常为同时编辑 Scene，应回到协作规则检查问题

七、资源使用约定（无 LFS 前提）

避免提交超大资源文件

建议资源类型：

png / jpg

wav / mp3（控制体积）

不建议提交：

psd 原文件

视频素材（如必须使用，需提前协商）

八、版本稳定性约定

main 分支应保持可正常打开和运行

不提交“无法进入 Play 模式”的破坏性修改

大改动前需提前沟通

九、共识声明

本项目协作以 减少冲突、提高稳定性 为优先目标。
当规则与便利性冲突时，优先遵守规则。
