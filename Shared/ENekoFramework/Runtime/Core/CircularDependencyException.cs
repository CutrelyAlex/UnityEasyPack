using System;

namespace EasyPack.ENekoFramework
{
    /// <summary>
    /// 检测到循环依赖时抛出的异常
    /// </summary>
    public class CircularDependencyException : Exception
    {
        /// <summary>
        /// 循环依赖路径（例如: "ServiceA → ServiceB → ServiceC → ServiceA"）
        /// </summary>
        public string DependencyPath { get; }
        
        /// <summary>
        /// 创建循环依赖异常
        /// </summary>
        /// <param name="dependencyPath">依赖路径</param>
        public CircularDependencyException(string dependencyPath)
            : base($"Circular dependency detected: {dependencyPath}")
        {
            DependencyPath = dependencyPath;
        }
    }
}
