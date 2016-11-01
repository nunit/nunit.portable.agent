// ***********************************************************************
// Copyright (c) 2016 Charlie Poole
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN METHOD
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Engine.Internal;
using System.Reflection;

namespace NUnit.Engine
{
    /// <summary>
    /// NUnitDriver is used by the test-runner to load and run
    /// tests using the NUnit framework assembly.
    /// </summary>
    public class NUnitPortableDriver
    {
        internal const string INVALID_FRAMEWORK_MESSAGE = "Running tests against this version of the framework using this driver is not supported. Please update NUnit.Framework to the latest version.";
        private const string LOAD_MESSAGE = "Method called without loading any assemblies";
    
        private const string CONTROLLER_TYPE = "NUnit.Framework.Api.FrameworkController";
        private const string LOAD_METHOD = "LoadTests";
        private const string EXPLORE_METHOD = "ExploreTests";
        private const string COUNT_METHOD = "CountTests";
        private const string RUN_METHOD = "RunTests";
        private const string RUN_ASYNC_METHOD = "RunTests";
        private const string STOP_RUN_METHOD = "StopRun";

        private static readonly ILogger log = InternalTrace.GetLogger("NUnit3PortableDriver");
        private readonly List<TestAssemblyWrapper> _testWrappers = new List<TestAssemblyWrapper>();

        /// <summary>
        /// An id prefix that will be passed to the test framework and used as part of the
        /// test ids created.
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// Loads the tests in an assembly.
        /// </summary>
        /// <param name="frameworkAssembly">The NUnit Framework that the tests reference</param>
        /// <param name="testAssembly">The test assembly</param>
        /// <param name="settings">The test settings</param>
        /// <returns>An Xml string representing the loaded test</returns>
        public string Load(Assembly frameworkAssembly, Assembly testAssembly, IDictionary<string, object> settings)
        {
            var idPrefix = string.IsNullOrEmpty(ID) ? "" : ID + "-";
            var frameworkController = CreateObject(CONTROLLER_TYPE, frameworkAssembly, testAssembly, idPrefix, settings);
            if (frameworkController == null)
                throw new NUnitPortableDriverException(INVALID_FRAMEWORK_MESSAGE);

            var testWrapper = new TestAssemblyWrapper(testAssembly, frameworkController);
            _testWrappers.Add(testWrapper);

            log.Info("Loading {0} - see separate log file", testAssembly.FullName);
            return testWrapper.ExecuteMethod(LOAD_METHOD) as string;
        }

        /// <summary>
        /// Counts the number of test cases for the loaded test assembly
        /// </summary>
        /// <param name="filter">The XML test filter</param>
        /// <returns>The number of test cases</returns>
        public int CountTestCases(string filter)
        {
            CheckAssembliesLoaded();

            var count = 0;

            foreach (var wrapper in _testWrappers)
            {
                object assemblyCount = wrapper.ExecuteMethod(COUNT_METHOD, filter);
                if (assemblyCount is int)
                    count += (int)assemblyCount;
            }

            return count;
        }

        /// <summary>
        /// Executes the tests in an assembly.
        /// </summary>
        /// <param name="callback">A callback that receives XML progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        /// <returns>An Xml string representing the result</returns>
        public string Run(Action<string> callback, string filter)
        {
            CheckAssembliesLoaded();

            Func<TestAssemblyWrapper, string> runTestsAction =
                p => p.ExecuteMethod(RUN_METHOD, new[] {typeof(Action<string>), typeof(string)}, callback, filter) as string;
            return SummarizeResults("Running", runTestsAction);
        }

        /// <summary>
        /// Executes the tests in an assembly asyncronously.
        /// </summary>
        /// <param name="callback">A callback that receives XML progress notices</param>
        /// <param name="filter">A filter that controls which tests are executed</param>
        public void RunAsync(Action<string> callback, string filter)
        {
            CheckAssembliesLoaded();

            foreach (var assembly in _testWrappers)
            {
                log.Info("Running {0} - see separate log file", assembly.FullName);
                assembly.ExecuteMethod(RUN_ASYNC_METHOD, new[] { typeof(Action<string>), typeof(string) }, callback, filter);
            }
        }

        /// <summary>
        /// Cancel the ongoing test run. If no test is running, the call is ignored.
        /// </summary>
        /// <param name="force">If true, cancel any ongoing test threads, otherwise wait for them to complete.</param>
        public void StopRun(bool force)
        {
            foreach (var assembly in _testWrappers)
                assembly.ExecuteMethod(STOP_RUN_METHOD, force);
        }

        /// <summary>
        /// Returns information about the tests in an assembly.
        /// </summary>
        /// <param name="filter">A filter indicating which tests to include</param>
        /// <returns>An Xml string representing the tests</returns>
        public string Explore(string filter)
        {
            CheckAssembliesLoaded();

            Func<TestAssemblyWrapper, string> exploreTestsAction = p => p.ExecuteMethod(EXPLORE_METHOD, filter) as string;
            return SummarizeResults("Exploring", exploreTestsAction);
        }

        #region Helper Methods

        private string SummarizeResults(string logTask, Func<TestAssemblyWrapper, string> testAction)
        {
            var summary = new ResultSummary();
            foreach (var assembly in _testWrappers)
            {

                log.Info("{0} {1} - see separate log file", logTask, assembly.FullName);
                summary.AddResult(testAction(assembly));
            }
            return summary.GetTestResults().ToString();
        }

        private void CheckAssembliesLoaded()
        {
            if (_testWrappers.Count == 0)
                throw new InvalidOperationException(LOAD_MESSAGE);
        }

        private static object CreateObject(string typeName, Assembly frameworkAssembly, params object[] args)
        {
            var typeinfo = frameworkAssembly.DefinedTypes.FirstOrDefault(t => t.FullName == typeName);
            if (typeinfo == null)
            {
                log.Error("Could not find type {0}", typeName);
                return null;
            }
            return Activator.CreateInstance(typeinfo.AsType(), args);
        }

        #endregion
    }
}
