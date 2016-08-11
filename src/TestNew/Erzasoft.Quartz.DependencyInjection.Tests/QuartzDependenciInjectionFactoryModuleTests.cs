using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Impl;
using Quartz.Simpl;
using Quartz.Spi;
using Xunit;

namespace Erzasoft.Quartz.DependencyInjection.Tests
{
    public class QuartzDependenciInjectionFactoryModuleTests
    {
        [Fact]
        public void ShouldRegisterSchedulerFactory()
        {
            var serviceProvider = this.PrepairServiceProvider();

            var factory = serviceProvider.GetRequiredService<ISchedulerFactory>();

            Assert.IsType<DependenciInjectionSchedulerFactory>(factory);
        }

        [Fact]
        public void ShouldRegisterFactoryAsSingleton()
        {
            var serviceProvider = this.PrepairServiceProvider();

            var factory1 = serviceProvider.GetRequiredService<ISchedulerFactory>();
            var factory2 = serviceProvider.GetRequiredService<ISchedulerFactory>();

            Assert.Same(factory1, factory2);
        }

        [Fact]
        public void ShouldRegisterJobFactory()
        {
            var serviceProvider = this.PrepairServiceProvider();

            var factory1 = serviceProvider.GetService<DependenciInjectionJobFactory>();
            var factory2 = serviceProvider.GetService<IJobFactory>();

            Assert.NotNull(factory1);
            Assert.NotNull(factory2);
            Assert.IsType<DependenciInjectionJobFactory>(factory2);
            Assert.Same(factory1, factory2);
        }

        [Fact]
        public void ShouldRegisterSchedulerAsSingleton()
        {
            var serviceProvider = this.PrepairServiceProvider();

            var scheduler1 = serviceProvider.GetRequiredService<IScheduler>();
            var scheduler2 = serviceProvider.GetRequiredService<IScheduler>();

            Assert.Same(scheduler1, scheduler2);
        }

        [Fact]
        public void ShouldExecuteConfigureSchedulerFactoryFunctionIfSet()
        {
            var configuration = new NameValueCollection();
            var customSchedulerName = Guid.NewGuid().ToString();
            configuration[StdSchedulerFactory.PropertySchedulerInstanceName] = customSchedulerName;
            var serviceProvider = this.PrepairServiceProvider(configuration);

            var scheduler = serviceProvider.GetRequiredService<IScheduler>();

            Assert.Equal(customSchedulerName, scheduler.SchedulerName);
        }

        private IServiceProvider PrepairServiceProvider(NameValueCollection properties = null)
        {
            properties = properties ?? new NameValueCollection();
            properties[StdSchedulerFactory.PropertyJobStoreType] = typeof(RAMJobStore).AssemblyQualifiedName;

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging();
            serviceCollection.AddJobFactory(properties);
            return serviceCollection.BuildServiceProvider();
        }
    }
}
