﻿using System.Linq;
using NUnit.Framework;
using StatLight.Core.Configuration;
using StatLight.Core.Tests;
using StatLight.Core.Reporting;

namespace StatLight.IntegrationTests.SpecialScenarios
{

#if DEBUG // These tests only run in debug mode
    [TestFixture]
    public class when_something_executing_in_silverlight_throws_up_a_debug_assertion_dialog
        : SpecialScenariosBase
    {
        private ClientTestRunConfiguration _clientTestRunConfiguration;

        protected override ClientTestRunConfiguration ClientTestRunConfiguration
        {
            get
            {
                if (_clientTestRunConfiguration == null)
                {
                    const string prefix = "StatLight.IntegrationTests.Silverlight.When_calling_debug_assert_with_each_overload";
                    _clientTestRunConfiguration = new IntegrationTestClientTestRunConfiguration(
                        new[]
                        {
                            prefix + ".debug_assert_overload_1",
                            prefix + ".debug_assert_overload_2",
                            prefix + ".debug_assert_overload_3",
                            prefix + ".debug_assert_overload_4",
                        });
                }

                return _clientTestRunConfiguration;
            }
        }

        [Test]
        public void the_final_result_should_be_a_failure()
        {
            TestReport.FinalResult.ShouldEqual(RunCompletedState.Failure);
        }

        [Test]
        public void Should_have_detected_three_message_box_failures()
        {
            TestReport.TotalFailed.ShouldEqual(4);
        }

        [Test]
        public void Should_have_scraped_the__debug_assert_overload_1__test_message_box_info()
        {
            var nonEmptyOtherInfoResults = TestReport.TestResults.Single(w => w.MethodName.Equals("debug_assert_overload_1"));
            nonEmptyOtherInfoResults.OtherInfo
                .ShouldContain("at When_calling_debug_assert_with_each_overload.debug_assert_overload_1()");
        }
    }
#endif
}


