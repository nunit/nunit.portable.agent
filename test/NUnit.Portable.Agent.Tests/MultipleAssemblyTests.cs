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
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// ***********************************************************************

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;
using NUnit.Tests.Assemblies;

namespace NUnit.Engine.Tests
{
    public class MultipleAssemblyTests
    {
        const string EMPTY_FILTER = "<filter />";
        private readonly IDictionary<string, object> _settings = new Dictionary<string, object>();

        private Assembly _mockAssembly;
        private Assembly _frameworkAssembly;
        private NUnitPortableDriver _driver;


        [SetUp]
        public void CreateDriver()
        {
            _mockAssembly = typeof(MockAssembly).GetTypeInfo().Assembly;
            _frameworkAssembly = typeof(Assert).GetTypeInfo().Assembly;
            _driver = new NUnitPortableDriver();
        }

        [Test]
        public void LoadReturnsSingleNodePerAssembly()
        {
            for (int i = 0; i < 2; i++)
            {
                var result = XmlHelper.CreateXElement(_driver.Load(_frameworkAssembly, _mockAssembly, _settings));
                Assert.That(result.Name.LocalName, Is.EqualTo("test-suite"));
                Assert.That(result.GetAttribute("type"), Is.EqualTo("Assembly"));
                Assert.That(result.GetAttribute("runstate"), Is.EqualTo("Runnable"));
            }
        }

        [Test]
        public void ExploreTestsAction_AfterLoadMultipleAssemblies_ReturnsAllSuites()
        {
            LoadMultipleAssemblies();
            var result = XmlHelper.CreateXElement(_driver.Explore(EMPTY_FILTER));
            Assert.That(result.Name.LocalName, Is.EqualTo("test-run"));
            Assert.That(result.Elements(XName.Get("test-suite")).Count(), Is.EqualTo(2));
        }

        [Test]
        public void CountTestsAction_AfterLoadMultipleAssemblies_ReturnsCorrectCount()
        {
            LoadMultipleAssemblies();
            Assert.That(_driver.CountTestCases(EMPTY_FILTER), Is.EqualTo(2 * MockAssembly.Tests));
        }

        [Test]
        public void RunTestsAction_AfterLoadMultipleAssemblies_RunsAllAssemblies()
        {
            LoadMultipleAssemblies();
            var result = XmlHelper.CreateXElement(_driver.Run(null, EMPTY_FILTER));

            Assert.That(result.Name.LocalName, Is.EqualTo("test-run"));
            var testSuites = result.Elements(XName.Get("test-suite")).ToList();
            Assert.That(testSuites, Has.Count.EqualTo(2));

            foreach (var suite in testSuites)
                Assert.That(suite.GetAttribute("runstate"), Is.EqualTo("Runnable"));
        }

        private void LoadMultipleAssemblies()
        {
            _driver.Load(_frameworkAssembly, _mockAssembly, _settings);
            _driver.Load(_frameworkAssembly, _mockAssembly, _settings);
        }
    }
}
