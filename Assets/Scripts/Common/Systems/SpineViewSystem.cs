using Spine.Unity;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Framework;
using PVZ;

namespace Common
{
    /// <summary>
    /// Spine 瑙嗗浘绯荤粺 - 澶勭悊浣跨敤 Spine 鍔ㄧ敾鐨勫疄浣擄紙鎬ц兘浼樺寲鐗堬級
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SpineViewSystem : ViewSystemBase
    {
        private Camera _mainCamera;
        private int _frameCounter = 0;
        
        // 鍙厤缃弬鏁帮紙閫氳繃 SetConfig 璁剧疆锟?
        private float _lodNearDistance = 15f;
        private float _lodFarDistance = 30f;
        private int _baseUpdateFrequency = 1;
        private bool _lodEnabled = true;
        private bool _cullingEnabled = true;
        private float _cullingMargin = 0.1f;
        private bool _frameSkipEnabled = true;
        private bool _colorUpdateNearOnly = true;
        
        private IResourceService _resourceService;
        private IModuleContext _context;
        private SpineRenderConfig _config;

        /// <summary>
        /// 鑾峰彇妯″潡涓婁笅鏂囧崟锟?
        /// </summary>
        private IModuleContext GetContext()
        {
            if (_context == null)
            {
                // 锟?GameBootstrap 鍗曚緥鑾峰彇鍏ㄥ眬 Context
                var bootstrap = GameBootstrap.Instance;
                if (bootstrap != null)
                {
                    _context = bootstrap.Context;
                }
            }
            return _context;
        }

        /// <summary>
        /// 鑾峰彇璧勬簮鏈嶅姟锛堜粠鍗曚緥 Context锟?
        /// </summary>
        private IResourceService GetResourceService()
        {
            if (_resourceService == null)
            {
                var context = GetContext();
                if (context != null)
                {
                    _resourceService = context.GetService<IResourceService>();
                }
            }
            return _resourceService;
        }

        /// <summary>
        /// 璁剧疆娓叉煋鍣ㄩ厤缃紙锟?SpineRenderModule 璋冪敤锟?
        /// </summary>
        public void SetConfig(SpineRenderConfig config)
        {
            _config = config;
            
            // 搴旂敤閰嶇疆鍙傛暟
            _lodEnabled = config.lodEnabled;
            _lodNearDistance = config.lodNearDistance;
            _lodFarDistance = config.lodFarDistance;
            _cullingEnabled = config.cullingEnabled;
            _cullingMargin = config.cullingMargin;
            _baseUpdateFrequency = config.baseUpdateFrequency;
            _frameSkipEnabled = config.frameSkipEnabled;
            _colorUpdateNearOnly = config.colorUpdateNearOnly;
            
            UnityEngine.Debug.Log($"[SpineViewSystem] Config applied - LOD:{_lodEnabled}, Near:{_lodNearDistance}, Far:{_lodFarDistance}, Culling:{_cullingEnabled}");
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            // 濡傛灉娌℃湁閰嶇疆锛屼娇鐢ㄩ粯璁わ拷?
            if (_config == null)
            {
                _config = SpineRenderConfig.Default();
                SetConfig(_config);
            }

            var config = ViewSystemConfig.Instance;
            if (!config.enableSpineSystem)
            {
                Enabled = false;
                UnityEngine.Debug.Log("SpineViewSystem is disabled (Spine rendering is not enabled).");
            }
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                UnityEngine.Debug.LogWarning("[SpineViewSystem] Main camera not found!");
            }
        }

        protected override void UpdateViews()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            _frameCounter++;
            var cameraPos = _mainCamera.transform.position;

