using FlowSynx.Plugins.Csv.Models;
using System.Dynamic;

namespace FlowSynx.Plugins.Csv.Services;

internal interface ICsvOperationHandler
{
    IEnumerable<ExpandoObject> Handle(IEnumerable<ExpandoObject> rows, InputParameter parameter);
}