﻿#region copyright

// Autofac Quartz integration
// https://github.com/alphacloud/Autofac.Extras.Quartz
// Licensed under MIT license.
// Copyright (c) 2014-2016 Alphacloud.Net

#endregion

namespace Autofac.Extras.Quartz
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using global::Quartz;
    using global::Quartz.Spi;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    ///     Resolve Quartz Job and it's dependencies from Autofac container.
    /// </summary>
    /// <remarks>
    ///     Factory retuns wrapper around read job. It wraps job execution in nested lifetime scope.
    /// </remarks>
    public class DependenciInjectionJobFactory : IJobFactory, IDisposable
    {
        readonly IServiceScopeFactory _serviceProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DependenciInjectionJobFactory" /> class.
        /// </summary>
        /// <param name="serviceProvider">The lifetime scope.</param>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="serviceProvider" /> or <paramref name="scopeName" /> is
        ///     <see langword="null" />.
        /// </exception>
        public DependenciInjectionJobFactory(IServiceScopeFactory serviceProvider, ILoggerFactory loggerFactory)
        {
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _serviceProvider = serviceProvider;

            this.Logger = loggerFactory.CreateLogger<DependenciInjectionJobFactory>();
        }

        protected ILogger Logger { get; private set; }

        internal ConcurrentDictionary<object, JobTrackingInfo> RunningJobs { get; } =
            new ConcurrentDictionary<object, JobTrackingInfo>();

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            var runningJobs = RunningJobs.ToArray();
            RunningJobs.Clear();

            if (runningJobs.Length > 0)
            {
                this.Logger.LogInformation("Cleaned {0} scopes for running jobs", runningJobs.Length);
            }
        }

        /// <summary>
        ///     Called by the scheduler at the time of the trigger firing, in order to
        ///     produce a <see cref="T:Quartz.IJob" /> instance on which to call Execute.
        /// </summary>
        /// <remarks>
        ///     It should be extremely rare for this method to throw an exception -
        ///     basically only the the case where there is no way at all to instantiate
        ///     and prepare the Job for execution.  When the exception is thrown, the
        ///     Scheduler will move all triggers associated with the Job into the
        ///     <see cref="F:Quartz.TriggerState.Error" /> state, which will require human
        ///     intervention (e.g. an application restart after fixing whatever
        ///     configuration problem led to the issue wih instantiating the Job.
        /// </remarks>
        /// <param name="bundle">
        ///     The TriggerFiredBundle from which the <see cref="T:Quartz.IJobDetail" />
        ///     and other info relating to the trigger firing can be obtained.
        /// </param>
        /// <param name="scheduler">a handle to the scheduler that is about to execute the job</param>
        /// <throws>SchedulerException if there is a problem instantiating the Job. </throws>
        /// <returns>
        ///     the newly instantiated Job
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="bundle" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="scheduler" /> is <see langword="null" />.</exception>
        /// <exception cref="SchedulerConfigException">
        ///     Error resolving exception. Original exception will be stored in
        ///     <see cref="Exception.InnerException" />.
        /// </exception>
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            if (bundle == null) throw new ArgumentNullException(nameof(bundle));
            if (scheduler == null) throw new ArgumentNullException(nameof(scheduler));

            var jobType = bundle.JobDetail.JobType;

            var nestedScope = _serviceProvider.CreateScope();

            IJob newJob = null;
            try
            {
                newJob = (IJob) nestedScope.ServiceProvider.GetService(jobType);
                var jobTrackingInfo = new JobTrackingInfo(nestedScope);
                RunningJobs[newJob] = jobTrackingInfo;

                if (this.Logger.IsEnabled(LogLevel.Trace))
                {
                    this.Logger.LogTrace("Scope 0x{0:x} associated with Job 0x{1:x}", jobTrackingInfo.Scope.GetHashCode(), newJob.GetHashCode());
                }

                nestedScope = null;
            }
            catch (Exception ex)
            {
                if (nestedScope != null)
                {
                    DisposeScope(newJob, nestedScope);
                }
                throw new SchedulerConfigException(string.Format(CultureInfo.InvariantCulture,
                    "Failed to instantiate Job '{0}' of type '{1}'",
                    bundle.JobDetail.Key, bundle.JobDetail.JobType), ex);
            }
            return newJob;
        }

        /// <summary>
        ///     Allows the the job factory to destroy/cleanup the job if needed.
        /// </summary>
        public void ReturnJob(IJob job)
        {
            if (job == null)
                return;

            JobTrackingInfo trackingInfo;
            if (!RunningJobs.TryRemove(job, out trackingInfo))
            {
                this.Logger.LogWarning("Tracking info for job 0x{0:x} not found", job.GetHashCode());
                // ReSharper disable once SuspiciousTypeConversion.Global
                var disposableJob = job as IDisposable;
                disposableJob?.Dispose();
            }
            else
            {
                DisposeScope(job, trackingInfo.Scope);
            }
        }

        protected void DisposeScope(IJob job, IServiceScope lifetimeScope)
        {
            if (this.Logger.IsEnabled(LogLevel.Trace))
            {
                this.Logger.LogTrace("Disposing Scope 0x{0:x} for Job 0x{1:x}",
                    lifetimeScope?.GetHashCode() ?? 0,
                    job?.GetHashCode() ?? 0);
            }

            lifetimeScope?.Dispose();
        }

        #region Job data

        internal sealed class JobTrackingInfo
        {
            /// <summary>
            ///     Initializes a new instance of the <see cref="T:System.Object" /> class.
            /// </summary>
            public JobTrackingInfo(IServiceScope scope)
            {
                Scope = scope;
            }

            public IServiceScope Scope { get; }
        }

        #endregion Job data
    }
}
