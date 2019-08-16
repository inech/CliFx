﻿using System;
using System.Collections.Generic;
using System.Linq;
using CliFx.Internal;
using CliFx.Models;

namespace CliFx.Services
{
    /// <summary>
    /// Default implementation of <see cref="ICommandHelpTextRenderer"/>.
    /// </summary>
    public partial class CommandHelpTextRenderer : ICommandHelpTextRenderer
    {
        /// <inheritdoc />
        public void RenderHelpText(IConsole console, HelpTextSource source)
        {
            console.GuardNotNull(nameof(console));
            source.GuardNotNull(nameof(source));

            // Track position
            var column = 0;
            var row = 0;

            // Get child command schemas
            var childCommandSchemas = source.AvailableCommandSchemas
                .Where(c => source.AvailableCommandSchemas.FindParent(c.Name) == source.TargetCommandSchema)
                .ToArray();

            // Define helper functions

            bool IsEmpty() => column == 0 && row == 0;

            void Render(string text)
            {
                console.Output.Write(text);

                column += text.Length;
            }

            void RenderNewLine()
            {
                console.Output.WriteLine();

                column = 0;
                row++;
            }

            void RenderIndent(int spaces = 2)
            {
                Render(' '.Repeat(spaces));
            }

            void RenderColumnIndent(int spaces = 20, int margin = 2)
            {
                if (column + margin >= spaces)
                {
                    RenderNewLine();
                    RenderIndent(spaces);
                }
                else
                {
                    RenderIndent(spaces - column);
                }
            }

            void RenderWithColor(string text, ConsoleColor foregroundColor)
            {
                console.WithForegroundColor(foregroundColor, () => Render(text));
            }

            void RenderWithColors(string text, ConsoleColor foregroundColor, ConsoleColor backgroundColor)
            {
                console.WithColors(foregroundColor, backgroundColor, () => Render(text));
            }

            void RenderHeader(string text)
            {
                RenderWithColors(text, ConsoleColor.Black, ConsoleColor.DarkGray);
                RenderNewLine();
            }

            void RenderApplicationInfo()
            {
                if (!source.TargetCommandSchema.IsDefault())
                    return;

                // Title
                RenderWithColor(source.ApplicationMetadata.Title, ConsoleColor.Yellow);
                Render(" ");
                RenderWithColor(source.ApplicationMetadata.VersionText, ConsoleColor.Yellow);
                RenderNewLine();
            }

            void RenderDescription()
            {
                if (source.TargetCommandSchema.Description.IsNullOrWhiteSpace())
                    return;

                // Margin
                if (!IsEmpty())
                    RenderNewLine();

                // Header
                RenderHeader("Description");

                // Description
                RenderIndent();
                Render(source.TargetCommandSchema.Description);
                RenderNewLine();
            }

            void RenderUsage()
            {
                // Margin
                if (!IsEmpty())
                    RenderNewLine();

                // Header
                RenderHeader("Usage");

                // Exe name
                RenderIndent();
                Render(source.ApplicationMetadata.ExecutableName);

                // Command name
                if (!source.TargetCommandSchema.IsDefault())
                {
                    Render(" ");
                    RenderWithColor(source.TargetCommandSchema.Name, ConsoleColor.Cyan);
                }

                // Child command
                if (childCommandSchemas.Any())
                {
                    Render(" ");
                    RenderWithColor("[command]", ConsoleColor.Cyan);
                }

                // Options
                Render(" ");
                RenderWithColor("[options]", ConsoleColor.White);
                RenderNewLine();
            }

            void RenderOptions()
            {
                var options = new List<CommandOptionSchema>();
                options.AddRange(source.TargetCommandSchema.Options);

                options.Add(new CommandOptionSchema(null, "help", 'h', null, false, "Shows help text."));

                if (source.TargetCommandSchema.IsDefault())
                    options.Add(new CommandOptionSchema(null, "version", null, null, false, "Shows application version."));

                // Margin
                if (!IsEmpty())
                    RenderNewLine();

                // Header
                RenderHeader("Options");

                // Options
                foreach (var option in options)
                {
                    RenderIndent();

                    // Short name
                    if (option.ShortName != null)
                    {
                        RenderWithColor($"-{option.ShortName}", ConsoleColor.White);
                    }

                    // Delimiter
                    if (!option.Name.IsNullOrWhiteSpace() && option.ShortName != null)
                    {
                        Render("|");
                    }

                    // Name
                    if (!option.Name.IsNullOrWhiteSpace())
                    {
                        RenderWithColor($"--{option.Name}", ConsoleColor.White);
                    }

                    // Description
                    if (!option.Description.IsNullOrWhiteSpace())
                    {
                        RenderColumnIndent();
                        Render(option.Description);
                    }

                    RenderNewLine();
                }
            }

            void RenderChildCommands()
            {
                if (!childCommandSchemas.Any())
                    return;

                // Margin
                if (!IsEmpty())
                    RenderNewLine();

                // Header
                RenderHeader("Commands");

                // Child commands
                foreach (var childCommandSchema in childCommandSchemas)
                {
                    var relativeCommandName = GetRelativeCommandName(childCommandSchema, source.TargetCommandSchema);

                    // Name
                    RenderIndent();
                    RenderWithColor(relativeCommandName, ConsoleColor.Cyan);

                    // Description
                    if (!childCommandSchema.Description.IsNullOrWhiteSpace())
                    {
                        RenderColumnIndent();
                        Render(childCommandSchema.Description);
                    }

                    RenderNewLine();
                }

                // Margin
                RenderNewLine();

                // Child command help tip
                Render("You can run `");
                Render(source.ApplicationMetadata.ExecutableName);

                if (!source.TargetCommandSchema.IsDefault())
                {
                    Render(" ");
                    RenderWithColor(source.TargetCommandSchema.Name, ConsoleColor.Cyan);
                }

                Render(" ");
                RenderWithColor("[command]", ConsoleColor.Cyan);

                Render(" ");
                RenderWithColor("--help", ConsoleColor.White);

                Render("` to show help on a specific command.");

                RenderNewLine();
            }

            // Render everything
            RenderApplicationInfo();
            RenderDescription();
            RenderUsage();
            RenderOptions();
            RenderChildCommands();
        }
    }

    public partial class CommandHelpTextRenderer
    {
        private static string GetRelativeCommandName(CommandSchema commandSchema, CommandSchema parentCommandSchema) =>
            parentCommandSchema.Name.IsNullOrWhiteSpace()
                ? commandSchema.Name
                : commandSchema.Name.Substring(parentCommandSchema.Name.Length + 1);
    }
}