using UnityEngine;
using Unity.Mathematics;

namespace Framework
{
    /// <summary>
    /// 音频服务接口 - 管理游戏中的音效和音�?
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="soundId">音效ID</param>
        /// <param name="volume">音量 (0-1)</param>
        /// <param name="position">3D音效位置（可选）</param>
        void PlaySound(string soundId, float volume = 1f, float3? position = null);

        /// <summary>
        /// 停止音效
        /// </summary>
        void StopSound(string soundId);

        /// <summary>
        /// 播放音乐
        /// </summary>
        /// <param name="musicId">音乐ID</param>
        /// <param name="fadeIn">是否淡入</param>
        /// <param name="fadeDuration">淡入时长（秒�?/param>
        void PlayMusic(string musicId, bool fadeIn = true, float fadeDuration = 1f);

        /// <summary>
        /// 停止音乐
        /// </summary>
        void StopMusic(bool fadeOut = true, float fadeDuration = 1f);

        /// <summary>
        /// 暂停/恢复音乐
        /// </summary>
        void PauseMusic(bool pause);

        /// <summary>
        /// 设置音效音量
        /// </summary>
        void SetSoundVolume(float volume);

        /// <summary>
        /// 设置音乐音量
        /// </summary>
        void SetMusicVolume(float volume);

        /// <summary>
        /// 设置主音�?
        /// </summary>
        void SetMasterVolume(float volume);
    }

    /// <summary>
    /// 资源加载服务接口 - 管理游戏资源的加载和卸载
    /// </summary>
    public interface IResourceService
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        T Load<T>(string path) where T : Object;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        void LoadAsync<T>(string path, System.Action<T> onComplete) where T : Object;

        /// <summary>
        /// 实例化预制体
        /// </summary>
        GameObject Instantiate(string path, float3 position, quaternion rotation);

        /// <summary>
        /// 卸载资源
        /// </summary>
        void Unload(string path);

        /// <summary>
        /// 卸载未使用的资源
        /// </summary>
        void UnloadUnusedAssets();

        /// <summary>
        /// 预加载资源列�?
        /// </summary>
        void PreloadAssets(string[] paths, System.Action onComplete = null);
    }

    /// <summary>
    /// 存档服务接口 - 管理游戏数据的保存和加载
    /// </summary>
    public interface ISaveService
    {
        /// <summary>
        /// 保存数据
        /// </summary>
        void Save<T>(string key, T data);

        /// <summary>
        /// 加载数据
        /// </summary>
        T Load<T>(string key, T defaultValue = default);

        /// <summary>
        /// 删除数据
        /// </summary>
        void Delete(string key);

        /// <summary>
        /// 是否存在数据
        /// </summary>
        bool HasKey(string key);

        /// <summary>
        /// 清空所有数�?
        /// </summary>
        void ClearAll();

        /// <summary>
        /// 保存所有更改（异步�?
        /// </summary>
        void SaveAll();
    }

    /// <summary>
    /// 对象池服务接�?- 管理可复用对�?
    /// </summary>
    public interface IPoolService
    {
        /// <summary>
        /// 从池中获取对�?
        /// </summary>
        GameObject Get(string poolId);

        /// <summary>
        /// 归还对象到池
        /// </summary>
        void Return(string poolId, GameObject obj);

        /// <summary>
        /// 创建对象�?
        /// </summary>
        void CreatePool(string poolId, GameObject prefab, int initialSize = 10, int maxSize = 100);

        /// <summary>
        /// 预热对象�?
        /// </summary>
        void WarmPool(string poolId, int count);

        /// <summary>
        /// 清空对象�?
        /// </summary>
        void ClearPool(string poolId);

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        void ClearAllPools();
    }

    /// <summary>
    /// 输入服务接口 - 管理玩家输入
    /// </summary>
    public interface IInputService
    {
        /// <summary>
        /// 获取屏幕点击位置
        /// </summary>
        bool GetMouseClick(out float3 worldPosition);

        /// <summary>
        /// 获取屏幕触摸位置
        /// </summary>
        bool GetTouch(out float3 worldPosition);

        /// <summary>
        /// 检查按键按�?
        /// </summary>
        bool GetKeyDown(KeyCode key);

        /// <summary>
        /// 检查按键按�?
        /// </summary>
        bool GetKey(KeyCode key);

        /// <summary>
        /// 检查按键抬�?
        /// </summary>
        bool GetKeyUp(KeyCode key);

        /// <summary>
        /// 启用/禁用输入
        /// </summary>
        void SetInputEnabled(bool enabled);
    }

    /// <summary>
    /// UI服务接口 - 管理UI界面的显示和隐藏
    /// </summary>
    public interface IUIService
    {
        /// <summary>
        /// 显示UI界面
        /// </summary>
        void ShowUI(string uiId, object data = null);

        /// <summary>
        /// 隐藏UI界面
        /// </summary>
        void HideUI(string uiId);

        /// <summary>
        /// 显示提示消息
        /// </summary>
        void ShowMessage(string message, float duration = 2f);

        /// <summary>
        /// 显示对话�?
        /// </summary>
        void ShowDialog(string title, string message, System.Action onConfirm = null, System.Action onCancel = null);

        /// <summary>
        /// 显示加载界面
        /// </summary>
        void ShowLoading(bool show, string message = "");
    }

    /// <summary>
    /// 分析统计服务接口 - 收集游戏数据用于分析
    /// </summary>
    public interface IAnalyticsService
    {
        /// <summary>
        /// 记录事件
        /// </summary>
        void TrackEvent(string eventName, params (string key, object value)[] parameters);

        /// <summary>
        /// 记录关卡开�?
        /// </summary>
        void TrackLevelStart(int levelId, string levelName);

        /// <summary>
        /// 记录关卡完成
        /// </summary>
        void TrackLevelComplete(int levelId, bool success, int score, float duration);

        /// <summary>
        /// 记录玩家行为
        /// </summary>
        void TrackPlayerAction(string action, string target);
    }

    /// <summary>
    /// 时间服务接口 - 管理游戏时间和计时器
    /// </summary>
    public interface ITimeService
    {
        /// <summary>
        /// 当前游戏时间（秒�?
        /// </summary>
        float CurrentTime { get; }

        /// <summary>
        /// 当前帧的DeltaTime
        /// </summary>
        float DeltaTime { get; }

        /// <summary>
        /// 游戏时间缩放
        /// </summary>
        float TimeScale { get; set; }

        /// <summary>
        /// 创建计时�?
        /// </summary>
        void CreateTimer(string timerId, float duration, System.Action onComplete, bool loop = false);

        /// <summary>
        /// 取消计时�?
        /// </summary>
        void CancelTimer(string timerId);

        /// <summary>
        /// 暂停游戏
        /// </summary>
        void PauseGame(bool pause);
    }
}
