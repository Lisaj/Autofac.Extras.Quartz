using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using Quartz.Impl;
using Quartz.Simpl;
using Xunit;

namespace Autofac.Extras.Quartz.Test
{
    public class ScopeTrackerTests
    {
        [Fact]
        public async Task ReturnJob_Should_DisposeJobIfMatchingScopeIsMissing()
        {
            var cb = new ServiceCollection();
            cb.AddScoped<SampleJob>();
            cb.AddScoped<DisposableDependency>();

            var serviceProvider = cb.BuildServiceProvider();

            var serviceCsopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var jobFactory = new DependenciInjectionJobFactory(serviceCsopeFactory, new LoggerFactory());

            var job = new Mock<IJob>();
            var disposableJob = job.As<IDisposable>();
            jobFactory.ReturnJob(job.Object);

            disposableJob.Verify(d => d.Dispose(), Times.Once, "Job was not disposed");

            (serviceProvider as IDisposable).Dispose();
        }

        [Fact]
        public async Task ReturnJob_Should_HandleMissingMatchingScope()
        {
            var cb = new ServiceCollection();
            cb.AddScoped<SampleJob>();
            cb.AddScoped<DisposableDependency>();

            var serviceProvider = cb.BuildServiceProvider();


            var serviceCsopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var jobFactory = new DependenciInjectionJobFactory(serviceCsopeFactory, new LoggerFactory());

            var job = new Mock<IJob>();

            Action returnJob = () => jobFactory.ReturnJob(job.Object);
            returnJob();

            (serviceProvider as IDisposable).Dispose();
        }

        [Fact(Skip = "Need design")]
        public async Task ShouldDisposeScopeAfterJobCompletion()
        {
            var cb = new ServiceCollection();
            cb.AddScoped<SampleJob>();
            cb.AddScoped<DisposableDependency>();

            var serviceProvider = cb.BuildServiceProvider();

            var properties = new NameValueCollection();
            properties[StdSchedulerFactory.PropertyJobStoreType] = typeof(RAMJobStore).AssemblyQualifiedName;

            var factory = new StdSchedulerFactory(properties);
            var scheduler = await factory.GetScheduler();
            var serviceCsopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
            var jobFactory = new DependenciInjectionJobFactory(serviceCsopeFactory, new LoggerFactory());

            var key = new JobKey("disposable", "grp2");
            var job1 = JobBuilder.Create<SampleJob>().WithIdentity(key).StoreDurably(true)
                .Build();
            var trigger =
                TriggerBuilder.Create().WithSimpleSchedule(s => s.WithIntervalInSeconds(1).WithRepeatCount(1)).Build();

            var scopesCreated = 0;
            var scopesDisposed = 0;
            DisposableDependency dependency = null;

            //serviceCsopeFactory.ChildLifetimeScopeBeginning += (sender, args) =>
            //{
            //    scopesCreated++;
            //    dependency = args.LifetimeScope.Resolve<DisposableDependency>();
            //    args.LifetimeScope.CurrentScopeEnding += (o, eventArgs) => { scopesDisposed++; };
            //};

            await scheduler.ScheduleJob(job1, trigger);
            await scheduler.Start();

            Thread.Sleep(3000);

            //jobFactory.RunningJobs.Should().BeEmpty("Scope was not disposed after job completion");
            Assert.True(dependency.Disposed);

            await scheduler.Shutdown(waitForJobsToComplete: false);
            (serviceProvider as IDisposable).Dispose();
        }

        [PersistJobDataAfterExecution]
        private class SampleJob : IJob
        {
            private readonly DisposableDependency _dependency;

            /// <summary>
            ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
            /// </summary>
            public SampleJob(DisposableDependency dependency)
            {
                _dependency = dependency;
            }

            public Task Execute(IJobExecutionContext context)
            {
                var data = context.JobDetail.JobDataMap;

                return Task.FromResult(0);
            }
        }

        private class DisposableDependency : IDisposable
        {
            public bool Disposed { get; private set; }

            /// <summary>
            ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
