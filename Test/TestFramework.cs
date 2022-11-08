using System.Reflection;
using Xunit.Abstractions;
using Xunit.Sdk;

/* solution from https://andrewlock.net/tracking-down-a-hanging-xunit-test-in-ci-building-a-custom-test-framework/ */

//[assembly: TestFramework("Ronin.Test.TestFramework", "Ronin.Test")]

namespace Ronin.Test;

internal class TestFramework : XunitTestFramework
{
    public TestFramework(IMessageSink messageSink) : base(messageSink) { }

    protected override ITestFrameworkExecutor CreateExecutor(AssemblyName assemblyName)
    {
        return new Executor(assemblyName, SourceInformationProvider, DiagnosticMessageSink);
    }

    private class Executor : XunitTestFrameworkExecutor
    {
        public Executor(AssemblyName assemblyName, ISourceInformationProvider sourceInformationProvider, IMessageSink diagnosticMessageSink) 
            : base(assemblyName, sourceInformationProvider, diagnosticMessageSink)
        {
        }

        protected override void RunTestCases(IEnumerable<IXunitTestCase> testCases, IMessageSink executionMessageSink, ITestFrameworkExecutionOptions executionOptions)
        {
            using AssemblyRunner runner = new(TestAssembly, testCases, DiagnosticMessageSink, executionMessageSink, executionOptions);
            runner.RunAsync().Wait();
        }
    }

    private class AssemblyRunner : XunitTestAssemblyRunner
    {
        public AssemblyRunner(
            ITestAssembly testAssembly, 
            IEnumerable<IXunitTestCase> testCases, 
            IMessageSink diagnosticMessageSink, 
            IMessageSink executionMessageSink, 
            ITestFrameworkExecutionOptions executionOptions) 
                : base(testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
        {
        }

        protected override Task<RunSummary> RunTestCollectionAsync(IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
        {
            TestCollectionRunner runner = new(testCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer, Aggregator, cancellationTokenSource);
            return runner.RunAsync();
        }
    }

    private class TestCollectionRunner : XunitTestCollectionRunner
    {
        public TestCollectionRunner(
            ITestCollection testCollection, 
            IEnumerable<IXunitTestCase> testCases, 
            IMessageSink diagnosticMessageSink, 
            IMessageBus messageBus, 
            ITestCaseOrderer testCaseOrderer, 
            ExceptionAggregator aggregator, 
            CancellationTokenSource cancellationTokenSource) 
                : base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
        {
        }

        protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
        {
            TestClassRunner runner = new(testClass, @class, testCases, DiagnosticMessageSink, MessageBus, TestCaseOrderer, Aggregator, CancellationTokenSource, CollectionFixtureMappings);
            return runner.RunAsync();
        }
    }

    private class TestClassRunner : XunitTestClassRunner
    {
        public TestClassRunner(
            ITestClass testClass, 
            IReflectionTypeInfo @class, 
            IEnumerable<IXunitTestCase> testCases, 
            IMessageSink diagnosticMessageSink, 
            IMessageBus messageBus, 
            ITestCaseOrderer testCaseOrderer, 
            ExceptionAggregator aggregator, 
            CancellationTokenSource cancellationTokenSource, 
            IDictionary<Type, object> collectionFixtureMappings) 
                : base(testClass, @class, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource, collectionFixtureMappings)
        {
        }

        protected override Task<RunSummary> RunTestMethodAsync(ITestMethod testMethod, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, object[] constructorArguments)
        {
            TestRunner runner = new(testMethod, Class, method, testCases, DiagnosticMessageSink, MessageBus, Aggregator, CancellationTokenSource, constructorArguments);
            return runner.RunAsync();
        }
    }

    private class TestRunner : XunitTestMethodRunner
    {
        public TestRunner(
            ITestMethod testMethod,
            IReflectionTypeInfo @class,
            IReflectionMethodInfo method,
            IEnumerable<IXunitTestCase> testCases,
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource,
            object[] constructorArguments)
                : base(testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource, constructorArguments)
        {
            _sink = diagnosticMessageSink;
            _args = constructorArguments;
        }

        protected override Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase)
        {
            var summary = testCase.RunAsync(_sink, MessageBus, _args, Aggregator, CancellationTokenSource).GetAwaiter().GetResult();
            return Task.FromResult(summary);
        }

        private readonly IMessageSink _sink;
        private readonly object[] _args;
    }
}


