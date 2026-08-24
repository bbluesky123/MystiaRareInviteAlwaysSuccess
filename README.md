# Mystia Rare Invite Always Success

《东方夜雀食堂》BepInEx IL2CPP Mod：让白天已经解锁的“邀请”稀客操作按满羁绊成功率执行。

> 当前版本：`1.2.1`

## 功能范围

- 只处理白天与稀客对话菜单中的“邀请”。
- 不解锁原本不可用的邀请选项；真实羁绊必须至少达到 LV2。
- 不修改游戏程序集。
- 不主动读写存档中的羁绊等级、羁绊进度、任务或奖励状态。
- 邀请成功后的对话、时间消耗和今晚来店登记仍由游戏原流程执行。

## 原版羁绊等级与邀请成功率

原版在羁绊 LV2 解锁邀请，之后每提高一级，成功率增加 25%。

| 羁绊等级 | 邀请状态 | 原版成功率 |
| --- | --- | ---: |
| LV1 | 未解锁 | — |
| LV2 | 已解锁 | 25% |
| LV3 | 已解锁 | 50% |
| LV4 | 已解锁 | 75% |
| LV5 | 已解锁 | 100% |

对于已解锁邀请的等级，可写成：

```text
邀请成功率 =（羁绊等级 - 1）× 25%
```

邀请成功后，稀客可以无视通常的出没区域，在当晚前往玩家选择的店铺。邀请失败并不等于该稀客当晚绝对不会出现：如果所选店铺本来就在她的自然出没区域，她仍可能通过原版刷新机制来店。

## 实现原理

主补丁位于 `DayScene.UI.DaySceneChatSelectionPannel.Invite` 的 Harmony Prefix。

1. 游戏已经根据真实羁绊和当前状态显示了邀请选项。
2. 玩家点击邀请，游戏调用 `Invite(characterLabel, currentKizunaLevel, callback)`。
3. Mod 记录传入的真实等级参数，并仅在这一次方法调用中把 `currentKizunaLevel` 改为 `5`。
4. 游戏继续执行原版 `Invite` 流程，以 LV5 的 100% 成功率选择邀请结果，并处理对话、时间消耗及今晚来店登记。
5. 方法调用结束后，Mod 不把临时数值写回角色羁绊状态或存档。

示意：

```text
存档/状态：角色真实羁绊 LV2
                     │
                     ▼
           点击已经解锁的“邀请”
                     │
                     ▼
Invite 调用参数：originalKizuna=2 → effectiveKizuna=5
                     │
                     ▼
        游戏原版邀请流程按 LV5 执行
                     │
                     ▼
存档/状态：预期仍为角色真实羁绊 LV2
```

项目还保留了对 `InviteSpecGuest` 的低层后备 Prefix：若某一游戏构建没有将该辅助方法内联，后备补丁会直接选择当前等级的成功对话并把返回结果设为成功。当前观察到的 IL2CPP 构建会绕过该辅助方法的 Harmony 调用点，因此 `Invite` Prefix 是主要生效路径。

## 临时使用 LV5 参数的影响与风险

这里修改的是 `Invite` 收到的局部参数，不是角色状态追踪器或存档字段，因此按当前游戏流程，不应永久改变真实羁绊，也不应直接完成羁绊任务或发放高等级奖励。

已经可以预期的可见影响：

- 实际羁绊为 LV2～LV4 时，邀请可能采用该角色的 `LV5_Invite_Succeed` 对话，而不是实际等级对应的成功对话。
- Mod 日志中的 `effectiveKizuna=5` 只表示本次邀请调用采用 LV5 参数，不表示存档已升到 LV5。

需要关注的潜在兼容性风险：

- `Invite` 内部凡是依赖 `currentKizunaLevel` 的其他分支，也会在本次调用期间把角色视为 LV5。
- 如果未来游戏版本在 `Invite` 中加入了等级奖励、任务推进或解锁逻辑，临时 LV5 参数可能意外触发这些新分支。
- 其他 Mod 如果也修改 `Invite`、`InviteSpecGuest` 或邀请对话数据，补丁顺序可能改变最终行为。
- 低层后备 Prefix 会跳过 `InviteSpecGuest` 的原实现；虽然外层原版流程仍会继续执行，但游戏更新后仍应重新验证今晚来店登记是否正常。

建议测试时在邀请前后检查图鉴中的真实羁绊等级和进度，并留意是否意外出现升级通知、任务完成、LV5 礼物、委托采集或其他高等级解锁。若只出现 LV5 邀请成功对话，而图鉴、任务和奖励状态保持不变，则符合当前设计。

## 安装

1. 为游戏安装支持 IL2CPP 的 BepInEx 6。
2. 编译项目，或取得已编译的 `MystiaRareInviteAlwaysSuccess.dll`。
3. 将 DLL 放入：

```text
<游戏目录>\BepInEx\plugins\MystiaRareInviteAlwaysSuccess\
```

4. 完全退出并重新启动游戏；插件不能在游戏运行中热更新。

## 日志验证

成功加载 `1.2.1` 时应出现：

```text
Mystia Rare Invite Always Success v1.2.1 loaded; patched DayScene.UI.DaySceneChatSelectionPannel.Invite.
```

点击邀请时应出现类似：

```text
Intercepted rare guest invite request: Akyuu, originalKizuna=2, effectiveKizuna=5.
```

如果只看到 `Loading [Mystia Rare Invite Always Success ...]`，随后出现 `Error loading`，说明插件没有激活。判断邀请是否由 Mod 介入时，应以 `Intercepted rare guest invite request` 为准，不能只根据成功对话判断，因为原版邀请本身也可能随机成功。

## 构建

项目目标框架为 `.NET 6`，引用路径由项目文件中的 `GameDir` 指向本地游戏目录：

```powershell
dotnet build .\MystiaRareInviteAlwaysSuccess.csproj -c Release
```

若只想编译而不复制到游戏插件目录：

```powershell
dotnet build .\MystiaRareInviteAlwaysSuccess.csproj -c Release -p:SkipCopyToPlugins=true
```
