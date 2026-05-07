using UnityEngine;

namespace Project.Infrastructure
{
    /// <summary>
    /// Unity MonoBehaviour 单例基类。
    ///
    /// 功能：
    /// 1. 保证同一类型的运行时组件在场景中只有一个实例。
    /// 2. 支持跨场景保留，适合 AppBootstrapper、SettingsManager、CurrentCharacterContext 等全局服务。
    /// 3. 支持通过 Instance 懒加载访问；如果场景中不存在该组件，会自动创建一个 GameObject。
    ///
    /// 使用方式：
    /// public class XxxManager : SingletonMonoBehaviour<XxxManager>
    /// {
    ///     protected override void OnSingletonAwake()
    ///     {
    ///         // 在这里写初始化逻辑，不要直接重写 Awake。
    ///     }
    /// }
    ///
    /// 对外暴露：
    /// - Instance：获取当前单例实例。
    /// - HasInstance：判断单例是否已经存在。
    /// - IsPersistent：子类可重写，控制是否 DontDestroyOnLoad。
    /// - OnSingletonAwake：子类初始化入口。
    /// </summary>
    /// <typeparam name="T">继承 SingletonMonoBehaviour 的具体 MonoBehaviour 类型。</typeparam>
    public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        /// <summary>
        /// 当前单例实例。
        ///
        /// 如果场景中已经存在该类型组件，则直接返回；
        /// 如果不存在，则自动创建一个新的 GameObject 并挂载该组件。
        ///
        /// 返回：
        /// 当前类型的唯一实例。
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                _instance = FindFirstObjectByType<T>();

                if (_instance != null)
                {
                    return _instance;
                }

                GameObject singletonObject = new GameObject($"[Singleton] {typeof(T).Name}");
                _instance = singletonObject.AddComponent<T>();

                return _instance;
            }
        }

        /// <summary>
        /// 判断当前类型的单例实例是否已经存在。
        ///
        /// 返回：
        /// true 表示实例已经存在；false 表示还未创建或已被销毁。
        /// </summary>
        public static bool HasInstance
        {
            get { return _instance != null; }
        }

        /// <summary>
        /// 是否跨场景保留。
        ///
        /// 默认返回 true，表示该单例不会因为切换场景而销毁。
        /// 如果某个子类只想在当前场景内生效，可以重写为 false。
        /// </summary>
        protected virtual bool IsPersistent
        {
            get { return true; }
        }

        /// <summary>
        /// Unity Awake 生命周期入口。
        ///
        /// 注意：
        /// 子类不要直接重写 Awake，否则可能破坏单例初始化流程。
        /// 子类应重写 OnSingletonAwake。
        /// </summary>
        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Debug.LogWarning($"[SingletonMonoBehaviour] 检测到重复单例：{typeof(T).Name}，已销毁重复对象。");
                Destroy(gameObject);
                return;
            }

            _instance = this as T;

            if (IsPersistent)
            {
                DontDestroyOnLoad(gameObject);
            }

            OnSingletonAwake();
        }

        /// <summary>
        /// 子类初始化入口。
        ///
        /// 功能：
        /// 让子类在单例实例确认完成之后执行自己的初始化逻辑。
        ///
        /// 参数：
        /// 无。
        ///
        /// 返回：
        /// 无。
        /// </summary>
        protected virtual void OnSingletonAwake()
        {
        }

        /// <summary>
        /// Unity OnDestroy 生命周期入口。
        ///
        /// 功能：
        /// 当当前对象就是单例实例时，清空静态引用，避免残留无效引用。
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}