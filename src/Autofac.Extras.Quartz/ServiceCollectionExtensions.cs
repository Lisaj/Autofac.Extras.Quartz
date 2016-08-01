#region copyright

// Autofac Quartz integration
// https://github.com/alphacloud/Autofac.Extras.Quartz
// Licensed under MIT license.
// Copyright (c) 2014-2016 Alphacloud.Net

#endregion

namespace Autofac.Extras.Quartz
{
    using System;
    using System.Collections.Specialized;
    using global::Quartz;
    using global::Quartz.Impl;
    using global::Quartz.Spi;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    ///     Registers <see cref="ISchedulerFactory" /> and default <see cref="IScheduler" />.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddJobFactory(this IServiceCollection services, NameValueCollection props)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IJobFactory, DependenciInjectionJobFactory>();

            services.AddSingleton(typeof(ISchedulerFactory), provider =>
            {
                return new DependenciInjectionSchedulerFactory(props, provider.GetRequiredService<IJobFactory>() as DependenciInjectionJobFactory);
            });

            //services.AddSingleton(typeof(Scheduler))

            //builder.Register(c => c.Resolve<ISchedulerFactory>().GetScheduler())
            //    .SingleInstance();

            return services;
        }
    }
}
