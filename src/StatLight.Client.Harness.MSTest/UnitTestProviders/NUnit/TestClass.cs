﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Silverlight.Testing.UnitTesting.Metadata;
using StatLight.Core.Configuration;

namespace StatLight.Client.Harness.Hosts.MSTest.UnitTestProviders.NUnit
{
    /// <summary>
    /// Test class wrapper.
    /// </summary>
    internal class TestClass : ITestClass
    {
        /// <summary>
        /// Construct a new test class metadata interface.
        /// </summary>
        /// <param name="assembly">Assembly metadata interface object.</param>
        private TestClass(IAssembly assembly)
        {
            _tests = new List<ITestMethod>();

            _m = new Dictionary<Methods, LazyDynamicMethodInfo>(4);
            _m[Methods.ClassCleanup] = null;
            _m[Methods.ClassInitialize] = null;
            _m[Methods.TestCleanup] = null;
            _m[Methods.TestInitialize] = null;

            Assembly = assembly;
        }

        /// <summary>
        /// Creates a new test class wrapper.
        /// </summary>
        /// <param name="assembly">Assembly metadata object.</param>
        /// <param name="testClassType">Type of the class.</param>
        public TestClass(IAssembly assembly, Type testClassType)
            : this(assembly)
        {
            _type = testClassType;

            if (_type == null)
            {
                throw new ArgumentNullException("testClassType");
            }

            _m[Methods.ClassCleanup] = new LazyDynamicMethodInfo(_type, NUnitAttributes.TestFixtureTearDown);
            _m[Methods.ClassInitialize] = new LazyDynamicMethodInfo(_type, NUnitAttributes.TestFixtureSetUp);
            _m[Methods.TestCleanup] = new LazyDynamicMethodInfo(_type, NUnitAttributes.TearDown);
            _m[Methods.TestInitialize] = new LazyDynamicMethodInfo(_type, NUnitAttributes.SetUp);
        }

        /// <summary>
        /// Methods enum.
        /// </summary>
        private enum Methods
        {
            /// <summary>
            /// Initialize method.
            /// </summary>
            ClassInitialize,

            /// <summary>
            /// Cleanup method.
            /// </summary>
            ClassCleanup,

            /// <summary>
            /// Test init method.
            /// </summary>
            TestInitialize,

            /// <summary>
            /// Test cleanup method.
            /// </summary>
            TestCleanup,
        }

        /// <summary>
        /// Test Type.
        /// </summary>
        private Type _type;

        /// <summary>
        /// Collection of test method interface objects.
        /// </summary>
        private ICollection<ITestMethod> _tests;

        /// <summary>
        /// A value indicating whether tests are loaded.
        /// </summary>
        private bool _testsLoaded;

        /// <summary>
        /// A dictionary of method types and method interface objects.
        /// </summary>
        private IDictionary<Methods, LazyDynamicMethodInfo> _m;

        public string Namespace
        {
            get { return _type.Namespace; }
        }

        /// <summary>
        /// Gets the test assembly metadata.
        /// </summary>
        public IAssembly Assembly
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the underlying Type of the test class.
        /// </summary>
        public Type Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Gets the name of the test class.
        /// </summary>
        public string Name
        {
            get { return _type.Name; }
        }

        /// <summary>
        /// Gets a collection of test method  wrapper instances.
        /// </summary>
        /// <returns>A collection of test method interface objects.</returns>
        public ICollection<ITestMethod> GetTestMethods()
        {
            if (!_testsLoaded)
            {
                ICollection<MethodInfo> methods = GetTestMethods(_type);
                _tests = new List<ITestMethod>(methods.Count);
                foreach (MethodInfo method in methods)
                {
                    if (ClientTestRunConfiguration.ContainsMethod(method))
                        if (method.HasAttribute(NUnitAttributes.TestCase))
                        {
                            var dynamicMethods = GetDynamicTestMethodsForTestCases(method);
                            foreach (var dynamicMethod in dynamicMethods)
                            {
                                _tests.Add(dynamicMethod);
                            }
                        }
                        else
                        {
                            _tests.Add(new TestMethod(method));
                        }
                }
                _testsLoaded = true;
            }
            return _tests;
        }

        private static IEnumerable<ITestMethod> GetDynamicTestMethodsForTestCases(MethodInfo method)
        {
            return method.GetAllAttributes(NUnitAttributes.TestCase)
                .Select(testCase => new TestCaseMethod(method, testCase))
                .Cast<ITestMethod>()
                .ToList();
        }

        public static ICollection<System.Reflection.MethodInfo> GetTestMethods(Type type)
        {
            var c = new List<System.Reflection.MethodInfo>();
            foreach (var method in type.GetMethods())
                if (method.HasAttribute(NUnitAttributes.Test))
                    c.Add(method);
            return c;
        }


        /// <summary>
        /// Gets a value indicating whether an Ignore attribute present 
        /// on the class.
        /// </summary>
        public bool Ignore
        {
            get { return _type.HasAttribute(NUnitAttributes.Ignore); }
        }

        /// <summary>
        /// Gets any test initialize method.
        /// </summary>
        public MethodInfo TestInitializeMethod
        {
            get { return _m[Methods.TestInitialize] == null ? null : _m[Methods.TestInitialize].GetMethodInfo(); }
        }

        /// <summary>
        /// Gets any test cleanup method.
        /// </summary>
        public MethodInfo TestCleanupMethod
        {
            get { return _m[Methods.TestCleanup] == null ? null : _m[Methods.TestCleanup].GetMethodInfo(); }
        }

        /// <summary>
        /// Gets any class initialize method.
        /// </summary>
        public MethodInfo ClassInitializeMethod
        {
            get { return _m[Methods.ClassInitialize] == null ? null : _m[Methods.ClassInitialize].GetMethodInfo(); }
        }

        /// <summary>
        /// Gets any class cleanup method.
        /// </summary>
        public MethodInfo ClassCleanupMethod
        {
            get { return _m[Methods.ClassCleanup] == null ? null : _m[Methods.ClassCleanup].GetMethodInfo(); }
        }
    }
}