            // 鏇存柊鎵€鏈変娇锟?Spine 娓叉煋鐨勫疄锟?
            foreach (var (viewState, transform, entity) in
                SystemAPI.Query<RefRW<ViewStateComponent>, RefRO<LocalTransform>>()
                .WithAll<SpineRenderComponent, ViewInstanceComponent>()
                .WithEntityAccess())
            {
                if (!SystemAPI.ManagedAPI.HasComponent<ViewInstanceComponent>(entity))
                    continue;

                var viewInstance = SystemAPI.ManagedAPI.GetComponent<ViewInstanceComponent>(entity);
                ref var viewStateRef = ref viewState.ValueRW;
                var skeleton = viewInstance.SpineSkeletonAnimation;

                if (skeleton == null)
                    continue;

                // 璁＄畻璺濈锛堢敤锟?LOD锟?
                var entityPos = transform.ValueRO.Position;
                float distanceSqr = math.distancesq(new float3(cameraPos.x, cameraPos.y, cameraPos.z), entityPos);
                float distance = math.sqrt(distanceSqr);

                // 瑙嗛敟鍓栭櫎锛堝鏋滃惎鐢級
                if (_cullingEnabled && !IsInCameraView(entityPos))
                {
                    // 灞忓箷澶栫殑鐩存帴绂佺敤
                    if (skeleton.enabled)
                    {
                        skeleton.enabled = false;
                    }
                    continue;
                }

                // 纭繚鍚敤
                if (!skeleton.enabled)
                {
                    skeleton.enabled = true;
                }

                // LOD 璺濈鍒嗙骇鏇存柊锛堝鏋滃惎鐢級
                if (_lodEnabled && _frameSkipEnabled)
                {
                    int updateFrequency = CalculateUpdateFrequency(distance);
                    if (_frameCounter % updateFrequency != 0)
                    {
                        continue; // 璺宠繃姝ゅ抚鏇存柊
                    }
                }

                // 鏇存柊 Spine 鍔ㄧ敾
                if (viewStateRef.NeedsAnimationUpdate)
                {
                    UpdateSpineAnimation(skeleton, ref viewStateRef);
                }

                // 鏇存柊棰滆壊锛堟牴鎹厤缃喅瀹氭槸鍚﹀彧鍦ㄨ繎璺濈锟?
                if (_colorUpdateNearOnly)
                {
                    if (distance < _lodNearDistance)
                    {
                        UpdateSpineColor(skeleton, ref viewStateRef);
                    }
                }
                else
                {
                    UpdateSpineColor(skeleton, ref viewStateRef);
                }
            }
        }

        /// <summary>
        /// 鏍规嵁璺濈璁＄畻鏇存柊棰戠巼
        /// </summary>
        private int CalculateUpdateFrequency(float distance)
        {
            if (!_lodEnabled)
            {
                return _baseUpdateFrequency; // LOD 绂佺敤鏃朵娇鐢ㄥ熀纭€棰戠巼
            }

            if (distance < _lodNearDistance)
            {
                return _baseUpdateFrequency; // 姣忓抚鏇存柊锛堟垨鍩虹棰戠巼锟?
            }
            else if (distance < _lodFarDistance)
            {
                return _baseUpdateFrequency * 2; // 锟?甯ф洿锟?
            }
            else
            {
                return _baseUpdateFrequency * 4; // 锟?甯ф洿锟?
            }
        }

        /// <summary>
        /// 蹇€熸鏌ユ槸鍚﹀湪鐩告満瑙嗛噹锟?
        /// </summary>
        private bool IsInCameraView(float3 worldPos)
        {
            var viewportPos = _mainCamera.WorldToViewportPoint(new Vector3(worldPos.x, worldPos.y, worldPos.z));
            float margin = _cullingMargin;
            return viewportPos.x >= -margin && viewportPos.x <= 1f + margin &&
                   viewportPos.y >= -margin && viewportPos.y <= 1f + margin &&
                   viewportPos.z > 0;
        }

        /// <summary>
        /// 鏇存柊 Spine 鍔ㄧ敾
        /// </summary>
        private void UpdateSpineAnimation(SkeletonAnimation skeleton, ref ViewStateComponent viewState)
        {
            string animationName = GetAnimationName(viewState.CurrentAnimationState);

            // 鏍规嵁鍔ㄧ敾鐘舵€佸喅瀹氭槸鍚﹀惊锟?
            bool loop = viewState.CurrentAnimationState != AnimationState.Death
                     && viewState.CurrentAnimationState != AnimationState.Hurt;

            var current = skeleton.AnimationState.GetCurrent(0);
            if (current != null && current.Animation != null)
            {
                if (current.Animation.Name == animationName && current.Loop == loop)
                {
                    viewState.NeedsAnimationUpdate = false;
                    return;
                }
            }

            skeleton.AnimationState.SetAnimation(0, animationName, loop);
            viewState.NeedsAnimationUpdate = false;
        }

        /// <summary>
        /// 鏇存柊 Spine 棰滆壊
        /// </summary>
        private void UpdateSpineColor(SkeletonAnimation skeleton, ref ViewStateComponent viewState)
        {
            float targetTint = Mathf.Clamp01(viewState.ColorTint);

            if (Mathf.Approximately(viewState.LastAppliedColorTint, targetTint))
                return;

            var skeletonData = skeleton.skeleton;
            skeletonData.R = targetTint;
            skeletonData.G = targetTint;
            skeletonData.B = targetTint;
            skeletonData.A = 1.0f;

            viewState.LastAppliedColorTint = targetTint;
        }
    }
}
