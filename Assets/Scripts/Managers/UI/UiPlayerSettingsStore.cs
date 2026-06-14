using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VisualAlgo.Managers.UI
{
    /// <summary>
    /// 玩家 UI 配置文件中的单场景说明面板显示配置。
    /// </summary>
    [Serializable]
    public sealed class SceneInstructionVisibilityEntry
    {
        /// <summary>
        /// 场景标识。
        /// </summary>
        public string SceneKey;

        /// <summary>
        /// 是否不再显示该场景说明面板。
        /// </summary>
        public bool HideInstructionPanel;
    }

    /// <summary>
    /// 玩家 UI 配置数据。
    /// </summary>
    [Serializable]
    public sealed class UiPlayerSettingsData
    {
        /// <summary>
        /// 相机缩放灵敏度。
        /// </summary>
        public float ZoomSensitivity = 0.15f;

        /// <summary>
        /// 屏幕显示模式。
        /// </summary>
        public int ScreenMode = 0;

        /// <summary>
        /// 窗口模式下的分辨率宽度。
        /// </summary>
        public int WindowedResolutionWidth = 1280;

        /// <summary>
        /// 窗口模式下的分辨率高度。
        /// </summary>
        public int WindowedResolutionHeight = 720;

        /// <summary>
        /// 各场景说明面板显示配置。
        /// </summary>
        public List<SceneInstructionVisibilityEntry> SceneInstructionVisibilityEntries = new();
    }

    /// <summary>
    /// 负责读取和保存玩家 UI 配置文件。
    /// </summary>
    public static class UiPlayerSettingsStore
    {
        /// <summary>
        /// 玩家 UI 配置文件名。
        /// </summary>
        public const string SettingsFileName = "player-settings.json";

        /// <summary>
        /// 玩家 UI 配置文件完整路径。
        /// </summary>
        public static string SettingsFilePath => Path.Combine(Application.persistentDataPath, SettingsFileName);

        /// <summary>
        /// 读取玩家 UI 配置；若读取失败则返回默认配置。
        /// </summary>
        /// <returns>读取到的配置数据。</returns>
        public static UiPlayerSettingsData Load()
        {
            UiPlayerSettingsData settings = null;
            if (File.Exists(SettingsFilePath))
            {
                try
                {
                    settings = JsonUtility.FromJson<UiPlayerSettingsData>(File.ReadAllText(SettingsFilePath));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"读取玩家 UI 配置失败：{exception.Message}");
                }
            }

            settings ??= new UiPlayerSettingsData();
            settings.SceneInstructionVisibilityEntries ??= new List<SceneInstructionVisibilityEntry>();
            return settings;
        }

        /// <summary>
        /// 保存玩家 UI 配置。
        /// </summary>
        /// <param name="settings">待保存配置。</param>
        public static void Save(UiPlayerSettingsData settings)
        {
            if (settings == null) return;

            settings.SceneInstructionVisibilityEntries ??= new List<SceneInstructionVisibilityEntry>();

            try
            {
                File.WriteAllText(SettingsFilePath, JsonUtility.ToJson(settings, true));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"保存玩家 UI 配置失败：{exception.Message}");
            }
        }

        /// <summary>
        /// 读取指定场景是否不再显示说明面板。
        /// </summary>
        /// <param name="sceneKey">场景标识。</param>
        /// <returns>若为真则表示下次进入场景不再显示。</returns>
        public static bool GetHideInstructionPanel(string sceneKey)
        {
            if (string.IsNullOrWhiteSpace(sceneKey)) return false;
            UiPlayerSettingsData settings = Load();
            SceneInstructionVisibilityEntry entry = FindEntry(settings, sceneKey);
            return entry != null && entry.HideInstructionPanel;
        }

        /// <summary>
        /// 设置指定场景是否不再显示说明面板。
        /// </summary>
        /// <param name="sceneKey">场景标识。</param>
        /// <param name="hide">是否不再显示。</param>
        public static void SetHideInstructionPanel(string sceneKey, bool hide)
        {
            if (string.IsNullOrWhiteSpace(sceneKey)) return;
            UiPlayerSettingsData settings = Load();
            SceneInstructionVisibilityEntry entry = FindOrCreateEntry(settings, sceneKey);
            entry.HideInstructionPanel = hide;
            Save(settings);
        }

        /// <summary>
        /// 从配置中查找指定场景配置项。
        /// </summary>
        /// <param name="settings">配置数据。</param>
        /// <param name="sceneKey">场景标识。</param>
        /// <returns>找到的配置项；若不存在则返回空。</returns>
        private static SceneInstructionVisibilityEntry FindEntry(UiPlayerSettingsData settings, string sceneKey)
        {
            if (settings?.SceneInstructionVisibilityEntries == null) return null;
            for (int index = 0; index < settings.SceneInstructionVisibilityEntries.Count; index++)
            {
                SceneInstructionVisibilityEntry entry = settings.SceneInstructionVisibilityEntries[index];
                if (entry != null && string.Equals(entry.SceneKey, sceneKey, StringComparison.Ordinal))
                    return entry;
            }

            return null;
        }

        /// <summary>
        /// 查找指定场景配置项；若不存在则创建。
        /// </summary>
        /// <param name="settings">配置数据。</param>
        /// <param name="sceneKey">场景标识。</param>
        /// <returns>查找到或新建的配置项。</returns>
        private static SceneInstructionVisibilityEntry FindOrCreateEntry(UiPlayerSettingsData settings, string sceneKey)
        {
            SceneInstructionVisibilityEntry entry = FindEntry(settings, sceneKey);
            if (entry != null) return entry;

            entry = new SceneInstructionVisibilityEntry { SceneKey = sceneKey, HideInstructionPanel = false };
            settings.SceneInstructionVisibilityEntries.Add(entry);
            return entry;
        }
    }
}
