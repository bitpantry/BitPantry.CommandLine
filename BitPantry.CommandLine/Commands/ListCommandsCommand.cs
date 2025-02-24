using BitPantry.CommandLine.API;
using Spectre.Console;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;

namespace BitPantry.CommandLine.Commands
{
    [Command(Name = "lc")]
    [Description("Filters and lists registered commands")]
    public class ListCommandsCommand : CommandBase
    {
        [Argument]
        [Alias('f')]
        [Description("A dynamic linq expression used to filter the commands")]
        public string Filter { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            var dataRows = new List<DataRow>();

            foreach (var cmd in ctx.CommandRegistry.Commands)
                dataRows.Add(new DataRow(
                    cmd.Namespace, 
                    cmd.Name, 
                    cmd.IsRemote, 
                    cmd.Description, 
                    cmd.InputType?.AssemblyQualifiedName, 
                    cmd.ReturnType == typeof(void) ? null : cmd.ReturnType.AssemblyQualifiedName));

            var filteredRows = !string.IsNullOrEmpty(Filter)
                ? dataRows.AsQueryable().Where(Filter.Replace("'", "\"")).ToList()
                : dataRows;
                

            // Create a table
            var table = new Table();

            // Define columns
            table.AddColumn("Namespace");
            table.AddColumn("Name");
            table.AddColumn("Is Remote");
            table.AddColumn("Description");
            table.AddColumn("Input Type");
            table.AddColumn("Return Type");

            // Populate table with filtered rows
            foreach (var row in filteredRows)
            {
                table.AddRow(
                    row.Namespace ?? "[grey](None)[/]",
                    row.Name,
                    row.IsRemote ? "[green]✔[/]" : "[red]✘[/]",
                    row.Description ?? "[grey](None)[/]",
                    row.InputType ?? "[grey](None)[/]",
                    row.ReturnType ?? "[grey](None)[/]"
                );
            }

            // Render table in the console

            Console.Write(table);

        }

        private record DataRow(string Namespace, string Name, bool IsRemote, string Description, string InputType, string ReturnType) { }
    }
}
