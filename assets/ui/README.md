# UI artwork

本目录保存可替换的战斗 UI 图标：

- `events/`：A 类通用战斗事件图标
- `statuses/`：B 类 Buff／Debuff 图标
- `skills/`：C 类角色专属技能图标

运行时映射统一位于 `wwwroot/config/ui-assets.json`。游戏规则和 UI 渲染代码只引用稳定的资产 ID，不直接写图片文件名。

替换已有 PNG 后，请把清单顶层的 `version` 加 1。资源组件会把版本写入图片 URL（例如 `?v=2`），从而让所有浏览器立即请求新文件，而不是继续显示旧缓存。

当前8张A类、9张B类和8张C类图片均已接入。A类不只用于日志和预测，也由同步战斗事件播放器显示在对应角色／队伍上；C类会作为技能与状态演出的次级徽记出现。现行规则新增／仍缺少以下B类文件：

```text
statuses/status_shield_complacency.png
statuses/status_beast_rage.png
```

缺少该文件时，`status.shield-complacency` 会安全显示 fallback，不会出现破图。补入文件后，只需把清单中该项的 `source` 改成上述相对路径。

完整维护说明见 `reference/TinyPixelFights_UI_Asset_Embedding.md`。
