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

using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace NUnit.Engine.Tests
{
    [TestFixture]
    public class AssemblyHelperTests
    {
#if NET451
        private static readonly string THIS_ASSEMBLY_PATH = "NUnit.Portable.Agent.Tests.exe";
#else
        private static readonly string THIS_ASSEMBLY_PATH = "NUnit.Portable.Agent.Tests.dll";
#endif
        private static readonly string THIS_ASSEMBLY_NAME = "NUnit.Portable.Agent.Tests";

        public void GetNameForAssembly()
        {
            var assemblyName = AssemblyHelper.GetAssemblyName(this.GetType().GetTypeInfo().Assembly);
            Assert.That(assemblyName.Name, Is.EqualTo(THIS_ASSEMBLY_NAME).IgnoreCase);
            Assert.That(assemblyName.FullName, Is.EqualTo(THIS_ASSEMBLY_PATH).IgnoreCase);
        }

        [Test]
        public void GetPathForAssembly()
        {
            string path = AssemblyHelper.GetAssemblyPath(this.GetType().GetTypeInfo().Assembly);
            Assert.That(Path.GetFileName(path), Is.EqualTo(THIS_ASSEMBLY_PATH).IgnoreCase);
            Assert.That(File.Exists(path));
        }

        [Test]
        public void GetPathForType()
        {
            string path = AssemblyHelper.GetAssemblyPath(this.GetType());
            Assert.That(Path.GetFileName(path), Is.EqualTo(THIS_ASSEMBLY_PATH).IgnoreCase);
            Assert.That(File.Exists(path));
        }
    }
}
