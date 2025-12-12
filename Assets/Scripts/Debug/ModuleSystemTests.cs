using NUnit.Framework;
using UnityEngine;
using Unity.Entities;
using Framework;

namespace PVZ
{
    /// <summary>
    /// 模块系统单元测试
    /// </summary>
    public class ModuleSystemTests
    {
        private ModuleRegistry _registry;
        private World _testWorld;

        [SetUp]
        public void Setup()
        {
            // 创建测试World
            _testWorld = new World("TestWorld");
            
            // 创建模块注册�?
            _registry = new ModuleRegistry();
            _registry.SetWorld(_testWorld);
        }

        [TearDown]
        public void Teardown()
        {
            // 清理
            _registry?.ShutdownAllModules();
            _testWorld?.Dispose();
            ModuleFactory.Clear();
        }

        [Test]
        public void TestModuleRegistration()
        {
            // 创建并注册模�?
            var module = new CoreECSModule();
            _registry.RegisterModule(module);

            // 验证注册成功
            var retrieved = _registry.GetModule<CoreECSModule>();
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(module.ModuleId, retrieved.ModuleId);
        }

        [Test]
        public void TestModuleInitialization()
        {
            // 注册模块
            var module = new CoreECSModule();
            _registry.RegisterModule(module);

            // 初始�?
            _registry.InitializeAllModules();

            // 验证初始化状�?
            Assert.IsTrue(module.IsInitialized);
        }

        [Test]
        public void TestDependencyResolution()
        {
            // 注册有依赖关系的模块
            var coreModule = new CoreECSModule();
            var viewModule = new RenderViewModule();
            var projectileModule = new CombatProjectileModule();

            // 故意乱序注册
            _registry.RegisterModule(projectileModule);
            _registry.RegisterModule(coreModule);
            _registry.RegisterModule(viewModule);

            // 初始�?
            Assert.DoesNotThrow(() => _registry.InitializeAllModules());

            // 验证所有模块都已初始化
            Assert.IsTrue(coreModule.IsInitialized);
            Assert.IsTrue(viewModule.IsInitialized);
            Assert.IsTrue(projectileModule.IsInitialized);
        }

        [Test]
        public void TestCircularDependencyDetection()
        {
            // 创建有循环依赖的模块（需要创建测试模块）
            var moduleA = new TestModuleA();
            var moduleB = new TestModuleB();

            _registry.RegisterModule(moduleA);
            _registry.RegisterModule(moduleB);

            // 应该抛出异常
            Assert.Throws<System.InvalidOperationException>(() => 
                _registry.InitializeAllModules());
        }

        [Test]
        public void TestMissingDependency()
        {
            // 注册一个依赖缺失的模块
            var projectileModule = new CombatProjectileModule();
            _registry.RegisterModule(projectileModule);

            // 应该抛出异常（因为依赖的模块未注册）
            Assert.Throws<System.InvalidOperationException>(() => 
                _registry.InitializeAllModules());
        }

        [Test]
        public void TestModuleShutdown()
        {
            var module = new CoreECSModule();
            _registry.RegisterModule(module);
            _registry.InitializeAllModules();

            Assert.IsTrue(module.IsInitialized);

            _registry.ShutdownAllModules();

            Assert.IsFalse(module.IsInitialized);
        }

        [Test]
        public void TestConfigParameterAccess()
        {
            _registry.SetConfigParameter("test.param", "test-value");
            _registry.SetConfigParameter("test.number", 123);

            var module = new TestParameterModule();
            _registry.RegisterModule(module);
            _registry.InitializeAllModules();

            // 验证模块能访问配置参�?
            Assert.AreEqual("test-value", module.StringParam);
            Assert.AreEqual(123, module.NumberParam);
        }

        [Test]
        public void TestServiceRegistration()
        {
            var service = new TestService { Value = 42 };
            _registry.RegisterService<ITestService>(service);

            var retrieved = _registry.GetService<ITestService>();
            
            Assert.IsNotNull(retrieved);
            Assert.AreEqual(42, retrieved.Value);
        }

        [Test]
        public void TestModuleFactory()
        {
            ModuleFactory.Initialize();

            // 验证核心模块已注�?
            var coreType = ModuleFactory.GetModuleType("core.ecs");
            Assert.IsNotNull(coreType);
            Assert.AreEqual(typeof(CoreECSModule), coreType);

            // 创建模块实例
            var module = ModuleFactory.CreateModule("core.ecs");
            Assert.IsNotNull(module);
            Assert.IsInstanceOf<CoreECSModule>(module);
        }

        [Test]
        public void TestModuleFactoryAutoDiscovery()
        {
            ModuleFactory.Initialize();

            var moduleIds = ModuleFactory.GetAllModuleIds();
            
            // 验证至少发现了核心模�?
            Assert.IsTrue(System.Linq.Enumerable.Contains(moduleIds, "core.ecs"));
            Assert.IsTrue(System.Linq.Enumerable.Contains(moduleIds, "render.view"));
        }

        [Test]
        public void TestModulePriorityOrdering()
        {
            var core = new CoreECSModule();      // Priority 0
            var view = new RenderViewModule();   // Priority 50
            var plant = new PVZPlantSystemModule(); // Priority 100

            // 乱序注册
            _registry.RegisterModule(plant);
            _registry.RegisterModule(core);
            _registry.RegisterModule(view);

            _registry.InitializeAllModules();

            // 所有模块都应该成功初始化（按正确顺序）
            Assert.IsTrue(core.IsInitialized);
            Assert.IsTrue(view.IsInitialized);
            Assert.IsTrue(plant.IsInitialized);
        }

        #region Test Helper Classes

        // 测试用模块A（依赖B�?
        private class TestModuleA : GameModuleBase
        {
            public override string ModuleId => "test.module-a";
            public override string DisplayName => "Test Module A";
            public override string[] Dependencies => new[] { "test.module-b" };

            protected override void OnInitialize() { }
        }

        // 测试用模块B（依赖A，形成循环）
        private class TestModuleB : GameModuleBase
        {
            public override string ModuleId => "test.module-b";
            public override string DisplayName => "Test Module B";
            public override string[] Dependencies => new[] { "test.module-a" };

            protected override void OnInitialize() { }
        }

        // 测试参数访问的模�?
        private class TestParameterModule : GameModuleBase
        {
            public override string ModuleId => "test.parameter";
            public override string DisplayName => "Test Parameter Module";

            public string StringParam { get; private set; }
            public int NumberParam { get; private set; }

            protected override void OnInitialize()
            {
                StringParam = Context.GetConfigParameter<string>("test.param", "default");
                NumberParam = Context.GetConfigParameter<int>("test.number", 0);
            }
        }

        // 测试服务接口
        private interface ITestService
        {
            int Value { get; set; }
        }

        // 测试服务实现
        private class TestService : ITestService
        {
            public int Value { get; set; }
        }

        #endregion
    }
}
