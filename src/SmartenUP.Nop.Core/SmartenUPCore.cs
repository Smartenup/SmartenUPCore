using Nop.Core.Plugins;
using SmartenUP.Core.Data;

namespace SmartenUP.Core
{
    public class SmartenUPCore : BasePlugin
    {
        private readonly SmartenUPObjectContext _context;

        public SmartenUPCore(SmartenUPObjectContext context)
        {
            _context = context;
        }

        public override void Install()
        {
            _context.Install();
            base.Install();
        }

        public override void Uninstall()
        {
            _context.Uninstall();
            base.Uninstall();
        }
    }
}
