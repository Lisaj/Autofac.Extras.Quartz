////using System;
////using System.Collections.Generic;
////using System.Linq;
////using System.Threading.Tasks;
////using Microsoft.Extensions.DependencyInjection;
////using Microsoft.Extensions.Logging;
////using Moq;
////using Quartz;
////using Quartz.Impl;
////using Quartz.Spi;
////using Xunit;

////namespace Erzasoft.Quartz.DependencyInjection.Tests
////{
////    public class DependenciInjectionJobFactoryTests
////    {
////        [Fact]
////        public void Should_Create_InterruptableWrapper_For_InterruptableJob()
////        {
////            var serviceProvider = this.PrepairServiceProvider();
////            var scheduler = new Mock<IScheduler>();

////            var factory = new DependenciInjectionJobFactory(serviceProvider.GetService<IServiceScopeFactory>(), new LoggerFactory());

////            var bundle = CreateBundle<InterruptableJob>();
////            var job = factory.NewJob(bundle, scheduler.Object);

////            Assert.NotNull(job as IInterruptableJob);
////        }

////        [Fact]
////        public void Should_Create_NonInterruptableWrapper_For_NonInterruptableJob()
////        {
////            var serviceProvider = this.PrepairServiceProvider();
////            var scheduler = new Mock<IScheduler>();
////            var factory = new DependenciInjectionJobFactory(serviceProvider.GetService<IServiceScopeFactory>(), new LoggerFactory());

////            var bundle = CreateBundle<NonInterruptableJob>();
////            var job = factory.NewJob(bundle, scheduler.Object);

////            Assert.Null(job as IInterruptableJob);
////        }

////        private IServiceProvider PrepairServiceProvider()
////        {
////            var serviceCollection = new ServiceCollection();
////            serviceCollection.AddScoped<InterruptableJob, InterruptableJob>();
////            serviceCollection.AddScoped<NonInterruptableJob, NonInterruptableJob>();
////            return serviceCollection.BuildServiceProvider();
////        }

////        private TriggerFiredBundle CreateBundle<TJob>()
////        {
////            var jobDetailImpl = new JobDetailImpl { JobType = typeof(TJob) };
////            var trigger = new Mock<IOperableTrigger>();
////            var calendar = new Mock<ICalendar>();
////            return new TriggerFiredBundle(jobDetailImpl, trigger.Object, calendar.Object, false, DateTimeOffset.Now,
////                DateTimeOffset.Now, null, null);
////        }

////        private class NonInterruptableJob : IJob
////        {
////            public Task Execute(IJobExecutionContext context)
////            {
////                return Task.FromResult(0);
////            }
////        }

////        private class InterruptableJob : IJob, IInterruptableJob
////        {
////            public void Interrupt()
////            {
////            }

////            public Task Execute(IJobExecutionContext context)
////            {
////                return Task.FromResult(0);
////            }
////        }
////    }
////}
