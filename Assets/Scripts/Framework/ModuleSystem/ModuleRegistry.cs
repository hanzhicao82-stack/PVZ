using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using SystemType = System.Type;
using UDebug = UnityEngine.Debug;

namespace Framework
{
    /// <summary>
    /// 模块注册�?- 管理所有游戏模块的生命周期
    /// </summary>
    public class ModuleRegistry : IModuleContext
    {
        private readonly Dictionary<string, IGameModule> _modules = new Dictionary<string, IGameModule>();
        private readonly Dictionary<SystemType, IGameModule> _modulesByType = new Dictionary<SystemType, IGameModule>();
        private readonly Dictionary<SystemType, object> _services = new Dictionary<SystemType, object>();
        private readonly Dictionary<string, object> _configParameters = new Dictionary<string, object>();
        
        private World _world;
        private List<IGameModule> _sortedModules;
        private bool _isInitialized;

        public World World => _world;

        /// <summary>
        /// 注册模块（不初始化）
        /// </summary>
        public void RegisterModule(IGameModule module)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (_modules.ContainsKey(module.ModuleId))
            {
                UDebug.LogWarning($"模块 {module.ModuleId} 已绋注册，将被覆盖");
            }

            _modules[module.ModuleId] = module;
            _modulesByType[module.GetType()] = module;
            
            UDebug.Log($"注册模块: {module.DisplayName} ({module.ModuleId})");
        }

        /// <summary>
        /// 设置ECS World
        /// </summary>
        public void SetWorld(World world)
        {
            _world = world;
        }

        /// <summary>
        /// 设置配置参数
        /// </summary>
        public void SetConfigParameter(string key, object value)
        {
            _configParameters[key] = value;
        }

        /// <summary>
        /// 初始化所有已注册的模�?
        /// </summary>
        public void InitializeAllModules()
        {
            if (_isInitialized)
            {
                UDebug.LogWarning("模块系统已绋初始化，跳过重复初始化");
                return;
            }

            UDebug.Log("====== 开始初始化模块系统 ======");

            // 解析依赖关系并排�?
            _sortedModules = ResolveDependenciesAndSort();

            // 按顺序初始化模块
            foreach (var module in _sortedModules)
            {
                module.Initialize(this);
            }

            _isInitialized = true;
            UDebug.Log($"====== 模块系统初始化完�?({_sortedModules.Count}个模�? ======");
        }

        /// <summary>
        /// 更新所有模�?
        /// </summary>
        public void UpdateAllModules(float deltaTime)
        {
            if (!_isInitialized || _sortedModules == null)
                return;

            foreach (var module in _sortedModules)
            {
                if (module.IsInitialized)
                {
                    module.Update(deltaTime);
                }
            }
        }

        /// <summary>
        /// 关闭所有模�?
        /// </summary>
        public void ShutdownAllModules()
        {
            if (!_isInitialized)
                return;

            UDebug.Log("====== 开始关闭模块系�?======");

            // 反向顺序关闭
            if (_sortedModules != null)
            {
                for (int i = _sortedModules.Count - 1; i >= 0; i--)
                {
                    _sortedModules[i].Shutdown();
                }
            }

            _isInitialized = false;
            UDebug.Log("====== 模块系统已关�?======");
        }

        /// <summary>
        /// 解析依赖关系并排序模�?
        /// </summary>
        private List<IGameModule> ResolveDependenciesAndSort()
        {
            var sorted = new List<IGameModule>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var module in _modules.Values)
            {
                VisitModule(module, sorted, visited, visiting);
            }

            // 按优先级进行二次排序（相同依赖层级的模块按优先级排序�?
            return sorted.OrderBy(m => m.Priority).ToList();
        }

        /// <summary>
        /// 深度优先遍历模块依赖�?
        /// </summary>
        private void VisitModule(IGameModule module, List<IGameModule> sorted, HashSet<string> visited, HashSet<string> visiting)
        {
            if (visited.Contains(module.ModuleId))
                return;

            if (visiting.Contains(module.ModuleId))
            {
                throw new InvalidOperationException($"检测到循环依赖: {module.ModuleId}");
            }

            visiting.Add(module.ModuleId);

            // 先初始化依赖的模�?
            foreach (var depId in module.Dependencies)
            {
                if (!_modules.TryGetValue(depId, out var dependency))
                {
                    throw new InvalidOperationException($"模块 {module.ModuleId} 依赖的模块 {depId} 未注册");
                }

                VisitModule(dependency, sorted, visited, visiting);
            }

            visiting.Remove(module.ModuleId);
            visited.Add(module.ModuleId);
            sorted.Add(module);
        }

        #region IModuleContext Implementation

        public T GetModule<T>() where T : class, IGameModule
        {
            if (_modulesByType.TryGetValue(typeof(T), out var module))
            {
                return module as T;
            }
            return null;
        }

        public IGameModule GetModule(string moduleId)
        {
            _modules.TryGetValue(moduleId, out var module);
            return module;
        }

        public World GetWorld()
        {
            return _world;
        }

        public EntityManager GetEntityManager()
        {
            if (_world == null)
            {
                throw new System.InvalidOperationException("World未设置，无法获取EntityManager");
            }
            return _world.EntityManager;
        }

        public T GetConfigParameter<T>(string key, T defaultValue = default)
        {
            if (_configParameters.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    UDebug.LogWarning($"无法转换配置参数 {key} 到类�?{typeof(T)}");
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public void RegisterService<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                UDebug.LogWarning($"服务 {type.Name} 已注册，将被覆盖");
            }

            _services[type] = service;
            UDebug.Log($"注册服务: {type.Name}");
        }

        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return service as T;
            }
            return null;
        }

        #endregion
    }
}
