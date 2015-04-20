using System;
using Xunit;

namespace Microsoft.Framework.Notify.Test
{
    public class NotifierTest
    {
        public class OneTarget
        {
            public int OneCallCount { get; private set; }

            [NotificationName("One")]
            public void One()
            {
                ++OneCallCount;
            }
        }

        [Fact]
        public void ShouldNotifyBecomesTrueAfterEnlisting()
        {
            var notifier = new Notifier();

            Assert.False(notifier.ShouldNotify("One"));
            Assert.False(notifier.ShouldNotify("Two"));

            notifier.EnlistTarget(new OneTarget());

            Assert.True(notifier.ShouldNotify("One"));
            Assert.False(notifier.ShouldNotify("Two"));
        }

        [Fact]
        public void CallingNotifyWillInvokeMethod()
        {
            var notifier = new Notifier();
            var target = new OneTarget();

            notifier.EnlistTarget(target);

            Assert.Equal(0, target.OneCallCount);
            notifier.Notify("One", new { });
            Assert.Equal(1, target.OneCallCount);
        }

        [Fact]
        public void CallingNotifyForNonEnlistedNameIsHarmless()
        {
            var notifier = new Notifier();
            var target = new OneTarget();

            notifier.EnlistTarget(target);

            Assert.Equal(0, target.OneCallCount);
            notifier.Notify("Two", new { });
            Assert.Equal(0, target.OneCallCount);
        }

        private class TwoTarget
        {
            public string Alpha { get; private set; }
            public string Beta { get; private set; }
            public int Delta { get; private set; }

            [NotificationName("Two")]
            public void Two(string alpha, string beta, int delta)
            {
                Alpha = alpha;
                Beta = beta;
                Delta = delta;
            }
        }

        [Fact]
        public void ParametersWillSplatFromObjectByName()
        {
            var notifier = new Notifier();
            var target = new TwoTarget();

            notifier.EnlistTarget(target);

            notifier.Notify("Two", new { alpha = "ALPHA", beta = "BETA", delta = -1 });

            Assert.Equal("ALPHA", target.Alpha);
            Assert.Equal("BETA", target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void ExtraParametersAreHarmless()
        {
            var notifier = new Notifier();
            var target = new TwoTarget();

            notifier.EnlistTarget(target);

            notifier.Notify("Two", new { alpha = "ALPHA", beta = "BETA", delta = -1, extra = this });

            Assert.Equal("ALPHA", target.Alpha);
            Assert.Equal("BETA", target.Beta);
            Assert.Equal(-1, target.Delta);
        }

        [Fact]
        public void MissingParametersArriveAsNull()
        {
            var notifier = new Notifier();
            var target = new TwoTarget();

            notifier.EnlistTarget(target);

            notifier.Notify("Two", new { alpha = "ALPHA", delta = -1 });

            Assert.Equal("ALPHA", target.Alpha);
            Assert.Null(target.Beta);
            Assert.Equal(-1, target.Delta);
        }
    }
}
