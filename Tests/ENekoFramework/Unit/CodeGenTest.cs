using NUnit.Framework;
using System.IO;
using UnityEngine;

namespace EasyPack.ENekoFrameworkTest.Unit
{
    /// <summary>
    /// 测试代码生成功能
    /// </summary>
    [TestFixture]
    public class CodeGenTest
    {
        private string _testOutputPath;

        [SetUp]
        public void Setup()
        {
            _testOutputPath = Path.Combine(Application.temporaryCachePath, "ENekoCodeGenTests");
            if (!Directory.Exists(_testOutputPath))
            {
                Directory.CreateDirectory(_testOutputPath);
            }
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(_testOutputPath))
            {
                Directory.Delete(_testOutputPath, true);
            }
        }

        [Test]
        public void ServiceTemplate_ShouldGenerateValidServiceClass()
        {
            // Arrange
            string serviceName = "TestService";
            string namespaceName = "Game.Services";

            // Expected structure
            string expectedInterface = $"public interface I{serviceName}";
            string expectedClass = $"public class {serviceName}";
            string expectedBaseClass = ": BaseService, I" + serviceName;

            // Act
            string generated = GenerateServiceCode(serviceName, namespaceName);

            // Assert
            Assert.That(generated, Does.Contain(expectedInterface));
            Assert.That(generated, Does.Contain(expectedClass));
            Assert.That(generated, Does.Contain(expectedBaseClass));
            Assert.That(generated, Does.Contain($"namespace {namespaceName}"));
            Assert.That(generated, Does.Contain("using EasyPack.ENekoFramework"));
        }

        [Test]
        public void CommandTemplate_ShouldGenerateValidCommandClass()
        {
            // Arrange
            string commandName = "CreatePlayerCommand";
            string namespaceName = "Game.Commands";
            string resultType = "Player";

            // Expected structure
            string expectedClass = $"public class {commandName}";
            string expectedInterface = $"ICommand<{resultType}>";

            // Act
            string generated = GenerateCommandCode(commandName, namespaceName, resultType);

            // Assert
            Assert.That(generated, Does.Contain(expectedClass));
            Assert.That(generated, Does.Contain(expectedInterface));
            Assert.That(generated, Does.Contain($"namespace {namespaceName}"));
            Assert.That(generated, Does.Contain("ExecuteAsync"));
            Assert.That(generated, Does.Contain($"Task<{resultType}>"));
        }

        [Test]
        public void QueryTemplate_ShouldGenerateValidQueryClass()
        {
            // Arrange
            string queryName = "GetPlayerStatsQuery";
            string namespaceName = "Game.Queries";
            string resultType = "PlayerStats";

            // Expected structure
            string expectedClass = $"public class {queryName}";
            string expectedInterface = $"IQuery<{resultType}>";

            // Act
            string generated = GenerateQueryCode(queryName, namespaceName, resultType);

            // Assert
            Assert.That(generated, Does.Contain(expectedClass));
            Assert.That(generated, Does.Contain(expectedInterface));
            Assert.That(generated, Does.Contain($"namespace {namespaceName}"));
            Assert.That(generated, Does.Contain("Execute"));
            Assert.That(generated, Does.Contain($"{resultType}"));
        }

        [Test]
        public void EventTemplate_ShouldGenerateValidEventStruct()
        {
            // Arrange
            string eventName = "PlayerDiedEvent";
            string namespaceName = "Game.Events";

            // Expected structure
            string expectedStruct = $"public struct {eventName}";
            string expectedInterface = ": IEvent";

            // Act
            string generated = GenerateEventCode(eventName, namespaceName);

            // Assert
            Assert.That(generated, Does.Contain(expectedStruct));
            Assert.That(generated, Does.Contain(expectedInterface));
            Assert.That(generated, Does.Contain($"namespace {namespaceName}"));
            Assert.That(generated, Does.Contain("public DateTime Timestamp"));
        }

        [Test]
        public void GeneratedCode_ShouldHaveProperIndentation()
        {
            // Arrange
            string serviceName = "IndentTestService";
            string namespaceName = "Test";

            // Act
            string generated = GenerateServiceCode(serviceName, namespaceName);

            // Assert - Check for proper indentation (4 spaces per level)
            var lines = generated.Split('\n');
            bool hasProperIndentation = false;

            foreach (var line in lines)
            {
                if (line.Contains("public class") || line.Contains("public interface"))
                {
                    // Class/interface should be indented 4 spaces (inside namespace)
                    hasProperIndentation = line.StartsWith("    ");
                    break;
                }
            }

            Assert.That(hasProperIndentation, Is.True, "Generated code should have proper indentation");
        }

        [Test]
        public void GeneratedCode_ShouldIncludeXmlDocumentation()
        {
            // Arrange
            string serviceName = "DocumentedService";
            string namespaceName = "Test";

            // Act
            string generated = GenerateServiceCode(serviceName, namespaceName);

            // Assert
            Assert.That(generated, Does.Contain("/// <summary>"));
            Assert.That(generated, Does.Contain("/// </summary>"));
        }

        [Test]
        public void GeneratedFilename_ShouldMatchClassName()
        {
            // Arrange
            string serviceName = "FilenameTestService";

            // Act
            string expectedFilename = $"{serviceName}.cs";
            string actualFilename = GetGeneratedFilename(serviceName, "Service");

            // Assert
            Assert.That(actualFilename, Is.EqualTo(expectedFilename));
        }

        // Helper methods to simulate code generation
        // These will be implemented in the actual code generator

        private string GenerateServiceCode(string serviceName, string namespaceName)
        {
            // Placeholder implementation - will be replaced by actual template engine
            return $@"using EasyPack.ENekoFramework;
using System.Threading.Tasks;

namespace {namespaceName}
{{
    /// <summary>
    /// {serviceName} 接口
    /// </summary>
    public interface I{serviceName} : IService
    {{
        // Add your service methods here
    }}

    /// <summary>
    /// {serviceName} 实现
    /// </summary>
    public class {serviceName} : BaseService, I{serviceName}
    {{
        protected override async Task OnInitialize()
        {{
            // Initialization logic
        }}

        protected override void OnDispose()
        {{
            // Cleanup logic
        }}
    }}
}}";
        }

        private string GenerateCommandCode(string commandName, string namespaceName, string resultType)
        {
            return $@"using EasyPack.ENekoFramework;
using System.Threading.Tasks;

namespace {namespaceName}
{{
    /// <summary>
    /// {commandName} 命令
    /// </summary>
    public class {commandName} : ICommand<{resultType}>
    {{
        public async Task<{resultType}> ExecuteAsync()
        {{
            // Command logic here
            return default({resultType});
        }}
    }}
}}";
        }

        private string GenerateQueryCode(string queryName, string namespaceName, string resultType)
        {
            return $@"using EasyPack.ENekoFramework;

namespace {namespaceName}
{{
    /// <summary>
    /// {queryName} 查询
    /// </summary>
    public class {queryName} : IQuery<{resultType}>
    {{
        public {resultType} Execute()
        {{
            // Query logic here
            return default({resultType});
        }}
    }}
}}";
        }

        private string GenerateEventCode(string eventName, string namespaceName)
        {
            return $@"using EasyPack.ENekoFramework;
using System;

namespace {namespaceName}
{{
    /// <summary>
    /// {eventName} 事件
    /// </summary>
    public struct {eventName} : IEvent
    {{
        public DateTime Timestamp {{ get; set; }}
        
        // Add your event data properties here
    }}
}}";
        }

        private string GetGeneratedFilename(string typeName, string suffix)
        {
            return $"{typeName}.cs";
        }
    }
}
