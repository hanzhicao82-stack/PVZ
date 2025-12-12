using System;
using System.Collections.Generic;
using UnityEngine;
using SystemType = System.Type;
using UDebug = UnityEngine.Debug;

namespace Framework
{
    /// <summary>
    /// 模块工厂 - 负责创建模块实例
    /// 支持模块注册和查�?
    /// </summary>
    public static class ModuleFactory
    {
        private static readonly Dictionary<string, SystemType> _moduleTypes = new Dictionary<string, SystemType>();
        private static bool _isInitialized = false;

        /// <summary>
        /// 初始化模块工厂（自动注册所有模块类型）
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            UDebug.Log("初始化模块工�?.");

            // 自动扫描并注册所有实现了IGameModule的类�?
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        // 检查是否实现IGameModule接口且不是抽象类
                        if (typeof(IGameModule).IsAssignableFrom(type) && 
                            !type.IsInterface && 
                            !type.IsAbstract)
                        {
                            // 创建临时实例获取ModuleId
                            try
                            {
                                var instance = Activator.CreateInstance(type) as IGameModule;
                                if (instance != null)
                                {
                                    RegisterModuleType(instance.ModuleId, type);
                                }
                            }
                            catch (Exception ex)
                            {
                                UDebug.LogWarning($"无法实例化模块类�?{type.Name}: {ex.Message}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    UDebug.LogWarning($"扫描程序�?{assembly.FullName} 时出�? {ex.Message}");
                }
            }

            _isInitialized = true;
            UDebug.Log($"模块工厂初始化完成，已注册{_moduleTypes.Count} 个模块类型");
        }

        /// <summary>
        /// 手动注册模块类型
        /// </summary>
        public static void RegisterModuleType(string moduleId, SystemType moduleType)
        {
            if (string.IsNullOrEmpty(moduleId))
                throw new ArgumentException("模块ID不能为空");

            if (moduleType == null)
                throw new ArgumentNullException(nameof(moduleType));

            if (!typeof(IGameModule).IsAssignableFrom(moduleType))
                throw new ArgumentException($"类型 {moduleType.Name} 必须实现 IGameModule 接口");

            if (_moduleTypes.ContainsKey(moduleId))
            {
                UDebug.LogWarning($"模块ID {moduleId} 已注册，类型: {_moduleTypes[moduleId].Name}，将�?{moduleType.Name} 覆盖");
            }

            _moduleTypes[moduleId] = moduleType;
            UDebug.Log($"注册模块类型: {moduleId} -> {moduleType.Name}");
        }

        /// <summary>
        /// 根据模块ID获取模块类型
        /// </summary>
        public static SystemType GetModuleType(string moduleId)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            _moduleTypes.TryGetValue(moduleId, out var type);
            return type;
        }

        /// <summary>
        /// 创建模块实例
        /// </summary>
        public static IGameModule CreateModule(string moduleId)
        {
            var type = GetModuleType(moduleId);
            if (type == null)
            {
                UDebug.LogError($"未找到模�? {moduleId}");
                return null;
            }

            try
            {
                return Activator.CreateInstance(type) as IGameModule;
            }
            catch (Exception ex)
            {
                UDebug.LogError($"创建模块实例失败 {moduleId}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取所有已注册的模块ID
        /// </summary>
        public static IEnumerable<string> GetAllModuleIds()
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _moduleTypes.Keys;
        }

        /// <summary>
        /// 清空注册的模块类�?
        /// </summary>
        public static void Clear()
        {
            _moduleTypes.Clear();
            _isInitialized = false;
        }
    }
}
