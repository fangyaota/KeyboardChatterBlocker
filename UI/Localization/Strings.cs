namespace KeyboardChatterBlocker.UI.Localization
{
    /// <summary>
    /// 全部界面文案。集中在此便于校对与后续增补语言。
    /// </summary>
    internal static class Strings
    {
        // —— 应用身份 ——
        public const string AppName = "键盘防抖";
        public const string AppNameFull = "键盘防抖 Keyboard Chatter Blocker";
        public const string AppSubtitle = "Keyboard Chatter Blocker";

        // —— 常驻状态区 ——
        public const string Enable = "启用";
        public const string Enabled = "已启用";
        public const string Disabled = "已停用";
        public const string AutoDisabled = "已自动停用";
        public const string GlobalThreshold = "全局抖动阈值";
        public const string Milliseconds = "毫秒";
        public const string StartWithWindows = "开机自启";
        public const string StartInTray = "启动时隐藏到托盘";
        /// <summary>启动文件夹里的 .lnk 存在、但指向的不是本程序时，悬停提示。{0} = 实际指向的路径。</summary>
        public const string StartupForeignTip = "启动项里的快捷方式指向的不是本程序：\n{0}\n\n勾选会把快捷方式改为指向当前这一份。";
        public const string CloseToTray = "点关闭时隐藏到托盘";
        public const string AutoDisablePrefix = "（已自动禁用：";
        public const string AutoDisableSuffix = "）";
        public const string ReasonFullscreen = "全屏应用";
        public const string ReasonProcessFormat = "已打开的进程 {0}";

        // —— 导航 ——
        public const string NavStatus = "概览";
        public const string NavLog = "抖动日志";
        public const string NavStats = "统计";
        public const string NavKeys = "按键配置";
        public const string NavAutoDisable = "自动禁用程序";
        public const string NavKeyboardDevices = "键盘设备";
        public const string NavKeyboardTest = "键盘测试";
        public const string NavSettings = "其他设置";
        public const string NavAbout = "关于";

        // —— 抖动日志 ——
        public const string LogTitle = "抖动日志";
        public const string LogHint = "这里实时记录被拦截的按键。双击「配置」列可直接为该键设置阈值。";
        public const string ColTime = "时间";
        public const string ColKey = "按键";
        public const string ColChatterDelay = "抖动间隔 (ms)";
        public const string ColConfigure = "配置";
        public const string CellEdit = "编辑";
        public const string LogEmpty = "还没有拦到任何抖动。";

        // —— 统计 ——
        public const string StatsTitle = "统计";
        public const string StatsHint = "仅统计已纳入监控的按键。抖动率 = 抖动次数 ÷ 按下次数。";
        public const string ColCount = "按下次数";
        public const string ColChatter = "抖动次数";
        public const string ColRate = "抖动率";
        public const string StatsEmpty = "暂无统计数据。";

        // —— 按键配置 ——
        public const string KeysTitle = "按键配置";
        public const string KeysHint = "为个别问题按键单独设置更高的阈值。未列出的按键使用全局阈值。";
        public const string ColThreshold = "抖动阈值";
        public const string ColRemove = "移除";
        public const string AddKey = "添加按键";
        public const string RemoveKey = "移除";
        public const string AddKeyHint = "点击「添加按键」，然后按下你要配置的那个键。";

        // —— 自动禁用程序 ——
        public const string AutoDisableTitle = "自动禁用程序";
        public const string AutoDisableHint = "列出的程序一旦运行，屏蔽会自动暂停（适合游戏等对输入延迟敏感的场合）。";
        public const string AutoDisableFullscreen = "检测到全屏时自动禁用";
        public const string AutoDisableForegroundOnly = "仅在该程序位于前台时禁用";
        public const string AutoDisableForegroundOnlyHint = "关闭则只要程序在运行就暂停屏蔽（上游原有行为）。";
        public const string AddToList = "添加到列表";
        public const string RemoveFromList = "从列表移除";
        public const string ProgramPlaceholder = "程序名（如 chrome.exe）";
        public const string ShowProgramList = "选择进程…";
        public const string AutoDisableEmpty = "列表为空。";

        // —— 其他设置 ——
        public const string OtherTitle = "其他设置";
        public const string OtherHint = "这些选项会影响检测的判定方式，通常保持默认即可。";
        public const string MeasureFrom = "计时起点";
        public const string MeasurePress = "按下时";
        public const string MeasureRelease = "松开时";
        public const string MeasureHint = "从按键按下还是松开开始计算间隔。";
        public const string ExcludeInjected = "排除注入事件";
        public const string ExcludeInjectedHint = "忽略由软件模拟的按键（如宏、输入法），只处理真实物理按键。";
        public const string SaveStats = "保存持久统计数据";
        public const string SaveStatsHint = "把统计写入 blocker_stats.csv，重开程序后继续累计。";
        public const string OtherKeysReset = "其他按键按下时重置计时";
        public const string OtherKeysResetHint = "快速连续输入不同按键时，把它们视为正常输入而非抖动。";

        // —— 长按救援（本版新增功能）——
        public const string HoldRescue = "长按救援阈值";
        public const string HoldRescueUnit = "毫秒";
        public const string HoldRescueHint = "0 = 关闭。被拦截的按下若持续超过该时长，会补发一次按键救回来。";

        // —— 键盘设备（本版新增功能）——
        public const string DevicesTitle = "键盘设备";
        public const string DevicesHint = "只有勾选的键盘会参与抖动拦截。全部勾选 = 所有键盘（默认）。";
        // 注意：ModernLabel 不处理换行，多行文案会被渲染成一整行 —— 这里的文案都写成单行
        public const string DevicesEmpty = "还没有识别到任何键盘 —— 按下任意键，用到的那个键盘就会出现在这里。";
        public const string DevicesIdentify = "识别";
        public const string DevicesIdentifyArmed = "请按一下要识别的那把键盘…";
        public const string DevicesIdentifiedFormat = "刚按的是「{0}」";
        public const string DevicesNote = "设备名是系统给的机器码；切换键盘后的第一次按键可能被判到上一把键盘上，之后立刻纠正。";

        // —— 键盘测试 ——
        public const string TestTitle = "键盘测试";
        public const string TestHint = "按下任意键即可开始测试。此页面不受「启用」开关影响；间隔统计基于同一按键的连续两次按下。";
        public const string TestLastKeyCaption = "最近按下";
        public const string TestVerdictCaption = "判定";
        public const string TestNoKey = "—";
        public const string TestSinceLastCaption = "距上次按键";
        public const string TestSameKeyCaption = "同键间隔";
        public const string TestSameKey = "同键间隔";
        public const string TestDownKeys = "当前按住";
        public const string TestVerdictAllow = "放行";
        public const string TestVerdictBlocked = "被拦下";
        public const string TestAwaiting = "等待按键…";
        public const string TestLegend = "蓝色 = 已按下并放行　红色 = 已按下但被屏蔽吞掉　红框 = 曾被拦下过";
        public const string TestClearMarksFormat = "清除标记 ({0})";
        public const string TestReset = "重置";
        public const string TestStatStatusGood = "✅ 良好 — 未检测到按键抖动";
        public const string TestStatStatusBad = "⚠ 检测到 {0} 次抖动，相关按键已在键盘图上标红";
        public const string TestStatTotalPresses = "总按键数";
        public const string TestStatChatterEvents = "抖动事件";
        public const string TestStatAvgInterval = "平均间隔";
        public const string TestStatMinInterval = "最小间隔";
        public const string TestStatThreshold = "全局抖动阈值";
        public const string TestStatNoData = "—";
        public const string TestHintAutoDisabled = "（程序当前被自动禁用，键盘钩子已卸载，此页暂时收不到按键）";

        // —— 关于 ——
        public const string AboutTitle = "关于";
        public const string AboutAuthor = "由 Alex \"mcmonkey\" Goodwin 与 Frenetic LLC 开发";
        public const string AboutCopyright = "版权所有 (C) 2019-2026，保留所有权利。";
        public const string AboutLicense = "依据 MIT 许可证条款向公众发布。";
        public const string AboutLinkText = "许可证、源代码及更多信息请见 GitHub";
        public const string AboutUrl = "https://github.com/FreneticLLC/KeyboardChatterBlocker";
        public const string AboutVersionFormat = "版本：{0}";
        public const string AboutPortNote = "本版本为现代化中文界面重写，核心拦截逻辑与原版完全一致。";

        // —— 托盘 ——
        public const string TrayShow = "显示主界面";
        public const string TrayExit = "强制退出";

        // —— 对话框 ——
        public const string DuplicateTitle = "检测到重复进程";
        public const string DuplicateBodyFormat = "进程“{0}”（PID={1}）已在运行。是否关闭它？\n\n是 = 关闭另一个进程\n否 = 允许重复运行\n取消 = 关闭本窗口";
        public const string DuplicateFailedTitle = "关闭失败";
        public const string DuplicateFailedBody = "无法关闭其他进程。仍要继续运行吗？\n\n是 = 继续运行\n否 = 退出";
        public const string CannotCloseCaption = "无法关闭他进程";
        public const string CannotCloseBody = "无法关闭其他进程。仍要继续运行吗？\n\n是 = 继续运行\n否 = 退出";
        public const string Cancel = "取消";
        public const string Done = "完成";
        public const string Ok = "确定";

        // —— 按键配置对话框 ——
        public const string KeyConfigTitle = "键盘防抖 - 按键配置";
        public const string KeyConfigConfigureFormat = "配置按键：{0}";
        public const string KeyConfigGlobalFormat = "全局默认：{0}";
        public const string KeyConfigWasFormat = "原值：{0}";
        public const string KeyConfigPrompt = "新的抖动阈值（毫秒）：";
        public const string KeyConfigQuick = "常用值";
        public const string KeyConfigUseGlobal = "使用全局值";

        // —— 按键捕获对话框 ——
        public const string NeedInputTitle = "键盘防抖：请按键";
        public const string NeedInputPrompt = "请按下任意按键，所按按键将被加入列表（若尚未存在）。";
        public const string NeedInputAlternate = "也可从下方下拉列表选择特殊按键：";
        public const string NeedInputAlreadyListed = "该按键已在列表中。";
        public const string NeedInputPlaceholder = "选择特殊按键…";

        // —— 错误提示 ——
        public const string ErrorTitle = "键盘防抖";
        public const string ErrorGridMisconfigured = "错误：表格数据异常，按键名无效。";
        public const string ErrorShortcutTitle = "无法创建开机自启快捷方式";
        public const string ErrorShortcutBody = "创建快捷方式失败，可能被系统策略或安全软件阻止。\n\n详细信息：\n{0}";
    }
}
