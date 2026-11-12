using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using DevelopmentDiagnostics.Assertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace DevelopmentDiagnostics.Tests
{
    public abstract class ProjectAssertSubscriberTests
    {
        private static readonly FieldInfo FailedField = typeof(ProjectAssert).GetField(
            "Failed", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo LogToConsoleField = typeof(ProjectAssert).GetField(
            "s_logToConsole", BindingFlags.Static | BindingFlags.NonPublic);

        private Action<ProjectAssertionFailure> m_originalSubscribers;
        private bool m_originalLogToConsole;
        private ILogHandler m_originalLogHandler;
        private GameObject m_context;

        [SetUp]
        public void SetUp()
        {
            // 테스트 중에는 실제 Editor 팝업과 외부 구독자를 호출하지 않는다.
            m_originalSubscribers = (Action<ProjectAssertionFailure>)FailedField.GetValue(null);
            m_originalLogToConsole = (bool)LogToConsoleField.GetValue(null);
            m_originalLogHandler = Debug.unityLogger.logHandler;
            FailedField.SetValue(null, null);
            ProjectAssert.ConfigureConsoleLogging(false);
            m_context = new GameObject("Assert subscriber test context");
        }

        [TearDown]
        public void TearDown()
        {
            Debug.unityLogger.logHandler = m_originalLogHandler;
            FailedField.SetValue(null, m_originalSubscribers);
            ProjectAssert.ConfigureConsoleLogging(m_originalLogToConsole);
            Object.DestroyImmediate(m_context);
            LogAssert.NoUnexpectedReceived();
        }

        [TestCase(0, 0), TestCase(0, 1), TestCase(0, 2)]
        [TestCase(1, 0), TestCase(1, 1), TestCase(1, 2)]
        [TestCase(2, 0), TestCase(2, 1), TestCase(2, 2)]
        [TestCase(3, 0), TestCase(3, 1), TestCase(3, 2)]
        public void ThrowingSubscriberDoesNotInterruptNotification(int variant, int throwingIndex)
        {
            List<int> order = new List<int>();
            List<ProjectAssertionFailure> failures = new List<ProjectAssertionFailure>();
            for (int index = 0; index < 3; index++)
            {
                int subscriberIndex = index;
                ProjectAssert.Failed += failure =>
                {
                    order.Add(subscriberIndex);
                    failures.Add(failure);
                    if (subscriberIndex == throwingIndex)
                    {
                        throw new ApplicationException("subscriber failure");
                    }
                };
            }

            ExpectSubscriberWarning("subscriber failure");
            VerifyOutcome(variant);

            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, order);
            Assert.That(failures.Count, Is.EqualTo(3));
            Assert.That(failures[1], Is.EqualTo(failures[0]));
            Assert.That(failures[2], Is.EqualTo(failures[0]));
            VerifyFailureLocation(failures[0], variant);
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void MultipleSubscriberExceptionsAreAllReported(int variant)
        {
            int lastSubscriberCalls = 0;
            ProjectAssert.Failed += _ => throw new ApplicationException("first failure");
            ProjectAssert.Failed += _ => throw new ApplicationException("second failure");
            ProjectAssert.Failed += _ => lastSubscriberCalls++;

            ExpectSubscriberWarning("first failure");
            ExpectSubscriberWarning("second failure");
            VerifyOutcome(variant);

            Assert.That(lastSubscriberCalls, Is.EqualTo(1));
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void ThrowingWarningLoggerDoesNotInterruptNotification(int variant)
        {
            ThrowingLogHandler logger = new ThrowingLogHandler();
            Debug.unityLogger.logHandler = logger;
            int lastSubscriberCalls = 0;
            ProjectAssert.Failed += _ => throw new ApplicationException("subscriber failure");
            ProjectAssert.Failed += _ => lastSubscriberCalls++;

            VerifyOutcome(variant);

            Assert.That(logger.CallCount, Is.EqualTo(1));
            Assert.That(logger.LastLogType, Is.EqualTo(LogType.Warning));
            Assert.That(lastSubscriberCalls, Is.EqualTo(1));
        }

        [TestCase(0), TestCase(1), TestCase(2), TestCase(3)]
        public void NoSubscribersPreservesAssertBehavior(int variant)
        {
            VerifyOutcome(variant);
        }

        [TestCase(0), TestCase(2)]
        public void ConsoleForwardingPreservesSeverityAndSubscriberIsolation(int variant)
        {
            ProjectAssert.ConfigureConsoleLogging(true);
            int lastSubscriberCalls = 0;
            ProjectAssert.Failed += _ => throw new ApplicationException("subscriber failure");
            ProjectAssert.Failed += _ => lastSubscriberCalls++;

            LogAssert.Expect(
                variant == 0 ? LogType.Warning : LogType.Assert,
                variant == 0 ? "[Project Assert/Normal] original assertion" : "[Project Assert/Critical] original assertion");
            ExpectSubscriberWarning("subscriber failure");
            VerifyOutcome(variant);

            Assert.That(lastSubscriberCalls, Is.EqualTo(1));
        }

        [Test]
        public void SubscriptionChangesApplyToTheNextFailure()
        {
            List<string> order = new List<string>();
            Action<ProjectAssertionFailure> last = _ => order.Add("last");
            Action<ProjectAssertionFailure> added = _ => order.Add("added");
            ProjectAssert.Failed += _ =>
            {
                order.Add("first");
                ProjectAssert.Failed -= last;
                ProjectAssert.Failed += added;
                throw new ApplicationException("subscriber failure");
            };
            ProjectAssert.Failed += last;

            ExpectSubscriberWarning("subscriber failure");
            ProjectAssert.Normal("first assertion");
            CollectionAssert.AreEqual(new[] { "first", "last" }, order);

            order.Clear();
            ExpectSubscriberWarning("subscriber failure");
            ProjectAssert.Normal("second assertion");
            CollectionAssert.AreEqual(new[] { "first", "added" }, order);
        }

        [Test]
        public void SuccessfulConditionsDoNotNotifySubscribers()
        {
            int calls = 0;
            ProjectAssert.Failed += _ => calls++;

            ProjectAssert.Normal(true, "normal success");
            ProjectAssert.Critical(true, "critical success");

            Assert.That(calls, Is.Zero);
        }

        [Test]
        public void CallsWithoutUnityAssertionsDoNotEvaluateArguments()
        {
            int calls = 0;
            ProjectAssert.Failed += _ => calls++;

            Assert.That(ProjectAssertNoSymbolProbe.Run(), Is.Zero);
            Assert.That(calls, Is.Zero);
        }

        private void VerifyOutcome(int variant)
        {
            if (variant < 2)
            {
                Assert.DoesNotThrow(() => ReportFailure(variant));
                return;
            }

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => ReportFailure(variant));
            Assert.That(exception.Message, Is.EqualTo("original assertion"));
            Assert.That(exception.StackTrace, Does.Contain(nameof(ReportFailure)));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void ReportFailure(int variant)
        {
            switch (variant)
            {
                case 0:
                    ProjectAssert.Normal(false, "original assertion", m_context);
                    break;
                case 1:
                    ProjectAssert.Normal("original assertion", m_context);
                    break;
                case 2:
                    ProjectAssert.Critical(false, "original assertion", m_context);
                    break;
                case 3:
                    ProjectAssert.Critical("original assertion", m_context);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(variant));
            }
        }

        private void VerifyFailureLocation(ProjectAssertionFailure failure, int variant)
        {
            Assert.That(failure.Message, Is.EqualTo("original assertion"));
            Assert.That(failure.Severity, Is.EqualTo(variant < 2 ? ProjectAssertSeverity.Normal : ProjectAssertSeverity.Critical));
            Assert.That(failure.Context, Is.SameAs(m_context));
            Assert.That(failure.MemberName, Is.EqualTo(nameof(ReportFailure)));
            Assert.That(failure.FilePath, Does.EndWith("ProjectAssertSubscriberTests.cs"));
            Assert.That(failure.LineNumber, Is.GreaterThan(0));
            string sourcePath = Path.IsPathRooted(failure.FilePath)
                ? failure.FilePath
                : Path.Combine(Application.dataPath, "..", failure.FilePath);
            Assert.That(File.ReadAllLines(sourcePath)[failure.LineNumber - 1], Does.Contain("ProjectAssert."));
            Assert.That(failure.StackTrace, Does.Contain("ProjectAssertSubscriberTests.ReportFailure()"));
            Assert.That(failure.StackTrace, Does.Not.Contain("ProjectAssert.Report()"));
            Assert.That(failure.StackTrace, Does.Not.Contain("ProjectAssert.NotifyFailed()"));
        }

        private static void ExpectSubscriberWarning(string message)
        {
            LogAssert.Expect(LogType.Warning, new Regex(
                @"^\[Project Assert/Subscriber\] Failed subscriber threw an exception:\nSystem.ApplicationException: " +
                Regex.Escape(message) + @"[\s\S]*ProjectAssertSubscriberTests"));
        }

        private sealed class ThrowingLogHandler : ILogHandler
        {
            public int CallCount { get; private set; }

            public LogType LastLogType { get; private set; }

            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                CallCount++;
                LastLogType = logType;
                throw new ApplicationException("warning logger failure");
            }

            public void LogException(Exception exception, Object context)
            {
                throw new ApplicationException("exception logger failure");
            }
        }
    }

    public sealed class ProjectAssertPlayModeTests : ProjectAssertSubscriberTests
    {
    }
}
