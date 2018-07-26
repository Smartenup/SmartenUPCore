using Autofac;
using Autofac.Core;
using Nop.Core.Configuration;
using Nop.Core.Data;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Nop.Data;
using Nop.Web.Framework.Mvc;
using SmartenUP.Core.Data;
using SmartenUP.Core.Domain;
using SmartenUP.Core.Services;

namespace SmartenUP.Core
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order => 0;

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            builder.RegisterType<HolidayService>().As<IHolidayService>().InstancePerLifetimeScope();
            builder.RegisterType<OrderNoteService>().As<IOrderNoteService>().InstancePerLifetimeScope();

            //data context
            this.RegisterPluginDataContext<SmartenUPObjectContext>(builder, "nop_object_context_smartenupcore");

            //override required repository with our custom context
            builder.RegisterType<EfRepository<Holiday>>()
                .As<IRepository<Holiday>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>("nop_object_context_smartenupcore"))
                .InstancePerLifetimeScope();

        }
    }
}
