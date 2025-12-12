using System;
using Unity.Entities;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 游戏模块接口 - 所有模块都需要实现此接口
    /// 模块是独立的功能单元，可以通过配置组合
    /// </summary>
    public interface IGameModule
    {
        /// <summary>
        /// 模块唯一标识�?
        /// 例如: "core.ecs", "combat.projectile", "pvz.zombie-system"
        /// </summary>
        string ModuleId { get; }

        /// <summary>
        /// 模块显示名称
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// 模块版本
        /// </summary>
        string Version { get; }

        /// <summary>
        /// 依赖的其他模块ID列表
        /// 模块系统会确保依赖模块先初始�?
        /// </summary>
        string[] Dependencies { get; }

        /// <summary>
        /// 模块初始化优先级（越小越先初始化�?
        /// 默认100，核心模块使�?-50，业务模块使�?00-200
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 初始化模�?
        /// </summary>
        /// <param name="context">模块上下文，提供对其他模块和服务的访�?/param>
        void Initialize(IModuleContext context);

        /// <summary>
        /// 更新模块（可选，不是所有模块都需要更新）
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// 关闭模块，清理资�?
        /// </summary>
        void Shutdown();

        /// <summary>
        /// 模块是否已初始化
        /// </summary>
        bool IsInitialized { get; }
    }

    /// <summary>
    /// 模块上下文接�?- 提供模块间通信和访问核心服�?
    /// </summary>
    public interface IModuleContext
    {
        /// <summary>
        /// 获取其他模块实例
        /// </summary>
        T GetModule<T>() where T : class, IGameModule;

        /// <summary>
        /// 根据ID获取模块
        /// </summary>
        IGameModule GetModule(string moduleId);

        /// <summary>
        /// 获取ECS World
        /// </summary>
        World GetWorld();

        /// <summary>
        /// 获取EntityManager
        /// </summary>
        EntityManager GetEntityManager();

        /// <summary>
        /// 获取配置参数
        /// </summary>
        T GetConfigParameter<T>(string key, T defaultValue = default);

        /// <summary>
        /// 注册服务供其他模块使�?
        /// </summary>
        void RegisterService<T>(T service) where T : class;

        /// <summary>
        /// 获取已注册的服务
        /// </summary>
        T GetService<T>() where T : class;
    }

    /// <summary>
    /// 模块配置数据
    /// </summary>
    [Serializable]
    public class ModuleConfig
    {
        /// <summary>
        /// 模块ID
        /// </summary>
        public string moduleId;

        /// <summary>
        /// 是否启用该模�?
        /// </summary>
        public bool enabled = true;

        /// <summary>
        /// 模块自定义参数（JSON字符串）
        /// </summary>
        public string parametersJson = "{}";

        /// <summary>
        /// 模块加载顺序（覆盖默认优先级�?
        /// </summary>
        public int orderOverride = -1;
    }

    /// <summary>
    /// 模块基类 - 提供通用实现
    /// </summary>
    public abstract class GameModuleBase : IGameModule
    {
        public abstract string ModuleId { get; }
        public abstract string DisplayName { get; }
        public virtual string Version => "1.0.0";
        public virtual string[] Dependencies => Array.Empty<string>();
        public virtual int Priority => 100;
        public bool IsInitialized { get; protected set; }

        protected IModuleContext Context { get; private set; }

        public void Initialize(IModuleContext context)
        {
            if (IsInitialized)
            {
                UnityEngine.Debug.LogWarning($"模块 {ModuleId} 已经初始化，跳过重复初始化");
                return;
            }

            Context = context;
            
            try
            {
                OnInitialize();
                IsInitialized = true;
                UnityEngine.Debug.Log($"模块 [{DisplayName}] ({ModuleId}) 初始化成功");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"模块 [{DisplayName}] ({ModuleId}) 初始化失败: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public virtual void Update(float deltaTime)
        {
            // 默认不需要更新，子类可以重写
        }

        public void Shutdown()
        {
            if (!IsInitialized)
                return;

            try
            {
                OnShutdown();
                IsInitialized = false;
                UnityEngine.Debug.Log($"�?模块 [{DisplayName}] ({ModuleId}) 已关闭");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"�?模块 [{DisplayName}] ({ModuleId}) 关闭失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 子类实现具体的初始化逻辑
        /// </summary>
        protected abstract void OnInitialize();

        /// <summary>
        /// 子类实现具体的关闭逻辑
        /// </summary>
        protected virtual void OnShutdown()
        {
            // 默认空实�?
        }
    }
}
