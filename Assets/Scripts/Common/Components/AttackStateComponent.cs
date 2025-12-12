using Unity.Entities;

namespace Common
{
    /// <summary>
    /// 攻击状态组�?- 标记实体正在执行攻击动作
    /// 用于驱动视图系统显示攻击动画
    /// </summary>
    public struct AttackStateComponent : IComponentData
    {
        /// <summary>
        /// 攻击开始时�?
        /// </summary>
        public float AttackStartTime;
        
        /// <summary>
        /// 攻击动画持续时间（秒�?
        /// </summary>
        public float AttackAnimationDuration;
        
        /// <summary>
        /// 是否已发射子�?造成伤害
        /// </summary>
        public bool HasDealtDamage;
    }
}
