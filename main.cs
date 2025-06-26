using VMS.TPS.Common.Model.API;
using System.Diagnostics;
using System.Reflection;
using MLC_Index;


namespace VMS.TPS
{
    public class Script
    {
        public Script()   //Constructor
        { }

        public void Execute(ScriptContext context)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            model _model = new model(context);         
            stopwatch.Stop();
        }
    }
}
