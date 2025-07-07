using FlowSynx.Plugins.Csv.Models;
using System.Dynamic;

namespace FlowSynx.Plugins.Csv.Services;

internal class ReadOperationHandler : ICsvOperationHandler
{
    public IEnumerable<ExpandoObject> Handle(IEnumerable<ExpandoObject> rows, InputParameter parameter)
    {
        return rows;
    }
}