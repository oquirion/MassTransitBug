using MassTransit.TestFramework.Logging;

namespace MassTransitBug.Test;


 using System;
    using System.Threading.Tasks;
 using MassTransit;
    using MassTransit.Testing;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;
    using Quartz;
 
    public class StateMachineTestFixture<TStateMachine, TInstance>
        where TStateMachine : class, SagaStateMachine<TInstance>
        where TInstance : class, SagaStateMachineInstance
    {
        Task<IScheduler> _scheduler;
        TimeSpan _testOffset;
        protected TStateMachine Machine;
        protected ServiceProvider Provider;
        protected ISagaStateMachineTestHarness<TStateMachine, TInstance> SagaHarness;
        protected ITestHarness TestHarness;

        protected Guid SingleTenantId = NewId.NextGuid();
        
        [OneTimeSetUp]
        public async Task Setup()
        {
            InterceptQuartzSystemTime();

            var collection = new ServiceCollection()
                .AddSingleton<ILoggerFactory>(provider => new TestOutputLoggerFactory(true))
                .AddMassTransitTestHarness(x =>
                {
                    x.SetKebabCaseEndpointNameFormatter();

                    x.AddSagaStateMachine<TStateMachine, TInstance>()
                        .InMemoryRepository();

                    x.AddPublishMessageScheduler();

                    ConfigureMassTransit(x);
                    
                    x.UsingInMemory((context, cfg) =>
                    {
                        cfg.UseInMemoryScheduler(out ISchedulerFactory factory);
                        _scheduler = factory.GetScheduler();
                        cfg.UseInMemoryOutbox();

                        cfg.ConfigureEndpoints(context);
                    });
                });

            ConfigureServices(collection);

            Provider = collection.BuildServiceProvider(true);

            ConfigureLogging();

            TestHarness = Provider.GetTestHarness();

            await TestHarness.Start();

            SagaHarness = TestHarness.GetSagaStateMachineHarness<TStateMachine, TInstance>();
            Machine = SagaHarness.StateMachine;
        }

        protected virtual void ConfigureMassTransit(IBusRegistrationConfigurator configurator)
        {
        }

        protected virtual void ConfigureServices(IServiceCollection collection)
        {
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            await Provider.DisposeAsync();

            RestoreDefaultQuartzSystemTime();
        }

        protected async Task AdvanceSystemTime(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration));

            var scheduler = await _scheduler.ConfigureAwait(false);

            await scheduler.Standby().ConfigureAwait(false);

            _testOffset += duration;

            await scheduler.Start().ConfigureAwait(false);
        }

        void ConfigureLogging()
        {
            var loggerFactory = Provider.GetRequiredService<ILoggerFactory>();

            LogContext.ConfigureCurrentLogContext(loggerFactory);
            //MessageHeaders.Quartz.Logging.LogContext.SetCurrentLogProvider(loggerFactory);
        }

        void InterceptQuartzSystemTime()
        {
            SystemTime.UtcNow = GetUtcNow;
            SystemTime.Now = GetNow;
        }

        static void RestoreDefaultQuartzSystemTime()
        {
            SystemTime.UtcNow = () => DateTimeOffset.UtcNow;
            SystemTime.Now = () => DateTimeOffset.Now;
        }

        DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow + _testOffset;
        }

        DateTimeOffset GetNow()
        {
            return DateTimeOffset.Now + _testOffset;
        }
    }