using BitPantry.CommandLine.Interface;
using BitPantry.Parsing.Strings;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.API
{
    public abstract class CommandBase
    {
        private IInterface _interface;

        internal void SetInterface(IInterface ifc)
        {
            _interface = ifc;
        }

        #region ////// INTERFACE ////////

        protected Writer StandardOut => _interface.WriterCollection.Standard;
        protected Writer Warning => _interface.WriterCollection.Warning;
        protected Writer Error => _interface.WriterCollection.Error;
        protected Writer Debug => _interface.WriterCollection.Debug;
        protected Writer Verbose => _interface.WriterCollection.Verbose;

        #endregion ////// INTERFACE ////////

        public string ReadLine(bool maskInput = false) => _interface.ReadLine(maskInput);

        protected bool Confirm(string prompt)
        {
            do
            {

                StandardOut.WriteLine(prompt);
                StandardOut.Write("[Y]es or [N]o: ");
                var resp = _interface.ReadKey();

                if (resp == 'Y' || resp == 'y') return true;
                if (resp == 'N' || resp == 'n') return true;

            } while (true);
        }
    }
}
